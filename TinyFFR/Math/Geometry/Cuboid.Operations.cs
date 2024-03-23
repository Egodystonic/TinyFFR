// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;

namespace Egodystonic.TinyFFR;

public readonly partial struct Cuboid
	: IMultiplyOperators<Cuboid, float, Cuboid> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Cuboid operator *(Cuboid cuboid, float scalar) => cuboid.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Cuboid operator *(float scalar, Cuboid cuboid) => cuboid.ScaledBy(scalar);

	public Cuboid ScaledBy(float scalar) => FromHalfDimensions(_halfWidth * scalar, _halfHeight * scalar, _halfDepth * scalar);

	// TODO these GetX methods need a naming pass and this file vs Cuboid.cs? etc

	public Location GetCorner(DiagonalOrientation3D corner) { // TODO add properties that enumerate corners, surfaces, etc and use them instead of foreach loops internally
		if (corner == DiagonalOrientation3D.None) throw new ArgumentOutOfRangeException(nameof(corner), corner, $"Can not be '{nameof(DiagonalOrientation3D.None)}'.");

		return new(
			corner.GetAxisSign(Axis.X) * HalfWidth,
			corner.GetAxisSign(Axis.Y) * HalfHeight,
			corner.GetAxisSign(Axis.Z) * HalfDepth
		);
	}
	
	public Plane GetSurfacePlane(CardinalOrientation3D side) { // TODO xmldoc that the planes' normals point away from the cuboid centre, e.g. side.ToDirection()
		if (side == CardinalOrientation3D.None) throw new ArgumentOutOfRangeException(nameof(side), side, $"Can not be '{nameof(CardinalOrientation3D.None)}'.");

		return Plane.FromNormalAndTranslationFromOrigin(side.ToDirection(), GetHalfDimension(side.GetAxis()));
	}

	public BoundedLine GetEdge(IntercardinalOrientation3D edge) {
		if (edge == IntercardinalOrientation3D.None) throw new ArgumentOutOfRangeException(nameof(edge), edge, $"Can not be '{nameof(IntercardinalOrientation3D.None)}'.");

		var unspecifiedAxis = edge.GetUnspecifiedAxis();
		return new(
			GetCorner((DiagonalOrientation3D) edge.AsGeneralOrientation().WithAxisSign(unspecifiedAxis, -1)),
			GetCorner((DiagonalOrientation3D) edge.AsGeneralOrientation().WithAxisSign(unspecifiedAxis, 1))
		);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Cuboid WithWidth(float newWidth) => this with { Width = newWidth };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Cuboid WithHeight(float newHeight) => this with { Height = newHeight };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Cuboid WithDepth(float newDepth) => this with { Depth = newDepth };

	public float DistanceFrom(Location location) {
		var xDist = MathF.Max(0f, MathF.Abs(location.X) - HalfWidth);
		var yDist = MathF.Max(0f, MathF.Abs(location.Y) - HalfHeight);
		var zDist = MathF.Max(0f, MathF.Abs(location.Z) - HalfDepth);

		return new Vector3(xDist, yDist, zDist).Length();
	}
	public float SurfaceDistanceFrom(Location location) {
		var xDist = MathF.Abs(MathF.Abs(location.X) - HalfWidth);
		var yDist = MathF.Abs(MathF.Abs(location.Y) - HalfHeight);
		var zDist = MathF.Abs(MathF.Abs(location.Z) - HalfDepth);

		return new Vector3(xDist, yDist, zDist).Length();
	}

	public bool Contains(Location location) => MathF.Abs(location.X) <= HalfWidth && MathF.Abs(location.Y) <= HalfHeight && MathF.Abs(location.Z) <= HalfDepth;

	public Location ClosestPointTo(Location location) {
		return new(
			Single.Clamp(location.X, -HalfWidth, HalfWidth),
			Single.Clamp(location.Y, -HalfHeight, HalfHeight),
			Single.Clamp(location.Z, -HalfDepth, HalfDepth)
		);
	}
	public Location ClosestPointOnSurfaceTo(Location location) {
		static Axis GetMinAxis(float x, float y, float z) {
			if (MathF.Abs(x) < MathF.Abs(y)) return MathF.Abs(x) < MathF.Abs(z) ? Axis.X : Axis.Z;
			else return MathF.Abs(y) < MathF.Abs(z) ? Axis.Y : Axis.Z;
		}

		var xSign = location.X < 0f ? -1f : 1f;
		var ySign = location.Y < 0f ? -1f : 1f;
		var zSign = location.Z < 0f ? -1f : 1f;
		var xDelta = xSign * (HalfWidth - MathF.Abs(location.X));
		var yDelta = ySign * (HalfHeight - MathF.Abs(location.Y));
		var zDelta = zSign * (HalfDepth - MathF.Abs(location.Z));

		return GetMinAxis(xDelta, yDelta, zDelta) switch {
			Axis.X => location with { X = location.X + xDelta },
			Axis.Y => location with { Y = location.Y + yDelta },
			_ => location with { Z = location.Z + zDelta },
		};
	}

	[InlineArray(3)] // TODO delete these two methods if we don't need them
	struct PlaneDistanceBuffer { float _; }
	// near plane distances are [x, y, z] axis; negative if point inside cuboid on that axis, zero if on edge, positive if point outside.
	// far plane distances are [x, y, z] axis; always positive (or zero for cuboid of extent 0 on that axis, god knows if extent is negative)
	void GetPlanePointDistances(Location startPoint, ref PlaneDistanceBuffer nearPlaneDistances, ref PlaneDistanceBuffer farPlaneDistances) {
		for (var i = 0; i < OrientationUtils.AllAxes.Length; ++i) {
			var axis = OrientationUtils.AllAxes[i];

			var nearPlaneSign = MathF.Sign(startPoint[axis]) & 0b1; // & 0b1 ensures it's either -1 or 1, never 0
			nearPlaneDistances[i] = GetSurfacePlane(axis.ToCardinal(nearPlaneSign)).SignedDistanceFrom(startPoint);
			// Flip the further plane so that both planes face the startPoint (unless it's inside the cuboid on this axis, in which case the signed distances will have differing signs)
			farPlaneDistances[i] = GetSurfacePlane(axis.ToCardinal(-nearPlaneSign)).Flipped.SignedDistanceFrom(startPoint);
		}
	}

	[InlineArray(3)]
	struct PlaneIntersectionBuffer { Location? _; }
	void GetPlaneLineIntersections<TLine>(TLine line, ref PlaneIntersectionBuffer frontFacingIntersections, ref PlaneIntersectionBuffer backFacingIntersections) where TLine : ILine {
		for (var i = 0; i < OrientationUtils.AllAxes.Length; ++i) {
			var axis = OrientationUtils.AllAxes[i];

			var farPlaneSign = MathF.Sign(line.Direction[axis]);
			if (farPlaneSign == 0) continue; // No intersections as we're parallel
			frontFacingIntersections[i] = GetSurfacePlane(axis.ToCardinal(-farPlaneSign)).IntersectionWith(line);
			backFacingIntersections[i] = GetSurfacePlane(axis.ToCardinal(farPlaneSign)).IntersectionWith(line);
		}
	}

	public Location ClosestPointTo<TLine>(TLine line) where TLine : ILine => ClosestPointTo(ClosestPointOn(line));
	public Location ClosestPointOn<TLine>(TLine line) where TLine : ILine {
		var unboundedIntersectionDistances = GetUnboundedLineIntersectionDistances(line.CoerceToRay());
		if (unboundedIntersectionDistances != null) {
			var potentialAnswerA = line.BoundedLocationAtDistance(unboundedIntersectionDistances.Value.Item1);
			var potentialAnswerB = line.BoundedLocationAtDistance(unboundedIntersectionDistances.Value.Item2);
			var distA = line.DistanceFrom(potentialAnswerA);
			var distB = line.DistanceFrom(potentialAnswerB);
			return distA < distB ? potentialAnswerA : potentialAnswerB;
		}

		if (Contains(line.StartPoint)) return line.StartPoint;

		var answerDistance = Single.PositiveInfinity;
		var answer = Location.Origin;
		foreach (var edgeOrientation in OrientationUtils.AllIntercardinals) {
			var edge = GetEdge(edgeOrientation);
			var closestPointToEdge = line.ClosestPointTo(edge);
			var distanceToEdge = edge.DistanceFrom(closestPointToEdge);
			if (distanceToEdge < answerDistance) {
				answerDistance = distanceToEdge;
				answer = closestPointToEdge;
			}
		}
		return answer;
	}
	public float DistanceFrom<TLine>(TLine line) where TLine : ILine {
		// TODO reimplement algo above but simply return distance, to make sure we always return 0 for actual intersections
	}

	public Location ClosestPointOnSurfaceTo<TLine>(TLine line) where TLine : ILine => ClosestPointOnSurfaceTo(ClosestPointToSurfaceOn(line));
	public Location ClosestPointToSurfaceOn<TLine>(TLine line) where TLine : ILine {
		var unboundedIntersectionDistances = GetUnboundedLineIntersectionDistances(line.CoerceToRay());
		if (unboundedIntersectionDistances != null) {
			var potentialAnswerA = line.BoundedLocationAtDistance(unboundedIntersectionDistances.Value.Item1);
			var potentialAnswerB = line.BoundedLocationAtDistance(unboundedIntersectionDistances.Value.Item2);
			var distA = line.DistanceFrom(potentialAnswerA);
			var distB = line.DistanceFrom(potentialAnswerB);
			return distA < distB ? potentialAnswerA : potentialAnswerB;
		}

		var answerDistance = Single.PositiveInfinity;
		var answer = Location.Origin;
		foreach (var edgeOrientation in OrientationUtils.AllIntercardinals) {
			var edge = GetEdge(edgeOrientation);
			var closestPointToEdge = line.ClosestPointTo(edge);
			var distanceToEdge = edge.DistanceFrom(closestPointToEdge);
			if (distanceToEdge < answerDistance) {
				answerDistance = distanceToEdge;
				answer = closestPointToEdge;
			}
		}
		return answer;
	}
	public float SurfaceDistanceFrom<TLine>(TLine line) where TLine : ILine {
		// TODO reimplement algo above but simply return distance, to make sure we always return 0 for actual intersections
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
		var maxY = MathF.Min(y1, y2);

		var z1 = SignedLineDistanceToPositiveSurfacePlane(ray.StartPoint, ray.Direction, Axis.Z);
		var z2 = SignedLineDistanceToNegativeSurfacePlane(ray.StartPoint, ray.Direction, Axis.Z);
		var minZ = MathF.Min(z1, z2);
		var maxZ = MathF.Min(z1, z2);

		var startDist = MathF.Max(MathF.Max(minX, minY), minZ);
		var endDist = MathF.Min(MathF.Min(maxX, maxY), maxZ);
		if (endDist > startDist) return null;
		else return (startDist, endDist);
	}

	// Returns an Infinity when line is parallel to plane -- but still with correct sign depending on which side of the face the start point is
	float SignedLineDistanceToPositiveSurfacePlane(Location lineStartPoint, Direction lineDirection, Axis axis) {
		return (GetHalfDimension(axis) - lineStartPoint[axis]) / lineDirection[axis];
	}
	// Returns an Infinity when line is parallel to plane -- but still with correct sign depending on which side of the face the start point is
	float SignedLineDistanceToNegativeSurfacePlane(Location lineStartPoint, Direction lineDirection, Axis axis) {
		return (-GetHalfDimension(axis) - lineStartPoint[axis]) / lineDirection[axis];
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
	public Location ClosestPointTo(Plane plane) => ClosestPointOnSurfaceTo(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Plane plane) => ClosestPointToSurfaceOn(plane);
	public float SignedDistanceFrom(Plane plane) {
		if (QuickPlaneCuboidIntersectionTest(plane)) return 0f;

		var result = Single.PositiveInfinity;
		foreach (var diagonal in OrientationUtils.AllDiagonals) {
			var corner = GetCorner(diagonal);
			var distance = plane.SignedDistanceFrom(corner);
			if (MathF.Abs(distance) < result) result = distance;
		}
		return result;
	}
	public float DistanceFrom(Plane plane) {
		if (QuickPlaneCuboidIntersectionTest(plane)) return 0f;

		var result = Single.PositiveInfinity;
		foreach (var diagonal in OrientationUtils.AllDiagonals) {
			var corner = GetCorner(diagonal);
			var distance = plane.DistanceFrom(corner);
			if (distance < result) result = distance;
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
			var corner = GetCorner(diagonal);
			var distance = plane.DistanceFrom(corner);
			if (distance < resultDistance) {
				resultDistance = distance;
				result = corner;
			}
		}
		return result;
	}
	public Location ClosestPointToSurfaceOn(Plane plane) => plane.ClosestPointTo(ClosestPointOnSurfaceTo(plane));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SurfaceDistanceFrom(Plane plane) => DistanceFrom(plane);
}