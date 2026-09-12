// Created on 2026-04-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Trait interface used to mark a shape as wrapped with a translation (position) offset.
/// </summary>
public interface ITranslatedShape : IShape {
	/// <summary>
	/// The offset applied to the wrapped shape.
	/// </summary>
	Vect Translation { get; init; }
}
/// <summary>
/// Extension of <see cref="ITranslatedShape"/> that includes a self type parameter allowing for more functional definitions.
/// </summary>
/// <typeparam name="TSelf">The type implementing this interface.</typeparam>
public interface ITranslatedShape<TSelf> : ITranslatedShape, IShape<TSelf>, ITranslatable<TSelf> where TSelf : ITranslatedShape<TSelf>;
/// <summary>
/// Extension of <see cref="ITranslatedShape{TSelf}"/> that additionally exposes the wrapped shape's type.
/// </summary>
/// <typeparam name="TSelf">The type implementing this interface.</typeparam>
/// <typeparam name="TBase">The type of the wrapped shape.</typeparam>
public interface ITranslatedShape<TSelf, TBase> : ITranslatedShape<TSelf> where TSelf : ITranslatedShape<TSelf, TBase> where TBase : IShape<TBase> {
	/// <summary>
	/// The wrapped shape, before the translation offset is applied.
	/// </summary>
	TBase BaseShape { get; init; }
}
/// <summary>
/// Combines <see cref="ITranslatedShape"/> and <see cref="IConvexShape"/>: a translated wrapper around a convex shape.
/// </summary>
public interface ITranslatedConvexShape : ITranslatedShape, IConvexShape;
/// <summary>
/// Extension of <see cref="ITranslatedConvexShape"/> that includes a self type parameter allowing for more functional definitions.
/// </summary>
/// <typeparam name="TSelf">The type implementing this interface.</typeparam>
public interface ITranslatedConvexShape<TSelf> : ITranslatedShape<TSelf>, ITranslatedConvexShape, IConvexShape<TSelf> where TSelf : ITranslatedConvexShape<TSelf>;
/// <summary>
/// Extension of <see cref="ITranslatedConvexShape{TSelf}"/> that additionally exposes the wrapped convex shape's type.
/// </summary>
/// <typeparam name="TSelf">The type implementing this interface.</typeparam>
/// <typeparam name="TBase">The type of the wrapped convex shape.</typeparam>
public interface ITranslatedConvexShape<TSelf, TBase> : ITranslatedConvexShape<TSelf>, ITranslatedShape<TSelf, TBase> where TSelf : ITranslatedConvexShape<TSelf, TBase> where TBase : IConvexShape<TBase>;

