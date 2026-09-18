// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.World;
using System;
using Egodystonic.TinyFFR.Assets.Baking;
using Egodystonic.TinyFFR.Rendering;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// Controls how a texture is sampled by the GPU once it has been created, i.e. how its texels are turned in to the pixels that
/// actually appear on a surface.
/// </summary>
/// <remarks>
/// The defaults suit almost every texture; these settings exist for the cases where a texture must be reproduced exactly
/// (a pixel-art sprite, say) or where sampling quality is being traded away for performance.
/// </remarks>
public readonly record struct TextureRenderingConfig {
	/// <summary>
	/// Whether to stop the texture tiling when a surface's texture coordinates go outside the range <c>0 &lt;= n &lt;= 1</c>.
	/// Defaults to <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// By default a texture repeats indefinitely in every direction, which is what makes a small tile of brickwork cover a large
	/// wall. Setting this to <see langword="true"/> instead stretches the texture's edge texels outwards to fill whatever lies
	/// beyond it.
	/// </para>
	/// <para>
	/// In some cases the innaccuracy of float-based texture sampling on an object's surface can "leak" the wrapped texture
	/// data on to the edges of that surface. Disabling texture repeat can help fix that problem.
	/// </para>
	/// </remarks>
	public bool DisableTextureRepeat { get; init; }
	/// <summary>
	/// Whether to stop neighbouring texels being blended together when the texture is magnified. Defaults to
	/// <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// By default a texture viewed from close up is smoothed, so that individual texels are not visible as hard-edged squares.
	/// Set this to <see langword="true"/> for deliberately blocky artwork, or where the texel values are data rather than colour
	/// and an average of two of them would be meaningless. Doing so also forces <see cref="AnisotropyLevel"/> to its minimum.
	/// </remarks>
	public bool DisableTexelBlending {
		get;
		init {
			field = value;
			AnisotropyLevel = CalcAnisotropy(value, AnisotropicFilteringQuality);
		}
	}
	/// <summary>
	/// How much effort to spend keeping the texture sharp on surfaces viewed at a steep angle. Defaults to
	/// <see cref="Quality.Standard"/>.
	/// </summary>
	/// <remarks>
	/// A texture on a surface receding away from the camera — a road stretching to the horizon, for example — otherwise turns to
	/// a blur in the distance. Raising this keeps such surfaces legible much further away, at some cost in performance; it has
	/// no effect at all when <see cref="DisableTexelBlending"/> is <see langword="true"/>.
	/// </remarks>
	public Quality AnisotropicFilteringQuality {
		get;
		init {
			field = value;
			AnisotropyLevel = CalcAnisotropy(DisableTexelBlending, value);
		}
	}
	internal float AnisotropyLevel { get; init; } = CalcAnisotropy(false, Quality.Standard);

	/// <summary>
	/// Constructs a new <see cref="TextureRenderingConfig"/> with default values for every property.
	/// </summary>
	public TextureRenderingConfig() { }

	/// <summary>
	/// Constructs a new <see cref="TextureRenderingConfig"/> with the given values.
	/// </summary>
	/// <param name="disableTextureRepeat">The value for <see cref="DisableTextureRepeat"/>.</param>
	/// <param name="disableTexelBlending">The value for <see cref="DisableTexelBlending"/>.</param>
	/// <param name="anisotropicFilteringQuality">The value for <see cref="AnisotropicFilteringQuality"/>. Ignored when
	/// <paramref name="disableTexelBlending"/> is <see langword="true"/>.</param>
	public TextureRenderingConfig(bool disableTextureRepeat, bool disableTexelBlending, Quality anisotropicFilteringQuality) {
		DisableTextureRepeat = disableTextureRepeat;
		DisableTexelBlending = disableTexelBlending;
		AnisotropicFilteringQuality = anisotropicFilteringQuality;
	}

	static float CalcAnisotropy(bool disableBlending, Quality filteringQuality) {
		if (disableBlending || !Enum.IsDefined(filteringQuality)) return 1f;
		else return 1 << (((int) filteringQuality) - ((int) Quality.VeryLow));
	}
}

/// <summary>
/// Controls how a texture file's contents are read from disc.
/// </summary>
public readonly ref struct TextureReadConfig : IConfigStruct<TextureReadConfig> {
	/// <summary>
	/// Whether to keep the file's alpha channel, where it has one. Defaults to <see langword="true"/>.
	/// </summary>
	/// <remarks>
	/// Setting this to <see langword="false"/> discards alpha data even when the file carries it, which saves a quarter of the
	/// texture's memory for images whose transparency you do not intend to use.
	/// </remarks>
	public bool IncludeWAlphaChannel { get; init; } = true;
	/// <summary>
	/// Whether to give the texture an alpha channel even when the file has none. Defaults to <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// The synthesised channel is fully opaque. This is useful when a texture must have a four-channel layout to be usable in
	/// some later step regardless of what the source file contained. It is not permitted to set this to <see langword="true"/>
	/// while <see cref="IncludeWAlphaChannel"/> is <see langword="false"/>.
	/// </remarks>
	public bool ForceWAlphaChannelPresence { get; init; } = false;

	/// <summary>
	/// Constructs a new <see cref="TextureReadConfig"/> with default values for every property.
	/// </summary>
	public TextureReadConfig() { }

	internal void ThrowIfInvalid() {
		if (ForceWAlphaChannelPresence && !IncludeWAlphaChannel) {
			throw new InvalidOperationException(
				$"It is not permitted for {nameof(ForceWAlphaChannelPresence)} to be true while {nameof(IncludeWAlphaChannel)} is false."
			);
		}
	}

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in TextureReadConfig src) {
		return	SerializationSizeOfBool() // IncludeWAlphaChannel
			+	SerializationSizeOfBool(); // ForceWAlphaChannelPresence
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in TextureReadConfig src) {
		SerializationWriteBool(ref dest, src.IncludeWAlphaChannel);
		SerializationWriteBool(ref dest, src.ForceWAlphaChannelPresence);
	}
	/// <inheritdoc />
	public static TextureReadConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new TextureReadConfig {
			IncludeWAlphaChannel = SerializationReadBool(ref src),
			ForceWAlphaChannelPresence = SerializationReadBool(ref src)
		};
	}
	/// <inheritdoc />
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}

