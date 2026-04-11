// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[DebuggerDisplay("{ToStringDescriptive()}")]
[StructLayout(LayoutKind.Explicit)]
public readonly partial struct Transform2D : IMathPrimitive<Transform2D>, IDescriptiveStringProvider {
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

	public bool IsInternallyRepresentedByMatrix => _isMatrixFlag;
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

	public Transform2D() : this(XYPair<float>.Zero, Angle.Zero, XYPair<float>.One) { }
	public Transform2D(float translationX, float translationY) : this(new XYPair<float>(translationX, translationY), Angle.Zero, XYPair<float>.One) { }
	public Transform2D(XYPair<float>? translation = null, Angle? rotation = null, XYPair<float>? scaling = null) : this(translation ?? XYPair<float>.Zero, rotation ?? Angle.Zero, scaling ?? XYPair<float>.One) { }
	public Transform2D(XYPair<float> translation, Angle rotation, XYPair<float> scaling) {
		_translation = translation;
		_rotation = rotation;
		_scaling = scaling;
		_isMatrixFlag = false;
	}
	public Transform2D(Matrix3x2 transformMatrix) {
		_matrix = transformMatrix;
		_isMatrixFlag = true;
	}

	#region Factories and Conversions
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform2D FromScalingOnly(XYPair<float> scaling) => new(scaling: scaling);
	public static Transform2D FromScalingOnly(float scalar) => FromScalingOnly(new XYPair<float>(scalar));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform2D FromRotationOnly(Angle rotation) => new(rotation: rotation);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform2D FromTranslationOnly(XYPair<float> translation) => new(translation: translation);

	public static void CoerceToMatrixRepresentation(ref Transform2D t) {
		if (t.IsInternallyRepresentedByMatrix) return;
		t = new(t.ToMatrix());
	}

	public static void CoerceToComponentRepresentation(ref Transform2D t) {
		if (!t.IsInternallyRepresentedByMatrix) return;
		t = MathUtils.GetBestGuessTransformFromMatrix(t._matrix);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Matrix3x2 ToMatrix() {
		var result = new Matrix3x2();
		ToMatrix(ref result);
		return result;
	}

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

	public Transform To3D() => To3D(new(Direction.Forward));
	public Transform To3D(DimensionConverter dimensionConverter) {
		return new(
			dimensionConverter.ConvertVect(Translation, 0f),
			dimensionConverter.ZBasis % Rotation,
			dimensionConverter.ConvertVect(Scaling, 1f)
		);
	}

	public void Deconstruct(out XYPair<float> translation, out Angle rotation, out XYPair<float> scaling) {
		translation = Translation;
		rotation = Rotation;
		scaling = Scaling;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Transform2D(Matrix3x2 operand) => new(operand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Matrix3x2(Transform2D operand) => operand.ToMatrix();
	#endregion

	#region Random
	public static Transform2D Random() {
		return new(
			XYPair<float>.Random(),
			Angle.Random(),
			XYPair<float>.Random(-XYPair<float>.One, XYPair<float>.One)
		);
	}

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
	public static int SerializationByteSpanLength { get; } = sizeof(float) * 6 + sizeof(bool);

	public static void SerializeToBytes(Span<byte> dest, Transform2D src) {
		BinaryPrimitives.WriteSingleLittleEndian(dest[(4 * 0)..], src._matrix.M11);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(4 * 1)..], src._matrix.M12);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(4 * 2)..], src._matrix.M21);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(4 * 3)..], src._matrix.M22);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(4 * 4)..], src._matrix.M31);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(4 * 5)..], src._matrix.M32);
		dest[(4 * 6)] = src._isMatrixFlag ? Byte.MaxValue : Byte.MinValue; 
	}

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

	public override string ToString() => ToString(null, null);
	public string ToString(string? format, IFormatProvider? formatProvider) {
		return GeometryUtils.StandardizedToString(format, formatProvider, nameof(Transform2D), (nameof(Translation), Translation), (nameof(Rotation), Rotation), (nameof(Scaling), Scaling));
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		return GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(Transform2D), (nameof(Translation), Translation), (nameof(Rotation), Rotation), (nameof(Scaling), Scaling));
	}

	public static Transform2D Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out Transform2D result) => TryParse(s.AsSpan(), provider, out result);

	public static Transform2D Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out XYPair<float> translation, out Angle rotation, out XYPair<float> scaling);
		return new(translation, rotation, scaling);
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Transform2D result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out XYPair<float> translation, out Angle rotation, out XYPair<float> scaling)) return false;
		result = new(translation, rotation, scaling);
		return true;
	}
	#endregion

	#region Equality
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Transform2D other) {
		if (IsInternallyRepresentedByMatrix || other.IsInternallyRepresentedByMatrix) return ToMatrix().Equals(other.ToMatrix());
		
		return Translation.Equals(other.Translation) 
			&& Rotation.Equals(other.Rotation) 
			&& Scaling.Equals(other.Scaling);
	}
	public bool Equals(Transform2D other, float tolerance) {
		if (IsInternallyRepresentedByMatrix || other.IsInternallyRepresentedByMatrix) return ToMatrix().Equals(other.ToMatrix(), tolerance);
		
		return Translation.Equals(other.Translation, tolerance)
			&& Rotation.Equals(other.Rotation, tolerance)
			&& Scaling.Equals(other.Scaling, tolerance);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Transform2D left, Transform2D right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Transform2D left, Transform2D right) => !left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => obj is Transform2D other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() {
		var thisCopy = this;
		CoerceToComponentRepresentation(ref thisCopy);
		return HashCode.Combine(thisCopy.Translation.GetHashCode(), thisCopy.Rotation.GetHashCode(), thisCopy.Scaling.GetHashCode());
	}
	#endregion
}
