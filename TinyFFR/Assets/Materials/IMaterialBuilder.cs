﻿// Created on 2024-08-13 by Ben Bowen
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

	static ColorVect DefaultTexelColor { get; } = StandardColor.White;
	static Direction DefaultTexelNormal { get; } = Direction.Forward;
	static float DefaultTexelOcclusion { get; } = 1f;
	static float DefaultTexelRoughness { get; } = 0.4f;
	static float DefaultTexelMetallic { get; } = 0f;
	static ColorVect DefaultTexelOrm { get; } = new(DefaultTexelOcclusion, DefaultTexelRoughness, DefaultTexelMetallic);

	Texture DefaultColorMap { get; }
	Texture DefaultNormalMap { get; }
	Texture DefaultOrmMap { get; }
	Material TestMaterial { get; }

	Texture CreateTexture<TTexel>(Span<TTexel> texels, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> => CreateTexture((ReadOnlySpan<TTexel>) texels, in generationConfig, in config);
	Texture CreateTexture<TTexel>(ReadOnlySpan<TTexel> texels, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel>;

	protected Texture CreateTextureAndDisposePreallocatedBuffer<TTexel>(PreallocatedBuffer<TTexel> preallocatedBuffer, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel>;
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
	Texture CreateColorMap(ColorVect plainFillColor, ReadOnlySpan<char> name = default) {
		return CreateColorMap(TexturePattern.PlainFill(plainFillColor), name);
	}
	Texture CreateColorMap(TexturePattern<ColorVect>? pattern = null, ReadOnlySpan<char> name = default) {
		pattern ??= TexturePattern.PlainFill(DefaultTexelColor);
		var dimensions = pattern.Value.Dimensions;
		TexturePattern.AssertDimensions(dimensions);

		var config = new TextureCreationConfig {
			GenerateMipMaps = dimensions.X > 1 || dimensions.Y > 1,
			Name = name
		};
		var genConfig = new TextureGenerationConfig {
			Height = dimensions.Y,
			Width = dimensions.X,
		};

		return CreateTextureAndDisposePreallocatedBuffer(FillPreallocatedBuffer<ColorVect, TexelRgb24>(pattern.Value), genConfig, config);
	}
	Texture CreateColorMapWithAlpha(ColorVect plainFillColor, ReadOnlySpan<char> name = default) {
		return CreateColorMapWithAlpha(TexturePattern.PlainFill(plainFillColor), name);
	}
	Texture CreateColorMapWithAlpha(TexturePattern<ColorVect>? pattern = null, ReadOnlySpan<char> name = default) {
		pattern ??= TexturePattern.PlainFill(DefaultTexelColor);
		var dimensions = pattern.Value.Dimensions;
		TexturePattern.AssertDimensions(dimensions);

		var config = new TextureCreationConfig {
			GenerateMipMaps = dimensions.X > 1 || dimensions.Y > 1,
			Name = name
		};
		var genConfig = new TextureGenerationConfig {
			Height = dimensions.Y,
			Width = dimensions.X,
		};

		return CreateTextureAndDisposePreallocatedBuffer(FillPreallocatedBuffer<ColorVect, TexelRgba32>(pattern.Value), genConfig, config);
	}
	Texture CreateNormalMap(TexturePattern<Direction>? pattern = null, ReadOnlySpan<char> name = default) {
		pattern ??= TexturePattern.PlainFill(DefaultTexelNormal);
		var dimensions = pattern.Value.Dimensions;
		TexturePattern.AssertDimensions(dimensions);

		var config = new TextureCreationConfig {
			GenerateMipMaps = dimensions.X > 1 || dimensions.Y > 1,
			Name = name
		};
		var genConfig = new TextureGenerationConfig {
			Height = dimensions.Y,
			Width = dimensions.X,
		};

		return CreateTextureAndDisposePreallocatedBuffer(FillPreallocatedBuffer<Direction, TexelRgb24>(pattern.Value), genConfig, config);
	}
	Texture CreateOrmMap(TexturePattern<Real>? occlusionPattern = null, TexturePattern<Real>? roughnessPattern = null, TexturePattern<Real>? metallicPattern = null, ReadOnlySpan<char> name = default) {
		return CreateOrmMap(
			occlusionPattern ?? TexturePattern.PlainFill((Real) DefaultTexelOcclusion),
			roughnessPattern ?? TexturePattern.PlainFill((Real) DefaultTexelRoughness),
			metallicPattern ?? TexturePattern.PlainFill((Real) DefaultTexelMetallic),
			name
		);
	}
	private Texture CreateOrmMap(TexturePattern<Real> occlusionPattern, TexturePattern<Real> roughnessPattern, TexturePattern<Real> metallicPattern, ReadOnlySpan<char> name = default) {
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
			Name = name
		};
		var genConfig = new TextureGenerationConfig {
			Height = dimensions.Y,
			Width = dimensions.X,
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
		
		return CreateTextureAndDisposePreallocatedBuffer(buffer, genConfig, config);
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

	Material CreateAlphaAwareMaterial(Texture? colorMap = null, Texture? normalMap = null, Texture? ormMap = null, AlphaMaterialType? type = null, ReadOnlySpan<char> name = default) {
		return CreateAlphaAwareMaterial(new AlphaAwareMaterialCreationConfig {
			ColorMap = colorMap ?? DefaultColorMap,
			NormalMap = normalMap ?? DefaultNormalMap,
			OrmMap = ormMap ?? DefaultOrmMap,
			Type = type ?? AlphaAwareMaterialCreationConfig.DefaultType,
			Name = name
		});
	}
	Material CreateAlphaAwareMaterial(in AlphaAwareMaterialCreationConfig config);
}