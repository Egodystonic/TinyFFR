// Created on 2026-08-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

public static partial class TextureUtils {
	const int SrgbLutSize = 256;
	static readonly float[] SrgbByteToLinearLut = CreateSrgbByteToLinearLut();

	static float[] CreateSrgbByteToLinearLut() {
		var result = new float[SrgbLutSize];
		for (var i = 0; i < SrgbLutSize; ++i) result[i] = ColorVect.SrgbToLinear(i / (float) Byte.MaxValue);
		return result;
	}

	public static int GetMipLevelCount(XYPair<int> dimensions) {
		if (dimensions.X < 1 || dimensions.Y < 1) {
			throw new ArgumentOutOfRangeException(nameof(dimensions), dimensions, "Dimensions X and Y must both be positive.");
		}
		return Int32.Log2(Int32.Max(dimensions.X, dimensions.Y)) + 1;
	}

	public static XYPair<int> GetMipLevelDimensions(XYPair<int> baseDimensions, int level) {
		if (level < 0) throw new ArgumentOutOfRangeException(nameof(level), level, "Level can not be negative.");
		return new XYPair<int>(
			Int32.Max(1, baseDimensions.X >> level),
			Int32.Max(1, baseDimensions.Y >> level)
		);
	}

	public static void GenerateNextMipLevel(ReadOnlySpan<TexelRgba32> source, XYPair<int> sourceDimensions, Span<TexelRgba32> destination, TextureDataType dataType) {
		var destDimensions = new XYPair<int>(
			Int32.Max(1, sourceDimensions.X >> 1),
			Int32.Max(1, sourceDimensions.Y >> 1)
		);

		if (source.Length < sourceDimensions.Area) {
			throw new ArgumentException(
				$"Source dimensions are {sourceDimensions.X}x{sourceDimensions.Y}, requiring a texel span of length {sourceDimensions.Area} or greater, " +
				$"but actual span length was {source.Length}.",
				nameof(source)
			);
		}
		if (destination.Length < destDimensions.Area) {
			throw new ArgumentException(
				$"Destination dimensions are {destDimensions.X}x{destDimensions.Y}, requiring a texel span of length {destDimensions.Area} or greater, " +
				$"but actual span length was {destination.Length}.",
				nameof(destination)
			);
		}
		
		switch (dataType) {
			case TextureDataType.LinearData:
			case TextureDataType.LinearDataTwoChannelMax:
				GenerateNextMipLevelLinear(source, sourceDimensions, destDimensions, destination);
				return;
			case TextureDataType.ColorSrgb:
				GenerateNextMipLevelSrgb(source, sourceDimensions, destDimensions, destination);
				return;
			case TextureDataType.LinearDataUnitVector:
				GenerateNextMipLevelVector(source, sourceDimensions, destDimensions, destination);
				return;
			default:
				throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
		}
	}

	static void DetermineMipMapSampleIndices(XYPair<int> sourceDimensions, int destX, int destY, out int a, out int b, out int c, out int d) {
		var x0 = Int32.Min(destX * 2, sourceDimensions.X - 1);
		var x1 = Int32.Min(destX * 2 + 1, sourceDimensions.X - 1);
		var y0 = Int32.Min(destY * 2, sourceDimensions.Y - 1);
		var y1 = Int32.Min(destY * 2 + 1, sourceDimensions.Y - 1);
		a = sourceDimensions.Index(x0, y0);
		b = sourceDimensions.Index(x1, y0);
		c = sourceDimensions.Index(x0, y1);
		d = sourceDimensions.Index(x1, y1);
	}

	// The +2 removes the bias of integer rounding truncation (e.g. 2 is half of the divisor (4))
	static byte GetLinearChannelAverage(byte a, byte b, byte c, byte d) => (byte) ((a + b + c + d + 2) / 4);

