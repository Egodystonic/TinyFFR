// Created on 2024-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Buffers;
using System.Globalization;
using System.Threading;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshBuilder {
	private static readonly Lock _staticMutationLock = new();
	
	#region Cuboid
	Mesh CreateMesh(Cuboid cuboidDesc, Transform2D? textureTransform = null, bool centreTextureOrigin = false, ReadOnlySpan<char> name = default) => CreateMesh(cuboidDesc, centreTextureOrigin, new MeshGenerationConfig { TextureTransform = textureTransform ?? Transform2D.None }, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(Cuboid cuboidDesc, bool centreTextureOrigin, in MeshGenerationConfig generationConfig, in MeshCreationConfig config) {
		if (!cuboidDesc.IsPhysicallyValid) {
			throw new ArgumentException("Given cuboid must be physically valid (all extents should be positive).", nameof(cuboidDesc));
		}

		var polyVertexSpan = (Span<Location>) stackalloc Location[4];
		using var polyGroup = AllocateNewPolygonGroup();

		// Back
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation.LeftUpBackward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation.LeftDownBackward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation.RightDownBackward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation.RightUpBackward);
		polyGroup.Add(
			new(polyVertexSpan, Direction.Backward),
			Direction.Right,
			Direction.Up,
			centreTextureOrigin ? cuboidDesc.CentroidAt(CardinalOrientation.Backward) : polyVertexSpan[1]
		);

		// Front
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation.RightUpForward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation.RightDownForward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation.LeftDownForward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation.LeftUpForward);
		polyGroup.Add(
			new(polyVertexSpan, Direction.Forward),
			Direction.Left,
			Direction.Up,
			centreTextureOrigin ? cuboidDesc.CentroidAt(CardinalOrientation.Forward) : polyVertexSpan[1]
		);

		// Right
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation.RightUpBackward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation.RightDownBackward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation.RightDownForward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation.RightUpForward);
		polyGroup.Add(
			new(polyVertexSpan, Direction.Right),
			Direction.Forward,
			Direction.Up,
			centreTextureOrigin ? cuboidDesc.CentroidAt(CardinalOrientation.Right) : polyVertexSpan[1]
		);

		// Left
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation.LeftUpForward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation.LeftDownForward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation.LeftDownBackward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation.LeftUpBackward);
		polyGroup.Add(
			new(polyVertexSpan, Direction.Left),
			Direction.Backward,
			Direction.Up,
			centreTextureOrigin ? cuboidDesc.CentroidAt(CardinalOrientation.Left) : polyVertexSpan[1]
		);

		// Top
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation.LeftUpForward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation.LeftUpBackward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation.RightUpBackward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation.RightUpForward);
		polyGroup.Add(
			new(polyVertexSpan, Direction.Up),
			Direction.Right,
			Direction.Forward,
			centreTextureOrigin ? cuboidDesc.CentroidAt(CardinalOrientation.Up) : polyVertexSpan[1]
		);

		// Bottom
		polyVertexSpan[0] = cuboidDesc.CornerAt(DiagonalOrientation.RightDownBackward);
		polyVertexSpan[1] = cuboidDesc.CornerAt(DiagonalOrientation.LeftDownBackward);
		polyVertexSpan[2] = cuboidDesc.CornerAt(DiagonalOrientation.LeftDownForward);
		polyVertexSpan[3] = cuboidDesc.CornerAt(DiagonalOrientation.RightDownForward);
		polyGroup.Add(
			new(polyVertexSpan, Direction.Down),
			Direction.Left,
			Direction.Backward,
			centreTextureOrigin ? cuboidDesc.CentroidAt(CardinalOrientation.Down) : polyVertexSpan[3]
		);

		return CreateMesh(polyGroup, in generationConfig, in config);
	}
	#endregion

	#region Sphere
	private const int MaxSphereSubdivisionLevel = 10;
	private static readonly ArrayPoolBackedVector<(MeshVertex[] Vertices, VertexTriangle[] Triangles)> _sphereMeshes = new();
	private static readonly HeapPool _sphereVertexPool = new();

	Mesh CreateMesh(Sphere sphereDesc, Transform2D? textureTransform = null, int subdivisionLevel = 4, ReadOnlySpan<char> name = default) => CreateMesh(sphereDesc, subdivisionLevel, new MeshGenerationConfig { TextureTransform = textureTransform ?? Transform2D.None }, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(Sphere sphereDesc, int subdivisionLevel, in MeshGenerationConfig generationConfig, in MeshCreationConfig config) {
		if (!sphereDesc.IsPhysicallyValid) {
			throw new ArgumentException("Given sphere must be physically valid (radius should be positive).", nameof(sphereDesc));
		}
		if (subdivisionLevel < 0) throw new ArgumentOutOfRangeException(nameof(subdivisionLevel), subdivisionLevel, $"Subdivision level can not be negative.");
		subdivisionLevel = Int32.Min(subdivisionLevel, MaxSphereSubdivisionLevel);

		ReadOnlySpan<MeshVertex> defaultVertices;
		ReadOnlySpan<VertexTriangle> triangles;
		PooledHeapMemory<MeshVertex>? verticesMemory;

		lock (_staticMutationLock) {
			if (_sphereMeshes.Count == 0) {
				_sphereMeshes.Add(GenerateStartingIcosphere());
			}

			while (_sphereMeshes.Count <= subdivisionLevel) {
				_sphereMeshes.Add(SubdivideIcosphere(_sphereMeshes[^1].Vertices, _sphereMeshes[^1].Triangles));
			}

			var prebuiltMeshTuple = _sphereMeshes[subdivisionLevel];
			defaultVertices = prebuiltMeshTuple.Vertices;
			triangles = prebuiltMeshTuple.Triangles;
			verticesMemory = generationConfig.TextureTransform != Transform2D.None
				? _sphereVertexPool.Borrow<MeshVertex>(defaultVertices.Length)
				: null;
		}

		try {
			var configWithScaling = config with { LinearRescalingFactor = config.LinearRescalingFactor * sphereDesc.Radius };
			if (verticesMemory == null) {
				return CreateMesh(defaultVertices, triangles, in configWithScaling);
			}
			else {
				for (var i = 0; i < defaultVertices.Length; ++i) {
					verticesMemory.Value.Buffer[i] = defaultVertices[i] with { TextureCoords = defaultVertices[i].TextureCoords * generationConfig.TextureTransform };
				}
				return CreateMesh(verticesMemory.Value.Buffer, triangles, in configWithScaling);
			}
		}
		finally {
			lock (_staticMutationLock) {
				verticesMemory?.Dispose();
			}
		}
	}

	private (MeshVertex[] Vertices, VertexTriangle[] Triangles) GenerateStartingIcosphere() {
		Span<Location> points = stackalloc Location[12];
		var vertices = new MeshVertex[20 * 3];
		var triangles = new VertexTriangle[20];
		
		points[0] = new(-1f, MathUtils.GoldenRatio, 0f);
		points[1] = new(1f, MathUtils.GoldenRatio, 0f);
		points[2] = new(-1f, -MathUtils.GoldenRatio, 0f);
		points[3] = new(1f, -MathUtils.GoldenRatio, 0f);

		for (var i = 0; i < 4; ++i) {
			points[i + 4] = points[i][Axis.Z, Axis.X, Axis.Y];
			points[i + 8] = points[i][Axis.Y, Axis.Z, Axis.X];
		}

		triangles[0] = new(0, 5, 11);
		triangles[1] = new(0, 1, 5);
		triangles[2] = new(0, 7, 1);
		triangles[3] = new(0, 10, 7);
		triangles[4] = new(0, 11, 10);
		
		triangles[5] = new(1, 9, 5);
		triangles[6] = new(5, 4, 11);
		triangles[7] = new(11, 2, 10);
		triangles[8] = new(10, 6, 7);
		triangles[9] = new(7, 8, 1);

		triangles[10] = new(3, 4, 9);
		triangles[11] = new(3, 2, 4);
		triangles[12] = new(3, 6, 2);
		triangles[13] = new(3, 8, 6);
		triangles[14] = new(3, 9, 8);

		triangles[15] = new(4, 5, 9);
		triangles[16] = new(2, 11, 4);
		triangles[17] = new(6, 10, 2);
		triangles[18] = new(8, 7, 6);
		triangles[19] = new(9, 1, 8);

		Span<XYPair<float>> pointUvs = stackalloc XYPair<float>[points.Length];
		for (var i = 0; i < points.Length; ++i) {
			points[i] = points[i].AsVect().AsUnitLength.AsLocation();
			pointUvs[i] = ConvertIcospherePointToTexUv(points[i]);
		}
		
		for (var i = 0; i < triangles.Length; ++i) {
			var triangle = triangles[i];

			WriteIcosphereTriangleVertices(
				points[triangle.IndexA],
				points[triangle.IndexB],
				points[triangle.IndexC],
				pointUvs[triangle.IndexA],
				pointUvs[triangle.IndexB],
				pointUvs[triangle.IndexC],
				vertices.AsSpan()[(i * 3)..]
			);
		}

		return (vertices, triangles);
	}

	private (MeshVertex[] Vertices, VertexTriangle[] Triangles) SubdivideIcosphere(ReadOnlySpan<MeshVertex> baseVertices, ReadOnlySpan<VertexTriangle> baseTriangles) {
		var resultVertices = new MeshVertex[baseVertices.Length * 3];
		var resultTriangles = new VertexTriangle[baseTriangles.Length * 3];

		for (var i = 0; i < baseTriangles.Length; ++i) {
			var baseTriangle = baseTriangles[i];
			var a = baseVertices[baseTriangle.IndexA].Location;
			var b = baseVertices[baseTriangle.IndexB].Location;
			var c = baseVertices[baseTriangle.IndexC].Location;
			var n = ((a.AsVect() + b.AsVect() + c.AsVect()) / 3f).AsUnitLength.AsLocation();

			var aUv = baseVertices[baseTriangle.IndexA].TextureCoords;
			var bUv = baseVertices[baseTriangle.IndexB].TextureCoords;
			var cUv = baseVertices[baseTriangle.IndexC].TextureCoords;
			var nUv = ConvertIcospherePointToTexUv(n);

			var iMult9 = i * 9;
			var resultVerticesSubSpan = resultVertices.AsSpan()[iMult9..];
			WriteIcosphereTriangleVertices(a, b, n, aUv, bUv, nUv, resultVerticesSubSpan[0..3]);
			WriteIcosphereTriangleVertices(b, c, n, bUv, cUv, nUv, resultVerticesSubSpan[3..6]);
			WriteIcosphereTriangleVertices(c, a, n, cUv, aUv, nUv, resultVerticesSubSpan[6..9]);
			resultTriangles[i * 3 + 0] = new VertexTriangle(iMult9 + 0, iMult9 + 1, iMult9 + 2);
			resultTriangles[i * 3 + 1] = new VertexTriangle(iMult9 + 3, iMult9 + 4, iMult9 + 5);
			resultTriangles[i * 3 + 2] = new VertexTriangle(iMult9 + 6, iMult9 + 7, iMult9 + 8);
		}

		return (resultVertices, resultTriangles);
	}

	private static void WriteIcosphereTriangleVertices(Location vertexA, Location vertexB, Location vertexC, XYPair<float> vertexAUv, XYPair<float> vertexBUv, XYPair<float> vertexCUv, Span<MeshVertex> dest) {
		var normal = ((vertexA.AsVect() + vertexB.AsVect() + vertexC.AsVect()) / 3f).Direction;
		var tangent = Direction.FromDualOrthogonalization(Direction.Up, normal);
		var bitangent = Direction.FromDualOrthogonalization(normal, tangent);

		dest[0] = new MeshVertex(
			vertexA,
			vertexAUv,
			tangent, bitangent, normal
		);

		dest[1] = new MeshVertex(
			vertexB,
			vertexAUv,
			tangent, bitangent, normal
		);

		dest[2] = new MeshVertex(
			vertexC,
			vertexCUv,
			tangent, bitangent, normal
		);
	}

	private static XYPair<float> ConvertIcospherePointToTexUv(Location point) {
		var xzPlaneConverter = new DimensionConverter(Direction.Left, Direction.Forward, Direction.Up);
		return new XYPair<float>(
			xzPlaneConverter.ConvertVect(point.AsVect()).PolarAngle?.FullCircleFraction ?? 0f,
			(point.Y + MathUtils.GoldenRatio) / (MathUtils.GoldenRatio * 2f)
		);
	}
	#endregion

	#region Polygon(s)
	IMeshPolygonGroup AllocateNewPolygonGroup();

	Mesh CreateMesh(Polygon polygon, Direction? textureUDirection = null, Direction? textureVDirection = null, Location? textureOrigin = null, Transform2D? textureTransform = null, ReadOnlySpan<char> name = default) {
		polygon.FillInMissingTriangulationParameters(ref textureUDirection, ref textureVDirection, ref textureOrigin);
		return CreateMesh(
			polygon,
			textureUDirection.Value,
			textureVDirection.Value,
			textureOrigin.Value,
			new MeshGenerationConfig { TextureTransform = textureTransform ?? Transform2D.None },
			new MeshCreationConfig { Name = name }
		);
	}
	Mesh CreateMesh(Polygon polygon, Direction textureUDirection, Direction textureVDirection, Location textureOrigin, in MeshGenerationConfig generationConfig, in MeshCreationConfig config) {
		using var polyGroup = AllocateNewPolygonGroup();
		polyGroup.Add(polygon, textureUDirection, textureVDirection, textureOrigin);
		return CreateMesh(polyGroup, in generationConfig, in config);
	}

	Mesh CreateMesh(IMeshPolygonGroup polygons, Transform2D? textureTransform = null, ReadOnlySpan<char> name = default) => CreateMesh(polygons, new MeshGenerationConfig { TextureTransform = textureTransform ?? Transform2D.None }, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(IMeshPolygonGroup polygons, in MeshGenerationConfig generationConfig, in MeshCreationConfig config) {
		ArgumentNullException.ThrowIfNull(polygons);
		polygons.Triangulate(generationConfig.TextureTransform, out var vertices, out var triangles);
		return CreateMesh(vertices, triangles, config);
	}
	#endregion

	#region Vertices
	Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, ReadOnlySpan<char> name = default) => CreateMesh(vertices, triangles, new MeshCreationConfig { Name = name });
	Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, in MeshCreationConfig config);
	#endregion
}