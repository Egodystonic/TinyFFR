// Created on 2026-10-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Globalization;
using System.IO;
using System.Text.Unicode;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Local;

static class RadianceHdrConformer {
	public readonly ref struct PadFillRows {
		public ReadOnlySpan<byte> SideBandRgbe { get; init; }
		public ReadOnlySpan<byte> TopRgbe { get; init; }
		public ReadOnlySpan<byte> BottomRgbe { get; init; }
	}

	struct ColorAccumulator {
		double _r;
		double _g;
		double _b;
		long _count;

		public void Add(ReadOnlySpan<float> decodedTexels) {
			for (var i = 0; i < decodedTexels.Length; i += FloatsPerDecodedTexel) {
				_r += decodedTexels[i];
				_g += decodedTexels[i + 1];
				_b += decodedTexels[i + 2];
			}
			_count += decodedTexels.Length / FloatsPerDecodedTexel;
		}

		public readonly Vector3 Mean => _count == 0L ? Vector3.Zero : new((float) (_r / _count), (float) (_g / _count), (float) (_b / _count));
	}

	public const int MaxHeaderSizeBytes = 8192;
	public const int MaxOutputWidth = 32766;
	const int RleMinWidth = 8;
	const int RleMaxWidth = 32767;
	const int BytesPerTexel = 4;
	const int FloatsPerDecodedTexel = 3;

	public static bool TryReadHeader(ReadOnlySpan<byte> file, out int width, out int height, out int dataOffset, out bool isStandardOrientation) {
		width = 0;
		height = 0;
		dataOffset = 0;
		isStandardOrientation = false;

		var lineStart = 0;
		while (lineStart < file.Length) {
			var lineLength = file[lineStart..].IndexOf((byte) '\n');
			if (lineLength < 0) return false;
			var line = file.Slice(lineStart, lineLength).TrimEnd((byte) '\r').TrimEnd((byte) ' ');
			lineStart += lineLength + 1;

			if (!TryParseResolutionLine(line, out var firstSign, out var firstAxis, out var firstValue, out var secondSign, out var secondAxis, out var secondValue)) continue;
			if (firstAxis == secondAxis) return false;

			if (firstAxis == (byte) 'Y') {
				height = firstValue;
				width = secondValue;
			}
			else {
				width = firstValue;
				height = secondValue;
			}
			dataOffset = lineStart;
			isStandardOrientation = firstAxis == (byte) 'Y' && firstSign == (byte) '-' && secondSign == (byte) '+';
			return true;
		}

		return false;
	}

	static bool TryParseResolutionLine(ReadOnlySpan<byte> line, out byte firstSign, out byte firstAxis, out int firstValue, out byte secondSign, out byte secondAxis, out int secondValue) {
		secondSign = 0;
		secondAxis = 0;
		secondValue = 0;
		var index = 0;
		if (!TryParseAxis(line, ref index, out firstSign, out firstAxis, out firstValue)) return false;
		if (index >= line.Length || line[index] != (byte) ' ') return false;
		++index;
		return TryParseAxis(line, ref index, out secondSign, out secondAxis, out secondValue) && index == line.Length;
	}

	static bool TryParseAxis(ReadOnlySpan<byte> line, ref int index, out byte sign, out byte axis, out int value) {
		sign = 0;
		axis = 0;
		value = 0;
		if (index + 3 > line.Length) return false;
		sign = line[index];
		axis = line[index + 1];
		if (sign is not ((byte) '+' or (byte) '-') || axis is not ((byte) 'X' or (byte) 'Y') || line[index + 2] != (byte) ' ') return false;
		index += 3;

		var digitsStart = index;
		while (index < line.Length && line[index] is >= (byte) '0' and <= (byte) '9') {
			if (value > (Int32.MaxValue - 9) / 10) return false;
			value = value * 10 + (line[index] - (byte) '0');
			++index;
		}
		return index > digitsStart && value > 0;
	}

	public static bool IsCmgenCompatible(int width, int height) {
		return width == height * 2L
			|| (BitOperations.IsPow2(width) && width * 3L == height * 4L)
			|| (BitOperations.IsPow2(height) && height * 3L == width * 4L);
	}

	public static void GetConformedDimensions(int width, int height, BackdropAspectRatioCorrection correction, out int outputWidth, out int outputHeight) {
		if (correction is not (BackdropAspectRatioCorrection.Stretch or BackdropAspectRatioCorrection.MirrorTile) && !IsPad(correction)) {
			throw new ArgumentOutOfRangeException(nameof(correction), correction, null);
		}
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

		var conformedHeight = Math.Max(height, (width + 1L) / 2L);

		if (conformedHeight * 2L > MaxOutputWidth) {
			if (IsPad(correction)) {
				throw new InvalidOperationException($"Can not pad a {width}x{height} HDR image to a 2:1 aspect ratio as the result would exceed the maximum width of {MaxOutputWidth} texels.");
			}
			conformedHeight = MaxOutputWidth / 2;
		}

		outputHeight = (int) conformedHeight;
		outputWidth = outputHeight * 2;
	}

