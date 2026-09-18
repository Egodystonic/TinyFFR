// Created on 2026-02-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using static Egodystonic.TinyFFR.Assets.Materials.TextureCombinationSourceTexture;
using static Egodystonic.TinyFFR.ColorChannel;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// Describes how a source texture that is smaller than the combined output should be stretched to fill it.
/// </summary>
/// <remarks>
/// When several textures are combined in to one, the output takes the largest width and height of any of them. Any source that
/// is smaller than that must be enlarged somehow, and this chooses between enlarging the image itself or simply repeating or
/// padding it.
/// </remarks>
public enum TextureCombinationScalingStrategy {
	/// <summary>
	/// Nearest-neighbour:
	/// Enlarges the source by repeating each of its texels, so the image grows as blocks of flat colour with hard edges between
	/// them.
	/// </summary>
	/// <remarks>
	/// This preserves the source's exact values, which matters when the channels carry data rather than colour and blending
	/// neighbouring values together would be meaningless.
	/// </remarks>
	PixelUpscale,
	/// <summary>
	/// Bilinear:
	/// Enlarges the source by blending between neighbouring texels, so the image grows smoothly rather than in blocks.
	/// </summary>
	/// <remarks>
	/// This usually looks better than <see cref="PixelUpscale"/> for colour data, but it invents values that were not in the
	/// source.
	/// </remarks>
	BilinearUpscale,
	/// <summary>
	/// Wrap:
	/// Keeps the source at its original size and tiles it across the output, starting again from its opposite edge each time it
	/// runs out.
	/// </summary>
	RepeatingTile,
	/// <summary>
	/// Center + Clamp:
	/// Keeps the source at its original size, places it in the centre of the output, and fills the remaining border by
	/// stretching the source's outermost texels outwards.
	/// </summary>
	ExtendEdges,
}
/// <summary>
/// Identifies one of the textures being combined, by the position it was passed in at.
/// </summary>
public enum TextureCombinationSourceTexture {
	/// <summary>
	/// The first texture passed to the combination.
	/// </summary>
	TextureA,
	/// <summary>
	/// The second texture passed to the combination.
	/// </summary>
	TextureB,
	/// <summary>
	/// The third texture passed to the combination.
	/// </summary>
	TextureC,
	/// <summary>
	/// The fourth texture passed to the combination.
	/// </summary>
	TextureD
}
/// <summary>
/// Names one channel of one source texture, as the origin of one channel of a combined texture.
/// </summary>
/// <param name="SourceTexture">Which of the textures being combined to take the value from.</param>
/// <param name="SourceChannel">Which channel of that texture to take.</param>
public readonly record struct TextureCombinationSource(TextureCombinationSourceTexture SourceTexture, ColorChannel SourceChannel) {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TChannel? SelectTexelChannel<TTexel, TChannel>(ReadOnlySpan<TTexel> samples) where TTexel : unmanaged, ITexel<TTexel, TChannel> where TChannel : struct {
		return samples[(int) SourceTexture].TryGetChannel(SourceChannel);
	}

	internal void ThrowIfInvalid(int numTexturesBeingCombined) {
		if (!Enum.IsDefined(SourceChannel)) {
			throw new InvalidOperationException($"{nameof(SourceChannel)} was not a recognised {nameof(ColorChannel)}.");
		}
		if ((int) SourceTexture < 0 || (int) SourceTexture >= numTexturesBeingCombined) {
			throw new InvalidOperationException($"Non-defined value or references a texture that was not provided (i.e. 'TextureC' when only textures A & B exist).");
		}
	}
}
/// <summary>
/// Describes how several textures should be packed in to a single output combined texture.
/// </summary>
/// <remarks>
/// <para>
/// The string-based constructors are usually the most readable way to write one of these.
/// </para>
/// </remarks>
/// <param name="ScalingStrategy">How a source smaller than the combined output is enlarged to fill it.</param>
/// <param name="OutputTextureXRedChannelSource">Which source channel supplies the output's red channel.</param>
/// <param name="OutputTextureYGreenChannelSource">Which source channel supplies the output's green channel.</param>
/// <param name="OutputTextureZBlueChannelSource">Which source channel supplies the output's blue channel.</param>
/// <param name="OutputTextureWAlphaChannelSource">Which source channel supplies the output's alpha channel, or <see langword="null"/> for an output with no alpha channel.</param>
public readonly record struct TextureCombinationConfig(TextureCombinationScalingStrategy ScalingStrategy, TextureCombinationSource OutputTextureXRedChannelSource, TextureCombinationSource OutputTextureYGreenChannelSource, TextureCombinationSource OutputTextureZBlueChannelSource, TextureCombinationSource? OutputTextureWAlphaChannelSource = null) : IConfigStruct<TextureCombinationConfig> {
	/// <summary>
	/// The default value for <see cref="ScalingStrategy"/>: <see cref="TextureCombinationScalingStrategy.PixelUpscale"/>.
	/// </summary>
	public static readonly TextureCombinationScalingStrategy DefaultScalingStrategy = TextureCombinationScalingStrategy.PixelUpscale;
	
	/// <summary>
	/// Constructs a new <see cref="TextureCombinationConfig"/> using <see cref="DefaultScalingStrategy"/>.
	/// </summary>
	/// <param name="OutputTextureXRedChannelSource">Which source channel supplies the output's red channel.</param>
	/// <param name="OutputTextureYGreenChannelSource">Which source channel supplies the output's green channel.</param>
	/// <param name="OutputTextureZBlueChannelSource">Which source channel supplies the output's blue channel.</param>
	/// <param name="OutputTextureWAlphaChannelSource">Which source channel supplies the output's alpha channel, or <see langword="null"/> for an output with no alpha channel.</param>
	public TextureCombinationConfig(TextureCombinationSource OutputTextureXRedChannelSource, TextureCombinationSource OutputTextureYGreenChannelSource, TextureCombinationSource OutputTextureZBlueChannelSource, TextureCombinationSource? OutputTextureWAlphaChannelSource = null)
		: this(DefaultScalingStrategy, OutputTextureXRedChannelSource, OutputTextureYGreenChannelSource, OutputTextureZBlueChannelSource, OutputTextureWAlphaChannelSource) { }

	static TextureCombinationSource ExtractFromString(ReadOnlySpan<char> twoChars) {
		return new TextureCombinationSource(
			Char.ToLowerInvariant(twoChars[0]) switch {
				'a' or '0' => TextureA,
				'b' or '1' => TextureB,
				'c' or '2' => TextureC,
				'd' or '3' => TextureD,
				_ => throw new ArgumentException($"Character '{twoChars[0]}' was expected to be one of 'a', 'b', 'c', 'd', '0', '1', '2', '3' (to denote a source texture).")
			},
			Char.ToLowerInvariant(twoChars[1]) switch {
				'r' or 'x' or '0' => R,
				'g' or 'y' or '1' => G,
				'b' or 'z' or '2' => B,
				'a' or 'w' or '3' => A,
				_ => throw new ArgumentException($"Character '{twoChars[1]}' was expected to be one of 'r', 'g', 'b', 'a', 'x', 'y', 'z', 'w' (to denote a source channel).")
			}
		);
	}

	/// <summary>
	/// Constructs a new <see cref="TextureCombinationConfig"/> from a short string describing the channel selection, using <see cref="DefaultScalingStrategy"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The string is read two characters at a time, giving the source for the output's red, green, blue and alpha channels in
	/// that order. The first character of each pair names the source texture (<c>a</c> to <c>d</c>, or <c>0</c> to <c>3</c>)
	/// and the second names its channel (<c>r</c>/<c>g</c>/<c>b</c>/<c>a</c>, or the equivalent <c>x</c>/<c>y</c>/<c>z</c>/<c>w</c>
	/// or <c>0</c> to <c>3</c>). Case is ignored.
	/// </para>
	/// <para>
	/// So <c>"aRbGcB"</c> builds a three-channel output from the red channel of A, the green of B and the blue of C. This is
	/// usually easier to read than naming each source separately.
	/// </para>
	/// </remarks>
	/// <param name="selectionString">The channel selection. Must be exactly 6 characters for an output with no alpha channel, or 8 for one with alpha.</param>
	/// <exception cref="ArgumentException">Thrown when any character does not name a source texture or channel.</exception>
	public TextureCombinationConfig(ReadOnlySpan<char> selectionString) : this(
		DefaultScalingStrategy,
		ExtractFromString(selectionString[0..2]),
		ExtractFromString(selectionString[2..4]),
		ExtractFromString(selectionString[4..6]),
		selectionString.Length >= 8 ? ExtractFromString(selectionString[6..8]) : null
	) { }
	/// <summary>
	/// Constructs a new <see cref="TextureCombinationConfig"/> from a short string describing the channel selection.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The string is read two characters at a time, giving the source for the output's red, green, blue and alpha channels in
	/// that order. The first character of each pair names the source texture (<c>a</c> to <c>d</c>, or <c>0</c> to <c>3</c>)
	/// and the second names its channel (<c>r</c>/<c>g</c>/<c>b</c>/<c>a</c>, or the equivalent <c>x</c>/<c>y</c>/<c>z</c>/<c>w</c>
	/// or <c>0</c> to <c>3</c>). Case is ignored.
	/// </para>
	/// <para>
	/// So <c>"aRbGcB"</c> builds a three-channel output from the red channel of A, the green of B and the blue of C. This is
	/// usually easier to read than naming each source separately.
	/// </para>
	/// </remarks>
	/// <param name="scalingStrategy">How sources smaller than the output are enlarged.</param>
	/// <param name="selectionString">The channel selection. Must be exactly 6 characters for an output with no alpha channel, or 8 for one with alpha.</param>
	/// <exception cref="ArgumentException">Thrown when any character does not name a source texture or channel.</exception>
	public TextureCombinationConfig(TextureCombinationScalingStrategy scalingStrategy, ReadOnlySpan<char> selectionString) : this(
		scalingStrategy,
		ExtractFromString(selectionString[0..2]),
		ExtractFromString(selectionString[2..4]),
		ExtractFromString(selectionString[4..6]),
		selectionString.Length >= 8 ? ExtractFromString(selectionString[6..8]) : null
	) { }
	/// <summary>
	/// Constructs a new <see cref="TextureCombinationConfig"/> for a three-channel output, using <see cref="DefaultScalingStrategy"/>.
	/// </summary>
	/// <param name="xRedSourceTex">Which source texture supplies the output's red channel.</param>
	/// <param name="xRedSourceChannel">Which channel of that texture supplies the output's red channel.</param>
	/// <param name="yGreenSourceTex">Which source texture supplies the output's green channel.</param>
	/// <param name="yGreenSourceChannel">Which channel of that texture supplies the output's green channel.</param>
	/// <param name="zBlueSourceTex">Which source texture supplies the output's blue channel.</param>
	/// <param name="zBlueSourceChannel">Which channel of that texture supplies the output's blue channel.</param>
	public TextureCombinationConfig(TextureCombinationSourceTexture xRedSourceTex, ColorChannel xRedSourceChannel, TextureCombinationSourceTexture yGreenSourceTex, ColorChannel yGreenSourceChannel, TextureCombinationSourceTexture zBlueSourceTex, ColorChannel zBlueSourceChannel)
		: this(DefaultScalingStrategy, new TextureCombinationSource(xRedSourceTex, xRedSourceChannel), new TextureCombinationSource(yGreenSourceTex, yGreenSourceChannel), new TextureCombinationSource(zBlueSourceTex, zBlueSourceChannel)) { }
	/// <summary>
	/// Constructs a new <see cref="TextureCombinationConfig"/> for a three-channel output.
	/// </summary>
	/// <param name="scalingStrategy">How sources smaller than the output are enlarged.</param>
	/// <param name="xRedSourceTex">Which source texture supplies the output's red channel.</param>
	/// <param name="xRedSourceChannel">Which channel of that texture supplies the output's red channel.</param>
	/// <param name="yGreenSourceTex">Which source texture supplies the output's green channel.</param>
	/// <param name="yGreenSourceChannel">Which channel of that texture supplies the output's green channel.</param>
	/// <param name="zBlueSourceTex">Which source texture supplies the output's blue channel.</param>
	/// <param name="zBlueSourceChannel">Which channel of that texture supplies the output's blue channel.</param>
	public TextureCombinationConfig(TextureCombinationScalingStrategy scalingStrategy, TextureCombinationSourceTexture xRedSourceTex, ColorChannel xRedSourceChannel, TextureCombinationSourceTexture yGreenSourceTex, ColorChannel yGreenSourceChannel, TextureCombinationSourceTexture zBlueSourceTex, ColorChannel zBlueSourceChannel)
		: this(scalingStrategy, new TextureCombinationSource(xRedSourceTex, xRedSourceChannel), new TextureCombinationSource(yGreenSourceTex, yGreenSourceChannel), new TextureCombinationSource(zBlueSourceTex, zBlueSourceChannel)) { }
	/// <summary>
	/// Constructs a new <see cref="TextureCombinationConfig"/> for a four-channel output, using <see cref="DefaultScalingStrategy"/>.
	/// </summary>
	/// <param name="xRedSourceTex">Which source texture supplies the output's red channel.</param>
	/// <param name="xRedSourceChannel">Which channel of that texture supplies the output's red channel.</param>
	/// <param name="yGreenSourceTex">Which source texture supplies the output's green channel.</param>
	/// <param name="yGreenSourceChannel">Which channel of that texture supplies the output's green channel.</param>
	/// <param name="zBlueSourceTex">Which source texture supplies the output's blue channel.</param>
	/// <param name="zBlueSourceChannel">Which channel of that texture supplies the output's blue channel.</param>
	/// <param name="wAlphaSourceTex">Which source texture supplies the output's alpha channel.</param>
	/// <param name="wAlphaSourceChannel">Which channel of that texture supplies the output's alpha channel.</param>
	public TextureCombinationConfig(TextureCombinationSourceTexture xRedSourceTex, ColorChannel xRedSourceChannel, TextureCombinationSourceTexture yGreenSourceTex, ColorChannel yGreenSourceChannel, TextureCombinationSourceTexture zBlueSourceTex, ColorChannel zBlueSourceChannel, TextureCombinationSourceTexture wAlphaSourceTex, ColorChannel wAlphaSourceChannel)
		: this(DefaultScalingStrategy, new TextureCombinationSource(xRedSourceTex, xRedSourceChannel), new TextureCombinationSource(yGreenSourceTex, yGreenSourceChannel), new TextureCombinationSource(zBlueSourceTex, zBlueSourceChannel), new TextureCombinationSource(wAlphaSourceTex, wAlphaSourceChannel)) { }
	/// <summary>
	/// Constructs a new <see cref="TextureCombinationConfig"/> for a four-channel output.
	/// </summary>
	/// <param name="scalingStrategy">How sources smaller than the output are enlarged.</param>
	/// <param name="xRedSourceTex">Which source texture supplies the output's red channel.</param>
	/// <param name="xRedSourceChannel">Which channel of that texture supplies the output's red channel.</param>
	/// <param name="yGreenSourceTex">Which source texture supplies the output's green channel.</param>
	/// <param name="yGreenSourceChannel">Which channel of that texture supplies the output's green channel.</param>
	/// <param name="zBlueSourceTex">Which source texture supplies the output's blue channel.</param>
	/// <param name="zBlueSourceChannel">Which channel of that texture supplies the output's blue channel.</param>
	/// <param name="wAlphaSourceTex">Which source texture supplies the output's alpha channel.</param>
	/// <param name="wAlphaSourceChannel">Which channel of that texture supplies the output's alpha channel.</param>
	public TextureCombinationConfig(TextureCombinationScalingStrategy scalingStrategy, TextureCombinationSourceTexture xRedSourceTex, ColorChannel xRedSourceChannel, TextureCombinationSourceTexture yGreenSourceTex, ColorChannel yGreenSourceChannel, TextureCombinationSourceTexture zBlueSourceTex, ColorChannel zBlueSourceChannel, TextureCombinationSourceTexture wAlphaSourceTex, ColorChannel wAlphaSourceChannel)
		: this(scalingStrategy, new TextureCombinationSource(xRedSourceTex, xRedSourceChannel), new TextureCombinationSource(yGreenSourceTex, yGreenSourceChannel), new TextureCombinationSource(zBlueSourceTex, zBlueSourceChannel), new TextureCombinationSource(wAlphaSourceTex, wAlphaSourceChannel)) { }


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static TChannel SelectChannelOrFallback<TIn, TChannel>(TextureCombinationSource source, ReadOnlySpan<TIn> samples, TChannel fallback) where TIn : unmanaged, ITexel<TIn, TChannel> where TChannel : struct {
		var requiredChannelCount = source.SourceChannel switch {
			R => 1,
			G => 2,
			B => 3,
			A => 4,
			_ => Int32.MaxValue
		};
		if (TIn.ChannelCount < requiredChannelCount) return fallback;
		return samples[(int) source.SourceTexture][source.SourceChannel];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TOut SelectTexel<TIn, TOut, TChannel>(ReadOnlySpan<TIn> samples) where TIn : unmanaged, ITexel<TIn, TChannel> where TOut : unmanaged, ITexel<TOut, TChannel> where TChannel : struct {
		return TOut.ConstructFromIgnoringExcessArguments(
			SelectChannelOrFallback(OutputTextureXRedChannelSource, samples, TOut.MinChannelValue),
			SelectChannelOrFallback(OutputTextureYGreenChannelSource, samples, TOut.MinChannelValue),
			SelectChannelOrFallback(OutputTextureZBlueChannelSource, samples, TOut.MinChannelValue),
			OutputTextureWAlphaChannelSource is { } alphaSource
				? SelectChannelOrFallback(alphaSource, samples, TOut.MaxChannelValue)
				: TOut.MaxChannelValue
		);
	}

	internal void ThrowIfInvalid(int numTexturesBeingCombined) {
		OutputTextureXRedChannelSource.ThrowIfInvalid(numTexturesBeingCombined);
		OutputTextureYGreenChannelSource.ThrowIfInvalid(numTexturesBeingCombined);
		OutputTextureZBlueChannelSource.ThrowIfInvalid(numTexturesBeingCombined);
		OutputTextureWAlphaChannelSource?.ThrowIfInvalid(numTexturesBeingCombined);
	}

	static int SerializationSizeOfSource() => SerializationSizeOfInt() * 2;
	static void SerializationWriteSource(scoped ref Span<byte> dest, TextureCombinationSource src) {
		SerializationWriteInt(ref dest, (int) src.SourceTexture);
		SerializationWriteInt(ref dest, (int) src.SourceChannel);
	}
	static TextureCombinationSource SerializationReadSource(scoped ref ReadOnlySpan<byte> src) {
		var sourceTexture = (TextureCombinationSourceTexture) SerializationReadInt(ref src);
		var sourceChannel = (ColorChannel) SerializationReadInt(ref src);
		return new TextureCombinationSource(sourceTexture, sourceChannel);
	}

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in TextureCombinationConfig src) {
		return	SerializationSizeOfInt() // ScalingStrategy
			+	SerializationSizeOfSource() // OutputTextureXRedChannelSource
			+	SerializationSizeOfSource() // OutputTextureYGreenChannelSource
			+	SerializationSizeOfSource() // OutputTextureZBlueChannelSource
			+	SerializationSizeOfBool() // OutputTextureWAlphaChannelSource.HasValue
			+	SerializationSizeOfSource(); // OutputTextureWAlphaChannelSource
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in TextureCombinationConfig src) {
		SerializationWriteInt(ref dest, (int) src.ScalingStrategy);
		SerializationWriteSource(ref dest, src.OutputTextureXRedChannelSource);
		SerializationWriteSource(ref dest, src.OutputTextureYGreenChannelSource);
		SerializationWriteSource(ref dest, src.OutputTextureZBlueChannelSource);
		SerializationWriteBool(ref dest, src.OutputTextureWAlphaChannelSource.HasValue);
		SerializationWriteSource(ref dest, src.OutputTextureWAlphaChannelSource ?? default);
	}
	/// <inheritdoc />
	public static TextureCombinationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		var scalingStrategy = (TextureCombinationScalingStrategy) SerializationReadInt(ref src);
		var xRedSource = SerializationReadSource(ref src);
		var yGreenSource = SerializationReadSource(ref src);
		var zBlueSource = SerializationReadSource(ref src);
		var wAlphaSourcePresent = SerializationReadBool(ref src);
		var wAlphaSource = SerializationReadSource(ref src);

		return new TextureCombinationConfig(
			scalingStrategy,
			xRedSource,
			yGreenSource,
			zBlueSource,
			wAlphaSourcePresent ? wAlphaSource : null
		);
	}
	/// <inheritdoc />
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}

