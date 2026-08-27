// Created on 2026-07-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

namespace TinyFFR.Tests.Integrations.Avalonia.Viewer;

sealed class ModelViewerScene : IDisposable {
	const float SpinDegreesPerSec = 90f;
	const float OverlayYawDegreesPerSec = -130f;
	const float OverlayPitchDegreesPerSec = -80f;

	sealed class LoadedModel {
		public required ResourceGroup Resources { get; init; }
		public required ModelInstanceGroup Instances { get; init; }
		public required Material[] OriginalMaterials { get; init; }
		public required string Summary { get; init; }
		public required string ResourceListing { get; init; }
	}

	readonly LocalTinyFfrFactory _factory;
	readonly Camera _camera;
	readonly InspectorCameraController _cameraController;
	readonly SpotLight _cameraLight;
	readonly DirectionalLight _sunlight;
	readonly Scene _scene;
	readonly Renderer _mainRenderer;
	readonly Dictionary<string, LoadedModel> _loadedModels = new();

	BackdropTexture? _backdrop;
	LoadedModel? _displayed;

	Camera? _overlayCamera;
	Scene? _overlayScene;
	Mesh? _overlayMesh;
	Texture? _overlayTexture;
	Material? _overlayMaterial;
	ModelInstance? _overlayInstance;
	PointLight? _overlayLight;
	Renderer? _overlayRenderer;
	RendererCompositor? _compositor;

	bool _spinX;
	bool _spinY;
	bool _spinZ;
	bool _overlayEnabled = true;
	bool _sunEnabled = true;
	ViewerShadingStyle _shadingStyle = ViewerShadingStyle.Textured;
	ViewerBackdropMode _backdropMode = ViewerBackdropMode.Texture;
	float _backdropIntensity = 1f;

	public bool IsDisposed { get; private set; }
	public Renderer? ActiveRenderer { get; private set; }
	public RendererCompositor? ActiveCompositor { get; private set; }
	public ILocalApplicationLoopBuilder ApplicationLoopBuilder => _factory.ApplicationLoopBuilder;
	public string ResourceListing => _displayed?.ResourceListing ?? "";

	public ModelViewerScene() {
		_factory = new LocalTinyFfrFactory();
		_camera = _factory.CameraBuilder.CreateCamera(new Location(0f, 0f, -1f), cameraRange: CameraPlaneConfiguration.CloseRange);
		_cameraController = _camera.CreateController<InspectorCameraController>();
		_cameraLight = _factory.LightBuilder.CreateSpotLight(position: _camera.Position, coneDirection: _camera.ViewDirection, highQuality: true, brightness: 0f);
		_sunlight = _factory.LightBuilder.CreateDirectionalLight(castsShadows: true);
		_scene = _factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.None);
		_mainRenderer = _factory.RendererBuilder.CreateBindableRenderer(_scene, _camera, _factory.ResourceAllocator);
		_mainRenderer.SetQuality(BuiltInQualityConfiguration.Ultra);

		_scene.Add(_cameraLight);
		_scene.Add(_sunlight);

