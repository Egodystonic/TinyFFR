// Created on 2024-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Buffers;
using System.Globalization;
using System.Threading;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Rendering;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// Creates meshes from shape descriptions, from polygons, or directly from vertices and triangles.
/// </summary>
/// <remarks>
/// <para>
/// This is the counterpart to loading geometry from a file. The shape overloads are the quickest way to get something on screen;
/// the polygon overloads suit flat-faced geometry described by its outlines; and the vertex overloads are for geometry your own
/// code produces.
/// </para>
/// <para>
/// Every mesh must be disposed when nothing uses it any more.
/// </para>
/// </remarks>
public interface IMeshBuilder {
	/// <summary>
	/// The greatest number of bones a skeletal mesh may have: <c>255</c>.
	/// </summary>
	public const int MaxSkeletalBoneCount = 255;
	private static readonly Lock _staticMutationLock = new();
	
	#region Cuboid
	/// <summary>
	/// Creates a box-shaped mesh.
	/// </summary>
	/// <param name="cuboidDesc">The box's dimensions. All three extents must be positive.</param>
	/// <param name="textureTransform">How to scale, rotate and shift the generated texture coordinates, or <see langword="null"/> for no change.</param>
	/// <param name="centreTextureOrigin">Whether each face's texture is centred on that face rather than starting at its corner.</param>
	/// <param name="name">The name to give the mesh. May be left empty.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="cuboidDesc"/> has a non-positive extent.</exception>
	Mesh CreateMesh(Cuboid cuboidDesc, Transform2D? textureTransform = null, bool centreTextureOrigin = false, ReadOnlySpan<char> name = default) => CreateMesh(cuboidDesc, centreTextureOrigin, new MeshGenerationConfig { TextureTransform = textureTransform ?? Transform2D.None }, new MeshCreationConfig { Name = name });
	/// <summary>
	/// Creates a box-shaped mesh, using the given configs.
	/// </summary>
	/// <param name="cuboidDesc">The box's dimensions. All three extents must be positive.</param>
	/// <param name="centreTextureOrigin">Whether each face's texture is centred on that face rather than starting at its corner.</param>
	/// <param name="generationConfig">Controls how the vertices are produced, chiefly how textures lie across them.</param>
	/// <param name="config">Controls how the mesh is created.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="cuboidDesc"/> has a non-positive extent.</exception>
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
	private const int MaxSphereSubdivisionLevel = 7;
	private static readonly ArrayPoolBackedVector<(MeshVertex[] Vertices, VertexTriangle[] Triangles)> _sphereMeshes = new();
	private static readonly ArrayPoolBackedVector<MeshVertex[]> _nonTransformedFixedSeamVertexCache = new();
	private static readonly HeapPool _sphereVertexPool = new();

