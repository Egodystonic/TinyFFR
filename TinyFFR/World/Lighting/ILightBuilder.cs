// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.World;

public interface ILightBuilder {
	PointLight CreatePointLight(Location position, ColorVect color, ReadOnlySpan<char> name = default) {
		return CreatePointLight(new LightCreationConfig { InitialColor = color, InitialPosition = position, Name = name });
	}
	PointLight CreatePointLight(in LightCreationConfig config);
}