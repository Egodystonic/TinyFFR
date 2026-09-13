// Created on 2026-04-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Trait interface used to mark a shape as wrapped with both a translation (position) offset and a rotation.
/// </summary>
public interface ITranslatedRotatedShape : ITranslatedShape {
	/// <summary>
	/// The rotation applied to the wrapped shape.
	/// </summary>
	Rotation Rotation { get; init; }
}
/// <summary>
/// Extension of <see cref="ITranslatedRotatedShape"/> that includes a self type parameter allowing for more functional definitions.
/// </summary>
/// <typeparam name="TSelf">The type implementing this interface.</typeparam>
public interface ITranslatedRotatedShape<TSelf> : ITranslatedRotatedShape, IShape<TSelf>, ITranslatable<TSelf>, IRotatable<TSelf> where TSelf : ITranslatedRotatedShape<TSelf>;
/// <summary>
/// Extension of <see cref="ITranslatedRotatedShape{TSelf}"/> that additionally exposes the wrapped shape's type.
/// </summary>
/// <typeparam name="TSelf">The type implementing this interface.</typeparam>
/// <typeparam name="TBase">The type of the wrapped shape.</typeparam>
public interface ITranslatedRotatedShape<TSelf, TBase> : ITranslatedRotatedShape<TSelf> where TSelf : ITranslatedRotatedShape<TSelf, TBase> where TBase : IShape<TBase> {
	/// <summary>
	/// The wrapped shape, before the translation and rotation are applied.
	/// </summary>
	TBase BaseShape { get; init; }
}
/// <summary>
/// Combines <see cref="ITranslatedRotatedShape"/> and <see cref="IConvexShape"/>: a translated-and-rotated wrapper around a convex shape.
/// </summary>
public interface ITranslatedRotatedConvexShape : ITranslatedRotatedShape, IConvexShape;
/// <summary>
/// Extension of <see cref="ITranslatedRotatedConvexShape"/> that includes a self type parameter allowing for more functional definitions.
/// </summary>
/// <typeparam name="TSelf">The type implementing this interface.</typeparam>
public interface ITranslatedRotatedConvexShape<TSelf> : ITranslatedRotatedShape<TSelf>, ITranslatedRotatedConvexShape, IConvexShape<TSelf> where TSelf : ITranslatedRotatedConvexShape<TSelf>;
/// <summary>
/// Extension of <see cref="ITranslatedRotatedConvexShape{TSelf}"/> that additionally exposes the wrapped convex shape's type.
/// </summary>
/// <typeparam name="TSelf">The type implementing this interface.</typeparam>
/// <typeparam name="TBase">The type of the wrapped convex shape.</typeparam>
public interface ITranslatedRotatedConvexShape<TSelf, TBase> : ITranslatedRotatedConvexShape<TSelf>, ITranslatedRotatedShape<TSelf, TBase> where TSelf : ITranslatedRotatedConvexShape<TSelf, TBase> where TBase : IConvexShape<TBase>;

