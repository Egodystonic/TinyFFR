// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.World;

public interface ILightBuilder {
	Light CreatePointLight(Location position, ColorVect color, ReadOnlySpan<char> name = default) {
		return CreatePointLight(new LightCreationConfig { InitialColor = color, InitialPosition = position, Name = name });
	}
	Light CreatePointLight(in LightCreationConfig config);
}