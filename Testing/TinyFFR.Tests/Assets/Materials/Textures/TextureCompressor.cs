// Created on 2026-08-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials.Local;

namespace Egodystonic.TinyFFR.Assets.Materials;

[TestFixture]
class TextureCompressorTest {
	const int TestDimension = 64;

	static TexelRgba32[] CreateTestImage() {
		var result = new TexelRgba32[TestDimension * TestDimension];
		for (var y = 0; y < TestDimension; ++y) {
			for (var x = 0; x < TestDimension; ++x) {
				var r = (byte) (x * 4);
				var g = (byte) (y * 4);
				var b = (byte) (((x / 8) + (y / 8)) % 2 == 0 ? 220 : 40);
				var a = (byte) (128 + ((x + y) % 128));
				result[y * TestDimension + x] = new TexelRgba32(r, g, b, a);
			}
		}
		return result;
	}

	static double RoundTrip(TextureCompressionFormat format, Quality quality, bool includeBlue, bool includeAlpha) {
		var original = CreateTestImage();
		var dimensions = new XYPair<int>(TestDimension, TestDimension);

		var compressed = new byte[TextureCompressor.GetCompressedSizeBytes(dimensions, format, includeMipChain: false)];
		LocalTextureCompressor.CompressWithMipChain<TexelRgba32>(original, dimensions, format, quality, isLinearColorspace: true, levelCount: 1, compressed);

		var decoded = new TexelRgba32[original.Length];
		LocalTextureCompressor.DecompressSingleLevel(compressed, dimensions, format, decoded);

		return CalculateTexturePeakSnr(original, decoded, includeBlue, includeAlpha);
	}

	[Test]
	public void ShouldRoundTripBc7WithHighFidelity() {
		var psnr = RoundTrip(TextureCompressionFormat.Bc7Linear, Quality.Standard, includeBlue: true, includeAlpha: true);
		Assert.IsTrue(psnr > 35d, $"BC7 round trip PSNR was only {psnr:N2} dB.");
	}

	[Test]
	public void ShouldRoundTripBc7SrgbWithHighFidelity() {
		var psnr = RoundTrip(TextureCompressionFormat.Bc7Srgb, Quality.Standard, includeBlue: true, includeAlpha: true);
		Assert.IsTrue(psnr > 30d, $"BC7 sRGB round trip PSNR was only {psnr:N2} dB.");
	}

	[Test]
	public void ShouldRoundTripBc1WithAcceptableFidelity() {
		var psnr = RoundTrip(TextureCompressionFormat.Bc1Srgb, Quality.VeryLow, includeBlue: true, includeAlpha: false);
		Assert.IsTrue(psnr > 25d, $"BC1 round trip PSNR was only {psnr:N2} dB.");
	}

	[Test]
	public void ShouldRoundTripBc3PreservingAlpha() {
		var psnr = RoundTrip(TextureCompressionFormat.Bc3Srgb, Quality.VeryLow, includeBlue: true, includeAlpha: true);
		Assert.IsTrue(psnr > 25d, $"BC3 round trip PSNR was only {psnr:N2} dB.");
	}

	[Test]
	public void ShouldRoundTripBc5PreservingRedAndGreenChannels() {
		var psnr = RoundTrip(TextureCompressionFormat.Bc5, Quality.Standard, includeBlue: false, includeAlpha: false);
		Assert.IsTrue(psnr > 40d, $"BC5 round trip PSNR was only {psnr:N2} dB.");
	}

	[Test]
	public void ShouldProduceBetterFidelityAtHigherQualityLevels() {
		var lowPsnr = RoundTrip(TextureCompressionFormat.Bc7Linear, Quality.Low, includeBlue: true, includeAlpha: true);
		var highPsnr = RoundTrip(TextureCompressionFormat.Bc7Linear, Quality.VeryHigh, includeBlue: true, includeAlpha: true);
		Assert.IsTrue(highPsnr >= lowPsnr - 0.01d, $"Higher quality produced worse fidelity ({highPsnr:N2} dB vs {lowPsnr:N2} dB).");
	}

