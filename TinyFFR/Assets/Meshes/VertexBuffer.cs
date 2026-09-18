// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// A block of video memory holding the vertices of one or more meshes.
/// </summary>
/// <remarks>
/// Several meshes commonly share one buffer — this is how the sub-meshes of a loaded model are stored — so buffers are
/// managed by the library rather than being created directly.
/// </remarks>
public readonly struct VertexBuffer : IDisposableResource<VertexBuffer, IVertexBufferImplProvider> {
	readonly ResourceHandle<VertexBuffer> _handle;
	readonly IVertexBufferImplProvider _impl;

	internal ResourceHandle<VertexBuffer> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(VertexBuffer)) : _handle;
	internal IVertexBufferImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<VertexBuffer>();

	IVertexBufferImplProvider IResource<VertexBuffer, IVertexBufferImplProvider>.Implementation => Implementation;
	ResourceHandle<VertexBuffer> IResource<VertexBuffer>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	internal VertexBuffer(ResourceHandle<VertexBuffer> handle, IVertexBufferImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static VertexBuffer IResource<VertexBuffer>.CreateFromHandleAndImpl(ResourceHandle<VertexBuffer> handle, IResourceImplProvider impl) {
		return new VertexBuffer(handle, impl as IVertexBufferImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<VertexBuffer> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<VertexBuffer> IResource<VertexBuffer>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	#region Disposal
	/// <summary>
	/// Disposes this buffer, releasing its video memory.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	/// <inheritdoc />
	public override string ToString() => $"Vertex Buffer {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	/// <inheritdoc />
	public bool Equals(VertexBuffer other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is VertexBuffer other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	/// <summary>
	/// Returns whether the two given vertex buffers are the same vertex buffer.
	/// </summary>
	/// <param name="left">The first vertex buffer to compare.</param>
	/// <param name="right">The second vertex buffer to compare.</param>
	public static bool operator ==(VertexBuffer left, VertexBuffer right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given vertex buffers are different vertex buffers.
	/// </summary>
	/// <param name="left">The first vertex buffer to compare.</param>
	/// <param name="right">The second vertex buffer to compare.</param>
	public static bool operator !=(VertexBuffer left, VertexBuffer right) => !left.Equals(right);
	#endregion
}