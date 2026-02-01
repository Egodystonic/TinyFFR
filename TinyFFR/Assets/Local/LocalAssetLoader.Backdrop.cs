// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.IO;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering.Local.Sync;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Local;

unsafe partial class LocalAssetLoader {
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
	readonly FixedByteBufferPool _ktxFileBufferPool;
	readonly BackdropTextureImplProvider _backdropTextureImplProvider;
	readonly TimeSpan _maxHdrProcessingTime;
	readonly ArrayPoolBackedMap<ResourceHandle<BackdropTexture>, BackdropTextureData> _loadedBackdropTextures = new();
	nuint _prevBackdropTextureHandle = 0;
	bool _hdrPreprocessorHasBeenExtracted = false;
	
	// This is a private embedded 'delegating' object to help provide distinction between some default interface methods
	// on both IModelImplProvider and IBackdropTextureImplProvider. 
	sealed class BackdropTextureImplProvider : IBackdropTextureImplProvider {
		readonly LocalAssetLoader _owner;

		public BackdropTextureImplProvider(LocalAssetLoader owner) => _owner = owner;

		public UIntPtr GetSkyboxTextureHandle(ResourceHandle<BackdropTexture> handle) => _owner.GetSkyboxTextureHandle(handle);
		public UIntPtr GetIndirectLightingTextureHandle(ResourceHandle<BackdropTexture> handle) => _owner.GetIndirectLightingTextureHandle(handle);
		public string GetNameAsNewStringObject(ResourceHandle<BackdropTexture> handle) => _owner.GetNameAsNewStringObject(handle);
		public int GetNameLength(ResourceHandle<BackdropTexture> handle) => _owner.GetNameLength(handle);
		public void CopyName(ResourceHandle<BackdropTexture> handle, Span<char> destinationBuffer) => _owner.CopyName(handle, destinationBuffer);
		public bool IsDisposed(ResourceHandle<BackdropTexture> handle) => _owner.IsDisposed(handle);
		public void Dispose(ResourceHandle<BackdropTexture> handle) => _owner.Dispose(handle);
		public override string ToString() => _owner.ToString();
	}
	
