// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Scene;

namespace Egodystonic.TinyFFR.Assets.Local;

sealed class LocalAssetLoader : IAssetLoader, IDisposable { // TODO remove this resource pool provider interface and put it in the globals; then move all name allocations to globals too
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly LocalMeshBuilder _meshBuilder;
	readonly LocalMaterialBuilder _materialBuilder;
	bool _isDisposed = false;

	public IMeshBuilder MeshBuilder => _meshBuilder;
	public IMaterialBuilder MaterialBuilder => _materialBuilder;

	public LocalAssetLoader(LocalFactoryGlobalObjectGroup globals, LocalAssetLoaderConfig config) {
		ArgumentNullException.ThrowIfNull(globals);
		ArgumentNullException.ThrowIfNull(config);

		_globals = globals;
		_meshBuilder = new LocalMeshBuilder(globals);
		_materialBuilder = new LocalMaterialBuilder(globals);
	}

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