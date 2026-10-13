using System.Numerics;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.World;
using Hexa.NET.ImGui;

namespace EquationPlotter;

sealed class ControlPanel {
	const ImGuiWindowFlags WindowFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings;
	static readonly Vector4 ErrorColor = new(1f, 0.45f, 0.4f, 1f);
	static readonly PlotType[] PlotTypes = Enum.GetValues<PlotType>();
	static readonly ColorMapKind[] ColorMaps = Enum.GetValues<ColorMapKind>();
	static readonly SliceAxis[] SliceAxes = Enum.GetValues<SliceAxis>();
	static readonly BackgroundStyle[] BackgroundStyles = Enum.GetValues<BackgroundStyle>();

	readonly PlotController _plots;
	readonly PlotClock _clock;
	readonly AxesGizmo _axes;
	readonly SceneSettings _settings;
	readonly Scene _scene;
	readonly string[][] _equations = new string[PlotTypes.Length][];
	PlotType _selectedType;
	string? _error;

	public ControlPanel(PlotController plots, PlotClock clock, AxesGizmo axes, SceneSettings settings, Scene scene) {
		_plots = plots;
		_clock = clock;
		_axes = axes;
		_settings = settings;
		_scene = scene;
		_settings.ApplyBackground(_scene);
		foreach (var type in PlotTypes) _equations[(int) type] = (string[]) Presets.All.First(p => p.Type == type).Equations.Clone();
		LoadPreset(Presets.All[0]);
	}

	public void Draw(ApplicationLoop loop) {
		var fontSize = ImGui.GetFontSize();
		var displaySize = ImGui.GetIO().DisplaySize;
		DrawEquationWindow(new Vector2(fontSize, fontSize), fontSize);
		DrawDomainWindow(new Vector2(displaySize.X - fontSize, fontSize), fontSize);
		DrawTimeWindow(new Vector2(fontSize, displaySize.Y - fontSize), fontSize, loop);
		DrawSceneWindow(new Vector2(displaySize.X - fontSize, displaySize.Y - fontSize), fontSize);
	}

	void DrawSceneWindow(Vector2 position, float fontSize) {
		ImGui.SetNextWindowPos(position, ImGuiCond.FirstUseEver, new Vector2(1f, 1f));
		if (ImGui.Begin("Scene", WindowFlags)) {
			ImGui.SeparatorText("Background");
			var backgroundChanged = false;
			ImGui.SetNextItemWidth(fontSize * 12f);
			if (ImGui.BeginCombo("Background", Describe(_settings.Background))) {
				foreach (var style in BackgroundStyles) {
					if (!ImGui.Selectable(Describe(style), style == _settings.Background)) continue;
					_settings.Background = style;
					backgroundChanged = true;
				}
				ImGui.EndCombo();
			}
			if (_settings.Background == BackgroundStyle.SolidColor) {
				var color = _settings.BackgroundColor;
				ImGui.SetNextItemWidth(fontSize * 12f);
				if (ImGui.ColorEdit3("Colour", ref color)) {
					_settings.BackgroundColor = color;
					backgroundChanged = true;
				}
			}
			if (backgroundChanged) _settings.ApplyBackground(_scene);

			ImGui.SeparatorText("Camera");
			var orbitSensitivity = _settings.OrbitSensitivity;
			ImGui.SetNextItemWidth(fontSize * 12f);
			if (ImGui.SliderFloat("Orbit sensitivity", ref orbitSensitivity, 0.1f, 4f)) _settings.OrbitSensitivity = orbitSensitivity;
			var zoomSensitivity = _settings.ZoomSensitivity;
			ImGui.SetNextItemWidth(fontSize * 12f);
			if (ImGui.SliderFloat("Zoom sensitivity", ref zoomSensitivity, 0.1f, 4f)) _settings.ZoomSensitivity = zoomSensitivity;
			var invertHorizontal = _settings.InvertHorizontal;
			if (ImGui.Checkbox("Invert X", ref invertHorizontal)) _settings.InvertHorizontal = invertHorizontal;
			ImGui.SameLine();
			var invertVertical = _settings.InvertVertical;
			if (ImGui.Checkbox("Invert Y", ref invertVertical)) _settings.InvertVertical = invertVertical;

			ImGui.SeparatorText("Labels");
			var labelsChanged = false;
			var labelsEnabled = _settings.LabelsEnabled;
			if (ImGui.Checkbox("Show labels", ref labelsEnabled)) {
				_settings.LabelsEnabled = labelsEnabled;
				labelsChanged = true;
			}
			if (labelsEnabled) {
				var labelColor = _settings.LabelColor;
				ImGui.SetNextItemWidth(fontSize * 12f);
				if (ImGui.ColorEdit3("Text colour", ref labelColor)) {
					_settings.LabelColor = labelColor;
					labelsChanged = true;
				}
				var outlineColor = _settings.LabelOutlineColor;
				ImGui.SetNextItemWidth(fontSize * 12f);
				if (ImGui.ColorEdit3("Outline colour", ref outlineColor)) {
					_settings.LabelOutlineColor = outlineColor;
					labelsChanged = true;
				}
				var labelSize = _settings.LabelSizeMultiplier;
				ImGui.SetNextItemWidth(fontSize * 12f);
				if (ImGui.SliderFloat("Label size", ref labelSize, 0.5f, 3f)) {
					_settings.LabelSizeMultiplier = labelSize;
					labelsChanged = true;
				}
			}
			if (labelsChanged) _axes.RebuildLabels();

			ImGui.SeparatorText("Plot box");
			var primitivesChanged = false;
			var boxEnabled = _settings.BoxEnabled;
			if (ImGui.Checkbox("Show box", ref boxEnabled)) {
				_settings.BoxEnabled = boxEnabled;
				primitivesChanged = true;
			}
			ImGui.SameLine();
			var axesEnabled = _settings.AxesEnabled;
			if (ImGui.Checkbox("Show axes", ref axesEnabled)) {
				_settings.AxesEnabled = axesEnabled;
				primitivesChanged = true;
			}
			primitivesChanged |= GridControl(0, "x", fontSize);
			primitivesChanged |= GridControl(1, "y", fontSize);
			primitivesChanged |= GridControl(2, "z", fontSize);
			if (primitivesChanged) _axes.RebuildPrimitives();
		}
		ImGui.End();
	}

