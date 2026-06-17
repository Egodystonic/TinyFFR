// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Materials;

public interface ITextureImplProvider : IDisposableResourceImplProvider<Texture> {
	XYPair<int> GetDimensions(ResourceHandle<Texture> handle);
	TexelType GetTexelType(ResourceHandle<Texture> handle);
	bool GetAllowsDynamicWrites(ResourceHandle<Texture> handle);
	bool GetContainsMipMaps(ResourceHandle<Texture> handle);
	TextureRenderingConfig GetRenderingConfig(ResourceHandle<Texture> handle);
	void OverwriteTexels<TTexel>(ResourceHandle<Texture> handle, ReadOnlySpan<TTexel> newTexels, XYPair<int> dimensions, XYPair<int> offset) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgb24>, IConversionSupplyingTexel<TTexel, TexelRgba32>;
}