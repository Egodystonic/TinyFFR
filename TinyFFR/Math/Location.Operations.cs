// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.MathUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

partial struct Location : 
	IAdditionOperators<Location, Vect, Location>,
	ISubtractionOperators<Location, Vect, Location>,
	ISubtractionOperators<Location, Location, Vect>,
	IInterpolatable<Location>,
	IBoundedRandomizable<Location> {
	internal const float DefaultRandomRange = 100f;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location operator +(Location locationOperand, Vect vectOperand) => locationOperand.MovedBy(vectOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location operator +(Vect vectOperand, Location locationOperand) => locationOperand.MovedBy(vectOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Location operator -(Location locationOperand, Vect vectOperand) => locationOperand.MovedBy(-vectOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location MovedBy(Vect vect) => new(AsVector4 + vect.AsVector4);



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator >>(Location start, Location end) => start.GetVectTo(end); // TODO maybe these should give Rays ... Use >>> for Vect? .. No, other way IMO
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator <<(Location end, Location start) => start.GetVectTo(end);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect operator -(Location lhs, Location rhs) => lhs.GetVectFrom(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect GetVectFrom(Location otherLocation) => new(AsVector4 - otherLocation.AsVector4);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect GetVectTo(Location otherLocation) => new(otherLocation.AsVector4 - AsVector4);



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction GetDirectionFrom(Location otherLocation) => GetVectFrom(otherLocation).Direction;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Direction GetDirectionTo(Location otherLocation) => GetVectTo(otherLocation).Direction;

	public static Location Interpolate(Location start, Location end, float distance) {
		return start + (end - start) * distance;
	}

	public static Location CreateNewRandom() {
		return FromVector3(new Vector3(
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive(),
			RandomUtils.NextSingleNegOneToOneInclusive()
		) * DefaultRandomRange);
	}
	public static Location CreateNewRandom(Location minInclusive, Location maxExclusive) {
		return minInclusive + ((minInclusive >> maxExclusive) * RandomUtils.NextSingle());
	}
}