/// <summary>
/// General purpose wrapper type that takes any <see cref="IShape{T}"/> and allows it to be both translated (moved) and rotated in 3D space.
/// </summary>
/// <remarks>
/// <para>
/// All methods required by the <see cref="IShape{T}"/> interface are implemented, but recalculated when invoked to
/// account for the given <see cref="Translation"/> and <see cref="Rotation"/>. The shape is rotated first, around its
/// own local origin, and the already-rotated result is then moved by <see cref="Translation"/> — so in practice the
/// wrapped shape spins in place around its own position rather than around the world origin.
/// </para>
/// <para>
/// Note that this type can not provide implementations for methods specific to the actual shape type <typeparamref name="T"/>. For more specialized
/// implementations, see <see cref="TranslatedRotatedConvexShape{T}"/> or better yet <see cref="PositionedRotatedCuboid"/>.
/// </para>
/// </remarks>
/// <typeparam name="T">The shape type to translate and rotate.</typeparam>
/// <seealso cref="PositionedSphere"/>
/// <seealso cref="PositionedCuboid"/>
/// <seealso cref="PositionedRotatedCuboid"/>
/// <seealso cref="TranslatedShape{T}"/>
/// <seealso cref="TranslatedConvexShape{T}"/>
/// <seealso cref="TranslatedRotatedConvexShape{T}"/>
public readonly struct TranslatedRotatedShape<T> : ITranslatedRotatedShape<TranslatedRotatedShape<T>, T> where T : IShape<T> {
	const string StringShapeTransformSeparator = " rotated by ";
	const string StringPositionRotationSeparator = " @ ";
	/// <summary>
	/// The wrapped shape, before <see cref="Translation"/> and <see cref="Rotation"/> are applied.
	/// </summary>
	public T BaseShape { get; init; }
	/// <summary>
	/// The offset applied to <see cref="BaseShape"/>.
	/// </summary>
	public Vect Translation { get; init; }
	/// <summary>
	/// The rotation applied to <see cref="BaseShape"/>, around its own local origin, before <see cref="Translation"/> is applied.
	/// </summary>
	public Rotation Rotation { get; init; }

	/// <summary>
	/// Determines whether this value's <see cref="BaseShape"/>, <see cref="Translation"/>, and <see cref="Rotation"/> are all physically valid.
	/// </summary>
	public bool IsPhysicallyValid => BaseShape.IsPhysicallyValid && Translation.IsPhysicallyValid && Rotation.IsPhysicallyValid;

	/// <summary>
	/// Constructs a new <see cref="TranslatedRotatedShape{T}"/> from the given <paramref name="baseShape"/>, <paramref name="translation"/>, and <paramref name="rotation"/>.
	/// </summary>
	/// <param name="baseShape">The shape to translate and rotate.</param>
	/// <param name="translation">The offset to apply.</param>
	/// <param name="rotation">The rotation to apply.</param>
	public TranslatedRotatedShape(T baseShape, Vect translation, Rotation rotation) {
		BaseShape = baseShape;
		Translation = translation;
		Rotation = rotation;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToShapeSpace<TVal>(TVal val) where TVal : ITranslatable<TVal>, IPointRotatable<TVal> => val.MovedBy(-Translation).RotatedAroundOriginBy(Rotation.Reversed);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToWorldSpace<TVal>(TVal val) where TVal : ITranslatable<TVal>, IPointRotatable<TVal> => val.RotatedAroundOriginBy(Rotation).MovedBy(Translation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToShapeSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal>, IPointRotatable<TVal> => val?.MovedBy(-Translation).RotatedAroundOriginBy(Rotation.Reversed);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToWorldSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal>, IPointRotatable<TVal> => val?.RotatedAroundOriginBy(Rotation).MovedBy(Translation);

	#region ToString / Format / Parse
	/// <inheritdoc />
	/// <remarks>
	/// The format is <c>"{BaseShape} rotated by {Rotation} @ {Translation}"</c>.
	/// </remarks>
	public string ToString(string? format, IFormatProvider? formatProvider) {
		return BaseShape.ToString(format, formatProvider)
			+ StringShapeTransformSeparator + Rotation.ToString(format, formatProvider)
			+ StringPositionRotationSeparator + Translation.ToString(format, formatProvider);
	}
	/// <inheritdoc />
	/// <remarks>
	/// The format is <c>"{BaseShape} rotated by {Rotation} @ {Translation}"</c>.
	/// </remarks>
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		charsWritten = 0;
		
		if (!BaseShape.TryFormat(destination, out var c, format, provider)) return false;
		charsWritten += c;
		destination = destination[c..];

		if (!StringShapeTransformSeparator.TryCopyTo(destination)) return false;
		charsWritten += StringShapeTransformSeparator.Length;
		destination = destination[StringShapeTransformSeparator.Length..];

		if (!Rotation.TryFormat(destination, out c, format, provider)) return false;
		charsWritten += c;
		destination = destination[c..];

		if (!StringPositionRotationSeparator.TryCopyTo(destination)) return false;
		charsWritten += StringPositionRotationSeparator.Length;
		destination = destination[StringPositionRotationSeparator.Length..];
		
		if (!Translation.TryFormat(destination, out c, format, provider)) return false;
		charsWritten += c;
		return true;
	}
	/// <inheritdoc />
	public static TranslatedRotatedShape<T> Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	/// <inheritdoc />
	public static bool TryParse(string? s, IFormatProvider? provider, out TranslatedRotatedShape<T> result) => TryParse(s.AsSpan(), provider, out result);
	/// <inheritdoc />
	/// <exception cref="ArgumentException">Thrown if <paramref name="s"/> is not in a valid format.</exception>
	public static TranslatedRotatedShape<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		if (!TryParse(s, provider, out var result)) {
			throw new ArgumentException($"Given input string \"{s}\" does not represent a valid translated-and-rotated {typeof(T).Name}.", nameof(s));
		}
		return result;
	}
	/// <inheritdoc />
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TranslatedRotatedShape<T> result) {
		result = default;

		var shapeTransformSplitIndex = s.IndexOf(StringShapeTransformSeparator);
		if (shapeTransformSplitIndex < 0) return false;

		if (!T.TryParse(s[..shapeTransformSplitIndex], provider, out var baseShape)) return false;
		s = s[(shapeTransformSplitIndex + StringShapeTransformSeparator.Length)..];

		var positionRotationSplitIndex = s.IndexOf(StringPositionRotationSeparator);
		if (positionRotationSplitIndex < 0) return false;
		if (!Rotation.TryParse(s[..positionRotationSplitIndex], provider, out var rotation)) return false;
		if (!Vect.TryParse(s[(positionRotationSplitIndex + StringPositionRotationSeparator.Length)..], provider, out var position)) return false;

		result = new(baseShape, position, rotation);
		return true;
	}
	#endregion

	#region Byte Span Serialization / Deserialization
	/// <inheritdoc />
	public static int SerializationByteSpanLength => T.SerializationByteSpanLength + Location.SerializationByteSpanLength + Rotation.SerializationByteSpanLength;
	/// <inheritdoc />
	public static void SerializeToBytes(Span<byte> dest, TranslatedRotatedShape<T> src) {
		T.SerializeToBytes(dest, src.BaseShape);
		Vect.SerializeToBytes(dest[T.SerializationByteSpanLength..], src.Translation);
		Rotation.SerializeToBytes(dest[(T.SerializationByteSpanLength + Location.SerializationByteSpanLength)..], src.Rotation);
	}
	/// <inheritdoc />
	public static TranslatedRotatedShape<T> DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			T.DeserializeFromBytes(src),
			Vect.DeserializeFromBytes(src[T.SerializationByteSpanLength..]),
			Rotation.DeserializeFromBytes(src[(T.SerializationByteSpanLength + Location.SerializationByteSpanLength)..])
		);
	}
	#endregion

	#region Move / Scale / Rotate
	/// <inheritdoc />
	public TranslatedRotatedShape<T> MovedBy(Vect v) => new(BaseShape, Translation.Plus(v), Rotation);
	/// <summary>
	/// Moves <paramref name="left"/> by <paramref name="right"/>; equivalent to <c>left.MovedBy(right)</c>.
	/// </summary>
	/// <param name="left">The shape to move.</param>
	/// <param name="right">The vector to move by.</param>
	public static TranslatedRotatedShape<T> operator +(TranslatedRotatedShape<T> left, Vect right) => new(left.BaseShape, left.Translation + right, left.Rotation);
	/// <summary>
	/// Moves <paramref name="left"/> in the direction opposite to <paramref name="right"/>; equivalent to <c>left.MovedBy(-right)</c>.
	/// </summary>
	/// <param name="left">The shape to move.</param>
	/// <param name="right">The vector to move by, in reverse.</param>
	public static TranslatedRotatedShape<T> operator -(TranslatedRotatedShape<T> left, Vect right) => new(left.BaseShape, left.Translation - right, left.Rotation);
	/// <summary>
	/// Moves <paramref name="right"/> by <paramref name="left"/>; equivalent to <c>right.MovedBy(left)</c>.
	/// </summary>
	/// <param name="left">The vector to move by.</param>
	/// <param name="right">The shape to move.</param>
	public static TranslatedRotatedShape<T> operator +(Vect left, TranslatedRotatedShape<T> right) => new(right.BaseShape, right.Translation + left, right.Rotation);

	/// <summary>
	/// Multiplies <paramref name="left"/> by <paramref name="right"/>; equivalent to <c>left.ScaledBy(right)</c>.
	/// </summary>
	/// <param name="left">The shape to scale.</param>
	/// <param name="right">The scale factor.</param>
	public static TranslatedRotatedShape<T> operator *(TranslatedRotatedShape<T> left, float right) => new(left.BaseShape * right, left.Translation, left.Rotation);
	/// <summary>
	/// Divides <paramref name="left"/> by <paramref name="right"/>; equivalent to <c>left.ScaledBy(1f / right)</c>.
	/// </summary>
	/// <param name="left">The shape to scale.</param>
	/// <param name="right">The divisor.</param>
	public static TranslatedRotatedShape<T> operator /(TranslatedRotatedShape<T> left, float right) => new(left.BaseShape / right, left.Translation, left.Rotation);
	/// <summary>
	/// Multiplies <paramref name="right"/> by <paramref name="left"/>; equivalent to <c>right.ScaledBy(left)</c>.
	/// </summary>
	/// <param name="left">The scale factor.</param>
	/// <param name="right">The shape to scale.</param>
	public static TranslatedRotatedShape<T> operator *(float left, TranslatedRotatedShape<T> right) => new(left * right.BaseShape, right.Translation, right.Rotation);
	/// <summary>
	/// Returns this value with <see cref="BaseShape"/> scaled by <paramref name="scalar"/>. <see cref="Translation"/> and <see cref="Rotation"/> are left unchanged.
	/// </summary>
	/// <param name="scalar">The scale factor.</param>
	public TranslatedRotatedShape<T> ScaledBy(float scalar) => new(BaseShape * scalar, Translation, Rotation);

	/// <summary>
	/// Returns <paramref name="left"/> with <paramref name="right"/> combined in to its <see cref="Rotation"/>; equivalent to <c>left.RotatedBy(right)</c>.
	/// </summary>
	/// <param name="left">The shape to rotate.</param>
	/// <param name="right">The rotation to apply.</param>
	public static TranslatedRotatedShape<T> operator *(TranslatedRotatedShape<T> left, Rotation right) => new(left.BaseShape, left.Translation, left.Rotation + right);
	/// <summary>
	/// Returns <paramref name="right"/> with <paramref name="left"/> combined in to its <see cref="Rotation"/>; equivalent to <c>right.RotatedBy(left)</c>.
	/// </summary>
	/// <param name="left">The rotation to apply.</param>
	/// <param name="right">The shape to rotate.</param>
	public static TranslatedRotatedShape<T> operator *(Rotation left, TranslatedRotatedShape<T> right) => new(right.BaseShape, right.Translation, right.Rotation + left);
	/// <summary>
	/// Returns this value with <paramref name="rot"/> combined in to its <see cref="Rotation"/>, applied after the existing rotation.
	/// </summary>
	/// <param name="rot">The rotation to combine.</param>
	public TranslatedRotatedShape<T> RotatedBy(Rotation rot) => new(BaseShape, Translation, Rotation + rot);
	/// <summary>
	/// Returns this value with <paramref name="rotQuat"/> combined in to its <see cref="Rotation"/>, applied after the existing rotation.
	/// </summary>
	/// <param name="rotQuat">The rotation, as a raw <see cref="Quaternion"/>, to combine.</param>
	public TranslatedRotatedShape<T> RotatedBy(Quaternion rotQuat) => new(BaseShape, Translation, Rotation.CombinedAndNormalizedWith(rotQuat));
	#endregion

	#region Equality
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is TranslatedRotatedShape<T> other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(BaseShape, Translation, Rotation);
	/// <inheritdoc />
	public bool Equals(TranslatedRotatedShape<T> other) => BaseShape.Equals(other.BaseShape) && Translation.Equals(other.Translation) && Rotation.Equals(other.Rotation);
	/// <summary>
	/// Determines whether this value is equal to <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <remarks>
	/// This compares <see cref="BaseShape"/>, <see cref="Translation"/>, and <see cref="Rotation"/> independently, each within <paramref name="tolerance"/>.
	/// </remarks>
	/// <param name="other">The other value.</param>
	/// <param name="tolerance">The tolerance value.</param>
	/// <returns>True if equal within tolerance, false if not.</returns>
	public bool Equals(TranslatedRotatedShape<T> other, float tolerance) => BaseShape.Equals(other.BaseShape, tolerance) && Translation.Equals(other.Translation, tolerance) && Rotation.Equals(other.Rotation, tolerance);
	/// <inheritdoc />
	public static bool operator ==(TranslatedRotatedShape<T> left, TranslatedRotatedShape<T> right) => left.Equals(right);
	/// <inheritdoc />
	public static bool operator !=(TranslatedRotatedShape<T> left, TranslatedRotatedShape<T> right) => !left.Equals(right);
	#endregion

	#region Random / Interp / Clamp
	/// <inheritdoc />
	public static TranslatedRotatedShape<T> Random() => new(T.Random(), Vect.Random(), Rotation.Random());
	/// <inheritdoc />
	public static TranslatedRotatedShape<T> Random(TranslatedRotatedShape<T> minInclusive, TranslatedRotatedShape<T> maxExclusive) {
		return new(
			T.Random(minInclusive.BaseShape, maxExclusive.BaseShape),
			Vect.Random(minInclusive.Translation, maxExclusive.Translation),
			Rotation.Random(minInclusive.Rotation, maxExclusive.Rotation)
		);
	}
	/// <inheritdoc />
	public static TranslatedRotatedShape<T> Interpolate(TranslatedRotatedShape<T> start, TranslatedRotatedShape<T> end, float distance) {
		return new(
			T.Interpolate(start.BaseShape, end.BaseShape, distance),
			Vect.Interpolate(start.Translation, end.Translation, distance),
			Rotation.Interpolate(start.Rotation, end.Rotation, distance)
		);
	}
	/// <inheritdoc />
	public TranslatedRotatedShape<T> Clamp(TranslatedRotatedShape<T> min, TranslatedRotatedShape<T> max) {
		return new(
			BaseShape.Clamp(min.BaseShape, max.BaseShape),
			Translation.Clamp(min.Translation, max.Translation),
			Rotation.Clamp(min.Rotation, max.Rotation)
		);
	}
	#endregion
}

