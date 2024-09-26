// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Resources;

public interface IResourceImplProvider {
	internal bool RawHandleIsDisposed(nuint handle);
	internal void RawHandleDispose(nuint handle);
	internal string RawHandleGetName(nuint handle);
	internal int RawHandleGetNameUsingSpan(nuint handle, Span<char> dest);
	internal int RawHandleGetNameSpanLength(nuint handle);
}

public unsafe interface IHandleImplPairResource : IStringSpanNameEnabled, IDisposable {
	internal readonly record struct ResourceIdent(nint TypeHandle, nuint RawResourceHandle);
	internal static readonly int SerializedLengthBytes = sizeof(GCHandle) + sizeof(nuint);

	internal nuint Handle { get; }
	internal IResourceImplProvider Implementation { get; }
	internal ResourceIdent Ident { get; }

	internal void AllocateGcHandleAndSerializeResource(Span<byte> dest) {
		var gcHandle = GCHandle.Alloc(Implementation, GCHandleType.Normal);
		MemoryMarshal.Write(dest, gcHandle);
		BinaryPrimitives.TryWriteUIntPtrLittleEndian(dest[sizeof(GCHandle)..], Handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static GCHandle ReadGcHandleFromSerializedResource(ReadOnlySpan<byte> src) => MemoryMarshal.Read<GCHandle>(src);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static nuint ReadHandleFromSerializedResource(ReadOnlySpan<byte> src) => BinaryPrimitives.ReadUIntPtrLittleEndian(src[sizeof(GCHandle)..]);
}

public interface IHandleImplPairResource<out TSelf> : IHandleImplPairResource where TSelf : IHandleImplPairResource<TSelf> {
	internal static abstract TSelf RecreateFromRawHandleAndImpl(nuint rawHandle, IResourceImplProvider impl);
}

public interface IHandleImplPairResource<out THandle, out TImpl> : IHandleImplPairResource where THandle : unmanaged, IResourceHandle<THandle> where TImpl : class, IResourceImplProvider {
	internal new THandle Handle { get; }
	internal new TImpl Implementation { get; }

	internal static (THandle Handle, TImpl Implementation) ReadResource(ReadOnlySpan<byte> src) {
		var impl = (ReadGcHandleFromSerializedResource(src).Target as TImpl) ?? throw new InvalidOperationException($"Unexpected null implementation.");
		var handle = THandle.CreateFromInteger(ReadHandleFromSerializedResource(src));
		return (handle, impl);
	}
}

public interface IHandleImplPairResource<out TSelf, out THandle, out TImpl>
	: IHandleImplPairResource<TSelf>, IHandleImplPairResource<THandle, TImpl> 
	where TSelf : IHandleImplPairResource<TSelf> 
	where THandle : unmanaged, IResourceHandle<THandle> 
	where TImpl : class, IResourceImplProvider;