// Created on 2024-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Buffers;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshBuilder {
	IMeshPolygonGroup CreateNewPolygonGroup();

	Mesh CreateMesh(CuboidDescriptor cuboidDesc, Transform2D? textureTransform = null, ReadOnlySpan<char> name = default) => CreateMesh(cuboidDesc, textureTransform ?? Transform2D.None, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(CuboidDescriptor cuboidDesc, Transform2D textureTransform, scoped in MeshCreationConfig config) {
		var vertices = (Span<MeshVertex>) stackalloc MeshVertex[6 * 4];
		var triangles = (Span<VertexTriangle>) stackalloc VertexTriangle[6 * 2];
		var polyVertexSpan = (Span<Location>) stackalloc Location[4];
		using var polyGroup = CreateNewPolygonGroup();

		// Back
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpBackward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpBackward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownBackward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownBackward);
		polyGroup.AddPolygon(new(polyVertexSpan, Direction.Backward), Direction.Right, Direction.Down, polyVertexSpan[0]);

		// Front
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpForward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpForward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownForward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownForward);
		polyGroup.AddPolygon(new(polyVertexSpan, Direction.Forward), Direction.Left, Direction.Down, polyVertexSpan[0]);

		// Right
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpBackward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpForward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownForward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownBackward);
		polyGroup.AddPolygon(new(polyVertexSpan, Direction.Right), Direction.Forward, Direction.Down, polyVertexSpan[0]);

		// Left
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpForward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpBackward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownBackward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownForward);
		polyGroup.AddPolygon(new(polyVertexSpan, Direction.Left), Direction.Backward, Direction.Down, polyVertexSpan[0]);

		// Top
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpForward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpForward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightUpBackward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftUpBackward);
		polyGroup.AddPolygon(new(polyVertexSpan, Direction.Up), Direction.Right, Direction.Backward, polyVertexSpan[0]);

		// Bottom
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownBackward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation3D.RightDownForward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownForward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation3D.LeftDownBackward);
		polyGroup.AddPolygon(new(polyVertexSpan, Direction.Down), Direction.Left, Direction.Up, polyVertexSpan[0]);

		CreateMeshPolygons(polyGroup, textureTransform, vertices, triangles);
		return CreateMesh(vertices, triangles, in config);
	}

	void CreateMeshPolygons(IMeshPolygonGroup polygons, Transform2D textureTransform, Span<MeshVertex> verticesDest, Span<VertexTriangle> trianglesDest) {
		if (verticesDest.Length < polygons.VertexCount) {
			throw new ArgumentException(
				$"Span length ({verticesDest.Length}) must be greater than " +
				$"or equal to {nameof(IMeshPolygonGroup.VertexCount)} ({polygons.VertexCount}).",
				nameof(verticesDest)
			);
		}
		if (trianglesDest.Length < polygons.TriangleCount) {
			throw new ArgumentException(
				$"Span length ({trianglesDest.Length}) must be greater than " +
				$"or equal to {nameof(IMeshPolygonGroup.TriangleCount)} ({polygons.TriangleCount}).",
				nameof(verticesDest)
			);
		}

		for (var p = 0; p < polygons.PolygonCount; ++p) {
			var polygon = polygons.GetPolygonAtIndex(p, out var texU, out var texV, out var texOrigin);

			var texCoordConverter = new DimensionConverter(texU, texV, polygon.Normal, texOrigin);

			for (var v = 0; v < polygon.VertexCount; ++v) {
				verticesDest[v] = new(
					polygon.Vertices[v],
					texCoordConverter.ConvertLocation(polygon.Vertices[v]) * textureTransform,
					texU,
					texV,
					polygon.Normal
				);
			}


			// const int MaxVerticesForStackStorage = 300;
			//
			// var heapVertexStore = null as XYPair<float>[];
			// scoped Span<XYPair<float>> vertexStore;
			//
			// if (polygon.VertexCount > MaxVerticesForStackStorage) {
			// 	heapVertexStore = ArrayPool<XYPair<float>>.Shared.Rent(polygon.VertexCount);
			// 	vertexStore = heapVertexStore.AsSpan()[..polygon.VertexCount];
			// }
			// else vertexStore = stackalloc XYPair<float>[polygon.VertexCount];

			polygon.ToPolygon2D(vertexStore).Triangulate(trianglesDest);

			//if (heapVertexStore != null) ArrayPool<XYPair<float>>.Shared.Return(heapVertexStore);
			
			verticesDest = verticesDest[polygon.VertexCount..];
			trianglesDest = trianglesDest[polygon.TriangleCount..];
		}
	}

	Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, ReadOnlySpan<char> name = default) => CreateMesh(vertices, triangles, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, scoped in MeshCreationConfig config);
}