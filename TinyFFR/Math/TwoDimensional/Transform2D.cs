// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents a combination of a 2D translation, rotation and scaling that can be applied to an <see cref="XYPair{T}"/>.
/// </summary>
/// <remarks>
/// By default a Transform2D is represented internally in-memory as its three separate operations (S, R, T) held distinctly.
/// Using this approach allows anyone to get the initial <see cref="Scaling"/>/<see cref="Rotation"/>/<see cref="Translation"/>
/// operations back out without degradation.
/// <para>
/// In some cases it is unavoidable that a Transform must be represented internally by a <see cref="Matrix3x2"/>. When this
/// is the case, reading the <see cref="Scaling"/> and <see cref="Rotation"/> require deconstructing the matrix and may
/// return skewed results if <see cref="Scaling"/> is not <see cref="XYPair{T}.One"/>.
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
public readonly partial struct Transform2D : IMathPrimitive<Transform2D>, IDescriptiveStringProvider {
	/// <summary>
	/// A <see cref="Transform2D"/> that has no effect (zero translation, zero rotation, and a scaling of <c>1</c> on both axes).
	/// </summary>
	public static readonly Transform2D None = new();

	[FieldOffset(0)]
	readonly XYPair<float> _translation;
	[FieldOffset(sizeof(float) * 2)]
	readonly Angle _rotation;
	[FieldOffset(sizeof(float) * 3)]
	readonly XYPair<float> _scaling;
	[FieldOffset(0)]
	readonly Matrix3x2 _matrix;
	[FieldOffset(sizeof(float) * 6)]
	readonly bool _isMatrixFlag;

	/// <summary>
	/// Whether this transform is currently stored internally as a raw <see cref="Matrix3x2"/> (<see langword="true"/>) or as separate <see cref="Translation"/>/<see cref="Rotation"/>/<see cref="Scaling"/> components (<see langword="false"/>).
	/// </summary>
	public bool IsInternallyRepresentedByMatrix => _isMatrixFlag;
	/// <summary>
	/// The translation (movement) component of this transform.
	/// </summary>
	public XYPair<float> Translation {
		get => IsInternallyRepresentedByMatrix ? new XYPair<float>(_matrix.M31, _matrix.M32) : _translation;
		init {
			if (IsInternallyRepresentedByMatrix) {
				_matrix.M31 = value.X;
				_matrix.M32 = value.Y;
				return;
			}

			_translation = value;
		}
	}
	/// <summary>
	/// The rotation component of this transform.
	/// </summary>
	/// <remarks>
	/// A positive angle turns anticlockwise, matching <see cref="XYPair{T}.PolarAngle"/>'s convention.
	/// </remarks>
	public Angle Rotation {
		get {
			if (!IsInternallyRepresentedByMatrix) return _rotation;
			var rotVectLength = MathF.Sqrt(_matrix.M11 * _matrix.M11 + _matrix.M12 * _matrix.M12);
			if (rotVectLength == 0f) return Angle.Zero;
			if (_matrix.GetDeterminant() < 0f) rotVectLength = -rotVectLength;
			return Angle.From2DPolarAngle(_matrix.M11 / rotVectLength, _matrix.M12 / rotVectLength) ?? Angle.Zero;
		}
		init {
			if (IsInternallyRepresentedByMatrix) {
				var t = MathUtils.GetBestGuessTransformFromMatrix(_matrix);
				_isMatrixFlag = false;
				_translation = t.Translation;
				_scaling = t.Scaling;
			}

			_rotation = value;
		}
	}
	/// <summary>
	/// The scaling component of this transform.
	/// </summary>
	public XYPair<float> Scaling {
		get {
			if (!IsInternallyRepresentedByMatrix) return _scaling;
			var xScaleVectLength = MathF.Sqrt(_matrix.M11 * _matrix.M11 + _matrix.M12 * _matrix.M12);
			var yScaleVectLength = MathF.Sqrt(_matrix.M21 * _matrix.M21 + _matrix.M22 * _matrix.M22);
			if (!Single.IsFinite(xScaleVectLength) || xScaleVectLength == 0f) xScaleVectLength = 1f;
			if (!Single.IsFinite(yScaleVectLength) || yScaleVectLength == 0f) yScaleVectLength = 1f;
			if (_matrix.GetDeterminant() < 0f) xScaleVectLength = -xScaleVectLength;
			return new XYPair<float>(xScaleVectLength, yScaleVectLength);
		}
		init {
			if (IsInternallyRepresentedByMatrix) {
				var t = MathUtils.GetBestGuessTransformFromMatrix(_matrix);
				_isMatrixFlag = false;
				_translation = t.Translation;
				_rotation = t.Rotation;
			}

			_scaling = value;
		}
	}

	/// <summary>
	/// Constructs a <see cref="Transform2D"/> equal to <see cref="None"/>.
	/// </summary>
	public Transform2D() : this(XYPair<float>.Zero, Angle.Zero, XYPair<float>.One) { }
	/// <summary>
	/// Constructs a new <see cref="Transform2D"/> with only a translation component.
	/// </summary>
	/// <param name="translationX">The X component of the translation.</param>
	/// <param name="translationY">The Y component of the translation.</param>
	public Transform2D(float translationX, float translationY) : this(new XYPair<float>(translationX, translationY), Angle.Zero, XYPair<float>.One) { }
	/// <summary>
	/// Constructs a new <see cref="Transform2D"/> from the given components, each defaulting to a no-op value if omitted.
	/// </summary>
	/// <param name="translation">The translation component. Defaults to <see cref="XYPair{T}.Zero"/> (no translation) if <see langword="null"/>.</param>
	/// <param name="rotation">The rotation component. Defaults to <see cref="Angle.Zero"/> (no rotation) if <see langword="null"/>.</param>
	/// <param name="scaling">The scaling component. Defaults to <see cref="XYPair{T}.One"/> (no scaling) if <see langword="null"/>.</param>
	public Transform2D(XYPair<float>? translation = null, Angle? rotation = null, XYPair<float>? scaling = null) : this(translation ?? XYPair<float>.Zero, rotation ?? Angle.Zero, scaling ?? XYPair<float>.One) { }
	/// <summary>
	/// Constructs a new <see cref="Transform2D"/> from the given components.
	/// </summary>
	/// <param name="translation">The translation component.</param>
	/// <param name="rotation">The rotation component.</param>
	/// <param name="scaling">The scaling component.</param>
	public Transform2D(XYPair<float> translation, Angle rotation, XYPair<float> scaling) {
		_translation = translation;
		_rotation = rotation;
		_scaling = scaling;
		_isMatrixFlag = false;
	}
	/// <summary>
	/// Constructs a new <see cref="Transform2D"/> directly from a transformation matrix.
	/// </summary>
	/// <param name="transformMatrix">The matrix to wrap.</param>
	public Transform2D(Matrix3x2 transformMatrix) {
		_matrix = transformMatrix;
		_isMatrixFlag = true;
	}

	#region Factories and Conversions
	/// <summary>
	/// Constructs a <see cref="Transform2D"/> with only a scaling component (no translation or rotation).
	/// </summary>
	/// <param name="scaling">The scaling component.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform2D FromScalingOnly(XYPair<float> scaling) => new(scaling: scaling);
	/// <summary>
	/// Constructs a <see cref="Transform2D"/> with only a uniform scaling component (no translation or rotation).
	/// </summary>
	/// <param name="scalar">The scale factor to apply uniformly on both axes.</param>
	public static Transform2D FromScalingOnly(float scalar) => FromScalingOnly(new XYPair<float>(scalar));

	/// <summary>
	/// Constructs a <see cref="Transform2D"/> with only a rotation component (no translation or scaling).
	/// </summary>
	/// <param name="rotation">The rotation component.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform2D FromRotationOnly(Angle rotation) => new(rotation: rotation);

	/// <summary>
	/// Constructs a <see cref="Transform2D"/> with only a translation component (no rotation or scaling).
	/// </summary>
	/// <param name="translation">The translation component.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform2D FromTranslationOnly(XYPair<float> translation) => new(translation: translation);

	/// <summary>
	/// Converts <paramref name="t"/> in-place to the matrix-backed representation (see <see cref="IsInternallyRepresentedByMatrix"/>), if it isn't already.
	/// </summary>
	/// <param name="t">The transform to convert.</param>
	public static void CoerceToMatrixRepresentation(ref Transform2D t) {
		if (t.IsInternallyRepresentedByMatrix) return;
		t = new(t.ToMatrix());
	}

	/// <summary>
	/// Converts <paramref name="t"/> in-place to the component-backed representation (see <see cref="IsInternallyRepresentedByMatrix"/>), if it isn't already.
	/// </summary>
	/// <remarks>
	/// If <paramref name="t"/> is a matrix that doesn't correspond exactly to a translation/rotation/scaling triple (for example, one with skew), the closest reasonable approximation is used instead.
	/// </remarks>
	/// <param name="t">The transform to convert.</param>
	public static void CoerceToComponentRepresentation(ref Transform2D t) {
		if (!t.IsInternallyRepresentedByMatrix) return;
		t = MathUtils.GetBestGuessTransformFromMatrix(t._matrix);
	}

	/// <summary>
	/// Converts this transform to a <see cref="Matrix3x2"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Matrix3x2 ToMatrix() {
		var result = new Matrix3x2();
		ToMatrix(ref result);
		return result;
	}

	/// <summary>
	/// Converts this transform to a <see cref="Matrix3x2"/>, writing the result into <paramref name="dest"/> rather than allocating a new value.
	/// </summary>
	/// <param name="dest">Receives the resultant matrix.</param>
	public void ToMatrix(ref Matrix3x2 dest) {
		if (IsInternallyRepresentedByMatrix) {
			dest = _matrix;
			return;
		}

		var (sin, cos) = MathF.SinCos(Rotation.Radians);
		dest.M11 = cos * Scaling.X;
		dest.M12 = sin * Scaling.X;
		dest.M21 = -sin * Scaling.Y;
		dest.M22 = cos * Scaling.Y;
		dest.M31 = Translation.X;
		dest.M32 = Translation.Y;
	}

	/// <summary>
	/// Converts this 2D transform to an equivalent 3D <see cref="Transform"/>, using <see cref="Direction.Forward"/> as the plane's normal.
	/// </summary>
	public Transform To3D() => To3D(new(Direction.Forward));
	/// <summary>
	/// Converts this 2D transform to an equivalent 3D <see cref="Transform"/>, using <paramref name="dimensionConverter"/> to map the 2D translation/scaling into 3D space.
	/// </summary>
	/// <remarks>
	/// <see cref="Rotation"/> is bridged to 3D by rotating around <paramref name="dimensionConverter"/>'s <see cref="DimensionConverter.ZBasis"/> — since a positive 2D rotation is anticlockwise when the (implicit) 2D Z axis points towards the viewer, and a positive 3D <see cref="TinyFFR.Rotation"/> is anticlockwise when its axis points towards the viewer, using <see cref="DimensionConverter.ZBasis"/> as that axis preserves the same apparent rotation.
	/// </remarks>
	/// <param name="dimensionConverter">The converter describing how the 2D plane sits within 3D space.</param>
	public Transform To3D(DimensionConverter dimensionConverter) {
		return new(
			dimensionConverter.ConvertVect(Translation, 0f),
			dimensionConverter.ZBasis % Rotation,
			dimensionConverter.ConvertVect(Scaling, 1f)
		);
	}

	/// <summary>
	/// Deconstructs this transform into its three components.
	/// </summary>
	/// <param name="translation">Receives <see cref="Translation"/>.</param>
	/// <param name="rotation">Receives <see cref="Rotation"/>.</param>
	/// <param name="scaling">Receives <see cref="Scaling"/>.</param>
	public void Deconstruct(out XYPair<float> translation, out Angle rotation, out XYPair<float> scaling) {
		translation = Translation;
		rotation = Rotation;
		scaling = Scaling;
	}

	/// <summary>
	/// Converts <paramref name="operand"/> to a <see cref="Transform2D"/>.
	/// </summary>
	/// <param name="operand">The matrix to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Transform2D(Matrix3x2 operand) => new(operand);
	/// <summary>
	/// Converts <paramref name="operand"/> to a <see cref="Matrix3x2"/>; equivalent to <see cref="ToMatrix()"/>.
	/// </summary>
	/// <param name="operand">The transform to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Matrix3x2(Transform2D operand) => operand.ToMatrix();
	#endregion

	#region Random
	/// <summary>
	/// Produces a random transform, with <see cref="Translation"/>, <see cref="Rotation"/> and <see cref="Scaling"/> all independently randomized.
	/// </summary>
	public static Transform2D Random() {
		return new(
			XYPair<float>.Random(),
			Angle.Random(),
			XYPair<float>.Random(-XYPair<float>.One, XYPair<float>.One)
		);
	}

	/// <summary>
	/// Produces a random transform, with <see cref="Translation"/>, <see cref="Rotation"/> and <see cref="Scaling"/> each independently randomized between the corresponding values of <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
	/// </summary>
	/// <param name="minInclusive">The lower bound for each component.</param>
	/// <param name="maxExclusive">The upper bound for each component.</param>
	public static Transform2D Random(Transform2D minInclusive, Transform2D maxExclusive) {
		CoerceToComponentRepresentation(ref minInclusive);
		CoerceToComponentRepresentation(ref maxExclusive);
		return new(
			XYPair<float>.Random(minInclusive.Translation, maxExclusive.Translation),
			Angle.Random(minInclusive.Rotation, maxExclusive.Rotation),
			XYPair<float>.Random(minInclusive.Scaling, maxExclusive.Scaling)
		);
	}
	#endregion

	#region Span Conversion
	/// <inheritdoc />
	public static int SerializationByteSpanLength { get; } = sizeof(float) * 6 + sizeof(bool);

	/// <inheritdoc />
	public static void SerializeToBytes(Span<byte> dest, Transform2D src) {
		BinaryPrimitives.WriteSingleLittleEndian(dest[(4 * 0)..], src._matrix.M11);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(4 * 1)..], src._matrix.M12);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(4 * 2)..], src._matrix.M21);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(4 * 3)..], src._matrix.M22);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(4 * 4)..], src._matrix.M31);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(4 * 5)..], src._matrix.M32);
		dest[(4 * 6)] = src._isMatrixFlag ? Byte.MaxValue : Byte.MinValue; 
	}

	/// <inheritdoc />
	public static Transform2D DeserializeFromBytes(ReadOnlySpan<byte> src) {
		var isMatrix = src[4 * 6] != Byte.MinValue;
		if (isMatrix) {
			return new Transform2D(new Matrix3x2(
				BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 0)..]),
				BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 1)..]),
				BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 2)..]),
				BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 3)..]),
				BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 4)..]),
				BinaryPrimitives.ReadSingleLittleEndian(src[(4 * 5)..])	
			));
		}
		else {
			return new Transform2D(
				XYPair<float>.DeserializeFromBytes(src),
				Angle.DeserializeFromBytes(src[XYPair<float>.SerializationByteSpanLength..]),
				XYPair<float>.DeserializeFromBytes(src[(XYPair<float>.SerializationByteSpanLength + Angle.SerializationByteSpanLength)..])
			);
		}
	}
	#endregion

	#region String Conversion
	/// <inheritdoc />
	public string ToStringDescriptive() {
		if (IsInternallyRepresentedByMatrix) return _matrix.ToString();

		// ReSharper disable CompareOfFloatsByEqualityOperator Explicit comparison with a representable-in-FP default is fine
		string scalingString;
		if (Scaling == None.Scaling) scalingString = PercentageUtils.ConvertFractionToPercentageString(1f, "N0", CultureInfo.CurrentCulture);
		else if (Scaling.X == Scaling.Y) scalingString = PercentageUtils.ConvertFractionToPercentageString(Scaling.X, "N0", CultureInfo.CurrentCulture);
		else {
			scalingString = $"{IVect.VectorStringPrefixChar}" +
							$"{PercentageUtils.ConvertFractionToPercentageString(Scaling.X, "N0", CultureInfo.CurrentCulture)}" +
							$"{NumberFormatInfo.CurrentInfo.NumberGroupSeparator}" +
							$"{PercentageUtils.ConvertFractionToPercentageString(Scaling.Y, "N0", CultureInfo.CurrentCulture)}" +
							$"{IVect.VectorStringSuffixChar}";
		}
		// ReSharper restore CompareOfFloatsByEqualityOperator

		return $"{nameof(Transform2D)}{GeometryUtils.ParameterStartToken}" +
			   $"{nameof(Translation)}{GeometryUtils.ParameterKeyValueSeparatorToken}{Translation}" +
			   $"{GeometryUtils.ParameterSeparatorToken}" +
			   $"{nameof(Rotation)}{GeometryUtils.ParameterKeyValueSeparatorToken}{Rotation}" +
			   $"{GeometryUtils.ParameterSeparatorToken}" +
			   $"{nameof(Scaling)}{GeometryUtils.ParameterKeyValueSeparatorToken}{scalingString}" +
			   $"{GeometryUtils.ParameterEndToken}";
	}

	/// <inheritdoc />
	public override string ToString() => ToString(null, null);
	/// <inheritdoc />
	public string ToString(string? format, IFormatProvider? formatProvider) {
		return GeometryUtils.StandardizedToString(format, formatProvider, nameof(Transform2D), (nameof(Translation), Translation), (nameof(Rotation), Rotation), (nameof(Scaling), Scaling));
	}

	/// <inheritdoc />
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		return GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(Transform2D), (nameof(Translation), Translation), (nameof(Rotation), Rotation), (nameof(Scaling), Scaling));
	}

	/// <inheritdoc />
	public static Transform2D Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	/// <inheritdoc />
	public static bool TryParse(string? s, IFormatProvider? provider, out Transform2D result) => TryParse(s.AsSpan(), provider, out result);

	/// <inheritdoc />
	public static Transform2D Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out XYPair<float> translation, out Angle rotation, out XYPair<float> scaling);
		return new(translation, rotation, scaling);
	}
	/// <inheritdoc />
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Transform2D result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out XYPair<float> translation, out Angle rotation, out XYPair<float> scaling)) return false;
		result = new(translation, rotation, scaling);
		return true;
	}
	#endregion

	#region Equality
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Transform2D other) {
		if (IsInternallyRepresentedByMatrix || other.IsInternallyRepresentedByMatrix) return ToMatrix().Equals(other.ToMatrix());

		return Translation.Equals(other.Translation)
			&& Rotation.Equals(other.Rotation)
			&& Scaling.Equals(other.Scaling);
	}
	/// <summary>
	/// Determines whether this transform is equal to <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <remarks>
	/// This compares <see cref="Translation"/>, <see cref="Rotation"/> and <see cref="Scaling"/> independently, each within <paramref name="tolerance"/>.
	/// </remarks>
	/// <param name="other">The other value.</param>
	/// <param name="tolerance">The tolerance value.</param>
	public bool Equals(Transform2D other, float tolerance) {
		if (IsInternallyRepresentedByMatrix || other.IsInternallyRepresentedByMatrix) return ToMatrix().Equals(other.ToMatrix(), tolerance);

		return Translation.Equals(other.Translation, tolerance)
			&& Rotation.Equals(other.Rotation, tolerance)
			&& Scaling.Equals(other.Scaling, tolerance);
	}
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Transform2D left, Transform2D right) => left.Equals(right);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Transform2D left, Transform2D right) => !left.Equals(right);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => obj is Transform2D other && Equals(other);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() {
		var thisCopy = this;
		CoerceToComponentRepresentation(ref thisCopy);
		return HashCode.Combine(thisCopy.Translation.GetHashCode(), thisCopy.Rotation.GetHashCode(), thisCopy.Scaling.GetHashCode());
	}
	#endregion
}
