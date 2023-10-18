// Created on 2023-10-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

partial struct Fraction {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Fraction ClampZeroToFull() => new(Math.Clamp(AsDecimal, Zero.AsDecimal, Full.AsDecimal));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Fraction ClampFullNegativeToFull() => new(Math.Clamp(AsDecimal, FullNegative.AsDecimal, Full.AsDecimal));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Fraction Clamp(Fraction min, Fraction max) => new(Math.Clamp(AsDecimal, min.AsDecimal, max.AsDecimal));

	// TODO consider overrideable operators

	// TODO interactions with other types
}