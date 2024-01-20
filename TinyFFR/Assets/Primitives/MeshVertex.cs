// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Numerics;

namespace Egodystonic.TinyFFR.Assets;

[StructLayout(LayoutKind.Sequential, Size = sizeof(float) * 3, Pack = 1)] // TODO in xmldoc, note that this can safely be pointer-aliased to/from Vector3
public readonly record struct MeshVertex(float X, float Y, float Z);