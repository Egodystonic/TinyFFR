// Created on 2026-04-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

public readonly struct PositionableSphere : ITranslatedShape<PositionableSphere, Sphere>, ISphere<PositionableSphere> {
	readonly TranslatedConvexShape<Sphere> _impl;

	public Location Center {
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

	Sphere ITranslatedShape<PositionableSphere, Sphere>.BaseShape {
		get => _impl.BaseShape;
		init => _impl = _impl with { BaseShape = value };
	}
	
	Vect ITranslatedShape.Translation {
		get => Center.AsVect(); 
		init => Center = value.AsLocation();
	}
	
	public PositionableSphere(float radius, Location centerPoint) : this(new Sphere(radius), centerPoint) { }
	public PositionableSphere(Sphere baseShape, Location centerPoint) : this(new(baseShape, centerPoint.AsVect())) { }
	public PositionableSphere(TranslatedConvexShape<Sphere> impl) {
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedConvexShape<Sphere>(PositionableSphere operand) => operand._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PositionableSphere(TranslatedConvexShape<Sphere> operand) => new(operand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedShape<Sphere>(PositionableSphere operand) => operand._impl;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PositionableSphere(TranslatedShape<Sphere> operand) => new(operand);
	
	public static PositionableSphere FromVolume(float volume, Location centerPoint) => new(Sphere.FromVolume(volume), centerPoint);
	public static PositionableSphere FromSurfaceArea(float surfaceArea, Location centerPoint) => new(Sphere.FromSurfaceArea(surfaceArea), centerPoint);
	public static PositionableSphere FromCircumference(float circumference, Location centerPoint) => new(Sphere.FromCircumference(circumference), centerPoint);
	public static PositionableSphere FromDiameter(float diameter, Location centerPoint) => new(Sphere.FromDiameter(diameter), centerPoint);
	public static PositionableSphere FromRadiusSquared(float radiusSquared, Location centerPoint) => new(Sphere.FromRadiusSquared(radiusSquared), centerPoint);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Sphere ToStandardSphere() => _impl.BaseShape;
	
	public bool TrySplit(Plane plane, out Location circleCentrePoint, out float circleRadius) {
		if (!_impl.BaseShape.TrySplit(_impl.TransformToShapeSpace(plane), out circleCentrePoint, out circleRadius)) return false;
		
		circleCentrePoint = _impl.TransformToWorldSpace(circleCentrePoint);
		return true;
	}

	#region Deferring Members
	public override string ToString() => ToString(null, null);
	public string ToString(string? format, IFormatProvider? formatProvider) => _impl.ToString(format, formatProvider);
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => _impl.TryFormat(destination, out charsWritten, format, provider);
	public PositionableSphere MovedBy(Vect v) => _impl.MovedBy(v);
	public PositionableSphere ScaledBy(float scalar) => _impl.ScaledBy(scalar);
	public PositionableSphere Clamp(PositionableSphere min, PositionableSphere max) => _impl.Clamp(min, max);
	public override bool Equals(object? obj) => obj is PositionableSphere other && Equals(other);
	public override int GetHashCode() => _impl.GetHashCode();
	public bool Equals(PositionableSphere other) => _impl.Equals(other);
	public bool Equals(PositionableSphere other, float tolerance) => _impl.Equals(other, tolerance);
	public Location PointClosestTo(Location location) => _impl.PointClosestTo(location);
	public float DistanceFrom(Location location) => _impl.DistanceFrom(location);
	public float DistanceSquaredFrom(Location location) => _impl.DistanceSquaredFrom(location);
	public bool Contains(Location location) => _impl.Contains(location);
	public Ray? ReflectionOf(Ray ray) => _impl.ReflectionOf(ray);
	public Ray FastReflectionOf(Ray ray) => _impl.FastReflectionOf(ray);
	public Angle? IncidentAngleWith(Ray ray) => _impl.IncidentAngleWith(ray);
	public Angle FastIncidentAngleWith(Ray ray) => _impl.FastIncidentAngleWith(ray);
	public BoundedRay? ReflectionOf(BoundedRay ray) => _impl.ReflectionOf(ray);
	public BoundedRay FastReflectionOf(BoundedRay ray) => _impl.FastReflectionOf(ray);
	public Angle? IncidentAngleWith(BoundedRay ray) => _impl.IncidentAngleWith(ray);
	public Angle FastIncidentAngleWith(BoundedRay ray) => _impl.FastIncidentAngleWith(ray);
	public Location ClosestPointOn(Line line) => _impl.ClosestPointOn(line);
	public Location ClosestPointOn(Ray ray) => _impl.ClosestPointOn(ray);
	public Location ClosestPointOn(BoundedRay ray) => _impl.ClosestPointOn(ray);
	public Location PointClosestTo(Line line) => _impl.PointClosestTo(line);
	public Location PointClosestTo(Ray ray) => _impl.PointClosestTo(ray);
	public Location PointClosestTo(BoundedRay ray) => _impl.PointClosestTo(ray);
	public float DistanceFrom(Line line) => _impl.DistanceFrom(line);
	public float DistanceSquaredFrom(Line line) => _impl.DistanceSquaredFrom(line);
	public float DistanceFrom(Ray ray) => _impl.DistanceFrom(ray);
	public float DistanceSquaredFrom(Ray ray) => _impl.DistanceSquaredFrom(ray);
	public float DistanceFrom(BoundedRay ray) => _impl.DistanceFrom(ray);
	public float DistanceSquaredFrom(BoundedRay ray) => _impl.DistanceSquaredFrom(ray);
	public bool Contains(BoundedRay ray) => _impl.Contains(ray);
	public bool IsIntersectedBy(Line line) => _impl.IsIntersectedBy(line);
	public bool IsIntersectedBy(Ray ray) => _impl.IsIntersectedBy(ray);
	public bool IsIntersectedBy(BoundedRay ray) => _impl.IsIntersectedBy(ray);
	public ConvexShapeLineIntersection? IntersectionWith(Line line) => _impl.IntersectionWith(line);
	public ConvexShapeLineIntersection FastIntersectionWith(Line line) => _impl.FastIntersectionWith(line);
	public ConvexShapeLineIntersection? IntersectionWith(Ray ray) => _impl.IntersectionWith(ray);
	public ConvexShapeLineIntersection FastIntersectionWith(Ray ray) => _impl.FastIntersectionWith(ray);
	public ConvexShapeLineIntersection? IntersectionWith(BoundedRay ray) => _impl.IntersectionWith(ray);
	public ConvexShapeLineIntersection FastIntersectionWith(BoundedRay ray) => _impl.FastIntersectionWith(ray);
	public float DistanceFrom(Plane plane) => _impl.DistanceFrom(plane);
	public float DistanceSquaredFrom(Plane plane) => _impl.DistanceSquaredFrom(plane);
	public float SignedDistanceFrom(Plane plane) => _impl.SignedDistanceFrom(plane);
	public Location PointClosestTo(Plane plane) => _impl.PointClosestTo(plane);
	public Location ClosestPointOn(Plane plane) => _impl.ClosestPointOn(plane);
	public PlaneObjectRelationship RelationshipTo(Plane plane) => _impl.RelationshipTo(plane);
	public Location SurfacePointClosestTo(Location point) => _impl.SurfacePointClosestTo(point);
	public float SurfaceDistanceFrom(Location point) => _impl.SurfaceDistanceFrom(point);
	public float SurfaceDistanceSquaredFrom(Location point) => _impl.SurfaceDistanceSquaredFrom(point);
	public Location SurfacePointClosestTo(Line line) => _impl.SurfacePointClosestTo(line);
	public Location ClosestPointToSurfaceOn(Line line) => _impl.ClosestPointToSurfaceOn(line);
	public float SurfaceDistanceFrom(Line line) => _impl.SurfaceDistanceFrom(line);
	public float SurfaceDistanceSquaredFrom(Line line) => _impl.SurfaceDistanceSquaredFrom(line);
	public Location SurfacePointClosestTo(Ray ray) => _impl.SurfacePointClosestTo(ray);
	public Location ClosestPointToSurfaceOn(Ray ray) => _impl.ClosestPointToSurfaceOn(ray);
	public float SurfaceDistanceFrom(Ray ray) => _impl.SurfaceDistanceFrom(ray);
	public float SurfaceDistanceSquaredFrom(Ray ray) => _impl.SurfaceDistanceSquaredFrom(ray);
	public Location SurfacePointClosestTo(BoundedRay ray) => _impl.SurfacePointClosestTo(ray);
	public Location ClosestPointToSurfaceOn(BoundedRay ray) => _impl.ClosestPointToSurfaceOn(ray);
	public float SurfaceDistanceFrom(BoundedRay ray) => _impl.SurfaceDistanceFrom(ray);
	public float SurfaceDistanceSquaredFrom(BoundedRay ray) => _impl.SurfaceDistanceSquaredFrom(ray);
	public Location SurfacePointClosestTo(Plane plane) => _impl.SurfacePointClosestTo(plane);
	public Location ClosestPointToSurfaceOn(Plane plane) => _impl.ClosestPointToSurfaceOn(plane);
	public static PositionableSphere Parse(string s, IFormatProvider? provider) => TranslatedConvexShape<Sphere>.Parse(s, provider);
	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out PositionableSphere result) {
		var returnVal = TranslatedConvexShape<Sphere>.TryParse(s, provider, out var interimResult);
		result = interimResult;
		return returnVal;
	}
	public static PositionableSphere Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => TranslatedConvexShape<Sphere>.Parse(s, provider);
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out PositionableSphere result) {
		var returnVal = TranslatedConvexShape<Sphere>.TryParse(s, provider, out var interimResult);
		result = interimResult;
		return returnVal;
	}
	public static void SerializeToBytes(Span<byte> dest, PositionableSphere src) => TranslatedConvexShape<Sphere>.SerializeToBytes(dest, src);
	public static PositionableSphere DeserializeFromBytes(ReadOnlySpan<byte> src) => TranslatedConvexShape<Sphere>.DeserializeFromBytes(src);
	public static int SerializationByteSpanLength => TranslatedConvexShape<Sphere>.SerializationByteSpanLength;
	public static bool operator ==(PositionableSphere left, PositionableSphere right) => left._impl == right._impl;
	public static bool operator !=(PositionableSphere left, PositionableSphere right) => left._impl != right._impl;
	public static PositionableSphere Random() => TranslatedConvexShape<Sphere>.Random();
	public static PositionableSphere operator *(PositionableSphere left, float right) => left._impl * right;
	public static PositionableSphere operator /(PositionableSphere left, float right) => left._impl / right;
	public static PositionableSphere operator *(float left, PositionableSphere right) => left * right._impl;
	public static PositionableSphere Random(PositionableSphere minInclusive, PositionableSphere maxExclusive) => TranslatedConvexShape<Sphere>.Random(minInclusive, maxExclusive);
	public static PositionableSphere Interpolate(PositionableSphere start, PositionableSphere end, float distance) => TranslatedConvexShape<Sphere>.Interpolate(start, end, distance);
	public static PositionableSphere operator +(PositionableSphere left, Vect right) => left._impl + right;
	public static PositionableSphere operator -(PositionableSphere left, Vect right) => left._impl - right;
	public static PositionableSphere operator +(Vect left, PositionableSphere right) => left + right._impl;
	#endregion
}