	[Test]
	public void ShouldCompressEveryMipLevelIntoContiguousBlockStorage() {
		var dimensions = new XYPair<int>(TestDimension, TestDimension);
		var original = CreateTestImage();
		const TextureCompressionFormat Format = TextureCompressionFormat.Bc7Linear;
		var levelCount = TextureUtils.GetMipLevelCount(dimensions);
		var compressed = new byte[TextureCompressor.GetCompressedSizeBytes(dimensions, Format, includeMipChain: true)];

		LocalTextureCompressor.CompressWithMipChain<TexelRgba32>(original, dimensions, Format, Quality.Standard, isLinearColorspace: true, levelCount, compressed);

		var data = new CompressedTextureData(compressed, Format, dimensions, levelCount);
		for (var level = 0; level < levelCount; ++level) {
			var levelDimensions = TextureUtils.GetMipLevelDimensions(dimensions, level);
			var decoded = new TexelRgba32[levelDimensions.Area];
			LocalTextureCompressor.DecompressSingleLevel(data.GetLevel(level), levelDimensions, Format, decoded);

			var allZero = true;
			foreach (var texel in decoded) {
				if (texel.R != 0 || texel.G != 0 || texel.B != 0) {
					allZero = false;
					break;
				}
			}
			Assert.IsFalse(allZero, $"Mip level {level} decoded to entirely black, suggesting it was never written.");
		}
	}

	[Test]
	public void ShouldCompressNonMultipleOfFourDimensions() {
		foreach (var dimensions in new[] { new XYPair<int>(5, 7), new XYPair<int>(1, 1), new XYPair<int>(3, 3), new XYPair<int>(9, 2) }) {
			var texels = new TexelRgba32[dimensions.Area];
			for (var i = 0; i < texels.Length; ++i) texels[i] = new TexelRgba32((byte) (i * 7), (byte) (i * 3), 100, 255);

			var compressed = new byte[TextureCompressor.GetCompressedSizeBytes(dimensions, TextureCompressionFormat.Bc7Linear, includeMipChain: false)];
			Assert.DoesNotThrow(() => LocalTextureCompressor.CompressWithMipChain<TexelRgba32>(texels, dimensions, TextureCompressionFormat.Bc7Linear, Quality.Standard, true, 1, compressed));

			var decoded = new TexelRgba32[dimensions.Area];
			Assert.DoesNotThrow(() => LocalTextureCompressor.DecompressSingleLevel(compressed, dimensions, TextureCompressionFormat.Bc7Linear, decoded));
		}
	}

	[Test]
	public void ShouldCompressRgb24SourcesByCoercion() {
		var dimensions = new XYPair<int>(TestDimension, TestDimension);
		var texels = new TexelRgb24[dimensions.Area];
		for (var i = 0; i < texels.Length; ++i) texels[i] = new TexelRgb24((byte) (i % 256), (byte) ((i * 3) % 256), 90);

		var compressed = new byte[TextureCompressor.GetCompressedSizeBytes(dimensions, TextureCompressionFormat.Bc7Linear, includeMipChain: false)];
		Assert.DoesNotThrow(() => LocalTextureCompressor.CompressWithMipChain<TexelRgb24>(texels, dimensions, TextureCompressionFormat.Bc7Linear, Quality.Standard, true, 1, compressed));

		var decoded = new TexelRgba32[dimensions.Area];
		LocalTextureCompressor.DecompressSingleLevel(compressed, dimensions, TextureCompressionFormat.Bc7Linear, decoded);

		var sumSquaredError = 0d;
		for (var i = 0; i < texels.Length; ++i) {
			sumSquaredError += Math.Pow(texels[i].R - decoded[i].R, 2);
			sumSquaredError += Math.Pow(texels[i].G - decoded[i].G, 2);
			sumSquaredError += Math.Pow(texels[i].B - decoded[i].B, 2);
		}
		var meanSquaredError = sumSquaredError / (texels.Length * 3);
		var psnr = meanSquaredError <= 0d ? Double.PositiveInfinity : 10d * Math.Log10(255d * 255d / meanSquaredError);
		Assert.IsTrue(psnr > 30d, $"RGB24 source round trip PSNR was only {psnr:N2} dB.");
	}
	
