// Created on 2024-03-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public readonly partial struct Plane : 
	IAdditionOperators<Plane, Vect, Plane>,
	IMultiplyOperators<Plane, (Location Pivot, Rotation Rotation), Plane>,
	IMultiplyOperators<Plane, (Rotation Rotation, Location Pivot), Plane>,
	IUnaryNegationOperators<Plane, Plane> {

	public Plane Flipped {
		get => new(-_normal, -_smallestDistanceFromOriginAlongNormal);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator -(Plane operand) => operand.Flipped;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator +(Plane plane, Vect v) => plane.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator +(Vect v, Plane plane) => plane.MovedBy(v);
	public Plane MovedBy(Vect v) => new(Normal, PointClosestToOrigin + v);
	

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *(Plane plane, (Location Pivot, Rotation Rotation) rotTuple) => plane.RotatedAround(rotTuple.Pivot, rotTuple.Rotation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *((Location Pivot, Rotation Rotation) rotTuple, Plane plane) => plane.RotatedAround(rotTuple.Pivot, rotTuple.Rotation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *(Plane plane, (Rotation Rotation, Location Pivot) rotTuple) => plane.RotatedAround(rotTuple.Pivot, rotTuple.Rotation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane operator *((Rotation Rotation, Location Pivot) rotTuple, Plane plane) => plane.RotatedAround(rotTuple.Pivot, rotTuple.Rotation);
	public Plane RotatedAround(Location pivotPoint, Rotation rot) => new(Normal * rot, ClosestPointTo(pivotPoint) * (pivotPoint, rot));


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Plane plane, Direction dir) => plane.AngleTo(dir);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle operator ^(Direction dir, Plane plane) => plane.AngleTo(dir);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Plane other) => Normal.AngleTo(other.Normal);
	public Angle AngleTo(Direction direction) => Angle.FromRadians(1f - MathF.Acos(MathF.Abs(Normal.SimilarityTo(direction))));
	public Direction Reflect(Direction direction) { // TODO explain in XML that this returns the same direction if the input is parallel to the plane
		return Direction.FromVector3(-2f * Vector3.Dot(Normal.ToVector3(), direction.ToVector3()) * Normal.ToVector3() + direction.ToVector3());
	}

	public Location ClosestPointTo(Location location) => location - PointClosestToOrigin.GetVectTo(location).ProjectedOnTo(Normal);
	public float SignedDistanceFrom(Location location) => Vector3.Dot(location.ToVector3(), _normal) - _smallestDistanceFromOriginAlongNormal;
	public float DistanceFrom(Location location) => MathF.Abs(SignedDistanceFrom(location));
	
	// TODO in Xml make it clear that "facing towards" means the normal is pointing out of this side of the plane; and that points on the plane (within the thickness value) will return false
	// Implementation note: We use a plane thickness by default because relying on the signed distance being exactly 0 for anything other than axis-aligned planes is pretty much stochastic
	// due to FP inaccuracy. Even for axis-aligned ones it's still pretty bad, but is possibly more consistent when moving around on the surface of the plane. In these cases, if users
	// really want 0-thickness planes, they can still specify as such using the overloads that take a thickness parameter.
	public bool FacesTowards(Location location) => FacesTowards(location, DefaultPlaneThickness);
	public bool FacesAwayFrom(Location location) => FacesAwayFrom(location, DefaultPlaneThickness);
	public bool FacesTowards(Location location, float planeThickness) => SignedDistanceFrom(location) > planeThickness;
	public bool FacesAwayFrom(Location location, float planeThickness) => SignedDistanceFrom(location) < -planeThickness;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location) => Contains(location, DefaultPlaneThickness);
	public bool Contains(Location location, float planeThickness) => DistanceFrom(location) <= planeThickness;

	public float DistanceFrom(Plane other) => MathF.Abs(Normal.SimilarityTo(other.Normal)) >= 0.999f ? PointClosestToOrigin.DistanceFrom(other.PointClosestToOrigin) : 0f;
	public Line? IntersectionLineWith(Plane other) {
		static (float A, float B) FindNonZeroComponents(float thisA, float thisB, float thisCoefficient, float otherA, float otherB, float otherCoefficient) {
			var divisor = thisA * otherB - otherA * thisB;

			return (
				(otherB * thisCoefficient - thisB * otherCoefficient) / divisor,
				(thisA * otherCoefficient - otherA * thisCoefficient) / divisor
			);
		}

		var lineDirection = Vector3.Cross(_normal, other._normal);
		if (lineDirection.LengthSquared() == 0f) return null; // parallel planes

		var dirXAbs = MathF.Abs(lineDirection.X);
		var dirYAbs = MathF.Abs(lineDirection.Y);
		var dirZAbs = MathF.Abs(lineDirection.Z);

		if (dirXAbs > dirYAbs) {
			if (dirXAbs > dirZAbs) goto calculateUsingZeroX;
			else goto calculateUsingZeroZ;
		}
		else if (dirYAbs > dirZAbs) goto calculateUsingZeroY;
		else goto calculateUsingZeroZ;

		calculateUsingZeroX: {
			var (y, z) = FindNonZeroComponents(_normal.Y, _normal.Z, _smallestDistanceFromOriginAlongNormal, other._normal.Y, other._normal.Z, other._smallestDistanceFromOriginAlongNormal);
			return new Line((0f, y, z), Direction.FromVector3(lineDirection));
		}

		calculateUsingZeroY: {
			var (x, z) = FindNonZeroComponents(_normal.X, _normal.Z, _smallestDistanceFromOriginAlongNormal, other._normal.X, other._normal.Z, other._smallestDistanceFromOriginAlongNormal);
			return new Line((x, 0f, z), Direction.FromVector3(lineDirection));
		}

		calculateUsingZeroZ: {
			var (x, y) = FindNonZeroComponents(_normal.X, _normal.Y, _smallestDistanceFromOriginAlongNormal, other._normal.X, other._normal.Y, other._smallestDistanceFromOriginAlongNormal);
			return new Line((x, y, 0f), Direction.FromVector3(lineDirection));
		}
	}
}

// ReSharper disable UnusedTypeParameter Type parameterization instead of directly using interface type is used to prevent boxing (instead relying on reification of each parameter combination)
public static class PlaneExtensions {
	public static Angle AngleTo<TLine>(this TLine @this, Plane plane) where TLine : ILine => plane.AngleTo(@this);
}
partial struct Direction {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Angle AngleTo(Plane plane) => plane.AngleTo(this);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction ReflectedBy(Plane plane) => plane.Reflect(this);
}