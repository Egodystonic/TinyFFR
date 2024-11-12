// Created on 2024-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshBuilder {
	Mesh CreateMesh(CuboidDescriptor cuboidDesc, ReadOnlySpan<char> name = default);
	Mesh CreateMesh(CuboidDescriptor cuboidDesc, in MeshCreationConfig config);
	// Mesh CreateMesh(SphereDescriptor sphereDesc, int extrapolationLevel = 3);
	// Mesh CreateMesh(SphereDescriptor sphereDesc, int extrapolationLevel, scoped in MeshCreationConfig config);
	Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<MeshTriangle> triangles, ReadOnlySpan<char> name = default);
	Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<MeshTriangle> triangles, scoped in MeshCreationConfig config);
}