// Created on 2023-10-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

public static class MathUtils {
	public const float GoldenRatio = 1.6180339887f;

	public static T TrueModulus<T>(T lhs, T rhs) where T : IModulusOperators<T, T, T>, IAdditionOperators<T, T, T> => (lhs % rhs + rhs) % rhs;

	public static Vector4 NormalizeOrZero(Vector4 v) {
		var norm = Vector4.Normalize(v);
		return Single.IsFinite(norm.X) ? norm : Vector4.Zero;
	}

	public static Quaternion NormalizeOrIdentity(Quaternion q) {
		var norm = Quaternion.Normalize(q);
		return Single.IsFinite(norm.X) ? norm : Quaternion.Identity;
	}

	public static bool IsPositiveAndFinite(this float @this) => Single.IsFinite(@this) && @this > 0f;
	public static bool IsNonNegativeAndFinite(this float @this) => Single.IsFinite(@this) && @this >= 0f;

	public static T SafeAbs<T>(T num) where T : IMinMaxValue<T>, ISignedNumber<T>, IBinaryInteger<T> {
		return num == T.MinValue ? T.MaxValue : T.Abs(num);
	}
	
	public static T RemapRange<T>(this T @this, Pair<T, T> inputRange, Pair<T, T> outputRange) where T : IOrdinal<T> {
		var inputDistance = T.GetInterpolationDistance(inputRange.First, inputRange.Second, @this);
		return T.Interpolate(outputRange.First, outputRange.Second, inputDistance);
	}
}