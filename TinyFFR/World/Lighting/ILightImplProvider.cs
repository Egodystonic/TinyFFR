// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public interface ILightImplProvider : IDisposableResourceImplProvider {
	LightType GetType(ResourceHandle handle);

	ColorVect GetColor(ResourceHandle handle);
	void SetColor(ResourceHandle handle, ColorVect newColor);

	void SetUniversalBrightness(ResourceHandle handle, float newBrightness);
	float GetUniversalBrightness(ResourceHandle handle);
	void AdjustBrightnessBy(ResourceHandle handle, float adjustment);
	void ScaleBrightnessBy(ResourceHandle handle, float scalar);

	bool GetIsShadowCaster(ResourceHandle handle);
	void SetIsShadowCaster(ResourceHandle handle, bool isShadowCaster);
	internal void SetShadowFidelity(ResourceHandle handle, LightShadowFidelityData fidelityArgs);

	Location GetPointLightPosition(ResourceHandle<PointLight> handle);
	void SetPointLightPosition(ResourceHandle<PointLight> handle, Location newPosition);

	float GetPointLightMaxIlluminationRadius(ResourceHandle<PointLight> handle);
	void SetPointLightMaxIlluminationRadius(ResourceHandle<PointLight> handle, float newRadius);

	Location GetSpotLightPosition(ResourceHandle<SpotLight> handle);
	void SetSpotLightPosition(ResourceHandle<SpotLight> handle, Location newPosition);

	float GetSpotLightMaxIlluminationDistance(ResourceHandle<SpotLight> handle);
	void SetSpotLightMaxIlluminationDistance(ResourceHandle<SpotLight> handle, float newDistance);

	Direction GetSpotLightConeDirection(ResourceHandle<SpotLight> handle);
	void SetSpotLightConeDirection(ResourceHandle<SpotLight> handle, Direction newDirection);

	Angle GetSpotLightConeAngle(ResourceHandle<SpotLight> handle);
	void SetSpotLightConeAngle(ResourceHandle<SpotLight> handle, Angle newAngle);

	Angle GetSpotLightIntenseBeamAngle(ResourceHandle<SpotLight> handle);
	void SetSpotLightIntenseBeamAngle(ResourceHandle<SpotLight> handle, Angle newAngle);

	Direction GetDirectionalLightDirection(ResourceHandle<DirectionalLight> handle);
	void SetDirectionalLightDirection(ResourceHandle<DirectionalLight> handle, Direction newDirection);

	void SetDirectionalLightSunDiscParameters(ResourceHandle<DirectionalLight> handle, SunDiscConfig config);
}