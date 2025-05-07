// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public interface ILightImplProvider : IDisposableResourceImplProvider {
	LightType GetType(ResourceHandle handle);

	Location GetPosition(ResourceHandle handle);
	void SetPosition(ResourceHandle handle, Location newPosition);

	ColorVect GetColor(ResourceHandle handle);
	void SetColor(ResourceHandle handle, ColorVect newColor);

	void SetUniversalBrightness(ResourceHandle handle, float newBrightness);
	float GetUniversalBrightness(ResourceHandle handle);

	float GetPointLightMaxIlluminationRadius(ResourceHandle<PointLight> handle);
	void SetPointLightMaxIlluminationRadius(ResourceHandle<PointLight> handle, float newRadius);

	float GetSpotLightMaxIlluminationDistance(ResourceHandle<SpotLight> handle);
	void SetSpotLightMaxIlluminationDistance(ResourceHandle<SpotLight> handle, float newDistance);

	Direction GetSpotLightConeDirection(ResourceHandle<SpotLight> handle);
	void SetSpotLightConeDirection(ResourceHandle<SpotLight> handle, Direction newDirection);

	Angle GetSpotLightConeAngle(ResourceHandle<SpotLight> handle);
	void SetSpotLightConeAngle(ResourceHandle<SpotLight> handle, Angle newAngle);

	Angle GetSpotLightIntenseBeamAngle(ResourceHandle<SpotLight> handle);
	void SetSpotLightIntenseBeamAngle(ResourceHandle<SpotLight> handle, Angle newAngle);

	void TranslateBy(ResourceHandle handle, Vect translation);
	void AdjustBrightnessBy(ResourceHandle handle, float adjustment);
	void ScaleBrightnessBy(ResourceHandle handle, float scalar);
}