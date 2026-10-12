using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;

namespace SceneProgrammer;

sealed class EditorSession : IDisposable {
	const float MinFlySpeed = 0.5f;
	const float MaxFlySpeed = 100f;
	const float GroundSize = 200f;
	const float GroundThickness = 0.1f;
	static readonly Angle LookSensitivityPerPixel = Angle.FromDegrees(0.2f);

	readonly FreeFlyingCameraController _flyController;
	readonly ProgrammedCameraController _previewController;
	bool _isDragging;
	float _dragPlaneHeight;
	Vect _dragOffset;
	Angle _editorFieldOfView;
	Renderer? _renderer;
	readonly Mesh _groundMesh;
	readonly Texture _groundColorMap;
	readonly Material _groundMaterial;
	readonly ModelInstance _ground;
	bool _groundIsInScene;

	public LocalTinyFfrFactory Factory { get; }
	public Scene Scene { get; }
	public DirectionalLight Sun { get; }
	public Camera EditorCamera { get; }
	public Renderer EditorRenderer => _renderer ?? throw new InvalidOperationException("The editor renderer is currently released.");
	public Renderer? BindableRenderer => _renderer;
	public SceneDocument Document { get; }
	public CameraTrack Track { get; } = new();
	public EnvironmentSettings Environment { get; } = new();
	public RenderSettings Rendering { get; } = new();
	public EditorOverlay Overlay { get; }
	public float FlySpeed { get; private set; } = 5f;
	public bool IsPreviewing { get; private set; }
	public bool IsPreviewPlaying { get; set; }
	public float PreviewTimeSeconds => _previewController.CurrentTimestampSeconds;

	SceneObject? _selection;
	public SceneObject? Selection {
		get => _selection;
		set {
			_selection = value;
			_isDragging = false;
			Overlay.SetSelection(value);
		}
	}

	public event Action<SceneObject?>? SelectionPicked;
	public event Action? DeleteRequested;
	public event Action? AddKeyframeRequested;

	public EditorSession() {
		Factory = new LocalTinyFfrFactory(rendererBuilderConfig: new RendererBuilderConfig { EnableVSync = false });
		Scene = Factory.SceneBuilder.CreateScene();
		Sun = Factory.LightBuilder.CreateDirectionalLight(castsShadows: true, showSunDisc: true);
		Scene.Add(Sun);

		EditorCamera = Factory.CameraBuilder.CreateCamera();
		_flyController = EditorCamera.CreateController<FreeFlyingCameraController>();
		_flyController.Position = new Location(0f, 2f, -6f);
		_flyController.Pitch = Angle.FromDegrees(10f);
		_previewController = EditorCamera.CreateController<ProgrammedCameraController>();
		RecreateRenderer();

		_groundMesh = Factory.MeshBuilder.CreateCuboid(new Cuboid(GroundSize, GroundThickness, GroundSize));
		_groundColorMap = Factory.TextureBuilder.CreateColorMap(new ColorVect(0.4f, 0.4f, 0.4f), includeAlpha: false);
		_groundMaterial = Factory.MaterialBuilder.CreateStandardMaterial(_groundColorMap);
		_ground = Factory.ObjectBuilder.CreateModelInstance(_groundMesh, _groundMaterial, initialPosition: new Location(0f, -GroundThickness * 0.5f, 0f));

		Document = new SceneDocument(Factory, Scene);
		Overlay = new EditorOverlay(Scene);
		Overlay.SetTrack(Track);
		Overlay.SetLights(Document.Lights, null);
		ApplyEnvironment();
	}

	public void ApplyEnvironment() {
		Environment.ApplyTo(Scene, Sun);
		if (Environment.ShowGround && !_groundIsInScene) Scene.Add(_ground);
		else if (!Environment.ShowGround && _groundIsInScene) Scene.Remove(_ground);
		_groundIsInScene = Environment.ShowGround;
		EditorCamera.SetExposure(Environment.Exposure);
	}

	public void ApplyRenderSettings() {
		if (_renderer is { } renderer) Rendering.ApplyTo(renderer, EditorCamera);
	}

	public void Tick(float deltaTime, ILatestKeyboardAndMouseInputRetriever input) {
		if (IsPreviewing) {
			if (IsPreviewPlaying) _previewController.Progress(deltaTime);
		}
		else {
			UpdateFlyingCamera(deltaTime, input);
			UpdatePickingAndDragging(input);
			if (input.KeyWasPressedThisIteration(KeyboardOrMouseKey.Delete)) DeleteRequested?.Invoke();
			if (input.KeyWasPressedThisIteration(KeyboardOrMouseKey.K)) AddKeyframeRequested?.Invoke();
			Overlay.UpdateSelectionBox();
		}
		_renderer?.Render();
	}