		ActiveRenderer = _mainRenderer;
	}

	public async Task LoadBackdropAsync() {
		var backdrop = await _factory.AssetLoader.LoadPreprocessedBackdropTextureAsync(
			CommonTestAssets.FindAsset(KnownTestAsset.MetroSkyKtx),
			CommonTestAssets.FindAsset(KnownTestAsset.MetroIblKtx)
		);

		if (IsDisposed) {
			backdrop.Dispose();
			return;
		}

		_backdrop = backdrop;
		ApplyBackdrop();
	}

	public async Task LoadModelAsync(string fileName) {
		var resources = await _factory.AssetLoader.LoadAllAsync(
			CommonTestAssets.FindAsset("models/" + fileName),
			new ModelCreationConfig { MeshConfig = new() { GenerateWireframeData = true } },
			new ModelReadConfig { MeshConfig = new() { LoadSkeletalAnimationDataIfPresent = false, CorrectFlippedOrientation = true }, HandleUriEscapedStrings = true }
		);

		if (IsDisposed || _loadedModels.ContainsKey(fileName)) {
			resources.Dispose();
			return;
		}

		var instances = _factory.ObjectBuilder.CreateModelInstances(resources.Models);

		// SetDefaultMaterialShadingStyle swaps each instance on to the default material, so the originals must be captured before any style is applied
		var originalMaterials = new Material[instances.Count];
		for (var i = 0; i < instances.Count; ++i) originalMaterials[i] = instances[i].Material;

		_loadedModels.Add(fileName, new LoadedModel {
			Resources = resources,
			Instances = instances,
			OriginalMaterials = originalMaterials,
			Summary = $"{fileName}: {resources.Models.Count} models / {resources.Meshes.Count} meshes / {resources.Materials.Count} materials / {resources.Textures.Count} textures",
			ResourceListing = BuildResourceListing(resources)
		});
	}

	public bool IsModelLoaded(string fileName) => _loadedModels.ContainsKey(fileName);

	public string DisplayModel(string fileName) {
		if (!_loadedModels.TryGetValue(fileName, out var model)) return $"{fileName} has not finished loading.";
		if (ReferenceEquals(model, _displayed)) return model.Summary;

		if (_displayed is { } previous) _scene.Remove(previous.Instances);
		_displayed = model;
		_scene.Add(model.Instances);

		_cameraController.SetParametersFromBoundingBox(model.Resources.Models.CalculateCombinedBoundingBox());

		ApplyShadingStyle(ColorVect.RandomOpaque());

		return model.Summary;
	}

	static string BuildResourceListing(ResourceGroup resources) {
		var builder = new StringBuilder();
		builder.AppendLine("Meshes:");
		foreach (var mesh in resources.Meshes) builder.Append('\t').AppendLine(mesh.ToString());
		builder.AppendLine("Materials:");
		foreach (var material in resources.Materials) builder.Append('\t').AppendLine(material.ToString());
		builder.AppendLine("Textures:");
		foreach (var texture in resources.Textures) builder.Append('\t').AppendLine(texture.ToString());
		return builder.ToString();
	}

	public void SetSpin(bool x, bool y, bool z) {
		_spinX = x;
		_spinY = y;
		_spinZ = z;
	}

	public void SetShadingStyle(ViewerShadingStyle style) {
		_shadingStyle = style;
		ApplyShadingStyle(ColorVect.RandomOpaque());
	}

	public void RandomizeColor(bool opaque) {
		if (_shadingStyle == ViewerShadingStyle.Textured) return;
		ApplyShadingStyle(opaque ? ColorVect.RandomOpaque() : ColorVect.Random().WithPremultipliedAlpha());
	}

	void ApplyShadingStyle(ColorVect color) {
		if (_displayed is not { } model) return;

		var instances = model.Instances;
		for (var i = 0; i < instances.Count; ++i) {
			var instance = instances[i];
			if (_shadingStyle == ViewerShadingStyle.Textured) {
				instance.SetMaterial(model.OriginalMaterials[i]);
				continue;
			}

			instance.SetDefaultMaterialShadingStyle(_shadingStyle == ViewerShadingStyle.Wireframe ? DefaultMaterialShadingStyle.Wireframe : DefaultMaterialShadingStyle.Plain3D);
			instance.SetDefaultMaterialBaseColor(color);
		}
	}

	public void ShiftLightHue() {
		_cameraLight.AdjustColorHueBy(30f);
		_sunlight.AdjustColorHueBy(30f);
	}

	public void SetCameraLightBrightness(float brightness) => _cameraLight.SetBrightness(brightness);

	public void SetSunEnabled(bool enabled) {
		if (enabled == _sunEnabled) return;
		_sunEnabled = enabled;
		if (enabled) _scene.Add(_sunlight);
		else _scene.Remove(_sunlight);
	}

	public void SetSunCastsShadows(bool castsShadows) => _sunlight.SetCastsShadows(castsShadows);

	public void SetSunBrightness(float brightness) => _sunlight.SetBrightness(brightness);

	public void SetBackdropMode(ViewerBackdropMode mode) {
		_backdropMode = mode;
		ApplyBackdrop();
	}

	public void SetBackdropIntensity(float intensity) {
		_backdropIntensity = intensity;
		ApplyBackdrop();
	}

	void ApplyBackdrop() {
		switch (_backdropMode) {
			case ViewerBackdropMode.Texture:
				if (_backdrop is { } backdrop) _scene.SetBackdrop(backdrop, _backdropIntensity);
				else _scene.RemoveBackdrop();
				break;
			case ViewerBackdropMode.Color:
				_scene.SetBackdrop(StandardColor.LightingSunMidday, _backdropIntensity);
				break;
			default:
				_scene.RemoveBackdrop();
				break;
		}
	}

	public void SetQuality(BuiltInQualityConfiguration quality) => _mainRenderer.SetQuality(quality);

	public async Task SetCompositeModeAsync(bool enabled) {
		if (enabled == (_compositor != null)) return;

		if (enabled) {
			await EnsureOverlayResourcesExistAsync();
			if (IsDisposed || _compositor != null) return;

			var compositor = _factory.RendererBuilder.CreateBindableCompositor();
			compositor.Add(_mainRenderer, RenderCompositionType.Standard);
			compositor.Add(_overlayRenderer!.Value, RenderCompositionType.RetainPreviousScenes);
			if (!_overlayEnabled) compositor.SetEnabledState(_overlayRenderer.Value, false);
			_compositor = compositor;
			ActiveRenderer = null;
			ActiveCompositor = compositor;
		}
		else {
			// The parameterless overload detaches the contained renderers instead of disposing them, so the main renderer survives the toggle
			_compositor!.Value.Dispose();
			_compositor = null;
			ActiveCompositor = null;
			ActiveRenderer = _mainRenderer;
		}
	}

	public void SetOverlayEnabled(bool enabled) {
		_overlayEnabled = enabled;
		if (_compositor is { } compositor && _overlayRenderer is { } overlayRenderer) compositor.SetEnabledState(overlayRenderer, enabled);
	}

	async Task EnsureOverlayResourcesExistAsync() {
		if (_overlayScene != null) return;

		var overlayTexture = await _factory.AssetLoader.LoadColorMapAsync(_factory.AssetLoader.BuiltInTexturePaths.White);

		if (IsDisposed || _overlayScene != null) {
			overlayTexture.Dispose();
			return;
		}

		var overlayCamera = _factory.CameraBuilder.CreateCamera(Location.Origin);
		var overlayScene = _factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.None);
		var overlayMesh = _factory.AssetLoader.MeshBuilder.CreateMesh(Cuboid.UnitCube);
		var overlayMaterial = _factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(overlayTexture);
		var overlayInstance = _factory.ObjectBuilder.CreateModelInstance(
			overlayMesh,
			overlayMaterial,
			initialPosition: overlayCamera.Position + Direction.Forward * 3f + Direction.Left * 0.9f + Direction.Up * 0.9f
		);
		var overlayLight = _factory.LightBuilder.CreatePointLight(overlayCamera.Position, ColorVect.FromHueSaturationLightness(120f, 0.8f, 0.75f));

		overlayScene.Add(overlayInstance);
		overlayScene.Add(overlayLight);

		_overlayCamera = overlayCamera;
		_overlayScene = overlayScene;
		_overlayMesh = overlayMesh;
		_overlayTexture = overlayTexture;
		_overlayMaterial = overlayMaterial;
		_overlayInstance = overlayInstance;
		_overlayLight = overlayLight;
		var overlayRenderer = _factory.RendererBuilder.CreateBindableRenderer(overlayScene, overlayCamera, _factory.ResourceAllocator);
		overlayRenderer.SetQuality(new RenderQualityConfig(BuiltInQualityConfiguration.High) {
			AntiAliasingMode = AntiAliasingMode.Fxaa
		});
		_overlayRenderer = overlayRenderer;
	}

	public void Tick(float deltaTime, ILatestKeyboardAndMouseInputRetriever input) {
		// AdjustAllViaDefaultControls orbits on any cursor movement; with no cursor-lock equivalent available we gate on the left button instead
		if (input.KeyIsCurrentlyDown(KeyboardOrMouseKey.MouseLeft)) {
			_cameraController.AdjustPitchViaMouseCursor(input, adjustmentPerPixel: InspectorCameraController.DefaultPitchSensitivityMouseCursor * 7f);
			_cameraController.AdjustYawViaMouseCursor(input, adjustmentPerPixel: InspectorCameraController.DefaultYawSensitivityMouseCursor * 7f);
		}
		_cameraController.AdjustDistancePercentageViaMouseWheel(input);
		_cameraController.Progress(deltaTime);

		_cameraLight.Position = _camera.Position;
		_cameraLight.ConeDirection = _camera.ViewDirection;

		if (_displayed is { } model) {
			if (_spinX) model.Instances.RotateBy((SpinDegreesPerSec * deltaTime) % Direction.Left);
			if (_spinY) model.Instances.RotateBy((SpinDegreesPerSec * deltaTime) % Direction.Up);
			if (_spinZ) model.Instances.RotateBy((SpinDegreesPerSec * deltaTime) % Direction.Forward);
		}

		if (_compositor != null && _overlayInstance is { } overlayInstance) {
			overlayInstance.RotateBy((OverlayYawDegreesPerSec * deltaTime) % Direction.Up);
			overlayInstance.RotateBy((OverlayPitchDegreesPerSec * deltaTime) % Direction.Right);
		}
	}

	public void Render() {
		if (_compositor is { } compositor) compositor.RenderAll();
		else _mainRenderer.Render();
	}

	public void Dispose() {
		if (IsDisposed) return;
		IsDisposed = true;

		try {
			if (_compositor is { } compositor) {
				compositor.Dispose();
				_compositor = null;
				ActiveCompositor = null;
			}

			_overlayRenderer?.Dispose();
			if (_overlayScene is { } overlayScene) {
				if (_overlayInstance is { } overlayInstance) overlayScene.Remove(overlayInstance);
				if (_overlayLight is { } overlayLight) overlayScene.Remove(overlayLight);
				overlayScene.Dispose();
			}
			_overlayInstance?.Dispose();
			_overlayLight?.Dispose();
			_overlayMaterial?.Dispose();
			_overlayTexture?.Dispose();
			_overlayMesh?.Dispose();
			_overlayCamera?.Dispose();

			_mainRenderer.Dispose();
			ActiveRenderer = null;
			_cameraController.Dispose();

			if (_displayed is { } displayed) _scene.Remove(displayed.Instances);
			_displayed = null;
			foreach (var model in _loadedModels.Values) {
				model.Instances.Dispose();
				model.Resources.Dispose();
			}
			_loadedModels.Clear();

			// Scene.Dispose would do both of these itself, but doing them explicitly means a dependency tracking regression surfaces here rather than being masked
			_scene.RemoveBackdrop();
			_scene.Remove(_sunlight);
			_scene.Remove(_cameraLight);
			_scene.Dispose();

			_backdrop?.Dispose();
			_sunlight.Dispose();
			_cameraLight.Dispose();
			_camera.Dispose();
		}
		finally {
			_factory.Dispose();
		}
	}
}
