// Created on 2026-04-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents a <see cref="Sphere"/> positioned at a specific <see cref="Position"/> in world space.
/// </summary>
public readonly struct PositionedSphere : ITranslatedConvexShape<PositionedSphere, Sphere>, ISphere<PositionedSphere>,
	IDistanceMeasurable<PositionedSphere>, IDistanceMeasurable<PositionedCuboid>, IDistanceMeasurable<PositionedRotatedCuboid>,
	IIntersectable<PositionedSphere>, IIntersectable<PositionedCuboid>, IIntersectable<PositionedRotatedCuboid> {
	/// <summary>
	/// A <see cref="Sphere.UnitSphere"/> positioned at <see cref="Location.Origin"/>.
	/// </summary>
	public static readonly PositionedSphere UnitSphereAtOrigin = new(Sphere.UnitSphere, Location.Origin);
	/// <summary>
	/// A <see cref="Sphere.OneMeterDiameterSphere"/> positioned at <see cref="Location.Origin"/>.
	/// </summary>
	public static readonly PositionedSphere OneMeterDiameterSphereAtOrigin = new(Sphere.OneMeterDiameterSphere, Location.Origin);
	/// <summary>
	/// A <see cref="Sphere.OneMeterCubedVolumeSphere"/> positioned at <see cref="Location.Origin"/>.
	/// </summary>
	public static readonly PositionedSphere OneMeterCubedVolumeSphereAtOrigin = new (Sphere.OneMeterCubedVolumeSphere, Location.Origin);
	readonly TranslatedConvexShape<Sphere> _impl;

	/// <summary>
	/// The centre point of this sphere.
	/// </summary>
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.Translation.AsLocation();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { Translation = value.AsVect() };
	}

	/// <inheritdoc />
	public float Radius {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.Radius;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _impl = _impl with { BaseShape = new Sphere(value) };
	}

	/// <inheritdoc />
	public float RadiusSquared {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.RadiusSquared;
	}
	/// <inheritdoc />
	public float Volume {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.Volume;
	}
	/// <inheritdoc />
	public float SurfaceArea {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.SurfaceArea;
	}
	/// <inheritdoc />
	public float Circumference {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.Circumference;
	}
	/// <inheritdoc />
	public float Diameter {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.BaseShape.Diameter;
	}

	/// <inheritdoc />
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
	
	/// <summary>
	/// Constructs a new <see cref="PositionedSphere"/> with the given <paramref name="radius"/>, at <paramref name="position"/>.
	/// </summary>
	/// <param name="radius">The radius of the sphere.</param>
	/// <param name="position">The centre point of the sphere.</param>
	public PositionedSphere(float radius, Location position) : this(new Sphere(radius), position) { }
	/// <summary>
	/// Constructs a new <see cref="PositionedSphere"/> from an existing <see cref="Sphere"/>, positioned at <paramref name="position"/>.
	/// </summary>
	/// <param name="baseShape">The unpositioned sphere.</param>
	/// <param name="position">The centre point of the resultant shape.</param>
	public PositionedSphere(Sphere baseShape, Location position) : this(new(baseShape, position.AsVect())) { }
	/// <summary>
	/// Constructs a new <see cref="PositionedSphere"/> directly from its underlying <see cref="TranslatedConvexShape{TShape}"/> representation.
	/// </summary>
	/// <param name="impl">The underlying translated shape.</param>
	public PositionedSphere(TranslatedConvexShape<Sphere> impl) {
		_impl = impl;
	}

	/// <summary>
	/// Converts <paramref name="operand"/> to its underlying <see cref="TranslatedConvexShape{TShape}"/> representation.
	/// </summary>
	/// <param name="operand">The shape to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedConvexShape<Sphere>(PositionedSphere operand) => operand._impl;
	/// <summary>
	/// Converts <paramref name="operand"/> to a <see cref="PositionedSphere"/>.
	/// </summary>
	/// <param name="operand">The shape to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PositionedSphere(TranslatedConvexShape<Sphere> operand) => new(operand);
	/// <summary>
	/// Converts <paramref name="operand"/> to its underlying <see cref="TranslatedShape{TShape}"/> representation.
	/// </summary>
	/// <param name="operand">The shape to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedShape<Sphere>(PositionedSphere operand) => operand._impl;
	/// <summary>
	/// Converts <paramref name="operand"/> to a <see cref="PositionedSphere"/>.
	/// </summary>
	/// <param name="operand">The shape to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PositionedSphere(TranslatedShape<Sphere> operand) => new(operand);

	/// <summary>
	/// Constructs the <see cref="PositionedSphere"/> with the given <see cref="Sphere.Volume"/>, at <paramref name="centerPoint"/>.
	/// </summary>
	/// <param name="volume">The desired volume. Must be non-negative.</param>
	/// <param name="centerPoint">The centre point of the resultant shape.</param>
	public static PositionedSphere FromVolume(float volume, Location centerPoint) => new(Sphere.FromVolume(volume), centerPoint);
	/// <summary>
	/// Constructs the <see cref="PositionedSphere"/> with the given <see cref="Sphere.SurfaceArea"/>, at <paramref name="centerPoint"/>.
	/// </summary>
	/// <param name="surfaceArea">The desired surface area. Must be non-negative.</param>
	/// <param name="centerPoint">The centre point of the resultant shape.</param>
	public static PositionedSphere FromSurfaceArea(float surfaceArea, Location centerPoint) => new(Sphere.FromSurfaceArea(surfaceArea), centerPoint);
	/// <summary>
	/// Constructs the <see cref="PositionedSphere"/> with the given <see cref="Sphere.Circumference"/>, at <paramref name="centerPoint"/>.
	/// </summary>
	/// <param name="circumference">The desired circumference. Must be non-negative.</param>
	/// <param name="centerPoint">The centre point of the resultant shape.</param>
	public static PositionedSphere FromCircumference(float circumference, Location centerPoint) => new(Sphere.FromCircumference(circumference), centerPoint);
	/// <summary>
	/// Constructs the <see cref="PositionedSphere"/> with the given <see cref="Sphere.Diameter"/>, at <paramref name="centerPoint"/>.
	/// </summary>
	/// <param name="diameter">The desired diameter. Must be non-negative.</param>
	/// <param name="centerPoint">The centre point of the resultant shape.</param>
	public static PositionedSphere FromDiameter(float diameter, Location centerPoint) => new(Sphere.FromDiameter(diameter), centerPoint);
	/// <summary>
	/// Constructs the <see cref="PositionedSphere"/> with the given <see cref="Sphere.RadiusSquared"/>, at <paramref name="centerPoint"/>.
	/// </summary>
	/// <param name="radiusSquared">The desired square of the radius. Must be non-negative.</param>
	/// <param name="centerPoint">The centre point of the resultant shape.</param>
	public static PositionedSphere FromRadiusSquared(float radiusSquared, Location centerPoint) => new(Sphere.FromRadiusSquared(radiusSquared), centerPoint);

	/// <summary>
	/// Converts this shape to an unpositioned <see cref="Sphere"/>, discarding <see cref="Position"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Sphere ToStandardSphere() => _impl.BaseShape;

	/// <inheritdoc/>
	public bool TrySplit(Plane plane, out Location circleCentrePoint, out float circleRadius) {
		if (!_impl.BaseShape.TrySplit(_impl.TransformToShapeSpace(plane), out circleCentrePoint, out circleRadius)) return false;

		circleCentrePoint = _impl.TransformToWorldSpace(circleCentrePoint);
		return true;
	}

	/// <summary>
	/// The smallest axis-aligned <see cref="PositionedCuboid"/>, sharing this sphere's <see cref="Position"/>, that fully encloses it.
	/// </summary>
	public PositionedCuboid SmallestEnclosingCube {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(_impl.BaseShape.SmallestEnclosingCube, Position);
	}
	/// <summary>
	/// The largest axis-aligned <see cref="PositionedCuboid"/>, sharing this sphere's <see cref="Position"/>, that fits entirely within it.
	/// </summary>
	public PositionedCuboid LargestEnclosedCube {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(_impl.BaseShape.LargestEnclosedCube, Position);
	}

	/// <summary>
	/// Calculates the distance between this sphere and <paramref name="sphere"/>.
	/// </summary>
	/// <param name="sphere">The other sphere to measure against.</param>
	public float DistanceFrom(PositionedSphere sphere) => Single.Max(0f, Position.DistanceFrom(sphere.Position) - (Radius + sphere.Radius));
	float IDistanceMeasurable<PositionedSphere>.DistanceSquaredFrom(PositionedSphere sphere) { var dist = DistanceFrom(sphere); return dist * dist; }
	/// <summary>
	/// Calculates the distance between this sphere and <paramref name="cuboid"/>.
	/// </summary>
	/// <param name="cuboid">The cuboid to measure against.</param>
	public float DistanceFrom(PositionedCuboid cuboid) => Single.Max(0f, cuboid.DistanceFrom(Position) - Radius);
	float IDistanceMeasurable<PositionedCuboid>.DistanceSquaredFrom(PositionedCuboid cuboid) { var dist = DistanceFrom(cuboid); return dist * dist; }
	/// <summary>
	/// Calculates the distance between this sphere and <paramref name="cuboid"/>.
	/// </summary>
	/// <param name="cuboid">The cuboid to measure against.</param>
	public float DistanceFrom(PositionedRotatedCuboid cuboid) => Single.Max(0f, cuboid.DistanceFrom(Position) - Radius);
	float IDistanceMeasurable<PositionedRotatedCuboid>.DistanceSquaredFrom(PositionedRotatedCuboid cuboid) { var dist = DistanceFrom(cuboid); return dist * dist; }
	/// <summary>
	/// Determines whether this sphere intersects <paramref name="sphere"/>.
	/// </summary>
	/// <param name="sphere">The other sphere to test against.</param>
	public bool IsIntersectedBy(PositionedSphere sphere) { var radiiSum = Radius + sphere.Radius; return Position.DistanceSquaredFrom(sphere.Position) < radiiSum * radiiSum; }
	/// <summary>
	/// Determines whether this sphere intersects <paramref name="cuboid"/>.
	/// </summary>
	/// <param name="cuboid">The cuboid to test against.</param>
	public bool IsIntersectedBy(PositionedCuboid cuboid) => cuboid.DistanceSquaredFrom(Position) < RadiusSquared;
	/// <summary>
	/// Determines whether this sphere intersects <paramref name="cuboid"/>.
	/// </summary>
	/// <param name="cuboid">The cuboid to test against.</param>
	public bool IsIntersectedBy(PositionedRotatedCuboid cuboid) => cuboid.DistanceSquaredFrom(Position) < RadiusSquared;

	#region Deferring Members
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override string ToString() => ToString(null, null);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public string ToString(string? format, IFormatProvider? formatProvider) => _impl.ToString(format, formatProvider);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => _impl.TryFormat(destination, out charsWritten, format, provider);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedSphere MovedBy(Vect v) => _impl.MovedBy(v);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedSphere ScaledBy(float scalar) => _impl.ScaledBy(scalar);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PositionedSphere Clamp(PositionedSphere min, PositionedSphere max) => _impl.Clamp(min, max);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override bool Equals(object? obj) => obj is PositionedSphere other && Equals(other);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public override int GetHashCode() => _impl.GetHashCode();
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(PositionedSphere other) => _impl.Equals(other);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Equals(PositionedSphere other, float tolerance) => _impl.Equals(other, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location PointClosestTo(Location location) => _impl.PointClosestTo(location);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceFrom(Location location) => _impl.DistanceFrom(location);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceSquaredFrom(Location location) => _impl.DistanceSquaredFrom(location);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Contains(Location location) => _impl.Contains(location);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Ray? ReflectionOf(Ray ray) => _impl.ReflectionOf(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Ray FastReflectionOf(Ray ray) => _impl.FastReflectionOf(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Angle? IncidentAngleWith(Ray ray) => _impl.IncidentAngleWith(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Angle FastIncidentAngleWith(Ray ray) => _impl.FastIncidentAngleWith(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public BoundedRay? ReflectionOf(BoundedRay ray) => _impl.ReflectionOf(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public BoundedRay FastReflectionOf(BoundedRay ray) => _impl.FastReflectionOf(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Angle? IncidentAngleWith(BoundedRay ray) => _impl.IncidentAngleWith(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Angle FastIncidentAngleWith(BoundedRay ray) => _impl.FastIncidentAngleWith(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointOn(Line line) => _impl.ClosestPointOn(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointOn(Ray ray) => _impl.ClosestPointOn(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointOn(BoundedRay ray) => _impl.ClosestPointOn(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location PointClosestTo(Line line) => _impl.PointClosestTo(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location PointClosestTo(Ray ray) => _impl.PointClosestTo(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location PointClosestTo(BoundedRay ray) => _impl.PointClosestTo(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceFrom(Line line) => _impl.DistanceFrom(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceSquaredFrom(Line line) => _impl.DistanceSquaredFrom(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceFrom(Ray ray) => _impl.DistanceFrom(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceSquaredFrom(Ray ray) => _impl.DistanceSquaredFrom(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceFrom(BoundedRay ray) => _impl.DistanceFrom(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceSquaredFrom(BoundedRay ray) => _impl.DistanceSquaredFrom(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Contains(BoundedRay ray) => _impl.Contains(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsIntersectedBy(Line line) => _impl.IsIntersectedBy(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsIntersectedBy(Ray ray) => _impl.IsIntersectedBy(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsIntersectedBy(BoundedRay ray) => _impl.IsIntersectedBy(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ConvexShapeLineIntersection? IntersectionWith(Line line) => _impl.IntersectionWith(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ConvexShapeLineIntersection FastIntersectionWith(Line line) => _impl.FastIntersectionWith(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ConvexShapeLineIntersection? IntersectionWith(Ray ray) => _impl.IntersectionWith(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ConvexShapeLineIntersection FastIntersectionWith(Ray ray) => _impl.FastIntersectionWith(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ConvexShapeLineIntersection? IntersectionWith(BoundedRay ray) => _impl.IntersectionWith(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ConvexShapeLineIntersection FastIntersectionWith(BoundedRay ray) => _impl.FastIntersectionWith(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceFrom(Plane plane) => _impl.DistanceFrom(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float DistanceSquaredFrom(Plane plane) => _impl.DistanceSquaredFrom(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SignedDistanceFrom(Plane plane) => _impl.SignedDistanceFrom(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location PointClosestTo(Plane plane) => _impl.PointClosestTo(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointOn(Plane plane) => _impl.ClosestPointOn(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public PlaneObjectRelationship RelationshipTo(Plane plane) => _impl.RelationshipTo(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location SurfacePointClosestTo(Location point) => _impl.SurfacePointClosestTo(point);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceFrom(Location point) => _impl.SurfaceDistanceFrom(point);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceSquaredFrom(Location point) => _impl.SurfaceDistanceSquaredFrom(point);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location SurfacePointClosestTo(Line line) => _impl.SurfacePointClosestTo(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointToSurfaceOn(Line line) => _impl.ClosestPointToSurfaceOn(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceFrom(Line line) => _impl.SurfaceDistanceFrom(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceSquaredFrom(Line line) => _impl.SurfaceDistanceSquaredFrom(line);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location SurfacePointClosestTo(Ray ray) => _impl.SurfacePointClosestTo(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointToSurfaceOn(Ray ray) => _impl.ClosestPointToSurfaceOn(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceFrom(Ray ray) => _impl.SurfaceDistanceFrom(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceSquaredFrom(Ray ray) => _impl.SurfaceDistanceSquaredFrom(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location SurfacePointClosestTo(BoundedRay ray) => _impl.SurfacePointClosestTo(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointToSurfaceOn(BoundedRay ray) => _impl.ClosestPointToSurfaceOn(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceFrom(BoundedRay ray) => _impl.SurfaceDistanceFrom(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float SurfaceDistanceSquaredFrom(BoundedRay ray) => _impl.SurfaceDistanceSquaredFrom(ray);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location SurfacePointClosestTo(Plane plane) => _impl.SurfacePointClosestTo(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public Location ClosestPointToSurfaceOn(Plane plane) => _impl.ClosestPointToSurfaceOn(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere Parse(string s, IFormatProvider? provider) => TranslatedConvexShape<Sphere>.Parse(s, provider);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out PositionedSphere result) {
		var returnVal = TranslatedConvexShape<Sphere>.TryParse(s, provider, out var interimResult);
		result = interimResult;
		return returnVal;
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => TranslatedConvexShape<Sphere>.Parse(s, provider);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out PositionedSphere result) {
		var returnVal = TranslatedConvexShape<Sphere>.TryParse(s, provider, out var interimResult);
		result = interimResult;
		return returnVal;
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static void SerializeToBytes(Span<byte> dest, PositionedSphere src) => TranslatedConvexShape<Sphere>.SerializeToBytes(dest, src);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere DeserializeFromBytes(ReadOnlySpan<byte> src) => TranslatedConvexShape<Sphere>.DeserializeFromBytes(src);
	/// <inheritdoc/>
	public static int SerializationByteSpanLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TranslatedConvexShape<Sphere>.SerializationByteSpanLength;
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator ==(PositionedSphere left, PositionedSphere right) => left._impl == right._impl;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool operator !=(PositionedSphere left, PositionedSphere right) => left._impl != right._impl;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere Random() => TranslatedConvexShape<Sphere>.Random();
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere operator *(PositionedSphere left, float right) => left._impl * right;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere operator /(PositionedSphere left, float right) => left._impl / right;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere operator *(float left, PositionedSphere right) => left * right._impl;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere Random(PositionedSphere minInclusive, PositionedSphere maxExclusive) => TranslatedConvexShape<Sphere>.Random(minInclusive, maxExclusive);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere Interpolate(PositionedSphere start, PositionedSphere end, float distance) => TranslatedConvexShape<Sphere>.Interpolate(start, end, distance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere operator +(PositionedSphere left, Vect right) => left._impl + right;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere operator -(PositionedSphere left, Vect right) => left._impl - right;
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static PositionedSphere operator +(Vect left, PositionedSphere right) => left + right._impl;
	Location IConvexShape.GetRandomInternalLocation() => ((IConvexShape) _impl).GetRandomInternalLocation();
	#endregion
}