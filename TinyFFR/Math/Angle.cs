﻿// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Buffers.Binary;
using System.Drawing;

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Size = sizeof(float), Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from float
public readonly partial struct Angle : IMathPrimitive<Angle> {
	public const string ToStringSuffix = "°";
	const float Tau = MathF.Tau;
	const float TauReciprocal = 1f / MathF.Tau;
	const float RadiansToDegreesRatio = 360f / Tau;
	const float DegreesToRadiansRatio = Tau / 360f;
	public static readonly Angle Zero = FromRadians(0f);
	public static readonly Angle EighthCircle = FromRadians(Tau * 0.125f);
	public static readonly Angle SixthCircle = FromRadians(Tau / 6f);
	public static readonly Angle ThirdCircle = FromRadians(Tau / 3f);
	public static readonly Angle QuarterCircle = FromRadians(Tau * 0.25f);
	public static readonly Angle HalfCircle = FromRadians(Tau * 0.5f);
	public static readonly Angle ThreeQuarterCircle = FromRadians(Tau * 0.75f);
	public static readonly Angle FullCircle = FromRadians(Tau * 1f);

	readonly float _radians;

	public float Radians {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _radians;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private init => _radians = value;
	}
	public float Degrees {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Radians * RadiansToDegreesRatio;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private init => Radians = value * DegreesToRadiansRatio;
	}
	public float FullCircleFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Radians * TauReciprocal;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private init => Radians = Tau * value;
	}

	// Chose degrees rather than radians to keep consistency with implicit conversion. See notes above implicit operator for more reasoning.
	public Angle(float degrees) => Degrees = degrees;

	#region Factories and Conversions
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle FromRadians(float radians) => new() { Radians = radians };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle FromDegrees(float degrees) => new() { Degrees = degrees };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle FromFullCircleFraction(float fullCircleFraction) => new() { FullCircleFraction = fullCircleFraction };
	
	public static Angle FromSine(float sine) {
		sine = Single.Clamp(sine, -1f, 1f);
		return FromRadians(MathF.Asin(sine));
	}

	public static Angle FromCosine(float cosine) {
		cosine = Single.Clamp(cosine, -1f, 1f);
		return FromRadians(MathF.Acos(cosine));
	}

	public static Angle FromAngleBetweenDirections(Direction d1, Direction d2) {
		const float FloatingPointErrorMargin = 1E-6f;

		if (d1 == Direction.None || d2 == Direction.None) return Zero;

		// This switch tries to take care of some FP inaccuracy.
		// Throughout my testing I've found that having two identical vectors return an angle of "0.02degrees" instead of exactly 0 is frankly just irritating
		// and throws off a lot of other assumptions throughout the codebase (similar for -v and v not returning 180 exactly, etc).
		// Having "near enough" results 'clamp' to 0/90/180deg is a much less egregious "hack" for FP inaccuracy than having those identical vectors not return perfect results IMO.
		// The other nice thing this does is make sure 'dot' never exceeds the [-1, 1] range (which causes NaNs from arccos).
		// And ultimately, if users REALLY don't want this, they can just do it manually for now (it's not that hard to write a simpler version, it's just arccos(dot(d1, d2))).
		var dot = Vector4.Dot(d1.AsVector4, d2.AsVector4);
		return dot switch {
			> 1f - FloatingPointErrorMargin => Zero,
			> -FloatingPointErrorMargin and < FloatingPointErrorMargin => QuarterCircle,
			< -(1f - FloatingPointErrorMargin) => HalfCircle,
			_ => FromCosine(dot)
		};
	}

	// TODO clarify this is the four-quadrant inverse tangent
	public static Angle? From2DPolarAngle<T>(XYPair<T> xy) where T : unmanaged, INumber<T> => From2DPolarAngle(Single.CreateTruncating(xy.X), Single.CreateTruncating(xy.Y));
	// TODO clarify this is the four-quadrant inverse tangent
	public static Angle? From2DPolarAngle(float x, float y) {
		if (x == 0f && y == 0f) return null;
		return FromRadians(MathF.Atan2(y, x)).Normalized;
	}
	// TODO clarify this is the four-quadrant inverse tangent
	public static Angle? From2DPolarAngle(Orientation2D orientation) => orientation switch {
		Orientation2D.None => null,
		Orientation2D.Right => 0f,
		Orientation2D.UpRight => 45f,
		Orientation2D.Up => 90f,
		Orientation2D.UpLeft => 135f,
		Orientation2D.Left => 180f,
		Orientation2D.DownLeft => 225f,
		Orientation2D.Down => 270f,
		Orientation2D.DownRight => 315f,
		_ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, "Orientation must be defined.")
	};

	/* I thought long and hard about whether this conversion should even exist and what it should assume the operand is.
	 * Arguments for/against 'operand' being:
	 * -- Full circle fraction:
	 *		It would be nice to specify some things as fractions of a full turn. For example, if I want to turn 30%
	 *		to the right, I could just specify a Rotation as 0.3f * Direction.Up.
	 *		In the end though, the static factory method is probably enough. I don't
	 *		think there's a natural idea of a float naturally being a fraction of a full turn without a context of what that
	 *		means-- the name of the static factory method provides that context, but an implicit conversion would leave
	 *		the reader of the code guessing/assuming.
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
	 *		convenience method that requires you to write "MathF.PI * " every time you want to specify an angle literal
	 *		is a little contrived. Perhaps it's naive but if I write this API/type correctly it should be possible to use the
	 *		entire library oblivious to the idea of radians, and that's what I'm aiming for.
	 *
	 *		I do foresee one group of users who would prefer this in radians-- people writing scientific/mathematical
	 *		software who will be doing a lot of calculations in radians naturally anyway. To those people... Sorry :).
	 *		Angle.FromRadians() will still be there for you! But in actuality I don't foresee this being as big a deal
	 *		as it maybe first seems because most scientific/math libraries tend to use full-precision floats (e.g. double or higher)
	 *		which won't implicitly convert to Angles anyway.
	 * -- Degrees:
	 *		Degrees are probably the unit that most people in the world are most familiar with. It's also the unit I
	 *		chose to output in ToString & related methods for that very reason. I think it would be odd to be able to
	 *		specify an Angle using a float literal in one unit and then have the ToString/Parse methods work with
	 *		another unit. Degrees therefore feels like the most natural fit that has the least friction in general
	 *		across the entire Angle type. You can specify an Angle as "270f" or "Angle.Parse("270") and get the same
	 *		result each time-- I think that's really important.
	 *		Ultimately the justification is more like "why did you choose degrees for Parse/ToString?" in this case...
	 *		See below.
	 *
	 * Another note: I probably won't include the opposite implicit conversion (e.g. Angle->float) because I think it's
	 * probably just error prone AF and I don't think specifying .Degrees is very onerous anyway. I actually don't think there's
	 * actually much use-case for it too: When you wanna convert to string, use ToString(), and when dealing with third-party
	 * APIs (e.g. math libs) you'll actually more likely want .Radians. And within this API/lib I will be using Angle
	 * everywhere so there shouldn't be much need to get a float value back out at all. The implicit conversion from
	 * float->Angle is just something to help quickly specify Angle "literals"-- not a declaration that there is a pure
	 * natural link between float and Angle. Angle to float makes a lot less sense for these reasons IMO.
	 *
	 * Finally, the reason for using degrees in the Parse/ToString methods instead of radians is basically the same
	 * as what I touched upon above: I think radians are ugly and unintuitive to look at on their own, e.g. "3.66519" vs "210°".
	 * Yes, you can make an educated guess usually by working out how far you are from/between 3.1415 or 6.283, but still, when
	 * printing an Angle to the console or screen or whatever it's SO much nicer to see the value in degrees! We could also print both
	 * I suppose, and in the future I might add format specifiers to let people override the ToString/Parse but the default will
	 * remain as degrees.
	 *
	 * If this ends up being problematic I'll probably just delete it and force people to be explicit using the factory methods.
	 *
	 * TLDR: Chose degrees for ToString/Parse because they're nicer to work with/print out than radians, and wanted the implicit
	 * conversion to match the Parse (e.g. "270f" == "Angle.Parse("270")").
	 */
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Angle(float operand) => FromDegrees(operand);
	#endregion

	#region Random
	public static Angle Random() => Random(Zero, FullCircle);
	public static Angle Random(Angle minInclusive, Angle maxExclusive) {
		return FromRadians(RandomUtils.NextSingle(minInclusive.Radians, maxExclusive.Radians));
	}
	#endregion

	#region Span Conversion
	public static int SerializationByteSpanLength { get; } = sizeof(float);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SerializeToBytes(Span<byte> dest, Angle src) => BinaryPrimitives.WriteSingleLittleEndian(dest, src.Radians);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle DeserializeFromBytes(ReadOnlySpan<byte> src) => FromRadians(BinaryPrimitives.ReadSingleLittleEndian(src));
	#endregion

	#region String Conversion
	public override string ToString() => ToString(null, null);

	public string ToString(string? format, IFormatProvider? formatProvider) => $"{Degrees.ToString(format, formatProvider)}{ToStringSuffix}";

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

		writeSuccess = destination.TryWrite($"{ToStringSuffix}", out tryWriteCharsWrittenOutVar);
		charsWritten += tryWriteCharsWrittenOutVar;
		return writeSuccess;
	}

	public static Angle Parse(string s, IFormatProvider? provider = null) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out Angle result) => TryParse(s.AsSpan(), provider, out result);

	public static Angle Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) {
		var indexOfSuffix = s.IndexOf(ToStringSuffix);

		var degrees = indexOfSuffix >= 0
			? Single.Parse(s[..indexOfSuffix], provider)
			: Single.Parse(s, provider);

		return FromDegrees(degrees);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Angle result) {
		var indexOfSuffix = s.IndexOf(ToStringSuffix);
		if (indexOfSuffix < 0) indexOfSuffix = s.Length;

		if (!Single.TryParse(s[..indexOfSuffix], provider, out var degrees)) {
			result = default;
			return false;
		}

		result = FromDegrees(degrees);
		return true;
	}
	#endregion

	#region Equality
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Angle other, Angle tolerance) => Equals(other, tolerance.Degrees);
	public bool Equals(Angle other, float toleranceDegrees) {
		// Using Degrees rather than Radians because the implicit conversion from float to Angle
		// assumes degrees and therefore I feel like the tolerance value here should also be degrees
		return MathF.Abs(Degrees - other.Degrees) <= toleranceDegrees;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsEquivalentWithinCircleTo(Angle other) => IsEquivalentWithinCircleTo(other, Zero);
	public bool IsEquivalentWithinCircleTo(Angle other, Angle tolerance) => IsEquivalentWithinCircleTo(other, tolerance.Degrees);
	public bool IsEquivalentWithinCircleTo(Angle other, float toleranceDegrees) {
		var absDiff = MathF.Abs(Normalized.Degrees - other.Normalized.Degrees);
		if (absDiff <= toleranceDegrees) return true;

		// This is to accomodate for cases where the normalized value is close but opposite sides of the 0/360 degree boundary;
		// e.g. this normalized is 0.1 deg and other normalized is 359.9 deg
		absDiff = MathF.Abs((this + HalfCircle).Normalized.Degrees - (other + HalfCircle).Normalized.Degrees);
		return absDiff <= toleranceDegrees;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Angle other) => Radians.Equals(other.Radians);
	public override bool Equals(object? obj) => obj is Angle other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => Normalized.Radians.GetHashCode();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Angle left, Angle right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Angle left, Angle right) => !left.Equals(right);
	#endregion
}