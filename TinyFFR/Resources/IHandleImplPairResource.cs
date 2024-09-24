// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using Egodystonic.TinyFFR.Assets;

namespace Egodystonic.TinyFFR.Resources;

unsafe interface IHandleImplResource {
	public static readonly int SerializedLengthBytes = sizeof(GCHandle) + sizeof(nuint);

	nuint Handle { get; }
	object Implementation { get; }
	
	void AllocateGcHandleAndSerializeResource(Span<byte> dest) {
		var gcHandle = GCHandle.Alloc(Implementation, GCHandleType.Normal);
		MemoryMarshal.Write(dest, gcHandle);
		BinaryPrimitives.TryWriteUIntPtrLittleEndian(dest[sizeof(GCHandle)..], Handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static GCHandle ReadGcHandleFromSerializedResource(ReadOnlySpan<byte> src) => MemoryMarshal.Read<GCHandle>(src);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static nuint ReadHandleFromSerializedResource(ReadOnlySpan<byte> src) => BinaryPrimitives.ReadUIntPtrLittleEndian(src[sizeof(GCHandle)..]);
}

interface IHandleImplResource<out THandle, out TImpl> : IHandleImplResource where THandle : unmanaged, IResourceHandle<THandle> where TImpl : class {
	new THandle Handle { get; }
	new TImpl Implementation { get; }

	public static (THandle Handle, TImpl Implementation) ReadResource(ReadOnlySpan<byte> src) {
		var impl = (ReadGcHandleFromSerializedResource(src).Target as TImpl) ?? throw new InvalidOperationException($"Unexpected null implementation.");
		var handle = THandle.CreateFromInteger(ReadHandleFromSerializedResource(src));
		return (handle, impl);
	}
}