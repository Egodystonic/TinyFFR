// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

public enum CameraPlaneConfiguration {
	Standard,
	CloseRange,
	LongRange
}

public interface ICameraBuilder {
	Camera CreateCamera(Location? initialPosition = null, Direction? initialViewDirection = null, CameraPlaneConfiguration cameraRange = CameraPlaneConfiguration.Standard, ReadOnlySpan<char> name = default) {
		return CreateCamera(new CameraCreationConfig {
			Position = initialPosition ?? CameraCreationConfig.DefaultPosition,
			ViewDirection = initialViewDirection ?? CameraCreationConfig.DefaultViewDirection,
			NearPlaneDistance = cameraRange switch {
				CameraPlaneConfiguration.CloseRange => 0.01f,
				CameraPlaneConfiguration.LongRange => 0.15f,
				_ => CameraCreationConfig.DefaultNearPlaneDistance,
			},
			FarPlaneDistance = cameraRange switch {
				CameraPlaneConfiguration.CloseRange => 333f,
				CameraPlaneConfiguration.LongRange => 5_000f,
				_ => CameraCreationConfig.DefaultFarPlaneDistance,
			},
			Name = name
		});
	}
	Camera CreateCamera(in CameraCreationConfig config);
}