/// <summary>
/// Describes what a texture's texels actually mean, which determines how they are interpreted, filtered and compressed.
/// </summary>
/// <remarks>
/// <para>
/// Getting this right matters because most image files store colour with a non-linear encoding that matches human vision, and
/// undoing that encoding is correct for colour but wrong for anything else. A roughness value of "half" is genuinely half;
/// a colour of "half brightness" is not stored as half.
/// </para>
/// <para>
/// This also decides how mipmaps are produced and which compressed format the texture is uploaded in. If TinyFFR selects
/// the "wrong" compression algorithm for a texture (because the wrong data type was specified) the data corruption can
/// be severe.
/// </para>
/// </remarks>
public enum TextureDataType {
	/// <summary>
	/// The texels are plain numeric data rather than colour. Use this for ORM maps and anisotropy maps.
	/// </summary>
	LinearData = 0,
	/// <summary>
	/// The texels are plain numeric data encoding unit-length direction vectors, such as a normal map.
	/// </summary>
	/// <remarks>
	/// Smaller mipmap levels are re-normalised rather than simply averaged, because the average of two unit vectors is not
	/// itself a unit vector.
	/// </remarks>
	LinearDataUnitVector = 1,
	/// <summary>
	/// The texels are plain numeric data using only the red and green channels, such as a clearcoat map.
	/// </summary>
	LinearDataTwoChannelMax = 2,
	/// <summary>
	/// The texels are colour, stored with the usual non-linear encoding of image files. Use this for colour maps, emissive maps
	/// and absorption-transmission maps.
	/// </summary>
	ColorSrgb = 3,
}

/// <summary>
/// Extension methods for <see cref="TextureDataType"/>.
/// </summary>
public static class TextureDataTypeExtensions {
	/// <summary>
	/// Returns whether the given data type's texels are plain numeric values rather than colour, and are therefore used exactly
	/// as stored.
	/// </summary>
	/// <param name="this">The data type to test.</param>
	public static bool UsesLinearColorspace(this TextureDataType @this) => @this != TextureDataType.ColorSrgb;
}

