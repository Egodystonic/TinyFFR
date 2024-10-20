// Created on 2024-10-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly record struct MeshBufferData(VertexBufferHandle VertexBufferHandle, IndexBufferHandle IndexBufferHandle, int IndexBufferStartIndex, int IndexBufferCount);