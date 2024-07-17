// Created on 2024-03-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

public partial interface ILineLike :
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
	IClosestExogenousPointDiscoverable<Plane>,

	IClosestConvexShapePointsDiscoverable, 
	IConvexShapeDistanceMeasurable,
	IConvexShapeIntersectable<ConvexShapeLineIntersection> {
	public const float DefaultLineThickness = 0.01f;

	Location StartPoint { get; }
	Direction Direction { get; }
	bool IsUnboundedInBothDirections { get; }
	[MemberNotNullWhen(true, nameof(Length), nameof(LengthSquared), nameof(StartToEndVect), nameof(EndPoint))]
	bool IsFiniteLength { get; }
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

	Location PointClosestToOrigin();
	bool Contains(Location location, float lineThickness);
	float DistanceFromOrigin();
	float DistanceSquaredFromOrigin();

	sealed Line CoerceToLine() => new(StartPoint, Direction);
	sealed Ray CoerceToRay() => new(StartPoint, Direction);
	sealed BoundedRay CoerceToBoundedRay(float length) => BoundedRay.FromStartPointAndVect(StartPoint, Direction * length);

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
	where TSelf : struct, ILineLike<TSelf>;
public interface ILineLike<TSelf, TSplit> : IReflectable<Plane, TSplit>, IIntersectionDeterminable<Plane, TSplit>, ILineLike<TSelf> where TSelf : struct, ILineLike<TSelf> where TSplit : struct, ILineLike<TSplit>;