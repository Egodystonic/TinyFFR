// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Materials;

public interface IMaterialImplProvider : IDisposableResourceImplProvider<Material> {
	bool GetSupportsPerInstanceEffects(ResourceHandle<Material> handle);
	bool GetSupportsColorKeying(ResourceHandle<Material> handle);
	bool GetIsDefault(ResourceHandle<Material> handle);
	Material Duplicate(ResourceHandle<Material> handle);
	Texture? TryGetAssociatedTexture(ResourceHandle<Material> handle, ReadOnlySpan<char> parameterName);
	void SetEffectTransform(ResourceHandle<Material> handle, Transform2D newTransform);
	void SetEffectBlendTexture(ResourceHandle<Material> handle, MaterialEffectMapType mapType, Texture mapTexture);
	void SetEffectBlendDistance(ResourceHandle<Material> handle, MaterialEffectMapType mapType, float distance);
	void SetEffectOpacity(ResourceHandle<Material> handle, float opacity);
	void SetKeyedColor(ResourceHandle<Material> handle, ColorChannel key, ColorVect color);
}