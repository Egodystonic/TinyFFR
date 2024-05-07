// Created on 2023-09-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

// TODO xmldoc describe that this type represents a stable serialization always across versions and platforms
public interface IByteSpanSerializable<TSelf> where TSelf : IByteSpanSerializable<TSelf> {
	static abstract int SerializationByteSpanLength { get; }
	static abstract void SerializeToBytes(Span<byte> dest, TSelf src);
	static abstract TSelf DeserializeFromBytes(ReadOnlySpan<byte> src);
}