/// <summary>
/// Controls how a texture is created on the GPU: how its data is interpreted, whether it is compressed or mipmapped, and how it
/// is sampled once in place.
/// </summary>
/// <remarks>
/// The <c>For...</c> static factory methods produce a config already set up correctly for a particular kind of texture, and are
/// almost always a better starting point than setting every property by hand.
/// </remarks>
public readonly ref struct TextureCreationConfig : IConfigStruct<TextureCreationConfig> {
	/// <summary>
	/// The default value for <see cref="CompressionQuality"/>: <see langword="null"/>, meaning no compression.
	/// </summary>
	public static readonly Quality? DefaultCompressionQuality = null;

	/// <summary>
	/// Creates a config for a texture whose texels are colour, using <see cref="DefaultCompressionQuality"/>.
	/// </summary>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	public static TextureCreationConfig ForColorTexture(ReadOnlySpan<char> name = default) => ForColorTexture(DefaultCompressionQuality, name);
	/// <summary>
	/// Creates a config for a texture whose texels are colour.
	/// </summary>
	/// <param name="compressionQuality">How aggressively to compress the texture, or <see langword="null"/> not to compress it
	/// at all.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	public static TextureCreationConfig ForColorTexture(Quality? compressionQuality, ReadOnlySpan<char> name = default) => new() {
		GenerateMipMaps = true,
		DataType = TextureDataType.ColorSrgb,
		RenderingConfig = new(),
		ProcessingToApply = TextureProcessingConfig.None,
		CompressionQuality = compressionQuality,
		Name = name
	};
	/// <summary>
	/// Creates a config for a texture that will be drawn flat on a canvas rather than mapped on to a surface in the world.
	/// </summary>
	/// <remarks>
	/// Canvas textures are drawn without the postprocessing pipeline a scene's objects go through, so they are treated as plain
	/// data, never tiled, and sampled without anisotropic filtering — a canvas element is always viewed face-on, so there is
	/// nothing for that filtering to improve.
	/// </remarks>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	public static TextureCreationConfig ForCanvasTexture(ReadOnlySpan<char> name = default) => new() {
		GenerateMipMaps = true,
		DataType = TextureDataType.LinearData, // Because canvas scenes have postprocessing disabled which includes the srgb/linear conversion pipeline
		RenderingConfig = new() {
			AnisotropicFilteringQuality	= Quality.VeryLow,
			AnisotropyLevel = 0f,
			DisableTextureRepeat = true
		},
		ProcessingToApply = TextureProcessingConfig.None,
		CompressionQuality = null,
		Name = name
	};
	/// <summary>
	/// Creates a config for a texture whose texels are plain numeric data rather than colour, using
	/// <see cref="DefaultCompressionQuality"/>.
	/// </summary>
	/// <param name="dataType">How the texture's data should be interpreted. Must not be
	/// <see cref="TextureDataType.ColorSrgb"/>, which is what <see cref="ForColorTexture(ReadOnlySpan{char})"/> is for.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	public static TextureCreationConfig ForDataTexture(TextureDataType dataType, ReadOnlySpan<char> name = default) => ForDataTexture(dataType, DefaultCompressionQuality, name);
	/// <summary>
	/// Creates a config for a texture whose texels are plain numeric data rather than colour.
	/// </summary>
	/// <param name="dataType">How the texture's data should be interpreted. Must not be
	/// <see cref="TextureDataType.ColorSrgb"/>, which is what <see cref="ForColorTexture(ReadOnlySpan{char})"/> is for.</param>
	/// <param name="compressionQuality">How aggressively to compress the texture, or <see langword="null"/> not to compress it
	/// at all.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	public static TextureCreationConfig ForDataTexture(TextureDataType dataType, Quality? compressionQuality, ReadOnlySpan<char> name = default) => new() {
		GenerateMipMaps = true,
		DataType = dataType,
		RenderingConfig = new(),
		ProcessingToApply = TextureProcessingConfig.None,
		CompressionQuality = compressionQuality,
		Name = name
	};

	/// <summary>
	/// Creates a config for a colour map, which supplies the base colour of a surface.
	/// </summary>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextureCreationConfig ForColorMap(ReadOnlySpan<char> name = default) => ForColorTexture(name);
	/// <summary>
	/// Creates a config for a normal map, which describes the small-scale bumps and grooves of a surface.
	/// </summary>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextureCreationConfig ForNormalMap(ReadOnlySpan<char> name = default) => ForDataTexture(TextureDataType.LinearDataUnitVector, name);
	/// <summary>
	/// Creates a config for an occlusion/roughness/metallic map.
	/// </summary>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextureCreationConfig ForOrmMap(ReadOnlySpan<char> name = default) => ForDataTexture(TextureDataType.LinearData, name);
	/// <summary>
	/// Creates a config for an occlusion/roughness/metallic/reflectance map.
	/// </summary>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextureCreationConfig ForOrmrMap(ReadOnlySpan<char> name = default) => ForDataTexture(TextureDataType.LinearData, name);
	/// <summary>
	/// Creates a config for an absorption-transmission map, which describes how light passes through a see-through surface.
	/// </summary>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextureCreationConfig ForAbsorptionTransmissionMap(ReadOnlySpan<char> name = default) => ForColorTexture(name);
	/// <summary>
	/// Creates a config for an emissive map, which makes parts of a surface appear to glow with their own light.
	/// </summary>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextureCreationConfig ForEmissiveMap(ReadOnlySpan<char> name = default) => ForColorTexture(name);
	/// <summary>
	/// Creates a config for an anisotropy map, which describes surfaces that reflect light unevenly in different directions.
	/// </summary>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextureCreationConfig ForAnisotropyMap(ReadOnlySpan<char> name = default) => ForDataTexture(TextureDataType.LinearData, name);
	/// <summary>
	/// Creates a config for a clearcoat map, which describes a thin glossy layer over the top of a surface.
	/// </summary>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextureCreationConfig ForClearCoatMap(ReadOnlySpan<char> name = default) => ForDataTexture(TextureDataType.LinearDataTwoChannelMax, name);

	/// <summary>
	/// Creates a config for a colour map, which supplies the base colour of a surface.
	/// </summary>
	/// <param name="compressionQuality">How aggressively to compress the texture, or <see langword="null"/> not to compress it
	/// at all.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextureCreationConfig ForColorMap(Quality? compressionQuality, ReadOnlySpan<char> name = default) => ForColorTexture(compressionQuality, name);
	/// <summary>
	/// Creates a config for a normal map, which describes the small-scale bumps and grooves of a surface.
	/// </summary>
	/// <param name="compressionQuality">How aggressively to compress the texture, or <see langword="null"/> not to compress it
	/// at all.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextureCreationConfig ForNormalMap(Quality? compressionQuality, ReadOnlySpan<char> name = default) => ForDataTexture(TextureDataType.LinearDataUnitVector, compressionQuality, name);
	/// <summary>
	/// Creates a config for an occlusion/roughness/metallic map.
	/// </summary>
	/// <param name="compressionQuality">How aggressively to compress the texture, or <see langword="null"/> not to compress it
	/// at all.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextureCreationConfig ForOrmMap(Quality? compressionQuality, ReadOnlySpan<char> name = default) => ForDataTexture(TextureDataType.LinearData, compressionQuality, name);
	/// <summary>
	/// Creates a config for an occlusion/roughness/metallic/reflectance map.
	/// </summary>
	/// <param name="compressionQuality">How aggressively to compress the texture, or <see langword="null"/> not to compress it
	/// at all.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextureCreationConfig ForOrmrMap(Quality? compressionQuality, ReadOnlySpan<char> name = default) => ForDataTexture(TextureDataType.LinearData, compressionQuality, name);
	/// <summary>
	/// Creates a config for an absorption-transmission map, which describes how light passes through a see-through surface.
	/// </summary>
	/// <param name="compressionQuality">How aggressively to compress the texture, or <see langword="null"/> not to compress it
	/// at all.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextureCreationConfig ForAbsorptionTransmissionMap(Quality? compressionQuality, ReadOnlySpan<char> name = default) => ForColorTexture(compressionQuality, name);
	/// <summary>
	/// Creates a config for an emissive map, which makes parts of a surface appear to glow with their own light.
	/// </summary>
	/// <param name="compressionQuality">How aggressively to compress the texture, or <see langword="null"/> not to compress it
	/// at all.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextureCreationConfig ForEmissiveMap(Quality? compressionQuality, ReadOnlySpan<char> name = default) => ForColorTexture(compressionQuality, name);
	/// <summary>
	/// Creates a config for an anisotropy map, which describes surfaces that reflect light unevenly in different directions.
	/// </summary>
	/// <param name="compressionQuality">How aggressively to compress the texture, or <see langword="null"/> not to compress it
	/// at all.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextureCreationConfig ForAnisotropyMap(Quality? compressionQuality, ReadOnlySpan<char> name = default) => ForDataTexture(TextureDataType.LinearData, compressionQuality, name);
	/// <summary>
	/// Creates a config for a clearcoat map, which describes a thin glossy layer over the top of a surface.
	/// </summary>
	/// <param name="compressionQuality">How aggressively to compress the texture, or <see langword="null"/> not to compress it
	/// at all.</param>
	/// <param name="name">The name to give the texture. May be left empty.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextureCreationConfig ForClearCoatMap(Quality? compressionQuality, ReadOnlySpan<char> name = default) => ForDataTexture(TextureDataType.LinearDataTwoChannelMax, compressionQuality, name);

	/// <summary>
	/// Whether to generate smaller copies of the texture for use when the surface is far from the camera. Defaults to
	/// <see langword="true"/>.
	/// </summary>
	/// <remarks>
	/// These smaller copies ("mipmaps") make distant surfaces look steadier instead of shimmering as the camera moves, and are
	/// faster for the GPU to read. They cost about a third more video memory. Turn them off only where a texture must stay
	/// pin-sharp at every distance, or where video memory is genuinely scarce.
	/// </remarks>
	public bool GenerateMipMaps { get; init; } = true;
	/// <summary>
	/// How aggressively to compress the texture in video memory, or <see langword="null"/> not to compress it at all. Defaults
	/// to <see cref="DefaultCompressionQuality"/>: <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Compression trades a little image quality for a large saving in video memory and faster sampling. The exact format used
	/// is chosen for you from this value and <see cref="DataType"/>; see <see cref="TextureCompressor"/> if you need to know or
	/// control which.
	/// </para>
	/// <para>
	/// Note that texture compression can take a very long time depending on the selected quality, and it's expected that you'll
	/// use higher quality levels only when creating textures for baking with the <see cref="IAssetBakery"/>.
	/// </para>
	/// </remarks>
	public Quality? CompressionQuality { get; init; } = DefaultCompressionQuality;
	/// <summary>
	/// What the texture's texels actually mean. This property is required.
	/// </summary>
	public required TextureDataType DataType { get; init; }
	/// <summary>
	/// Whether the texture's contents may be overwritten after it has been created. Defaults to <see langword="false"/>.
	/// Setting this to <c>true</c> enables the <see cref="Texture.OverwriteTexels{TTexel}(ReadOnlySpan{TTexel})"/> API.
	/// </summary>
	/// <remarks>
	/// Setting this to <see langword="true"/> forces <see cref="GenerateMipMaps"/> to <see langword="false"/> and
	/// <see cref="CompressionQuality"/> to <see langword="null"/>, because neither can be maintained for data that changes.
	/// Setting either of those explicitly afterwards, alongside this, is rejected as a contradiction.
	/// </remarks>
	public bool AllowsDynamicWrites {
		get;
		init {
			if (value) {
				GenerateMipMaps = false;
				CompressionQuality = null;
			}
			field = value;
		}
	} = false;
	/// <summary>
	/// How the texture should be sampled once created. Defaults to a <see cref="TextureRenderingConfig"/> with default values.
	/// </summary>
	public TextureRenderingConfig RenderingConfig { get; init; } = new();
	/// <summary>
	/// The name to give the texture. May be left empty.
	/// </summary>
	public ReadOnlySpan<char> Name { get; init; }
	/// <summary>
	/// How the texture's data should be altered before it is uploaded. Defaults to <see cref="TextureProcessingConfig.None"/>,
	/// i.e. no alteration at all.
	/// </summary>
	public TextureProcessingConfig ProcessingToApply { get; init; } = TextureProcessingConfig.None;

	/// <summary>
	/// Constructs a new <see cref="TextureCreationConfig"/> with default values for every property except
	/// <see cref="DataType"/>, which must be supplied.
	/// </summary>
	public TextureCreationConfig() { }

	internal void ThrowIfInvalid() {
		if (AllowsDynamicWrites && GenerateMipMaps) {
			throw new InvalidOperationException(
				$"It is not permitted for both {nameof(GenerateMipMaps)} and {nameof(AllowsDynamicWrites)} to be true simultaneously."
			);
		}
		if (AllowsDynamicWrites && CompressionQuality != null) {
			throw new InvalidOperationException(
				$"It is not permitted for {nameof(CompressionQuality)} to be non-null and {nameof(AllowsDynamicWrites)} to be true simultaneously."
			);
		}
	}

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in TextureCreationConfig src) {
		return	SerializationSizeOfBool() // GenerateMipMaps
			+	SerializationSizeOfBool() // AllowsDynamicWrites
			+	SerializationSizeOfBool() // CompressionQuality.HasValue
			+	SerializationSizeOfInt() // CompressionQuality
			+	SerializationSizeOfInt() // DataType
			+	SerializationSizeOfBool() // SamplingConfig.DisableTextureRepeat
			+	SerializationSizeOfBool() // SamplingConfig.DisableTexelBlending
			+	SerializationSizeOfInt() // SamplingConfig.AnisotropicFilteringQuality
			+	SerializationSizeOfFloat() // SamplingConfig.AnisotropyLevel
			+	SerializationSizeOfString(src.Name) // Name
			+	SerializationSizeOfSubConfig(src.ProcessingToApply);
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in TextureCreationConfig src) {
		SerializationWriteBool(ref dest, src.GenerateMipMaps);
		SerializationWriteBool(ref dest, src.AllowsDynamicWrites);
		SerializationWriteBool(ref dest, src.CompressionQuality != null);
		SerializationWriteInt(ref dest, (int) (src.CompressionQuality ?? default));
		SerializationWriteInt(ref dest, (int) src.DataType);
		SerializationWriteBool(ref dest, src.RenderingConfig.DisableTextureRepeat);
		SerializationWriteBool(ref dest, src.RenderingConfig.DisableTexelBlending);
		SerializationWriteInt(ref dest, (int) src.RenderingConfig.AnisotropicFilteringQuality);
		SerializationWriteFloat(ref dest, src.RenderingConfig.AnisotropyLevel);
		SerializationWriteString(ref dest, src.Name);
		SerializationWriteSubConfig(ref dest, src.ProcessingToApply);
	}
	/// <inheritdoc />
	public static TextureCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var generateMipMaps = SerializationReadBool(ref src);
		var allowsDynamicWrites = SerializationReadBool(ref src);
		var hasCompressionQuality = SerializationReadBool(ref src);
		var compressionQualityValue = (Quality) SerializationReadInt(ref src);
		var dataType = (TextureDataType) SerializationReadInt(ref src);
		var disableTextureRepeat = SerializationReadBool(ref src);
		var disableTexelBlending = SerializationReadBool(ref src);
		var anisotropicFilteringQuality = (Quality) SerializationReadInt(ref src);
		var anisotropyLevel = SerializationReadFloat(ref src);

		return new TextureCreationConfig {
			GenerateMipMaps = generateMipMaps,
			AllowsDynamicWrites = allowsDynamicWrites,
			CompressionQuality = hasCompressionQuality ? compressionQualityValue : null,
			DataType = dataType,
			RenderingConfig = new TextureRenderingConfig(disableTextureRepeat, disableTexelBlending, anisotropicFilteringQuality) {
				AnisotropyLevel = anisotropyLevel
			},
			Name = SerializationReadString(ref src),
			ProcessingToApply = SerializationReadSubConfig<TextureProcessingConfig>(ref src),
		};
	}
	/// <inheritdoc />
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var converted = ConvertFromAllocatedHeapStorage(src);
		var processingConfigSlice = src[(GetHeapStorageFormattedLength(in converted) - SerializationSizeOfSubConfig(converted.ProcessingToApply))..];
		SerializationDisposeSubConfig<TextureProcessingConfig>(ref processingConfigSlice);
	}
}

