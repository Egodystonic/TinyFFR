// Created on 2026-08-04 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
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

	readonly record struct CanvasItemData(CanvasDock Dock, QuadInstance? Quad, TextInstance? Text) {
		public CanvasItemData(CanvasDock Dock, QuadInstance Quad) : this(Dock, Quad, null) { }
		public CanvasItemData(CanvasDock Dock, TextInstance Text) : this(Dock, null, Text) { }
		
		public ModelInstance ModelInstance => Quad?.UnderlyingModelInstance ?? Text!.Value.UnderlyingModelInstance;
	}

	readonly record struct CanvasSceneData(
		Camera AssociatedCamera,
		XYPair<int> ViewportSize,
		XYPair<int> SubAreaBottomLeft,
		XYPair<int> TargetSize
	);

	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedMap<ResourceHandle<ModelInstance>, CanvasItemData>> _canvasItemMap = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, CanvasSceneData> _canvasSceneDataMap = new();
	readonly MapPool<ResourceHandle<ModelInstance>, CanvasItemData> _canvasItemMapPool;

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

	public DiagonalOrientation2D GetCanvasOrigin(ResourceHandle<Scene> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return GetCanvasSceneData(handle).Origin;
	}

	public int GetCanvasLayerRange(ResourceHandle<Scene> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return GetCanvasSceneData(handle).LayerRange;
	}

	public XYPair<int> GetCanvasSizePixels(ResourceHandle<Scene> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return GetCanvasSceneData(handle).ViewportSize;
	}

	CanvasSceneData GetCanvasSceneData(ResourceHandle<Scene> handle) {
		if (!_canvasSceneDataMap.TryGetValue(handle, out var result)) {
			throw new InvalidOperationException($"Given {nameof(Scene)} was not created as a {nameof(CanvasScene)}.");
		}
		return result;
	}

	public void AddCanvasItem(ResourceHandle<Scene> handle, QuadInstance quad, in CanvasDock dock) {
		ThrowIfThisOrHandleIsDisposed(handle);
		AddCanvasItem(handle, new CanvasItemData(dock, quad, null));
	}
	public void AddCanvasItem(ResourceHandle<Scene> handle, TextInstance text, in CanvasDock dock) {
		ThrowIfThisOrHandleIsDisposed(handle);
		AddCanvasItem(handle, new CanvasItemData(dock, null, text));
	}
	void AddCanvasItem(ResourceHandle<Scene> handle, in CanvasItemData data) {
		_ = GetCanvasSceneData(handle);
		var modelInstance = data.ModelInstance;
		if (_canvasItemMap[handle].TryAdd(modelInstance.Handle, data)) Add(handle, modelInstance);
		else _canvasItemMap[handle][modelInstance.Handle] = data;
		PlaceCanvasItem(handle, in data);
	}

	public void SetCanvasItemDock(ResourceHandle<Scene> handle, ModelInstance modelInstance, in CanvasDock dock) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_canvasItemMap[handle].TryGetValue(modelInstance.Handle, out var data)) {
			throw new InvalidOperationException($"Given object is not a docked item within this {nameof(CanvasScene)}.");
		}
		var newData = data with { Dock = dock };
		_canvasItemMap[handle][modelInstance.Handle] = newData;
		PlaceCanvasItem(handle, in newData);
	}

	public CanvasDock GetCanvasItemDock(ResourceHandle<Scene> handle, ModelInstance modelInstance) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_canvasItemMap[handle].TryGetValue(modelInstance.Handle, out var data)) {
			throw new InvalidOperationException($"Given object is not a docked item within this {nameof(CanvasScene)}.");
		}
		return data.Dock;
	}

	public bool ContainsCanvasItem(ResourceHandle<Scene> handle, ModelInstance modelInstance) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _canvasItemMap[handle].ContainsKey(modelInstance.Handle);
	}

	void RemoveInstanceFromCanvasMaps(ResourceHandle<Scene> handle, ModelInstance modelInstance) {
		_canvasItemMap[handle].Remove(modelInstance.Handle);
	}

	public void SetUp2DItemsForRender(ResourceHandle<Scene> handle, XYPair<int> viewportSize, XYPair<int> subAreaBottomLeft, XYPair<int> targetSize) {
		if (!_canvasSceneDataMap.TryGetValue(handle, out var sceneData)) return;
		if (sceneData.ViewportSize == viewportSize && sceneData.SubAreaBottomLeft == subAreaBottomLeft && sceneData.TargetSize == targetSize) return;

		sceneData = sceneData with { ViewportSize = viewportSize, SubAreaBottomLeft = subAreaBottomLeft, TargetSize = targetSize };
		_canvasSceneDataMap[handle] = sceneData;

		foreach (var item in _canvasItemMap[handle].Values) PlaceCanvasItem(handle, in item);
	}

	void PlaceCanvasItem(ResourceHandle<Scene> handle, in CanvasItemData data) {
		var sceneData = GetCanvasSceneData(handle);
		if (sceneData.ViewportSize.X <= 0 || sceneData.ViewportSize.Y <= 0) return;

		var dock = data.Dock;
		var position = CalculateCanvasLocation(sceneData.ViewportSize, dock.CanvasAnchor, dock.ResolveOffset(sceneData.ViewportSize), dock.Layer);
		var size = dock.ResolveSize(sceneData.ViewportSize);

		if (data.Quad is { } quad) {
			quad.SetTransform(position, size, CanvasElementFacingDirection, CanvasElementUprightDirection, dock.EffectiveElementAnchor);
		}
		else {
			var text = data.Text!.Value;
			text.SetTransform(
				position,
				CanvasElementFacingDirection,
				CanvasElementUprightDirection,
				new TextMeshLayout(size.Y > 0f ? size.Y : null, dock.EffectiveElementAnchor, size.X > 0f ? size.X : null, false)
			);
		}
	}

	public static Location CalculateCanvasLocation(XYPair<int> canvasSizePixels, Orientation2D anchor, XYPair<int> anchorOffset, int layer) {
		return CreateCanvasDimensionConverter().ConvertLocation(CalculateCanvasCenterRelativeCoord(canvasSizePixels, anchor, anchorOffset), layer);
	}

	static XYPair<float> CalculateCanvasCenterRelativeCoord(XYPair<int> canvasSizePixels, Orientation2D anchor, XYPair<int> anchorOffset) {
		return UiUtils.TranslateAnchoredCanvasOffset(canvasSizePixels, DiagonalOrientation2D.None, anchor, anchorOffset).Cast<float>();
	}

	public Location GetCanvasLocation(ResourceHandle<Scene> handle, Orientation2D anchor, XYPair<int> anchorOffset, int layer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return CalculateCanvasLocation(GetCanvasSceneData(handle).ViewportSize, anchor, anchorOffset, layer);
	}

	public Location GetCanvasLocation(ResourceHandle<Scene> handle, XYPair<int> pixelCoord, int layer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var sceneData = GetCanvasSceneData(handle);
		return CalculateCanvasLocation(sceneData.ViewportSize, sceneData.Origin.AsGeneralOrientation(), pixelCoord, layer);
	}

	public XYPair<int> GetCanvasPixelCoord(ResourceHandle<Scene> handle, Location worldLocation) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var sceneData = GetCanvasSceneData(handle);
		var centerRelative = CreateCanvasDimensionConverter().ConvertLocation(worldLocation);
		var halfSize = sceneData.ViewportSize.Cast<float>().ScaledBy(0.5f);

		return new XYPair<float>(
			sceneData.Origin.GetHorizontalComponent() switch {
				HorizontalOrientation2D.Right => halfSize.X - centerRelative.X,
				HorizontalOrientation2D.Left => centerRelative.X + halfSize.X,
				_ => centerRelative.X
			},
			sceneData.Origin.GetVerticalComponent() switch {
				VerticalOrientation2D.Up => halfSize.Y - centerRelative.Y,
				VerticalOrientation2D.Down => centerRelative.Y + halfSize.Y,
				_ => centerRelative.Y
			}
		).CastWithRoundingIfNecessary<float, int>();
	}

	public XYPair<int> GetCanvasPixelCoordFromCursor(ResourceHandle<Scene> handle, XYPair<int> windowRelativeCursorPosition) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var sceneData = GetCanvasSceneData(handle);
		if (sceneData.TargetSize == sceneData.ViewportSize) return windowRelativeCursorPosition;

		var subAreaTopFromTargetTop = sceneData.TargetSize.Y - (sceneData.SubAreaBottomLeft.Y + sceneData.ViewportSize.Y);
		return new XYPair<int>(
			windowRelativeCursorPosition.X - sceneData.SubAreaBottomLeft.X,
			windowRelativeCursorPosition.Y - subAreaTopFromTargetTop
		);
	}

	public bool CanvasContains(ResourceHandle<Scene> handle, XYPair<int> pixelCoord) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var sceneData = GetCanvasSceneData(handle);
		var centerRelative = CalculateCanvasCenterRelativeCoord(sceneData.ViewportSize, sceneData.Origin.AsGeneralOrientation(), pixelCoord);
		var halfSize = sceneData.ViewportSize.Cast<float>().ScaledBy(0.5f);
		return MathF.Abs(centerRelative.X) <= halfSize.X && MathF.Abs(centerRelative.Y) <= halfSize.Y;
	}

	public XYPair<float> GetCanvasSize(ResourceHandle<Scene> handle, XYPair<float> fractionalSize) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return GetCanvasSceneData(handle).ViewportSize.Cast<float>().ScaledBy(fractionalSize);
	}
	
	void DisposeAllCanvasData(ResourceHandle<Scene> handle) {
		
	}
}
