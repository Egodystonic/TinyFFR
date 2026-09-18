// Created on 2023-10-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR;

/// <summary>
/// A static class containing utility methods for working with math.
/// </summary>
public static class MathUtils {
	/// <summary>
	/// The golden ratio, <c>φ</c> (approximately <c>1.618</c>).
	/// </summary>
	public const float GoldenRatio = 1.6180339887f;
	/// <summary>
	/// The square root of 2 (approximately <c>1.414</c>).
	/// </summary>
	public const float SquareRootOfTwo = 1.4142135623f;
	/// <summary>
	/// The square root of 3 (approximately <c>1.732</c>).
	/// </summary>
	public const float SquareRootOfThree = 1.7320508075f;
	/// <summary>
	/// The reciprocal of <see cref="SquareRootOfTwo"/> (approximately <c>0.707</c>).
	/// </summary>
	public const float SquareRootOfTwoReciprocal = 1f / SquareRootOfTwo;
	/// <summary>
	/// The reciprocal of <see cref="SquareRootOfThree"/> (approximately <c>0.577</c>).
	/// </summary>
	public const float SquareRootOfThreeReciprocal = 1f / SquareRootOfThree;

	/// <summary>
	/// Calculates the modulus of <paramref name="lhs"/> and <paramref name="rhs"/>, but unlike the standard <c>%</c>
	/// operator, the result always has the same sign as <paramref name="rhs"/> (the divisor).
	/// </summary>
	/// <remarks>
	/// For example, <c>TrueModulus(-100, 30)</c> returns <c>20</c> (whereas <c>-100 % 30</c> returns <c>-10</c>), and
	/// <c>TrueModulus(100, -30)</c> returns <c>-20</c>.
	/// </remarks>
	/// <param name="lhs">The dividend.</param>
	/// <param name="rhs">The divisor.</param>
	public static T TrueModulus<T>(T lhs, T rhs) where T : IModulusOperators<T, T, T>, IAdditionOperators<T, T, T> => (lhs % rhs + rhs) % rhs;

	/// <summary>
	/// Returns the smallest of <paramref name="operand1"/>, <paramref name="operand2"/>, and <paramref name="operand3"/>.
	/// </summary>
	/// <param name="operand1">The first operand.</param>
	/// <param name="operand2">The second operand.</param>
	/// <param name="operand3">The third operand.</param>
	public static T Min<T>(T operand1, T operand2, T operand3) where T : IComparisonOperators<T, T, bool> {
		return operand1 < operand2 ? (operand1 < operand3 ? operand1 : operand3) : (operand2 < operand3 ? operand2 : operand3);
	}
	/// <summary>
	/// Returns the smallest of the given <paramref name="operands"/>.
	/// </summary>
	/// <param name="operands">The operands to compare. Must not be empty.</param>
	/// <exception cref="ArgumentException">Thrown if <paramref name="operands"/> is empty.</exception>
	public static T Min<T>(params ReadOnlySpan<T> operands) where T : IComparisonOperators<T, T, bool> {
		if (operands.Length == 0) throw new ArgumentException("Requires at least one operand.", nameof(operands));
		var result = operands[0];
		for (var i = 1; i < operands.Length; ++i) {
			if (operands[i] < result) result = operands[i];
		}
		return result;
	}

	/// <summary>
	/// Returns the largest of <paramref name="operand1"/>, <paramref name="operand2"/>, and <paramref name="operand3"/>.
	/// </summary>
	/// <param name="operand1">The first operand.</param>
	/// <param name="operand2">The second operand.</param>
	/// <param name="operand3">The third operand.</param>
	public static T Max<T>(T operand1, T operand2, T operand3) where T : IComparisonOperators<T, T, bool> {
		return operand1 > operand2 ? (operand1 > operand3 ? operand1 : operand3) : (operand2 > operand3 ? operand2 : operand3);
	}
	/// <summary>
	/// Returns the largest of the given <paramref name="operands"/>.
	/// </summary>
	/// <param name="operands">The operands to compare. Must not be empty.</param>
	/// <exception cref="ArgumentException">Thrown if <paramref name="operands"/> is empty.</exception>
	public static T Max<T>(params ReadOnlySpan<T> operands) where T : IComparisonOperators<T, T, bool> {
		if (operands.Length == 0) throw new ArgumentException("Requires at least one operand.", nameof(operands));
		var result = operands[0];
		for (var i = 1; i < operands.Length; ++i) {
			if (operands[i] > result) result = operands[i];
		}
		return result;
	}

