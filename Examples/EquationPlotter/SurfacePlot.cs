using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

namespace EquationPlotter;

sealed class SurfacePlot : IPlot {
	readonly Scene _scene;
	readonly PlotDomain _domain;
	readonly MutableGridMesh _mesh;
	readonly GridTexture _colors;
	readonly MutableGridInstance _grid;
	readonly XYPair<float>[] _normalizedCoords;
	CompiledExpression? _currentExpression;
	float _currentTime;

	public string Description { get; }
	public float ColorRangeMin => _domain.YMin;
	public float ColorRangeMax => _domain.YMax;

	public SurfacePlot(ILocalTinyFfrFactory factory, Scene scene, PlotDomain domain) {
		_scene = scene;
		_domain = domain;
		var dimensions = new XYPair<int>(domain.SurfaceResolution, domain.SurfaceResolution);
		_mesh = factory.MeshBuilder.CreateMutableGrid(dimensions, maxHeightDisplacement: PlotDomain.WorldHalfSize, twoSided: true, xDir: Direction.Left, yDir: Direction.Forward, upDir: Direction.Up);
		_colors = new GridTexture(factory, dimensions);
		_grid = factory.ObjectBuilder.CreateMutableGridInstance(_mesh, _colors.Material, Location.Origin, new XYPair<float>(PlotDomain.WorldHalfSize * 2f, PlotDomain.WorldHalfSize * 2f));
		scene.Add(_grid);
		_normalizedCoords = new XYPair<float>[dimensions.Area];
		for (var i = 0; i < _normalizedCoords.Length; ++i) _normalizedCoords[i] = _grid.GetVertexCoordinateNormalized(i);
		Description = $"Surface: {dimensions.X} x {dimensions.Y} vertices";
	}

	public unsafe void Update(float time, CompiledExpression[] expressions) {
		_currentExpression = expressions[0];
		_currentTime = time;
		_grid.WriteVerticesParallelized(&WriteVertexBlock, this, permitLateralDisplacement: false, recalculateNormals: true);
		_colors.Upload();
	}

	static void WriteVertexBlock(Span<MutableGridVertex> block, XYPair<int> offset, XYPair<int> dimensions, SurfacePlot plot) {
		var expression = plot._currentExpression!;
		var domain = plot._domain;
		Span<float> variables = stackalloc float[3];
		variables[2] = plot._currentTime;
		var firstIndex = offset.Y * dimensions.X;
		for (var i = 0; i < block.Length; ++i) {
			var index = firstIndex + i;
			var normalized = plot._normalizedCoords[index];
			variables[0] = domain.FromNormalizedX(normalized.X);
			variables[1] = domain.FromNormalizedZ(normalized.Y);
			var value = expression.Evaluate(variables);
			var height = Single.IsFinite(value) ? Math.Clamp(domain.ToWorldY(value), -PlotDomain.WorldHalfSize, PlotDomain.WorldHalfSize) : 0f;
			block[i] = new MutableGridVertex(height);
			plot._colors.Set(new XYPair<int>(index % dimensions.X, index / dimensions.X), ColorMap.SampleTexel(domain.ColorMap, height / (PlotDomain.WorldHalfSize * 2f) + 0.5f));
		}
	}

	public void Dispose() {
		_scene.Remove(_grid);
		_grid.Dispose();
		_mesh.Dispose();
		_colors.Dispose();
	}
}
