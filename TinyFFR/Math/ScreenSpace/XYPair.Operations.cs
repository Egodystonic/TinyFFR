// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Numerics;

namespace Egodystonic.TinyFFR;

public readonly partial struct XYPair :
	IAdditionOperators<XYPair, XYPair, XYPair>,
	ISubtractionOperators<XYPair, XYPair, XYPair>,
	IMultiplyOperators<XYPair, float, XYPair>,
	IDivisionOperators<XYPair, float, XYPair> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair operator +(XYPair lhs, XYPair rhs) => lhs.Plus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair Plus(XYPair other) => new(AsVector2 + other.AsVector2);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair operator -(XYPair lhs, XYPair rhs) => lhs.Minus(rhs);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair Minus(XYPair other) => new(AsVector2 - other.AsVector2);


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair operator -(XYPair operand) => operand.Reversed;
	public XYPair Reversed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(-AsVector2);
	}

	public Angle PolarAngle { // TODO clarify this is the four-quadrant inverse tangent
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Angle.FromRadians(MathF.Atan2(Y, X));
	}

	public XYPair Absolute {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(MathF.Abs(AsVector2.X), MathF.Abs(AsVector2.Y));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair operator *(XYPair vectOperand, float scalarOperand) => vectOperand.ScaledBy(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair operator *(float scalarOperand, XYPair vectOperand) => vectOperand.ScaledBy(scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XYPair operator /(XYPair vectOperand, float scalarOperand) => vectOperand.ScaledBy(1f / scalarOperand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair ScaledBy(float scalar) => new(Vector2.Multiply(AsVector2, scalar));
}