// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.World;

public interface ICameraController : IDisposable {
	Camera Camera { get; }
	
	void ResetParametersToDefault();
	void Progress(TimeSpan deltaTime) => Progress(deltaTime.AsDeltaTime());
	void Progress(float deltaTime); 
}
public interface ICameraController<TSelf> : ICameraController where TSelf : ICameraController<TSelf> {
	internal static abstract TSelf RentAndTetherToCamera(Camera camera);
}