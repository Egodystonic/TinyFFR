// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.IO;
using Egodystonic.TinyFFR.Assets.Baking;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Rendering.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Threading;

namespace Egodystonic.TinyFFR.Assets.Local;

unsafe partial class LocalAssetLoader : IResourceDirectory<BackdropTexture> {
	readonly record struct BackdropTextureData(UIntPtr SkyboxTextureHandle, UIntPtr IblTextureHandle);
	const string DefaultBackdropTextureName = "Unnamed Backdrop Texture";
	const string HdrPreprocessorNameWin = "cmgen.exe";
	const string HdrPreprocessorNameLinux = "cmgen";
	const string HdrPreprocessorNameMacos = "cmgen_mac";
	const string HdrPreprocessorResourceNameStart = "Assets.Local.";
	const string HdrPreprocessedSkyboxFileSearch = "*_skybox.ktx";
	const string HdrPreprocessedIblFileSearch = "*_ibl.ktx";
	readonly string _hdrPreprocessorFilePath;
	readonly string _hdrPreprocessorResourceName;
	readonly LocalBackdropTextureImplProvider _backdropTextureImplProvider;
	readonly TimeSpan _maxHdrProcessingTime;
	readonly WorkerJobSyncHelper<LocalAssetLoader, BackdropLoadContext, BackdropTextureCreationConfig> _backdropLoadWorkerSyncHelper;
	readonly ArrayPoolBackedMap<ResourceHandle<BackdropTexture>, BackdropTextureData> _loadedBackdropTextures = new();
	nuint _prevBackdropTextureHandle = 0;
	bool _hdrPreprocessorHasBeenExtracted = false;

	#region Creation
	sealed class BackdropLoadContext : WorkerJobSyncHelper<LocalAssetLoader, BackdropLoadContext, BackdropTextureCreationConfig>.WorkerJobSyncHelperContext {
		// Primary thread owned
		public PooledHeapMemory<char>? SkyboxFilePath { get; set; } = null;
		public PooledHeapMemory<char>? IblFilePath { get; set; } = null;
		public PooledHeapMemory<char>? DirectoryPath { get; set; } = null;
		public PooledHeapMemory<char>? HdrOrExrFilePath { get; set; } = null;
		public PooledHeapMemory<char>? DestinationDirectoryPath { get; set; } = null;
		public BackdropTextureResolution Resolution { get; set; } = BackdropTextureResolution.Standard;

		// Worker thread owned
		public PooledHeapMemory<byte>? SkyboxFileData { get; set; } = null;
		public PooledHeapMemory<byte>? IblFileData { get; set; } = null;
		public string? PerCallWorkspaceDirectory { get; set; } = null;

		public override void TearDown() {
			SkyboxFileData?.Dispose();
			SkyboxFileData = null;
			IblFileData?.Dispose();
			IblFileData = null;
			if (PerCallWorkspaceDirectory is { } workspaceDir) {
				try {
					Directory.Delete(workspaceDir, recursive: true);
				}
#pragma warning disable CA1031 // "Don't catch & swallow exceptions" -- A workspace directory we can not delete must not fail the load that already succeeded
				catch (Exception e) {
					Console.WriteLine($"Could not clear out backdrop texture generation workspace directory at {workspaceDir}: {e.GetAllMessages()}");
				}
#pragma warning restore CA1031
				PerCallWorkspaceDirectory = null;
			}
			if (HeapPoolSerializedConfig is { } config) {
				BackdropTextureCreationConfig.DisposeAllocatedHeapStorage(config.Span);
				config.Dispose();
				HeapPoolSerializedConfig = null;
			}
			SkyboxFilePath?.Dispose();
			SkyboxFilePath = null;
			IblFilePath?.Dispose();
			IblFilePath = null;
			DirectoryPath?.Dispose();
			DirectoryPath = null;
			HdrOrExrFilePath?.Dispose();
			HdrOrExrFilePath = null;
			DestinationDirectoryPath?.Dispose();
			DestinationDirectoryPath = null;
			Resolution = BackdropTextureResolution.Standard;
			HeapPool = null!;
			Self = null!;
		}
		
		public override void Dispose() { /* no-op */ }
	}

