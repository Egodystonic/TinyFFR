// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;

namespace Egodystonic.TinyFFR;

public readonly partial struct OriginCuboid
	: IMultiplyOperators<OriginCuboid, float, OriginCuboid> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static OriginCuboid operator *(OriginCuboid cuboid, float scalar) => cuboid.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static OriginCuboid operator *(float scalar, OriginCuboid cuboid) => cuboid.ScaledBy(scalar);

	public OriginCuboid ScaledBy(float scalar) => FromHalfDimensions(HalfWidth * scalar, HalfHeight * scalar, HalfDepth * scalar);
	public OriginCuboid WithVolume(float newVolume) {
		var diffCubeRoot = MathF.Cbrt(newVolume / Volume);
		return FromHalfDimensions(HalfWidth * diffCubeRoot, HalfHeight * diffCubeRoot, HalfDepth * diffCubeRoot);
	}
	public OriginCuboid WithSurfaceArea(float newSurfaceArea) {
		var diffSquareRoot = MathF.Sqrt(newSurfaceArea / SurfaceArea);
		return FromHalfDimensions(HalfWidth * diffSquareRoot, HalfHeight * diffSquareRoot, HalfDepth * diffSquareRoot);
	}

	// TODO these GetX methods need a naming pass and this file vs Cuboid.cs? etc

	public Location GetCornerLocation(DiagonalOrientation3D corner) { // TODO add properties that enumerate corners, surfaces, etc and use them instead of foreach loops internally
		if (corner == DiagonalOrientation3D.None) throw new ArgumentOutOfRangeException(nameof(corner), corner, $"Can not be '{nameof(DiagonalOrientation3D.None)}'.");

		return new(
			corner.GetAxisSign(Axis.X) * HalfWidth,
			corner.GetAxisSign(Axis.Y) * HalfHeight,
			corner.GetAxisSign(Axis.Z) * HalfDepth
		);
	}
	
	public Plane GetSideSurfacePlane(CardinalOrientation3D side) { // TODO xmldoc that the planes' normals point away from the cuboid centre, e.g. side.ToDirection()
		if (side == CardinalOrientation3D.None) throw new ArgumentOutOfRangeException(nameof(side), side, $"Can not be '{nameof(CardinalOrientation3D.None)}'.");

		return new(side.ToDirection(), GetHalfExtent(side.GetAxis()));
	}

	public BoundedLine GetEdge(IntercardinalOrientation3D edge) {
		if (edge == IntercardinalOrientation3D.None) throw new ArgumentOutOfRangeException(nameof(edge), edge, $"Can not be '{nameof(IntercardinalOrientation3D.None)}'.");

		var unspecifiedAxis = edge.GetUnspecifiedAxis();
		return new(
			GetCornerLocation((DiagonalOrientation3D) edge.AsGeneralOrientation().WithAxisSign(unspecifiedAxis, -1)),
			GetCornerLocation((DiagonalOrientation3D) edge.AsGeneralOrientation().WithAxisSign(unspecifiedAxis, 1))
		);
	}

	public float DistanceFrom(Location location) {
		var xDist = MathF.Max(0f, MathF.Abs(location.X) - HalfWidth);
		var yDist = MathF.Max(0f, MathF.Abs(location.Y) - HalfHeight);
		var zDist = MathF.Max(0f, MathF.Abs(location.Z) - HalfDepth);

		return new Vector3(xDist, yDist, zDist).Length();
	}
	public float SurfaceDistanceFrom(Location location) {
		var xDist = MathF.Abs(location.X) - HalfWidth;
		var yDist = MathF.Abs(location.Y) - HalfHeight;
		var zDist = MathF.Abs(location.Z) - HalfDepth;

		if (xDist < 0f && yDist < 0f && zDist < 0f) { // Inside the cuboid
			return MathF.Abs(MathF.Max(xDist, MathF.Max(yDist, zDist)));
		}

		return new Vector3(MathF.Max(0f, xDist), MathF.Max(0f, yDist), MathF.Max(0f, zDist)).Length();
	}

	public bool Contains(Location location) => MathF.Abs(location.X) <= HalfWidth && MathF.Abs(location.Y) <= HalfHeight && MathF.Abs(location.Z) <= HalfDepth;

	public Location PointClosestTo(Location location) {
		return new(
			Single.Clamp(location.X, -HalfWidth, HalfWidth),
			Single.Clamp(location.Y, -HalfHeight, HalfHeight),
			Single.Clamp(location.Z, -HalfDepth, HalfDepth)
		);
	}
	public Location SurfacePointClosestTo(Location location) {
		var closestNonSurfacePoint = PointClosestTo(location);
		if (location != closestNonSurfacePoint) return closestNonSurfacePoint;

		var xDiff = HalfWidth - MathF.Abs(location.X);
		var yDiff = HalfHeight - MathF.Abs(location.Y);
		var zDiff = HalfDepth - MathF.Abs(location.Z);
		if (xDiff < yDiff) {
			if (xDiff < zDiff) return location with { X = HalfWidth * (location.X < 0f ? -1f : 1f) };
			else return location with { Z = HalfDepth * (location.Z < 0f ? -1f : 1f) };
		}
		else if (yDiff < zDiff) return location with { Y = HalfHeight * (location.Y < 0f ? -1f : 1f) };
		else return location with { Z = HalfDepth * (location.Z < 0f ? -1f : 1f) };
	}

	public Location ClosestPointTo<TLine>(TLine line) where TLine : ILine => PointClosestTo(ClosestPointOn(line));
	public Location ClosestPointOn<TLine>(TLine line) where TLine : ILine {
		if (Contains(line.StartPoint)) return line.StartPoint;
		else return ClosestPointToSurfaceOn(line);
	}
	public float DistanceFrom<TLine>(TLine line) where TLine : ILine {
		if (Contains(line.StartPoint)) return 0f;
		else return SurfaceDistanceFrom(line);
	}

	public Location ClosestPointOnSurfaceTo<TLine>(TLine line) where TLine : ILine => SurfacePointClosestTo(ClosestPointToSurfaceOn(line));
	public Location ClosestPointToSurfaceOn(Line line) {
		var intersections = GetUnboundedLineIntersectionDistances(new Ray(line.PointOnLine, line.Direction));
		if (intersections != null) return line.LocationAtDistance(intersections.Value.Item1);
		else return GetClosestPointToSurfaceOnNonIntersectingLine(line);
	}
	public Location ClosestPointToSurfaceOn(Ray ray) {
		var intersections = GetUnboundedLineIntersectionDistances(ray);
		return (intersections?.Item1 >= 0f, intersections?.Item2 >= 0f) switch {
			(true, true) => ray.UnboundedLocationAtDistance(intersections!.Value.Item1 < intersections.Value.Item2 ? intersections.Value.Item1 : intersections.Value.Item2),
			(true, false) => ray.UnboundedLocationAtDistance(intersections!.Value.Item1),
			(false, true) => ray.UnboundedLocationAtDistance(intersections!.Value.Item2),
			_ => GetClosestPointToSurfaceOnNonIntersectingLine(ray)
		};
	}
	public Location ClosestPointToSurfaceOn(BoundedLine line) {
		var intersections = GetUnboundedLineIntersectionDistances(new Ray(line.StartPoint, line.Direction));
		var lineLength = line.Length;
		return (intersections?.Item1 >= 0f && intersections?.Item1 <= lineLength, intersections?.Item2 >= 0f && intersections?.Item2 <= lineLength) switch {
			(true, true) => line.UnboundedLocationAtDistance(intersections!.Value.Item1 < intersections.Value.Item2 ? intersections.Value.Item1 : intersections.Value.Item2),
			(true, false) => line.UnboundedLocationAtDistance(intersections!.Value.Item1),
			(false, true) => line.UnboundedLocationAtDistance(intersections!.Value.Item2),
			_ => GetClosestPointToSurfaceOnNonIntersectingLine(line)
		};
	}
	public Location ClosestPointToSurfaceOn<TLine>(TLine line) where TLine : ILine {
		return line switch {
			Line l => ClosestPointToSurfaceOn(l),
			Ray r => ClosestPointToSurfaceOn(r),
			BoundedLine b => ClosestPointToSurfaceOn(b),
			_ => FallbackClosestPointToSurfaceOn(line)
		};
	}
	Location FallbackClosestPointToSurfaceOn<TLine>(TLine line) where TLine : ILine {
		var intersection = IntersectionWith(line);
		if (intersection == null) return GetClosestPointToSurfaceOnNonIntersectingLine(line);
		else return intersection.Value.First;
	}
	Location GetClosestPointToSurfaceOnNonIntersectingLine<TLine>(TLine line) where TLine : ILine {
		var answerDistance = Single.PositiveInfinity;
		var answer = Location.Origin;
		foreach (var edgeOrientation in OrientationUtils.AllIntercardinals) {
			var edge = GetEdge(edgeOrientation);
			var closestPointToEdge = line.PointClosestTo(edge);
			var distanceToEdge = edge.DistanceFrom(closestPointToEdge);
			if (distanceToEdge < answerDistance) {
				answerDistance = distanceToEdge;
				answer = closestPointToEdge;
			}
		}
		return answer;
	}
	public float SurfaceDistanceFrom<TLine>(TLine line) where TLine : ILine => SurfaceDistanceFrom(ClosestPointToSurfaceOn(line));

	public bool IsIntersectedBy<TLine>(TLine line) where TLine : ILine {
		var distanceTuple = GetUnboundedLineIntersectionDistances(line.CoerceToRay());
		if (distanceTuple == null) return false;
		return line.DistanceIsWithinLineBounds(distanceTuple.Value.Item1) || line.DistanceIsWithinLineBounds(distanceTuple.Value.Item2);
	}
	public ConvexShapeLineIntersection? IntersectionWith<TLine>(TLine line) where TLine : ILine {
		var unboundedDistances = GetUnboundedLineIntersectionDistances(line.CoerceToRay());
		if (unboundedDistances == null) return null;
		return ConvexShapeLineIntersection.FromTwoPotentiallyNullArgs(line.LocationAtDistanceOrNull(unboundedDistances.Value.Item1), line.LocationAtDistanceOrNull(unboundedDistances.Value.Item2));
	}

	(float, float)? GetUnboundedLineIntersectionDistances(Ray ray) {
		var x1 = SignedLineDistanceToPositiveSurfacePlane(ray.StartPoint, ray.Direction, Axis.X);
		var x2 = SignedLineDistanceToNegativeSurfacePlane(ray.StartPoint, ray.Direction, Axis.X);
		var minX = MathF.Min(x1, x2);
		var maxX = MathF.Max(x1, x2);

		var y1 = SignedLineDistanceToPositiveSurfacePlane(ray.StartPoint, ray.Direction, Axis.Y);
		var y2 = SignedLineDistanceToNegativeSurfacePlane(ray.StartPoint, ray.Direction, Axis.Y);
		var minY = MathF.Min(y1, y2);
		var maxY = MathF.Max(y1, y2);

		var z1 = SignedLineDistanceToPositiveSurfacePlane(ray.StartPoint, ray.Direction, Axis.Z);
		var z2 = SignedLineDistanceToNegativeSurfacePlane(ray.StartPoint, ray.Direction, Axis.Z);
		var minZ = MathF.Min(z1, z2);
		var maxZ = MathF.Max(z1, z2);

		var startDist = MathF.Max(MathF.Max(minX, minY), minZ);
		var endDist = MathF.Min(MathF.Min(maxX, maxY), maxZ);
		if (endDist < startDist) return null;
		else return (startDist, endDist);
	}

	// Returns an Infinity when line is parallel to plane -- but still with correct sign depending on which side of the face the start point is
	float SignedLineDistanceToPositiveSurfacePlane(Location lineStartPoint, Direction lineDirection, Axis axis) {
		return (GetHalfExtent(axis) - lineStartPoint[axis]) / lineDirection[axis];
	}
	// Returns an Infinity when line is parallel to plane -- but still with correct sign depending on which side of the face the start point is
	float SignedLineDistanceToNegativeSurfacePlane(Location lineStartPoint, Direction lineDirection, Axis axis) {
		return (-GetHalfExtent(axis) - lineStartPoint[axis]) / lineDirection[axis];
	}

	bool QuickPlaneCuboidIntersectionTest(Plane plane) { // https://gdbooks.gitbooks.io/3dcollisions/content/Chapter2/static_aabb_plane.html
		var halfDiagonalProjection = Width * MathF.Abs(plane.Normal.X) + Height * MathF.Abs(plane.Normal.Y) + Depth * MathF.Abs(plane.Normal.Z);
		return plane.DistanceFromOrigin() <= halfDiagonalProjection;
	}

	const int MaxPlaneIntersectionPoints = 6;
	int GetPlaneIntersectionPoints(Plane plane, Span<Location> pointSpan) { 
		var intersectionCount = 0;
		foreach (var edge in OrientationUtils.AllIntercardinals) {
			var edgeIntersection = GetEdge(edge).IntersectionWith(plane);
			if (edgeIntersection.HasValue) pointSpan[intersectionCount++] = edgeIntersection.Value;
		}
		return intersectionCount;
	}
	Location? GetAnyPlaneIntersectionPoint(Plane plane) {
		foreach (var edge in OrientationUtils.AllIntercardinals) {
			var edgeIntersection = GetEdge(edge).IntersectionWith(plane);
			if (edgeIntersection.HasValue) return edgeIntersection.Value;
		}
		return null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo(Plane plane) => ClosestPointOnSurfaceTo(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Plane plane) => ClosestPointToSurfaceOn(plane);
	public float SignedDistanceFrom(Plane plane) {
		if (QuickPlaneCuboidIntersectionTest(plane)) return 0f;

		var result = Single.PositiveInfinity;
		foreach (var diagonal in OrientationUtils.AllDiagonals) {
			var corner = GetCornerLocation(diagonal);
			var distance = plane.SignedDistanceFrom(corner);
			result = MathF.MinMagnitude(distance, result);
		}
		return result;
	}
	public float DistanceFrom(Plane plane) {
		if (QuickPlaneCuboidIntersectionTest(plane)) return 0f;

		var result = Single.PositiveInfinity;
		foreach (var diagonal in OrientationUtils.AllDiagonals) {
			var corner = GetCornerLocation(diagonal);
			var distance = plane.DistanceFrom(corner);
			result = MathF.Min(distance, result);
		}
		return result;
	}
	public PlaneObjectRelationship RelationshipTo(Plane plane) {
		if (QuickPlaneCuboidIntersectionTest(plane)) return PlaneObjectRelationship.PlaneIntersectsObject;
		return plane.FacesTowardsOrigin(planeThickness: 0f) ? PlaneObjectRelationship.PlaneFacesTowardsObject : PlaneObjectRelationship.PlaneFacesAwayFromObject;
	}
	public Location ClosestPointOnSurfaceTo(Plane plane) {
		if (QuickPlaneCuboidIntersectionTest(plane)) return GetAnyPlaneIntersectionPoint(plane)!.Value;
		var resultDistance = Single.PositiveInfinity;
		var result = Location.Origin;
		foreach (var diagonal in OrientationUtils.AllDiagonals) {
			var corner = GetCornerLocation(diagonal);
			var distance = plane.DistanceFrom(corner);
			if (distance < resultDistance) {
				resultDistance = distance;
				result = corner;
			}
		}
		return result;
	}
	public Location ClosestPointToSurfaceOn(Plane plane) => plane.PointClosestTo(ClosestPointOnSurfaceTo(plane));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SurfaceDistanceFrom(Plane plane) => DistanceFrom(plane);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SignedSurfaceDistanceFrom(Plane plane) => SignedDistanceFrom(plane);

	public float GetExtent(Axis axis) => axis switch { // TODO rename this and below to get extent? Probably because it's actually an extent (e.g. it's just a measurement, not any actual point)
		Axis.X => Width,
		Axis.Y => Height,
		Axis.Z => Depth,
		_ => throw new ArgumentException($"{nameof(Axis)} can not be {nameof(Axis.None)}.", nameof(axis))
	};
	public float GetHalfExtent(Axis axis) => axis switch {
		Axis.X => HalfWidth,
		Axis.Y => HalfHeight,
		Axis.Z => HalfDepth,
		_ => throw new ArgumentException($"{nameof(Axis)} can not be {nameof(Axis.None)}.", nameof(axis))
	};

	public float GetSideSurfaceArea(CardinalOrientation3D side) {
		return side.GetAxis() switch {
			Axis.X => HalfHeight * HalfDepth * 4f,
			Axis.Y => HalfDepth * HalfWidth * 4f,
			Axis.Z => HalfWidth * HalfHeight * 4f,
			_ => throw new ArgumentException($"{nameof(CardinalOrientation3D)} can not be {nameof(CardinalOrientation3D.None)}.", nameof(side))
		};
	}

	public static OriginCuboid Interpolate(OriginCuboid start, OriginCuboid end, float distance) {
		return FromHalfDimensions(
			Single.Lerp(start.HalfWidth, end.HalfWidth, distance),
			Single.Lerp(start.HalfHeight, end.HalfHeight, distance),
			Single.Lerp(start.HalfDepth, end.HalfDepth, distance)
		);
	}

	public static OriginCuboid CreateNewRandom() {
		return FromHalfDimensions(
			RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax),
			RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax),
			RandomUtils.NextSingle(DefaultRandomMin, DefaultRandomMax)
		);
	}
	public static OriginCuboid CreateNewRandom(OriginCuboid minInclusive, OriginCuboid maxExclusive) {
		return FromHalfDimensions(
			RandomUtils.NextSingle(minInclusive.HalfWidth, maxExclusive.HalfWidth),
			RandomUtils.NextSingle(minInclusive.HalfHeight, maxExclusive.HalfHeight),
			RandomUtils.NextSingle(minInclusive.HalfDepth, maxExclusive.HalfDepth)
		);
	}
}