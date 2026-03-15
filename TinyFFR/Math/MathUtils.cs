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

	public static Transform GetBestGuessTransformFromMatrix(Matrix4x4 mat) {
		if (Matrix4x4.Decompose(mat, out var s, out var r, out var t)) {
			return new Transform(
				Vect.FromVector3(t),
				r,
				Vect.FromVector3(s)
			);
		}

		var rowA = new Vector3(mat[0, 0], mat[0, 1], mat[0, 2]);
		var rowB = new Vector3(mat[1, 0], mat[1, 1], mat[1, 2]);
		var rowC = new Vector3(mat[2, 0], mat[2, 1], mat[2, 2]);

		var xScale = rowA.Length();
		var yScale = rowB.Length();
		var zScale = rowC.Length();

		// Flip A/X if 3x3 mat has negative determinant
		var aCrossB = Vector3.Cross(rowA, rowB);
		if (Vector3.Dot(aCrossB, rowC) < 0f) {
			xScale = -xScale;
			rowA = -rowA;
		}

		rowA /= xScale;
		rowB /= yScale;

		// Gram-Schmidt                                                                                                                                                      
		rowB -= Vector3.Dot(rowB, rowA) * rowA;
		rowB = Vector3.Normalize(rowB);
		rowC = Vector3.Cross(rowA, rowB);

		var rotationQuat = Quaternion.CreateFromRotationMatrix(new Matrix4x4(
			rowA.X, rowA.Y, rowA.Z, 0f,
			rowB.X, rowB.Y, rowB.Z, 0f,
			rowC.X, rowC.Y, rowC.Z, 0f,
			0f, 0f, 0f, 1f
		));

		return new Transform(
			new Vect(mat.M41, mat.M42, mat.M43),
			rotationQuat,
			new Vect(xScale, yScale, zScale)
		);
	}
}