using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Avalonia;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;

namespace SceneProgrammer;

sealed partial class MainViewModel : ObservableObject, IDisposable {
	readonly EditorSession _session = new();
	InputElement? _inputSource;
	IDisposable? _uiLoop;
	bool _isUpdatingPreviewTimeFromTick;
	bool _isLoadingFogPreset;

	public ObservableCollection<SceneObject> Objects => _session.Document.Objects;
	public ObservableCollection<CameraKeyframe> Keyframes => _session.Track.Keyframes;
	public ObservableCollection<SceneLight> Lights => _session.Document.Lights;
	public PointLightBrightnessPreset[] PointBrightnessPresets { get; } = Enum.GetValues<PointLightBrightnessPreset>();
	public SpotLightBrightnessPreset[] SpotBrightnessPresets { get; } = Enum.GetValues<SpotLightBrightnessPreset>();
	public ObservableCollection<LookAtOption> LookAtChoices { get; } = [LookAtOption.None, LookAtOption.Point];
	public EasingStyle[] EasingStyles { get; } = Enum.GetValues<EasingStyle>();
	public AnimationWrapStyle[] WrapStyles { get; } = Enum.GetValues<AnimationWrapStyle>();
	public ObservableCollection<BackdropStyle> BackdropStyles { get; } = [BackdropStyle.Clouds, BackdropStyle.Starfield, BackdropStyle.SkyColor, BackdropStyle.None];
	public SceneBackdropBrightnessPreset[] BackdropBrightnessPresets { get; } = Enum.GetValues<SceneBackdropBrightnessPreset>();
	public Quality[] HdrQualities { get; } = Enum.GetValues<Quality>();
	public BuiltInQualityConfiguration[] RenderQualities { get; } = [BuiltInQualityConfiguration.VeryLow, BuiltInQualityConfiguration.Low, BuiltInQualityConfiguration.Medium, BuiltInQualityConfiguration.High, BuiltInQualityConfiguration.VeryHigh, BuiltInQualityConfiguration.Ultra];
	public FogDensity[] FogPresets { get; } = Enum.GetValues<FogDensity>();
	public LightingPreset[] LightingPresets { get; } = Enum.GetValues<LightingPreset>();
	public CameraExposurePreset[] ExposurePresets { get; } = Enum.GetValues<CameraExposurePreset>();

	[ObservableProperty] public partial Renderer? ActiveRenderer { get; set; }
	[ObservableProperty] public partial SceneObject? SelectedObject { get; set; }
	[ObservableProperty] public partial CameraKeyframe? SelectedKeyframe { get; set; }
	[ObservableProperty] public partial SceneLight? SelectedLight { get; set; }
	[ObservableProperty] public partial string StatusText { get; set; } = "Import a model to get started.";
	[ObservableProperty] public partial bool IsBusy { get; set; }
	[ObservableProperty] public partial bool IsPreviewing { get; set; }
	[ObservableProperty] public partial bool IsPreviewPlaying { get; set; }
	[ObservableProperty] public partial double PreviewTimeSeconds { get; set; }
	[ObservableProperty] public partial double TrackLengthSeconds { get; set; }
	[ObservableProperty] public partial AnimationWrapStyle Wrapping { get; set; } = AnimationWrapStyle.Once;
	[ObservableProperty] public partial BackdropStyle Backdrop { get; set; } = BackdropStyle.Clouds;
	[ObservableProperty] public partial LightingPreset Lighting { get; set; } = LightingPreset.Midday;
	[ObservableProperty] public partial double SunAzimuthDegrees { get; set; } = 35d;
	[ObservableProperty] public partial double SunElevationDegrees { get; set; } = 50d;
	[ObservableProperty] public partial bool SunCastsShadows { get; set; } = true;
	[ObservableProperty] public partial bool ShowGround { get; set; } = true;
	[ObservableProperty] public partial bool ShowGrid { get; set; } = true;
	[ObservableProperty] public partial double SunBrightnessMultiplier { get; set; } = 1d;
	[ObservableProperty] public partial Color SunColor { get; set; } = Colors.White;
	[ObservableProperty] public partial SceneBackdropBrightnessPreset BackdropBrightness { get; set; } = SceneBackdropBrightnessPreset.Midday;
	[ObservableProperty] public partial bool UseBackdropBrightnessSlider { get; set; }
	[ObservableProperty] public partial double BackdropBrightnessPercent { get; set; } = 100d;
	[ObservableProperty] public partial Quality HdrQuality { get; set; } = Quality.Standard;
	[ObservableProperty] public partial string? CustomBackdropName { get; set; }
	[ObservableProperty] public partial BuiltInQualityConfiguration RenderQuality { get; set; } = BuiltInQualityConfiguration.High;
	[ObservableProperty] public partial bool DepthOfFieldEnabled { get; set; }
	[ObservableProperty] public partial decimal FocusDistance { get; set; } = 5m;
	[ObservableProperty] public partial double DepthOfFieldStrength { get; set; } = 1d;
	[ObservableProperty] public partial bool FogEnabled { get; set; }
	[ObservableProperty] public partial FogDensity FogPreset { get; set; } = FogDensity.Thin;
	[ObservableProperty] public partial Color FogColor { get; set; }
	[ObservableProperty] public partial double FogOpacity { get; set; }
	[ObservableProperty] public partial double FogDensityMultiplier { get; set; }
	[ObservableProperty] public partial double FogStartDistance { get; set; }
	[ObservableProperty] public partial double FogGroundLayerHeight { get; set; }
	[ObservableProperty] public partial double FogSkywardFalloff { get; set; }
	[ObservableProperty] public partial double FogSunScattering { get; set; }
	[ObservableProperty] public partial bool FogOccludesBackdrop { get; set; }

