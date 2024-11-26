// Created on 2024-10-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;

namespace Egodystonic.TinyFFR;

// Maintainer's note: I mostly named this "ColorVect" rather than "Color" simply to differentiate it from all the other "Color" structs in various common libraries.
// But it does also make it clearer immediately that this is stored in 4-float format.
[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 4, Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from Vector4
public readonly partial struct ColorVect : IVect<ColorVect> {
	public static readonly Angle RedHueAngle = 0f;
	public static readonly Angle GreenHueAngle = 120f;
	public static readonly Angle BlueHueAngle = 240f;

	internal readonly Vector4 AsVector4;

	public float Red {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.X;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector4.X = value;
	}

	public float Green {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.Y;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector4.Y = value;
	}

	public float Blue {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.Z;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector4.Z = value;
	}

	public float Alpha {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsVector4.W;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsVector4.W = value;
	}

	public Angle Hue {
		get {
			ToHueSaturationLightness(out var result, out _, out _);
			return result;
		}
	}

	public float Saturation {
		get {
			ToHueSaturationLightness(out _, out var result, out _);
			return result;
		}
	}

	public float Lightness {
		get {
			ToHueSaturationLightness(out _, out _, out var result);
			return result;
		}
	}

	public float this[ColorChannel channel] => channel switch {
		ColorChannel.R => Red,
		ColorChannel.G => Green,
		ColorChannel.B => Blue,
		_ => Alpha
	};
	public XYPair<float> this[ColorChannel first, ColorChannel second] => new(this[first], this[second]);
	public ColorVect this[ColorChannel first, ColorChannel second, ColorChannel third] => new(this[first], this[second], this[third]);
	public ColorVect this[ColorChannel first, ColorChannel second, ColorChannel third, ColorChannel fourth] => new(this[first], this[second], this[third], this[fourth]);

	float IVect.X => Red;
	float IVect.Y => Green;
	float IVect.Z => Blue;
	float IVect.this[Axis axis] => this[(ColorChannel) axis];
	XYPair<float> IVect.this[Axis first, Axis second] => new(this[(ColorChannel) first], this[(ColorChannel) second]);
	ColorVect IVect<ColorVect>.this[Axis first, Axis second, Axis third] => new(this[(ColorChannel) first], this[(ColorChannel) second], this[(ColorChannel) third]);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect() : this(0f, 0f, 0f) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect(float red, float green, float blue) : this(red, green, blue, 1f) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect(float red, float green, float blue, float alpha) : this(new Vector4(red, green, blue, alpha)) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ColorVect(Vector4 v) { AsVector4 = v; }

	#region Factories and Conversions
	public static ColorVect PremultiplyAlpha(ColorVect nonpremultipliedInput) {
		return new(
			nonpremultipliedInput.Red * nonpremultipliedInput.Alpha,
			nonpremultipliedInput.Green * nonpremultipliedInput.Alpha,
			nonpremultipliedInput.Blue * nonpremultipliedInput.Alpha,
			nonpremultipliedInput.Alpha
		);
	}

	public static ColorVect FromRgba32(uint rgba) {
		const float Multiplicand = 1f / Byte.MaxValue;
		return new(new Vector4(
			(0xFF000000 & rgba) >> 24,
			(0xFF0000 & rgba) >> 16,
			(0xFF00 & rgba) >> 8,
			0xFF & rgba
		) * Multiplicand);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect FromRgba32(int rgba) => FromRgba32((uint) rgba);

	public static ColorVect FromRgba32(byte r, byte g, byte b, byte a) {
		const float Multiplicand = 1f / Byte.MaxValue;
		return new(new Vector4(r, g, b, a) * Multiplicand);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect FromRgb24(uint rgb) => FromRgba32((rgb << 8) | Byte.MaxValue);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect FromRgb24(int rgb) => FromRgb24((uint) rgb);

	public static ColorVect FromRgb24(byte r, byte g, byte b) => FromRgba32(r, g, b, Byte.MaxValue);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect FromHueSaturationLightness(Angle hue, float saturation, float lightness) => FromHueSaturationLightness(hue, saturation, lightness, 1f);
	public static ColorVect FromHueSaturationLightness(Angle hue, float saturation, float lightness, float alpha) {
		const float SixthCircleRads = MathF.Tau / 6f;

		hue = hue.Normalized;
		var hueRads = hue.Radians;
		saturation = Single.Clamp(saturation, 0f, 1f);
		lightness = Single.Clamp(lightness, 0f, 1f);
		alpha = Single.Clamp(alpha, 0f, 1f);

		var c = (1f - MathF.Abs(2f * lightness - 1f)) * saturation;
		var x = c * (1f - MathF.Abs(MathUtils.TrueModulus(hueRads / SixthCircleRads, 2f) - 1f));
		var m = lightness - c * 0.5f;

		return hueRads switch {
			< 1f * SixthCircleRads => new(m + c, m + x, m, alpha),
			< 2f * SixthCircleRads => new(m + x, m + c, m, alpha),
			< 3f * SixthCircleRads => new(m, m + c, m + x, alpha),
			< 4f * SixthCircleRads => new(m, m + x, m + c, alpha),
			< 5f * SixthCircleRads => new(m + x, m, m + c, alpha),
			_ => new(m + c, m, m + x, alpha)
		};
	}

	public void ToHueSaturationLightness(out Angle outHue, out float outSaturation, out float outLightness) {
		const float SixthCircleRads = MathF.Tau / 6f;

		var cMax = MathF.Max(Red, MathF.Max(Green, Blue));
		var cMin = MathF.Min(Red, MathF.Min(Green, Blue));
		var delta = cMax - cMin;

		outLightness = (cMax + cMin) * 0.5f;

		// ReSharper disable CompareOfFloatsByEqualityOperator Direct comparison is correct here as we're comparing with the returned value of MathF.Max which should return exactly one of its inputs
		if (delta == 0f) {
			outHue = Angle.Zero;
			outSaturation = 0f;
			return;
		}

		if (cMax == Red) {
			outHue = Angle.FromRadians(SixthCircleRads * MathUtils.TrueModulus((Green - Blue) / delta, 6f));
		}
		else if (cMax == Green) {
			outHue = Angle.FromRadians(SixthCircleRads * ((Blue - Red) / delta + 2f));
		}
		else {
			outHue = Angle.FromRadians(SixthCircleRads * ((Red - Green) / delta + 4f));
		}
		outSaturation = delta / (1f - MathF.Abs(cMax + cMin - 1f));
		// ReSharper restore CompareOfFloatsByEqualityOperator
	}

	public uint ToRgba32() {
		ToRgba32(out var r, out var g, out var b, out var a);
		return (uint) ((r << 24) + (g << 16) + (b << 8) + a);
	}
	public void ToRgba32(out byte r, out byte g, out byte b, out byte a) {
		const float Multiplicand = Byte.MaxValue;

		var v = AsVector4 * Multiplicand;
		r = (byte) Single.Clamp(v.X, Byte.MinValue, Byte.MaxValue);
		g = (byte) Single.Clamp(v.Y, Byte.MinValue, Byte.MaxValue);
		b = (byte) Single.Clamp(v.Z, Byte.MinValue, Byte.MaxValue);
		a = (byte) Single.Clamp(v.W, Byte.MinValue, Byte.MaxValue);
	}

	public uint ToRgb24() {
		ToRgb24(out var r, out var g, out var b);
		return (uint) ((r << 16) + (g << 8) + b);
	}
	public void ToRgb24(out byte r, out byte g, out byte b) {
		const float Multiplicand = Byte.MaxValue;

		var v = AsVector4 * Multiplicand;
		r = (byte) Single.Clamp(v.X, Byte.MinValue, Byte.MaxValue);
		g = (byte) Single.Clamp(v.Y, Byte.MinValue, Byte.MaxValue);
		b = (byte) Single.Clamp(v.Z, Byte.MinValue, Byte.MaxValue);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect FromVector3(Vector3 v) => new(v.X, v.Y, v.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect FromVector4(Vector4 v) => new(v);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector3 ToVector3() => new(AsVector4.X, AsVector4.Y, AsVector4.Z);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector4 ToVector4() => AsVector4;

	public void Deconstruct(out float red, out float green, out float blue) {
		red = Red;
		green = Green;
		blue = Blue;
	}
	public static implicit operator ColorVect((float Red, float Green, float Blue) tuple) => new(tuple.Red, tuple.Green, tuple.Blue);
	static implicit IVect<ColorVect>.operator ColorVect((float X, float Y, float Z) tuple) => new(tuple.X, tuple.Y, tuple.Z);
	public static implicit operator ColorVect((float Red, float Green, float Blue, float Alpha) tuple) => new(tuple.Red, tuple.Green, tuple.Blue, tuple.Alpha);
	public void Deconstruct(out float red, out float green, out float blue, out float alpha) {
		red = Red;
		green = Green;
		blue = Blue;
		alpha = Alpha;
	}

	public static implicit operator ColorVect(int rgb) => FromRgba32(rgb);
	public static implicit operator ColorVect(uint rgb) => FromRgba32(rgb);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Vect IVect.AsVect() => Vect.FromVector3(ToVector3());
	#endregion

	#region Random
	public static ColorVect Random() {
		return new(
			RandomUtils.NextSingleZeroToOneInclusive(),
			RandomUtils.NextSingleZeroToOneInclusive(),
			RandomUtils.NextSingleZeroToOneInclusive(),
			RandomUtils.NextSingleZeroToOneInclusive()
		);
	}
	public static ColorVect RandomOpaque() {
		return new(
			RandomUtils.NextSingleZeroToOneInclusive(),
			RandomUtils.NextSingleZeroToOneInclusive(),
			RandomUtils.NextSingleZeroToOneInclusive()
		);
	}
	public static ColorVect Random(ColorVect minInclusive, ColorVect maxExclusive) {
		return new(
			RandomUtils.NextSingle(minInclusive.Red, maxExclusive.Red),
			RandomUtils.NextSingle(minInclusive.Green, maxExclusive.Green),
			RandomUtils.NextSingle(minInclusive.Blue, maxExclusive.Blue),
			RandomUtils.NextSingle(minInclusive.Alpha, maxExclusive.Alpha)
		);
	}
	#endregion

	#region Span Conversion
	public static int SerializationByteSpanLength { get; } = sizeof(float) * 4;

	public static void SerializeToBytes(Span<byte> dest, ColorVect src) {
		BinaryPrimitives.WriteSingleLittleEndian(dest, src.Red);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 1)..], src.Green);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 2)..], src.Blue);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 3)..], src.Alpha);
	}

	public static ColorVect DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			BinaryPrimitives.ReadSingleLittleEndian(src),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 1)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 2)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 3)..])
		);
	}
	#endregion

	#region String Conversion
	public const char RedChar = 'R';
	public const char GreenChar = 'G';
	public const char BlueChar = 'B';
	public const char AlphaChar = 'A';

	public override string ToString() => ToString(null, null);

	public string ToString(string? format, IFormatProvider? formatProvider) {
		return IVect.VectorStringPrefixChar +
			   $"{RedChar} {PercentageUtils.ConvertFractionToPercentageString(Red, format, formatProvider)}, " +
			   $"{GreenChar} {PercentageUtils.ConvertFractionToPercentageString(Green, format, formatProvider)}, " +
			   $"{BlueChar} {PercentageUtils.ConvertFractionToPercentageString(Blue, format, formatProvider)}, " +
			   $"{AlphaChar} {PercentageUtils.ConvertFractionToPercentageString(Alpha, format, formatProvider)}" +
			   IVect.VectorStringSuffixChar;
	}
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		charsWritten = 0;
		// ReSharper disable once InlineOutVariableDeclaration This is neater
		int tryWriteCharsWrittenOutVar;
		// ReSharper disable once JoinDeclarationAndInitializer This is neater
		bool writeSuccess;

		// <
		if (destination.Length == 0) return false;
		destination[0] = IVect.VectorStringPrefixChar;
		charsWritten++;
		destination = destination[1..];



		// R
		if (destination.Length < 2) return false;
		destination[0] = RedChar;
		destination[1] = ' ';
		charsWritten += 2;
		destination = destination[2..];

		// Red
		writeSuccess = PercentageUtils.TryFormatFractionToPercentageString(Red, destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// ,
		writeSuccess = destination.TryWrite($"{numberFormatter.NumberGroupSeparator} ", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];



		// G
		if (destination.Length < 2) return false;
		destination[0] = GreenChar;
		destination[1] = ' ';
		charsWritten += 2;
		destination = destination[2..];

		// Green
		writeSuccess = PercentageUtils.TryFormatFractionToPercentageString(Green, destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// ,
		writeSuccess = destination.TryWrite($"{numberFormatter.NumberGroupSeparator} ", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];



		// B
		if (destination.Length < 2) return false;
		destination[0] = BlueChar;
		destination[1] = ' ';
		charsWritten += 2;
		destination = destination[2..];

		// Blue
		writeSuccess = PercentageUtils.TryFormatFractionToPercentageString(Blue, destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// ,
		writeSuccess = destination.TryWrite($"{numberFormatter.NumberGroupSeparator} ", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];



		// A
		if (destination.Length < 2) return false;
		destination[0] = AlphaChar;
		destination[1] = ' ';
		charsWritten += 2;
		destination = destination[2..];

		// Alpha
		writeSuccess = PercentageUtils.TryFormatFractionToPercentageString(Alpha, destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];



		// >
		if (destination.Length == 0) return false;
		destination[0] = IVect.VectorStringSuffixChar;
		charsWritten++;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect Parse(string s, IFormatProvider? provider = null) => Parse(s.AsSpan(), provider);

	public static bool TryParse(string? s, IFormatProvider? provider, out ColorVect result) {
		if (s != null && TryParse(s.AsSpan(), provider, out result)) return true;
		result = default;
		return false;
	}

	public static ColorVect Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		s = s[1..]; // Assume starts with VectorStringPrefixChar

		var indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		var red = PercentageUtils.ParsePercentageStringToFraction(s[2..indexOfSeparator], provider);
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		var green = PercentageUtils.ParsePercentageStringToFraction(s[2..indexOfSeparator], provider);
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		var blue = PercentageUtils.ParsePercentageStringToFraction(s[2..indexOfSeparator], provider);
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		// Assume ends with VectorStringSuffixChar
		var alpha = PercentageUtils.ParsePercentageStringToFraction(s[2..^1], provider); 

		return new(red, green, blue, alpha);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ColorVect result) {
		var numberFormatter = NumberFormatInfo.GetInstance(provider);
		result = default;

		if (s.Length <= 2) return false;
		if (s[0] != IVect.VectorStringPrefixChar) return false;
		if (s[^1] != IVect.VectorStringSuffixChar) return false;
		s = s[1..^1];

		var indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		if (indexOfSeparator < 2 || s[0] != RedChar || s[1] != ' ') return false;
		if (!PercentageUtils.TryParsePercentageStringToFraction(s[2..indexOfSeparator], provider, out var red)) return false;
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];
		if (s.Length == 0 || s[0] != ' ') return false;
		s = s[1..];

		indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		if (indexOfSeparator < 2 || s[0] != GreenChar || s[1] != ' ') return false;
		if (!PercentageUtils.TryParsePercentageStringToFraction(s[2..indexOfSeparator], provider, out var green)) return false;
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];
		if (s.Length == 0 || s[0] != ' ') return false;
		s = s[1..];

		indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		if (indexOfSeparator < 2 || s[0] != BlueChar || s[1] != ' ') return false;
		if (!PercentageUtils.TryParsePercentageStringToFraction(s[2..indexOfSeparator], provider, out var blue)) return false;
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];
		if (s.Length == 0 || s[0] != ' ') return false;
		s = s[1..];

		if (s.Length < 4 || s[0] != AlphaChar || s[1] != ' ') return false;
		if (!PercentageUtils.TryParsePercentageStringToFraction(s[2..^1], provider, out var alpha)) return false;

		result = new(red, green, blue, alpha);
		return true;
	}
	#endregion

	#region Equality
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(ColorVect other) => AsVector4.Equals(other.AsVector4);
	public bool Equals(ColorVect other, float tolerance) {
		return MathF.Abs(Red - other.Red) <= tolerance
			&& MathF.Abs(Green - other.Green) <= tolerance
			&& MathF.Abs(Blue - other.Blue) <= tolerance
			&& MathF.Abs(Alpha - other.Alpha) <= tolerance;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(ColorVect left, ColorVect right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(ColorVect left, ColorVect right) => !left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => obj is ColorVect other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => AsVector4.GetHashCode();
	#endregion
}