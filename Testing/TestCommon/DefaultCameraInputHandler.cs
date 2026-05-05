// Created on 2025-06-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Testing;

public static class DefaultCameraInputHandler {
	public static void TickKbm(ILatestKeyboardAndMouseInputRetriever input, ICameraController controller, float deltaTime, Window? cursorLockWindow) {
		if (cursorLockWindow is { } w && input.KeyWasPressedThisIteration(KeyboardOrMouseKey.MouseLeft)) {
			w.SetLockCursor(!w.LockCursor);
		}
		controller.AdjustAllViaDefaultControls(input, deltaTime);
	}

	public static void TickGamepad(ILatestGameControllerInputStateRetriever input, ICameraController controller, float deltaTime) {
		controller.AdjustAllViaDefaultControls(input, deltaTime);
	}
	
	public static void Progress(ICameraController controller, float deltaTime) => controller.Progress(deltaTime);
}