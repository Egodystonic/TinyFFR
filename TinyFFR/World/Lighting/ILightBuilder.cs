// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.World;

public interface ILightBuilder {
	PointLight CreatePointLight(Location? position = null, ColorVect? color = null, float? brightness = null, float? falloffRange = null, ReadOnlySpan<char> name = default) {
		return CreatePointLight(new PointLightCreationConfig {
			InitialPosition = position ?? PointLightCreationConfig.DefaultInitialPosition, 
			InitialColor = color ?? PointLightCreationConfig.DefaultInitialColor,
			InitialBrightness = brightness ?? PointLightCreationConfig.DefaultInitialBrightness,
			InitialMaxIlluminationRadius = falloffRange ?? PointLightCreationConfig.DefaultInitialMaxIlluminationRadius,
			Name = name
		});
	}
	PointLight CreatePointLight(in PointLightCreationConfig config);

	SpotLight CreateSpotLight(Location? position = null, Direction? coneDirection = null, Angle? coneAngle = null, Angle? intenseBeamAngle = null, ColorVect? color = null, float? brightness = null, float? maxDistance = null, ReadOnlySpan<char> name = default) {
		return CreateSpotLight(new SpotLightCreationConfig {
			InitialPosition = position ?? SpotLightCreationConfig.DefaultInitialPosition,
			InitialConeDirection = coneDirection ?? SpotLightCreationConfig.DefaultInitialConeDirection,
			InitialConeAngle = coneAngle ?? SpotLightCreationConfig.DefaultInitialConeAngle,
			InitialIntenseBeamAngle = intenseBeamAngle ?? SpotLightCreationConfig.DefaultInitialIntenseBeamAngle,
			InitialColor = color ?? SpotLightCreationConfig.DefaultInitialColor,
			InitialBrightness = brightness ?? SpotLightCreationConfig.DefaultInitialBrightness,
			InitialMaxIlluminationDistance = maxDistance ?? SpotLightCreationConfig.DefaultInitialMaxIlluminationDistance,
			Name = name
		});
	}
	SpotLight CreateSpotLight(in SpotLightCreationConfig config);
}