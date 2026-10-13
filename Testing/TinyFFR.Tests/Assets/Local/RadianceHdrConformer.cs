// Created on 2026-10-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.IO;
using System.Numerics;
using System.Text;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Local;

[TestFixture]
class RadianceHdrConformerTest {
	static byte[] CreateRgbe(int width, int height, Func<int, int, (byte R, byte G, byte B, byte E)> texelSelector) {
		var result = new byte[width * height * 4];
		for (var y = 0; y < height; ++y) {
			for (var x = 0; x < width; ++x) {
				var (r, g, b, e) = texelSelector(x, y);
				var i = (y * width + x) * 4;
				result[i] = r;
				result[i + 1] = g;
				result[i + 2] = b;
				result[i + 3] = e;
			}
		}
		return result;
	}

	static byte[] CreateHdrFile(int width, int height, byte[] rgbe, bool useRle, string resolutionLine = null) {
		var stream = new MemoryStream();
		var header = "#?RADIANCE\n# Created with Blender\nEXPOSURE=          1.0000000000000\nFORMAT=32-bit_rle_rgbe\n\n" + (resolutionLine ?? $"-Y {height} +X {width}") + "\n";
		stream.Write(Encoding.ASCII.GetBytes(header));

		for (var y = 0; y < height; ++y) {
			if (!useRle) {
				stream.Write(rgbe, y * width * 4, width * 4);
				continue;
			}
			stream.WriteByte(0x02);
			stream.WriteByte(0x02);
			stream.WriteByte((byte) (width >> 8));
			stream.WriteByte((byte) (width & 0xFF));
			for (var channel = 0; channel < 4; ++channel) {
				var x = 0;
				while (x < width) {
					var value = rgbe[(y * width + x) * 4 + channel];
					var runLength = 1;
					while (x + runLength < width && runLength < 127 && rgbe[(y * width + x + runLength) * 4 + channel] == value) ++runLength;
					if (runLength >= 3) {
						stream.WriteByte((byte) (128 + runLength));
						stream.WriteByte(value);
						x += runLength;
						continue;
					}
					var literalLength = Math.Min(Math.Min(runLength, 128), width - x);
					stream.WriteByte((byte) literalLength);
					for (var i = 0; i < literalLength; ++i) stream.WriteByte(rgbe[(y * width + x + i) * 4 + channel]);
					x += literalLength;
				}
			}
		}
		return stream.ToArray();
	}

	static (int Width, int Height, byte[] Rgbe) Conform(byte[] file, BackdropAspectRatioCorrection correction) {
		using var heapPool = new HeapPool();
		var output = new MemoryStream();
		RadianceHdrConformer.WriteConformed(heapPool, file, correction, output);
		var bytes = output.ToArray();
		Assert.IsTrue(RadianceHdrConformer.TryReadHeader(bytes, out var width, out var height, out var dataOffset, out var isStandardOrientation));
		Assert.IsTrue(isStandardOrientation);
		Assert.AreEqual(width * height * 4, bytes.Length - dataOffset);
		return (width, height, bytes[dataOffset..]);
	}

	static (byte R, byte G, byte B, byte E) GradientTexel(int x, int y) => ((byte) (128 + x * 7 % 128), (byte) (128 + y * 13 % 128), (byte) (x * y % 256), (byte) (120 + (x + y) % 16));

	[Test]
	public void ShouldParseBlenderStyleHeader() {
		var file = CreateHdrFile(16, 9, CreateRgbe(16, 9, GradientTexel), useRle: false);
		Assert.IsTrue(RadianceHdrConformer.TryReadHeader(file, out var width, out var height, out var dataOffset, out var isStandardOrientation));
		Assert.AreEqual(16, width);
		Assert.AreEqual(9, height);
		Assert.IsTrue(isStandardOrientation);
		Assert.AreEqual(file.Length - 16 * 9 * 4, dataOffset);
	}

	[Test]
	public void ShouldReportNonStandardOrientations() {
		var rgbe = CreateRgbe(16, 9, GradientTexel);
		Assert.IsTrue(RadianceHdrConformer.TryReadHeader(CreateHdrFile(16, 9, rgbe, false, "+Y 9 +X 16"), out _, out _, out _, out var flipped));
		Assert.IsFalse(flipped);
		Assert.IsTrue(RadianceHdrConformer.TryReadHeader(CreateHdrFile(16, 9, rgbe, false, "+X 16 -Y 9"), out var width, out var height, out _, out var xFirst));
		Assert.IsFalse(xFirst);
		Assert.AreEqual(16, width);
		Assert.AreEqual(9, height);

		using var heapPool = new HeapPool();
		Assert.Throws<InvalidOperationException>(() => RadianceHdrConformer.WriteConformed(heapPool, CreateHdrFile(16, 9, rgbe, false, "+Y 9 +X 16"), BackdropAspectRatioCorrection.Stretch, new MemoryStream()));
	}

