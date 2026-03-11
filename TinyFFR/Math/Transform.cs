// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[DebuggerDisplay("{ToStringDescriptive()}")]
public readonly partial struct Transform : IMathPrimitive<Transform>, IDescriptiveStringProvider {
	public static readonly Transform None = new();

	public Vect Translation { get; init; }
	public Quaternion RotationQuaternion { get; init; }
	public Rotation Rotation {
		get => Rotation.FromQuaternionPreNormalized(RotationQuaternion);
		init => RotationQuaternion = value.ToQuaternion();
	}
	public Vect Scaling { get; init; }

	public Transform() : this(Vect.Zero, Quaternion.Identity, Vect.One) { }
	public Transform(float translationX, float translationY, float translationZ) : this(new Vect(translationX, translationY, translationZ), Quaternion.Identity, Vect.One) { }
	public Transform(Vect? translation = null, Rotation? rotation = null, Vect? scaling = null) : this(translation ?? Vect.Zero, rotation?.ToQuaternion() ?? Quaternion.Identity, scaling ?? Vect.One) { }
	public Transform(Vect translation, Rotation rotation, Vect scaling) : this(translation, rotation.ToQuaternion(), scaling) { }
	public Transform(Vect translation, Quaternion rotationQuaternion, Vect scaling) {
		Translation = translation;
		RotationQuaternion = rotationQuaternion;
		Scaling = scaling;
	}

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
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Matrix4x4 ToMatrix() {
		var result = new Matrix4x4();
		ToMatrix(ref result);
		return result;
	}
	
	public void ToMatrix(ref Matrix4x4 dest) {
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
		return new(
			Vect.Random(minInclusive.Translation, maxExclusive.Translation),
			Rotation.Random(minInclusive.Rotation, maxExclusive.Rotation),
			Vect.Random(minInclusive.Scaling, maxExclusive.Scaling)
		);
	}
	#endregion

	#region Span Conversion
	public static int SerializationByteSpanLength { get; } = Vect.SerializationByteSpanLength + sizeof(float) * 4 + Vect.SerializationByteSpanLength;

	public static void SerializeToBytes(Span<byte> dest, Transform src) {
		Vect.SerializeToBytes(dest, src.Translation);
		dest = dest[Vect.SerializationByteSpanLength..];
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 0)..(sizeof(float) * 1)], src.RotationQuaternion.X);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 1)..(sizeof(float) * 2)], src.RotationQuaternion.Y);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 2)..(sizeof(float) * 3)], src.RotationQuaternion.Z);
		BinaryPrimitives.WriteSingleLittleEndian(dest[(sizeof(float) * 3)..(sizeof(float) * 4)], src.RotationQuaternion.W);
		dest = dest[(sizeof(float) * 4)..];
		Vect.SerializeToBytes(dest, src.Scaling);
	}

	public static Transform DeserializeFromBytes(ReadOnlySpan<byte> src) {
		var location = Vect.DeserializeFromBytes(src);
		src = src[Vect.SerializationByteSpanLength..];
		var x = BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 0)..(sizeof(float) * 1)]);
		var y = BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 1)..(sizeof(float) * 2)]);
		var z = BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 2)..(sizeof(float) * 3)]);
		var w = BinaryPrimitives.ReadSingleLittleEndian(src[(sizeof(float) * 3)..(sizeof(float) * 4)]);
		var rotationQuat = new Quaternion(x, y, z, w);
		src = src[(sizeof(float) * 4)..];
		var scaling = Vect.DeserializeFromBytes(src);
		return new(location, rotationQuat, scaling);
	}
	#endregion

	#region String Conversion
	public string ToStringDescriptive() {
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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Transform other) => Translation.Equals(other.Translation) && (RotationQuaternion.Equals(other.RotationQuaternion) || RotationQuaternion.Equals(-other.RotationQuaternion)) && Scaling.Equals(other.Scaling);
	public bool Equals(Transform other, float tolerance) {
		static bool CompareQuats(Quaternion a, Quaternion b, float t) {
			return MathF.Abs(a.X - b.X) <= t
				&& MathF.Abs(a.Y - b.Y) <= t
				&& MathF.Abs(a.Z - b.Z) <= t
				&& MathF.Abs(a.W - b.W) <= t;
		}

		return Translation.Equals(other.Translation, tolerance) 
			   && (CompareQuats(RotationQuaternion, other.RotationQuaternion, tolerance) || CompareQuats(RotationQuaternion, -other.RotationQuaternion, tolerance)) 
			   && Scaling.Equals(other.Scaling, tolerance);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Transform left, Transform right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Transform left, Transform right) => !left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => obj is Transform other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => HashCode.Combine(Translation.GetHashCode(), Rotation.GetHashCode(), Scaling.GetHashCode());
	#endregion
}