public static partial class TextureUtils {
	#region Public API
	/// <summary>
	/// Returns the width and height the texture combining these 2 sources would have.
	/// </summary>
	/// <remarks>
	/// The combined output takes the largest width and height of any of its sources, so it is never smaller than the largest
	/// one given.
	/// </remarks>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	public static XYPair<int> GetCombinedTextureDimensions(XYPair<int> aDimensions, XYPair<int> bDimensions) => GetCombinedTextureDimensions(aDimensions, bDimensions, out _);
	/// <summary>
	/// Returns the width and height the texture combining these 2 sources would have.
	/// </summary>
	/// <remarks>
	/// The combined output takes the largest width and height of any of its sources, so it is never smaller than the largest
	/// one given.
	/// </remarks>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="allDimensionsMatched">Set to <see langword="true"/> if every source was already the same size, in which case no source needs enlarging at all.</param>
	public static XYPair<int> GetCombinedTextureDimensions(XYPair<int> aDimensions, XYPair<int> bDimensions, out bool allDimensionsMatched) {
		allDimensionsMatched = aDimensions == bDimensions;
		if (allDimensionsMatched) return aDimensions;

		return new(
			Int32.Max(aDimensions.X, bDimensions.X),
			Int32.Max(aDimensions.Y, bDimensions.Y)
		);
	}
	
