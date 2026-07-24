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
		
		if (data.Quad is { } q) {
			Remove(handle, q);
			q.Dispose();
		}
		else if (data.Text is { } t) {
			Remove(handle, t);
			t.Dispose();
		}
		
		return data.Paintbrush;
	}
	void RegisterPrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle, in CameraLockedQuadInstance quad, in PrimitivePaintbrush paintbrush) {
		_primitiveMap[handle][primitiveHandle] = new(quad, null, paintbrush);
		Add(handle, quad);
	}
	void RegisterPrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle, in CameraLockedTextInstance text, in PrimitivePaintbrush paintbrush) {
		_primitiveMap[handle][primitiveHandle] = new(null, text, paintbrush);
		Add(handle, text);
	}
	
	QuadMesh GetSharedQuadMesh() {
		if (_primitiveSharedQuadMesh is { } qm) return qm;
		return (_primitiveSharedQuadMesh = _assetLoader.MeshBuilder.CreateQuadMesh(twoSided: false)).Value;
	}

	public void SetPrimitivePaintbrush(ResourceHandle<Scene> handle, UIntPtr primitiveHandle, in PrimitivePaintbrush paintbrush) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_primitiveMap[handle].TryGetValue(primitiveHandle, out var data)) return;
		
		
		
		_primitiveMap[handle][primitiveHandle] = data with { Paintbrush = paintbrush };
	}

	public void SetPrimitiveGeometry(ResourceHandle<Scene> handle, nuint primitiveHandle, Location point) {
		var paintbrush = DisposeExistingPrimitiveAndGetPaintbrush(handle, primitiveHandle);
		
		if (_primitivePointResources is not { } resources) {
			var oneToneTex = _assetLoader.TextureBuilder.CreateColorMap(
				TexturePattern.GradientRadial(ColorVect.RedOpaque, ColorVect.BlackTransparent, fringeCorners: false, resolution: (256, 256)),
				includeAlpha: false,
				name: "Primitive Point One-Tone Key Texture"
			);
			var twoToneTex = _assetLoader.TextureBuilder.CreateColorMap(
				TexturePattern.Circles(ColorVect.RedOpaque, ColorVect.GreenOpaque, ColorVect.BlackTransparent, repetitions: (1, 1)),
				includeAlpha: false,
				name: "Primitive Point Two-Tone Key Texture"
			);
			var oneToneMat = _assetLoader.MaterialBuilder.CreateColorKeyedMaterial(
				oneToneTex, 
				outputIncludesAlphaChannel: true,
				name: "Primitive Point One-Tone Material"
			);
			var twoToneMat = _assetLoader.MaterialBuilder.CreateColorKeyedMaterial(
				twoToneTex, 
				outputIncludesAlphaChannel: true,
				name: "Primitive Point Two-Tone Material"
			);
			resources = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: true);
			resources.Add(oneToneTex);
			resources.Add(twoToneTex);
			resources.Add(oneToneMat);
			resources.Add(twoToneMat);
			resources.Seal();
			_primitivePointResources = resources;
		}
		
		var instance = ((IObjectBuilder) _objectBuilder).CreateCameraLockedQuadInstance(
			GetSharedQuadMesh(),
			paintbrush.SecondaryColor is { } sc ? resources.Materials[1] : resources.Materials[0],
			position: point,
			size: new(paintbrush.Size),
			scalingMode: CameraLockedScalingMode.ViewportFractionalFixedWidthAndHeight,
			lockStyle: CameraLockStyle.FaceCameraPlane
		);
		
		RegisterPrimitive(handle, primitiveHandle, in instance, in paintbrush);
	}

	public void DisposePrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle) {
		_ = DisposeExistingPrimitiveAndGetPaintbrush(handle, primitiveHandle);
	}
	
	void DisposePrimitiveResources() {
		_primitivePointResources?.Dispose();
		_primitiveSharedQuadMesh?.Dispose();
	}
}
