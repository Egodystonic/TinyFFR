// Created on 2026-04-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;

namespace Egodystonic.TinyFFR.World;

public interface ICameraController : IDisposable {
	Camera Camera { get; }
	
	void ResetParametersToDefault();
	void Progress(float deltaTime); 
	void SetGlobalSmoothing(Strength newSmoothingStrength);
	void AdjustAllViaDefaultControls(ILatestKeyboardAndMouseInputRetriever input, float deltaTime);
	void AdjustAllViaDefaultControls(ILatestGameControllerInputStateRetriever input, float deltaTime);
}
public interface ICameraController<out TSelf> : ICameraController where TSelf : ICameraController<TSelf> {
	internal static abstract TSelf RentAndTetherToCamera(Camera camera);
}