	[Test]
	public void ShouldRejectFilesWithoutResolutionLine() {
		Assert.IsFalse(RadianceHdrConformer.TryReadHeader(Encoding.ASCII.GetBytes("#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n"), out _, out _, out _, out _));
		Assert.IsFalse(RadianceHdrConformer.TryReadHeader(Encoding.ASCII.GetBytes("not an hdr file"), out _, out _, out _, out _));
	}

	[Test]
	public void ShouldDecodeRleAndFlatScanlinesIdentically() {
		const int Width = 37;
		const int Height = 5;
		var rgbe = CreateRgbe(Width, Height, (x, y) => x < 20 ? ((byte) 200, (byte) 150, (byte) 100, (byte) 130) : GradientTexel(x, y));

		foreach (var useRle in new[] { false, true }) {
			var file = CreateHdrFile(Width, Height, rgbe, useRle);
			Assert.IsTrue(RadianceHdrConformer.TryReadHeader(file, out _, out _, out var dataOffset, out _));
			var decoded = new byte[rgbe.Length];
			RadianceHdrConformer.DecodeScanlines(file.AsSpan(dataOffset), Width, Height, decoded);
			CollectionAssert.AreEqual(rgbe, decoded, $"RLE: {useRle}");
		}
	}

	[Test]
	public void ShouldThrowOnTruncatedPixelData() {
		var file = CreateHdrFile(16, 9, CreateRgbe(16, 9, GradientTexel), useRle: true);
		Assert.IsTrue(RadianceHdrConformer.TryReadHeader(file, out _, out _, out var dataOffset, out _));
		var decoded = new byte[16 * 9 * 4];
		Assert.Throws<InvalidDataException>(() => RadianceHdrConformer.DecodeScanlines(file.AsSpan(dataOffset, (file.Length - dataOffset) / 2), 16, 9, decoded));
	}

	[Test]
	public void ShouldCorrectlyDetermineCmgenCompatibility() {
		Assert.IsTrue(RadianceHdrConformer.IsCmgenCompatible(8192, 4096));
		Assert.IsTrue(RadianceHdrConformer.IsCmgenCompatible(1000, 500));
		Assert.IsTrue(RadianceHdrConformer.IsCmgenCompatible(4096, 3072));
		Assert.IsTrue(RadianceHdrConformer.IsCmgenCompatible(3072, 4096));
		Assert.IsFalse(RadianceHdrConformer.IsCmgenCompatible(4000, 3000));
		Assert.IsFalse(RadianceHdrConformer.IsCmgenCompatible(7680, 4320));
		Assert.IsFalse(RadianceHdrConformer.IsCmgenCompatible(1024, 1024));
	}

	[Test]
	public void ShouldCorrectlyDetermineConformedDimensions() {
		void AssertDimensions(int width, int height, BackdropAspectRatioCorrection correction, int expectedWidth, int expectedHeight) {
			RadianceHdrConformer.GetConformedDimensions(width, height, correction, out var outputWidth, out var outputHeight);
			Assert.AreEqual(expectedWidth, outputWidth, $"{width}x{height} {correction}");
			Assert.AreEqual(expectedHeight, outputHeight, $"{width}x{height} {correction}");
		}

		AssertDimensions(7680, 4320, BackdropAspectRatioCorrection.Stretch, 8640, 4320);
		AssertDimensions(7680, 4320, BackdropAspectRatioCorrection.PadWithAverageColor, 8640, 4320);
		AssertDimensions(7680, 4320, BackdropAspectRatioCorrection.MirrorTile, 8640, 4320);
		AssertDimensions(3001, 1000, BackdropAspectRatioCorrection.Stretch, 3002, 1501);
		AssertDimensions(3001, 1000, BackdropAspectRatioCorrection.PadWithEdgeColor, 3002, 1501);
		AssertDimensions(1000, 1000, BackdropAspectRatioCorrection.MirrorTile, 2000, 1000);
		AssertDimensions(100, 30000, BackdropAspectRatioCorrection.Stretch, RadianceHdrConformer.MaxOutputWidth, RadianceHdrConformer.MaxOutputWidth / 2);

		Assert.Throws<InvalidOperationException>(() => RadianceHdrConformer.GetConformedDimensions(100, 30000, BackdropAspectRatioCorrection.PadWithEdgeColor, out _, out _));
		Assert.Throws<ArgumentOutOfRangeException>(() => RadianceHdrConformer.GetConformedDimensions(16, 9, BackdropAspectRatioCorrection.Throw, out _, out _));
	}

