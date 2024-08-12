// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Assets;

[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 3 * 3, Pack = 1)]
public readonly record struct MeshTriangle(MeshVertex VertexOne, MeshVertex VertexTwo, MeshVertex VertexThree);