	public bool HasSelectedObject => SelectedObject != null;
	public bool HasSelectedKeyframe => SelectedKeyframe != null;
	public bool HasSelectedLight => SelectedLight != null;
	public bool CanPlay => Keyframes.Count >= 2 && !IsBusy;
	public bool CanUseBackdropBrightnessSlider => Backdrop == BackdropStyle.CustomHdr;
	public bool IsBackdropBrightnessSliderActive => UseBackdropBrightnessSlider && CanUseBackdropBrightnessSlider;

	public MainViewModel() {
		LoadFogPreset(FogPreset);
		ActiveRenderer = _session.BindableRenderer;
		_session.SelectionPicked += picked => SelectedObject = picked;
		_session.DeleteRequested += () => DeleteSelectedObject();
		_session.AddKeyframeRequested += () => AddKeyframe();
		Objects.CollectionChanged += OnObjectsChanged;
		Keyframes.CollectionChanged += (_, _) => OnTrackChanged();
	}

	public void AttachInputSource(InputElement inputSource) {
		_inputSource = inputSource;
		StartUiLoop();
	}

	void StartUiLoop() {
		if (_inputSource == null) return;
		_uiLoop = _session.Factory.ApplicationLoopBuilder.StartAvaloniaUiLoop(_inputSource, Tick);
	}

	void Tick(TimeSpan deltaTime, ILatestInputRetriever input) {
		_session.Tick(deltaTime.AsDeltaTime(), input.KeyboardAndMouse);
		if (!IsPreviewPlaying) return;

		_isUpdatingPreviewTimeFromTick = true;
		PreviewTimeSeconds = Math.Min(_session.PreviewTimeSeconds, TrackLengthSeconds);
		_isUpdatingPreviewTimeFromTick = false;
		if (Wrapping == AnimationWrapStyle.Once && _session.PreviewTimeSeconds >= TrackLengthSeconds) IsPreviewPlaying = false;
	}

	public async Task ImportAsync(IReadOnlyList<string> filePaths) {
		if (filePaths.Count == 0) return;
		IsBusy = true;
		try {
			for (var i = 0; i < filePaths.Count; ++i) {
				StatusText = $"Importing {Path.GetFileName(filePaths[i])} ({i + 1} of {filePaths.Count})...";
				SelectedObject = await _session.Document.ImportAsync(filePaths[i], _session.GetPlacementPointInView());
			}
			StatusText = $"Imported {filePaths.Count} model{(filePaths.Count == 1 ? "" : "s")}.";
		}
		catch (Exception e) {
			StatusText = $"Import failed: {e.GetBaseException().Message}";
		}
		finally {
			IsBusy = false;
		}
	}

