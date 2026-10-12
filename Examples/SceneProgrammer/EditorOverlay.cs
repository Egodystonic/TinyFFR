using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.World;

namespace SceneProgrammer;

sealed class EditorOverlay : IDisposable {
	static readonly PrimitivePaintbrush SelectionBrush = new(new ColorVect(1f, 0.75f, 0.1f));
	static readonly PrimitivePaintbrush PathBrush = new(new ColorVect(0.2f, 0.9f, 1f));
	static readonly PrimitivePaintbrush LookBrush = new(new ColorVect(1f, 0.3f, 0.6f));
	static readonly PrimitivePaintbrush PivotBrush = new(new ColorVect(0.6f, 1f, 0.3f));
	static readonly PrimitivePaintbrush GridBrush = new(new ColorVect(0.35f, 0.55f, 0.9f), new ColorVect(0.75f, 0.75f, 0.75f), new ColorVect(0.5f, 0.5f, 0.5f));

	const int ArcSegmentCount = 24;

	readonly Scene _scene;
	readonly List<ScenePrimitive> _pathPrimitives = new();
	ScenePrimitive? _grid;
	ScenePrimitive? _selectionBox;
	readonly List<ScenePrimitive> _lightPrimitives = new();
	IReadOnlyList<SceneLight>? _lights;
	SceneLight? _selectedLight;
	SceneObject? _selection;
	CameraTrack? _track;
	bool _isVisible;
	bool _isGridVisible = true;

	public EditorOverlay(Scene scene) {
		_scene = scene;
		SetVisible(true);
	}

	public void SetVisible(bool isVisible) {
		if (_isVisible == isVisible) return;
		_isVisible = isVisible;
		if (isVisible) {
			RebuildPath();
			RebuildLights();
		}
		else {
			ClearPath();
			RebuildLights();
		}
		UpdateGrid();
		UpdateSelectionBox();
	}

	public void SetGridVisible(bool isGridVisible) {
		_isGridVisible = isGridVisible;
		UpdateGrid();
	}

	void UpdateGrid() {
		var shouldShow = _isVisible && _isGridVisible;
		if (shouldShow && _grid == null) {
			_grid = _scene.AddPrimitiveGrid(new Location(0f, 0.005f, 0f), GridBrush, gridNormal: Direction.Up, gridSize: 100f, majorGridLineSpacing: 10f, minorGridLineSpacing: 1f);
		}
		else if (!shouldShow && _grid is { } grid) {
			grid.Dispose();
			_grid = null;
		}
	}

	public void SetSelection(SceneObject? selection) {
		_selection = selection;
		UpdateSelectionBox();
	}

	public void SetLights(IReadOnlyList<SceneLight> lights, SceneLight? selectedLight) {
		_lights = lights;
		_selectedLight = selectedLight;
		RebuildLights();
	}

	public void RebuildLights() {
		foreach (var primitive in _lightPrimitives) primitive.Dispose();
		_lightPrimitives.Clear();
		if (!_isVisible || _lights == null) return;

		foreach (var light in _lights) {
			var brush = new PrimitivePaintbrush(light.DisplayColor, light == _selectedLight ? SelectionBrush.PrimaryColor : new ColorVect(0f, 0f, 0f), null);
			var size = light == _selectedLight ? ScenePrimitiveSize.Large : ScenePrimitiveSize.Medium;
			_lightPrimitives.Add(_scene.AddPrimitivePoint(light.Position, brush, size));
			if (light.IsSpotLight) _lightPrimitives.Add(_scene.AddPrimitiveArrow(light.Position, light.ConeDirection, brush, size));
		}
	}

	public void SetTrack(CameraTrack track) {
		_track = track;
		RebuildPath();
	}

	public void UpdateSelectionBox() {
		if (!_isVisible || _selection == null) {
			_selectionBox?.Dispose();
			_selectionBox = null;
			return;
		}

		PositionedRotatedCuboid box = _selection.CalculateWorldBoundingBox();
		if (_selectionBox is { } existing) existing.SetGeometryShape(box, wireframe: true);
		else _selectionBox = _scene.AddPrimitiveShape(box, SelectionBrush, wireframe: true);
	}

	public void RebuildPath() {
		ClearPath();
		if (!_isVisible || _track == null) return;

		var keyframes = _track.Keyframes;
		for (var i = 0; i < keyframes.Count; ++i) {
			var keyframe = keyframes[i];
			if (i > 0) AddPathSegment(keyframes[i - 1].Position, keyframe);
			var lookAt = keyframe.LookAtLocation;
			_pathPrimitives.Add(_scene.AddPrimitiveArrow(keyframe.Position, keyframe.GetViewDirection(), lookAt != null ? LookBrush : PathBrush, ScenePrimitiveSize.Medium));
			_pathPrimitives.Add(_scene.AddPrimitiveString(keyframe.Position + new Vect(0f, 0.25f, 0f), keyframe.Number.ToString(), PathBrush));
			if (keyframe.LookAt == LookAtOption.Point && lookAt is { } lookAtPoint) {
				_pathPrimitives.Add(_scene.AddPrimitivePoint(lookAtPoint, LookBrush, ScenePrimitiveSize.Medium));
				_pathPrimitives.Add(_scene.AddPrimitiveShape(new BoundedRay(keyframe.Position, lookAtPoint), LookBrush, ScenePrimitiveSize.VerySmall, includeEndpoints: false));
			}
		}
	}

	void AddPathSegment(Location start, CameraKeyframe keyframe) {
		var end = keyframe.Position;
		if (keyframe.PivotPoint is not { } pivot) {
			_pathPrimitives.Add(_scene.AddPrimitiveShape(new BoundedRay(start, end), PathBrush, includeEndpoints: false));
			return;
		}

		_pathPrimitives.Add(_scene.AddPrimitivePoint(pivot, PivotBrush, ScenePrimitiveSize.Medium));
		var startVect = start - pivot;
		var endVect = end - pivot;
		var previous = start;
		for (var step = 1; step <= ArcSegmentCount; ++step) {
			var fraction = step / (float) ArcSegmentCount;
			var length = startVect.Length + (endVect.Length - startVect.Length) * fraction;
			var next = startVect.Direction == Direction.None || endVect.Direction == Direction.None
				? start + (end - start) * fraction
				: pivot + Direction.Interpolate(startVect.Direction, endVect.Direction, fraction) * length;
			_pathPrimitives.Add(_scene.AddPrimitiveShape(new BoundedRay(previous, next), PathBrush, includeEndpoints: false));
			previous = next;
		}
	}

	void ClearPath() {
		foreach (var primitive in _pathPrimitives) primitive.Dispose();
		_pathPrimitives.Clear();
	}

	public void Dispose() {
		foreach (var primitive in _lightPrimitives) primitive.Dispose();
		ClearPath();
		_grid?.Dispose();
		_selectionBox?.Dispose();
	}
}