/// <summary>
/// General purpose wrapper type that takes any <see cref="IShape{T}"/> and allows it to be translated (moved)
/// around 3D space.
/// </summary>
/// <remarks>
/// All methods required by the <see cref="IShape{T}"/> interface are implemented,
/// but recalculated when invoked to account for the given <see cref="Translation"/>.
/// <para>
/// For example, a function calculating the distance to the <see cref="BaseShape"/> from a given <see cref="Line"/>
/// will return the correct value accounting for the position of the shape.
/// </para>
/// <para>
/// Note that this type can not provide implementations for methods specific to the actual shape type <typeparamref name="T"/>. For more specialized
/// implementations, see <see cref="TranslatedConvexShape{T}"/> or better yet <see cref="PositionedSphere"/>, <see cref="PositionedCuboid"/>, etc.
/// </para>
/// </remarks>
/// <typeparam name="T">The shape type to translate.</typeparam>
/// <seealso cref="PositionedSphere"/>
/// <seealso cref="PositionedCuboid"/>
/// <seealso cref="PositionedRotatedCuboid"/>
/// <seealso cref="TranslatedConvexShape{T}"/>
/// <seealso cref="TranslatedRotatedShape{T}"/>
/// <seealso cref="TranslatedRotatedConvexShape{T}"/>
public readonly struct TranslatedShape<T> : ITranslatedShape<TranslatedShape<T>, T> where T : IShape<T> {
	const string StringComponentSeparator = " @ ";
	/// <summary>
	/// The wrapped shape, before <see cref="Translation"/> is applied.
	/// </summary>
	public T BaseShape { get; init; }
	/// <summary>
	/// The offset applied to <see cref="BaseShape"/>.
	/// </summary>
	public Vect Translation { get; init; }

	/// <summary>
	/// Determines whether this value's <see cref="BaseShape"/> and <see cref="Translation"/> are both physically valid.
	/// </summary>
	public bool IsPhysicallyValid => BaseShape.IsPhysicallyValid && Translation.IsPhysicallyValid;

	/// <summary>
	/// Constructs a new <see cref="TranslatedShape{T}"/> from the given <paramref name="baseShape"/> and <paramref name="translation"/>.
	/// </summary>
	/// <param name="baseShape">The shape to translate.</param>
	/// <param name="translation">The offset to apply.</param>
	public TranslatedShape(T baseShape, Vect translation) {
		BaseShape = baseShape;
		Translation = translation;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToShapeSpace<TVal>(TVal val) where TVal : ITranslatable<TVal> => val.MovedBy(-Translation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToWorldSpace<TVal>(TVal val) where TVal : ITranslatable<TVal> => val.MovedBy(Translation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToShapeSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal> => val?.MovedBy(-Translation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToWorldSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal> => val?.MovedBy(Translation);

	#region ToString / Format / Parse
	/// <inheritdoc />
	/// <remarks>
	/// The format is <c>"{BaseShape} @ {Translation}"</c>.
	/// </remarks>
	public string ToString(string? format, IFormatProvider? formatProvider) {
		return BaseShape.ToString(format, formatProvider) + StringComponentSeparator + Translation.ToString(format, formatProvider);
	}
	/// <inheritdoc />
	/// <remarks>
	/// The format is <c>"{BaseShape} @ {Translation}"</c>.
	/// </remarks>
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		charsWritten = 0;

		if (!BaseShape.TryFormat(destination, out var c, format, provider)) return false;
		charsWritten += c;
		destination = destination[c..];

		if (!StringComponentSeparator.TryCopyTo(destination)) return false;
		charsWritten += StringComponentSeparator.Length;
		destination = destination[StringComponentSeparator.Length..];

		if (!Translation.TryFormat(destination, out c, format, provider)) return false;
		charsWritten += c;
		return true;
	}
	/// <inheritdoc />
	public static TranslatedShape<T> Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	/// <inheritdoc />
	public static bool TryParse(string? s, IFormatProvider? provider, out TranslatedShape<T> result) => TryParse(s.AsSpan(), provider, out result);
	/// <inheritdoc />
	/// <exception cref="ArgumentException">Thrown if <paramref name="s"/> is not in a valid format.</exception>
	public static TranslatedShape<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		if (!TryParse(s, provider, out var result)) {
			throw new ArgumentException($"Given input string \"{s}\" does not represent a valid translated {typeof(T).Name}.", nameof(s));
		}
		return result;
	}
	/// <inheritdoc />
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TranslatedShape<T> result) {
		result = default;

		var splitIndex = s.IndexOf(StringComponentSeparator);
		if (splitIndex < 0) return false;

		if (!T.TryParse(s[..splitIndex], provider, out var baseShape)) return false;
		if (!Vect.TryParse(s[(splitIndex + StringComponentSeparator.Length)..], provider, out var translation)) return false;

		result = new(baseShape, translation);
		return true;
	}
	#endregion

	#region Byte Span Serialization / Deserialization
	/// <inheritdoc />
	public static int SerializationByteSpanLength => T.SerializationByteSpanLength + Location.SerializationByteSpanLength;
	/// <inheritdoc />
	public static void SerializeToBytes(Span<byte> dest, TranslatedShape<T> src) {
		T.SerializeToBytes(dest, src.BaseShape);
		Vect.SerializeToBytes(dest[T.SerializationByteSpanLength..], src.Translation);
	}
	/// <inheritdoc />
	public static TranslatedShape<T> DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			T.DeserializeFromBytes(src),
			Vect.DeserializeFromBytes(src[T.SerializationByteSpanLength..])
		);
	}
	#endregion

	#region Move / Scale
	/// <inheritdoc />
	public TranslatedShape<T> MovedBy(Vect v) => new(BaseShape, Translation.Plus(v));
	/// <summary>
	/// Moves <paramref name="left"/> by <paramref name="right"/>; equivalent to <c>left.MovedBy(right)</c>.
	/// </summary>
	/// <param name="left">The shape to move.</param>
	/// <param name="right">The vector to move by.</param>
	public static TranslatedShape<T> operator +(TranslatedShape<T> left, Vect right) => new(left.BaseShape, left.Translation + right);
	/// <summary>
	/// Moves <paramref name="left"/> in the direction opposite to <paramref name="right"/>; equivalent to <c>left.MovedBy(-right)</c>.
	/// </summary>
	/// <param name="left">The shape to move.</param>
	/// <param name="right">The vector to move by, in reverse.</param>
	public static TranslatedShape<T> operator -(TranslatedShape<T> left, Vect right) => new(left.BaseShape, left.Translation - right);
	/// <summary>
	/// Moves <paramref name="right"/> by <paramref name="left"/>; equivalent to <c>right.MovedBy(left)</c>.
	/// </summary>
	/// <param name="left">The vector to move by.</param>
	/// <param name="right">The shape to move.</param>
	public static TranslatedShape<T> operator +(Vect left, TranslatedShape<T> right) => new(right.BaseShape, right.Translation + left);

	/// <summary>
	/// Multiplies <paramref name="left"/> by <paramref name="right"/>; equivalent to <c>left.ScaledBy(right)</c>.
	/// </summary>
	/// <param name="left">The shape to scale.</param>
	/// <param name="right">The scale factor.</param>
	public static TranslatedShape<T> operator *(TranslatedShape<T> left, float right) => new(left.BaseShape * right, left.Translation);
	/// <summary>
	/// Divides <paramref name="left"/> by <paramref name="right"/>; equivalent to <c>left.ScaledBy(1f / right)</c>.
	/// </summary>
	/// <param name="left">The shape to scale.</param>
	/// <param name="right">The divisor.</param>
	public static TranslatedShape<T> operator /(TranslatedShape<T> left, float right) => new(left.BaseShape / right, left.Translation);
	/// <summary>
	/// Multiplies <paramref name="right"/> by <paramref name="left"/>; equivalent to <c>right.ScaledBy(left)</c>.
	/// </summary>
	/// <param name="left">The scale factor.</param>
	/// <param name="right">The shape to scale.</param>
	public static TranslatedShape<T> operator *(float left, TranslatedShape<T> right) => new(left * right.BaseShape, right.Translation);
	/// <summary>
	/// Returns this value with <see cref="BaseShape"/> scaled by <paramref name="scalar"/>. <see cref="Translation"/> is left unchanged.
	/// </summary>
	/// <param name="scalar">The scale factor.</param>
	public TranslatedShape<T> ScaledBy(float scalar) => new(BaseShape * scalar, Translation);
	#endregion

	#region Equality
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is TranslatedShape<T> other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(BaseShape, Translation);
	/// <inheritdoc />
	public bool Equals(TranslatedShape<T> other) => BaseShape.Equals(other.BaseShape) && Translation.Equals(other.Translation);
	/// <summary>
	/// Determines whether this value is equal to <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <remarks>
	/// This compares <see cref="BaseShape"/> and <see cref="Translation"/> independently, each within <paramref name="tolerance"/>.
	/// </remarks>
	/// <param name="other">The other value.</param>
	/// <param name="tolerance">The tolerance value.</param>
	/// <returns>True if equal within tolerance, false if not.</returns>
	public bool Equals(TranslatedShape<T> other, float tolerance) => BaseShape.Equals(other.BaseShape, tolerance) && Translation.Equals(other.Translation, tolerance);
	/// <inheritdoc />
	public static bool operator ==(TranslatedShape<T> left, TranslatedShape<T> right) => left.Equals(right);
	/// <inheritdoc />
	public static bool operator !=(TranslatedShape<T> left, TranslatedShape<T> right) => !left.Equals(right);
	#endregion

	#region Random / Interp / Clamp
	/// <inheritdoc />
	public static TranslatedShape<T> Random() => new(T.Random(), Vect.Random());
	/// <inheritdoc />
	public static TranslatedShape<T> Random(TranslatedShape<T> minInclusive, TranslatedShape<T> maxExclusive) {
		return new(
			T.Random(minInclusive.BaseShape, maxExclusive.BaseShape),
			Vect.Random(minInclusive.Translation, maxExclusive.Translation)
		);
	}
	/// <inheritdoc />
	public static TranslatedShape<T> Interpolate(TranslatedShape<T> start, TranslatedShape<T> end, float distance) {
		return new(
			T.Interpolate(start.BaseShape, end.BaseShape, distance),
			Vect.Interpolate(start.Translation, end.Translation, distance)
		);
	}
	/// <inheritdoc />
	public TranslatedShape<T> Clamp(TranslatedShape<T> min, TranslatedShape<T> max) {
		return new(
			BaseShape.Clamp(min.BaseShape, max.BaseShape),
			Translation.Clamp(min.Translation, max.Translation)
		);
	}
	#endregion
}

