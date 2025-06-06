﻿// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly struct IndexBuffer : IDisposableResource<IndexBuffer, IIndexBufferImplProvider> {
	readonly ResourceHandle<IndexBuffer> _handle;
	readonly IIndexBufferImplProvider _impl;

	internal ResourceHandle<IndexBuffer> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(IndexBuffer)) : _handle;
	internal IIndexBufferImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<IndexBuffer>();

	IIndexBufferImplProvider IResource<IndexBuffer, IIndexBufferImplProvider>.Implementation => Implementation;
	ResourceHandle<IndexBuffer> IResource<IndexBuffer>.Handle => Handle;

	internal IndexBuffer(ResourceHandle<IndexBuffer> handle, IIndexBufferImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static IndexBuffer IResource<IndexBuffer>.CreateFromHandleAndImpl(ResourceHandle<IndexBuffer> handle, IResourceImplProvider impl) {
		return new IndexBuffer(handle, impl as IIndexBufferImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Index Buffer {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	public bool Equals(IndexBuffer other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is IndexBuffer other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(IndexBuffer left, IndexBuffer right) => left.Equals(right);
	public static bool operator !=(IndexBuffer left, IndexBuffer right) => !left.Equals(right);
	#endregion
}