	[Test]
	public void ShouldRoundTripRgbeEncodingExactly() {
		var rgbe = CreateRgbe(64, 1, (x, _) => ((byte) (128 + x), (byte) (x * 2), (byte) 64, (byte) (100 + x)));
		var decoded = new float[64 * 3];
		RadianceHdrConformer.DecodeRow(rgbe, decoded);
		var reencoded = new byte[rgbe.Length];
		for (var x = 0; x < 64; ++x) RadianceHdrConformer.EncodeRgbe(decoded[x * 3], decoded[x * 3 + 1], decoded[x * 3 + 2], reencoded.AsSpan(x * 4, 4));
		CollectionAssert.AreEqual(rgbe, reencoded);

		var black = new byte[4];
		RadianceHdrConformer.EncodeRgbe(0f, 0f, 0f, black);
		CollectionAssert.AreEqual(new byte[4], black);
		RadianceHdrConformer.EncodeRgbe(Single.NaN, 0f, 0f, black);
		CollectionAssert.AreEqual(new byte[4], black);
	}

	[Test]
	public void ShouldPreserveConstantColorWhenStretching([Values(BackdropAspectRatioCorrection.Stretch, BackdropAspectRatioCorrection.MirrorTile)] BackdropAspectRatioCorrection correction) {
		var file = CreateHdrFile(16, 9, CreateRgbe(16, 9, (_, _) => (200, 150, 100, 130)), useRle: true);
		var (width, height, rgbe) = Conform(file, correction);
		Assert.AreEqual(width, height * 2);
		for (var i = 0; i < width * height; ++i) {
			Assert.AreEqual(200, rgbe[i * 4]);
			Assert.AreEqual(150, rgbe[i * 4 + 1]);
			Assert.AreEqual(100, rgbe[i * 4 + 2]);
			Assert.AreEqual(130, rgbe[i * 4 + 3]);
		}
	}

	static Vector3 DecodeTexel(byte[] rgbe, int texelIndex) {
		var decoded = new float[3];
		RadianceHdrConformer.DecodeRow(rgbe.AsSpan(texelIndex * 4, 4), decoded);
		return new(decoded[0], decoded[1], decoded[2]);
	}

	static void AssertColor(Vector3 expected, Vector3 actual, string message) {
		var tolerance = MathF.Max(expected.X, MathF.Max(expected.Y, expected.Z)) * 0.02f;
		Assert.AreEqual(expected.X, actual.X, tolerance, message);
		Assert.AreEqual(expected.Y, actual.Y, tolerance, message);
		Assert.AreEqual(expected.Z, actual.Z, tolerance, message);
	}

	[Test]
	public void ShouldPadAroundUnmodifiedSource([Values(BackdropAspectRatioCorrection.PadWithAverageColor, BackdropAspectRatioCorrection.PadWithEdgeColor)] BackdropAspectRatioCorrection correction) {
		const int Width = 16;
		const int Height = 9;
		var source = CreateRgbe(Width, Height, GradientTexel);
		var (width, height, rgbe) = Conform(CreateHdrFile(Width, Height, source, useRle: true), correction);
		Assert.AreEqual(18, width);
		Assert.AreEqual(9, height);

		for (var y = 0; y < height; ++y) {
			for (var x = 1; x < 17; ++x) {
				CollectionAssert.AreEqual(source.AsSpan((y * Width + x - 1) * 4, 4).ToArray(), rgbe.AsSpan((y * width + x) * 4, 4).ToArray(), $"{x},{y}");
			}
		}
	}

	[Test]
	public void ShouldPadWithAverageColor() {
		const int Width = 16;
		const int Height = 9;
		var source = CreateRgbe(Width, Height, (x, _) => x < Width / 4 ? ((byte) 255, (byte) 0, (byte) 0, (byte) 129) : ((byte) 0, (byte) 0, (byte) 255, (byte) 129));
		var red = DecodeTexel(source, 0);
		var blue = DecodeTexel(source, Width - 1);
		var expected = red * 0.25f + blue * 0.75f;

		var (width, height, rgbe) = Conform(CreateHdrFile(Width, Height, source, useRle: true), BackdropAspectRatioCorrection.PadWithAverageColor);
		Assert.AreEqual(18, width);
		for (var y = 0; y < height; ++y) {
			AssertColor(expected, DecodeTexel(rgbe, y * width), $"left band, row {y}");
			AssertColor(expected, DecodeTexel(rgbe, y * width + 17), $"right band, row {y}");
		}
	}

