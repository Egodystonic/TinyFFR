using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

namespace EquationPlotter;

sealed class PlotController : IDisposable {
	readonly ILocalTinyFfrFactory _factory;
	readonly Scene _scene;
	IPlot? _plot;
	CompiledExpression[] _expressions = [];

	public PlotDomain Domain { get; } = new();
	public PlotType Type { get; private set; } = PlotType.Surface;
	public IPlot? Plot => _plot;

	public PlotController(ILocalTinyFfrFactory factory, Scene scene) {
		_factory = factory;
		_scene = scene;
	}

	public static string[] GetVariableNames(PlotType type) => type switch {
		PlotType.Surface => ["x", "z", "t"],
		PlotType.ParametricSurface => ["u", "v", "t"],
		_ => ["x", "y", "z", "t"]
	};

	public static string[] GetEquationLabels(PlotType type) => type switch {
		PlotType.VectorField => ["Fx", "Fy", "Fz"],
		PlotType.ParametricSurface => ["x", "y", "z"],
		PlotType.ScalarSlice => ["w"],
		_ => ["y"]
	};

	public void Apply(PlotType type, string[] equations) {
		var variableNames = GetVariableNames(type);
		var compiled = new CompiledExpression[equations.Length];
		for (var i = 0; i < equations.Length; ++i) compiled[i] = ExpressionParser.Compile(equations[i], variableNames);

		_expressions = compiled;
		if (type != Type || _plot == null) {
			Type = type;
			Rebuild();
		}
	}

	public void Rebuild() {
		_plot?.Dispose();
		_plot = Type switch {
			PlotType.VectorField => new VectorFieldPlot(_factory, _scene, Domain),
			PlotType.ParametricSurface => new ParametricSurfacePlot(_factory, _scene, Domain),
			PlotType.ScalarSlice => new ScalarSlicePlot(_factory, _scene, Domain),
			_ => new SurfacePlot(_factory, _scene, Domain)
		};
	}

	public void Update(float time) {
		if (_plot == null || _expressions.Length == 0) return;
		_plot.Update(time, _expressions);
	}

	public void Dispose() => _plot?.Dispose();
}
