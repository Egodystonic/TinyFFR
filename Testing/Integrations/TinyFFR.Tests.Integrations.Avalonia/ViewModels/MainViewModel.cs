// Created on 2026-07-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Avalonia;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Rendering;
using TinyFFR.Tests.Integrations.Avalonia.Viewer;

namespace TinyFFR.Tests.Integrations.Avalonia.ViewModels;

public sealed record ResolutionOption(string DisplayName, Size? Value);

public partial class MainViewModel : ViewModelBase {
	ModelViewerScene? _viewer;
	IDisposable? _loop;
	InputElement? _inputSource;
	bool _startInFlight;
	bool _compositorSwitchInFlight;
	bool _hasDisplayedAModel;

	// Set by the view; the UI loop needs an element to source TinyFFR's input abstraction from.
	// Rendering can only start once we have it, so this doubles as the trigger for the initial start.
	public void SetInputSource(InputElement inputSource) {
		_inputSource = inputSource;
		if (_viewer == null) _ = StartRenderingAsync();
	}

	public IReadOnlyList<ViewerShadingStyle> ShadingStyles { get; } = Enum.GetValues<ViewerShadingStyle>();
	public IReadOnlyList<ViewerBackdropMode> BackdropModes { get; } = Enum.GetValues<ViewerBackdropMode>();
	public IReadOnlyList<BuiltInQualityConfiguration> QualityPresets { get; } = Enum.GetValues<BuiltInQualityConfiguration>();
	public IReadOnlyList<ResolutionOption> ResolutionOptions { get; } = new[] {
		new ResolutionOption("Auto (pane size)", null),
		new ResolutionOption("320 x 180", new Size(320d, 180d)),
		new ResolutionOption("640 x 360", new Size(640d, 360d)),
		new ResolutionOption("1280 x 720", new Size(1280d, 720d))
	};

	[ObservableProperty]
	public partial IReadOnlyList<ModelListEntry> CatalogItems { get; set; } = ModelCatalog.Build();

	[ObservableProperty]
	public partial Renderer? Renderer { get; set; }

