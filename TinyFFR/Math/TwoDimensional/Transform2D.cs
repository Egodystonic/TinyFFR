// Created on 2024-10-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[DebuggerDisplay("{ToStringDescriptive()}")]
public readonly partial struct Transform2D : IMathPrimitive<Transform2D>, IDescriptiveStringProvider {
	public static readonly Transform2D None = new();

	public XYPair<float> Translation { get; init; }
	public Angle Rotation { get; init; }
	public XYPair<float> Scaling { get; init; }

	public Transform2D() : this(XYPair<float>.Zero, Angle.Zero, XYPair<float>.One) { }
	public Transform2D(float translationX, float translationY) : this(new XYPair<float>(translationX, translationY), Angle.Zero, XYPair<float>.One) { }
	public Transform2D(XYPair<float>? translation = null, Angle? rotation = null, XYPair<float>? scaling = null) : this(translation ?? XYPair<float>.Zero, rotation ?? Angle.Zero, scaling ?? XYPair<float>.One) { }
	public Transform2D(XYPair<float> translation, Angle rotation, XYPair<float> scaling) {
		Translation = translation;
		Rotation = rotation;
		Scaling = scaling;
	}

	#region Factories and Conversions
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform2D FromScalingOnly(XYPair<float> scaling) => new(scaling: scaling);
	public static Transform2D FromScalingOnly(float scalar) => FromScalingOnly(new XYPair<float>(scalar));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform2D FromRotationOnly(Angle rotation) => new(rotation: rotation);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Transform2D FromTranslationOnly(XYPair<float> translation) => new(translation: translation);

	public Transform To3DTransform() => To3DTransform(new(Direction.Forward));
	public Transform To3DTransform(DimensionConverter dimensionConverter) {
		return new(
			dimensionConverter.ConvertVect(Translation),
			dimensionConverter.ZBasis % Rotation,
			dimensionConverter.ConvertVect(Scaling)
		);
	}

	public void Deconstruct(out XYPair<float> translation, out Angle rotation, out XYPair<float> scaling) {
		translation = Translation;
		rotation = Rotation;
		scaling = Scaling;
	}
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
		return new(
			XYPair<float>.Random(minInclusive.Translation, maxExclusive.Translation),
			Angle.Random(minInclusive.Rotation, maxExclusive.Rotation),
			XYPair<float>.Random(minInclusive.Scaling, maxExclusive.Scaling)
		);
	}
	#endregion

	#region Span Conversion
	public static int SerializationByteSpanLength { get; } = XYPair<float>.SerializationByteSpanLength + Angle.SerializationByteSpanLength + XYPair<float>.SerializationByteSpanLength;

	public static void SerializeToBytes(Span<byte> dest, Transform2D src) {
		XYPair<float>.SerializeToBytes(dest, src.Translation);
		dest = dest[XYPair<float>.SerializationByteSpanLength..];
		Angle.SerializeToBytes(dest, src.Rotation);
		dest = dest[Angle.SerializationByteSpanLength..];
		XYPair<float>.SerializeToBytes(dest, src.Scaling);
	}

	public static Transform2D DeserializeFromBytes(ReadOnlySpan<byte> src) {
		var location = XYPair<float>.DeserializeFromBytes(src);
		src = src[XYPair<float>.SerializationByteSpanLength..];
		var rotation = Angle.DeserializeFromBytes(src);
		src = src[Angle.SerializationByteSpanLength..];
		var scaling = XYPair<float>.DeserializeFromBytes(src);
		return new(location, rotation, scaling);
	}
	#endregion

	#region String Conversion
	public string ToStringDescriptive() {
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
	public bool Equals(Transform2D other) => Translation.Equals(other.Translation) && Rotation.Equals(other.Rotation) && Scaling.Equals(other.Scaling);
	public bool Equals(Transform2D other, float tolerance) => Translation.Equals(other.Translation, tolerance) && Rotation.Equals(other.Rotation, tolerance) && Scaling.Equals(other.Scaling, tolerance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Transform2D left, Transform2D right) => left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Transform2D left, Transform2D right) => !left.Equals(right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(object? obj) => obj is Transform2D other && Equals(other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode() => HashCode.Combine(Translation.GetHashCode(), Rotation.GetHashCode(), Scaling.GetHashCode());
	#endregion
}