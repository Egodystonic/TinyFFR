// Created on 2026-08-04 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

sealed partial class LocalSceneBuilder {
	static readonly Direction CanvasElementFacingDirection = Direction.Backward;
	static readonly Direction CanvasElementPositiveYDirection = Direction.Up;
	static readonly Direction CanvasElementPositiveXDirection = Direction.Right;
	static readonly DimensionConverter CanvasDimensionConverter = new(CanvasElementPositiveXDirection, CanvasElementPositiveYDirection, CanvasElementFacingDirection, Location.Origin);
	static readonly string[] CanvasMaterialColorMapParamNames = {
		LightingIgnoringMaterialCreationConfig.ColorMapParameterString.ToString(),
		ColorKeyedMaterialCreationConfig.KeyMapParameterString.ToString(),
		StandardMaterialCreationConfig.ColorMapParameterString.ToString(),
		TransmissiveMaterialCreationConfig.ColorMapParameterString.ToString()
	};

	readonly record struct CanvasDock {
		public Orientation2D CanvasAnchor { get; init; } = Orientation2D.None;
		public Orientation2D? ObjectAnchor { get; init; } = null;
		public Angle Rotation { get; init; } = Angle.Zero;
		public int Layer { get; init; } = CanvasScene.ZPriorityDefault;
		public XYPair<int> PixelOffset { get; init; } = XYPair<int>.Zero;
		public XYPair<float> FractionalOffset { get; init; } = XYPair<float>.Zero;
		public XYPair<int> PixelSize { get; init; } = XYPair<int>.Zero;
		public XYPair<float> FractionalSize { get; init; } = XYPair<float>.Zero;

		public CanvasDock() { }

		public Orientation2D EffectiveObjectAnchor => ObjectAnchor ?? CanvasAnchor;

		public XYPair<int> ResolveOffset(XYPair<int> canvasSizePixels) => PixelOffset + canvasSizePixels.ScaledByReal(FractionalOffset);
		public XYPair<float> ResolveSize(XYPair<int> canvasSizePixels) => PixelSize.Cast<float>() + canvasSizePixels.Cast<float>().ScaledBy(FractionalSize);
	}

	readonly record struct CanvasTextureData {
		public XYPair<int> TextureDimensions { get; init; } = XYPair<int>.One;
		public Texture? Texture { get; init; } = null;
		public Material? OwnedMaterial { get; init; } = null;
		public XYPair<float> UvOffset { get; init; } = XYPair<float>.Zero;
		public XYPair<float> UvExtent { get; init; } = new(1f);
		public Texture? BlendTexture { get; init; } = null;
		public float BlendDistance { get; init; } = 0f;

		public CanvasTextureData() { }
	}

	readonly record struct CanvasTextData {
		public bool DisableAutoHeightScaling { get; init; } = false;

		public CanvasTextData() { }
	}

	readonly record struct CanvasItemData(CanvasDock Dock, QuadInstance? Quad, TextInstance? Text) {
		public CanvasItemData(CanvasDock Dock, QuadInstance Quad) : this(Dock, Quad, null) { }
		public CanvasItemData(CanvasDock Dock, TextInstance Text) : this(Dock, null, Text) { }

		public ModelInstance ModelInstance => Quad?.UnderlyingModelInstance ?? Text!.Value.UnderlyingModelInstance;
	}

	readonly record struct CanvasSceneData(
		Camera Camera,
		XYPair<int> ViewportSize,
		XYPair<int> ViewportBottomLeftOffset,
		XYPair<int> RenderTargetSize
	);

	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedMap<ResourceHandle<ModelInstance>, CanvasItemData>> _canvasItemMap = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedMap<ResourceHandle<ModelInstance>, CanvasTextureData>> _canvasTextureDataMap = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedMap<ResourceHandle<ModelInstance>, CanvasTextData>> _canvasTextDataMap = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, CanvasSceneData> _canvasSceneDataMap = new();
	readonly MapPool<ResourceHandle<ModelInstance>, CanvasItemData> _canvasItemMapPool;
	readonly MapPool<ResourceHandle<ModelInstance>, CanvasTextureData> _canvasTextureDataMapPool;
	readonly MapPool<ResourceHandle<ModelInstance>, CanvasTextData> _canvasTextDataMapPool;

