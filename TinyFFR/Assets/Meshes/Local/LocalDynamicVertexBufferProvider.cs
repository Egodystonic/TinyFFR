// Created on 2026-08-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Meshes.Local;

sealed class LocalDynamicVertexBufferProvider : IDynamicVertexBufferImplProvider {
	readonly LocalMeshBuilder _owner;

	public LocalDynamicVertexBufferProvider(LocalMeshBuilder owner) {
		ArgumentNullException.ThrowIfNull(owner);
		_owner = owner;
	}

	public int GetVertexBufferSize(ResourceHandle<DynamicVertexBuffer> handle) => _owner.GetVertexBufferSize(handle);
	public int GetIndexBufferSize(ResourceHandle<DynamicVertexBuffer> handle) => _owner.GetIndexBufferSize(handle);

	public void ResizeVertexBuffer(ResourceHandle<DynamicVertexBuffer> handle, int newBufferSize) => _owner.ResizeVertexBuffer(handle, newBufferSize);
	public void ResizeIndexBuffer(ResourceHandle<DynamicVertexBuffer> handle, int newBufferSize) => _owner.ResizeIndexBuffer(handle, newBufferSize);

	public void SetVertices(ResourceHandle<DynamicVertexBuffer> handle, ReadOnlySpan<MeshVertex> vertices, int offset) => _owner.SetVertices(handle, vertices, offset);
	public void SetVerticesImGui(ResourceHandle<DynamicVertexBuffer> handle, ReadOnlySpan<MeshVertexImGui> vertices, int offset) => _owner.SetVerticesImGui(handle, vertices, offset);

	public void SetIndices(ResourceHandle<DynamicVertexBuffer> handle, ReadOnlySpan<ushort> indices, int offset) => _owner.SetIndices(handle, indices, offset);

	public Mesh CreateMeshView(ResourceHandle<DynamicVertexBuffer> handle, Range indicesRange, PositionedCuboid? boundingBoxOverride) => _owner.CreateMeshView(handle, indicesRange, boundingBoxOverride);

	public string GetNameAsNewStringObject(ResourceHandle<DynamicVertexBuffer> handle) => _owner.GetNameAsNewStringObject(handle);
	public int GetNameLength(ResourceHandle<DynamicVertexBuffer> handle) => _owner.GetNameLength(handle);
	public void CopyName(ResourceHandle<DynamicVertexBuffer> handle, Span<char> destinationBuffer) => _owner.CopyName(handle, destinationBuffer);

	public bool IsDisposed(ResourceHandle<DynamicVertexBuffer> handle) => _owner.IsDynamicVertexBufferDisposed(handle);
	public void Dispose(ResourceHandle<DynamicVertexBuffer> handle) => _owner.Dispose(handle);

	public override string ToString() => "TinyFFR Local Dynamic Mesh Provider";
}