	static void GenerateNextMipLevelLinear(ReadOnlySpan<TexelRgba32> source, XYPair<int> sourceDimensions, XYPair<int> destDimensions, Span<TexelRgba32> destination) {
		for (var y = 0; y < destDimensions.Y; ++y) {
			for (var x = 0; x < destDimensions.X; ++x) {
				DetermineMipMapSampleIndices(sourceDimensions, x, y, out var ia, out var ib, out var ic, out var id);
				var ta = source[ia];
				var tb = source[ib];
				var tc = source[ic];
				var td = source[id];
				destination[y * destDimensions.X + x] = new TexelRgba32(
					GetLinearChannelAverage(ta.R, tb.R, tc.R, td.R),
					GetLinearChannelAverage(ta.G, tb.G, tc.G, td.G),
					GetLinearChannelAverage(ta.B, tb.B, tc.B, td.B),
					GetLinearChannelAverage(ta.A, tb.A, tc.A, td.A)
				);
			}
		}
	}

	static void GenerateNextMipLevelSrgb(ReadOnlySpan<TexelRgba32> source, XYPair<int> sourceDimensions, XYPair<int> destDimensions, Span<TexelRgba32> destination) {
		static byte GetSrgbChannelAverage(byte a, byte b, byte c, byte d) {
			var lut = SrgbByteToLinearLut;
			var averageLinear = (lut[a] + lut[b] + lut[c] + lut[d]) * 0.25f;
			var reEncoded = ColorVect.LinearToSrgb(averageLinear) * Byte.MaxValue;
			return (byte) Single.Clamp(MathF.Round(reEncoded), 0f, Byte.MaxValue);
		}
		
		for (var y = 0; y < destDimensions.Y; ++y) {
			for (var x = 0; x < destDimensions.X; ++x) {
				DetermineMipMapSampleIndices(sourceDimensions, x, y, out var ia, out var ib, out var ic, out var id);
				var ta = source[ia];
				var tb = source[ib];
				var tc = source[ic];
				var td = source[id];
				destination[y * destDimensions.X + x] = new TexelRgba32(
					GetSrgbChannelAverage(ta.R, tb.R, tc.R, td.R),
					GetSrgbChannelAverage(ta.G, tb.G, tc.G, td.G),
					GetSrgbChannelAverage(ta.B, tb.B, tc.B, td.B),
					GetLinearChannelAverage(ta.A, tb.A, tc.A, td.A)
				);
			}
		}
	}

	static void GenerateNextMipLevelVector(ReadOnlySpan<TexelRgba32> source, XYPair<int> sourceDimensions, XYPair<int> destDimensions, Span<TexelRgba32> destination) {
		static Vect TexelToVect(TexelRgba32 texel) {
			return new Vect(
				texel.R / (float) Byte.MaxValue * 2f - 1f,
				texel.G / (float) Byte.MaxValue * 2f - 1f,
				texel.B / (float) Byte.MaxValue * 2f - 1f
			);
		}
		
		static byte EncodeVectChannel(float value) {
			var encoded = (value * 0.5f + 0.5f) * Byte.MaxValue;
			return (byte) Single.Clamp(MathF.Round(encoded), 0f, Byte.MaxValue);
		}
		
		for (var y = 0; y < destDimensions.Y; ++y) {
			for (var x = 0; x < destDimensions.X; ++x) {
				DetermineMipMapSampleIndices(sourceDimensions, x, y, out var ia, out var ib, out var ic, out var id);
				var ta = source[ia];
				var tb = source[ib];
				var tc = source[ic];
				var td = source[id];

				var summed = TexelToVect(ta) + TexelToVect(tb) + TexelToVect(tc) + TexelToVect(td);
				var lengthSquared = summed.LengthSquared;
				var normalized = lengthSquared > 0f ? summed / MathF.Sqrt(lengthSquared) : new Vect(0f, 0f, 1f);

				destination[y * destDimensions.X + x] = new TexelRgba32(
					EncodeVectChannel(normalized.X),
					EncodeVectChannel(normalized.Y),
					EncodeVectChannel(normalized.Z),
					GetLinearChannelAverage(ta.A, tb.A, tc.A, td.A)
				);
			}
		}
	}
}
