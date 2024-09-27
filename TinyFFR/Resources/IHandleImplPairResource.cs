// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using Egodystonic.TinyFFR.Resources.Memory;
using static Egodystonic.TinyFFR.Resources.IHandleImplPairResource;

namespace Egodystonic.TinyFFR.Resources;

public interface IResourceImplProvider {
	internal bool RawHandleIsDisposed(nuint handle);
	internal void RawHandleDispose(nuint handle);
	internal string RawHandleGetName(nuint handle);
	internal int RawHandleGetNameUsingSpan(nuint handle, Span<char> dest);
	internal int RawHandleGetNameSpanLength(nuint handle);
}

public interface IResourceImplProvider<in THandle> : IResourceImplProvider where THandle : IResourceHandle<THandle> {
	bool IsDisposed(THandle handle);
	void Dispose(THandle handle);
	string GetName(THandle handle);
	int GetNameUsingSpan(THandle handle, Span<char> dest);
	int GetNameSpanLength(THandle handle);

	bool IResourceImplProvider.RawHandleIsDisposed(nuint handle) => IsDisposed(THandle.CreateFromInteger(handle));
	void IResourceImplProvider.RawHandleDispose(nuint handle) => Dispose(THandle.CreateFromInteger(handle));
	string IResourceImplProvider.RawHandleGetName(nuint handle) => GetName(THandle.CreateFromInteger(handle));
	int IResourceImplProvider.RawHandleGetNameUsingSpan(nuint handle, Span<char> dest) => GetNameUsingSpan(THandle.CreateFromInteger(handle), dest);
	int IResourceImplProvider.RawHandleGetNameSpanLength(nuint handle) => GetNameSpanLength(THandle.CreateFromInteger(handle));
}

readonly record struct ResourceIdent(nint TypeHandle, nuint RawResourceHandle);
readonly record struct StubResource(ResourceIdent Ident, IResourceImplProvider Implementation) : IHandleImplPairResource {
	public string Name => Implementation.RawHandleGetName(Ident.RawResourceHandle);
	public int GetNameUsingSpan(Span<char> dest) => Implementation.RawHandleGetNameUsingSpan(Ident.RawResourceHandle, dest);
	public int GetNameSpanLength() => Implementation.RawHandleGetNameSpanLength(Ident.RawResourceHandle);
	public void Dispose() => Implementation.RawHandleDispose(Ident.RawResourceHandle);
	public nuint Handle => Ident.RawResourceHandle;
}

public unsafe interface IHandleImplPairResource : IStringSpanNameEnabled, IDisposable {
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

	internal static StubResource RecreateFromRawHandleAndImpl(nuint rawHandle, IResourceImplProvider impl, nint typeHandle) => new(new(typeHandle, rawHandle), impl);
}

public interface IHandleImplPairResource<TSelf> : IHandleImplPairResource, IEquatable<TSelf> where TSelf : IHandleImplPairResource<TSelf> {
	internal static abstract TSelf RecreateFromRawHandleAndImpl(nuint rawHandle, IResourceImplProvider impl);
}

public interface IHandleImplPairResource<out THandle, out TImpl> : IHandleImplPairResource where THandle : unmanaged, IResourceHandle<THandle> where TImpl : class, IResourceImplProvider {
	internal new THandle Handle { get; }
	internal new TImpl Implementation { get; }

	nuint IHandleImplPairResource.Handle => Handle.AsInteger;
	IResourceImplProvider IHandleImplPairResource.Implementation => Implementation;

	internal static (THandle Handle, TImpl Implementation) ReadResource(ReadOnlySpan<byte> src) {
		var impl = (ReadGcHandleFromSerializedResource(src).Target as TImpl) ?? throw new InvalidOperationException($"Unexpected null implementation.");
		var handle = THandle.CreateFromInteger(ReadHandleFromSerializedResource(src));
		return (handle, impl);
	}
}

public interface IHandleImplPairResource<TSelf, out THandle, out TImpl>
	: IHandleImplPairResource<TSelf>, IHandleImplPairResource<THandle, TImpl> 
	where TSelf : IHandleImplPairResource<TSelf> 
	where THandle : unmanaged, IResourceHandle<THandle> 
	where TImpl : class, IResourceImplProvider;