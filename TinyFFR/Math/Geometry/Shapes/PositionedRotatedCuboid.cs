// Created on 2026-04-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

public readonly struct PositionedRotatedCuboid : ITranslatedRotatedConvexShape<PositionedRotatedCuboid, Cuboid>, ICuboid<PositionedRotatedCuboid>, 
	IDistanceMeasurable<PositionedSphere>, IDistanceMeasurable<PositionedCuboid>, IDistanceMeasurable<PositionedRotatedCuboid>,
	IIntersectable<PositionedSphere>, IIntersectable<PositionedCuboid>, IIntersectable<PositionedRotatedCuboid> {
	public static readonly PositionedRotatedCuboid UnitCubeAtOriginUnrotated = new(Cuboid.UnitCube, Location.Origin, Rotation.None);
	readonly TranslatedRotatedConvexShape<Cuboid> _impl;

	// TODO xmldoc this is the center point
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.Translation.AsLocation();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { Translation = value.AsVect() };
	}
	
	public Rotation Rotation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.Rotation;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { Rotation = value };
	}

	public float HalfWidth {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.HalfWidth;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { BaseShape = _impl.BaseShape with { HalfWidth = value } };
	}
	public float HalfHeight {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.HalfHeight;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { BaseShape = _impl.BaseShape with { HalfHeight = value } };
	}
	public float HalfDepth {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.HalfDepth;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { BaseShape = _impl.BaseShape with { HalfDepth = value } };
	}

	public float Width {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.Width;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { BaseShape = _impl.BaseShape with { Width = value } };
	}
	public float Height {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.Height;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { BaseShape = _impl.BaseShape with { Height = value } };
	}
	public float Depth {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.Depth;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { BaseShape = _impl.BaseShape with { Depth = value } };
	}

	public float Volume {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.Volume;
	}
	public float SurfaceArea {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.SurfaceArea;
	}

	public bool IsPhysicallyValid {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.IsPhysicallyValid;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location CentroidAt(CardinalOrientation side) => _impl.TransformToWorldSpace(_impl.BaseShape.CentroidAt(side));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location CornerAt(DiagonalOrientation corner) => _impl.TransformToWorldSpace(_impl.BaseShape.CornerAt(corner));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Plane SideAt(CardinalOrientation side) => _impl.TransformToWorldSpace(_impl.BaseShape.SideAt(side));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedRay EdgeAt(IntercardinalOrientation edge) => _impl.TransformToWorldSpace(_impl.BaseShape.EdgeAt(edge));

	public unsafe IndirectEnumerable<PositionedRotatedCuboid, Location> Corners => new(this, GetIteratorVersion(this), &GetCornerCountForEnumerator, &GetIteratorVersion, &GetCornerForEnumerator);
	static int GetCornerCountForEnumerator(PositionedRotatedCuboid _) => 8;
	static Location GetCornerForEnumerator(PositionedRotatedCuboid @this, int index) => @this.CornerAt(OrientationUtils.AllDiagonals[index]);

	public unsafe IndirectEnumerable<PositionedRotatedCuboid, BoundedRay> Edges => new(this, GetIteratorVersion(this), &GetEdgeCountForEnumerator, &GetIteratorVersion, &GetEdgeForEnumerator);
	static int GetEdgeCountForEnumerator(PositionedRotatedCuboid _) => 12;
	static BoundedRay GetEdgeForEnumerator(PositionedRotatedCuboid @this, int index) => @this.EdgeAt(OrientationUtils.AllIntercardinals[index]);

	public unsafe IndirectEnumerable<PositionedRotatedCuboid, Plane> Sides => new(this, GetIteratorVersion(this), &GetSideCountForEnumerator, &GetIteratorVersion, &GetSideForEnumerator);
	static int GetSideCountForEnumerator(PositionedRotatedCuboid _) => 6;
	static Plane GetSideForEnumerator(PositionedRotatedCuboid @this, int index) => @this.SideAt(OrientationUtils.AllCardinals[index]);

	public unsafe IndirectEnumerable<PositionedRotatedCuboid, Location> Centroids => new(this, GetIteratorVersion(this), &GetCentroidCountForEnumerator, &GetIteratorVersion, &GetCentroidForEnumerator);
	static int GetCentroidCountForEnumerator(PositionedRotatedCuboid _) => 6;
	static Location GetCentroidForEnumerator(PositionedRotatedCuboid @this, int index) => @this.CentroidAt(OrientationUtils.AllCardinals[index]);

	static int GetIteratorVersion(PositionedRotatedCuboid _) => 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float GetExtent(Axis axis) => _impl.BaseShape.GetExtent(axis);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float GetHalfExtent(Axis axis) => _impl.BaseShape.GetHalfExtent(axis);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float GetSideSurfaceArea(CardinalOrientation side) => _impl.BaseShape.GetSideSurfaceArea(side);
	
	public PositionedRotatedCuboid WithVolume(float newVolume) => new(_impl.BaseShape.WithVolume(newVolume), Position, Rotation);
	public PositionedRotatedCuboid WithSurfaceArea(float newSurfaceArea) => new(_impl.BaseShape.WithSurfaceArea(newSurfaceArea), Position, Rotation);
	public PositionedRotatedCuboid WithAllExtentsAdjustedBy(float adjustment) => new(_impl.BaseShape.WithAllExtentsAdjustedBy(adjustment), Position, Rotation);

	Cuboid ITranslatedRotatedShape<PositionedRotatedCuboid, Cuboid>.BaseShape {
		get => _impl.BaseShape;
		init => _impl = _impl with { BaseShape = value };
	}

	Vect ITranslatedShape.Translation {
		get => Position.AsVect();
		init => Position = value.AsLocation();
	}

	public PositionedRotatedCuboid(float width, float height, float depth, Location centerPoint, Rotation rotation) : this(new Cuboid(width, height, depth), centerPoint, rotation) { }
	public PositionedRotatedCuboid(Cuboid baseShape, Location centerPoint, Rotation rotation) : this(new(baseShape, centerPoint.AsVect(), rotation)) { }
	public PositionedRotatedCuboid(TranslatedRotatedConvexShape<Cuboid> impl) {
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedRotatedConvexShape<Cuboid>(PositionedRotatedCuboid operand) => operand._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PositionedRotatedCuboid(TranslatedRotatedConvexShape<Cuboid> operand) => new(operand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedRotatedShape<Cuboid>(PositionedRotatedCuboid operand) => operand._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PositionedRotatedCuboid(TranslatedRotatedShape<Cuboid> operand) => new(operand);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator TranslatedConvexShape<Cuboid>(PositionedRotatedCuboid operand) => (TranslatedConvexShape<Cuboid>) operand._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator TranslatedShape<Cuboid>(PositionedRotatedCuboid operand) => (TranslatedShape<Cuboid>) operand._impl;
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator PositionedCuboid(PositionedRotatedCuboid operand) => new((TranslatedConvexShape<Cuboid>) operand._impl);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator PositionedRotatedCuboid(PositionedCuboid operand) => new(operand.ToStandardCuboid(), operand.Position, Rotation.None);

	public static PositionedRotatedCuboid FromHalfDimensions(float halfWidth, float halfHeight, float halfDepth, Location centerPoint, Rotation rotation) => new(Cuboid.FromHalfDimensions(halfWidth, halfHeight, halfDepth), centerPoint, rotation);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Cuboid ToStandardCuboid() => _impl.BaseShape;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PositionedCuboid ToTranslatedCuboid() => new((TranslatedConvexShape<Cuboid>) _impl);
	
	public PositionedSphere SmallestEnclosingSphere {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(_impl.BaseShape.SmallestEnclosingSphere, Position);
	}
	public PositionedSphere LargestEnclosedSphere {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(_impl.BaseShape.LargestEnclosedSphere, Position);
	}
	
	// https://dev.to/pratyush_mohanty_6b8f2749/the-math-behind-bounding-box-collision-detection-aabb-vs-obbseparate-axis-theorem-1gdn
	// https://gamedev.stackexchange.com/questions/44500/how-many-and-which-axes-to-use-for-3d-obb-collision-with-sat
	static bool DetectIntersectionViaSeparatingAxisTest(Cuboid a, Cuboid b, Vector3 centerDelta, Vector3 bAxisX, Vector3 bAxisY, Vector3 bAxisZ) {
		static bool AxisProjectionsDoNotOverlap(Cuboid a, Cuboid b, Vector3 centerDelta, Vector3 bx, Vector3 by, Vector3 bz, Vector3 axisToTest) {
			const float MinCrossLengthSquared = 1E-9f;
			if (axisToTest.LengthSquared() < MinCrossLengthSquared) return false; // cross products will be zero when axes are parallel
			var aProjectedLen = a.HalfWidth * MathF.Abs(axisToTest.X) + a.HalfHeight * MathF.Abs(axisToTest.Y) + a.HalfDepth * MathF.Abs(axisToTest.Z);
			var bProjectedLen = b.HalfWidth * MathF.Abs(Vector3.Dot(axisToTest, bx)) + b.HalfHeight * MathF.Abs(Vector3.Dot(axisToTest, by)) + b.HalfDepth * MathF.Abs(Vector3.Dot(axisToTest, bz));
			var centerDeltaProjectedLen = MathF.Abs(Vector3.Dot(centerDelta, axisToTest));
			return centerDeltaProjectedLen >= aProjectedLen + bProjectedLen;
		}
		
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, new(1f, 0f, 0f))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, new(0f, 1f, 0f))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, new(0f, 0f, 1f))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, bAxisX)) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, bAxisY)) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, bAxisZ)) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, Vector3.Cross(new(1f, 0f, 0f), bAxisX))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, Vector3.Cross(new(1f, 0f, 0f), bAxisY))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, Vector3.Cross(new(1f, 0f, 0f), bAxisZ))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, Vector3.Cross(new(0f, 1f, 0f), bAxisX))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, Vector3.Cross(new(0f, 1f, 0f), bAxisY))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, Vector3.Cross(new(0f, 1f, 0f), bAxisZ))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, Vector3.Cross(new(0f, 0f, 1f), bAxisX))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, Vector3.Cross(new(0f, 0f, 1f), bAxisY))) return false;
		if (AxisProjectionsDoNotOverlap(a, b, centerDelta, bAxisX, bAxisY, bAxisZ, Vector3.Cross(new(0f, 0f, 1f), bAxisZ))) return false;
		return true;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(PositionedSphere sphere) => sphere.DistanceFrom(this);
	float IDistanceMeasurable<PositionedSphere>.DistanceSquaredFrom(PositionedSphere sphere) { var dist = DistanceFrom(sphere); return dist * dist; }
	public float DistanceFrom(PositionedCuboid cuboid) {
		if (IsIntersectedBy(cuboid)) return 0f;
		var min = Single.PositiveInfinity;
		foreach (var corner in Corners) min = MathF.Min(min, cuboid.DistanceFrom(corner));
		foreach (var corner in cuboid.Corners) min = MathF.Min(min, DistanceFrom(corner));
		foreach (var ea in Edges) {
			foreach (var eb in cuboid.Edges) min = MathF.Min(min, ea.DistanceFrom(eb));
		}
		return min;
	}
	float IDistanceMeasurable<PositionedCuboid>.DistanceSquaredFrom(PositionedCuboid cuboid) { var dist = DistanceFrom(cuboid); return dist * dist; }
	public float DistanceFrom(PositionedRotatedCuboid cuboid) {
		if (IsIntersectedBy(cuboid)) return 0f;
		var min = Single.PositiveInfinity;
		foreach (var corner in Corners) min = MathF.Min(min, cuboid.DistanceFrom(corner));
		foreach (var corner in cuboid.Corners) min = MathF.Min(min, DistanceFrom(corner));
		foreach (var ea in Edges) {
			foreach (var eb in cuboid.Edges) min = MathF.Min(min, ea.DistanceFrom(eb));
		}
		return min;
	}
	float IDistanceMeasurable<PositionedRotatedCuboid>.DistanceSquaredFrom(PositionedRotatedCuboid cuboid) { var dist = DistanceFrom(cuboid); return dist * dist; }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(PositionedSphere sphere) => sphere.IsIntersectedBy(this);
	public bool IsIntersectedBy(PositionedCuboid cuboid) {
		var reverseRot = Rotation.Reversed;
		
		return DetectIntersectionViaSeparatingAxisTest(
			_impl.BaseShape, 
			cuboid.ToStandardCuboid(), 
			((cuboid.Position - Position) * reverseRot).ToVector3(), 
			Direction.Left.RotatedBy(reverseRot).ToVector3(), 
			Direction.Up.RotatedBy(reverseRot).ToVector3(), 
			Direction.Forward.RotatedBy(reverseRot).ToVector3()
		);
	}
	public bool IsIntersectedBy(PositionedRotatedCuboid cuboid) {
		var reverseRot = Rotation.Reversed;
	
		return DetectIntersectionViaSeparatingAxisTest(
			_impl.BaseShape, 
			cuboid.ToStandardCuboid(), 
			((cuboid.Position - Position) * reverseRot).ToVector3(), 
			Direction.Left.RotatedBy(cuboid.Rotation + reverseRot).ToVector3(), 
			Direction.Up.RotatedBy(cuboid.Rotation + reverseRot).ToVector3(), 
			Direction.Forward.RotatedBy(cuboid.Rotation + reverseRot).ToVector3()
		);
	}

	#region Deferring Members
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => ToString(null, null);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public string ToString(string? format, IFormatProvider? formatProvider) => _impl.ToString(format, formatProvider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => _impl.TryFormat(destination, out charsWritten, format, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedRotatedCuboid MovedBy(Vect v) => _impl.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedRotatedCuboid ScaledBy(float scalar) => _impl.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedRotatedCuboid Clamp(PositionedRotatedCuboid min, PositionedRotatedCuboid max) => _impl.Clamp(min, max);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object? obj) => obj is PositionedRotatedCuboid other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => _impl.GetHashCode();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(PositionedRotatedCuboid other) => _impl.Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(PositionedRotatedCuboid other, float tolerance) => _impl.Equals(other, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location PointClosestTo(Location location) => _impl.PointClosestTo(location);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceFrom(Location location) => _impl.DistanceFrom(location);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceSquaredFrom(Location location) => _impl.DistanceSquaredFrom(location);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Contains(Location location) => _impl.Contains(location);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Ray? ReflectionOf(Ray ray) => _impl.ReflectionOf(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Ray FastReflectionOf(Ray ray) => _impl.FastReflectionOf(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Angle? IncidentAngleWith(Ray ray) => _impl.IncidentAngleWith(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Angle FastIncidentAngleWith(Ray ray) => _impl.FastIncidentAngleWith(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public BoundedRay? ReflectionOf(BoundedRay ray) => _impl.ReflectionOf(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public BoundedRay FastReflectionOf(BoundedRay ray) => _impl.FastReflectionOf(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Angle? IncidentAngleWith(BoundedRay ray) => _impl.IncidentAngleWith(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Angle FastIncidentAngleWith(BoundedRay ray) => _impl.FastIncidentAngleWith(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointOn(Line line) => _impl.ClosestPointOn(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointOn(Ray ray) => _impl.ClosestPointOn(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointOn(BoundedRay ray) => _impl.ClosestPointOn(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location PointClosestTo(Line line) => _impl.PointClosestTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location PointClosestTo(Ray ray) => _impl.PointClosestTo(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location PointClosestTo(BoundedRay ray) => _impl.PointClosestTo(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceFrom(Line line) => _impl.DistanceFrom(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceSquaredFrom(Line line) => _impl.DistanceSquaredFrom(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceFrom(Ray ray) => _impl.DistanceFrom(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceSquaredFrom(Ray ray) => _impl.DistanceSquaredFrom(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceFrom(BoundedRay ray) => _impl.DistanceFrom(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceSquaredFrom(BoundedRay ray) => _impl.DistanceSquaredFrom(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Contains(BoundedRay ray) => _impl.Contains(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsIntersectedBy(Line line) => _impl.IsIntersectedBy(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsIntersectedBy(Ray ray) => _impl.IsIntersectedBy(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsIntersectedBy(BoundedRay ray) => _impl.IsIntersectedBy(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ConvexShapeLineIntersection? IntersectionWith(Line line) => _impl.IntersectionWith(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ConvexShapeLineIntersection FastIntersectionWith(Line line) => _impl.FastIntersectionWith(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ConvexShapeLineIntersection? IntersectionWith(Ray ray) => _impl.IntersectionWith(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ConvexShapeLineIntersection FastIntersectionWith(Ray ray) => _impl.FastIntersectionWith(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ConvexShapeLineIntersection? IntersectionWith(BoundedRay ray) => _impl.IntersectionWith(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ConvexShapeLineIntersection FastIntersectionWith(BoundedRay ray) => _impl.FastIntersectionWith(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceFrom(Plane plane) => _impl.DistanceFrom(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceSquaredFrom(Plane plane) => _impl.DistanceSquaredFrom(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SignedDistanceFrom(Plane plane) => _impl.SignedDistanceFrom(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location PointClosestTo(Plane plane) => _impl.PointClosestTo(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointOn(Plane plane) => _impl.ClosestPointOn(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PlaneObjectRelationship RelationshipTo(Plane plane) => _impl.RelationshipTo(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location SurfacePointClosestTo(Location point) => _impl.SurfacePointClosestTo(point);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceFrom(Location point) => _impl.SurfaceDistanceFrom(point);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceSquaredFrom(Location point) => _impl.SurfaceDistanceSquaredFrom(point);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location SurfacePointClosestTo(Line line) => _impl.SurfacePointClosestTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointToSurfaceOn(Line line) => _impl.ClosestPointToSurfaceOn(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceFrom(Line line) => _impl.SurfaceDistanceFrom(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceSquaredFrom(Line line) => _impl.SurfaceDistanceSquaredFrom(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location SurfacePointClosestTo(Ray ray) => _impl.SurfacePointClosestTo(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointToSurfaceOn(Ray ray) => _impl.ClosestPointToSurfaceOn(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceFrom(Ray ray) => _impl.SurfaceDistanceFrom(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceSquaredFrom(Ray ray) => _impl.SurfaceDistanceSquaredFrom(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location SurfacePointClosestTo(BoundedRay ray) => _impl.SurfacePointClosestTo(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointToSurfaceOn(BoundedRay ray) => _impl.ClosestPointToSurfaceOn(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceFrom(BoundedRay ray) => _impl.SurfaceDistanceFrom(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceSquaredFrom(BoundedRay ray) => _impl.SurfaceDistanceSquaredFrom(ray);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location SurfacePointClosestTo(Plane plane) => _impl.SurfacePointClosestTo(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointToSurfaceOn(Plane plane) => _impl.ClosestPointToSurfaceOn(plane);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid Parse(string s, IFormatProvider? provider) => TranslatedRotatedConvexShape<Cuboid>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out PositionedRotatedCuboid result) {
		var returnVal = TranslatedRotatedConvexShape<Cuboid>.TryParse(s, provider, out var interimResult);
		result = interimResult;
		return returnVal;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => TranslatedRotatedConvexShape<Cuboid>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out PositionedRotatedCuboid result) {
		var returnVal = TranslatedRotatedConvexShape<Cuboid>.TryParse(s, provider, out var interimResult);
		result = interimResult;
		return returnVal;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void SerializeToBytes(Span<byte> dest, PositionedRotatedCuboid src) => TranslatedRotatedConvexShape<Cuboid>.SerializeToBytes(dest, src);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid DeserializeFromBytes(ReadOnlySpan<byte> src) => TranslatedRotatedConvexShape<Cuboid>.DeserializeFromBytes(src);
	public static int SerializationByteSpanLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TranslatedRotatedConvexShape<Cuboid>.SerializationByteSpanLength;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(PositionedRotatedCuboid left, PositionedRotatedCuboid right) => left._impl == right._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(PositionedRotatedCuboid left, PositionedRotatedCuboid right) => left._impl != right._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid Random() => TranslatedRotatedConvexShape<Cuboid>.Random();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid operator *(PositionedRotatedCuboid left, float right) => left._impl * right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid operator /(PositionedRotatedCuboid left, float right) => left._impl / right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid operator *(float left, PositionedRotatedCuboid right) => left * right._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid Random(PositionedRotatedCuboid minInclusive, PositionedRotatedCuboid maxExclusive) => TranslatedRotatedConvexShape<Cuboid>.Random(minInclusive, maxExclusive);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid Interpolate(PositionedRotatedCuboid start, PositionedRotatedCuboid end, float distance) => TranslatedRotatedConvexShape<Cuboid>.Interpolate(start, end, distance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid operator +(PositionedRotatedCuboid left, Vect right) => left._impl + right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid operator -(PositionedRotatedCuboid left, Vect right) => left._impl - right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid operator +(Vect left, PositionedRotatedCuboid right) => left + right._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid operator *(PositionedRotatedCuboid left, Rotation right) => left._impl * right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedRotatedCuboid operator *(Rotation left, PositionedRotatedCuboid right) => left * right._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedRotatedCuboid RotatedBy(Rotation rot) => _impl.RotatedBy(rot);
	Location IConvexShape.GetRandomInternalLocation() => ((IConvexShape) _impl).GetRandomInternalLocation();
	#endregion
}