	public async Task LoadCustomBackdropAsync(string filePath) {
		IsBusy = true;
		try {
			StatusText = $"Converting {Path.GetFileName(filePath)} to a backdrop at {HdrQuality} quality (this can take a minute or more)...";
			var backdrop = await _session.Factory.AssetLoader.LoadBackdropTextureAsync(filePath, HdrQuality);
			_session.SetCustomBackdrop(backdrop);
			if (!BackdropStyles.Contains(BackdropStyle.CustomHdr)) BackdropStyles.Insert(3, BackdropStyle.CustomHdr);
			CustomBackdropName = $"Custom HDR: {Path.GetFileName(filePath)} ({HdrQuality})";
			Backdrop = BackdropStyle.CustomHdr;
			StatusText = $"Loaded backdrop {Path.GetFileName(filePath)}.";
		}
		catch (Exception e) {
			StatusText = $"Backdrop load failed: {e.GetBaseException().Message}";
		}
		finally {
			IsBusy = false;
		}
	}

	[RelayCommand]
	void FocusOnSelectedObject() {
		if (SelectedObject is not { } selected) return;
		var distance = _session.EditorCamera.Position.DistanceFrom(selected.CalculateWorldBoundingBox().Position);
		FocusDistance = Math.Round((decimal) distance, 2);
		DepthOfFieldEnabled = true;
	}

	[RelayCommand]
	void FocusSelectedKeyframeOnSelectedObject() {
		if (SelectedKeyframe is not { } keyframe || SelectedObject is not { } selected) return;
		var distance = keyframe.Position.DistanceFrom(selected.CalculateWorldBoundingBox().Position);
		keyframe.FocusDistance = Math.Round((decimal) MathF.Max(distance, 0.01f), 2);
		keyframe.DepthOfFieldEnabled = true;
	}

	[RelayCommand]
	void ApplyFogPreset() {
		LoadFogPreset(FogPreset);
		FogEnabled = true;
		ApplyEnvironment();
	}

	void LoadFogPreset(FogDensity preset) {
		var fog = new FogDescriptor(preset);
		var srgb = ColorVect.LinearToSrgb(fog.Color);
		_isLoadingFogPreset = true;
		FogColor = Color.FromRgb((byte) Math.Round(srgb.Red * 255f), (byte) Math.Round(srgb.Green * 255f), (byte) Math.Round(srgb.Blue * 255f));
		FogOpacity = fog.Color.Alpha;
		FogDensityMultiplier = fog.DensityMultiplier;
		FogStartDistance = fog.StartDistance;
		FogGroundLayerHeight = fog.GroundLayerHeight;
		FogSkywardFalloff = fog.SkywardDensityFalloffMultiplier;
		FogSunScattering = fog.DirectionalLightScatteringStrengthMultiplier;
		FogOccludesBackdrop = fog.OccludesBackdrop;
		_isLoadingFogPreset = false;
	}

	FogDescriptor CreateFogDescriptor() {
		var linear = ColorVect.SrgbToLinear(new ColorVect(FogColor.R / 255f, FogColor.G / 255f, FogColor.B / 255f));
		return new FogDescriptor {
			Color = new ColorVect(linear.Red, linear.Green, linear.Blue, (float) FogOpacity),
			DensityMultiplier = (float) FogDensityMultiplier,
			StartDistance = (float) FogStartDistance,
			GroundLayerHeight = (float) FogGroundLayerHeight,
			SkywardDensityFalloffMultiplier = (float) FogSkywardFalloff,
			DirectionalLightScatteringStrengthMultiplier = (float) FogSunScattering,
			OccludesBackdrop = FogOccludesBackdrop
		};
	}

	[RelayCommand]
	void AddPointLight() => SelectNewLight(_session.Document.AddPointLight(_session.EditorCamera.Position));

	[RelayCommand]
	void AddSpotLight() => SelectNewLight(_session.Document.AddSpotLight(_session.EditorCamera.Position, _session.EditorCamera.ViewDirection));

