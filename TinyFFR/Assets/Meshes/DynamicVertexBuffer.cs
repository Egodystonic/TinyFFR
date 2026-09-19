// Created on 2026-08-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using System;
using System.Runtime.InteropServices;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// A buffer of vertices and indices whose contents can be rewritten at any time; and in to which "views" or "ranges" of
/// vertices can be used to create dynamically-alterable <see cref="Mesh"/>es. Created via the factory's <see cref="IMeshBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is what geometry that is regenerated from scratch each frame is written in to.
/// Meshes created from it are views on to ranges of its indices, so one buffer can back many separately-drawn
/// pieces of geometry without any of them owning memory of their own.
/// </para>
/// <para>
/// Dispose the buffer when nothing uses it, and after disposing every mesh created from it.
/// </para>
/// </remarks>
public readonly struct DynamicVertexBuffer : IDisposableResource<DynamicVertexBuffer, IDynamicVertexBufferImplProvider> {
	readonly ResourceHandle<DynamicVertexBuffer> _handle;
	readonly IDynamicVertexBufferImplProvider _impl;

	internal ResourceHandle<DynamicVertexBuffer> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(DynamicVertexBuffer)) : _handle;
	internal IDynamicVertexBufferImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<DynamicVertexBuffer>();

	IDynamicVertexBufferImplProvider IResource<DynamicVertexBuffer, IDynamicVertexBufferImplProvider>.Implementation => Implementation;
	ResourceHandle<DynamicVertexBuffer> IResource<DynamicVertexBuffer>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	/// <summary>
	/// How many vertices this buffer can currently hold.
	/// </summary>
	public int VertexBufferSize {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetVertexBufferSize(_handle);
	}
	/// <summary>
	/// How many indices this buffer can currently hold.
	/// </summary>
	public int IndexBufferSize {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetIndexBufferSize(_handle);
	}

	internal DynamicVertexBuffer(ResourceHandle<DynamicVertexBuffer> handle, IDynamicVertexBufferImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	/// <summary>
	/// Changes how many vertices this buffer can hold.
	/// </summary>
	/// <remarks>
	/// Existing contents are preserved as far as the new size allows. Resizing is not free, so size the buffer close to what
	/// will actually be used rather than growing it repeatedly.
	/// </remarks>
	/// <param name="newBufferSize">The new capacity, in vertices. Must not be negative.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ResizeVertexBuffer(int newBufferSize) => Implementation.ResizeVertexBuffer(_handle, newBufferSize);
	/// <summary>
	/// Changes how many indices this buffer can hold.
	/// </summary>
	/// <remarks>
	/// Existing contents are preserved as far as the new size allows.
	/// </remarks>
	/// <param name="newBufferSize">The new capacity, in indices. Must not be negative.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ResizeIndexBuffer(int newBufferSize) => Implementation.ResizeIndexBuffer(_handle, newBufferSize);

	/// <summary>
	/// Borrows this buffer's vertices for writing, applying the changes when the lease is disposed.
	/// </summary>
	/// <remarks>
	/// Nothing reaches the GPU until the lease is disposed.
	/// </remarks>
	/// <param name="recalculateBoundingBoxOnLeaseDispose">Whether to work out the new bounding box from the altered data when the lease is disposed. Leaving this <see langword="false"/> is faster but leaves the box stale, which can make geometry disappear when it should still be on screen.</param>
	/// <param name="overwriteChildMeshBoundingBoxes">Whether to give every mesh carved out of this buffer the same bounding box as the buffer itself, rather than leaving each with its own.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ScopedSpanLease<MeshVertex> BorrowVerticesSpan(bool recalculateBoundingBoxOnLeaseDispose, bool overwriteChildMeshBoundingBoxes) => Implementation.BorrowVerticesSpan(_handle, Range.All, recalculateBoundingBoxOnLeaseDispose, overwriteChildMeshBoundingBoxes);
	/// <summary>
	/// Borrows part of this buffer's vertices for writing, applying the changes when the lease is disposed.
	/// </summary>
	/// <remarks>
	/// Nothing reaches the GPU until the lease is disposed.
	/// </remarks>
	/// <param name="recalculateBoundingBoxOnLeaseDispose">Whether to work out the new bounding box from the altered data when the lease is disposed. Leaving this <see langword="false"/> is faster but leaves the box stale, which can make geometry disappear when it should still be on screen.</param>
	/// <param name="overwriteChildMeshBoundingBoxes">Whether to give every mesh carved out of this buffer the same bounding box as the buffer itself, rather than leaving each with its own.</param>
	/// <param name="range">Which stretch of the buffer to borrow.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ScopedSpanLease<MeshVertex> BorrowVerticesSpan(bool recalculateBoundingBoxOnLeaseDispose, bool overwriteChildMeshBoundingBoxes, Range range) => Implementation.BorrowVerticesSpan(_handle, range, recalculateBoundingBoxOnLeaseDispose, overwriteChildMeshBoundingBoxes);
	/// <summary>
	/// Borrows this buffer's vertices for reading only.
	/// </summary>
	/// <remarks>
	/// Because nothing can be written, no bounding box recalculation is needed when the lease is disposed.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ScopedReadOnlySpanLease<MeshVertex> BorrowVerticesSpanReadOnly() => Implementation.BorrowVerticesSpanReadOnly(_handle);

	/// <summary>
	/// Borrows this buffer's indices for writing, applying the changes when the lease is disposed.
	/// </summary>
	/// <remarks>
	/// Nothing reaches the GPU until the lease is disposed.
	/// </remarks>
	/// <param name="recalculateBoundingBoxOnLeaseDispose">Whether to work out the new bounding box from the altered data when the lease is disposed. Leaving this <see langword="false"/> is faster but leaves the box stale, which can make geometry disappear when it should still be on screen.</param>
	/// <param name="overwriteChildMeshBoundingBoxes">Whether to give every mesh carved out of this buffer the same bounding box as the buffer itself, rather than leaving each with its own.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ScopedSpanLease<ushort> BorrowIndicesSpan(bool recalculateBoundingBoxOnLeaseDispose, bool overwriteChildMeshBoundingBoxes) => Implementation.BorrowIndicesSpan(_handle, Range.All, recalculateBoundingBoxOnLeaseDispose, overwriteChildMeshBoundingBoxes);
	/// <summary>
	/// Borrows part of this buffer's indices for writing, applying the changes when the lease is disposed.
	/// </summary>
	/// <remarks>
	/// Nothing reaches the GPU until the lease is disposed.
	/// </remarks>
	/// <param name="recalculateBoundingBoxOnLeaseDispose">Whether to work out the new bounding box from the altered data when the lease is disposed. Leaving this <see langword="false"/> is faster but leaves the box stale, which can make geometry disappear when it should still be on screen.</param>
	/// <param name="overwriteChildMeshBoundingBoxes">Whether to give every mesh carved out of this buffer the same bounding box as the buffer itself, rather than leaving each with its own.</param>
	/// <param name="range">Which stretch of the buffer to borrow.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ScopedSpanLease<ushort> BorrowIndicesSpan(bool recalculateBoundingBoxOnLeaseDispose, bool overwriteChildMeshBoundingBoxes, Range range) => Implementation.BorrowIndicesSpan(_handle, range, recalculateBoundingBoxOnLeaseDispose, overwriteChildMeshBoundingBoxes);
	/// <summary>
	/// Borrows this buffer's indices for reading only.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ScopedReadOnlySpanLease<ushort> BorrowIndicesSpanReadOnly() => Implementation.BorrowIndicesSpanReadOnly(_handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetImGuiVertices(ReadOnlySpan<MeshVertexImGui> vertices) => Implementation.SetVerticesImGui(_handle, vertices, 0);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetImGuiVertices(ReadOnlySpan<MeshVertexImGui> vertices, int offset) => Implementation.SetVerticesImGui(_handle, vertices, offset);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetImGuiIndices(ReadOnlySpan<ushort> indices) => Implementation.SetIndicesImGui(_handle, indices, 0);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetImGuiIndices(ReadOnlySpan<ushort> indices, int offset) => Implementation.SetIndicesImGui(_handle, indices, offset);

	/// <summary>
	/// Works out this buffer's bounding box from its current contents.
	/// </summary>
	/// <remarks>
	/// Use this where the vertices were altered with recalculation switched off and the box now needs bringing up to date.
	/// </remarks>
	/// <param name="overwriteChildMeshBoundingBoxes">Whether to give every mesh carved out of this buffer the same bounding box as the buffer itself, rather than leaving each with its own.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void TriggerManualBoundingBoxRecalculation(bool overwriteChildMeshBoundingBoxes) => Implementation.TriggerManualBoundingBoxRecalculation(_handle, overwriteChildMeshBoundingBoxes);
	/// <summary>
	/// Sets this buffer's bounding box explicitly, instead of deriving it from the contents.
	/// </summary>
	/// <remarks>
	/// This is cheaper than deriving the box, and is the right choice when you already know the extents of the geometry you are
	/// writing.
	/// </remarks>
	/// <param name="newBoundingBox">The box to use.</param>
	/// <param name="overwriteChildMeshBoundingBoxes">Whether to give every mesh carved out of this buffer the same bounding box as the buffer itself, rather than leaving each with its own.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBoundingBox(PositionedCuboid newBoundingBox, bool overwriteChildMeshBoundingBoxes) => Implementation.SetBoundingBox(_handle, newBoundingBox, overwriteChildMeshBoundingBoxes);
	/// <summary>
	/// Sets the bounding box of one mesh carved out of this buffer, leaving the others alone.
	/// </summary>
	/// <param name="mesh">The mesh whose box is being set. Must be one created from this buffer.</param>
	/// <param name="newBoundingBox">The box to use.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBoundingBox(Mesh mesh, PositionedCuboid newBoundingBox) => Implementation.SetBoundingBox(_handle, mesh, newBoundingBox);

	/// <summary>
	/// Creates a mesh drawing this buffer's entire contents.
	/// </summary>
	/// <remarks>
	/// The resulting mesh is a view on to this buffer rather than a copy of it, so rewriting the buffer changes what the mesh
	/// draws. The buffer must outlive every mesh created from it.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Mesh CreateMesh() => Implementation.CreateMeshView(_handle, Range.All, null);
	/// <summary>
	/// Creates a mesh drawing part of this buffer's contents.
	/// </summary>
	/// <remarks>
	/// The resulting mesh is a view on to this buffer rather than a copy of it, so rewriting the buffer changes what the mesh
	/// draws. The buffer must outlive every mesh created from it.
	/// </remarks>
	/// <param name="indicesRange">Which stretch of the index buffer the mesh draws.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Mesh CreateMesh(Range indicesRange) => Implementation.CreateMeshView(_handle, indicesRange, null);
	/// <summary>
	/// Creates a mesh drawing part of this buffer's contents, with a bounding box of your own.
	/// </summary>
	/// <remarks>
	/// The resulting mesh is a view on to this buffer rather than a copy of it, so rewriting the buffer changes what the mesh
	/// draws. The buffer must outlive every mesh created from it.
	/// </remarks>
	/// <param name="indicesRange">Which stretch of the index buffer the mesh draws.</param>
	/// <param name="boundingBoxOverride">The bounding box to give the mesh, instead of deriving one.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Mesh CreateMesh(Range indicesRange, PositionedCuboid boundingBoxOverride) => Implementation.CreateMeshView(_handle, indicesRange, boundingBoxOverride);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static DynamicVertexBuffer IResource<DynamicVertexBuffer>.CreateFromHandleAndImpl(ResourceHandle<DynamicVertexBuffer> handle, IResourceImplProvider impl) {
		return new DynamicVertexBuffer(handle, impl as IDynamicVertexBufferImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<DynamicVertexBuffer> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<DynamicVertexBuffer> IResource<DynamicVertexBuffer>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	#region Disposal
	/// <summary>
	/// Disposes this buffer, releasing its video memory.
	/// </summary>
	/// <remarks>
	/// Every mesh created from this buffer must be disposed first.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	/// <inheritdoc />
	public override string ToString() => $"Dynamic Vertex Buffer {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	/// <inheritdoc />
	public bool Equals(DynamicVertexBuffer other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is DynamicVertexBuffer other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	/// <summary>
	/// Returns whether the two given buffers are the same buffer.
	/// </summary>
	/// <param name="left">The first buffer to compare.</param>
	/// <param name="right">The second buffer to compare.</param>
	public static bool operator ==(DynamicVertexBuffer left, DynamicVertexBuffer right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given buffers are different buffers.
	/// </summary>
	/// <param name="left">The first buffer to compare.</param>
	/// <param name="right">The second buffer to compare.</param>
	public static bool operator !=(DynamicVertexBuffer left, DynamicVertexBuffer right) => !left.Equals(right);
	#endregion
}
