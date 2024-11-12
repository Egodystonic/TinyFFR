// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Scene;

public interface ICameraBuilder {
	Camera CreateCamera(Location? initialPosition = null, Direction? initialViewDirection = null, ReadOnlySpan<char> name = default);
	Camera CreateCamera(in CameraCreationConfig config);
}