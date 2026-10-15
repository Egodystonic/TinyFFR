using System.Numerics;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Threading;
using Egodystonic.TinyFFR.World;

namespace EquationPlotter;

sealed class ParametricSurfacePlot : IPlot {
	const int GradientHeight = 256;

	readonly Scene _scene;
	readonly PlotDomain _domain;
	readonly int _resolution;
	readonly Vector3[] _positions;
	readonly DynamicVertexBuffer _buffer;
	readonly Mesh _mesh;
	readonly GridTexture _gradient;
	readonly ModelInstance _instance;
	readonly IResourceAllocator _allocator;
	readonly Vector3[] _rowMinimums;
	readonly Vector3[] _rowMaximums;
	CompiledExpression[]? _currentExpressions;
	float _currentTime;
	ColorMapKind _gradientColorMap;

	public string Description { get; }
	public float ColorRangeMin => _domain.YMin;
	public float ColorRangeMax => _domain.YMax;

	public ParametricSurfacePlot(ILocalTinyFfrFactory factory, Scene scene, PlotDomain domain) {
		_scene = scene;
		_domain = domain;
		_resolution = domain.ParametricResolution;
		var vertexCount = _resolution * _resolution;
		var quadCount = (_resolution - 1) * (_resolution - 1);
		_positions = new Vector3[vertexCount];
		_allocator = factory.ResourceAllocator;
		_rowMinimums = new Vector3[_resolution];
		_rowMaximums = new Vector3[_resolution];
		_buffer = factory.MeshBuilder.CreateDynamicVertexBuffer(vertexCount * 2, quadCount * 4);
		WriteTriangles();
		_mesh = _buffer.CreateMesh();
		_gradient = new GridTexture(factory, new XYPair<int>(2, GradientHeight));
		_gradientColorMap = (ColorMapKind) (-1);
		_instance = factory.ObjectBuilder.CreateModelInstance(_mesh, _gradient.Material);
		scene.Add(_instance);
		Description = $"Parametric surface: {_resolution} x {_resolution} vertices";
	}

	unsafe void WriteTriangles() {
		_buffer.WriteTrianglesParallelized(&WriteTriangleBlock, _resolution, recalculateBoundingBoxOnLeaseDispose: false, overwriteChildMeshBoundingBoxes: false);
	}

	static void WriteTriangleBlock(Span<VertexTriangle> block, int startIndex, int resolution) {
		var backOffset = resolution * resolution;
		var quadsPerRow = resolution - 1;
		for (var i = 0; i < block.Length; ++i) {
			var triangleIndex = startIndex + i;
			var quad = triangleIndex >> 2;
			var u = quad / quadsPerRow;
			var v = quad % quadsPerRow;
			var a = u * resolution + v;
			var b = (u + 1) * resolution + v;
			var c = u * resolution + v + 1;
			var d = (u + 1) * resolution + v + 1;
			block[i] = (triangleIndex & 3) switch {
				0 => new VertexTriangle(a, b, c),
				1 => new VertexTriangle(b, d, c),
				2 => new VertexTriangle(backOffset + a, backOffset + c, backOffset + b),
				_ => new VertexTriangle(backOffset + b, backOffset + c, backOffset + d)
			};
		}
	}

	public unsafe void Update(float time, CompiledExpression[] expressions) {
		UpdateGradient();
		_currentExpressions = expressions;
		_currentTime = time;
		ParallelizationProvider.Execute(_allocator.ParallelizingFacade, _resolution, &EvaluatePositionRows, this);

		var max = Vector3.Max(Vector3.Zero, Vector3.Zero);
		var min = max;
		for (var u = 0; u < _resolution; ++u) {
			min = Vector3.Min(min, _rowMinimums[u]);
			max = Vector3.Max(max, _rowMaximums[u]);
		}

		_buffer.WriteVerticesParallelized(&WriteVertexBlock, this, recalculateBoundingBoxOnLeaseDispose: false, overwriteChildMeshBoundingBoxes: true);
		_buffer.SetBoundingBox(PositionedCuboid.FromHalfDimensions((max.X - min.X) * 0.5f, (max.Y - min.Y) * 0.5f, (max.Z - min.Z) * 0.5f, Location.FromVector3((min + max) * 0.5f)), overwriteChildMeshBoundingBoxes: true);
	}

