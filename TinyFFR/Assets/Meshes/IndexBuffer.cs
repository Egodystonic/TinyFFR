// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// A block of video memory holding the triangles that join the vertices of one or more meshes.
/// </summary>
/// <remarks>
/// Several meshes commonly share one buffer (this is how the sub-meshes of a loaded model are stored), so buffers are
/// managed by the library rather than being created directly.
/// </remarks>
public readonly struct IndexBuffer : IDisposableResource<IndexBuffer, IIndexBufferImplProvider> {
	readonly ResourceHandle<IndexBuffer> _handle;
	readonly IIndexBufferImplProvider _impl;

	internal ResourceHandle<IndexBuffer> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(IndexBuffer)) : _handle;
	internal IIndexBufferImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<IndexBuffer>();

	IIndexBufferImplProvider IResource<IndexBuffer, IIndexBufferImplProvider>.Implementation => Implementation;
	ResourceHandle<IndexBuffer> IResource<IndexBuffer>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	internal IndexBuffer(ResourceHandle<IndexBuffer> handle, IIndexBufferImplProvider impl) {
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

	static IndexBuffer IResource<IndexBuffer>.CreateFromHandleAndImpl(ResourceHandle<IndexBuffer> handle, IResourceImplProvider impl) {
		return new IndexBuffer(handle, impl as IIndexBufferImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<IndexBuffer> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<IndexBuffer> IResource<IndexBuffer>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

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
	public override string ToString() => $"Index Buffer {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	/// <inheritdoc />
	public bool Equals(IndexBuffer other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is IndexBuffer other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	/// <summary>
	/// Returns whether the two given index buffers are the same index buffer.
	/// </summary>
	/// <param name="left">The first index buffer to compare.</param>
	/// <param name="right">The second index buffer to compare.</param>
	public static bool operator ==(IndexBuffer left, IndexBuffer right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given index buffers are different index buffers.
	/// </summary>
	/// <param name="left">The first index buffer to compare.</param>
	/// <param name="right">The second index buffer to compare.</param>
	public static bool operator !=(IndexBuffer left, IndexBuffer right) => !left.Equals(right);
	#endregion
}