// Created on 2023-09-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

/// <summary>
/// Represents any object in TinyFFR that can be serialized to/from byte-span representation.
/// </summary>
/// <remarks>
/// The serialized representation can be considered stable across versions of the library and across target OS platforms.
/// </remarks>
/// <typeparam name="TSelf">The type that is serializable.</typeparam>
/// <seealso cref="IFixedLengthByteSpanSerializable{TSelf}"/>
public interface IByteSpanSerializable<TSelf> where TSelf : IByteSpanSerializable<TSelf>, allows ref struct {
	/// <summary>
	/// Returns the number of bytes required to serialize <paramref name="src"/> via <see cref="SerializeToBytes"/>.
	/// </summary>
	/// <param name="src">The value that would be serialized.</param>
	/// <returns>The exact length, in bytes, of the span that <see cref="SerializeToBytes"/> requires as its destination for <paramref name="src"/>.</returns>
	static abstract int GetSerializationByteSpanLength(TSelf src); // This is a static for a) consistency with other methods and b) to not pollute TSelf's instance API with serialization-oriented junk
	/// <summary>
	/// Serializes <paramref name="src"/> to its byte-span representation.
	/// </summary>
	/// <param name="dest">The destination span to write to. Must be at least <see cref="GetSerializationByteSpanLength"/> bytes long for <paramref name="src"/>.</param>
	/// <param name="src">The value to serialize.</param>
	static abstract void SerializeToBytes(Span<byte> dest, TSelf src);
	/// <summary>
	/// Deserializes a <typeparamref name="TSelf"/> from its byte-span representation.
	/// </summary>
	/// <param name="src">A span previously written to by <see cref="SerializeToBytes"/> (or an equivalent, compatible source).</param>
	/// <returns>The deserialized value.</returns>
	static abstract TSelf DeserializeFromBytes(ReadOnlySpan<byte> src);
}

/// <summary>
/// A specialization of <see cref="IByteSpanSerializable{TSelf}"/> applicable to types that have a fixed serialization byte length (<see cref="SerializationByteSpanLength"/>).
/// </summary>
/// <typeparam name="TSelf">The type that is serializable.</typeparam>
public interface IFixedLengthByteSpanSerializable<TSelf> : IByteSpanSerializable<TSelf> where TSelf : IFixedLengthByteSpanSerializable<TSelf>, allows ref struct {
	/// <summary>
	/// The number of bytes required to serialize any value of <typeparamref name="TSelf"/> via <see cref="IByteSpanSerializable{TSelf}.SerializeToBytes"/>.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="IByteSpanSerializable{TSelf}.GetSerializationByteSpanLength"/>, this length is the same for every value of <typeparamref name="TSelf"/>.
	/// </remarks>
	static abstract int SerializationByteSpanLength { get; }
	static int IByteSpanSerializable<TSelf>.GetSerializationByteSpanLength(TSelf src) => TSelf.SerializationByteSpanLength;
}