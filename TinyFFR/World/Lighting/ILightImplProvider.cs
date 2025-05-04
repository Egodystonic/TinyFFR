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

	void SetUniversalBrightness(ResourceHandle<Light> handle, float newBrightness);
	float GetUniversalBrightness(ResourceHandle<Light> handle);

	float GetPointLightMaxIlluminationRadius(ResourceHandle<Light> handle);
	void SetPointLightMaxIlluminationRadius(ResourceHandle<Light> handle, float newRadius);

	float GetSpotLightMaxIlluminationDistance(ResourceHandle<Light> handle);
	void SetSpotLightMaxIlluminationDistance(ResourceHandle<Light> handle, float newDistance);

	Direction GetSpotLightConeDirection(ResourceHandle<Light> handle);
	void SetSpotLightConeDirection(ResourceHandle<Light> handle, Direction newDirection);

	Angle GetSpotLightConeAngle(ResourceHandle<Light> handle);
	void SetSpotLightConeAngle(ResourceHandle<Light> handle, Angle newAngle);

	Angle GetSpotLightIntenseBeamAngle(ResourceHandle<Light> handle);
	void SetSpotLightIntenseBeamAngle(ResourceHandle<Light> handle, Angle newAngle);

	void TranslateBy(ResourceHandle<Light> handle, Vect translation);
	void AdjustBrightnessBy(ResourceHandle<Light> handle, float adjustment);
	void ScaleBrightnessBy(ResourceHandle<Light> handle, float scalar);
}