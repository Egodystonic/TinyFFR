// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// Provides the implementation behind <see cref="Texture"/>.
/// </summary>
public interface ITextureImplProvider : IDisposableResourceImplProvider<Texture> {
	/// <summary>
	/// Invoked via <see cref="Texture.Dimensions"/>.
	/// </summary>
	XYPair<int> GetDimensions(ResourceHandle<Texture> handle);
	/// <summary>
	/// Invoked via <see cref="Texture.TexelType"/>.
	/// </summary>
	TexelType GetTexelType(ResourceHandle<Texture> handle);
	/// <summary>
	/// Invoked via <see cref="Texture.AllowsDynamicWrites"/>.
	/// </summary>
	bool GetAllowsDynamicWrites(ResourceHandle<Texture> handle);
	/// <summary>
	/// Invoked via <see cref="Texture.ContainsMipMaps"/>.
	/// </summary>
	bool GetContainsMipMaps(ResourceHandle<Texture> handle);
	/// <summary>
	/// Invoked via <see cref="Texture.RenderingConfig"/>.
	/// </summary>
	TextureRenderingConfig GetRenderingConfig(ResourceHandle<Texture> handle);
	/// <summary>
	/// Invoked via <see cref="Texture.OverwriteTexels{TTexel}(ReadOnlySpan{TTexel})"/>, <see cref="Texture.OverwriteTexels{TTexel}(ReadOnlySpan{TTexel}, XYPair{int}, XYPair{int})"/>.
	/// </summary>
	void OverwriteTexels<TTexel>(ResourceHandle<Texture> handle, ReadOnlySpan<TTexel> newTexels, XYPair<int> dimensions, XYPair<int> offset) where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, TexelRgb24>, IConversionSupplyingTexel<TTexel, TexelRgba32>;
}