/// <summary>
/// A function that alters a texture's texels, supplied to <see cref="TextureProcessingConfig.PostProcessingFunction"/> to make
/// an arbitrary change to a texture as it is loaded. Create via <see cref="Create"/>.
/// </summary>
/// <remarks>
/// <para>
/// This exists for transformations the built-in processing options can not express. The function is invoked on a worker thread
/// as part of loading, which avoids a separate pass over the texture afterwards.
/// </para>
/// <para>
/// A function is bound to the exact texel type it was created for. Applying it to a texture of a different texel type is not an
/// error: The texels are converted to the expected type, the function runs, and the result is converted back. That conversion
/// is lossy in the usual way, so a function written for a four-channel texel applied to a three-channel texture cannot preserve
/// what the third type never stored. Note also that the conversion is not free in terms of performance. For these two reasons
/// it's better to aim for a matching texel type where possible.
/// </para>
/// </remarks>
public readonly struct TexelProcessingFunction : IEquatable<TexelProcessingFunction> {
	internal nint FunctionPtr { get; private init; }
	internal nint TexelTypeHandle { get; private init; }

	/// <summary>
	/// Creates a <see cref="TexelProcessingFunction"/> from a function pointer.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The function receives the texture's texels as a span it may write to in place, together with whatever was set as
	/// <see cref="TextureProcessingConfig.PostProcessingArgument"/>.
	/// </para>
	/// <para>
	/// A function is bound to the exact texel type it was created for. Applying it to a texture of a different texel type is not an
	/// error: The texels are converted to the expected type, the function runs, and the result is converted back. That conversion
	/// is lossy in the usual way, so a function written for a four-channel texel applied to a three-channel texture cannot preserve
	/// what the third type never stored. Note also that the conversion is not free in terms of performance. For these two reasons
	/// it's better to aim for a matching texel type where possible.
	/// </para>
	/// </remarks>
	/// <typeparam name="TTexel">The texel type the function operates on. The resulting
	/// <see cref="TexelProcessingFunction"/> is bound to this exact type.</typeparam>
	/// <param name="function">The function to invoke. Must not be <see langword="null"/>.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="function"/> is <see langword="null"/>.</exception>
	public static unsafe TexelProcessingFunction Create<TTexel>(delegate*<Span<TTexel>, object?, void> function) where TTexel : unmanaged, ITexel<TTexel> {
		if (function == null) throw new ArgumentNullException(nameof(function));
		return new TexelProcessingFunction {
			FunctionPtr = (nint) function,
			TexelTypeHandle = typeof(TTexel).TypeHandle.Value
		};
	}

	/// <summary>
	/// Returns whether this function was created for the given texel type exactly.
	/// </summary>
	/// <remarks>
	/// A function can still be applied to textures of other texel types, via conversion; this reports the type it was written
	/// for and will therefore receive without any conversion taking place.
	/// </remarks>
	/// <typeparam name="TTexel">The texel type to test against.</typeparam>
	public bool ExpectsTexelType<TTexel>() => TexelTypeHandle == typeof(TTexel).TypeHandle.Value;

	internal static TexelProcessingFunction FromSerializedValues(nint functionPtr, nint texelTypeHandle) {
		return new TexelProcessingFunction {
			FunctionPtr = functionPtr,
			TexelTypeHandle = texelTypeHandle
		};
	}

	internal unsafe void Invoke<TTexel>(Span<TTexel> texels, object? argument) where TTexel : unmanaged, ITexel<TTexel> {
		if (!ExpectsTexelType<TTexel>()) {
			throw new InvalidOperationException(
				$"This {nameof(TexelProcessingFunction)} was created for a different texel type than {typeof(TTexel).Name}. " +
				$"A function created via {nameof(TexelProcessingFunction)}.{nameof(Create)}<T>() may only be applied to textures whose texel type is exactly T."
			);
		}
		((delegate*<Span<TTexel>, object?, void>) FunctionPtr)(texels, argument);
	}

	/// <inheritdoc />
	public bool Equals(TexelProcessingFunction other) => FunctionPtr == other.FunctionPtr && TexelTypeHandle == other.TexelTypeHandle;
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is TexelProcessingFunction other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(FunctionPtr, TexelTypeHandle);
	/// <inheritdoc cref="Equals(TexelProcessingFunction)" />
	public static bool operator ==(TexelProcessingFunction left, TexelProcessingFunction right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given functions are not the same function bound to the same texel type.
	/// </summary>
	/// <param name="left">The first function to compare.</param>
	/// <param name="right">The second function to compare.</param>
	public static bool operator !=(TexelProcessingFunction left, TexelProcessingFunction right) => !left.Equals(right);
}

/// <summary>
/// Describes alterations to be made to a texture's data before it is uploaded to the GPU or printed to file, etc.
/// </summary>
/// <remarks>
/// <para>
/// This exists mostly to reconcile texture files authored under a different convention from the one TinyFFR expects — a map
/// whose green channel is inverted, or whose rows run the other way up — without having to edit the file itself.
/// </para>
/// <para>
/// The steps are applied in a fixed order: flips first, then channel inversions, then alpha premultiplication, then channel
/// copies, and finally <see cref="PostProcessingFunction"/> if one is set.
/// </para>
/// </remarks>
public readonly record struct TextureProcessingConfig : IConfigStruct<TextureProcessingConfig> {
	/// <summary>
	/// A config that applies no processing at all.
	/// </summary>
	public static readonly TextureProcessingConfig None = new();

	/// <summary>
	/// Whether to mirror the texture left-to-right, i.e. around its vertical centre line. Defaults to <see langword="false"/>.
	/// </summary>
	public bool FlipX {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value;
		}
	} = false;
	/// <summary>
	/// Whether to mirror the texture top-to-bottom, i.e. around its horizontal centre line. Defaults to
	/// <see langword="false"/>.
	/// </summary>
	public bool FlipY {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value;
		}
	} = false;
	/// <summary>
	/// Whether to invert the red channel, so that its strongest values become its weakest and vice versa. Defaults to
	/// <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// This is chiefly useful for maps whose channels carry data rather than colour and were authored with the opposite meaning
	/// — turning a "glossiness" map, where <c>1</c> means smooth, in to the roughness map TinyFFR expects, where <c>1</c> means
	/// rough.
	/// </remarks>
	public bool InvertXRedChannel {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value;
		}
	} = false;
	/// <summary>
	/// Whether to invert the green channel, so that its strongest values become its weakest and vice versa. Defaults to
	/// <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// Inverting green alone is how a normal map authored in one convention is converted to the other.
	/// </remarks>
	public bool InvertYGreenChannel {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value;
		}
	} = false;
	/// <summary>
	/// Whether to invert the blue channel, so that its strongest values become its weakest and vice versa. Defaults to
	/// <see langword="false"/>.
	/// </summary>
	public bool InvertZBlueChannel {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value;
		}
	} = false;
	/// <summary>
	/// Whether to invert the alpha channel, so that fully opaque texels become fully transparent and vice versa. Defaults to
	/// <see langword="false"/>.
	/// </summary>
	public bool InvertWAlphaChannel {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value;
		}
	} = false;
	/// <summary>
	/// Whether to multiply each texel's colour channels by its alpha channel. Defaults to <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// Materials that blend properly with the scene expect their colour maps in this form. Without it, blending a
	/// partially-transparent texel drags in whatever colour was stored alongside the transparency, which typically shows up as a
	/// dark fringe around the edges of the visible parts of the image.
	/// </remarks>
	public bool MultiplyAlpha {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value;
		}
	} = false;
	/// <summary>
	/// Which channel of the source supplies the final red channel. Defaults to <see cref="ColorChannel.R"/>, i.e. its own.
	/// </summary>
	/// <remarks>
	/// Setting this to <see cref="ColorChannel.G"/> means the source's <b>green</b> data is copied to the output's <b>red</b>
	/// channel. These copies all happen together as the final built-in step, reading from the values as they stood after every
	/// flip and inversion, so channels can be exchanged without one overwriting another partway through.
	/// </remarks>
	public ColorChannel XRedFinalOutputSource {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value != ColorChannel.R;
		}
	} = ColorChannel.R;
	/// <summary>
	/// Which channel of the source supplies the final green channel. Defaults to <see cref="ColorChannel.G"/>, i.e. its own.
	/// </summary>
	/// <remarks>
	/// Setting this to <see cref="ColorChannel.B"/> means the source's <b>blue</b> data is copied to the output's <b>green</b>
	/// channel. These copies all happen together as the final built-in step, reading from the values as they stood after every
	/// flip and inversion, so channels can be exchanged without one overwriting another partway through.
	/// </remarks>
	public ColorChannel YGreenFinalOutputSource {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value != ColorChannel.G;
		}
	} = ColorChannel.G;
	/// <summary>
	/// Which channel of the source supplies the final blue channel. Defaults to <see cref="ColorChannel.B"/>, i.e. its own.
	/// </summary>
	/// <remarks>
	/// Setting this to <see cref="ColorChannel.G"/> means the source's <b>green</b> data is copied to the output's <b>blue</b>
	/// channel. These copies all happen together as the final built-in step, reading from the values as they stood after every
	/// flip and inversion, so channels can be exchanged without one overwriting another partway through.
	/// </remarks>
	public ColorChannel ZBlueFinalOutputSource {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value != ColorChannel.B;
		}
	} = ColorChannel.B;
	/// <summary>
	/// Which channel of the source supplies the final alpha channel. Defaults to <see cref="ColorChannel.A"/>, i.e. its own.
	/// </summary>
	/// <remarks>
	/// Setting this to <see cref="ColorChannel.G"/> means the source's <b>green</b> data is copied to the output's <b>alpha</b>
	/// channel. These copies all happen together as the final built-in step, reading from the values as they stood after every
	/// flip and inversion, so channels can be exchanged without one overwriting another partway through.
	/// </remarks>
	public ColorChannel WAlphaFinalOutputSource {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value != ColorChannel.A;
		}
	} = ColorChannel.A;

	/// <summary>
	/// An arbitrary function to run over the texture's texels, or <see langword="null"/> for none. Defaults to
	/// <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// This runs last, after every other step here.
	/// </remarks>
	public TexelProcessingFunction? PostProcessingFunction {
		get;
		init {
			field = value;
			RequiresProcessing = RequiresProcessing || value != null;
		}
	} = null;
	/// <summary>
	/// An object passed through to <see cref="PostProcessingFunction"/> when it is invoked. Defaults to
	/// <see langword="null"/>.
	/// </summary>
	public object? PostProcessingArgument { get; init; } = null;

	internal bool RequiresProcessing { get; private init; } = false;

	/// <summary>
	/// Constructs a new <see cref="TextureProcessingConfig"/> that applies no processing.
	/// </summary>
	public TextureProcessingConfig() { }

	/// <summary>
	/// Creates a config that mirrors the texture and does nothing else.
	/// </summary>
	/// <param name="aroundVerticalCentre">Whether to mirror the texture left-to-right; the value for <see cref="FlipX"/>.</param>
	/// <param name="aroundHorizontalCentre">Whether to mirror the texture top-to-bottom; the value for <see cref="FlipY"/>.</param>
	public static TextureProcessingConfig Flip(bool aroundVerticalCentre, bool aroundHorizontalCentre) {
		return new() {
			FlipX = aroundVerticalCentre,
			FlipY = aroundHorizontalCentre
		};
	}
	/// <summary>
	/// Creates a config that inverts one or more channels and does nothing else.
	/// </summary>
	/// <param name="includeRedChannel">Whether to invert the red channel.</param>
	/// <param name="includeGreenChannel">Whether to invert the green channel.</param>
	/// <param name="includeBlueChannel">Whether to invert the blue channel.</param>
	/// <param name="includeAlphaChannel">Whether to invert the alpha channel.</param>
	public static TextureProcessingConfig Invert(bool includeRedChannel = true, bool includeGreenChannel = true, bool includeBlueChannel = true, bool includeAlphaChannel = true) {
		return new() {
			InvertXRedChannel = includeRedChannel,
			InvertYGreenChannel = includeGreenChannel,
			InvertZBlueChannel = includeBlueChannel,
			InvertWAlphaChannel = includeAlphaChannel
		};
	}
	/// <summary>
	/// Creates a config that rearranges the texture's channels and does nothing else.
	/// </summary>
	/// <remarks>
	/// Each parameter names the source channel for one output channel, so passing <see cref="ColorChannel.G"/> as
	/// <paramref name="redSource"/> copies green in to red.
	/// </remarks>
	/// <param name="redSource">Which channel supplies the output's red channel.</param>
	/// <param name="greenSource">Which channel supplies the output's green channel.</param>
	/// <param name="blueSource">Which channel supplies the output's blue channel.</param>
	/// <param name="alphaSource">Which channel supplies the output's alpha channel.</param>
	public static TextureProcessingConfig Swizzle(ColorChannel redSource = ColorChannel.R, ColorChannel greenSource = ColorChannel.G, ColorChannel blueSource = ColorChannel.B, ColorChannel alphaSource = ColorChannel.A) {
		return new() {
			XRedFinalOutputSource = redSource,
			YGreenFinalOutputSource = greenSource,
			ZBlueFinalOutputSource = blueSource,
			WAlphaFinalOutputSource = alphaSource
		};
	}
	/// <summary>
	/// Creates a config that premultiplies the texture's alpha and does nothing else.
	/// </summary>
	public static TextureProcessingConfig PremultiplyAlpha() {
		return new() {
			MultiplyAlpha = true
		};
	}

	internal void ThrowIfInvalid() {
		/* no-op */
	}

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in TextureProcessingConfig src) {
		return SerializationSizeOfBool() // FlipX
			+ SerializationSizeOfBool() // FlipY
			+ SerializationSizeOfBool() // InvertXRedChannel
			+ SerializationSizeOfBool() // InvertYGreenChannel
			+ SerializationSizeOfBool() // InvertZBlueChannel
			+ SerializationSizeOfBool() // InvertWAlphaChannel
			+ SerializationSizeOfBool() // MultiplyAlpha
			+ SerializationSizeOfInt() // XRedFinalOutputSource
			+ SerializationSizeOfInt() // YGreenFinalOutputSource
			+ SerializationSizeOfInt() // ZBlueFinalOutputSource
			+ SerializationSizeOfInt() // WAlphaFinalOutputSource
			+ SerializationSizeOfLong()
			+ SerializationSizeOfLong()
			+ SerializationSizeOfLong();
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in TextureProcessingConfig src) {
		SerializationWriteBool(ref dest, src.FlipX);
		SerializationWriteBool(ref dest, src.FlipY);
		SerializationWriteBool(ref dest, src.InvertXRedChannel);
		SerializationWriteBool(ref dest, src.InvertYGreenChannel);
		SerializationWriteBool(ref dest, src.InvertZBlueChannel);
		SerializationWriteBool(ref dest, src.InvertWAlphaChannel);
		SerializationWriteBool(ref dest, src.MultiplyAlpha);
		SerializationWriteInt(ref dest, (int) src.XRedFinalOutputSource);
		SerializationWriteInt(ref dest, (int) src.YGreenFinalOutputSource);
		SerializationWriteInt(ref dest, (int) src.ZBlueFinalOutputSource);
		SerializationWriteInt(ref dest, (int) src.WAlphaFinalOutputSource);
		SerializationWriteLong(ref dest, src.PostProcessingFunction is { } function ? function.FunctionPtr : 0L);
		SerializationWriteLong(ref dest, src.PostProcessingFunction is { } fn ? fn.TexelTypeHandle : 0L);
		SerializationWriteLong(ref dest, src.PostProcessingArgument is { } argument ? GCHandle.ToIntPtr(GCHandle.Alloc(argument)) : 0L);
	}
	/// <inheritdoc />
	public static TextureProcessingConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var flipX = SerializationReadBool(ref src);
		var flipY = SerializationReadBool(ref src);
		var invertXRedChannel = SerializationReadBool(ref src);
		var invertYGreenChannel = SerializationReadBool(ref src);
		var invertZBlueChannel = SerializationReadBool(ref src);
		var invertWAlphaChannel = SerializationReadBool(ref src);
		var multiplyAlpha = SerializationReadBool(ref src);
		var xRedFinalOutputSource = (ColorChannel) SerializationReadInt(ref src);
		var yGreenFinalOutputSource = (ColorChannel) SerializationReadInt(ref src);
		var zBlueFinalOutputSource = (ColorChannel) SerializationReadInt(ref src);
		var wAlphaFinalOutputSource = (ColorChannel) SerializationReadInt(ref src);
		var functionPtr = (nint) SerializationReadLong(ref src);
		var texelTypeHandle = (nint) SerializationReadLong(ref src);
		var argumentHandle = (nint) SerializationReadLong(ref src);

		return new TextureProcessingConfig {
			FlipX = flipX,
			FlipY = flipY,
			InvertXRedChannel = invertXRedChannel,
			InvertYGreenChannel = invertYGreenChannel,
			InvertZBlueChannel = invertZBlueChannel,
			InvertWAlphaChannel = invertWAlphaChannel,
			MultiplyAlpha = multiplyAlpha,
			XRedFinalOutputSource = xRedFinalOutputSource,
			YGreenFinalOutputSource = yGreenFinalOutputSource,
			ZBlueFinalOutputSource = zBlueFinalOutputSource,
			WAlphaFinalOutputSource = wAlphaFinalOutputSource,
			PostProcessingFunction = functionPtr != 0 ? TexelProcessingFunction.FromSerializedValues(functionPtr, texelTypeHandle) : null,
			PostProcessingArgument = argumentHandle != 0 ? GCHandle.FromIntPtr(argumentHandle).Target : null
		};
	}
	/// <inheritdoc />
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var argumentSlice = src[^SerializationSizeOfLong()..];
		var argumentHandle = (nint) SerializationReadLong(ref argumentSlice);
		if (argumentHandle != 0) GCHandle.FromIntPtr(argumentHandle).Free();
	}
}

