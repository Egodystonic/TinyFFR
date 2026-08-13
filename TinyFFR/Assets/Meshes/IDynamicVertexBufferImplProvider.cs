// Created on 2026-08-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IDynamicVertexBufferImplProvider : IDisposableResourceImplProvider<DynamicVertexBuffer> {
	int GetVertexBufferSize(ResourceHandle<DynamicVertexBuffer> handle);
	int GetIndexBufferSize(ResourceHandle<DynamicVertexBuffer> handle);

	void ResizeVertexBuffer(ResourceHandle<DynamicVertexBuffer> handle, int newBufferSize);
	void ResizeIndexBuffer(ResourceHandle<DynamicVertexBuffer> handle, int newBufferSize);

	ScopedSpanLease<MeshVertex> BorrowVerticesSpan(ResourceHandle<DynamicVertexBuffer> handle, Range range, bool recalculateBoundingBoxOnLeaseDispose, bool overwriteChildMeshBoundingBoxes);
	ScopedReadOnlySpanLease<MeshVertex> BorrowVerticesSpanReadOnly(ResourceHandle<DynamicVertexBuffer> handle);
	ScopedSpanLease<ushort> BorrowIndicesSpan(ResourceHandle<DynamicVertexBuffer> handle, Range range, bool recalculateBoundingBoxOnLeaseDispose, bool overwriteChildMeshBoundingBoxes);
	ScopedReadOnlySpanLease<ushort> BorrowIndicesSpanReadOnly(ResourceHandle<DynamicVertexBuffer> handle);

	internal void SetVerticesImGui(ResourceHandle<DynamicVertexBuffer> handle, ReadOnlySpan<MeshVertexImGui> vertices, int offset);
	internal void SetIndicesImGui(ResourceHandle<DynamicVertexBuffer> handle, ReadOnlySpan<ushort> indices, int offset);

	void TriggerManualBoundingBoxRecalculation(ResourceHandle<DynamicVertexBuffer> handle, bool overwriteChildMeshBoundingBoxes);
	void SetBoundingBox(ResourceHandle<DynamicVertexBuffer> handle, PositionedCuboid newBoundingBox, bool overwriteChildMeshBoundingBoxes);
	void SetBoundingBox(ResourceHandle<DynamicVertexBuffer> handle, Mesh mesh, PositionedCuboid newBoundingBox);

	Mesh CreateMeshView(ResourceHandle<DynamicVertexBuffer> handle, Range indicesRange, PositionedCuboid? boundingBoxOverride);
}
