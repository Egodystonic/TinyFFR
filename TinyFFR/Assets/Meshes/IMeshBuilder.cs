// Created on 2024-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshBuilder {
	void CreateMeshVertices(BoundedPlane surface, Transform2D textureTransform, Span<MeshVertex> verticesDest, Span<MeshTriangle> trianglesDest) {

	}

	// TODO before starting BoundedPlane, create a Polygon type that represents a chain of XYPair<float>s
	// TODO maybe both these types should be ref structs so we can store spans and make things easy, but then provide a simple wrapper type for each e.g. HeapBoundedPlane ... Or should we just expect people to store the memory themselves?
	Mesh CreateMesh(BoundedPlane surface, Transform2D? textureTransform = null, ReadOnlySpan<char> name = default) => CreateMesh(surface, textureTransform ?? Transform2D.None, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(BoundedPlane surface, Transform2D textureTransform, scoped in MeshCreationConfig config) {
		
	}

	Mesh CreateMesh(CuboidDescriptor cuboidDesc, Transform2D? textureTransform = null, ReadOnlySpan<char> name = default) => CreateMesh(cuboidDesc, textureTransform ?? Transform2D.None, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(CuboidDescriptor cuboidDesc, Transform2D textureTransform, scoped in MeshCreationConfig config) {
		var vertices = (Span<MeshVertex>) stackalloc MeshVertex[6 * 4];
		var triangles = (Span<MeshTriangle>) stackalloc MeshTriangle[6 * 2];

		foreach (var side in OrientationUtils.AllCardinals) {
			cuboidDesc.
		}
	}

	Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<MeshTriangle> triangles, ReadOnlySpan<char> name = default) => CreateMesh(vertices, triangles, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<MeshTriangle> triangles, scoped in MeshCreationConfig config);
}