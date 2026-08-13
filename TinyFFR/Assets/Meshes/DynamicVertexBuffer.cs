// Created on 2026-08-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using System;
using System.Runtime.InteropServices;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly struct DynamicVertexBuffer : IDisposableResource<DynamicVertexBuffer, IDynamicVertexBufferImplProvider> {
	readonly ResourceHandle<DynamicVertexBuffer> _handle;
	readonly IDynamicVertexBufferImplProvider _impl;

	internal ResourceHandle<DynamicVertexBuffer> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(DynamicVertexBuffer)) : _handle;
	internal IDynamicVertexBufferImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<DynamicVertexBuffer>();

	IDynamicVertexBufferImplProvider IResource<DynamicVertexBuffer, IDynamicVertexBufferImplProvider>.Implementation => Implementation;
	ResourceHandle<DynamicVertexBuffer> IResource<DynamicVertexBuffer>.Handle => Handle;

	public int VertexBufferSize {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetVertexBufferSize(_handle);
	}
	public int IndexBufferSize {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetIndexBufferSize(_handle);
	}

	internal DynamicVertexBuffer(ResourceHandle<DynamicVertexBuffer> handle, IDynamicVertexBufferImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ResizeVertexBuffer(int newBufferSize) => Implementation.ResizeVertexBuffer(_handle, newBufferSize);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ResizeIndexBuffer(int newBufferSize) => Implementation.ResizeIndexBuffer(_handle, newBufferSize);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ScopedSpanLease<MeshVertex> BorrowVerticesSpan(bool recalculateBoundingBoxOnLeaseDispose, bool overwriteChildMeshBoundingBoxes) => Implementation.BorrowVerticesSpan(_handle, Range.All, recalculateBoundingBoxOnLeaseDispose, overwriteChildMeshBoundingBoxes);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ScopedSpanLease<MeshVertex> BorrowVerticesSpan(bool recalculateBoundingBoxOnLeaseDispose, bool overwriteChildMeshBoundingBoxes, Range range) => Implementation.BorrowVerticesSpan(_handle, range, recalculateBoundingBoxOnLeaseDispose, overwriteChildMeshBoundingBoxes);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ScopedReadOnlySpanLease<MeshVertex> BorrowVerticesSpanReadOnly() => Implementation.BorrowVerticesSpanReadOnly(_handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ScopedSpanLease<ushort> BorrowIndicesSpan(bool recalculateBoundingBoxOnLeaseDispose, bool overwriteChildMeshBoundingBoxes) => Implementation.BorrowIndicesSpan(_handle, Range.All, recalculateBoundingBoxOnLeaseDispose, overwriteChildMeshBoundingBoxes);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ScopedSpanLease<ushort> BorrowIndicesSpan(bool recalculateBoundingBoxOnLeaseDispose, bool overwriteChildMeshBoundingBoxes, Range range) => Implementation.BorrowIndicesSpan(_handle, range, recalculateBoundingBoxOnLeaseDispose, overwriteChildMeshBoundingBoxes);
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void TriggerManualBoundingBoxRecalculation(bool overwriteChildMeshBoundingBoxes) => Implementation.TriggerManualBoundingBoxRecalculation(_handle, overwriteChildMeshBoundingBoxes);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBoundingBox(PositionedCuboid newBoundingBox, bool overwriteChildMeshBoundingBoxes) => Implementation.SetBoundingBox(_handle, newBoundingBox, overwriteChildMeshBoundingBoxes);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBoundingBox(Mesh mesh, PositionedCuboid newBoundingBox) => Implementation.SetBoundingBox(_handle, mesh, newBoundingBox);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Mesh CreateMesh() => Implementation.CreateMeshView(_handle, Range.All, null);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Mesh CreateMesh(Range indicesRange) => Implementation.CreateMeshView(_handle, indicesRange, null);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Mesh CreateMesh(Range indicesRange, PositionedCuboid boundingBoxOverride) => Implementation.CreateMeshView(_handle, indicesRange, boundingBoxOverride);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static DynamicVertexBuffer IResource<DynamicVertexBuffer>.CreateFromHandleAndImpl(ResourceHandle<DynamicVertexBuffer> handle, IResourceImplProvider impl) {
		return new DynamicVertexBuffer(handle, impl as IDynamicVertexBufferImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ResourceHandle<DynamicVertexBuffer> GetHandleWithoutDisposeCheck() => _handle;

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Dynamic Vertex Buffer {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	public bool Equals(DynamicVertexBuffer other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	public override bool Equals(object? obj) => obj is DynamicVertexBuffer other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(DynamicVertexBuffer left, DynamicVertexBuffer right) => left.Equals(right);
	public static bool operator !=(DynamicVertexBuffer left, DynamicVertexBuffer right) => !left.Equals(right);
	#endregion
}
