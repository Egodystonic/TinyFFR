// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// An <see cref="IResourceImplProvider"/> for <see cref="Light"/> resources of every kind.
/// </summary>
/// <remarks>
/// Several members here back the equivalent member on <see cref="Light"/> and on each of <see cref="PointLight"/>, <see cref="SpotLight"/> and
/// <see cref="DirectionalLight"/>, because those types all expose the same common light properties.
/// </remarks>
public interface ILightImplProvider : IDisposableResourceImplProvider {
	/// <summary>
	/// Invoked via <see cref="ILight.Type"/>.
	/// </summary>
	LightType GetType(ResourceHandle handle);

	/// <summary>
	/// Invoked via <see cref="ILight.Color"/>.
	/// </summary>
	ColorVect GetColor(ResourceHandle handle);
	/// <summary>
	/// Invoked via <see cref="ILight.Color"/>.
	/// </summary>
	void SetColor(ResourceHandle handle, ColorVect newColor);

	/// <summary>
	/// Invoked via <see cref="ILight.Brightness"/>.
	/// </summary>
	void SetUniversalBrightness(ResourceHandle handle, float newBrightness);
	/// <summary>
	/// Invoked via <see cref="ILight.Brightness"/>.
	/// </summary>
	float GetUniversalBrightness(ResourceHandle handle);
	/// <summary>
	/// Invoked via <see cref="ILight.AdjustBrightnessBy"/>.
	/// </summary>
	void AdjustBrightnessBy(ResourceHandle handle, float adjustment);
	/// <summary>
	/// Invoked via <see cref="ILight.ScaleBrightnessBy"/>.
	/// </summary>
	void ScaleBrightnessBy(ResourceHandle handle, float scalar);

	/// <summary>
	/// Invoked via <see cref="ILight.CastsShadows"/>.
	/// </summary>
	bool GetIsShadowCaster(ResourceHandle handle);
	/// <summary>
	/// Invoked via <see cref="ILight.CastsShadows"/>.
	/// </summary>
	void SetIsShadowCaster(ResourceHandle handle, bool isShadowCaster);
	internal void SetShadowFidelity(ResourceHandle handle, LightShadowFidelityData fidelityArgs);

	/// <summary>
	/// Invoked via <see cref="PointLight.Position"/>.
	/// </summary>
	Location GetPointLightPosition(ResourceHandle<PointLight> handle);
	/// <summary>
	/// Invoked via <see cref="PointLight.Position"/>.
	/// </summary>
	void SetPointLightPosition(ResourceHandle<PointLight> handle, Location newPosition);

	/// <summary>
	/// Invoked via <see cref="PointLight.MaxIlluminationRadius"/>.
	/// </summary>
	float GetPointLightMaxIlluminationRadius(ResourceHandle<PointLight> handle);
	/// <summary>
	/// Invoked via <see cref="PointLight.MaxIlluminationRadius"/>.
	/// </summary>
	void SetPointLightMaxIlluminationRadius(ResourceHandle<PointLight> handle, float newRadius);

	/// <summary>
	/// Invoked via <see cref="SpotLight.Position"/>.
	/// </summary>
	Location GetSpotLightPosition(ResourceHandle<SpotLight> handle);
	/// <summary>
	/// Invoked via <see cref="SpotLight.Position"/>.
	/// </summary>
	void SetSpotLightPosition(ResourceHandle<SpotLight> handle, Location newPosition);

	/// <summary>
	/// Invoked via <see cref="SpotLight.MaxIlluminationDistance"/>.
	/// </summary>
	float GetSpotLightMaxIlluminationDistance(ResourceHandle<SpotLight> handle);
	/// <summary>
	/// Invoked via <see cref="SpotLight.MaxIlluminationDistance"/>.
	/// </summary>
	void SetSpotLightMaxIlluminationDistance(ResourceHandle<SpotLight> handle, float newDistance);

	/// <summary>
	/// Invoked via <see cref="SpotLight.ConeDirection"/>.
	/// </summary>
	Direction GetSpotLightConeDirection(ResourceHandle<SpotLight> handle);
	/// <summary>
	/// Invoked via <see cref="SpotLight.ConeDirection"/>.
	/// </summary>
	void SetSpotLightConeDirection(ResourceHandle<SpotLight> handle, Direction newDirection);

	/// <summary>
	/// Invoked via <see cref="SpotLight.ConeAngle"/>.
	/// </summary>
	Angle GetSpotLightConeAngle(ResourceHandle<SpotLight> handle);
	/// <summary>
	/// Invoked via <see cref="SpotLight.ConeAngle"/>.
	/// </summary>
	void SetSpotLightConeAngle(ResourceHandle<SpotLight> handle, Angle newAngle);

	/// <summary>
	/// Invoked via <see cref="SpotLight.IntenseBeamAngle"/>.
	/// </summary>
	Angle GetSpotLightIntenseBeamAngle(ResourceHandle<SpotLight> handle);
	/// <summary>
	/// Invoked via <see cref="SpotLight.IntenseBeamAngle"/>.
	/// </summary>
	void SetSpotLightIntenseBeamAngle(ResourceHandle<SpotLight> handle, Angle newAngle);

	/// <summary>
	/// Invoked via <see cref="DirectionalLight.Direction"/>.
	/// </summary>
	Direction GetDirectionalLightDirection(ResourceHandle<DirectionalLight> handle);
	/// <summary>
	/// Invoked via <see cref="DirectionalLight.Direction"/>.
	/// </summary>
	void SetDirectionalLightDirection(ResourceHandle<DirectionalLight> handle, Direction newDirection);

	/// <summary>
	/// Invoked via <see cref="DirectionalLight.SetSunDiscParameters"/>.
	/// </summary>
	void SetDirectionalLightSunDiscParameters(ResourceHandle<DirectionalLight> handle, SunDiscConfig config);
}