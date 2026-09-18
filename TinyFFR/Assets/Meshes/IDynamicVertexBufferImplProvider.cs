// Created on 2026-08-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// Provides the implementation behind <see cref="DynamicVertexBuffer"/>.
/// </summary>
public interface IDynamicVertexBufferImplProvider : IDisposableResourceImplProvider<DynamicVertexBuffer> {
	/// <summary>
	/// Invoked via <see cref="DynamicVertexBuffer.VertexBufferSize"/>.
	/// </summary>
	int GetVertexBufferSize(ResourceHandle<DynamicVertexBuffer> handle);
	/// <summary>
	/// Invoked via <see cref="DynamicVertexBuffer.IndexBufferSize"/>.
	/// </summary>
	int GetIndexBufferSize(ResourceHandle<DynamicVertexBuffer> handle);

	/// <summary>
	/// Invoked via <see cref="DynamicVertexBuffer.ResizeVertexBuffer"/>.
	/// </summary>
	void ResizeVertexBuffer(ResourceHandle<DynamicVertexBuffer> handle, int newBufferSize);
	/// <summary>
	/// Invoked via <see cref="DynamicVertexBuffer.ResizeIndexBuffer"/>.
	/// </summary>
	void ResizeIndexBuffer(ResourceHandle<DynamicVertexBuffer> handle, int newBufferSize);

	/// <summary>
	/// Invoked via <see cref="DynamicVertexBuffer.BorrowVerticesSpan(bool, bool)"/>, <see cref="DynamicVertexBuffer.BorrowVerticesSpan(bool, bool, Range)"/>.
	/// </summary>
	ScopedSpanLease<MeshVertex> BorrowVerticesSpan(ResourceHandle<DynamicVertexBuffer> handle, Range range, bool recalculateBoundingBoxOnLeaseDispose, bool overwriteChildMeshBoundingBoxes);
	/// <summary>
	/// Invoked via <see cref="DynamicVertexBuffer.BorrowVerticesSpanReadOnly"/>.
	/// </summary>
	ScopedReadOnlySpanLease<MeshVertex> BorrowVerticesSpanReadOnly(ResourceHandle<DynamicVertexBuffer> handle);
	/// <summary>
	/// Invoked via <see cref="DynamicVertexBuffer.BorrowIndicesSpan(bool, bool)"/>, <see cref="DynamicVertexBuffer.BorrowIndicesSpan(bool, bool, Range)"/>.
	/// </summary>
	ScopedSpanLease<ushort> BorrowIndicesSpan(ResourceHandle<DynamicVertexBuffer> handle, Range range, bool recalculateBoundingBoxOnLeaseDispose, bool overwriteChildMeshBoundingBoxes);
	/// <summary>
	/// Invoked via <see cref="DynamicVertexBuffer.BorrowIndicesSpanReadOnly"/>.
	/// </summary>
	ScopedReadOnlySpanLease<ushort> BorrowIndicesSpanReadOnly(ResourceHandle<DynamicVertexBuffer> handle);

	internal void SetVerticesImGui(ResourceHandle<DynamicVertexBuffer> handle, ReadOnlySpan<MeshVertexImGui> vertices, int offset);
	internal void SetIndicesImGui(ResourceHandle<DynamicVertexBuffer> handle, ReadOnlySpan<ushort> indices, int offset);

	/// <summary>
	/// Invoked via <see cref="DynamicVertexBuffer.TriggerManualBoundingBoxRecalculation"/>.
	/// </summary>
	void TriggerManualBoundingBoxRecalculation(ResourceHandle<DynamicVertexBuffer> handle, bool overwriteChildMeshBoundingBoxes);
	/// <summary>
	/// Invoked via <see cref="DynamicVertexBuffer.SetBoundingBox(PositionedCuboid, bool)"/>.
	/// </summary>
	void SetBoundingBox(ResourceHandle<DynamicVertexBuffer> handle, PositionedCuboid newBoundingBox, bool overwriteChildMeshBoundingBoxes);
	/// <summary>
	/// Invoked via <see cref="DynamicVertexBuffer.SetBoundingBox(Mesh, PositionedCuboid)"/>.
	/// </summary>
	void SetBoundingBox(ResourceHandle<DynamicVertexBuffer> handle, Mesh mesh, PositionedCuboid newBoundingBox);

	/// <summary>
	/// Invoked via <see cref="DynamicVertexBuffer.CreateMesh()"/>, <see cref="DynamicVertexBuffer.CreateMesh(Range)"/>, <see cref="DynamicVertexBuffer.CreateMesh(Range, PositionedCuboid)"/>.
	/// </summary>
	Mesh CreateMeshView(ResourceHandle<DynamicVertexBuffer> handle, Range indicesRange, PositionedCuboid? boundingBoxOverride);
}
