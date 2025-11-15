// Created on 2024-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Xml.Linq;

namespace Egodystonic.TinyFFR.Assets.Materials;

public unsafe interface IMaterialBuilder {
	protected readonly ref struct PreallocatedBuffer<TTexel> where TTexel : unmanaged, ITexel<TTexel> {
		public nuint BufferId { get; }
		public Span<TTexel> Buffer { get; }
		public PreallocatedBuffer(UIntPtr bufferId, Span<TTexel> buffer) {
			BufferId = bufferId;
			Buffer = buffer;
		}
	}

	#region Texture Creation
	Texture CreateTexture<TTexel>(Span<TTexel> texels, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> => CreateTexture((ReadOnlySpan<TTexel>) texels, in generationConfig, in config);
	Texture CreateTexture<TTexel>(ReadOnlySpan<TTexel> texels, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel>;
	#endregion

	#region Texture Generation
	protected Texture CreateTextureAndDisposePreallocatedBuffer<TTexel>(PreallocatedBuffer<TTexel> preallocatedBuffer, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel>;
	protected PreallocatedBuffer<TTexel> PreallocateBuffer<TTexel>(int texelCount) where TTexel : unmanaged, ITexel<TTexel>;
	private PreallocatedBuffer<TTexel> FillPreallocatedBuffer<TTexel>(TexturePattern<TTexel> pattern) where TTexel : unmanaged, ITexel<TTexel> {
		var dimensions = pattern.Dimensions;
		var buffer = PreallocateBuffer<TTexel>(dimensions.X * dimensions.Y);
		var texelIndex = 0;
		for (var y = 0; y < dimensions.Y; ++y) {
			for (var x = 0; x < dimensions.X; ++x) {
				buffer.Buffer[texelIndex++] = pattern[x, y];
			}
		}
		return buffer;
	}
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
	private PreallocatedBuffer<TTexel> FillPreallocatedBuffer<T, TTexel>(TexturePattern<T> pattern, delegate* managed<T, TTexel> conversionMapFunc) where T : unmanaged where TTexel : unmanaged, ITexel<TTexel> {
		var dimensions = pattern.Dimensions;
		var buffer = PreallocateBuffer<TTexel>(dimensions.X * dimensions.Y);
		var texelIndex = 0;
		for (var y = 0; y < dimensions.Y; ++y) {
			for (var x = 0; x < dimensions.X; ++x) {
				buffer.Buffer[texelIndex++] = conversionMapFunc(pattern[x, y]);
			}
		}
		return buffer;
	}
	private void GetPatternConfigObjects<T>(TexturePattern<T> pattern, ReadOnlySpan<char> name, out TextureGenerationConfig outGenerationConfig, out TextureCreationConfig outConfig) where T : unmanaged {
		var dimensions = pattern.Dimensions;
		TexturePattern.AssertDimensions(dimensions);

		outConfig = new TextureCreationConfig {
			GenerateMipMaps = dimensions.X > 1 || dimensions.Y > 1,
			Name = name
		};
		outGenerationConfig = new TextureGenerationConfig { Dimensions = dimensions };
	}
	Texture CreateTexture(TexturePattern<ColorVect> pattern, bool includeAlpha, ReadOnlySpan<char> name = default) {
		GetPatternConfigObjects(pattern, name, out var genConfig, out var config);
		return includeAlpha
			? CreateTextureAndDisposePreallocatedBuffer(FillPreallocatedBuffer<ColorVect, TexelRgba32>(pattern), genConfig, config)
			: CreateTextureAndDisposePreallocatedBuffer(FillPreallocatedBuffer<ColorVect, TexelRgb24>(pattern), genConfig, config);
	}

	Texture CreateTexture(TexturePattern<UnitSphericalCoordinate> pattern, ReadOnlySpan<char> name = default) {
		static TexelRgb24 Convert(UnitSphericalCoordinate coord) {
			const float Multiplicand = Byte.MaxValue * 0.5f;

			var v = coord.ToDirection(new Direction(1f, 0f, 0f), new Direction(0f, 0f, 1f))
						.ToVector3()
						+ Vector3.One;
			v *= Multiplicand;
			return new((byte) v.X, (byte) v.Y, (byte) v.Z);
		}

		GetPatternConfigObjects(pattern, name, out var genConfig, out var config);
		return CreateTextureAndDisposePreallocatedBuffer(FillPreallocatedBuffer(pattern, &Convert), genConfig, config);
	}

	Texture CreateTexture(TexturePattern<TexelRgb24> pattern, ReadOnlySpan<char> name = default) {
		GetPatternConfigObjects(pattern, name, out var genConfig, out var config);
		return CreateTextureAndDisposePreallocatedBuffer(FillPreallocatedBuffer(pattern), genConfig, config);
	}
	Texture CreateTexture(TexturePattern<TexelRgba32> pattern, ReadOnlySpan<char> name = default) {
		GetPatternConfigObjects(pattern, name, out var genConfig, out var config);
		return CreateTextureAndDisposePreallocatedBuffer(FillPreallocatedBuffer(pattern), genConfig, config);
	}

	Texture CreateTexture<T>(TexturePattern<T> pattern, ReadOnlySpan<char> name = default) where T : unmanaged, IThreeByteChannelTexel<T> {
		GetPatternConfigObjects(pattern, name, out var genConfig, out var config);
		return CreateTextureAndDisposePreallocatedBuffer(FillPreallocatedBuffer(pattern, &TexelRgb24.ConvertFrom), genConfig, config);
	}

	Texture CreateTexture<T>(TexturePattern<T> pattern, delegate* managed<T, TexelRgb24> conversionMapFunc, ReadOnlySpan<char> name = default) where T : unmanaged {
		GetPatternConfigObjects(pattern, name, out var genConfig, out var config);
		return CreateTextureAndDisposePreallocatedBuffer(FillPreallocatedBuffer(pattern, conversionMapFunc), genConfig, config);
	}
	Texture CreateTexture<T>(TexturePattern<T> pattern, delegate* managed<T, TexelRgba32> conversionMapFunc, ReadOnlySpan<char> name = default) where T : unmanaged {
		GetPatternConfigObjects(pattern, name, out var genConfig, out var config);
		return CreateTextureAndDisposePreallocatedBuffer(FillPreallocatedBuffer(pattern, conversionMapFunc), genConfig, config);
	}

	Texture CreateTexture(TexturePattern<byte> xRedPattern, TexturePattern<byte> yGreenPattern, TexturePattern<byte> zBluePattern, ReadOnlySpan<char> name = default) {
		static byte Convert(byte b) => b;
		return CreateTexture(xRedPattern, yGreenPattern, zBluePattern, &Convert, name);
	}
	Texture CreateTexture(TexturePattern<byte> xRedPattern, TexturePattern<byte> yGreenPattern, TexturePattern<byte> zBluePattern, TexturePattern<byte> wAlphaPattern, ReadOnlySpan<char> name = default) {
		static byte Convert(byte b) => b;
		return CreateTexture(xRedPattern, yGreenPattern, zBluePattern, wAlphaPattern, &Convert, name);
	}
	Texture CreateTexture(TexturePattern<Real> xRedPattern, TexturePattern<Real> yGreenPattern, TexturePattern<Real> zBluePattern, ReadOnlySpan<char> name = default) {
		static byte Convert(Real r) => (byte) (r * Byte.MaxValue);
		return CreateTexture(xRedPattern, yGreenPattern, zBluePattern, &Convert, name);
	}
	Texture CreateTexture(TexturePattern<Real> xRedPattern, TexturePattern<Real> yGreenPattern, TexturePattern<Real> zBluePattern, TexturePattern<Real> wAlphaPattern, ReadOnlySpan<char> name = default) {
		static byte Convert(Real r) => (byte) (r * Byte.MaxValue);
		return CreateTexture(xRedPattern, yGreenPattern, zBluePattern, wAlphaPattern, &Convert, name);
	}
	Texture CreateTexture<T>(TexturePattern<T> xRedPattern, TexturePattern<T> yGreenPattern, TexturePattern<T> zBluePattern, delegate* managed<T, byte> conversionMapFunc, ReadOnlySpan<char> name = default) where T : unmanaged {
		XYPair<int> dimensions;
		var sameDimensions = xRedPattern.Dimensions == yGreenPattern.Dimensions && yGreenPattern.Dimensions == zBluePattern.Dimensions;
		if (sameDimensions) {
			dimensions = xRedPattern.Dimensions;
		}
		else {
			dimensions = new XYPair<int>(
				Math.Max(xRedPattern.Dimensions.X, Math.Max(yGreenPattern.Dimensions.X, zBluePattern.Dimensions.X)),
				Math.Max(xRedPattern.Dimensions.Y, Math.Max(yGreenPattern.Dimensions.Y, zBluePattern.Dimensions.Y))
			);
		}

		TexturePattern.AssertDimensions(dimensions);

		var config = new TextureCreationConfig {
			GenerateMipMaps = dimensions.X > 1 || dimensions.Y > 1,
			Name = name
		};
		var genConfig = new TextureGenerationConfig { Dimensions = dimensions };

		var buffer = PreallocateBuffer<TexelRgb24>(dimensions.X * dimensions.Y);
		if (sameDimensions) {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					buffer.Buffer[texelIndex++] = new(
						conversionMapFunc(xRedPattern[x, y]),
						conversionMapFunc(yGreenPattern[x, y]),
						conversionMapFunc(zBluePattern[x, y])
					);
				}
			}
		}
		else {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					buffer.Buffer[texelIndex++] = new(
						conversionMapFunc(xRedPattern[x % xRedPattern.Dimensions.X, y % xRedPattern.Dimensions.Y]),
						conversionMapFunc(yGreenPattern[x % yGreenPattern.Dimensions.X, y % yGreenPattern.Dimensions.Y]),
						conversionMapFunc(zBluePattern[x % zBluePattern.Dimensions.X, y % zBluePattern.Dimensions.Y])
					);
				}
			}
		}

