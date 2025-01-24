// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[DebuggerDisplay("{ToStringDescriptive()}")]
public readonly partial struct Transform : IMathPrimitive<Transform>, IDescriptiveStringProvider {
	public static readonly Transform None = new();

	public Vect Translation { get; init; }
	public Rotation Rotation { get; init; }
	public Vect Scaling { get; init; }

	public Transform() : this(Vect.Zero, Rotation.None, Vect.One) { }
	public Transform(float translationX, float translationY, float translationZ) : this(new Vect(translationX, translationY, translationZ), Rotation.None, Vect.One) { }
	public Transform(Vect? translation = null, Rotation? rotation = null, Vect? scaling = null) : this(translation ?? Vect.Zero, rotation ?? Rotation.None, scaling ?? Vect.One) { }
	public Transform(Vect translation, Rotation rotation, Vect scaling) {
		Translation = translation;
		Rotation = rotation;
		Scaling = scaling;
	}

	#region Factories and Conversions
	public Matrix4x4 ToMatrix() {
		var rotVect = Rotation.AsVector4;
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

		return new Matrix4x4(
			rowA.X, rowA.Y, rowA.Z, rowA.W,
			rowB.X, rowB.Y, rowB.Z, rowB.W,
			rowC.X, rowC.Y, rowC.Z, rowC.W,
			Translation.X, Translation.Y, Translation.Z, 1f
		);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ToMatrix(out Matrix4x4 dest) => dest = ToMatrix();

	public Transform2D ToTransform2D() => ToTransform2D(new(Direction.Forward));
	public Transform2D ToTransform2D(DimensionConverter dimensionConverter) {
		return new(
			dimensionConverter.ConvertVect(Translation),
			Rotation.AngleAroundAxis(dimensionConverter.PlaneNormal),
			dimensionConverter.ConvertVect(Scaling)
		);
	}

	public void Deconstruct(out Vect translation, out Rotation rotation, out Vect scaling) {
		translation = Translation;
		rotation = Rotation;
		scaling = Scaling;
	}

	public static implicit operator Transform(Vect translation) => new(translation);
	public static implicit operator Transform((Vect Translation, Rotation Rotation) tuple) => new(tuple.Translation, tuple.Rotation);
	public static implicit operator Transform((Vect Translation, Rotation Rotation, Vect Scaling) tuple) => new(tuple.Translation, tuple.Rotation, tuple.Scaling);
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
	public static int SerializationByteSpanLength { get; } = Vect.SerializationByteSpanLength + Rotation.SerializationByteSpanLength + Vect.SerializationByteSpanLength;

	public static void SerializeToBytes(Span<byte> dest, Transform src) {
		Vect.SerializeToBytes(dest, src.Translation);
		dest = dest[Vect.SerializationByteSpanLength..];
		Rotation.SerializeToBytes(dest, src.Rotation);
		dest = dest[Rotation.SerializationByteSpanLength..];
		Vect.SerializeToBytes(dest, src.Scaling);
	}

	public static Transform DeserializeFromBytes(ReadOnlySpan<byte> src) {
		var location = Vect.DeserializeFromBytes(src);
		src = src[Vect.SerializationByteSpanLength..];
		var rotation = Rotation.DeserializeFromBytes(src);
		src = src[Rotation.SerializationByteSpanLength..];
		var scaling = Vect.DeserializeFromBytes(src);
		return new(location, rotation, scaling);
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
	public bool Equals(Transform other) => Translation.Equals(other.Translation) && Rotation.Equals(other.Rotation) && Scaling.Equals(other.Scaling);
	public bool Equals(Transform other, float tolerance) => Translation.Equals(other.Translation, tolerance) && Rotation.Equals(other.Rotation, tolerance) && Scaling.Equals(other.Scaling, tolerance);
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