	void ExtractHdrPreprocessorIfNecessary() {
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

	public void PreprocessHdrTextureToBackdropTextureDirectory(ReadOnlySpan<char> hdrFilePath, ReadOnlySpan<char> destinationDirectoryPath) {
		ThrowIfThisIsDisposed();

		var destDirString = destinationDirectoryPath.ToString();
		var fileString = hdrFilePath.ToString();

		ExtractHdrPreprocessorIfNecessary();

		if (!File.Exists(_hdrPreprocessorFilePath)) {
			throw new InvalidOperationException($"Can not preprocess HDR textures as the preprocessor executable ({_hdrPreprocessorResourceName}) " +
												$"is not present at the expected location ({_hdrPreprocessorFilePath}).");
		}
		if (!File.Exists(fileString)) {
			throw new ArgumentException($"File '{fileString}' does not exist.", nameof(hdrFilePath));
		}
		
		try {
			var process = Process.Start(_hdrPreprocessorFilePath, "-q -f ktx -x \"" + destinationDirectoryPath.ToString() + "\" \"" + fileString + "\"");
			if (!process.WaitForExit(_maxHdrProcessingTime)) {
				try {
					process.Kill(entireProcessTree: true);
				}
#pragma warning disable CA1031 // "Don't catch & swallow exceptions" -- In this case we don't care if we couldn't kill the process, we're going to throw an exception anyway
				catch { /* no op */ }
#pragma warning restore CA1031

				throw new InvalidOperationException($"Aborting HDR preprocessing operation after timeout of {_maxHdrProcessingTime.ToStringMs()}. " +
													$"This value can be altered by setting the {nameof(LocalAssetLoaderConfig.MaxHdrProcessingTime)} configuration " +
													$"value on the {nameof(LocalAssetLoaderConfig)} instance passed in to the factory constructor.");
			}

			if (!Directory.Exists(destDirString) || Directory.GetFiles(destDirString, HdrPreprocessedSkyboxFileSearch).Length == 0 || Directory.GetFiles(destDirString, HdrPreprocessedIblFileSearch).Length == 0) {
				throw new InvalidOperationException($"Error when processing texture. Check arguments and file formats.");
			}
		}
		catch (Exception e) {
			throw new InvalidOperationException("Can not preprocess HDR textures as there was an issue encountered when running the preprocessor executable.", e);
		}
	}
	// TODO xmldoc that the directory should be empty other than the preprocessed hdr file contents
	public BackdropTexture LoadBackdropTextureFromPreprocessedHdrDirectory(ReadOnlySpan<char> directoryPath, in BackdropTextureCreationConfig config) {
		try {
			var dirPathString = directoryPath.ToString();
			var skyboxFile = Directory.GetFiles(dirPathString, HdrPreprocessedSkyboxFileSearch).FirstOrDefault();
			var iblFile = Directory.GetFiles(dirPathString, HdrPreprocessedIblFileSearch).FirstOrDefault();

			if (skyboxFile == null || iblFile == null) {
				throw new InvalidOperationException($"Could not find skybox ({HdrPreprocessedSkyboxFileSearch}) and/or IBL ({HdrPreprocessedIblFileSearch}) file in given directory ({dirPathString}).");
			}

			return LoadBackdropTexture(skyboxFile, iblFile, config);
		}
		catch (Exception e) {
			throw new InvalidOperationException("Could not load processed HDR directory.", e);
		}
	}
	public BackdropTexture LoadBackdropTexture(ReadOnlySpan<char> skyboxKtxFilePath, ReadOnlySpan<char> iblKtxFilePath, in BackdropTextureCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();
		try {
			checked {
				using var skyboxFs = new FileStream(skyboxKtxFilePath.ToString(), FileMode.Open, FileAccess.Read, FileShare.Read);
				using var iblFs = new FileStream(iblKtxFilePath.ToString(), FileMode.Open, FileAccess.Read, FileShare.Read);

				var skyboxFileLen = (int) skyboxFs.Length;
				var skyboxFixedBuffer = _ktxFileBufferPool.Rent(skyboxFileLen);
				skyboxFs.ReadExactly(skyboxFixedBuffer.AsByteSpan[..skyboxFileLen]);
				LoadSkyboxFileInToMemory(
						(byte*) skyboxFixedBuffer.StartPtr, 
						skyboxFileLen, 
						out var skyboxTextureHandle
				).ThrowIfFailure();
				_ktxFileBufferPool.Return(skyboxFixedBuffer);

				var iblFileLen = (int) iblFs.Length;
				var iblFixedBuffer = _ktxFileBufferPool.Rent(iblFileLen);
				iblFs.ReadExactly(iblFixedBuffer.AsByteSpan[..iblFileLen]);
				LoadIblFileInToMemory(
					(byte*) iblFixedBuffer.StartPtr,
					iblFileLen,
					out var iblTextureHandle
				).ThrowIfFailure();
				_ktxFileBufferPool.Return(iblFixedBuffer);

				++_prevBackdropTextureHandle;
				var handle = (ResourceHandle<BackdropTexture>) _prevBackdropTextureHandle;
				_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, config.Name, DefaultBackdropTextureName);
				_loadedBackdropTextures.Add(_prevBackdropTextureHandle, new(skyboxTextureHandle, iblTextureHandle));
				return HandleToInstance(handle);
			}
		}
		catch (Exception e) {
			if (!File.Exists(skyboxKtxFilePath.ToString())) throw new InvalidOperationException($"File '{skyboxKtxFilePath}' does not exist.", e);
			if (!File.Exists(iblKtxFilePath.ToString())) throw new InvalidOperationException($"File '{iblKtxFilePath}' does not exist.", e);
			throw new InvalidOperationException("Error occured when reading and/or loading skybox or IBL file.", e);
		}
	}

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