	public static void DecodeScanlines(ReadOnlySpan<byte> data, int width, int height, Span<byte> destRgbe) {
		var rowLengthBytes = width * BytesPerTexel;
		if (destRgbe.Length < (long) rowLengthBytes * height) throw new ArgumentException("Destination buffer is too small.", nameof(destRgbe));

		var src = 0;
		for (var y = 0; y < height; ++y) {
			var row = destRgbe.Slice(y * rowLengthBytes, rowLengthBytes);
			if (width is >= RleMinWidth and <= RleMaxWidth && src + 4 <= data.Length && data[src] == 0x02 && data[src + 1] == 0x02 && (data[src + 2] & 0x80) == 0) {
				var encodedWidth = (data[src + 2] << 8) | data[src + 3];
				if (encodedWidth != width) throw new InvalidDataException($"HDR scanline {y} declares a width of {encodedWidth} but the image is {width} texels wide.");
				src += 4;

				for (var channel = 0; channel < BytesPerTexel; ++channel) {
					var x = 0;
					while (x < width) {
						int count = ReadByte(data, ref src);
						if (count > 128) {
							count -= 128;
							if (count > width - x) throw new InvalidDataException($"HDR scanline {y} run overflows the scanline.");
							var value = ReadByte(data, ref src);
							for (var i = 0; i < count; ++i) row[(x++) * BytesPerTexel + channel] = value;
						}
						else {
							if (count == 0 || count > width - x) throw new InvalidDataException($"HDR scanline {y} literal overflows the scanline.");
							if (src + count > data.Length) throw new InvalidDataException("Unexpected end of HDR pixel data.");
							for (var i = 0; i < count; ++i) row[(x++) * BytesPerTexel + channel] = data[src++];
						}
					}
				}
			}
			else {
				if (src + rowLengthBytes > data.Length) throw new InvalidDataException("Unexpected end of HDR pixel data.");
				data.Slice(src, rowLengthBytes).CopyTo(row);
				src += rowLengthBytes;
			}
		}
	}

	static byte ReadByte(ReadOnlySpan<byte> data, ref int src) {
		if (src >= data.Length) throw new InvalidDataException("Unexpected end of HDR pixel data.");
		return data[src++];
	}

	public static int GetRowScratchLength(int sourceWidth) => sourceWidth * FloatsPerDecodedTexel * 2;

	static bool IsPad(BackdropAspectRatioCorrection correction) => correction is BackdropAspectRatioCorrection.PadWithAverageColor or BackdropAspectRatioCorrection.PadWithEdgeColor;

	public static void ComputePadFill(ReadOnlySpan<byte> sourceRgbe, int sourceWidth, int sourceHeight, BackdropAspectRatioCorrection correction, Span<float> rowScratch, out Vector3 leftEdge, out Vector3 rightEdge, out Vector3 topEdge, out Vector3 bottomEdge) {
		if (!IsPad(correction)) throw new ArgumentOutOfRangeException(nameof(correction), correction, null);

		var sourceRowLengthBytes = sourceWidth * BytesPerTexel;
		var decodedRow = rowScratch[..(sourceWidth * FloatsPerDecodedTexel)];
		var stripColumns = Math.Max(1, sourceWidth / 100);
		var stripRows = Math.Max(1, sourceHeight / 100);
		var stripLength = stripColumns * FloatsPerDecodedTexel;

		var whole = new ColorAccumulator();
		var left = new ColorAccumulator();
		var right = new ColorAccumulator();
		var top = new ColorAccumulator();
		var bottom = new ColorAccumulator();
		for (var y = 0; y < sourceHeight; ++y) {
			DecodeRow(sourceRgbe.Slice(y * sourceRowLengthBytes, sourceRowLengthBytes), decodedRow);
			if (correction == BackdropAspectRatioCorrection.PadWithAverageColor) {
				whole.Add(decodedRow);
				continue;
			}
			left.Add(decodedRow[..stripLength]);
			right.Add(decodedRow[^stripLength..]);
			if (y < stripRows) top.Add(decodedRow);
			if (y >= sourceHeight - stripRows) bottom.Add(decodedRow);
		}

		if (correction == BackdropAspectRatioCorrection.PadWithAverageColor) {
			leftEdge = rightEdge = topEdge = bottomEdge = whole.Mean;
			return;
		}
		leftEdge = left.Mean;
		rightEdge = right.Mean;
		topEdge = top.Mean;
		bottomEdge = bottom.Mean;
	}