		return CreateTextureAndDisposePreallocatedBuffer(buffer, genConfig, config);
	}
	Texture CreateTexture<T>(TexturePattern<T> xRedPattern, TexturePattern<T> yGreenPattern, TexturePattern<T> zBluePattern, TexturePattern<T> wAlphaPattern, delegate* managed<T, byte> conversionMapFunc, ReadOnlySpan<char> name = default) where T : unmanaged {
		XYPair<int> dimensions;
		var sameDimensions = xRedPattern.Dimensions == yGreenPattern.Dimensions && yGreenPattern.Dimensions == zBluePattern.Dimensions && zBluePattern.Dimensions == wAlphaPattern.Dimensions;
		if (sameDimensions) {
			dimensions = xRedPattern.Dimensions;
		}
		else {
			dimensions = new XYPair<int>(
				Math.Max(xRedPattern.Dimensions.X, Math.Max(yGreenPattern.Dimensions.X, Math.Max(zBluePattern.Dimensions.X, wAlphaPattern.Dimensions.X))),
				Math.Max(xRedPattern.Dimensions.Y, Math.Max(yGreenPattern.Dimensions.Y, Math.Max(zBluePattern.Dimensions.Y, wAlphaPattern.Dimensions.Y)))
			);
		}

		TexturePattern.AssertDimensions(dimensions);

		var config = new TextureCreationConfig {
			GenerateMipMaps = dimensions.X > 1 || dimensions.Y > 1,
			Name = name
		};
		var genConfig = new TextureGenerationConfig { Dimensions = dimensions };

		var buffer = PreallocateBuffer<TexelRgba32>(dimensions.X * dimensions.Y);
		if (sameDimensions) {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					buffer.Buffer[texelIndex++] = new(
						conversionMapFunc(xRedPattern[x, y]),
						conversionMapFunc(yGreenPattern[x, y]),
						conversionMapFunc(zBluePattern[x, y]),
						conversionMapFunc(wAlphaPattern[x, y])
					);
				}
			}
		}
		else {
			var texelIndex = 0;
			for (var y = 0; y < dimensions.Y; ++y) {
				for (var x = 0; x < dimensions.X; ++x) {
					buffer.Buffer[texelIndex++] = new(
						conversionMapFunc(xRedPattern[x % xRedPattern.Dimensions.X, y % xRedPattern.Dimensions.Y]),
						conversionMapFunc(yGreenPattern[x % yGreenPattern.Dimensions.X, y % yGreenPattern.Dimensions.Y]),
						conversionMapFunc(zBluePattern[x % zBluePattern.Dimensions.X, y % zBluePattern.Dimensions.Y]),
						conversionMapFunc(wAlphaPattern[x % wAlphaPattern.Dimensions.X, y % wAlphaPattern.Dimensions.Y])
					);
				}
			}
		}

		return CreateTextureAndDisposePreallocatedBuffer(buffer, genConfig, config);
	}
	#endregion
	
	#region Material Creation
	Material CreateTestMaterial();

	Material CreateSimpleMaterial(Texture colorMap, Texture? emissiveMap = null, ReadOnlySpan<char> name = default) {
		return CreateSimpleMaterial(new SimpleMaterialCreationConfig {
			ColorMap = colorMap,
			EmissiveMap = emissiveMap,
			Name = name
		});
	}
	Material CreateSimpleMaterial(in SimpleMaterialCreationConfig config);

	Material CreateStandardMaterial(Texture colorMap, Texture? normalMap = null, Texture? ormOrOrmrMap = null, Texture? anisotropyMap = null, Texture? emissiveMap = null, Texture? clearCoatMap = null, StandardMaterialAlphaMode? alphaMode = null, ReadOnlySpan<char> name = default) {
		return CreateStandardMaterial(new StandardMaterialCreationConfig {
			ColorMap = colorMap,
			NormalMap = normalMap,
			OcclusionRoughnessMetallicReflectanceMap = ormOrOrmrMap,
			AnisotropyMap = anisotropyMap,
			EmissiveMap = emissiveMap,
			ClearCoatMap = clearCoatMap,
			AlphaMode = alphaMode ?? StandardMaterialAlphaMode.MaskOnly,
			Name = name
		});
	}
	Material CreateStandardMaterial(in StandardMaterialCreationConfig config);

	Material CreateTransmissiveMaterial(Texture colorMap, Texture absorptionTransmissionMap, TransmissiveMaterialQuality? quality = null, Texture? normalMap = null, Texture? ormrMap = null, Texture? anisotropyMap = null, Texture? emissiveMap = null, TransmissiveMaterialAlphaMode? alphaMode = null, ReadOnlySpan<char> name = default) {
		return CreateTransmissiveMaterial(new TransmissiveMaterialCreationConfig {
			ColorMap = colorMap,
			AbsorptionTransmissionMap = absorptionTransmissionMap,
			NormalMap = normalMap,
			OcclusionRoughnessMetallicReflectanceMap = ormrMap,
			AnisotropyMap = anisotropyMap,
			EmissiveMap = emissiveMap,
			Quality = quality ?? TransmissiveMaterialQuality.SkyboxReflectionsAndRefraction,
			AlphaMode = alphaMode ?? TransmissiveMaterialAlphaMode.MaskOnly,
			Name = name
		});
	}
	Material CreateTransmissiveMaterial(in TransmissiveMaterialCreationConfig config);
	#endregion
}