/// <summary>
/// General purpose wrapper type that takes any <see cref="IConvexShape{T}"/> and allows it to be both translated (moved) and rotated in 3D space.
/// </summary>
/// <remarks>
/// <para>
/// This is the convex-shape-constrained counterpart to <see cref="TranslatedRotatedShape{T}"/>: because <typeparamref name="T"/>
/// is guaranteed to be an <see cref="IConvexShape{T}"/>, this type additionally implements the full <see cref="IConvexShape"/>
/// surface (closest-point, distance, intersection, and reflection queries), recalculated on every call to account for <see cref="Translation"/> and <see cref="Rotation"/>.
/// </para>
/// <para>
/// For more specialized implementations with concrete shape-specific members, see <see cref="PositionedRotatedCuboid"/>.
/// </para>
/// </remarks>
/// <typeparam name="T">The convex shape type to translate and rotate.</typeparam>
/// <seealso cref="PositionedSphere"/>
/// <seealso cref="PositionedCuboid"/>
/// <seealso cref="PositionedRotatedCuboid"/>
/// <seealso cref="TranslatedShape{T}"/>
/// <seealso cref="TranslatedConvexShape{T}"/>
/// <seealso cref="TranslatedRotatedShape{T}"/>
public readonly struct TranslatedRotatedConvexShape<T> : ITranslatedRotatedConvexShape<TranslatedRotatedConvexShape<T>, T> where T : IConvexShape<T> {
	/// <summary>
	/// The wrapped shape, before <see cref="Translation"/> and <see cref="Rotation"/> are applied.
	/// </summary>
	public T BaseShape { get; init; }
	/// <summary>
	/// The offset applied to <see cref="BaseShape"/>.
	/// </summary>
	public Vect Translation { get; init; }
	/// <summary>
	/// The rotation applied to <see cref="BaseShape"/>, around its own local origin, before <see cref="Translation"/> is applied.
	/// </summary>
	public Rotation Rotation { get; init; }

	/// <summary>
	/// Constructs a new <see cref="TranslatedRotatedConvexShape{T}"/> from the given <paramref name="baseShape"/>, <paramref name="translation"/>, and <paramref name="rotation"/>.
	/// </summary>
	/// <param name="baseShape">The shape to translate and rotate.</param>
	/// <param name="translation">The offset to apply.</param>
	/// <param name="rotation">The rotation to apply.</param>
	public TranslatedRotatedConvexShape(T baseShape, Vect translation, Rotation rotation) {
		BaseShape = baseShape;
		Translation = translation;
		Rotation = rotation;
	}

	/// <summary>
	/// Implicitly converts this value to the non-convex-constrained <see cref="TranslatedRotatedShape{T}"/>.
	/// </summary>
	/// <param name="operand">The value to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedRotatedShape<T>(TranslatedRotatedConvexShape<T> operand) => new(operand.BaseShape, operand.Translation, operand.Rotation);
	/// <summary>
	/// Implicitly converts <paramref name="operand"/> to a <see cref="TranslatedRotatedConvexShape{T}"/>.
	/// </summary>
	/// <param name="operand">The value to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedRotatedConvexShape<T>(TranslatedRotatedShape<T> operand) => new(operand.BaseShape, operand.Translation, operand.Rotation);

	/// <summary>
	/// Converts this value to a <see cref="TranslatedShape{T}"/>, discarding its <see cref="Rotation"/>.
	/// </summary>
	/// <param name="operand">The value to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator TranslatedShape<T>(TranslatedRotatedConvexShape<T> operand) => new(operand.BaseShape, operand.Translation);
	/// <summary>
	/// Converts this value to a <see cref="TranslatedConvexShape{T}"/>, discarding its <see cref="Rotation"/>.
	/// </summary>
	/// <param name="operand">The value to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator TranslatedConvexShape<T>(TranslatedRotatedConvexShape<T> operand) => new(operand.BaseShape, operand.Translation);

	/// <summary>
	/// Converts <paramref name="operand"/> to a <see cref="TranslatedRotatedConvexShape{T}"/>, with <see cref="Rotation"/> set to <see cref="Egodystonic.TinyFFR.Rotation.None"/>.
	/// </summary>
	/// <param name="operand">The value to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator TranslatedRotatedConvexShape<T>(TranslatedShape<T> operand) => new(operand.BaseShape, operand.Translation, Rotation.None);
	/// <summary>
	/// Converts <paramref name="operand"/> to a <see cref="TranslatedRotatedConvexShape{T}"/>, with <see cref="Rotation"/> set to <see cref="Egodystonic.TinyFFR.Rotation.None"/>.
	/// </summary>
	/// <param name="operand">The value to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator TranslatedRotatedConvexShape<T>(TranslatedConvexShape<T> operand) => new(operand.BaseShape, operand.Translation, Rotation.None);

	#region Deferred Members
	/// <summary>
	/// Determines whether this value's <see cref="BaseShape"/>, <see cref="Translation"/>, and <see cref="Rotation"/> are all physically valid.
	/// </summary>
	public bool IsPhysicallyValid {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ((TranslatedRotatedShape<T>) this).IsPhysicallyValid;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToShapeSpace<TVal>(TVal val) where TVal : ITranslatable<TVal>, IPointRotatable<TVal> => ((TranslatedRotatedShape<T>) this).TransformToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToWorldSpace<TVal>(TVal val) where TVal : ITranslatable<TVal>, IPointRotatable<TVal> => ((TranslatedRotatedShape<T>) this).TransformToWorldSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToShapeSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal>, IPointRotatable<TVal> => ((TranslatedRotatedShape<T>) this).TransformToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToWorldSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal>, IPointRotatable<TVal> => ((TranslatedRotatedShape<T>) this).TransformToWorldSpace(val);
	/// <inheritdoc />
	/// <remarks>
	/// The format is <c>"{BaseShape} rotated by {Rotation} @ {Translation}"</c>.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ToString(string? format, IFormatProvider? formatProvider) => ((TranslatedRotatedShape<T>) this).ToString(format, formatProvider);
	/// <inheritdoc />
	/// <remarks>
	/// The format is <c>"{BaseShape} rotated by {Rotation} @ {Translation}"</c>.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => ((TranslatedRotatedShape<T>) this).TryFormat(destination, out charsWritten, format, provider);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> Parse(string s, IFormatProvider? provider) => TranslatedRotatedShape<T>.Parse(s, provider);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(string? s, IFormatProvider? provider, out TranslatedRotatedConvexShape<T> result) {
		var returnValue = TranslatedRotatedShape<T>.TryParse(s, provider, out var r);
		result = r;
		return returnValue;
	}
	/// <inheritdoc />
	/// <exception cref="ArgumentException">Thrown if <paramref name="s"/> is not in a valid format.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => TranslatedRotatedShape<T>.Parse(s, provider);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TranslatedRotatedConvexShape<T> result) {
		var returnValue = TranslatedRotatedShape<T>.TryParse(s, provider, out var r);
		result = r;
		return returnValue;
	}
	/// <inheritdoc />
	public static int SerializationByteSpanLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TranslatedRotatedShape<T>.SerializationByteSpanLength;
	}
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SerializeToBytes(Span<byte> dest, TranslatedRotatedConvexShape<T> src) => TranslatedRotatedShape<T>.SerializeToBytes(dest, src);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> DeserializeFromBytes(ReadOnlySpan<byte> src) => TranslatedRotatedShape<T>.DeserializeFromBytes(src);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TranslatedRotatedConvexShape<T> MovedBy(Vect v) => ((TranslatedRotatedShape<T>) this).MovedBy(v);
	/// <summary>
	/// Moves <paramref name="left"/> by <paramref name="right"/>; equivalent to <c>left.MovedBy(right)</c>.
	/// </summary>
	/// <param name="left">The shape to move.</param>
	/// <param name="right">The vector to move by.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> operator +(TranslatedRotatedConvexShape<T> left, Vect right) => ((TranslatedRotatedShape<T>) left) + right;
	/// <summary>
	/// Moves <paramref name="left"/> in the direction opposite to <paramref name="right"/>; equivalent to <c>left.MovedBy(-right)</c>.
	/// </summary>
	/// <param name="left">The shape to move.</param>
	/// <param name="right">The vector to move by, in reverse.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> operator -(TranslatedRotatedConvexShape<T> left, Vect right) => ((TranslatedRotatedShape<T>) left) - right;
	/// <summary>
	/// Moves <paramref name="right"/> by <paramref name="left"/>; equivalent to <c>right.MovedBy(left)</c>.
	/// </summary>
	/// <param name="left">The vector to move by.</param>
	/// <param name="right">The shape to move.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> operator +(Vect left, TranslatedRotatedConvexShape<T> right) => left + ((TranslatedRotatedShape<T>) right);
	/// <summary>
	/// Multiplies <paramref name="left"/> by <paramref name="right"/>; equivalent to <c>left.ScaledBy(right)</c>.
	/// </summary>
	/// <param name="left">The shape to scale.</param>
	/// <param name="right">The scale factor.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> operator *(TranslatedRotatedConvexShape<T> left, float right) => ((TranslatedRotatedShape<T>) left) * right;
	/// <summary>
	/// Divides <paramref name="left"/> by <paramref name="right"/>; equivalent to <c>left.ScaledBy(1f / right)</c>.
	/// </summary>
	/// <param name="left">The shape to scale.</param>
	/// <param name="right">The divisor.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> operator /(TranslatedRotatedConvexShape<T> left, float right) => ((TranslatedRotatedShape<T>) left) / right;
	/// <summary>
	/// Multiplies <paramref name="right"/> by <paramref name="left"/>; equivalent to <c>right.ScaledBy(left)</c>.
	/// </summary>
	/// <param name="left">The scale factor.</param>
	/// <param name="right">The shape to scale.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> operator *(float left, TranslatedRotatedConvexShape<T> right) => ((TranslatedRotatedShape<T>) right) * left;
	/// <summary>
	/// Returns this value with <see cref="BaseShape"/> scaled by <paramref name="scalar"/>. <see cref="Translation"/> and <see cref="Rotation"/> are left unchanged.
	/// </summary>
	/// <param name="scalar">The scale factor.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TranslatedRotatedConvexShape<T> ScaledBy(float scalar) => ((TranslatedRotatedShape<T>) this).ScaledBy(scalar);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> Random() => TranslatedRotatedShape<T>.Random();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> Random(TranslatedRotatedConvexShape<T> minInclusive, TranslatedRotatedConvexShape<T> maxExclusive) => TranslatedRotatedShape<T>.Random(minInclusive, maxExclusive);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> Interpolate(TranslatedRotatedConvexShape<T> start, TranslatedRotatedConvexShape<T> end, float distance) => TranslatedRotatedShape<T>.Interpolate(start, end, distance);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TranslatedRotatedConvexShape<T> Clamp(TranslatedRotatedConvexShape<T> min, TranslatedRotatedConvexShape<T> max) => ((TranslatedRotatedShape<T>) this).Clamp(min, max);
	/// <summary>
	/// Returns <paramref name="left"/> with <paramref name="right"/> combined in to its <see cref="Rotation"/>; equivalent to <c>left.RotatedBy(right)</c>.
	/// </summary>
	/// <param name="left">The shape to rotate.</param>
	/// <param name="right">The rotation to apply.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> operator *(TranslatedRotatedConvexShape<T> left, Rotation right) => ((TranslatedRotatedShape<T>) left) * right;
	/// <summary>
	/// Returns <paramref name="right"/> with <paramref name="left"/> combined in to its <see cref="Rotation"/>; equivalent to <c>right.RotatedBy(left)</c>.
	/// </summary>
	/// <param name="left">The rotation to apply.</param>
	/// <param name="right">The shape to rotate.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> operator *(Rotation left, TranslatedRotatedConvexShape<T> right) => left * ((TranslatedRotatedShape<T>) right);
	/// <summary>
	/// Returns this value with <paramref name="rot"/> combined in to its <see cref="Rotation"/>, applied after the existing rotation.
	/// </summary>
	/// <param name="rot">The rotation to combine.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TranslatedRotatedConvexShape<T> RotatedBy(Rotation rot) => ((TranslatedRotatedShape<T>) this).RotatedBy(rot);
	/// <summary>
	/// Returns this value with <paramref name="rotQuat"/> combined in to its <see cref="Rotation"/>, applied after the existing rotation.
	/// </summary>
	/// <param name="rotQuat">The rotation, as a raw <see cref="Quaternion"/>, to combine.</param>
	public TranslatedRotatedConvexShape<T> RotatedBy(Quaternion rotQuat) => ((TranslatedRotatedShape<T>) this).RotatedBy(rotQuat);
	#endregion

	#region Equality
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is TranslatedRotatedConvexShape<T> other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(BaseShape, Translation, Rotation);
	/// <inheritdoc />
	public bool Equals(TranslatedRotatedConvexShape<T> other) => BaseShape.Equals(other.BaseShape) && Translation.Equals(other.Translation) && Rotation.Equals(other.Rotation);
	/// <summary>
	/// Determines whether this value is equal to <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <remarks>
	/// This compares <see cref="BaseShape"/>, <see cref="Translation"/>, and <see cref="Rotation"/> independently, each within <paramref name="tolerance"/>.
	/// </remarks>
	/// <param name="other">The other value.</param>
	/// <param name="tolerance">The tolerance value.</param>
	/// <returns>True if equal within tolerance, false if not.</returns>
	public bool Equals(TranslatedRotatedConvexShape<T> other, float tolerance) => BaseShape.Equals(other.BaseShape, tolerance) && Translation.Equals(other.Translation, tolerance) && Rotation.Equals(other.Rotation, tolerance);
	/// <inheritdoc />
	public static bool operator ==(TranslatedRotatedConvexShape<T> left, TranslatedRotatedConvexShape<T> right) => left.Equals(right);
	/// <inheritdoc />
	public static bool operator !=(TranslatedRotatedConvexShape<T> left, TranslatedRotatedConvexShape<T> right) => !left.Equals(right);
	#endregion

	/// <inheritdoc />
	public Location PointClosestTo(Location location) => TransformToWorldSpace(BaseShape.PointClosestTo(TransformToShapeSpace(location)));
	/// <inheritdoc />
	public float DistanceFrom(Location location) => BaseShape.DistanceFrom(TransformToShapeSpace(location));
	/// <inheritdoc />
	public float DistanceSquaredFrom(Location location) => BaseShape.DistanceSquaredFrom(TransformToShapeSpace(location));
	/// <inheritdoc />
	public bool Contains(Location location) => BaseShape.Contains(TransformToShapeSpace(location));

	/// <inheritdoc />
	public Ray? ReflectionOf(Ray ray) => TransformToWorldSpace(BaseShape.ReflectionOf(TransformToShapeSpace(ray)));
	/// <inheritdoc />
	public Ray FastReflectionOf(Ray ray) => TransformToWorldSpace(BaseShape.FastReflectionOf(TransformToShapeSpace(ray)));
	/// <inheritdoc />
	public Angle? IncidentAngleWith(Ray ray) => BaseShape.IncidentAngleWith(TransformToShapeSpace(ray));
	/// <inheritdoc />
	public Angle FastIncidentAngleWith(Ray ray) => BaseShape.FastIncidentAngleWith(TransformToShapeSpace(ray));
	/// <inheritdoc />
	public BoundedRay? ReflectionOf(BoundedRay ray) => TransformToWorldSpace(BaseShape.ReflectionOf(TransformToShapeSpace(ray)));
	/// <inheritdoc />
	public BoundedRay FastReflectionOf(BoundedRay ray) => TransformToWorldSpace(BaseShape.FastReflectionOf(TransformToShapeSpace(ray)));
	/// <inheritdoc />
	public Angle? IncidentAngleWith(BoundedRay ray) => BaseShape.IncidentAngleWith(TransformToShapeSpace(ray));
	/// <inheritdoc />
	public Angle FastIncidentAngleWith(BoundedRay ray) => BaseShape.FastIncidentAngleWith(TransformToShapeSpace(ray));

	/// <inheritdoc />
	public Location ClosestPointOn(Line line) => TransformToWorldSpace(BaseShape.ClosestPointOn(TransformToShapeSpace(line)));
	/// <inheritdoc />
	public Location ClosestPointOn(Ray ray) => TransformToWorldSpace(BaseShape.ClosestPointOn(TransformToShapeSpace(ray)));
	/// <inheritdoc />
	public Location ClosestPointOn(BoundedRay ray) => TransformToWorldSpace(BaseShape.ClosestPointOn(TransformToShapeSpace(ray)));
	/// <inheritdoc />
	public Location PointClosestTo(Line line) => TransformToWorldSpace(BaseShape.PointClosestTo(TransformToShapeSpace(line)));
	/// <inheritdoc />
	public Location PointClosestTo(Ray ray) => TransformToWorldSpace(BaseShape.PointClosestTo(TransformToShapeSpace(ray)));
	/// <inheritdoc />
	public Location PointClosestTo(BoundedRay ray) => TransformToWorldSpace(BaseShape.PointClosestTo(TransformToShapeSpace(ray)));
	/// <inheritdoc />
	public float DistanceFrom(Line line) => BaseShape.DistanceFrom(TransformToShapeSpace(line));
	/// <inheritdoc />
	public float DistanceSquaredFrom(Line line) => BaseShape.DistanceSquaredFrom(TransformToShapeSpace(line));
	/// <inheritdoc />
	public float DistanceFrom(Ray ray) => BaseShape.DistanceFrom(TransformToShapeSpace(ray));
	/// <inheritdoc />
	public float DistanceSquaredFrom(Ray ray) => BaseShape.DistanceSquaredFrom(TransformToShapeSpace(ray));
	/// <inheritdoc />
	public float DistanceFrom(BoundedRay ray) => BaseShape.DistanceFrom(TransformToShapeSpace(ray));
	/// <inheritdoc />
	public float DistanceSquaredFrom(BoundedRay ray) => BaseShape.DistanceSquaredFrom(TransformToShapeSpace(ray));
	/// <inheritdoc />
	public bool Contains(BoundedRay ray) => BaseShape.Contains(TransformToShapeSpace(ray));

	/// <inheritdoc />
	public bool IsIntersectedBy(Line line) => BaseShape.IsIntersectedBy(TransformToShapeSpace(line));
	/// <inheritdoc />
	public bool IsIntersectedBy(Ray ray) => BaseShape.IsIntersectedBy(TransformToShapeSpace(ray));
	/// <inheritdoc />
	public bool IsIntersectedBy(BoundedRay ray) => BaseShape.IsIntersectedBy(TransformToShapeSpace(ray));
	/// <inheritdoc />
	public ConvexShapeLineIntersection? IntersectionWith(Line line) {
		var shapeSpaceResult = BaseShape.IntersectionWith(TransformToShapeSpace(line));
		return shapeSpaceResult == null
			? null
			: new(TransformToWorldSpace(shapeSpaceResult.Value.First), TransformToWorldSpace(shapeSpaceResult.Value.Second));
	}
	/// <inheritdoc />
	public ConvexShapeLineIntersection FastIntersectionWith(Line line) {
		var shapeSpaceResult = BaseShape.FastIntersectionWith(TransformToShapeSpace(line));
		return new(TransformToWorldSpace(shapeSpaceResult.First), TransformToWorldSpace(shapeSpaceResult.Second));
	}
	/// <inheritdoc />
	public ConvexShapeLineIntersection? IntersectionWith(Ray ray) {
		var shapeSpaceResult = BaseShape.IntersectionWith(TransformToShapeSpace(ray));
		return shapeSpaceResult == null
			? null
			: new(TransformToWorldSpace(shapeSpaceResult.Value.First), TransformToWorldSpace(shapeSpaceResult.Value.Second));
	}
	/// <inheritdoc />
	public ConvexShapeLineIntersection FastIntersectionWith(Ray ray) {
		var shapeSpaceResult = BaseShape.FastIntersectionWith(TransformToShapeSpace(ray));
		return new(TransformToWorldSpace(shapeSpaceResult.First), TransformToWorldSpace(shapeSpaceResult.Second));
	}
	/// <inheritdoc />
	public ConvexShapeLineIntersection? IntersectionWith(BoundedRay ray) {
		var shapeSpaceResult = BaseShape.IntersectionWith(TransformToShapeSpace(ray));
		return shapeSpaceResult == null
			? null
			: new(TransformToWorldSpace(shapeSpaceResult.Value.First), TransformToWorldSpace(shapeSpaceResult.Value.Second));
	}
	/// <inheritdoc />
	public ConvexShapeLineIntersection FastIntersectionWith(BoundedRay ray) {
		var shapeSpaceResult = BaseShape.FastIntersectionWith(TransformToShapeSpace(ray));
		return new(TransformToWorldSpace(shapeSpaceResult.First), TransformToWorldSpace(shapeSpaceResult.Second));
	}

	/// <inheritdoc />
	public float DistanceFrom(Plane plane) => BaseShape.DistanceFrom(TransformToShapeSpace(plane));
	/// <inheritdoc />
	public float DistanceSquaredFrom(Plane plane) => BaseShape.DistanceSquaredFrom(TransformToShapeSpace(plane));
	/// <inheritdoc />
	public float SignedDistanceFrom(Plane plane) => BaseShape.SignedDistanceFrom(TransformToShapeSpace(plane));
	/// <inheritdoc />
	public Location PointClosestTo(Plane plane) => TransformToWorldSpace(BaseShape.PointClosestTo(TransformToShapeSpace(plane)));
	/// <inheritdoc />
	public Location ClosestPointOn(Plane plane) => TransformToWorldSpace(BaseShape.ClosestPointOn(TransformToShapeSpace(plane)));
	/// <inheritdoc />
	public PlaneObjectRelationship RelationshipTo(Plane plane) => BaseShape.RelationshipTo(TransformToShapeSpace(plane));

	/// <inheritdoc />
	public Location SurfacePointClosestTo(Location point) => TransformToWorldSpace(BaseShape.SurfacePointClosestTo(TransformToShapeSpace(point)));
	/// <inheritdoc />
	public float SurfaceDistanceFrom(Location point) => BaseShape.SurfaceDistanceFrom(TransformToShapeSpace(point));
	/// <inheritdoc />
	public float SurfaceDistanceSquaredFrom(Location point) => BaseShape.SurfaceDistanceSquaredFrom(TransformToShapeSpace(point));
	/// <inheritdoc />
	public Location SurfacePointClosestTo(Line line) => TransformToWorldSpace(BaseShape.SurfacePointClosestTo(TransformToShapeSpace(line)));
	/// <inheritdoc />
	public Location ClosestPointToSurfaceOn(Line line) => TransformToWorldSpace(BaseShape.ClosestPointToSurfaceOn(TransformToShapeSpace(line)));
	/// <inheritdoc />
	public float SurfaceDistanceFrom(Line line) => BaseShape.SurfaceDistanceFrom(TransformToShapeSpace(line));
	/// <inheritdoc />
	public float SurfaceDistanceSquaredFrom(Line line) => BaseShape.SurfaceDistanceSquaredFrom(TransformToShapeSpace(line));
	/// <inheritdoc />
	public Location SurfacePointClosestTo(Ray ray) => TransformToWorldSpace(BaseShape.SurfacePointClosestTo(TransformToShapeSpace(ray)));
	/// <inheritdoc />
	public Location ClosestPointToSurfaceOn(Ray ray) => TransformToWorldSpace(BaseShape.ClosestPointToSurfaceOn(TransformToShapeSpace(ray)));
	/// <inheritdoc />
	public float SurfaceDistanceFrom(Ray ray) => BaseShape.SurfaceDistanceFrom(TransformToShapeSpace(ray));
	/// <inheritdoc />
	public float SurfaceDistanceSquaredFrom(Ray ray) => BaseShape.SurfaceDistanceSquaredFrom(TransformToShapeSpace(ray));
	/// <inheritdoc />
	public Location SurfacePointClosestTo(BoundedRay ray) => TransformToWorldSpace(BaseShape.SurfacePointClosestTo(TransformToShapeSpace(ray)));
	/// <inheritdoc />
	public Location ClosestPointToSurfaceOn(BoundedRay ray) => TransformToWorldSpace(BaseShape.ClosestPointToSurfaceOn(TransformToShapeSpace(ray)));
	/// <inheritdoc />
	public float SurfaceDistanceFrom(BoundedRay ray) => BaseShape.SurfaceDistanceFrom(TransformToShapeSpace(ray));
	/// <inheritdoc />
	public float SurfaceDistanceSquaredFrom(BoundedRay ray) => BaseShape.SurfaceDistanceSquaredFrom(TransformToShapeSpace(ray));
	/// <inheritdoc />
	public Location SurfacePointClosestTo(Plane plane) => TransformToWorldSpace(BaseShape.SurfacePointClosestTo(TransformToShapeSpace(plane)));
	/// <inheritdoc />
	public Location ClosestPointToSurfaceOn(Plane plane) => TransformToWorldSpace(BaseShape.ClosestPointToSurfaceOn(TransformToShapeSpace(plane)));
	Location IConvexShape.GetRandomInternalLocation() => TransformToWorldSpace(BaseShape.GetRandomInternalLocation());
}