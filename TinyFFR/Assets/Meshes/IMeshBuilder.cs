// Created on 2024-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshBuilder {
	Mesh CreateMesh(BoundedPlane surface, Transform2D? textureTransform = null, ReadOnlySpan<char> name = default) => CreateMesh(surface, textureTransform ?? Transform2D.None, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(BoundedPlane surface, Transform2D textureTransform, scoped in MeshCreationConfig config) {
		
	}
	Mesh CreateMesh(CuboidDescriptor cuboidDesc, Transform2D? textureTransform = null, ReadOnlySpan<char> name = default) => CreateMesh(cuboidDesc, textureTransform ?? Transform2D.None, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(CuboidDescriptor cuboidDesc, Transform2D textureTransform, scoped in MeshCreationConfig config);
	Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<MeshTriangle> triangles, ReadOnlySpan<char> name = default) => CreateMesh(vertices, triangles, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<MeshTriangle> triangles, scoped in MeshCreationConfig config);
}