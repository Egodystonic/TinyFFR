// Created on 2026-10-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers.Binary;
using System.Buffers.Text;

namespace Egodystonic.TinyFFR.World;

static class BackdropIntensityUtils {
	internal enum KtxTexelFormat {
		R11G11B10Float,
		Rgba16Float,
		Rgb16Float,
		Rgba32Float,
		Rgb32Float,
		Rgba8Unorm,
		Rgb8Unorm,
		Srgba8,
		Srgb8
	}

	internal readonly record struct KtxHeader(bool IsByteSwapped, uint GlType, uint GlInternalFormat, int Width, int Height, int ArrayElements, int Faces, int MipLevels, int KeyValueDataLength);

	public const float TinyFfrLightIntensityTasteEv100Adjustment = -0.5f; // Adjustment to taste - slightly lower intensity than filament's default (I think it looks a little washed out)
	const float FilamentDefaultIndirectLightIntensity = 30_000f;
	const float TypicalNormalizedUpwardRadiance = 1f;
	const float LuminanceRedWeight = 0.2126f;
	const float LuminanceGreenWeight = 0.7152f;
	const float LuminanceBlueWeight = 0.0722f;
	const int KtxHeaderLength = 64;
	const uint KtxEndiannessNative = 0x04030201;
	const uint KtxEndiannessSwapped = 0x01020304;
	const int MinIntegratedFaceSize = 32;
	const int SphericalHarmonicsCoefficientCount = 27;
	const uint GlR11FG11FB10F = 0x8C3A;
	const uint GlRgba16F = 0x881A;
	const uint GlRgb16F = 0x881B;
	const uint GlRgba32F = 0x8814;
	const uint GlRgb32F = 0x8815;
	const uint GlRgba8 = 0x8058;
	const uint GlRgb8 = 0x8051;
	const uint GlSrgb8 = 0x8C41;
	const uint GlSrgb8Alpha8 = 0x8C43;
	const uint GlRgb = 0x1907;
	const uint GlRgba = 0x1908;
	const uint GlUnsignedByte = 0x1401;
	const uint GlHalfFloat = 0x140B;
	const uint GlFloat = 0x1406;

	static ReadOnlySpan<byte> KtxIdentifier => [0xAB, 0x4B, 0x54, 0x58, 0x20, 0x31, 0x31, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A];
	static ReadOnlySpan<byte> SphericalHarmonicsKey => "sh"u8;

	public static readonly float NativeIntensityAtUnitIntensity = FilamentDefaultIndirectLightIntensity * MathF.Pow(
		2f,
		(CameraExposurePreset.InsideBrightLighting.ToExposureParams().Ev100 + TinyFfrLightIntensityTasteEv100Adjustment) - CameraExposurePreset.OutsideMidday.ToExposureParams().Ev100
	);
	public static readonly float TypicalLuxAtUnitIntensity = NormalizedUpwardRadianceToLux(TypicalNormalizedUpwardRadiance);

	public static float SanitizeIntensity(float intensity) => Single.IsFinite(intensity) && intensity > 0f ? Single.Min(intensity, Scene.MaxBrightness) : 0f;

	public static float ToNativeIntensity(float intensity) {
		var sanitized = SanitizeIntensity(intensity);
		return sanitized * sanitized * NativeIntensityAtUnitIntensity;
	}

	public static float NormalizedUpwardRadianceToLux(float normalizedUpwardRadiance) {
		return Single.IsFinite(normalizedUpwardRadiance) ? MathF.PI * Single.Max(normalizedUpwardRadiance, 0f) * NativeIntensityAtUnitIntensity : 0f;
	}

	public static float LuxToIntensity(float lux, float luxAtUnitIntensity) {
		if (!Single.IsFinite(luxAtUnitIntensity) || luxAtUnitIntensity <= 0f) return 1f;
		if (!Single.IsFinite(lux) || lux <= 0f) return 0f;
		return Single.Min(MathF.Sqrt(lux / luxAtUnitIntensity), Scene.MaxBrightness);
	}

	public static float IntensityToLux(float intensity, float luxAtUnitIntensity) {
		var sanitized = SanitizeIntensity(intensity);
		return sanitized * sanitized * luxAtUnitIntensity;
	}

	public static float MeasureLuxAtUnitIntensity(ColorVect color) {
		return NormalizedUpwardRadianceToLux(CalculateLuminance(color.Red, color.Green, color.Blue));
	}

