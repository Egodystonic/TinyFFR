// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Buffers.Binary;
using System.Drawing;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents an angle in degrees.
/// </summary>
/// <remarks>
/// An Angle can be constructed via implicit conversion from float (e.g. <c>Angle a = 120f</c> creates an Angle 'a' representing 120°).
/// You can also use the static factory methods (e.g. <see cref="FromDegrees"/>, <see cref="FromRadians"/>, <see cref="FromFullCircleFraction"/>, etc).
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = sizeof(float), Pack = 1)]
public readonly partial struct Angle : IMathPrimitive<Angle> {
	/// <summary>
	/// The suffix appended to the string representation of an angle by <see cref="ToString()"/> and its overloads (i.e. <c>"°"</c>).
	/// </summary>
	public const string ToStringSuffix = "°";
	const float Tau = MathF.Tau;
	const float TauReciprocal = 1f / MathF.Tau;
	const float RadiansToDegreesRatio = 360f / Tau;
	const float DegreesToRadiansRatio = Tau / 360f;
	/// <summary>
	/// An angle of exactly <c>0°</c>.
	/// </summary>
	public static readonly Angle Zero = FromRadians(0f);
	/// <summary>
	/// Represents an angle of 45°.
	/// </summary>
	public static readonly Angle EighthCircle = FromRadians(Tau * 0.125f);
	/// <summary>
	/// Represents an angle of 60°.
	/// </summary>
	public static readonly Angle SixthCircle = FromRadians(Tau / 6f);
	/// <summary>
	/// Represents an angle of 120°.
	/// </summary>
	public static readonly Angle ThirdCircle = FromRadians(Tau / 3f);
	/// <summary>
	/// Represents an angle of 90°.
	/// </summary>
	public static readonly Angle QuarterCircle = FromRadians(Tau * 0.25f);
	/// <summary>
	/// Represents an angle of 180°.
	/// </summary>
	public static readonly Angle HalfCircle = FromRadians(Tau * 0.5f);
	/// <summary>
	/// Represents an angle of 270°.
	/// </summary>
	public static readonly Angle ThreeQuarterCircle = FromRadians(Tau * 0.75f);
	/// <summary>
	/// Represents an angle of 360°.
	/// </summary>
	public static readonly Angle FullCircle = FromRadians(Tau * 1f);

	readonly float _radians;

	/// <summary>
	/// Returns the value of this angle in radians.
	/// </summary>
	public float Radians {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _radians;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private init => _radians = value;
	}
	/// <summary>
	/// Returns the value of this angle in degrees.
	/// </summary>
	public float Degrees {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Radians * RadiansToDegreesRatio;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private init => Radians = value * DegreesToRadiansRatio;
	}
	/// <summary>
	/// Returns the value of this angle in full-circle-fraction representation (i.e. 180° = 0.5, 360° = 1.0, etc).
	/// </summary>
	public float FullCircleFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Radians * TauReciprocal;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private init => Radians = Tau * value;
	}

	// Chose degrees rather than radians to keep consistency with implicit conversion. See notes above implicit operator for more reasoning.
	/// <summary>
	/// Constructs a new Angle object. See also: <see cref="FromRadians"/>, <see cref="FromDegrees"/>, <see cref="FromFullCircleFraction"/>, etc.
	/// </summary>
	/// <param name="degrees">The value of this angle in degrees (°).</param>
	public Angle(float degrees) => Degrees = degrees;

	#region Factories and Conversions
	/// <summary>
	/// Returns the angle represented by the given value in radians.
	/// </summary>
	/// <param name="radians">The value in radians. Can be positive, zero, or negative. Infinite and NaN values are permitted but discouraged unless explicitly/deliberately expected downstream.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle FromRadians(float radians) => new() { Radians = radians };
	/// <summary>
	/// Returns the angle represented by the given value in degrees.
	/// This is functionally identical to using the <c>Angle</c> constructor.
	/// </summary>
	/// <param name="degrees">The value in degrees. Can be positive, zero, or negative. Infinite and NaN values are permitted but discouraged unless explicitly/deliberately expected downstream.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle FromDegrees(float degrees) => new() { Degrees = degrees };
	/// <summary>
	/// Returns the angle represented by the given full circle fraction.
	/// </summary>
	/// <param name="fullCircleFraction">The distance around a circle this angle represents (e.g. 0.5 = 180°, 1.0 = 360°, etc). Can be positive, zero, or negative. Infinite and NaN values are permitted but discouraged unless explicitly/deliberately expected downstream.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle FromFullCircleFraction(float fullCircleFraction) => new() { FullCircleFraction = fullCircleFraction };
	
	/// <summary>
	/// Returns the angle that is the arcsine of <paramref name="sine"/> (e.g. the singular <c>angle</c> in the range -90° to 90° that maps <c>sin(angle)</c> to <paramref name="sine"/>).  
	/// </summary>
	/// <param name="sine">The sine value. Will be clamped to the range <c>[-1, 1]</c>.</param>
	public static Angle FromSine(float sine) {
		sine = Single.Clamp(sine, -1f, 1f);
		return FromRadians(MathF.Asin(sine));
	}

	/// <summary>
	/// Returns the angle that is the arccosine of <paramref name="cosine"/> (e.g. the singular <c>angle</c> in the range 0° to 180° that maps <c>cos(angle)</c> to <paramref name="cosine"/>).
	/// </summary>
	/// <param name="cosine">The cosine value. Will be clamped to the range <c>[-1, 1]</c>.</param>
	public static Angle FromCosine(float cosine) {
		cosine = Single.Clamp(cosine, -1f, 1f);
		return FromRadians(MathF.Acos(cosine));
	}

	/// <summary>
	/// Calculates the angle between <paramref name="d1"/> and <paramref name="d2"/>.
	/// </summary>
	/// <remarks>
	/// Note that this function does some small corrections to help fight floating-point inaccuracy:
	/// <ul>
	/// <li>Results that are very near 0° will be clamped to 0°.</li>
	/// <li>Results that are very near 90° will be clamped to 90°.</li>
	/// <li>Results that are very near 180° will be clamped to 180°.</li>
	/// </ul>
	/// This clamping is done to help pre-emptively fix typical comparisons downstream (e.g. "<c>if (angleBetweenDirections == Angle.Zero)</c>" and similar). 
	/// <para>
	/// Furthermore, the linear algebra function used to calculate the value is clamped before trigonometric inversion
	/// (i.e. <c>dot</c> is clamped before <c>acos</c>) to make sure floating-point inaccuracy does not accidentaly create
	/// "NaN" values. 
	/// </para>
	/// </remarks>
	/// <param name="d1">Any <see cref="Direction"/>. If this is <see cref="Direction.None"/> the returned value will always be 0°, regardless of what <paramref name="d2"/> is.</param>
	/// <param name="d2">Any <see cref="Direction"/>. If this is <see cref="Direction.None"/> the returned value will always be 0°, regardless of what <paramref name="d1"/> is.</param>
	/// <returns>A value between 0° and 180°.</returns>
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

	/// <summary>
	/// Calculates the angle around a circle represented by <paramref name="xy"/> as a 2D vector.
	/// </summary>
	/// <remarks>
	/// This function takes the four-quadrant inverse tangent of <paramref name="xy"/>.X and <paramref name="xy"/>.Y.
	/// This means that the direction <paramref name="xy"/> points is transformed to the resultant angle according to the following rules:
	/// <ul>
	/// <li>If <paramref name="xy"/> points exactly right (i.e. positive X and zero Y) this function returns 0°.</li>
	/// <li>If <paramref name="xy"/> points exactly up (i.e. zero X and positive Y) this function returns 90°.</li>
	/// <li>If <paramref name="xy"/> points exactly left (i.e. negative X and zero Y) this function returns 180°.</li>
	/// <li>If <paramref name="xy"/> points exactly down (i.e. zero X and negative Y) this function returns 270°.</li>
	/// <li>In general, the value returned by this function "starts" at 0° for a right-facing <paramref name="xy"/> and increases as <paramref name="xy"/> rotates anticlockwise.</li>
	/// </ul>
	/// </remarks>
	/// <param name="xy">The <see cref="XYPair{T}"/> representing a 2D vector. If this is <see cref="XYPair{T}.Zero"/>, the function will return <c>null</c>.</param>
	/// <returns><c>atan2(x, y)</c>, or <c>null</c> if <paramref name="xy"/> is <see cref="XYPair{T}.Zero"/>.</returns>
	public static Angle? From2DPolarAngle<T>(XYPair<T> xy) where T : unmanaged, INumber<T> => From2DPolarAngle(Single.CreateTruncating(xy.X), Single.CreateTruncating(xy.Y));
	/// <summary>
	/// Calculates the angle around a circle represented by the 2D vector (<paramref name="x"/>, <paramref name="y"/>).
	/// </summary>
	/// <remarks>
	/// This function takes the four-quadrant inverse tangent of <paramref name="x"/> and <paramref name="y"/>.
	/// This means that the direction the vector points is transformed to the resultant angle according to the following rules:
	/// <ul>
	/// <li>If the vector points exactly right (i.e. positive <paramref name="x"/> and zero <paramref name="y"/>) this function returns 0°.</li>
	/// <li>If the vector points exactly up (i.e. zero <paramref name="x"/> and positive <paramref name="y"/>) this function returns 90°.</li>
	/// <li>If the vector points exactly left (i.e. negative <paramref name="x"/> and zero <paramref name="y"/>) this function returns 180°.</li>
	/// <li>If the vector points exactly down (i.e. zero <paramref name="x"/> and negative <paramref name="y"/>) this function returns 270°.</li>
	/// <li>In general, the value returned by this function "starts" at 0° for a right-facing vector and increases as the vector rotates anticlockwise.</li>
	/// </ul>
	/// </remarks>
	/// <param name="x">The X component of the 2D vector.</param>
	/// <param name="y">The Y component of the 2D vector.</param>
	/// <returns><c>atan2(x, y)</c>, or <c>null</c> if both <paramref name="x"/> and <paramref name="y"/> are <c>0f</c>.</returns>
	public static Angle? From2DPolarAngle(float x, float y) {
		if (x == 0f && y == 0f) return null;
		return FromRadians(MathF.Atan2(y, x)).Normalized;
	}
	/// <summary>
	/// Returns the angle around a circle represented by the given 2D <paramref name="orientation"/>.
	/// </summary>
	/// <remarks>
	/// Each compass-style <see cref="Orientation2D"/> value maps to a fixed 45°-multiple angle (e.g. <see cref="Orientation2D.Right"/> is 0°, <see cref="Orientation2D.Up"/> is 90°, and so on),
	/// following the same "starts at 0° facing right, increases anticlockwise" convention as <see cref="From2DPolarAngle(float,float)"/>.
	/// </remarks>
	/// <param name="orientation">The orientation to convert.</param>
	/// <returns>The angle represented by <paramref name="orientation"/>, or <c>null</c> if <paramref name="orientation"/> is <see cref="Orientation2D.None"/>.</returns>
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
	/// <summary>
	/// Returns the angle represented by the given value in degrees.
	/// This is functionally identical to using the <c>Angle</c> constructor.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Angle(float operand) => FromDegrees(operand);
	#endregion

	#region Random
	/// <summary>
	/// Creates a new random angle <c>v</c> such that <c>0° &lt;= v &lt; 360°</c>.
	/// </summary>
	public static Angle Random() => Random(Zero, FullCircle);
	/// <summary>
	/// Creates a new random angle <c>v</c> such that <c>minInclusive &lt;= v &lt; maxExclusive</c>.
	/// </summary>
	/// <param name="minInclusive">The minimum value that can be produced.</param>
	/// <param name="maxExclusive">The ceiling of values that can be produced. No values higher/greater than this value will be produced, and nor will this value itself.</param>
	public static Angle Random(Angle minInclusive, Angle maxExclusive) {
		return FromRadians(RandomUtils.NextSingle(minInclusive.Radians, maxExclusive.Radians));
	}
	#endregion

	#region Span Conversion
	/// <inheritdoc />
	public static int SerializationByteSpanLength { get; } = sizeof(float);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SerializeToBytes(Span<byte> dest, Angle src) => BinaryPrimitives.WriteSingleLittleEndian(dest, src.Radians);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle DeserializeFromBytes(ReadOnlySpan<byte> src) => FromRadians(BinaryPrimitives.ReadSingleLittleEndian(src));
	#endregion

	#region String Conversion
	/// <inheritdoc />
	public override string ToString() => ToString(null, null);

	/// <inheritdoc />
	public string ToString(string? format, IFormatProvider? formatProvider) => $"{Degrees.ToString(format, formatProvider)}{ToStringSuffix}";

	/// <inheritdoc />
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

	/// <inheritdoc />
	public static Angle Parse(string s, IFormatProvider? provider = null) => Parse(s.AsSpan(), provider);
	/// <inheritdoc />
	public static bool TryParse(string? s, IFormatProvider? provider, out Angle result) => TryParse(s.AsSpan(), provider, out result);

	/// <inheritdoc />
	public static Angle Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) {
		var indexOfSuffix = s.IndexOf(ToStringSuffix);

		var degrees = indexOfSuffix >= 0
			? Single.Parse(s[..indexOfSuffix], provider)
			: Single.Parse(s, provider);

		return FromDegrees(degrees);
	}

	/// <inheritdoc />
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
	/// <summary>
	/// Determines whether this value is equal to <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="other">The other value.</param>
	/// <param name="tolerance">The tolerance value.</param>
	/// <returns>True if equal within tolerance, false if not.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Angle other, Angle tolerance) => Equals(other, tolerance.Degrees);
	/// <summary>
	/// Determines whether this value is equal to <paramref name="other"/> within a given tolerance in degrees.
	/// </summary>
	/// <param name="other">The other value.</param>
	/// <param name="toleranceDegrees">The tolerance value, in degrees.</param>
	/// <returns>True if equal within tolerance, false if not.</returns>
	public bool Equals(Angle other, float toleranceDegrees) {
		// Using Degrees rather than Radians because the implicit conversion from float to Angle
		// assumes degrees and therefore I feel like the tolerance value here should also be degrees
		return MathF.Abs(Degrees - other.Degrees) <= toleranceDegrees;
	}
	/// <summary>
	/// Determines whether this angle is equivalent to <paramref name="other"/>
	/// when used in the context of a rotating or orientating function around a circle. 
	/// </summary>
	/// <remarks>
	/// This function tells you whether <c>this</c> and <paramref name="other"/> "point" the same way. Examples:
	/// <ul>
	/// <li>0° and 360° returns <c>true</c>.</li>
	/// <li>180° and -180° returns <c>true</c>.</li>
	/// <li>90° and 450° returns <c>true</c>.</li>
	/// <li>90° and -90° returns <c>false</c>.</li>
	/// </ul>
	/// </remarks>
	/// <param name="other">The other angle to compare to. Can be positive, negative, or zero.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsEquivalentWithinCircleTo(Angle other) => IsEquivalentWithinCircleTo(other, Zero);
	/// <summary>
	/// Determines whether this angle is equivalent to <paramref name="other"/>, within a given <paramref name="tolerance"/>,
	/// when used in the context of a rotating or orientating function around a circle.
	/// </summary>
	/// <remarks>
	/// See <see cref="IsEquivalentWithinCircleTo(Angle)"/> for a description of what "equivalent within a circle" means.
	/// </remarks>
	/// <param name="other">The other angle to compare to. Can be positive, negative, or zero.</param>
	/// <param name="tolerance">The tolerance to allow between the normalized values of this angle and <paramref name="other"/>.</param>
	public bool IsEquivalentWithinCircleTo(Angle other, Angle tolerance) => IsEquivalentWithinCircleTo(other, tolerance.Degrees);
	/// <summary>
	/// Determines whether this angle is equivalent to <paramref name="other"/>, within a given tolerance in degrees,
	/// when used in the context of a rotating or orientating function around a circle.
	/// </summary>
	/// <remarks>
	/// See <see cref="IsEquivalentWithinCircleTo(Angle)"/> for a description of what "equivalent within a circle" means.
	/// </remarks>
	/// <param name="other">The other angle to compare to. Can be positive, negative, or zero.</param>
	/// <param name="toleranceDegrees">The tolerance, in degrees, to allow between the normalized values of this angle and <paramref name="other"/>.</param>
	public bool IsEquivalentWithinCircleTo(Angle other, float toleranceDegrees) {
		var absDiff = MathF.Abs(Normalized.Degrees - other.Normalized.Degrees);
		if (absDiff <= toleranceDegrees) return true;

		// This is to accomodate for cases where the normalized value is close but opposite sides of the 0/360 degree boundary;
		// e.g. this normalized is 0.1 deg and other normalized is 359.9 deg
		absDiff = MathF.Abs((this + HalfCircle).Normalized.Degrees - (other + HalfCircle).Normalized.Degrees);
		return absDiff <= toleranceDegrees;
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Angle other) => Radians.Equals(other.Radians);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Angle other && Equals(other);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => Normalized.Radians.GetHashCode();

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Angle left, Angle right) => left.Equals(right);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Angle left, Angle right) => !left.Equals(right);
	#endregion
}