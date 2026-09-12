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
	static abstract int GetSerializationByteSpanLength(TSelf src); // This is a static for a) consistency with other methods and b) to not pollute TSelf's instance API with serialization-oriented junk
	static abstract void SerializeToBytes(Span<byte> dest, TSelf src);
	static abstract TSelf DeserializeFromBytes(ReadOnlySpan<byte> src);
}

/// <summary>
/// A specialization of <see cref="IByteSpanSerializable{TSelf}"/> applicable to types that have a fixed serialization byte length (<see cref="SerializationByteSpanLength"/>).
/// </summary>
/// <typeparam name="TSelf">The type that is serializable.</typeparam>
public interface IFixedLengthByteSpanSerializable<TSelf> : IByteSpanSerializable<TSelf> where TSelf : IFixedLengthByteSpanSerializable<TSelf>, allows ref struct {
	static abstract int SerializationByteSpanLength { get; }
	static int IByteSpanSerializable<TSelf>.GetSerializationByteSpanLength(TSelf src) => TSelf.SerializationByteSpanLength;
}