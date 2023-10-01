// Created on 2023-10-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

partial struct Fraction {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Fraction ClampZeroToFull() => new(Math.Clamp(AsCoefficient, Zero.AsCoefficient, Full.AsCoefficient));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Fraction ClampFullInverseToFull() => new(Math.Clamp(AsCoefficient, FullInverse.AsCoefficient, Full.AsCoefficient));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Fraction Clamp(Fraction min, Fraction max) => new(Math.Clamp(AsCoefficient, min.AsCoefficient, max.AsCoefficient));

	// TODO consider overrideable operators

	// TODO interactions with other types
}