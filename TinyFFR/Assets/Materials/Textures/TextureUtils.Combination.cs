// Created on 2026-02-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using static Egodystonic.TinyFFR.Assets.Materials.TextureCombinationSourceTexture;
using static Egodystonic.TinyFFR.ColorChannel;

namespace Egodystonic.TinyFFR.Assets.Materials;

public enum TextureCombinationSourceTexture {
	TextureA,
	TextureB,
	TextureC,
	TextureD
}
public readonly record struct TextureCombinationSource(TextureCombinationSourceTexture SourceTexture, ColorChannel SourceChannel) {
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
public readonly record struct TextureCombinationConfig(TextureCombinationSource OutputTextureXRedChannelSource, TextureCombinationSource OutputTextureYGreenChannelSource, TextureCombinationSource OutputTextureZBlueChannelSource, TextureCombinationSource? OutputTextureWAlphaChannelSource = null) {
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
				'r' or 'x' => R,
				'g' or 'y' => G,
				'b' or 'z' => B,
				'a' or 'w' => A,
				_ => throw new ArgumentException($"Character '{twoChars[1]}' was expected to be one of 'r', 'g', 'b', 'a', 'x', 'y', 'z', 'w' (to denote a source channel).")
			}
		);
	}

	public TextureCombinationConfig(ReadOnlySpan<char> selectionString) : this(
		ExtractFromString(selectionString[0..2]),
		ExtractFromString(selectionString[2..4]),
		ExtractFromString(selectionString[4..6]),
		selectionString.Length >= 8 ? ExtractFromString(selectionString[6..8]) : null
	) { }
	public TextureCombinationConfig(TextureCombinationSourceTexture xRedSourceTex, ColorChannel xRedSourceChannel, TextureCombinationSourceTexture yGreenSourceTex, ColorChannel yGreenSourceChannel, TextureCombinationSourceTexture zBlueSourceTex, ColorChannel zBlueSourceChannel)
		: this(new TextureCombinationSource(xRedSourceTex, xRedSourceChannel), new TextureCombinationSource(yGreenSourceTex, yGreenSourceChannel), new TextureCombinationSource(zBlueSourceTex, zBlueSourceChannel)) { }
	public TextureCombinationConfig(TextureCombinationSourceTexture xRedSourceTex, ColorChannel xRedSourceChannel, TextureCombinationSourceTexture yGreenSourceTex, ColorChannel yGreenSourceChannel, TextureCombinationSourceTexture zBlueSourceTex, ColorChannel zBlueSourceChannel, TextureCombinationSourceTexture wAlphaSourceTex, ColorChannel wAlphaSourceChannel)
		: this(new TextureCombinationSource(xRedSourceTex, xRedSourceChannel), new TextureCombinationSource(yGreenSourceTex, yGreenSourceChannel), new TextureCombinationSource(zBlueSourceTex, zBlueSourceChannel), new TextureCombinationSource(wAlphaSourceTex, wAlphaSourceChannel)) { }


	internal TTexel SelectTexel<TTexel, TChannel>(ReadOnlySpan<TTexel> samples) where TTexel : unmanaged, ITexel<TTexel, TChannel> where TChannel : struct {
		return TTexel.ConstructFromIgnoringExcessArguments(
			OutputTextureXRedChannelSource.SelectTexelChannel<TTexel, TChannel>(samples) ?? TTexel.MinChannelValue,
			OutputTextureYGreenChannelSource.SelectTexelChannel<TTexel, TChannel>(samples) ?? TTexel.MinChannelValue,
			OutputTextureZBlueChannelSource.SelectTexelChannel<TTexel, TChannel>(samples) ?? TTexel.MinChannelValue,
			OutputTextureWAlphaChannelSource?.SelectTexelChannel<TTexel, TChannel>(samples) ?? TTexel.MaxChannelValue
		);
	}

	internal void ThrowIfInvalid(int numTexturesBeingCombined) {
		OutputTextureXRedChannelSource.ThrowIfInvalid(numTexturesBeingCombined);
		OutputTextureYGreenChannelSource.ThrowIfInvalid(numTexturesBeingCombined);
		OutputTextureZBlueChannelSource.ThrowIfInvalid(numTexturesBeingCombined);
		OutputTextureWAlphaChannelSource?.ThrowIfInvalid(numTexturesBeingCombined);
	}
}

public static partial class TextureUtils {
	public static XYPair<int> GetCombinedTextureDimensions(XYPair<int> aDimensions, XYPair<int> bDimensions, XYPair<int>? cDimensions, XYPair<int>? dDimensions, out bool allDimensionsMatched) {
		cDimensions ??= aDimensions;
		dDimensions ??= bDimensions;
		allDimensionsMatched = aDimensions == bDimensions && bDimensions == cDimensions && cDimensions == dDimensions;

		if (allDimensionsMatched) return aDimensions;

		return new(
			Int32.Max(Int32.Max(Int32.Max(aDimensions.X, bDimensions.X), cDimensions.Value.X), dDimensions.Value.X),
			Int32.Max(Int32.Max(Int32.Max(aDimensions.Y, bDimensions.Y), cDimensions.Value.Y), dDimensions.Value.Y)
		);
	}

	static int CalculateWrappedIndexForCombination(XYPair<int> dimensions, int x, int y) => dimensions.X * (y % dimensions.Y) + (x % dimensions.X);

	public static void CombineTextures<TTexel, TChannel>(
		ReadOnlySpan<TTexel> aBuffer, XYPair<int> aDimensions,
		ReadOnlySpan<TTexel> bBuffer, XYPair<int> bDimensions,
		TextureCombinationConfig combinationConfig, Span<TTexel> destinationBuffer
	) where TTexel : unmanaged, ITexel<TTexel, TChannel> where TChannel : struct {
		const int NumTexturesBeingCombined = 2;
		combinationConfig.ThrowIfInvalid(NumTexturesBeingCombined);

		var destDimensions = GetCombinedTextureDimensions(aDimensions, bDimensions, null, null, out var allDimensionsMatch);
		if (destinationBuffer.Length < destDimensions.Area) {
			throw new ArgumentException(
				$"Destination buffer length needs to be at least {destDimensions.Area} " +
				$"(output combined texture would have dimensions {destDimensions}). " +
				$"Calculate the dimensions of the output texture first using {nameof(ReadCombinedTextureMetadata)}.",
				nameof(destinationBuffer)
			);
		}

		Span<TTexel> localSampleBuffer = stackalloc TTexel[NumTexturesBeingCombined];

		if (allDimensionsMatch) {
			for (var i = 0; i < destDimensions.Area; ++i) {
				localSampleBuffer[0] = aBuffer[i];
				localSampleBuffer[1] = bBuffer[i];
				destinationBuffer[i] = combinationConfig.SelectTexel<TTexel, TChannel>(localSampleBuffer);
			}
		}
		else {
			for (var x = 0; x < destDimensions.X; ++x) {
				for (var y = 0; y < destDimensions.Y; ++y) {
					localSampleBuffer[0] = aBuffer[CalculateWrappedIndexForCombination(aMetadata.Dimensions, x, y)];
					localSampleBuffer[1] = bBuffer[CalculateWrappedIndexForCombination(bMetadata.Dimensions, x, y)];
					destinationBuffer[destDimensions.X * y + x] = TTexel.ConvertFrom(combinationConfig.SelectTexel(localSampleBuffer));
				}
			}
		}
	}
}