	/// <summary>
	/// Creates a sphere-shaped mesh.
	/// </summary>
	/// <remarks>
	/// A sphere is built by repeatedly subdividing a twenty-sided solid, so each extra subdivision level roughly quadruples
	/// the triangle count. Level <c>4</c> looks round at ordinary distances; higher levels are rarely worth their cost.
	/// </remarks>
	/// <param name="sphereDesc">The sphere's radius, which must be positive.</param>
	/// <param name="textureTransform">How to scale, rotate and shift the generated texture coordinates, or <see langword="null"/> for no change.</param>
	/// <param name="subdivisionLevel">How many times to subdivide, and so how round the result is.
	/// Must not be negative, and is capped at <c>7</c>. Defaults to <c>4</c>. Higher levels take longer to compute
	/// (first invocation only) and cost more to render, but look smoother.</param>
	/// <param name="name">The name to give the mesh. May be left empty.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="sphereDesc"/> has a non-positive radius.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="subdivisionLevel"/> is negative.</exception>
	Mesh CreateMesh(Sphere sphereDesc, Transform2D? textureTransform = null, int subdivisionLevel = 4, ReadOnlySpan<char> name = default) => CreateMesh(sphereDesc, subdivisionLevel, new MeshGenerationConfig { TextureTransform = textureTransform ?? Transform2D.None }, new MeshCreationConfig { Name = name });
	/// <summary>
	/// Creates a sphere-shaped mesh, using the given configs.
	/// </summary>
	/// <remarks>
	/// A sphere is built by repeatedly subdividing a twenty-sided solid, so each extra subdivision level roughly quadruples
	/// the triangle count. Level <c>4</c> looks round at ordinary distances; higher levels are rarely worth their cost.
	/// </remarks>
	/// <param name="sphereDesc">The sphere's radius, which must be positive.</param>
	/// <param name="subdivisionLevel">How many times to subdivide, and so how round the result is.
	/// Must not be negative, and is capped at <c>7</c>. Defaults to <c>4</c>. Higher levels take longer to compute
	/// (first invocation only) and cost more to render, but look smoother.</param>
	/// <param name="generationConfig">Controls how the vertices are produced, chiefly how textures lie across them.</param>
	/// <param name="config">Controls how the mesh is created.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="sphereDesc"/> has a non-positive radius.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="subdivisionLevel"/> is negative.</exception>
	Mesh CreateMesh(Sphere sphereDesc, int subdivisionLevel, in MeshGenerationConfig generationConfig, in MeshCreationConfig config) {
		static void CreateFixedSeamVertexCacheForLatestMeshLevel() {
			var latestMeshLevelTuple = _sphereMeshes[^1];
			var fixedSeamVertices = new MeshVertex[latestMeshLevelTuple.Vertices.Length];
			FixIcosphereSeams(latestMeshLevelTuple.Vertices, latestMeshLevelTuple.Triangles, fixedSeamVertices, 1f);
			_nonTransformedFixedSeamVertexCache.Add(fixedSeamVertices);
		}
		
		if (!sphereDesc.IsPhysicallyValid) {
			throw new ArgumentException("Given sphere must be physically valid (radius should be positive).", nameof(sphereDesc));
		}
		if (subdivisionLevel < 0) throw new ArgumentOutOfRangeException(nameof(subdivisionLevel), subdivisionLevel, $"Subdivision level can not be negative.");
		subdivisionLevel = Int32.Min(subdivisionLevel, MaxSphereSubdivisionLevel);

		ReadOnlySpan<MeshVertex> defaultVertices;
		ReadOnlySpan<MeshVertex> fixedVertices;
		ReadOnlySpan<VertexTriangle> triangles;
		PooledHeapMemory<MeshVertex>? verticesMemory;

		lock (_staticMutationLock) {
			if (_sphereMeshes.Count == 0) {
				_sphereMeshes.Add(GenerateStartingIcosphere());
				CreateFixedSeamVertexCacheForLatestMeshLevel();
			}

			while (_sphereMeshes.Count <= subdivisionLevel) {
				_sphereMeshes.Add(SubdivideIcosphere(_sphereMeshes[^1].Vertices, _sphereMeshes[^1].Triangles));
				CreateFixedSeamVertexCacheForLatestMeshLevel();
			}

			var prebuiltMeshTuple = _sphereMeshes[subdivisionLevel];
			defaultVertices = prebuiltMeshTuple.Vertices;
			fixedVertices = _nonTransformedFixedSeamVertexCache[subdivisionLevel];
			triangles = prebuiltMeshTuple.Triangles;
			verticesMemory = generationConfig.TextureTransform != Transform2D.None
				? _sphereVertexPool.Borrow<MeshVertex>(defaultVertices.Length)
				: null;
		}

		try {
			var configWithScaling = config with { LinearRescalingFactor = config.LinearRescalingFactor * sphereDesc.Radius };
			if (verticesMemory == null) {
				return CreateMesh(fixedVertices, triangles, in configWithScaling);
			}
			else {
				var texTransform = generationConfig.TextureTransform with { Scaling = generationConfig.TextureTransform.Scaling.Reciprocal ?? XYPair<float>.Zero };
				for (var i = 0; i < defaultVertices.Length; ++i) {
					verticesMemory.Value.Span[i] = defaultVertices[i] with { TextureCoords = defaultVertices[i].TextureCoords * texTransform };
				}
				FixIcosphereSeams(verticesMemory.Value.Span, triangles, verticesMemory.Value.Span, texTransform.Scaling.X);
				return CreateMesh(verticesMemory.Value.Span, triangles, in configWithScaling);
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

		triangles[0] = new(5, 0, 11);
		triangles[1] = new(1, 0, 5);
		triangles[2] = new(7, 0, 1);
		triangles[3] = new(10, 0, 7);
		triangles[4] = new(11, 0, 10);
		
		triangles[5] = new(9, 1, 5);
		triangles[6] = new(4, 5, 11);
		triangles[7] = new(2, 11, 10);
		triangles[8] = new(6, 10, 7);
		triangles[9] = new(8, 7, 1);

		triangles[10] = new(4, 3, 9);
		triangles[11] = new(2, 3, 4);
		triangles[12] = new(6, 3, 2);
		triangles[13] = new(8, 3, 6);
		triangles[14] = new(9, 3, 8);

		triangles[15] = new(5, 4, 9);
		triangles[16] = new(11, 2, 4);
		triangles[17] = new(10, 6, 2);
		triangles[18] = new(7, 8, 6);
		triangles[19] = new(1, 9, 8);

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

			triangles[i] = new VertexTriangle(i * 3, i * 3 + 1, i * 3 + 2);
		}

		return (vertices, triangles);
	}

	private (MeshVertex[] Vertices, VertexTriangle[] Triangles) SubdivideIcosphere(ReadOnlySpan<MeshVertex> baseVertices, ReadOnlySpan<VertexTriangle> baseTriangles) {
		var resultVertices = new MeshVertex[baseVertices.Length * 4];
		var resultTriangles = new VertexTriangle[baseTriangles.Length * 4];

		for (var i = 0; i < baseTriangles.Length; ++i) {
			var baseTriangle = baseTriangles[i];
			var a = baseVertices[baseTriangle.IndexA].Location;
			var b = baseVertices[baseTriangle.IndexB].Location;
			var c = baseVertices[baseTriangle.IndexC].Location;
			
			var ab = (a.AsVect() + b.AsVect()).AsUnitLength.AsLocation();
			var bc = (b.AsVect() + c.AsVect()).AsUnitLength.AsLocation();
			var ca = (c.AsVect() + a.AsVect()).AsUnitLength.AsLocation();

			var aUv = baseVertices[baseTriangle.IndexA].TextureCoords;
			var bUv = baseVertices[baseTriangle.IndexB].TextureCoords;
			var cUv = baseVertices[baseTriangle.IndexC].TextureCoords;
			var abUv = ConvertIcospherePointToTexUv(ab);
			var bcUv = ConvertIcospherePointToTexUv(bc);
			var caUv = ConvertIcospherePointToTexUv(ca);

			var iTimes12 = i * 12;
			var resultVerticesSubSpan = resultVertices.AsSpan()[iTimes12..];
			WriteIcosphereTriangleVertices(ca, a, ab, caUv, aUv, abUv, resultVerticesSubSpan[0..3]);
			WriteIcosphereTriangleVertices(ab, b, bc, abUv, bUv, bcUv, resultVerticesSubSpan[3..6]);
			WriteIcosphereTriangleVertices(bc, c, ca, bcUv, cUv, caUv, resultVerticesSubSpan[6..9]);
			WriteIcosphereTriangleVertices(ab, bc, ca, abUv, bcUv, caUv, resultVerticesSubSpan[9..12]);
			
			resultTriangles[i * 4 + 0] = new VertexTriangle(iTimes12 + 0, iTimes12 + 1, iTimes12 + 2);
			resultTriangles[i * 4 + 1] = new VertexTriangle(iTimes12 + 3, iTimes12 + 4, iTimes12 + 5);
			resultTriangles[i * 4 + 2] = new VertexTriangle(iTimes12 + 6, iTimes12 + 7, iTimes12 + 8);
			resultTriangles[i * 4 + 3] = new VertexTriangle(iTimes12 + 9, iTimes12 + 10, iTimes12 + 11);
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
			vertexBUv,
			tangent, bitangent, normal
		);

		dest[2] = new MeshVertex(
			vertexC,
			vertexCUv,
			tangent, bitangent, normal
		);
	}

	private static XYPair<float> ConvertIcospherePointToTexUv(Location point) {
		var xzPlaneConverter = new DimensionConverter(Direction.Right, Direction.Forward, Direction.Up);
		var y = (point.Y + 1f) * 0.5f;
		// Maintainer's note: The 3x multiplier is the best way to map the texture laterally trying to keep its original proportionality intact.
		// 3.14 would be perfect but that creates an obvious seam; 3x is the closest value that perfectly repeats.
		return new XYPair<float>(
			((xzPlaneConverter.ConvertVect(point.AsVect()).PolarAngle?.FullCircleFraction ?? y) * 3f) % 1f,
			y
		);
	}

	private static void FixIcosphereSeams(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, Span<MeshVertex> dest, float lateralScale) {
		var maxDiffBeforeFix = (0.5f * lateralScale);

		foreach (var triangle in triangles) {
			var vertexAUv = vertices[triangle.IndexA].TextureCoords;
			var vertexBUv = vertices[triangle.IndexB].TextureCoords;
			var vertexCUv = vertices[triangle.IndexC].TextureCoords;

			var qDiff = MathF.Abs(vertexAUv.X - vertexBUv.X);
			var rDiff = MathF.Abs(vertexBUv.X - vertexCUv.X);

			static XYPair<float> AdjustSeamUv(XYPair<float> input, float median, float adjustment) => new(input.X < median ? input.X + adjustment : input.X, input.Y);

			if (qDiff > maxDiffBeforeFix || rDiff > maxDiffBeforeFix) {
				vertexAUv = AdjustSeamUv(vertexAUv, maxDiffBeforeFix, lateralScale);
				vertexBUv = AdjustSeamUv(vertexBUv, maxDiffBeforeFix, lateralScale);
				vertexCUv = AdjustSeamUv(vertexCUv, maxDiffBeforeFix, lateralScale);
			}

			dest[triangle.IndexA] = vertices[triangle.IndexA] with { TextureCoords = vertexAUv };
			dest[triangle.IndexB] = vertices[triangle.IndexB] with { TextureCoords = vertexBUv };
			dest[triangle.IndexC] = vertices[triangle.IndexC] with { TextureCoords = vertexCUv };
		}
	}
	#endregion

	#region Arrow
	/// <summary>
	/// The number of segments an arrow mesh is divided in to around its tail-to-head axis when none is specified: <c>32</c>.
	/// </summary>
	public const int DefaultArrowSegmentCount = 32;
	/// <summary>
	/// The fewest segments an arrow mesh may be divided in to around its tail-to-head axis: <c>3</c>.
	/// </summary>
	public const int MinArrowSegmentCount = 3;

	/// <summary>
	/// Creates an arrow-shaped mesh: A round stem capped at its tail, topped by a cone-shaped head that is at least as wide as the stem.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The arrow points along <see cref="Direction.Forward"/>, with its tail at the mesh's origin unless <paramref name="origin"/> says otherwise.
	/// </para>
	/// <para>
	/// The texture wraps once around the arrow along its horizontal (U) axis, and runs from <c>0</c> at the tail to <c>1</c> at the tip of the head along its
	/// vertical (V) axis, in proportion to the distance along the arrow. The flat end of the stem and the flat back of the head therefore each sample a single
	/// row of the texture, which makes the mesh well suited to textures that vary from tail to head, such as gradients.
	/// </para>
	/// </remarks>
	/// <param name="stemLength">The length of the stem, from the tail to the back of the head. Must be positive.</param>
	/// <param name="stemRadius">The radius of the stem. Must be positive.</param>
	/// <param name="headLength">The length of the head, from its flat back to its tip. Must be positive.</param>
	/// <param name="headRadius">The radius of the flat back of the head. Must be positive and no smaller than <paramref name="stemRadius"/>.</param>
	/// <param name="origin">Which point along the arrow becomes the mesh's origin. Defaults to <see cref="ArrowMeshOrigin.Tail"/>.</param>
	/// <param name="segmentCount">How many segments to divide the arrow in to around its tail-to-head axis, and so how round it looks.
	/// Must be at least <see cref="MinArrowSegmentCount"/>. Defaults to <see cref="DefaultArrowSegmentCount"/>.</param>
	/// <param name="textureTransform">How to scale, rotate and shift the generated texture coordinates, or <see langword="null"/> for no change.</param>
	/// <param name="name">The name to give the mesh. May be left empty.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when any length or radius is not positive, or when <paramref name="segmentCount"/> is less than <see cref="MinArrowSegmentCount"/>.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="headRadius"/> is smaller than <paramref name="stemRadius"/>.</exception>
	Mesh CreateMesh(float stemLength, float stemRadius, float headLength, float headRadius, ArrowMeshOrigin origin = ArrowMeshOrigin.Tail, int segmentCount = DefaultArrowSegmentCount, Transform2D? textureTransform = null, ReadOnlySpan<char> name = default) {
		return CreateMesh(
			stemLength,
			stemRadius,
			headLength,
			headRadius,
			origin,
			segmentCount,
			new MeshGenerationConfig { TextureTransform = textureTransform ?? Transform2D.None },
			new MeshCreationConfig { Name = name }
		);
	}
	/// <summary>
	/// Creates an arrow-shaped mesh: A round stem capped at its tail, topped by a cone-shaped head that is at least as wide as the stem; using the given configs.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The arrow points along <see cref="Direction.Forward"/>, with its tail at the mesh's origin unless <paramref name="origin"/> says otherwise.
	/// Any <see cref="MeshCreationConfig.OriginTranslation"/> in <paramref name="config"/> is applied on top of the origin chosen by <paramref name="origin"/>.
	/// </para>
	/// <para>
	/// The texture wraps once around the arrow along its horizontal (U) axis, and runs from <c>0</c> at the tail to <c>1</c> at the tip of the head along its
	/// vertical (V) axis, in proportion to the distance along the arrow. The flat end of the stem and the flat back of the head therefore each sample a single
	/// row of the texture, which makes the mesh well suited to textures that vary from tail to head, such as gradients.
	/// </para>
	/// </remarks>
	/// <param name="stemLength">The length of the stem, from the tail to the back of the head. Must be positive.</param>
	/// <param name="stemRadius">The radius of the stem. Must be positive.</param>
	/// <param name="headLength">The length of the head, from its flat back to its tip. Must be positive.</param>
	/// <param name="headRadius">The radius of the flat back of the head. Must be positive and no smaller than <paramref name="stemRadius"/>.</param>
	/// <param name="origin">Which point along the arrow becomes the mesh's origin.</param>
	/// <param name="segmentCount">How many segments to divide the arrow in to around its tail-to-head axis, and so how round it looks.
	/// Must be at least <see cref="MinArrowSegmentCount"/>.</param>
	/// <param name="generationConfig">Controls how the vertices are produced, chiefly how textures lie across them.</param>
	/// <param name="config">Controls how the mesh is created.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when any length or radius is not positive, or when <paramref name="segmentCount"/> is less than <see cref="MinArrowSegmentCount"/>.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="headRadius"/> is smaller than <paramref name="stemRadius"/>.</exception>
	Mesh CreateMesh(float stemLength, float stemRadius, float headLength, float headRadius, ArrowMeshOrigin origin, int segmentCount, in MeshGenerationConfig generationConfig, in MeshCreationConfig config) {
		if (!(stemLength > 0f)) throw new ArgumentOutOfRangeException(nameof(stemLength), stemLength, "Stem length must be positive.");
		if (!(stemRadius > 0f)) throw new ArgumentOutOfRangeException(nameof(stemRadius), stemRadius, "Stem radius must be positive.");
		if (!(headLength > 0f)) throw new ArgumentOutOfRangeException(nameof(headLength), headLength, "Head length must be positive.");
		if (!(headRadius > 0f)) throw new ArgumentOutOfRangeException(nameof(headRadius), headRadius, "Head radius must be positive.");
		if (headRadius < stemRadius) throw new ArgumentException("Head radius can not be smaller than stem radius.", nameof(headRadius));
		if (segmentCount < MinArrowSegmentCount) throw new ArgumentOutOfRangeException(nameof(segmentCount), segmentCount, $"Segment count must be at least {MinArrowSegmentCount}.");

		var n = segmentCount;
		var totalLength = stemLength + headLength;
		var headBaseV = stemLength / totalLength;
		var texTransform = generationConfig.TextureTransform;

		using var vertexBuffer = GetPooledVertexBuffer(GetArrowMeshVertexCount(n));
		using var triangleBuffer = GetPooledTriangleBuffer(GetArrowMeshTriangleCount(n));
		var vertices = vertexBuffer.Span;
		var triangles = triangleBuffer.Span;

		var slantMagnitudeReciprocal = 1f / MathF.Sqrt(headLength * headLength + headRadius * headRadius);
		var slantAxial = headRadius * slantMagnitudeReciprocal;
		var slantRadial = headLength * slantMagnitudeReciprocal;

		var tailCapCentreStart = 0;
		var tailCapRingStart = tailCapCentreStart + n;
		var stemTailRingStart = tailCapRingStart + (n + 1);
		var stemHeadRingStart = stemTailRingStart + (n + 1);
		var shoulderInnerRingStart = stemHeadRingStart + (n + 1);
		var shoulderOuterRingStart = shoulderInnerRingStart + (n + 1);
		var coneBaseRingStart = shoulderOuterRingStart + (n + 1);
		var coneTipStart = coneBaseRingStart + (n + 1);

		for (var i = 0; i <= n; ++i) {
			var u = i == n ? 1f : (float) i / n;
			var (sin, cos) = i == n ? (0f, 1f) : MathF.SinCos(u * MathF.Tau);
			var radial = new Direction(cos, sin, 0f);
			var tangent = new Direction(-sin, cos, 0f);

			vertices[tailCapRingStart + i] = new MeshVertex(new Location(cos * stemRadius, sin * stemRadius, 0f), new XYPair<float>(u, 0f) * texTransform, tangent, radial, Direction.Backward);
			vertices[stemTailRingStart + i] = new MeshVertex(new Location(cos * stemRadius, sin * stemRadius, 0f), new XYPair<float>(u, 0f) * texTransform, tangent, Direction.Forward, radial);
			vertices[stemHeadRingStart + i] = new MeshVertex(new Location(cos * stemRadius, sin * stemRadius, stemLength), new XYPair<float>(u, headBaseV) * texTransform, tangent, Direction.Forward, radial);
			vertices[shoulderInnerRingStart + i] = new MeshVertex(new Location(cos * stemRadius, sin * stemRadius, stemLength), new XYPair<float>(u, headBaseV) * texTransform, tangent, radial, Direction.Backward);
			vertices[shoulderOuterRingStart + i] = new MeshVertex(new Location(cos * headRadius, sin * headRadius, stemLength), new XYPair<float>(u, headBaseV) * texTransform, tangent, radial, Direction.Backward);
			vertices[coneBaseRingStart + i] = new MeshVertex(
				new Location(cos * headRadius, sin * headRadius, stemLength),
				new XYPair<float>(u, headBaseV) * texTransform,
				tangent,
				new Direction(-cos * slantAxial, -sin * slantAxial, slantRadial),
				new Direction(cos * slantRadial, sin * slantRadial, slantAxial)
			);
		}

		for (var i = 0; i < n; ++i) {
			var u = (i + 0.5f) / n;
			var (sin, cos) = MathF.SinCos(u * MathF.Tau);
			var tangent = new Direction(-sin, cos, 0f);

			vertices[tailCapCentreStart + i] = new MeshVertex(Location.Origin, new XYPair<float>(u, 0f) * texTransform, tangent, new Direction(cos, sin, 0f), Direction.Backward);
			vertices[coneTipStart + i] = new MeshVertex(
				new Location(0f, 0f, totalLength),
				new XYPair<float>(u, 1f) * texTransform,
				tangent,
				new Direction(-cos * slantAxial, -sin * slantAxial, slantRadial),
				new Direction(cos * slantRadial, sin * slantRadial, slantAxial)
			);
		}

		var t = 0;
		for (var i = 0; i < n; ++i) {
			triangles[t++] = new VertexTriangle(tailCapCentreStart + i, tailCapRingStart + i + 1, tailCapRingStart + i);

			triangles[t++] = new VertexTriangle(stemTailRingStart + i, stemTailRingStart + i + 1, stemHeadRingStart + i);
			triangles[t++] = new VertexTriangle(stemTailRingStart + i + 1, stemHeadRingStart + i + 1, stemHeadRingStart + i);

			triangles[t++] = new VertexTriangle(shoulderInnerRingStart + i, shoulderInnerRingStart + i + 1, shoulderOuterRingStart + i);
			triangles[t++] = new VertexTriangle(shoulderInnerRingStart + i + 1, shoulderOuterRingStart + i + 1, shoulderOuterRingStart + i);

			triangles[t++] = new VertexTriangle(coneBaseRingStart + i, coneBaseRingStart + i + 1, coneTipStart + i);
		}

		var originOffset = origin switch {
			ArrowMeshOrigin.HeadTip => totalLength,
			ArrowMeshOrigin.Centre => totalLength * 0.5f,
			_ => 0f
		};

		return CreateMesh(
			vertices,
			triangles,
			config with { OriginTranslation = config.OriginTranslation + new Vect(0f, 0f, originOffset) }
		);
	}

	internal static int GetArrowMeshVertexCount(int segmentCount) => segmentCount * 8 + 6;
	internal static int GetArrowMeshTriangleCount(int segmentCount) => segmentCount * 6;
	#endregion

	#region Polygon(s)
	/// <summary>
	/// Obtains an empty polygon group for assembling a group of polygons that can then be passed to a <c>CreateMesh</c> overload that consumes them.
	/// </summary>
	/// <remarks>
	/// Dispose the group when finished with it, or clear and refill it to build another mesh without allocating again.
	/// </remarks>
	IMeshPolygonGroup AllocateNewPolygonGroup();
	
	/// <summary>
	/// Creates a mesh from a single polygon.
	/// </summary>
	/// <param name="polygon">The polygon to build the mesh from. Its vertices must be given in anticlockwise order as seen from its front face.</param>
	/// <param name="textureUDirection">The direction across the polygon in which the texture's horizontal coordinate increases, or <see langword="null"/> to derive one.</param>
	/// <param name="textureVDirection">The direction across the polygon in which the texture's vertical coordinate increases, or <see langword="null"/> to derive one.</param>
	/// <param name="textureOrigin">The point on the polygon that maps to the texture's origin, or <see langword="null"/> to derive one.</param>
	/// <param name="textureTransform">How to scale, rotate and shift the generated texture coordinates, or <see langword="null"/> for no change.</param>
	/// <param name="name">The name to give the mesh. May be left empty.</param>
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
	/// <summary>
	/// Creates a mesh from a single polygon, using the given configs.
	/// </summary>
	/// <param name="polygon">The polygon to build the mesh from. Its vertices must be given in anticlockwise order as seen from its front face.</param>
	/// <param name="textureUDirection">The direction across the polygon in which the texture's horizontal coordinate increases.</param>
	/// <param name="textureVDirection">The direction across the polygon in which the texture's vertical coordinate increases.</param>
	/// <param name="textureOrigin">The point on the polygon that maps to the texture's origin.</param>
	/// <param name="generationConfig">Controls how the vertices are produced, chiefly how textures lie across them.</param>
	/// <param name="config">Controls how the mesh is created.</param>
	Mesh CreateMesh(Polygon polygon, Direction textureUDirection, Direction textureVDirection, Location textureOrigin, in MeshGenerationConfig generationConfig, in MeshCreationConfig config) {
		using var polyGroup = AllocateNewPolygonGroup();
		polyGroup.Add(polygon, textureUDirection, textureVDirection, textureOrigin);
		return CreateMesh(polyGroup, in generationConfig, in config);
	}

	/// <summary>
	/// Creates a mesh from a group of polygons.
	/// </summary>
	/// <remarks>
	/// The cost of triangulating a single polygon grows steeply with its vertex count, so prefer several simple polygons to
	/// one very complex outline.
	/// </remarks>
	/// <param name="polygons">The polygons to build the mesh from.</param>
	/// <param name="textureTransform">How to scale, rotate and shift the generated texture coordinates, or <see langword="null"/> for no change..</param>
	/// <param name="name">The name to give the mesh. May be left empty.</param>
	Mesh CreateMesh(IMeshPolygonGroup polygons, Transform2D? textureTransform = null, ReadOnlySpan<char> name = default) => CreateMesh(polygons, new MeshGenerationConfig { TextureTransform = textureTransform ?? Transform2D.None }, new MeshCreationConfig { Name = name });
	/// <summary>
	/// Creates a mesh from a group of polygons, using the given configs.
	/// </summary>
	/// <remarks>
	/// The cost of triangulating a single polygon grows steeply with its vertex count, so prefer several simple polygons to
	/// one very complex outline.
	/// </remarks>
	/// <param name="polygons">The polygons to build the mesh from.</param>
	/// <param name="generationConfig">Controls how the vertices are produced, chiefly how textures lie across them.</param>
	/// <param name="config">Controls how the mesh is created.</param>
	Mesh CreateMesh(IMeshPolygonGroup polygons, in MeshGenerationConfig generationConfig, in MeshCreationConfig config) {
		ArgumentNullException.ThrowIfNull(polygons);
		polygons.Triangulate(generationConfig.TextureTransform, out var vertices, out var triangles);
		return CreateMesh(vertices, triangles, config);
	}
	#endregion
	
	#region Grid / Quad
	/// <summary>
	/// Creates a flat rectangular mesh.
	/// </summary>
	/// <remarks>
	/// A quad mesh is a one-by-one square centred on its own origin, which is then scaled and rotated in to place. It is what
	/// billboards, sprites, labels and canvas panels are drawn on.
	/// </remarks>
	/// <param name="twoSided">Whether the quad is visible from behind as well as in front.
	/// Leaving this <see langword="true"/> avoids the quad vanishing when seen from the wrong side; which may or may not be what you want.</param>
	/// <param name="backSideInvertsTextures">Whether the back face mirrors its texture, so that the quad reads the same way round from either side rather than appearing reversed from behind.</param>
	/// <param name="textureTransform">How to scale, rotate and shift the generated texture coordinates, or <see langword="null"/> for no change.</param>
	/// <param name="name">The name to give the mesh. May be left empty.</param>
	QuadMesh CreateQuadMesh(bool twoSided = true, bool backSideInvertsTextures = false, Transform2D? textureTransform = null, ReadOnlySpan<char> name = default) {
		return CreateQuadMesh(
			twoSided,
			backSideInvertsTextures,
			new MeshGenerationConfig { TextureTransform = textureTransform ?? Transform2D.None },
			new MeshCreationConfig { Name = name }
		);
	}
	/// <summary>
	/// Creates a flat rectangular mesh, using the given configs.
	/// </summary>
	/// <remarks>
	/// A quad mesh is a one-by-one square centred on its own origin, which is then scaled and rotated in to place. It is what
	/// billboards, sprites, labels and canvas panels are drawn on.
	/// </remarks>
	/// <param name="twoSided">Whether the quad is visible from behind as well as in front.
	/// Leaving this <see langword="true"/> avoids the quad vanishing when seen from the wrong side; which may or may not be what you want.</param>
	/// <param name="backSideInvertsTextures">Whether the back face mirrors its texture, so that the quad reads the same way round from either side rather than appearing reversed from behind.</param>
	/// <param name="generationConfig">Controls how the vertices are produced, chiefly how textures lie across them.</param>
	/// <param name="config">Controls how the mesh is created.</param>
	QuadMesh CreateQuadMesh(bool twoSided, bool backSideInvertsTextures, in MeshGenerationConfig generationConfig, in MeshCreationConfig config) {
		Span<MeshVertex> vertices = stackalloc MeshVertex[twoSided ? 8 : 4];
		Span<VertexTriangle> triangles = stackalloc VertexTriangle[twoSided ? 4 : 2];
		
		vertices[0] = new MeshVertex(
			new Location(-0.5f, -0.5f, 0f),
			new XYPair<float>(1f, 0f) * generationConfig.TextureTransform,
			new Direction(-1f, 0f, 0f),
			new Direction(0f, 1f, 0f),
			new Direction(0f, 0f, -1f)
		);
		vertices[1] = new MeshVertex(
			new Location(0.5f, -0.5f, 0f),
			new XYPair<float>(0f, 0f) * generationConfig.TextureTransform,
			new Direction(-1f, 0f, 0f),
			new Direction(0f, 1f, 0f),
			new Direction(0f, 0f, -1f)
		);
		vertices[2] = new MeshVertex(
			new Location(0.5f, 0.5f, 0f),
			new XYPair<float>(0f, 1f) * generationConfig.TextureTransform,
			new Direction(-1f, 0f, 0f),
			new Direction(0f, 1f, 0f),
			new Direction(0f, 0f, -1f)
		);
		vertices[3] = new MeshVertex(
			new Location(-0.5f, 0.5f, 0f),
			new XYPair<float>(1f, 1f) * generationConfig.TextureTransform,
			new Direction(-1f, 0f, 0f),
			new Direction(0f, 1f, 0f),
			new Direction(0f, 0f, -1f)
		);
		triangles[0] = new VertexTriangle(1, 0, 2);
		triangles[1] = new VertexTriangle(0, 3, 2);

		if (twoSided) {
			vertices[4] = new MeshVertex(
				new Location(-0.5f, -0.5f, 0f),
				new XYPair<float>(backSideInvertsTextures ? 1f : 0f, 0f) * generationConfig.TextureTransform,
				new Direction(1f, 0f, 0f),
				new Direction(0f, 1f, 0f),
				new Direction(0f, 0f, 1f)
			);
			vertices[5] = new MeshVertex(
				new Location(0.5f, -0.5f, 0f),
				new XYPair<float>(backSideInvertsTextures ? 0f : 1f, 0f) * generationConfig.TextureTransform,
				new Direction(1f, 0f, 0f),
				new Direction(0f, 1f, 0f),
				new Direction(0f, 0f, 1f)
			);
			vertices[6] = new MeshVertex(
				new Location(0.5f, 0.5f, 0f),
				new XYPair<float>(backSideInvertsTextures ? 0f : 1f, 1f) * generationConfig.TextureTransform,
				new Direction(1f, 0f, 0f),
				new Direction(0f, 1f, 0f),
				new Direction(0f, 0f, 1f)
			);
			vertices[7] = new MeshVertex(
				new Location(-0.5f, 0.5f, 0f),
				new XYPair<float>(backSideInvertsTextures ? 1f : 0f, 1f) * generationConfig.TextureTransform,
				new Direction(1f, 0f, 0f),
				new Direction(0f, 1f, 0f),
				new Direction(0f, 0f, 1f)
			);
			triangles[2] = new VertexTriangle(4, 5, 7);
			triangles[3] = new VertexTriangle(5, 6, 7);
		}
		
		return new QuadMesh(CreateMesh(vertices, triangles, in config));
	}
	
	/// <summary>
	/// The direction a mutable grid's first axis runs in when none is specified: <see cref="Direction.Right"/>.
	/// </summary>
	public static readonly Direction DefaultMutableGridMeshXDir = Direction.Right;
	/// <summary>
	/// The direction a mutable grid's second axis runs in when none is specified: <see cref="Direction.Forward"/>.
	/// </summary>
	public static readonly Direction DefaultMutableGridMeshYDir = Direction.Forward;
	/// <summary>
	/// The direction a mutable grid's vertices are raised in when none is specified: <see cref="Direction.Up"/>.
	/// </summary>
	public static readonly Direction DefaultMutableGridMeshUpDir = Direction.Up;
	/// <summary>
	/// Creates a flat grid sheet mesh whose vertices can be displaced at runtime, at one of the preset densities.
	/// </summary>
	/// <remarks>
	/// A mutable grid is a flat sheet of vertices that objects created from it can displace at runtime, which is how effects like rippling
	/// water, rolling terrain and waving cloth can be made. Mutable grids are also useful for mathematical or diagnostic manifold/plane visualizations.
	/// Often (but not always) paired with a dynamic <see cref="Texture"/> (i.e. one created with <see cref="Texture.AllowsDynamicWrites"/> set to <c>true</c>).
	/// </remarks>
	/// <param name="meshDensity">How finely the grid is divided. Denser grids deform more smoothly but cost more to draw and to update.</param>
	/// <param name="name">The name to give the mesh. May be left empty.</param>
	MutableGridMesh CreateMutableGridMesh(Quality meshDensity, ReadOnlySpan<char> name = default) {
		var gridDimensions = meshDensity switch {
			Quality.VeryLow => new XYPair<int>(32, 32),
			Quality.Low => new XYPair<int>(64, 64),
			Quality.High => new XYPair<int>(256, 256),
			Quality.VeryHigh => new XYPair<int>(512, 512),
			_ => new XYPair<int>(128, 128)
		};
		
		return CreateMutableGridMesh(gridDimensions, name: name);
	}
	/// <summary>
	/// Creates a flat grid sheet mesh whose vertices can be displaced at runtime.
	/// </summary>
	/// <remarks>
	/// A mutable grid is a flat sheet of vertices that objects created from it can displace at runtime, which is how effects like rippling
	/// water, rolling terrain and waving cloth can be made. Mutable grids are also useful for mathematical or diagnostic manifold/plane visualizations.
	/// Often (but not always) paired with a dynamic <see cref="Texture"/> (i.e. one created with <see cref="Texture.AllowsDynamicWrites"/> set to <c>true</c>).
	/// </remarks>
	/// <param name="gridDimensions">How many vertices the grid has across and down. Both components must be at least <c>2</c>.</param>
	/// <param name="maxHeightDisplacement">The furthest any vertex will be displaced, in metres. This is not enforced later, but
	/// sizes the mesh's bounding box now so that a deformed grid is not wrongly judged to be off screen.</param>
	/// <param name="twoSided">Whether the grid is visible from underneath as well as from above.</param>
	/// <param name="xDir">The direction the grid's first axis runs in, or <see langword="null"/> for <see cref="DefaultMutableGridMeshXDir"/>.</param>
	/// <param name="yDir">The direction the grid's second axis runs in, or <see langword="null"/> for <see cref="DefaultMutableGridMeshYDir"/>.</param>
	/// <param name="upDir">The direction vertices are raised in, or <see langword="null"/> for <see cref="DefaultMutableGridMeshUpDir"/>.</param>
	/// <param name="textureTransform">How to scale, rotate and shift the generated texture coordinates, or <see langword="null"/> for no change.</param>
	/// <param name="gridOrigin">Which corner of the grid its own origin sits at (or the centre if <see cref="Orientation2D.None"/>).</param>
	/// <param name="name">The name to give the mesh. May be left empty.</param>
	MutableGridMesh CreateMutableGridMesh(XYPair<int> gridDimensions, float maxHeightDisplacement = 1f, bool twoSided = true, Direction? xDir = null, Direction? yDir = null, Direction? upDir = null, Transform2D? textureTransform = null, Orientation2D gridOrigin = Orientation2D.None, ReadOnlySpan<char> name = default) {
		return CreateMutableGridMesh(
			gridDimensions,
			maxHeightDisplacement,
			twoSided,
			xDir ?? DefaultMutableGridMeshXDir,
			yDir ?? DefaultMutableGridMeshYDir,
			upDir ?? DefaultMutableGridMeshUpDir,
			gridOrigin,
			new MeshGenerationConfig { TextureTransform = textureTransform ?? Transform2D.None },
			new MeshCreationConfig { Name = name }
		);
	}
	/// <summary>
	/// Creates a flat grid sheet mesh whose vertices can be displaced at runtime.
	/// </summary>
	/// <remarks>
	/// A mutable grid is a flat sheet of vertices that objects created from it can displace at runtime, which is how effects like rippling
	/// water, rolling terrain and waving cloth can be made. Mutable grids are also useful for mathematical or diagnostic manifold/plane visualizations.
	/// Often (but not always) paired with a dynamic <see cref="Texture"/> (i.e. one created with <see cref="Texture.AllowsDynamicWrites"/> set to <c>true</c>).
	/// </remarks>
	/// <param name="gridDimensions">How many vertices the grid has across and down. Both components must be at least <c>2</c>.</param>
	/// <param name="maxHeightDisplacement">The furthest any vertex will be displaced, in metres. This is not enforced later, but
	/// sizes the mesh's bounding box now so that a deformed grid is not wrongly judged to be off screen.</param>
	/// <param name="twoSided">Whether the grid is visible from underneath as well as from above.</param>
	/// <param name="xDir">The direction the grid's first axis runs in.</param>
	/// <param name="yDir">The direction the grid's second axis runs in.</param>
	/// <param name="upDir">The direction vertices are raised in.</param>
	/// <param name="gridOrigin">Which corner of the grid its own origin sits at (or the centre if <see cref="Orientation2D.None"/>).</param>
	/// <param name="generationConfig">Controls how the vertices are produced, chiefly how textures lie across them.</param>
	/// <param name="config">Controls how the mesh is created.</param>
	MutableGridMesh CreateMutableGridMesh(XYPair<int> gridDimensions, float maxHeightDisplacement, bool twoSided, Direction xDir, Direction yDir, Direction upDir, Orientation2D gridOrigin, in MeshGenerationConfig generationConfig, in MeshCreationConfig config) {
		if (gridDimensions.X < 2 || gridDimensions.Y < 2) {
			throw new ArgumentOutOfRangeException(nameof(gridDimensions), gridDimensions, "Vertex count X and Y must be at least 2.");
		}
		
		Direction.OrthogonalizeAll(xDir, ref yDir, ref upDir);

		var cellCount = gridDimensions - XYPair<int>.One;
		const int TrianglesPerCell = 2;

		using var vertexBuffer = GetPooledVertexBuffer(gridDimensions.Area * (twoSided ? 2 : 1));
		using var triangleBuffer = GetPooledTriangleBuffer(cellCount.Area * TrianglesPerCell * (twoSided ? 2 : 1));

		var cellSize = cellCount.Cast<float>().Reciprocal!.Value;
		
		for (var y = 0; y < gridDimensions.Y; ++y) {
			// We override the value for the outer edge vertices to avoid floating point inaccuracy meaning the quad would be slightly less or more than its intended size
			var coordY = y == cellCount.Y ? 1f : y * cellSize.Y;

			for (var x = 0; x < gridDimensions.X; ++x) {
				var coordX = x == cellCount.X ? 1f : x * cellSize.X;

				var location = (xDir * coordX + yDir * coordY).AsLocation();
				var texCoord = new XYPair<float>(coordX, coordY) * generationConfig.TextureTransform;
				var vertexIndex = gridDimensions.Index(x, y);

				vertexBuffer.Span[vertexIndex] = new(location, texCoord, xDir, yDir, upDir);
				if (twoSided) {
					vertexBuffer.Span[gridDimensions.Area + vertexIndex] = new(location, texCoord, xDir, -yDir, -upDir);
				}
			}
		}

		var upDirIsLeftHanded = Direction.FromDualOrthogonalization(xDir, yDir).Dot(upDir) < 0f;
		var backFaceTriangleOffset = cellCount.Area * TrianglesPerCell;
		for (var y = 0; y < cellCount.Y; ++y) {
			for (var x = 0; x < cellCount.X; ++x) {
				var bottomLeftVertexIdx = gridDimensions.Index(x, y);
				var bottomRightVertexIdx = bottomLeftVertexIdx + 1;
				var topLeftVertexIdx = bottomLeftVertexIdx + gridDimensions.X;
				var topRightVertexIdx = topLeftVertexIdx + 1;

				var triangleBufferIdx = cellCount.Index(x, y) * TrianglesPerCell;
				var frontTri1 = new VertexTriangle(bottomLeftVertexIdx, bottomRightVertexIdx, topLeftVertexIdx);
				var frontTri2 = new VertexTriangle(bottomRightVertexIdx, topRightVertexIdx, topLeftVertexIdx);
				if (upDirIsLeftHanded) {
					frontTri1 = frontTri1.Flipped();
					frontTri2 = frontTri2.Flipped();
				}

				triangleBuffer.Span[triangleBufferIdx] = frontTri1;
				triangleBuffer.Span[triangleBufferIdx + 1] = frontTri2;

				if (twoSided) {
					triangleBuffer.Span[backFaceTriangleOffset + triangleBufferIdx] = frontTri1.ShiftedBy(gridDimensions.Area).Flipped();
					triangleBuffer.Span[backFaceTriangleOffset + triangleBufferIdx + 1] = frontTri2.ShiftedBy(gridDimensions.Area).Flipped();
				}
			}
		}

		var gridOriginNormalizedOffset = MathUtils.FindAnchorInNormalized2DCoordinateSystem(DiagonalOrientation2D.DownLeft, gridOrigin);
		var originTranslation = config.OriginTranslation + gridOriginNormalizedOffset.X * xDir + gridOriginNormalizedOffset.Y * yDir;
		// ReSharper disable once CompareOfFloatsByEqualityOperator Explicit comparison against 1f is correct here
		var doubleMaxHeight = maxHeightDisplacement * 2f;
		var boundingBoxOverride = new PositionedCuboid(
			MathF.Abs(xDir.X) + MathF.Abs(yDir.X) + doubleMaxHeight * MathF.Abs(upDir.X),
			MathF.Abs(xDir.Y) + MathF.Abs(yDir.Y) + doubleMaxHeight * MathF.Abs(upDir.Y),
			MathF.Abs(xDir.Z) + MathF.Abs(yDir.Z) + doubleMaxHeight * MathF.Abs(upDir.Z),
			(xDir * 0.5f + yDir * 0.5f).AsLocation() - originTranslation
		);
		var mesh = CreateMesh(
			vertexBuffer.Span, 
			triangleBuffer.Span, 
			config with {
				AllowsPerInstanceVertexMutation = true,
				OriginTranslation = originTranslation,
				BoundingBoxOverride = boundingBoxOverride
			}
		);
		return new MutableGridMesh(mesh, gridDimensions, xDir, yDir, upDir, gridOrigin);
	}
	#endregion

	#region Vertices
	/// <summary>
	/// Obtains a scratch buffer of vertices for assembling mesh data, returned to a pool when the lease is disposed.
	/// </summary>
	/// <param name="vertexCount">How many vertices the buffer must hold.</param>
	protected ScopedSpanLease<MeshVertex> GetPooledVertexBuffer(int vertexCount);
	/// <summary>
	/// Obtains a scratch buffer of triangles for assembling mesh data, returned to a pool when the lease is disposed.
	/// </summary>
	/// <param name="triangleCount">How many triangles the buffer must hold.</param>
	protected ScopedSpanLease<VertexTriangle> GetPooledTriangleBuffer(int triangleCount);
	
	/// <summary>
	/// Creates a buffer of vertices and indices that can be rewritten at any time, from which meshes can be carved out.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Unlike an ordinary mesh, a dynamic buffer's contents are expected to change; it is what geometry generated afresh each
	/// frame is written in to. Meshes created from it are views on to a range of its indices rather than copies of its data.
	/// </para>
	/// <para>
	/// The buffer grows as needed, but growing it is not free, so give capacities close to what will actually be used.
	/// </para>
	/// </remarks>
	/// <param name="initialVertexCapacity">How many vertices the buffer should initially hold.</param>
	/// <param name="initialIndexCapacity">How many indices the buffer should initially hold.</param>
	/// <param name="name">The name to give the buffer. May be left empty.</param>
	DynamicVertexBuffer CreateDynamicVertexBuffer(int initialVertexCapacity, int initialIndexCapacity, ReadOnlySpan<char> name = default);

	/// <summary>
	/// Creates a mesh directly from vertices and triangles.
	/// </summary>
	/// <remarks>
	/// This is the most direct way to build a mesh, and the one to use for geometry produced by your own code rather than
	/// described as a shape or a set of polygons.
	/// </remarks>
	/// <param name="vertices">The mesh's vertices.</param>
	/// <param name="triangles">The triangles joining those vertices, each given as three indices in to <paramref name="vertices"/>.
	/// Each triangle's indices must be in anticlockwise order as seen from its front face.</param>
	/// <param name="name">The name to give the mesh. May be left empty.</param>
	Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, ReadOnlySpan<char> name = default) => CreateMesh(vertices, triangles, new MeshCreationConfig { Name = name });
	/// <summary>
	/// Creates a mesh directly from vertices and triangles, using the given config.
	/// </summary>
	/// <param name="vertices">The mesh's vertices.</param>
	/// <param name="triangles">The triangles joining those vertices, each given as three indices in to <paramref name="vertices"/>. Each triangle's indices must be in anticlockwise order as seen from its front face.</param>
	/// <param name="config">Controls how the mesh is created.</param>
	Mesh CreateMesh(ReadOnlySpan<MeshVertex> vertices, ReadOnlySpan<VertexTriangle> triangles, in MeshCreationConfig config);

