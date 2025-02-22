// Created on 2024-10-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly record struct MeshBufferData(ResourceHandle<VertexBuffer> VertexBufferHandle, ResourceHandle<IndexBuffer> IndexBufferHandle, int IndexBufferStartIndex, int IndexBufferCount);