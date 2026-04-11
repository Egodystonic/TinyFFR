// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[DebuggerDisplay("{ToStringDescriptive()}")]
[StructLayout(LayoutKind.Explicit)]
public readonly partial struct Transform : IMathPrimitive<Transform>, IDescriptiveStringProvider {
	public static readonly Transform None = new();

	[FieldOffset(0)]
	readonly Vect _translation;
	[FieldOffset(sizeof(float) * 4)]
	readonly Quaternion _rotation;
	[FieldOffset(sizeof(float) * 8)]
	readonly Vect _scaling;
	[FieldOffset(0)]
	readonly Matrix4x4 _matrix;
	
	public bool IsInternallyRepresentedByMatrix => _matrix.GetRow(3) != Vector4.Zero;
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
	public Rotation Rotation {
		get => Rotation.FromQuaternionPreNormalized(RotationQuaternion);
		init => RotationQuaternion = value.ToQuaternion();
	}
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

	public Transform() : this(Vect.Zero, Quaternion.Identity, Vect.One) { }
	public Transform(float translationX, float translationY, float translationZ) : this(new Vect(translationX, translationY, translationZ), Quaternion.Identity, Vect.One) { }
	public Transform(Vect? translation = null, Rotation? rotation = null, Vect? scaling = null) : this(translation ?? Vect.Zero, rotation?.ToQuaternion() ?? Quaternion.Identity, scaling ?? Vect.One) { }
	public Transform(Vect translation, Rotation rotation, Vect scaling) : this(translation, rotation.ToQuaternion(), scaling) { }
	public Transform(Vect translation, Quaternion rotationQuaternion, Vect scaling) {
		_translation = translation;
		_rotation = rotationQuaternion;
		_scaling = scaling;
	}
	public Transform(Matrix4x4 transformMatrix) => _matrix = transformMatrix;

	#region Factories and Conversions
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform FromScalingOnly(Vect scaling) => new(scaling: scaling);
	public static Transform FromScalingOnly(float scalar) => FromScalingOnly(new Vect(scalar));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform FromRotationOnly(Rotation rotation) => new(rotation: rotation);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform FromRotationOnly(Quaternion rotationQuaternion) => new(Vect.Zero, rotationQuaternion, Vect.One);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform FromTranslationOnly(Vect translation) => new(translation: translation);

	public static void CoerceToMatrixRepresentation(ref Transform t) {
		if (t.IsInternallyRepresentedByMatrix) return;
		t = new(t.ToMatrix());
	}
	
	public static void CoerceToComponentRepresentation(ref Transform t) {
		if (!t.IsInternallyRepresentedByMatrix) return;
		t = MathUtils.GetBestGuessTransformFromMatrix(t._matrix);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Matrix4x4 ToMatrix() {
		ToMatrix(out var result);
		return result;
	}
	
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

	public Transform2D To2D() => To2D(new(Direction.Forward));
	public Transform2D To2D(DimensionConverter dimensionConverter) {
		return new(
			dimensionConverter.ConvertVect(Translation),
			Rotation.AngleAroundAxis(dimensionConverter.ZBasis),
			dimensionConverter.ConvertVect(Scaling)
		);
	}

	public void Deconstruct(out Vect translation, out Rotation rotation, out Vect scaling) {
		translation = Translation;
		rotation = Rotation;
		scaling = Scaling;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Transform(Matrix4x4 operand) => new(operand);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Matrix4x4(Transform operand) => operand.ToMatrix();
	#endregion

	#region Random
	public static Transform Random() {
		return new(
			Vect.Random(),
			Rotation.Random(),
			Vect.Random(-Vect.One, Vect.One)
		);
	}

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
	public static int SerializationByteSpanLength { get; } = sizeof(float) * 16;

	public static void SerializeToBytes(Span<byte> dest, Transform src) {
		for (var i = 0; i < 16; ++i) {
			BinaryPrimitives.WriteSingleLittleEndian(dest[(i * sizeof(float))..], src._matrix[(i >> 2) & 0b11, i & 0b11]);
		}
	}

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

	public override string ToString() => ToString(null, null);
	public string ToString(string? format, IFormatProvider? formatProvider) {
		return GeometryUtils.StandardizedToString(format, formatProvider, nameof(Transform), (nameof(Translation), Translation), (nameof(Rotation), Rotation), (nameof(Scaling), Scaling));
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		return GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(Transform), (nameof(Translation), Translation), (nameof(Rotation), Rotation), (nameof(Scaling), Scaling));
	}

	public static Transform Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out Transform result) => TryParse(s.AsSpan(), provider, out result);

	public static Transform Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out Vect translation, out Rotation rotation, out Vect scaling);
		return new(translation, rotation, scaling);
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Transform result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out Vect translation, out Rotation rotation, out Vect scaling)) return false;
		result = new(translation, rotation, scaling);
		return true;
	}
	#endregion

	#region Equality
	public bool Equals(Transform other) {
		if (IsInternallyRepresentedByMatrix || other.IsInternallyRepresentedByMatrix) return ToMatrix().Equals(other.ToMatrix());
		
		return Translation.Equals(other.Translation)
			&& Rotation.Equals(other.Rotation)
			&& Scaling.Equals(other.Scaling);
	}
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Transform left, Transform right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Transform left, Transform right) => !left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => obj is Transform other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() {
		var thisCopy = this;
		CoerceToComponentRepresentation(ref thisCopy);
		return HashCode.Combine(thisCopy.Translation.GetHashCode(), thisCopy.Rotation.GetHashCode(), thisCopy.Scaling.GetHashCode());
	}
	#endregion
}