	/// <summary>
	/// Creates a skeletal mesh, whose vertices are moved by a tree of joints.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The vertices given here are the mesh's <i>bind pose</i>: the shape it takes before any animation is applied.
	/// </para>
	/// <para>
	/// Animations are attached afterwards with <see cref="AttachAnimation"/>, which means the mesh's bounding box is derived
	/// from the bind pose alone and does not know about poses that move vertices further out. Where an animation does that,
	/// supply a large enough box through the creation config.
	/// </para>
	/// <para>
	/// Node names can be set afterwards with <see cref="SetSkeletonNodeName"/>.
	/// </para>
	/// </remarks>
	/// <param name="vertices">The mesh's vertices.</param>
	/// <param name="triangles">The triangles joining those vertices, each given as three indices in to <paramref name="vertices"/>.
	/// Each triangle's indices must be in anticlockwise order as seen from its front face.</param>
	/// <param name="skeletalNodes">The skeleton's joints, in the order the vertices' bone indices refer to them. Must hold no more than <see cref="MaxSkeletalBoneCount"/> bones.</param>
	/// <param name="name">The name to give the mesh. May be left empty.</param>
	Mesh CreateMesh(ReadOnlySpan<MeshVertexSkeletal> vertices, ReadOnlySpan<VertexTriangle> triangles, ReadOnlySpan<SkeletalAnimationNode> skeletalNodes, ReadOnlySpan<char> name = default) {
		return CreateMesh(
			vertices,
			triangles,
			skeletalNodes,
			new MeshCreationConfig {
				Name = name
			}
		);
	}
	/// <summary>
	/// Creates a skeletal mesh, whose vertices are moved by a tree of joints, using the given config.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The vertices given here are the mesh's <i>bind pose</i>: the shape it takes before any animation is applied.
	/// </para>
	/// <para>
	/// Animations are attached afterwards with <see cref="AttachAnimation"/>, which means the mesh's bounding box is derived
	/// from the bind pose alone and does not know about poses that move vertices further out. Where an animation does that,
	/// supply a large enough box through the creation config.
	/// </para>
	/// <para>
	/// Node names can be set afterwards with <see cref="SetSkeletonNodeName"/>.
	/// </para>
	/// </remarks>
	/// <param name="vertices">The mesh's vertices.</param>
	/// <param name="triangles">The triangles joining those vertices, each given as three indices in to <paramref name="vertices"/>.
	/// Each triangle's indices must be in anticlockwise order as seen from its front face.</param>
	/// <param name="skeletalNodes">The skeleton's joints, in the order the vertices' bone indices refer to them. Must hold no more than <see cref="MaxSkeletalBoneCount"/> bones.</param>
	/// <param name="config">Controls how the mesh is created.</param>
	Mesh CreateMesh(ReadOnlySpan<MeshVertexSkeletal> vertices, ReadOnlySpan<VertexTriangle> triangles, ReadOnlySpan<SkeletalAnimationNode> skeletalNodes, in MeshCreationConfig config);
	#endregion