	void SelectNewLight(SceneLight light) {
		light.PropertyChanged += (_, _) => _session.Overlay.RebuildLights();
		SelectedLight = light;
		StatusText = $"Added {light.Name.ToLowerInvariant()} at the camera's position.";
	}

	[RelayCommand]
	void DeleteSelectedLight() {
		if (SelectedLight is not { } light) return;
		SelectedLight = null;
		_session.Document.DeleteLight(light);
		_session.Overlay.RebuildLights();
	}

	[RelayCommand]
	void MoveSelectedLightToCamera() {
		if (SelectedLight is { } light) light.Position = _session.EditorCamera.Position;
	}

	[RelayCommand]
	void AimSelectedLightAlongView() {
		if (SelectedLight is { } light) light.AimAlong(_session.EditorCamera.ViewDirection);
	}

	[RelayCommand]
	void AimSelectedLightAtSelectedObject() {
		if (SelectedLight is not { } light || SelectedObject is not { } target) return;
		var direction = light.Position.DirectionTo(target.CalculateWorldBoundingBox().Position);
		if (direction != Direction.None) light.AimAlong(direction);
	}

	partial void OnSelectedLightChanged(SceneLight? value) {
		OnPropertyChanged(nameof(HasSelectedLight));
		_session.Overlay.SetLights(Lights, value);
	}

	[RelayCommand]
	void DuplicateSelectedObject() {
		if (SelectedObject is { } selected) SelectedObject = _session.Document.Duplicate(selected);
	}

	[RelayCommand]
	void DeleteSelectedObject() {
		if (SelectedObject is not { } selected) return;
		SelectedObject = null;
		foreach (var keyframe in Keyframes) {
			if (keyframe.LookAtTarget == selected) keyframe.LookAt = LookAtOption.None;
		}
		_session.Document.Delete(selected);
	}

	[RelayCommand]
	void AddKeyframe() {
		if (IsPreviewing) return;
		var keyframe = _session.CaptureKeyframe();
		keyframe.PropertyChanged += OnKeyframePropertyChanged;
		var insertIndex = SelectedKeyframe is { } selected ? Keyframes.IndexOf(selected) + 1 : Keyframes.Count;
		Keyframes.Insert(insertIndex, keyframe);
		SelectedKeyframe = keyframe;
	}

	[RelayCommand]
	void RecaptureSelectedKeyframe() {
		if (IsPreviewing) return;
		SelectedKeyframe?.CaptureFrom(_session.EditorCamera);
	}

	[RelayCommand]
	void SetLookAtPointToCamera() {
		if (SelectedKeyframe is not { } keyframe || IsPreviewing) return;
		keyframe.LookAtPoint = _session.EditorCamera.Position;
		keyframe.LookAt = LookAtOption.Point;
	}

	[RelayCommand]
	void ClearLookAt() {
		if (SelectedKeyframe is not { } keyframe) return;
		keyframe.LookAt = LookAtOption.None;
		keyframe.LookAtPoint = null;
	}

	[RelayCommand]
	void SetPivotToCamera() {
		if (SelectedKeyframe is not { CanHavePivot: true } keyframe || IsPreviewing) return;
		keyframe.PivotPoint = _session.EditorCamera.Position;
	}

	[RelayCommand]
	void ClearPivot() {
		if (SelectedKeyframe is { } keyframe) keyframe.PivotPoint = null;
	}

	[RelayCommand]
	void MoveSelectedKeyframeUp() => MoveSelectedKeyframe(-1);

	[RelayCommand]
	void MoveSelectedKeyframeDown() => MoveSelectedKeyframe(1);

	void MoveSelectedKeyframe(int offset) {
		if (SelectedKeyframe is not { } selected) return;
		var index = Keyframes.IndexOf(selected);
		var newIndex = Math.Clamp(index + offset, 0, Keyframes.Count - 1);
		if (newIndex == index) return;
		Keyframes.Move(index, newIndex);
		SelectedKeyframe = selected;
	}

	[RelayCommand]
	void DeleteSelectedKeyframe() {
		if (SelectedKeyframe is not { } selected) return;
		var index = Keyframes.IndexOf(selected);
		selected.PropertyChanged -= OnKeyframePropertyChanged;
		Keyframes.Remove(selected);
		SelectedKeyframe = Keyframes.Count > 0 ? Keyframes[Math.Min(index, Keyframes.Count - 1)] : null;
	}

