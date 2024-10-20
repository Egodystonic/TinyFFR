// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Scene;

sealed unsafe class LocalSceneBuilder : ISceneBuilder, ISceneImplProvider, IDisposable {
	readonly record struct SceneBuilderObjectGroup(LocalCameraBuilder CameraBuilder, LocalObjectBuilder ObjectBuilder);
	const string DefaultSceneName = "Unnamed Scene";

	readonly ArrayPoolBackedMap<SceneHandle, SceneBuilderObjectGroup> _activeSceneMap = new();
	readonly ObjectPool<LocalCameraBuilder, LocalSceneBuilder> _cameraBuilderPool;
	readonly ObjectPool<LocalObjectBuilder, LocalSceneBuilder> _objectBuilderPool;
	readonly LocalFactoryGlobalObjectGroup _globals;
	bool _isDisposed = false;

	public LocalSceneBuilder(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);

		_globals = globals;
		_cameraBuilderPool = new(&CreateCameraBuilder, this);
		_objectBuilderPool = new(&CreateObjectBuilder, this);
	}

	static LocalCameraBuilder CreateCameraBuilder(LocalSceneBuilder owningSceneBuilder) => new(owningSceneBuilder._globals);
	static LocalObjectBuilder CreateObjectBuilder(LocalSceneBuilder owningSceneBuilder) => new(owningSceneBuilder._globals);

	public Scene CreateScene() {

	}
	public Scene CreateScene(in SceneCreationConfig config) {

	}

	public ICameraBuilder GetCameraBuilder(SceneHandle handle) => this;
	public IObjectBuilder GetObjectBuilder(SceneHandle handle) => this;

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
			foreach (var kvp in _activeSceneMap) Dispose(kvp.Key, removeFromMap: false);
		}
		finally {
			_isDisposed = true;
		}
	}

	public bool IsDisposed(SceneHandle handle) => _isDisposed || !_activeSceneMap.ContainsKey(handle);

	public void Dispose(SceneHandle handle) => Dispose(handle, removeFromMap: true);
	void Dispose(SceneHandle handle, bool removeFromMap) {
		if (IsDisposed(handle)) return;
		DisposeScene(handle).ThrowIfFailure();
		if (removeFromMap) _activeScenes.Remove(handle);
	}

	void ThrowIfThisOrHandleIsDisposed(SceneHandle handle) {
		ThrowIfThisIsDisposed();
		ObjectDisposedException.ThrowIf(!_activeScenes.Contains(handle), typeof(Scene));
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(ICameraBuilder));
	}
	#endregion
}








// TODO I need to find a way to pass the camera builder and object builder per-scene without creating garbage when they're disposed.