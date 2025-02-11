// Created on 2024-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Buffers;
using System.Globalization;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshBuilder {
	IMeshPolygonGroup AllocateNewPolygonGroup();

	Mesh CreateMesh(CuboidDescriptor cuboidDesc, Transform2D? textureTransform = null, bool centreTextureOrigin = false, ReadOnlySpan<char> name = default) => CreateMesh(cuboidDesc, textureTransform ?? Transform2D.None, centreTextureOrigin, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(CuboidDescriptor cuboidDesc, Transform2D textureTransform, bool centreTextureOrigin, scoped in MeshCreationConfig config) {
		var polyVertexSpan = (Span<Location>) stackalloc Location[4];
		using var polyGroup = AllocateNewPolygonGroup();

		// Back
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpBackward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownBackward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownBackward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpBackward);
		polyGroup.Add(
			new(polyVertexSpan, Direction.Backward),
			Direction.Right,
			Direction.Up,
			centreTextureOrigin ? cuboidDesc.CentroidAt(CardinalOrientation3D.Backward) : polyVertexSpan[1]
		);

		// Front
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpForward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownForward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownForward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpForward);
		polyGroup.Add(
			new(polyVertexSpan, Direction.Forward),
			Direction.Left,
			Direction.Up,
			centreTextureOrigin ? cuboidDesc.CentroidAt(CardinalOrientation3D.Forward) : polyVertexSpan[1]
		);

		// Right
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpBackward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownBackward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownForward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpForward);
		polyGroup.Add(
			new(polyVertexSpan, Direction.Right),
			Direction.Forward,
			Direction.Up,
			centreTextureOrigin ? cuboidDesc.CentroidAt(CardinalOrientation3D.Right) : polyVertexSpan[1]
		);

		// Left
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpForward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownForward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownBackward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpBackward);
		polyGroup.Add(
			new(polyVertexSpan, Direction.Left),
			Direction.Backward,
			Direction.Up,
			centreTextureOrigin ? cuboidDesc.CentroidAt(CardinalOrientation3D.Left) : polyVertexSpan[1]
		);

		// Top
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpForward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpBackward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpBackward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpForward);
		polyGroup.Add(
			new(polyVertexSpan, Direction.Up),
			Direction.Right,
			Direction.Forward,
			centreTextureOrigin ? cuboidDesc.CentroidAt(CardinalOrientation3D.Up) : polyVertexSpan[1]
		);

		// Bottom
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownBackward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownBackward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownForward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownForward);
		polyGroup.Add(
			new(polyVertexSpan, Direction.Down),
			Direction.Left,
			Direction.Backward,
			centreTextureOrigin ? cuboidDesc.CentroidAt(CardinalOrientation3D.Down) : polyVertexSpan[3]
		);

		return CreateMesh(polyGroup, textureTransform, in config);
	}

	Mesh CreateMesh(Polygon polygon, Direction? textureUDirection = null, Direction? textureVDirection = null, Location? textureOrigin = null, Transform2D? textureTransform = null, ReadOnlySpan<char> name = default) {
		textureUDirection ??= polygon.Normal.AnyOrthogonal();
		return CreateMesh(
			polygon,
			textureUDirection.Value,
			textureVDirection ?? Direction.FromDualOrthogonalization(polygon.Normal, textureUDirection.Value),
			textureOrigin ?? polygon.Centroid,
			textureTransform ?? Transform2D.None, 
			new MeshCreationConfig { Name = name }
		);
	}
	Mesh CreateMesh(Polygon polygon, Direction textureUDirection, Direction textureVDirection, Location textureOrigin, Transform2D textureTransform, scoped in MeshCreationConfig config) {
		using var polyGroup = AllocateNewPolygonGroup();
		polyGroup.Add(polygon, textureUDirection, textureVDirection, textureOrigin);
		return CreateMesh(polyGroup, textureTransform, in config);
	}

	Mesh CreateMesh(IMeshPolygonGroup polygons, Transform2D? textureTransform = null, ReadOnlySpan<char> name = default) => CreateMesh(polygons, textureTransform ?? Transform2D.None, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(IMeshPolygonGroup polygons, Transform2D textureTransform, scoped in MeshCreationConfig config) {
		polygons.Triangulate(textureTransform, out var vertices, out var triangles);
		return CreateMesh(vertices, triangles, config);
	}

	Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, ReadOnlySpan<char> name = default) => CreateMesh(vertices, triangles, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, scoped in MeshCreationConfig config);
}