	[RelayCommand]
	void TogglePreviewPlayback() {
		if (!IsPreviewing) IsPreviewing = true;
		if (!IsPreviewPlaying && PreviewTimeSeconds >= TrackLengthSeconds) PreviewTimeSeconds = 0d;
		IsPreviewPlaying = !IsPreviewPlaying;
	}

	[RelayCommand]
	void PlayFullscreen() {
		if (!CanPlay) return;
		IsPreviewing = false;
		_uiLoop?.Dispose();
		_uiLoop = null;
		ActiveRenderer = null;
		_session.ReleaseRenderer();
		_session.Overlay.SetVisible(false);
		try {
			var summary = FullscreenPlayer.Play(_session);
			StatusText = $"Played {summary.DurationSeconds:N1}s at an average of {summary.AverageFramesPerSecond:N0} FPS.";
		}
		catch (Exception e) {
			StatusText = $"Playback failed: {e.GetBaseException().Message}";
		}
		finally {
			_session.Overlay.SetVisible(true);
			ActiveRenderer = _session.RecreateRenderer();
			StartUiLoop();
		}
	}

	partial void OnSelectedObjectChanged(SceneObject? value) {
		if (_session.Selection != value) _session.Selection = value;
		OnPropertyChanged(nameof(HasSelectedObject));
	}

