// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;

namespace Egodystonic.TinyFFR;

partial struct CuboidDescriptor : IIndependentAxisScalable<CuboidDescriptor> {
	#region With Methods
	public CuboidDescriptor WithVolume(float newVolume) {
		var diffCubeRoot = MathF.Cbrt(newVolume / Volume);
		return FromHalfDimensions(HalfWidth * diffCubeRoot, HalfHeight * diffCubeRoot, HalfDepth * diffCubeRoot);
	}
	public CuboidDescriptor WithSurfaceArea(float newSurfaceArea) {
		var diffSquareRoot = MathF.Sqrt(newSurfaceArea / SurfaceArea);
		return FromHalfDimensions(HalfWidth * diffSquareRoot, HalfHeight * diffSquareRoot, HalfDepth * diffSquareRoot);
	}
	#endregion

	#region Scaling
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CuboidDescriptor operator *(CuboidDescriptor descriptor, float scalar) => descriptor.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CuboidDescriptor operator /(CuboidDescriptor descriptor, float scalar) => descriptor.ScaledBy(1f / scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CuboidDescriptor operator *(float scalar, CuboidDescriptor descriptor) => descriptor.ScaledBy(scalar);

	public CuboidDescriptor ScaledBy(float scalar) => FromHalfDimensions(HalfWidth * scalar, HalfHeight * scalar, HalfDepth * scalar);
	public CuboidDescriptor ScaledBy(Vect vect) => FromHalfDimensions(HalfWidth * vect.X, HalfHeight * vect.Y, HalfDepth * vect.Z);
	#endregion

	#region Distance From / Containment (Location & Line-Like)
	public float DistanceFrom(Location location) => MathF.Sqrt(DistanceSquaredFrom(location));
	public float DistanceSquaredFrom(Location location) {
		var xDist = MathF.Max(0f, MathF.Abs(location.X) - HalfWidth);
		var yDist = MathF.Max(0f, MathF.Abs(location.Y) - HalfHeight);
		var zDist = MathF.Max(0f, MathF.Abs(location.Z) - HalfDepth);

		return new Vector3(xDist, yDist, zDist).LengthSquared();
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
	public float SurfaceDistanceSquaredFrom(Location location) { var sqrt = SurfaceDistanceFrom(location); return sqrt * sqrt; }

	public bool Contains(Location location) => MathF.Abs(location.X) <= HalfWidth && MathF.Abs(location.Y) <= HalfHeight && MathF.Abs(location.Z) <= HalfDepth;
	public bool Contains(BoundedRay ray) => Contains(ray.StartPoint) && Contains(ray.EndPoint);

	float DistanceFromLineLike<TLine>(TLine line) where TLine : ILineLike {
		if (Contains(line.StartPoint)) return 0f;
		else return SurfaceDistanceFromLineLike(line);
	}
	float DistanceSquaredFromLineLike<TLine>(TLine line) where TLine : ILineLike {
		if (Contains(line.StartPoint)) return 0f;
		else return SurfaceDistanceSquaredFromLineLike(line);
	}
	float SurfaceDistanceFromLineLike<TLine>(TLine line) where TLine : ILineLike => SurfaceDistanceFrom(ClosestPointToSurfaceOnLineLike(line));
	float SurfaceDistanceSquaredFromLineLike<TLine>(TLine line) where TLine : ILineLike => SurfaceDistanceSquaredFrom(ClosestPointToSurfaceOnLineLike(line));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Line line) => DistanceFromLineLike(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Line line) => DistanceSquaredFromLineLike(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Ray ray) => DistanceFromLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(Ray ray) => DistanceSquaredFromLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(BoundedRay ray) => DistanceFromLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceSquaredFrom(BoundedRay ray) => DistanceSquaredFromLineLike(ray);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SurfaceDistanceFrom(Line line) => SurfaceDistanceFromLineLike(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SurfaceDistanceSquaredFrom(Line line) => SurfaceDistanceSquaredFromLineLike(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SurfaceDistanceFrom(Ray ray) => SurfaceDistanceFromLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SurfaceDistanceSquaredFrom(Ray ray) => SurfaceDistanceSquaredFromLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SurfaceDistanceFrom(BoundedRay ray) => SurfaceDistanceFromLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SurfaceDistanceSquaredFrom(BoundedRay ray) => SurfaceDistanceSquaredFromLineLike(ray);
	#endregion

	#region Closest Point (Location & Line-Like)
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

	Location PointClosestToLineLike<TLine>(TLine line) where TLine : ILineLike => PointClosestTo(ClosestPointOnLineLike(line));
	Location ClosestPointOnLineLike<TLine>(TLine line) where TLine : ILineLike {
		if (Contains(line.StartPoint)) return line.StartPoint;
		else return ClosestPointToSurfaceOnLineLike(line);
	}
	Location SurfacePointClosestToLineLike<TLine>(TLine line) where TLine : ILineLike => SurfacePointClosestTo(ClosestPointToSurfaceOnLineLike(line));
	Location ClosestPointToSurfaceOnLineLike<TLine>(TLine line) where TLine : ILineLike {
		return line switch {
			Line l => ClosestPointToSurfaceOn(l),
			Ray r => ClosestPointToSurfaceOn(r),
			BoundedRay b => ClosestPointToSurfaceOn(b),
			_ => line.IsUnboundedInBothDirections ? ClosestPointToSurfaceOn(line.CoerceToLine()) : (line.IsFiniteLength ? ClosestPointToSurfaceOn(line.CoerceToBoundedRay(line.Length.Value)) : ClosestPointToSurfaceOn(line.CoerceToRay()))
		};
	}
	Location GetClosestPointToSurfaceOnNonIntersectingLine<TLine>(TLine line) where TLine : ILineLike {
		var answerDistance = Single.PositiveInfinity;
		var answer = Location.Origin;
		foreach (var edge in Edges) {
			var closestPointToEdge = line.PointClosestTo(edge);
			var distanceToEdge = edge.DistanceFrom(closestPointToEdge);
			if (distanceToEdge < answerDistance) {
				answerDistance = distanceToEdge;
				answer = closestPointToEdge;
			}
		}
		return answer;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Line line) => ClosestPointOnLineLike(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Ray ray) => ClosestPointOnLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(BoundedRay ray) => ClosestPointOnLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo(Line line) => PointClosestToLineLike(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo(Ray ray) => PointClosestToLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo(BoundedRay ray) => PointClosestToLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location SurfacePointClosestTo(Line line) => SurfacePointClosestToLineLike(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location SurfacePointClosestTo(Ray ray) => SurfacePointClosestToLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location SurfacePointClosestTo(BoundedRay ray) => SurfacePointClosestToLineLike(ray);

	public Location ClosestPointToSurfaceOn(Line line) {
		var intersections = GetUnboundedLineIntersectionDistances(new Ray(line.PointOnLine, line.Direction));
		if (intersections != null) return line.LocationAtDistance(intersections.Value.First);
		else return GetClosestPointToSurfaceOnNonIntersectingLine(line);
	}
	public Location ClosestPointToSurfaceOn(Ray ray) {
		var intersections = GetUnboundedLineIntersectionDistances(ray);
		return (intersections?.First >= 0f, intersections?.Second >= 0f) switch {
			(true, true) => ray.UnboundedLocationAtDistance(intersections!.Value.First < intersections.Value.Second ? intersections.Value.First : intersections.Value.Second),
			(true, false) => ray.UnboundedLocationAtDistance(intersections!.Value.First),
			(false, true) => ray.UnboundedLocationAtDistance(intersections!.Value.Second),
			_ => GetClosestPointToSurfaceOnNonIntersectingLine(ray)
		};
	}
	public Location ClosestPointToSurfaceOn(BoundedRay ray) {
		var intersections = GetUnboundedLineIntersectionDistances(new Ray(ray.StartPoint, ray.Direction));
		var lineLength = ray.Length;
		return (intersections?.First >= 0f && intersections.Value.First <= lineLength, intersections?.Second >= 0f && intersections.Value.Second <= lineLength) switch {
			(true, true) => ray.UnboundedLocationAtDistance(intersections!.Value.First < intersections.Value.Second ? intersections.Value.First : intersections.Value.Second),
			(true, false) => ray.UnboundedLocationAtDistance(intersections!.Value.First),
			(false, true) => ray.UnboundedLocationAtDistance(intersections!.Value.Second),
			_ => GetClosestPointToSurfaceOnNonIntersectingLine(ray)
		};
	}
	#endregion

	#region Intersection (Line-Like)
	// Returns an Infinity when line is parallel to plane -- but still with correct sign depending on which side of the face the start point is
	float SignedLineDistanceToPositiveSurfacePlane(Location lineStartPoint, Direction lineDirection, Axis axis) {
		return (GetHalfExtent(axis) - lineStartPoint[axis]) / lineDirection[axis];
	}
	// Returns an Infinity when line is parallel to plane -- but still with correct sign depending on which side of the face the start point is
	float SignedLineDistanceToNegativeSurfacePlane(Location lineStartPoint, Direction lineDirection, Axis axis) {
		return (-GetHalfExtent(axis) - lineStartPoint[axis]) / lineDirection[axis];
	}
	(float First, float Second)? GetUnboundedLineIntersectionDistances<TLine>(TLine line) where TLine : ILineLike {
		var x1 = SignedLineDistanceToPositiveSurfacePlane(line.StartPoint, line.Direction, Axis.X);
		var x2 = SignedLineDistanceToNegativeSurfacePlane(line.StartPoint, line.Direction, Axis.X);
		var minX = MathF.Min(x1, x2);
		var maxX = MathF.Max(x1, x2);

		var y1 = SignedLineDistanceToPositiveSurfacePlane(line.StartPoint, line.Direction, Axis.Y);
		var y2 = SignedLineDistanceToNegativeSurfacePlane(line.StartPoint, line.Direction, Axis.Y);
		var minY = MathF.Min(y1, y2);
		var maxY = MathF.Max(y1, y2);

		var z1 = SignedLineDistanceToPositiveSurfacePlane(line.StartPoint, line.Direction, Axis.Z);
		var z2 = SignedLineDistanceToNegativeSurfacePlane(line.StartPoint, line.Direction, Axis.Z);
		var minZ = MathF.Min(z1, z2);
		var maxZ = MathF.Max(z1, z2);

		var startDist = MathF.Max(MathF.Max(minX, minY), minZ);
		var endDist = MathF.Min(MathF.Min(maxX, maxY), maxZ);
		if (endDist < startDist) return null;
		else return (startDist, endDist);
	}
	(float First, float Second) FastGetUnboundedLineIntersectionDistances<TLine>(TLine line) where TLine : ILineLike {
		var x1 = SignedLineDistanceToPositiveSurfacePlane(line.StartPoint, line.Direction, Axis.X);
		var x2 = SignedLineDistanceToNegativeSurfacePlane(line.StartPoint, line.Direction, Axis.X);
		var minX = MathF.Min(x1, x2);
		var maxX = MathF.Max(x1, x2);

		var y1 = SignedLineDistanceToPositiveSurfacePlane(line.StartPoint, line.Direction, Axis.Y);
		var y2 = SignedLineDistanceToNegativeSurfacePlane(line.StartPoint, line.Direction, Axis.Y);
		var minY = MathF.Min(y1, y2);
		var maxY = MathF.Max(y1, y2);

		var z1 = SignedLineDistanceToPositiveSurfacePlane(line.StartPoint, line.Direction, Axis.Z);
		var z2 = SignedLineDistanceToNegativeSurfacePlane(line.StartPoint, line.Direction, Axis.Z);
		var minZ = MathF.Min(z1, z2);
		var maxZ = MathF.Max(z1, z2);

		var startDist = MathF.Max(MathF.Max(minX, minY), minZ);
		var endDist = MathF.Min(MathF.Min(maxX, maxY), maxZ);

		return (startDist, endDist);
	}
	bool IsIntersectedByLineLike<TLine>(TLine line) where TLine : ILineLike {
		var distanceTuple = GetUnboundedLineIntersectionDistances(line);
		if (distanceTuple == null) return false;
		return line.DistanceIsWithinLineBounds(distanceTuple.Value.First) || line.DistanceIsWithinLineBounds(distanceTuple.Value.Second);
	}
	ConvexShapeLineIntersection? IntersectionWithLineLike<TLine>(TLine line) where TLine : ILineLike {
		var unboundedDistances = GetUnboundedLineIntersectionDistances(line);
		if (unboundedDistances == null) return null;
		return ConvexShapeLineIntersection.FromTwoPotentiallyNullArgs(line.LocationAtDistanceOrNull(unboundedDistances.Value.First), line.LocationAtDistanceOrNull(unboundedDistances.Value.Second));
	}
	ConvexShapeLineIntersection FastIntersectionWithLineLike<TLine>(TLine line) where TLine : ILineLike {
		var unboundedDistances = FastGetUnboundedLineIntersectionDistances(line);
		return ConvexShapeLineIntersection.FromTwoPotentiallyNullArgs(line.LocationAtDistanceOrNull(unboundedDistances.First), line.LocationAtDistanceOrNull(unboundedDistances.Second))!.Value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Line line) => IsIntersectedByLineLike(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(Ray ray) => IsIntersectedByLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(BoundedRay ray) => IsIntersectedByLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection? IntersectionWith(Line line) => IntersectionWithLineLike(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection? IntersectionWith(Ray ray) => IntersectionWithLineLike(ray); // TODO xmldoc that the first intersection is always the one nearest the start point
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection? IntersectionWith(BoundedRay ray) => IntersectionWithLineLike(ray); // TODO xmldoc that the first intersection is always the one nearest the start point
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection FastIntersectionWith(Line line) => FastIntersectionWithLineLike(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection FastIntersectionWith(Ray ray) => FastIntersectionWithLineLike(ray); // TODO xmldoc that the first intersection is always the one nearest the start point
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ConvexShapeLineIntersection FastIntersectionWith(BoundedRay ray) => FastIntersectionWithLineLike(ray); // TODO xmldoc that the first intersection is always the one nearest the start point
	#endregion

	#region Incident Angle Measurement / Reflection (Line-Like)
	(float HitDistance, Location HitPoint, Plane Side)? GetHitPointAndSidePlaneOfLineLike<TLine>(TLine line) where TLine : ILineLike {
		var intersectionDistances = GetUnboundedLineIntersectionDistances(line);
		if (intersectionDistances == null) return null;

		var firstHitDistance = intersectionDistances.Value.First;
		var hitLoc = line.LocationAtDistanceOrNull(firstHitDistance);
		if (hitLoc == null) {
			firstHitDistance = intersectionDistances.Value.Second;
			hitLoc = line.LocationAtDistanceOrNull(firstHitDistance);
			if (hitLoc == null) return null;
		}

		var xDiff = HalfWidth - MathF.Abs(hitLoc.Value.X);
		var yDiff = HalfHeight - MathF.Abs(hitLoc.Value.Y);
		var zDiff = HalfDepth - MathF.Abs(hitLoc.Value.Z);

		if (xDiff < yDiff) {
			if (xDiff < zDiff) {
				return (
					firstHitDistance,
					hitLoc.Value,
					SideAt((CardinalOrientation3D) OrientationUtils.CreateXAxisOrientationFromValueSign(MathF.Sign(hitLoc.Value.X)))
				);
			}
			else {
				return (
					firstHitDistance,
					hitLoc.Value,
					SideAt((CardinalOrientation3D) OrientationUtils.CreateZAxisOrientationFromValueSign(MathF.Sign(hitLoc.Value.Z)))
				);
			}
		}
		else if (yDiff < zDiff) {
			return (
				firstHitDistance,
				hitLoc.Value,
				SideAt((CardinalOrientation3D) OrientationUtils.CreateYAxisOrientationFromValueSign(MathF.Sign(hitLoc.Value.Y)))
			);
		}
		else {
			return (
				firstHitDistance,
				hitLoc.Value,
				SideAt((CardinalOrientation3D) OrientationUtils.CreateZAxisOrientationFromValueSign(MathF.Sign(hitLoc.Value.Z)))
			);
		}
	}
	(float HitDistance, Location HitPoint, Plane Side) FastGetHitPointAndSidePlaneOfLineLike<TLine>(TLine line) where TLine : ILineLike {
		var intersectionDistances = FastGetUnboundedLineIntersectionDistances(line);

		var firstHitDistance = intersectionDistances.First;
		Location hitLoc;
		var prospectiveHitLoc = line.LocationAtDistanceOrNull(firstHitDistance);
		if (prospectiveHitLoc != null) hitLoc = prospectiveHitLoc.Value;
		else {
			firstHitDistance = intersectionDistances.Second;
			hitLoc = line.UnboundedLocationAtDistance(firstHitDistance);
		}

		var xDiff = HalfWidth - MathF.Abs(hitLoc.X);
		var yDiff = HalfHeight - MathF.Abs(hitLoc.Y);
		var zDiff = HalfDepth - MathF.Abs(hitLoc.Z);

		if (xDiff < yDiff) {
			if (xDiff < zDiff) {
				return (
					firstHitDistance,
					hitLoc,
					SideAt((CardinalOrientation3D) OrientationUtils.CreateXAxisOrientationFromValueSign(MathF.Sign(hitLoc.X)))
				);
			}
			else {
				return (
					firstHitDistance,
					hitLoc,
					SideAt((CardinalOrientation3D) OrientationUtils.CreateZAxisOrientationFromValueSign(MathF.Sign(hitLoc.Z)))
				);
			}
		}
		else if (yDiff < zDiff) {
			return (
				firstHitDistance,
				hitLoc,
				SideAt((CardinalOrientation3D) OrientationUtils.CreateYAxisOrientationFromValueSign(MathF.Sign(hitLoc.Y)))
			);
		}
		else {
			return (
				firstHitDistance,
				hitLoc,
				SideAt((CardinalOrientation3D) OrientationUtils.CreateZAxisOrientationFromValueSign(MathF.Sign(hitLoc.Z)))
			);
		}
	}

	Angle? GetIncidentAngleOfLineLike<TLine>(TLine line) where TLine : ILineLike {
		return GetHitPointAndSidePlaneOfLineLike(line)?.Side.IncidentAngleWith(line.Direction);
	}
	Angle FastGetIncidentAngleOfLineLike<TLine>(TLine line) where TLine : ILineLike {
		return FastGetHitPointAndSidePlaneOfLineLike(line).Side.FastIncidentAngleWith(line.Direction);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle? IncidentAngleWith(Ray ray) => GetIncidentAngleOfLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle? IncidentAngleWith(BoundedRay ray) => GetIncidentAngleOfLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle FastIncidentAngleWith(Ray ray) => FastGetIncidentAngleOfLineLike(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle FastIncidentAngleWith(BoundedRay ray) => FastGetIncidentAngleOfLineLike(ray);

#pragma warning disable CS8629 // "Nullable value type may be null" -- seems like a compiler bug? It thinks 'tuple' could be null
	public Ray? ReflectionOf(Ray ray) {
		var tuple = GetHitPointAndSidePlaneOfLineLike(ray);
		var reflection = tuple?.Side.ReflectionOf(ray.Direction);
		if (reflection == null) return null;

		return new Ray(tuple.Value.HitPoint, reflection.Value);
	}
	public BoundedRay? ReflectionOf(BoundedRay ray) {
		var tuple = GetHitPointAndSidePlaneOfLineLike(ray);
		var reflection = tuple?.Side.ReflectionOf(ray.Direction);
		if (reflection == null) return null;

		return BoundedRay.FromStartPointAndVect(tuple.Value.HitPoint, reflection.Value * (ray.Length - tuple.Value.HitDistance));
	}
	public Ray FastReflectionOf(Ray ray) {
		var tuple = GetHitPointAndSidePlaneOfLineLike(ray);
		return new Ray(tuple.Value.HitPoint, tuple.Value.Side.FastReflectionOf(ray.Direction));
	}
	public BoundedRay FastReflectionOf(BoundedRay ray) {
		var tuple = GetHitPointAndSidePlaneOfLineLike(ray);
		return BoundedRay.FromStartPointAndVect(tuple.Value.HitPoint, tuple.Value.Side.FastReflectionOf(ray.Direction) * (ray.Length - tuple.Value.HitDistance));
	}
#pragma warning restore CS8629 // "Nullable value type may be null"
	#endregion

	#region Distance From / Closest Point / Intersection (Plane)
	bool QuickPlaneCuboidIntersectionTest(Plane plane) { // https://gdbooks.gitbooks.io/3dcollisions/content/Chapter2/static_aabb_plane.html
		var halfDiagonalProjection = Width * MathF.Abs(plane.Normal.X) + Height * MathF.Abs(plane.Normal.Y) + Depth * MathF.Abs(plane.Normal.Z);
		return plane.DistanceFromOrigin() <= halfDiagonalProjection;
	}

	const int MaxPlaneIntersectionPoints = 6;
	int GetPlaneIntersectionPoints(Plane plane, Span<Location> pointSpan) {
		var intersectionCount = 0;
		foreach (var edge in Edges) {
			var edgeIntersection = edge.IntersectionWith(plane);
			if (edgeIntersection.HasValue) pointSpan[intersectionCount++] = edgeIntersection.Value;
		}
		return intersectionCount;
	}
	Location? GetAnyPlaneIntersectionPoint(Plane plane) {
		foreach (var edge in Edges) {
			var edgeIntersection = edge.IntersectionWith(plane);
			if (edgeIntersection.HasValue) return edgeIntersection.Value;
		}
		return null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location PointClosestTo(Plane plane) => SurfacePointClosestTo(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Plane plane) => ClosestPointToSurfaceOn(plane);
	public float SignedDistanceFrom(Plane plane) {
		if (QuickPlaneCuboidIntersectionTest(plane)) return 0f;

		var result = Single.PositiveInfinity;
		foreach (var corner in Corners) {
			var distance = plane.SignedDistanceFrom(corner);
			result = MathF.MinMagnitude(distance, result);
		}
		return result;
	}
	public float DistanceFrom(Plane plane) {
		if (QuickPlaneCuboidIntersectionTest(plane)) return 0f;

		var result = Single.PositiveInfinity;
		foreach (var corner in Corners) {
			var distance = plane.DistanceFrom(corner);
			result = MathF.Min(distance, result);
		}
		return result;
	}
	float IDistanceMeasurable<Plane>.DistanceSquaredFrom(Plane plane) { var sqrt = DistanceFrom(plane); return sqrt * sqrt; }
	public PlaneObjectRelationship RelationshipTo(Plane plane) {
		if (QuickPlaneCuboidIntersectionTest(plane)) return PlaneObjectRelationship.PlaneIntersectsObject;
		return plane.FacesTowardsOrigin(planeThickness: 0f) ? PlaneObjectRelationship.PlaneFacesTowardsObject : PlaneObjectRelationship.PlaneFacesAwayFromObject;
	}

	public Location SurfacePointClosestTo(Plane plane) {
		if (QuickPlaneCuboidIntersectionTest(plane)) return GetAnyPlaneIntersectionPoint(plane)!.Value;
		var resultDistance = Single.PositiveInfinity;
		var result = Location.Origin;
		foreach (var corner in Corners) {
			var distance = plane.DistanceFrom(corner);
			if (distance < resultDistance) {
				resultDistance = distance;
				result = corner;
			}
		}
		return result;
	}
	public Location ClosestPointToSurfaceOn(Plane plane) => plane.PointClosestTo(SurfacePointClosestTo(plane));

	float IConvexShape.SurfaceDistanceFrom(Plane plane) => DistanceFrom(plane);
	float IConvexShape.SurfaceDistanceSquaredFrom(Plane plane) { var sqrt = DistanceFrom(plane); return sqrt * sqrt; }
	#endregion

	#region Clamping and Interpolation
	public static CuboidDescriptor Interpolate(CuboidDescriptor start, CuboidDescriptor end, float distance) {
		return FromHalfDimensions(
			Single.Lerp(start.HalfWidth, end.HalfWidth, distance),
			Single.Lerp(start.HalfHeight, end.HalfHeight, distance),
			Single.Lerp(start.HalfDepth, end.HalfDepth, distance)
		);
	}
	#endregion
}