	void UpdateFlyingCamera(float deltaTime, ILatestKeyboardAndMouseInputRetriever input) {
		if (input.KeyIsCurrentlyDown(KeyboardOrMouseKey.MouseRight)) {
			_flyController.AdjustYawViaMouseCursor(input, LookSensitivityPerPixel);
			_flyController.AdjustPitchViaMouseCursor(input, LookSensitivityPerPixel);

			var speed = input.KeyIsCurrentlyDown(KeyboardOrMouseKey.LeftShift) ? FlySpeed * 3f : FlySpeed;
			_flyController.AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.W, Orientation.Forward, speed);
			_flyController.AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.S, Orientation.Backward, speed);
			_flyController.AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.A, Orientation.Left, speed);
			_flyController.AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.D, Orientation.Right, speed);
			_flyController.AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.E, Orientation.Up, speed);
			_flyController.AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.Q, Orientation.Down, speed);
		}

		if (input.MouseScrollWheelDelta != 0) {
			FlySpeed = Math.Clamp(FlySpeed * MathF.Pow(1.25f, -input.MouseScrollWheelDelta), MinFlySpeed, MaxFlySpeed);
		}
		_flyController.Progress(deltaTime);
	}

	void UpdatePickingAndDragging(ILatestKeyboardAndMouseInputRetriever input) {
		if (input.KeyWasPressedThisIteration(KeyboardOrMouseKey.MouseLeft)) {
			var pick = EditorRenderer.PickModelInstanceFromRenderSurface(input.MouseCursorPosition);
			var picked = pick is { } p ? Document.FindOwner(p.ModelInstance) : null;
			Selection = picked;
			SelectionPicked?.Invoke(picked);
			if (picked != null && pick is { } hit) {
				_isDragging = true;
				_dragPlaneHeight = hit.Position.Y;
				_dragOffset = picked.Position.AsVect() - hit.Position.AsVect();
			}
			return;
		}

		if (!_isDragging || _selection == null) return;
		if (!input.KeyIsCurrentlyDown(KeyboardOrMouseKey.MouseLeft)) {
			_isDragging = false;
			return;
		}
		if (input.MouseCursorDelta == XYPair<int>.Zero) return;

		if (input.KeyIsCurrentlyDown(KeyboardOrMouseKey.LeftShift)) {
			var distance = EditorCamera.Position.DistanceFrom(_selection.Position);
			var rise = -input.MouseCursorDelta.Y * distance * 0.0025f;
			_dragPlaneHeight += rise;
			_selection.Position += new Vect(0f, rise, 0f);
			return;
		}

		var ray = EditorRenderer.CreateRayFromRenderSurface(input.MouseCursorPosition);
		if (ray.IntersectionWith(new Plane(Direction.Up, new Location(0f, _dragPlaneHeight, 0f))) is not { } groundHit) return;
		if (groundHit.DistanceFrom(EditorCamera.Position) > 500f) return;
		var target = groundHit + _dragOffset;
		_selection.Position = new Location(target.X, _selection.Position.Y, target.Z);
	}

	public Location GetPlacementPointInView() {
		var ray = EditorCamera.CreateRayFromNearPlane();
		if (ray.IntersectionWith(new Plane(Direction.Up)) is { } hit && hit.DistanceFrom(EditorCamera.Position) < 30f) return hit;
		var ahead = EditorCamera.Position + EditorCamera.ViewDirection * 6f;
		return new Location(ahead.X, 0f, ahead.Z);
	}

	public CameraKeyframe CaptureKeyframe() => new(EditorCamera);

	public void SetCustomBackdrop(BackdropTexture backdrop) {
		var previous = Environment.CustomBackdrop;
		Environment.CustomBackdrop = backdrop;
		Environment.Backdrop = BackdropStyle.CustomHdr;
		ApplyEnvironment();
		previous?.Dispose();
	}

	public void ReleaseRenderer() {
		_renderer?.Dispose();
		_renderer = null;
	}

	public Renderer RecreateRenderer() {
		_renderer ??= Factory.RendererBuilder.CreateBindableRenderer(Scene, EditorCamera, Factory.ResourceAllocator);
		Rendering.ApplyTo(_renderer.Value, EditorCamera);
		return _renderer.Value;
	}

	public void EnterPreview(float timeSeconds) {
		if (!IsPreviewing) _editorFieldOfView = EditorCamera.VerticalFieldOfView;
		IsPreviewing = true;
		IsPreviewPlaying = false;
		Overlay.SetVisible(false);
		SeekPreview(timeSeconds);
	}

	public void ExitPreview() {
		if (IsPreviewing) EditorCamera.SetVerticalFieldOfView(_editorFieldOfView);
		IsPreviewing = false;
		IsPreviewPlaying = false;
		Overlay.SetVisible(true);
	}

	public void SeekPreview(float timeSeconds) {
		Track.ApplyTo(_previewController);
		_previewController.CurrentTimestampSeconds = timeSeconds;
		_previewController.Progress(0f);
	}

	public void Dispose() {
		Overlay.Dispose();
		Document.Dispose();
		_previewController.Dispose();
		_flyController.Dispose();
		ReleaseRenderer();
		EditorCamera.Dispose();
		Scene.RemoveBackdrop();
		Environment.CustomBackdrop?.Dispose();
		Scene.Remove(Sun);
		Sun.Dispose();
		if (_groundIsInScene) Scene.Remove(_ground);
		_ground.Dispose();
		_groundMaterial.Dispose();
		_groundColorMap.Dispose();
		_groundMesh.Dispose();
		Scene.Dispose();
		Factory.Dispose();
	}
}
