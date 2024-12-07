// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.World;

public interface ICameraBuilder {
	Camera CreateCamera(Location? initialPosition = null, Direction? initialViewDirection = null, ReadOnlySpan<char> name = default) {
		return CreateCamera(new CameraCreationConfig {
			Position = initialPosition ?? CameraCreationConfig.DefaultPosition,
			ViewDirection = initialViewDirection ?? CameraCreationConfig.DefaultViewDirection,
			Name = name
		});
	}
	Camera CreateCamera(in CameraCreationConfig config);
}