	void DrawEquationWindow(Vector2 position, float fontSize) {
		ImGui.SetNextWindowPos(position, ImGuiCond.FirstUseEver);
		if (ImGui.Begin("Equation", WindowFlags)) {
			ImGui.SetNextItemWidth(fontSize * 22f);
			if (ImGui.BeginCombo("Plot type", Describe(_selectedType))) {
				foreach (var type in PlotTypes) {
					if (ImGui.Selectable(Describe(type), type == _selectedType)) {
						_selectedType = type;
						Apply();
					}
				}
				ImGui.EndCombo();
			}

			ImGui.SetNextItemWidth(fontSize * 22f);
			if (ImGui.BeginCombo("Presets", "Choose a preset...")) {
				foreach (var preset in Presets.All) {
					if (preset.Type == _selectedType && ImGui.Selectable(preset.Name)) LoadPreset(preset);
				}
				ImGui.EndCombo();
			}

			ImGui.Separator();
			var labels = PlotController.GetEquationLabels(_selectedType);
			var equations = _equations[(int) _selectedType];
			var applyRequested = false;
			for (var i = 0; i < labels.Length; ++i) {
				ImGui.AlignTextToFramePadding();
				ImGui.Text($"{labels[i],2} =");
				ImGui.SameLine();
				ImGui.SetNextItemWidth(fontSize * 30f);
				if (ImGui.InputText($"##equation{i}", ref equations[i], 512, ImGuiInputTextFlags.EnterReturnsTrue)) applyRequested = true;
			}
			if (ImGui.Button("Apply (Enter)") || applyRequested) Apply();

			ImGui.TextDisabled($"Variables: {String.Join(", ", PlotController.GetVariableNames(_selectedType))}.  Constants: pi, tau, e.");
			ImGui.TextDisabled("Functions: sin cos tan asin acos atan atan2 sinh cosh tanh sqrt abs");
			ImGui.TextDisabled("exp log floor ceil round sign min max clamp pow mod");
			if (_error != null) ImGui.TextColored(ErrorColor, _error);
		}
		ImGui.End();
	}