	#region Nodes & Animations
	/// <summary>
	/// Gives one of a skeletal mesh's joints a name, so that it can be looked up by name later.
	/// </summary>
	/// <remarks>
	/// Naming the joints you care about is what allows a particular one (a hand, say) to be found and its position read back
	/// after an animation has been applied.
	/// </remarks>
	/// <param name="mesh">The skeletal mesh whose joint is being named.</param>
	/// <param name="nodeIndex">Which joint to name, as an index in to the skeleton's node list.</param>
	/// <param name="name">The name to give the joint.</param>
	void SetSkeletonNodeName(
		Mesh mesh,
		int nodeIndex,
		ReadOnlySpan<char> name
	);
	
	/// <summary>
	/// Attaches an animation to a skeletal mesh previously created through this builder.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The keyframes are supplied as three flat lists covering every joint the animation touches, with one mutation descriptor
	/// per joint saying which stretch of each list belongs to it.
	/// </para>
	/// <para>
	/// Keyframes within each run must be ordered by time, ascending. The three lists need not have the same length or the same
	/// time points as one another.
	/// </para>
	/// </remarks>
	/// <param name="mesh">The skeletal mesh to attach the animation to.</param>
	/// <param name="scalingKeyframes">Every scaling keyframe in the animation. Must hold at least one entry.</param>
	/// <param name="rotationKeyframes">Every rotation keyframe in the animation. Must hold at least one entry.</param>
	/// <param name="translationKeyframes">Every translation keyframe in the animation. Must hold at least one entry.</param>
	/// <param name="boneMutations">Which stretch of each keyframe list drives which joint.</param>
	/// <param name="defaultCompletionTimeSeconds">How long the animation takes to play from start to finish at its authored speed, in seconds.</param>
	/// <param name="name">The name to give the animation. Must be unique among the mesh's animations.</param>
	MeshAnimation AttachAnimation(
		Mesh mesh, 
		ReadOnlySpan<SkeletalAnimationScalingKeyframe> scalingKeyframes, 
		ReadOnlySpan<SkeletalAnimationRotationKeyframe> rotationKeyframes, 
		ReadOnlySpan<SkeletalAnimationTranslationKeyframe> translationKeyframes, 
		ReadOnlySpan<SkeletalAnimationNodeMutationDescriptor> boneMutations, 
		float defaultCompletionTimeSeconds, 
		ReadOnlySpan<char> name
	);
	#endregion
}