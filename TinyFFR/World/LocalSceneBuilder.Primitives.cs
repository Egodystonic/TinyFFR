// Created on 2026-07-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Egodystonic.TinyFFR.World;

sealed partial class LocalSceneBuilder {
	readonly record struct PrimitiveLineData(ModelInstance StartPoint, ModelInstance? EndPoint);
	readonly record struct PrimitiveData(ModelInstance? ModelInstance, TextInstance? TextInstance, PrimitiveLineData? LinePoints, PrimitivePaintbrush Paintbrush);
	
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedMap<nuint, PrimitiveData>> _primitiveMap = new();
	readonly MapPool<nuint, PrimitiveData> _primitiveMapPool;
	QuadMesh? _primitiveSharedQuadMesh;
	ResourceGroup? _primitivePointResources;
	ResourceGroup? _primitiveStringResources;
	ResourceGroup? _primitiveCuboidResources;
	ResourceGroup? _primitiveSphereResources;
	ResourceGroup? _primitiveLineResources;
	nuint _prevPrimitiveHandleId = 0U;

	public ScenePrimitive CreatePrimitive(ResourceHandle<Scene> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		
		var primitiveHandle = ++_prevPrimitiveHandleId;
		_primitiveMap[handle].Add(primitiveHandle, new(null, null, null, new()));
		return new ScenePrimitive(HandleToInstance(handle), primitiveHandle);
	}
	