	[Test]
	public void ShouldCorrectlyCalculateMipLevelCounts() {
		Assert.AreEqual(1, TextureUtils.GetMipLevelCount(new(1, 1)));
		Assert.AreEqual(2, TextureUtils.GetMipLevelCount(new(2, 2)));
		Assert.AreEqual(2, TextureUtils.GetMipLevelCount(new(2, 1)));
		Assert.AreEqual(3, TextureUtils.GetMipLevelCount(new(4, 4)));
		Assert.AreEqual(9, TextureUtils.GetMipLevelCount(new(256, 256)));
		Assert.AreEqual(11, TextureUtils.GetMipLevelCount(new(1024, 1)));
		Assert.AreEqual(11, TextureUtils.GetMipLevelCount(new(1024, 512)));
		Assert.AreEqual(3, TextureUtils.GetMipLevelCount(new(5, 7)));
		Assert.AreEqual(4, TextureUtils.GetMipLevelCount(new(9, 3)));

		Assert.Throws<ArgumentOutOfRangeException>(() => TextureUtils.GetMipLevelCount(new(0, 4)));
		Assert.Throws<ArgumentOutOfRangeException>(() => TextureUtils.GetMipLevelCount(new(4, -1)));
	}

	[Test]
	public void ShouldCorrectlyCalculateMipLevelDimensions() {
		Assert.AreEqual(new XYPair<int>(256, 128), TextureUtils.GetMipLevelDimensions(new(256, 128), 0));
		Assert.AreEqual(new XYPair<int>(128, 64), TextureUtils.GetMipLevelDimensions(new(256, 128), 1));
		Assert.AreEqual(new XYPair<int>(1, 1), TextureUtils.GetMipLevelDimensions(new(256, 128), 8));

		Assert.AreEqual(new XYPair<int>(1, 4), TextureUtils.GetMipLevelDimensions(new(2, 8), 1));
		Assert.AreEqual(new XYPair<int>(1, 2), TextureUtils.GetMipLevelDimensions(new(2, 8), 2));
		Assert.AreEqual(new XYPair<int>(1, 1), TextureUtils.GetMipLevelDimensions(new(2, 8), 3));

		Assert.AreEqual(new XYPair<int>(2, 3), TextureUtils.GetMipLevelDimensions(new(5, 7), 1));
		Assert.AreEqual(new XYPair<int>(1, 1), TextureUtils.GetMipLevelDimensions(new(5, 7), 2));

		Assert.Throws<ArgumentOutOfRangeException>(() => TextureUtils.GetMipLevelDimensions(new(4, 4), -1));
	}

	[Test]
	public void ShouldCorrectlyCalculateBlockCountsAndSizes() {
		Assert.AreEqual(1, TextureCompressor.GetBlockCount(new(1, 1)));
		Assert.AreEqual(1, TextureCompressor.GetBlockCount(new(4, 4)));
		Assert.AreEqual(4, TextureCompressor.GetBlockCount(new(8, 8)));
		Assert.AreEqual(4, TextureCompressor.GetBlockCount(new(5, 5)));
		Assert.AreEqual(6, TextureCompressor.GetBlockCount(new(9, 7)));
		Assert.AreEqual(64 * 64, TextureCompressor.GetBlockCount(new(256, 256)));

		Assert.AreEqual(8, TextureCompressor.GetBlockSizeBytes(TextureCompressionFormat.Bc1Srgb));
		Assert.AreEqual(16, TextureCompressor.GetBlockSizeBytes(TextureCompressionFormat.Bc3Srgb));
		Assert.AreEqual(16, TextureCompressor.GetBlockSizeBytes(TextureCompressionFormat.Bc5));
		Assert.AreEqual(16, TextureCompressor.GetBlockSizeBytes(TextureCompressionFormat.Bc7Srgb));
		Assert.AreEqual(16, TextureCompressor.GetBlockSizeBytes(TextureCompressionFormat.Bc7Linear));

		Assert.AreEqual(64 * 64 * 16, TextureCompressor.GetLevelSizeBytes(new(256, 256), TextureCompressionFormat.Bc7Srgb));
		Assert.AreEqual(64 * 64 * 8, TextureCompressor.GetLevelSizeBytes(new(256, 256), TextureCompressionFormat.Bc1Srgb));
	}

