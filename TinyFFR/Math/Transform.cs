// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents a transformation of any given three-dimensional entity; specifically a
/// <see cref="Scaling"/> operation, then a <see cref="Rotation"/>, and finally a <see cref="Translation"/> (i.e. move).  
/// </summary>
/// <remarks>
/// By default a Transform is represented internally in-memory as its three separate operations (S, R, T) held distinctly.
/// Using this approach allows anyone to get the initial <see cref="Scaling"/>/<see cref="Rotation"/>/<see cref="Translation"/>
/// operations back out without degradation.
/// <para>
/// In some cases it is unavoidable that a Transform must be represented internally by a <see cref="Matrix4x4"/>. When this
/// is the case, reading the <see cref="Scaling"/> and <see cref="Rotation"/> require deconstructing the matrix and may
/// return skewed results if <see cref="Scaling"/> is not <see cref="Vect.One"/>.
/// Together these values will combine to the same outcome when applied together but they may not be the same as the values
/// used to construct the Transform and/or its matrix.
/// </para>
/// <para>
/// <see cref="Translation"/> is always accurately returned regardless of internal representation. You can check the internal
/// representation with <see cref="IsInternallyRepresentedByMatrix"/>.
/// </para>
/// </remarks>
[DebuggerDisplay("{ToStringDescriptive()}")]
[StructLayout(LayoutKind.Explicit)]
public readonly partial struct Transform : IMathPrimitive<Transform>, IDescriptiveStringProvider {
	/// <summary>
	/// A transform that has no effect: <see cref="Vect.Zero"/> translation, <see cref="Rotation.None"/> rotation, and <see cref="Vect.One"/> scaling.
	/// </summary>
	public static readonly Transform None = new();

	[FieldOffset(0)]
	readonly Vect _translation;
	[FieldOffset(sizeof(float) * 4)]
	readonly Quaternion _rotation;
	[FieldOffset(sizeof(float) * 8)]
	readonly Vect _scaling;
	[FieldOffset(0)]
	readonly Matrix4x4 _matrix;

	/// <summary>
	/// Determines whether this transform is currently stored internally as a raw <see cref="Matrix4x4"/> rather than as separate <see cref="Translation"/>/<see cref="Rotation"/>/<see cref="Scaling"/> components.
	/// </summary>
	/// <remarks>
	/// See the remarks on <see cref="Transform"/> for why this distinction matters: reading <see cref="Scaling"/>
	/// or <see cref="Rotation"/> from a matrix-represented transform may return skewed results if the original scaling
	/// was not <see cref="Vect.One"/>, since those values must be reconstructed from the combined matrix.
	/// </remarks>
	public bool IsInternallyRepresentedByMatrix => _matrix.GetRow(3) != Vector4.Zero;
	/// <summary>
	/// The translation (movement) component of this transform.
	/// </summary>
	public Vect Translation {
		get => IsInternallyRepresentedByMatrix ? MathUtils.GetTranslationFromMatrix(_matrix) : _translation;
		init {
			if (IsInternallyRepresentedByMatrix) {
				_matrix.Translation = value.ToVector3();
				return;
			}

			_translation = value;
		}
	}
	/// <summary>
	/// The rotation component of this transform, as a raw <see cref="Quaternion"/>.
	/// </summary>
	public Quaternion RotationQuaternion {
		get => IsInternallyRepresentedByMatrix ? MathUtils.GetBestGuessRotationFromMatrix(_matrix) : _rotation;
		init {
			if (IsInternallyRepresentedByMatrix) {
				var t = MathUtils.GetBestGuessTransformFromMatrix(_matrix);
				_matrix = default;
				_translation = t.Translation;
				_scaling = t.Scaling;
			}

			_rotation = value;
		}
	}
	/// <summary>
	/// The rotation component of this transform.
	/// </summary>
	public Rotation Rotation {
		get => Rotation.FromQuaternionPreNormalized(RotationQuaternion);
		init => RotationQuaternion = value.ToQuaternion();
	}
	/// <summary>
	/// The scaling component of this transform.
	/// </summary>
	public Vect Scaling {
		get => IsInternallyRepresentedByMatrix ? MathUtils.GetBestGuessScalingFromMatrix(_matrix) : _scaling;
		init {
			if (IsInternallyRepresentedByMatrix) {
				var t = MathUtils.GetBestGuessTransformFromMatrix(_matrix);
				_matrix = default;
				_translation = t.Translation;
				_rotation = t.RotationQuaternion;
			}

			_scaling = value;
		}
	}

	/// <summary>
	/// Constructs a new <see cref="Transform"/> equal to <see cref="None"/>.
	/// </summary>
	public Transform() : this(Vect.Zero, Quaternion.Identity, Vect.One) { }
	/// <summary>
	/// Constructs a new <see cref="Transform"/> with only a translation component, from the given <paramref name="translationX"/>, <paramref name="translationY"/>, and <paramref name="translationZ"/> values.
	/// </summary>
	/// <param name="translationX">The X component of the translation.</param>
	/// <param name="translationY">The Y component of the translation.</param>
	/// <param name="translationZ">The Z component of the translation.</param>
	public Transform(float translationX, float translationY, float translationZ) : this(new Vect(translationX, translationY, translationZ), Quaternion.Identity, Vect.One) { }
	/// <summary>
	/// Constructs a new <see cref="Transform"/> from the given optional <paramref name="translation"/>, <paramref name="rotation"/>, and <paramref name="scaling"/> components.
	/// </summary>
	/// <param name="translation">The translation component. Defaults to <see cref="Vect.Zero"/> if omitted.</param>
	/// <param name="rotation">The rotation component. Defaults to <see cref="Rotation.None"/> if omitted.</param>
	/// <param name="scaling">The scaling component. Defaults to <see cref="Vect.One"/> if omitted.</param>
	public Transform(Vect? translation = null, Rotation? rotation = null, Vect? scaling = null) : this(translation ?? Vect.Zero, rotation?.ToQuaternion() ?? Quaternion.Identity, scaling ?? Vect.One) { }
	/// <summary>
	/// Constructs a new <see cref="Transform"/> from the given <paramref name="translation"/>, <paramref name="rotation"/>, and <paramref name="scaling"/> components.
	/// </summary>
	/// <param name="translation">The translation component.</param>
	/// <param name="rotation">The rotation component.</param>
	/// <param name="scaling">The scaling component.</param>
	public Transform(Vect translation, Rotation rotation, Vect scaling) : this(translation, rotation.ToQuaternion(), scaling) { }
	/// <summary>
	/// Constructs a new <see cref="Transform"/> from the given <paramref name="translation"/>, <paramref name="rotationQuaternion"/>, and <paramref name="scaling"/> components.
	/// </summary>
	/// <param name="translation">The translation component.</param>
	/// <param name="rotationQuaternion">The rotation component, as a raw <see cref="Quaternion"/>.</param>
	/// <param name="scaling">The scaling component.</param>
	public Transform(Vect translation, Quaternion rotationQuaternion, Vect scaling) {
		_translation = translation;
		_rotation = rotationQuaternion;
		_scaling = scaling;
	}
	/// <summary>
	/// Constructs a new <see cref="Transform"/> directly from a raw <paramref name="transformMatrix"/>.
	/// </summary>
	/// <remarks>
	/// The resultant transform is internally represented by the matrix (see <see cref="IsInternallyRepresentedByMatrix"/>);
	/// reading <see cref="Scaling"/> or <see cref="Rotation"/> from it may return skewed results if <paramref name="transformMatrix"/>
	/// encodes a non-uniform scale combined with a rotation or shear.
	/// </remarks>
	/// <param name="transformMatrix">The matrix to construct this transform from.</param>
	public Transform(Matrix4x4 transformMatrix) => _matrix = transformMatrix;

	#region Factories and Conversions
	/// <summary>
	/// Constructs a new <see cref="Transform"/> with only a scaling component; equivalent to <c>new Transform(scaling: scaling)</c>.
	/// </summary>
	/// <param name="scaling">The scaling component.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform FromScalingOnly(Vect scaling) => new(scaling: scaling);
	/// <summary>
	/// Constructs a new <see cref="Transform"/> that scales uniformly by <paramref name="scalar"/> on all three axes.
	/// </summary>
	/// <param name="scalar">The uniform scale factor.</param>
	public static Transform FromScalingOnly(float scalar) => FromScalingOnly(new Vect(scalar));

	/// <summary>
	/// Constructs a new <see cref="Transform"/> with only a rotation component; equivalent to <c>new Transform(rotation: rotation)</c>.
	/// </summary>
	/// <param name="rotation">The rotation component.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform FromRotationOnly(Rotation rotation) => new(rotation: rotation);

	/// <summary>
	/// Constructs a new <see cref="Transform"/> with only a rotation component, given as a raw <see cref="Quaternion"/>.
	/// </summary>
	/// <param name="rotationQuaternion">The rotation component.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform FromRotationOnly(Quaternion rotationQuaternion) => new(Vect.Zero, rotationQuaternion, Vect.One);

	/// <summary>
	/// Constructs a new <see cref="Transform"/> with only a translation component; equivalent to <c>new Transform(translation: translation)</c>.
	/// </summary>
	/// <param name="translation">The translation component.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform FromTranslationOnly(Vect translation) => new(translation: translation);

	/// <summary>
	/// Converts <paramref name="t"/> in place to be internally represented by a raw matrix, if it isn't already.
	/// </summary>
	/// <param name="t">The transform to coerce.</param>
	public static void CoerceToMatrixRepresentation(ref Transform t) {
		if (t.IsInternallyRepresentedByMatrix) return;
		t = new(t.ToMatrix());
	}

	/// <summary>
	/// Converts <paramref name="t"/> in place to be internally represented by separate <see cref="Translation"/>/<see cref="Rotation"/>/<see cref="Scaling"/> components, if it isn't already.
	/// </summary>
	/// <remarks>
	/// If <paramref name="t"/> was matrix-represented, this may lose fidelity: the reconstructed components may not
	/// exactly reproduce the original matrix if it encoded a non-uniform scale combined with a rotation or shear.
	/// </remarks>
	/// <param name="t">The transform to coerce.</param>
	public static void CoerceToComponentRepresentation(ref Transform t) {
		if (!t.IsInternallyRepresentedByMatrix) return;
		t = MathUtils.GetBestGuessTransformFromMatrix(t._matrix);
	}

	/// <summary>
	/// Converts this transform to a raw <see cref="Matrix4x4"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Matrix4x4 ToMatrix() {
		ToMatrix(out var result);
		return result;
	}

	/// <summary>
	/// Converts this transform to a raw <see cref="Matrix4x4"/>, writing the result to <paramref name="dest"/> instead of returning it.
	/// </summary>
	/// <param name="dest">Will be set to the matrix representation of this transform.</param>
	public void ToMatrix(out Matrix4x4 dest) {
		if (IsInternallyRepresentedByMatrix) {
			dest = _matrix;
			return;
		}
		
		var rotVect = RotationQuaternion.AsVector4();
		var rotVectSquared = rotVect * rotVect;

		var rowA = new Vector4(
			1f - 2f * rotVectSquared.Y - 2f * rotVectSquared.Z,
			2f * rotVect.X * rotVect.Y + 2f * rotVect.Z * rotVect.W,
			2f * rotVect.X * rotVect.Z - 2f * rotVect.Y * rotVect.W,
			0f
		) * Scaling.X;
		var rowB = new Vector4(
			2f * rotVect.X * rotVect.Y - 2f * rotVect.Z * rotVect.W,
			1f - 2f * rotVectSquared.X - 2f * rotVectSquared.Z,
			2f * rotVect.Y * rotVect.Z + 2f * rotVect.X * rotVect.W,
			0f
		) * Scaling.Y;
		var rowC = new Vector4(
			2f * rotVect.X * rotVect.Z + 2f * rotVect.Y * rotVect.W,
			2f * rotVect.Y * rotVect.Z - 2f * rotVect.X * rotVect.W,
			1f - 2f * rotVectSquared.X - 2f * rotVectSquared.Y,
			0f
		) * Scaling.Z;

		dest.M11 = rowA.X; dest.M12 = rowA.Y; dest.M13 = rowA.Z; dest.M14 = rowA.W;
		dest.M21 = rowB.X; dest.M22 = rowB.Y; dest.M23 = rowB.Z; dest.M24 = rowB.W;
		dest.M31 = rowC.X; dest.M32 = rowC.Y; dest.M33 = rowC.Z; dest.M34 = rowC.W;
		dest.M41 = Translation.X;
		dest.M42 = Translation.Y; 
		dest.M43 = Translation.Z;
		dest.M44 = 1f;
	}

	/// <summary>
	/// Converts this transform to its equivalent 2D representation, using the standard XY plane (i.e. as viewed from <see cref="Direction.Forward"/>).
	/// </summary>
	public Transform2D To2D() => To2D(new(Direction.Forward));
	/// <summary>
	/// Converts this transform to its equivalent 2D representation, projected using <paramref name="dimensionConverter"/>.
	/// </summary>
	/// <param name="dimensionConverter">The converter describing how to map 3D space down to the target 2D plane.</param>
	public Transform2D To2D(DimensionConverter dimensionConverter) {
		return new(
			dimensionConverter.ConvertVect(Translation),
			Rotation.AngleAroundAxis(dimensionConverter.ZBasis),
			dimensionConverter.ConvertVect(Scaling)
		);
	}

	/// <summary>
	/// Deconstructs this transform in to its individual <paramref name="translation"/>, <paramref name="rotation"/>, and <paramref name="scaling"/> components.
	/// </summary>
	/// <param name="translation">Will be set to the value of <see cref="Translation"/>.</param>
	/// <param name="rotation">Will be set to the value of <see cref="Rotation"/>.</param>
	/// <param name="scaling">Will be set to the value of <see cref="Scaling"/>.</param>
	public void Deconstruct(out Vect translation, out Rotation rotation, out Vect scaling) {
		translation = Translation;
		rotation = Rotation;
		scaling = Scaling;
	}

	/// <summary>
	/// Implicitly converts a raw <see cref="Matrix4x4"/> to a <see cref="Transform"/>; equivalent to <c>new Transform(operand)</c>.
	/// </summary>
	/// <param name="operand">The matrix to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Transform(Matrix4x4 operand) => new(operand);
	/// <summary>
	/// Implicitly converts a <see cref="Transform"/> to a raw <see cref="Matrix4x4"/>; equivalent to <c>operand.ToMatrix()</c>.
	/// </summary>
	/// <param name="operand">The transform to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Matrix4x4(Transform operand) => operand.ToMatrix();
	#endregion

	#region Random
	/// <summary>
	/// Produces a random transform: a random translation (see <see cref="Vect.Random()"/>), a random rotation (see <see cref="Rotation.Random()"/>),
	/// and a random scaling with each axis independently between <c>-1f</c> and <c>1f</c>.
	/// </summary>
	public static Transform Random() {
		return new(
			Vect.Random(),
			Rotation.Random(),
			Vect.Random(-Vect.One, Vect.One)
		);
	}

	/// <summary>
	/// Produces a random transform, with its translation, rotation, and scaling each independently randomized between
	/// the corresponding components of <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
	/// </summary>
	/// <remarks>
	/// See <see cref="Vect.Random(Vect,Vect)"/> and <see cref="Rotation.Random(Rotation,Rotation)"/> for how the
	/// translation/scaling and rotation bounds are respectively interpreted.
	/// </remarks>
	/// <param name="minInclusive">The lower bound for each component.</param>
	/// <param name="maxExclusive">The upper bound for each component.</param>
	public static Transform Random(Transform minInclusive, Transform maxExclusive) {
		CoerceToComponentRepresentation(ref minInclusive);
		CoerceToComponentRepresentation(ref maxExclusive);
		return new(
			Vect.Random(minInclusive.Translation, maxExclusive.Translation),
			Rotation.Random(minInclusive.Rotation, maxExclusive.Rotation),
			Vect.Random(minInclusive.Scaling, maxExclusive.Scaling)
		);
	}
	#endregion

	#region Span Conversion
	/// <inheritdoc />
	public static int SerializationByteSpanLength { get; } = sizeof(float) * 16;

	/// <inheritdoc />
	public static void SerializeToBytes(Span<byte> dest, Transform src) {
		for (var i = 0; i < 16; ++i) {
			BinaryPrimitives.WriteSingleLittleEndian(dest[(i * sizeof(float))..], src._matrix[(i >> 2) & 0b11, i & 0b11]);
		}
	}

	/// <inheritdoc />
	public static Transform DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new Transform(new Matrix4x4(
			BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 0)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 1)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 2)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 3)..]),
			
			BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 4)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 5)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 6)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 7)..]),
			
			BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 8)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 9)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 10)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 11)..]),
			
			BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 12)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 13)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 14)..]),
			BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 15)..])
		));
	}
	#endregion

	#region String Conversion
	/// <inheritdoc />
	public string ToStringDescriptive() {
		if (IsInternallyRepresentedByMatrix) return _matrix.ToStringDescriptive();
		
		// ReSharper disable CompareOfFloatsByEqualityOperator Explicit comparison with a representable-in-FP default is fine
		string scalingString;
		if (Scaling == None.Scaling) scalingString = PercentageUtils.ConvertFractionToPercentageString(1f, "N0", CultureInfo.CurrentCulture);
		else if (Scaling.X == Scaling.Y && Scaling.Y == Scaling.Z) scalingString = PercentageUtils.ConvertFractionToPercentageString(Scaling.X, "N0", CultureInfo.CurrentCulture);
		else {
			scalingString = $"{IVect.VectorStringPrefixChar}" +
							$"{PercentageUtils.ConvertFractionToPercentageString(Scaling.X, "N0", CultureInfo.CurrentCulture)}" +
							$"{NumberFormatInfo.CurrentInfo.NumberGroupSeparator}" +
							$"{PercentageUtils.ConvertFractionToPercentageString(Scaling.Y, "N0", CultureInfo.CurrentCulture)}" +
							$"{NumberFormatInfo.CurrentInfo.NumberGroupSeparator}" +
							$"{PercentageUtils.ConvertFractionToPercentageString(Scaling.Z, "N0", CultureInfo.CurrentCulture)}" +
							$"{IVect.VectorStringSuffixChar}";
		}
		// ReSharper restore CompareOfFloatsByEqualityOperator

		return $"{nameof(Transform)}{GeometryUtils.ParameterStartToken}" +
			   $"{nameof(Translation)}{GeometryUtils.ParameterKeyValueSeparatorToken}{Translation}" +
			   $"{GeometryUtils.ParameterSeparatorToken}" +
			   $"{nameof(Rotation)}{GeometryUtils.ParameterKeyValueSeparatorToken}{Rotation.ToStringDescriptive()}" +
			   $"{GeometryUtils.ParameterSeparatorToken}" +
			   $"{nameof(Scaling)}{GeometryUtils.ParameterKeyValueSeparatorToken}{scalingString}" +
			   $"{GeometryUtils.ParameterEndToken}";
	}

	/// <inheritdoc />
	public override string ToString() => ToString(null, null);
	/// <inheritdoc />
	public string ToString(string? format, IFormatProvider? formatProvider) {
		return GeometryUtils.StandardizedToString(format, formatProvider, nameof(Transform), (nameof(Translation), Translation), (nameof(Rotation), Rotation), (nameof(Scaling), Scaling));
	}

	/// <inheritdoc />
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		return GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(Transform), (nameof(Translation), Translation), (nameof(Rotation), Rotation), (nameof(Scaling), Scaling));
	}

	/// <inheritdoc />
	public static Transform Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	/// <inheritdoc />
	public static bool TryParse(string? s, IFormatProvider? provider, out Transform result) => TryParse(s.AsSpan(), provider, out result);

	/// <inheritdoc />
	public static Transform Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out Vect translation, out Rotation rotation, out Vect scaling);
		return new(translation, rotation, scaling);
	}
	/// <inheritdoc />
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Transform result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out Vect translation, out Rotation rotation, out Vect scaling)) return false;
		result = new(translation, rotation, scaling);
		return true;
	}
	#endregion

	#region Equality
	/// <inheritdoc />
	/// <remarks>
	/// If either transform is internally represented by a matrix (see <see cref="IsInternallyRepresentedByMatrix"/>),
	/// this compares the two transforms' full matrices instead of their individual <see cref="Translation"/>/<see cref="Rotation"/>/<see cref="Scaling"/> components.
	/// </remarks>
	public bool Equals(Transform other) {
		if (IsInternallyRepresentedByMatrix || other.IsInternallyRepresentedByMatrix) return ToMatrix().Equals(other.ToMatrix());

		return Translation.Equals(other.Translation)
			&& Rotation.Equals(other.Rotation)
			&& Scaling.Equals(other.Scaling);
	}
	/// <summary>
	/// Determines whether this transform is equal to <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <remarks>
	/// This compares the <see cref="Translation"/>, <see cref="Rotation"/>, and <see cref="Scaling"/> components
	/// independently, each within <paramref name="tolerance"/>. If either transform is internally represented by a
	/// matrix (see <see cref="IsInternallyRepresentedByMatrix"/>), this compares the two transforms' full matrices instead.
	/// </remarks>
	/// <param name="other">The other value.</param>
	/// <param name="tolerance">The tolerance value.</param>
	/// <returns>True if equal within tolerance, false if not.</returns>
	public bool Equals(Transform other, float tolerance) {
		if (IsInternallyRepresentedByMatrix || other.IsInternallyRepresentedByMatrix) return ToMatrix().Equals(other.ToMatrix(), tolerance);

		static bool CompareQuats(Quaternion a, Quaternion b, float t) {
			return MathF.Abs(a.X - b.X) <= t
				&& MathF.Abs(a.Y - b.Y) <= t
				&& MathF.Abs(a.Z - b.Z) <= t
				&& MathF.Abs(a.W - b.W) <= t;
		}

		return Translation.Equals(other.Translation, tolerance)
			   && (CompareQuats(RotationQuaternion, other.RotationQuaternion, tolerance) || CompareQuats(RotationQuaternion, other.RotationQuaternion, tolerance))
			   && Scaling.Equals(other.Scaling, tolerance);
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Transform left, Transform right) => left.Equals(right);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Transform left, Transform right) => !left.Equals(right);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => obj is Transform other && Equals(other);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() {
		var thisCopy = this;
		CoerceToComponentRepresentation(ref thisCopy);
		return HashCode.Combine(thisCopy.Translation.GetHashCode(), thisCopy.Rotation.GetHashCode(), thisCopy.Scaling.GetHashCode());
	}
	#endregion
}