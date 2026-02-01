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

sealed unsafe partial class LocalAssetLoader : ILocalAssetLoader, IModelImplProvider, IDisposable {
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly InteropStringBuffer _assetFilePathBuffer;
	bool _isDisposed = false;

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
		_assetFilePathBuffer = new InteropStringBuffer(config.MaxAssetFilePathLengthChars, addOneForNullTerminator: true);
		_vertexTriangleBufferPool = new FixedByteBufferPool(config.MaxAssetVertexIndexBufferSizeBytes);
		_ktxFileBufferPool = new FixedByteBufferPool(config.MaxKtxFileBufferSizeBytes);
		_embeddedAssetTextureBufferPool = new FixedByteBufferPool(config.MaxEmbeddedAssetTextureFileSizeBytes);
		_maxHdrProcessingTime = config.MaxHdrProcessingTime;
		_backdropTextureImplProvider = new BackdropTextureImplProvider(this);

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

	public override string ToString() => _isDisposed ? "TinyFFR Local Asset Loader [Disposed]" : "TinyFFR Local Asset Loader";

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var model in _loadedModels.Keys) Dispose(model, removeFromCollection: false);
			foreach (var backdropTex in _loadedBackdropTextures.Keys) Dispose(backdropTex, removeFromCollection: false);
			_embeddedAssetTextureBufferPool.Dispose();
			_ktxFileBufferPool.Dispose();
			_vertexTriangleBufferPool.Dispose();
			_assetFilePathBuffer.Dispose();
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