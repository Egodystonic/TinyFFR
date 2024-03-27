// Created on 2024-03-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

public interface ILine : 
	IClosestEndogenousPointDiscoverable<Location>, IDistanceMeasurable<Location>, IContainmentTestable<Location>,
	ILineDistanceMeasurable, 
	ILineClosestPointDiscoverable, 
	ILineIntersectable<Location>,
	IClosestPointDiscoverable<Plane>, ISignedDistanceMeasurable<Plane>, IRelationshipDeterminable<Plane, PlaneObjectRelationship>, IIntersectable<Plane, Location>,
	IGeometryInteractable {
	public const float DefaultLineThickness = 0.01f;

	Location StartPoint { get; }
	Direction Direction { get; }
	bool IsUnboundedInBothDirections { get; }
	[MemberNotNull(nameof(LengthSquared), nameof(StartToEndVect), nameof(EndPoint))]
	float? Length { get; }
	[MemberNotNull(nameof(Length), nameof(StartToEndVect), nameof(EndPoint))]
	float? LengthSquared { get; }
	[MemberNotNull(nameof(Length), nameof(LengthSquared), nameof(EndPoint))]
	Vect? StartToEndVect { get; }
	[MemberNotNull(nameof(Length), nameof(LengthSquared), nameof(StartToEndVect))]
	Location? EndPoint { get; }

	Location BoundedLocationAtDistance(float distanceFromStart);
	Location UnboundedLocationAtDistance(float distanceFromStart);
	Location? LocationAtDistanceOrNull(float distanceFromStart);

	Location ClosestPointToOrigin();
	bool Contains(Location location, float lineThickness);

	Location? IntersectionWith<TLine>(TLine line, float lineThickness = DefaultLineThickness) where TLine : ILine;
	new Location? IntersectionWith(Plane plane);

	sealed Line CoerceToLine() => new(StartPoint, Direction);
	sealed Ray CoerceToRay() => new(StartPoint, Direction);
	sealed BoundedLine CoerceToBoundedLine(float length) => new(StartPoint, Direction * length);

	protected static Location CalculateClosestLocationToOtherLine<TThis, TOther>(TThis @this, TOther other) where TThis : ILine where TOther : ILine {
		const float ParallelTolerance = 0.0001f;

		var thisStart = @this.StartPoint.ToVector3();
		var otherStart = other.StartPoint.ToVector3();

		var thisDir = @this.Direction.ToVector3();
		var otherDir = other.Direction.ToVector3();

		var dot = Vector3.Dot(thisDir, otherDir);
		var linesAreParallel = 1f - MathF.Abs(dot) < ParallelTolerance;

		if (!linesAreParallel) {
			var startDiff = thisStart - otherStart;
			var localOrientationStartDiffDot = Vector3.Dot(thisDir, startDiff);
			var otherOrientationStartDiffDot = Vector3.Dot(otherDir, startDiff);
			var oneMinusDotSquared = 1f - (dot * dot);
			var distFromThisStart = (dot * otherOrientationStartDiffDot - localOrientationStartDiffDot) / oneMinusDotSquared;
			return @this.BoundedLocationAtDistance(distFromThisStart);
		}

		// ==================================== Parallel Lines ========================================================================
		// Everything below this line is handling the case where the lines are parallel, so there are potentially infinite solutions
		// (but not necessarily, e.g. if both lines are bounded in at least one direction and have no overlap, there is still only one answer).
		// The following is my attempt at solving this as best I can, but it's probably a complete car crash and someone smarter than me might fix it one day.

		// We first of all test to see if either line is completely unbounded, if so there are infinite valid answers so just return the easiest one:
		if (@this.IsUnboundedInBothDirections) return other.StartPoint;
		else if (other.IsUnboundedInBothDirections) return @this.StartPoint;

		// We have now narrowed it down to four possible combinations involving the two lines being rays or bounded lines.
		// The following four local methods return the distance along @this that is closest to other, given the correct combination of ray/bounded line inputs.

		/* This method does the calculation assuming both lines are rays.
		 * There are six possible configurations of this and other:
		 *
		 * This				*----->
		 *
		 * 0				*----->
		 * 1		  <-----*
		 *
		 * 2		<-----*
		 * 3			  *----->
		 *
		 * 4			<-----*
		 * 5				  *----->
		 *
		 * The * indicates the start point. In all cases except the last one thisStart is at least equally as close to the other ray as any other point.
		 *		Cases 0/1 show when the other ray's start point is the same as ours the other ray's normal is irrelevant- thisStart will always be on the other ray.
		 *		Cases 2/3 show when the other ray's start point is behind ours our start point will always either lie on the other ray or be the closest point to its start point anyway.
		 *		Cases 4/5 show when the other ray's start point is in front of ours. For case 4, our start point will still be on the other ray. Case 5 shows the only exception when instead otherStart is closer than thisStart.
		 *
		 * So, in summary:
		 *		If otherLineNormalPointsInSameDir is false we can always return thisStart.
		 *		If distanceOnThisLineToOtherStart is <= 0 we can always return thisStart.
		 *		Otherwise, return otherStart.
		 */
		static float RayVsRay(bool otherLineNormalPointsInSameDir, float distanceOnThisLineToOtherStart) {
			return (!otherLineNormalPointsInSameDir || distanceOnThisLineToOtherStart <= 0f) ? 0f : distanceOnThisLineToOtherStart;
		}

		/* This method does the calculation assuming this is a ray and other is a bounded line.
		 *
		 * If the other line's start point or end point are in front of our start point, just return one of those.
		 * Otherwise, the entirety of the other line is behind our start point, so just return our start point as it's got to be the closest.
		 */
		static float RayVsBounded(float distanceOnThisLineToOtherStart, float distanceOnThisLineToOtherEnd) {
			return MathF.Max(distanceOnThisLineToOtherStart, MathF.Max(distanceOnThisLineToOtherEnd, 0f));
		}

		/* This method does the calculation assuming this is a bounded line and other is a ray.
		 * There are six possible configurations of this and other:
		 *
		 * This				*-->--*
		 *
		 * 0					*----->
		 * 1			  <-----*
		 *
		 * 2	<-----*
		 * 3		  *----->
		 *
		 * 4					<-----*
		 * 5						  *----->
		 *
		 * The * indicates the start point.
		 *		Cases 0/1 show when the other ray's start point is in front of our start point but less far than our end point. In these cases, we can return the other ray's start point.
		 *		Cases 2/3 show when the other ray's start point is behind ours. In these cases, we can return our own start point.
		 *		Cases 4/5 show when the other ray's start point is in front of ours and further than our end point. In these cases, we can return our own end point.
		 */
		static float BoundedVsRay(float distanceOnThisLineToOtherStart, float thisLength) {
			return Single.Clamp(distanceOnThisLineToOtherStart, 0f, thisLength);
		}

		/* This method does the calculation assuming both lines are bounded.
		 *
		 * In this case, if the other line's start or end point are within this line's bounds, just return that point (case 1).
		 * Otherwise, if the points are other sides of the start/end of this line, we can return our own start point (case 2)
		 * (as that implies the other line contains this one completely so our start point is as good as any other point on this line).
		 * Otherwise, the lines don't have any overlap so if the other line's start is behind us (case 3), we return our start point; otherwise
		 * we return our end point as the other line is beyond ours (case 4).
		 *
		 * I use two 'flag' variables to quickly determine the relative position of the other line's two points. Each flag can be -1, 0, or 1; meaning:
		 *	-1 means the corresponding point on the other line is behind our start point.
		 *	 0 means the corresponding point on the other line is within our start/end points.
		 *   1 means the corresponding point on the other line is beyond our end point.
		 */
		static float BoundedVsBounded(float distanceOnThisLineToOtherStart, float distanceOnThisLineToOtherEnd, float thisLength) {
			var startFlag = 0;
			if (distanceOnThisLineToOtherStart < 0f) startFlag = -1;
			else if (distanceOnThisLineToOtherStart > thisLength) startFlag = 1;
			
			if (startFlag == 0) return distanceOnThisLineToOtherStart; // case 1

			var endFlag = 0;
			if (distanceOnThisLineToOtherEnd < 0f) endFlag = -1;
			else if (distanceOnThisLineToOtherEnd > thisLength) endFlag = 1;

			if (endFlag == 0) return distanceOnThisLineToOtherEnd; // case 1

			// After this line we know neither flag is 0. In other words, both flags are 1 or -1.
			if (startFlag != endFlag || startFlag == -1) return 0f; // case 2 || case 3
			else return thisLength; // case 4
		}

		var thisEnd = @this.EndPoint;
		var otherEnd = other.EndPoint;
		var distanceOnThisLineToOtherStart = @this.StartPoint.GetVectTo(other.StartPoint).LengthWhenProjectedOnTo(@this.Direction);
		var answerAsDistance = (thisEnd, otherEnd) switch {
			(null, null) => RayVsRay(dot > 0f, distanceOnThisLineToOtherStart),
			(null, _) => RayVsBounded(distanceOnThisLineToOtherStart, @this.StartPoint.GetVectTo(otherEnd.Value).LengthWhenProjectedOnTo(@this.Direction)),
			(_, null) => BoundedVsRay(distanceOnThisLineToOtherStart, @this.Length.Value),
			_ => BoundedVsBounded(distanceOnThisLineToOtherStart, @this.StartPoint.GetVectTo(otherEnd.Value).LengthWhenProjectedOnTo(@this.Direction), @this.Length.Value)
		};
		return @this.UnboundedLocationAtDistance(answerAsDistance); // Using Unbounded variant because we've already done all the bounding in the code above
	}
}
public interface ILine<TSelf> : ILine, IGeometryPrimitive<TSelf> where TSelf : ILine<TSelf> {
	TSelf ProjectedOnTo(Plane plane);
	TSelf ParallelizedWith(Plane plane);
	TSelf OrthogonalizedAgainst(Plane plane);
}
public interface ILine<TSelf, TSplit> : ILine<TSelf> where TSelf : ILine<TSelf> where TSplit : struct, ILine<TSplit> {
	TSplit? ReflectedBy(Plane plane);
	TSplit? SplitBy(Plane plane);
}