	public static void EncodeSideBand(Vector3 rightEdge, Vector3 leftEdge, Span<byte> destRgbe) {
		var bandLength = destRgbe.Length / BytesPerTexel;
		for (var d = 0; d < bandLength; ++d) {
			var color = Vector3.Lerp(rightEdge, leftEdge, (d + 0.5f) / bandLength);
			EncodeRgbe(color.X, color.Y, color.Z, destRgbe.Slice(d * BytesPerTexel, BytesPerTexel));
		}
	}

	static void FillWithTexel(Span<byte> destRgbe, ReadOnlySpan<byte> texel) {
		MemoryMarshal.Cast<byte, uint>(destRgbe).Fill(MemoryMarshal.Read<uint>(texel));
	}

	public static void GenerateConformedRow(ReadOnlySpan<byte> sourceRgbe, int sourceWidth, int sourceHeight, BackdropAspectRatioCorrection correction, int outputWidth, int outputHeight, int outputRow, Span<float> rowScratch, Span<byte> destRgbe, PadFillRows padFill = default) {
		destRgbe = destRgbe[..(outputWidth * BytesPerTexel)];
		var sourceRowLengthBytes = sourceWidth * BytesPerTexel;

		if (IsPad(correction)) {
			var padRow = outputRow - (outputHeight - sourceHeight) / 2;
			if (padRow < 0 || padRow >= sourceHeight) {
				FillWithTexel(destRgbe, padRow < 0 ? padFill.TopRgbe : padFill.BottomRgbe);
				return;
			}
			var imageStart = (outputWidth - sourceWidth) / 2;
			var rightBandStart = imageStart + sourceWidth;
			var rightBandLengthBytes = (outputWidth - rightBandStart) * BytesPerTexel;
			padFill.SideBandRgbe[..rightBandLengthBytes].CopyTo(destRgbe[(rightBandStart * BytesPerTexel)..]);
			padFill.SideBandRgbe[rightBandLengthBytes..].CopyTo(destRgbe);
			sourceRgbe.Slice(padRow * sourceRowLengthBytes, sourceRowLengthBytes).CopyTo(destRgbe[(imageStart * BytesPerTexel)..]);
			return;
		}

		var isMirrored = correction == BackdropAspectRatioCorrection.MirrorTile;
		var virtualWidth = isMirrored ? sourceWidth * 2 : sourceWidth;

		var sourceY = (outputRow + 0.5d) * sourceHeight / outputHeight - 0.5d;
		var sourceYFloor = Math.Floor(sourceY);
		var yFraction = (float) (sourceY - sourceYFloor);
		var topRowIndex = Math.Clamp((int) sourceYFloor, 0, sourceHeight - 1);
		var bottomRowIndex = Math.Clamp((int) sourceYFloor + 1, 0, sourceHeight - 1);

		var decodedRowLength = sourceWidth * FloatsPerDecodedTexel;
		var topRow = rowScratch[..decodedRowLength];
		var bottomRow = rowScratch.Slice(decodedRowLength, decodedRowLength);
		DecodeRow(sourceRgbe.Slice(topRowIndex * sourceRowLengthBytes, sourceRowLengthBytes), topRow);
		DecodeRow(sourceRgbe.Slice(bottomRowIndex * sourceRowLengthBytes, sourceRowLengthBytes), bottomRow);

		for (var x = 0; x < outputWidth; ++x) {
			var sourceX = (x + 0.5d) * virtualWidth / outputWidth - 0.5d;
			var sourceXFloor = Math.Floor(sourceX);
			var xFraction = (float) (sourceX - sourceXFloor);
			var leftColumn = MapColumn((int) sourceXFloor, sourceWidth, virtualWidth, isMirrored) * FloatsPerDecodedTexel;
			var rightColumn = MapColumn((int) sourceXFloor + 1, sourceWidth, virtualWidth, isMirrored) * FloatsPerDecodedTexel;

			var r = Bilerp(topRow[leftColumn], topRow[rightColumn], bottomRow[leftColumn], bottomRow[rightColumn], xFraction, yFraction);
			var g = Bilerp(topRow[leftColumn + 1], topRow[rightColumn + 1], bottomRow[leftColumn + 1], bottomRow[rightColumn + 1], xFraction, yFraction);
			var b = Bilerp(topRow[leftColumn + 2], topRow[rightColumn + 2], bottomRow[leftColumn + 2], bottomRow[rightColumn + 2], xFraction, yFraction);
			EncodeRgbe(r, g, b, destRgbe.Slice(x * BytesPerTexel, BytesPerTexel));
		}
	}

	static int MapColumn(int virtualColumn, int sourceWidth, int virtualWidth, bool isMirrored) {
		virtualColumn %= virtualWidth;
		if (virtualColumn < 0) virtualColumn += virtualWidth;
		return isMirrored && virtualColumn >= sourceWidth ? virtualWidth - 1 - virtualColumn : virtualColumn;
	}

