// Created on 2024-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Buffers;
using System.Globalization;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshBuilder {
	IMeshPolygonGroup AllocateNewPolygonGroup();

	Mesh CreateMesh(CuboidDescriptor cuboidDesc, Transform2D? textureTransform = null, ReadOnlySpan<char> name = default) => CreateMesh(cuboidDesc, textureTransform ?? Transform2D.None, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(CuboidDescriptor cuboidDesc, Transform2D textureTransform, scoped in MeshCreationConfig config) {
		var polyVertexSpan = (Span<Location>) stackalloc Location[4];
		using var polyGroup = AllocateNewPolygonGroup();

		// Back
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpBackward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownBackward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownBackward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpBackward);
		polyGroup.Add(new(polyVertexSpan, Direction.Backward), Direction.Right, Direction.Down, polyVertexSpan[0]);

		// Front
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpForward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownForward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownForward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpForward);
		polyGroup.Add(new(polyVertexSpan, Direction.Forward), Direction.Left, Direction.Down, polyVertexSpan[0]);

		// Right
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpBackward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownBackward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownForward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpForward);
		polyGroup.Add(new(polyVertexSpan, Direction.Right), Direction.Forward, Direction.Down, polyVertexSpan[0]);

		// Left
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpForward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownForward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownBackward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpBackward);
		polyGroup.Add(new(polyVertexSpan, Direction.Left), Direction.Backward, Direction.Down, polyVertexSpan[0]);

		// Top
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpForward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpBackward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpBackward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpForward);
		polyGroup.Add(new(polyVertexSpan, Direction.Up), Direction.Right, Direction.Backward, polyVertexSpan[0]);

		// Bottom
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownBackward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownBackward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownForward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownForward);
		polyGroup.Add(new(polyVertexSpan, Direction.Down), Direction.Left, Direction.Forward, polyVertexSpan[0]);

		return CreateMesh(polyGroup, textureTransform, in config);
	}

	Mesh CreateMesh(Polygon polygon, Direction? textureUDirection, Direction? textureVDirection, Location? textureOrigin, Transform2D? textureTransform = null, ReadOnlySpan<char> name = default) {
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

		Console.WriteLine();
		for (var i = 0; i < polygons.TotalPolygonCount; ++i) {
			var polygon = polygons.GetPolygonAtIndex(i, out var texU, out var texV, out var texO);
			Console.WriteLine($"FACE {polygon.Normal.NearestOrientation.AsEnum} (tex U x V = {texU.NearestOrientation.AsEnum} x {texV.NearestOrientation.AsEnum}) (texO = {texO})");
			for (var v = 0; v < polygon.VertexCount; ++v) {
				Console.WriteLine($"\t {polygon.Vertices[v]}");
			}
		}

		polygons.Triangulate(textureTransform, out var vertices, out var triangles);


		Console.WriteLine();
		var vert = 0;
		for (var i = 0; i < polygons.TotalPolygonCount; ++i) {
			var polygon = polygons.GetPolygonAtIndex(i, out _, out _, out _);
			Console.WriteLine("\t" + polygon.Normal.NearestOrientation.AsEnum);
			for (var pv = 0; pv < polygon.VertexCount; ++pv) {
				Console.WriteLine("\t\t" + vertices[vert]);
				vert++;
			}
			Console.WriteLine();
		}

		return CreateMesh(vertices, triangles, config);
	}

	Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, ReadOnlySpan<char> name = default) => CreateMesh(vertices, triangles, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, scoped in MeshCreationConfig config);
}