	[Test]
	public void ShouldCorrectlyCalculateTotalCompressedSizesIncludingMipChain() {
		Assert.AreEqual(16, TextureCompressor.GetCompressedSizeBytes(new(4, 4), TextureCompressionFormat.Bc7Srgb, includeMipChain: false));

		var expected = 0;
		for (var level = 0; level < TextureUtils.GetMipLevelCount(new(256, 256)); ++level) {
			expected += TextureCompressor.GetLevelSizeBytes(TextureUtils.GetMipLevelDimensions(new(256, 256), level), TextureCompressionFormat.Bc7Srgb);
		}
		Assert.AreEqual(expected, TextureCompressor.GetCompressedSizeBytes(new(256, 256), TextureCompressionFormat.Bc7Srgb, includeMipChain: true));

		Assert.AreEqual(256 * 256 * 4, TextureCompressor.GetNonCompressedSizeBytes(new(256, 256), 4, includeMipChain: false));
		Assert.IsTrue(TextureCompressor.GetNonCompressedSizeBytes(new(256, 256), 4, includeMipChain: true) > 256 * 256 * 4);

		Assert.Throws<ArgumentOutOfRangeException>(() => TextureCompressor.GetCompressedSizeBytes(new(4, 4), TextureCompressionFormat.None, false));
	}

	[Test]
	public void ShouldCorrectlyDetermineLevelExtentsForCompressedData() {
		var dimensions = new XYPair<int>(64, 64);
		const TextureCompressionFormat Format = TextureCompressionFormat.Bc7Srgb;
		var levelCount = TextureUtils.GetMipLevelCount(dimensions);
		var totalSize = TextureCompressor.GetCompressedSizeBytes(dimensions, Format, includeMipChain: true);
		var buffer = new byte[totalSize];

		var data = new CompressedTextureData(buffer, Format, dimensions, levelCount);

		var runningOffset = 0;
		for (var level = 0; level < levelCount; ++level) {
			data.GetLevelExtent(level, out var offset, out var length);
			Assert.AreEqual(runningOffset, offset);
			Assert.AreEqual(TextureCompressor.GetLevelSizeBytes(TextureUtils.GetMipLevelDimensions(dimensions, level), Format), length);
			Assert.AreEqual(length, data.GetLevel(level).Length);
			runningOffset += length;
		}

		Assert.AreEqual(totalSize, runningOffset);
	}

	[Test]
	public void ShouldNotDarkenSrgbTexturesWhenGeneratingMipLevels() {
		const byte MidGrey = 128;
		var source = new TexelRgba32[4 * 4];
		Array.Fill(source, new TexelRgba32(MidGrey, MidGrey, MidGrey, 200));
		var destination = new TexelRgba32[2 * 2];

		TextureUtils.GenerateNextMipLevel(source, new(4, 4), destination, TextureMipMapGenerationType.Srgb);

		foreach (var texel in destination) {
			Assert.AreEqual(MidGrey, texel.R, 1d);
			Assert.AreEqual(MidGrey, texel.G, 1d);
			Assert.AreEqual(MidGrey, texel.B, 1d);
			Assert.AreEqual(200, texel.A, 1d);
		}
	}

	[Test]
	public void ShouldAverageSrgbTexturesInLinearSpace() {
		var source = new TexelRgba32[2 * 2];
		source[0] = new TexelRgba32(0, 0, 0, 0);
		source[1] = new TexelRgba32(255, 255, 255, 255);
		source[2] = new TexelRgba32(0, 0, 0, 0);
		source[3] = new TexelRgba32(255, 255, 255, 255);
		var destination = new TexelRgba32[1];

		TextureUtils.GenerateNextMipLevel(source, new(2, 2), destination, TextureMipMapGenerationType.Srgb);

		var expectedSrgb = ColorVect.LinearToSrgb(0.5f) * 255f;
		Assert.AreEqual(expectedSrgb, destination[0].R, 1.5d);
		Assert.IsTrue(destination[0].R > 180, $"Expected sRGB-correct average well above the naive 128, but was {destination[0].R}.");

		Assert.AreEqual(128, destination[0].A, 1d);

		TextureUtils.GenerateNextMipLevel(source, new(2, 2), destination, TextureMipMapGenerationType.Linear);
		Assert.AreEqual(128, destination[0].R, 1d);
	}

