// Created on 2025-11-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Xml.Linq;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using static Egodystonic.TinyFFR.Assets.Materials.TexturePatternPrinter;

namespace Egodystonic.TinyFFR.Assets.Materials;

public unsafe interface ITextureBuilder {
	#region Fundamental Methods
	protected readonly ref struct PreallocatedBuffer<TTexel> where TTexel : unmanaged, ITexel<TTexel> {
		public nuint BufferId { get; }
		public Span<TTexel> Span { get; }
		public PreallocatedBuffer(UIntPtr bufferId, Span<TTexel> span) {
			BufferId = bufferId;
			Span = span;
		}
	}

	protected Texture CreateTextureAndDisposePreallocatedBuffer<TTexel>(PreallocatedBuffer<TTexel> preallocatedBuffer, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel>;
	protected PreallocatedBuffer<TTexel> PreallocateBuffer<TTexel>(int texelCount) where TTexel : unmanaged, ITexel<TTexel>;

	Texture CreateTexture<TTexel>(ReadOnlySpan<TTexel> texels, XYPair<int> dimensions, bool isLinearColorspace, bool? generateMipMaps = null, ReadOnlySpan<char> name = default) where TTexel : unmanaged, ITexel<TTexel> {
		return CreateTexture(
			texels,
			new TextureGenerationConfig {Dimensions = dimensions},
			new TextureCreationConfig {
				IsLinearColorspace = isLinearColorspace,
				GenerateMipMaps = generateMipMaps ?? dimensions.Area > 1,
				Name = name,
				ProcessingToApply = TextureProcessingConfig.None
			}
		);
	}
	Texture CreateTexture<TTexel>(ReadOnlySpan<TTexel> texels, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel>;
	#endregion

	#region Generic Patterns
	Texture CreateTexture<TTexel>(in TexturePattern<TTexel> pattern, bool isLinearColorspace, ReadOnlySpan<char> name = default) where TTexel : unmanaged, ITexel<TTexel> {
		return CreateTexture(
			pattern,
			new TextureCreationConfig {
				IsLinearColorspace = isLinearColorspace,
				GenerateMipMaps = pattern.Dimensions.Area != 1,
				Name = name,
				ProcessingToApply = TextureProcessingConfig.None
			}
		);
	}
	Texture CreateTexture<TTexel>(in TexturePattern<TTexel> pattern, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> {
		var buffer = PreallocateBuffer<TTexel>(pattern.Dimensions.Area);
		_ = PrintPattern(pattern, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = pattern.Dimensions }, in config);
	}

	Texture CreateTexture<TTexel>(TTexel plainFill, bool isLinearColorspace, ReadOnlySpan<char> name = default) where TTexel : unmanaged, ITexel<TTexel> {
		return CreateTexture(
			new ReadOnlySpan<TTexel>(in plainFill),
			XYPair<int>.One,
			isLinearColorspace,
			generateMipMaps: false,
			name
		);
	}
	Texture CreateTexture<TTexel>(TTexel plainFill, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> {
		return CreateTexture(
			new ReadOnlySpan<TTexel>(in plainFill),
			new TextureGenerationConfig { Dimensions = XYPair<int>.One },
			in config
		);
	}
	#endregion

	#region Color Map Patterns
	static readonly ColorVect DefaultColor = ColorVect.WhiteOpaque;
	static TexelRgba32 CreateColorTexel(ColorVect color) => new(color);

	static TextureCreationConfig GetColorMapCreationConfig(XYPair<int> dimensions, bool includeAlpha, ReadOnlySpan<char> name = default) {
		return TextureCreationConfig.ForColorTexture(name) with {
			GenerateMipMaps = dimensions.Area != 1,
			ProcessingToApply = includeAlpha ? TextureProcessingConfig.PremultiplyAlpha() : TextureProcessingConfig.None
		};
	}
	static void PrintColorMap(in TexturePattern<ColorVect> colorPattern, Span<TexelRgba32> destinationBuffer) => _ = PrintPattern(colorPattern, &TexelRgba32.ConvertFrom, destinationBuffer);
	static void PrintColorMap(in TexturePattern<ColorVect> colorPattern, Span<TexelRgb24> destinationBuffer) => _ = PrintPattern(colorPattern, &TexelRgb24.ConvertFrom, destinationBuffer);

	Texture CreateColorMap(in TexturePattern<ColorVect> colorPattern, bool includeAlpha, ReadOnlySpan<char> name = default) {
		return CreateColorMap(colorPattern, includeAlpha, GetColorMapCreationConfig(colorPattern.Dimensions, includeAlpha, name));
	}
	Texture CreateColorMap(in TexturePattern<ColorVect> colorPattern, bool includeAlpha, in TextureCreationConfig config) {
		if (includeAlpha) {
			var buffer = PreallocateBuffer<TexelRgba32>(colorPattern.Dimensions.Area);
			PrintColorMap(colorPattern, buffer.Span);
			return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = colorPattern.Dimensions }, in config);
		}
		else {
			var buffer = PreallocateBuffer<TexelRgb24>(colorPattern.Dimensions.Area);
			PrintColorMap(colorPattern, buffer.Span);
			return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = colorPattern.Dimensions }, in config);
		}
	}

	Texture CreateColorMap(ReadOnlySpan<char> name = default) => CreateColorMap(DefaultColor, includeAlpha: false, name);
	Texture CreateColorMap(ColorVect color, bool includeAlpha, ReadOnlySpan<char> name = default) {
		return CreateColorMap(color, includeAlpha, GetColorMapCreationConfig(XYPair<int>.One, includeAlpha, name));
	}
	Texture CreateColorMap(ColorVect color, bool includeAlpha, in TextureCreationConfig config) {
		return includeAlpha
			? CreateTexture(new TexelRgba32(color), in config)
			: CreateTexture(new TexelRgb24(color), in config);
	}

	static TextureCreationConfig GetCanvasTextureCreationConfig(XYPair<int> dimensions, bool includeAlpha, ReadOnlySpan<char> name = default) {
		return TextureCreationConfig.ForCanvasTexture(name) with {
			GenerateMipMaps = dimensions.Area != 1,
			ProcessingToApply = includeAlpha ? TextureProcessingConfig.PremultiplyAlpha() : TextureProcessingConfig.None
		};
	}
	static void PrintCanvasTexture(in TexturePattern<ColorVect> colorPattern, Span<TexelRgba32> destinationBuffer) => _ = PrintPattern(colorPattern, &TexelRgba32.ConvertFrom, destinationBuffer);
	static void PrintCanvasTexture(in TexturePattern<ColorVect> colorPattern, Span<TexelRgb24> destinationBuffer) => _ = PrintPattern(colorPattern, &TexelRgb24.ConvertFrom, destinationBuffer);

	Texture CreateCanvasTexture(in TexturePattern<ColorVect> colorPattern, bool includeAlpha, ReadOnlySpan<char> name = default) {
		return CreateCanvasTexture(colorPattern, includeAlpha, GetCanvasTextureCreationConfig(colorPattern.Dimensions, includeAlpha, name));
	}
	Texture CreateCanvasTexture(in TexturePattern<ColorVect> colorPattern, bool includeAlpha, in TextureCreationConfig config) {
		if (includeAlpha) {
			var buffer = PreallocateBuffer<TexelRgba32>(colorPattern.Dimensions.Area);
			PrintCanvasTexture(colorPattern, buffer.Span);
			return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = colorPattern.Dimensions }, in config);
		}
		else {
			var buffer = PreallocateBuffer<TexelRgb24>(colorPattern.Dimensions.Area);
			PrintCanvasTexture(colorPattern, buffer.Span);
			return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = colorPattern.Dimensions }, in config);
		}
	}

	Texture CreateCanvasTexture(ReadOnlySpan<char> name = default) => CreateCanvasTexture(DefaultColor, includeAlpha: false, name);
	Texture CreateCanvasTexture(ColorVect color, bool includeAlpha, ReadOnlySpan<char> name = default) {
		return CreateCanvasTexture(color, includeAlpha, GetCanvasTextureCreationConfig(XYPair<int>.One, includeAlpha, name));
	}
	Texture CreateCanvasTexture(ColorVect color, bool includeAlpha, in TextureCreationConfig config) {
		return includeAlpha
			? CreateTexture(new TexelRgba32(color), in config)
			: CreateTexture(new TexelRgb24(color), in config);
	}
	#endregion

	#region Normal Map Patterns
	static readonly SphericalTranslation DefaultNormalOffset = SphericalTranslation.ZeroZero;
	static TexelRgb24 CreateNormalTexel(SphericalTranslation normalOffset) {
		const float Multiplicand = Byte.MaxValue * 0.5f;

		var v = normalOffset.Translate(new Direction(1f, 0f, 0f), new Direction(0f, 0f, 1f))
					.ToVector3()
					+ Vector3.One;
		v *= Multiplicand;
		return new((byte) v.X, (byte) v.Y, (byte) v.Z);
	}

	static TextureCreationConfig GetNormalMapCreationConfig(XYPair<int> dimensions, ReadOnlySpan<char> name = default) {
		return TextureCreationConfig.ForDataTexture(TextureDataType.LinearUnitVector, name) with {
			GenerateMipMaps = dimensions.Area != 1
		};
	}
	static void PrintNormalMap(in TexturePattern<SphericalTranslation> normalPattern, Span<TexelRgb24> destinationBuffer) => _ = PrintPattern(normalPattern, &CreateNormalTexel, destinationBuffer);

	Texture CreateNormalMap(in TexturePattern<SphericalTranslation> normalPattern, ReadOnlySpan<char> name = default) {
		return CreateNormalMap(normalPattern, GetNormalMapCreationConfig(normalPattern.Dimensions, name));
	}
	Texture CreateNormalMap(in TexturePattern<SphericalTranslation> normalPattern, in TextureCreationConfig config) {
		var buffer = PreallocateBuffer<TexelRgb24>(normalPattern.Dimensions.Area);
		PrintNormalMap(normalPattern, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = normalPattern.Dimensions }, in config);
	}

	Texture CreateNormalMap(SphericalTranslation? normalOffset = null, ReadOnlySpan<char> name = default) {
		return CreateNormalMap(normalOffset ?? DefaultNormalOffset, GetNormalMapCreationConfig(XYPair<int>.One, name));
	}
	Texture CreateNormalMap(SphericalTranslation normalOffset, in TextureCreationConfig config) {
		return CreateTexture(CreateNormalTexel(normalOffset), in config);
	}
	#endregion

	#region Orm/Ormr Map Patterns
	static readonly Real DefaultOcclusion = 1f;
	static readonly Real DefaultRoughness = 0.4f;
	static readonly Real DefaultMetallic = 0f;
	static readonly Real DefaultReflectance = 0.5f;
	static TexelRgb24 CreateOcclusionRoughnessMetallicTexel(Real occlusion, Real roughness, Real metallic) => TexelRgb24.FromNormalizedFloats(occlusion, roughness, metallic);
	static TexelRgba32 CreateOcclusionRoughnessMetallicReflectanceTexel(Real occlusion, Real roughness, Real metallic, Real reflectance) => TexelRgba32.FromNormalizedFloats(occlusion, roughness, metallic, reflectance);

	static TextureCreationConfig GetOcclusionRoughnessMetallicMapCreationConfig(XYPair<int> dimensions, ReadOnlySpan<char> name = default) {
		return TextureCreationConfig.ForDataTexture(name) with {
			GenerateMipMaps = dimensions.Area != 1
		};
	}
	static void PrintOcclusionRoughnessMetallicMap(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, Span<TexelRgb24> destinationBuffer) {
		_ = PrintPattern(occlusionPattern, roughnessPattern, metallicPattern, &TexelRgb24.FromNormalizedFloats, destinationBuffer);
	}

	static TextureCreationConfig GetOcclusionRoughnessMetallicReflectanceMapCreationConfig(XYPair<int> dimensions, ReadOnlySpan<char> name = default) {
		return TextureCreationConfig.ForDataTexture(name) with {
			GenerateMipMaps = dimensions.Area != 1
		};
	}
	static void PrintOcclusionRoughnessMetallicReflectanceMap(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, in TexturePattern<Real> reflectancePattern, Span<TexelRgba32> destinationBuffer) {
		_ = PrintPattern(occlusionPattern, roughnessPattern, metallicPattern, reflectancePattern, &TexelRgba32.FromNormalizedFloats, destinationBuffer);
	}

	Texture CreateOcclusionRoughnessMetallicMap(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, ReadOnlySpan<char> name = default) {
		return CreateOcclusionRoughnessMetallicMap(
			occlusionPattern,
			roughnessPattern,
			metallicPattern,
			GetOcclusionRoughnessMetallicMapCreationConfig(GetCompositePatternDimensions(occlusionPattern, roughnessPattern, metallicPattern), name)
		);
	}

	Texture CreateOcclusionRoughnessMetallicMap(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(occlusionPattern, roughnessPattern, metallicPattern);
		var buffer = PreallocateBuffer<TexelRgb24>(dimensions.Area);
		PrintOcclusionRoughnessMetallicMap(occlusionPattern, roughnessPattern, metallicPattern, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}

	Texture CreateOcclusionRoughnessMetallicReflectanceMap(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, in TexturePattern<Real> reflectancePattern, ReadOnlySpan<char> name = default) {
		return CreateOcclusionRoughnessMetallicReflectanceMap(
			occlusionPattern,
			roughnessPattern,
			metallicPattern,
			reflectancePattern,
			GetOcclusionRoughnessMetallicReflectanceMapCreationConfig(GetCompositePatternDimensions(occlusionPattern, roughnessPattern, metallicPattern, reflectancePattern), name)
		);
	}

	Texture CreateOcclusionRoughnessMetallicReflectanceMap(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, in TexturePattern<Real> reflectancePattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(occlusionPattern, roughnessPattern, metallicPattern, reflectancePattern);
		var buffer = PreallocateBuffer<TexelRgba32>(dimensions.Area);
		PrintOcclusionRoughnessMetallicReflectanceMap(occlusionPattern, roughnessPattern, metallicPattern, reflectancePattern, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}

	Texture CreateOcclusionRoughnessMetallicMap(Real? occlusion = null, Real? roughness = null, Real? metallic = null, ReadOnlySpan<char> name = default) {
		return CreateOcclusionRoughnessMetallicMap(
			occlusion ?? DefaultOcclusion,
			roughness ?? DefaultRoughness,
			metallic ?? DefaultMetallic,
			GetOcclusionRoughnessMetallicMapCreationConfig(XYPair<int>.One, name)
		);
	}
	Texture CreateOcclusionRoughnessMetallicMap(Real occlusion, Real roughness, Real metallic, in TextureCreationConfig config) {
		return CreateTexture(TexelRgb24.FromNormalizedFloats(occlusion, roughness, metallic), in config);
	}
	Texture CreateOcclusionRoughnessMetallicReflectanceMap(Real? occlusion = null, Real? roughness = null, Real? metallic = null, Real? reflectance = null, ReadOnlySpan<char> name = default) {
		return CreateOcclusionRoughnessMetallicReflectanceMap(
			occlusion ?? DefaultOcclusion,
			roughness ?? DefaultRoughness,
			metallic ?? DefaultMetallic,
			reflectance ?? DefaultReflectance,
			GetOcclusionRoughnessMetallicReflectanceMapCreationConfig(XYPair<int>.One, name)
		);
	}
	Texture CreateOcclusionRoughnessMetallicReflectanceMap(Real occlusion, Real roughness, Real metallic, Real reflectance, in TextureCreationConfig config) {
		return CreateTexture(TexelRgba32.FromNormalizedFloats(occlusion, roughness, metallic, reflectance), in config);
	}
	#endregion

	#region Absorption Transmission Map Patterns
	static readonly ColorVect DefaultAbsorption = ColorVect.BlackOpaque;
	static readonly Real DefaultTransmission = 0.5f;
	static TexelRgba32 CreateAbsorptionTransmissionTexel(ColorVect absorption, Real transmission) => new(new TexelRgb24(absorption), (byte) (transmission * Byte.MaxValue));

	static TextureCreationConfig GetAbsorptionTransmissionMapCreationConfig(XYPair<int> dimensions, ReadOnlySpan<char> name = default) {
		return TextureCreationConfig.ForColorTexture(name) with {
			GenerateMipMaps = dimensions.Area != 1
		};
	}
	static void PrintAbsorptionTransmissionMap(in TexturePattern<ColorVect> absorptionPattern, in TexturePattern<Real> transmissionPattern, Span<TexelRgba32> destinationBuffer) {
		_ = PrintPattern(absorptionPattern, transmissionPattern, &CreateAbsorptionTransmissionTexel, destinationBuffer);
	}

	Texture CreateAbsorptionTransmissionMap(in TexturePattern<ColorVect> absorptionPattern, in TexturePattern<Real> transmissionPattern, ReadOnlySpan<char> name = default) {
		return CreateAbsorptionTransmissionMap(
			absorptionPattern,
			transmissionPattern,
			GetAbsorptionTransmissionMapCreationConfig(GetCompositePatternDimensions(absorptionPattern, transmissionPattern), name)
		);
	}
	Texture CreateAbsorptionTransmissionMap(in TexturePattern<ColorVect> absorptionPattern, in TexturePattern<Real> transmissionPattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(absorptionPattern, transmissionPattern);
		var buffer = PreallocateBuffer<TexelRgba32>(dimensions.Area);
		PrintAbsorptionTransmissionMap(absorptionPattern, transmissionPattern, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}

	Texture CreateAbsorptionTransmissionMap(ColorVect? absorption = null, Real? transmission = null, ReadOnlySpan<char> name = default) {
		return CreateAbsorptionTransmissionMap(
			absorption ?? DefaultAbsorption,
			transmission ?? DefaultTransmission,
			GetAbsorptionTransmissionMapCreationConfig(XYPair<int>.One, name)
		);
	}
	Texture CreateAbsorptionTransmissionMap(ColorVect absorption, Real transmission, in TextureCreationConfig config) {
		return CreateTexture(CreateAbsorptionTransmissionTexel(absorption, transmission), in config);
	}
	#endregion

	#region Emissive Map Patterns
	static readonly ColorVect DefaultEmissiveColor = StandardColor.LightingIncandescentBulb;
	static readonly Real DefaultEmissiveIntensity = 1f;
	static TexelRgba32 CreateEmissiveTexel(ColorVect color, Real intensity) => new(new TexelRgb24(color), (byte) (intensity * Byte.MaxValue));

	static TextureCreationConfig GetEmissiveMapCreationConfig(XYPair<int> dimensions, ReadOnlySpan<char> name = default) {
		return TextureCreationConfig.ForColorTexture(name) with {
			GenerateMipMaps = dimensions.Area != 1
		};
	}
	static void PrintEmissiveMap(in TexturePattern<ColorVect> colorPattern, in TexturePattern<Real> intensityPattern, Span<TexelRgba32> destinationBuffer) {
		_ = PrintPattern(colorPattern, intensityPattern, &CreateEmissiveTexel, destinationBuffer);
	}

	Texture CreateEmissiveMap(in TexturePattern<ColorVect> colorPattern, in TexturePattern<Real> intensityPattern, ReadOnlySpan<char> name = default) {
		return CreateEmissiveMap(
			colorPattern,
			intensityPattern,
			GetEmissiveMapCreationConfig(GetCompositePatternDimensions(colorPattern, intensityPattern), name)
		);
	}
	Texture CreateEmissiveMap(in TexturePattern<ColorVect> colorPattern, in TexturePattern<Real> intensityPattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(colorPattern, intensityPattern);
		var buffer = PreallocateBuffer<TexelRgba32>(dimensions.Area);
		PrintEmissiveMap(colorPattern, intensityPattern, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}

	Texture CreateEmissiveMap(ColorVect? color = null, Real? intensity = null, ReadOnlySpan<char> name = default) {
		return CreateEmissiveMap(
			color ?? DefaultEmissiveColor,
			intensity ?? DefaultEmissiveIntensity,
			GetEmissiveMapCreationConfig(XYPair<int>.One, name)
		);
	}
	Texture CreateEmissiveMap(ColorVect color, Real intensity, in TextureCreationConfig config) {
		return CreateTexture(CreateEmissiveTexel(color, intensity), in config);
	}
	#endregion

	#region Anisotropy Map Patterns
	static readonly Angle DefaultAnisotropyRadialAngle = 0f;
	static readonly Real DefaultAnisotropyStrength = 1f;
	static TexelRgb24 CreateAnisotropyTexel(Angle radialAngle, Real strength) {
		var asTangentSpaceVect2 = ((XYPair<float>.FromPolarAngle(radialAngle)
			+ XYPair<float>.One)
			* (Byte.MaxValue * 0.5f))
			.CastWithRoundingIfNecessary<float, byte>(MidpointRounding.AwayFromZero);

		return new TexelRgb24(asTangentSpaceVect2.X, asTangentSpaceVect2.Y, (byte) (strength * Byte.MaxValue));
	}

	static TextureCreationConfig GetAnisotropyMapCreationConfig(XYPair<int> dimensions, ReadOnlySpan<char> name = default) {
		return TextureCreationConfig.ForDataTexture(name) with {
			GenerateMipMaps = dimensions.Area != 1
		};
	}
	static void PrintAnisotropyMap(in TexturePattern<Angle> radialAnglePattern, in TexturePattern<Real> strengthPattern, Span<TexelRgb24> destinationBuffer) {
		_ = PrintPattern(radialAnglePattern, strengthPattern, &CreateAnisotropyTexel, destinationBuffer);
	}

	Texture CreateAnisotropyMap(in TexturePattern<Angle> radialAnglePattern, in TexturePattern<Real> strengthPattern, ReadOnlySpan<char> name = default) {
		return CreateAnisotropyMap(
			radialAnglePattern,
			strengthPattern,
			GetAnisotropyMapCreationConfig(GetCompositePatternDimensions(radialAnglePattern, strengthPattern), name)
		);
	}
	Texture CreateAnisotropyMap(in TexturePattern<Angle> radialAnglePattern, in TexturePattern<Real> strengthPattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(radialAnglePattern, strengthPattern);
		var buffer = PreallocateBuffer<TexelRgb24>(dimensions.Area);
		PrintAnisotropyMap(radialAnglePattern, strengthPattern, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}

	Texture CreateAnisotropyMap(Angle? radialAngle = null, Real? strength = null, ReadOnlySpan<char> name = default) {
		return CreateAnisotropyMap(
			radialAngle ?? DefaultAnisotropyRadialAngle,
			strength ?? DefaultAnisotropyStrength,
			GetAnisotropyMapCreationConfig(XYPair<int>.One, name)
		);
	}
	Texture CreateAnisotropyMap(Angle radialAngle, Real strength, in TextureCreationConfig config) {
		return CreateTexture(CreateAnisotropyTexel(radialAngle, strength), in config);
	}
	#endregion

	#region ClearCoat Map Patterns
	static readonly Real DefaultClearCoatThickness = 1f;
	static readonly Real DefaultClearCoatRoughness = 0f;
	static TexelRgb24 CreateClearCoatTexel(Real thickness, Real roughness) => TexelRgb24.FromNormalizedFloats(thickness, roughness, Real.Zero);

	static TextureCreationConfig GetClearCoatMapCreationConfig(XYPair<int> dimensions, ReadOnlySpan<char> name = default) {
		return TextureCreationConfig.ForDataTexture(TextureDataType.LinearUnitVector, name) with {
			GenerateMipMaps = dimensions.Area != 1
		};
	}
	static void PrintClearCoatMap(in TexturePattern<Real> thicknessPattern, in TexturePattern<Real> roughnessPattern, Span<TexelRgb24> destinationBuffer) {
		_ = PrintPattern(thicknessPattern, roughnessPattern, &CreateClearCoatTexel, destinationBuffer);
	}

	Texture CreateClearCoatMap(in TexturePattern<Real> thicknessPattern, in TexturePattern<Real> roughnessPattern, ReadOnlySpan<char> name = default) {
		return CreateClearCoatMap(
			thicknessPattern,
			roughnessPattern,
			GetClearCoatMapCreationConfig(GetCompositePatternDimensions(thicknessPattern, roughnessPattern), name)
		);
	}
	Texture CreateClearCoatMap(in TexturePattern<Real> thicknessPattern, in TexturePattern<Real> roughnessPattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(thicknessPattern, roughnessPattern);
		var buffer = PreallocateBuffer<TexelRgb24>(dimensions.Area);
		PrintClearCoatMap(thicknessPattern, roughnessPattern, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}

	Texture CreateClearCoatMap(Real? thickness = null, Real? roughness = null, ReadOnlySpan<char> name = default) {
		return CreateClearCoatMap(
			thickness ?? DefaultClearCoatThickness,
			roughness ?? DefaultClearCoatRoughness,
			GetClearCoatMapCreationConfig(XYPair<int>.One, name)
		);
	}
	Texture CreateClearCoatMap(Real thickness, Real roughness, in TextureCreationConfig config) {
		return CreateTexture(CreateClearCoatTexel(thickness, roughness), in config);
	}
	#endregion
}
