// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Materials;

public interface IMaterialImplProvider : IDisposableResourceImplProvider<Material> {
	bool GetSupportsPerInstanceEffects(ResourceHandle<Material> handle);
	Material Duplicate(ResourceHandle<Material> handle);
	void SetEffectTransform(ResourceHandle<Material> handle, Transform2D newTransform);
	void SetEffectBlendTexture(ResourceHandle<Material> handle, MaterialEffectMapType mapType, Texture mapTexture);
	void SetEffectBlendDistance(ResourceHandle<Material> handle, MaterialEffectMapType mapType, float distance);
}