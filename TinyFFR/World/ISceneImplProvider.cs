// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using static Egodystonic.TinyFFR.World.Scene;

namespace Egodystonic.TinyFFR.World;

public interface ISceneImplProvider : IDisposableResourceImplProvider<Scene> {
	void Add(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	void Remove(ResourceHandle<Scene> handle, ModelInstance modelInstance);

	void Add<TLight>(ResourceHandle<Scene> handle, TLight light) where TLight : ILight<TLight>;
	void Remove<TLight>(ResourceHandle<Scene> handle, TLight light) where TLight : ILight<TLight>;

	void SetBackdrop(ResourceHandle<Scene> handle, EnvironmentCubemap cubemap, float indirectLightingIntensity, Rotation rotation);
	void SetBackdrop(ResourceHandle<Scene> handle, ColorVect color, float indirectLightingIntensity);
	void SetBackdropWithoutIndirectLighting(ResourceHandle<Scene> handle, EnvironmentCubemap cubemap, float backdropIntensity, Rotation rotation);
	void SetBackdropWithoutIndirectLighting(ResourceHandle<Scene> handle, ColorVect color);
	void RemoveBackdrop(ResourceHandle<Scene> handle);

	internal void SetLightShadowFidelity(ResourceHandle<Scene> handle, Quality qualityPreset, LightShadowFidelityData pointLightFidelity, LightShadowFidelityData spotLightFidelity, LightShadowFidelityData directionalLightFidelity);
}