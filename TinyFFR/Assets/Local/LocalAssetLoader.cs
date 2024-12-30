// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Local;

sealed class LocalAssetLoader : IAssetLoader, IDisposable {
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly LocalMeshBuilder _meshBuilder;
	readonly LocalMaterialBuilder _materialBuilder;
	bool _isDisposed = false;

	public IMeshBuilder MeshBuilder => _isDisposed ? throw new ObjectDisposedException(nameof(IAssetLoader)) : _meshBuilder;
	public IMaterialBuilder MaterialBuilder => _isDisposed ? throw new ObjectDisposedException(nameof(IAssetLoader)) : _materialBuilder;

	public LocalAssetLoader(LocalFactoryGlobalObjectGroup globals, LocalAssetLoaderConfig config) {
		ArgumentNullException.ThrowIfNull(globals);
		ArgumentNullException.ThrowIfNull(config);

		_globals = globals;
		_meshBuilder = new LocalMeshBuilder(globals);
		_materialBuilder = new LocalMaterialBuilder(globals, config);
	}

	public override string ToString() => _isDisposed ? "TinyFFR Local Asset Loader [Disposed]" : "TinyFFR Local Asset Loader";

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			_meshBuilder.Dispose();
			_materialBuilder.Dispose();
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