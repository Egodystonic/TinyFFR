// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Numerics;

namespace Egodystonic.TinyFFR;

public readonly partial struct XYPair {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair operator +(XYPair lhs, XYPair rhs) => locationOperand.MovedBy(vectOperand);
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
}