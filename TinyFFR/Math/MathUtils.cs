// Created on 2023-10-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

static class MathUtils {
	public static T TrueModulus<T>(T lhs, T rhs) where T : IModulusOperators<T, T, T>, IAdditionOperators<T, T, T> => (lhs % rhs + rhs) % rhs;
}