// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public readonly partial struct Sphere
	: IMultiplyOperators<Sphere, float, Sphere> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Sphere operator *(Sphere sphere, float scalar) => sphere.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Sphere operator *(float scalar, Sphere sphere) => sphere.ScaledBy(scalar);

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
	public Location ClosestSurfacePointTo(Location location) {
		var vectFromLocToCentre = (Vect) location;
		return (Location) vectFromLocToCentre.WithLength(vectFromLocToCentre.Length - Radius);
	}
	public Location ClosestPointTo<TLine>(TLine line) where TLine : ILine => line.ClosestPointTo(Location.Origin);
	public Location ClosestSurfacePointTo<TLine>(TLine line) where TLine : ILine => (Location) ((Vect) line.ClosestPointTo(Location.Origin)).WithLength(Radius);

	public Location? SurfaceIntersectionPointWith<TLine>(TLine line) where TLine : ILine {
		
	}
}