	[Test]
	public void ShouldRenormalizeVectorTexturesWhenGeneratingMipLevels() {
		static TexelRgba32 Encode(float x, float y, float z) {
			return new TexelRgba32(
				(byte) MathF.Round((x * 0.5f + 0.5f) * 255f),
				(byte) MathF.Round((y * 0.5f + 0.5f) * 255f),
				(byte) MathF.Round((z * 0.5f + 0.5f) * 255f),
				255
			);
		}

		var source = new TexelRgba32[2 * 2];
		source[0] = Encode(0.6f, 0f, 0.8f);
		source[1] = Encode(-0.6f, 0f, 0.8f);
		source[2] = Encode(0f, 0.6f, 0.8f);
		source[3] = Encode(0f, -0.6f, 0.8f);
		var destination = new TexelRgba32[1];

		TextureUtils.GenerateNextMipLevel(source, new(2, 2), destination, TextureMipMapGenerationType.Vector);

		var x = destination[0].R / 255f * 2f - 1f;
		var y = destination[0].G / 255f * 2f - 1f;
		var z = destination[0].B / 255f * 2f - 1f;
		var length = MathF.Sqrt(x * x + y * y + z * z);

		Assert.AreEqual(1f, length, 0.01f);
		Assert.AreEqual(0f, x, 0.01f);
		Assert.AreEqual(0f, y, 0.01f);
		Assert.AreEqual(1f, z, 0.01f);
	}

	[Test]
	public void ShouldClampToEdgeWhenGeneratingMipLevelsForOddDimensions() {
		var source = new TexelRgba32[3 * 3];
		for (var i = 0; i < source.Length; ++i) source[i] = new TexelRgba32((byte) (i * 10), 0, 0, 255);
		var destination = new TexelRgba32[1];

		Assert.DoesNotThrow(() => TextureUtils.GenerateNextMipLevel(source, new(3, 3), destination, TextureMipMapGenerationType.Linear));
	}

	[Test]
	public void ShouldSelectAppropriateCompressionFormatsPerTextureSemantic() {
		Assert.AreEqual(
			TextureCompressionFormat.Bc1Srgb,
			TextureCompressor.GetRecommendedFormat(TexelType.Rgb24, isLinearColorspace: false, TextureDataType.Other, Quality.VeryLow)
		);
		Assert.AreEqual(
			TextureCompressionFormat.Bc3Srgb,
			TextureCompressor.GetRecommendedFormat(TexelType.Rgba32, isLinearColorspace: false, TextureDataType.Other, Quality.VeryLow)
		);

		foreach (var quality in new[] { Quality.Low, Quality.Standard, Quality.High, Quality.VeryHigh }) {
			Assert.AreEqual(
				TextureCompressionFormat.Bc7Srgb,
				TextureCompressor.GetRecommendedFormat(TexelType.Rgb24, isLinearColorspace: false, TextureDataType.Other, quality)
			);
			Assert.AreEqual(
				TextureCompressionFormat.Bc7Srgb,
				TextureCompressor.GetRecommendedFormat(TexelType.Rgba32, isLinearColorspace: false, TextureDataType.Other, quality)
			);
		}

		foreach (var quality in new[] { Quality.VeryLow, Quality.Low, Quality.Standard, Quality.High, Quality.VeryHigh }) {
			Assert.AreEqual(
				TextureCompressionFormat.Bc7Linear,
				TextureCompressor.GetRecommendedFormat(TexelType.Rgb24, isLinearColorspace: true, TextureDataType.Other, quality)
			);
			Assert.AreEqual(
				TextureCompressionFormat.Bc5,
				TextureCompressor.GetRecommendedFormat(TexelType.Rgb24, isLinearColorspace: true, TextureDataType.LinearUnitVector, quality)
			);
		}
	}

	[Test]
	public void ShouldNeverSelectBc1ForTexturesCarryingAlpha() {
		foreach (var quality in Enum.GetValues<Quality>()) {
			var format = TextureCompressor.GetRecommendedFormat(TexelType.Rgba32, isLinearColorspace: false, TextureDataType.Other, quality);
			Assert.AreNotEqual(TextureCompressionFormat.Bc1Srgb, format);
		}
	}

