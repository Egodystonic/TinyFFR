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

/// <summary>
/// Interface representing any resource (generally those created by the factory and its builders).
/// </summary>
/// <remarks>
/// <para>
/// Every resource type in TinyFFR is an opaque handle; i.e. an immutable struct that represents but does not actually contain the resource data.
/// For example, a <see cref="Egodystonic.TinyFFR.World.Camera" /> instance does not actually contain any mutable state or camera data,
/// it only ultimately wraps a pointer to the camera data and a reference to the interface that provides the implementation for that pointer.
/// </para>
/// <para>
/// In other words, resource types contain just two fields internally:
/// <ul>
/// <li>A pointer/handle;</li>
/// <li>A reference to the implementation for operations using that pointer/handle.</li>
/// </ul>
/// </para>
/// <para>
/// It's possible to extract the handle and implementation via the <see cref="ResourceUtils"/> class
/// (<see cref="ResourceUtils.ExtractHandle"/> / <see cref="ResourceUtils.ExtractImplementation"/>).
/// </para>
/// </remarks>
public unsafe interface IResource : IStringSpanNameEnabled {
	internal static readonly int SerializedLengthBytes = sizeof(IntPtr) + sizeof(nuint);

	internal ResourceHandle Handle { get; }
	internal IResourceImplProvider Implementation { get; }
	internal ResourceIdent Ident { get; }
	internal ResourceStub AsStub => new(Ident, Implementation);

	internal static void AllocateGcHandleAndSerializeResource<TResource>(TResource resource, Span<byte> dest) where TResource : IResource<TResource> {
		var gcHandle = GCHandle.Alloc(resource.Implementation, GCHandleType.Normal);
		BinaryPrimitives.WriteIntPtrLittleEndian(dest, GCHandle.ToIntPtr(gcHandle));
		BinaryPrimitives.WriteUIntPtrLittleEndian(dest[IntPtr.Size..], resource.Handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static GCHandle ReadGcHandleFromSerializedResource(ReadOnlySpan<byte> src) => GCHandle.FromIntPtr(BinaryPrimitives.ReadIntPtrLittleEndian(src));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static nuint ReadHandleFromSerializedResource(ReadOnlySpan<byte> src) => BinaryPrimitives.ReadUIntPtrLittleEndian(src[sizeof(IntPtr)..]);
}
/// <inheritdoc cref="IResource" />
/// <typeparam name="TSelf">The type implementing this interface.</typeparam>
public interface IResource<TSelf> : IResource, IEquatable<TSelf> where TSelf : IResource<TSelf> {
	internal new ResourceHandle<TSelf> Handle { get; }
	ResourceHandle IResource.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;

	internal static abstract TSelf CreateFromHandleAndImpl(ResourceHandle<TSelf> handle, IResourceImplProvider impl);
	internal static virtual TSelf CreateFromStub(ResourceStub stub) => TSelf.CreateFromHandleAndImpl(stub.CreateTypedHandleWithTypeCheck<TSelf>(), stub.Implementation);
	internal ResourceHandle<TSelf> GetHandleWithoutDisposeCheck();
	internal static virtual TSelf CreateFromSerializedAndFreeAllocatedGcHandle(ReadOnlySpan<byte> src) {
		var gcHandle = ReadGcHandleFromSerializedResource(src);
		var handle = ReadHandleFromSerializedResource(src);
		var result = TSelf.CreateFromHandleAndImpl(handle, (IResourceImplProvider) gcHandle.Target!);
		gcHandle.Free();
		return result;
	}
}

/// <inheritdoc cref="IResource{TSelf}" />
/// <typeparam name="TSelf">The type implementing this interface.</typeparam>
/// <typeparam name="TImpl">The type that provides the actual implementation for this resource type.</typeparam>
public interface IResource<TSelf, out TImpl> : IResource<TSelf>
	where TSelf : IResource<TSelf> 
	where TImpl : class, IResourceImplProvider {

	internal new TImpl Implementation { get; }
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	internal static TSelf RecreateFromResourceStub(ResourceStub stub) {
		if (stub.TypeHandle != ResourceHandle<TSelf>.TypeHandle) {
			throw new InvalidOperationException($"Type handles do not match. Target type = {typeof(TSelf).Name}; target type handle = {ResourceHandle<TSelf>.TypeHandle}; given type handle = {stub.TypeHandle}.");
		}
		return TSelf.CreateFromHandleAndImpl(stub.CreateTypedHandleWithTypeCheck<TSelf>(), stub.Implementation);
	}
}





/// <inheritdoc cref="IResource" />
/// This type additionally implements <see cref="IDisposable"/>.
public interface IDisposableResource : IResource, IDisposable;
/// <inheritdoc cref="IResource{TSelf}" />
/// This type additionally implements <see cref="IDisposable"/>.
public interface IDisposableResource<TSelf> : IDisposableResource, IResource<TSelf> where TSelf : IDisposableResource<TSelf>;
/// <inheritdoc cref="IResource{TSelf, TImpl}" />
/// This type additionally implements <see cref="IDisposable"/>.
public interface IDisposableResource<TSelf, out TImpl>: IDisposableResource<TSelf>, IResource<TSelf, TImpl> where TSelf : IDisposableResource<TSelf> where TImpl : class, IDisposableResourceImplProvider;