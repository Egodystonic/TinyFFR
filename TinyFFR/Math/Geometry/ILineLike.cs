// Created on 2024-03-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

public interface ILineLike :
	IMathPrimitive,
	
	ILineAngleMeasurable,
	ILineDistanceMeasurable,
	ILineClosestEndogenousPointDiscoverable,
	ILineClosestExogenousPointDiscoverable,
	ILineIntersectionDeterminable<Location>,

	IDistanceMeasurable<Location>,
	IClosestEndogenousPointDiscoverable<Location>,
	IContainer<Location>,

	ISignedDistanceMeasurable<Plane>,
	IRelatable<Plane, PlaneObjectRelationship>,
	IClosestEndogenousPointDiscoverable<Plane>,
	IClosestExogenousPointDiscoverable<Plane> {
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
	float BoundedDistanceAtPointClosestTo(Location point);
	float UnboundedDistanceAtPointClosestTo(Location point);

	Location ClosestPointToOrigin();
	bool Contains(Location location, float lineThickness);
	float DistanceFromOrigin();
	float DistanceSquaredFromOrigin();

	sealed Line CoerceToLine() => new(StartPoint, Direction);
	sealed Ray CoerceToRay() => new(StartPoint, Direction);
	sealed BoundedRay CoerceToBoundedLine(float length) => BoundedRay.FromStartPointAndVect(StartPoint, Direction * length);

	// TODO static methods for generic usage of ILineLike interfaces with TLines (maybe defined on ILineLike<TSelf>? would make sense I think... Do they even need to be static, or can they be implemented as instance methods on the interface?)
	// Could we move our internal statics below on to that? The thing about making them non-static is that means we sometimes might need to cast to the generic interface, so I don't like that
	// Make it statics so we don't have overload resolution issues

	protected internal static float? CalculateUnboundedIntersectionDistanceOnThisLine<TThis, TOther>(TThis @this, TOther other) where TThis : ILineLike where TOther : ILineLike {
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

	protected internal static (float ThisDistance, float OtherDistance)? CalculateUnboundedIntersectionDistancesOnBothLines<TThis, TOther>(TThis @this, TOther other) where TThis : ILineLike where TOther : ILineLike {
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

	protected internal static Angle AngleTo<TThis, TOther>(TThis @this, TOther other) where TThis : ILineLike where TOther : ILineLike {
		return @this.Direction.AngleTo(other.Direction);
	}

	protected internal static Location? IntersectionWith<TThis, TOther>(TThis @this, TOther other, float lineThickness) where TThis : ILineLike where TOther : ILineLike {
		var closestPointOnLine = @this.ClosestPointOn(other);
		return @this.DistanceFrom(closestPointOnLine) <= lineThickness ? closestPointOnLine : null;
	}
}
public interface ILineLike<TSelf> : ILineLike,
	IMathPrimitive<TSelf>,
	IInvertible<TSelf>,
	IInterpolatable<TSelf>,
	ITranslatable<TSelf>,
	IRotatable<TSelf>,
	IPointRotatable<TSelf>,
	IProjectable<TSelf, Plane>,
	IParallelizable<TSelf, Plane>,
	IOrthogonalizable<TSelf, Plane>
	where TSelf : ILineLike<TSelf>;
public interface ILineLike<TSelf, TSplit> : IReflectable<Plane, TSplit>, IIntersectionDeterminable<Plane, TSplit>, ILineLike<TSelf> where TSelf : ILineLike<TSelf> where TSplit : struct, ILineLike<TSplit>;

partial struct Line {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Line line) => ILineLike.AngleTo(this, line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Ray ray) => ILineLike.AngleTo(this, ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(BoundedRay ray) => ILineLike.AngleTo(this, ray);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Line line) => DistanceSquaredFrom(ClosestPointOn(line));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Ray ray) => DistanceSquaredFrom(ClosestPointOn(ray));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(BoundedRay ray) => DistanceSquaredFrom(ClosestPointOn(ray));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Plane plane) => DistanceSquaredFrom(ClosestPointOn(plane));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line) => IsIntersectedBy(line, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line, float lineThickness) => ILineLike.IntersectionWith(this, line, lineThickness) != null;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray) => IsIntersectedBy(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness) != null;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray) => IsIntersectedBy(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness) != null;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line) => IntersectionWith(line, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line, float lineThickness) => ILineLike.IntersectionWith(this, line, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray) => IntersectionWith(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray) => IntersectionWith(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Line line) => line.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Ray ray) => ray.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(BoundedRay boundedRay) => boundedRay.PointClosestTo(this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Line line) => DistanceFrom(ClosestPointOn(line));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Ray ray) => DistanceFrom(ClosestPointOn(ray));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(BoundedRay boundedRay) => DistanceFrom(ClosestPointOn(boundedRay));
}
partial struct Ray {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Line line) => ILineLike.AngleTo(this, line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Ray ray) => ILineLike.AngleTo(this, ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(BoundedRay ray) => ILineLike.AngleTo(this, ray);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Line line) => DistanceSquaredFrom(ClosestPointOn(line));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Ray ray) => DistanceSquaredFrom(ClosestPointOn(ray));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(BoundedRay ray) => DistanceSquaredFrom(ClosestPointOn(ray));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Plane plane) => DistanceSquaredFrom(ClosestPointOn(plane));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line) => IsIntersectedBy(line, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line, float lineThickness) => ILineLike.IntersectionWith(this, line, lineThickness) != null;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray) => IsIntersectedBy(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness) != null;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray) => IsIntersectedBy(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness) != null;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line) => IntersectionWith(line, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line, float lineThickness) => ILineLike.IntersectionWith(this, line, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray) => IntersectionWith(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray) => IntersectionWith(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Line line) => line.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Ray ray) => ray.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(BoundedRay boundedRay) => boundedRay.PointClosestTo(this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Line line) => DistanceFrom(ClosestPointOn(line));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Ray ray) => DistanceFrom(ClosestPointOn(ray));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(BoundedRay boundedRay) => DistanceFrom(ClosestPointOn(boundedRay));
}
partial struct BoundedRay {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Line line) => ILineLike.AngleTo(this, line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Ray ray) => ILineLike.AngleTo(this, ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(BoundedRay ray) => ILineLike.AngleTo(this, ray);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Line line) => DistanceSquaredFrom(ClosestPointOn(line));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Ray ray) => DistanceSquaredFrom(ClosestPointOn(ray));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(BoundedRay ray) => DistanceSquaredFrom(ClosestPointOn(ray));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Plane plane) => DistanceSquaredFrom(ClosestPointOn(plane));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line) => IsIntersectedBy(line, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line, float lineThickness) => ILineLike.IntersectionWith(this, line, lineThickness) != null;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray) => IsIntersectedBy(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness) != null;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray) => IsIntersectedBy(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness) != null;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line) => IntersectionWith(line, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Line line, float lineThickness) => ILineLike.IntersectionWith(this, line, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray) => IntersectionWith(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(Ray ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray) => IntersectionWith(ray, ILineLike.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionWith(BoundedRay ray, float lineThickness) => ILineLike.IntersectionWith(this, ray, lineThickness);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Line line) => line.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Ray ray) => ray.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(BoundedRay boundedRay) => boundedRay.PointClosestTo(this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Line line) => DistanceFrom(ClosestPointOn(line));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Ray ray) => DistanceFrom(ClosestPointOn(ray));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(BoundedRay boundedRay) => DistanceFrom(ClosestPointOn(boundedRay));
}