	[Test]
	public void ShouldNeverSelectS3tcFormatsForLinearDataTextures() {
		foreach (var quality in Enum.GetValues<Quality>()) {
			foreach (var dataType in Enum.GetValues<TextureDataType>()) {
				foreach (var texelType in new[] { TexelType.Rgb24, TexelType.Rgba32 }) {
					var format = TextureCompressor.GetRecommendedFormat(texelType, isLinearColorspace: true, dataType, quality);
					Assert.AreNotEqual(TextureCompressionFormat.Bc1Srgb, format);
					Assert.AreNotEqual(TextureCompressionFormat.Bc3Srgb, format);
				}
			}
		}
	}

	[Test]
	public void ShouldFallBackWhenBackendDoesNotSupportDesiredFormat() {
		const TextureCompressionSupport NoBptc = TextureCompressionSupport.Bc1Srgb | TextureCompressionSupport.Bc3Srgb | TextureCompressionSupport.Bc5;

		Assert.AreEqual(
			TextureCompressionFormat.Bc1Srgb,
			TextureCompressor.ApplySupportFallback(TextureCompressionFormat.Bc7Srgb, TexelType.Rgb24, NoBptc)
		);
		Assert.AreEqual(
			TextureCompressionFormat.Bc3Srgb,
			TextureCompressor.ApplySupportFallback(TextureCompressionFormat.Bc7Srgb, TexelType.Rgba32, NoBptc)
		);
		Assert.AreEqual(
			TextureCompressionFormat.Bc5,
			TextureCompressor.ApplySupportFallback(TextureCompressionFormat.Bc5, TexelType.Rgb24, NoBptc)
		);

		Assert.AreEqual(
			TextureCompressionFormat.Bc7Linear,
			TextureCompressor.ApplySupportFallback(TextureCompressionFormat.Bc5, TexelType.Rgb24, TextureCompressionSupport.Bc7Linear)
		);
		Assert.AreEqual(
			TextureCompressionFormat.None,
			TextureCompressor.ApplySupportFallback(TextureCompressionFormat.Bc7Srgb, TexelType.Rgb24, TextureCompressionSupport.None)
		);
		Assert.AreEqual(
			TextureCompressionFormat.None,
			TextureCompressor.ApplySupportFallback(TextureCompressionFormat.Bc5, TexelType.Rgb24, TextureCompressionSupport.None)
		);
	}

	[Test]
	public void ShouldNotCompressWhenCompressionWouldNotReduceSize() {
		Assert.AreEqual(
			TextureCompressionFormat.None,
			TextureCompressor.SelectFormatWithFallback(TexelType.Rgba32, false, TextureDataType.Other, Quality.Standard, false, new(1, 1), false, TextureCompressionSupport.All)
		);
		Assert.AreEqual(
			TextureCompressionFormat.None,
			TextureCompressor.SelectFormatWithFallback(TexelType.Rgba32, false, TextureDataType.Other, Quality.Standard, false, new(2, 2), false, TextureCompressionSupport.All)
		);

		Assert.AreEqual(
			TextureCompressionFormat.Bc7Srgb,
			TextureCompressor.SelectFormatWithFallback(TexelType.Rgba32, false, TextureDataType.Other, Quality.Standard, false, new(256, 256), true, TextureCompressionSupport.All)
		);
	}

	[Test]
	public void ShouldNotCompressWhenDisabledOrDynamicallyWritable() {
		Assert.AreEqual(
			TextureCompressionFormat.None,
			TextureCompressor.SelectFormatWithFallback(TexelType.Rgba32, false, TextureDataType.Other, null, false, new(256, 256), true, TextureCompressionSupport.All)
		);
		Assert.AreEqual(
			TextureCompressionFormat.None,
			TextureCompressor.SelectFormatWithFallback(TexelType.Rgba32, false, TextureDataType.Other, Quality.Standard, true, new(256, 256), true, TextureCompressionSupport.All)
		);
	}

	[Test]
	public void ShouldMapQualityLevelsOntoMonotonicEffortLevels() {
		var previous = -1;
		foreach (var quality in new[] { Quality.VeryLow, Quality.Low, Quality.Standard, Quality.High, Quality.VeryHigh }) {
			var effort = TextureCompressor.GetEffortLevel(quality);
			Assert.IsTrue(effort >= TextureCompressor.MinEffortLevel && effort <= TextureCompressor.MaxEffortLevel);
			Assert.IsTrue(effort >= previous, $"Effort for {quality} ({effort}) regressed below the previous level ({previous}).");
			previous = effort;
		}
	}
}