/// <summary>
/// General purpose wrapper type that takes any <see cref="IConvexShape{T}"/> and allows it to be translated (moved) around 3D space.
/// </summary>
/// <remarks>
/// This is the convex-shape-constrained counterpart to <see cref="TranslatedShape{T}"/>: because <typeparamref name="T"/>
/// is guaranteed to be an <see cref="IConvexShape{T}"/>, this type additionally implements the full <see cref="IConvexShape"/>
/// surface (closest-point, distance, intersection, and reflection queries), recalculated on every call to account for <see cref="Translation"/>.
/// <para>
/// For more specialized implementations with concrete shape-specific members, see <see cref="PositionedSphere"/>, <see cref="PositionedCuboid"/>, etc.
/// </para>
/// </remarks>
/// <typeparam name="T">The convex shape type to translate.</typeparam>
/// <seealso cref="PositionedSphere"/>
/// <seealso cref="PositionedCuboid"/>
/// <seealso cref="PositionedRotatedCuboid"/>
/// <seealso cref="TranslatedShape{T}"/>
/// <seealso cref="TranslatedRotatedShape{T}"/>
/// <seealso cref="TranslatedRotatedConvexShape{T}"/>
public readonly struct TranslatedConvexShape<T> : ITranslatedConvexShape<TranslatedConvexShape<T>, T> where T : IConvexShape<T> {
	/// <summary>
	/// The wrapped shape, before <see cref="Translation"/> is applied.
	/// </summary>
	public T BaseShape { get; init; }
	/// <summary>
	/// The offset applied to <see cref="BaseShape"/>.
	/// </summary>
	public Vect Translation { get; init; }

	/// <summary>
	/// Constructs a new <see cref="TranslatedConvexShape{T}"/> from the given <paramref name="baseShape"/> and <paramref name="translation"/>.
	/// </summary>
	/// <param name="baseShape">The shape to translate.</param>
	/// <param name="translation">The offset to apply.</param>
	public TranslatedConvexShape(T baseShape, Vect translation) {
		BaseShape = baseShape;
		Translation = translation;
	}

	/// <summary>
	/// Implicitly converts this value to the non-convex-constrained <see cref="TranslatedShape{T}"/>.
	/// </summary>
	/// <param name="operand">The value to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedShape<T>(TranslatedConvexShape<T> operand) => new(operand.BaseShape, operand.Translation);
	/// <summary>
	/// Implicitly converts <paramref name="operand"/> to a <see cref="TranslatedConvexShape{T}"/>.
	/// </summary>
	/// <param name="operand">The value to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedConvexShape<T>(TranslatedShape<T> operand) => new(operand.BaseShape, operand.Translation);

	#region Deferred Members
	/// <summary>
	/// Determines whether this value's <see cref="BaseShape"/> and <see cref="Translation"/> are both physically valid.
	/// </summary>
	public bool IsPhysicallyValid {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ((TranslatedShape<T>) this).IsPhysicallyValid;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToShapeSpace<TVal>(TVal val) where TVal : ITranslatable<TVal> => ((TranslatedShape<T>) this).TransformToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToWorldSpace<TVal>(TVal val) where TVal : ITranslatable<TVal> => ((TranslatedShape<T>) this).TransformToWorldSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToShapeSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal> => ((TranslatedShape<T>) this).TransformToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToWorldSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal> => ((TranslatedShape<T>) this).TransformToWorldSpace(val);
	/// <inheritdoc />
	/// <remarks>
	/// The format is <c>"{BaseShape} @ {Translation}"</c>.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ToString(string? format, IFormatProvider? formatProvider) => ((TranslatedShape<T>) this).ToString(format, formatProvider);
	/// <inheritdoc />
	/// <remarks>
	/// The format is <c>"{BaseShape} @ {Translation}"</c>.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => ((TranslatedShape<T>) this).TryFormat(destination, out charsWritten, format, provider);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> Parse(string s, IFormatProvider? provider) => TranslatedShape<T>.Parse(s, provider);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(string? s, IFormatProvider? provider, out TranslatedConvexShape<T> result) {
		var returnValue = TranslatedShape<T>.TryParse(s, provider, out var r);
		result = r;
		return returnValue;
	}
	/// <inheritdoc />
	/// <exception cref="ArgumentException">Thrown if <paramref name="s"/> is not in a valid format.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => TranslatedShape<T>.Parse(s, provider);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TranslatedConvexShape<T> result) {
		var returnValue = TranslatedShape<T>.TryParse(s, provider, out var r);
		result = r;
		return returnValue;
	}
	/// <inheritdoc />
	public static int SerializationByteSpanLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TranslatedShape<T>.SerializationByteSpanLength;
	}
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SerializeToBytes(Span<byte> dest, TranslatedConvexShape<T> src) => TranslatedShape<T>.SerializeToBytes(dest, src);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> DeserializeFromBytes(ReadOnlySpan<byte> src) => TranslatedShape<T>.DeserializeFromBytes(src);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TranslatedConvexShape<T> MovedBy(Vect v) => ((TranslatedShape<T>) this).MovedBy(v);
	/// <summary>
	/// Moves <paramref name="left"/> by <paramref name="right"/>; equivalent to <c>left.MovedBy(right)</c>.
	/// </summary>
	/// <param name="left">The shape to move.</param>
	/// <param name="right">The vector to move by.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> operator +(TranslatedConvexShape<T> left, Vect right) => ((TranslatedShape<T>) left) + right;
	/// <summary>
	/// Moves <paramref name="left"/> in the direction opposite to <paramref name="right"/>; equivalent to <c>left.MovedBy(-right)</c>.
	/// </summary>
	/// <param name="left">The shape to move.</param>
	/// <param name="right">The vector to move by, in reverse.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> operator -(TranslatedConvexShape<T> left, Vect right) => ((TranslatedShape<T>) left) - right;
	/// <summary>
	/// Moves <paramref name="right"/> by <paramref name="left"/>; equivalent to <c>right.MovedBy(left)</c>.
	/// </summary>
	/// <param name="left">The vector to move by.</param>
	/// <param name="right">The shape to move.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> operator +(Vect left, TranslatedConvexShape<T> right) => left + ((TranslatedShape<T>) right);
	/// <summary>
	/// Multiplies <paramref name="left"/> by <paramref name="right"/>; equivalent to <c>left.ScaledBy(right)</c>.
	/// </summary>
	/// <param name="left">The shape to scale.</param>
	/// <param name="right">The scale factor.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> operator *(TranslatedConvexShape<T> left, float right) => ((TranslatedShape<T>) left) * right;
	/// <summary>
	/// Divides <paramref name="left"/> by <paramref name="right"/>; equivalent to <c>left.ScaledBy(1f / right)</c>.
	/// </summary>
	/// <param name="left">The shape to scale.</param>
	/// <param name="right">The divisor.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> operator /(TranslatedConvexShape<T> left, float right) => ((TranslatedShape<T>) left) / right;
	/// <summary>
	/// Multiplies <paramref name="right"/> by <paramref name="left"/>; equivalent to <c>right.ScaledBy(left)</c>.
	/// </summary>
	/// <param name="left">The scale factor.</param>
	/// <param name="right">The shape to scale.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> operator *(float left, TranslatedConvexShape<T> right) => ((TranslatedShape<T>) right) * left;
	/// <summary>
	/// Returns this value with <see cref="BaseShape"/> scaled by <paramref name="scalar"/>. <see cref="Translation"/> is left unchanged.
	/// </summary>
	/// <param name="scalar">The scale factor.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TranslatedConvexShape<T> ScaledBy(float scalar) => ((TranslatedShape<T>) this).ScaledBy(scalar);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> Random() => TranslatedShape<T>.Random();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> Random(TranslatedConvexShape<T> minInclusive, TranslatedConvexShape<T> maxExclusive) => TranslatedShape<T>.Random(minInclusive, maxExclusive);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> Interpolate(TranslatedConvexShape<T> start, TranslatedConvexShape<T> end, float distance) => TranslatedShape<T>.Interpolate(start, end, distance);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TranslatedConvexShape<T> Clamp(TranslatedConvexShape<T> min, TranslatedConvexShape<T> max) => ((TranslatedShape<T>) this).Clamp(min, max);
	#endregion

	#region Equality
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is TranslatedConvexShape<T> other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(BaseShape, Translation);
	/// <inheritdoc />
	public bool Equals(TranslatedConvexShape<T> other) => BaseShape.Equals(other.BaseShape) && Translation.Equals(other.Translation);
	/// <summary>
	/// Determines whether this value is equal to <paramref name="other"/> within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <remarks>
	/// This compares <see cref="BaseShape"/> and <see cref="Translation"/> independently, each within <paramref name="tolerance"/>.
	/// </remarks>
	/// <param name="other">The other value.</param>
	/// <param name="tolerance">The tolerance value.</param>
	/// <returns>True if equal within tolerance, false if not.</returns>
	public bool Equals(TranslatedConvexShape<T> other, float tolerance) => BaseShape.Equals(other.BaseShape, tolerance) && Translation.Equals(other.Translation, tolerance);
	/// <inheritdoc />
	public static bool operator ==(TranslatedConvexShape<T> left, TranslatedConvexShape<T> right) => left.Equals(right);
	/// <inheritdoc />
	public static bool operator !=(TranslatedConvexShape<T> left, TranslatedConvexShape<T> right) => !left.Equals(right);
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