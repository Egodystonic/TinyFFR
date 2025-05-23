﻿// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly struct VertexBuffer : IDisposableResource<VertexBuffer, IVertexBufferImplProvider> {
	readonly ResourceHandle<VertexBuffer> _handle;
	readonly IVertexBufferImplProvider _impl;

	internal ResourceHandle<VertexBuffer> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(VertexBuffer)) : _handle;
	internal IVertexBufferImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<VertexBuffer>();

	IVertexBufferImplProvider IResource<VertexBuffer, IVertexBufferImplProvider>.Implementation => Implementation;
	ResourceHandle<VertexBuffer> IResource<VertexBuffer>.Handle => Handle;

	internal VertexBuffer(ResourceHandle<VertexBuffer> handle, IVertexBufferImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static VertexBuffer IResource<VertexBuffer>.CreateFromHandleAndImpl(ResourceHandle<VertexBuffer> handle, IResourceImplProvider impl) {
		return new VertexBuffer(handle, impl as IVertexBufferImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Vertex Buffer {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	public bool Equals(VertexBuffer other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is VertexBuffer other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(VertexBuffer left, VertexBuffer right) => left.Equals(right);
	public static bool operator !=(VertexBuffer left, VertexBuffer right) => !left.Equals(right);
	#endregion
}