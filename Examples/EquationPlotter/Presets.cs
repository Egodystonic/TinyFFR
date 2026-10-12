namespace EquationPlotter;

sealed record Preset(string Name, PlotType Type, string[] Equations, Action<PlotDomain>? ConfigureDomain = null);

static class Presets {
	public static readonly Preset[] All = [
		new("Ripples", PlotType.Surface, ["sin(sqrt(x^2 + z^2) * 3 - t * 3) / (1 + 0.3 sqrt(x^2 + z^2))"]),
		new("Breathing saddle", PlotType.Surface, ["(x^2 - z^2) / 6 * cos(t)"]),
		new("Interfering waves", PlotType.Surface, ["0.6 sin(x * 2 + t * 2) + 0.6 cos(z * 1.5 - t * 1.3)"]),
		new("Wandering hill", PlotType.Surface, ["1.4 exp(-((x - 2 cos(t))^2 + (z - 2 sin(t))^2))"]),

		new("Rotating vortex", PlotType.VectorField, ["-z + 0.3 sin(t) x", "0.4 sin(t + sqrt(x^2 + z^2))", "x + 0.3 sin(t) z"]),
		new("Pulsing source", PlotType.VectorField, ["x * cos(t)", "y * cos(t)", "z * cos(t)"]),
		new("Twisting shear", PlotType.VectorField, ["sin(y + t)", "cos(z + t)", "sin(x + t)"]),
		new("Orbiting dipole", PlotType.VectorField, ["(x - cos(t)) / ((x - cos(t))^2 + y^2 + (z - sin(t))^2 + 0.2)^1.5 - (x + cos(t)) / ((x + cos(t))^2 + y^2 + (z + sin(t))^2 + 0.2)^1.5", "y / ((x - cos(t))^2 + y^2 + (z - sin(t))^2 + 0.2)^1.5 - y / ((x + cos(t))^2 + y^2 + (z + sin(t))^2 + 0.2)^1.5", "(z - sin(t)) / ((x - cos(t))^2 + y^2 + (z - sin(t))^2 + 0.2)^1.5 - (z + sin(t)) / ((x + cos(t))^2 + y^2 + (z + sin(t))^2 + 0.2)^1.5"], d => { d.XMin = -2.5f; d.XMax = 2.5f; d.YMin = -2.5f; d.YMax = 2.5f; d.ZMin = -2.5f; d.ZMax = 2.5f; }),

		new("Breathing torus", PlotType.ParametricSurface, ["(2 + (0.6 + 0.2 sin(t * 2 + u * 3)) cos(v)) cos(u)", "(0.6 + 0.2 sin(t * 2 + u * 3)) sin(v)", "(2 + (0.6 + 0.2 sin(t * 2 + u * 3)) cos(v)) sin(u)"], d => { d.YMin = -3f; d.YMax = 3f; d.UMin = 0f; d.UMax = MathF.Tau; d.VMin = 0f; d.VMax = MathF.Tau; }),
		new("Twisting Möbius strip", PlotType.ParametricSurface, ["(2 + v cos((u + t) / 2)) cos(u)", "v sin((u + t) / 2)", "(2 + v cos((u + t) / 2)) sin(u)"], d => { d.YMin = -3f; d.YMax = 3f; d.UMin = 0f; d.UMax = MathF.Tau; d.VMin = -0.8f; d.VMax = 0.8f; }),
		new("Wobbling sphere", PlotType.ParametricSurface, ["(2 + 0.25 sin(4 u + t * 3) sin(3 v)) sin(v) cos(u)", "(2 + 0.25 sin(4 u + t * 3) sin(3 v)) cos(v)", "(2 + 0.25 sin(4 u + t * 3) sin(3 v)) sin(v) sin(u)"], d => { d.YMin = -3f; d.YMax = 3f; d.UMin = 0f; d.UMax = MathF.Tau; d.VMin = 0f; d.VMax = MathF.PI; }),
		new("Helicoid", PlotType.ParametricSurface, ["v cos(u + t)", "u / 2.5", "v sin(u + t)"], d => { d.YMin = -3f; d.YMax = 3f; d.UMin = -6f; d.UMax = 6f; d.VMin = -2f; d.VMax = 2f; }),

		new("Standing wave", PlotType.ScalarSlice, ["sin(x * 2) * sin(y * 2) * sin(z * 2) * cos(t * 2)"], d => d.SlicePosition = 0.7f),
		new("Orbiting charge", PlotType.ScalarSlice, ["1 / (0.3 + (x - 2 cos(t))^2 + y^2 + (z - 2 sin(t))^2)"]),
		new("Gyroid", PlotType.ScalarSlice, ["sin(x * 2 + t) cos(y * 2) + sin(y * 2) cos(z * 2 + t) + sin(z * 2) cos(x * 2)"])
	];
}
