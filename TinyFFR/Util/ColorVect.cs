// Created on 2024-10-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;

namespace Egodystonic.TinyFFR;

// Maintainer's note: I mostly named this "ColorVect" rather than "Color" simply to differentiate it from all the other "Color" structs in various common libraries.
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

		}
	}

	public float Saturation {
		get {

		}
	}

	public float Lightness {
		get {

		}
	}

	public float this[Axis axis] => axis switch {
		Axis.X => Red,
		Axis.Y => Green,
		Axis.Z => Blue,
		_ => throw new ArgumentOutOfRangeException(nameof(axis), axis, $"{nameof(Axis)} must not be anything except {nameof(Axis.X)}, {nameof(Axis.Y)} or {nameof(Axis.Z)}.")
	};
	public XYPair<float> this[Axis first, Axis second] => new(this[first], this[second]);
	public ColorVect this[Axis first, Axis second, Axis third] => new(this[first], this[second], this[third]);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect() : this(0f, 0f, 0f) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect(float red, float green, float blue) : this(red, green, blue, 1f) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ColorVect(float red, float green, float blue, float alpha) : this(new Vector4(red, green, blue, alpha)) { }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ColorVect(Vector4 v) { AsVector4 = v; }

	#region Factories and Conversions
	public static ColorVect From32BitArgb(uint argb) {
		const float ByteMaxReciprocalAsFloat = 1f / Byte.MaxValue;
		return new ColorVect(
			((0xFF0000 & argb) >> 16) * ByteMaxReciprocalAsFloat,
			((0xFF00 & argb) >> 8) * ByteMaxReciprocalAsFloat,
			(0xFF & argb) * ByteMaxReciprocalAsFloat,
			((0xFF000000 & argb) >> 24) * ByteMaxReciprocalAsFloat
		);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect From32BitArgb(int argb) => From32BitArgb((uint) argb);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect FromHueSaturationLightness(Angle hue, float saturation, float lightness) => FromHueSaturationLightness(hue, saturation, lightness, 1f);
	public static ColorVect FromHueSaturationLightness(Angle hue, float saturation, float lightness, float alpha) {
		hue = hue.Normalized;
		var hueDegrees = hue.AsDegrees;
		saturation = Single.Clamp(saturation, 0f, 1f);
		lightness = Single.Clamp(lightness, 0f, 1f);
		alpha = Single.Clamp(alpha, 0f, 1f);

		var c = (1f - MathF.Abs(2f * lightness - 1f)) * saturation;
		var x = c * (1f - MathF.Abs(MathUtils.TrueModulus(hueDegrees / Angle.SixthCircle.AsDegrees, 2f) - 1f));
		var m = lightness - c * 0.5f;

		return hueDegrees switch {
			< 60f => new(c + m, x + m, 0f, alpha),
		};
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

	public void Deconstruct(out float red, out float green, out float blue, out float alpha) {
		red = Red;
		green = Green;
		blue = Blue;
		alpha = Alpha;
	}
	public static implicit operator ColorVect((float Red, float Green, float Blue, float Alpha) tuple) => new(tuple.Red, tuple.Green, tuple.Blue, tuple.Alpha);

	public static implicit operator ColorVect(int rgba) => From32BitArgb(rgba);
	public static implicit operator ColorVect(uint rgba) => From32BitArgb(rgba);

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

		// [
		if (destination.Length == 0) return false;
		destination[0] = IVect.VectorStringPrefixChar;
		charsWritten++;
		destination = destination[1..];



		// R
		if (destination.Length == 0) return false;
		destination[0] = RedChar;
		charsWritten++;
		destination = destination[1..];

		// Red
		writeSuccess = Red.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// ,
		writeSuccess = destination.TryWrite($"{numberFormatter.NumberGroupSeparator} ", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];



		// G
		if (destination.Length == 0) return false;
		destination[0] = GreenChar;
		charsWritten++;
		destination = destination[1..];

		// Green
		writeSuccess = Green.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// ,
		writeSuccess = destination.TryWrite($"{numberFormatter.NumberGroupSeparator} ", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];



		// B
		if (destination.Length == 0) return false;
		destination[0] = BlueChar;
		charsWritten++;
		destination = destination[1..];

		// Blue
		writeSuccess = Blue.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		// ,
		writeSuccess = destination.TryWrite($"{numberFormatter.NumberGroupSeparator} ", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];



		// A
		if (destination.Length == 0) return false;
		destination[0] = AlphaChar;
		charsWritten++;
		destination = destination[1..];

		// Alpha
		writeSuccess = Alpha.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];



		// [
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

		indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		if (indexOfSeparator < 2 || s[0] != GreenChar || s[1] != ' ') return false;
		if (!PercentageUtils.TryParsePercentageStringToFraction(s[2..indexOfSeparator], provider, out var green)) return false;
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

		indexOfSeparator = s.IndexOf(numberFormatter.NumberGroupSeparator);
		if (indexOfSeparator < 2 || s[0] != BlueChar || s[1] != ' ') return false;
		if (!PercentageUtils.TryParsePercentageStringToFraction(s[2..indexOfSeparator], provider, out var blue)) return false;
		s = s[(indexOfSeparator + numberFormatter.NumberGroupSeparator.Length)..];

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