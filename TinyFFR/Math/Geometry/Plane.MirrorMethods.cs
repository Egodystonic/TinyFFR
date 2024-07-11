// Created on 2024-03-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

partial struct Location : ISignedDistanceMeasurable<Location, Plane>, IContainable<Location, Plane>, IClosestExogenousPointDiscoverable<Location, Plane> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Plane plane) => plane.DistanceFrom(this);
	float IDistanceMeasurable<Plane>.DistanceSquaredFrom(Plane plane) {
		var sqrtResult = DistanceFrom(plane);
		return sqrtResult * sqrtResult;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SignedDistanceFrom(Plane plane) => plane.SignedDistanceFrom(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsContainedWithin(Plane plane) => plane.Contains(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsContainedWithin(Plane plane, float planeThickness) => plane.Contains(this, planeThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Plane plane) => plane.PointClosestTo(this);
}
partial struct Direction : IAngleMeasurable<Plane>, IReflectable<Plane, Direction>, IProjectable<Direction, Plane>, IParallelizable<Direction, Plane>, IOrthogonalizable<Direction, Plane> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Plane plane) => plane.AngleTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction ReflectedBy(Plane plane) => plane.ReflectionOf(this);
	Direction? IReflectable<Plane, Direction>.ReflectedBy(Plane plane) => plane.ReflectionOf(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction? ProjectedOnTo(Plane plane) => plane.ProjectionOf(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction FastProjectedOnTo(Plane plane) => plane.FastProjectionOf(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction? ParallelizedWith(Plane plane) => plane.ParallelizationOf(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction FastParallelizedWith(Plane plane) => plane.FastParallelizationOf(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction? OrthogonalizedAgainst(Plane plane) => plane.OrthogonalizationOf(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction FastOrthogonalizedAgainst(Plane plane) => plane.FastOrthogonalizationOf(this);
}
partial struct Vect : IAngleMeasurable<Plane>, IReflectable<Plane, Vect>, IProjectable<Vect, Plane>, IParallelizable<Vect, Plane>, IOrthogonalizable<Vect, Plane> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Plane plane) => plane.AngleTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ReflectedBy(Plane plane) => plane.ReflectionOf(this);
	Vect? IReflectable<Plane, Vect>.ReflectedBy(Plane plane) => plane.ReflectionOf(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? ProjectedOnTo(Plane plane) => plane.ProjectionOf(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastProjectedOnTo(Plane plane) => plane.FastProjectionOf(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? ParallelizedWith(Plane plane) => plane.ParallelizationOf(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastParallelizedWith(Plane plane) => plane.FastParallelizationOf(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? OrthogonalizedAgainst(Plane plane) => plane.OrthogonalizationOf(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastOrthogonalizedAgainst(Plane plane) => plane.FastOrthogonalizationOf(this);
}