	PrimitivePaintbrush DisposeExistingPrimitiveAndGetPaintbrush(ResourceHandle<Scene> handle, nuint primitiveHandle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_primitiveMap[handle].Remove(primitiveHandle, out var data)) return new();
		DisposeData(handle, in data, true);
		return data.Paintbrush;
	}
	void RegisterPrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle, in CameraLockedQuadInstance quad, in PrimitivePaintbrush paintbrush) {
		var data = new PrimitiveData(quad.UnderlyingQuadInstance.UnderlyingModelInstance, null, null, paintbrush);
		_primitiveMap[handle].Add(primitiveHandle, data);
		Add(handle, quad);
	}
	void RegisterPrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle, in CameraLockedQuadInstance quad, in CameraLockedQuadInstance lineStartPoint, in PrimitivePaintbrush paintbrush) {
		var data = new PrimitiveData(quad.UnderlyingQuadInstance.UnderlyingModelInstance, null, new PrimitiveLineData(lineStartPoint.UnderlyingQuadInstance.UnderlyingModelInstance, null), paintbrush);
		_primitiveMap[handle].Add(primitiveHandle, data);
		Add(handle, quad);
		Add(handle, lineStartPoint);
	}
	void RegisterPrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle, in CameraLockedQuadInstance quad, in CameraLockedQuadInstance lineStartPoint, in CameraLockedQuadInstance lineEndPoint, in PrimitivePaintbrush paintbrush) {
		var data = new PrimitiveData(quad.UnderlyingQuadInstance.UnderlyingModelInstance, null, new PrimitiveLineData(lineStartPoint.UnderlyingQuadInstance.UnderlyingModelInstance, lineEndPoint.UnderlyingQuadInstance.UnderlyingModelInstance), paintbrush);
		_primitiveMap[handle].Add(primitiveHandle, data);
		Add(handle, quad);
		Add(handle, lineStartPoint);
		Add(handle, lineEndPoint);
	}
	void RegisterPrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle, in ModelInstance modelInstance, in PrimitivePaintbrush paintbrush) {
		var data = new PrimitiveData(modelInstance, null, null, paintbrush);
		_primitiveMap[handle].Add(primitiveHandle, data);
		Add(handle, modelInstance);
	}
	void RegisterPrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle, in CameraLockedTextInstance text, in PrimitivePaintbrush paintbrush) {
		var data = new PrimitiveData(null, text.UnderlyingTextInstance, null, paintbrush);
		_primitiveMap[handle].Add(primitiveHandle, data);
		Add(handle, text);
	}
	
	QuadMesh GetSharedQuadMesh() {
		if (_primitiveSharedQuadMesh is { } qm) return qm;
		return (_primitiveSharedQuadMesh = _assetLoader.MeshBuilder.CreateQuadMesh(twoSided: true)).Value;
	}

	public void SetPrimitivePaintbrush(ResourceHandle<Scene> handle, nuint primitiveHandle, in PrimitivePaintbrush paintbrush) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_primitiveMap[handle].TryGetValue(primitiveHandle, out var data)) return;
		
		_primitiveMap[handle][primitiveHandle] = data with { Paintbrush = paintbrush };
		
		if (data.ModelInstance is { } mi) {
			mi.SetKeyedMaterialColor(ColorChannel.R, paintbrush.PrimaryColor);
			mi.SetKeyedMaterialColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
			mi.SetKeyedMaterialColor(ColorChannel.A, ColorVect.BlackTransparent);
			mi.SetNullMaterialBaseColor(paintbrush.PrimaryColor);
		}
		if (data.LinePoints?.StartPoint is { } lsp) {
			lsp.SetKeyedMaterialColor(ColorChannel.R, paintbrush.PrimaryColor);
			lsp.SetKeyedMaterialColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
			lsp.SetKeyedMaterialColor(ColorChannel.A, ColorVect.BlackTransparent);
			lsp.SetNullMaterialBaseColor(paintbrush.PrimaryColor);
		}
		if (data.LinePoints?.EndPoint is { } lep) {
			lep.SetKeyedMaterialColor(ColorChannel.R, paintbrush.PrimaryColor);
			lep.SetKeyedMaterialColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
			lep.SetKeyedMaterialColor(ColorChannel.A, ColorVect.BlackTransparent);
			lep.SetNullMaterialBaseColor(paintbrush.PrimaryColor);
		}
		if (data.TextInstance is { } t) {
			var oldPen = t.Pen;
			var newPen = paintbrush.SecondaryColor is { } sc 
				? oldPen.Font.CreatePen(paintbrush.PrimaryColor, sc, 0.5f)
				: oldPen.Font.CreatePen(paintbrush.PrimaryColor);
			t.Pen = newPen;
			oldPen.Dispose();
		}
	}
	
	ResourceGroup GetSharedPointResources() {
		if (_primitivePointResources != null) return _primitivePointResources.Value;
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
		_primitivePointResources = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: true, initialCapacity: 2);
		_primitivePointResources.Value.Add(pointTex);
		_primitivePointResources.Value.Add(pointMat);
		_primitivePointResources.Value.Seal();
		return _primitivePointResources.Value;
	}

	public void SetPrimitiveGeometry(ResourceHandle<Scene> handle, nuint primitiveHandle, Location point, float size, bool constantScreenSize) {
		var paintbrush = DisposeExistingPrimitiveAndGetPaintbrush(handle, primitiveHandle);
		
		var resources = GetSharedPointResources();
		
		var instance = ((IObjectBuilder) _objectBuilder).CreateCameraLockedQuadInstance(
			GetSharedQuadMesh(),
			resources.Materials[0],
			position: point,
			size: new(size),
			scalingMode: constantScreenSize ? CameraLockedScalingMode.ViewportFractionalFixedHeightPlusPreservedAspectRatio : CameraLockedScalingMode.Standard,
			lockStyle: CameraLockStyle.FaceCameraPlane,
			name: "Primitive Point"
		);
		instance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.R, paintbrush.PrimaryColor);
		instance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
		instance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.A, ColorVect.BlackTransparent);

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

	public void SetPrimitiveGeometry(ResourceHandle<Scene> handle, nuint primitiveHandle, PositionedRotatedCuboid cuboid, bool wireframe) {
		var paintbrush = DisposeExistingPrimitiveAndGetPaintbrush(handle, primitiveHandle);
		
		if (_primitiveCuboidResources is not { } resources) {
			var mesh = _assetLoader.MeshBuilder.CreateMesh(
				Cuboid.UnitCube, 
				true, 
				new MeshGenerationConfig { TextureTransform = Transform2D.None }, 
				new MeshCreationConfig { GenerateWireframeData = true, Name = "Primitive Cuboid Mesh" }
			);
			resources = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: true, initialCapacity: 1);
			resources.Add(mesh);
			resources.Seal();
			_primitiveCuboidResources = resources;
		}
		
		var instance = ((IObjectBuilder) _objectBuilder).CreateModelInstance(
			resources.Meshes[0],
			null,
			new ModelInstanceCreationConfig {
				InitialTransform = new Transform(cuboid.Position.AsVect(), cuboid.Rotation, new Vect(cuboid.ToStandardCuboid().Width, cuboid.ToStandardCuboid().Height, cuboid.ToStandardCuboid().Depth)),
				Name = "Primitive Cuboid"
			}
		);
		instance.SetNullMaterialBaseColor(paintbrush.PrimaryColor);
		instance.SetNullMaterialShadingStyle(wireframe ? NullMaterialShadingStyle.Wireframe : NullMaterialShadingStyle.Plain3D);

		RegisterPrimitive(handle, primitiveHandle, in instance, in paintbrush);
	}
	public void SetPrimitiveGeometry(ResourceHandle<Scene> handle, nuint primitiveHandle, PositionedSphere sphere, bool wireframe) {
		var paintbrush = DisposeExistingPrimitiveAndGetPaintbrush(handle, primitiveHandle);
		
		if (_primitiveSphereResources is not { } resources) {
			var solidMesh = _assetLoader.MeshBuilder.CreateMesh(
				Sphere.UnitSphere, 
				6,
				new MeshGenerationConfig { TextureTransform = Transform2D.None }, 
				new MeshCreationConfig { GenerateWireframeData = true, Name = "Primitive Sphere (Solid) Mesh" }
			);
			var wireframeMesh = _assetLoader.MeshBuilder.CreateMesh(
				Sphere.UnitSphere, 
				2,
				new MeshGenerationConfig { TextureTransform = Transform2D.None }, 
				new MeshCreationConfig { GenerateWireframeData = true, Name = "Primitive Sphere (Wireframe) Mesh" }
			);
			resources = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: true, initialCapacity: 2);
			resources.Add(solidMesh);
			resources.Add(wireframeMesh);
			resources.Seal();
			_primitiveSphereResources = resources;
		}
		
		var instance = ((IObjectBuilder) _objectBuilder).CreateModelInstance(
			wireframe ? resources.Meshes[1] : resources.Meshes[0],
			null,
			new ModelInstanceCreationConfig {
				InitialTransform = new Transform(sphere.Position.AsVect(), Rotation.None, new Vect(sphere.ToStandardSphere().Radius)),
				Name = "Primitive Sphere"
			}
		);
		instance.SetNullMaterialBaseColor(paintbrush.PrimaryColor);
		instance.SetNullMaterialShadingStyle(wireframe ? NullMaterialShadingStyle.Wireframe : NullMaterialShadingStyle.Plain3D);

		RegisterPrimitive(handle, primitiveHandle, in instance, in paintbrush);
	}
	
	ResourceGroup GetSharedLineResources() {
		if (_primitiveLineResources != null) return _primitiveLineResources.Value;
		var lineTex = _assetLoader.TextureBuilder.CreateColorMap(
			TexturePattern.Lines(
				ColorVect.GreenOpaque, 
				ColorVect.RedOpaque, 
				ColorVect.RedOpaque, 
				ColorVect.RedOpaque, 
				ColorVect.GreenOpaque,
				horizontal: false,
				numRepeats: 1,
				lineThickness: 16
			),
			includeAlpha: true,
			new TextureCreationConfig {
				IsLinearColorspace = true,
				GenerateMipMaps = true,
				Name = "Primitive Line Texture",
				RenderingConfig = new() {
					DisableTextureRepeat = true
				}
			}
		);
		var lineMat = _assetLoader.MaterialBuilder.CreateColorKeyedMaterial(
			lineTex, 
			outputIncludesAlphaChannel: true,
			name: "Primitive Line Material"
		);
		_primitiveLineResources = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: true, initialCapacity: 2);
		_primitiveLineResources.Value.Add(lineTex);
		_primitiveLineResources.Value.Add(lineMat);
		_primitiveLineResources.Value.Seal();
		return _primitiveLineResources.Value;
	}

	public void SetPrimitiveGeometry(ResourceHandle<Scene> handle, nuint primitiveHandle, BoundedRay ray, float size, bool constantScreenSize) {
		var paintbrush = DisposeExistingPrimitiveAndGetPaintbrush(handle, primitiveHandle);
		
		var resources = GetSharedLineResources();
		
		var lineInstance = ((IObjectBuilder) _objectBuilder).CreateCameraLockedQuadInstance(
			GetSharedQuadMesh(),
			resources.Materials[0],
			position: ray.MiddlePoint,
			lockedUprightDirection: ray.Direction,
			size: new(size, ray.Length),
			scalingMode: constantScreenSize ? CameraLockedScalingMode.ViewportFractionalFixedWidth : CameraLockedScalingMode.Standard,
			lockStyle: CameraLockStyle.FaceCameraPosition,
			name: "Primitive Bounded Ray"
		);
		lineInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.R, paintbrush.PrimaryColor);
		lineInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
		lineInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.A, ColorVect.BlackTransparent);
		
		var pointResources = GetSharedPointResources();
		
		var startPointInstance = ((IObjectBuilder) _objectBuilder).CreateCameraLockedQuadInstance(
			GetSharedQuadMesh(),
			pointResources.Materials[0],
			position: ray.StartPoint,
			size: new(size * ScenePrimitive.PointToLineSizeRatioReciprocal),
			scalingMode: constantScreenSize ? CameraLockedScalingMode.ViewportFractionalFixedHeightPlusPreservedAspectRatio : CameraLockedScalingMode.Standard,
			lockStyle: CameraLockStyle.FaceCameraPlane,
			name: "Primitive Bounded Ray Start Point"
		);
		startPointInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.R, paintbrush.PrimaryColor);
		startPointInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
		startPointInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.A, ColorVect.BlackTransparent);
		
		var endPointInstance = ((IObjectBuilder) _objectBuilder).CreateCameraLockedQuadInstance(
			GetSharedQuadMesh(),
			pointResources.Materials[0],
			position: ray.EndPoint,
			size: new(size * ScenePrimitive.PointToLineSizeRatioReciprocal),
			scalingMode: constantScreenSize ? CameraLockedScalingMode.ViewportFractionalFixedHeightPlusPreservedAspectRatio : CameraLockedScalingMode.Standard,
			lockStyle: CameraLockStyle.FaceCameraPlane,
			name: "Primitive Bounded Ray End Point"
		);
		endPointInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.R, paintbrush.PrimaryColor);
		endPointInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
		endPointInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.A, ColorVect.BlackTransparent);

		RegisterPrimitive(handle, primitiveHandle, in lineInstance, startPointInstance, endPointInstance, in paintbrush);
	}
	
	public void SetPrimitiveGeometry(ResourceHandle<Scene> handle, nuint primitiveHandle, Ray ray, float size, bool constantScreenSize) {
		var paintbrush = DisposeExistingPrimitiveAndGetPaintbrush(handle, primitiveHandle);
		
		var resources = GetSharedLineResources();
		
		var lineInstance = ((IObjectBuilder) _objectBuilder).CreateCameraLockedQuadInstance(
			GetSharedQuadMesh(),
			resources.Materials[0],
			position: ray.UnboundedLocationAtDistance(0.25f),
			lockedUprightDirection: ray.Direction,
			size: new(size, 0.5f),
			scalingMode: constantScreenSize ? CameraLockedScalingMode.ViewportFractionalFixedWidth : CameraLockedScalingMode.Standard,
			lockStyle: CameraLockStyle.FaceCameraPosition,
			name: "Primitive Ray"
		);
		lineInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.R, paintbrush.PrimaryColor);
		lineInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
		lineInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.A, ColorVect.BlackTransparent);
		
		var pointResources = GetSharedPointResources();
		
		var startPointInstance = ((IObjectBuilder) _objectBuilder).CreateCameraLockedQuadInstance(
			GetSharedQuadMesh(),
			pointResources.Materials[0],
			position: ray.StartPoint,
			size: new(size * ScenePrimitive.PointToLineSizeRatioReciprocal),
			scalingMode: constantScreenSize ? CameraLockedScalingMode.ViewportFractionalFixedHeightPlusPreservedAspectRatio : CameraLockedScalingMode.Standard,
			lockStyle: CameraLockStyle.FaceCameraPlane,
			name: "Primitive Ray Start Point"
		);
		startPointInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.R, paintbrush.PrimaryColor);
		startPointInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
		startPointInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.A, ColorVect.BlackTransparent);

		RegisterPrimitive(handle, primitiveHandle, in lineInstance, startPointInstance, in paintbrush);
	}
	
	public void PrepareCameraSensitivePrimitivesForRender(ResourceHandle<Scene> handle, Camera targetCamera) {
		
	}

	public void DisposePrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle) {
		_ = DisposeExistingPrimitiveAndGetPaintbrush(handle, primitiveHandle);
	}
	
	bool IsPrimitiveInstance(ResourceHandle<Scene> handle, ModelInstance i) {
		foreach (var kvp in _primitiveMap[handle]) {
			if ((kvp.Value.ModelInstance ?? kvp.Value.TextInstance?.UnderlyingModelInstance) == i) return true;
		}
		return false;
	}
	
	void DisposeData(ResourceHandle<Scene> handle, in PrimitiveData data, bool removeFromScene) {
		if (data.ModelInstance is { } mi) {
			if (removeFromScene) Remove(handle, mi);
			mi.Dispose();
		}
		if (data.LinePoints?.StartPoint is { } lsp) {
			if (removeFromScene) Remove(handle, lsp);
			lsp.Dispose();
		}
		if (data.LinePoints?.EndPoint is { } lep) {
			if (removeFromScene) Remove(handle, lep);
			lep.Dispose();
		}
		if (data.TextInstance is { } t) {
			if (removeFromScene) Remove(handle, t.UnderlyingModelInstance);
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
		_primitiveCuboidResources?.Dispose();
		_primitiveSphereResources?.Dispose();
		_primitiveLineResources?.Dispose();
		_primitiveSharedQuadMesh?.Dispose();
	}
}