	static float Bilerp(float topLeft, float topRight, float bottomLeft, float bottomRight, float xFraction, float yFraction) {
		var top = topLeft + (topRight - topLeft) * xFraction;
		var bottom = bottomLeft + (bottomRight - bottomLeft) * xFraction;
		return top + (bottom - top) * yFraction;
	}

	public static void DecodeRow(ReadOnlySpan<byte> rgbe, Span<float> dest) {
		var texelCount = rgbe.Length / BytesPerTexel;
		for (var i = 0; i < texelCount; ++i) {
			var texel = rgbe.Slice(i * BytesPerTexel, BytesPerTexel);
			var decoded = dest.Slice(i * FloatsPerDecodedTexel, FloatsPerDecodedTexel);
			if (texel[3] == 0) {
				decoded.Clear();
				continue;
			}
			var scale = MathF.ScaleB(1f, texel[3] - 136);
			decoded[0] = (texel[0] + 0.5f) * scale;
			decoded[1] = (texel[1] + 0.5f) * scale;
			decoded[2] = (texel[2] + 0.5f) * scale;
		}
	}

	public static void EncodeRgbe(float r, float g, float b, Span<byte> dest) {
		var max = MathF.Max(r, MathF.Max(g, b));
		if (!(max >= 1E-32f)) {
			dest[..BytesPerTexel].Clear();
			return;
		}

		var exponent = Math.Min(MathF.ILogB(MathF.Min(max, Single.MaxValue)) + 1, 127);
		var scale = MathF.ScaleB(256f, -exponent);
		dest[0] = (byte) Math.Clamp((int) (r * scale), 0, 255);
		dest[1] = (byte) Math.Clamp((int) (g * scale), 0, 255);
		dest[2] = (byte) Math.Clamp((int) (b * scale), 0, 255);
		dest[3] = (byte) (exponent + 128);
	}

	public static void WriteHeader(Stream destination, int outputWidth, int outputHeight) {
		Span<byte> header = stackalloc byte[128];
		if (!Utf8.TryWrite(header, CultureInfo.InvariantCulture, $"#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n-Y {outputHeight} +X {outputWidth}\n", out var bytesWritten)) {
			throw new InvalidOperationException("HDR header buffer too small (this is a bug in TinyFFR).");
		}
		destination.Write(header[..bytesWritten]);
	}

	public static void WriteConformed(IHeapPool heapPool, ReadOnlySpan<byte> file, BackdropAspectRatioCorrection correction, Stream destination) {
		if (!TryReadHeader(file, out var width, out var height, out var dataOffset, out var isStandardOrientation)) {
			throw new InvalidDataException("Could not find the resolution line in the HDR file header.");
		}
		if (!isStandardOrientation) {
			throw new InvalidOperationException("Only Radiance HDR files with the standard '-Y <height> +X <width>' orientation can have their aspect ratio corrected.");
		}

		GetConformedDimensions(width, height, correction, out var outputWidth, out var outputHeight);
		using var decoded = heapPool.Borrow(checked(width * height * BytesPerTexel));
		DecodeScanlines(file[dataOffset..], width, height, decoded.Span);

		using var rowScratch = heapPool.Borrow<float>(GetRowScratchLength(width));
		using var outputRow = heapPool.Borrow(outputWidth * BytesPerTexel);

		var sideBandLength = IsPad(correction) ? outputWidth - width : 0;
		using var sideBandMemory = heapPool.Borrow(Math.Max(1, sideBandLength) * BytesPerTexel);
		var sideBand = sideBandMemory.Span[..(sideBandLength * BytesPerTexel)];
		Span<byte> topTexel = stackalloc byte[BytesPerTexel];
		Span<byte> bottomTexel = stackalloc byte[BytesPerTexel];
		if (IsPad(correction)) {
			ComputePadFill(decoded.Span, width, height, correction, rowScratch.Span, out var leftEdge, out var rightEdge, out var topEdge, out var bottomEdge);
			EncodeSideBand(rightEdge, leftEdge, sideBand);
			EncodeRgbe(topEdge.X, topEdge.Y, topEdge.Z, topTexel);
			EncodeRgbe(bottomEdge.X, bottomEdge.Y, bottomEdge.Z, bottomTexel);
		}
		var padFill = new PadFillRows { SideBandRgbe = sideBand, TopRgbe = topTexel, BottomRgbe = bottomTexel };

		WriteHeader(destination, outputWidth, outputHeight);
		for (var y = 0; y < outputHeight; ++y) {
			GenerateConformedRow(decoded.Span, width, height, correction, outputWidth, outputHeight, y, rowScratch.Span, outputRow.Span, padFill);
			destination.Write(outputRow.Span);
		}
	}
}