	/// <summary>
	/// Normalizes <paramref name="v"/> to unit length, or returns <see cref="Vector4.Zero"/> if <paramref name="v"/> is a zero-length vector.
	/// </summary>
	/// <param name="v">The vector to normalize.</param>
	public static Vector4 NormalizeOrZero(Vector4 v) {
		var norm = Vector4.Normalize(v);
		return Single.IsFinite(norm.X) ? norm : Vector4.Zero;
	}

	/// <summary>
	/// Normalizes <paramref name="q"/> to unit length, or returns <see cref="Quaternion.Identity"/> if <paramref name="q"/> is a zero quaternion.
	/// </summary>
	/// <param name="q">The quaternion to normalize.</param>
	public static Quaternion NormalizeOrIdentity(Quaternion q) {
		var norm = Quaternion.Normalize(q);
		return Single.IsFinite(norm.X) ? norm : Quaternion.Identity;
	}

	/// <summary>
	/// Determines whether this value is both finite and strictly greater than zero.
	/// </summary>
	/// <remarks>
	/// Note that <c>-0f</c> returns <see langword="false"/> here (it is not strictly positive), but returns <see langword="true"/> from <see cref="IsNonNegativeAndFinite"/>.
	/// </remarks>
	/// <param name="this">The extended value.</param>
	public static bool IsPositiveAndFinite(this float @this) => Single.IsFinite(@this) && @this > 0f;
	/// <summary>
	/// Determines whether this value is both finite and greater than or equal to zero.
	/// </summary>
	/// <param name="this">The extended value.</param>
	public static bool IsNonNegativeAndFinite(this float @this) => Single.IsFinite(@this) && @this >= 0f;

	/// <summary>
	/// Returns the absolute value of <paramref name="num"/>, without the risk of overflow that <see cref="Math.Abs(int)"/>-style
	/// functions have when given the minimum representable value of <typeparamref name="T"/>.
	/// </summary>
	/// <remarks>
	/// For example, <c>SafeAbs(Int32.MinValue)</c> returns <see cref="Int32.MaxValue"/> rather than overflowing.
	/// </remarks>
	/// <param name="num">The value to take the absolute value of.</param>
	public static T SafeAbs<T>(T num) where T : IMinMaxValue<T>, ISignedNumber<T>, IBinaryInteger<T> {
		return num == T.MinValue ? T.MaxValue : T.Abs(num);
	}

	/// <summary>
	/// Maps this value from <paramref name="inputRange"/> in to the equivalent position within <paramref name="outputRange"/>.
	/// </summary>
	/// <remarks>
	/// If this value lies outside <paramref name="inputRange"/>, the result is extrapolated outside <paramref name="outputRange"/> accordingly, rather than being clamped.
	/// </remarks>
	/// <param name="this">The extended value.</param>
	/// <param name="inputRange">The range this value is currently expressed in terms of.</param>
	/// <param name="outputRange">The range to map this value in to.</param>
	public static T RemapRange<T>(this T @this, Pair<T, T> inputRange, Pair<T, T> outputRange) where T : IOrdinal<T> {
		var inputDistance = T.GetInterpolationDistance(inputRange.First, inputRange.Second, @this);
		return T.Interpolate(outputRange.First, outputRange.Second, inputDistance);
	}

	/// <summary>
	/// Extracts the translation component from <paramref name="mat"/>.
	/// </summary>
	/// <param name="mat">The matrix to extract the translation from.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vect GetTranslationFromMatrix(Matrix4x4 mat) => Vect.FromVector3(mat.Translation);

