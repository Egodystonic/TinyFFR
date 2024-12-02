// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using Egodystonic.TinyFFR.Resources.Memory;
using static Egodystonic.TinyFFR.Resources.IResource;

namespace Egodystonic.TinyFFR.Resources;

readonly record struct ResourceIdent(nint TypeHandle, nuint RawResourceHandle);
readonly record struct ResourceStub(ResourceIdent Ident, IResourceImplProvider Implementation) : IDisposableResource {
	public ReadOnlySpan<char> Name => Implementation.RawHandleGetName(Ident.RawResourceHandle);
	public bool IsDisposed => (Implementation as IDisposableResourceImplProvider)?.RawHandleIsDisposed(Ident.RawResourceHandle) ?? false;
	public void Dispose() => (Implementation as IDisposableResourceImplProvider)?.RawHandleDispose(Ident.RawResourceHandle);
	public nuint Handle => Ident.RawResourceHandle;
	public nint TypeHandle => Ident.TypeHandle;
}

public unsafe interface IResource : IStringSpanNameEnabled {
	internal static readonly int SerializedLengthBytes = sizeof(GCHandle) + sizeof(nuint);

	internal nuint Handle { get; }
	internal IResourceImplProvider Implementation { get; }
	internal ResourceIdent Ident { get; }
	internal ResourceStub AsStub => new(Ident, Implementation);

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
public interface IResource<TSelf> : IResource, IEquatable<TSelf> where TSelf : IResource<TSelf> {
	internal static abstract TSelf RecreateFromRawHandleAndImpl(nuint rawHandle, IResourceImplProvider impl);
	internal static virtual TSelf RecreateFromStub(ResourceStub stub) => TSelf.RecreateFromRawHandleAndImpl(stub.Handle, stub.Implementation);
}
public interface IResource<out THandle, out TImpl> : IResource where THandle : unmanaged, IResourceHandle<THandle> where TImpl : class, IResourceImplProvider {
	internal new THandle Handle { get; }
	internal new TImpl Implementation { get; }

	nuint IResource.Handle => Handle.AsInteger;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceIdent IResource.Ident => Handle.Ident;

	internal static (THandle Handle, TImpl Implementation) ReadResource(ReadOnlySpan<byte> src) {
		var impl = (ReadGcHandleFromSerializedResource(src).Target as TImpl) ?? throw new InvalidOperationException($"Unexpected null implementation.");
		var handle = THandle.CreateFromInteger(ReadHandleFromSerializedResource(src));
		return (handle, impl);
	}
}
public interface IResource<TSelf, out THandle, out TImpl>
	: IResource<TSelf>, IResource<THandle, TImpl> 
	where TSelf : IResource<TSelf> 
	where THandle : unmanaged, IResourceHandle<THandle> 
	where TImpl : class, IResourceImplProvider {
	internal static TSelf RecreateFromResourceStub(ResourceStub stub) {
		if (stub.TypeHandle != THandle.TypeHandle) {
			throw new InvalidOperationException($"Type handles do not match. Target type = {typeof(THandle).Name}; target type handle = {THandle.TypeHandle}; given type handle = {stub.TypeHandle}.");
		}
		return TSelf.RecreateFromRawHandleAndImpl(stub.Handle, stub.Implementation);
	}
}





public interface IDisposableResource : IResource, IDisposable;
public interface IDisposableResource<TSelf> : IDisposableResource, IResource<TSelf> where TSelf : IDisposableResource<TSelf>;
public interface IDisposableResource<out THandle, out TImpl> : IDisposableResource, IResource<THandle, TImpl> where THandle : unmanaged, IResourceHandle<THandle> where TImpl : class, IDisposableResourceImplProvider;
public interface IDisposableResource<TSelf, out THandle, out TImpl>: IDisposableResource<TSelf>, IDisposableResource<THandle, TImpl>, IResource<TSelf, THandle, TImpl>
	where TSelf : IDisposableResource<TSelf>
	where THandle : unmanaged, IResourceHandle<THandle>
	where TImpl : class, IDisposableResourceImplProvider;