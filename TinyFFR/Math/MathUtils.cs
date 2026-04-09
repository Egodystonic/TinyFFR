// Created on 2023-10-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;

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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect GetTranslationFromMatrix(Matrix4x4 mat) => Vect.FromVector3(mat.Translation);
	
	public static Vect GetBestGuessScalingFromMatrix(Matrix4x4 mat) {
		if (Matrix4x4.Decompose(mat, out var s, out _, out _)) return Vect.FromVector3(s);

		var rowA = new Vector3(mat[0, 0], mat[0, 1], mat[0, 2]);
		var rowB = new Vector3(mat[1, 0], mat[1, 1], mat[1, 2]);
		var rowC = new Vector3(mat[2, 0], mat[2, 1], mat[2, 2]);

		var xScale = rowA.Length();
		var yScale = rowB.Length();
		var zScale = rowC.Length();
		
		if (!Single.IsFinite(xScale) || xScale == 0f) xScale = 1f;
		if (!Single.IsFinite(yScale) || yScale == 0f) yScale = 1f;
		if (!Single.IsFinite(zScale) || zScale == 0f) zScale = 1f;

		// Flip A/X if 3x3 mat has negative determinant
		var aCrossB = Vector3.Cross(rowA, rowB);
		if (Vector3.Dot(aCrossB, rowC) < 0f) {
			xScale = -xScale;
		}
		
		return new Vect(xScale, yScale, zScale);
	}
	
	public static Quaternion GetBestGuessRotationFromMatrix(Matrix4x4 mat) {
		if (Matrix4x4.Decompose(mat, out _, out var r, out _)) return r;

		var rowA = new Vector3(mat[0, 0], mat[0, 1], mat[0, 2]);
		var rowB = new Vector3(mat[1, 0], mat[1, 1], mat[1, 2]);
		var rowC = new Vector3(mat[2, 0], mat[2, 1], mat[2, 2]);

		var xScale = rowA.Length();
		var yScale = rowB.Length();
		
		if (!Single.IsFinite(xScale) || xScale == 0f) xScale = 1f;
		if (!Single.IsFinite(yScale) || yScale == 0f) yScale = 1f;

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

		return Quaternion.CreateFromRotationMatrix(new Matrix4x4(
			rowA.X, rowA.Y, rowA.Z, 0f,
			rowB.X, rowB.Y, rowB.Z, 0f,
			rowC.X, rowC.Y, rowC.Z, 0f,
			0f, 0f, 0f, 1f
		));
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
		
		if (!Single.IsFinite(xScale) || xScale == 0f) xScale = 1f;
		if (!Single.IsFinite(yScale) || yScale == 0f) yScale = 1f;
		if (!Single.IsFinite(zScale) || zScale == 0f) zScale = 1f;

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
			Rotation.FromQuaternion(rotationQuat),
			new Vect(xScale, yScale, zScale)
		);
	}
	
	public static Matrix4x4 ForceInvertMatrix(Matrix4x4 mat) {
		if (Matrix4x4.Invert(mat, out var simpleSolution)) return simpleSolution;
		
		var transform = GetBestGuessTransformFromMatrix(mat);
		
		static float FixScalingComponent(float scalar) {
			const float MinAxisScaling = 1E-8f;
			var scalarSign = MathF.Sign(scalar);
			if (scalarSign == 0) scalarSign = 1;
			
			// Alternative approach: return MathF.Abs(scalar) < MinAxisScaling ? scalarSign * MinAxisScaling : scalar;
			// This returns the tiny value -- but this assumes we're unable to invert due to FP inaccuracy rather than invalid input.
			// Both are valid approaches, but in the end returning 1/-1 will stop things from blowing up/out to huge scales when they're not meant to
			// and probably is a little less destructive to the scene overall.
			// The approach chosen below works better if someone tries to squash something's scale progressively from N => 0; N is unlikely to be 1E8f.
			if (MathF.Abs(scalar) >= MinAxisScaling) return scalar;
			return scalarSign;
		}
		var newScaling = transform.Scaling with {
			X = FixScalingComponent(transform.Scaling.X),
			Y = FixScalingComponent(transform.Scaling.Y),
			Z = FixScalingComponent(transform.Scaling.Z),
		};

		var fixedMatrix = 
			Matrix4x4.CreateScale(newScaling.ToVector3())
			* Matrix4x4.CreateFromQuaternion(transform.RotationQuaternion)
			* Matrix4x4.CreateTranslation(transform.Translation.ToVector3());

		if (Matrix4x4.Invert(fixedMatrix, out var fixedSolution)) return fixedSolution;
		return Matrix4x4.Identity;
	}
	
	// This is pretty ugly code, I wrote it while debugging some stuff and figured it's still
	// kinda useful for debugging at times.
	public static string ToStringDescriptive(this Matrix4x4 @this) {
		var result = "<";
		var isIdentity = true;
		var isOnlyTranslation = true;
		for (var r = 0; r < 4; ++r) {
			for (var c = 0; c < 4; ++c) {
				var val2dp = @this[r, c].ToString("N2", CultureInfo.InvariantCulture);
				result += val2dp + (r == 3 && c == 3 ? ">" : " ");
				if (MathF.Abs(Matrix4x4.Identity[r, c] - @this[r, c]) > 0.001f) {
					isIdentity = false;
					if (r != 3) isOnlyTranslation = false;
				}
			}
			if (r != 3) result += "| ";
		}

		if (isIdentity) return "Identity";
		if (isOnlyTranslation) return $"Translation[{@this[3, 0]:N2}/{@this[3,1]:N2}/{@this[3,2]:N2}]";

		foreach (var c in OrientationUtils.AllCardinals) {
			var rotMat = new Transform(rotation: 90f % c.ToDirection()).ToMatrix();
			for (var x = 0; x < 3; ++x) {
				for (var y = 0; y < 3; ++y) {
					if (MathF.Abs(rotMat[x, y] - @this[x, y]) >= 0.001f) {
						goto noMatch;
					}
				}
			}
			result = "Rotation[" + new Angle(90f).ToString("N0", CultureInfo.InvariantCulture) + " around " + c +"]";
			if (MathF.Abs(@this[3, 0]) > 0.001f || MathF.Abs(@this[3, 1]) > 0.001f || MathF.Abs(@this[3, 2]) > 0.001f) {
				result += $"Translation[{@this[3, 0]:N2}/{@this[3,1]:N2}/{@this[3,2]:N2}]";
			}
			return result;
			noMatch: continue;
		}

		return result;
	}
	
	public static bool Equals(this Matrix4x4 @this, Matrix4x4 other, float tolerance) {
		for (var i = 0; i < 16; ++i) {
			if (MathF.Abs(@this[(i >> 2) & 0b11, i & 0b11] - other[(i >> 2) & 0b11, i & 0b11]) > tolerance) return false;
		}
		return true;
	}
	
	public static Vector4 GetRow(this Matrix4x4 @this, int rowIndex) {
		return new Vector4(@this[rowIndex, 0], @this[rowIndex, 1], @this[rowIndex, 2], @this[rowIndex, 3]);
	}
	public static Vector4 GetColumn(this Matrix4x4 @this, int columnIndex) {
		return new Vector4(@this[0, columnIndex], @this[1, columnIndex], @this[2, columnIndex], @this[3, columnIndex]);
	}
}