	/// <summary>
	/// Extracts the scaling component from <paramref name="mat"/>, using a best-effort fallback if the matrix can't be cleanly decomposed.
	/// </summary>
	/// <remarks>
	/// This first attempts <see cref="Matrix4x4.Decompose(Matrix4x4,out Vector3,out Quaternion,out Vector3)"/>; if that
	/// fails (which can happen for degenerate matrices, e.g. ones with a zero or near-zero scale on an axis), it falls
	/// back to measuring the length of each basis row directly, substituting <c>1f</c> for any axis whose length is
	/// zero or non-finite so the result never contains an unusable scale component.
	/// </remarks>
	/// <param name="mat">The matrix to extract the scaling from.</param>
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
	
	/// <summary>
	/// Extracts the rotation component from <paramref name="mat"/>, using a best-effort fallback if the matrix can't be cleanly decomposed.
	/// </summary>
	/// <remarks>
	/// This first attempts <see cref="Matrix4x4.Decompose(Matrix4x4,out Vector3,out Quaternion,out Vector3)"/>; if that
	/// fails, it falls back to orthogonalizing the matrix's basis rows (via Gram-Schmidt) before converting them to a
	/// quaternion, so a result is always produced even for degenerate or skewed matrices.
	/// </remarks>
	/// <param name="mat">The matrix to extract the rotation from.</param>
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

	/// <summary>
	/// Decomposes <paramref name="mat"/> in to an equivalent <see cref="Transform"/>, using a best-effort fallback if the matrix can't be cleanly decomposed.
	/// </summary>
	/// <remarks>
	/// This first attempts <see cref="Matrix4x4.Decompose(Matrix4x4,out Vector3,out Quaternion,out Vector3)"/>; if that
	/// fails, it falls back to the same best-effort logic used by <see cref="GetBestGuessScalingFromMatrix"/> and
	/// <see cref="GetBestGuessRotationFromMatrix"/>, so a result is always produced even for degenerate or skewed matrices.
	/// </remarks>
	/// <param name="mat">The matrix to decompose.</param>
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
	
	/// <summary>
	/// Inverts <paramref name="mat"/>, always returning a usable result even if the matrix is singular (non-invertible).
	/// </summary>
	/// <remarks>
	/// This first attempts a standard <see cref="Matrix4x4.Invert(Matrix4x4,out Matrix4x4)"/>; if that fails (e.g. because
	/// <paramref name="mat"/> has a zero or near-zero scale on one or more axes), it decomposes the matrix, substitutes a
	/// small non-zero scale for any degenerate axis, and inverts the corrected matrix instead. Unlike <see cref="Matrix4x4.Invert(Matrix4x4,out Matrix4x4)"/>,
	/// this method never fails outright: in the worst case it returns <see cref="Matrix4x4.Identity"/>.
	/// </remarks>
	/// <param name="mat">The matrix to invert.</param>
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

	/// <summary>
	/// Decomposes <paramref name="mat"/> in to an equivalent <see cref="Transform2D"/>, using a best-effort fallback for degenerate matrices.
	/// </summary>
	/// <remarks>
	/// This substitutes <c>1f</c> for either axis' scale if it computes as zero or non-finite, so a usable result is
	/// always produced even for degenerate matrices.
	/// </remarks>
	/// <param name="mat">The matrix to decompose.</param>
	public static Transform2D GetBestGuessTransformFromMatrix(Matrix3x2 mat) {
		var sx = MathF.Sqrt(mat.M11 * mat.M11 + mat.M12 * mat.M12);
		var sy = MathF.Sqrt(mat.M21 * mat.M21 + mat.M22 * mat.M22);

		if (!Single.IsFinite(sx) || sx == 0f) sx = 1f;
		if (!Single.IsFinite(sy) || sy == 0f) sy = 1f;

		var det = mat.M11 * mat.M22 - mat.M12 * mat.M21;
		if (det < 0f) sx = -sx;

		var angle = Angle.FromRadians(MathF.Atan2(mat.M12 / sx, mat.M11 / sx));
		return new Transform2D(new XYPair<float>(mat.M31, mat.M32), angle, new XYPair<float>(sx, sy));
	}

