// Created on 2024-03-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

partial struct Location : ISignedDistanceMeasurable<Location, Plane>, IContainable<Location, Plane>, IClosestExogenousPointDiscoverable<Location, Plane> {
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Plane plane) => plane.DistanceFrom(this);
	float IDistanceMeasurable<Plane>.DistanceSquaredFrom(Plane plane) {
		var sqrtResult = DistanceFrom(plane);
		return sqrtResult * sqrtResult;
	}
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float SignedDistanceFrom(Plane plane) => plane.SignedDistanceFrom(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsContainedWithin(Plane plane) => plane.Contains(this);
	/// <summary>
	/// Determines whether this location lies on <paramref name="plane"/>, within <paramref name="planeThickness"/>.
	/// </summary>
	/// <param name="plane">The plane to test against.</param>
	/// <param name="planeThickness">How far from the plane this location is permitted to be and still be considered contained within it.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsContainedWithin(Plane plane, float planeThickness) => plane.Contains(this, planeThickness);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointOn(Plane plane) => plane.PointClosestTo(this);
}
partial struct Direction : IAngleMeasurable<Plane>, IReflectable<Plane, Direction>, IParallelizable<Direction, Plane>, IOrthogonalizable<Direction, Plane> {
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Plane plane) => plane.AngleTo(this);
	/// <inheritdoc cref="Plane.SignedAngleTo(Direction)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle SignedAngleTo(Plane plane) => plane.SignedAngleTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle? IncidentAngleWith(Plane plane) => plane.IncidentAngleWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle FastIncidentAngleWith(Plane plane) => plane.FastIncidentAngleWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction? ReflectedBy(Plane plane) => plane.ReflectionOf(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction FastReflectedBy(Plane plane) => plane.FastReflectionOf(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction? ParallelizedWith(Plane plane) => plane.ParallelizationOf(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction FastParallelizedWith(Plane plane) => plane.FastParallelizationOf(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction? OrthogonalizedAgainst(Plane plane) => plane.OrthogonalizationOf(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction FastOrthogonalizedAgainst(Plane plane) => plane.FastOrthogonalizationOf(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Plane plane) => plane.IsParallelTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Plane plane) => plane.IsApproximatelyParallelTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Plane plane, Angle tolerance) => plane.IsApproximatelyParallelTo(this, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Plane plane) => plane.IsOrthogonalTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Plane plane) => plane.IsApproximatelyOrthogonalTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Plane plane, Angle tolerance) => plane.IsApproximatelyOrthogonalTo(this, tolerance);
}
partial struct Vect : IAngleMeasurable<Plane>, IReflectable<Plane, Vect>, IProjectable<Vect, Plane>, IParallelizable<Vect, Plane>, IOrthogonalizable<Vect, Plane> {
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Plane plane) => plane.AngleTo(this);
	/// <inheritdoc cref="Plane.SignedAngleTo(Direction)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle SignedAngleTo(Plane plane) => plane.SignedAngleTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle? IncidentAngleWith(Plane plane) => plane.IncidentAngleWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle FastIncidentAngleWith(Plane plane) => plane.FastIncidentAngleWith(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? ReflectedBy(Plane plane) => plane.ReflectionOf(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastReflectedBy(Plane plane) => plane.FastReflectionOf(this);
	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	/// <remarks>
	/// Unlike the general <see cref="IProjectable{TSelf,TOther}"/> contract, this never returns <see langword="null"/>: projecting a <see cref="Vect"/> on to a plane is always well-defined (it may just come out zero-length, if this vector was exactly orthogonal to the plane).
	/// </remarks>
	/// <param name="plane">The plane to project onto.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect ProjectedOnTo(Plane plane) => plane.ProjectionOf(this);
	Vect? IProjectable<Vect, Plane>.ProjectedOnTo(Plane plane) => ProjectedOnTo(plane);
	Vect IProjectable<Vect, Plane>.FastProjectedOnTo(Plane plane) => ProjectedOnTo(plane);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? ParallelizedWith(Plane plane) => plane.ParallelizationOf(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastParallelizedWith(Plane plane) => plane.FastParallelizationOf(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect? OrthogonalizedAgainst(Plane plane) => plane.OrthogonalizationOf(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect FastOrthogonalizedAgainst(Plane plane) => plane.FastOrthogonalizationOf(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsParallelTo(Plane plane) => plane.IsParallelTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Plane plane) => plane.IsApproximatelyParallelTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyParallelTo(Plane plane, Angle tolerance) => plane.IsApproximatelyParallelTo(this, tolerance);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsOrthogonalTo(Plane plane) => plane.IsOrthogonalTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Plane plane) => plane.IsApproximatelyOrthogonalTo(this);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsApproximatelyOrthogonalTo(Plane plane, Angle tolerance) => plane.IsApproximatelyOrthogonalTo(this, tolerance);
}