	void DrawDomainWindow(Vector2 position, float fontSize) {
		ImGui.SetNextWindowPos(position, ImGuiCond.FirstUseEver, new Vector2(1f, 0f));
		if (ImGui.Begin("Domain & Display", WindowFlags)) {
			var domain = _plots.Domain;
			var rangesChanged = false;
			if (_plots.Type == PlotType.ParametricSurface) {
				rangesChanged |= RangeControl("u range", domain.UMin, domain.UMax, (min, max) => { domain.UMin = min; domain.UMax = max; }, fontSize);
				rangesChanged |= RangeControl("v range", domain.VMin, domain.VMax, (min, max) => { domain.VMin = min; domain.VMax = max; }, fontSize);
				ImGui.Separator();
			}
			rangesChanged |= RangeControl("x range", domain.XMin, domain.XMax, (min, max) => { domain.XMin = min; domain.XMax = max; }, fontSize);
			rangesChanged |= RangeControl("y range", domain.YMin, domain.YMax, (min, max) => { domain.YMin = min; domain.YMax = max; }, fontSize);
			rangesChanged |= RangeControl("z range", domain.ZMin, domain.ZMax, (min, max) => { domain.ZMin = min; domain.ZMax = max; }, fontSize);
			if (rangesChanged) _axes.Rebuild(_plots.Type, domain);

			ImGui.Separator();
			switch (_plots.Type) {
				case PlotType.Surface:
					ResolutionControl("Grid size", domain.SurfaceResolution, 16, 256, value => domain.SurfaceResolution = value, fontSize);
					break;
				case PlotType.VectorField:
					ResolutionControl("Arrows per side", domain.VectorFieldResolution, 3, 14, value => domain.VectorFieldResolution = value, fontSize);
					var normalize = domain.NormalizeArrowLengths;
					if (ImGui.Checkbox("Scale arrows to the largest", ref normalize)) domain.NormalizeArrowLengths = normalize;
					if (!normalize) {
						var scale = domain.ArrowLengthScale;
						ImGui.SetNextItemWidth(fontSize * 12f);
						if (ImGui.SliderFloat("Arrow length scale", ref scale, 0.05f, 3f)) domain.ArrowLengthScale = scale;
					}
					break;
				case PlotType.ParametricSurface:
					ResolutionControl("Grid size", domain.ParametricResolution, 8, 192, value => domain.ParametricResolution = value, fontSize);
					break;
				case PlotType.ScalarSlice:
					ResolutionControl("Grid size", domain.SliceResolution, 16, 256, value => domain.SliceResolution = value, fontSize);
					ImGui.SetNextItemWidth(fontSize * 12f);
					if (ImGui.BeginCombo("Slice plane", Describe(domain.SliceAxis))) {
						foreach (var axis in SliceAxes) {
							if (!ImGui.Selectable(Describe(axis), axis == domain.SliceAxis)) continue;
							domain.SliceAxis = axis;
							domain.SlicePosition = 0f;
							_plots.Rebuild();
						}
						ImGui.EndCombo();
					}
					var (sliceMin, sliceMax) = domain.SliceAxis switch {
						SliceAxis.FacingZ => (domain.ZMin, domain.ZMax),
						SliceAxis.FacingX => (domain.XMin, domain.XMax),
						_ => (domain.YMin, domain.YMax)
					};
					var slicePosition = domain.SlicePosition;
					ImGui.SetNextItemWidth(fontSize * 12f);
					if (ImGui.SliderFloat("Slice position", ref slicePosition, sliceMin, sliceMax)) domain.SlicePosition = slicePosition;
					break;
			}

			ImGui.Separator();
			ImGui.SetNextItemWidth(fontSize * 12f);
			if (ImGui.BeginCombo("Colour map", domain.ColorMap.ToString())) {
				foreach (var colorMap in ColorMaps) {
					if (ImGui.Selectable(colorMap.ToString(), colorMap == domain.ColorMap)) domain.ColorMap = colorMap;
				}
				ImGui.EndCombo();
			}
			if (_plots.Plot is { } plot) DrawColorLegend(plot, domain.ColorMap, fontSize);
		}
		ImGui.End();
	}

	void DrawTimeWindow(Vector2 position, float fontSize, ApplicationLoop loop) {
		ImGui.SetNextWindowPos(position, ImGuiCond.FirstUseEver, new Vector2(0f, 1f));
		if (ImGui.Begin("Time", WindowFlags)) {
			if (ImGui.Button(_clock.IsPlaying ? "Pause" : "Play")) _clock.IsPlaying = !_clock.IsPlaying;
			ImGui.SameLine();
			if (ImGui.Button("Reset")) _clock.Time = _clock.IsLooping ? _clock.LoopStart : 0f;

			var time = _clock.Time;
			var (sliderMin, sliderMax) = _clock.IsLooping ? (_clock.LoopStart, _clock.LoopEnd) : (MathF.Min(0f, time), MathF.Max(30f, MathF.Ceiling(time / 30f) * 30f));
			ImGui.SetNextItemWidth(fontSize * 18f);
			if (ImGui.SliderFloat("t", ref time, sliderMin, sliderMax)) _clock.Time = time;

			var speed = _clock.Speed;
			ImGui.SetNextItemWidth(fontSize * 18f);
			if (ImGui.SliderFloat("Speed", ref speed, -3f, 3f)) _clock.Speed = speed;

			var isLooping = _clock.IsLooping;
			if (ImGui.Checkbox("Loop", ref isLooping)) _clock.IsLooping = isLooping;
			if (isLooping) {
				ImGui.SameLine();
				var loopRange = new Vector2(_clock.LoopStart, _clock.LoopEnd);
				ImGui.SetNextItemWidth(fontSize * 12f);
				if (ImGui.DragFloat2("Loop range", ref loopRange, 0.05f)) {
					_clock.LoopStart = loopRange.X;
					_clock.LoopEnd = MathF.Max(loopRange.Y, loopRange.X + 0.1f);
				}
			}

			ImGui.Separator();
			ImGui.TextDisabled($"{loop.FramesPerSecondRecentAverage:N0} FPS  |  {_plots.Plot?.Description}");
		}
		ImGui.End();
	}

