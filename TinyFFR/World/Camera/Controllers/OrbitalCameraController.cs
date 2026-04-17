// // Created on 2026-04-16 by Ben Bowen
// // (c) Egodystonic / TinyFFR 2026
//
// using Egodystonic.TinyFFR.Resources.Memory;
//
// namespace Egodystonic.TinyFFR.World;
//
// public sealed class OrbitalCameraController : ICameraController<OrbitalCameraController> {
// 	#region Creation / Pooling
// 	static readonly unsafe ObjectPool<OrbitalCameraController> _controllerPool = new(&New);
// 	public static OrbitalCameraController RentAndTetherToCamera(Camera camera) {
// 		var result = _controllerPool.Rent();
// 		result._camera = camera;
// 		result.ResetParametersToDefault();
// 		return result;
// 	}
// 	static OrbitalCameraController New() => new();
// 	OrbitalCameraController() { }
// 	Camera? _camera;
// 	public Camera Camera => _camera ?? throw new ObjectDisposedException(nameof(OrbitalCameraController));
// 	public void Dispose() {
// 		if (_camera == null) return;
// 		_camera = null;
// 		_controllerPool.Return(this);
// 	}
// 	#endregion
//
// 	
// 	
// 	public void ResetParametersToDefault() {
// 		
// 	}
// }