	[ObservableProperty]
	public partial RendererCompositor? Compositor { get; set; }

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(RenderOnceCommand))]
	[NotifyCanExecuteChangedFor(nameof(RandomizeColorCommand))]
	[NotifyCanExecuteChangedFor(nameof(ShiftLightHueCommand))]
	public partial bool IsRendering { get; set; }

	[ObservableProperty]
	public partial bool IsAwaitingFirstModel { get; set; }

	[ObservableProperty]
	public partial int LoadedModelCount { get; set; }

	[ObservableProperty]
	public partial int FailedModelCount { get; set; }

	[ObservableProperty]
	public partial int TotalModelCount { get; set; }

	[ObservableProperty]
	public partial string LoadProgressText { get; set; } = "";

	[ObservableProperty]
	public partial string StatusText { get; set; } = "Not rendering.";

	[ObservableProperty]
	public partial string ResourceListing { get; set; } = "";

	[ObservableProperty]
	public partial bool Animate { get; set; } = true;

	[ObservableProperty]
	public partial ModelListEntry? SelectedModel { get; set; }

	[ObservableProperty]
	public partial bool SpinX { get; set; }

	[ObservableProperty]
	public partial bool SpinY { get; set; }

	[ObservableProperty]
	public partial bool SpinZ { get; set; }

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(RandomizeColorCommand))]
	public partial ViewerShadingStyle ShadingStyle { get; set; }

	[ObservableProperty]
	public partial bool RandomColorOpaque { get; set; } = true;

	[ObservableProperty]
	public partial float CameraLightBrightness { get; set; }

	[ObservableProperty]
	public partial bool SunEnabled { get; set; } = true;

	[ObservableProperty]
	public partial bool SunCastsShadows { get; set; } = true;

	[ObservableProperty]
	public partial float SunBrightness { get; set; } = 1f;

	[ObservableProperty]
	public partial ViewerBackdropMode BackdropMode { get; set; }

	[ObservableProperty]
	public partial float BackdropIntensity { get; set; } = 1f;

	[ObservableProperty]
	public partial BuiltInQualityConfiguration Quality { get; set; } = BuiltInQualityConfiguration.Ultra;

	[ObservableProperty]
	public partial bool UseCompositor { get; set; }

	[ObservableProperty]
	public partial bool OverlayEnabled { get; set; } = true;

	[ObservableProperty]
	public partial ResolutionOption? SelectedResolution { get; set; }

	[ObservableProperty]
	public partial Size? InternalRenderResolution { get; set; }

	public MainViewModel() {
		SelectedResolution = ResolutionOptions[0];
	}

	[RelayCommand]
	void ToggleRendering() {
		if (_viewer == null) _ = StartRenderingAsync();
		else Shutdown();
	}

	[RelayCommand(CanExecute = nameof(IsRendering))]
	void RenderOnce() => _viewer?.Render();

	[RelayCommand(CanExecute = nameof(CanRandomizeColor))]
	void RandomizeColor() => _viewer?.RandomizeColor(RandomColorOpaque);

	bool CanRandomizeColor() => IsRendering && ShadingStyle != ViewerShadingStyle.Textured;

	[RelayCommand(CanExecute = nameof(IsRendering))]
	void ShiftLightHue() => _viewer?.ShiftLightHue();

	async Task StartRenderingAsync() {
		if (_startInFlight || _viewer != null) return;
		if (_inputSource is not { } inputSource) return;

		_startInFlight = true;
		try {
			var entries = ModelCatalog.Build();
			var selectableCount = 0;
			foreach (var entry in entries) {
				if (entry.FileName != null) ++selectableCount;
			}

			CatalogItems = entries;
			SelectedModel = null;
			LoadedModelCount = 0;
			FailedModelCount = 0;
			TotalModelCount = selectableCount;
			_hasDisplayedAModel = false;
			IsAwaitingFirstModel = true;
			UpdateLoadProgress();

			var viewer = new ModelViewerScene();
			_viewer = viewer;

			PushAllSettings();

			Renderer = viewer.ActiveRenderer;
			Compositor = viewer.ActiveCompositor;
			IsRendering = true;
			StatusText = "Rendering. Models are streaming in asynchronously.";

			_loop = viewer.ApplicationLoopBuilder.StartAvaloniaUiLoop(inputSource, TickWithInput);

			await viewer.LoadBackdropAsync();
			if (!ReferenceEquals(_viewer, viewer)) return;

			if (UseCompositor) {
				await ApplyCompositorAsync(true);
				if (!ReferenceEquals(_viewer, viewer)) return;
			}

			foreach (var entry in entries) {
				if (entry.FileName == null) continue;
				_ = LoadEntryAsync(viewer, entry);
			}
		}
		catch (Exception e) {
			StatusText = $"Failed to start rendering: {e.GetBaseException().Message}";
		}
		finally {
			_startInFlight = false;
		}
	}

	async Task LoadEntryAsync(ModelViewerScene viewer, ModelListEntry entry) {
		try {
			await viewer.LoadModelAsync(entry.FileName!);
			if (viewer.IsDisposed || !ReferenceEquals(_viewer, viewer)) return;

			entry.IsLoaded = true;
			++LoadedModelCount;

			if (!_hasDisplayedAModel) {
				_hasDisplayedAModel = true;
				IsAwaitingFirstModel = false;
				SelectedModel = entry;
			}

			UpdateLoadProgress();
		}
		catch (Exception e) {
			if (viewer.IsDisposed || !ReferenceEquals(_viewer, viewer)) return;

			entry.LoadFailed = true;
			++FailedModelCount;
			UpdateLoadProgress();
			StatusText = $"Failed to load {entry.FileName}: {e.GetBaseException().Message}";
		}
	}

	void UpdateLoadProgress() {
		var pending = TotalModelCount - LoadedModelCount - FailedModelCount;
		if (pending <= 0 && FailedModelCount == 0) LoadProgressText = $"All {TotalModelCount} models loaded.";
		else if (FailedModelCount > 0) LoadProgressText = $"Loaded {LoadedModelCount} of {TotalModelCount} ({FailedModelCount} failed, {pending} pending)";
		else LoadProgressText = $"Loaded {LoadedModelCount} of {TotalModelCount} ({pending} pending)";
	}

	public void Shutdown() {
		if (_viewer == null && _loop == null) return;

		var viewer = _viewer;
		_viewer = null;

		_loop?.Dispose();
		_loop = null;

		Renderer = null;
		Compositor = null;

		viewer?.Dispose();

		IsRendering = false;
		IsAwaitingFirstModel = false;
		LoadedModelCount = 0;
		FailedModelCount = 0;
		LoadProgressText = "";
		ResourceListing = "";
		StatusText = "Not rendering.";
		_hasDisplayedAModel = false;
		SelectedModel = null;
	}

	void PushAllSettings() {
		if (_viewer is not { } viewer) return;

		viewer.SetSpin(SpinX, SpinY, SpinZ);
		viewer.SetShadingStyle(ShadingStyle);
		viewer.SetCameraLightBrightness(CameraLightBrightness);
		viewer.SetSunEnabled(SunEnabled);
		viewer.SetSunCastsShadows(SunCastsShadows);
		viewer.SetSunBrightness(SunBrightness);
		viewer.SetBackdropMode(BackdropMode);
		viewer.SetBackdropIntensity(BackdropIntensity);
		viewer.SetQuality(Quality);
		viewer.SetOverlayEnabled(OverlayEnabled);
	}

	void TickWithInput(TimeSpan deltaTime, ILatestInputRetriever input) {
		if (_viewer is not { } viewer) return;
		viewer.Tick(deltaTime.AsDeltaTime(), input.KeyboardAndMouse);
		if (Animate) viewer.Render();
	}

	partial void OnSelectedModelChanged(ModelListEntry? value) {
		if (value is { IsHeader: true }) {
			SelectedModel = null;
			return;
		}
		if (value?.FileName is not { } fileName || _viewer is not { } viewer) return;
		if (!viewer.IsModelLoaded(fileName)) return;

		StatusText = viewer.DisplayModel(fileName);
		ResourceListing = viewer.ResourceListing;
		viewer.Render();
	}

	partial void OnSpinXChanged(bool value) => _viewer?.SetSpin(SpinX, SpinY, SpinZ);
	partial void OnSpinYChanged(bool value) => _viewer?.SetSpin(SpinX, SpinY, SpinZ);
	partial void OnSpinZChanged(bool value) => _viewer?.SetSpin(SpinX, SpinY, SpinZ);
	partial void OnShadingStyleChanged(ViewerShadingStyle value) => _viewer?.SetShadingStyle(value);
	partial void OnCameraLightBrightnessChanged(float value) => _viewer?.SetCameraLightBrightness(value);
	partial void OnSunEnabledChanged(bool value) => _viewer?.SetSunEnabled(value);
	partial void OnSunCastsShadowsChanged(bool value) => _viewer?.SetSunCastsShadows(value);
	partial void OnSunBrightnessChanged(float value) => _viewer?.SetSunBrightness(value);
	partial void OnBackdropModeChanged(ViewerBackdropMode value) => _viewer?.SetBackdropMode(value);
	partial void OnBackdropIntensityChanged(float value) => _viewer?.SetBackdropIntensity(value);
	partial void OnQualityChanged(BuiltInQualityConfiguration value) => _viewer?.SetQuality(value);
	partial void OnOverlayEnabledChanged(bool value) => _viewer?.SetOverlayEnabled(value);
	partial void OnSelectedResolutionChanged(ResolutionOption? value) => InternalRenderResolution = value?.Value;

	partial void OnUseCompositorChanged(bool value) => _ = ApplyCompositorAsync(value);

	async Task ApplyCompositorAsync(bool value) {
		if (_viewer is not { } viewer || _compositorSwitchInFlight) return;

		_compositorSwitchInFlight = true;
		try {
			// Both must be unbound before the switch: the scene view rejects having Renderer and Compositor set simultaneously,
			// and a renderer that is still supplying frames cannot be attached to a compositor
			Renderer = null;
			Compositor = null;

			await viewer.SetCompositeModeAsync(value);
			if (viewer.IsDisposed || !ReferenceEquals(_viewer, viewer)) return;

			Renderer = viewer.ActiveRenderer;
			Compositor = viewer.ActiveCompositor;
		}
		catch (Exception e) {
			StatusText = $"Failed to change compositor mode: {e.GetBaseException().Message}";
		}
		finally {
			_compositorSwitchInFlight = false;
		}
	}
}