	/// <summary>
	/// Inverts <paramref name="mat"/>, always returning a usable result even if the matrix is singular (non-invertible).
	/// </summary>
	/// <remarks>
	/// This is the 2D equivalent of <see cref="ForceInvertMatrix(Matrix4x4)"/>: it falls back to substituting a small
	/// non-zero scale for any degenerate axis before inverting, and never fails outright, returning <see cref="Matrix3x2.Identity"/> in the worst case.
	/// </remarks>
	/// <param name="mat">The matrix to invert.</param>
	public static Matrix3x2 ForceInvertMatrix(Matrix3x2 mat) {
		if (Matrix3x2.Invert(mat, out var simpleSolution)) return simpleSolution;

		var transform = GetBestGuessTransformFromMatrix(mat);

		static float FixScalingComponent(float scalar) {
			const float MinAxisScaling = 1E-8f;
			var scalarSign = MathF.Sign(scalar);
			if (scalarSign == 0) scalarSign = 1;
			if (MathF.Abs(scalar) >= MinAxisScaling) return scalar;
			return scalarSign;
		}
		var newScaling = new XYPair<float>(
			FixScalingComponent(transform.Scaling.X),
			FixScalingComponent(transform.Scaling.Y)
		);

		var fixedTransform = new Transform2D(transform.Translation, transform.Rotation, newScaling);
		var fixedMatrix = fixedTransform.ToMatrix();

		if (Matrix3x2.Invert(fixedMatrix, out var fixedSolution)) return fixedSolution;
		return Matrix3x2.Identity;
	}
	
	/// <summary>
	/// Returns a human-readable description of this matrix, recognizing common special cases (identity, pure
	/// translation, or a 90°-multiple rotation around a cardinal axis) and falling back to a full grid of its values otherwise.
	/// </summary>
	/// <param name="this">The extended matrix.</param>
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
	
	/// <summary>
	/// Determines whether this matrix is equal to <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <remarks>
	/// This compares each of the sixteen components independently, each within <paramref name="tolerance"/>.
	/// </remarks>
	/// <param name="this">The extended matrix.</param>
	/// <param name="other">The other matrix.</param>
	/// <param name="tolerance">The tolerance value.</param>
	/// <returns>True if equal within tolerance, false if not.</returns>
	public static bool Equals(this Matrix4x4 @this, Matrix4x4 other, float tolerance) {
		for (var i = 0; i < 16; ++i) {
			if (MathF.Abs(@this[(i >> 2) & 0b11, i & 0b11] - other[(i >> 2) & 0b11, i & 0b11]) > tolerance) return false;
		}
		return true;
	}

	/// <summary>
	/// Determines whether this matrix is equal to <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <remarks>
	/// This compares each of the six components independently, each within <paramref name="tolerance"/>.
	/// </remarks>
	/// <param name="this">The extended matrix.</param>
	/// <param name="other">The other matrix.</param>
	/// <param name="tolerance">The tolerance value.</param>
	/// <returns>True if equal within tolerance, false if not.</returns>
	public static bool Equals(this Matrix3x2 @this, Matrix3x2 other, float tolerance) {
		return MathF.Abs(@this.M11 - other.M11) <= tolerance
			&& MathF.Abs(@this.M12 - other.M12) <= tolerance
			&& MathF.Abs(@this.M21 - other.M21) <= tolerance
			&& MathF.Abs(@this.M22 - other.M22) <= tolerance
			&& MathF.Abs(@this.M31 - other.M31) <= tolerance
			&& MathF.Abs(@this.M32 - other.M32) <= tolerance;
	}

	/// <summary>
	/// Returns the four values making up row <paramref name="rowIndex"/> of this matrix.
	/// </summary>
	/// <param name="this">The extended matrix.</param>
	/// <param name="rowIndex">The index of the row to retrieve. Must be between <c>0</c> and <c>3</c> inclusive.</param>
	public static Vector4 GetRow(this Matrix4x4 @this, int rowIndex) {
		return new Vector4(@this[rowIndex, 0], @this[rowIndex, 1], @this[rowIndex, 2], @this[rowIndex, 3]);
	}
	/// <summary>
	/// Returns the four values making up column <paramref name="columnIndex"/> of this matrix.
	/// </summary>
	/// <param name="this">The extended matrix.</param>
	/// <param name="columnIndex">The index of the column to retrieve. Must be between <c>0</c> and <c>3</c> inclusive.</param>
	public static Vector4 GetColumn(this Matrix4x4 @this, int columnIndex) {
		return new Vector4(@this[0, columnIndex], @this[1, columnIndex], @this[2, columnIndex], @this[3, columnIndex]);
	}
	
