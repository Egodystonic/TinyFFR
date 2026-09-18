// Created on 2025-08-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Buffers.Binary;
using System.Diagnostics;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR;

/// <summary>
/// Interface representing any struct type in TinyFFR used to supply configuration to an API.
/// </summary>
/// <seealso cref="IConfigStruct{TSelf}"/>
public interface IConfigStruct {
	/// <summary>
	/// The number of bytes used to record the length of a variable-length field: <c>4</c>.
	/// </summary>
	/// <remarks>
	/// Strings, spans and nested configs are all written with their length in front of them, so that a reader knows how far to
	/// advance without being told separately.
	/// </remarks>
	protected const int SerializationFieldCountSizeBytes = sizeof(int);

	/// <summary>
	/// Returns the number of bytes a <see langword="float"/> occupies in a config struct's marshalled form.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOfFloat() => sizeof(float);
	/// <summary>
	/// Returns the number of bytes an <see langword="int"/> occupies in a config struct's marshalled form.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOfInt() => sizeof(int);
	/// <summary>
	/// Returns the number of bytes a <see langword="long"/> occupies in a config struct's marshalled form.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOfLong() => sizeof(long);
	/// <summary>
	/// Returns the number of bytes a <see langword="bool"/> occupies in a config struct's marshalled form.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOfBool() => 1;
	/// <summary>
	/// Returns the number of bytes a value of a type that serializes to a fixed number of bytes occupies in a config struct's marshalled form.
	/// </summary>
	/// <typeparam name="T">The type being measured.</typeparam>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOf<T>() where T : IFixedLengthByteSpanSerializable<T> => T.SerializationByteSpanLength;
	/// <summary>
	/// Returns the number of bytes the given string, including the length written in front of it occupies in a config struct's marshalled form.
	/// </summary>
	/// <param name="v">The string that would be written.</param>
	protected static int SerializationSizeOfString(ReadOnlySpan<char> v) => SerializationFieldCountSizeBytes + v.Length * sizeof(char);
	/// <summary>
	/// Returns the number of bytes the given nested config, including the length written in front of it occupies in a config struct's marshalled form.
	/// </summary>
	/// <typeparam name="T">The nested config's type.</typeparam>
	/// <param name="v">The nested config that would be written.</param>
	protected static int SerializationSizeOfSubConfig<T>(scoped in T v) where T : struct, IConfigStruct<T>, allows ref struct => SerializationFieldCountSizeBytes + T.GetHeapStorageFormattedLength(v);
	/// <summary>
	/// Returns the number of bytes a resource handle occupies in a config struct's marshalled form.
	/// </summary>
	protected static int SerializationSizeOfResource() => IResource.SerializedLengthBytes;
	/// <summary>
	/// Returns the number of bytes the given span, including the length written in front of it occupies in a config struct's marshalled form.
	/// </summary>
	/// <typeparam name="T">The span's element type.</typeparam>
	/// <param name="v">The span that would be written.</param>
	protected static int SerializationSizeOfSpan<T>(ReadOnlySpan<T> v) where T : unmanaged => SerializationFieldCountSizeBytes + MemoryMarshal.AsBytes(v).Length;
	/// <summary>
	/// Returns the number of bytes a nullable <see langword="float"/>, including the flag saying whether it has a value occupies in a config struct's marshalled form.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOfNullableFloat() => sizeof(bool) + sizeof(float);
	/// <summary>
	/// Returns the number of bytes a nullable <see langword="int"/>, including the flag saying whether it has a value occupies in a config struct's marshalled form.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOfNullableInt() => sizeof(bool) + sizeof(int);
	/// <summary>
	/// Returns the number of bytes a nullable <see langword="long"/>, including the flag saying whether it has a value occupies in a config struct's marshalled form.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOfNullableLong() => sizeof(bool) + sizeof(long);
	/// <summary>
	/// Returns the number of bytes a nullable <see langword="bool"/>, including the flag saying whether it has a value occupies in a config struct's marshalled form.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOfNullableBool() => sizeof(bool) + sizeof(bool);
	/// <summary>
	/// Returns the number of bytes a nullable resource handle, including the flag saying whether it has a value occupies in a config struct's marshalled form.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SerializationSizeOfNullableResource() => sizeof(bool) + IResource.SerializedLengthBytes;
	/// <summary>
	/// Returns the number of bytes a nullable value of a type that serializes to a fixed number of bytes, including the flag saying whether it has a value occupies in a config struct's marshalled form.
	/// </summary>
	/// <typeparam name="T">The type being measured.</typeparam>
	protected static int SerializationSizeOfNullable<T>() where T : IFixedLengthByteSpanSerializable<T> => sizeof(bool) + T.SerializationByteSpanLength;

