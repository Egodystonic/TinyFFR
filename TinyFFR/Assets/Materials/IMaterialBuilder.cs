// Created on 2024-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Materials;

public interface IMaterialBuilder {
	protected readonly ref struct PreallocatedBuffer<TTexel> where TTexel : unmanaged, ITexel<TTexel> {
		public nuint BufferId { get; }
		public Span<TTexel> Buffer { get; }
		public PreallocatedBuffer(UIntPtr bufferId, Span<TTexel> buffer) {
			BufferId = bufferId;
			Buffer = buffer;
		}
	}

	Texture DefaultColorMap { get; }
	Texture DefaultNormalMap { get; }
	Texture DefaultOrmMap { get; }

	Texture CreateTexture<TTexel>(Span<TTexel> texels, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> => CreateTexture((ReadOnlySpan<TTexel>) texels, config);
	Texture CreateTexture<TTexel>(ReadOnlySpan<TTexel> texels, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel>;

	protected Texture CreateTextureUsingPreallocatedBuffer<TTexel>(PreallocatedBuffer<TTexel> preallocatedBuffer, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel>;
	protected PreallocatedBuffer<TTexel> PreallocateBuffer<TTexel>(int texelCount) where TTexel : unmanaged, ITexel<TTexel>;
	private PreallocatedBuffer<TTexel> FillPreallocatedBuffer<T, TTexel>(TexturePattern<T> pattern) where T : unmanaged where TTexel : unmanaged, IConversionSupplyingTexel<TTexel, T> {
		var dimensions = pattern.Dimensions;
		var buffer = PreallocateBuffer<TTexel>(dimensions.X * dimensions.Y);
		var texelIndex = 0;
		for (var y = 0; y < dimensions.Y; ++y) {
			for (var x = 0; x < dimensions.X; ++x) {
				buffer.Buffer[texelIndex++] = TTexel.ConvertFrom(pattern[x, y]);
			}
		}
		return buffer;
	}
	Texture CreateColorMap(TexturePattern<ColorVect> pattern, bool includeAlphaChannel = false, ReadOnlySpan<char> name = default) {
		var dimensions = pattern.Dimensions;
		TexturePattern.AssertDimensions(dimensions);

		var config = new TextureCreationConfig {
			GenerateMipMaps = dimensions.X > 1 || dimensions.Y > 1,
			Height = dimensions.Y,
			Width = dimensions.X,
			Name = name
		};

		if (includeAlphaChannel) {
			return CreateTextureUsingPreallocatedBuffer(FillPreallocatedBuffer<ColorVect, TexelRgba32>(pattern), config);
		}
		return CreateTextureUsingPreallocatedBuffer(FillPreallocatedBuffer<ColorVect, TexelRgb24>(pattern), config);
	}
	Texture CreateNormalMap(TexturePattern<Direction> pattern, ReadOnlySpan<char> name = default) {
		var dimensions = pattern.Dimensions;
		TexturePattern.AssertDimensions(dimensions);

		var config = new TextureCreationConfig {
			GenerateMipMaps = dimensions.X > 1 || dimensions.Y > 1,
			Height = dimensions.Y,
			Width = dimensions.X,
			Name = name
		};

		return CreateTextureUsingPreallocatedBuffer(FillPreallocatedBuffer<Direction, TexelRgb24>(pattern), config);
	}
	Texture CreateOrmMap(TexturePattern<float> occlusionPattern, TexturePattern<float> roughnessPattern, TexturePattern<float> metallicPattern, ReadOnlySpan<char> name = default) {
		static byte FloatToByte(float f) => (byte) (f * Byte.MaxValue);
		
		XYPair<int> dimensions;
		var sameDimensions = occlusionPattern.Dimensions == roughnessPattern.Dimensions && roughnessPattern.Dimensions == metallicPattern.Dimensions;
		if (sameDimensions) {
			dimensions = occlusionPattern.Dimensions;
		}
		else {
			dimensions = new XYPair<int>(
				Math.Max(occlusionPattern.Dimensions.X, Math.Max(roughnessPattern.Dimensions.X, metallicPattern.Dimensions.X)),
				Math.Max(occlusionPattern.Dimensions.Y, Math.Max(roughnessPattern.Dimensions.Y, metallicPattern.Dimensions.Y))
			);
		}
		
		TexturePattern.AssertDimensions(dimensions);

		var config = new TextureCreationConfig {
			GenerateMipMaps = dimensions.X > 1 || dimensions.Y > 1,
			Height = dimensions.Y,
			Width = dimensions.X,
			Name = name
		};

		var buffer = PreallocateBuffer<TexelRgb24>(dimensions.X * dimensions.Y);
		if (sameDimensions) {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					buffer.Buffer[texelIndex++] = new(
						FloatToByte(occlusionPattern[x, y]),
						FloatToByte(roughnessPattern[x, y]),
						FloatToByte(metallicPattern[x, y])
					);
				}
			}
		}
		else {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					buffer.Buffer[texelIndex++] = new(
						FloatToByte(occlusionPattern[x % occlusionPattern.Dimensions.X, y % occlusionPattern.Dimensions.Y]),
						FloatToByte(roughnessPattern[x % roughnessPattern.Dimensions.X, y % roughnessPattern.Dimensions.Y]),
						FloatToByte(metallicPattern[x % metallicPattern.Dimensions.X, y % metallicPattern.Dimensions.Y])
					);
				}
			}
		}
		
		return CreateTextureUsingPreallocatedBuffer(buffer, config);
	}

	Material CreateOpaqueMaterial(Texture? colorMap = null, Texture? normalMap = null, Texture? ormMap = null, ReadOnlySpan<char> name = default) {
		return CreateOpaqueMaterial(new OpaqueMaterialCreationConfig {
			ColorMap = colorMap ?? DefaultColorMap,
			NormalMap = normalMap ?? DefaultNormalMap,
			OrmMap = ormMap ?? DefaultOrmMap,
			Name = name
		});
	}
	Material CreateOpaqueMaterial(in OpaqueMaterialCreationConfig config);
}