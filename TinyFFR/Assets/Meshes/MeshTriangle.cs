// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct MeshTriangle {
	public readonly int IndexA, IndexB, IndexC;

	public MeshTriangle(int indexA, int indexB, int indexC) {
		IndexA = indexA;
		IndexB = indexB;
		IndexC = indexC;
	}
}