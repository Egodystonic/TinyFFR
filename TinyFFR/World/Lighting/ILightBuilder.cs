// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.World;

public interface ILightBuilder {
	PointLight CreatePointLight(Location position, ColorVect? color = null, float? lumens = null, float? falloffRange = null, ReadOnlySpan<char> name = default) {
		return CreatePointLight(new PointLightCreationConfig {
			InitialPosition = position, 
			InitialColor = color ?? LightCreationConfig.DefaultInitialColor,
			InitialLumens = lumens ?? PointLightCreationConfig.DefaultInitialLumens,
			InitialMaxIlluminationRadius = falloffRange ?? PointLightCreationConfig.DefaultInitialMaxIlluminationRadius,
			Name = name
		});
	}
	PointLight CreatePointLight(in PointLightCreationConfig config);
}