	/// <summary>
	/// Writes a <see langword="float"/> to the given buffer.
	/// </summary>
	/// <param name="dest">The buffer to write in to. On return this is advanced past the bytes written, so that the next write continues where this one left off. Must be long enough to hold the value.</param>
	/// <param name="v">The value to write.</param>
	protected static void SerializationWriteFloat(scoped ref Span<byte> dest, float v) {
		BinaryPrimitives.WriteSingleLittleEndian(dest, v);
		dest = dest[sizeof(float)..];
	}
	/// <summary>
	/// Writes an <see langword="int"/> to the given buffer.
	/// </summary>
	/// <param name="dest">The buffer to write in to. On return this is advanced past the bytes written, so that the next write continues where this one left off. Must be long enough to hold the value.</param>
	/// <param name="v">The value to write.</param>
	protected static void SerializationWriteInt(scoped ref Span<byte> dest, int v) {
		BinaryPrimitives.WriteInt32LittleEndian(dest, v);
		dest = dest[sizeof(int)..];
	}
	/// <summary>
	/// Writes a <see langword="long"/> to the given buffer.
	/// </summary>
	/// <param name="dest">The buffer to write in to. On return this is advanced past the bytes written, so that the next write continues where this one left off. Must be long enough to hold the value.</param>
	/// <param name="v">The value to write.</param>
	protected static void SerializationWriteLong(scoped ref Span<byte> dest, long v) {
		BinaryPrimitives.WriteInt64LittleEndian(dest, v);
		dest = dest[sizeof(long)..];
	}
	/// <summary>
	/// Writes a <see langword="bool"/> to the given buffer.
	/// </summary>
	/// <param name="dest">The buffer to write in to. On return this is advanced past the bytes written, so that the next write continues where this one left off. Must be long enough to hold the value.</param>
	/// <param name="v">The value to write.</param>
	protected static void SerializationWriteBool(scoped ref Span<byte> dest, bool v) {
		dest[0] = v ? Byte.MaxValue : Byte.MinValue;
		dest = dest[1..];
	}
	/// <summary>
	/// Writes a value of a type that serializes to a fixed number of bytes to the given buffer.
	/// </summary>
	/// <param name="dest">The buffer to write in to. On return this is advanced past the bytes written, so that the next write continues where this one left off. Must be long enough to hold the value.</param>
	/// <param name="v">The value to write.</param>
	/// <typeparam name="T">The type being written.</typeparam>
	protected static void SerializationWrite<T>(scoped ref Span<byte> dest, T v) where T : IFixedLengthByteSpanSerializable<T> {
		T.SerializeToBytes(dest, v);
		dest = dest[T.SerializationByteSpanLength..];
	}
	/// <summary>
	/// Writes a string, preceded by its length to the given buffer.
	/// </summary>
	/// <remarks>
	/// The string's characters are copied as-is; no encoding conversion takes place.
	/// </remarks>
	/// <param name="dest">The buffer to write in to. On return this is advanced past the bytes written, so that the next write continues where this one left off. Must be long enough to hold the value.</param>
	/// <param name="v">The string to write.</param>
	protected static void SerializationWriteString(scoped ref Span<byte> dest, ReadOnlySpan<char> v) {
		var numBytesWritten = v.Length * sizeof(char);
		BinaryPrimitives.WriteInt32LittleEndian(dest, numBytesWritten);
		MemoryMarshal.AsBytes(v).CopyTo(dest[sizeof(int)..]);
		dest = dest[(sizeof(int) + numBytesWritten)..];
	}
	/// <summary>
	/// Writes a nested config struct, preceded by its length to the given buffer.
	/// </summary>
	/// <param name="dest">The buffer to write in to. On return this is advanced past the bytes written, so that the next write continues where this one left off. Must be long enough to hold the value.</param>
	/// <typeparam name="T">The nested config's type.</typeparam>
	/// <param name="v">The nested config to write.</param>
	protected static void SerializationWriteSubConfig<T>(scoped ref Span<byte> dest, scoped in T v) where T : struct, IConfigStruct<T>, allows ref struct {
		var byteCount = T.GetHeapStorageFormattedLength(v);
		BinaryPrimitives.WriteInt32LittleEndian(dest, byteCount);
		T.AllocateAndConvertToHeapStorage(dest[sizeof(int)..], v);
		dest = dest[(sizeof(int) + byteCount)..];
	}
	/// <summary>
	/// Writes a resource handle, allocating the handle that keeps its implementation alive to the given buffer.
	/// </summary>
	/// <remarks>
	/// The allocated handle must later be released with <see cref="SerializationDisposeResourceHandle"/>, or it pins
	/// its implementation for the lifetime of the process.
	/// </remarks>
	/// <param name="dest">The buffer to write in to. On return this is advanced past the bytes written, so that the next write continues where this one left off. Must be long enough to hold the value.</param>
	/// <typeparam name="T">The resource's type.</typeparam>
	/// <param name="v">The resource to write.</param>
	protected static void SerializationWriteAndAllocateResource<T>(scoped ref Span<byte> dest, T v) where T : IResource<T> {
		IResource.AllocateGcHandleAndSerializeResource(v, dest);
		dest = dest[SerializationSizeOfResource()..];
	}
	/// <summary>
	/// Writes a span of values, preceded by its length in bytes to the given buffer.
	/// </summary>
	/// <param name="dest">The buffer to write in to. On return this is advanced past the bytes written, so that the next write continues where this one left off. Must be long enough to hold the value.</param>
	/// <typeparam name="T">The span's element type.</typeparam>
	/// <param name="v">The span to write.</param>
	protected static void SerializationWriteSpan<T>(scoped ref Span<byte> dest, ReadOnlySpan<T> v) where T : unmanaged {
		var byteSpan = MemoryMarshal.AsBytes(v);
		SerializationWriteInt(ref dest, byteSpan.Length);
		byteSpan.CopyTo(dest);
		dest = dest[byteSpan.Length..];
	}
	/// <summary>
	/// Writes a nullable <see langword="float"/>, preceded by a flag saying whether it has a value to the given buffer.
	/// </summary>
	/// <param name="dest">The buffer to write in to. On return this is advanced past the bytes written, so that the next write continues where this one left off. Must be long enough to hold the value.</param>
	/// <param name="v">The value to write.</param>
	protected static void SerializationWriteNullableFloat(scoped ref Span<byte> dest, float? v) {
		SerializationWriteBool(ref dest, v.HasValue);
		BinaryPrimitives.WriteSingleLittleEndian(dest, v ?? default);
		dest = dest[sizeof(float)..];
	}
	/// <summary>
	/// Writes a nullable <see langword="int"/>, preceded by a flag saying whether it has a value to the given buffer.
	/// </summary>
	/// <param name="dest">The buffer to write in to. On return this is advanced past the bytes written, so that the next write continues where this one left off. Must be long enough to hold the value.</param>
	/// <param name="v">The value to write.</param>
	protected static void SerializationWriteNullableInt(scoped ref Span<byte> dest, int? v) {
		SerializationWriteBool(ref dest, v.HasValue);
		BinaryPrimitives.WriteInt32LittleEndian(dest, v ?? default);
		dest = dest[sizeof(int)..];
	}
	/// <summary>
	/// Writes a nullable <see langword="long"/>, preceded by a flag saying whether it has a value to the given buffer.
	/// </summary>
	/// <param name="dest">The buffer to write in to. On return this is advanced past the bytes written, so that the next write continues where this one left off. Must be long enough to hold the value.</param>
	/// <param name="v">The value to write.</param>
	protected static void SerializationWriteNullableLong(scoped ref Span<byte> dest, long? v) {
		SerializationWriteBool(ref dest, v.HasValue);
		BinaryPrimitives.WriteInt64LittleEndian(dest, v ?? default);
		dest = dest[sizeof(long)..];
	}
	/// <summary>
	/// Writes a nullable <see langword="bool"/>, preceded by a flag saying whether it has a value to the given buffer.
	/// </summary>
	/// <param name="dest">The buffer to write in to. On return this is advanced past the bytes written, so that the next write continues where this one left off. Must be long enough to hold the value.</param>
	/// <param name="v">The value to write.</param>
	protected static void SerializationWriteNullableBool(scoped ref Span<byte> dest, bool? v) {
		SerializationWriteBool(ref dest, v.HasValue);
		dest[0] = (v ?? default) ? Byte.MaxValue : Byte.MinValue;
		dest = dest[1..];
	}
	/// <summary>
	/// Writes a nullable resource handle, preceded by a flag saying whether it has a value to the given buffer.
	/// </summary>
	/// <remarks>
	/// A handle is only allocated when there is a resource to write.
	/// </remarks>
	/// <param name="dest">The buffer to write in to. On return this is advanced past the bytes written, so that the next write continues where this one left off. Must be long enough to hold the value.</param>
	/// <typeparam name="T">The resource's type.</typeparam>
	/// <param name="v">The resource to write, or <see langword="null"/> for none.</param>
	protected static void SerializationWriteAndAllocateNullableResource<T>(scoped ref Span<byte> dest, T? v) where T : struct, IResource<T> {
		SerializationWriteBool(ref dest, v.HasValue);
		if (v.HasValue) IResource.AllocateGcHandleAndSerializeResource(v.Value, dest);
		else dest[..SerializationSizeOfResource()].Clear();
		dest = dest[SerializationSizeOfResource()..];
	}
	/// <summary>
	/// Writes a nullable value of a type that serializes to a fixed number of bytes, preceded by a flag saying whether it has a value to the given buffer.
	/// </summary>
	/// <param name="dest">The buffer to write in to. On return this is advanced past the bytes written, so that the next write continues where this one left off. Must be long enough to hold the value.</param>
	/// <param name="v">The value to write.</param>
	/// <typeparam name="T">The type being written.</typeparam>
	protected static void SerializationWriteNullable<T>(scoped ref Span<byte> dest, T? v) where T : struct, IFixedLengthByteSpanSerializable<T> {
		SerializationWriteBool(ref dest, v.HasValue);
		T.SerializeToBytes(dest, v ?? default);
		dest = dest[T.SerializationByteSpanLength..];
	}

