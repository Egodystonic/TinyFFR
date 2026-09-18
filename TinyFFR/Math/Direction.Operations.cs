// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

/// <summary>
/// The result of finding the compass-style orientation nearest to a given <see cref="Direction"/> (e.g. via <see cref="Direction.NearestOrientation"/>).
/// </summary>
/// <param name="AsEnum">The nearest orientation, as an enum value.</param>
/// <param name="AsDirection">The nearest orientation, as its equivalent <see cref="Direction"/>.</param>
/// <typeparam name="TOrientation">The enum type representing the orientation (e.g. <see cref="CardinalOrientation"/>, <see cref="Orientation"/>).</typeparam>
public readonly record struct NearestOrientationResult<TOrientation>(TOrientation AsEnum, Direction AsDirection) where TOrientation : Enum;

partial struct Direction :
	IPhysicalValidityDeterminable,
	IInvertible<Direction>,
	IMultiplyOperators<Direction, float, Vect>,
	IModulusOperators<Direction, Angle, Rotation>,
	IPrecomputationInterpolatable<Direction, Rotation>,
	IInnerProductSpace<Direction>,
	IVectorProductSpace<Direction>,
	IAngleMeasurable<Direction, Direction>,
	ITransitionRepresentable<Direction, Rotation>,
	IRotatable<Direction>,
	IOrthogonalizable<Direction, Direction>,
	IParallelizable<Direction, Direction>,
	IProjectionTarget<Direction, Vect>,
	IOrthogonalizationTarget<Direction, Vect>,
	IParallelizationTarget<Direction, Vect>,
	IOrthogonalizationTarget<Direction, Direction>,
	IParallelizationTarget<Direction, Direction> {
	/// <summary>
	/// The default tolerance, in degrees, used by the parameterless overloads of <see cref="IsApproximatelyOrthogonalTo(Direction)"/> and <see cref="IsApproximatelyParallelTo(Direction)"/> (and their <see cref="Vect"/>-accepting equivalents).
	/// </summary>
	public const float DefaultParallelOrthogonalTestApproximationDegrees = 0.1f;
	const float ParallelComponentsCheckErrorMargin = 1E-5f;
	const float OrthogonalDotErrorMargin = 1E-5f;

	internal bool IsApproxUnitLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get {
			const float FloatingPointErrorMargin = 3E-1f * 3E-1f;
			return MathF.Abs(AsVector4.LengthSquared() - 1f) < FloatingPointErrorMargin;
		}
	}

	/// <summary>
	/// Negates <paramref name="operand"/>; equivalent to reading <see cref="Flipped"/>.
	/// </summary>
	/// <param name="operand">The direction to negate.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction operator -(Direction operand) => operand.Flipped;
	/// <summary>
	/// Returns the direction pointing exactly opposite to this one (e.g. <see cref="Up"/> becomes <see cref="Down"/>).
	/// </summary>
	/// <remarks>
	/// If this is <see cref="None"/>, the result is also <see cref="None"/>.
	/// </remarks>
	public Direction Flipped {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(-AsVector4);
	}
	Direction IInvertible<Direction>.Inverted => Flipped;

	/// <summary>
	/// Finds the nearest of the six <see cref="AllCardinals"/> directions to this one.
	/// </summary>
	public NearestOrientationResult<CardinalOrientation> NearestOrientationCardinal {
		get {
			GetNearestDirectionAndOrientation(this, AllCardinals, out var e, out var d);
			return new((CardinalOrientation) e, d);
		}
	}
	/// <summary>
	/// Finds the nearest of the twelve <see cref="AllIntercardinals"/> directions to this one.
	/// </summary>
	public NearestOrientationResult<IntercardinalOrientation> NearestOrientationIntercardinal {
		get {
			GetNearestDirectionAndOrientation(this, AllIntercardinals, out var e, out var d);
			return new((IntercardinalOrientation) e, d);
		}
	}
	/// <summary>
	/// Finds the nearest of the eight <see cref="AllDiagonals"/> directions to this one.
	/// </summary>
	public NearestOrientationResult<DiagonalOrientation> NearestOrientationDiagonal {
		get {
			GetNearestDirectionAndOrientation(this, AllDiagonals, out var e, out var d);
			return new((DiagonalOrientation) e, d);
		}
	}
	/// <summary>
	/// Finds the nearest of the twenty-six <see cref="AllOrientations"/> directions to this one.
	/// </summary>
	public NearestOrientationResult<Orientation> NearestOrientation {
		get {
			GetNearestDirectionAndOrientation(this, AllOrientations, out var e, out var d);
			return new(e, d);
		}
	}

	/// <summary>
	/// Converts this direction to a unit-length <see cref="Vect"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect AsVect() => (Vect) this;
	/// <summary>
	/// Converts this direction to a <see cref="Vect"/> with the given <paramref name="length"/>.
	/// </summary>
	/// <param name="length">The desired length of the resultant vector. Can be negative, in which case the resultant vector points opposite to this direction.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect AsVect(float length) => new(AsVector4 * length);

	/// <summary>
	/// Finds the index within <paramref name="span"/> of whichever element is closest (by angle) to <paramref name="targetDir"/>.
	/// </summary>
	/// <param name="targetDir">The direction to find the nearest match for.</param>
	/// <param name="span">The candidate directions to search. Must not be empty.</param>
	/// <returns>The index within <paramref name="span"/> of the nearest direction.</returns>
	public static int GetIndexOfNearestDirectionInSpan(Direction targetDir, ReadOnlySpan<Direction> span) {
		var result = -1;
		var resultAngle = Angle.FullCircle;
		for (var i = 0; i < span.Length; ++i) {
			var newAngle = span[i] ^ targetDir;
			if (newAngle >= resultAngle) continue;

			resultAngle = newAngle;
			result = i;
		}
		return result;
	}
	static void GetNearestDirectionAndOrientation(Direction targetDir, ReadOnlySpan<Direction> span, out Orientation orientation, out Direction direction) {
		orientation = Orientation.None;
		direction = None;
		if (targetDir == None) {
			return;
		}

		var dirAngle = Angle.FullCircle;
		for (var i = 0; i < span.Length; ++i) {
			var testDir = span[i];
			if (targetDir.X != 0f && Single.Sign(testDir.X) == -Single.Sign(targetDir.X)) continue;
			if (targetDir.Y != 0f && Single.Sign(testDir.Y) == -Single.Sign(targetDir.Y)) continue;
			if (targetDir.Z != 0f && Single.Sign(testDir.Z) == -Single.Sign(targetDir.Z)) continue;

			var newAngle = testDir ^ targetDir;
			if (newAngle >= dirAngle) continue;

			dirAngle = newAngle;
			direction = testDir;
		}

		orientation = OrientationUtils.CreateOrientationFromValueSigns(direction.X, direction.Y, direction.Z);
	}
	
	/// <summary>
	/// Determines whether this direction has a valid, finite, unit-length value.
	/// </summary>
	/// <remarks>
	/// <see cref="None"/> is considered physically valid (it's a deliberate sentinel value), as is any direction whose
	/// components are all finite and whose length is (approximately) <c>1f</c>. If you want to exclude <see cref="None"/>, use <see cref="IsPhysicallyValidAndNotNone"/>.
	/// </remarks>
	public bool IsPhysicallyValid => this == None || IsApproxUnitLength;
	/// <summary>
	/// Determines whether this direction has a valid, finite, unit-length value, and is not <see cref="None"/>.
	/// </summary>
	public bool IsPhysicallyValidAndNotNone => IsApproxUnitLength;

	#region Scaling and Addition/Subtraction
	/// <summary>
	/// Multiplies <paramref name="directionOperand"/> by <paramref name="scalarOperand"/>; equivalent to <c>directionOperand.AsVect(scalarOperand)</c>.
	/// </summary>
	/// <param name="directionOperand">The direction to scale.</param>
	/// <param name="scalarOperand">The desired length of the resultant vector. Can be negative, in which case the resultant vector points opposite to <paramref name="directionOperand"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(Direction directionOperand, float scalarOperand) => directionOperand.AsVect(scalarOperand);
	/// <summary>
	/// Multiplies <paramref name="directionOperand"/> by <paramref name="scalarOperand"/>; equivalent to <c>directionOperand.AsVect(scalarOperand)</c>.
	/// </summary>
	/// <param name="scalarOperand">The desired length of the resultant vector. Can be negative, in which case the resultant vector points opposite to <paramref name="directionOperand"/>.</param>
	/// <param name="directionOperand">The direction to scale.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator *(float scalarOperand, Direction directionOperand) => directionOperand.AsVect(scalarOperand);
	#endregion

	#region Interactions w/ Direction
	/// <summary>
	/// Calculates the dot product of this direction and <paramref name="other"/>.
	/// </summary>
	/// <remarks>
	/// Because both operands are unit-length, this is equivalent to the cosine of the angle between them: it is <c>1f</c>
	/// when the two directions are identical, <c>-1f</c> when they point exactly opposite, and <c>0f</c> when they are
	/// orthogonal (perpendicular). The result is always clamped to <c>[-1, 1]</c> to guard against floating-point error.
	/// </remarks>
	/// <param name="other">The other direction to calculate the dot product with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float Dot(Direction other) => Single.Clamp(Vector4.Dot(AsVector4, other.AsVector4), -1f, 1f);
	/// <summary>
	/// Calculates the cross product of this direction and <paramref name="other"/>.
	/// </summary>
	/// <remarks>
	/// The result is normalized back to unit length, so it is itself a valid <see cref="Direction"/> perpendicular to
	/// both operands. If <paramref name="other"/> is parallel or opposite to this direction (or either is <see cref="None"/>),
	/// the result is <see cref="None"/>.
	/// </remarks>
	/// <param name="other">The other direction to calculate the cross product with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction Cross(Direction other) => FromVector3(Vector3.Cross(ToVector3(), other.ToVector3()));

	/// <summary>
	/// Calculates the angle formed between <paramref name="d1"/> and <paramref name="d2"/>; equivalent to <c>d1.AngleTo(d2)</c>.
	/// </summary>
	/// <param name="d1">The first direction.</param>
	/// <param name="d2">The second direction.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Direction d1, Direction d2) => Angle.FromAngleBetweenDirections(d1, d2);
	/// <summary>
	/// Determines the angle formed between this direction and <paramref name="other"/>.
	/// </summary>
	/// <remarks>
	/// This is equivalent to calling <see cref="Angle.FromAngleBetweenDirections"/>. 
	/// </remarks>
	/// <param name="other">The other direction. Can be <see cref="None"/> (in which case 0° will be returned).</param>
	/// <returns>The angle between the two directions. If either direction is <see cref="None"/>, returns 0°.</returns>
	/// <seealso cref="SignedAngleTo(Direction, Direction)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Direction other) => Angle.FromAngleBetweenDirections(this, other);
	// Maintainer's notes: We have the following constraints for this to help it feel consistent:
	// 1) This method should never give a different answer to AngleTo excepting the sign.
	// 2) When swapping this & other or reversing the direction of clockwiseAxis, the sign should always change, except in case (3) or (4):
	// 3) When the non-signed angle between this & other is 180deg, this will always return positive 180deg; in other words the range of values for this function is [-179.99.., 180.00]
	// 4) When clockwiseAxis is None, this method always returns the same answer to non-signed AngleTo()
	// These cases are commented inline below.
	/// <summary>
	/// Determines the angle formed between this and <paramref name="other"/>, additionally attributing a sign (+ or -)
	/// to the result making it possible to differentiate the winding/chirality of the two directions.
	/// </summary>
	/// <remarks>
	/// The output of this function has the following guarantees:
	/// <ul>
	/// <li>The answer returned will never differ from that given by <see cref="AngleTo(Direction)"/> excepting the sign.</li>
	/// <li>When swapping the arguments (<c>this</c> and <paramref name="other"/>) or reversing <paramref name="clockwiseAxis"/> the sign will always flip (except according to the two caveats below).</li>
	/// <li>Caveat: When the non-signed angle between <c>this</c> and <paramref name="other"/> is 180° the returned answer will always be +180° (i.e. the range of values for this function is -179.99.. to 180.00).</li>
	/// <li>Caveat: When <paramref name="clockwiseAxis"/> is <see cref="None"/> this method always returns the same answer as would be given by <see cref="AngleTo(Direction)"/>.</li>
	/// </ul>
	/// </remarks>
	/// <param name="other">The other direction. Can be <see cref="None"/> (in which case 0° will be returned).</param>
	/// <param name="clockwiseAxis">The axis used to determine the sign. When looking along this axis, an apparent clockwise
	/// winding from <c>this</c> to <paramref name="other"/> will be reported with a positive value. For example: <c>Direction.Forward.SignedAngleTo(Direction.Right, Direction.Down)</c> returns +90°.</param>
	/// <returns>The signed angle between the two directions. If either direction is <see cref="None"/>, returns 0°.</returns>
	public Angle SignedAngleTo(Direction other, Direction clockwiseAxis) {
		const float FloatingPointErrorMargin = 1E-6f;

		static int CalcArbitrarySign(Direction a, Direction b, Direction c) {
			if (c == None) return 1; // (4)
			
			var aCrossB = a.Cross(b);

			var s = aCrossB.Dot(Up);
			if (MathF.Abs(s) < FloatingPointErrorMargin) s = aCrossB.Dot(Forward);
			if (MathF.Abs(s) < FloatingPointErrorMargin) s = aCrossB.Dot(Right);
			
			var q = c.Dot(Up);
			if (MathF.Abs(q) < FloatingPointErrorMargin) q = c.Dot(Forward);
			if (MathF.Abs(q) < FloatingPointErrorMargin) q = c.Dot(Right);
			
			return MathF.Sign(s) * MathF.Sign(q);
		}

		var unsignedAngle = AngleTo(other);
		if (unsignedAngle == Angle.HalfCircle) return unsignedAngle; // (3)
		var dot = clockwiseAxis.Dot(Cross(other)); // (2)
		var axisSign = dot switch {
			> FloatingPointErrorMargin => 1,
			< -FloatingPointErrorMargin => -1,
			_ => CalcArbitrarySign(this, other, clockwiseAxis) // (2) -- outcome of this function is sensitive to orientations of a, b, and c
		}; 
		return unsignedAngle * axisSign; // (1)
	}

	/// <summary>
	/// Returns an arbitrary but consistent direction orthogonal (perpendicular) to this one.
	/// </summary>
	/// <remarks>
	/// There are infinitely many directions orthogonal to any given direction; this method deterministically picks one
	/// of them (the same one every time for a given input), which is useful when you need "some direction at right
	/// angles to this one" but don't care which. If this is <see cref="None"/>, the result is also <see cref="None"/>.
	/// </remarks>
	public Direction AnyOrthogonal() {
		return FromVector3(Vector3.Cross(
			ToVector3(),
			MathF.Abs(Z) > MathF.Abs(X) ? new Vector3(1f, 0f, 0f) : new Vector3(0f, 0f, 1f)
		));
	}

	/// <summary>
	/// Attempts to orthogonalize this direction against <paramref name="d"/>.
	/// Orthogonalization refers to adjusting this direction such that it forms an angle exactly 90° with the target (<paramref name="d"/>).
	/// </summary>
	/// <param name="d">The target direction. Can be <see cref="None"/> (in which case this function returns <c>this</c>).</param>
	/// <returns>This direction adjusted such that it forms a 90° angle with <paramref name="d"/>; or <c>null</c> if there is no single
	/// answer (i.e. the two values point in exactly the same direction or exactly opposite).
	/// If <c>this</c> or <paramref name="d"/> are <see cref="None"/>, returns <c>this</c>.</returns>
	public Direction? OrthogonalizedAgainst(Direction d) {
		if (this == None || d == None) return this;
		if (IsParallelTo(d)) return null;
		return new(Normalize(AsVector4 - d.AsVector4 * Dot(d)));
	}
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAgainst(Direction)"/> but skips some correctness checks, trading safety
	/// for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes the following conditions:
	/// <ul>
	/// <li>Neither <c>this</c> or <paramref name="d"/> are <see cref="None"/>.</li>
	/// <li><c>this</c> and <paramref name="d"/> are not parallel.</li>
	/// </ul>
	/// The returned value of this function is undefined when any condition above is broken.
	/// </remarks>
	/// <param name="d">The target direction.</param>
	/// <returns>This direction adjusted to form a 90° angle with <paramref name="d"/>.</returns>
	public Direction FastOrthogonalizedAgainst(Direction d) => new(Normalize(AsVector4 - d.AsVector4 * Vector4.Dot(AsVector4, d.AsVector4)));

	/// <summary>
	/// Attempts to parallelize this direction with <paramref name="d"/>.
	/// Parallelization refers to adjusting this direction such that it forms an angle of exactly 0° or 180° with the target (<paramref name="d"/>).
	/// </summary>
	/// <param name="d">The target direction. Can be <see cref="None"/> (in which case this function returns <c>this</c>).</param>
	/// <returns>Either <paramref name="d"/> or <c>-</c><paramref name="d"/> (whichever is closer to <c>this</c>); or <c>null</c> if there is
	/// no single answer (i.e. <c>this</c> and <paramref name="d"/> are already exactly orthogonal).
	/// If <c>this</c> or <paramref name="d"/> are <see cref="None"/>, returns <c>this</c>.</returns>
	public Direction? ParallelizedWith(Direction d) {
		if (this == None || d == None) return this;
		var dot = Vector4.Dot(AsVector4, d.AsVector4);
		if (MathF.Abs(dot) < OrthogonalDotErrorMargin) return null;
		return new(d.AsVector4 * MathF.Sign(dot));
	}
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedWith(Direction)"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes the following conditions:
	/// <ul>
	/// <li>Neither <c>this</c> or <paramref name="d"/> are <see cref="None"/>.</li>
	/// <li><c>this</c> and <paramref name="d"/> are not orthogonal.</li>
	/// </ul>
	/// The returned value of this function is undefined when any condition above is broken.
	/// </remarks>
	/// <param name="d">The target direction.</param>
	/// <returns>Either <paramref name="d"/> or <c>-</c><paramref name="d"/> (whichever is closer to <c>this</c>).</returns>
	public Direction FastParallelizedWith(Direction d) => new(d.AsVector4 * MathF.Sign(Vector4.Dot(AsVector4, d.AsVector4)));

	/// <summary>
	/// Equivalent to <c>d.OrthogonalizedAgainst(this)</c>.
	/// </summary>
	/// <param name="d">The direction to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction? OrthogonalizationOf(Direction d) => d.OrthogonalizedAgainst(this);
	/// <summary>
	/// Equivalent to <c>d.FastOrthogonalizedAgainst(this)</c>.
	/// </summary>
	/// <param name="d">The direction to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction FastOrthogonalizationOf(Direction d) => d.FastOrthogonalizedAgainst(this);
	/// <summary>
	/// Equivalent to <c>d.ParallelizedWith(this)</c>.
	/// </summary>
	/// <param name="d">The direction to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction? ParallelizationOf(Direction d) => d.ParallelizedWith(this);
	/// <summary>
	/// Equivalent to <c>d.FastParallelizedWith(this)</c>.
	/// </summary>
	/// <param name="d">The direction to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction FastParallelizationOf(Direction d) => d.FastParallelizedWith(this);

	/// <summary>
	/// Determines whether this direction is exactly orthogonal (perpendicular) to <paramref name="other"/>.
	/// </summary>
	/// <remarks>
	/// This is an exact check and is therefore prone to floating-point inaccuracy rejecting directions that are
	/// "orthogonal enough" for practical purposes; in most cases prefer <see cref="IsApproximatelyOrthogonalTo(Direction)"/>.
	/// This method is faster, however, and is useful for determining in advance whether <see cref="ParallelizedWith(Direction)"/> will return <c>null</c>.
	/// </remarks>
	/// <param name="other">The other direction to compare to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Direction other) => MathF.Abs(Vector4.Dot(AsVector4, other.AsVector4)) < OrthogonalDotErrorMargin && this != None && other != None;
	/// <summary>
	/// Determines whether this direction is orthogonal (perpendicular) to <paramref name="other"/>, within <see cref="DefaultParallelOrthogonalTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="other">The other direction to compare to.</param>
	public bool IsApproximatelyOrthogonalTo(Direction other) => IsApproximatelyOrthogonalTo(other, DefaultParallelOrthogonalTestApproximationDegrees);
	/// <summary>
	/// Determines whether this direction is orthogonal (perpendicular) to <paramref name="other"/>, within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="other">The other direction to compare to. If this or <c>this</c> is <see cref="None"/>, returns <see langword="false"/>.</param>
	/// <param name="tolerance">How far away from exactly 90° the angle between the two directions is allowed to be.</param>
	public bool IsApproximatelyOrthogonalTo(Direction other, Angle tolerance) {
		if (this == None || other == None) return false;
		return AngleTo(other).Equals(Angle.QuarterCircle, tolerance);
	}

	/// <summary>
	/// Determines whether this direction is exactly parallel (or exactly opposite) to <paramref name="other"/>.
	/// </summary>
	/// <remarks>
	/// This is an exact check and is therefore prone to floating-point inaccuracy rejecting directions that are
	/// "parallel enough" for practical purposes; in most cases prefer <see cref="IsApproximatelyParallelTo(Direction)"/>.
	/// This method is faster, however, and is useful for determining in advance whether <see cref="OrthogonalizedAgainst(Direction)"/> will return <c>null</c>.
	/// </remarks>
	/// <param name="other">The other direction to compare to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Direction other) => (Equals(other, ParallelComponentsCheckErrorMargin) || Equals(-other, ParallelComponentsCheckErrorMargin)) && this != None;
	/// <summary>
	/// Determines whether this direction is parallel (or opposite) to <paramref name="other"/>, within <see cref="DefaultParallelOrthogonalTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="other">The other direction to compare to.</param>
	public bool IsApproximatelyParallelTo(Direction other) => IsApproximatelyParallelTo(other, DefaultParallelOrthogonalTestApproximationDegrees);
	/// <summary>
	/// Determines whether this direction is parallel (or opposite) to <paramref name="other"/>, within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="other">The other direction to compare to. If this or <c>this</c> is <see cref="None"/>, returns <see langword="false"/>.</param>
	/// <param name="tolerance">How far away from exactly 0° or exactly 180° the angle between the two directions is allowed to be.</param>
	public bool IsApproximatelyParallelTo(Direction other, Angle tolerance) {
		if (this == None || other == None) return false;
		var angle = AngleTo(other);
		return angle.Equals(Angle.Zero, tolerance) || angle.Equals(Angle.HalfCircle, tolerance);
	}

	/// <summary>
	/// Determines whether the angle between this direction and <paramref name="other"/> is no greater than <paramref name="angle"/>.
	/// </summary>
	/// <param name="other">The other direction to compare to.</param>
	/// <param name="angle">The maximum permitted angle between the two directions.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsWithinAngleTo(Direction other, Angle angle) => (this ^ other) <= angle;

	/// <summary>
	/// Adjusts <paramref name="secondary"/> and <paramref name="tertiary"/> in place so that all three directions are mutually orthogonal.
	/// </summary>
	/// <remarks>
	/// <paramref name="primary"/> is never altered. <paramref name="secondary"/> is orthogonalized against <paramref name="primary"/>
	/// only. <paramref name="tertiary"/> is then set to whichever direction is orthogonal to both <paramref name="primary"/>
	/// and the (now-adjusted) <paramref name="secondary"/>, choosing the sign that keeps it closest to its original value.
	/// This is useful for building an orthonormal basis (e.g. a camera's forward/up/right vectors) out of directions that
	/// may not start out perfectly orthogonal. Any of the three parameters that is <see cref="None"/> is left unaltered,
	/// and is treated as if it were not present when orthogonalizing the others.
	/// </remarks>
	/// <param name="primary">The reference direction. Never modified by this method.</param>
	/// <param name="secondary">The second direction; orthogonalized against <paramref name="primary"/> in place.</param>
	/// <param name="tertiary">The third direction; set in place to be orthogonal to both <paramref name="primary"/> and <paramref name="secondary"/>.</param>
	public static void OrthogonalizeAll(Direction primary, ref Direction secondary, ref Direction tertiary) {
		switch (primary == None, secondary == None, tertiary == None) {
			case (false, false, false):
				secondary = secondary.OrthogonalizedAgainst(primary) ?? primary.AnyOrthogonal();
				var dualOrthogonal = FromDualOrthogonalization(primary, secondary);
				tertiary = dualOrthogonal.Dot(tertiary) < 0f ? -dualOrthogonal : dualOrthogonal;
				break;
			case (true, false, false):
				tertiary = tertiary.OrthogonalizedAgainst(secondary) ?? secondary.AnyOrthogonal();
				break;
			case (false, true, false):
				tertiary = tertiary.OrthogonalizedAgainst(primary) ?? primary.AnyOrthogonal();
				break;
			case (false, false, true):
				secondary = secondary.OrthogonalizedAgainst(primary) ?? primary.AnyOrthogonal();
				break;
			// default: Do nothing (only one or zero inputs is non-None, so nothing to orthogonalize)
		}
	}
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizeAll"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes none of <paramref name="primary"/>, <paramref name="secondary"/>, or <paramref name="tertiary"/>
	/// are <see cref="None"/>, and that no two of them are parallel or opposite. The returned value of this function is
	/// undefined when any condition above is broken.
	/// </remarks>
	/// <param name="primary">The reference direction. Never modified by this method.</param>
	/// <param name="secondary">The second direction; orthogonalized against <paramref name="primary"/> in place.</param>
	/// <param name="tertiary">The third direction; set in place to be orthogonal to both <paramref name="primary"/> and <paramref name="secondary"/>.</param>
	public static void FastOrthogonalizeAll(Direction primary, ref Direction secondary, ref Direction tertiary) {
		secondary = secondary.FastOrthogonalizedAgainst(primary);
		var dualOrthogonal = FastFromDualOrthogonalization(primary, secondary);
		tertiary = dualOrthogonal.Dot(tertiary) < 0f ? -dualOrthogonal : dualOrthogonal;
	}
	#endregion

	#region Interactions w/ Vect
	/// <summary>
	/// Calculates the dot product of this direction and <paramref name="other"/>; equivalent to <c>other.Dot(this)</c>.
	/// </summary>
	/// <param name="other">The vector to calculate the dot product with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float Dot(Vect other) => other.Dot(this);

	/// <summary>
	/// Calculates the cross product of this direction and <paramref name="other"/>.
	/// </summary>
	/// <remarks>
	/// The result is normalized back to unit length, so it is itself a valid <see cref="Direction"/> perpendicular to
	/// both operands. If <paramref name="other"/>'s direction is parallel or opposite to this direction (or either is
	/// zero-length/<see cref="None"/>), the result is <see cref="None"/>.
	/// </remarks>
	/// <param name="other">The vector to calculate the cross product with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction Cross(Vect other) => FromVector3(Vector3.Cross(ToVector3(), other.ToVector3()));

	/// <summary>
	/// Equivalent to <c>v.OrthogonalizedAgainst(this)</c>.
	/// </summary>
	/// <param name="v">The vector to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? OrthogonalizationOf(Vect v) => v.OrthogonalizedAgainst(this);
	/// <summary>
	/// Equivalent to <c>v.FastOrthogonalizedAgainst(this)</c>.
	/// </summary>
	/// <param name="v">The vector to orthogonalize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastOrthogonalizationOf(Vect v) => v.FastOrthogonalizedAgainst(this);

	/// <summary>
	/// Equivalent to <c>v.ParallelizedWith(this)</c>.
	/// </summary>
	/// <param name="v">The vector to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? ParallelizationOf(Vect v) => v.ParallelizedWith(this);
	/// <summary>
	/// Equivalent to <c>v.FastParallelizedWith(this)</c>.
	/// </summary>
	/// <param name="v">The vector to parallelize.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastParallelizationOf(Vect v) => v.FastParallelizedWith(this);

	Vect ProjectionOf(Vect v) => v.ProjectedOnTo(this);
	Vect? IProjectionTarget<Vect>.ProjectionOf(Vect v) => ProjectionOf(v);
	Vect IProjectionTarget<Vect>.FastProjectionOf(Vect v) => ProjectionOf(v);

	/// <summary>
	/// Determines whether this direction is exactly orthogonal (perpendicular) to <paramref name="v"/>'s direction; equivalent to <c>IsOrthogonalTo(v.Direction)</c>.
	/// </summary>
	/// <param name="v">The vector to compare to.</param>
	public bool IsOrthogonalTo(Vect v) => IsOrthogonalTo(v.Direction);
	/// <summary>
	/// Determines whether this direction is orthogonal (perpendicular) to <paramref name="v"/>'s direction, within <see cref="DefaultParallelOrthogonalTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="v">The vector to compare to.</param>
	public bool IsApproximatelyOrthogonalTo(Vect v) => IsApproximatelyOrthogonalTo(v, DefaultParallelOrthogonalTestApproximationDegrees);
	/// <summary>
	/// Determines whether this direction is orthogonal (perpendicular) to <paramref name="v"/>'s direction, within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="v">The vector to compare to.</param>
	/// <param name="tolerance">How far away from exactly 90° the angle between this direction and <paramref name="v"/>'s direction is allowed to be.</param>
	public bool IsApproximatelyOrthogonalTo(Vect v, Angle tolerance) => IsApproximatelyOrthogonalTo(v.Direction, tolerance);
	/// <summary>
	/// Determines whether this direction is exactly parallel (or exactly opposite) to <paramref name="v"/>'s direction; equivalent to <c>IsParallelTo(v.Direction)</c>.
	/// </summary>
	/// <param name="v">The vector to compare to.</param>
	public bool IsParallelTo(Vect v) => IsParallelTo(v.Direction);
	/// <summary>
	/// Determines whether this direction is parallel (or opposite) to <paramref name="v"/>'s direction, within <see cref="DefaultParallelOrthogonalTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="v">The vector to compare to.</param>
	public bool IsApproximatelyParallelTo(Vect v) => IsApproximatelyParallelTo(v, DefaultParallelOrthogonalTestApproximationDegrees);
	/// <summary>
	/// Determines whether this direction is parallel (or opposite) to <paramref name="v"/>'s direction, within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="v">The vector to compare to.</param>
	/// <param name="tolerance">How far away from exactly 0° or exactly 180° the angle between this direction and <paramref name="v"/>'s direction is allowed to be.</param>
	public bool IsApproximatelyParallelTo(Vect v, Angle tolerance) => IsApproximatelyParallelTo(v.Direction, tolerance);
	#endregion

	#region Rotation
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator >>(Direction start, Direction end) => Rotation.FromStartAndEndDirection(start, end);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator <<(Direction end, Direction start) => Rotation.FromStartAndEndDirection(start, end);
	/// <summary>
	/// Returns the rotation that would turn this direction into <paramref name="other"/>; equivalent to <c>this &gt;&gt; other</c>.
	/// </summary>
	/// <param name="other">The direction this direction should be rotated to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation RotationTo(Direction other) => Rotation.FromStartAndEndDirection(this, other);
	/// <summary>
	/// Returns the rotation that would turn <paramref name="other"/> into this direction; equivalent to <c>this &lt;&lt; other</c>.
	/// </summary>
	/// <param name="other">The direction that should be rotated to this direction.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Rotation RotationFrom(Direction other) => Rotation.FromStartAndEndDirection(other, this);

	/// <summary>
	/// Combines <paramref name="axis"/> and <paramref name="angle"/> in to a new <see cref="Rotation"/>; equivalent to <c>new Rotation(angle, axis)</c>.
	/// </summary>
	/// <param name="axis">The axis of the resultant rotation.</param>
	/// <param name="angle">The angle of the resultant rotation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator %(Direction axis, Angle angle) => new(angle, axis);
	/// <summary>
	/// Combines <paramref name="axis"/> and <paramref name="angle"/> in to a new <see cref="Rotation"/>; equivalent to <c>new Rotation(angle, axis)</c>.
	/// </summary>
	/// <param name="angle">The angle of the resultant rotation.</param>
	/// <param name="axis">The axis of the resultant rotation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Rotation operator %(Angle angle, Direction axis) => new(angle, axis);

	/// <summary>
	/// Returns this direction after being turned by <paramref name="rotation"/>; equivalent to <c>rotation.Rotate(this)</c>.
	/// </summary>
	/// <param name="rotation">The rotation to apply.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction RotatedBy(Rotation rotation) => rotation.Rotate(this);
	/// <summary>
	/// Returns this direction after being turned by <paramref name="rotationQuaternion"/>.
	/// </summary>
	/// <param name="rotationQuaternion">The rotation, as a raw <see cref="Quaternion"/>, to apply.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction RotatedBy(Quaternion rotationQuaternion) => Rotation.Rotate(this, rotationQuaternion);
	/// <summary>
	/// Returns <paramref name="d"/> after being turned by <paramref name="r"/>; equivalent to <c>d.RotatedBy(r)</c>.
	/// </summary>
	/// <param name="d">The direction to rotate.</param>
	/// <param name="r">The rotation to apply.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction operator *(Direction d, Rotation r) => r.Rotate(d);
	/// <summary>
	/// Returns <paramref name="d"/> after being turned by <paramref name="r"/>; equivalent to <c>d.RotatedBy(r)</c>.
	/// </summary>
	/// <param name="r">The rotation to apply.</param>
	/// <param name="d">The direction to rotate.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction operator *(Rotation r, Direction d) => r.Rotate(d);
	#endregion

	#region Clamping and Interpolation
	/// <inheritdoc />
	/// <remarks>
	/// This rotates <paramref name="start"/> towards <paramref name="end"/> by the shortest path (the geodesic between
	/// them on a unit sphere), so the interpolated direction sweeps smoothly and at a constant angular rate from one to
	/// the other as <paramref name="distance"/> goes from <c>0f</c> to <c>1f</c>.
	/// </remarks>
	public static Direction Interpolate(Direction start, Direction end, float distance) {
		return Rotation.FromStartAndEndDirection(start, end).ScaledBy(distance) * start;
	}
	/// <inheritdoc />
	public static Rotation CreateInterpolationPrecomputation(Direction start, Direction end) {
		return Rotation.FromStartAndEndDirection(start, end);
	}
	/// <inheritdoc />
	public static Direction InterpolateUsingPrecomputation(Direction start, Direction end, Rotation precomputation, float distance) {
		return precomputation.ScaledBy(distance) * start;
	}

	/// <summary>
	/// Clamps this direction between <paramref name="min"/> and <paramref name="max"/>.
	/// For example, clamping between <see cref="Up"/> and <see cref="Forward"/> will produce a direction somewhere on the 90° arc
	/// between up and forward.
	/// </summary>
	/// <remarks>
	/// This clamps within the shorter of the two arcs between <paramref name="min"/> and <paramref name="max"/> on their
	/// shared great circle, so it is only meaningful for arcs shorter than 180°. For example, you can't use this to
	/// restrict a direction to a 270°-wide viewing arc, because the "shortest arc" between the two boundary directions
	/// would instead be the other, 90°-wide arc. If you want to clamp within a 3D cone (including cones of 90° or wider),
	/// use <see cref="Clamp(Direction,Angle)"/> instead. If you want to clamp within a 2D arc on a plane (including for arcs greater than 180°),
	/// use <see cref="Clamp(Plane,Direction,Angle,bool)"/> instead.
	/// </remarks>
	/// <param name="min">One end of the arc to clamp within. The meaning of min and max are interchangable here (i.e. min and max can be swapped with no effect on outcome).
	/// Can be <see cref="None"/> (in which case this method returns <c>this</c>).</param>
	/// <param name="max">The other end of the arc to clamp within. The meaning of min and max are interchangable here (i.e. min and max can be swapped with no effect on outcome).
	/// Can be <see cref="None"/> (in which case this method returns <c>this</c>).</param>
	/// <returns>This direction clamped on to the shortest arc between min and max. If min, max, or this are <see cref="None"/>, returns <c>this</c> unchanged.
	/// If min and max are antipodal (i.e. they're exactly opposite directions), returns <c>this</c> unchanged also.</returns>
	public Direction Clamp(Direction min, Direction max) {
		// Doesn't make sense to clamp to "None", so return this
		if (min == None || max == None || this == None) return this;

		// Create a plane that is aligned with the great circle formed between min and max on the unit sphere, 
		// and then create a dimension converter to convert min, max, and this to 2D; with 'min' representing the X-axis
		// (and therefore min in 2D is equal to <1, 0>). Because min is set by definition to be <1, 0> (e.g. the X-axis
		// basis), its polar angle will be, by definition, 0 degrees.
		var minLoc = (Location) min;
		var maxLoc = (Location) max;
		var thisLoc = (Location) this;
		var greatCirclePlane = Plane.FromTriangleOnSurface(minLoc, maxLoc, Location.Origin);
		if (greatCirclePlane == null) {
			// If min and max are antipodal then the entire range of possible directions is valid, so just return this
			if (min.Equals(-max, 0.5f)) return this;
			// Else if min and max are the same then we just return min
			else return min;
		}
		var converter = greatCirclePlane.Value.CreateDimensionConverter(Location.Origin, min);

		// Project max and this on to the 2D plane. Then, compare their polar angles. If this direction's polar angle is between 0 and
		// max's polar angle, it already lies on the arc (after projection), so return the renormalization of the projected value back in
		// to 3D (along the great-circle plane).
		// Otherwise, if the polar angle is greater than max's, we just need to find which point is closer (min or max). We do that by seeing
		// how far around the great circle 'this' is-- if it's further than halfway around from max back to min, we return min; otherwise
		// we return max (that's the final if statement at the bottom).
		// Finally, if this direction was projected down to <0, 0> it is exactly perpendicular to the plane, and therefore perpendicular
		// to the arc. "ThisAngle" will be null, and the if check will result in 'false', and we'll fall through to the final
		// check below, returning 'min'. This is fine, as anywhere on the arc is equally valid here.
		var maxProjection = converter.ConvertLocation(maxLoc);
		var thisProjection = converter.ConvertLocation(thisLoc);
		var thisAngle = thisProjection.PolarAngle;
		if (thisAngle < maxProjection.PolarAngle) return FromVector3(converter.ConvertLocation(thisProjection).ToVector3());
		var midpoint = (Angle.FullCircle - maxProjection.PolarAngle) * 0.5f + maxProjection.PolarAngle;
		return (thisAngle < midpoint) ? max : min;
	}

	/// <summary>
	/// Clamps this direction so that it is no more than <paramref name="maxDifference"/> away (by angle) from <paramref name="target"/>.
	/// This method clamps within a 3D cone around <paramref name="target"/>.
	/// </summary>
	/// <param name="target">The centre of the cone to clamp within. If <paramref name="target"/> or <c>this</c> is <see cref="None"/>, this method returns <c>this</c> unchanged.</param>
	/// <param name="maxDifference">The maximum permitted angle between the result and <paramref name="target"/>. This value is clamped internally between 0° and 180°.</param>
	public Direction Clamp(Direction target, Angle maxDifference) {
		if (target == None || this == None) return this;
		maxDifference = maxDifference.ClampZeroToHalfCircle();

		var difference = target ^ this;
		if (difference <= maxDifference) return this;

		return (target >> this).ScaledBy(maxDifference.Radians / difference.Radians) * target;
	}

	/// <summary>
	/// Clamps this direction so that, when projected on to <paramref name="plane"/>, it lies within an arc of <paramref name="maxArcCentreDifference"/> centred on <paramref name="arcCentre"/>.
	/// </summary>
	/// <remarks>
	/// <paramref name="maxArcCentreDifference"/> is the total width of the permitted arc, split evenly either side of
	/// <paramref name="arcCentre"/> (e.g. a maximum difference of 90° permits up to 45° of rotation either way from
	/// <paramref name="arcCentre"/> within the plane). Only <paramref name="plane"/>'s normal is used; its location is
	/// irrelevant, since directions have no position. If <paramref name="retainOrthogonalDimension"/> is <see langword="true"/>,
	/// the result keeps the same angle to <paramref name="plane"/> as this direction originally had; if <see langword="false"/>,
	/// the result is collapsed fully into the plane.
	/// </remarks>
	/// <param name="plane">The plane to clamp within. Only its normal is used.</param>
	/// <param name="arcCentre">The direction, lying within <paramref name="plane"/>, at the centre of the permitted arc.
	/// If <paramref name="arcCentre"/> or <c>this</c> is <see cref="None"/>, or <paramref name="arcCentre"/> is exactly orthogonal to <paramref name="plane"/>, this method returns <c>this</c> unchanged.</param>
	/// <param name="maxArcCentreDifference">The total angular width of the permitted arc around <paramref name="arcCentre"/>. This value is clamped internally between 0° and 360°.</param>
	/// <param name="retainOrthogonalDimension">Whether to preserve this direction's original angle to <paramref name="plane"/> (<see langword="true"/>), or collapse the result fully into the plane (<see langword="false"/>).</param>
	public Direction Clamp(Plane plane, Direction arcCentre, Angle maxArcCentreDifference, bool retainOrthogonalDimension) {
		if (this == None || arcCentre == None) return this;
		if (arcCentre.ParallelizedWith(plane) == null) return this;
		var halfArc = maxArcCentreDifference.ClampZeroToFullCircle() * 0.5f;
		var converter = plane.CreateDimensionConverter(Location.Origin, arcCentre);
		var resultOnPlane = converter.ConvertVect((Vect) this);
		var polarAngle = (resultOnPlane with { Y = -resultOnPlane.Y }).PolarAngle; // We have to flip Y because the co-ordinate system after 2D conversion has inverted coordinates
		if (polarAngle == null) return this;

		// Outside the max arc diff
		if (polarAngle.Value.ShortestDifferenceTo(Angle.Zero) > halfArc) {
			resultOnPlane = XYPair<float>.FromPolarAngleAndLength(polarAngle > Angle.HalfCircle ? halfArc : -halfArc, resultOnPlane.ToVector2().Length());
		}

		var result = FromVector3(converter.ConvertVect(resultOnPlane).ToVector3());
		if (!retainOrthogonalDimension) return result;

		var angleToPlane = SignedAngleTo(plane);
		return (result >> plane.Normal) with { Angle = angleToPlane } * result;
	}
	#endregion
}