	/// <summary>
	/// Returns the width and height the texture combining these 3 sources would have.
	/// </summary>
	/// <remarks>
	/// The combined output takes the largest width and height of any of its sources, so it is never smaller than the largest
	/// one given.
	/// </remarks>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="cDimensions">The third source texture's width and height, in texels.</param>
	public static XYPair<int> GetCombinedTextureDimensions(XYPair<int> aDimensions, XYPair<int> bDimensions, XYPair<int> cDimensions) => GetCombinedTextureDimensions(aDimensions, bDimensions, cDimensions, out _);
	/// <summary>
	/// Returns the width and height the texture combining these 3 sources would have.
	/// </summary>
	/// <remarks>
	/// The combined output takes the largest width and height of any of its sources, so it is never smaller than the largest
	/// one given.
	/// </remarks>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="cDimensions">The third source texture's width and height, in texels.</param>
	/// <param name="allDimensionsMatched">Set to <see langword="true"/> if every source was already the same size, in which case no source needs enlarging at all.</param>
	public static XYPair<int> GetCombinedTextureDimensions(XYPair<int> aDimensions, XYPair<int> bDimensions, XYPair<int> cDimensions, out bool allDimensionsMatched) {
		allDimensionsMatched = aDimensions == bDimensions && bDimensions == cDimensions;
		if (allDimensionsMatched) return aDimensions;

		return new(
			Int32.Max(Int32.Max(aDimensions.X, bDimensions.X), cDimensions.X),
			Int32.Max(Int32.Max(aDimensions.Y, bDimensions.Y), cDimensions.Y)
		);
	}
	