	static void EvaluatePositionRows(int firstU, int count, ParametricSurfacePlot plot) {
		var expressions = plot._currentExpressions!;
		var domain = plot._domain;
		var resolution = plot._resolution;
		Span<float> variables = stackalloc float[3];
		variables[2] = plot._currentTime;
		for (var u = firstU; u < firstU + count; ++u) {
			variables[0] = domain.FromNormalizedU(u / (resolution - 1f) - 0.5f);
			var min = new Vector3(Single.PositiveInfinity);
			var max = new Vector3(Single.NegativeInfinity);
			for (var v = 0; v < resolution; ++v) {
				variables[1] = domain.FromNormalizedV(v / (resolution - 1f) - 0.5f);
				var position = new Vector3(
					domain.ToWorldX(SafeEvaluate(expressions[0], variables)),
					domain.ToWorldY(SafeEvaluate(expressions[1], variables)),
					domain.ToWorldZ(SafeEvaluate(expressions[2], variables))
				);
				plot._positions[u * resolution + v] = position;
				min = Vector3.Min(min, position);
				max = Vector3.Max(max, position);
			}
			plot._rowMinimums[u] = min;
			plot._rowMaximums[u] = max;
		}
	}

	static void WriteVertexBlock(Span<MeshVertex> block, int startIndex, ParametricSurfacePlot plot) {
		var positions = plot._positions;
		var resolution = plot._resolution;
		var backOffset = resolution * resolution;
		for (var i = 0; i < block.Length; ++i) {
			var bufferIndex = startIndex + i;
			var isBack = bufferIndex >= backOffset;
			var index = isBack ? bufferIndex - backOffset : bufferIndex;
			var u = index / resolution;
			var v = index % resolution;
			var position = positions[index];
			var alongU = Vector3.Normalize(positions[Math.Min(u + 1, resolution - 1) * resolution + v] - positions[Math.Max(u - 1, 0) * resolution + v]);
			var alongV = Vector3.Normalize(positions[u * resolution + Math.Min(v + 1, resolution - 1)] - positions[u * resolution + Math.Max(v - 1, 0)]);
			var normal = Vector3.Normalize(Vector3.Cross(alongU, alongV));
			if (!Single.IsFinite(normal.X) || !Single.IsFinite(alongU.X)) {
				normal = Vector3.UnitY;
				alongU = Vector3.UnitX;
			}
			var bitangent = Vector3.Cross(normal, alongU);
			if (isBack) {
				bitangent = -bitangent;
				normal = -normal;
			}
			var colorCoord = new XYPair<float>(0.5f, Math.Clamp(position.Y / (PlotDomain.WorldHalfSize * 2f) + 0.5f, 0f, 1f));
			block[i] = new MeshVertex(Location.FromVector3(position), colorCoord, MeshVertex.CalculateTangentRotation(Direction.FromVector3(alongU), Direction.FromVector3(bitangent), Direction.FromVector3(normal)));
		}
	}

	void UpdateGradient() {
		if (_gradientColorMap == _domain.ColorMap) return;
		_gradientColorMap = _domain.ColorMap;
		for (var row = 0; row < GradientHeight; ++row) {
			var texel = ColorMap.SampleTexel(_gradientColorMap, (row + 0.5f) / GradientHeight);
			_gradient.Set(new XYPair<int>(0, row), texel);
			_gradient.Set(new XYPair<int>(1, row), texel);
		}
		_gradient.Upload();
	}

	static float SafeEvaluate(CompiledExpression expression, ReadOnlySpan<float> variables) {
		var value = expression.Evaluate(variables);
		return Single.IsFinite(value) ? value : 0f;
	}

	public void Dispose() {
		_scene.Remove(_instance);
		_instance.Dispose();
		_mesh.Dispose();
		_buffer.Dispose();
		_gradient.Dispose();
	}
}
