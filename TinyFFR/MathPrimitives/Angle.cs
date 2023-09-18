// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.VectorUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

[StructLayout(LayoutKind.Sequential, Size = sizeof(float), Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from float
public readonly partial struct Angle {
	const float Tau = MathF.Tau;
	const float TauReciprocal = 1f / MathF.Tau;
	const float RadiansToDegreesRatio = 360f / Tau;
	const float DegreesToRadiansRatio = Tau / 360f;
	public static readonly Angle None = FromFractionOfFullCircle(0f);
	public static readonly Angle QuarterCircle = FromFractionOfFullCircle(0.25f);
	public static readonly Angle HalfCircle = FromFractionOfFullCircle(0.5f);
	public static readonly Angle ThreeQuarterCircle = FromFractionOfFullCircle(0.75f);
	public static readonly Angle PiRadians = FromRadians(MathF.PI);

	public float AsRadians { get; init; }
	public float AsDegrees {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsRadians * RadiansToDegreesRatio;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsRadians = value * DegreesToRadiansRatio;
	}
	public float AsFractionOfFullCircle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsRadians * TauReciprocal;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => AsRadians = Tau * value;
	}

	public Angle(float fullCircleFraction) => AsFractionOfFullCircle = fullCircleFraction;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle FromRadians(float radians) => new() { AsRadians = radians };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle FromDegrees(float degrees) => new() { AsDegrees = degrees };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle FromFractionOfFullCircle(float fullCircleFraction) => new() { AsFractionOfFullCircle = fullCircleFraction };

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Angle(float operand) => FromFractionOfFullCircle(operand);
}