	public CanvasScene CreateCanvasScene(in CanvasSceneCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		const float CameraPositionMargin = 1f;
		const float CameraNearPlaneMargin = 0.5f;
		const float CameraFarPlaneMargin = 10f;
		var camera = _cameraBuilder.CreateCamera(new CameraCreationConfig {
			ProjectionType = CameraProjectionType.Orthographic,
			Position = CanvasDimensionConverter.ConvertLocation(XYPair<float>.Zero, CanvasScene.ZPriorityMax + CameraPositionMargin),
			ViewDirection = -CanvasElementFacingDirection,
			UpDirection = CanvasElementPositiveYDirection,
			NearPlaneDistance = 1f - CameraNearPlaneMargin,
			FarPlaneDistance = CanvasScene.ZPriorityRange + CameraFarPlaneMargin,
			Name = config.Name
		});

		var scene = CreateScene(config.ToSceneCreationConfig());
		_canvasSceneDataMap[scene.GetHandleWithoutDisposeCheck()] = new CanvasSceneData(
			camera,
			XYPair<int>.Zero,
			XYPair<int>.Zero,
			XYPair<int>.Zero
		);
		return new CanvasScene(scene);
	}

	public bool IsCanvasScene(ResourceHandle<Scene> handle) => _canvasSceneDataMap.ContainsKey(handle);

	public CanvasTexture AddCanvasObject(ResourceHandle<Scene> handle, Texture texture) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var sceneData = GetCanvasSceneData(handle);

		var material = CreateCanvasTextureMaterial(texture);
		var quad = ((IObjectBuilder) _objectBuilder).CreateQuadInstance(GetSharedQuadMesh(), material);

		_canvasTextureDataMap[handle][quad.UnderlyingModelInstance.Handle] = new CanvasTextureData { Texture = texture, OwnedMaterial = material, TextureDimensions = texture.Dimensions };
		AddCanvasItem(handle, new CanvasItemData(new CanvasDock(), quad), sceneData.ViewportSize);
		ApplyCanvasMaterialEffects(handle, quad);

