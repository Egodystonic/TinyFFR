// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Scene;

public interface ICameraBuilder {
	Camera CreateCamera();
	Camera CreateCamera(Location initialPosition, Direction initialViewDirection);
	Camera CreateCamera(in CameraCreationConfig config);
}