	bool GridControl(int axisIndex, string axisName, float fontSize) {
		var changed = false;
		var enabled = _settings.GridEnabled[axisIndex];
		if (ImGui.Checkbox($"{axisName} grid", ref enabled)) {
			_settings.GridEnabled[axisIndex] = enabled;
			changed = true;
		}
		if (!enabled) return changed;
		ImGui.SameLine();
		var count = _settings.GridCount[axisIndex];
		ImGui.SetNextItemWidth(fontSize * 8f);
		if (ImGui.SliderInt($"count##grid{axisName}", ref count, 1, 8)) {
			_settings.GridCount[axisIndex] = count;
			changed = true;
		}
		return changed;
	}

	static bool RangeControl(string label, float min, float max, Action<float, float> setRange, float fontSize) {
		var range = new Vector2(min, max);
		ImGui.SetNextItemWidth(fontSize * 12f);
		if (!ImGui.DragFloat2(label, ref range, 0.05f)) return false;
		setRange(range.X, MathF.Max(range.Y, range.X + 0.01f));
		return true;
	}

	void ResolutionControl(string label, int value, int min, int max, Action<int> setValue, float fontSize) {
		ImGui.SetNextItemWidth(fontSize * 12f);
		if (ImGui.SliderInt(label, ref value, min, max)) setValue(value);
		if (ImGui.IsItemDeactivatedAfterEdit()) _plots.Rebuild();
	}

	static void DrawColorLegend(IPlot plot, ColorMapKind colorMap, float fontSize) {
		const int Steps = 24;
		var drawList = ImGui.GetWindowDrawList();
		var origin = ImGui.GetCursorScreenPos();
		var width = fontSize * 12f;
		var height = fontSize * 0.8f;
		for (var i = 0; i < Steps; ++i) {
			var color = ColorMap.Sample(colorMap, (i + 0.5f) / Steps);
			var packed = ImGui.ColorConvertFloat4ToU32(new Vector4(color.Red, color.Green, color.Blue, 1f));
			drawList.AddRectFilled(new Vector2(origin.X + width * i / Steps, origin.Y), new Vector2(origin.X + width * (i + 1) / Steps, origin.Y + height), packed);
		}
		ImGui.Dummy(new Vector2(width, height));
		ImGui.TextDisabled($"{plot.ColorRangeMin:0.##}  to  {plot.ColorRangeMax:0.##}");
	}

	void LoadPreset(Preset preset) {
		_selectedType = preset.Type;
		_equations[(int) preset.Type] = (string[]) preset.Equations.Clone();
		var domain = _plots.Domain;
		var defaults = new PlotDomain();
		domain.XMin = defaults.XMin; domain.XMax = defaults.XMax;
		domain.YMin = defaults.YMin; domain.YMax = defaults.YMax;
		domain.ZMin = defaults.ZMin; domain.ZMax = defaults.ZMax;
		domain.UMin = defaults.UMin; domain.UMax = defaults.UMax;
		domain.VMin = defaults.VMin; domain.VMax = defaults.VMax;
		domain.SlicePosition = defaults.SlicePosition;
		preset.ConfigureDomain?.Invoke(domain);
		Apply();
	}

	void Apply() {
		var labels = PlotController.GetEquationLabels(_selectedType);
		var equations = _equations[(int) _selectedType];
		for (var i = 0; i < equations.Length; ++i) {
			try {
				ExpressionParser.Compile(equations[i], PlotController.GetVariableNames(_selectedType));
			}
			catch (ExpressionException e) {
				_error = $"{labels[i]}: {e.Message} (at character {e.Position + 1})";
				return;
			}
		}
		_error = null;
		_plots.Apply(_selectedType, equations);
		_axes.Rebuild(_plots.Type, _plots.Domain);
	}

	static string Describe(PlotType type) => type switch {
		PlotType.VectorField => "Vector field  F(x, y, z, t)",
		PlotType.ParametricSurface => "Parametric surface  (u, v, t)",
		PlotType.ScalarSlice => "Scalar field slice  w(x, y, z, t)",
		_ => "Surface  y = f(x, z, t)"
	};

	static string Describe(BackgroundStyle style) => style switch {
		BackgroundStyle.Clouds => "Clouds",
		BackgroundStyle.Starfield => "Starfield",
		_ => "Solid colour"
	};

	static string Describe(SliceAxis axis) => axis switch {
		SliceAxis.FacingZ => "x-y plane (move along z)",
		SliceAxis.FacingX => "z-y plane (move along x)",
		_ => "x-z plane (move along y)"
	};
}