	void ExtractHdrPreprocessorIfNecessary() {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		if (_hdrPreprocessorHasBeenExtracted) return;

		try {
			var data = EmbeddedResourceResolver.GetResource(_hdrPreprocessorResourceName);
			File.WriteAllBytes(_hdrPreprocessorFilePath, data.AsSpan);
			if (!OperatingSystem.IsWindows()) {
				var chmodProc = Process.Start("chmod", $"+x \"{_hdrPreprocessorFilePath}\"");
				if (!chmodProc.WaitForExit(_maxHdrProcessingTime) || chmodProc.ExitCode != 0) {
					throw new InvalidOperationException($"Could not set execution permission on extracted HDR preprocessor executable (" +
														$"{(chmodProc.HasExited ? $"0x{chmodProc.ExitCode.ToString("x", CultureInfo.InvariantCulture)}" : "timed out")}).");
				}
			}
		}
		catch (Exception e) {
			throw new InvalidOperationException($"Could not extract HDR preprocessor executable ({_hdrPreprocessorResourceName}) " +
												$"to target directory ({LocalFileSystemUtils.ApplicationDataDirectoryPath}).", e);
		}

		_hdrPreprocessorHasBeenExtracted = true;
	}

	void SetUpPreprocessedFileContext(BackdropLoadContext context, ReadOnlySpan<char> skyboxKtxFilePath, ReadOnlySpan<char> iblKtxFilePath, ReadOnlySpan<char> name) {
		context.SkyboxFilePath = _globals.HeapPool.BorrowAndCopy(skyboxKtxFilePath);
		context.IblFilePath = _globals.HeapPool.BorrowAndCopy(iblKtxFilePath);
		context.SetName(name);
	}
	void SetUpDirectoryContext(BackdropLoadContext context, ReadOnlySpan<char> directoryPath, ReadOnlySpan<char> name) {
		context.DirectoryPath = _globals.HeapPool.BorrowAndCopy(directoryPath);
		context.SetName(name);
	}
	void SetUpHdrOrExrContext(BackdropLoadContext context, ReadOnlySpan<char> hdrOrExrFilePath, ReadOnlySpan<char> name, BackdropTextureResolution resolution) {
		ExtractHdrPreprocessorIfNecessary();
		context.HdrOrExrFilePath = _globals.HeapPool.BorrowAndCopy(hdrOrExrFilePath);
		context.Resolution = resolution;
		context.SetName(name);
	}
	void SetUpPreprocessOnlyContext(BackdropLoadContext context, ReadOnlySpan<char> hdrOrExrFilePath, ReadOnlySpan<char> destinationDirectoryPath, BackdropTextureResolution resolution) {
		ExtractHdrPreprocessorIfNecessary();
		context.HdrOrExrFilePath = _globals.HeapPool.BorrowAndCopy(hdrOrExrFilePath);
		context.DestinationDirectoryPath = _globals.HeapPool.BorrowAndCopy(destinationDirectoryPath);
		context.Resolution = resolution;
	}

	public void PreprocessHdrOrExrTextureToBackdropTextureDirectory(ReadOnlySpan<char> hdrOrExrFilePath, ReadOnlySpan<char> destinationDirectoryPath, BackdropTextureResolution backdropTextureResolution = BackdropTextureResolution.Standard) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();

		var contextWrapper = _backdropLoadWorkerSyncHelper.CreateContextWrapper();
		SetUpPreprocessOnlyContext(contextWrapper.Context, hdrOrExrFilePath, destinationDirectoryPath, backdropTextureResolution);