// ==================== Below this line: Various "inverted"/reversed/mirrored line methods defined as either extensions or added directly in partial definitions ====================
// I do it this way to keep these definitions close by as they're basically just the same as the definitions above but "the inverse of"
// and I think it makes more sense to keep it all in the same file that's related to Line types.
// ReSharper disable UnusedTypeParameter Type parameterization instead of directly using interface type is used to prevent boxing (instead relying on reification of each parameter combination)
partial struct Location {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsContainedWithin<TLine>(TLine line, float lineThickness = ILine.DefaultLineThickness) where TLine : ILine => line.Contains(this, lineThickness);
}
// Just a bunch of functions specific to working with every line type and Planes that I also want visible when using Plane as the single dispatch target
partial struct Plane {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane lhs, Plane rhs) => lhs.AngleTo(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane plane, Line line) => plane.AngleTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Line line, Plane plane) => plane.AngleTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane plane, Ray line) => plane.AngleTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Ray line, Plane plane) => plane.AngleTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane plane, BoundedLine line) => plane.AngleTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(BoundedLine line, Plane plane) => plane.AngleTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo<TLine>(TLine line) where TLine : ILine => AngleTo(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float ParallelismWith<TLine>(TLine line) where TLine : ILine => ParallelismWith(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TSplit? Reflect<TLine, TSplit>(TLine line) where TLine : ILine<TLine, TSplit> where TSplit : struct, ILine<TSplit> => line.ReflectedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TSplit? Split<TLine, TSplit>(TLine line) where TLine : ILine<TLine, TSplit> where TSplit : struct, ILine<TSplit> => line.SplitBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TLine ProjectionOf<TLine>(TLine line) where TLine : ILine<TLine> => line.ProjectedOnTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TLine ParallelizationOf<TLine>(TLine line) where TLine : ILine<TLine> => line.ParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TLine OrthogonalizationOf<TLine>(TLine line) where TLine : ILine<TLine> => line.OrthogonalizedAgainst(this);
}

// These extensions try to make the reverse/mirror implementations work for TLine (where TLine : ILine) work everywhere in a way that's more or less transparent to the user
public static class LineExtensions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle AngleTo<TLine>(this TLine @this, Plane plane) where TLine : ILine => plane.AngleTo(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float ParallelismWith<TLine>(this TLine @this, Plane plane) where TLine : ILine => plane.ParallelismWith(@this);



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceFrom<TLine, T>(this TLine @this, T geometricPrimitive) where TLine : ILine where T : ILineDistanceMeasurable => ILineDistanceMeasurable.GetDistanceFromGenericLine(geometricPrimitive, @this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceFromSurfaceOf<TLine, T>(this TLine @this, T geometricPrimitive) where TLine : ILine where T : ILineSurfaceDistanceMeasurable => ILineSurfaceDistanceMeasurable.GetSurfaceDistanceFromGenericLine(geometricPrimitive, @this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointOn<TLine, T>(this TLine @this, T geometricPrimitive) where TLine : ILine where T : ILineClosestPointDiscoverable => ILineClosestPointDiscoverable.GetClosestPointToGenericLine(geometricPrimitive, @this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointTo<TLine, T>(this TLine @this, T geometricPrimitive) where TLine : ILine where T : ILineClosestPointDiscoverable => ILineClosestPointDiscoverable.GetClosestPointOnGenericLine(geometricPrimitive, @this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointOnSurfaceOf<TLine, T>(this TLine @this, T geometricPrimitive) where TLine : ILine where T : ILineClosestSurfacePointDiscoverable => ILineClosestSurfacePointDiscoverable.GetClosestPointOnSurfaceToGenericLine(geometricPrimitive, @this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointToSurfaceOf<TLine, T>(this TLine @this, T geometricPrimitive) where TLine : ILine where T : ILineClosestSurfacePointDiscoverable => ILineClosestSurfacePointDiscoverable.GetClosestPointToSurfaceOnGenericLine(geometricPrimitive, @this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ConvexShapeLineIntersection? IntersectionWith<TLine, T>(this TLine @this, T geometricPrimitive) where T : ILineIntersectable<ConvexShapeLineIntersection> where TLine : ILine => ILineIntersectable<ConvexShapeLineIntersection>.GetIntersectionWithGenericLine(geometricPrimitive, @this);



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceFrom<T>(this Line @this, T geometricPrimitive) where T : ILineDistanceMeasurable => geometricPrimitive.DistanceFrom(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceFromSurfaceOf<T>(this Line @this, T geometricPrimitive) where T : ILineSurfaceDistanceMeasurable => geometricPrimitive.SurfaceDistanceFrom(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointOn<T>(this Line @this, T geometricPrimitive) where T : IClosestEndogenousPointDiscoverable<Line> => geometricPrimitive.ClosestPointTo(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointTo<T>(this Line @this, T geometricPrimitive) where T : IClosestExogenousPointDiscoverable<Line> => geometricPrimitive.ClosestPointOn(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointOnSurfaceOf<T>(this Line @this, T geometricPrimitive) where T : IClosestEndogenousSurfacePointDiscoverable<Line> => geometricPrimitive.ClosestPointOnSurfaceTo(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointToSurfaceOf<T>(this Line @this, T geometricPrimitive) where T : IClosestExogenousSurfacePointDiscoverable<Line> => geometricPrimitive.ClosestPointToSurfaceOn(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ConvexShapeLineIntersection? IntersectionWith<T>(this Line @this, T geometricPrimitive) where T : IIntersectable<Line, ConvexShapeLineIntersection> => geometricPrimitive.IntersectionWith(@this);



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceFrom<T>(this Ray @this, T geometricPrimitive) where T : ILineDistanceMeasurable => geometricPrimitive.DistanceFrom(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceFromSurfaceOf<T>(this Ray @this, T geometricPrimitive) where T : ILineSurfaceDistanceMeasurable => geometricPrimitive.SurfaceDistanceFrom(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointOn<T>(this Ray @this, T geometricPrimitive) where T : IClosestEndogenousPointDiscoverable<Ray> => geometricPrimitive.ClosestPointTo(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointTo<T>(this Ray @this, T geometricPrimitive) where T : IClosestExogenousPointDiscoverable<Ray> => geometricPrimitive.ClosestPointOn(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointOnSurfaceOf<T>(this Ray @this, T geometricPrimitive) where T : IClosestEndogenousSurfacePointDiscoverable<Ray> => geometricPrimitive.ClosestPointOnSurfaceTo(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointToSurfaceOf<T>(this Ray @this, T geometricPrimitive) where T : IClosestExogenousSurfacePointDiscoverable<Ray> => geometricPrimitive.ClosestPointToSurfaceOn(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ConvexShapeLineIntersection? IntersectionWith<T>(this Ray @this, T geometricPrimitive) where T : IIntersectable<Ray, ConvexShapeLineIntersection> => geometricPrimitive.IntersectionWith(@this);



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceFrom<T>(this BoundedLine @this, T geometricPrimitive) where T : ILineDistanceMeasurable => geometricPrimitive.DistanceFrom(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceFromSurfaceOf<T>(this BoundedLine @this, T geometricPrimitive) where T : ILineSurfaceDistanceMeasurable => geometricPrimitive.SurfaceDistanceFrom(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointOn<T>(this BoundedLine @this, T geometricPrimitive) where T : IClosestEndogenousPointDiscoverable<BoundedLine> => geometricPrimitive.ClosestPointTo(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointTo<T>(this BoundedLine @this, T geometricPrimitive) where T : IClosestExogenousPointDiscoverable<BoundedLine> => geometricPrimitive.ClosestPointOn(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointOnSurfaceOf<T>(this BoundedLine @this, T geometricPrimitive) where T : IClosestEndogenousSurfacePointDiscoverable<BoundedLine> => geometricPrimitive.ClosestPointOnSurfaceTo(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location ClosestPointToSurfaceOf<T>(this BoundedLine @this, T geometricPrimitive) where T : IClosestExogenousSurfacePointDiscoverable<BoundedLine> => geometricPrimitive.ClosestPointToSurfaceOn(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ConvexShapeLineIntersection? IntersectionWith<T>(this BoundedLine @this, T geometricPrimitive) where T : IIntersectable<BoundedLine, ConvexShapeLineIntersection> => geometricPrimitive.IntersectionWith(@this);
}