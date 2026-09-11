// Created on 2024-02-25 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics;
using System.Globalization;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents an offset around a sphere of a spherical co-ordinate reference frame.
/// </summary>
/// <remarks>
/// A SphericalTranslation describes an offset to be applied to any given reference frame, where a reference frame specifically means
/// "an azimuthal starting direction and a polar 'up' direction".
/// <see cref="Translate"/> takes this reference frame to produce a <see cref="Direction"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(float) * 2)]
public readonly partial struct SphericalTranslation : IMathPrimitive<SphericalTranslation> {
	/// <summary>
	/// A <see cref="SphericalTranslation"/> with both offsets at <see cref="Angle.Zero"/> (i.e. no translation).
	/// </summary>
	public static readonly SphericalTranslation ZeroZero = new(0f, 0f);
	readonly Angle _azimuthalOffset;
	readonly Angle _polarOffset;

	/// <summary>
	/// The azimuthal component of this translation: the angle rotated around the reference pole.
	/// </summary>
	/// <remarks>
	/// The angle is measured anticlockwise when the reference polar direction is pointing towards the viewer.
	/// </remarks>
	public Angle AzimuthalOffset {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _azimuthalOffset;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _azimuthalOffset = value;
	}
	/// <summary>
	/// The polar component of this translation: the angle away from the reference pole.
	/// </summary>
	public Angle PolarOffset {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _polarOffset;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		init => _polarOffset = value;
	}

	/// <summary>
	/// Constructs a new <see cref="SphericalTranslation"/> from the given <paramref name="azimuthalOffset"/> and <paramref name="polarOffset"/>.
	/// </summary>
	/// <param name="azimuthalOffset">The azimuthal component of the translation.</param>
	/// <param name="polarOffset">The polar component of the translation.</param>
	public SphericalTranslation(Angle azimuthalOffset, Angle polarOffset) {
		_azimuthalOffset = azimuthalOffset;
		_polarOffset = polarOffset;
	}

	#region Random
	/// <summary>
	/// Produces a random spherical translation, with both the azimuthal and polar offsets independently uniformly random between 0° and 360°.
	/// </summary>
	public static SphericalTranslation Random() => new(Angle.Random(Angle.Zero, Angle.FullCircle), Angle.Random(Angle.Zero, Angle.FullCircle));
	/// <summary>
	/// Produces a random spherical translation, with the azimuthal and polar offsets each independently randomized
	/// between the corresponding components of <paramref name="minInclusive"/> and <paramref name="maxExclusive"/>.
	/// </summary>
	/// <param name="minInclusive">The lower bound for each offset.</param>
	/// <param name="maxExclusive">The exclusive ceiling for each offset.</param>
	public static SphericalTranslation Random(SphericalTranslation minInclusive, SphericalTranslation maxExclusive) {
		return new(Angle.Random(minInclusive.AzimuthalOffset, maxExclusive.AzimuthalOffset), Angle.Random(minInclusive.PolarOffset, maxExclusive.PolarOffset));
	}
	#endregion

	#region Span Conversions
	/// <inheritdoc />
	public static int SerializationByteSpanLength { get; } = Angle.SerializationByteSpanLength * 2;

	/// <inheritdoc />
	public static void SerializeToBytes(Span<byte> dest, SphericalTranslation src) {
		Angle.SerializeToBytes(dest, src.AzimuthalOffset);
		Angle.SerializeToBytes(dest[Angle.SerializationByteSpanLength..], src.PolarOffset);
	}

	/// <inheritdoc />
	public static SphericalTranslation DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			Angle.DeserializeFromBytes(src),
			Angle.DeserializeFromBytes(src[Angle.SerializationByteSpanLength..])
		);
	}
	#endregion

	#region String Conversions
	/// <inheritdoc />
	public override string ToString() => ToString(null, null);
	/// <inheritdoc />
	public string ToString(string? format, IFormatProvider? formatProvider) => GeometryUtils.StandardizedToString(format, formatProvider, nameof(SphericalTranslation), (nameof(AzimuthalOffset), AzimuthalOffset), (nameof(PolarOffset), PolarOffset));
	/// <inheritdoc />
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => GeometryUtils.StandardizedTryFormat(destination, out charsWritten, format, provider, nameof(SphericalTranslation), (nameof(AzimuthalOffset), AzimuthalOffset), (nameof(PolarOffset), PolarOffset));

	/// <inheritdoc />
	public static SphericalTranslation Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	/// <inheritdoc />
	public static bool TryParse(string? s, IFormatProvider? provider, out SphericalTranslation result) => TryParse(s.AsSpan(), provider, out result);

	/// <inheritdoc />
	public static SphericalTranslation Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		GeometryUtils.StandardizedParse(s, provider, out Angle azimuthalOffset, out Angle polarOffset);
		return new(azimuthalOffset, polarOffset);
	}
	/// <inheritdoc />
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out SphericalTranslation result) {
		result = default;
		if (!GeometryUtils.StandardizedTryParse(s, provider, out Angle azimuthalOffset, out Angle polarOffset)) return false;
		result = new(azimuthalOffset, polarOffset);
		return true;
	}
	#endregion

	#region Equality
	/// <inheritdoc />
	public bool Equals(SphericalTranslation other) => _azimuthalOffset.Equals(other._azimuthalOffset) && _polarOffset.Equals(other._polarOffset);
	/// <summary>
	/// Determines whether this translation is equal to <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <remarks>
	/// This compares the raw <see cref="AzimuthalOffset"/> and <see cref="PolarOffset"/> values independently; it is not
	/// circle-wraparound-aware (for that, use <see cref="IsEquivalentWithinSphereTo(SphericalTranslation)"/> instead).
	/// </remarks>
	/// <param name="other">The other value.</param>
	/// <param name="tolerance">The tolerance value.</param>
	/// <returns>True if equal within tolerance, false if not.</returns>
	public bool Equals(SphericalTranslation other, float tolerance) => _azimuthalOffset.Equals(other._azimuthalOffset, tolerance) && _polarOffset.Equals(other._polarOffset, tolerance);
	/// <summary>
	/// Determines whether this translation is equivalent to <paramref name="other"/> when used to translate a direction around a sphere.
	/// </summary>
	/// <remarks>
	/// Like <see cref="Angle.IsEquivalentWithinCircleTo(Angle)"/>, this accounts for both offsets wrapping around a full
	/// circle (e.g. an azimuthal offset of 0° is equivalent to 360°).
	/// </remarks>
	/// <param name="other">The other translation to compare to.</param>
	public bool IsEquivalentWithinSphereTo(SphericalTranslation other) => IsEquivalentWithinSphereTo(other, 0f);
	/// <summary>
	/// Determines whether this translation is equivalent to <paramref name="other"/>, within a given <paramref name="tolerance"/>, when used to translate a direction around a sphere.
	/// </summary>
	/// <remarks>
	/// See <see cref="IsEquivalentWithinSphereTo(SphericalTranslation)"/> for a description of what "equivalent within a sphere" means.
	/// </remarks>
	/// <param name="other">The other translation to compare to.</param>
	/// <param name="tolerance">The tolerance to allow between each offset.</param>
	public bool IsEquivalentWithinSphereTo(SphericalTranslation other, Angle tolerance) => IsEquivalentWithinSphereTo(other, tolerance.Degrees);
	/// <summary>
	/// Determines whether this translation is equivalent to <paramref name="other"/>, within a given tolerance in degrees, when used to translate a direction around a sphere.
	/// </summary>
	/// <remarks>
	/// See <see cref="IsEquivalentWithinSphereTo(SphericalTranslation)"/> for a description of what "equivalent within a sphere" means.
	/// </remarks>
	/// <param name="other">The other translation to compare to.</param>
	/// <param name="toleranceDegrees">The tolerance, in degrees, to allow between each offset.</param>
	public bool IsEquivalentWithinSphereTo(SphericalTranslation other, float toleranceDegrees) {
		return _azimuthalOffset.IsEquivalentWithinCircleTo(other._azimuthalOffset, toleranceDegrees)
			&& _polarOffset.IsEquivalentWithinCircleTo(other._polarOffset, toleranceDegrees);
	}

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is SphericalTranslation other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(_azimuthalOffset, _polarOffset);
	/// <inheritdoc />
	public static bool operator ==(SphericalTranslation left, SphericalTranslation right) => left.Equals(right);
	/// <inheritdoc />
	public static bool operator !=(SphericalTranslation left, SphericalTranslation right) => !left.Equals(right);
	#endregion
}