// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.World;

public interface ILightBuilder {
	PointLight CreatePointLight(Location? position = null, ColorVect? color = null, float? brightness = null, float? falloffRange = null, bool? castsShadows = null, ReadOnlySpan<char> name = default) {
		return CreatePointLight(new PointLightCreationConfig {
			InitialPosition = position ?? PointLightCreationConfig.DefaultInitialPosition, 
			InitialColor = color ?? PointLightCreationConfig.DefaultInitialColor,
			InitialBrightness = brightness ?? PointLightCreationConfig.DefaultInitialBrightness,
			InitialMaxIlluminationRadius = falloffRange ?? PointLightCreationConfig.DefaultInitialMaxIlluminationRadius,
			CastsShadows = castsShadows ?? PointLightCreationConfig.DefaultCastsShadows,
			Name = name
		});
	}
	PointLight CreatePointLight(in PointLightCreationConfig config);

	SpotLight CreateSpotLight(Location? position = null, Direction? coneDirection = null, Angle? coneAngle = null, Angle? intenseBeamAngle = null, ColorVect? color = null, float? brightness = null, float? maxDistance = null, bool? castsShadows = null, bool? highQuality = null, ReadOnlySpan<char> name = default) {
		return CreateSpotLight(new SpotLightCreationConfig {
			InitialPosition = position ?? SpotLightCreationConfig.DefaultInitialPosition,
			InitialConeDirection = coneDirection ?? SpotLightCreationConfig.DefaultInitialConeDirection,
			InitialConeAngle = coneAngle ?? SpotLightCreationConfig.DefaultInitialConeAngle,
			InitialIntenseBeamAngle = intenseBeamAngle ?? SpotLightCreationConfig.DefaultInitialIntenseBeamAngle,
			InitialColor = color ?? SpotLightCreationConfig.DefaultInitialColor,
			InitialBrightness = brightness ?? SpotLightCreationConfig.DefaultInitialBrightness,
			InitialMaxIlluminationDistance = maxDistance ?? SpotLightCreationConfig.DefaultInitialMaxIlluminationDistance,
			IsHighQuality = highQuality ?? SpotLightCreationConfig.DefaultIsHighQuality,
			CastsShadows = castsShadows ?? SpotLightCreationConfig.DefaultCastsShadows,
			Name = name
		});
	}
	SpotLight CreateSpotLight(in SpotLightCreationConfig config);

	DirectionalLight CreateDirectionalLight(Direction? direction = null, ColorVect? color = null, float? brightness = null, bool? castsShadows = null, bool? showSunDisc = null, ReadOnlySpan<char> name = default) {
		return CreateDirectionalLight(new DirectionalLightCreationConfig {
			InitialDirection = direction ?? DirectionalLightCreationConfig.DefaultInitialDirection,
			InitialColor = color ?? DirectionalLightCreationConfig.DefaultInitialColor,
			InitialBrightness = brightness ?? DirectionalLightCreationConfig.DefaultInitialBrightness,
			ShowSunDisc = showSunDisc ?? DirectionalLightCreationConfig.DefaultShowSunDisc,
			CastsShadows = castsShadows ?? DirectionalLightCreationConfig.DefaultCastsShadows,
			Name = name
		});
	}
	DirectionalLight CreateDirectionalLight(in DirectionalLightCreationConfig config);
}