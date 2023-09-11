// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.VectorUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

partial struct Vect {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect FromDirectionAndDistance(Direction direction, float distance) => direction * distance;
}