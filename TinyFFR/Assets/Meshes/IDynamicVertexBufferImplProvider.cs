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

	void SetVertices(ResourceHandle<DynamicVertexBuffer> handle, ReadOnlySpan<MeshVertex> vertices, int offset);
	internal void SetVerticesImGui(ResourceHandle<DynamicVertexBuffer> handle, ReadOnlySpan<MeshVertexImGui> vertices, int offset);
	void SetIndices(ResourceHandle<DynamicVertexBuffer> handle, ReadOnlySpan<ushort> indices, int offset);

	Mesh CreateMeshView(ResourceHandle<DynamicVertexBuffer> handle, Range indicesRange, PositionedCuboid? boundingBoxOverride);
}
