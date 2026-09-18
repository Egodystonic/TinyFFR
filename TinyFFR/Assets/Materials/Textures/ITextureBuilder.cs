// Created on 2025-11-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Xml.Linq;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using static Egodystonic.TinyFFR.Assets.Materials.TexturePatternPrinter;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// Creates textures from patterns, from texel data held in memory, or as single-texel constants.
/// </summary>
/// <remarks>
/// <para>
/// This is the counterpart to loading textures from files: where the asset loader reads images from disc, this generates them.
/// That is useful for programmatic or data-driven applications, for the small constant maps a material needs when you have no file for one,
/// and for placeholder content.
/// </para>
/// <para>
/// Each map type has three related members: <c>CreateXyzMap</c> makes the texture, while the static <c>PrintXyzMap</c> and
/// <c>GetXyzMapCreationConfig</c> expose the two halves of what it does (the texel generation and the configuration) separately.
/// </para>
/// </remarks>
public unsafe interface ITextureBuilder {
	#region Fundamental Methods
	/// <summary>
	/// A block of texel memory obtained from the builder, to be filled and then handed back to create a texture from.
	/// </summary>
	/// <remarks>
	/// This exists so that texels can be written straight in to the memory the texture will be created from, rather than in to
	/// a buffer that must then be copied. A buffer must be passed to
	/// <see cref="CreateTextureAndDisposePreallocatedBuffer"/> exactly once, which is what releases it.
	/// </remarks>
	/// <typeparam name="TTexel">The texel type the buffer holds.</typeparam>
	protected readonly ref struct PreallocatedBuffer<TTexel> where TTexel : unmanaged, ITexel<TTexel> {
		/// <summary>
		/// An identifier the builder uses to recognise this buffer when it is handed back.
		/// </summary>
		public nuint BufferId { get; }
		/// <summary>
		/// The texel memory itself, to be written in to.
		/// </summary>
		public Span<TTexel> Span { get; }
		/// <summary>
		/// Constructs a new <see cref="PreallocatedBuffer{TTexel}"/> from the given identifier and memory.
		/// </summary>
		/// <param name="bufferId">The value for <see cref="BufferId"/>.</param>
		/// <param name="span">The value for <see cref="Span"/>.</param>
		public PreallocatedBuffer(UIntPtr bufferId, Span<TTexel> span) {
			BufferId = bufferId;
			Span = span;
		}
	}

	/// <summary>
	/// Creates a texture from a previously preallocated buffer, and releases that buffer.
	/// </summary>
	/// <remarks>
	/// The buffer must not be used after this returns.
	/// </remarks>
	/// <typeparam name="TTexel">The texel type the buffer holds.</typeparam>
	/// <param name="preallocatedBuffer">The filled buffer to create the texture from.</param>
	/// <param name="generationConfig">The dimensions of the texture being created.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	protected Texture CreateTextureAndDisposePreallocatedBuffer<TTexel>(PreallocatedBuffer<TTexel> preallocatedBuffer, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel>;
	/// <summary>
	/// Obtains a block of texel memory to write texture data in to.
	/// </summary>
	/// <remarks>
	/// Every buffer obtained here must be passed to <see cref="CreateTextureAndDisposePreallocatedBuffer"/> exactly once.
	/// </remarks>
	/// <typeparam name="TTexel">The texel type the buffer should hold.</typeparam>
	/// <param name="texelCount">How many texels the buffer must hold. Must be positive.</param>
	protected PreallocatedBuffer<TTexel> PreallocateBuffer<TTexel>(int texelCount) where TTexel : unmanaged, ITexel<TTexel>;