	/// <summary>
	/// Reads a <see langword="float"/> from the given buffer.
	/// </summary>
	/// <param name="src">The buffer to read from. On return this is advanced past the bytes read, so that the next read continues where this one left off.</param>
	protected static float SerializationReadFloat(scoped ref ReadOnlySpan<byte> src) {
		var result = BinaryPrimitives.ReadSingleLittleEndian(src);
		src = src[sizeof(float)..];
		return result;
	}
	/// <summary>
	/// Reads an <see langword="int"/> from the given buffer.
	/// </summary>
	/// <param name="src">The buffer to read from. On return this is advanced past the bytes read, so that the next read continues where this one left off.</param>
	protected static int SerializationReadInt(scoped ref ReadOnlySpan<byte> src) {
		var result = BinaryPrimitives.ReadInt32LittleEndian(src);
		src = src[sizeof(int)..];
		return result;
	}
	/// <summary>
	/// Reads a <see langword="long"/> from the given buffer.
	/// </summary>
	/// <param name="src">The buffer to read from. On return this is advanced past the bytes read, so that the next read continues where this one left off.</param>
	protected static long SerializationReadLong(scoped ref ReadOnlySpan<byte> src) {
		var result = BinaryPrimitives.ReadInt64LittleEndian(src);
		src = src[sizeof(long)..];
		return result;
	}
	/// <summary>
	/// Reads a <see langword="bool"/> from the given buffer.
	/// </summary>
	/// <param name="src">The buffer to read from. On return this is advanced past the bytes read, so that the next read continues where this one left off.</param>
	protected static bool SerializationReadBool(scoped ref ReadOnlySpan<byte> src) {
		var result = src[0] > 0;
		src = src[1..];
		return result;
	}
	/// <summary>
	/// Reads a value of a type that serializes to a fixed number of bytes from the given buffer.
	/// </summary>
	/// <param name="src">The buffer to read from. On return this is advanced past the bytes read, so that the next read continues where this one left off.</param>
	/// <typeparam name="T">The type being read.</typeparam>
	protected static T SerializationRead<T>(scoped ref ReadOnlySpan<byte> src) where T : IFixedLengthByteSpanSerializable<T> {
		var result = T.DeserializeFromBytes(src);
		src = src[T.SerializationByteSpanLength..];
		return result;
	}
	/// <summary>
	/// Reads a string that was written with its length in front of it from the given buffer.
	/// </summary>
	/// <remarks>
	/// The result points in to the buffer rather than copying out of it, so it is only valid for as long as the buffer is.
	/// </remarks>
	/// <param name="src">The buffer to read from. On return this is advanced past the bytes read, so that the next read continues where this one left off.</param>
	protected static ReadOnlySpan<char> SerializationReadString(scoped ref ReadOnlySpan<byte> src) {
		var byteCount = BinaryPrimitives.ReadInt32LittleEndian(src);
		var strEnd = (sizeof(int) + byteCount);
		var result = MemoryMarshal.Cast<byte, char>(src[sizeof(int)..strEnd]);
		src = src[strEnd..];
		return result;
	}
	/// <summary>
	/// Reads a nested config struct that was written with its length in front of it from the given buffer.
	/// </summary>
	/// <param name="src">The buffer to read from. On return this is advanced past the bytes read, so that the next read continues where this one left off.</param>
	/// <typeparam name="T">The nested config's type.</typeparam>
	protected static T SerializationReadSubConfig<T>(scoped ref ReadOnlySpan<byte> src) where T : struct, IConfigStruct<T>, allows ref struct {
		var byteCount = BinaryPrimitives.ReadInt32LittleEndian(src);
		var cfgEnd = sizeof(int) + byteCount;
		var result = T.ConvertFromAllocatedHeapStorage(src[sizeof(int)..cfgEnd]);
		src = src[cfgEnd..];
		return result;
	}
	/// <summary>
	/// Releases the resources held by a nested config struct that was written in to the given buffer, and advances past it.
	/// </summary>
	/// <remarks>
	/// Write a dispose method as a replay of the corresponding read method, so that every nested config is reached in the same
	/// order it was written. A nested config that is never reached leaks whatever handles it allocated.
	/// </remarks>
	/// <typeparam name="T">The nested config's type.</typeparam>
	/// <param name="src">The buffer to read from. On return this is advanced past the bytes read, so that the next read continues where this one left off.</param>
	protected static void SerializationDisposeSubConfig<T>(scoped ref ReadOnlySpan<byte> src) where T : struct, IConfigStruct<T>, allows ref struct {
		var byteCount = BinaryPrimitives.ReadInt32LittleEndian(src);
		var cfgEnd = sizeof(int) + byteCount;
		T.DisposeAllocatedHeapStorage(src[sizeof(int)..cfgEnd]);
		src = src[cfgEnd..];
	}
	/// <summary>
	/// Reads a resource handle from the given buffer.
	/// </summary>
	/// <param name="src">The buffer to read from. On return this is advanced past the bytes read, so that the next read continues where this one left off.</param>
	/// <typeparam name="T">The resource's type.</typeparam>
	protected static T SerializationReadResource<T>(scoped ref ReadOnlySpan<byte> src) where T : IResource<T> {
		var result = T.CreateFromHandleAndImpl(
			IResource.ReadHandleFromSerializedResource(src),
			(IResourceImplProvider) IResource.ReadGcHandleFromSerializedResource(src).Target!
		);
		src = src[SerializationSizeOfResource()..];
		return result;
	}
	/// <summary>
	/// Reads a span of values that was written with its length in front of it from the given buffer.
	/// </summary>
	/// <remarks>
	/// The result points in to the buffer rather than copying out of it, so it is only valid for as long as the buffer is.
	/// </remarks>
	/// <param name="src">The buffer to read from. On return this is advanced past the bytes read, so that the next read continues where this one left off.</param>
	/// <typeparam name="T">The span's element type.</typeparam>
	protected static ReadOnlySpan<T> SerializationReadSpan<T>(scoped ref ReadOnlySpan<byte> src) where T : unmanaged {
		var length = SerializationReadInt(ref src);
		Debug.Assert(length % Unsafe.SizeOf<T>() == 0, $"Serialized span byte length {length} is not a whole multiple of sizeof({typeof(T).Name}) ({Unsafe.SizeOf<T>()}); MemoryMarshal.Cast would silently drop the ragged tail.");
		var result = MemoryMarshal.Cast<byte, T>(src[..length]);
		src = src[length..];
		return result;
	}
	/// <summary>
	/// Releases the handle that a written resource allocated to keep its implementation alive.
	/// </summary>
	/// <remarks>
	/// This must be done exactly once for each resource written, and is what stops a marshalled config pinning its resources
	/// for the lifetime of the process.
	/// </remarks>
	/// <param name="resourceData">The buffer positioned at the start of the written resource.</param>
	protected static void SerializationDisposeResourceHandle(ReadOnlySpan<byte> resourceData) {
		IResource.ReadGcHandleFromSerializedResource(resourceData).Free();
	}
	/// <summary>
	/// Releases the handle that a written nullable resource allocated, if it had a value.
	/// </summary>
	/// <param name="flagAndResourceData">The buffer positioned at the start of the flag preceding the written resource.</param>
	protected static void SerializationDisposeNullableResourceHandle(ReadOnlySpan<byte> flagAndResourceData) {
		if (SerializationReadBool(ref flagAndResourceData)) SerializationDisposeResourceHandle(flagAndResourceData);
	}

