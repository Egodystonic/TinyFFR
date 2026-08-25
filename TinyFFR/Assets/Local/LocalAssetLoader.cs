// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Local;

sealed unsafe partial class LocalAssetLoader : ILocalAssetLoader, IModelImplProvider, IDisposable {
	internal const int MaxAssetBufferSizeBytes = 1 << 30;

	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly ThreadLocal<InteropStringBuffer> _assetFilePathBufferStore;
	bool _isDisposed = false;

	InteropStringBuffer AssetFilePathBuffer => _assetFilePathBufferStore.Value;

	public IMeshBuilder MeshBuilder => _isDisposed ? throw new ObjectDisposedException(nameof(IAssetLoader)) : _meshBuilder;
	public ITextureBuilder TextureBuilder => _isDisposed ? throw new ObjectDisposedException(nameof(IAssetLoader)) : _textureBuilder;
	public IMaterialBuilder MaterialBuilder => _isDisposed ? throw new ObjectDisposedException(nameof(IAssetLoader)) : _materialBuilder;
	public IBuiltInTexturePathLibrary BuiltInTexturePaths => _isDisposed ? throw new ObjectDisposedException(nameof(IAssetLoader)) : _builtInTextureLibrary;

	public LocalAssetLoader(LocalFactoryGlobalObjectGroup globals, LocalAssetLoaderConfig config) {
		ArgumentNullException.ThrowIfNull(globals);
		ArgumentNullException.ThrowIfNull(config);

		_globals = globals;
		_testMaterialTextures = new(CreateTestMaterialTextures);
		_meshBuilder = new LocalMeshBuilder(globals);
		_textureBuilder = new LocalTextureBuilder(globals, config);
		_materialBuilder = new LocalMaterialBuilder(globals, config, _textureBuilder, _testMaterialTextures);
		_fontLoader = new LocalFontLoader(globals, config, _meshBuilder, _textureBuilder, _materialBuilder);
		var maxAssetFilePathLengthChars = config.MaxAssetFilePathLengthChars;
		_assetFilePathBufferStore = new(() => new InteropStringBuffer(maxAssetFilePathLengthChars, addOneForNullTerminator: true), trackAllValues: true);
		_animationAndNodeNameBuffer = new InteropStringBuffer(config.MaxAnimationAndNodeNameLengthChars, addOneForNullTerminator: true);
		_maxAnimationAndNodeNameLengthChars = config.MaxAnimationAndNodeNameLengthChars;
		_maxHdrProcessingTime = config.MaxHdrProcessingTime;
		_backdropTextureImplProvider = new LocalBackdropTextureImplProvider(this);
		_backdropLoadWorkerSyncHelper = new(this, _globals.HeapPool.ThreadSafeWrapper, _globals.PrimaryThreadDispatcher, _globals.SynchronousWorkScheduler, _globals.ThreadPoolWorkScheduler);
		_meshLoadWorkerSyncHelper = new(this, _globals.HeapPool.ThreadSafeWrapper, _globals.PrimaryThreadDispatcher, _globals.SynchronousWorkScheduler, _globals.ThreadPoolWorkScheduler);
		_textureLoadWorkerSyncHelper = new(this, _globals.HeapPool.ThreadSafeWrapper, _globals.PrimaryThreadDispatcher, _globals.SynchronousWorkScheduler, _globals.ThreadPoolWorkScheduler);
		_combinedTextureLoadWorkerSyncHelper = new(this, _globals.HeapPool.ThreadSafeWrapper, _globals.PrimaryThreadDispatcher, _globals.SynchronousWorkScheduler, _globals.ThreadPoolWorkScheduler);

		if (OperatingSystem.IsWindows()) {
			_hdrPreprocessorFilePath = Path.Combine(LocalFileSystemUtils.ApplicationDataDirectoryPath, HdrPreprocessorNameWin);
			_hdrPreprocessorResourceName = HdrPreprocessorResourceNameStart + HdrPreprocessorNameWin;
		}
		else if (OperatingSystem.IsMacOS()) {
			_hdrPreprocessorFilePath = Path.Combine(LocalFileSystemUtils.ApplicationDataDirectoryPath, HdrPreprocessorNameMacos);
			_hdrPreprocessorResourceName = HdrPreprocessorResourceNameStart + HdrPreprocessorNameMacos;
		}
		else {
			_hdrPreprocessorFilePath = Path.Combine(LocalFileSystemUtils.ApplicationDataDirectoryPath, HdrPreprocessorNameLinux);
			_hdrPreprocessorResourceName = HdrPreprocessorResourceNameStart + HdrPreprocessorNameLinux;
		}
	}

	internal static void ThrowIfAssetBufferSizeExceedsMaximum(long requiredSizeBytes, string assetDescription) {
		if (requiredSizeBytes <= MaxAssetBufferSizeBytes) return;
		throw new InvalidOperationException(
			$"Can not load {assetDescription} because it requires an in-memory buffer of {requiredSizeBytes} bytes, " +
			$"which exceeds the maximum size TinyFFR supports for a single asset buffer ({MaxAssetBufferSizeBytes} bytes)."
		);
	}

	public override string ToString() => _isDisposed ? "TinyFFR Local Asset Loader [Disposed]" : "TinyFFR Local Asset Loader";

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var model in _loadedModels.Keys) Dispose(model, removeFromCollection: false);
			foreach (var backdropTex in _loadedBackdropTextures.Keys) Dispose(backdropTex, removeFromCollection: false);
			_backdropLoadWorkerSyncHelper.Dispose();
			_meshLoadWorkerSyncHelper.Dispose();
			_textureLoadWorkerSyncHelper.Dispose();
			_combinedTextureLoadWorkerSyncHelper.Dispose();
			_primaryThreadGatherBuffers.Dispose();
			_animationAndNodeNameBuffer.Dispose();
			foreach (var pathBuffer in _assetFilePathBufferStore.Values) pathBuffer.Dispose();
			_assetFilePathBufferStore.Dispose();
			_fontLoader.Dispose();
			_meshBuilder.Dispose();
			_materialBuilder.Dispose();

			if (_testMaterialTextures.IsValueCreated) {
				_testMaterialTextures.Value.Dispose(disposeContainedResources: true);
			}

			_textureBuilder.Dispose();
			_loadedBackdropTextures.Dispose();
			_loadedModels.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(IAssetLoader));
	}
	#endregion
}