	/// <summary>
	/// Calculates the coordinate of <paramref name="anchor"/> + <paramref name="anchorOffset"/> within a 2D grid specified by <paramref name="gridSize"/> and <paramref name="coordinateSystemOrigin"/>.
	/// </summary>
	/// <param name="gridSize">The total size of the grid.</param>
	/// <param name="coordinateSystemOrigin">Which corner of the grid is defined as <c>&lt;0, 0&gt;</c> (or the centre if <see cref="DiagonalOrientation2D.None"/>).</param>
	/// <param name="anchor">Which corner or edge of the grid <paramref name="anchorOffset"/> is measured from (or the centre if <see cref="Orientation2D.None"/>).</param>
	/// <param name="anchorOffset">
	/// An additional offset from the given <paramref name="anchor"/>.
	/// A positive <see cref="XYPair{T}.X">X</see>/<see cref="XYPair{T}.Y">Y</see> component value moves the resulting point <i>in</i> to the grid, a negative component value <i>out</i> of the grid.
	/// Ambiguous values (e.g. a non-zero Y component for a <see cref="Orientation2D.Left">Left</see>-edge <paramref name="anchor"/>) move rightward/upward for positive X/Y and leftward/downward for negative X/Y.
	/// </param>
	public static XYPair<int> FindAnchoredPointIn2DCoordinateSystem(XYPair<int> gridSize, DiagonalOrientation2D coordinateSystemOrigin, Orientation2D anchor, XYPair<int> anchorOffset) {
		if (!Enum.IsDefined(coordinateSystemOrigin)) {
			throw new ArgumentOutOfRangeException(nameof(coordinateSystemOrigin), coordinateSystemOrigin, $"Canvas origin must be {DiagonalOrientation2D.None} (indicating the canvas centre) or one of {DiagonalOrientation2D.DownLeft}, {DiagonalOrientation2D.DownRight}, {DiagonalOrientation2D.UpLeft} or {DiagonalOrientation2D.UpRight}.");
		}

		// Step 1: Determine the coord assuming the TinyFFR convention of bottom-left being (0, 0)
		var downLeftOriginResult = new XYPair<int>(
			anchor.GetHorizontalComponent() switch {
				HorizontalOrientation2D.Right => gridSize.X - anchorOffset.X,
				HorizontalOrientation2D.Left => anchorOffset.X,
				_ => (gridSize.X / 2) + anchorOffset.X,
			},
			anchor.GetVerticalComponent() switch {
				VerticalOrientation2D.Up => gridSize.Y - anchorOffset.Y,
				VerticalOrientation2D.Down => anchorOffset.Y,
				_ => (gridSize.Y / 2) + anchorOffset.Y,
			}
		);

		// Step 2: Convert for the actually-requested origin point
		return new XYPair<int>(
			coordinateSystemOrigin.GetHorizontalComponent() switch {
				HorizontalOrientation2D.Right => gridSize.X - downLeftOriginResult.X,
				HorizontalOrientation2D.Left => downLeftOriginResult.X,
				_ => downLeftOriginResult.X - (gridSize.X / 2)
			},
			coordinateSystemOrigin.GetVerticalComponent() switch {
				VerticalOrientation2D.Up => gridSize.Y - downLeftOriginResult.Y,
				VerticalOrientation2D.Down => downLeftOriginResult.Y,
				_ => downLeftOriginResult.Y - (gridSize.Y / 2)
			}
		);
	}
	
