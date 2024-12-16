// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public interface ILightImplProvider : IDisposableResourceImplProvider<LightHandle> {
	LightType GetType(LightHandle handle);

	Location GetPosition(LightHandle handle);
	void SetPosition(LightHandle handle, Location newPosition);

	ColorVect GetColor(LightHandle handle);
	void SetColor(LightHandle handle, ColorVect newColor);

	void TranslateBy(LightHandle handle, Vect translation);
}