	/// <summary>
	/// Returns the width and height the texture combining these 4 sources would have.
	/// </summary>
	/// <remarks>
	/// The combined output takes the largest width and height of any of its sources, so it is never smaller than the largest
	/// one given.
	/// </remarks>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="cDimensions">The third source texture's width and height, in texels.</param>
	/// <param name="dDimensions">The fourth source texture's width and height, in texels.</param>
	public static XYPair<int> GetCombinedTextureDimensions(XYPair<int> aDimensions, XYPair<int> bDimensions, XYPair<int> cDimensions, XYPair<int> dDimensions) => GetCombinedTextureDimensions(aDimensions, bDimensions, cDimensions, dDimensions, out _);
	/// <summary>
	/// Returns the width and height the texture combining these 4 sources would have.
	/// </summary>
	/// <remarks>
	/// The combined output takes the largest width and height of any of its sources, so it is never smaller than the largest
	/// one given.
	/// </remarks>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="cDimensions">The third source texture's width and height, in texels.</param>
	/// <param name="dDimensions">The fourth source texture's width and height, in texels.</param>
	/// <param name="allDimensionsMatched">Set to <see langword="true"/> if every source was already the same size, in which case no source needs enlarging at all.</param>
	public static XYPair<int> GetCombinedTextureDimensions(XYPair<int> aDimensions, XYPair<int> bDimensions, XYPair<int> cDimensions, XYPair<int> dDimensions, out bool allDimensionsMatched) {
		allDimensionsMatched = aDimensions == bDimensions && bDimensions == cDimensions && cDimensions == dDimensions;
		if (allDimensionsMatched) return aDimensions;

		return new(
			Int32.Max(Int32.Max(Int32.Max(aDimensions.X, bDimensions.X), cDimensions.X), dDimensions.X),
			Int32.Max(Int32.Max(Int32.Max(aDimensions.Y, bDimensions.Y), cDimensions.Y), dDimensions.Y)
		);
	}