	public static float MeasureLuxAtUnitIntensity(ReadOnlySpan<byte> iblKtxData) {
		return TryMeasureNormalizedUpwardRadiance(iblKtxData, out var normalizedUpwardRadiance)
			? NormalizedUpwardRadianceToLux(normalizedUpwardRadiance)
			: TypicalLuxAtUnitIntensity;
	}

	internal static bool TryMeasureNormalizedUpwardRadiance(ReadOnlySpan<byte> iblKtxData, out float normalizedUpwardRadiance) {
		normalizedUpwardRadiance = default;
		if (!TryReadKtxHeader(iblKtxData, out var header)) return false;
		return TryReadSphericalHarmonicsUpwardRadiance(iblKtxData, header, out normalizedUpwardRadiance)
			|| TryIntegrateUpwardRadiance(iblKtxData, header, out normalizedUpwardRadiance);
	}

	internal static bool TryReadKtxHeader(ReadOnlySpan<byte> data, out KtxHeader header) {
		header = default;
		if (data.Length < KtxHeaderLength || !data[..KtxIdentifier.Length].SequenceEqual(KtxIdentifier)) return false;

		var endianness = BinaryPrimitives.ReadUInt32LittleEndian(data[12..]);
		bool isByteSwapped;
		if (endianness == KtxEndiannessNative) isByteSwapped = false;
		else if (endianness == KtxEndiannessSwapped) isByteSwapped = true;
		else return false;

		var width = ReadKtxUInt32(data, 36, isByteSwapped);
		var height = ReadKtxUInt32(data, 40, isByteSwapped);
		var arrayElements = ReadKtxUInt32(data, 48, isByteSwapped);
		var faces = ReadKtxUInt32(data, 52, isByteSwapped);
		var mipLevels = ReadKtxUInt32(data, 56, isByteSwapped);
		var keyValueDataLength = ReadKtxUInt32(data, 60, isByteSwapped);
		if (width is 0 or > Int32.MaxValue || height > Int32.MaxValue || arrayElements > Int32.MaxValue || faces is not (1 or 6) || mipLevels > 32) return false;
		if (keyValueDataLength > (uint) (data.Length - KtxHeaderLength)) return false;

		header = new KtxHeader(
			isByteSwapped,
			ReadKtxUInt32(data, 16, isByteSwapped),
			ReadKtxUInt32(data, 28, isByteSwapped),
			(int) width,
			(int) Math.Max(height, 1U),
			(int) arrayElements,
			(int) faces,
			(int) Math.Max(mipLevels, 1U),
			(int) keyValueDataLength
		);
		return true;
	}

	internal static bool TryReadSphericalHarmonicsUpwardRadiance(ReadOnlySpan<byte> data, in KtxHeader header, out float normalizedUpwardRadiance) {
		normalizedUpwardRadiance = default;
		if (!TryFindKtxKeyValue(data, header, SphericalHarmonicsKey, out var value)) return false;

		Span<float> coefficients = stackalloc float[SphericalHarmonicsCoefficientCount];
		for (var i = 0; i < coefficients.Length; ++i) {
			value = TrimStartWhitespace(value);
			if (!Utf8Parser.TryParse(value, out float coefficient, out var bytesConsumed) || !Single.IsFinite(coefficient)) return false;
			coefficients[i] = coefficient;
			value = value[bytesConsumed..];
		}

		var r = coefficients[0] + coefficients[3] - coefficients[18] - coefficients[24];
		var g = coefficients[1] + coefficients[4] - coefficients[19] - coefficients[25];
		var b = coefficients[2] + coefficients[5] - coefficients[20] - coefficients[26];
		normalizedUpwardRadiance = Single.Max(CalculateLuminance(r, g, b), 0f);
		return true;
	}

