// Created on 2025-08-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Buffers.Binary;

namespace Egodystonic.TinyFFR;

public interface IConfigStruct {
	const int SerializationFieldCountSizeBytes = sizeof(int);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOf(float _) => sizeof(float);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOf(int _) => sizeof(int);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOf(bool _) => 1;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOf<T>(T _) where T : IFixedLengthByteSpanSerializable<T> => T.SerializationByteSpanLength;
	protected static int SerializationSizeOf(ReadOnlySpan<char> v) => SerializationFieldCountSizeBytes + v.Length * sizeof(char);
	protected static int SerializationSizeOf<T>(scoped in T v) where T : struct, IConfigStruct<T>, allows ref struct => SerializationFieldCountSizeBytes + T.GetHeapStorageFormattedLength(v);

	protected static void SerializationWrite(scoped ref Span<byte> dest, float v) {
		BinaryPrimitives.WriteSingleLittleEndian(dest, v);
		dest = dest[sizeof(float)..];
	}
	protected static void SerializationWrite(scoped ref Span<byte> dest, int v) {
		BinaryPrimitives.WriteInt32LittleEndian(dest, v);
		dest = dest[sizeof(int)..];
	}
	protected static void SerializationWrite(scoped ref Span<byte> dest, bool v) {
		dest[0] = v ? Byte.MaxValue : Byte.MinValue;
		dest = dest[1..];
	}
	protected static void SerializationWrite<T>(scoped ref Span<byte> dest, T v) where T : IFixedLengthByteSpanSerializable<T> {
		T.SerializeToBytes(dest, v);
		dest = dest[T.SerializationByteSpanLength..];
	}
	protected static void SerializationWrite(scoped ref Span<byte> dest, ReadOnlySpan<char> v) {
		var numBytesWritten = v.Length * sizeof(char);
		BinaryPrimitives.WriteInt32LittleEndian(dest, numBytesWritten);
		MemoryMarshal.AsBytes(v).CopyTo(dest[sizeof(int)..]);
		dest = dest[(sizeof(int) + numBytesWritten)..];
	}
	protected static void SerializationWrite<T>(scoped ref Span<byte> dest, scoped in T v) where T : struct, IConfigStruct<T>, allows ref struct {
		var byteCount = T.GetHeapStorageFormattedLength(v);
		BinaryPrimitives.WriteInt32LittleEndian(dest, byteCount);
		T.ConvertToHeapStorageFormat(dest[sizeof(int)..], v);
		dest = dest[(sizeof(int) + byteCount)..];
	}

	protected static float SerializationReadFloat(scoped ref ReadOnlySpan<byte> dest) {
		var result = BinaryPrimitives.ReadSingleLittleEndian(dest);
		dest = dest[sizeof(float)..];
		return result;
	}
	protected static int SerializationReadInt(scoped ref ReadOnlySpan<byte> src) {
		var result = BinaryPrimitives.ReadInt32LittleEndian(src);
		src = src[sizeof(int)..];
		return result;
	}
	protected static bool SerializationReadBool(scoped ref ReadOnlySpan<byte> src) {
		var result = src[0] > 0;
		src = src[1..];
		return result;
	}
	protected static T SerializationRead<T>(scoped ref ReadOnlySpan<byte> src) where T : IFixedLengthByteSpanSerializable<T> {
		var result = T.DeserializeFromBytes(src);
		src = src[T.SerializationByteSpanLength..];
		return result;
	}
	protected static ReadOnlySpan<char> SerializationReadString(scoped ref ReadOnlySpan<byte> src) {
		var byteCount = BinaryPrimitives.ReadInt32LittleEndian(src);
		var strEnd = (sizeof(int) + byteCount);
		var result = MemoryMarshal.Cast<byte, char>(src[sizeof(int)..strEnd]);
		src = src[strEnd..];
		return result;
	}
	protected static T SerializationReadSubConfig<T>(scoped ref ReadOnlySpan<byte> src) where T : struct, IConfigStruct<T>, allows ref struct {
		var byteCount = BinaryPrimitives.ReadInt32LittleEndian(src);
		var cfgEnd = (sizeof(int) + byteCount);
		var result = T.ConvertFromHeapStorageFormat(src[sizeof(int)..cfgEnd]);
		src = src[cfgEnd..];
		return result;
	}
}
public interface IConfigStruct<TSelf> : IConfigStruct where TSelf : struct, IConfigStruct<TSelf>, allows ref struct {
	static abstract int GetHeapStorageFormattedLength(in TSelf src);
	static abstract void ConvertToHeapStorageFormat(Span<byte> dest, in TSelf src);
	static abstract TSelf ConvertFromHeapStorageFormat(ReadOnlySpan<byte> src);
}