	/// <summary>
	/// Combines the channels of 2 textures held in memory in to one.
	/// </summary>
	/// <remarks>
	/// The output takes the largest width and height of any source, so a source smaller than that is enlarged according
	/// to the combination config's scaling strategy.
	/// </remarks>
	/// <param name="aBuffer">The first source texture's texels, laid out row by row.</param>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bBuffer">The second source texture's texels, laid out row by row.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output, and how sources smaller than the output are enlarged. Must not refer to a source texture beyond those supplied here.</param>
	/// <param name="destinationBuffer">The buffer to write the combined texels in to, laid out row by row. Must be at least as long as the area reported by <c>GetCombinedTextureDimensions</c>.</param>
	public static void CombineTextures(
		ReadOnlySpan<TexelRgba32> aBuffer, XYPair<int> aDimensions,
		ReadOnlySpan<TexelRgba32> bBuffer, XYPair<int> bDimensions,
		TextureCombinationConfig combinationConfig, Span<TexelRgba32> destinationBuffer) => CombineTextures<TexelRgba32, TexelRgba32, byte>(aBuffer, aDimensions, bBuffer, bDimensions, combinationConfig, destinationBuffer);
	/// <summary>
	/// Combines the channels of 2 textures held in memory in to one.
	/// </summary>
	/// <remarks>
	/// The output takes the largest width and height of any source, so a source smaller than that is enlarged according
	/// to the combination config's scaling strategy.
	/// </remarks>
	/// <param name="aBuffer">The first source texture's texels, laid out row by row.</param>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bBuffer">The second source texture's texels, laid out row by row.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output, and how sources smaller than the output are enlarged. Must not refer to a source texture beyond those supplied here.</param>
	/// <param name="destinationBuffer">The buffer to write the combined texels in to, laid out row by row. Must be at least as long as the area reported by <c>GetCombinedTextureDimensions</c>.</param>
	public static void CombineTextures(
		ReadOnlySpan<TexelRgb24> aBuffer, XYPair<int> aDimensions,
		ReadOnlySpan<TexelRgb24> bBuffer, XYPair<int> bDimensions,
		TextureCombinationConfig combinationConfig, Span<TexelRgb24> destinationBuffer) => CombineTextures<TexelRgb24, TexelRgb24, byte>(aBuffer, aDimensions, bBuffer, bDimensions, combinationConfig, destinationBuffer);
	/// <summary>
	/// Combines the channels of 2 textures held in memory in to one.
	/// </summary>
	/// <remarks>
	/// The output takes the largest width and height of any source, so a source smaller than that is enlarged according
	/// to the combination config's scaling strategy.
	/// </remarks>
	/// <param name="aBuffer">The first source texture's texels, laid out row by row.</param>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bBuffer">The second source texture's texels, laid out row by row.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output, and how sources smaller than the output are enlarged. Must not refer to a source texture beyond those supplied here.</param>
	/// <param name="destinationBuffer">The buffer to write the combined texels in to, laid out row by row. Must be at least as long as the area reported by <c>GetCombinedTextureDimensions</c>.</param>
	public static void CombineTextures(
		ReadOnlySpan<TexelRgba32> aBuffer, XYPair<int> aDimensions,
		ReadOnlySpan<TexelRgba32> bBuffer, XYPair<int> bDimensions,
		TextureCombinationConfig combinationConfig, Span<TexelRgb24> destinationBuffer) => CombineTextures<TexelRgba32, TexelRgb24, byte>(aBuffer, aDimensions, bBuffer, bDimensions, combinationConfig, destinationBuffer);
	/// <summary>
	/// Combines the channels of 2 textures held in memory in to one.
	/// </summary>
	/// <remarks>
	/// The output takes the largest width and height of any source, so a source smaller than that is enlarged according
	/// to the combination config's scaling strategy.
	/// </remarks>
	/// <param name="aBuffer">The first source texture's texels, laid out row by row.</param>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bBuffer">The second source texture's texels, laid out row by row.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output, and how sources smaller than the output are enlarged. Must not refer to a source texture beyond those supplied here.</param>
	/// <param name="destinationBuffer">The buffer to write the combined texels in to, laid out row by row. Must be at least as long as the area reported by <c>GetCombinedTextureDimensions</c>.</param>
	public static void CombineTextures(
		ReadOnlySpan<TexelRgb24> aBuffer, XYPair<int> aDimensions,
		ReadOnlySpan<TexelRgb24> bBuffer, XYPair<int> bDimensions,
		TextureCombinationConfig combinationConfig, Span<TexelRgba32> destinationBuffer) => CombineTextures<TexelRgb24, TexelRgba32, byte>(aBuffer, aDimensions, bBuffer, bDimensions, combinationConfig, destinationBuffer);
	/// <summary>
	/// Combines the channels of 2 textures held in memory in to one.
	/// </summary>
	/// <remarks>
	/// This is the general form; the non-generic overloads simply pin the texel types to the two standard ones.
	/// </remarks>
	/// <typeparam name="TIn">The texel type of the source textures.</typeparam>
	/// <typeparam name="TOut">The texel type to write.</typeparam>
	/// <typeparam name="TChannel">The type of each texel channel, common to both.</typeparam>
	/// <param name="aBuffer">The first source texture's texels, laid out row by row.</param>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bBuffer">The second source texture's texels, laid out row by row.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output, and how sources smaller than the output are enlarged. Must not refer to a source texture beyond those supplied here.</param>
	/// <param name="destinationBuffer">The buffer to write the combined texels in to, laid out row by row. Must be at least as long as the area reported by <c>GetCombinedTextureDimensions</c>.</param>
	public static void CombineTextures<TIn, TOut, TChannel>(
		ReadOnlySpan<TIn> aBuffer, XYPair<int> aDimensions,
		ReadOnlySpan<TIn> bBuffer, XYPair<int> bDimensions,
		TextureCombinationConfig combinationConfig, Span<TOut> destinationBuffer
	) where TIn : unmanaged, ITexel<TIn, TChannel> where TOut : unmanaged, ITexel<TOut, TChannel> where TChannel : struct {
		const int NumTexturesBeingCombined = 2;
		combinationConfig.ThrowIfInvalid(NumTexturesBeingCombined);

		var destDimensions = GetCombinedTextureDimensions(aDimensions, bDimensions, out var allDimensionsMatch);
		if (destinationBuffer.Length < destDimensions.Area) {
			throw new ArgumentException(
				$"Destination buffer length needs to be at least {destDimensions.Area} " +
				$"(output combined texture would have dimensions {destDimensions}). " +
				$"Calculate the dimensions of the output texture first using {nameof(GetCombinedTextureDimensions)}.",
				nameof(destinationBuffer)
			);
		}

		Span<TIn> localSampleBuffer = stackalloc TIn[NumTexturesBeingCombined];

		var aDimensionsMatchDest = aDimensions == destDimensions;
		var bDimensionsMatchDest = bDimensions == destDimensions;
		var aIsSingleTexel = aDimensions == XYPair<int>.One;
		var bIsSingleTexel = bDimensions == XYPair<int>.One;
		var canSkipRescaling = allDimensionsMatch || ((aDimensionsMatchDest || aIsSingleTexel) && (bDimensionsMatchDest || bIsSingleTexel));

		if (!canSkipRescaling) {
			var aCentralizingOffset = CalculateCentralizingOffsetForCenterClampSampling(aDimensions, destDimensions);
			var bCentralizingOffset = CalculateCentralizingOffsetForCenterClampSampling(bDimensions, destDimensions);
			for (var y = 0; y < destDimensions.Y; ++y) {
				for (var x = 0; x < destDimensions.X; ++x) {
					localSampleBuffer[0] = CalculateUpwardRescaledValue(x, y, aBuffer, aDimensions, destDimensions, aCentralizingOffset, combinationConfig.ScalingStrategy);
					localSampleBuffer[1] = CalculateUpwardRescaledValue(x, y, bBuffer, bDimensions, destDimensions, bCentralizingOffset, combinationConfig.ScalingStrategy);
					destinationBuffer[destDimensions.Index(x, y)] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
			}
			return;
		}
		
		var area = destDimensions.Area;
		switch (aDimensionsMatchDest, bDimensionsMatchDest) {
			case (true, true): {
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[0] = aBuffer[i];
					localSampleBuffer[1] = bBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			case (false, true): {
				localSampleBuffer[0] = aBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[1] = bBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			default: {
				localSampleBuffer[1] = bBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[0] = aBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
		}
	}

	/// <summary>
	/// Combines the channels of 3 textures held in memory in to one.
	/// </summary>
	/// <remarks>
	/// The output takes the largest width and height of any source, so a source smaller than that is enlarged according
	/// to the combination config's scaling strategy.
	/// </remarks>
	/// <param name="aBuffer">The first source texture's texels, laid out row by row.</param>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bBuffer">The second source texture's texels, laid out row by row.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="cBuffer">The third source texture's texels, laid out row by row.</param>
	/// <param name="cDimensions">The third source texture's width and height, in texels.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output, and how sources smaller than the output are enlarged. Must not refer to a source texture beyond those supplied here.</param>
	/// <param name="destinationBuffer">The buffer to write the combined texels in to, laid out row by row. Must be at least as long as the area reported by <c>GetCombinedTextureDimensions</c>.</param>
	public static void CombineTextures(
		ReadOnlySpan<TexelRgba32> aBuffer, XYPair<int> aDimensions,
		ReadOnlySpan<TexelRgba32> bBuffer, XYPair<int> bDimensions,
		ReadOnlySpan<TexelRgba32> cBuffer, XYPair<int> cDimensions,
		TextureCombinationConfig combinationConfig, Span<TexelRgba32> destinationBuffer) => CombineTextures<TexelRgba32, TexelRgba32, byte>(aBuffer, aDimensions, bBuffer, bDimensions, cBuffer, cDimensions, combinationConfig, destinationBuffer);
	/// <summary>
	/// Combines the channels of 3 textures held in memory in to one.
	/// </summary>
	/// <remarks>
	/// The output takes the largest width and height of any source, so a source smaller than that is enlarged according
	/// to the combination config's scaling strategy.
	/// </remarks>
	/// <param name="aBuffer">The first source texture's texels, laid out row by row.</param>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bBuffer">The second source texture's texels, laid out row by row.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="cBuffer">The third source texture's texels, laid out row by row.</param>
	/// <param name="cDimensions">The third source texture's width and height, in texels.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output, and how sources smaller than the output are enlarged. Must not refer to a source texture beyond those supplied here.</param>
	/// <param name="destinationBuffer">The buffer to write the combined texels in to, laid out row by row. Must be at least as long as the area reported by <c>GetCombinedTextureDimensions</c>.</param>
	public static void CombineTextures(
		ReadOnlySpan<TexelRgb24> aBuffer, XYPair<int> aDimensions,
		ReadOnlySpan<TexelRgb24> bBuffer, XYPair<int> bDimensions,
		ReadOnlySpan<TexelRgb24> cBuffer, XYPair<int> cDimensions,
		TextureCombinationConfig combinationConfig, Span<TexelRgb24> destinationBuffer) => CombineTextures<TexelRgb24, TexelRgb24, byte>(aBuffer, aDimensions, bBuffer, bDimensions, cBuffer, cDimensions, combinationConfig, destinationBuffer);
	/// <summary>
	/// Combines the channels of 3 textures held in memory in to one.
	/// </summary>
	/// <remarks>
	/// The output takes the largest width and height of any source, so a source smaller than that is enlarged according
	/// to the combination config's scaling strategy.
	/// </remarks>
	/// <param name="aBuffer">The first source texture's texels, laid out row by row.</param>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bBuffer">The second source texture's texels, laid out row by row.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="cBuffer">The third source texture's texels, laid out row by row.</param>
	/// <param name="cDimensions">The third source texture's width and height, in texels.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output, and how sources smaller than the output are enlarged. Must not refer to a source texture beyond those supplied here.</param>
	/// <param name="destinationBuffer">The buffer to write the combined texels in to, laid out row by row. Must be at least as long as the area reported by <c>GetCombinedTextureDimensions</c>.</param>
	public static void CombineTextures(
		ReadOnlySpan<TexelRgba32> aBuffer, XYPair<int> aDimensions,
		ReadOnlySpan<TexelRgba32> bBuffer, XYPair<int> bDimensions,
		ReadOnlySpan<TexelRgba32> cBuffer, XYPair<int> cDimensions,
		TextureCombinationConfig combinationConfig, Span<TexelRgb24> destinationBuffer) => CombineTextures<TexelRgba32, TexelRgb24, byte>(aBuffer, aDimensions, bBuffer, bDimensions, cBuffer, cDimensions, combinationConfig, destinationBuffer);
	/// <summary>
	/// Combines the channels of 3 textures held in memory in to one.
	/// </summary>
	/// <remarks>
	/// The output takes the largest width and height of any source, so a source smaller than that is enlarged according
	/// to the combination config's scaling strategy.
	/// </remarks>
	/// <param name="aBuffer">The first source texture's texels, laid out row by row.</param>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bBuffer">The second source texture's texels, laid out row by row.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="cBuffer">The third source texture's texels, laid out row by row.</param>
	/// <param name="cDimensions">The third source texture's width and height, in texels.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output, and how sources smaller than the output are enlarged. Must not refer to a source texture beyond those supplied here.</param>
	/// <param name="destinationBuffer">The buffer to write the combined texels in to, laid out row by row. Must be at least as long as the area reported by <c>GetCombinedTextureDimensions</c>.</param>
	public static void CombineTextures(
		ReadOnlySpan<TexelRgb24> aBuffer, XYPair<int> aDimensions,
		ReadOnlySpan<TexelRgb24> bBuffer, XYPair<int> bDimensions,
		ReadOnlySpan<TexelRgb24> cBuffer, XYPair<int> cDimensions,
		TextureCombinationConfig combinationConfig, Span<TexelRgba32> destinationBuffer) => CombineTextures<TexelRgb24, TexelRgba32, byte>(aBuffer, aDimensions, bBuffer, bDimensions, cBuffer, cDimensions, combinationConfig, destinationBuffer);
	/// <summary>
	/// Combines the channels of 3 textures held in memory in to one.
	/// </summary>
	/// <remarks>
	/// This is the general form; the non-generic overloads simply pin the texel types to the two standard ones.
	/// </remarks>
	/// <typeparam name="TIn">The texel type of the source textures.</typeparam>
	/// <typeparam name="TOut">The texel type to write.</typeparam>
	/// <typeparam name="TChannel">The type of each texel channel, common to both.</typeparam>
	/// <param name="aBuffer">The first source texture's texels, laid out row by row.</param>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bBuffer">The second source texture's texels, laid out row by row.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="cBuffer">The third source texture's texels, laid out row by row.</param>
	/// <param name="cDimensions">The third source texture's width and height, in texels.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output, and how sources smaller than the output are enlarged. Must not refer to a source texture beyond those supplied here.</param>
	/// <param name="destinationBuffer">The buffer to write the combined texels in to, laid out row by row. Must be at least as long as the area reported by <c>GetCombinedTextureDimensions</c>.</param>
	public static void CombineTextures<TIn, TOut, TChannel>(
		ReadOnlySpan<TIn> aBuffer, XYPair<int> aDimensions,
		ReadOnlySpan<TIn> bBuffer, XYPair<int> bDimensions,
		ReadOnlySpan<TIn> cBuffer, XYPair<int> cDimensions,
		TextureCombinationConfig combinationConfig, Span<TOut> destinationBuffer
	) where TIn : unmanaged, ITexel<TIn, TChannel> where TOut : unmanaged, ITexel<TOut, TChannel> where TChannel : struct {
		const int NumTexturesBeingCombined = 3;
		combinationConfig.ThrowIfInvalid(NumTexturesBeingCombined);

		var destDimensions = GetCombinedTextureDimensions(aDimensions, bDimensions, cDimensions, out var allDimensionsMatch);
		if (destinationBuffer.Length < destDimensions.Area) {
			throw new ArgumentException(
				$"Destination buffer length needs to be at least {destDimensions.Area} " +
				$"(output combined texture would have dimensions {destDimensions}). " +
				$"Calculate the dimensions of the output texture first using {nameof(GetCombinedTextureDimensions)}.",
				nameof(destinationBuffer)
			);
		}

		Span<TIn> localSampleBuffer = stackalloc TIn[NumTexturesBeingCombined];

		var aDimensionsMatchDest = aDimensions == destDimensions;
		var bDimensionsMatchDest = bDimensions == destDimensions;
		var cDimensionsMatchDest = cDimensions == destDimensions;
		var aIsSingleTexel = aDimensions == XYPair<int>.One;
		var bIsSingleTexel = bDimensions == XYPair<int>.One;
		var cIsSingleTexel = cDimensions == XYPair<int>.One;
		var canSkipRescaling = allDimensionsMatch || ((aDimensionsMatchDest || aIsSingleTexel) && (bDimensionsMatchDest || bIsSingleTexel) && (cDimensionsMatchDest || cIsSingleTexel));

		if (!canSkipRescaling) {
			var aCentralizingOffset = CalculateCentralizingOffsetForCenterClampSampling(aDimensions, destDimensions);
			var bCentralizingOffset = CalculateCentralizingOffsetForCenterClampSampling(bDimensions, destDimensions);
			var cCentralizingOffset = CalculateCentralizingOffsetForCenterClampSampling(cDimensions, destDimensions);
			for (var y = 0; y < destDimensions.Y; ++y) {
				for (var x = 0; x < destDimensions.X; ++x) {
					localSampleBuffer[0] = CalculateUpwardRescaledValue(x, y, aBuffer, aDimensions, destDimensions, aCentralizingOffset, combinationConfig.ScalingStrategy);
					localSampleBuffer[1] = CalculateUpwardRescaledValue(x, y, bBuffer, bDimensions, destDimensions, bCentralizingOffset, combinationConfig.ScalingStrategy);
					localSampleBuffer[2] = CalculateUpwardRescaledValue(x, y, cBuffer, cDimensions, destDimensions, cCentralizingOffset, combinationConfig.ScalingStrategy);
					destinationBuffer[destDimensions.Index(x, y)] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
			}
			return;
		}
		
		var area = destDimensions.Area;
		switch (aDimensionsMatchDest, bDimensionsMatchDest, cDimensionsMatchDest) {
			case (true, true, true): {
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[0] = aBuffer[i];
					localSampleBuffer[1] = bBuffer[i];
					localSampleBuffer[2] = cBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			case (false, true, true): {
				localSampleBuffer[0] = aBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[1] = bBuffer[i];
					localSampleBuffer[2] = cBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			case (true, false, true): {
				localSampleBuffer[1] = bBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[0] = aBuffer[i];
					localSampleBuffer[2] = cBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			case (false, false, true): {
				localSampleBuffer[0] = aBuffer[0];
				localSampleBuffer[1] = bBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[2] = cBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			case (true, true, false): {
				localSampleBuffer[2] = cBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[0] = aBuffer[i];
					localSampleBuffer[1] = bBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			case (false, true, false): {
				localSampleBuffer[0] = aBuffer[0];
				localSampleBuffer[2] = cBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[1] = bBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			default: {
				localSampleBuffer[1] = bBuffer[0];
				localSampleBuffer[2] = cBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[0] = aBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
		}
	}

	/// <summary>
	/// Combines the channels of 4 textures held in memory in to one.
	/// </summary>
	/// <remarks>
	/// The output takes the largest width and height of any source, so a source smaller than that is enlarged according
	/// to the combination config's scaling strategy.
	/// </remarks>
	/// <param name="aBuffer">The first source texture's texels, laid out row by row.</param>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bBuffer">The second source texture's texels, laid out row by row.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="cBuffer">The third source texture's texels, laid out row by row.</param>
	/// <param name="cDimensions">The third source texture's width and height, in texels.</param>
	/// <param name="dBuffer">The fourth source texture's texels, laid out row by row.</param>
	/// <param name="dDimensions">The fourth source texture's width and height, in texels.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output, and how sources smaller than the output are enlarged. Must not refer to a source texture beyond those supplied here.</param>
	/// <param name="destinationBuffer">The buffer to write the combined texels in to, laid out row by row. Must be at least as long as the area reported by <c>GetCombinedTextureDimensions</c>.</param>
	public static void CombineTextures(
		ReadOnlySpan<TexelRgba32> aBuffer, XYPair<int> aDimensions,
		ReadOnlySpan<TexelRgba32> bBuffer, XYPair<int> bDimensions,
		ReadOnlySpan<TexelRgba32> cBuffer, XYPair<int> cDimensions,
		ReadOnlySpan<TexelRgba32> dBuffer, XYPair<int> dDimensions,
		TextureCombinationConfig combinationConfig, Span<TexelRgba32> destinationBuffer) => CombineTextures<TexelRgba32, TexelRgba32, byte>(aBuffer, aDimensions, bBuffer, bDimensions, cBuffer, cDimensions, dBuffer, dDimensions, combinationConfig, destinationBuffer);
	/// <summary>
	/// Combines the channels of 4 textures held in memory in to one.
	/// </summary>
	/// <remarks>
	/// The output takes the largest width and height of any source, so a source smaller than that is enlarged according
	/// to the combination config's scaling strategy.
	/// </remarks>
	/// <param name="aBuffer">The first source texture's texels, laid out row by row.</param>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bBuffer">The second source texture's texels, laid out row by row.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="cBuffer">The third source texture's texels, laid out row by row.</param>
	/// <param name="cDimensions">The third source texture's width and height, in texels.</param>
	/// <param name="dBuffer">The fourth source texture's texels, laid out row by row.</param>
	/// <param name="dDimensions">The fourth source texture's width and height, in texels.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output, and how sources smaller than the output are enlarged. Must not refer to a source texture beyond those supplied here.</param>
	/// <param name="destinationBuffer">The buffer to write the combined texels in to, laid out row by row. Must be at least as long as the area reported by <c>GetCombinedTextureDimensions</c>.</param>
	public static void CombineTextures(
		ReadOnlySpan<TexelRgb24> aBuffer, XYPair<int> aDimensions,
		ReadOnlySpan<TexelRgb24> bBuffer, XYPair<int> bDimensions,
		ReadOnlySpan<TexelRgb24> cBuffer, XYPair<int> cDimensions,
		ReadOnlySpan<TexelRgb24> dBuffer, XYPair<int> dDimensions,
		TextureCombinationConfig combinationConfig, Span<TexelRgb24> destinationBuffer) => CombineTextures<TexelRgb24, TexelRgb24, byte>(aBuffer, aDimensions, bBuffer, bDimensions, cBuffer, cDimensions, dBuffer, dDimensions, combinationConfig, destinationBuffer);
	/// <summary>
	/// Combines the channels of 4 textures held in memory in to one.
	/// </summary>
	/// <remarks>
	/// The output takes the largest width and height of any source, so a source smaller than that is enlarged according
	/// to the combination config's scaling strategy.
	/// </remarks>
	/// <param name="aBuffer">The first source texture's texels, laid out row by row.</param>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bBuffer">The second source texture's texels, laid out row by row.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="cBuffer">The third source texture's texels, laid out row by row.</param>
	/// <param name="cDimensions">The third source texture's width and height, in texels.</param>
	/// <param name="dBuffer">The fourth source texture's texels, laid out row by row.</param>
	/// <param name="dDimensions">The fourth source texture's width and height, in texels.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output, and how sources smaller than the output are enlarged. Must not refer to a source texture beyond those supplied here.</param>
	/// <param name="destinationBuffer">The buffer to write the combined texels in to, laid out row by row. Must be at least as long as the area reported by <c>GetCombinedTextureDimensions</c>.</param>
	public static void CombineTextures(
		ReadOnlySpan<TexelRgba32> aBuffer, XYPair<int> aDimensions,
		ReadOnlySpan<TexelRgba32> bBuffer, XYPair<int> bDimensions,
		ReadOnlySpan<TexelRgba32> cBuffer, XYPair<int> cDimensions,
		ReadOnlySpan<TexelRgba32> dBuffer, XYPair<int> dDimensions,
		TextureCombinationConfig combinationConfig, Span<TexelRgb24> destinationBuffer) => CombineTextures<TexelRgba32, TexelRgb24, byte>(aBuffer, aDimensions, bBuffer, bDimensions, cBuffer, cDimensions, dBuffer, dDimensions, combinationConfig, destinationBuffer);
	/// <summary>
	/// Combines the channels of 4 textures held in memory in to one.
	/// </summary>
	/// <remarks>
	/// The output takes the largest width and height of any source, so a source smaller than that is enlarged according
	/// to the combination config's scaling strategy.
	/// </remarks>
	/// <param name="aBuffer">The first source texture's texels, laid out row by row.</param>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bBuffer">The second source texture's texels, laid out row by row.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="cBuffer">The third source texture's texels, laid out row by row.</param>
	/// <param name="cDimensions">The third source texture's width and height, in texels.</param>
	/// <param name="dBuffer">The fourth source texture's texels, laid out row by row.</param>
	/// <param name="dDimensions">The fourth source texture's width and height, in texels.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output, and how sources smaller than the output are enlarged. Must not refer to a source texture beyond those supplied here.</param>
	/// <param name="destinationBuffer">The buffer to write the combined texels in to, laid out row by row. Must be at least as long as the area reported by <c>GetCombinedTextureDimensions</c>.</param>
	public static void CombineTextures(
		ReadOnlySpan<TexelRgb24> aBuffer, XYPair<int> aDimensions,
		ReadOnlySpan<TexelRgb24> bBuffer, XYPair<int> bDimensions,
		ReadOnlySpan<TexelRgb24> cBuffer, XYPair<int> cDimensions,
		ReadOnlySpan<TexelRgb24> dBuffer, XYPair<int> dDimensions,
		TextureCombinationConfig combinationConfig, Span<TexelRgba32> destinationBuffer) => CombineTextures<TexelRgb24, TexelRgba32, byte>(aBuffer, aDimensions, bBuffer, bDimensions, cBuffer, cDimensions, dBuffer, dDimensions, combinationConfig, destinationBuffer);
	/// <summary>
	/// Combines the channels of 4 textures held in memory in to one.
	/// </summary>
	/// <remarks>
	/// This is the general form; the non-generic overloads simply pin the texel types to the two standard ones.
	/// </remarks>
	/// <typeparam name="TIn">The texel type of the source textures.</typeparam>
	/// <typeparam name="TOut">The texel type to write.</typeparam>
	/// <typeparam name="TChannel">The type of each texel channel, common to both.</typeparam>
	/// <param name="aBuffer">The first source texture's texels, laid out row by row.</param>
	/// <param name="aDimensions">The first source texture's width and height, in texels.</param>
	/// <param name="bBuffer">The second source texture's texels, laid out row by row.</param>
	/// <param name="bDimensions">The second source texture's width and height, in texels.</param>
	/// <param name="cBuffer">The third source texture's texels, laid out row by row.</param>
	/// <param name="cDimensions">The third source texture's width and height, in texels.</param>
	/// <param name="dBuffer">The fourth source texture's texels, laid out row by row.</param>
	/// <param name="dDimensions">The fourth source texture's width and height, in texels.</param>
	/// <param name="combinationConfig">Which source channel supplies each channel of the output, and how sources smaller than the output are enlarged. Must not refer to a source texture beyond those supplied here.</param>
	/// <param name="destinationBuffer">The buffer to write the combined texels in to, laid out row by row. Must be at least as long as the area reported by <c>GetCombinedTextureDimensions</c>.</param>
	public static void CombineTextures<TIn, TOut, TChannel>(
		ReadOnlySpan<TIn> aBuffer, XYPair<int> aDimensions,
		ReadOnlySpan<TIn> bBuffer, XYPair<int> bDimensions,
		ReadOnlySpan<TIn> cBuffer, XYPair<int> cDimensions,
		ReadOnlySpan<TIn> dBuffer, XYPair<int> dDimensions,
		TextureCombinationConfig combinationConfig, Span<TOut> destinationBuffer
	) where TIn : unmanaged, ITexel<TIn, TChannel> where TOut : unmanaged, ITexel<TOut, TChannel> where TChannel : struct {
		const int NumTexturesBeingCombined = 4;
		combinationConfig.ThrowIfInvalid(NumTexturesBeingCombined);

		var destDimensions = GetCombinedTextureDimensions(aDimensions, bDimensions, cDimensions, dDimensions, out var allDimensionsMatch);
		if (destinationBuffer.Length < destDimensions.Area) {
			throw new ArgumentException(
				$"Destination buffer length needs to be at least {destDimensions.Area} " +
				$"(output combined texture would have dimensions {destDimensions}). " +
				$"Calculate the dimensions of the output texture first using {nameof(GetCombinedTextureDimensions)}.",
				nameof(destinationBuffer)
			);
		}

		Span<TIn> localSampleBuffer = stackalloc TIn[NumTexturesBeingCombined];

		var aDimensionsMatchDest = aDimensions == destDimensions;
		var bDimensionsMatchDest = bDimensions == destDimensions;
		var cDimensionsMatchDest = cDimensions == destDimensions;
		var dDimensionsMatchDest = dDimensions == destDimensions;
		var aIsSingleTexel = aDimensions == XYPair<int>.One;
		var bIsSingleTexel = bDimensions == XYPair<int>.One;
		var cIsSingleTexel = cDimensions == XYPair<int>.One;
		var dIsSingleTexel = dDimensions == XYPair<int>.One;
		var canSkipRescaling = allDimensionsMatch || ((aDimensionsMatchDest || aIsSingleTexel) && (bDimensionsMatchDest || bIsSingleTexel) && (cDimensionsMatchDest || cIsSingleTexel) && (dDimensionsMatchDest || dIsSingleTexel));

		if (!canSkipRescaling) {
			var aCentralizingOffset = CalculateCentralizingOffsetForCenterClampSampling(aDimensions, destDimensions);
			var bCentralizingOffset = CalculateCentralizingOffsetForCenterClampSampling(bDimensions, destDimensions);
			var cCentralizingOffset = CalculateCentralizingOffsetForCenterClampSampling(cDimensions, destDimensions);
			var dCentralizingOffset = CalculateCentralizingOffsetForCenterClampSampling(dDimensions, destDimensions);
			for (var y = 0; y < destDimensions.Y; ++y) {
				for (var x = 0; x < destDimensions.X; ++x) {
					localSampleBuffer[0] = CalculateUpwardRescaledValue(x, y, aBuffer, aDimensions, destDimensions, aCentralizingOffset, combinationConfig.ScalingStrategy);
					localSampleBuffer[1] = CalculateUpwardRescaledValue(x, y, bBuffer, bDimensions, destDimensions, bCentralizingOffset, combinationConfig.ScalingStrategy);
					localSampleBuffer[2] = CalculateUpwardRescaledValue(x, y, cBuffer, cDimensions, destDimensions, cCentralizingOffset, combinationConfig.ScalingStrategy);
					localSampleBuffer[3] = CalculateUpwardRescaledValue(x, y, dBuffer, dDimensions, destDimensions, dCentralizingOffset, combinationConfig.ScalingStrategy);
					destinationBuffer[destDimensions.Index(x, y)] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
			}
			return;
		}
		
		var area = destDimensions.Area;
		switch (aDimensionsMatchDest, bDimensionsMatchDest, cDimensionsMatchDest, dDimensionsMatchDest) {
			case (true, true, true, true): {
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[0] = aBuffer[i];
					localSampleBuffer[1] = bBuffer[i];
					localSampleBuffer[2] = cBuffer[i];
					localSampleBuffer[3] = dBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			case (false, true, true, true): {
				localSampleBuffer[0] = aBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[1] = bBuffer[i];
					localSampleBuffer[2] = cBuffer[i];
					localSampleBuffer[3] = dBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			case (true, false, true, true): {
				localSampleBuffer[1] = bBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[0] = aBuffer[i];
					localSampleBuffer[2] = cBuffer[i];
					localSampleBuffer[3] = dBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			case (false, false, true, true): {
				localSampleBuffer[0] = aBuffer[0];
				localSampleBuffer[1] = bBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[2] = cBuffer[i];
					localSampleBuffer[3] = dBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			case (true, true, false, true): {
				localSampleBuffer[2] = cBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[0] = aBuffer[i];
					localSampleBuffer[1] = bBuffer[i];
					localSampleBuffer[3] = dBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			case (false, true, false, true): {
				localSampleBuffer[0] = aBuffer[0];
				localSampleBuffer[2] = cBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[1] = bBuffer[i];
					localSampleBuffer[3] = dBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			case (true, false, false, true): {
				localSampleBuffer[1] = bBuffer[0];
				localSampleBuffer[2] = cBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[0] = aBuffer[i];
					localSampleBuffer[3] = dBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			case (false, false, false, true): {
				localSampleBuffer[0] = aBuffer[0];
				localSampleBuffer[1] = bBuffer[0];
				localSampleBuffer[2] = cBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[3] = dBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			case (true, true, true, false): {
				localSampleBuffer[3] = dBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[0] = aBuffer[i];
					localSampleBuffer[1] = bBuffer[i];
					localSampleBuffer[2] = cBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			case (false, true, true, false): {
				localSampleBuffer[0] = aBuffer[0];
				localSampleBuffer[3] = dBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[1] = bBuffer[i];
					localSampleBuffer[2] = cBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			case (true, false, true, false): {
				localSampleBuffer[1] = bBuffer[0];
				localSampleBuffer[3] = dBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[0] = aBuffer[i];
					localSampleBuffer[2] = cBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			case (false, false, true, false): {
				localSampleBuffer[0] = aBuffer[0];
				localSampleBuffer[1] = bBuffer[0];
				localSampleBuffer[3] = dBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[2] = cBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			case (true, true, false, false): {
				localSampleBuffer[2] = cBuffer[0];
				localSampleBuffer[3] = dBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[0] = aBuffer[i];
					localSampleBuffer[1] = bBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			case (false, true, false, false): {
				localSampleBuffer[0] = aBuffer[0];
				localSampleBuffer[2] = cBuffer[0];
				localSampleBuffer[3] = dBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[1] = bBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
			default: {
				localSampleBuffer[1] = bBuffer[0];
				localSampleBuffer[2] = cBuffer[0];
				localSampleBuffer[3] = dBuffer[0];
				for (var i = 0; i < area; ++i) {
					localSampleBuffer[0] = aBuffer[i];
					destinationBuffer[i] = combinationConfig.SelectTexel<TIn, TOut, TChannel>(localSampleBuffer);
				}
				break;
			}
		}
	}
	#endregion
	
	#region Rescaling Algorithms
	internal static T CalculateUpwardRescaledValue<T>(
		int x, int y,
		ReadOnlySpan<T> sourceBuffer,
		XYPair<int> sourceDimensions,
		XYPair<int> destinationDimensions,
		XYPair<int> centralizingOffset,
		TextureCombinationScalingStrategy scalingStrategy
	) where T : unmanaged, IBlendable<T> {
		// Perf + image stability optimizations:
		// These following two lines gives us an early exit where it's either a perf improvement and/or
		//	applying a rescaling algorithm will actually give a worse result due to FP inaccuracy (e.g.
		//	bilinear filtering for matching texture dimensions is strictly worse than just returning the
		//	original texel).  
		if (sourceDimensions == destinationDimensions) return sourceBuffer[sourceDimensions.Index(x, y)];
		if (sourceDimensions == XYPair<int>.One) return sourceBuffer[0];
		
		return scalingStrategy switch {
			TextureCombinationScalingStrategy.PixelUpscale => SampleResizedBufferNearestNeighbor(x, y, sourceBuffer, sourceDimensions, destinationDimensions),
			TextureCombinationScalingStrategy.BilinearUpscale => SampleResizedBufferBilinear(x, y, sourceBuffer, sourceDimensions, destinationDimensions),
			TextureCombinationScalingStrategy.RepeatingTile => SampleResizedBufferWrapped(x, y, sourceBuffer, sourceDimensions),
			TextureCombinationScalingStrategy.ExtendEdges => SampleResizedBufferCenteredClamped(x, y, sourceBuffer, sourceDimensions, centralizingOffset),
			_ => throw new ArgumentOutOfRangeException(nameof(scalingStrategy), scalingStrategy, $"Unknown {nameof(TextureCombinationScalingStrategy)} value.")
		};
	}

	static T SampleResizedBufferWrapped<T>(int x, int y, ReadOnlySpan<T> sourceBuffer, XYPair<int> sourceDimensions) {
		return sourceBuffer[sourceDimensions.X * (y % sourceDimensions.Y) + (x % sourceDimensions.X)];
	}
	
	internal static XYPair<int> CalculateCentralizingOffsetForCenterClampSampling(XYPair<int> sourceDimensions, XYPair<int> destDimensions) {
		return (destDimensions - sourceDimensions) / 2; 
	}
	static T SampleResizedBufferCenteredClamped<T>(int x, int y, ReadOnlySpan<T> sourceBuffer, XYPair<int> sourceDimensions, XYPair<int> centralizingOffset) {
		var resultIndex = sourceDimensions.Index(
			Math.Clamp(x - centralizingOffset.X, 0, sourceDimensions.X - 1),
			Math.Clamp(y - centralizingOffset.Y, 0, sourceDimensions.Y - 1)
		);
		return sourceBuffer[resultIndex];
	}

	static T SampleResizedBufferNearestNeighbor<T>(int x, int y, ReadOnlySpan<T> sourceBuffer, XYPair<int> sourceDimensions, XYPair<int> destinationDimensions) {
		var resultIndex = sourceDimensions.Index(
			Math.Clamp((int) ((x + 0.5f) * sourceDimensions.X / destinationDimensions.X), 0, sourceDimensions.X - 1),
			Math.Clamp((int) ((y + 0.5f) * sourceDimensions.Y / destinationDimensions.Y), 0, sourceDimensions.Y - 1)
		);
		return sourceBuffer[resultIndex];
	}

	static T SampleResizedBufferBilinear<T>(int x, int y, ReadOnlySpan<T> sourceBuffer, XYPair<int> sourceDimensions, XYPair<int> destinationDimensions) where T : IBlendable<T> {
		var sourceCoordsReal = new XYPair<float>(
			(x + 0.5f) * sourceDimensions.X / destinationDimensions.X - 0.5f,
			(y + 0.5f) * sourceDimensions.Y / destinationDimensions.Y - 0.5f
		);
		var roundedDownCoords = new XYPair<int>((int) MathF.Floor(sourceCoordsReal.X), (int) MathF.Floor(sourceCoordsReal.Y));
		var roundedUpCoords = new XYPair<int>(roundedDownCoords.X + 1, roundedDownCoords.Y + 1);

		var xDist = sourceCoordsReal.X - roundedDownCoords.X;
		var yDist = sourceCoordsReal.Y - roundedDownCoords.Y;
		
		var bottom = T.Blend(
			sourceBuffer[sourceDimensions.IndexClamped(roundedDownCoords.X, roundedDownCoords.Y)],	
			sourceBuffer[sourceDimensions.IndexClamped(roundedUpCoords.X, roundedDownCoords.Y)],
			xDist
		);
		var top = T.Blend(
			sourceBuffer[sourceDimensions.IndexClamped(roundedDownCoords.X, roundedUpCoords.Y)],	
			sourceBuffer[sourceDimensions.IndexClamped(roundedUpCoords.X, roundedUpCoords.Y)],
			xDist
		);
		return T.Blend(
			bottom,
			top,
			yDist
		);
	}
	#endregion
}