partial struct Location : ILineDistanceMeasurable, ILineClosestExogenousPointDiscoverable, ILineContainable {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Line line) => line.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Ray ray) => ray.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(BoundedRay ray) => ray.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Line line) => line.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Ray ray) => ray.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(BoundedRay ray) => ray.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Line line) => line.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Ray ray) => ray.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(BoundedRay ray) => ray.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsContainedWithin(Line line) => line.Contains(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsContainedWithin(Ray ray) => ray.Contains(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsContainedWithin(BoundedRay ray) => ray.Contains(this);
}
partial struct Plane : 
	ILineProjectionTarget, 
	ILineOrthogonalizationTarget, 
	ILineParallelizationTarget, 
	ILineSignedDistanceMeasurable, 
	ILineClosestEndogenousPointDiscoverable, 
	ILineClosestExogenousPointDiscoverable,
	ILineRelatable<PlaneObjectRelationship>,
	IIntersectionDeterminable<Plane, Line, Ray>,
	IIntersectionDeterminable<Plane, Ray, Ray>,
	IIntersectionDeterminable<Plane, BoundedRay, BoundedRay> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line ProjectionOf(Line line) => line.ProjectedOnTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray ProjectionOf(Ray ray) => ray.ProjectedOnTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay ProjectionOf(BoundedRay ray) => ray.ProjectedOnTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line OrthogonalizationOf(Line line) => line.OrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray OrthogonalizationOf(Ray ray) => ray.OrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay OrthogonalizationOf(BoundedRay ray) => ray.OrthogonalizedAgainst(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line ParallelizationOf(Line line) => line.ParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray ParallelizationOf(Ray ray) => ray.ParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay ParallelizationOf(BoundedRay ray) => ray.ParallelizedWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line) => line.IsIntersectedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray) => ray.IsIntersectedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray) => ray.IsIntersectedBy(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? IntersectionWith(Line line) => line.IntersectionWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray? IntersectionWith(Ray ray) => ray.IntersectionWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay? IntersectionWith(BoundedRay ray) => ray.IntersectionWith(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Line line) => line.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SignedDistanceFrom(Line line) => line.SignedDistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Line line) => line.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Ray ray) => ray.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SignedDistanceFrom(Ray ray) => ray.SignedDistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Ray ray) => ray.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(BoundedRay ray) => ray.DistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SignedDistanceFrom(BoundedRay ray) => ray.SignedDistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(BoundedRay ray) => ray.DistanceSquaredFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo(Line line) => line.ClosestPointOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo(Ray ray) => ray.ClosestPointOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo(BoundedRay ray) => ray.ClosestPointOn(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Line line) => line.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Ray ray) => ray.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(BoundedRay ray) => ray.PointClosestTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PlaneObjectRelationship RelationshipTo(Line line) => line.RelationshipTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PlaneObjectRelationship RelationshipTo(Ray ray) => ray.RelationshipTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PlaneObjectRelationship RelationshipTo(BoundedRay ray) => ray.RelationshipTo(this);
}