// Created on 2025-08-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Buffers.Binary;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR;

public interface IConfigStruct {
	const int SerializationFieldCountSizeBytes = sizeof(int);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOfFloat() => sizeof(float);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOfInt() => sizeof(int);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOfLong() => sizeof(long);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOfBool() => 1;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOf<T>() where T : IFixedLengthByteSpanSerializable<T> => T.SerializationByteSpanLength;
	protected static int SerializationSizeOfString(ReadOnlySpan<char> v) => SerializationFieldCountSizeBytes + v.Length * sizeof(char);
	protected static int SerializationSizeOfSubConfig<T>(scoped in T v) where T : struct, IConfigStruct<T>, allows ref struct => SerializationFieldCountSizeBytes + T.GetHeapStorageFormattedLength(v);
	protected static int SerializationSizeOfResource() => Marshal.SizeOf<GCHandle>() + UIntPtr.Size;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOfNullableFloat() => sizeof(bool) + sizeof(float);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOfNullableInt() => sizeof(bool) + sizeof(int);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOfNullableLong() => sizeof(bool) + sizeof(long);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOfNullableBool() => sizeof(bool) + sizeof(bool);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOfNullable<T>() where T : IFixedLengthByteSpanSerializable<T> => sizeof(bool) + T.SerializationByteSpanLength;

	protected static void SerializationWriteFloat(scoped ref Span<byte> dest, float v) {
		BinaryPrimitives.WriteSingleLittleEndian(dest, v);
		dest = dest[sizeof(float)..];
	}
	protected static void SerializationWriteInt(scoped ref Span<byte> dest, int v) {
		BinaryPrimitives.WriteInt32LittleEndian(dest, v);
		dest = dest[sizeof(int)..];
	}
	protected static void SerializationWriteLong(scoped ref Span<byte> dest, long v) {
		BinaryPrimitives.WriteInt64LittleEndian(dest, v);
		dest = dest[sizeof(long)..];
	}
	protected static void SerializationWriteBool(scoped ref Span<byte> dest, bool v) {
		dest[0] = v ? Byte.MaxValue : Byte.MinValue;
		dest = dest[1..];
	}
	protected static void SerializationWrite<T>(scoped ref Span<byte> dest, T v) where T : IFixedLengthByteSpanSerializable<T> {
		T.SerializeToBytes(dest, v);
		dest = dest[T.SerializationByteSpanLength..];
	}
	protected static void SerializationWriteString(scoped ref Span<byte> dest, ReadOnlySpan<char> v) {
		var numBytesWritten = v.Length * sizeof(char);
		BinaryPrimitives.WriteInt32LittleEndian(dest, numBytesWritten);
		MemoryMarshal.AsBytes(v).CopyTo(dest[sizeof(int)..]);
		dest = dest[(sizeof(int) + numBytesWritten)..];
	}
	protected static void SerializationWriteSubConfig<T>(scoped ref Span<byte> dest, scoped in T v) where T : struct, IConfigStruct<T>, allows ref struct {
		var byteCount = T.GetHeapStorageFormattedLength(v);
		BinaryPrimitives.WriteInt32LittleEndian(dest, byteCount);
		T.AllocateAndConvertToHeapStorage(dest[sizeof(int)..], v);
		dest = dest[(sizeof(int) + byteCount)..];
	}
	protected static void SerializationWriteResource<T>(scoped ref Span<byte> dest, T v) where T : IResource<T> {
		v.AllocateGcHandleAndSerializeResource(dest);
		dest = dest[SerializationSizeOfResource()..];
	}
	protected static void SerializationWriteNullableFloat(scoped ref Span<byte> dest, float? v) {
		SerializationWriteBool(ref dest, v.HasValue);
		BinaryPrimitives.WriteSingleLittleEndian(dest, v ?? default);
		dest = dest[sizeof(float)..];
	}
	protected static void SerializationWriteNullableInt(scoped ref Span<byte> dest, int? v) {
		SerializationWriteBool(ref dest, v.HasValue);
		BinaryPrimitives.WriteInt32LittleEndian(dest, v ?? default);
		dest = dest[sizeof(int)..];
	}
	protected static void SerializationWriteNullableLong(scoped ref Span<byte> dest, long? v) {
		SerializationWriteBool(ref dest, v.HasValue);
		BinaryPrimitives.WriteInt64LittleEndian(dest, v ?? default);
		dest = dest[sizeof(long)..];
	}
	protected static void SerializationWriteNullableBool(scoped ref Span<byte> dest, bool? v) {
		SerializationWriteBool(ref dest, v.HasValue);
		dest[0] = (v ?? default) ? Byte.MaxValue : Byte.MinValue;
		dest = dest[1..];
	}
	protected static void SerializationWriteNullable<T>(scoped ref Span<byte> dest, T? v) where T : struct, IFixedLengthByteSpanSerializable<T> {
		SerializationWriteBool(ref dest, v.HasValue);
		T.SerializeToBytes(dest, v ?? default);
		dest = dest[T.SerializationByteSpanLength..];
	}