		_ = contextWrapper.DispatchArbitrarySynchronousOperation(&PreprocessHdrOrExrCore);
	}
	public TinyFfrAsyncOperation PreprocessHdrOrExrTextureToBackdropTextureDirectoryAsync(ReadOnlySpan<char> hdrOrExrFilePath, ReadOnlySpan<char> destinationDirectoryPath, BackdropTextureResolution backdropTextureResolution = BackdropTextureResolution.Standard) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();

		var contextWrapper = _backdropLoadWorkerSyncHelper.CreateContextWrapper();
		SetUpPreprocessOnlyContext(contextWrapper.Context, hdrOrExrFilePath, destinationDirectoryPath, backdropTextureResolution);

		return contextWrapper.DispatchArbitraryAsynchronousOperation(&PreprocessHdrOrExrCore);
	}

	public BackdropTexture LoadBackdropTextureFromPreprocessedDirectory(ReadOnlySpan<char> directoryPath, in BackdropTextureCreationConfig config) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		var contextWrapper = _backdropLoadWorkerSyncHelper.CreateContextWrapper();
		SetUpDirectoryContext(contextWrapper.Context, directoryPath, config.Name);

		return contextWrapper.DispatchResourceReturningSynchronousOperation(&LoadBackdropTextureFromPreprocessedDirectoryCore, in config);
	}
	public TinyFfrAsyncOperation<BackdropTexture> LoadBackdropTextureFromPreprocessedDirectoryAsync(ReadOnlySpan<char> directoryPath, in BackdropTextureCreationConfig config) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		var contextWrapper = _backdropLoadWorkerSyncHelper.CreateContextWrapper();
		SetUpDirectoryContext(contextWrapper.Context, directoryPath, config.Name);

		return contextWrapper.DispatchResourceReturningAsynchronousOperation(&LoadBackdropTextureFromPreprocessedDirectoryCore, in config);
	}

	public BackdropTexture LoadPreprocessedBackdropTexture(ReadOnlySpan<char> skyboxKtxFilePath, ReadOnlySpan<char> iblKtxFilePath, in BackdropTextureCreationConfig config) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		var contextWrapper = _backdropLoadWorkerSyncHelper.CreateContextWrapper();
		SetUpPreprocessedFileContext(contextWrapper.Context, skyboxKtxFilePath, iblKtxFilePath, config.Name);

		return contextWrapper.DispatchResourceReturningSynchronousOperation(&LoadPreprocessedBackdropTextureCore, in config);
	}
	public TinyFfrAsyncOperation<BackdropTexture> LoadPreprocessedBackdropTextureAsync(ReadOnlySpan<char> skyboxKtxFilePath, ReadOnlySpan<char> iblKtxFilePath, in BackdropTextureCreationConfig config) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		var contextWrapper = _backdropLoadWorkerSyncHelper.CreateContextWrapper();
		SetUpPreprocessedFileContext(contextWrapper.Context, skyboxKtxFilePath, iblKtxFilePath, config.Name);

		return contextWrapper.DispatchResourceReturningAsynchronousOperation(&LoadPreprocessedBackdropTextureCore, in config);
	}

	public BackdropTexture LoadBackdropTexture(ReadOnlySpan<char> hdrOrExrFilePath, in BackdropTextureCreationConfig config, BackdropTextureResolution backdropTextureResolution = BackdropTextureResolution.Standard) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		var contextWrapper = _backdropLoadWorkerSyncHelper.CreateContextWrapper();
		SetUpHdrOrExrContext(contextWrapper.Context, hdrOrExrFilePath, config.Name, backdropTextureResolution);

		return contextWrapper.DispatchResourceReturningSynchronousOperation(&LoadBackdropTextureCore, in config);
	}
	public TinyFfrAsyncOperation<BackdropTexture> LoadBackdropTextureAsync(ReadOnlySpan<char> hdrOrExrFilePath, in BackdropTextureCreationConfig config, BackdropTextureResolution backdropTextureResolution = BackdropTextureResolution.Standard) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		var contextWrapper = _backdropLoadWorkerSyncHelper.CreateContextWrapper();
		SetUpHdrOrExrContext(contextWrapper.Context, hdrOrExrFilePath, config.Name, backdropTextureResolution);

		return contextWrapper.DispatchResourceReturningAsynchronousOperation(&LoadBackdropTextureCore, in config);
	}

	public BackdropTexture LoadBinaryBackdropTexture(EmbeddedResourceResolver.ResourceDataRef iblData, EmbeddedResourceResolver.ResourceDataRef skyData, in BackdropTextureCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		
		LoadSkyboxFileInToMemory(
			(byte*) skyData.DataPtr, 
			skyData.DataLenBytes, 
			out var skyboxTextureHandle
		).ThrowIfFailure();

		try {
			LoadIblFileInToMemory(
				(byte*) iblData.DataPtr, 
				iblData.DataLenBytes, 
				out var iblTextureHandle
			).ThrowIfFailure();

			return StoreLoadedBackdropTexture(skyboxTextureHandle, iblTextureHandle, config.Name);
		}
		catch {
			UnloadSkyboxFileFromMemory(skyboxTextureHandle);
			throw;
		}
	}

	static object? PreprocessHdrOrExrCore(BackdropLoadContext context) {
		if (context.HdrOrExrFilePath is not { } hdrOrExrFilePath || context.DestinationDirectoryPath is not { } destinationDirectoryPath) {
			throw new InvalidOperationException("No HDR/EXR file path and/or destination directory path set in context (this is a bug in TinyFFR).");
		}

		RunHdrPreprocessor(context, hdrOrExrFilePath.Span, new String(destinationDirectoryPath.Span));
		return null;
	}

	static BackdropTexture LoadBackdropTextureCore(BackdropLoadContext context, in BackdropTextureCreationConfig config) {
		if (context.HdrOrExrFilePath is not { } hdrOrExrFilePath) {
			throw new InvalidOperationException("No HDR/EXR file path set in context (this is a bug in TinyFFR).");
		}

		var workspaceDirectory = Path.Combine(ILocalAssetLoader.HdrExrToKtxWorkspaceDirectoryPath, Guid.NewGuid().ToString("N"));
		try {
			context.PerCallWorkspaceDirectory = workspaceDirectory;
			Directory.CreateDirectory(workspaceDirectory);
			RunHdrPreprocessor(context, hdrOrExrFilePath.Span, workspaceDirectory);
		}
		catch (Exception e) {
			throw new InvalidOperationException("Can not generate and load HDR file: When attempting to preprocess " +
												$"and generate KTX files in directory '{workspaceDirectory}', an error was encountered.", e);
		}

		return LoadBackdropTextureFromDirectory(context, workspaceDirectory);
	}

	static BackdropTexture LoadBackdropTextureFromPreprocessedDirectoryCore(BackdropLoadContext context, in BackdropTextureCreationConfig config) {
		if (context.DirectoryPath is not { } directoryPath) {
			throw new InvalidOperationException("No directory path set in context (this is a bug in TinyFFR).");
		}

		return LoadBackdropTextureFromDirectory(context, new String(directoryPath.Span));
	}

	static BackdropTexture LoadPreprocessedBackdropTextureCore(BackdropLoadContext context, in BackdropTextureCreationConfig config) {
		if (context.SkyboxFilePath is not { } skyboxFilePath || context.IblFilePath is not { } iblFilePath) {
			throw new InvalidOperationException("No skybox and/or IBL file path set in context (this is a bug in TinyFFR).");
		}

		return ReadKtxFilesAndCreateBackdropTexture(context, new String(skyboxFilePath.Span), new String(iblFilePath.Span));
	}

	static BackdropTexture LoadBackdropTextureFromDirectory(BackdropLoadContext context, string directoryPath) {
		string? skyboxFile;
		string? iblFile;
		try {
			skyboxFile = Directory.GetFiles(directoryPath, HdrPreprocessedSkyboxFileSearch).FirstOrDefault();
			iblFile = Directory.GetFiles(directoryPath, HdrPreprocessedIblFileSearch).FirstOrDefault();
		}
		catch (Exception e) {
			throw new InvalidOperationException("Could not load processed HDR directory.", e);
		}

		if (skyboxFile == null || iblFile == null) {
			throw new InvalidOperationException($"Could not find skybox ({HdrPreprocessedSkyboxFileSearch}) and/or IBL ({HdrPreprocessedIblFileSearch}) file in given directory ({directoryPath}).");
		}

		return ReadKtxFilesAndCreateBackdropTexture(context, skyboxFile, iblFile);
	}

	static BackdropTexture ReadKtxFilesAndCreateBackdropTexture(BackdropLoadContext context, string skyboxKtxFilePath, string iblKtxFilePath) {
		try {
			context.SkyboxFileData = LocalFileSystemUtils.ReadFileIntoPooledMemory(context.HeapPool, skyboxKtxFilePath, "KTX file");
			context.IblFileData = LocalFileSystemUtils.ReadFileIntoPooledMemory(context.HeapPool, iblKtxFilePath, "KTX file");
		}
		catch (Exception e) {
			if (!File.Exists(skyboxKtxFilePath)) throw new InvalidOperationException($"File '{skyboxKtxFilePath}' does not exist.", e);
			if (!File.Exists(iblKtxFilePath)) throw new InvalidOperationException($"File '{iblKtxFilePath}' does not exist.", e);
			throw new InvalidOperationException("Error occured when reading and/or loading skybox or IBL file.", e);
		}

		return context.GenerateResourceOnPrimaryAndWait(&CompleteBackdropTextureCreation);
	}

	static void RunHdrPreprocessor(BackdropLoadContext context, ReadOnlySpan<char> hdrOrExrFilePath, string destinationDirectoryPath) {
		var self = context.Self;
		var fileString = new String(hdrOrExrFilePath);

		if (!File.Exists(self._hdrPreprocessorFilePath)) {
			throw new InvalidOperationException($"Can not preprocess HDR textures as the preprocessor executable ({self._hdrPreprocessorResourceName}) " +
												$"is not present at the expected location ({self._hdrPreprocessorFilePath}).");
		}
		if (!File.Exists(fileString)) {
			throw new ArgumentException($"File '{fileString}' does not exist.", nameof(hdrOrExrFilePath));
		}

		try {
			var quality = context.Resolution switch {
				BackdropTextureResolution.RoughDraft => "64",
				BackdropTextureResolution.Higher => "512",
				BackdropTextureResolution.VeryHigh => "2048",
				BackdropTextureResolution.Production => "4096",
				_ => "256"
			};
			var process = Process.Start(self._hdrPreprocessorFilePath, "-q -s " + quality + " -f ktx -x \"" + destinationDirectoryPath + "\" \"" + fileString + "\"");
			if (!process.WaitForExit(self._maxHdrProcessingTime)) {
				try {
					process.Kill(entireProcessTree: true);
				}
#pragma warning disable CA1031 // "Don't catch & swallow exceptions" -- In this case we don't care if we couldn't kill the process, we're going to throw an exception anyway
				catch { /* no op */ }
#pragma warning restore CA1031

				throw new InvalidOperationException($"Aborting HDR preprocessing operation after timeout of {self._maxHdrProcessingTime.ToStringMs()}. " +
													$"This value can be altered by setting the {nameof(LocalAssetLoaderConfig.MaxHdrProcessingTime)} configuration " +
													$"value on the {nameof(LocalAssetLoaderConfig)} instance passed in to the factory constructor.");
			}

			if (!Directory.Exists(destinationDirectoryPath) || Directory.GetFiles(destinationDirectoryPath, HdrPreprocessedSkyboxFileSearch).Length == 0 || Directory.GetFiles(destinationDirectoryPath, HdrPreprocessedIblFileSearch).Length == 0) {
				throw new InvalidOperationException($"Error when processing texture. Check arguments and file formats.");
			}
		}
		catch (Exception e) {
			throw new InvalidOperationException("Can not preprocess HDR textures as there was an issue encountered when running the preprocessor executable.", e);
		}
	}

	static BackdropTexture CompleteBackdropTextureCreation(BackdropLoadContext context) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		if (context.SkyboxFileData is not { } skyboxFileData || context.IblFileData is not { } iblFileData) {
			throw new InvalidOperationException("Skybox and/or IBL file data was null (this is a bug in TinyFFR).");
		}

		var skyboxPin = skyboxFileData.FixData();
		try {
			LoadSkyboxFileInToMemory(
				(byte*) skyboxPin.AddrOfPinnedObject(),
				skyboxFileData.Span.Length,
				out var skyboxTextureHandle
			).ThrowIfFailure();

			try {
				var iblPin = iblFileData.FixData();
				try {
					LoadIblFileInToMemory(
						(byte*) iblPin.AddrOfPinnedObject(),
						iblFileData.Span.Length,
						out var iblTextureHandle
					).ThrowIfFailure();

					var result = context.Self.StoreLoadedBackdropTexture(skyboxTextureHandle, iblTextureHandle, context.Name);
					context.Self.RegisterInBakery(result, skyboxFileData.Span, iblFileData.Span, context.Name);
					return result;
				}
				finally {
					iblPin.Free();
				}
			}
			catch {
				UnloadSkyboxFileFromMemory(skyboxTextureHandle);
				throw;
			}
		}
		finally {
			skyboxPin.Free();
		}
	}

	BackdropTexture StoreLoadedBackdropTexture(UIntPtr skyboxTextureHandle, UIntPtr iblTextureHandle, ReadOnlySpan<char> name) {
		++_prevBackdropTextureHandle;
		var handle = (ResourceHandle<BackdropTexture>) _prevBackdropTextureHandle;
		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, name, DefaultBackdropTextureName);
		_loadedBackdropTextures.Add(_prevBackdropTextureHandle, new(skyboxTextureHandle, iblTextureHandle));
		return HandleToInstance(handle);
	}
	#endregion

	public UIntPtr GetSkyboxTextureHandle(ResourceHandle<BackdropTexture> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedBackdropTextures[handle].SkyboxTextureHandle;
	}
	public UIntPtr GetIndirectLightingTextureHandle(ResourceHandle<BackdropTexture> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedBackdropTextures[handle].IblTextureHandle;
	}

	public string GetNameAsNewStringObject(ResourceHandle<BackdropTexture> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultBackdropTextureName));
	}
	public int GetNameLength(ResourceHandle<BackdropTexture> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultBackdropTextureName).Length;
	}
	public void CopyName(ResourceHandle<BackdropTexture> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultBackdropTextureName, destinationBuffer);
	}

	#region Resource Directory
	unsafe IndirectEnumerable<object, BackdropTexture> IResourceDirectory<BackdropTexture>.AllActiveInstances {
		get {
			static LocalAssetLoader CastSelf(object self) => self as LocalAssetLoader ?? throw new InvalidOperationException($"Enumeration invoked on {self?.GetType().Name}.");
			static int GetCount(object self) => CastSelf(self)._loadedBackdropTextures.Count;
			static int GetVersion(object self) => CastSelf(self)._loadedBackdropTextures.Version;
			static BackdropTexture GetItem(object self, int index) => CastSelf(self).HandleToInstance(CastSelf(self)._loadedBackdropTextures.GetPairAtIndex(index).Key);

			ThrowIfThisIsDisposed();
			return new(
				this,
				GetVersion(this),
				&GetCount,
				&GetVersion,
				&GetItem
			);
		}
	}
	public bool ResourceNameMatchIsMatching(BackdropTexture resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType) {
		var handle = resource.GetHandleWithoutDisposeCheck();
		ThrowIfThisOrHandleIsDisposed(handle);
		return allowPartialMatch
			? _globals.GetResourceName(handle.Ident, DefaultBackdropTextureName).Contains(name, comparisonType)
			: _globals.GetResourceName(handle.Ident, DefaultBackdropTextureName).Equals(name, comparisonType);
	}
	#endregion
	
	#region Baking
	const string BakerySectionBackdropTextureSkyboxData = "skybox";
	const string BakerySectionBackdropTextureIblData = "ibl";
	
	void RegisterInBakery(BackdropTexture resource, ReadOnlySpan<byte> skyboxData, ReadOnlySpan<byte> iblData, ReadOnlySpan<char> name) {
		var bakery = _globals.Bakery;
		if (!bakery.Enabled) return;
		
		bakery.StartResourceBake(resource);
		bakery.AddResourceBakeValue(resource, LocalAssetBakery.ResourceNameSectionName, name);
		bakery.AddResourceBakeValue(resource, BakerySectionBackdropTextureSkyboxData, skyboxData);
		bakery.AddResourceBakeValue(resource, BakerySectionBackdropTextureIblData, iblData);
		bakery.CompleteResourceBake(resource);
	}

	public BackdropTexture LoadBakedBackdropTexture(ReadOnlySpan<char> bakedAssetFilePath) {
		
	}
	
	public TinyFfrAsyncOperation<BackdropTexture> LoadBakedBackdropTextureAsync(ReadOnlySpan<char> bakedAssetFilePath) {

	}
	
	static BackdropTexture LoadBakedBackdropTextureCore(LocalAssetBakery.AssetLoadContext loadContext, ) {
		
	}
	#endregion

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "load_skybox_file_in_to_memory")]
	static extern InteropResult LoadSkyboxFileInToMemory(
		byte* dataPtr,
		int dataLen,
		out UIntPtr outTextureHandle
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "unload_skybox_file_from_memory")]
	static extern InteropResult UnloadSkyboxFileFromMemory(
		UIntPtr textureHandle
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "load_ibl_file_in_to_memory")]
	static extern InteropResult LoadIblFileInToMemory(
		byte* dataPtr,
		int dataLen,
		out UIntPtr outTextureHandle
	);
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "unload_ibl_file_from_memory")]
	static extern InteropResult UnloadIblFileFromMemory(
		UIntPtr textureHandle
	);
	#endregion
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	BackdropTexture HandleToInstance(ResourceHandle<BackdropTexture> h) => new(h, _backdropTextureImplProvider);
	
	#region Disposal
	public bool IsDisposed(ResourceHandle<BackdropTexture> handle) => _isDisposed || !_loadedBackdropTextures.ContainsKey(handle);
	
	public void Dispose(ResourceHandle<BackdropTexture> handle) => Dispose(handle, removeFromCollection: true);
	void Dispose(ResourceHandle<BackdropTexture> handle, bool removeFromCollection) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		var data = _loadedBackdropTextures[handle];
		LocalFrameSynchronizationManager.QueueResourceDisposal(data.IblTextureHandle, &UnloadIblFileFromMemory);
		LocalFrameSynchronizationManager.QueueResourceDisposal(data.SkyboxTextureHandle, &UnloadSkyboxFileFromMemory);
		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (removeFromCollection) _loadedBackdropTextures.Remove(handle);
	}
	
	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<BackdropTexture> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(BackdropTexture));
	#endregion
}