	/// <summary>
	/// Creates a texture from the given texel data.
	/// </summary>
	/// <typeparam name="TTexel">The type of the texels being supplied.</typeparam>
	/// <param name="texels">The texel data, laid out row by row (bottom to top). Must hold at least <c>dimensions.X * dimensions.Y</c> entries.</param>
	/// <param name="dimensions">The width and height of the texture, in texels. Both must be positive.</param>
	/// <param name="dataType">What the texels represent, which determines how they are interpreted and compressed.</param>
	/// <param name="generateMipMaps">Whether to also generate smaller copies of the texture for use at a distance, or <see langword="null"/> to let TinyFFR decide according to the texture size + data type.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateTexture<TTexel>(ReadOnlySpan<TTexel> texels, XYPair<int> dimensions, TextureDataType dataType, bool? generateMipMaps = null, ReadOnlySpan<char> name = default) where TTexel : unmanaged, ITexel<TTexel> {
		return CreateTexture(
			texels,
			new TextureGenerationConfig {Dimensions = dimensions},
			new TextureCreationConfig {
				DataType = dataType,
				GenerateMipMaps = generateMipMaps ?? dimensions.Area > 1,
				Name = name,
				ProcessingToApply = TextureProcessingConfig.None
			}
		);
	}
	/// <summary>
	/// Creates a texture from the given texel data, using the given configs.
	/// </summary>
	/// <typeparam name="TTexel">The type of the texels being supplied.</typeparam>
	/// <param name="texels">The texel data, laid out row by row. Must hold at least as many entries as the configured dimensions require.</param>
	/// <param name="generationConfig">The dimensions of the texture being created.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture CreateTexture<TTexel>(ReadOnlySpan<TTexel> texels, in TextureGenerationConfig generationConfig, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel>;
	#endregion

	#region Generic Patterns
	/// <summary>
	/// Creates a texture by evaluating the given texel pattern.
	/// </summary>
	/// <remarks>
	/// The texture takes its dimensions from the pattern.
	/// </remarks>
	/// <typeparam name="TTexel">The texel type the pattern produces.</typeparam>
	/// <param name="pattern">The pattern to evaluate.</param>
	/// <param name="dataType">What the texels represent, which determines how they are interpreted and compressed.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateTexture<TTexel>(in TexturePattern<TTexel> pattern, TextureDataType dataType, ReadOnlySpan<char> name = default) where TTexel : unmanaged, ITexel<TTexel> {
		return CreateTexture(
			pattern,
			new TextureCreationConfig {
				DataType = dataType,
				GenerateMipMaps = pattern.Dimensions.Area != 1,
				Name = name,
				ProcessingToApply = TextureProcessingConfig.None
			}
		);
	}
	/// <summary>
	/// Creates a texture by evaluating the given texel pattern, using the given config.
	/// </summary>
	/// <typeparam name="TTexel">The texel type the pattern produces.</typeparam>
	/// <param name="pattern">The pattern to evaluate. The texture takes its dimensions from this.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture CreateTexture<TTexel>(in TexturePattern<TTexel> pattern, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> {
		var buffer = PreallocateBuffer<TTexel>(pattern.Dimensions.Area);
		_ = PrintPattern(pattern, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = pattern.Dimensions }, in config);
	}

	/// <summary>
	/// Creates a single-texel texture of one uniform value.
	/// </summary>
	/// <remarks>
	/// A texture of one texel is the cheapest way to supply a constant value to a material that requires a whole map.
	/// </remarks>
	/// <typeparam name="TTexel">The type of the texel being supplied.</typeparam>
	/// <param name="plainFill">The value the texture's only texel takes.</param>
	/// <param name="dataType">What the texel represents, which determines how it is interpreted.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateTexture<TTexel>(TTexel plainFill, TextureDataType dataType, ReadOnlySpan<char> name = default) where TTexel : unmanaged, ITexel<TTexel> {
		return CreateTexture(
			new ReadOnlySpan<TTexel>(in plainFill),
			XYPair<int>.One,
			dataType,
			generateMipMaps: false,
			name
		);
	}
	/// <summary>
	/// Creates a single-texel texture of one uniform value, using the given config.
	/// </summary>
	/// <remarks>
	/// A texture of one texel is the cheapest way to supply a constant value to a material that requires a whole map.
	/// </remarks>
	/// <typeparam name="TTexel">The type of the texel being supplied.</typeparam>
	/// <param name="plainFill">The value the texture's only texel takes.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture CreateTexture<TTexel>(TTexel plainFill, in TextureCreationConfig config) where TTexel : unmanaged, ITexel<TTexel> {
		return CreateTexture(
			new ReadOnlySpan<TTexel>(in plainFill),
			new TextureGenerationConfig { Dimensions = XYPair<int>.One },
			in config
		);
	}
	#endregion

	#region Color Map Patterns
	/// <summary>
	/// The colour used for a colour map when none is specified: opaque white.
	/// </summary>
	static readonly ColorVect DefaultColor = ColorVect.WhiteOpaque;
	/// <summary>
	/// Converts a colour in to the texel form a colour map stores.
	/// </summary>
	/// <param name="color">The colour to convert.</param>
	static TexelRgba32 CreateColorTexel(ColorVect color) => new(color);

	/// <summary>
	/// Returns the texture creation config that <c>CreateColorMap</c> uses for a colour map of the given dimensions.
	/// </summary>
	/// <remarks>
	/// Exposed so that code preparing its own texels can create the resulting texture with exactly the same settings
	/// <c>CreateColorMap</c> would have used.
	/// </remarks>
	/// <param name="dimensions">The width and height the map will have, in texels.</param>
	/// <param name="includeAlpha">Whether the map will carry an alpha channel.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	static TextureCreationConfig GetColorMapCreationConfig(XYPair<int> dimensions, bool includeAlpha, ReadOnlySpan<char> name = default) {
		return TextureCreationConfig.ForColorTexture(name) with {
			GenerateMipMaps = dimensions.Area != 1,
			ProcessingToApply = includeAlpha ? TextureProcessingConfig.PremultiplyAlpha() : TextureProcessingConfig.None
		};
	}
	/// <summary>
	/// Writes the texels of a colour map in to the given buffer, with an alpha channel.
	/// </summary>
	/// <remarks>
	/// This is the same texel generation <c>CreateColorMap</c> performs, exposed separately so that texels can be produced
	/// without touching the builder — on a worker thread, for example.
	/// </remarks>
	/// <param name="colorPattern">The pattern supplying each texel's colour.</param>
	/// <param name="destinationBuffer">The buffer to write in to. Must be at least as long as the pattern's area.</param>
	static void PrintColorMap(in TexturePattern<ColorVect> colorPattern, Span<TexelRgba32> destinationBuffer) => _ = PrintPattern(colorPattern, &TexelRgba32.ConvertFrom, destinationBuffer);
	/// <summary>
	/// Writes the texels of a colour map in to the given buffer, without an alpha channel.
	/// </summary>
	/// <remarks>
	/// This is the same texel generation <c>CreateColorMap</c> performs, exposed separately so that texels can be produced
	/// without touching the builder — on a worker thread, for example.
	/// </remarks>
	/// <param name="colorPattern">The pattern supplying each texel's colour.</param>
	/// <param name="destinationBuffer">The buffer to write in to. Must be at least as long as the pattern's area.</param>
	static void PrintColorMap(in TexturePattern<ColorVect> colorPattern, Span<TexelRgb24> destinationBuffer) => _ = PrintPattern(colorPattern, &TexelRgb24.ConvertFrom, destinationBuffer);

	/// <summary>
	/// Creates a colour map from the given pattern.
	/// </summary>
	/// <remarks>
	/// A colour map supplies the base colour of a surface.
	/// </remarks>
	/// <param name="colorPattern">The pattern supplying each texel's colour. The texture takes its dimensions from this.</param>
	/// <param name="includeAlpha">Whether to keep the pattern's alpha channel. Doing so also premultiplies the result, which is what a blending material expects.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateColorMap(in TexturePattern<ColorVect> colorPattern, bool includeAlpha, ReadOnlySpan<char> name = default) {
		return CreateColorMap(colorPattern, includeAlpha, GetColorMapCreationConfig(colorPattern.Dimensions, includeAlpha, name));
	}
	/// <summary>
	/// Creates a colour map from the given pattern, using the given config.
	/// </summary>
	/// <param name="colorPattern">The pattern supplying each texel's colour. The texture takes its dimensions from this.</param>
	/// <param name="includeAlpha">Whether to keep the pattern's alpha channel.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
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

	/// <summary>
	/// Creates a single-texel colour map of opaque white.
	/// </summary>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateColorMap(ReadOnlySpan<char> name = default) => CreateColorMap(DefaultColor, includeAlpha: false, name);
	/// <summary>
	/// Creates a single-texel colour map of one uniform colour.
	/// </summary>
	/// <param name="color">The colour of the map's only texel.</param>
	/// <param name="includeAlpha">Whether to keep the colour's alpha channel.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateColorMap(ColorVect color, bool includeAlpha, ReadOnlySpan<char> name = default) {
		return CreateColorMap(color, includeAlpha, GetColorMapCreationConfig(XYPair<int>.One, includeAlpha, name));
	}
	/// <summary>
	/// Creates a single-texel colour map of one uniform colour, using the given config.
	/// </summary>
	/// <param name="color">The colour of the map's only texel.</param>
	/// <param name="includeAlpha">Whether to keep the colour's alpha channel.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture CreateColorMap(ColorVect color, bool includeAlpha, in TextureCreationConfig config) {
		return includeAlpha
			? CreateTexture(new TexelRgba32(color), in config)
			: CreateTexture(new TexelRgb24(color), in config);
	}

	/// <summary>
	/// Returns the texture creation config that <c>CreateCanvasTexture</c> uses for a canvas texture of the given dimensions.
	/// </summary>
	/// <remarks>
	/// Exposed so that code preparing its own texels can create the resulting texture with exactly the same settings
	/// <c>CreateCanvasTexture</c> would have used.
	/// </remarks>
	/// <param name="dimensions">The width and height the texture will have, in texels.</param>
	/// <param name="includeAlpha">Whether the texture will carry an alpha channel.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	static TextureCreationConfig GetCanvasTextureCreationConfig(XYPair<int> dimensions, bool includeAlpha, ReadOnlySpan<char> name = default) {
		return TextureCreationConfig.ForCanvasTexture(name) with {
			GenerateMipMaps = dimensions.Area != 1,
			ProcessingToApply = includeAlpha ? TextureProcessingConfig.PremultiplyAlpha() : TextureProcessingConfig.None
		};
	}
	/// <summary>
	/// Writes the texels of a canvas texture in to the given buffer, with an alpha channel.
	/// </summary>
	/// <remarks>
	/// This is the same texel generation <c>CreateCanvasTexture</c> performs, exposed separately so that texels can be
	/// produced without touching the builder — on a worker thread, for example.
	/// </remarks>
	/// <param name="colorPattern">The pattern supplying each texel's colour.</param>
	/// <param name="destinationBuffer">The buffer to write in to. Must be at least as long as the pattern's area.</param>
	static void PrintCanvasTexture(in TexturePattern<ColorVect> colorPattern, Span<TexelRgba32> destinationBuffer) => _ = PrintPattern(colorPattern, &TexelRgba32.ConvertFrom, destinationBuffer);
	/// <summary>
	/// Writes the texels of a canvas texture in to the given buffer, without an alpha channel.
	/// </summary>
	/// <remarks>
	/// This is the same texel generation <c>CreateCanvasTexture</c> performs, exposed separately so that texels can be
	/// produced without touching the builder — on a worker thread, for example.
	/// </remarks>
	/// <param name="colorPattern">The pattern supplying each texel's colour.</param>
	/// <param name="destinationBuffer">The buffer to write in to. Must be at least as long as the pattern's area.</param>
	static void PrintCanvasTexture(in TexturePattern<ColorVect> colorPattern, Span<TexelRgb24> destinationBuffer) => _ = PrintPattern(colorPattern, &TexelRgb24.ConvertFrom, destinationBuffer);

	/// <summary>
	/// Creates a canvas texture from the given pattern.
	/// </summary>
	/// <remarks>
	/// Canvas textures are drawn flat over the scene rather than mapped on to a surface in it, so they skip the colour
	/// conversion and filtering that a texture in the world goes through.
	/// </remarks>
	/// <param name="colorPattern">The pattern supplying each texel's colour. The texture takes its dimensions from this.</param>
	/// <param name="includeAlpha">Whether to keep the pattern's alpha channel. Doing so also premultiplies the result.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateCanvasTexture(in TexturePattern<ColorVect> colorPattern, bool includeAlpha, ReadOnlySpan<char> name = default) {
		return CreateCanvasTexture(colorPattern, includeAlpha, GetCanvasTextureCreationConfig(colorPattern.Dimensions, includeAlpha, name));
	}
	/// <summary>
	/// Creates a canvas texture from the given pattern, using the given config.
	/// </summary>
	/// <param name="colorPattern">The pattern supplying each texel's colour. The texture takes its dimensions from this.</param>
	/// <param name="includeAlpha">Whether to keep the pattern's alpha channel.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
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

	/// <summary>
	/// Creates a single-texel canvas texture of opaque white.
	/// </summary>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateCanvasTexture(ReadOnlySpan<char> name = default) => CreateCanvasTexture(DefaultColor, includeAlpha: false, name);
	/// <summary>
	/// Creates a single-texel canvas texture of one uniform colour.
	/// </summary>
	/// <param name="color">The colour of the texture's only texel.</param>
	/// <param name="includeAlpha">Whether to keep the colour's alpha channel.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateCanvasTexture(ColorVect color, bool includeAlpha, ReadOnlySpan<char> name = default) {
		return CreateCanvasTexture(color, includeAlpha, GetCanvasTextureCreationConfig(XYPair<int>.One, includeAlpha, name));
	}
	/// <summary>
	/// Creates a single-texel canvas texture of one uniform colour, using the given config.
	/// </summary>
	/// <param name="color">The colour of the texture's only texel.</param>
	/// <param name="includeAlpha">Whether to keep the colour's alpha channel.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture CreateCanvasTexture(ColorVect color, bool includeAlpha, in TextureCreationConfig config) {
		return includeAlpha
			? CreateTexture(new TexelRgba32(color), in config)
			: CreateTexture(new TexelRgb24(color), in config);
	}
	#endregion

	#region Normal Map Patterns
	/// <summary>
	/// The surface offset used for a normal map when none is specified: none at all, i.e. a perfectly flat surface.
	/// </summary>
	static readonly SphericalTranslation DefaultNormalOffset = SphericalTranslation.ZeroZero;
	/// <summary>
	/// Converts a surface offset in to the texel form a normal map stores.
	/// </summary>
	/// <param name="normalOffset">How far, and in which direction, the surface tilts away from flat at this texel.</param>
	static TexelRgb24 CreateNormalTexel(SphericalTranslation normalOffset) {
		const float Multiplicand = Byte.MaxValue * 0.5f;

		var v = normalOffset.Translate(new Direction(1f, 0f, 0f), new Direction(0f, 0f, 1f))
					.ToVector3()
					+ Vector3.One;
		v *= Multiplicand;
		return new((byte) v.X, (byte) v.Y, (byte) v.Z);
	}

	/// <summary>
	/// Returns the texture creation config that <c>CreateNormalMap</c> uses for a normal map of the given dimensions.
	/// </summary>
	/// <remarks>
	/// Exposed so that code preparing its own texels can create the resulting texture with exactly the same settings
	/// <c>CreateNormalMap</c> would have used.
	/// </remarks>
	/// <param name="dimensions">The width and height the map will have, in texels.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	static TextureCreationConfig GetNormalMapCreationConfig(XYPair<int> dimensions, ReadOnlySpan<char> name = default) {
		return TextureCreationConfig.ForDataTexture(TextureDataType.LinearDataUnitVector, name) with {
			GenerateMipMaps = dimensions.Area != 1
		};
	}
	/// <summary>
	/// Writes the texels of a normal map in to the given buffer.
	/// </summary>
	/// <remarks>
	/// This is the same texel generation <c>CreateNormalMap</c> performs, exposed separately so that texels can be produced
	/// without touching the builder — on a worker thread, for example.
	/// </remarks>
	/// <param name="normalPattern">The pattern supplying each texel's surface offset.</param>
	/// <param name="destinationBuffer">The buffer to write in to. Must be at least as long as the pattern's area.</param>
	static void PrintNormalMap(in TexturePattern<SphericalTranslation> normalPattern, Span<TexelRgb24> destinationBuffer) => _ = PrintPattern(normalPattern, &CreateNormalTexel, destinationBuffer);

	/// <summary>
	/// Creates a normal map from the given pattern.
	/// </summary>
	/// <remarks>
	/// A normal map describes the small-scale bumps and grooves of a surface, which is what makes light catch on detail the
	/// geometry itself does not have.
	/// </remarks>
	/// <param name="normalPattern">The pattern supplying each texel's surface offset. The texture takes its dimensions from this.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateNormalMap(in TexturePattern<SphericalTranslation> normalPattern, ReadOnlySpan<char> name = default) {
		return CreateNormalMap(normalPattern, GetNormalMapCreationConfig(normalPattern.Dimensions, name));
	}
	/// <summary>
	/// Creates a normal map from the given pattern, using the given config.
	/// </summary>
	/// <param name="normalPattern">The pattern supplying each texel's surface offset. The texture takes its dimensions from this.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture CreateNormalMap(in TexturePattern<SphericalTranslation> normalPattern, in TextureCreationConfig config) {
		var buffer = PreallocateBuffer<TexelRgb24>(normalPattern.Dimensions.Area);
		PrintNormalMap(normalPattern, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = normalPattern.Dimensions }, in config);
	}

	/// <summary>
	/// Creates a single-texel normal map of one uniform surface offset.
	/// </summary>
	/// <param name="normalOffset">How far, and in which direction, the surface tilts away from flat, or <see langword="null"/> for a perfectly flat surface.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateNormalMap(SphericalTranslation? normalOffset = null, ReadOnlySpan<char> name = default) {
		return CreateNormalMap(normalOffset ?? DefaultNormalOffset, GetNormalMapCreationConfig(XYPair<int>.One, name));
	}
	/// <summary>
	/// Creates a single-texel normal map of one uniform surface offset, using the given config.
	/// </summary>
	/// <param name="normalOffset">How far, and in which direction, the surface tilts away from flat.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture CreateNormalMap(SphericalTranslation normalOffset, in TextureCreationConfig config) {
		return CreateTexture(CreateNormalTexel(normalOffset), in config);
	}
	#endregion

	#region Orm/Ormr Map Patterns
	/// <summary>
	/// The occlusion value used when none is specified: <c>1f</c>, i.e. no part of the surface shadowed by its own shape.
	/// </summary>
	static readonly Real DefaultOcclusion = 1f;
	/// <summary>
	/// The roughness value used when none is specified: <c>0.4f</c>, i.e. neither mirror-smooth nor completely matte.
	/// </summary>
	static readonly Real DefaultRoughness = 0.4f;
	/// <summary>
	/// The metallic value used when none is specified: <c>0f</c>, i.e. not metallic at all.
	/// </summary>
	static readonly Real DefaultMetallic = 0f;
	/// <summary>
	/// The reflectance value used when none is specified: <c>0.5f</c>, the ordinary reflectance of most non-metallic surfaces.
	/// </summary>
	static readonly Real DefaultReflectance = 0.5f;
	/// <summary>
	/// Combines occlusion, roughness and metallic values in to the texel form an ORM map stores.
	/// </summary>
	/// <param name="occlusion">How much ambient light this part of the surface is shielded from by its own shape, where <c>1f</c> is none at all.</param>
	/// <param name="roughness">How rough the surface is, where <c>0f</c> is mirror-smooth and <c>1f</c> is completely matte.</param>
	/// <param name="metallic">How metallic the surface is, where <c>0f</c> is not at all and <c>1f</c> is fully metallic.</param>
	static TexelRgb24 CreateOcclusionRoughnessMetallicTexel(Real occlusion, Real roughness, Real metallic) => TexelRgb24.FromNormalizedFloats(occlusion, roughness, metallic);
	/// <summary>
	/// Combines occlusion, roughness, metallic and reflectance values in to the texel form an ORMR map stores.
	/// </summary>
	/// <param name="occlusion">How much ambient light this part of the surface is shielded from by its own shape, where <c>1f</c> is none at all.</param>
	/// <param name="roughness">How rough the surface is, where <c>0f</c> is mirror-smooth and <c>1f</c> is completely matte.</param>
	/// <param name="metallic">How metallic the surface is, where <c>0f</c> is not at all and <c>1f</c> is fully metallic.</param>
	/// <param name="reflectance">How strongly the surface reflects light striking it head-on.</param>
	static TexelRgba32 CreateOcclusionRoughnessMetallicReflectanceTexel(Real occlusion, Real roughness, Real metallic, Real reflectance) => TexelRgba32.FromNormalizedFloats(occlusion, roughness, metallic, reflectance);

	/// <summary>
	/// Returns the texture creation config that <c>CreateOcclusionRoughnessMetallicMap</c> uses for a map of the given dimensions.
	/// </summary>
	/// <remarks>
	/// Exposed so that code preparing its own texels can create the resulting texture with exactly the same settings.
	/// </remarks>
	/// <param name="dimensions">The width and height the map will have, in texels.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	static TextureCreationConfig GetOcclusionRoughnessMetallicMapCreationConfig(XYPair<int> dimensions, ReadOnlySpan<char> name = default) {
		return TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, name) with {
			GenerateMipMaps = dimensions.Area != 1
		};
	}
	/// <summary>
	/// Writes the texels of an ORM map in to the given buffer.
	/// </summary>
	/// <remarks>
	/// This is the same texel generation <c>CreateOcclusionRoughnessMetallicMap</c> performs, exposed separately so that
	/// texels can be produced without touching the builder — on a worker thread, for example.
	/// </remarks>
	/// <param name="occlusionPattern">The pattern supplying each texel's occlusion value.</param>
	/// <param name="roughnessPattern">The pattern supplying each texel's roughness value.</param>
	/// <param name="metallicPattern">The pattern supplying each texel's metallic value.</param>
	/// <param name="destinationBuffer">The buffer to write in to. Must be at least as long as the combined area of the patterns, which takes the largest width and height of any of them.</param>
	static void PrintOcclusionRoughnessMetallicMap(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, Span<TexelRgb24> destinationBuffer) {
		_ = PrintPattern(occlusionPattern, roughnessPattern, metallicPattern, &TexelRgb24.FromNormalizedFloats, destinationBuffer);
	}

	/// <summary>
	/// Returns the texture creation config that <c>CreateOcclusionRoughnessMetallicReflectanceMap</c> uses for a map of the given dimensions.
	/// </summary>
	/// <remarks>
	/// Exposed so that code preparing its own texels can create the resulting texture with exactly the same settings.
	/// </remarks>
	/// <param name="dimensions">The width and height the map will have, in texels.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	static TextureCreationConfig GetOcclusionRoughnessMetallicReflectanceMapCreationConfig(XYPair<int> dimensions, ReadOnlySpan<char> name = default) {
		return TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, name) with {
			GenerateMipMaps = dimensions.Area != 1
		};
	}
	/// <summary>
	/// Writes the texels of an ORMR map in to the given buffer.
	/// </summary>
	/// <remarks>
	/// This is the same texel generation <c>CreateOcclusionRoughnessMetallicReflectanceMap</c> performs, exposed separately
	/// so that texels can be produced without touching the builder — on a worker thread, for example.
	/// </remarks>
	/// <param name="occlusionPattern">The pattern supplying each texel's occlusion value.</param>
	/// <param name="roughnessPattern">The pattern supplying each texel's roughness value.</param>
	/// <param name="metallicPattern">The pattern supplying each texel's metallic value.</param>
	/// <param name="reflectancePattern">The pattern supplying each texel's reflectance value.</param>
	/// <param name="destinationBuffer">The buffer to write in to. Must be at least as long as the combined area of the patterns, which takes the largest width and height of any of them.</param>
	static void PrintOcclusionRoughnessMetallicReflectanceMap(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, in TexturePattern<Real> reflectancePattern, Span<TexelRgba32> destinationBuffer) {
		_ = PrintPattern(occlusionPattern, roughnessPattern, metallicPattern, reflectancePattern, &TexelRgba32.FromNormalizedFloats, destinationBuffer);
	}

	/// <summary>
	/// Creates an ORM map from the given patterns.
	/// </summary>
	/// <remarks>
	/// An ORM map packs occlusion, roughness and metallic values in to one texture's three channels; the result takes the
	/// largest width and height of any of the patterns given.
	/// </remarks>
	/// <param name="occlusionPattern">The pattern supplying each texel's occlusion value.</param>
	/// <param name="roughnessPattern">The pattern supplying each texel's roughness value.</param>
	/// <param name="metallicPattern">The pattern supplying each texel's metallic value.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateOcclusionRoughnessMetallicMap(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, ReadOnlySpan<char> name = default) {
		return CreateOcclusionRoughnessMetallicMap(
			occlusionPattern,
			roughnessPattern,
			metallicPattern,
			GetOcclusionRoughnessMetallicMapCreationConfig(GetCompositePatternDimensions(occlusionPattern, roughnessPattern, metallicPattern), name)
		);
	}

	/// <summary>
	/// Creates an ORM map from the given patterns, using the given config.
	/// </summary>
	/// <param name="occlusionPattern">The pattern supplying each texel's occlusion value.</param>
	/// <param name="roughnessPattern">The pattern supplying each texel's roughness value.</param>
	/// <param name="metallicPattern">The pattern supplying each texel's metallic value.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture CreateOcclusionRoughnessMetallicMap(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(occlusionPattern, roughnessPattern, metallicPattern);
		var buffer = PreallocateBuffer<TexelRgb24>(dimensions.Area);
		PrintOcclusionRoughnessMetallicMap(occlusionPattern, roughnessPattern, metallicPattern, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}

	/// <summary>
	/// Creates an ORMR map from the given patterns.
	/// </summary>
	/// <remarks>
	/// An ORMR map packs occlusion, roughness, metallic and reflectance values in to one texture's four channels; the result
	/// takes the largest width and height of any of the patterns given.
	/// </remarks>
	/// <param name="occlusionPattern">The pattern supplying each texel's occlusion value.</param>
	/// <param name="roughnessPattern">The pattern supplying each texel's roughness value.</param>
	/// <param name="metallicPattern">The pattern supplying each texel's metallic value.</param>
	/// <param name="reflectancePattern">The pattern supplying each texel's reflectance value.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateOcclusionRoughnessMetallicReflectanceMap(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, in TexturePattern<Real> reflectancePattern, ReadOnlySpan<char> name = default) {
		return CreateOcclusionRoughnessMetallicReflectanceMap(
			occlusionPattern,
			roughnessPattern,
			metallicPattern,
			reflectancePattern,
			GetOcclusionRoughnessMetallicReflectanceMapCreationConfig(GetCompositePatternDimensions(occlusionPattern, roughnessPattern, metallicPattern, reflectancePattern), name)
		);
	}

	/// <summary>
	/// Creates an ORMR map from the given patterns, using the given config.
	/// </summary>
	/// <param name="occlusionPattern">The pattern supplying each texel's occlusion value.</param>
	/// <param name="roughnessPattern">The pattern supplying each texel's roughness value.</param>
	/// <param name="metallicPattern">The pattern supplying each texel's metallic value.</param>
	/// <param name="reflectancePattern">The pattern supplying each texel's reflectance value.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture CreateOcclusionRoughnessMetallicReflectanceMap(in TexturePattern<Real> occlusionPattern, in TexturePattern<Real> roughnessPattern, in TexturePattern<Real> metallicPattern, in TexturePattern<Real> reflectancePattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(occlusionPattern, roughnessPattern, metallicPattern, reflectancePattern);
		var buffer = PreallocateBuffer<TexelRgba32>(dimensions.Area);
		PrintOcclusionRoughnessMetallicReflectanceMap(occlusionPattern, roughnessPattern, metallicPattern, reflectancePattern, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}

	/// <summary>
	/// Creates a single-texel ORM map of uniform values.
	/// </summary>
	/// <param name="occlusion">The occlusion value, or <see langword="null"/> for <c>1f</c>.</param>
	/// <param name="roughness">The roughness value, or <see langword="null"/> for <c>0.4f</c>.</param>
	/// <param name="metallic">The metallic value, or <see langword="null"/> for <c>0f</c>.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateOcclusionRoughnessMetallicMap(Real? occlusion = null, Real? roughness = null, Real? metallic = null, ReadOnlySpan<char> name = default) {
		return CreateOcclusionRoughnessMetallicMap(
			occlusion ?? DefaultOcclusion,
			roughness ?? DefaultRoughness,
			metallic ?? DefaultMetallic,
			GetOcclusionRoughnessMetallicMapCreationConfig(XYPair<int>.One, name)
		);
	}
	/// <summary>
	/// Creates a single-texel ORM map of uniform values, using the given config.
	/// </summary>
	/// <param name="occlusion">The occlusion value.</param>
	/// <param name="roughness">The roughness value.</param>
	/// <param name="metallic">The metallic value.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture CreateOcclusionRoughnessMetallicMap(Real occlusion, Real roughness, Real metallic, in TextureCreationConfig config) {
		return CreateTexture(TexelRgb24.FromNormalizedFloats(occlusion, roughness, metallic), in config);
	}
	/// <summary>
	/// Creates a single-texel ORMR map of uniform values.
	/// </summary>
	/// <param name="occlusion">The occlusion value, or <see langword="null"/> for <c>1f</c>.</param>
	/// <param name="roughness">The roughness value, or <see langword="null"/> for <c>0.4f</c>.</param>
	/// <param name="metallic">The metallic value, or <see langword="null"/> for <c>0f</c>.</param>
	/// <param name="reflectance">The reflectance value, or <see langword="null"/> for <c>0.5f</c>.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateOcclusionRoughnessMetallicReflectanceMap(Real? occlusion = null, Real? roughness = null, Real? metallic = null, Real? reflectance = null, ReadOnlySpan<char> name = default) {
		return CreateOcclusionRoughnessMetallicReflectanceMap(
			occlusion ?? DefaultOcclusion,
			roughness ?? DefaultRoughness,
			metallic ?? DefaultMetallic,
			reflectance ?? DefaultReflectance,
			GetOcclusionRoughnessMetallicReflectanceMapCreationConfig(XYPair<int>.One, name)
		);
	}
	/// <summary>
	/// Creates a single-texel ORMR map of uniform values, using the given config.
	/// </summary>
	/// <param name="occlusion">The occlusion value.</param>
	/// <param name="roughness">The roughness value.</param>
	/// <param name="metallic">The metallic value.</param>
	/// <param name="reflectance">The reflectance value.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture CreateOcclusionRoughnessMetallicReflectanceMap(Real occlusion, Real roughness, Real metallic, Real reflectance, in TextureCreationConfig config) {
		return CreateTexture(TexelRgba32.FromNormalizedFloats(occlusion, roughness, metallic, reflectance), in config);
	}
	#endregion

	#region Absorption Transmission Map Patterns
	/// <summary>
	/// The absorption colour used when none is specified: black, i.e. no colour of light is absorbed.
	/// </summary>
	static readonly ColorVect DefaultAbsorption = ColorVect.BlackOpaque;
	/// <summary>
	/// The transmission value used when none is specified: <c>0.5f</c>, i.e. half the light gets through.
	/// </summary>
	static readonly Real DefaultTransmission = 0.5f;
	/// <summary>
	/// Combines an absorption colour and a transmission value in to the texel form an absorption-transmission map stores.
	/// </summary>
	/// <param name="absorption">Which colours of light the surface absorbs; what remains is what is seen through it.</param>
	/// <param name="transmission">How much light gets through the surface at all, where <c>0f</c> is opaque and <c>1f</c> fully transparent.</param>
	static TexelRgba32 CreateAbsorptionTransmissionTexel(ColorVect absorption, Real transmission) => new(new TexelRgb24(absorption), (byte) (transmission * Byte.MaxValue));

	/// <summary>
	/// Returns the texture creation config that <c>CreateAbsorptionTransmissionMap</c> uses for a map of the given dimensions.
	/// </summary>
	/// <remarks>
	/// Exposed so that code preparing its own texels can create the resulting texture with exactly the same settings.
	/// </remarks>
	/// <param name="dimensions">The width and height the map will have, in texels.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	static TextureCreationConfig GetAbsorptionTransmissionMapCreationConfig(XYPair<int> dimensions, ReadOnlySpan<char> name = default) {
		return TextureCreationConfig.ForColorTexture(name) with {
			GenerateMipMaps = dimensions.Area != 1
		};
	}
	/// <summary>
	/// Writes the texels of an absorption-transmission map in to the given buffer.
	/// </summary>
	/// <remarks>
	/// This is the same texel generation <c>CreateAbsorptionTransmissionMap</c> performs, exposed separately so that texels
	/// can be produced without touching the builder — on a worker thread, for example.
	/// </remarks>
	/// <param name="absorptionPattern">The pattern supplying each texel's absorption colour.</param>
	/// <param name="transmissionPattern">The pattern supplying each texel's transmission value.</param>
	/// <param name="destinationBuffer">The buffer to write in to. Must be at least as long as the combined area of the patterns, which takes the largest width and height of either.</param>
	static void PrintAbsorptionTransmissionMap(in TexturePattern<ColorVect> absorptionPattern, in TexturePattern<Real> transmissionPattern, Span<TexelRgba32> destinationBuffer) {
		_ = PrintPattern(absorptionPattern, transmissionPattern, &CreateAbsorptionTransmissionTexel, destinationBuffer);
	}

	/// <summary>
	/// Creates an absorption-transmission map from the given patterns.
	/// </summary>
	/// <remarks>
	/// This describes how light passes through a see-through surface: which colours it absorbs, and how much light gets
	/// through at all. Only transmissive materials use it.
	/// </remarks>
	/// <param name="absorptionPattern">The pattern supplying each texel's absorption colour.</param>
	/// <param name="transmissionPattern">The pattern supplying each texel's transmission value.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateAbsorptionTransmissionMap(in TexturePattern<ColorVect> absorptionPattern, in TexturePattern<Real> transmissionPattern, ReadOnlySpan<char> name = default) {
		return CreateAbsorptionTransmissionMap(
			absorptionPattern,
			transmissionPattern,
			GetAbsorptionTransmissionMapCreationConfig(GetCompositePatternDimensions(absorptionPattern, transmissionPattern), name)
		);
	}
	/// <summary>
	/// Creates an absorption-transmission map from the given patterns, using the given config.
	/// </summary>
	/// <param name="absorptionPattern">The pattern supplying each texel's absorption colour.</param>
	/// <param name="transmissionPattern">The pattern supplying each texel's transmission value.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture CreateAbsorptionTransmissionMap(in TexturePattern<ColorVect> absorptionPattern, in TexturePattern<Real> transmissionPattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(absorptionPattern, transmissionPattern);
		var buffer = PreallocateBuffer<TexelRgba32>(dimensions.Area);
		PrintAbsorptionTransmissionMap(absorptionPattern, transmissionPattern, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}

	/// <summary>
	/// Creates a single-texel absorption-transmission map of uniform values.
	/// </summary>
	/// <param name="absorption">The absorption colour, or <see langword="null"/> for black, which absorbs nothing.</param>
	/// <param name="transmission">The transmission value, or <see langword="null"/> for <c>0.5f</c>.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateAbsorptionTransmissionMap(ColorVect? absorption = null, Real? transmission = null, ReadOnlySpan<char> name = default) {
		return CreateAbsorptionTransmissionMap(
			absorption ?? DefaultAbsorption,
			transmission ?? DefaultTransmission,
			GetAbsorptionTransmissionMapCreationConfig(XYPair<int>.One, name)
		);
	}
	/// <summary>
	/// Creates a single-texel absorption-transmission map of uniform values, using the given config.
	/// </summary>
	/// <param name="absorption">The absorption colour.</param>
	/// <param name="transmission">The transmission value.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture CreateAbsorptionTransmissionMap(ColorVect absorption, Real transmission, in TextureCreationConfig config) {
		return CreateTexture(CreateAbsorptionTransmissionTexel(absorption, transmission), in config);
	}
	#endregion

	#region Emissive Map Patterns
	/// <summary>
	/// The emissive colour used when none is specified: the warm yellow-white of an incandescent light bulb.
	/// </summary>
	static readonly ColorVect DefaultEmissiveColor = StandardColor.LightingIncandescentBulb;
	/// <summary>
	/// The emissive intensity used when none is specified: <c>1f</c>, i.e. glowing at full strength.
	/// </summary>
	static readonly Real DefaultEmissiveIntensity = 1f;
	/// <summary>
	/// Combines a colour and an intensity in to the texel form an emissive map stores.
	/// </summary>
	/// <param name="color">The colour of the light this part of the surface appears to emit.</param>
	/// <param name="intensity">How strongly it glows, where <c>0f</c> is not at all and <c>1f</c> is full strength.</param>
	static TexelRgba32 CreateEmissiveTexel(ColorVect color, Real intensity) => new(new TexelRgb24(color), (byte) (intensity * Byte.MaxValue));

	/// <summary>
	/// Returns the texture creation config that <c>CreateEmissiveMap</c> uses for a map of the given dimensions.
	/// </summary>
	/// <remarks>
	/// Exposed so that code preparing its own texels can create the resulting texture with exactly the same settings.
	/// </remarks>
	/// <param name="dimensions">The width and height the map will have, in texels.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	static TextureCreationConfig GetEmissiveMapCreationConfig(XYPair<int> dimensions, ReadOnlySpan<char> name = default) {
		return TextureCreationConfig.ForColorTexture(name) with {
			GenerateMipMaps = dimensions.Area != 1
		};
	}
	/// <summary>
	/// Writes the texels of an emissive map in to the given buffer.
	/// </summary>
	/// <remarks>
	/// This is the same texel generation <c>CreateEmissiveMap</c> performs, exposed separately so that texels can be
	/// produced without touching the builder — on a worker thread, for example.
	/// </remarks>
	/// <param name="colorPattern">The pattern supplying each texel's emitted colour.</param>
	/// <param name="intensityPattern">The pattern supplying each texel's intensity.</param>
	/// <param name="destinationBuffer">The buffer to write in to. Must be at least as long as the combined area of the patterns, which takes the largest width and height of either.</param>
	static void PrintEmissiveMap(in TexturePattern<ColorVect> colorPattern, in TexturePattern<Real> intensityPattern, Span<TexelRgba32> destinationBuffer) {
		_ = PrintPattern(colorPattern, intensityPattern, &CreateEmissiveTexel, destinationBuffer);
	}

	/// <summary>
	/// Creates an emissive map from the given patterns.
	/// </summary>
	/// <remarks>
	/// An emissive map makes parts of a surface appear to glow with their own light, independently of whatever else is
	/// lighting the scene.
	/// </remarks>
	/// <param name="colorPattern">The pattern supplying each texel's emitted colour.</param>
	/// <param name="intensityPattern">The pattern supplying each texel's intensity.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateEmissiveMap(in TexturePattern<ColorVect> colorPattern, in TexturePattern<Real> intensityPattern, ReadOnlySpan<char> name = default) {
		return CreateEmissiveMap(
			colorPattern,
			intensityPattern,
			GetEmissiveMapCreationConfig(GetCompositePatternDimensions(colorPattern, intensityPattern), name)
		);
	}
	/// <summary>
	/// Creates an emissive map from the given patterns, using the given config.
	/// </summary>
	/// <param name="colorPattern">The pattern supplying each texel's emitted colour.</param>
	/// <param name="intensityPattern">The pattern supplying each texel's intensity.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture CreateEmissiveMap(in TexturePattern<ColorVect> colorPattern, in TexturePattern<Real> intensityPattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(colorPattern, intensityPattern);
		var buffer = PreallocateBuffer<TexelRgba32>(dimensions.Area);
		PrintEmissiveMap(colorPattern, intensityPattern, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}

	/// <summary>
	/// Creates a single-texel emissive map of one uniform colour and intensity.
	/// </summary>
	/// <param name="color">The emitted colour, or <see langword="null"/> for the colour of an incandescent bulb.</param>
	/// <param name="intensity">The intensity, or <see langword="null"/> for <c>1f</c>.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateEmissiveMap(ColorVect? color = null, Real? intensity = null, ReadOnlySpan<char> name = default) {
		return CreateEmissiveMap(
			color ?? DefaultEmissiveColor,
			intensity ?? DefaultEmissiveIntensity,
			GetEmissiveMapCreationConfig(XYPair<int>.One, name)
		);
	}
	/// <summary>
	/// Creates a single-texel emissive map of one uniform colour and intensity, using the given config.
	/// </summary>
	/// <param name="color">The emitted colour.</param>
	/// <param name="intensity">The intensity.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture CreateEmissiveMap(ColorVect color, Real intensity, in TextureCreationConfig config) {
		return CreateTexture(CreateEmissiveTexel(color, intensity), in config);
	}
	#endregion

	#region Anisotropy Map Patterns
	/// <summary>
	/// The anisotropy angle used when none is specified: <c>0°</c>.
	/// </summary>
	static readonly Angle DefaultAnisotropyRadialAngle = 0f;
	/// <summary>
	/// The anisotropy strength used when none is specified: <c>1f</c>, i.e. full strength.
	/// </summary>
	static readonly Real DefaultAnisotropyStrength = 1f;
	/// <summary>
	/// Combines an angle and a strength in to the texel form an anisotropy map stores.
	/// </summary>
	/// <param name="radialAngle">The direction, across the surface, along which light is stretched.</param>
	/// <param name="strength">How pronounced the effect is, where <c>0f</c> is none at all and <c>1f</c> is full strength.</param>
	static TexelRgb24 CreateAnisotropyTexel(Angle radialAngle, Real strength) {
		var asTangentSpaceVect2 = ((XYPair<float>.FromPolarAngle(radialAngle)
			+ XYPair<float>.One)
			* (Byte.MaxValue * 0.5f))
			.CastWithRoundingIfNecessary<float, byte>(MidpointRounding.AwayFromZero);

		return new TexelRgb24(asTangentSpaceVect2.X, asTangentSpaceVect2.Y, (byte) (strength * Byte.MaxValue));
	}

	/// <summary>
	/// Returns the texture creation config that <c>CreateAnisotropyMap</c> uses for a map of the given dimensions.
	/// </summary>
	/// <remarks>
	/// Exposed so that code preparing its own texels can create the resulting texture with exactly the same settings.
	/// </remarks>
	/// <param name="dimensions">The width and height the map will have, in texels.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	static TextureCreationConfig GetAnisotropyMapCreationConfig(XYPair<int> dimensions, ReadOnlySpan<char> name = default) {
		return TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, name) with {
			GenerateMipMaps = dimensions.Area != 1
		};
	}
	/// <summary>
	/// Writes the texels of an anisotropy map in to the given buffer.
	/// </summary>
	/// <remarks>
	/// This is the same texel generation <c>CreateAnisotropyMap</c> performs, exposed separately so that texels can be
	/// produced without touching the builder — on a worker thread, for example.
	/// </remarks>
	/// <param name="radialAnglePattern">The pattern supplying each texel's angle.</param>
	/// <param name="strengthPattern">The pattern supplying each texel's strength.</param>
	/// <param name="destinationBuffer">The buffer to write in to. Must be at least as long as the combined area of the patterns, which takes the largest width and height of either.</param>
	static void PrintAnisotropyMap(in TexturePattern<Angle> radialAnglePattern, in TexturePattern<Real> strengthPattern, Span<TexelRgb24> destinationBuffer) {
		_ = PrintPattern(radialAnglePattern, strengthPattern, &CreateAnisotropyTexel, destinationBuffer);
	}

	/// <summary>
	/// Creates an anisotropy map from the given patterns.
	/// </summary>
	/// <remarks>
	/// An anisotropy map describes surfaces that reflect light unevenly in different directions — brushed metal, for
	/// instance, whose highlights stretch along the direction of the brushing rather than forming a round spot.
	/// </remarks>
	/// <param name="radialAnglePattern">The pattern supplying each texel's angle.</param>
	/// <param name="strengthPattern">The pattern supplying each texel's strength.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateAnisotropyMap(in TexturePattern<Angle> radialAnglePattern, in TexturePattern<Real> strengthPattern, ReadOnlySpan<char> name = default) {
		return CreateAnisotropyMap(
			radialAnglePattern,
			strengthPattern,
			GetAnisotropyMapCreationConfig(GetCompositePatternDimensions(radialAnglePattern, strengthPattern), name)
		);
	}
	/// <summary>
	/// Creates an anisotropy map from the given patterns, using the given config.
	/// </summary>
	/// <param name="radialAnglePattern">The pattern supplying each texel's angle.</param>
	/// <param name="strengthPattern">The pattern supplying each texel's strength.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture CreateAnisotropyMap(in TexturePattern<Angle> radialAnglePattern, in TexturePattern<Real> strengthPattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(radialAnglePattern, strengthPattern);
		var buffer = PreallocateBuffer<TexelRgb24>(dimensions.Area);
		PrintAnisotropyMap(radialAnglePattern, strengthPattern, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}

	/// <summary>
	/// Creates a single-texel anisotropy map of one uniform angle and strength.
	/// </summary>
	/// <param name="radialAngle">The angle, or <see langword="null"/> for <c>0°</c>.</param>
	/// <param name="strength">The strength, or <see langword="null"/> for <c>1f</c>.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateAnisotropyMap(Angle? radialAngle = null, Real? strength = null, ReadOnlySpan<char> name = default) {
		return CreateAnisotropyMap(
			radialAngle ?? DefaultAnisotropyRadialAngle,
			strength ?? DefaultAnisotropyStrength,
			GetAnisotropyMapCreationConfig(XYPair<int>.One, name)
		);
	}
	/// <summary>
	/// Creates a single-texel anisotropy map of one uniform angle and strength, using the given config.
	/// </summary>
	/// <param name="radialAngle">The angle.</param>
	/// <param name="strength">The strength.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture CreateAnisotropyMap(Angle radialAngle, Real strength, in TextureCreationConfig config) {
		return CreateTexture(CreateAnisotropyTexel(radialAngle, strength), in config);
	}
	#endregion

	#region ClearCoat Map Patterns
	/// <summary>
	/// The clearcoat thickness used when none is specified: <c>1f</c>, i.e. a maximally thick coat.
	/// </summary>
	static readonly Real DefaultClearCoatThickness = 1f;
	/// <summary>
	/// The clearcoat roughness used when none is specified: <c>0f</c>, i.e. a completely glossy coat.
	/// </summary>
	static readonly Real DefaultClearCoatRoughness = 0f;
	/// <summary>
	/// Combines a thickness and a roughness in to the texel form a clearcoat map stores.
	/// </summary>
	/// <param name="thickness">How thick the coat is, where <c>0f</c> is no coat at all and <c>1f</c> is a full coat.</param>
	/// <param name="roughness">How rough the coat is, where <c>0f</c> is completely glossy and <c>1f</c> completely matte.</param>
	static TexelRgb24 CreateClearCoatTexel(Real thickness, Real roughness) => TexelRgb24.FromNormalizedFloats(thickness, roughness, Real.Zero);

	/// <summary>
	/// Returns the texture creation config that <c>CreateClearCoatMap</c> uses for a map of the given dimensions.
	/// </summary>
	/// <remarks>
	/// Exposed so that code preparing its own texels can create the resulting texture with exactly the same settings.
	/// </remarks>
	/// <param name="dimensions">The width and height the map will have, in texels.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	static TextureCreationConfig GetClearCoatMapCreationConfig(XYPair<int> dimensions, ReadOnlySpan<char> name = default) {
		return TextureCreationConfig.ForDataTexture(TextureDataType.LinearDataTwoChannelMax, name) with {
			GenerateMipMaps = dimensions.Area != 1
		};
	}
	/// <summary>
	/// Writes the texels of a clearcoat map in to the given buffer.
	/// </summary>
	/// <remarks>
	/// This is the same texel generation <c>CreateClearCoatMap</c> performs, exposed separately so that texels can be
	/// produced without touching the builder — on a worker thread, for example.
	/// </remarks>
	/// <param name="thicknessPattern">The pattern supplying each texel's thickness.</param>
	/// <param name="roughnessPattern">The pattern supplying each texel's roughness.</param>
	/// <param name="destinationBuffer">The buffer to write in to. Must be at least as long as the combined area of the patterns, which takes the largest width and height of either.</param>
	static void PrintClearCoatMap(in TexturePattern<Real> thicknessPattern, in TexturePattern<Real> roughnessPattern, Span<TexelRgb24> destinationBuffer) {
		_ = PrintPattern(thicknessPattern, roughnessPattern, &CreateClearCoatTexel, destinationBuffer);
	}

	/// <summary>
	/// Creates a clearcoat map from the given patterns.
	/// </summary>
	/// <remarks>
	/// A clearcoat map describes a thin glossy layer over the top of a surface, like a coat of lacquer or wax, which
	/// reflects light in its own right on top of whatever the surface beneath it does.
	/// </remarks>
	/// <param name="thicknessPattern">The pattern supplying each texel's thickness.</param>
	/// <param name="roughnessPattern">The pattern supplying each texel's roughness.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateClearCoatMap(in TexturePattern<Real> thicknessPattern, in TexturePattern<Real> roughnessPattern, ReadOnlySpan<char> name = default) {
		return CreateClearCoatMap(
			thicknessPattern,
			roughnessPattern,
			GetClearCoatMapCreationConfig(GetCompositePatternDimensions(thicknessPattern, roughnessPattern), name)
		);
	}
	/// <summary>
	/// Creates a clearcoat map from the given patterns, using the given config.
	/// </summary>
	/// <param name="thicknessPattern">The pattern supplying each texel's thickness.</param>
	/// <param name="roughnessPattern">The pattern supplying each texel's roughness.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture CreateClearCoatMap(in TexturePattern<Real> thicknessPattern, in TexturePattern<Real> roughnessPattern, in TextureCreationConfig config) {
		var dimensions = GetCompositePatternDimensions(thicknessPattern, roughnessPattern);
		var buffer = PreallocateBuffer<TexelRgb24>(dimensions.Area);
		PrintClearCoatMap(thicknessPattern, roughnessPattern, buffer.Span);
		return CreateTextureAndDisposePreallocatedBuffer(buffer, new TextureGenerationConfig { Dimensions = dimensions }, in config);
	}

	/// <summary>
	/// Creates a single-texel clearcoat map of one uniform thickness and roughness.
	/// </summary>
	/// <param name="thickness">The thickness, or <see langword="null"/> for <c>1f</c>.</param>
	/// <param name="roughness">The roughness, or <see langword="null"/> for <c>0f</c>.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	Texture CreateClearCoatMap(Real? thickness = null, Real? roughness = null, ReadOnlySpan<char> name = default) {
		return CreateClearCoatMap(
			thickness ?? DefaultClearCoatThickness,
			roughness ?? DefaultClearCoatRoughness,
			GetClearCoatMapCreationConfig(XYPair<int>.One, name)
		);
	}
	/// <summary>
	/// Creates a single-texel clearcoat map of one uniform thickness and roughness, using the given config.
	/// </summary>
	/// <param name="thickness">The thickness.</param>
	/// <param name="roughness">The roughness.</param>
	/// <param name="config">Controls how the texture is created on the GPU.</param>
	Texture CreateClearCoatMap(Real thickness, Real roughness, in TextureCreationConfig config) {
		return CreateTexture(CreateClearCoatTexel(thickness, roughness), in config);
	}
	#endregion
}