		return new CanvasTexture(new CanvasScene(HandleToInstance(handle)), quad);
	}

	public CanvasTexture AddCanvasObject(ResourceHandle<Scene> handle, Material material) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var sceneData = GetCanvasSceneData(handle);

		var quad = ((IObjectBuilder) _objectBuilder).CreateQuadInstance(GetSharedQuadMesh(), material);

		_canvasTextureDataMap[handle][quad.UnderlyingModelInstance.Handle] = new CanvasTextureData { TextureDimensions = ResolveCanvasMaterialTextureDimensions(material) };
		AddCanvasItem(handle, new CanvasItemData(new CanvasDock(), quad), sceneData.ViewportSize);

		return new CanvasTexture(new CanvasScene(HandleToInstance(handle)), quad);
	}

	public CanvasText AddCanvasObject(ResourceHandle<Scene> handle, FontString str, FontPen pen) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var sceneData = GetCanvasSceneData(handle);

		var text = ((IObjectBuilder) _objectBuilder).CreateTextInstance(pen, str);

		_canvasTextDataMap[handle][text.UnderlyingModelInstance.Handle] = new CanvasTextData();
		AddCanvasItem(handle, new CanvasItemData(new CanvasDock(), text), sceneData.ViewportSize);

		return new CanvasText(new CanvasScene(HandleToInstance(handle)), text);
	}

	void AddCanvasItem(ResourceHandle<Scene> handle, in CanvasItemData itemData, XYPair<int> viewportSize) {
		var modelInstance = itemData.ModelInstance;
		_canvasItemMap[handle][modelInstance.Handle] = itemData;
		Add(handle, modelInstance);
		SetCanvasItemTransformAccordingToViewport(handle, in itemData, viewportSize);
	}

	static XYPair<int> ResolveCanvasMaterialTextureDimensions(Material material) {
		foreach (var paramName in CanvasMaterialColorMapParamNames) {
			if (material.TryGetAssociatedTexture(paramName) is { } texture) return texture.Dimensions;
		}
		return XYPair<int>.One;
	}

	Material CreateCanvasTextureMaterial(Texture texture) {
		return _assetLoader.MaterialBuilder.CreateLightingIgnoringMaterial(texture, enablePerInstanceEffects: true);
	}

	public Camera GetCanvasCamera(ResourceHandle<Scene> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return GetCanvasSceneData(handle).Camera;
	}

	public void PrepareCanvasObjectsForRender(ResourceHandle<Scene> handle, XYPair<int> viewportSize, XYPair<int> subAreaBottomLeft, XYPair<int> targetSize) {
		if (!_canvasSceneDataMap.TryGetValue(handle, out var sceneData)) return;

		var viewportAlterationDetected = sceneData.ViewportSize != viewportSize;

		sceneData = sceneData with { ViewportSize = viewportSize, ViewportBottomLeftOffset = subAreaBottomLeft, RenderTargetSize = targetSize };
		_canvasSceneDataMap[handle] = sceneData;

		if (viewportAlterationDetected) {
			sceneData.Camera.SetAspectRatio(viewportSize.Ratio ?? CameraCreationConfig.DefaultAspectRatio);
			sceneData.Camera.SetOrthographicHeight(viewportSize.Y);

			foreach (var item in _canvasItemMap[handle].Values) SetCanvasItemTransformAccordingToViewport(handle, in item, viewportSize);
		}
	}

	void SetCanvasItemTransformAccordingToViewport(ResourceHandle<Scene> handle, in CanvasItemData itemData, XYPair<int> viewportSize) {
		if (viewportSize.X <= 0 || viewportSize.Y <= 0) return;

		var dock = itemData.Dock;
		var position = CalculateCanvasLocation(viewportSize, dock.CanvasAnchor, dock.ResolveOffset(viewportSize), dock.Layer);
		var size = dock.ResolveSize(viewportSize);
		var uprightDirection = dock.Rotation == Angle.Zero
			? CanvasElementPositiveYDirection
			: CanvasElementPositiveYDirection.RotatedBy(new Rotation(dock.Rotation, CanvasElementFacingDirection));

		if (itemData.Quad is { } quad) {
			quad.SetTransform(position, size, CanvasElementFacingDirection, uprightDirection, dock.EffectiveObjectAnchor);
		}
		else {
			var text = itemData.Text!.Value;
			text.SetTransform(
				position,
				CanvasElementFacingDirection,
				uprightDirection,
				CreateCanvasTextLayout(in dock, size, GetCanvasTextData(handle, text).DisableAutoHeightScaling)
			);
		}
	}

	static TextLayout CreateCanvasTextLayout(in CanvasDock dock, XYPair<float> size, bool disableAutoHeightScaling) {
		return new TextLayout(
			size.Y > 0f ? size.Y : null,
			dock.EffectiveObjectAnchor,
			size.X > 0f ? size.X : null,
			disableAutoHeightScaling
		);
	}

	void ReapplyCanvasItemTransform(ResourceHandle<Scene> handle, ModelInstance modelInstance) {
		var itemData = GetCanvasItemData(handle, modelInstance);
		SetCanvasItemTransformAccordingToViewport(handle, in itemData, GetCanvasSceneData(handle).ViewportSize);
	}

	public XYPair<int> GetCanvasPrecisePixelCoord(ResourceHandle<Scene> handle, XYPair<int> renderTargetCoord) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var sceneData = GetCanvasSceneData(handle);

		return new XYPair<int>(
			renderTargetCoord.X - sceneData.ViewportBottomLeftOffset.X,
			renderTargetCoord.Y - (sceneData.RenderTargetSize.Y - (sceneData.ViewportBottomLeftOffset.Y + sceneData.ViewportSize.Y))
		);
	}

	public static Location CalculateCanvasLocation(XYPair<int> canvasSizePixels, Orientation2D anchor, XYPair<int> anchorOffset, int layer) {
		return CanvasDimensionConverter.ConvertLocation(CalculateCanvasCenterRelativeCoord(canvasSizePixels, anchor, anchorOffset), layer);
	}

	static XYPair<float> CalculateCanvasCenterRelativeCoord(XYPair<int> canvasSizePixels, Orientation2D anchor, XYPair<int> anchorOffset) {
		return UiUtils.TranslateAnchoredCanvasOffset(canvasSizePixels, DiagonalOrientation2D.None, anchor, anchorOffset).Cast<float>();
	}

	CanvasSceneData GetCanvasSceneData(ResourceHandle<Scene> handle) {
		if (!_canvasSceneDataMap.TryGetValue(handle, out var result)) {
			throw new InvalidOperationException($"Given {nameof(Scene)} was not created as a {nameof(CanvasScene)}.");
		}
		return result;
	}

	CanvasItemData GetCanvasItemData(ResourceHandle<Scene> handle, ModelInstance modelInstance) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_canvasItemMap[handle].TryGetValue(modelInstance.Handle, out var result)) {
			throw new InvalidOperationException($"Given object is not a docked item within this {nameof(CanvasScene)}.");
		}
		return result;
	}

	CanvasDock GetCanvasDock(ResourceHandle<Scene> handle, ModelInstance modelInstance) => GetCanvasItemData(handle, modelInstance).Dock;

	void SetCanvasDock(ResourceHandle<Scene> handle, ModelInstance modelInstance, in CanvasDock newDock) {
		var itemMap = _canvasItemMap[handle];
		var instanceHandle = modelInstance.Handle;
		if (!itemMap.TryGetValue(instanceHandle, out var itemData)) {
			throw new InvalidOperationException($"Given object is not a docked item within this {nameof(CanvasScene)}.");
		}

		var newData = itemData with { Dock = newDock };
		itemMap[instanceHandle] = newData;
		SetCanvasItemTransformAccordingToViewport(handle, in newData, GetCanvasSceneData(handle).ViewportSize);
	}

	CanvasTextureData GetCanvasTextureData(ResourceHandle<Scene> handle, QuadInstance quad) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _canvasTextureDataMap[handle].TryGetValue(quad.UnderlyingModelInstance.Handle, out var result) ? result : new CanvasTextureData();
	}

	void SetCanvasTextureData(ResourceHandle<Scene> handle, QuadInstance quad, in CanvasTextureData newData) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_canvasTextureDataMap[handle][quad.UnderlyingModelInstance.Handle] = newData;
	}

	CanvasTextData GetCanvasTextData(ResourceHandle<Scene> handle, TextInstance text) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _canvasTextDataMap[handle].TryGetValue(text.UnderlyingModelInstance.Handle, out var result) ? result : new CanvasTextData();
	}

	void SetCanvasTextData(ResourceHandle<Scene> handle, TextInstance text, in CanvasTextData newData) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_canvasTextDataMap[handle][text.UnderlyingModelInstance.Handle] = newData;
	}

	public Orientation2D GetCanvasObjectCanvasAnchor(ResourceHandle<Scene> handle, ModelInstance modelInstance) {
		return GetCanvasDock(handle, modelInstance).CanvasAnchor;
	}
	public void SetCanvasObjectCanvasAnchor(ResourceHandle<Scene> handle, ModelInstance modelInstance, Orientation2D newValue) {
		SetCanvasDock(handle, modelInstance, GetCanvasDock(handle, modelInstance) with { CanvasAnchor = newValue });
	}

	public Orientation2D? GetCanvasObjectAnchor(ResourceHandle<Scene> handle, ModelInstance modelInstance) {
		return GetCanvasDock(handle, modelInstance).ObjectAnchor;
	}
	public void SetCanvasObjectAnchor(ResourceHandle<Scene> handle, ModelInstance modelInstance, Orientation2D? newValue) {
		SetCanvasDock(handle, modelInstance, GetCanvasDock(handle, modelInstance) with { ObjectAnchor = newValue });
	}

	public Angle GetCanvasObjectRotation(ResourceHandle<Scene> handle, ModelInstance modelInstance) {
		return GetCanvasDock(handle, modelInstance).Rotation;
	}
	public void SetCanvasObjectRotation(ResourceHandle<Scene> handle, ModelInstance modelInstance, Angle newValue) {
		SetCanvasDock(handle, modelInstance, GetCanvasDock(handle, modelInstance) with { Rotation = newValue });
	}

	public int GetCanvasObjectLayer(ResourceHandle<Scene> handle, ModelInstance modelInstance) {
		return GetCanvasDock(handle, modelInstance).Layer;
	}
	public void SetCanvasObjectLayer(ResourceHandle<Scene> handle, ModelInstance modelInstance, int newValue) {
		SetCanvasDock(handle, modelInstance, GetCanvasDock(handle, modelInstance) with { Layer = newValue });
	}
	
	public bool GetCanvasObjectVisibility(ResourceHandle<Scene> handle, ModelInstance modelInstance) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _modelInstanceMap[handle].Contains(modelInstance);
	}
	public void SetCanvasObjectVisibility(ResourceHandle<Scene> handle, ModelInstance modelInstance, bool newValue) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (newValue) {
			Add(handle, modelInstance);
		}
		else {
			Remove(handle, modelInstance);
			// Remove de-links the dependency but we want to maintain it
			_globals.DependencyTracker.RegisterDependency(HandleToInstance(handle), modelInstance);
		}
	}

	public XYPair<int> GetCanvasObjectPositionPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance) {
		return GetCanvasDock(handle, modelInstance).PixelOffset;
	}
	public void SetCanvasObjectPositionPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<int> newValue) {
		SetCanvasDock(handle, modelInstance, GetCanvasDock(handle, modelInstance) with { PixelOffset = newValue });
	}

	public XYPair<float> GetCanvasObjectPositionFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance) {
		return GetCanvasDock(handle, modelInstance).FractionalOffset;
	}
	public void SetCanvasObjectPositionFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> newValue) {
		SetCanvasDock(handle, modelInstance, GetCanvasDock(handle, modelInstance) with { FractionalOffset = newValue });
	}

	public XYPair<int> GetCanvasObjectSizePixels(ResourceHandle<Scene> handle, ModelInstance modelInstance) {
		return GetCanvasDock(handle, modelInstance).PixelSize;
	}
	public void SetCanvasObjectSizePixels(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<int> newValue) {
		SetCanvasDock(handle, modelInstance, GetCanvasDock(handle, modelInstance) with { PixelSize = newValue });
	}

	public XYPair<float> GetCanvasObjectSizeFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance) {
		return GetCanvasDock(handle, modelInstance).FractionalSize;
	}
	public void SetCanvasObjectSizeFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> newValue) {
		SetCanvasDock(handle, modelInstance, GetCanvasDock(handle, modelInstance) with { FractionalSize = newValue });
	}

	public void MoveCanvasObjectByPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<int> translation) {
		var dock = GetCanvasDock(handle, modelInstance);
		SetCanvasDock(handle, modelInstance, dock with { PixelOffset = dock.PixelOffset + translation });
	}
	public void MoveCanvasObjectByFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> translation) {
		var dock = GetCanvasDock(handle, modelInstance);
		SetCanvasDock(handle, modelInstance, dock with { FractionalOffset = dock.FractionalOffset + translation });
	}

	public void ScaleCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, float scalar) {
		ScaleCanvasObjectBy(handle, modelInstance, new XYPair<float>(scalar));
	}
	public void ScaleCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> vect) {
		var dock = GetCanvasDock(handle, modelInstance);
		SetCanvasDock(handle, modelInstance, dock with {
			PixelSize = dock.PixelSize.ScaledByReal(vect),
			FractionalSize = dock.FractionalSize.ScaledBy(vect)
		});
	}

	public void AdjustCanvasObjectScaleByPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<int> vect) {
		var dock = GetCanvasDock(handle, modelInstance);
		SetCanvasDock(handle, modelInstance, dock with { PixelSize = dock.PixelSize + vect });
	}
	public void AdjustCanvasObjectScaleByFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> vect) {
		var dock = GetCanvasDock(handle, modelInstance);
		SetCanvasDock(handle, modelInstance, dock with { FractionalSize = dock.FractionalSize + vect });
	}

	public void RotateCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, Angle rotation) {
		var dock = GetCanvasDock(handle, modelInstance);
		SetCanvasDock(handle, modelInstance, dock with { Rotation = dock.Rotation + rotation });
	}
	public void RotateCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, Angle rotation, XYPair<int> pivotPointPixels) {
		var dock = GetCanvasDock(handle, modelInstance);
		SetCanvasDock(handle, modelInstance, dock with {
			Rotation = dock.Rotation + rotation,
			PixelOffset = dock.PixelOffset.RotatedBy(rotation, pivotPointPixels)
		});
	}
	public void RotateCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, Angle rotation, XYPair<float> pivotPointFraction) {
		var dock = GetCanvasDock(handle, modelInstance);
		SetCanvasDock(handle, modelInstance, dock with {
			Rotation = dock.Rotation + rotation,
			FractionalOffset = dock.FractionalOffset.RotatedBy(rotation, pivotPointFraction)
		});
	}

	public XYPair<int> GetCanvasObjectTextureDimensions(ResourceHandle<Scene> handle, QuadInstance quad) {
		return GetCanvasTextureData(handle, quad).TextureDimensions;
	}
	public void SetCanvasObjectTexture(ResourceHandle<Scene> handle, QuadInstance quad, Texture newValue) {
		var data = GetCanvasTextureData(handle, quad);
		var newMaterial = CreateCanvasTextureMaterial(newValue);
		quad.SetMaterial(newMaterial);
		if (data.OwnedMaterial is { } previousMaterial) previousMaterial.Dispose();
		SetCanvasTextureData(handle, quad, data with { Texture = newValue, OwnedMaterial = newMaterial, TextureDimensions = newValue.Dimensions });
		ApplyCanvasMaterialEffects(handle, quad);
	}

	public XYPair<float> GetCanvasObjectTextureOffset(ResourceHandle<Scene> handle, QuadInstance quad) {
		return GetCanvasTextureData(handle, quad).UvOffset;
	}
	public void SetCanvasObjectTextureOffset(ResourceHandle<Scene> handle, QuadInstance quad, XYPair<float> newValue) {
		SetCanvasTextureData(handle, quad, GetCanvasTextureData(handle, quad) with { UvOffset = newValue });
		ApplyCanvasMaterialEffects(handle, quad);
	}
	public void SetCanvasObjectTextureOffsetPixels(ResourceHandle<Scene> handle, QuadInstance quad, XYPair<int> newValue) {
		SetCanvasObjectTextureOffset(handle, quad, NormalizeCanvasTexturePixels(handle, quad, newValue));
	}

	public XYPair<float> GetCanvasObjectTextureExtent(ResourceHandle<Scene> handle, QuadInstance quad) {
		return GetCanvasTextureData(handle, quad).UvExtent;
	}
	public void SetCanvasObjectTextureExtent(ResourceHandle<Scene> handle, QuadInstance quad, XYPair<float> newValue) {
		SetCanvasTextureData(handle, quad, GetCanvasTextureData(handle, quad) with { UvExtent = newValue });
		ApplyCanvasMaterialEffects(handle, quad);
	}
	public void SetCanvasObjectTextureExtentPixels(ResourceHandle<Scene> handle, QuadInstance quad, XYPair<int> newValue) {
		SetCanvasObjectTextureExtent(handle, quad, NormalizeCanvasTexturePixels(handle, quad, newValue));
	}

	XYPair<float> NormalizeCanvasTexturePixels(ResourceHandle<Scene> handle, QuadInstance quad, XYPair<int> pixels) {
		return pixels.Cast<float>() / GetCanvasObjectTextureDimensions(handle, quad).Cast<float>();
	}

	public void SetCanvasBlendTexture(ResourceHandle<Scene> handle, QuadInstance quad, Texture blendTexture) {
		SetCanvasTextureData(handle, quad, GetCanvasTextureData(handle, quad) with { BlendTexture = blendTexture });
		ApplyCanvasMaterialEffects(handle, quad);
	}

	public void SetCanvasBlendTextureDistance(ResourceHandle<Scene> handle, QuadInstance quad, float distance) {
		SetCanvasTextureData(handle, quad, GetCanvasTextureData(handle, quad) with { BlendDistance = distance });
		ApplyCanvasMaterialEffects(handle, quad);
	}

	void ApplyCanvasMaterialEffects(ResourceHandle<Scene> handle, QuadInstance quad) {
		var data = GetCanvasTextureData(handle, quad);
		var instanceHandle = quad.UnderlyingModelInstance.Handle;

		_objectBuilder.SetMaterialEffectTransform(instanceHandle, new Transform2D(data.UvOffset, Angle.Zero, data.UvExtent));
		if (data.BlendTexture is { } blendTexture) {
			_objectBuilder.SetMaterialEffectBlendTexture(instanceHandle, MaterialEffectMapType.Color, blendTexture);
		}
		_objectBuilder.SetMaterialEffectBlendDistance(instanceHandle, MaterialEffectMapType.Color, data.BlendDistance);
	}

	public FontString GetCanvasTextString(ResourceHandle<Scene> handle, TextInstance text) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return text.String;
	}
	public void SetCanvasTextString(ResourceHandle<Scene> handle, TextInstance text, FontString newValue) {
		ThrowIfThisOrHandleIsDisposed(handle);
		text.String = newValue;
		ReapplyCanvasItemTransform(handle, text.UnderlyingModelInstance);
	}

	public TextLayout GetCanvasTextLayout(ResourceHandle<Scene> handle, TextInstance text) {
		var dock = GetCanvasDock(handle, text.UnderlyingModelInstance);
		return CreateCanvasTextLayout(
			in dock,
			dock.ResolveSize(GetCanvasSceneData(handle).ViewportSize),
			GetCanvasTextData(handle, text).DisableAutoHeightScaling
		);
	}
	public void SetCanvasTextLayout(ResourceHandle<Scene> handle, TextInstance text, TextLayout newValue) {
		SetCanvasTextData(handle, text, GetCanvasTextData(handle, text) with { DisableAutoHeightScaling = newValue.DisableAutomaticLineCountBasedHeightScaling });
		SetCanvasDock(handle, text.UnderlyingModelInstance, GetCanvasDock(handle, text.UnderlyingModelInstance) with {
			ObjectAnchor = newValue.PositionAnchor,
			PixelSize = new XYPair<int>((int) (newValue.Width ?? 0f), (int) (newValue.Height ?? 0f)),
			FractionalSize = XYPair<float>.Zero
		});
	}

	public bool GetCanvasTextAutomaticLineCountScalingDisabled(ResourceHandle<Scene> handle, TextInstance text) {
		return GetCanvasTextData(handle, text).DisableAutoHeightScaling;
	}
	public void SetCanvasTextAutomaticLineCountScalingDisabled(ResourceHandle<Scene> handle, TextInstance text, bool newValue) {
		SetCanvasTextData(handle, text, GetCanvasTextData(handle, text) with { DisableAutoHeightScaling = newValue });
		ReapplyCanvasItemTransform(handle, text.UnderlyingModelInstance);
	}

	public void DisposeCanvasObject(ResourceHandle<Scene> handle, ModelInstance modelInstance) {
		ThrowIfThisOrHandleIsDisposed(handle);
		DisposeCanvasObject(handle, modelInstance, true);
	}

	void DisposeCanvasObject(ResourceHandle<Scene> handle, ModelInstance modelInstance, bool detachFromScene) {
		var instanceHandle = modelInstance.GetHandleWithoutDisposeCheck();

		Material? ownedMaterial = null;
		if (_canvasTextureDataMap.TryGetValue(handle, out var textureDataMap) && textureDataMap.TryGetValue(instanceHandle, out var textureData)) {
			ownedMaterial = textureData.OwnedMaterial;
			textureDataMap.Remove(instanceHandle);
		}
		if (_canvasTextDataMap.TryGetValue(handle, out var textDataMap)) textDataMap.Remove(instanceHandle);

		if (detachFromScene) {
			if (_canvasItemMap.TryGetValue(handle, out var canvasItemMap)) canvasItemMap.Remove(instanceHandle);
			Remove(handle, modelInstance);
			_globals.DependencyTracker.DeregisterDependency(HandleToInstance(handle), modelInstance);
		}

		modelInstance.Dispose();
		ownedMaterial?.Dispose();
	}

	void DisposeAllCanvasItems(ResourceHandle<Scene> handle) {
		if (!_canvasItemMap.TryGetValue(handle, out var canvasItemMap)) return;

		foreach (var data in canvasItemMap.Values) DisposeCanvasObject(handle, data.ModelInstance, false);

		canvasItemMap.Clear();
		if (_canvasTextureDataMap.TryGetValue(handle, out var textureDataMap)) textureDataMap.Clear();
		if (_canvasTextDataMap.TryGetValue(handle, out var textDataMap)) textDataMap.Clear();
	}

	void DisposeAllCanvasData(ResourceHandle<Scene> handle) {
		DisposeAllCanvasItems(handle);
		if (_canvasSceneDataMap.TryGetValue(handle, out var sceneData)) sceneData.Camera.Dispose();

		_canvasItemMapPool.Return(_canvasItemMap[handle]);
		_canvasItemMap.Remove(handle);
		_canvasTextureDataMapPool.Return(_canvasTextureDataMap[handle]);
		_canvasTextureDataMap.Remove(handle);
		_canvasTextDataMapPool.Return(_canvasTextDataMap[handle]);
		_canvasTextDataMap.Remove(handle);
		_canvasSceneDataMap.Remove(handle);
	}
	
	void DisposeAllCanvasResources() {
		_canvasItemMap.Dispose();
		_canvasTextureDataMap.Dispose();
		_canvasTextDataMap.Dispose();
		_canvasSceneDataMap.Dispose();
		_canvasItemMapPool.Dispose();
		_canvasTextureDataMapPool.Dispose();
		_canvasTextDataMapPool.Dispose();
	}
}