	internal static bool TryIntegrateUpwardRadiance(ReadOnlySpan<byte> data, in KtxHeader header, out float normalizedUpwardRadiance) {
		normalizedUpwardRadiance = default;
		if (header.IsByteSwapped || header.Faces != 6 || header.ArrayElements > 1 || header.Width != header.Height) return false;
		if (!TryGetTexelFormat(header, out var format)) return false;

		var bytesPerTexel = GetBytesPerTexel(format);
		var chosenLevel = 0;
		for (var level = 1; level < header.MipLevels; ++level) {
			if (Math.Max(1, header.Width >> level) < MinIntegratedFaceSize) break;
			chosenLevel = level;
		}

		var offset = (long) KtxHeaderLength + header.KeyValueDataLength;
		for (var level = 0; level <= chosenLevel; ++level) {
			var faceSize = Math.Max(1, header.Width >> level);
			var rowStride = AlignToFour((long) faceSize * bytesPerTexel);
			var faceByteLength = rowStride * faceSize;
			var paddedFaceByteLength = AlignToFour(faceByteLength);
			if (offset + sizeof(uint) > data.Length) return false;
			var imageSize = ReadKtxUInt32(data, (int) offset, false);
			if (imageSize != faceByteLength && imageSize != faceByteLength * header.Faces) return false;
			offset += sizeof(uint);
			if (offset + paddedFaceByteLength * header.Faces > data.Length) return false;

			if (level == chosenLevel) {
				normalizedUpwardRadiance = IntegrateCubemapUpwardRadiance(data.Slice((int) offset, (int) (paddedFaceByteLength * header.Faces)), format, faceSize, (int) rowStride, (int) paddedFaceByteLength);
				return Single.IsFinite(normalizedUpwardRadiance);
			}

			offset = AlignToFour(offset + paddedFaceByteLength * header.Faces);
		}

		return false;
	}

	static float IntegrateCubemapUpwardRadiance(ReadOnlySpan<byte> faceData, KtxTexelFormat format, int faceSize, int rowStride, int faceStride) {
		var bytesPerTexel = GetBytesPerTexel(format);
		var texelSpan = 2f / faceSize;
		var texelArea = texelSpan * texelSpan;
		var sum = 0d;

		for (var face = 0; face < 6; ++face) {
			if (face == 3) continue;
			var faceTexels = faceData.Slice(face * faceStride, faceStride);
			for (var y = 0; y < faceSize; ++y) {
				var t = (y + 0.5f) * texelSpan - 1f;
				var upComponent = face == 2 ? 1f : -t;
				if (upComponent <= 0f) continue;
				var row = faceTexels.Slice(y * rowStride, rowStride);
				for (var x = 0; x < faceSize; ++x) {
					var s = (x + 0.5f) * texelSpan - 1f;
					var lengthSquared = 1f + s * s + t * t;
					var length = MathF.Sqrt(lengthSquared);
					var cosine = upComponent / length;
					var solidAngle = texelArea / (lengthSquared * length);
					sum += DecodeTexelLuminance(row.Slice(x * bytesPerTexel, bytesPerTexel), format) * cosine * solidAngle;
				}
			}
		}

		return (float) (sum / Math.PI);
	}

	static bool TryFindKtxKeyValue(ReadOnlySpan<byte> data, in KtxHeader header, ReadOnlySpan<byte> key, out ReadOnlySpan<byte> value) {
		value = default;
		var keyValueData = data.Slice(KtxHeaderLength, header.KeyValueDataLength);
		while (keyValueData.Length >= sizeof(uint)) {
			var pairLength = ReadKtxUInt32(keyValueData, 0, header.IsByteSwapped);
			if (pairLength > (uint) (keyValueData.Length - sizeof(uint))) return false;
			var pair = keyValueData.Slice(sizeof(uint), (int) pairLength);
			var terminatorIndex = pair.IndexOf((byte) 0);
			if (terminatorIndex >= 0 && pair[..terminatorIndex].SequenceEqual(key)) {
				value = pair[(terminatorIndex + 1)..];
				var valueTerminatorIndex = value.IndexOf((byte) 0);
				if (valueTerminatorIndex >= 0) value = value[..valueTerminatorIndex];
				return true;
			}
			var advance = AlignToFour(sizeof(uint) + (long) pairLength);
			if (advance >= keyValueData.Length) return false;
			keyValueData = keyValueData[(int) advance..];
		}
		return false;
	}

	static bool TryGetTexelFormat(in KtxHeader header, out KtxTexelFormat format) {
		switch (header.GlInternalFormat) {
			case GlR11FG11FB10F: format = KtxTexelFormat.R11G11B10Float; return true;
			case GlRgba16F: format = KtxTexelFormat.Rgba16Float; return true;
			case GlRgb16F: format = KtxTexelFormat.Rgb16Float; return true;
			case GlRgba32F: format = KtxTexelFormat.Rgba32Float; return true;
			case GlRgb32F: format = KtxTexelFormat.Rgb32Float; return true;
			case GlRgba8: format = KtxTexelFormat.Rgba8Unorm; return true;
			case GlRgb8: format = KtxTexelFormat.Rgb8Unorm; return true;
			case GlSrgb8Alpha8: format = KtxTexelFormat.Srgba8; return true;
			case GlSrgb8: format = KtxTexelFormat.Srgb8; return true;
		}

		switch ((header.GlInternalFormat, header.GlType)) {
			case (GlRgba, GlHalfFloat): format = KtxTexelFormat.Rgba16Float; return true;
			case (GlRgb, GlHalfFloat): format = KtxTexelFormat.Rgb16Float; return true;
			case (GlRgba, GlFloat): format = KtxTexelFormat.Rgba32Float; return true;
			case (GlRgb, GlFloat): format = KtxTexelFormat.Rgb32Float; return true;
			case (GlRgba, GlUnsignedByte): format = KtxTexelFormat.Rgba8Unorm; return true;
			case (GlRgb, GlUnsignedByte): format = KtxTexelFormat.Rgb8Unorm; return true;
		}

		format = default;
		return false;
	}

