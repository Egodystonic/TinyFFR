using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Threading;
using Egodystonic.TinyFFR.World;

namespace EquationPlotter;

sealed class ScalarSlicePlot : IPlot {
	readonly Scene _scene;
	readonly PlotDomain _domain;
	readonly SliceAxis _axis;
	readonly MutableGridMesh _mesh;
	readonly GridTexture _colors;
	readonly MutableGridInstance _grid;
	readonly IResourceAllocator _allocator;
	readonly XYPair<float>[] _normalizedCoords;
	readonly float[] _rowMinimums;
	readonly float[] _rowMaximums;
	CompiledExpression? _currentExpression;
	float _currentTime;
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
		_allocator = factory.ResourceAllocator;
		_normalizedCoords = new XYPair<float>[dimensions.Area];
		for (var i = 0; i < _normalizedCoords.Length; ++i) _normalizedCoords[i] = _grid.GetVertexCoordinateNormalized(i);
		_rowMinimums = new float[dimensions.Y];
		_rowMaximums = new float[dimensions.Y];
		Description = $"Scalar field slice: {dimensions.X} x {dimensions.Y} samples";
	}

	Location GetSliceCentre() {
		return _axis switch {
			SliceAxis.FacingZ => new Location(0f, 0f, _domain.ToWorldZ(_domain.SlicePosition)),
			SliceAxis.FacingX => new Location(_domain.ToWorldX(_domain.SlicePosition), 0f, 0f),
			_ => new Location(0f, _domain.ToWorldY(_domain.SlicePosition), 0f)
		};
	}

	public unsafe void Update(float time, CompiledExpression[] expressions) {
		_grid.SetPosition(GetSliceCentre());
		_currentExpression = expressions[0];
		_currentTime = time;
		ParallelizationProvider.ExecuteRows(_allocator.ParallelizingFacade, _colors.Dimensions, &SampleRowBand, this);
		_colors.Upload();

		var minimum = Single.PositiveInfinity;
		var maximum = Single.NegativeInfinity;
		for (var row = 0; row < _rowMinimums.Length; ++row) {
			minimum = MathF.Min(minimum, _rowMinimums[row]);
			maximum = MathF.Max(maximum, _rowMaximums[row]);
		}
		_rangeMin = minimum;
		_rangeMax = MathF.Max(maximum, minimum + 0.0001f);
	}

	static void SampleRowBand(XYPair<int> offset, XYPair<int> dimensions, ScalarSlicePlot plot) {
		var expression = plot._currentExpression!;
		var domain = plot._domain;
		var range = MathF.Max(plot._rangeMax - plot._rangeMin, 0.0001f);
		Span<float> variables = stackalloc float[4];
		variables[3] = plot._currentTime;

		for (var y = offset.Y; y < offset.Y + dimensions.Y; ++y) {
			var minimum = Single.PositiveInfinity;
			var maximum = Single.NegativeInfinity;
			for (var x = 0; x < dimensions.X; ++x) {
				var normalized = plot._normalizedCoords[y * dimensions.X + x];
				switch (plot._axis) {
					case SliceAxis.FacingZ:
						variables[0] = domain.FromNormalizedX(normalized.X);
						variables[1] = domain.FromNormalizedY(normalized.Y);
						variables[2] = domain.SlicePosition;
						break;
					case SliceAxis.FacingX:
						variables[0] = domain.SlicePosition;
						variables[1] = domain.FromNormalizedY(normalized.Y);
						variables[2] = domain.FromNormalizedZ(normalized.X);
						break;
					default:
						variables[0] = domain.FromNormalizedX(normalized.X);
						variables[1] = domain.SlicePosition;
						variables[2] = domain.FromNormalizedZ(normalized.Y);
						break;
				}

				var value = expression.Evaluate(variables);
				if (!Single.IsFinite(value)) value = 0f;
				minimum = MathF.Min(minimum, value);
				maximum = MathF.Max(maximum, value);
				plot._colors.Set(new XYPair<int>(x, y), ColorMap.SampleTexel(domain.ColorMap, (value - plot._rangeMin) / range));
			}
			plot._rowMinimums[y] = minimum;
			plot._rowMaximums[y] = maximum;
		}
	}

	public void Dispose() {
		_scene.Remove(_grid);
		_grid.Dispose();
		_mesh.Dispose();
		_colors.Dispose();
	}
}
