// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public readonly partial struct Sphere
	: IMultiplyOperators<Sphere, float, Sphere> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Sphere operator *(Sphere sphere, float scalar) => sphere.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Sphere operator *(float scalar, Sphere sphere) => sphere.ScaledBy(scalar);

	public Sphere ScaledBy(float scalar) => new(Radius * scalar);

	public float DistanceFrom(Location location) => MathF.Max(0f, ((Vect) location).Length - Radius);
	public float SurfaceDistanceFrom(Location location) => MathF.Abs(((Vect) location).Length - Radius);
	public float DistanceFrom<TLine>(TLine line) where TLine : ILine => MathF.Max(0f, line.DistanceFrom(Location.Origin) - Radius);
	public float SurfaceDistanceFrom<TLine>(TLine line) where TLine : ILine => MathF.Abs(line.DistanceFrom(Location.Origin) - Radius);

	public bool Contains(Location location) => ((Vect) location).LengthSquared <= RadiusSquared;
	
	public Location ClosestPointTo(Location location) {
		var vectFromLocToCentre = (Vect) location;
		if (vectFromLocToCentre.LengthSquared <= RadiusSquared) return location;
		else return location - vectFromLocToCentre.ShortenedBy(Radius);
	}
	public Location ClosestPointOnSurfaceTo(Location location) {
		var vectFromLocToCentre = (Vect) location;
		return (Location) vectFromLocToCentre.WithLength(vectFromLocToCentre.Length - Radius);
	}

	public Location ClosestPointTo<TLine>(TLine line) where TLine : ILine => line.ClosestPointToOrigin();
	public Location ClosestPointOnSurfaceTo<TLine>(TLine line) where TLine : ILine => (Location) ((Vect) line.ClosestPointToOrigin()).WithLength(Radius);

	public Location? SurfaceIntersectionPointWith<TLine>(TLine line) where TLine : ILine {
		// Firstly we solve this always as a simple line as it lets us solve as a quadratic, e.g. distance-from-start = (-b +/- sqrt(b^2 - 4ac)) / 2a
		//																								where a = direction dot direction	(always 1 for unit-length vectors)
		//																								where b = 2(start dot direction)
		//																								where c = (start dot start) - radius^2	(v dot v is equal to v.LengthSquared)
		// This can have zero, one, or two real solutions (just like any quadratic) where the discriminant (the part inside the sqrt) can be:
		//	- negative -> sqrt(negative number) has no real solutions, this indicates there is no intersection
		//	- zero -> sqrt(zero) is zero, meaning "both" solutions are actually identical, meaning the line is exactly tangent to the sphere surface (there is one intersection)
		//	- positive -> sqrt(positive number) has two solutions, this indicates the line enters and exits
		// For simple lines we choose the closest intersection point as the "direction" of an infinite line is irrelevant.
		// For rays we choose the closest intersection in the correction direction
		// For bounded lines we do the same as rays and then make sure it's not further than the end point

		var direction = line.Direction.ToVector3();
		var start = line.StartPoint.ToVector3();
		var b = 2f * Vector3.Dot(start, direction);
		var c = start.LengthSquared() - RadiusSquared;

		var discriminant = b * b - 4 * c;
		if (discriminant < 0f) return null;

		var sqrtDiscriminant = MathF.Sqrt(discriminant);
		var negB = -b;
		var intersectionOne = (negB + sqrtDiscriminant) * 0.5f;
		var intersectionTwo = (negB - sqrtDiscriminant) * 0.5f;

		var intersectionOneIsCloserToStart = MathF.Abs(intersectionOne) < MathF.Abs(intersectionTwo);

		if (line.IsUnboundedInBothDirections) {
			return Location.FromVector3(start + direction * (intersectionOneIsCloserToStart ? intersectionOne : intersectionTwo));
		}

		var lengthFromStart = (MathF.Sign(intersectionOne), MathF.Sign(intersectionTwo), intersectionOneIsCloserToStart) switch {
			(>= 0, >= 0, true) => intersectionOne,
			(>= 0, >= 0, false) => intersectionTwo,
			(>= 0, _, _) => intersectionOne,
			(_, >= 0, _) => intersectionTwo,
			_ => (float?) null
		};
		if (lengthFromStart == null) return null;
		return lengthFromStart > line.Length ? null : Location.FromVector3(start + direction * lengthFromStart.Value);
	}
}