	/// <summary>
	/// Equivalent to <see cref="FindAnchoredPointIn2DCoordinateSystem"/> but using a normalized floating-point grid size of <c>1.0 x 1.0</c> with no additional anchor offset.
	/// </summary>
	/// <param name="coordinateSystemOrigin">Which corner of the grid is defined as <c>&lt;0, 0&gt;</c> (or the centre if <see cref="DiagonalOrientation2D.None"/>).</param>
	/// <param name="anchor">Which corner or edge of the grid to compute the normalized coordinate for (or the centre if <see cref="Orientation2D.None"/>).</param>
	/// <returns>A normalized coordinate with each component in the range <c>[0, 1]</c>.</returns>
	public static XYPair<float> FindAnchorInNormalized2DCoordinateSystem(DiagonalOrientation2D coordinateSystemOrigin, Orientation2D anchor) {
		return FindAnchoredPointIn2DCoordinateSystem((2, 2), coordinateSystemOrigin, anchor, (0, 0)).Cast<float>().ScaledBy(0.5f);
	}
	
	/// <summary>
	/// Calculates the coordinate of the corner of a rectangular area (of size <paramref name="area"/>) nearest <paramref name="coordinateSystemOrigin"/>
	/// within a 2D grid specified by <paramref name="gridSize"/>,
	/// given that the area itself is anchored (via <paramref name="anchor"/> and <paramref name="anchorOffset"/>).
	/// </summary>
	/// <remarks>
	/// This is mostly useful for 2D canvas anchoring calculations.
	/// Use this once you already have an anchored point in mind (see <see cref="FindAnchoredPointIn2DCoordinateSystem"/>) and need to know where to actually start
	/// drawing/measuring a rectangular element of a known size, such that the element appears to grow away from its anchor rather than overlap it.
	/// </remarks>
	/// <param name="gridSize">The total size of the grid.</param>
	/// <param name="coordinateSystemOrigin">Which corner of the grid is defined as <c>&lt;0, 0&gt;</c> (or the centre if <see cref="DiagonalOrientation2D.None"/>).</param>
	/// <param name="anchor">Which corner or edge of the grid <paramref name="anchorOffset"/> is measured from (or the centre if <see cref="Orientation2D.None"/>).</param>
	/// <param name="anchorOffset">
	/// An additional offset from the given <paramref name="anchor"/>.
	/// A positive <see cref="XYPair{T}.X">X</see>/<see cref="XYPair{T}.Y">Y</see> component value moves the resulting point <i>in</i> to the grid, a negative component value <i>out</i> of the grid.
	/// Ambiguous values (e.g. a non-zero Y component for a <see cref="Orientation2D.Left">Left</see>-edge <paramref name="anchor"/>) move rightward/upward for positive X/Y and leftward/downward for negative X/Y.
	/// </param>
	/// <param name="area">The size of the area being positioned.</param>
	/// <returns>The coordinate of the area's corner nearest <paramref name="coordinateSystemOrigin"/>, measured from <paramref name="coordinateSystemOrigin"/>.</returns>
	public static XYPair<int> FindAnchoredAreaIn2DCoordinateSystem(XYPair<int> gridSize, DiagonalOrientation2D coordinateSystemOrigin, Orientation2D anchor, XYPair<int> anchorOffset, XYPair<int> area) {
		var anchorCoord = FindAnchoredPointIn2DCoordinateSystem(gridSize, coordinateSystemOrigin, anchor, anchorOffset);
		var anchorH = anchor.GetHorizontalComponent();
		var anchorV = anchor.GetVerticalComponent();
		var canvasH = coordinateSystemOrigin == DiagonalOrientation2D.None ? HorizontalOrientation2D.Left : coordinateSystemOrigin.GetHorizontalComponent();
		var canvasV = coordinateSystemOrigin == DiagonalOrientation2D.None ? VerticalOrientation2D.Down : coordinateSystemOrigin.GetVerticalComponent();
		
		return new XYPair<int>(
			(anchorH, canvasH) switch {
				(HorizontalOrientation2D.None, _) => anchorCoord.X - area.X / 2,
				_ when anchorH != canvasH => anchorCoord.X - area.X,
				_ => anchorCoord.X
			},
			(anchorV, canvasV) switch {
				(VerticalOrientation2D.None, _) => anchorCoord.Y - area.Y / 2,
				_ when anchorV != canvasV => anchorCoord.Y - area.Y,
				_ => anchorCoord.Y
			}
		);
	}
}