// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Size = sizeof(float), Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from float
public readonly partial struct Angle : IMathPrimitive<Angle>, IComparable<Angle>, IComparisonOperators<Angle, Angle, bool> {
	public const string StringSuffix = "°";
	const float Tau = MathF.Tau;
	const float TauReciprocal = 1f / MathF.Tau;
	const float RadiansToDegreesRatio = 360f / Tau;
	const float DegreesToRadiansRatio = Tau / 360f;
	public static readonly Angle None = FromRadians(0f);
	public static readonly Angle QuarterCircle = FromRadians(Tau * 0.25f);
	public static readonly Angle HalfCircle = FromRadians(Tau * 0.5f);
	public static readonly Angle ThreeQuarterCircle = FromRadians(Tau * 0.75f);
	public static readonly Angle FullCircle = FromRadians(Tau * 1f);

	readonly float _asRadians;

	public float Radians {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _asRadians;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _asRadians = value;
	}
	public float Degrees {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Radians * RadiansToDegreesRatio;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => Radians = value * DegreesToRadiansRatio;
	}
	public Fraction FullCircleFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Radians * TauReciprocal;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => Radians = Tau * value;
	}

	// Chose degrees rather than radians to keep consistency with implicit conversion. See notes above implicit operator for more reasoning.
	public Angle(float degrees) => Degrees = degrees;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle FromRadians(float radians) => new() { Radians = radians };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle FromDegrees(float degrees) => new() { Degrees = degrees };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle FromFullCircleFraction(Fraction fullCircleFraction) => new() { FullCircleFraction = fullCircleFraction };
	public static Angle FromAngleBetweenDirections(Direction d1, Direction d2) => FromRadians(MathF.Acos(Vector4.Dot(d1.AsVector4, d2.AsVector4)));

	/* I thought long and hard about whether this conversion should even exist and what it should assume the operand is.
	 * Arguments for/against 'operand' being:
	 * -- Coefficient of full circle:
	 *		It would be nice to specify some things as fractions of a full turn. For example, if I want to turn 30%
	 *		to the right, I could just specify a Rotation as 0.3f * Direction.Up.
	 *		In the end though, the static factory method is probably enough. I don't
	 *		think there's a natural idea of a conversion between Fraction<->Angle without a context of what that
	 *		means-- the name of the static factory method provides that context.
	 * -- Radians:
	 *		It will probably be obvious to a sizable chunk of users using this API that this type most naturally
	 *		represents a value in radians, so perhaps an implicit conversion from radians made the most sense.
	 *		However, this API (and library) is meant to be usable by people who don't have a strong background
	 *		in maths; and the fact this this type is ultimately abstracting over a value in radians actually has
	 *		a performance reasoning behind it more than anything (i.e. it makes it easy to feed in to underlying
	 *		libraries that work with radians). Radians are ultimately probably the least user-friendly abstraction
	 *		for encoding angles (from a software engineering perspective, not a math perspective) as they require
	 *		thinking in multiples of pi. It's tedious to think of "rotating 30% to the right" and having to work that
	 *		out as Pi * 0.15f IMO. Implicit conversions are basically convenience methods so offering a
	 *		convenience method that requires you to write "MathF.PI * " every time you want to specify an angle constant
	 *		is a little contrived. Perhaps it's naive but if I write this API/type correctly it should be possible to use the
	 *		entire library oblivious to the idea of radians, and that's what I'm aiming for.
	 * -- Degrees:
	 *		Degrees are probably the unit that most people in the world are most familiar with. It's also the unit I
	 *		chose to output in ToString & related methods for that very reason. I think it would be odd to be able to
	 *		specify an Angle using a float literal in one unit and then have the ToString/Parse methods work with
	 *		another unit. Degrees therefore feels like the most natural fit that has the least friction in general
	 *		across the entire Angle type. You can specify an Angle as "270f" or "Angle.Parse("270") and get the same
	 *		result each time-- I think that's really important. Finally, one minor positive of using degrees is that
	 *		the range people will be working with is a lot less prone to floating point inaccuracy build-up (e.g.
	 *		-720f to 720f is a lot safer to manipulate than -4pi to 4pi and/or -2f to 2f). That positive falls on its
	 *		arse once actually converted to Angle (radians under the hood) but at least it might encourage working in
	 *		degrees naturally in places. Maybe.
	 *
	 * Another note: I probably won't include the opposite implicit conversion (e.g. Angle->float) because I think it's
	 * probably just error prone AF and I don't think specifying .Degrees is very onerous anyway. I actually don't think there's
	 * actually much use-case for it too: When you wanna convert to string, use ToString(), and when dealing with third-party
	 * APIs (e.g. math libs) you'll actually more likely want .Radians. And within this API/lib I will be using Angle
	 * everywhere so there shouldn't be much need to get a float value back out at all. The implicit conversion from
	 * float->Angle is just something to help quickly specify Angle "literals"-- not a declaration that there is a pure
	 * natural link between float and Angle. Angle to float makes a lot less sense for these reasons IMO.
	 */
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Angle(float operand) => FromDegrees(operand);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<float> ConvertToSpan(in Angle src) => new(src._asRadians);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle ConvertFromSpan(ReadOnlySpan<float> src) => FromRadians(src[0]);

	public override string ToString() => ToString(null, null);

	public string ToString(string? format, IFormatProvider? formatProvider) => $"{Degrees.ToString(format, formatProvider)}{StringSuffix}";

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		charsWritten = 0;
		// ReSharper disable once InlineOutVariableDeclaration This is neater
		int tryWriteCharsWrittenOutVar;
		// ReSharper disable once JoinDeclarationAndInitializer This is neater
		bool writeSuccess;

		writeSuccess = Degrees.TryFormat(destination, out tryWriteCharsWrittenOutVar, format, provider);
		charsWritten += tryWriteCharsWrittenOutVar;
		if (!writeSuccess) return false;
		destination = destination[tryWriteCharsWrittenOutVar..];

		writeSuccess = destination.TryWrite($"{StringSuffix}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		return writeSuccess;
	}

	public static Angle Parse(string s, IFormatProvider? provider = null) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out Angle result) => TryParse(s.AsSpan(), provider, out result);

	public static Angle Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) {
		var indexOfSuffix = s.IndexOf(StringSuffix);

		var degrees = indexOfSuffix >= 0
			? Single.Parse(s[..indexOfSuffix], provider)
			: Single.Parse(s, provider);

		return FromDegrees(degrees);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Angle result) {
		var indexOfSuffix = s.IndexOf(StringSuffix);
		if (indexOfSuffix < 0) indexOfSuffix = s.Length;

		if (!Single.TryParse(s[..indexOfSuffix], provider, out var degrees)) {
			result = default;
			return false;
		}

		result = FromDegrees(degrees);
		return true;
	}

	public bool Equals(Angle other, float tolerance) {
		// Using Degrees rather than _asRadians because the implicit conversion from float to Angle
		// assumes degrees and therefore I feel like the tolerance value here should also be degrees
		return MathF.Abs(Degrees - other.Degrees) <= tolerance;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Angle other) => _asRadians.Equals(other._asRadians);
	public override bool Equals(object? obj) => obj is Angle other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => _asRadians.GetHashCode();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Angle left, Angle right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Angle left, Angle right) => !left.Equals(right);
}