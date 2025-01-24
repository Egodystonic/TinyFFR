// Created on 2024-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshBuilder {
	void CreateMeshVertices(Polygon surface, Transform2D textureTransform, Span<MeshVertex> verticesDest, Span<VertexTriangle> trianglesDest) {

	}

	// TODO actually let's make this take a params span of MeshPolygons -- these are Polygon + Tangent
	Mesh CreateMesh(Polygon surface, Transform2D? textureTransform = null, ReadOnlySpan<char> name = default) => CreateMesh(surface, textureTransform ?? Transform2D.None, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(Polygon surface, Transform2D textureTransform, scoped in MeshCreationConfig config) {
		
	}

	Mesh CreateMesh(CuboidDescriptor cuboidDesc, Transform2D? textureTransform = null, ReadOnlySpan<char> name = default) => CreateMesh(cuboidDesc, textureTransform ?? Transform2D.None, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(CuboidDescriptor cuboidDesc, Transform2D textureTransform, scoped in MeshCreationConfig config) {
		var vertices = (Span<MeshVertex>) stackalloc MeshVertex[6 * 4];
		var triangles = (Span<VertexTriangle>) stackalloc VertexTriangle[6 * 2];

		foreach (var side in OrientationUtils.AllCardinals) {
			cuboidDesc.
		}
	}

	Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, ReadOnlySpan<char> name = default) => CreateMesh(vertices, triangles, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, scoped in MeshCreationConfig config);
}