	[Test]
	public void ShouldPadSidesWithEdgeColorGradient() {
		const int Width = 10;
		const int Height = 15;
		var source = CreateRgbe(Width, Height, (x, _) => x == 0 ? ((byte) 255, (byte) 0, (byte) 0, (byte) 129) : x == Width - 1 ? ((byte) 0, (byte) 0, (byte) 255, (byte) 129) : ((byte) 128, (byte) 128, (byte) 128, (byte) 129));
		var red = DecodeTexel(source, 0);
		var blue = DecodeTexel(source, Width - 1);

		var (width, height, rgbe) = Conform(CreateHdrFile(Width, Height, source, useRle: false), BackdropAspectRatioCorrection.PadWithEdgeColor);
		Assert.AreEqual(30, width);
		Assert.AreEqual(15, height);
		const int ImageStart = 10;
		const int RightBandStart = 20;
		const int BandLength = 20;

		var bandOrder = Enumerable.Range(RightBandStart, width - RightBandStart).Concat(Enumerable.Range(0, ImageStart)).ToArray();
		Assert.AreEqual(BandLength, bandOrder.Length);
		for (var y = 0; y < height; ++y) {
			var previousRed = -1f;
			for (var d = 0; d < BandLength; ++d) {
				var texel = rgbe.AsSpan((y * width + bandOrder[d]) * 4, 4).ToArray();
				CollectionAssert.AreEqual(rgbe.AsSpan(bandOrder[d] * 4, 4).ToArray(), texel, $"row {y} band texel {d} differs from row 0");
				var color = DecodeTexel(rgbe, y * width + bandOrder[d]);
				Assert.GreaterOrEqual(color.X, previousRed, $"row {y} band texel {d}");
				previousRed = color.X;
			}
			AssertColor(Vector3.Lerp(blue, red, 0.5f / BandLength), DecodeTexel(rgbe, y * width + RightBandStart), $"next to right edge, row {y}");
			AssertColor(Vector3.Lerp(blue, red, (BandLength - 0.5f) / BandLength), DecodeTexel(rgbe, y * width + ImageStart - 1), $"next to left edge, row {y}");
		}
	}

	[Test]
	public void ShouldPadTopAndBottomWithEdgeColor() {
		const int Width = 40;
		const int Height = 10;
		var source = CreateRgbe(Width, Height, (_, y) => y == 0 ? ((byte) 255, (byte) 0, (byte) 0, (byte) 129) : y == Height - 1 ? ((byte) 0, (byte) 255, (byte) 0, (byte) 129) : ((byte) 128, (byte) 128, (byte) 128, (byte) 129));
		var top = DecodeTexel(source, 0);
		var bottom = DecodeTexel(source, (Height - 1) * Width);

		var (width, height, rgbe) = Conform(CreateHdrFile(Width, Height, source, useRle: true), BackdropAspectRatioCorrection.PadWithEdgeColor);
		Assert.AreEqual(40, width);
		Assert.AreEqual(20, height);
		for (var x = 0; x < width; ++x) {
			for (var y = 0; y < 5; ++y) AssertColor(top, DecodeTexel(rgbe, y * width + x), $"top band {x},{y}");
			for (var y = 15; y < 20; ++y) AssertColor(bottom, DecodeTexel(rgbe, y * width + x), $"bottom band {x},{y}");
		}
	}

	[Test]
	public void ShouldMirrorTileSeamlessly() {
		const int Width = 6;
		const int Height = 4;
		var source = CreateRgbe(Width, Height, GradientTexel);
		var (width, height, rgbe) = Conform(CreateHdrFile(Width, Height, source, useRle: false), BackdropAspectRatioCorrection.MirrorTile);
		Assert.AreEqual(8, width);
		Assert.AreEqual(4, height);

		var decoded = new float[width * height * 3];
		RadianceHdrConformer.DecodeRow(rgbe, decoded);
		for (var y = 0; y < height; ++y) {
			for (var x = 0; x < width; ++x) {
				for (var channel = 0; channel < 3; ++channel) {
					var expected = decoded[(y * width + width - 1 - x) * 3 + channel];
					var actual = decoded[(y * width + x) * 3 + channel];
					Assert.AreEqual(expected, actual, MathF.Max(expected, actual) * 0.02f, $"{x},{y},{channel}");
				}
			}
		}
	}

	[Test]
	public void ShouldNotShiftContentWhenStretching() {
		const int Width = 16;
		const int Height = 9;
		var source = CreateRgbe(Width, Height, (x, _) => x < Width / 2 ? ((byte) 200, (byte) 0, (byte) 0, (byte) 130) : ((byte) 0, (byte) 0, (byte) 200, (byte) 130));
		var (width, height, rgbe) = Conform(CreateHdrFile(Width, Height, source, useRle: true), BackdropAspectRatioCorrection.Stretch);
		Assert.AreEqual(18, width);
		Assert.AreEqual(9, height);

		for (var y = 0; y < height; ++y) {
			Assert.AreEqual(200, rgbe[(y * width + 2) * 4]);
			Assert.AreEqual(0, rgbe[(y * width + 2) * 4 + 2]);
			Assert.AreEqual(0, rgbe[(y * width + 15) * 4]);
			Assert.AreEqual(200, rgbe[(y * width + 15) * 4 + 2]);
		}
	}
}
