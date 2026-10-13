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
		Description = $"Surface: {dimensions.X} x {dimensions.Y} vertices";
	}

	public void Update(float time, CompiledExpression[] expressions) {
		var expression = expressions[0];
		Span<float> variables = stackalloc float[3];
		variables[2] = time;

		using (var lease = _grid.BorrowVerticesSpan(permitLateralDisplacement: false, recalculateNormals: true)) {
			var vertices = lease.Span;
			for (var i = 0; i < vertices.Length; ++i) {
				var normalized = _grid.GetVertexCoordinateNormalized(i);
				variables[0] = _domain.FromNormalizedX(normalized.X);
				variables[1] = _domain.FromNormalizedZ(normalized.Y);
				var value = expression.Evaluate(variables);
				var height = Single.IsFinite(value) ? Math.Clamp(_domain.ToWorldY(value), -PlotDomain.WorldHalfSize, PlotDomain.WorldHalfSize) : 0f;
				vertices[i] = new MutableGridVertex(height);
				_colors.Set(_grid.GetVertexCoordinate(i), ColorMap.SampleTexel(_domain.ColorMap, height / (PlotDomain.WorldHalfSize * 2f) + 0.5f));
			}
		}
		_colors.Upload();
	}

	public void Dispose() {
		_scene.Remove(_grid);
		_grid.Dispose();
		_mesh.Dispose();
		_colors.Dispose();
	}
}