	protected static float SerializationReadFloat(scoped ref ReadOnlySpan<byte> src) {
		var result = BinaryPrimitives.ReadSingleLittleEndian(src);
		src = src[sizeof(float)..];
		return result;
	}
	protected static int SerializationReadInt(scoped ref ReadOnlySpan<byte> src) {
		var result = BinaryPrimitives.ReadInt32LittleEndian(src);
		src = src[sizeof(int)..];
		return result;
	}
	protected static long SerializationReadLong(scoped ref ReadOnlySpan<byte> src) {
		var result = BinaryPrimitives.ReadInt64LittleEndian(src);
		src = src[sizeof(long)..];
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
		var cfgEnd = sizeof(int) + byteCount;
		var result = T.ConvertFromAllocatedHeapStorage(src[sizeof(int)..cfgEnd]);
		src = src[cfgEnd..];
		return result;
	}
	protected static T SerializationReadResource<T>(scoped ref ReadOnlySpan<byte> src) where T : IResource<T> {
		var result = T.CreateFromHandleAndImpl(
			IResource.ReadHandleFromSerializedResource(src),
			(IResourceImplProvider) IResource.ReadGcHandleFromSerializedResource(src).Target!
		);
		src = src[SerializationSizeOfResource()..];
		return result;
	}
	protected static void SerializationDisposeResourceHandle(ReadOnlySpan<byte> resourceData) {
		IResource.ReadGcHandleFromSerializedResource(resourceData).Free();
	}

	protected static float? SerializationReadNullableFloat(scoped ref ReadOnlySpan<byte> src) {
		var hasValue = SerializationReadBool(ref src);
		var value = SerializationReadFloat(ref src);
		return hasValue ? value : null;
	}
	protected static int? SerializationReadNullableInt(scoped ref ReadOnlySpan<byte> src) {
		var hasValue = SerializationReadBool(ref src);
		var value = SerializationReadInt(ref src);
		return hasValue ? value : null;
	}
	protected static long? SerializationReadNullableLong(scoped ref ReadOnlySpan<byte> src) {
		var hasValue = SerializationReadBool(ref src);
		var value = SerializationReadLong(ref src);
		return hasValue ? value : null;
	}
	protected static bool? SerializationReadNullableBool(scoped ref ReadOnlySpan<byte> src) {
		var hasValue = SerializationReadBool(ref src);
		var value = SerializationReadBool(ref src);
		return hasValue ? value : null;
	}
	protected static T? SerializationReadNullable<T>(scoped ref ReadOnlySpan<byte> src) where T : struct, IFixedLengthByteSpanSerializable<T> {
		var hasValue = SerializationReadBool(ref src);
		var value = SerializationRead<T>(ref src);
		return hasValue ? value : null;
	}
}
public interface IConfigStruct<TSelf> : IConfigStruct where TSelf : struct, IConfigStruct<TSelf>, allows ref struct {
	static abstract int GetHeapStorageFormattedLength(in TSelf src);
	static abstract void AllocateAndConvertToHeapStorage(Span<byte> dest, in TSelf src);
	static abstract TSelf ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src);
	static abstract void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src);
}