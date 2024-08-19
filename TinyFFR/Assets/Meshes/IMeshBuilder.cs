// Created on 2024-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshBuilder {
	MeshAsset CreateMesh(CuboidDescriptor cuboidDesc);
	MeshAsset CreateMesh(CuboidDescriptor cuboidDesc, in MeshCreationConfig config);
	// MeshAsset CreateMesh(SphereDescriptor sphereDesc, int extrapolationLevel = 3);
	// MeshAsset CreateMesh(SphereDescriptor sphereDesc, int extrapolationLevel, scoped in MeshCreationConfig config);
	MeshAsset CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<MeshTriangle> triangles);
	MeshAsset CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<MeshTriangle> triangles, scoped in MeshCreationConfig config);
}