// Created on 2024-03-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public readonly partial struct Plane : 
	IAdditionOperators<Plane, Vect, Plane>,
	IUnaryNegationOperators<Plane, Plane> {

	public Plane Reversed {
		get => new(-_normal, PointClosestToOrigin.ToVector3());
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator -(Plane operand) => operand.Reversed;




	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane lhs, Plane rhs) => lhs.AngleTo(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane plane, Line line) => plane.AngleTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Line line, Plane plane) => plane.AngleTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane plane, Ray line) => plane.AngleTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Ray line, Plane plane) => plane.AngleTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane plane, BoundedLine line) => plane.AngleTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(BoundedLine line, Plane plane) => plane.AngleTo(line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Plane other) => Normal.AngleTo(other.Normal);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo<TLine>(TLine line) {

	}


	public Ray? Reflect(Line line) {

	}
	public Ray? Reflect(Ray ray) {

	}
	public BoundedLine? Reflect(BoundedLine line) {

	}


	public Plane RotatedAround(Location pivotPoint, Rotation rot) {
		// get a vect from the pivot point to the closest point on the plane, and a vect from the closest point to the closest-to-origin
		// then, rotate the first by rot, and rotate the second also, and re-translate it to the end of the first vect
		// I think
	}


	public bool FacesTowards(Location location) {

	}
	public bool FacesAwayFrom(Location location) {

	}

	public bool TrySplitLine(Line line, out Ray outNormalSide, out Ray outOppositeSide) {
		// TODO just find the intersection and etc
	}

	public Line? IntersectionLineWith(Plane other) {
		// Check for parallel
	}

	public bool IsParallelWith(Plane other) {
		// If dot of normals is 1/-1 then yes, otherwise no
	}

	public float DistanceFrom(Plane other) {
		// Always 0 unless the dot of the normals is 1/-1, in which case the planes are parallel, and then we can just take the distance between the closestPoints
	}

	public TLine Snap<TLine>(TLine lineToSnap) where TLine : ILine<TLine> {
		// This is basically projecting the dir on to
	}

	public float SignedDistanceFrom(Location location) {

	}
}