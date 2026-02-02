// Created on 2025-11-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Assets.Materials;

public static partial class TextureUtils {
	public static void FlipTexture<TTexel>(Span<TTexel> buffer, XYPair<int> dimensions, bool aroundVerticalCentre, bool aroundHorizontalCentre) where TTexel : unmanaged, ITexel<TTexel> {
		ProcessTexture(buffer, dimensions, TextureProcessingConfig.Flip(aroundVerticalCentre, aroundHorizontalCentre));
	}
	public static void NegateTexture<TTexel>(Span<TTexel> buffer, XYPair<int> dimensions, bool includeRedChannel = true, bool includeGreenChannel = true, bool includeBlueChannel = true, bool includeAlphaChannel = true) where TTexel : unmanaged, ITexel<TTexel> {
		ProcessTexture(buffer, dimensions, TextureProcessingConfig.Invert(includeRedChannel, includeGreenChannel, includeBlueChannel, includeAlphaChannel));
	}
	public static void SwizzleTexture<TTexel>(Span<TTexel> buffer, XYPair<int> dimensions, ColorChannel redSource = ColorChannel.R, ColorChannel greenSource = ColorChannel.G, ColorChannel blueSource = ColorChannel.B, ColorChannel alphaSource = ColorChannel.A) where TTexel : unmanaged, ITexel<TTexel> {
		ProcessTexture(buffer, dimensions, TextureProcessingConfig.Swizzle(redSource, greenSource, blueSource, alphaSource));
	}

	public static void ProcessTexture<TTexel>(Span<TTexel> buffer, XYPair<int> dimensions, in TextureProcessingConfig config) where TTexel : unmanaged, ITexel<TTexel> {
		const int MaxTextureWidthForStackRowSwap = 65_536;
		config.ThrowIfInvalid();
		if (!config.RequiresProcessing) return;

		var width = dimensions.X;
		var height = dimensions.Y;

		var texelCount = width * height;

		if (texelCount > buffer.Length) {
			throw new ArgumentException(
				$"Texture dimensions are {width}x{height}, requiring a texel span of length {texelCount} or greater, " +
				$"but actual span length was {buffer.Length}.",
				nameof(buffer)
			);
		}

		if (config.FlipX) {
			for (var y = 0; y < height; ++y) {
				var row = buffer[(y * width)..((y + 1) * width)];
				for (var x = 0; x < width / 2; ++x) {
					(row[x], row[^(x + 1)]) = (row[^(x + 1)], row[x]);
				}
			}
		}
		if (config.FlipY) {
			var rowSwapSpace = width > MaxTextureWidthForStackRowSwap ? new TTexel[width] : stackalloc TTexel[width];
			for (var y = 0; y < height / 2; ++y) {
				var lowerRow = buffer[(y * width)..((y + 1) * width)];
				var upperRow = buffer[((height - (y + 1)) * width)..((height - y) * width)];
				lowerRow.CopyTo(rowSwapSpace);
				upperRow.CopyTo(lowerRow);
				rowSwapSpace.CopyTo(upperRow);
			}
		}

		var shouldSwizzle = config.XRedFinalOutputSource != ColorChannel.R
						|| config.YGreenFinalOutputSource != ColorChannel.G
						|| config.ZBlueFinalOutputSource != ColorChannel.B
						|| config.WAlphaFinalOutputSource != ColorChannel.A;
		var shouldPreprocess = config.InvertXRedChannel || config.InvertYGreenChannel || config.InvertZBlueChannel || config.InvertWAlphaChannel || shouldSwizzle;
		if (!shouldPreprocess) return;

		for (var i = 0; i < texelCount; ++i) {
			if (config.InvertXRedChannel) buffer[i] = buffer[i].WithInvertedChannelIfPresent(0);
			if (config.InvertYGreenChannel) buffer[i] = buffer[i].WithInvertedChannelIfPresent(1);
			if (config.InvertZBlueChannel) buffer[i] = buffer[i].WithInvertedChannelIfPresent(2);
			if (config.InvertWAlphaChannel) buffer[i] = buffer[i].WithInvertedChannelIfPresent(3);
			if (shouldSwizzle) {
				buffer[i] = buffer[i].SwizzlePresentChannels(
					config.XRedFinalOutputSource,
					config.YGreenFinalOutputSource,
					config.ZBlueFinalOutputSource,
					config.WAlphaFinalOutputSource
				);
			}
		}
	}
	
	public static void Convert<TTexelIn, TTexelOut>(ReadOnlySpan<TTexelIn> inputBuffer, Span<TTexelOut> outputBuffer) where TTexelOut : unmanaged, IConversionSupplyingTexel<TTexelOut, TTexelIn> {
		if (outputBuffer.Length < inputBuffer.Length) throw new ArgumentException($"Output buffer length ({outputBuffer.Length}) must be at least as high as input buffer length ({inputBuffer.Length}).", nameof(outputBuffer));
		for (var i = 0; i < inputBuffer.Length; ++i) {
			outputBuffer[i] = TTexelOut.ConvertFrom(inputBuffer[i]);
		}
	}
}