	static int GetBytesPerTexel(KtxTexelFormat format) {
		return format switch {
			KtxTexelFormat.R11G11B10Float => 4,
			KtxTexelFormat.Rgba16Float => 8,
			KtxTexelFormat.Rgb16Float => 6,
			KtxTexelFormat.Rgba32Float => 16,
			KtxTexelFormat.Rgb32Float => 12,
			KtxTexelFormat.Rgba8Unorm or KtxTexelFormat.Srgba8 => 4,
			_ => 3
		};
	}

	internal static float DecodeTexelLuminance(ReadOnlySpan<byte> texel, KtxTexelFormat format) {
		float r, g, b;
		switch (format) {
			case KtxTexelFormat.R11G11B10Float:
				var packed = BinaryPrimitives.ReadUInt32LittleEndian(texel);
				r = DecodeUnsignedSmallFloat(packed & 0x7FFU, 6);
				g = DecodeUnsignedSmallFloat((packed >> 11) & 0x7FFU, 6);
				b = DecodeUnsignedSmallFloat((packed >> 22) & 0x3FFU, 5);
				break;
			case KtxTexelFormat.Rgba16Float:
			case KtxTexelFormat.Rgb16Float:
				r = (float) BinaryPrimitives.ReadHalfLittleEndian(texel);
				g = (float) BinaryPrimitives.ReadHalfLittleEndian(texel[2..]);
				b = (float) BinaryPrimitives.ReadHalfLittleEndian(texel[4..]);
				break;
			case KtxTexelFormat.Rgba32Float:
			case KtxTexelFormat.Rgb32Float:
				r = BinaryPrimitives.ReadSingleLittleEndian(texel);
				g = BinaryPrimitives.ReadSingleLittleEndian(texel[4..]);
				b = BinaryPrimitives.ReadSingleLittleEndian(texel[8..]);
				break;
			case KtxTexelFormat.Srgba8:
			case KtxTexelFormat.Srgb8:
				r = ColorVect.SrgbToLinear(texel[0] / 255f);
				g = ColorVect.SrgbToLinear(texel[1] / 255f);
				b = ColorVect.SrgbToLinear(texel[2] / 255f);
				break;
			default:
				r = texel[0] / 255f;
				g = texel[1] / 255f;
				b = texel[2] / 255f;
				break;
		}

		var luminance = CalculateLuminance(r, g, b);
		return Single.IsFinite(luminance) ? Single.Max(luminance, 0f) : 0f;
	}

	internal static float DecodeUnsignedSmallFloat(uint bits, int mantissaBitCount) {
		var exponent = (int) (bits >> mantissaBitCount) & 0x1F;
		var mantissa = bits & ((1U << mantissaBitCount) - 1U);
		var mantissaScale = 1f / (1U << mantissaBitCount);
		if (exponent == 0) return MathF.ScaleB(mantissa * mantissaScale, -14);
		if (exponent == 0x1F) return 0f;
		return MathF.ScaleB(1f + mantissa * mantissaScale, exponent - 15);
	}

	static float CalculateLuminance(float r, float g, float b) => r * LuminanceRedWeight + g * LuminanceGreenWeight + b * LuminanceBlueWeight;

	static uint ReadKtxUInt32(ReadOnlySpan<byte> data, int offset, bool isByteSwapped) {
		return isByteSwapped ? BinaryPrimitives.ReadUInt32BigEndian(data[offset..]) : BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
	}

	static long AlignToFour(long value) => (value + 3L) & ~3L;

	static ReadOnlySpan<byte> TrimStartWhitespace(ReadOnlySpan<byte> value) {
		var index = 0;
		while (index < value.Length && value[index] is (byte) ' ' or (byte) '\n' or (byte) '\r' or (byte) '\t') ++index;
		return value[index..];
	}
}
