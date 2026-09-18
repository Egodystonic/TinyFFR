// Created on 2024-10-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// Identifies the GPU buffers a <see cref="Mesh"/> is drawn from, and the region of the index buffer that belongs to it.
/// </summary>
/// <remarks>
/// Several meshes can share one pair of buffers (this is how the sub-meshes of a loaded model are stored), which is why a mesh
/// is identified by a range within the index buffer rather than by the buffer alone.
/// </remarks>
/// <param name="VertexBufferHandle">The vertex buffer holding this mesh's vertices.</param>
/// <param name="IndexBufferHandle">The index buffer holding the triangles that join those vertices.</param>
/// <param name="IndexBufferStartIndex">The position in the index buffer at which this mesh's triangles begin.</param>
/// <param name="IndexBufferCount">The number of index buffer entries belonging to this mesh, i.e. three per triangle.</param>
/// <param name="BoneCount">The number of bones in this mesh's skeleton, or <c>0</c> for a mesh that is not skeletal.</param>
public readonly record struct MeshBufferData(ResourceHandle<VertexBuffer> VertexBufferHandle, ResourceHandle<IndexBuffer> IndexBufferHandle, int IndexBufferStartIndex, int IndexBufferCount, int BoneCount);
