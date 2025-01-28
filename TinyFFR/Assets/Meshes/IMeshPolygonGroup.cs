// Created on 2025-01-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshPolygonGroup : IDisposable {
	int PolygonCount { get; }
	int VertexCount { get; }
	int TriangleCount { get; }

	void AddPolygon(Polygon p, Direction textureUDirection, Direction textureVDirection, Location textureOrigin);
	void Clear();

	Polygon GetPolygonAtIndex(int index, out Direction textureU, out Direction textureV, out Location textureOrigin);
}