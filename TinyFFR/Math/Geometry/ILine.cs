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

	bool DistanceIsWithinLineBounds(float signedDistanceFromStart);
	float BindDistance(float signedDistanceFromStart);
	Location BoundedLocationAtDistance(float signedDistanceFromStart);
	Location UnboundedLocationAtDistance(float signedDistanceFromStart);
	Location? LocationAtDistanceOrNull(float signedDistanceFromStart);
	//float BoundedDistanceAtPointClosestTo(Location point); //TODO
	//float UnboundedDistanceAtPointClosestTo(Location point); //TODO

	Location ClosestPointToOrigin();
	bool Contains(Location location, float lineThickness);
	float DistanceFromOrigin();

	bool IsIntersectedBy<TLine>(TLine line, float lineThickness = DefaultLineThickness) where TLine : ILine;
	Location? IntersectionWith<TLine>(TLine line, float lineThickness = DefaultLineThickness) where TLine : ILine;
	new Location? IntersectionWith(Plane plane);
	new bool IsIntersectedBy(Plane plane);

	sealed Line CoerceToLine() => new(StartPoint, Direction);
	sealed Ray CoerceToRay() => new(StartPoint, Direction);
	sealed BoundedLine CoerceToBoundedLine(float length) => BoundedLine.FromStartPointAndVect(StartPoint, Direction * length);

	protected internal static float? CalculateUnboundedIntersectionDistanceOnThisLine<TThis, TOther>(TThis @this, TOther other) where TThis : ILine where TOther : ILine {
		const float ParallelTolerance = 1E-7f;

		var thisStart = @this.StartPoint.ToVector3();
		var otherStart = other.StartPoint.ToVector3();

		var thisDir = @this.Direction.ToVector3();
		var otherDir = other.Direction.ToVector3();

		var dot = Vector3.Dot(thisDir, otherDir);
		var linesAreParallel = 1f - MathF.Abs(dot) < ParallelTolerance;

		if (linesAreParallel) return null;

		var oneMinusDotSquared = 1f - (dot * dot);
		var startDiff = thisStart - otherStart;
		var localOrientationStartDiffDot = Vector3.Dot(thisDir, startDiff);
		var otherOrientationStartDiffDot = Vector3.Dot(otherDir, startDiff);
		return (dot * otherOrientationStartDiffDot - localOrientationStartDiffDot) / oneMinusDotSquared;
	}

	protected internal static (float ThisDistance, float OtherDistance)? CalculateUnboundedIntersectionDistancesOnBothLines<TThis, TOther>(TThis @this, TOther other) where TThis : ILine where TOther : ILine {
		const float ParallelTolerance = 1E-7f;

		var thisStart = @this.StartPoint.ToVector3();
		var otherStart = other.StartPoint.ToVector3();

		var thisDir = @this.Direction.ToVector3();
		var otherDir = other.Direction.ToVector3();

		var dot = Vector3.Dot(thisDir, otherDir);
		var linesAreParallel = 1f - MathF.Abs(dot) < ParallelTolerance;

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
public interface ILine<TSelf> : ILine, IGeometryPrimitive<TSelf> where TSelf : ILine<TSelf> {
	TSelf ProjectedOnTo(Plane plane);
	TSelf ParallelizedWith(Plane plane);
	TSelf OrthogonalizedAgainst(Plane plane);
}
public interface ILine<TSelf, TSplit> : ILine<TSelf> where TSelf : ILine<TSelf> where TSplit : struct, ILine<TSplit> {
	TSplit? ReflectedBy(Plane plane);
	TSplit? SlicedBy(Plane plane);
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
	public float PerpendicularityWith<TLine>(TLine line) where TLine : ILine => PerpendicularityWith(line.Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TSplit? Reflect<TLine, TSplit>(TLine line) where TLine : ILine<TLine, TSplit> where TSplit : struct, ILine<TSplit> => line.ReflectedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TSplit? Slice<TLine, TSplit>(TLine line) where TLine : ILine<TLine, TSplit> where TSplit : struct, ILine<TSplit> => line.SlicedBy(this);
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
	public static float PerpendicularityWith<TLine>(this TLine @this, Plane plane) where TLine : ILine => plane.PerpendicularityWith(@this);



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
	public static bool IsIntersectedBy<TLine, T>(this TLine @this, T geometricPrimitive) where T : ILineIntersectable<ConvexShapeLineIntersection> where TLine : ILine => ILineIntersectable<ConvexShapeLineIntersection>.IsIntersectedByGenericLine(geometricPrimitive, @this);



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
	public static bool IsIntersectedBy<T>(this Line @this, T geometricPrimitive) where T : IIntersectable<Line> => geometricPrimitive.IsIntersectedBy(@this);



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
	public static bool IsIntersectedBy<T>(this Ray @this, T geometricPrimitive) where T : IIntersectable<Ray> => geometricPrimitive.IsIntersectedBy(@this);



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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsIntersectedBy<T>(this BoundedLine @this, T geometricPrimitive) where T : IIntersectable<BoundedLine> => geometricPrimitive.IsIntersectedBy(@this);
}