/// <summary>
/// Describes how a texture should be generated.
/// </summary>
public readonly ref struct TextureGenerationConfig : IConfigStruct<TextureGenerationConfig> {
	/// <summary>
	/// The width and height of the texture, in texels. Both must be positive. This property is required.
	/// </summary>
	/// <remarks>
	/// The texel data supplied alongside this must contain at least <c>Dimensions.X * Dimensions.Y</c> entries, laid out row by
	/// row.
	/// </remarks>
	public required XYPair<int> Dimensions { get; init; }

	/// <summary>
	/// Constructs a new <see cref="TextureGenerationConfig"/>. <see cref="Dimensions"/> must be supplied.
	/// </summary>
	public TextureGenerationConfig() { }

	internal void ThrowIfInvalid() {
		static void ThrowArgException(object erroneousArg, string message, [CallerArgumentExpression(nameof(erroneousArg))] string? argName = null) {
			throw new InvalidOperationException($"{nameof(TextureCreationConfig)}.{argName} {message} Value was {erroneousArg}.");
		}

		if (Dimensions.X < 1) {
			ThrowArgException(Dimensions.X, "must be positive.");
		}
		if (Dimensions.Y < 1) {
			ThrowArgException(Dimensions.Y, "must be positive.");
		}
	}

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in TextureGenerationConfig src) {
		return	SerializationSizeOf<XYPair<int>>(); // Dimensions
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in TextureGenerationConfig src) {
		SerializationWrite(ref dest, src.Dimensions);
	}
	/// <inheritdoc />
	public static TextureGenerationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new TextureGenerationConfig {
			Dimensions = SerializationRead<XYPair<int>>(ref src)
		};
	}
	/// <inheritdoc />
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}
readonly ref struct TextureLoadConfig : IConfigStruct<TextureLoadConfig> {
	public TextureCreationConfig CreationConfig { get; init; }
	public TextureReadConfig ReadConfig { get; init; }

	public TextureLoadConfig() { }

	internal void ThrowIfInvalid() {
		CreationConfig.ThrowIfInvalid();
		ReadConfig.ThrowIfInvalid();
	}

	public static int GetHeapStorageFormattedLength(in TextureLoadConfig src) {
		return	SerializationSizeOfSubConfig(src.CreationConfig) // CreationConfig
			+	SerializationSizeOfSubConfig(src.ReadConfig); // ReadConfig
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in TextureLoadConfig src) {
		SerializationWriteSubConfig(ref dest, src.CreationConfig);
		SerializationWriteSubConfig(ref dest, src.ReadConfig);
	}
	public static TextureLoadConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new TextureLoadConfig {
			CreationConfig = SerializationReadSubConfig<TextureCreationConfig>(ref src),
			ReadConfig = SerializationReadSubConfig<TextureReadConfig>(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SerializationDisposeSubConfig<TextureCreationConfig>(ref src);
		SerializationDisposeSubConfig<TextureReadConfig>(ref src);
	}
}

readonly ref struct TextureCombinedLoadConfig : IConfigStruct<TextureCombinedLoadConfig> {
	public TextureCreationConfig CreationConfig { get; init; }
	public TextureCombinationConfig CombinationConfig { get; init; }
	public TextureProcessingConfig ProcessingConfigA { get; init; }
	public TextureProcessingConfig ProcessingConfigB { get; init; }
	public TextureProcessingConfig ProcessingConfigC { get; init; }
	public TextureProcessingConfig ProcessingConfigD { get; init; }
	public int SourceCount { get; init; }

	public TextureCombinedLoadConfig() { }

	internal void ThrowIfInvalid() {
		CreationConfig.ThrowIfInvalid();
		CombinationConfig.ThrowIfInvalid(SourceCount);
		ProcessingConfigA.ThrowIfInvalid();
		ProcessingConfigB.ThrowIfInvalid();
		ProcessingConfigC.ThrowIfInvalid();
		ProcessingConfigD.ThrowIfInvalid();
	}

	public static int GetHeapStorageFormattedLength(in TextureCombinedLoadConfig src) {
		return	SerializationSizeOfSubConfig(src.CreationConfig) // CreationConfig
			+	SerializationSizeOfSubConfig(src.CombinationConfig) // CombinationConfig
			+	SerializationSizeOfSubConfig(src.ProcessingConfigA) // ProcessingConfigA
			+	SerializationSizeOfSubConfig(src.ProcessingConfigB) // ProcessingConfigB
			+	SerializationSizeOfSubConfig(src.ProcessingConfigC) // ProcessingConfigC
			+	SerializationSizeOfSubConfig(src.ProcessingConfigD) // ProcessingConfigD
			+	SerializationSizeOfInt(); // SourceCount
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in TextureCombinedLoadConfig src) {
		SerializationWriteSubConfig(ref dest, src.CreationConfig);
		SerializationWriteSubConfig(ref dest, src.CombinationConfig);
		SerializationWriteSubConfig(ref dest, src.ProcessingConfigA);
		SerializationWriteSubConfig(ref dest, src.ProcessingConfigB);
		SerializationWriteSubConfig(ref dest, src.ProcessingConfigC);
		SerializationWriteSubConfig(ref dest, src.ProcessingConfigD);
		SerializationWriteInt(ref dest, src.SourceCount);
	}
	public static TextureCombinedLoadConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new TextureCombinedLoadConfig {
			CreationConfig = SerializationReadSubConfig<TextureCreationConfig>(ref src),
			CombinationConfig = SerializationReadSubConfig<TextureCombinationConfig>(ref src),
			ProcessingConfigA = SerializationReadSubConfig<TextureProcessingConfig>(ref src),
			ProcessingConfigB = SerializationReadSubConfig<TextureProcessingConfig>(ref src),
			ProcessingConfigC = SerializationReadSubConfig<TextureProcessingConfig>(ref src),
			ProcessingConfigD = SerializationReadSubConfig<TextureProcessingConfig>(ref src),
			SourceCount = SerializationReadInt(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SerializationDisposeSubConfig<TextureCreationConfig>(ref src);
		SerializationDisposeSubConfig<TextureCombinationConfig>(ref src);
		SerializationDisposeSubConfig<TextureProcessingConfig>(ref src);
		SerializationDisposeSubConfig<TextureProcessingConfig>(ref src);
		SerializationDisposeSubConfig<TextureProcessingConfig>(ref src);
		SerializationDisposeSubConfig<TextureProcessingConfig>(ref src);
	}
}