	partial void OnSelectedKeyframeChanged(CameraKeyframe? value) => OnPropertyChanged(nameof(HasSelectedKeyframe));
	partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(CanPlay));

	partial void OnIsPreviewingChanged(bool value) {
		if (value) _session.EnterPreview((float) PreviewTimeSeconds);
		else _session.ExitPreview();
		IsPreviewPlaying = false;
	}

	partial void OnIsPreviewPlayingChanged(bool value) {
		if (value && !IsPreviewing) IsPreviewing = true;
		_session.IsPreviewPlaying = value;
		if (value) _session.SeekPreview((float) PreviewTimeSeconds);
	}

	partial void OnPreviewTimeSecondsChanged(double value) {
		if (_isUpdatingPreviewTimeFromTick) return;
		if (!IsPreviewing) IsPreviewing = true;
		_session.SeekPreview((float) value);
	}

	partial void OnWrappingChanged(AnimationWrapStyle value) {
		_session.Track.Wrapping = value;
		OnTrackChanged();
	}

	partial void OnBackdropChanged(BackdropStyle value) {
		OnPropertyChanged(nameof(CanUseBackdropBrightnessSlider));
		OnPropertyChanged(nameof(IsBackdropBrightnessSliderActive));
		ApplyEnvironment();
	}

	partial void OnLightingChanged(LightingPreset value) {
		BackdropBrightness = EnvironmentSettings.GetMatchingBackdropBrightness(value);
		ApplyEnvironment();
	}

	partial void OnBackdropBrightnessChanged(SceneBackdropBrightnessPreset value) => ApplyEnvironment();
	partial void OnFogEnabledChanged(bool value) => ApplyEnvironment();
	partial void OnFogColorChanged(Color value) => ApplyEnvironment();
	partial void OnFogOpacityChanged(double value) => ApplyEnvironment();
	partial void OnFogDensityMultiplierChanged(double value) => ApplyEnvironment();
	partial void OnFogStartDistanceChanged(double value) => ApplyEnvironment();
	partial void OnFogGroundLayerHeightChanged(double value) => ApplyEnvironment();
	partial void OnFogSkywardFalloffChanged(double value) => ApplyEnvironment();
	partial void OnFogSunScatteringChanged(double value) => ApplyEnvironment();
	partial void OnFogOccludesBackdropChanged(bool value) => ApplyEnvironment();

	partial void OnRenderQualityChanged(BuiltInQualityConfiguration value) => ApplyRenderSettings();
	partial void OnDepthOfFieldEnabledChanged(bool value) => ApplyRenderSettings();
	partial void OnFocusDistanceChanged(decimal value) => ApplyRenderSettings();
	partial void OnDepthOfFieldStrengthChanged(double value) => ApplyRenderSettings();

	void ApplyRenderSettings() {
		var rendering = _session.Rendering;
		rendering.Quality = RenderQuality;
		rendering.DepthOfFieldEnabled = DepthOfFieldEnabled;
		rendering.FocusDistance = MathF.Max((float) FocusDistance, 0.01f);
		rendering.DepthOfFieldStrength = (float) DepthOfFieldStrength;
		_session.ApplyRenderSettings();
	}
	partial void OnBackdropBrightnessPercentChanged(double value) => ApplyEnvironment();

	partial void OnUseBackdropBrightnessSliderChanged(bool value) {
		OnPropertyChanged(nameof(IsBackdropBrightnessSliderActive));
		ApplyEnvironment();
	}
	partial void OnSunAzimuthDegreesChanged(double value) => ApplyEnvironment();
	partial void OnSunElevationDegreesChanged(double value) => ApplyEnvironment();
	partial void OnSunCastsShadowsChanged(bool value) => ApplyEnvironment();
	partial void OnShowGroundChanged(bool value) => ApplyEnvironment();
	partial void OnShowGridChanged(bool value) => _session.Overlay.SetGridVisible(value);
	partial void OnSunBrightnessMultiplierChanged(double value) => ApplyEnvironment();
	partial void OnSunColorChanged(Color value) => ApplyEnvironment();

	void ApplyEnvironment() {
		if (_isLoadingFogPreset) return;
		var environment = _session.Environment;
		environment.Fog = FogEnabled ? CreateFogDescriptor() : null;
		environment.Backdrop = Backdrop;
		environment.Lighting = Lighting;
		environment.SunAzimuthDegrees = (float) SunAzimuthDegrees;
		environment.SunElevationDegrees = (float) SunElevationDegrees;
		environment.SunCastsShadows = SunCastsShadows;
		environment.SunBrightnessMultiplier = (float) SunBrightnessMultiplier;
		environment.SunColor = ColorVect.SrgbToLinear(new ColorVect(SunColor.R / 255f, SunColor.G / 255f, SunColor.B / 255f));
		environment.ShowGround = ShowGround;
		environment.BackdropBrightness = BackdropBrightness;
		environment.UseBackdropBrightnessSlider = IsBackdropBrightnessSliderActive;
		environment.BackdropBrightnessPercent = (float) BackdropBrightnessPercent;
		_session.ApplyEnvironment();
	}

	void OnKeyframePropertyChanged(object? sender, PropertyChangedEventArgs e) {
		if (e.PropertyName is nameof(CameraKeyframe.Summary) or nameof(CameraKeyframe.Number) or nameof(CameraKeyframe.CanHavePivot) or nameof(CameraKeyframe.LookAtPointDescription) or nameof(CameraKeyframe.PivotDescription)) return;
		if (e.PropertyName == nameof(CameraKeyframe.LookAt) && sender is CameraKeyframe { LookAt: var lookAt } keyframe && lookAt == LookAtOption.Point && keyframe.LookAtPoint == null) {
			keyframe.LookAtPoint = _session.EditorCamera.Position;
			return;
		}
		OnTrackChanged();
	}

	void OnTrackChanged() {
		_session.Track.Renumber();
		_session.Overlay.RebuildPath();
		TrackLengthSeconds = _session.Track.LengthSeconds;
		OnPropertyChanged(nameof(CanPlay));
		if (IsPreviewing) _session.SeekPreview((float) Math.Min(PreviewTimeSeconds, TrackLengthSeconds));
	}

	void OnObjectsChanged(object? sender, NotifyCollectionChangedEventArgs e) {
		if (e.OldItems != null) {
			foreach (SceneObject removed in e.OldItems) LookAtChoices.Remove(removed.AsLookAtTarget);
		}
		if (e.NewItems != null) {
			foreach (SceneObject added in e.NewItems) LookAtChoices.Add(added.AsLookAtTarget);
		}
	}

	public void Dispose() {
		_uiLoop?.Dispose();
		_uiLoop = null;
		ActiveRenderer = null;
		_session.Dispose();
	}
}
