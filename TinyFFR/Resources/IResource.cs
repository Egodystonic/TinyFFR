// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using Egodystonic.TinyFFR.Resources.Memory;
using static Egodystonic.TinyFFR.Resources.IResource;

namespace Egodystonic.TinyFFR.Resources;

readonly record struct ResourceIdent(nint TypeHandle, nuint RawResourceHandle);
readonly record struct ResourceStub(ResourceIdent Ident, IResourceImplProvider Implementation) : IDisposableResource {
	public bool IsDisposed => (Implementation as IDisposableResourceImplProvider)?.IsDisposed(Handle) ?? false;
	public void Dispose() => (Implementation as IDisposableResourceImplProvider)?.Dispose(Handle);
	public ResourceHandle Handle => Ident.RawResourceHandle;
	public nint TypeHandle => Ident.TypeHandle;
	public ResourceHandle<TResource> CreateTypedHandleWithTypeCheck<TResource>() where TResource : IResource<TResource> {
		return ResourceHandle<TResource>.TypeHandle == TypeHandle 
			? (ResourceHandle<TResource>) Handle
			: throw new InvalidOperationException($"Resource was not of type {typeof(TResource).Name}.");
	}
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(Handle);
	public int GetNameLength() => Implementation.GetNameLength(Handle);
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(Handle, destinationBuffer);
}

public unsafe interface IResource : IStringSpanNameEnabled {
	internal static readonly int SerializedLengthBytes = sizeof(IntPtr) + sizeof(nuint);

	internal ResourceHandle Handle { get; }
	internal IResourceImplProvider Implementation { get; }
	internal ResourceIdent Ident { get; }
	internal ResourceStub AsStub => new(Ident, Implementation);

	internal void AllocateGcHandleAndSerializeResource(Span<byte> dest) {
		var gcHandle = GCHandle.Alloc(Implementation, GCHandleType.Normal);
		BinaryPrimitives.WriteIntPtrLittleEndian(dest, GCHandle.ToIntPtr(gcHandle));
		BinaryPrimitives.WriteUIntPtrLittleEndian(dest[IntPtr.Size..], Handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static GCHandle ReadGcHandleFromSerializedResource(ReadOnlySpan<byte> src) => GCHandle.FromIntPtr(BinaryPrimitives.ReadIntPtrLittleEndian(src));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static nuint ReadHandleFromSerializedResource(ReadOnlySpan<byte> src) => BinaryPrimitives.ReadUIntPtrLittleEndian(src[sizeof(IntPtr)..]);
}
public interface IResource<TSelf> : IResource, IEquatable<TSelf> where TSelf : IResource<TSelf> {
	internal new ResourceHandle<TSelf> Handle { get; }
	ResourceHandle IResource.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;

	internal static abstract TSelf CreateFromHandleAndImpl(ResourceHandle<TSelf> handle, IResourceImplProvider impl);
	internal static virtual TSelf CreateFromStub(ResourceStub stub) => TSelf.CreateFromHandleAndImpl(stub.CreateTypedHandleWithTypeCheck<TSelf>(), stub.Implementation);
}
public interface IResource<TSelf, out TImpl> : IResource<TSelf>
	where TSelf : IResource<TSelf> 
	where TImpl : class, IResourceImplProvider {

	internal new TImpl Implementation { get; }
	IResourceImplProvider IResource.Implementation => Implementation;

	internal static TSelf RecreateFromResourceStub(ResourceStub stub) {
		if (stub.TypeHandle != ResourceHandle<TSelf>.TypeHandle) {
			throw new InvalidOperationException($"Type handles do not match. Target type = {typeof(TSelf).Name}; target type handle = {ResourceHandle<TSelf>.TypeHandle}; given type handle = {stub.TypeHandle}.");
		}
		return TSelf.CreateFromHandleAndImpl(stub.CreateTypedHandleWithTypeCheck<TSelf>(), stub.Implementation);
	}
}





public interface IDisposableResource : IResource, IDisposable;
public interface IDisposableResource<TSelf> : IDisposableResource, IResource<TSelf> where TSelf : IDisposableResource<TSelf>;
public interface IDisposableResource<TSelf, out TImpl>: IDisposableResource<TSelf>, IResource<TSelf, TImpl> where TSelf : IDisposableResource<TSelf> where TImpl : class, IDisposableResourceImplProvider;