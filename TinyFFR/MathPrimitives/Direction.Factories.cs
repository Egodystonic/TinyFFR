// Created on 2023-09-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using static Egodystonic.TinyFFR.VectorUtils;
using static System.Numerics.Vector4;

namespace Egodystonic.TinyFFR;

partial struct Direction {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromVector3PreNormalized(float x, float y, float z) => FromVector3PreNormalized(new Vector3(x, y, z));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromVector3PreNormalized(ReadOnlySpan<float> xyz) => FromVector3PreNormalized(new Vector3(xyz));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction FromVector3PreNormalized(Vector3 v) => new(new Vector4(v, WValue));
}