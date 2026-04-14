// Created on 2026-04-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

public readonly struct PositionedSphere : ITranslatedConvexShape<PositionedSphere, Sphere>, ISphere<PositionedSphere> {
	readonly TranslatedConvexShape<Sphere> _impl;

	// TODO xmldoc this is the center point
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.Translation.AsLocation();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { Translation = value.AsVect() };
	}

	public float Radius {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.Radius;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { BaseShape = new Sphere(value) };
	}
	
	public float RadiusSquared {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.RadiusSquared;
	}
	public float Volume {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.Volume;
	}
	public float SurfaceArea {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.SurfaceArea;
	}
	public float Circumference {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.Circumference;
	}
	public float Diameter {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.Diameter;
	}

	public bool IsPhysicallyValid {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.IsPhysicallyValid;
	}

	Sphere ITranslatedShape<PositionedSphere, Sphere>.BaseShape {
		get => _impl.BaseShape;
		init => _impl = _impl with { BaseShape = value };
	}
	
	Vect ITranslatedShape.Translation {
		get => Position.AsVect(); 
		init => Position = value.AsLocation();
	}
	
	public PositionedSphere(float radius, Location position) : this(new Sphere(radius), position) { }
	public PositionedSphere(Sphere baseShape, Location position) : this(new(baseShape, position.AsVect())) { }
	public PositionedSphere(TranslatedConvexShape<Sphere> impl) {
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedConvexShape<Sphere>(PositionedSphere operand) => operand._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PositionedSphere(TranslatedConvexShape<Sphere> operand) => new(operand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedShape<Sphere>(PositionedSphere operand) => operand._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PositionedSphere(TranslatedShape<Sphere> operand) => new(operand);
	
	public static PositionedSphere FromVolume(float volume, Location centerPoint) => new(Sphere.FromVolume(volume), centerPoint);
	public static PositionedSphere FromSurfaceArea(float surfaceArea, Location centerPoint) => new(Sphere.FromSurfaceArea(surfaceArea), centerPoint);
	public static PositionedSphere FromCircumference(float circumference, Location centerPoint) => new(Sphere.FromCircumference(circumference), centerPoint);
	public static PositionedSphere FromDiameter(float diameter, Location centerPoint) => new(Sphere.FromDiameter(diameter), centerPoint);
	public static PositionedSphere FromRadiusSquared(float radiusSquared, Location centerPoint) => new(Sphere.FromRadiusSquared(radiusSquared), centerPoint);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Sphere ToStandardSphere() => _impl.BaseShape;
	
	public bool TrySplit(Plane plane, out Location circleCentrePoint, out float circleRadius) {
		if (!_impl.BaseShape.TrySplit(_impl.TransformToShapeSpace(plane), out circleCentrePoint, out circleRadius)) return false;
		
		circleCentrePoint = _impl.TransformToWorldSpace(circleCentrePoint);
		return true;
	}

	#region Deferring Members
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => ToString(null, null);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public string ToString(string? format, IFormatProvider? formatProvider) => _impl.ToString(format, formatProvider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => _impl.TryFormat(destination, out charsWritten, format, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedSphere MovedBy(Vect v) => _impl.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedSphere ScaledBy(float scalar) => _impl.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedSphere Clamp(PositionedSphere min, PositionedSphere max) => _impl.Clamp(min, max);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object? obj) => obj is PositionedSphere other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => _impl.GetHashCode();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(PositionedSphere other) => _impl.Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(PositionedSphere other, float tolerance) => _impl.Equals(other, tolerance);
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
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere Parse(string s, IFormatProvider? provider) => TranslatedConvexShape<Sphere>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out PositionedSphere result) {
		var returnVal = TranslatedConvexShape<Sphere>.TryParse(s, provider, out var interimResult);
		result = interimResult;
		return returnVal;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => TranslatedConvexShape<Sphere>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out PositionedSphere result) {
		var returnVal = TranslatedConvexShape<Sphere>.TryParse(s, provider, out var interimResult);
		result = interimResult;
		return returnVal;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void SerializeToBytes(Span<byte> dest, PositionedSphere src) => TranslatedConvexShape<Sphere>.SerializeToBytes(dest, src);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere DeserializeFromBytes(ReadOnlySpan<byte> src) => TranslatedConvexShape<Sphere>.DeserializeFromBytes(src);
	public static int SerializationByteSpanLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TranslatedConvexShape<Sphere>.SerializationByteSpanLength;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(PositionedSphere left, PositionedSphere right) => left._impl == right._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(PositionedSphere left, PositionedSphere right) => left._impl != right._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere Random() => TranslatedConvexShape<Sphere>.Random();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere operator *(PositionedSphere left, float right) => left._impl * right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere operator /(PositionedSphere left, float right) => left._impl / right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere operator *(float left, PositionedSphere right) => left * right._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere Random(PositionedSphere minInclusive, PositionedSphere maxExclusive) => TranslatedConvexShape<Sphere>.Random(minInclusive, maxExclusive);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere Interpolate(PositionedSphere start, PositionedSphere end, float distance) => TranslatedConvexShape<Sphere>.Interpolate(start, end, distance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere operator +(PositionedSphere left, Vect right) => left._impl + right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere operator -(PositionedSphere left, Vect right) => left._impl - right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere operator +(Vect left, PositionedSphere right) => left + right._impl;
	#endregion
}