	/// <summary>
	/// Reads a nullable <see langword="float"/> that was written with a flag saying whether it has a value from the given buffer.
	/// </summary>
	/// <param name="src">The buffer to read from. On return this is advanced past the bytes read, so that the next read continues where this one left off.</param>
	protected static float? SerializationReadNullableFloat(scoped ref ReadOnlySpan<byte> src) {
		var hasValue = SerializationReadBool(ref src);
		var value = SerializationReadFloat(ref src);
		return hasValue ? value : null;
	}
	/// <summary>
	/// Reads a nullable <see langword="int"/> that was written with a flag saying whether it has a value from the given buffer.
	/// </summary>
	/// <param name="src">The buffer to read from. On return this is advanced past the bytes read, so that the next read continues where this one left off.</param>
	protected static int? SerializationReadNullableInt(scoped ref ReadOnlySpan<byte> src) {
		var hasValue = SerializationReadBool(ref src);
		var value = SerializationReadInt(ref src);
		return hasValue ? value : null;
	}
	/// <summary>
	/// Reads a nullable <see langword="long"/> that was written with a flag saying whether it has a value from the given buffer.
	/// </summary>
	/// <param name="src">The buffer to read from. On return this is advanced past the bytes read, so that the next read continues where this one left off.</param>
	protected static long? SerializationReadNullableLong(scoped ref ReadOnlySpan<byte> src) {
		var hasValue = SerializationReadBool(ref src);
		var value = SerializationReadLong(ref src);
		return hasValue ? value : null;
	}
	/// <summary>
	/// Reads a nullable <see langword="bool"/> that was written with a flag saying whether it has a value from the given buffer.
	/// </summary>
	/// <param name="src">The buffer to read from. On return this is advanced past the bytes read, so that the next read continues where this one left off.</param>
	protected static bool? SerializationReadNullableBool(scoped ref ReadOnlySpan<byte> src) {
		var hasValue = SerializationReadBool(ref src);
		var value = SerializationReadBool(ref src);
		return hasValue ? value : null;
	}
	/// <summary>
	/// Reads a nullable resource handle that was written with a flag saying whether it has a value from the given buffer.
	/// </summary>
	/// <param name="src">The buffer to read from. On return this is advanced past the bytes read, so that the next read continues where this one left off.</param>
	/// <typeparam name="T">The resource's type.</typeparam>
	protected static T? SerializationReadNullableResource<T>(scoped ref ReadOnlySpan<byte> src) where T : struct, IResource<T> {
		var hasValue = SerializationReadBool(ref src);
		if (hasValue) return SerializationReadResource<T>(ref src);

		src = src[SerializationSizeOfResource()..];
		return null;
	}
	/// <summary>
	/// Reads a nullable value of a type that serializes to a fixed number of bytes, written with a flag saying whether it has a value from the given buffer.
	/// </summary>
	/// <param name="src">The buffer to read from. On return this is advanced past the bytes read, so that the next read continues where this one left off.</param>
	/// <typeparam name="T">The type being read.</typeparam>
	protected static T? SerializationReadNullable<T>(scoped ref ReadOnlySpan<byte> src) where T : struct, IFixedLengthByteSpanSerializable<T> {
		var hasValue = SerializationReadBool(ref src);
		var value = SerializationRead<T>(ref src);
		return hasValue ? value : null;
	}
}
/// <summary>
/// Interface representing any struct type in TinyFFR used to supply configuration to an API.
/// </summary>
/// <remarks>
/// <para>
/// As many config struct types in TinyFFR are <c>ref struct</c>s they can not easily be stored on the heap or on other objects.
/// This interface exposes static members that can be used to marshal a given config object to binary representation to be stored on
/// the heap, and methods that marshal that data back in to the config struct type.
/// </para>
/// <para>
/// Note that the marshalled data is not safe to store to disc, send across a network, or even outlive the currently active
/// <see cref="Egodystonic.TinyFFR.Factory.ITinyFfrFactory">factory</see>. This is because it may or may not contain local-runtime-specific data (such as <see cref="GCHandle"/>s or
/// <see cref="ResourceHandle"/>s to data only active for the current factory). 
/// </para>
/// </remarks>
/// <typeparam name="TSelf">The config struct type.</typeparam>
public interface IConfigStruct<TSelf> : IConfigStruct where TSelf : struct, IConfigStruct<TSelf>, allows ref struct {
	/// <summary>
	/// Returns the size in bytes required for a buffer to store the given config struct.
	/// </summary>
	/// <param name="src">The config struct you wish to potentially marshal to binary format.</param>
	static abstract int GetHeapStorageFormattedLength(in TSelf src);
	/// <summary>
	/// Allocates internal handles if necessary and marshals the given <paramref name="src"/> struct to
	/// binary format in to <paramref name="dest"/>. This data can then be stored on the heap (i.e. in an
	/// array, Memory&lt;byte&gt;, List, etc).
	/// </summary>
	/// <remarks>
	/// Note that you <b>must</b> invoke <see cref="DisposeAllocatedHeapStorage"/> on this binary data once
	/// before disposing the currently-active <see cref="Egodystonic.TinyFFR.Factory.ITinyFfrFactory"/> or you
	/// will risk leaking handle resources and/or memory.
	/// </remarks>
	/// <param name="dest">The destination buffer to write to. Its length must be at least enough to accomodate the data
	/// (as exposed by <see cref="GetHeapStorageFormattedLength"/>).</param>
	/// <param name="src">The config struct you wish to marshal to binary format.</param>
	static abstract void AllocateAndConvertToHeapStorage(Span<byte> dest, in TSelf src);
	/// <summary>
	/// Converts previously-marshalled binary data of type <typeparamref name="TSelf"/> back in to its
	/// struct representation.
	/// </summary>
	/// <param name="src">The binary data that was previously marshalled using <see cref="AllocateAndConvertToHeapStorage"/>.
	/// The data must <b>not</b> have been disposed via <see cref="DisposeAllocatedHeapStorage"/>. This span is expected to
	/// have a length <b>exactly</b> as large as the binary data.</param>
	static abstract TSelf ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src);
	/// <summary>
	/// Disposes previously-allocated binary data.
	/// This function must be invoked once and only once on the binary data returned by each invocation of
	/// <see cref="AllocateAndConvertToHeapStorage"/>.
	/// </summary>
	/// <param name="src">The binary data that was previously marshalled using <see cref="AllocateAndConvertToHeapStorage"/>.
	/// The data must <b>not</b> have been disposed already. This span is expected to
	/// have a length <b>exactly</b> as large as the binary data.</param>
	static abstract void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src);
}