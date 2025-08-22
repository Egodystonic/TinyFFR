// Created on 2023-09-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

// TODO xmldoc describe that this type represents a stable serialization always across versions and platforms
public interface IByteSpanSerializable<TSelf> where TSelf : IByteSpanSerializable<TSelf>, allows ref struct {
	static abstract int GetSerializationByteSpanLength(TSelf src); // This is a static for a) consistency with other methods and b) to not pollute TSelf's instance API with serialization-oriented junk
	static abstract void SerializeToBytes(Span<byte> dest, TSelf src);
	static abstract TSelf DeserializeFromBytes(ReadOnlySpan<byte> src);
}

public interface IFixedLengthByteSpanSerializable<TSelf> : IByteSpanSerializable<TSelf> where TSelf : IFixedLengthByteSpanSerializable<TSelf>, allows ref struct {
	static abstract int SerializationByteSpanLength { get; }
	static int IByteSpanSerializable<TSelf>.GetSerializationByteSpanLength(TSelf src) => TSelf.SerializationByteSpanLength;
}