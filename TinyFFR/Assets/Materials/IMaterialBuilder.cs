// Created on 2024-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Materials;

public interface IMaterialBuilder {
	Texture CreateTexture<TTexel>(Span<TTexel> texels, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> => CreateTexture((ReadOnlySpan<TTexel>) texels, config);
	Texture CreateTexture<TTexel>(ReadOnlySpan<TTexel> texels, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel>;

	protected Texture CreateTextureUsingPreallocatedBuffer<TTexel>(Span<TTexel> preallocatedBuffer, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> => CreateTexture((ReadOnlySpan<TTexel>) texels, config);
	protected Span<TTexel> PreallocateBuffer<TTexel>(int size) where TTexel : unmanaged, ITexel<TTexel>;
	private unsafe Span<TTexel> FillPreallocatedBuffer<T, TTexel>(TexturePattern<T> pattern, delegate* managed<T, TTexel> conversionFunc) where T : unmanaged where TTexel : unmanaged, ITexel<TTexel> {
		var dimensions = pattern.Dimensions;
		var buffer = PreallocateBuffer<TTexel>(dimensions.X * dimensions.Y);
		var texelIndex = 0;
		for (var y = 0; y < dimensions.Y; ++y) {
			for (var x = 0; x < dimensions.X; ++x) {
				buffer[texelIndex++] = conversionFunc(pattern[x, y]);
			}
		}
		return buffer;
	}
	unsafe Texture CreateColorMap(TexturePattern<ColorVect> pattern, bool includeAlphaChannel = false, ReadOnlySpan<char> name = default) {
		static TexelRgba32 Convert32(ColorVect v) => new(v);
		static TexelRgb24 Convert24(ColorVect v) => new(v);

		var dimensions = pattern.Dimensions;
		TexturePattern.AssertDimensions(dimensions);

		var config = new TextureCreationConfig {
			GenerateMipMaps = dimensions.X > 1 || dimensions.Y > 1,
			Height = dimensions.Y,
			Width = dimensions.X,
			Name = name
		};

		if (includeAlphaChannel) {
			return CreateTextureUsingPreallocatedBuffer(FillPreallocatedBuffer(pattern, &Convert32), config);
		}
		return CreateTextureUsingPreallocatedBuffer(FillPreallocatedBuffer(pattern, &Convert24), config);
	}

	Material CreateOpaqueMaterial(Texture colorMap, Texture normalMap, Texture ormMap, ReadOnlySpan<char> name = default) {
		return CreateOpaqueMaterial(new StandardMaterialCreationConfig {
			Albedo = albedo,
			Name = name
		});
	}
	Material CreateOpaqueMaterial(in StandardMaterialCreationConfig config);
}