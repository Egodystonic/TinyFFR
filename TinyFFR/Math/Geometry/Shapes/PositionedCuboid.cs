// Created on 2026-04-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

public readonly struct PositionedCuboid : ITranslatedConvexShape<PositionedCuboid, Cuboid>, ICuboid<PositionedCuboid>, 
	IDistanceMeasurable<PositionedSphere>, IDistanceMeasurable<PositionedCuboid>, IDistanceMeasurable<PositionedRotatedCuboid>,
	IIntersectable<PositionedSphere>, IIntersectable<PositionedCuboid>, IIntersectable<PositionedRotatedCuboid> {
	public static readonly PositionedCuboid UnitCubeAtOrigin = new(Cuboid.UnitCube, Location.Origin);
	readonly TranslatedConvexShape<Cuboid> _impl;

	// TODO xmldoc this is the center point
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.Translation.AsLocation();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { Translation = value.AsVect() };
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


	public unsafe IndirectEnumerable<PositionedCuboid, Location> Corners => new(this, GetIteratorVersion(this), &GetCornerCountForEnumerator, &GetIteratorVersion, &GetCornerForEnumerator);
	static int GetCornerCountForEnumerator(PositionedCuboid _) => 8;
	static Location GetCornerForEnumerator(PositionedCuboid @this, int index) => @this.CornerAt(OrientationUtils.AllDiagonals[index]);

	public unsafe IndirectEnumerable<PositionedCuboid, BoundedRay> Edges => new(this, GetIteratorVersion(this), &GetEdgeCountForEnumerator, &GetIteratorVersion, &GetEdgeForEnumerator);
	static int GetEdgeCountForEnumerator(PositionedCuboid _) => 12;
	static BoundedRay GetEdgeForEnumerator(PositionedCuboid @this, int index) => @this.EdgeAt(OrientationUtils.AllIntercardinals[index]);

	public unsafe IndirectEnumerable<PositionedCuboid, Plane> Sides => new(this, GetIteratorVersion(this), &GetSideCountForEnumerator, &GetIteratorVersion, &GetSideForEnumerator);
	static int GetSideCountForEnumerator(PositionedCuboid _) => 6;
	static Plane GetSideForEnumerator(PositionedCuboid @this, int index) => @this.SideAt(OrientationUtils.AllCardinals[index]);

	public unsafe IndirectEnumerable<PositionedCuboid, Location> Centroids => new(this, GetIteratorVersion(this), &GetCentroidCountForEnumerator, &GetIteratorVersion, &GetCentroidForEnumerator);
	static int GetCentroidCountForEnumerator(PositionedCuboid _) => 6;
	static Location GetCentroidForEnumerator(PositionedCuboid @this, int index) => @this.CentroidAt(OrientationUtils.AllCardinals[index]);

	static int GetIteratorVersion(PositionedCuboid _) => 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float GetExtent(Axis axis) => _impl.BaseShape.GetExtent(axis);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float GetHalfExtent(Axis axis) => _impl.BaseShape.GetHalfExtent(axis);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float GetSideSurfaceArea(CardinalOrientation side) => _impl.BaseShape.GetSideSurfaceArea(side);

	public PositionedCuboid WithVolume(float newVolume) => new(_impl.BaseShape.WithVolume(newVolume), Position);
	public PositionedCuboid WithSurfaceArea(float newSurfaceArea) => new(_impl.BaseShape.WithSurfaceArea(newSurfaceArea), Position);
	public PositionedCuboid WithAllExtentsAdjustedBy(float adjustment) => new(_impl.BaseShape.WithAllExtentsAdjustedBy(adjustment), Position);

	Cuboid ITranslatedShape<PositionedCuboid, Cuboid>.BaseShape {
		get => _impl.BaseShape;
		init => _impl = _impl with { BaseShape = value };
	}

	Vect ITranslatedShape.Translation {
		get => Position.AsVect();
		init => Position = value.AsLocation();
	}

	public PositionedCuboid(float width, float height, float depth, Location centerPoint) : this(new Cuboid(width, height, depth), centerPoint) { }
	public PositionedCuboid(Cuboid baseShape, Location centerPoint) : this(new(baseShape, centerPoint.AsVect())) { }
	public PositionedCuboid(TranslatedConvexShape<Cuboid> impl) {
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedConvexShape<Cuboid>(PositionedCuboid operand) => operand._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PositionedCuboid(TranslatedConvexShape<Cuboid> operand) => new(operand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedShape<Cuboid>(PositionedCuboid operand) => operand._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PositionedCuboid(TranslatedShape<Cuboid> operand) => new(operand);

	public static PositionedCuboid FromHalfDimensions(float halfWidth, float halfHeight, float halfDepth, Location centerPoint) => new(Cuboid.FromHalfDimensions(halfWidth, halfHeight, halfDepth), centerPoint);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Cuboid ToStandardCuboid() => _impl.BaseShape;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PositionedRotatedCuboid WithRotation(Rotation rotation) => ToTranslatedRotatedCuboid(rotation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PositionedRotatedCuboid ToTranslatedRotatedCuboid(Rotation rotation) => new(_impl.BaseShape, Position, rotation);
	
	public PositionedSphere SmallestEnclosingSphere {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(_impl.BaseShape.SmallestEnclosingSphere, Position);
	}
	public PositionedSphere LargestEnclosedSphere {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(_impl.BaseShape.LargestEnclosedSphere, Position);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(PositionedSphere sphere) => sphere.DistanceFrom(this);
	float IDistanceMeasurable<PositionedSphere>.DistanceSquaredFrom(PositionedSphere sphere) { var dist = DistanceFrom(sphere); return dist * dist; }
	public float DistanceFrom(PositionedCuboid cuboid) {
		var d = Position - cuboid.Position;
		var dx = MathF.Max(0f, MathF.Abs(d.X) - HalfWidth - cuboid.HalfWidth);
		var dy = MathF.Max(0f, MathF.Abs(d.Y) - HalfHeight - cuboid.HalfHeight);
		var dz = MathF.Max(0f, MathF.Abs(d.Z) - HalfDepth - cuboid.HalfDepth);
		return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
	}
	float IDistanceMeasurable<PositionedCuboid>.DistanceSquaredFrom(PositionedCuboid cuboid) { var dist = DistanceFrom(cuboid); return dist * dist; }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(PositionedRotatedCuboid cuboid) => cuboid.DistanceFrom(this);
	float IDistanceMeasurable<PositionedRotatedCuboid>.DistanceSquaredFrom(PositionedRotatedCuboid cuboid) { var dist = DistanceFrom(cuboid); return dist * dist; }
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(PositionedSphere sphere) => sphere.IsIntersectedBy(this);
	public bool IsIntersectedBy(PositionedCuboid cuboid) {
		var d = Position - cuboid.Position;
		return MathF.Abs(d.X) < HalfWidth + cuboid.HalfWidth
			&& MathF.Abs(d.Y) < HalfHeight + cuboid.HalfHeight
			&& MathF.Abs(d.Z) < HalfDepth + cuboid.HalfDepth;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsIntersectedBy(PositionedRotatedCuboid cuboid) => cuboid.IsIntersectedBy(this);

	#region Deferring Members
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => ToString(null, null);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public string ToString(string? format, IFormatProvider? formatProvider) => _impl.ToString(format, formatProvider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => _impl.TryFormat(destination, out charsWritten, format, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedCuboid MovedBy(Vect v) => _impl.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedCuboid ScaledBy(float scalar) => _impl.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedCuboid Clamp(PositionedCuboid min, PositionedCuboid max) => _impl.Clamp(min, max);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object? obj) => obj is PositionedCuboid other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => _impl.GetHashCode();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(PositionedCuboid other) => _impl.Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(PositionedCuboid other, float tolerance) => _impl.Equals(other, tolerance);
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
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid Parse(string s, IFormatProvider? provider) => TranslatedConvexShape<Cuboid>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out PositionedCuboid result) {
		var returnVal = TranslatedConvexShape<Cuboid>.TryParse(s, provider, out var interimResult);
		result = interimResult;
		return returnVal;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => TranslatedConvexShape<Cuboid>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out PositionedCuboid result) {
		var returnVal = TranslatedConvexShape<Cuboid>.TryParse(s, provider, out var interimResult);
		result = interimResult;
		return returnVal;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void SerializeToBytes(Span<byte> dest, PositionedCuboid src) => TranslatedConvexShape<Cuboid>.SerializeToBytes(dest, src);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid DeserializeFromBytes(ReadOnlySpan<byte> src) => TranslatedConvexShape<Cuboid>.DeserializeFromBytes(src);
	public static int SerializationByteSpanLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TranslatedConvexShape<Cuboid>.SerializationByteSpanLength;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(PositionedCuboid left, PositionedCuboid right) => left._impl == right._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(PositionedCuboid left, PositionedCuboid right) => left._impl != right._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid Random() => TranslatedConvexShape<Cuboid>.Random();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid operator *(PositionedCuboid left, float right) => left._impl * right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid operator /(PositionedCuboid left, float right) => left._impl / right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid operator *(float left, PositionedCuboid right) => left * right._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid Random(PositionedCuboid minInclusive, PositionedCuboid maxExclusive) => TranslatedConvexShape<Cuboid>.Random(minInclusive, maxExclusive);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid Interpolate(PositionedCuboid start, PositionedCuboid end, float distance) => TranslatedConvexShape<Cuboid>.Interpolate(start, end, distance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid operator +(PositionedCuboid left, Vect right) => left._impl + right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid operator -(PositionedCuboid left, Vect right) => left._impl - right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedCuboid operator +(Vect left, PositionedCuboid right) => left + right._impl;
	Location IConvexShape.GetRandomInternalLocation() => ((IConvexShape) _impl).GetRandomInternalLocation();
	#endregion
}