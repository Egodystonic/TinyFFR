using System.Globalization;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.World;

namespace EquationPlotter;

sealed class AxesGizmo : IDisposable {
	const float Half = PlotDomain.WorldHalfSize;
	static readonly PrimitivePaintbrush BoxBrush = new(new ColorVect(0.55f, 0.6f, 0.7f));
	static readonly PrimitivePaintbrush GridBrush = new(new ColorVect(0.35f, 0.4f, 0.5f), new ColorVect(0.3f, 0.33f, 0.4f), new ColorVect(0.22f, 0.24f, 0.3f));
	static readonly PrimitivePaintbrush XBrush = new(new ColorVect(0.95f, 0.35f, 0.35f));
	static readonly PrimitivePaintbrush YBrush = new(new ColorVect(0.4f, 0.9f, 0.4f));
	static readonly PrimitivePaintbrush ZBrush = new(new ColorVect(0.4f, 0.6f, 1f));
	static readonly Vect XLength = new(Half * 2f, 0f, 0f);
	static readonly Vect YLength = new(0f, Half * 2f, 0f);
	static readonly Vect ZLength = new(0f, 0f, Half * 2f);
	static readonly Location Corner = new(-Half, -Half, -Half);

	readonly Scene _scene;
	readonly Scene _labelScene;
	readonly SceneSettings _settings;
	readonly List<ScenePrimitive> _primitives = new();
	readonly List<ScenePrimitive> _labels = new();
	PlotType _type;
	PlotDomain? _domain;

	public AxesGizmo(Scene scene, Scene labelScene, SceneSettings settings) {
		_scene = scene;
		_labelScene = labelScene;
		_settings = settings;
	}

	public void Rebuild(PlotType type, PlotDomain domain) {
		_type = type;
		_domain = domain;
		RebuildPrimitives();
		RebuildLabels();
	}

	public void RebuildPrimitives() {
		Clear(_primitives);
		if (_settings.BoxEnabled) {
			_primitives.Add(_scene.AddPrimitiveShape(new PositionedRotatedCuboid(Half * 2f, Half * 2f, Half * 2f, Location.Origin, Rotation.None), BoxBrush, wireframe: true));
		}
		AddGrids(0, XLength);
		AddGrids(1, YLength);
		AddGrids(2, ZLength);
		if (_settings.AxesEnabled) {
			AddAxis(XLength, XBrush);
			AddAxis(YLength, YBrush);
			AddAxis(ZLength, ZBrush);
		}
	}

	void AddGrids(int axisIndex, Vect length) {
		if (!_settings.GridEnabled[axisIndex]) return;
		var count = Math.Max(_settings.GridCount[axisIndex], 1);
		for (var i = 0; i < count; ++i) {
			var centre = Location.Origin + length * (i / (float) count - 0.5f);
			_primitives.Add(_scene.AddPrimitiveGrid(centre, GridBrush, gridNormal: length.Direction, gridSize: Half * 2f, majorGridLineSpacing: 1f, minorGridLineSpacing: 0.25f));
		}
	}

	public void RebuildLabels() {
		Clear(_labels);
		if (!_settings.LabelsEnabled || _domain is not { } domain) return;

		var brush = new PrimitivePaintbrush(ToColorVect(_settings.LabelColor), ToColorVect(_settings.LabelOutlineColor), null);
		var nameSize = ScenePrimitive.ConvertStringPrimitiveSize(ScenePrimitiveSize.Medium) * _settings.LabelSizeMultiplier;
		var rangeSize = ScenePrimitive.ConvertStringPrimitiveSize(ScenePrimitiveSize.Small) * _settings.LabelSizeMultiplier;
		AddAxisLabels(XLength, "x", domain.XMin, domain.XMax, brush, nameSize, rangeSize);
		AddAxisLabels(YLength, _type == PlotType.Surface ? "y = f(x, z, t)" : "y", domain.YMin, domain.YMax, brush, nameSize, rangeSize);
		AddAxisLabels(ZLength, "z", domain.ZMin, domain.ZMax, brush, nameSize, rangeSize);
	}

	void AddAxis(Vect length, in PrimitivePaintbrush brush) {
		var end = Corner + length;
		_primitives.Add(_scene.AddPrimitiveShape(new BoundedRay(Corner, end), brush, ScenePrimitiveSize.Medium, includeEndpoints: false));
		_primitives.Add(_scene.AddPrimitiveArrow(end, length.Direction, brush, ScenePrimitiveSize.Medium));
	}

	void AddAxisLabels(Vect length, string name, float min, float max, in PrimitivePaintbrush brush, float nameSize, float rangeSize) {
		var end = Corner + length;
		var outward = (Corner + length * 0.5f).AsVect().WithLength(0.3f);
		_labels.Add(_labelScene.AddPrimitiveString(end + length.WithLength(1.1f) + outward * 2f, name, brush, nameSize, constantScreenSize: true));
		_labels.Add(_labelScene.AddPrimitiveString(Corner + length * 0.05f + outward, Format(min), brush, rangeSize, constantScreenSize: true));
		_labels.Add(_labelScene.AddPrimitiveString(end - length * 0.05f + outward, Format(max), brush, rangeSize, constantScreenSize: true));
	}

	static ColorVect ToColorVect(System.Numerics.Vector3 color) => new(color.X, color.Y, color.Z);

	static string Format(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

	static void Clear(List<ScenePrimitive> primitives) {
		foreach (var primitive in primitives) primitive.Dispose();
		primitives.Clear();
	}

	public void Dispose() {
		Clear(_labels);
		Clear(_primitives);
	}
}
