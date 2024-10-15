// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Scene;

sealed class LocalSceneBuilder : ISceneBuilder, ISceneImplProvider, IDisposable {
	const string DefaultSceneName = "Unnamed Scene";

	readonly ArrayPoolBackedVector<SceneHandle> _activeScenes = new();
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly LocalSceneCameraBuilder _cameraBuilder;
	readonly LocalSceneObjectBuilder _objectBuilder;
	bool _isDisposed = false;

	public LocalSceneBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);

		_globals = globals;
		_cameraBuilder = new(globals);
		_objectBuilder = new(globals);
	}

	public Scene CreateScene() {

	}
	public Scene CreateScene(in SceneCreationConfig config) {

	}

	public ISceneCameraBuilder GetCameraBuilder(SceneHandle handle) => this;
	public ISceneObjectBuilder GetObjectBuilder(SceneHandle handle) => this;

	public string GetName(SceneHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceNameAsNewStringObject(handle.Ident, DefaultSceneName);
	}
	public int GetNameUsingSpan(SceneHandle handle, Span<char> dest) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.CopyResourceName(handle.Ident, DefaultSceneName, dest);
	}
	public int GetNameSpanLength(SceneHandle handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceNameLength(handle.Ident, DefaultSceneName);
	}

	#region Camera Delegation
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Camera RegisterCamera(Camera input) {
		AddCameraToScene( input.Handle)
		return input;
	}

	public Camera CreateCamera() {
		_cameraBuilder.CreateCamera();
	}
	public Camera CreateCamera(Location initialPosition, Direction initialViewDirection) { throw new NotImplementedException(); }
	public Camera CreateCamera(in CameraCreationConfig config) { throw new NotImplementedException(); }
	#endregion

	#region Native Methods

	#endregion

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var scene in _activeScenes) Dispose(scene, removeFromVector: false);
		}
		finally {
			_isDisposed = true;
		}
	}

	public bool IsDisposed(SceneHandle handle) => _isDisposed || !_activeScenes.Contains(handle);

	public void Dispose(SceneHandle handle) => Dispose(handle, removeFromVector: true);
	void Dispose(SceneHandle handle, bool removeFromVector) {
		if (IsDisposed(handle)) return;
		DisposeScene(handle).ThrowIfFailure();
		if (removeFromVector) _activeScenes.Remove(handle);
	}

	void ThrowIfThisOrHandleIsDisposed(SceneHandle handle) {
		ThrowIfThisIsDisposed();
		ObjectDisposedException.ThrowIf(!_activeScenes.Contains(handle), typeof(Scene));
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(ISceneCameraBuilder));
	}
	#endregion
}








// TODO I need to find a way to pass the camera builder and object builder per-scene without creating garbage when they're disposed.