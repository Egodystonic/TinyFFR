// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly struct IndexBuffer : IDisposableResource<IndexBuffer, IndexBufferHandle, IIndexBufferImplProvider> {
	readonly IndexBufferHandle _handle;
	readonly IIndexBufferImplProvider _impl;

	internal IndexBufferHandle Handle => IsDisposed ? throw new ObjectDisposedException(nameof(IndexBuffer)) : _handle;
	internal IIndexBufferImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<IndexBuffer>();

	IIndexBufferImplProvider IResource<IndexBufferHandle, IIndexBufferImplProvider>.Implementation => Implementation;
	IndexBufferHandle IResource<IndexBufferHandle, IIndexBufferImplProvider>.Handle => Handle;

	public ReadOnlySpan<char> Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(_handle);
	}

	internal IndexBuffer(IndexBufferHandle handle, IIndexBufferImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static IndexBuffer IResource<IndexBuffer>.RecreateFromRawHandleAndImpl(nuint rawHandle, IResourceImplProvider impl) {
		return new IndexBuffer(rawHandle, impl as IIndexBufferImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Index Buffer {(IsDisposed ? "(Disposed)" : $"\"{Name}\"")}";

	#region Equality
	public bool Equals(IndexBuffer other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is IndexBuffer other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(IndexBuffer left, IndexBuffer right) => left.Equals(right);
	public static bool operator !=(IndexBuffer left, IndexBuffer right) => !left.Equals(right);
	#endregion
}