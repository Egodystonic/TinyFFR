// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(int) * 3)]
public readonly record struct VertexTriangle(int IndexA, int IndexB, int IndexC) {
	public VertexTriangle ShiftedBy(int indexShift) => new(IndexA + indexShift, IndexB + indexShift, IndexC + indexShift);
	public override string ToString() => $"<{IndexA}, {IndexB}, {IndexC}>";
}