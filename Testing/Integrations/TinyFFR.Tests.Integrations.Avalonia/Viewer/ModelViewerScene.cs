// Created on 2026-07-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Collections.Generic;
using System.Text;
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

	readonly LocalTinyFfrFactory _factory;
	readonly Camera _camera;
	readonly InspectorCameraController _cameraController;
	readonly SpotLight _cameraLight;
	readonly DirectionalLight _sunlight;
	readonly BackdropTexture _backdrop;
	readonly Scene _scene;
	readonly Renderer _mainRenderer;
	readonly List<Material> _originalMaterials = new();

	Camera? _overlayCamera;
	Scene? _overlayScene;
	Mesh? _overlayMesh;
	Texture? _overlayTexture;
	Material? _overlayMaterial;
	ModelInstance? _overlayInstance;
	PointLight? _overlayLight;
	Renderer? _overlayRenderer;
	RendererCompositor? _compositor;

	ResourceGroup? _loadedResources;
	ModelInstanceGroup? _modelInstances;

	bool _spinX;
	bool _spinY;
	bool _spinZ;
	bool _overlayEnabled = true;
	bool _sunEnabled = true;
	ViewerShadingStyle _shadingStyle = ViewerShadingStyle.Textured;
	ViewerBackdropMode _backdropMode = ViewerBackdropMode.Texture;
	float _backdropIntensity = 1f;

	public Renderer? ActiveRenderer { get; private set; }
	public RendererCompositor? ActiveCompositor { get; private set; }
	public ILocalApplicationLoopBuilder ApplicationLoopBuilder => _factory.ApplicationLoopBuilder;
	public string ResourceListing { get; private set; } = "";

	public ModelViewerScene() {
		_factory = new LocalTinyFfrFactory();
		_camera = _factory.CameraBuilder.CreateCamera(new Location(0f, 0f, -1f), cameraRange: CameraPlaneConfiguration.CloseRange);
		_cameraController = _camera.CreateController<InspectorCameraController>();
		_cameraLight = _factory.LightBuilder.CreateSpotLight(position: _camera.Position, coneDirection: _camera.ViewDirection, highQuality: true, brightness: 0f);
		_sunlight = _factory.LightBuilder.CreateDirectionalLight(castsShadows: true);
		_backdrop = _factory.AssetLoader.LoadPreprocessedBackdropTexture(
			CommonTestAssets.FindAsset(KnownTestAsset.MetroSkyKtx),
			CommonTestAssets.FindAsset(KnownTestAsset.MetroIblKtx)
		);
		_scene = _factory.SceneBuilder.CreateScene(_backdrop);
		_mainRenderer = _factory.RendererBuilder.CreateBindableRenderer(_scene, _camera, _factory.ResourceAllocator);
		_mainRenderer.SetQuality(new(BuiltInQualityConfiguration.Ultra));

		_scene.Add(_cameraLight);
		_scene.Add(_sunlight);

		ActiveRenderer = _mainRenderer;
	}

	public string LoadModel(string fileName) {
		UnloadModel();

		var resources = _factory.AssetLoader.LoadAll(
			CommonTestAssets.FindAsset("models/" + fileName),
			new ModelCreationConfig { MeshConfig = new() { GenerateWireframeData = true } },
			new ModelReadConfig { MeshConfig = new() { LoadSkeletalAnimationDataIfPresent = false, CorrectFlippedOrientation = true }, HandleUriEscapedStrings = true }
		);
		_loadedResources = resources;

		_cameraController.SetParametersFromBoundingBox(resources.Models.CalculateCombinedBoundingBox());

		var instances = _factory.ObjectBuilder.CreateModelInstances(resources.Models);
		_modelInstances = instances;

		// SetDefaultMaterialShadingStyle swaps each instance on to the default material, so the originals must be captured before any style is applied
		foreach (var instance in instances.Instances) _originalMaterials.Add(instance.Material);

		_scene.Add(instances);

		if (_shadingStyle != ViewerShadingStyle.Textured) ApplyShadingStyle(ColorVect.RandomOpaque());

		ResourceListing = BuildResourceListing(resources);
		return $"{fileName}: {resources.Models.Count} models / {resources.Meshes.Count} meshes / {resources.Materials.Count} materials / {resources.Textures.Count} textures";
	}

	public void UnloadModel() {
		if (_modelInstances is { } instances) {
			_scene.Remove(instances);
			instances.Dispose();
			_modelInstances = null;
		}
		if (_loadedResources is { } resources) {
			resources.Dispose();
			_loadedResources = null;
		}
		_originalMaterials.Clear();
		ResourceListing = "";
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
		if (_modelInstances is not { } instances) return;

		for (var i = 0; i < instances.Count; ++i) {
			var instance = instances[i];
			if (_shadingStyle == ViewerShadingStyle.Textured) {
				instance.SetMaterial(_originalMaterials[i]);
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
				_scene.SetBackdrop(_backdrop, _backdropIntensity);
				break;
			case ViewerBackdropMode.Color:
				_scene.SetBackdrop(StandardColor.LightingSunMidday, _backdropIntensity);
				break;
			default:
				_scene.RemoveBackdrop();
				break;
		}
	}

	public void SetQuality(BuiltInQualityConfiguration quality) => _mainRenderer.SetQuality(new(quality));

	public void SetCompositeMode(bool enabled) {
		if (enabled == (_compositor != null)) return;

		if (enabled) {
			EnsureOverlayResourcesExist();
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

	void EnsureOverlayResourcesExist() {
		if (_overlayScene != null) return;

		var overlayCamera = _factory.CameraBuilder.CreateCamera(Location.Origin);
		var overlayScene = _factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.None);
		var overlayMesh = _factory.AssetLoader.MeshBuilder.CreateMesh(Cuboid.UnitCube);
		var overlayTexture = _factory.AssetLoader.LoadColorMap(_factory.AssetLoader.BuiltInTexturePaths.White);
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
		_overlayRenderer = _factory.RendererBuilder.CreateBindableRenderer(overlayScene, overlayCamera, _factory.ResourceAllocator);
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

		if (_modelInstances is { } instances) {
			if (_spinX) instances.RotateBy((SpinDegreesPerSec * deltaTime) % Direction.Left);
			if (_spinY) instances.RotateBy((SpinDegreesPerSec * deltaTime) % Direction.Up);
			if (_spinZ) instances.RotateBy((SpinDegreesPerSec * deltaTime) % Direction.Forward);
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

			UnloadModel();

			// Scene.Dispose would do both of these itself, but doing them explicitly means a dependency tracking regression surfaces here rather than being masked
			_scene.RemoveBackdrop();
			_scene.Remove(_sunlight);
			_scene.Remove(_cameraLight);
			_scene.Dispose();

			_backdrop.Dispose();
			_sunlight.Dispose();
			_cameraLight.Dispose();
			_camera.Dispose();
		}
		finally {
			_factory.Dispose();
		}
	}
}
