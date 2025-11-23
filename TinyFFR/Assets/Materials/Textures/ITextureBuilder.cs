// Created on 2025-11-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Xml.Linq;
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
	void ProcessTexture<TTexel>(Span<TTexel> texels, XYPair<int> dimensions, in TextureProcessingConfig config) where TTexel : unmanaged, ITexel<TTexel>;
	#endregion

	#region Generic Patterns
	Texture CreateTexture<TTexel>(in TexturePattern<TTexel> pattern, bool isLinearColorspace, ReadOnlySpan<char> name = default) where TTexel : unmanaged, ITexel<TTexel> {
		return CreateTexture(
			pattern,
			new TextureCreationConfig {
				GenerateMipMaps = pattern.Dimensions.Area != 1,
				IsLinearColorspace = isLinearColorspace,
				ProcessingToApply = TextureProcessingConfig.None,
				Name = name
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
	static readonly ColorVect DefaultColor = ColorVect.White;
	static TexelRgba32 CreateColorTexel(ColorVect color) => new(color);

	Texture CreateColorMap(in TexturePattern<ColorVect> colorPattern, bool includeAlpha, ReadOnlySpan<char> name = default) { 
		var creationConfig = new TextureCreationConfig {
			GenerateMipMaps = colorPattern.Dimensions.Area != 1,
			IsLinearColorspace = false,
			Name = name,
			ProcessingToApply = TextureProcessingConfig.None
		};
		return CreateColorMap(colorPattern, includeAlpha, in creationConfig); 
	}
	Texture CreateColorMap(in TexturePattern<ColorVect> colorPattern, bool includeAlpha, in TextureCreationConfig config) {
		if (includeAlpha) {
			var buffer = PreallocateBuffer<TexelRgba32>(colorPattern.Dimensions.Area);
			_ = PrintPattern(colorPattern, &TexelRgba32.ConvertFrom, buffer.Span);
			return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = colorPattern.Dimensions }, in config);
		}
		else {
			var buffer = PreallocateBuffer<TexelRgb24>(colorPattern.Dimensions.Area);
			_ = PrintPattern(colorPattern, &TexelRgb24.ConvertFrom, buffer.Span);
			return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = colorPattern.Dimensions }, in config);
		}
	}

	Texture CreateColorMap(ReadOnlySpan<char> name = default) => CreateColorMap(DefaultColor, includeAlpha: false, name);
	Texture CreateColorMap(ColorVect color, bool includeAlpha, ReadOnlySpan<char> name = default) {
		return includeAlpha
			? CreateTexture(new TexelRgba32(color), isLinearColorspace: false, name)
			: CreateTexture(new TexelRgb24(color), isLinearColorspace: false, name);
	}
	Texture CreateColorMap(ColorVect color, bool includeAlpha, in TextureCreationConfig config) {
		return includeAlpha
			? CreateTexture(new TexelRgba32(color), in config)
			: CreateTexture(new TexelRgb24(color), in config);
	}
	#endregion

	#region Normal Map Patterns
	static readonly UnitSphericalCoordinate DefaultNormalOffset = UnitSphericalCoordinate.ZeroZero;
	static TexelRgb24 CreateNormalTexel(UnitSphericalCoordinate normalOffset) {
		const float Multiplicand = Byte.MaxValue * 0.5f;

		var v = normalOffset.ToDirection(new Direction(1f, 0f, 0f), new Direction(0f, 0f, 1f))
					.ToVector3()
					+ Vector3.One;
		v *= Multiplicand;
		return new((byte) v.X, (byte) v.Y, (byte) v.Z);
	}

	Texture CreateNormalMap(in TexturePattern<UnitSphericalCoordinate> normalPattern, ReadOnlySpan<char> name = default) {
		return CreateNormalMap(
			normalPattern,
			new TextureCreationConfig {
				GenerateMipMaps = normalPattern.Dimensions.Area != 1,
				IsLinearColorspace = true,
				Name = name,
				ProcessingToApply = TextureProcessingConfig.None
			}
		);
	}
	Texture CreateNormalMap(in TexturePattern<UnitSphericalCoordinate> normalPattern, in TextureCreationConfig config) {
		var buffer = PreallocateBuffer<TexelRgb24>(normalPattern.Dimensions.Area);
		_ = PrintPattern(normalPattern, &CreateNormalTexel, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = normalPattern.Dimensions }, in config);
	}

	Texture CreateNormalMap(UnitSphericalCoordinate? normalOffset = null, ReadOnlySpan<char> name = default) {
		return CreateTexture(CreateNormalTexel(normalOffset ?? DefaultNormalOffset), isLinearColorspace: true, name);
	}
	Texture CreateNormalMap(UnitSphericalCoordinate normalOffset, in TextureCreationConfig config) {
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

	Texture CreateOcclusionRoughnessMetallicMap(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, ReadOnlySpan<char> name = default) {
		return CreateOcclusionRoughnessMetallicMap(
			occlusionPattern,
			roughnessPattern,
			metallicPattern,
			new TextureCreationConfig {
				GenerateMipMaps = occlusionPattern.Dimensions.Area != 1 || roughnessPattern.Dimensions.Area != 1 || metallicPattern.Dimensions.Area != 1,
				IsLinearColorspace = true,
				Name = name,
				ProcessingToApply = TextureProcessingConfig.None
			}
		);
	}

	Texture CreateOcclusionRoughnessMetallicMap(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(occlusionPattern, roughnessPattern, metallicPattern);
		var buffer = PreallocateBuffer<TexelRgb24>(dimensions.Area);
		_ = PrintPattern(occlusionPattern, roughnessPattern, metallicPattern, &TexelRgb24.FromNormalizedFloats, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}

	Texture CreateOcclusionRoughnessMetallicReflectanceMap(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, in TexturePattern<Real> reflectancePattern, ReadOnlySpan<char> name = default) {
		return CreateOcclusionRoughnessMetallicReflectanceMap(
			occlusionPattern,
			roughnessPattern,
			metallicPattern,
			reflectancePattern,
			new TextureCreationConfig {
				GenerateMipMaps = occlusionPattern.Dimensions.Area != 1 || roughnessPattern.Dimensions.Area != 1 || metallicPattern.Dimensions.Area != 1 || reflectancePattern.Dimensions.Area != 1,
				IsLinearColorspace = true,
				Name = name,
				ProcessingToApply = TextureProcessingConfig.None
			}
		);
	}

	Texture CreateOcclusionRoughnessMetallicReflectanceMap(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, in TexturePattern<Real> reflectancePattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(occlusionPattern, roughnessPattern, metallicPattern, reflectancePattern);
		var buffer = PreallocateBuffer<TexelRgba32>(dimensions.Area);
		_ = PrintPattern(occlusionPattern, roughnessPattern, metallicPattern, reflectancePattern, &TexelRgba32.FromNormalizedFloats, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}

	Texture CreateOcclusionRoughnessMetallicMap(Real? occlusion = null, Real? roughness = null, Real? metallic = null, ReadOnlySpan<char> name = default) {
		return CreateTexture(TexelRgb24.FromNormalizedFloats(occlusion ?? DefaultOcclusion, roughness ?? DefaultRoughness, metallic ?? DefaultMetallic), isLinearColorspace: true, name);
	}
	Texture CreateOcclusionRoughnessMetallicMap(Real occlusion, Real roughness, Real metallic, in TextureCreationConfig config) {
		return CreateTexture(TexelRgb24.FromNormalizedFloats(occlusion, roughness, metallic), in config);
	}
	Texture CreateOcclusionRoughnessMetallicReflectanceMap(Real? occlusion = null, Real? roughness = null, Real? metallic = null, Real? reflectance = null, ReadOnlySpan<char> name = default) {
		return CreateTexture(TexelRgba32.FromNormalizedFloats(occlusion ?? DefaultOcclusion, roughness ?? DefaultRoughness, metallic ?? DefaultMetallic, reflectance ?? DefaultReflectance), isLinearColorspace: true, name);
	}
	Texture CreateOcclusionRoughnessMetallicReflectanceMap(Real occlusion, Real roughness, Real metallic, Real reflectance, in TextureCreationConfig config) {
		return CreateTexture(TexelRgba32.FromNormalizedFloats(occlusion, roughness, metallic, reflectance), in config);
	}
	#endregion

	#region Absorption Transmission Map Patterns
	static readonly ColorVect DefaultAbsorption = ColorVect.White;
	static readonly Real DefaultTransmission = 0.5f;
	static TexelRgba32 CreateAbsorptionTransmissionTexel(ColorVect absorption, Real transmission) => new(new TexelRgb24(absorption), (byte) (transmission * Byte.MaxValue));

	Texture CreateAbsorptionTransmissionMap(in TexturePattern<ColorVect> absorptionPattern, in TexturePattern<Real> transmissionPattern, ReadOnlySpan<char> name = default) {
		var creationConfig = new TextureCreationConfig {
			GenerateMipMaps = absorptionPattern.Dimensions.Area != 1 || transmissionPattern.Dimensions.Area != 1,
			IsLinearColorspace = false,
			Name = name,
			ProcessingToApply = TextureProcessingConfig.None
		};
		return CreateAbsorptionTransmissionMap(absorptionPattern, transmissionPattern, in creationConfig);
	}
	Texture CreateAbsorptionTransmissionMap(in TexturePattern<ColorVect> absorptionPattern, in TexturePattern<Real> transmissionPattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(absorptionPattern, transmissionPattern);
		var buffer = PreallocateBuffer<TexelRgba32>(dimensions.Area);
		_ = PrintPattern(absorptionPattern, transmissionPattern, &CreateAbsorptionTransmissionTexel, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}

	Texture CreateAbsorptionTransmissionMap(ColorVect? absorption = null, Real? transmission = null, ReadOnlySpan<char> name = default) {
		return CreateTexture(CreateAbsorptionTransmissionTexel(absorption ?? DefaultAbsorption, transmission ?? DefaultTransmission), isLinearColorspace: true, name);
	}
	Texture CreateAbsorptionTransmissionMap(ColorVect absorption, Real transmission, in TextureCreationConfig config) {
		return CreateTexture(CreateAbsorptionTransmissionTexel(absorption, transmission), in config);
	}
	#endregion

	#region Emissive Map Patterns
	static readonly ColorVect DefaultEmissiveColor = StandardColor.LightingIncandescentBulb;
	static readonly Real DefaultEmissiveIntensity = 0.5f;
	static TexelRgba32 CreateEmissiveTexel(ColorVect color, Real intensity) => new(new TexelRgb24(color), (byte) (intensity * Byte.MaxValue));

	Texture CreateEmissiveMap(in TexturePattern<ColorVect> colorPattern, in TexturePattern<Real> intensityPattern, ReadOnlySpan<char> name = default) {
		var creationConfig = new TextureCreationConfig {
			GenerateMipMaps = colorPattern.Dimensions.Area != 1 || intensityPattern.Dimensions.Area != 1,
			IsLinearColorspace = false,
			Name = name,
			ProcessingToApply = TextureProcessingConfig.None
		};
		return CreateEmissiveMap(colorPattern, intensityPattern, in creationConfig);
	}
	Texture CreateEmissiveMap(in TexturePattern<ColorVect> colorPattern, in TexturePattern<Real> intensityPattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(colorPattern, intensityPattern);
		var buffer = PreallocateBuffer<TexelRgba32>(dimensions.Area);
		_ = PrintPattern(colorPattern, intensityPattern, &CreateEmissiveTexel, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}

	Texture CreateEmissiveMap(ColorVect? color = null, Real? intensity = null, ReadOnlySpan<char> name = default) {
		return CreateTexture(CreateEmissiveTexel(color ?? DefaultEmissiveColor, intensity ?? DefaultEmissiveIntensity), isLinearColorspace: true, name);
	}
	Texture CreateEmissiveMap(ColorVect color, Real intensity, in TextureCreationConfig config) {
		return CreateTexture(CreateEmissiveTexel(color, intensity), in config);
	}
	#endregion

	#region Anisotropy Map Patterns
	static readonly Angle DefaultAnisotropyTangent = 0f;
	static readonly Real DefaultAnisotropyStrength = 1f;
	static TexelRgb24 CreateAnisotropyTexel(Angle tangent, Real strength) {
		var asTangentSpaceVect2 = ((XYPair<float>.FromPolarAngle(tangent)
			+ XYPair<float>.One)
			* (Byte.MaxValue * 0.5f))
			.CastWithRoundingIfNecessary<float, byte>(MidpointRounding.AwayFromZero);

		return new TexelRgb24(asTangentSpaceVect2.X, asTangentSpaceVect2.Y, (byte) (strength * Byte.MaxValue));
	}

	Texture CreateAnisotropyMap(in TexturePattern<Angle> tangentPattern, in TexturePattern<Real> strengthPattern, ReadOnlySpan<char> name = default) {
		var creationConfig = new TextureCreationConfig {
			GenerateMipMaps = tangentPattern.Dimensions.Area != 1 || strengthPattern.Dimensions.Area != 1,
			IsLinearColorspace = true,
			Name = name,
			ProcessingToApply = TextureProcessingConfig.None
		};
		return CreateAnisotropyMap(tangentPattern, strengthPattern, in creationConfig);
	}
	Texture CreateAnisotropyMap(in TexturePattern<Angle> tangentPattern, in TexturePattern<Real> strengthPattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(tangentPattern, strengthPattern);
		var buffer = PreallocateBuffer<TexelRgb24>(dimensions.Area);
		_ = PrintPattern(tangentPattern, strengthPattern, &CreateAnisotropyTexel, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}

	Texture CreateAnisotropyMap(Angle? tangent = null, Real? strength = null, ReadOnlySpan<char> name = default) {
		return CreateTexture(CreateAnisotropyTexel(tangent ?? DefaultAnisotropyTangent, strength ?? DefaultAnisotropyStrength), isLinearColorspace: true, name);
	}
	Texture CreateAnisotropyMap(Angle tangent, Real strength, in TextureCreationConfig config) {
		return CreateTexture(CreateAnisotropyTexel(tangent, strength), in config);
	}
	#endregion

	#region ClearCoat Map Patterns
	static readonly Real DefaultClearCoatThickness = 1f;
	static readonly Real DefaultClearCoatRoughness = 0f;
	static TexelRgb24 CreateClearCoatTexel(Real thickness, Real roughness) => TexelRgb24.FromNormalizedFloats(thickness, roughness, Real.Zero);

	Texture CreateClearCoatMap(in TexturePattern<Real> thicknessPattern, in TexturePattern<Real> roughnessPattern, ReadOnlySpan<char> name = default) {
		var creationConfig = new TextureCreationConfig {
			GenerateMipMaps = thicknessPattern.Dimensions.Area != 1 || roughnessPattern.Dimensions.Area != 1,
			IsLinearColorspace = true,
			Name = name,
			ProcessingToApply = TextureProcessingConfig.None
		};
		return CreateClearCoatMap(thicknessPattern, roughnessPattern, in creationConfig);
	}
	Texture CreateClearCoatMap(in TexturePattern<Real> thicknessPattern, in TexturePattern<Real> roughnessPattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(thicknessPattern, roughnessPattern);
		var buffer = PreallocateBuffer<TexelRgb24>(dimensions.Area);
		_ = PrintPattern(thicknessPattern, roughnessPattern, &CreateClearCoatTexel, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}

	Texture CreateClearCoatMap(Real? thickness = null, Real? roughness = null, ReadOnlySpan<char> name = default) {
		return CreateTexture(CreateClearCoatTexel(thickness ?? DefaultClearCoatThickness, roughness ?? DefaultClearCoatRoughness), isLinearColorspace: true, name);
	}
	Texture CreateClearCoatMap(Real thickness, Real roughness, in TextureCreationConfig config) {
		return CreateTexture(CreateClearCoatTexel(thickness, roughness), in config);
	}
	#endregion
}