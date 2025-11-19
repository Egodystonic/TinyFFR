// Created on 2025-11-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Xml.Linq;
using static Egodystonic.TinyFFR.Assets.Materials.TexturePatternPrinter;

namespace Egodystonic.TinyFFR.Assets.Materials;

public unsafe interface ITextureBuilder {
	#region Texture Creation And Processing
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

	#region Single Texel Patterns
	Texture CreateTextureWithSingleTexel<TTexel>(TTexel plainFill, bool isLinearColorspace, ReadOnlySpan<char> name = default) where TTexel : unmanaged, ITexel<TTexel> {
		return CreateTexture(
			new ReadOnlySpan<TTexel>(in plainFill),
			XYPair<int>.One,
			isLinearColorspace,
			generateMipMaps: false,
			name
		);
	}
	Texture CreateTextureWithSingleTexel<TTexel>(TTexel plainFill, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> {
		return CreateTexture(
			new ReadOnlySpan<TTexel>(in plainFill),
			new TextureGenerationConfig { Dimensions = XYPair<int>.One },
			in config
		);
	}

	Texture CreateTextureWithSingleTexel(ColorVect plainFill, bool includeAlpha, ReadOnlySpan<char> name = default) {
		return includeAlpha 
			? CreateTextureWithSingleTexel(new TexelRgba32(plainFill), isLinearColorspace: false, name)
			: CreateTextureWithSingleTexel(new TexelRgb24(plainFill), isLinearColorspace: false, name);
	}
	Texture CreateTextureWithSingleTexel(ColorVect plainFill, bool includeAlpha, in TextureCreationConfig config) {
		return includeAlpha
			? CreateTextureWithSingleTexel(new TexelRgba32(plainFill), in config)
			: CreateTextureWithSingleTexel(new TexelRgb24(plainFill), in config);
	}

	Texture CreateTextureWithSingleTexel(UnitSphericalCoordinate plainFill, ReadOnlySpan<char> name = default) {
		return CreateTextureWithSingleTexel(ConvertSphericalCoordToNormalTexel(plainFill), isLinearColorspace: true, name);
	}
	Texture CreateTextureWithSingleTexel(UnitSphericalCoordinate plainFill, in TextureCreationConfig config) {
		return CreateTextureWithSingleTexel(ConvertSphericalCoordToNormalTexel(plainFill), in config);
	}

	Texture CreateTextureWithSingleTexel(UnitSphericalCoordinate xyzPlainFill, Real wPlainFill, ReadOnlySpan<char> name = default) {
		return CreateTextureWithSingleTexel(new TexelRgba32(ConvertSphericalCoordToNormalTexel(xyzPlainFill), ConvertNormalizedRealToTexelByteChannel(wPlainFill)), isLinearColorspace: true, name);
	}
	Texture CreateTextureWithSingleTexel(UnitSphericalCoordinate xyzPlainFill, Real wPlainFill, in TextureCreationConfig config) {
		return CreateTextureWithSingleTexel(new TexelRgba32(ConvertSphericalCoordToNormalTexel(xyzPlainFill), ConvertNormalizedRealToTexelByteChannel(wPlainFill)), in config);
	}

	Texture CreateTextureWithSingleTexel(byte redPlainFill, byte greenPlainFill, ReadOnlySpan<char> name = default) {
		return CreateTextureWithSingleTexel(new TexelRgb24(redPlainFill, greenPlainFill, 0), isLinearColorspace: true, name);
	}
	Texture CreateTextureWithSingleTexel(byte redPlainFill, byte greenPlainFill, in TextureCreationConfig config) {
		return CreateTextureWithSingleTexel(new TexelRgb24(redPlainFill, greenPlainFill, 0), in config);
	}
	Texture CreateTextureWithSingleTexel(byte redPlainFill, byte greenPlainFill, byte bluePlainFill, ReadOnlySpan<char> name = default) {
		return CreateTextureWithSingleTexel(new TexelRgb24(redPlainFill, greenPlainFill, bluePlainFill), isLinearColorspace: true, name);
	}
	Texture CreateTextureWithSingleTexel(byte redPlainFill, byte greenPlainFill, byte bluePlainFill, in TextureCreationConfig config) {
		return CreateTextureWithSingleTexel(new TexelRgb24(redPlainFill, greenPlainFill, bluePlainFill), in config);
	}
	Texture CreateTextureWithSingleTexel(byte redPlainFill, byte greenPlainFill, byte bluePlainFill, byte alphaPlainFill, ReadOnlySpan<char> name = default) {
		return CreateTextureWithSingleTexel(new TexelRgba32(redPlainFill, greenPlainFill, bluePlainFill, alphaPlainFill), isLinearColorspace: true, name);
	}
	Texture CreateTextureWithSingleTexel(byte redPlainFill, byte greenPlainFill, byte bluePlainFill, byte alphaPlainFill, in TextureCreationConfig config) {
		return CreateTextureWithSingleTexel(new TexelRgba32(redPlainFill, greenPlainFill, bluePlainFill, alphaPlainFill), in config);
	}

	Texture CreateTextureWithSingleTexel(Real xPlainFill, Real yPlainFill, ReadOnlySpan<char> name = default) {
		return CreateTextureWithSingleTexel(new TexelRgb24(ConvertNormalizedRealToTexelByteChannel(xPlainFill), ConvertNormalizedRealToTexelByteChannel(yPlainFill), 0), isLinearColorspace: true, name);
	}
	Texture CreateTextureWithSingleTexel(Real xPlainFill, Real yPlainFill, in TextureCreationConfig config) {
		return CreateTextureWithSingleTexel(new TexelRgb24(ConvertNormalizedRealToTexelByteChannel(xPlainFill), ConvertNormalizedRealToTexelByteChannel(yPlainFill), 0), in config);
	}
	Texture CreateTextureWithSingleTexel(Real xPlainFill, Real yPlainFill, Real zPlainFill, ReadOnlySpan<char> name = default) {
		return CreateTextureWithSingleTexel(new TexelRgb24(ConvertNormalizedRealToTexelByteChannel(xPlainFill), ConvertNormalizedRealToTexelByteChannel(yPlainFill), ConvertNormalizedRealToTexelByteChannel(zPlainFill)), isLinearColorspace: true, name);
	}
	Texture CreateTextureWithSingleTexel(Real xPlainFill, Real yPlainFill, Real zPlainFill, in TextureCreationConfig config) {
		return CreateTextureWithSingleTexel(new TexelRgb24(ConvertNormalizedRealToTexelByteChannel(xPlainFill), ConvertNormalizedRealToTexelByteChannel(yPlainFill), ConvertNormalizedRealToTexelByteChannel(zPlainFill)), in config);
	}
	Texture CreateTextureWithSingleTexel(Real xPlainFill, Real yPlainFill, Real zPlainFill, Real wPlainFill, ReadOnlySpan<char> name = default) {
		return CreateTextureWithSingleTexel(new TexelRgba32(ConvertNormalizedRealToTexelByteChannel(xPlainFill), ConvertNormalizedRealToTexelByteChannel(yPlainFill), ConvertNormalizedRealToTexelByteChannel(zPlainFill), ConvertNormalizedRealToTexelByteChannel(wPlainFill)), isLinearColorspace: true, name);
	}
	Texture CreateTextureWithSingleTexel(Real xPlainFill, Real yPlainFill, Real zPlainFill, Real wPlainFill, in TextureCreationConfig config) {
		return CreateTextureWithSingleTexel(new TexelRgba32(ConvertNormalizedRealToTexelByteChannel(xPlainFill), ConvertNormalizedRealToTexelByteChannel(yPlainFill), ConvertNormalizedRealToTexelByteChannel(zPlainFill), ConvertNormalizedRealToTexelByteChannel(wPlainFill)), in config);
	}
	#endregion

	#region Generic Patterns
	Texture CreateTextureFromPattern<TTexel>(in TexturePattern<TTexel> pattern, bool isLinearColorspace, ReadOnlySpan<char> name = default) where TTexel : unmanaged, ITexel<TTexel> {
		return CreateTextureFromPattern(
			pattern,
			new TextureCreationConfig {
				GenerateMipMaps = pattern.Dimensions.Area != 1,
				IsLinearColorspace = isLinearColorspace,
				ProcessingToApply = TextureProcessingConfig.None,
				Name = name
			}
		);
	}
	Texture CreateTextureFromPattern<TTexel>(in TexturePattern<TTexel> pattern, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> {
		var buffer = PreallocateBuffer<TTexel>(pattern.Dimensions.Area);
		_ = PrintPattern(pattern, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = pattern.Dimensions }, in config);
	}
	#endregion

	#region Color Map Patterns
	Texture CreateColorMapFromPattern(in TexturePattern<ColorVect> colorPattern, bool includeAlpha, ReadOnlySpan<char> name = default) { 
		var creationConfig = new TextureCreationConfig {
			GenerateMipMaps = colorPattern.Dimensions.Area != 1,
			IsLinearColorspace = false,
			Name = name,
			ProcessingToApply = TextureProcessingConfig.None
		};
		return CreateColorMapFromPattern(colorPattern, includeAlpha, in creationConfig); 
	}
	Texture CreateColorMapFromPattern(in TexturePattern<ColorVect> colorPattern, bool includeAlpha, in TextureCreationConfig config) {
		if (includeAlpha) {
			var buffer = PreallocateBuffer<TexelRgba32>(colorPattern.Dimensions.Area);
			_ = PrintPattern(colorPattern, buffer.Span);
			return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = colorPattern.Dimensions }, in config);
		}
		else {
			var buffer = PreallocateBuffer<TexelRgb24>(colorPattern.Dimensions.Area);
			_ = PrintPattern(colorPattern, buffer.Span);
			return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = colorPattern.Dimensions }, in config);
		}
	}
	#endregion

	#region Normal Map Patterns
	Texture CreateNormalMapFromPattern(in TexturePattern<UnitSphericalCoordinate> normalPattern, ReadOnlySpan<char> name = default) {
		return CreateNormalMapFromPattern(
			normalPattern,
			new TextureCreationConfig {
				GenerateMipMaps = normalPattern.Dimensions.Area != 1,
				IsLinearColorspace = true,
				Name = name,
				ProcessingToApply = TextureProcessingConfig.None
			}
		);
	}
	Texture CreateNormalMapFromPattern(in TexturePattern<UnitSphericalCoordinate> normalPattern, in TextureCreationConfig config) {
		var buffer = PreallocateBuffer<TexelRgb24>(normalPattern.Dimensions.Area);
		_ = PrintPattern(normalPattern, &ConvertSphericalCoordToNormalTexel, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = normalPattern.Dimensions }, in config);
	}
	#endregion

	#region Orm/Ormr Map Patterns
	Texture CreateOcclusionRoughnessMetallicMapFromPattern(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, ReadOnlySpan<char> name = default) {
		return CreateOcclusionRoughnessMetallicMapFromPattern(
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

	Texture CreateOcclusionRoughnessMetallicMapFromPattern(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(occlusionPattern, roughnessPattern, metallicPattern);
		var buffer = PreallocateBuffer<TexelRgb24>(dimensions.Area);
		_ = PrintPattern(occlusionPattern, roughnessPattern, metallicPattern, &ConvertNormalizedRealToTexelByteChannel, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}

	Texture CreateOcclusionRoughnessMetallicReflectanceMapFromPattern(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, in TexturePattern<Real> reflectancePattern, ReadOnlySpan<char> name = default) {
		return CreateOcclusionRoughnessMetallicReflectanceMapFromPattern(
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

	Texture CreateOcclusionRoughnessMetallicReflectanceMapFromPattern(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, in TexturePattern<Real> reflectancePattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(occlusionPattern, roughnessPattern, metallicPattern, reflectancePattern);
		var buffer = PreallocateBuffer<TexelRgba32>(dimensions.Area);
		_ = PrintPattern(occlusionPattern, roughnessPattern, metallicPattern, reflectancePattern, &ConvertNormalizedRealToTexelByteChannel, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}
	#endregion

	#region Absorption Transmission Map Patterns
	Texture CreateAbsorptionTransmissionMapFromPattern(in TexturePattern<ColorVect> absorptionPattern, in TexturePattern<Real> transmissionPattern, ReadOnlySpan<char> name = default) {
		var creationConfig = new TextureCreationConfig {
			GenerateMipMaps = absorptionPattern.Dimensions.Area != 1 || transmissionPattern.Dimensions.Area != 1,
			IsLinearColorspace = false,
			Name = name,
			ProcessingToApply = TextureProcessingConfig.None
		};
		return CreateAbsorptionTransmissionMapFromPattern(absorptionPattern, transmissionPattern, in creationConfig);
	}
	Texture CreateAbsorptionTransmissionMapFromPattern(in TexturePattern<ColorVect> absorptionPattern, in TexturePattern<Real> transmissionPattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(absorptionPattern, transmissionPattern);
		var buffer = PreallocateBuffer<TexelRgba32>(dimensions.Area);
		_ = PrintPattern(absorptionPattern, transmissionPattern, &TexelRgb24.ConvertFrom, &ConvertNormalizedRealToTexelByteChannel, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}
	#endregion

	#region Emissive Map Patterns
	Texture CreateEmissiveMapFromPattern(in TexturePattern<ColorVect> colorPattern, in TexturePattern<Real> intensityPattern, ReadOnlySpan<char> name = default) {
		var creationConfig = new TextureCreationConfig {
			GenerateMipMaps = colorPattern.Dimensions.Area != 1 || intensityPattern.Dimensions.Area != 1,
			IsLinearColorspace = false,
			Name = name,
			ProcessingToApply = TextureProcessingConfig.None
		};
		return CreateEmissiveMapFromPattern(colorPattern, intensityPattern, in creationConfig);
	}
	Texture CreateEmissiveMapFromPattern(in TexturePattern<ColorVect> colorPattern, in TexturePattern<Real> intensityPattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(colorPattern, intensityPattern);
		var buffer = PreallocateBuffer<TexelRgba32>(dimensions.Area);
		_ = PrintPattern(colorPattern, intensityPattern, &TexelRgb24.ConvertFrom, &ConvertNormalizedRealToTexelByteChannel, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}
	#endregion

	#region Anisotropy Map Patterns
	Texture CreateAnisotropyMapFromPattern(in TexturePattern<Angle> tangentPattern, in TexturePattern<Real> strengthPattern, ReadOnlySpan<char> name = default) {
		var creationConfig = new TextureCreationConfig {
			GenerateMipMaps = anisotropyPattern.Dimensions.Area != 1,
			IsLinearColorspace = true,
			Name = name,
			ProcessingToApply = TextureProcessingConfig.None
		};
		return CreateAnisotropyMapFromPattern(anisotropyPattern, in creationConfig);
	}
	Texture CreateAnisotropyMapFromPattern(in TexturePattern<Angle> tangentPattern, in TexturePattern<Real> strengthPattern, in TextureCreationConfig config) {
		var buffer = PreallocateBuffer<TexelRgb24>(anisotropyPattern.Dimensions.Area);
		_ = PrintPattern(xyzPattern, wPattern, &Convert, &Convert, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}
	#endregion

	#region ClearCoat Map Patterns
	Texture CreateTextureFromPattern(in TexturePattern<Real> xPattern, in TexturePattern<Real> yPattern, ReadOnlySpan<char> name = default) {
		var creationConfig = new TextureCreationConfig {
			GenerateMipMaps = xPattern.Dimensions.Area != 1 || yPattern.Dimensions.Area != 1,
			IsLinearColorspace = true,
			Name = name,
			ProcessingToApply = TextureProcessingConfig.None
		};
		return CreateTextureFromPattern(xPattern, yPattern, in creationConfig);
	}
	Texture CreateTextureFromPattern(in TexturePattern<Real> xPattern, in TexturePattern<Real> yPattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(xPattern, yPattern);
		var buffer = PreallocateBuffer<TexelRgb24>(dimensions.Area);
		_ = PrintPattern(xPattern, yPattern, &Convert, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}
	#endregion
}