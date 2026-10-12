using System.Numerics;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
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
		_buffer = factory.MeshBuilder.CreateDynamicVertexBuffer(vertexCount * 2, quadCount * 4);
		WriteTriangles();
		_mesh = _buffer.CreateMesh();
		_gradient = new GridTexture(factory, new XYPair<int>(2, GradientHeight));
		_gradientColorMap = (ColorMapKind) (-1);
		_instance = factory.ObjectBuilder.CreateModelInstance(_mesh, _gradient.Material);
		scene.Add(_instance);
		Description = $"Parametric surface: {_resolution} x {_resolution} vertices";
	}

	void WriteTriangles() {
		using var lease = _buffer.BorrowTrianglesSpan(recalculateBoundingBoxOnLeaseDispose: false, overwriteChildMeshBoundingBoxes: false);
		var triangles = lease.Span;
		var backOffset = _resolution * _resolution;
		var t = 0;
		for (var u = 0; u < _resolution - 1; ++u) {
			for (var v = 0; v < _resolution - 1; ++v) {
				var a = u * _resolution + v;
				var b = (u + 1) * _resolution + v;
				var c = u * _resolution + v + 1;
				var d = (u + 1) * _resolution + v + 1;
				triangles[t++] = new VertexTriangle(a, b, c);
				triangles[t++] = new VertexTriangle(b, d, c);
				triangles[t++] = new VertexTriangle(backOffset + a, backOffset + c, backOffset + b);
				triangles[t++] = new VertexTriangle(backOffset + b, backOffset + c, backOffset + d);
			}
		}
	}

	public void Update(float time, CompiledExpression[] expressions) {
		UpdateGradient();
		Span<float> variables = stackalloc float[3];
		variables[2] = time;
		var max = Vector3.Max(Vector3.Zero, Vector3.Zero);
		var min = max;
		for (var u = 0; u < _resolution; ++u) {
			variables[0] = _domain.FromNormalizedU(u / (_resolution - 1f) - 0.5f);
			for (var v = 0; v < _resolution; ++v) {
				variables[1] = _domain.FromNormalizedV(v / (_resolution - 1f) - 0.5f);
				var position = new Vector3(
					_domain.ToWorldX(SafeEvaluate(expressions[0], variables)),
					_domain.ToWorldY(SafeEvaluate(expressions[1], variables)),
					_domain.ToWorldZ(SafeEvaluate(expressions[2], variables))
				);
				_positions[u * _resolution + v] = position;
				min = Vector3.Min(min, position);
				max = Vector3.Max(max, position);
			}
		}

		using (var lease = _buffer.BorrowVerticesSpan(recalculateBoundingBoxOnLeaseDispose: false, overwriteChildMeshBoundingBoxes: true)) {
			var vertices = lease.Span;
			var backOffset = _resolution * _resolution;
			for (var u = 0; u < _resolution; ++u) {
				for (var v = 0; v < _resolution; ++v) {
					var index = u * _resolution + v;
					var position = _positions[index];
					var alongU = Vector3.Normalize(_positions[Math.Min(u + 1, _resolution - 1) * _resolution + v] - _positions[Math.Max(u - 1, 0) * _resolution + v]);
					var alongV = Vector3.Normalize(_positions[u * _resolution + Math.Min(v + 1, _resolution - 1)] - _positions[u * _resolution + Math.Max(v - 1, 0)]);
					var normal = Vector3.Normalize(Vector3.Cross(alongU, alongV));
					if (!Single.IsFinite(normal.X) || !Single.IsFinite(alongU.X)) {
						normal = Vector3.UnitY;
						alongU = Vector3.UnitX;
					}
					var bitangent = Vector3.Cross(normal, alongU);
					var colorCoord = new XYPair<float>(0.5f, Math.Clamp(position.Y / (PlotDomain.WorldHalfSize * 2f) + 0.5f, 0f, 1f));
					var location = Location.FromVector3(position);
					vertices[index] = new MeshVertex(location, colorCoord, MeshVertex.CalculateTangentRotation(Direction.FromVector3(alongU), Direction.FromVector3(bitangent), Direction.FromVector3(normal)));
					vertices[backOffset + index] = new MeshVertex(location, colorCoord, MeshVertex.CalculateTangentRotation(Direction.FromVector3(alongU), Direction.FromVector3(-bitangent), Direction.FromVector3(-normal)));
				}
			}
		}
		_buffer.SetBoundingBox(PositionedCuboid.FromHalfDimensions((max.X - min.X) * 0.5f, (max.Y - min.Y) * 0.5f, (max.Z - min.Z) * 0.5f, Location.FromVector3((min + max) * 0.5f)), overwriteChildMeshBoundingBoxes: true);
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
