// Created on 2026-07-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

sealed partial class LocalSceneBuilder {
	readonly record struct PrimitiveData(CameraLockedQuadInstance? Quad, CameraLockedTextInstance? Text, PrimitivePaintbrush Paintbrush);
	
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedMap<nuint, PrimitiveData>> _primitiveMap = new();
	readonly MapPool<nuint, PrimitiveData> _primitiveMapPool;
	QuadMesh? _primitiveSharedQuadMesh;
	ResourceGroup? _primitivePointResources;
	ResourceGroup? _primitiveStringResources;
	nuint _prevPrimitiveHandleId = 0U;

	public ScenePrimitive CreatePrimitive(ResourceHandle<Scene> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		
		var primitiveHandle = ++_prevPrimitiveHandleId;
		_primitiveMap[handle].Add(primitiveHandle, new(null, null, new()));
		return new ScenePrimitive(HandleToInstance(handle), primitiveHandle);
	}
	
	PrimitivePaintbrush DisposeExistingPrimitiveAndGetPaintbrush(ResourceHandle<Scene> handle, nuint primitiveHandle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_primitiveMap[handle].Remove(primitiveHandle, out var data)) return new();
		DisposeData(handle, in data, true);
		return data.Paintbrush;
	}
	void RegisterPrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle, in CameraLockedQuadInstance quad, in PrimitivePaintbrush paintbrush) {
		var data = new PrimitiveData(quad, null, paintbrush);
		_primitiveMap[handle].Add(primitiveHandle, data);
		Add(handle, quad);
		ApplyPaintbrush(in data, in paintbrush);
	}
	void RegisterPrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle, in CameraLockedTextInstance text, in PrimitivePaintbrush paintbrush) {
		var data = new PrimitiveData(null, text, paintbrush);
		_primitiveMap[handle].Add(primitiveHandle, data);
		Add(handle, text);
		ApplyPaintbrush(in data, in paintbrush);
	}
	
	QuadMesh GetSharedQuadMesh() {
		if (_primitiveSharedQuadMesh is { } qm) return qm;
		return (_primitiveSharedQuadMesh = _assetLoader.MeshBuilder.CreateQuadMesh(twoSided: false)).Value;
	}

	public void SetPrimitivePaintbrush(ResourceHandle<Scene> handle, nuint primitiveHandle, in PrimitivePaintbrush paintbrush) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_primitiveMap[handle].TryGetValue(primitiveHandle, out var data)) return;
		
		_primitiveMap[handle][primitiveHandle] = data with { Paintbrush = paintbrush };
		
		ApplyPaintbrush(in data, in paintbrush);
	}
	void ApplyPaintbrush(in PrimitiveData data, in PrimitivePaintbrush paintbrush) {
		if (data.Quad is { } q) {
			q.Material?.SetKeyedColor(ColorChannel.R, paintbrush.PrimaryColor);
			q.Material?.SetKeyedColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
			q.Material?.SetKeyedColor(ColorChannel.A, ColorVect.BlackTransparent);
		}
		else if (data.Text is { } t) {
			var oldPen = t.Pen;
			var newPen = paintbrush.SecondaryColor is { } sc 
				? oldPen.Font.CreatePen(paintbrush.PrimaryColor, sc, 0.5f)
				: oldPen.Font.CreatePen(paintbrush.PrimaryColor);
			t.Pen = newPen;
			oldPen.Dispose();
		}
	}

	public void SetPrimitiveGeometry(ResourceHandle<Scene> handle, nuint primitiveHandle, Location point, float size, bool constantScreenSize) {
		var paintbrush = DisposeExistingPrimitiveAndGetPaintbrush(handle, primitiveHandle);
		
		if (_primitivePointResources is not { } resources) {
			var pointTex = _assetLoader.TextureBuilder.CreateColorMap(
				TexturePattern.Circles(ColorVect.RedOpaque, ColorVect.GreenOpaque, ColorVect.BlackTransparent, interiorRadius: 128, borderSize: 48, repetitions: (1, 1)),
				includeAlpha: true,
				new TextureCreationConfig {
					IsLinearColorspace = true,
					GenerateMipMaps = true,
					Name = "Primitive Point Texture",
					RenderingConfig = new() {
						DisableTextureRepeat = true
					}
				}
			);
			var pointMat = _assetLoader.MaterialBuilder.CreateColorKeyedMaterial(
				pointTex, 
				outputIncludesAlphaChannel: true,
				name: "Primitive Point Material"
			);
			resources = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: true, initialCapacity: 2);
			resources.Add(pointTex);
			resources.Add(pointMat);
			resources.Seal();
			_primitivePointResources = resources;
		}
		
		var instance = ((IObjectBuilder) _objectBuilder).CreateCameraLockedQuadInstance(
			GetSharedQuadMesh(),
			resources.Materials[0],
			position: point,
			size: new(size),
			scalingMode: constantScreenSize ? CameraLockedScalingMode.ViewportFractionalFixedHeightPlusPreservedAspectRatio : CameraLockedScalingMode.Standard,
			lockStyle: CameraLockStyle.FaceCameraPlane,
			name: "Primitive Point"
		);
		
		RegisterPrimitive(handle, primitiveHandle, in instance, in paintbrush);
	}

	public void SetPrimitiveGeometry(ResourceHandle<Scene> handle, nuint primitiveHandle, Location position, ReadOnlySpan<char> str, float size, bool constantScreenSize) {
		var paintbrush = DisposeExistingPrimitiveAndGetPaintbrush(handle, primitiveHandle);
		
		if (_primitiveStringResources is not { } resources) {
			var font = _assetLoader.LoadFont(BuiltInFont.Default, new FontCreationConfig { Name = "Primitive Font" });
			resources = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: true, initialCapacity: 1);
			resources.Add(font);
			resources.Seal();
			_primitiveStringResources = resources;
		}
		
		var f = resources.GetNthResourceOfType<Font>(0);
		var fontString = f.CreateString(str);
		var fontPen = paintbrush.SecondaryColor is { } sc ? f.CreatePen(paintbrush.PrimaryColor, sc, 0.5f) : f.CreatePen(paintbrush.PrimaryColor);
		var instance = ((IObjectBuilder) _objectBuilder).CreateCameraLockedTextInstance(
			fontPen,
			fontString,
			position: position,
			scalingMode: constantScreenSize ? CameraLockedScalingMode.ViewportFractionalFixedHeightPlusPreservedAspectRatio : CameraLockedScalingMode.Standard,
			lockStyle: CameraLockStyle.FaceCameraPlane,
			layout: new() { Height = size, PositionAnchor = Orientation2D.None },
			name: "Primitive String"
		);
		
		RegisterPrimitive(handle, primitiveHandle, in instance, in paintbrush);
	}

	public void DisposePrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle) {
		_ = DisposeExistingPrimitiveAndGetPaintbrush(handle, primitiveHandle);
	}
	
	bool IsPrimitiveInstance(ResourceHandle<Scene> handle, ModelInstance i) {
		foreach (var kvp in _primitiveMap[handle]) {
			if ((kvp.Value.Quad?.UnderlyingQuadInstance.UnderlyingModelInstance ?? kvp.Value.Text?.UnderlyingTextInstance.UnderlyingModelInstance) == i) return true;
		}
		return false;
	}
	
	void DisposeData(ResourceHandle<Scene> handle, in PrimitiveData data, bool removeFromScene) {
		if (data.Quad is { } q) {
			if (removeFromScene) Remove(handle, q);
			q.Dispose();
		}
		else if (data.Text is { } t) {
			if (removeFromScene) Remove(handle, t);
			var str = t.String;
			var pen = t.Pen;
			t.Dispose();
			str.Dispose();
			pen.Dispose();
		}
	}
	
	void RemoveAllPrimitives(ResourceHandle<Scene> handle, bool removeFromScene) {
		foreach (var data in _primitiveMap[handle].Values) DisposeData(handle, in data, removeFromScene);
		_primitiveMap[handle].Clear();
	}
	
	void DisposePrimitiveResources() {
		_primitivePointResources?.Dispose();
		_primitiveStringResources?.Dispose();
		_primitiveSharedQuadMesh?.Dispose();
	}
}
