using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

namespace EquationPlotter;

sealed class ScalarSlicePlot : IPlot {
	readonly Scene _scene;
	readonly PlotDomain _domain;
	readonly SliceAxis _axis;
	readonly MutableGridMesh _mesh;
	readonly GridTexture _colors;
	readonly MutableGridInstance _grid;
	float _rangeMin = -1f;
	float _rangeMax = 1f;

	public string Description { get; }
	public float ColorRangeMin => _rangeMin;
	public float ColorRangeMax => _rangeMax;

	public ScalarSlicePlot(ILocalTinyFfrFactory factory, Scene scene, PlotDomain domain) {
		_scene = scene;
		_domain = domain;
		_axis = domain.SliceAxis;
		var dimensions = new XYPair<int>(domain.SliceResolution, domain.SliceResolution);
		var (xDirection, yDirection, upDirection) = _axis switch {
			SliceAxis.FacingZ => (Direction.Left, Direction.Up, Direction.Backward),
			SliceAxis.FacingX => (Direction.Forward, Direction.Up, Direction.Left),
			_ => (Direction.Left, Direction.Forward, Direction.Up)
		};
		_mesh = factory.MeshBuilder.CreateMutableGrid(dimensions, maxHeightDisplacement: 0.01f, twoSided: true, xDir: xDirection, yDir: yDirection, upDir: upDirection);
		_colors = new GridTexture(factory, dimensions, ignoresLighting: true);
		_grid = factory.ObjectBuilder.CreateMutableGridInstance(_mesh, _colors.Material, GetSliceCentre(), new XYPair<float>(PlotDomain.WorldHalfSize * 2f, PlotDomain.WorldHalfSize * 2f));
		scene.Add(_grid);
		Description = $"Scalar field slice: {dimensions.X} x {dimensions.Y} samples";
	}

	Location GetSliceCentre() {
		return _axis switch {
			SliceAxis.FacingZ => new Location(0f, 0f, _domain.ToWorldZ(_domain.SlicePosition)),
			SliceAxis.FacingX => new Location(_domain.ToWorldX(_domain.SlicePosition), 0f, 0f),
			_ => new Location(0f, _domain.ToWorldY(_domain.SlicePosition), 0f)
		};
	}

	public void Update(float time, CompiledExpression[] expressions) {
		_grid.SetPosition(GetSliceCentre());
		var expression = expressions[0];
		Span<float> variables = stackalloc float[4];
		variables[3] = time;
		var dimensions = _colors.Dimensions;
		var sampleCount = dimensions.Area;
		var minimum = Single.PositiveInfinity;
		var maximum = Single.NegativeInfinity;
		var range = MathF.Max(_rangeMax - _rangeMin, 0.0001f);

		for (var i = 0; i < sampleCount; ++i) {
			var normalized = _grid.GetVertexCoordinateNormalized(i);
			switch (_axis) {
				case SliceAxis.FacingZ:
					variables[0] = _domain.FromNormalizedX(normalized.X);
					variables[1] = _domain.FromNormalizedY(normalized.Y);
					variables[2] = _domain.SlicePosition;
					break;
				case SliceAxis.FacingX:
					variables[0] = _domain.SlicePosition;
					variables[1] = _domain.FromNormalizedY(normalized.Y);
					variables[2] = _domain.FromNormalizedZ(normalized.X);
					break;
				default:
					variables[0] = _domain.FromNormalizedX(normalized.X);
					variables[1] = _domain.SlicePosition;
					variables[2] = _domain.FromNormalizedZ(normalized.Y);
					break;
			}

			var value = expression.Evaluate(variables);
			if (!Single.IsFinite(value)) value = 0f;
			minimum = MathF.Min(minimum, value);
			maximum = MathF.Max(maximum, value);
			_colors.Set(_grid.GetVertexCoordinate(i), ColorMap.SampleTexel(_domain.ColorMap, (value - _rangeMin) / range));
		}
		_colors.Upload();

		_rangeMin = minimum;
		_rangeMax = MathF.Max(maximum, minimum + 0.0001f);
	}

	public void Dispose() {
		_scene.Remove(_grid);
		_grid.Dispose();
		_mesh.Dispose();
		_colors.Dispose();
	}
}
