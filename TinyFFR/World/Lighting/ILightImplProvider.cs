// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public interface ILightImplProvider : IDisposableResourceImplProvider<Light> {
	LightType GetType(ResourceHandle<Light> handle);

	Location GetPosition(ResourceHandle<Light> handle);
	void SetPosition(ResourceHandle<Light> handle, Location newPosition);

	ColorVect GetColor(ResourceHandle<Light> handle);
	void SetColor(ResourceHandle<Light> handle, ColorVect newColor);

	float GetPointLightLumens(ResourceHandle<Light> handle);
	void SetPointLightLumens(ResourceHandle<Light> handle, float newLumens);

	float GetPointLightMaxIlluminationRadius(ResourceHandle<Light> handle);
	void SetPointLightMaxIlluminationRadius(ResourceHandle<Light> handle, float newRadius);

	void TranslateBy(ResourceHandle<Light> handle, Vect translation);
}