// Created on 2024-03-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Interface used to represent any line-like geometric primitive (e.g. <see cref="Line"/>, <see cref="Ray"/>, <see cref="BoundedRay"/>).
/// </summary>
public partial interface ILineLike :
	IMathPrimitive,
	
	ILineAngleMeasurable,
	ILineDistanceMeasurable,
	ILineClosestEndogenousPointDiscoverable,
	ILineClosestExogenousPointDiscoverable,
	ILineIntersectionDeterminable<Location>,
	ILineParallelizationTarget,
	ILineOrthogonalizationTarget,

	IDistanceMeasurable<Location>,
	IClosestEndogenousPointDiscoverable<Location>,
	IContainer<Location>,

	IAngleMeasurable<Plane>,
	ISignedDistanceMeasurable<Plane>,
	IRelatable<Plane, PlaneObjectRelationship>,
	IClosestEndogenousPointDiscoverable<Plane>,
	IClosestExogenousPointDiscoverable<Plane>,
	IIntersectionDeterminable<Plane, Location>,

	IClosestConvexShapePointsDiscoverable, 
	IConvexShapeDistanceMeasurable,
	IConvexShapeIntersectable<ConvexShapeLineIntersection> {
	/// <summary>
	/// The default thickness used for many line-like geometry operations.
	/// </summary>
	/// <remarks>
	/// A non-zero thickness is often required for meaningful test operations on line-likes (such as intersection tests)
	/// because the finite granularity of floating-point storage does not generally allow for true "line equation" math
	/// to work correctly.
	/// </remarks>
	public const float DefaultLineThickness = 0.01f;
	/// <summary>
	/// The default tolerance (in degrees) used for parallelism, orthogonality, and colinearity tests between line-likes.
	/// </summary>
	public const float DefaultParallelOrthogonalColinearTestApproximationDegrees = Direction.DefaultParallelOrthogonalTestApproximationDegrees;

	/// <summary>
	/// Where in space this line-like starts.
	/// </summary>
	Location StartPoint { get; }
	/// <summary>
	/// Which direction in space this line-like travels along.
	/// </summary>
	Direction Direction { get; }
	/// <summary>
	/// If true, this line-like extends infinitely far in both its stated <see cref="Direction"/> and in the reverse direction.
	/// </summary>
	bool IsUnboundedInBothDirections { get; }
	/// <summary>
	/// If true, this line-like has a finite length. Otherwise it extends infinitely far in at least one direction (but not necessarily two).
	/// </summary>
	[MemberNotNullWhen(true, nameof(Length), nameof(LengthSquared), nameof(StartToEndVect), nameof(EndPoint))]
	bool IsFiniteLength { get; }
	/// <summary>
	/// The length of this line-like, or <c>null</c> if its length is non-finite.
	/// </summary>
	[MemberNotNull(nameof(LengthSquared), nameof(StartToEndVect), nameof(EndPoint))]
	float? Length { get; }
	/// <summary>
	/// The length of this line-like squared, or <c>null</c> if its length is non-finite.
	/// </summary>
	[MemberNotNull(nameof(Length), nameof(StartToEndVect), nameof(EndPoint))]
	float? LengthSquared { get; }
	/// <summary>
	/// The <see cref="Vect"/> representing the displacement from the <see cref="StartPoint"/> of this line-like to its <see cref="EndPoint"/>,
	/// or <c>null</c> if the line-like has non-finite length.
	/// </summary>
	[MemberNotNull(nameof(Length), nameof(LengthSquared), nameof(EndPoint))]
	Vect? StartToEndVect { get; }
	/// <summary>
	/// Where in space this line-like ends, or <c>null</c> if it has no end point.
	/// </summary>
	[MemberNotNull(nameof(Length), nameof(LengthSquared), nameof(StartToEndVect))]
	Location? EndPoint { get; }

	/// <summary>
	/// Determines whether the given distance along <see cref="Direction"/> from <see cref="StartPoint"/>
	/// is within the bounds of this line.
	/// </summary>
	/// <remarks>
	/// Specifically, this method measures <paramref name="signedDistanceFromStart"/> along the
	/// line's <see cref="Direction"/>; where <c>0f</c> is assumed to be the line's <see cref="StartPoint"/>.
	/// A positive value extends along <see cref="Direction"/>, a negative value extends in reverse.
	/// </remarks>
	/// <param name="signedDistanceFromStart">The distance along this line to travel. Positive, negative, zero, and infinite values are permitted.</param>
	/// <returns>Whether the point at the given distance along the line extends beyond it.</returns>
	bool DistanceIsWithinLineBounds(float signedDistanceFromStart);
	/// <summary>
	/// Clamps the given distance to the bounds of this line.
	/// </summary>
	/// <remarks>
	/// Specifically, this method measures <paramref name="signedDistanceFromStart"/> along the
	/// line's <see cref="Direction"/>; where <c>0f</c> is assumed to be the line's <see cref="StartPoint"/>.
	/// A positive value extends along <see cref="Direction"/>, a negative value extends in reverse.
	/// </remarks>
	/// <param name="signedDistanceFromStart">The distance along this line to travel. Positive, negative, zero, and infinite values are permitted.</param>
	/// <returns>If the calculated resultant point is beyond the extent of this line, the returned value will
	/// be clamped to the nearest value that produces a point still on the line. Otherwise, <paramref name="signedDistanceFromStart"/>
	/// is returned unaltered.</returns>
	float BindDistance(float signedDistanceFromStart);
	/// <summary>
	/// Binds <paramref name="signedDistanceFromStart"/> via <see cref="BindDistance"/> (clamping the distance such that the resultant <see cref="Location"/>
	/// is guaranteed to be within the bounds of this line); and then
	/// returns the location of the point on this line found by travelling the bound distance from <see cref="StartPoint"/> along this line's <see cref="Direction"/>.
	/// </summary>
	/// <param name="signedDistanceFromStart">The distance along this line to travel. Positive, negative, and zero values are permitted.</param>
	/// <seealso cref="UnboundedLocationAtDistance"/>
	/// <seealso cref="LocationAtDistanceOrNull"/>
	Location BoundedLocationAtDistance(float signedDistanceFromStart);
	/// <summary>
	/// Returns the location of the point on this line found by travelling the requested distance from <see cref="StartPoint"/> along this line's <see cref="Direction"/>. 
	/// </summary>
	/// <param name="signedDistanceFromStart">The distance along this line to travel. Positive, negative, and zero values are permitted.</param>
	/// <seealso cref="BoundedLocationAtDistance"/>
	/// <seealso cref="LocationAtDistanceOrNull"/>
	Location UnboundedLocationAtDistance(float signedDistanceFromStart);
	/// <summary>
	/// Returns the same as <see cref="UnboundedLocationAtDistance"/> <i>unless</i> the given distance would return a location outside the extents of this line;
	/// in which case <c>null</c> is returned instead.
	/// </summary>
	/// <param name="signedDistanceFromStart">The distance along this line to travel. Positive, negative, and zero values are permitted.</param>
	/// <seealso cref="BoundedLocationAtDistance"/>
	/// <seealso cref="UnboundedLocationAtDistance"/>
	Location? LocationAtDistanceOrNull(float signedDistanceFromStart);
	/// <summary>
	/// Returns the distance along this line that specifies the location closest to the input <paramref name="point"/>.
	/// If an endpoint of this line is closest to the point, the distance will indicate that endpoint. In other words,
	/// the returned distance is guaranteed to be within the actual extents of this line.
	/// </summary>
	/// <param name="point">The point to get the distance on this line nearest to.</param>
	/// <seealso cref="UnboundedDistanceAtPointClosestTo"/>
	float BoundedDistanceAtPointClosestTo(Location point);
	/// <summary>
	/// Returns the distance along this line that specifies the location closest to the input <paramref name="point"/>.
	/// Note that the returned distance may be beyond the extents of the line.
	/// </summary>
	/// <param name="point">The point to get the distance on this line nearest to.</param>
	/// <seealso cref="BoundedDistanceAtPointClosestTo"/>
	float UnboundedDistanceAtPointClosestTo(Location point);

	/// <summary>
	/// Returns the point on this line-like that is closest to the world origin (<see cref="Location.Origin"/>).
	/// </summary>
	Location PointClosestToOrigin();
	/// <summary>
	/// Determines whether this line-like contains <paramref name="location"/>, treating the line as having the given <paramref name="lineThickness"/>.
	/// </summary>
	/// <param name="location">The location to test.</param>
	/// <param name="lineThickness">How far <paramref name="location"/> is permitted to be from the mathematically exact line and still be considered "contained". See <see cref="DefaultLineThickness"/> for more on why this is necessary.</param>
	bool Contains(Location location, float lineThickness);
	/// <summary>
	/// Calculates the distance between this line-like and the world origin (<see cref="Location.Origin"/>).
	/// </summary>
	float DistanceFromOrigin();
	/// <summary>
	/// Calculates the square of the distance between this line-like and the world origin (<see cref="Location.Origin"/>).
	/// </summary>
	/// <remarks>
	/// This is faster than <see cref="DistanceFromOrigin"/> as it avoids a square root, and is sufficient when you only need to compare distances rather than know the exact value.
	/// </remarks>
	float DistanceSquaredFromOrigin();

	/// <summary>
	/// Converts this line-like to a <see cref="Line"/> with the same <see cref="StartPoint"/> and <see cref="Direction"/>, discarding any length/bounds information.
	/// </summary>
	sealed Line CoerceToLine() => new(StartPoint, Direction);
	/// <summary>
	/// Converts this line-like to a <see cref="Ray"/> with the same <see cref="StartPoint"/> and <see cref="Direction"/>, discarding any end-point information.
	/// </summary>
	sealed Ray CoerceToRay() => new(StartPoint, Direction);
	/// <summary>
	/// Converts this line-like to a <see cref="BoundedRay"/> with the same <see cref="StartPoint"/> and <see cref="Direction"/>, but with the given <paramref name="length"/>.
	/// </summary>
	/// <param name="length">The desired length of the resultant ray. Can be negative, in which case the resultant ray points opposite to this line-like's <see cref="Direction"/>.</param>
	sealed BoundedRay CoerceToBoundedRay(float length) => new(StartPoint, Direction * length);

	/// <summary>
	/// Determines whether this line-like is exactly colinear with <paramref name="line"/> (i.e. they lie along the same infinite line, regardless of any length/bounds).
	/// </summary>
	/// <param name="line">The line to compare to.</param>
	/// <param name="lineThickness">How far apart the two lines are permitted to be and still be considered colinear. See <see cref="DefaultLineThickness"/> for more on why this is necessary.</param>
	bool IsExactlyColinearWith(Line line, float lineThickness);
	/// <summary>
	/// Determines whether this line-like is exactly colinear with <paramref name="ray"/> (i.e. they lie along the same infinite line, regardless of any length/bounds).
	/// </summary>
	/// <param name="ray">The ray to compare to.</param>
	/// <param name="lineThickness">How far apart the two lines are permitted to be and still be considered colinear. See <see cref="DefaultLineThickness"/> for more on why this is necessary.</param>
	bool IsExactlyColinearWith(Ray ray, float lineThickness);
	/// <summary>
	/// Determines whether this line-like is exactly colinear with <paramref name="ray"/> (i.e. they lie along the same infinite line, regardless of any length/bounds).
	/// </summary>
	/// <param name="ray">The ray to compare to.</param>
	/// <param name="lineThickness">How far apart the two lines are permitted to be and still be considered colinear. See <see cref="DefaultLineThickness"/> for more on why this is necessary.</param>
	bool IsExactlyColinearWith(BoundedRay ray, float lineThickness);
	/// <summary>
	/// Determines whether this line-like is colinear with <paramref name="line"/>, within <see cref="DefaultLineThickness"/> and <see cref="DefaultParallelOrthogonalColinearTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="line">The line to compare to.</param>
	bool IsApproximatelyColinearWith(Line line);
	/// <summary>
	/// Determines whether this line-like is colinear with <paramref name="ray"/>, within <see cref="DefaultLineThickness"/> and <see cref="DefaultParallelOrthogonalColinearTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="ray">The ray to compare to.</param>
	bool IsApproximatelyColinearWith(Ray ray);
	/// <summary>
	/// Determines whether this line-like is colinear with <paramref name="ray"/>, within <see cref="DefaultLineThickness"/> and <see cref="DefaultParallelOrthogonalColinearTestApproximationDegrees"/>.
	/// </summary>
	/// <param name="ray">The ray to compare to.</param>
	bool IsApproximatelyColinearWith(BoundedRay ray);
	/// <summary>
	/// Determines whether this line-like is colinear with <paramref name="line"/>, within a given <paramref name="lineThickness"/> and angular <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="line">The line to compare to.</param>
	/// <param name="lineThickness">How far apart the two lines are permitted to be and still be considered colinear.</param>
	/// <param name="tolerance">How far away from exactly parallel the two lines' directions are permitted to be.</param>
	bool IsApproximatelyColinearWith(Line line, float lineThickness, Angle tolerance);
	/// <summary>
	/// Determines whether this line-like is colinear with <paramref name="ray"/>, within a given <paramref name="lineThickness"/> and angular <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="ray">The ray to compare to.</param>
	/// <param name="lineThickness">How far apart the two lines are permitted to be and still be considered colinear.</param>
	/// <param name="tolerance">How far away from exactly parallel the two lines' directions are permitted to be.</param>
	bool IsApproximatelyColinearWith(Ray ray, float lineThickness, Angle tolerance);
	/// <summary>
	/// Determines whether this line-like is colinear with <paramref name="ray"/>, within a given <paramref name="lineThickness"/> and angular <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="ray">The ray to compare to.</param>
	/// <param name="lineThickness">How far apart the two lines are permitted to be and still be considered colinear.</param>
	/// <param name="tolerance">How far away from exactly parallel the two lines' directions are permitted to be.</param>
	bool IsApproximatelyColinearWith(BoundedRay ray, float lineThickness, Angle tolerance);

	protected internal static float? CalculateUnboundedIntersectionDistanceOnThisLine<TThis, TOther>(TThis @this, TOther other) where TThis : ILineLike where TOther : ILineLike {
		const float ParallelTolerance = 1E-7f;

		var thisStart = @this.StartPoint.ToVector3();
		var otherStart = other.StartPoint.ToVector3();

		var thisDir = @this.Direction.ToVector3();
		var otherDir = other.Direction.ToVector3();

		var dot = Vector3.Dot(thisDir, otherDir);
		var linesAreParallel = MathF.Abs(dot) >= 1f - ParallelTolerance; // Small tolerance margin to prevent runaway values as we approach 0
		if (linesAreParallel) return null;

		var oneMinusDotSquared = 1f - (dot * dot);
		var startDiff = thisStart - otherStart;
		var localOrientationStartDiffDot = Vector3.Dot(thisDir, startDiff);
		var otherOrientationStartDiffDot = Vector3.Dot(otherDir, startDiff);
		return (dot * otherOrientationStartDiffDot - localOrientationStartDiffDot) / oneMinusDotSquared;
	}

	protected internal static (float ThisDistance, float OtherDistance)? CalculateUnboundedIntersectionDistancesOnBothLines<TThis, TOther>(TThis @this, TOther other) where TThis : ILineLike where TOther : ILineLike {
		const float ParallelTolerance = 1E-7f;

		var thisStart = @this.StartPoint.ToVector3();
		var otherStart = other.StartPoint.ToVector3();

		var thisDir = @this.Direction.ToVector3();
		var otherDir = other.Direction.ToVector3();

		var dot = Vector3.Dot(thisDir, otherDir);
		var linesAreParallel = MathF.Abs(dot) >= 1f - ParallelTolerance; // Small tolerance margin to prevent runaway values as we approach 0

		if (linesAreParallel) return null;

		var oneMinusDotSquared = 1f - (dot * dot);
		var startDiff = thisStart - otherStart;
		var localOrientationStartDiffDot = Vector3.Dot(thisDir, startDiff);
		var otherOrientationStartDiffDot = Vector3.Dot(otherDir, startDiff);
		var thisDist = (dot * otherOrientationStartDiffDot - localOrientationStartDiffDot) / oneMinusDotSquared;
		var otherDist = (-dot * localOrientationStartDiffDot + otherOrientationStartDiffDot) / oneMinusDotSquared;
		return (thisDist, otherDist);
	}
}
/// <summary>
/// Extension of <see cref="ILineLike"/> that includes a self type parameter allowing for more functional definitions.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface ILineLike<TSelf> : ILineLike,
	IMathPrimitive<TSelf>,
	IInvertible<TSelf>,
	IInterpolatable<TSelf>,
	ITranslatable<TSelf>,
	IPointRotatable<TSelf>,
	IProjectable<TSelf, Plane>,
	IParallelizable<TSelf, Plane>,
	IOrthogonalizable<TSelf, Plane>,
	IReflectable<Plane, TSelf>,
	IParallelizable<TSelf, Direction>,
	IParallelizable<TSelf, Line>,
	IParallelizable<TSelf, Ray>,
	IParallelizable<TSelf, BoundedRay>,
	IOrthogonalizable<TSelf, Direction>,
	IOrthogonalizable<TSelf, Line>,
	IOrthogonalizable<TSelf, Ray>,
	IOrthogonalizable<TSelf, BoundedRay>
	where TSelf : struct, ILineLike<TSelf> {
	/// <summary>
	/// Returns this line-like after being turned by <paramref name="rotation"/>, around the point found by travelling <paramref name="signedPivotDistance"/> along this line-like from its <see cref="ILineLike.StartPoint"/>.
	/// </summary>
	/// <param name="rotation">The rotation to apply.</param>
	/// <param name="signedPivotDistance">The distance along this line-like, from <see cref="ILineLike.StartPoint"/>, of the point to pivot around.</param>
	TSelf RotatedBy(Rotation rotation, float signedPivotDistance);
}
/// <summary>
/// Extension of <see cref="ILineLike{TSelf}"/> for line-like types that can be split in to two pieces by a <see cref="Plane"/>.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
/// <typeparam name="TSplitFirst">The type of the first piece resulting from a split.</typeparam>
/// <typeparam name="TSplitSecond">The type of the second piece resulting from a split.</typeparam>
public interface ILineLike<TSelf, TSplitFirst, TSplitSecond> : ILineLike<TSelf> where TSelf : struct, ILineLike<TSelf> {
	/// <summary>
	/// Splits this line-like in to two pieces at the point(s) where it crosses <paramref name="plane"/>.
	/// </summary>
	/// <param name="plane">The plane to split by.</param>
	/// <returns><see langword="null"/> if this line-like does not cross <paramref name="plane"/> (i.e. it lies entirely on one side); the two resultant pieces otherwise.</returns>
	Pair<TSplitFirst, TSplitSecond>? SplitBy(Plane plane);
	/// <summary>
	/// Executes the same function as <see cref="SplitBy"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes this line-like does cross <paramref name="plane"/>. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="plane">The plane to split by.</param>
	Pair<TSplitFirst, TSplitSecond> FastSplitBy(Plane plane);
}