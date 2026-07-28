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
	enum PrimitiveLineExtent { Bounded, Ray, Line }
	readonly record struct PrimitiveLineBodyData(PrimitiveLineExtent Extent, Location Anchor, Direction Direction, float BoundedLength, float Width, bool ConstantScreenSize);
	readonly record struct PrimitiveLineData(ModelInstance? StartPoint, ModelInstance? EndPoint, PrimitiveLineBodyData Line);
	readonly record struct PrimitiveData(ModelInstance? ModelInstance, TextInstance? TextInstance, PrimitiveLineData? LinePoints, Plane? Plane, PrimitivePaintbrush Paintbrush) {
		public ResourceGroup? OwnedResources { get; init; }
	}
	
	const float PlaneSize = 200f;
	static readonly float[] LineBandCrossSectionXFractions = { -1f, -0.6f, -0.6f, 0.6f, 0.6f, 1f };
	static readonly float[] LineBandCrossSectionUCoords = { 0.9f, 0.9f, 0.5f, 0.5f, 0.1f, 0.1f };
	readonly ArrayPoolBackedMap<ResourceHandle<Scene>, ArrayPoolBackedMap<nuint, PrimitiveData>> _primitiveMap = new();
	readonly MapPool<nuint, PrimitiveData> _primitiveMapPool;
	QuadMesh? _primitiveSharedQuadMesh;
	Mesh? _primitiveSharedMutableLineStripMesh;
	ResourceGroup? _primitivePointResources;
	ResourceGroup? _primitiveStringResources;
	ResourceGroup? _primitiveCuboidResources;
	ResourceGroup? _primitiveSphereResources;
	ResourceGroup? _primitiveLineResources;
	ResourceGroup? _primitivePlaneResources;
	nuint _prevPrimitiveHandleId = 0U;

	public ScenePrimitive CreatePrimitive(ResourceHandle<Scene> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		
		var primitiveHandle = ++_prevPrimitiveHandleId;
		_primitiveMap[handle].Add(primitiveHandle, new(null, null, null, null, new()));
		return new ScenePrimitive(HandleToInstance(handle), primitiveHandle);
	}
	
	PrimitivePaintbrush DisposeExistingPrimitiveAndGetPaintbrush(ResourceHandle<Scene> handle, nuint primitiveHandle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_primitiveMap[handle].Remove(primitiveHandle, out var data)) return new();
		DisposeData(handle, in data, true);
		return data.Paintbrush;
	}
	void RegisterPrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle, in CameraLockedQuadInstance quad, in PrimitivePaintbrush paintbrush) {
		var data = new PrimitiveData(quad.UnderlyingQuadInstance.UnderlyingModelInstance, null, null, null, paintbrush);
		_primitiveMap[handle].Add(primitiveHandle, data);
		Add(handle, quad);
	}
	void RegisterPrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle, in ModelInstance lineBody, in PrimitiveLineBodyData lineBodyData, in CameraLockedQuadInstance startPoint, in CameraLockedQuadInstance endPoint, in PrimitivePaintbrush paintbrush) {
		var data = new PrimitiveData(
			lineBody,
			null,
			new PrimitiveLineData(startPoint.UnderlyingQuadInstance.UnderlyingModelInstance, endPoint.UnderlyingQuadInstance.UnderlyingModelInstance, lineBodyData),
			null,
			paintbrush
		);
		_primitiveMap[handle].Add(primitiveHandle, data);
		Add(handle, lineBody);
		Add(handle, startPoint);
		Add(handle, endPoint);
	}
	void RegisterPrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle, in ModelInstance lineBody, in PrimitiveLineBodyData lineBodyData, in CameraLockedQuadInstance startPoint, in PrimitivePaintbrush paintbrush) {
		var data = new PrimitiveData(
			lineBody,
			null,
			new PrimitiveLineData(startPoint.UnderlyingQuadInstance.UnderlyingModelInstance, null, lineBodyData),
			null,
			paintbrush
		);
		_primitiveMap[handle].Add(primitiveHandle, data);
		Add(handle, lineBody);
		Add(handle, startPoint);
	}
	void RegisterPrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle, in ModelInstance lineBody, in PrimitiveLineBodyData lineBodyData, in PrimitivePaintbrush paintbrush) {
		var data = new PrimitiveData(
			lineBody,
			null,
			new PrimitiveLineData(null, null, lineBodyData),
			null,
			paintbrush
		);
		_primitiveMap[handle].Add(primitiveHandle, data);
		Add(handle, lineBody);
	}
	void RegisterPrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle, in ModelInstance modelInstance, in PrimitivePaintbrush paintbrush) {
		var data = new PrimitiveData(modelInstance, null, null, null, paintbrush);
		_primitiveMap[handle].Add(primitiveHandle, data);
		Add(handle, modelInstance);
	}
	void RegisterPrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle, in ModelInstance modelInstance, Plane p, in PrimitivePaintbrush paintbrush) {
		var data = new PrimitiveData(modelInstance, null, null, p, paintbrush);
		_primitiveMap[handle].Add(primitiveHandle, data);
		Add(handle, modelInstance);
	}
	void RegisterPrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle, in ModelInstance modelInstance, ResourceGroup ownedResources, in PrimitivePaintbrush paintbrush) {
		var data = new PrimitiveData(modelInstance, null, null, null, paintbrush) { OwnedResources = ownedResources };
		_primitiveMap[handle].Add(primitiveHandle, data);
		Add(handle, modelInstance);
	}
	void RegisterPrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle, in CameraLockedTextInstance text, in PrimitivePaintbrush paintbrush) {
		var data = new PrimitiveData(null, text.UnderlyingTextInstance, null, null, paintbrush);
		_primitiveMap[handle].Add(primitiveHandle, data);
		Add(handle, text);
	}
	static bool PaintbrushRequiresBlending(in PrimitivePaintbrush paintbrush) => paintbrush.PrimaryColor.Alpha < 1f || paintbrush.SecondaryColor?.Alpha < 1f || paintbrush.TertiaryColor?.Alpha < 1f; 
	
	QuadMesh GetSharedQuadMesh() {
		if (_primitiveSharedQuadMesh is { } qm) return qm;
		return (_primitiveSharedQuadMesh = _assetLoader.MeshBuilder.CreateQuadMesh(twoSided: true)).Value;
	}

	Mesh GetSharedMutableLineStripMesh() {
		if (_primitiveSharedMutableLineStripMesh is { } m) return m;

		var xf = LineBandCrossSectionXFractions;
		var uc = LineBandCrossSectionUCoords;
		Span<MeshVertex> vertices = stackalloc MeshVertex[24]; // front bottom 0-5, front top 6-11, back bottom 12-17, back top 18-23
		Span<VertexTriangle> triangles = stackalloc VertexTriangle[12];

		for (var j = 0; j < 6; j++) {
			vertices[j] = new MeshVertex(new Location(xf[j] * 0.5f, -0.5f, 0f), new XYPair<float>(uc[j], 0f), new Direction(-1f, 0f, 0f), new Direction(0f, 1f, 0f), new Direction(0f, 0f, -1f));
			vertices[6 + j] = new MeshVertex(new Location(xf[j] * 0.5f, 0.5f, 0f), new XYPair<float>(uc[j], 1f), new Direction(-1f, 0f, 0f), new Direction(0f, 1f, 0f), new Direction(0f, 0f, -1f));
			vertices[12 + j] = new MeshVertex(new Location(xf[j] * 0.5f, -0.5f, 0f), new XYPair<float>(uc[j], 0f), new Direction(1f, 0f, 0f), new Direction(0f, 1f, 0f), new Direction(0f, 0f, 1f));
			vertices[18 + j] = new MeshVertex(new Location(xf[j] * 0.5f, 0.5f, 0f), new XYPair<float>(uc[j], 1f), new Direction(1f, 0f, 0f), new Direction(0f, 1f, 0f), new Direction(0f, 0f, 1f));
		}

		var t = 0;
		for (var b = 0; b < 3; b++) { // one quad per band: bottom pair (2b, 2b+1), top pair (6+2b, 6+2b+1)
			int bl = 2 * b, br = 2 * b + 1, tl = 6 + 2 * b, tr = 6 + 2 * b + 1;
			triangles[t++] = new VertexTriangle(br, bl, tr);
			triangles[t++] = new VertexTriangle(bl, tl, tr);
			int fbl = 12 + 2 * b, fbr = 12 + 2 * b + 1, ftl = 18 + 2 * b, ftr = 18 + 2 * b + 1;
			triangles[t++] = new VertexTriangle(fbl, fbr, ftl);
			triangles[t++] = new VertexTriangle(fbr, ftr, ftl);
		}

		return (_primitiveSharedMutableLineStripMesh = _assetLoader.MeshBuilder.CreateMesh(
			vertices,
			triangles,
			new MeshCreationConfig { AllowsPerInstanceVertexMutation = true, Name = "Primitive Line Band" }
		)).Value;
	}

	public void SetPrimitivePaintbrush(ResourceHandle<Scene> handle, nuint primitiveHandle, in PrimitivePaintbrush paintbrush) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (!_primitiveMap[handle].TryGetValue(primitiveHandle, out var data)) return;
		
		_primitiveMap[handle][primitiveHandle] = data with { Paintbrush = paintbrush };
		
		if (data.ModelInstance is { } mi) {
			if (data.LinePoints is not null) {
				mi.SetMaterial(PaintbrushRequiresBlending(in paintbrush) ? GetSharedLineResources().Materials[1] : GetSharedLineResources().Materials[0]);
			}
			mi.SetKeyedMaterialColor(ColorChannel.R, paintbrush.PrimaryColor);
			mi.SetKeyedMaterialColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
			mi.SetKeyedMaterialColor(ColorChannel.B, paintbrush.TertiaryColor ?? paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
			mi.SetKeyedMaterialColor(ColorChannel.A, ColorVect.BlackTransparent);
			mi.SetNullMaterialBaseColor(paintbrush.PrimaryColor);
		}
		if (data.LinePoints?.StartPoint is { } lsp) {
			lsp.SetKeyedMaterialColor(ColorChannel.R, paintbrush.TertiaryColor ?? paintbrush.PrimaryColor);
			lsp.SetKeyedMaterialColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
			lsp.SetKeyedMaterialColor(ColorChannel.B, paintbrush.TertiaryColor ?? paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
			lsp.SetKeyedMaterialColor(ColorChannel.A, ColorVect.BlackTransparent);
			lsp.SetNullMaterialBaseColor(paintbrush.PrimaryColor);
		}
		if (data.LinePoints?.EndPoint is { } lep) {
			lep.SetKeyedMaterialColor(ColorChannel.R, paintbrush.TertiaryColor ?? paintbrush.PrimaryColor);
			lep.SetKeyedMaterialColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
			lep.SetKeyedMaterialColor(ColorChannel.B, paintbrush.TertiaryColor ?? paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
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
			blendOutputAlphaWithScene: true,
			name: "Primitive Point Material"
		);
		_primitivePointResources = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: true, initialCapacity: 2);
		_primitivePointResources.Value.Add(pointTex);
		_primitivePointResources.Value.Add(pointMat);
		_primitivePointResources.Value.Seal();
		return _primitivePointResources.Value;
	}

	public void SetPrimitiveGeometryPoint(ResourceHandle<Scene> handle, nuint primitiveHandle, Location point, float size, bool constantScreenSize) {
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

	public void SetPrimitiveGeometryString(ResourceHandle<Scene> handle, nuint primitiveHandle, Location position, ReadOnlySpan<char> str, float size, bool constantScreenSize) {
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

	public void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, nuint primitiveHandle, PositionedRotatedCuboid cuboid, bool wireframe) {
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
	public void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, nuint primitiveHandle, PositionedSphere sphere, bool wireframe) {
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
	public void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, nuint primitiveHandle, Plane plane) {
		var paintbrush = DisposeExistingPrimitiveAndGetPaintbrush(handle, primitiveHandle);
		
		if (_primitivePlaneResources is not { } resources) {
			var mesh = _assetLoader.MeshBuilder.CreateQuadMesh(
				twoSided: true, 
				backSideInvertsTextures: false,
				new MeshGenerationConfig { TextureTransform = Transform2D.FromScalingOnly(PlaneSize) }, 
				new MeshCreationConfig { Name = "Primitive Plane Mesh" }
			);
			var tex = _assetLoader.TextureBuilder.CreateColorMap(
				TexturePattern.ChequerboardBordered(
					borderValue: ColorVect.RedOpaque,
					borderWidth: 1,
					firstValue: ColorVect.BlackTransparent,
					repetitionCount: (16, 16),
					cellResolution: 48
				),
				includeAlpha: false,
				new TextureCreationConfig { IsLinearColorspace = true, Name = "Primitive Plane Texture" }
			);
			var mat = _assetLoader.MaterialBuilder.CreateColorKeyedMaterial(tex, true, "Primitive Plane Material");
			
			resources = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: true, initialCapacity: 3);
			resources.Add(mesh.UnderlyingMesh);
			resources.Add(tex);
			resources.Add(mat);
			resources.Seal();
			_primitivePlaneResources = resources;
		}
		
		var instance = ((IObjectBuilder) _objectBuilder).CreateModelInstance(
			resources.Meshes[0],
			resources.Materials[0],
			new ModelInstanceCreationConfig {
				InitialTransform = new Transform(plane.PointClosestToOrigin.AsVect(), Direction.Backward >> plane.Normal, new Vect(PlaneSize, PlaneSize, 1f)),
				Name = "Primitive Plane"
			}
		);
		instance.SetKeyedMaterialColor(ColorChannel.R, paintbrush.PrimaryColor);
		instance.SetKeyedMaterialColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
		instance.SetKeyedMaterialColor(ColorChannel.A, ColorVect.BlackTransparent);

		RegisterPrimitive(handle, primitiveHandle, in instance, plane, in paintbrush);
	}

	public void SetPrimitiveGeometryGrid(ResourceHandle<Scene> handle, nuint primitiveHandle, Location gridCentre, Direction gridNormal, Direction gridX, float gridSize, float majorGridLineSpacing, float minorGridLineSpacing) {
		var paintbrush = DisposeExistingPrimitiveAndGetPaintbrush(handle, primitiveHandle);

		const int GridResolution = 2048;
		var tex = _assetLoader.TextureBuilder.CreateColorMap(
			TexturePattern.Grid(
				centreValue: ColorVect.RedOpaque,
				majorValue: ColorVect.GreenOpaque,
				minorValue: ColorVect.BlueOpaque,
				backgroundValue: ColorVect.BlackTransparent,
				resolution: GridResolution,
				majorFraction: majorGridLineSpacing / gridSize,
				minorFraction: minorGridLineSpacing / gridSize,
				centreLineThickness: 6,
				majorLineThickness: 4,
				minorLineThickness: 1
			),
			includeAlpha: false,
			new TextureCreationConfig { IsLinearColorspace = true, GenerateMipMaps = false, Name = "Primitive Grid Texture" }
		);
		var mat = _assetLoader.MaterialBuilder.CreateColorKeyedMaterial(tex, false, "Primitive Grid Material");

		var owned = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: true, initialCapacity: 2);
		owned.Add(tex);
		owned.Add(mat);
		owned.Seal();

		var instance = ((IObjectBuilder) _objectBuilder).CreateModelInstance(
			GetSharedQuadMesh(),
			mat,
			new ModelInstanceCreationConfig {
				InitialTransform = new Transform(gridCentre.AsVect(), Rotation.FromStartAndEndOrientation(Direction.Backward, Direction.Left, gridNormal, gridX), new Vect(gridSize, gridSize, 1f)),
				Name = "Primitive Grid"
			}
		);
		instance.SetKeyedMaterialColor(ColorChannel.R, paintbrush.PrimaryColor);
		instance.SetKeyedMaterialColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
		instance.SetKeyedMaterialColor(ColorChannel.B, paintbrush.TertiaryColor ?? paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
		instance.SetKeyedMaterialColor(ColorChannel.A, ColorVect.BlackTransparent);

		RegisterPrimitive(handle, primitiveHandle, in instance, owned, in paintbrush);
	}

	ResourceGroup GetSharedLineResources() {
		if (_primitiveLineResources != null) return _primitiveLineResources.Value;
		var lineTex = _assetLoader.TextureBuilder.CreateColorMap(
			TexturePattern.Lines(
				ColorVect.GreenOpaque,
				ColorVect.RedOpaque,
				ColorVect.RedOpaque,
				ColorVect.GreenOpaque,
				horizontal: false,
				numRepeats: 1,
				lineThickness: 2
			),
			includeAlpha: true,
			new TextureCreationConfig {
				IsLinearColorspace = true,
				GenerateMipMaps = false,
				Name = "Primitive Line Texture",
				RenderingConfig = new() {
					DisableTextureRepeat = true,
					DisableTexelBlending = true
				}
			}
		);
		var lineMatStandard = _assetLoader.MaterialBuilder.CreateColorKeyedMaterial(
			lineTex, 
			blendOutputAlphaWithScene: false,
			name: "Primitive Line (Standard) Material"
		);
		var lineMatBlended = _assetLoader.MaterialBuilder.CreateColorKeyedMaterial(
			lineTex, 
			blendOutputAlphaWithScene: true,
			name: "Primitive Line (Blended) Material"
		);
		_primitiveLineResources = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: true, initialCapacity: 3);
		_primitiveLineResources.Value.Add(lineTex);
		_primitiveLineResources.Value.Add(lineMatStandard);
		_primitiveLineResources.Value.Add(lineMatBlended);
		_primitiveLineResources.Value.Seal();
		return _primitiveLineResources.Value;
	}

	public void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, nuint primitiveHandle, BoundedRay ray, float size, bool includeEndpoints, bool constantScreenSize) {
		var paintbrush = DisposeExistingPrimitiveAndGetPaintbrush(handle, primitiveHandle);
		
		var resources = GetSharedLineResources();
		
		var lineInstance = ((IObjectBuilder) _objectBuilder).CreateModelInstance(
			GetSharedMutableLineStripMesh(),
			PaintbrushRequiresBlending(in paintbrush) ? resources.Materials[1] : resources.Materials[0],
			initialPosition: ray.MiddlePoint,
			name: "Primitive Bounded Ray"
		);
		lineInstance.SetKeyedMaterialColor(ColorChannel.R, paintbrush.PrimaryColor);
		lineInstance.SetKeyedMaterialColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
		lineInstance.SetKeyedMaterialColor(ColorChannel.A, ColorVect.BlackTransparent);
		
		if (!includeEndpoints) {
			RegisterPrimitive(handle, primitiveHandle, in lineInstance, new PrimitiveLineBodyData(PrimitiveLineExtent.Bounded, ray.StartPoint, ray.Direction, ray.Length, size, constantScreenSize), in paintbrush);
			return;
		}

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
		startPointInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.R, paintbrush.TertiaryColor ?? paintbrush.PrimaryColor);
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
		endPointInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.R, paintbrush.TertiaryColor ?? paintbrush.PrimaryColor);
		endPointInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
		endPointInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.A, ColorVect.BlackTransparent);

		RegisterPrimitive(handle, primitiveHandle, in lineInstance, new PrimitiveLineBodyData(PrimitiveLineExtent.Bounded, ray.StartPoint, ray.Direction, ray.Length, size, constantScreenSize), startPointInstance, endPointInstance, in paintbrush);
	}

	public void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, nuint primitiveHandle, Ray ray, float size, bool includeStartPoint, bool constantScreenSize) {
		var paintbrush = DisposeExistingPrimitiveAndGetPaintbrush(handle, primitiveHandle);
		
		var resources = GetSharedLineResources();
		
		var lineInstance = ((IObjectBuilder) _objectBuilder).CreateModelInstance(
			GetSharedMutableLineStripMesh(),
			PaintbrushRequiresBlending(in paintbrush) ? resources.Materials[1] : resources.Materials[0],
			initialPosition: ray.StartPoint,
			name: "Primitive Ray"
		);
		lineInstance.SetKeyedMaterialColor(ColorChannel.R, paintbrush.PrimaryColor);
		lineInstance.SetKeyedMaterialColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
		lineInstance.SetKeyedMaterialColor(ColorChannel.A, ColorVect.BlackTransparent);
		
		if (!includeStartPoint) {
			RegisterPrimitive(handle, primitiveHandle, in lineInstance, new PrimitiveLineBodyData(PrimitiveLineExtent.Ray, ray.StartPoint, ray.Direction, 0f, size, constantScreenSize), in paintbrush);
			return;
		}

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
		startPointInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.R, paintbrush.TertiaryColor ?? paintbrush.PrimaryColor);
		startPointInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
		startPointInstance.UnderlyingQuadInstance.UnderlyingModelInstance.SetKeyedMaterialColor(ColorChannel.A, ColorVect.BlackTransparent);

		RegisterPrimitive(handle, primitiveHandle, in lineInstance, new PrimitiveLineBodyData(PrimitiveLineExtent.Ray, ray.StartPoint, ray.Direction, 0f, size, constantScreenSize), startPointInstance, in paintbrush);
	}

	public void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, nuint primitiveHandle, Line line, float size, bool constantScreenSize) {
		var paintbrush = DisposeExistingPrimitiveAndGetPaintbrush(handle, primitiveHandle);

		var resources = GetSharedLineResources();

		var lineInstance = ((IObjectBuilder) _objectBuilder).CreateModelInstance(
			GetSharedMutableLineStripMesh(),
			PaintbrushRequiresBlending(in paintbrush) ? resources.Materials[1] : resources.Materials[0],
			initialPosition: line.PointOnLine,
			name: "Primitive Line"
		);
		lineInstance.SetKeyedMaterialColor(ColorChannel.R, paintbrush.PrimaryColor);
		lineInstance.SetKeyedMaterialColor(ColorChannel.G, paintbrush.SecondaryColor ?? paintbrush.PrimaryColor);
		lineInstance.SetKeyedMaterialColor(ColorChannel.A, ColorVect.BlackTransparent);

		RegisterPrimitive(handle, primitiveHandle, in lineInstance, new PrimitiveLineBodyData(PrimitiveLineExtent.Line, line.PointOnLine, line.Direction, 0f, size, constantScreenSize), in paintbrush);
	}

	public void PrepareCameraSensitivePrimitivesForRender(ResourceHandle<Scene> handle, Camera targetCamera) {
		var cameraPosition = targetCamera.Position;
		var cameraViewDirection = targetCamera.ViewDirection;
		var nearPlane = new Plane(cameraViewDirection, cameraPosition + cameraViewDirection * targetCamera.NearPlaneDistance);
		var farDistance = targetCamera.FarPlaneDistance;
		var screenScaling = new ScreenScalingContext(
			targetCamera.ProjectionType,
			MathF.Tan(targetCamera.HorizontalFieldOfView.Radians * 0.5f),
			MathF.Tan(targetCamera.VerticalFieldOfView.Radians * 0.5f),
			targetCamera.OrthographicHeight,
			targetCamera.AspectRatio,
			cameraPosition,
			cameraViewDirection
		);
		
		static BoundedRay? ComputeVisibleLineSegment<TLine>(TLine line, Location cameraPosition, float farDistance) where TLine : struct, ILineLike {
			var footDistance = line.UnboundedDistanceAtPointClosestTo(cameraPosition);
			var nearDistance = line.BindDistance(footDistance - farDistance);
			var farDistanceBound = line.BindDistance(footDistance + farDistance);
			if (farDistanceBound - nearDistance <= 0f) return null;
			return new BoundedRay(line.UnboundedLocationAtDistance(nearDistance), line.UnboundedLocationAtDistance(farDistanceBound));
		}
		
		static void UpdateLineBodyBillboard(ModelInstance bodyInstance, BoundedRay segment, float width, bool constantScreenSize, in Plane nearPlane, in ScreenScalingContext screenScaling) {
			var startInFront = nearPlane.SignedDistanceFrom(segment.StartPoint) > 0f;
			var endInFront = nearPlane.SignedDistanceFrom(segment.EndPoint) > 0f;

			if (!startInFront && !endInFront) {
				bodyInstance.SetTransform(new Transform(segment.MiddlePoint.AsVect(), Rotation.None, Vect.Zero));
				return;
			}

			var clipStart = segment.StartPoint;
			var clipEnd = segment.EndPoint;
			if (startInFront != endInFront && segment.IntersectionWith(nearPlane) is { } intersection) {
				if (!startInFront) clipStart = intersection;
				else clipEnd = intersection;
			}

			var clipped = new BoundedRay(clipStart, clipEnd);

			var facingAnchor = clipped.BoundedLocationAtDistance(clipped.BoundedDistanceAtPointClosestTo(screenScaling.CameraPosition));
			var billboard = new Transform(facingAnchor.AsVect(), Rotation.None, new Vect(1f, clipped.Length, 1f));
			if (screenScaling.ProjectionType == CameraProjectionType.Orthographic) {
				GetPlanarCylindricalCameraLockedTransform(ref billboard, Vect.Zero, -screenScaling.CameraViewDirection, segment.Direction);
			}
			else {
				GetCylindricalCameraLockedTransform(ref billboard, Vect.Zero, screenScaling.CameraPosition, segment.Direction);
			}
			billboard = billboard with { Translation = clipped.MiddlePoint.AsVect() };
			bodyInstance.SetTransform(billboard);

			var halfStart = HalfLineWidthAt(clipStart, width, constantScreenSize, in screenScaling);
			var halfEnd = HalfLineWidthAt(clipEnd, width, constantScreenSize, in screenScaling);
			var xf = LineBandCrossSectionXFractions;
			using var vertexLease = bodyInstance.BorrowVerticesSpan(recalculateBoundingBoxOnLeaseDispose: true);
			var vertices = vertexLease.Span;
			var hasBackFace = vertices.Length >= 24;
			for (var j = 0; j < 6; j++) {
				var start = new Location(xf[j] * halfStart, -0.5f, 0f);
				var end = new Location(xf[j] * halfEnd, 0.5f, 0f);
				vertices[j] = vertices[j] with { Location = start };
				vertices[6 + j] = vertices[6 + j] with { Location = end };
				if (hasBackFace) {
					vertices[12 + j] = vertices[12 + j] with { Location = start };
					vertices[18 + j] = vertices[18 + j] with { Location = end };
				}
			}
		}

		static float HalfLineWidthAt(Location end, float width, bool constantScreenSize, in ScreenScalingContext screenScaling) {
			if (!constantScreenSize) return width * 0.5f;
			if (screenScaling.ProjectionType == CameraProjectionType.Orthographic) return width * screenScaling.OrthographicHeight * screenScaling.AspectRatio * 0.5f;
			var depth = MathF.Max((end.AsVect() - screenScaling.CameraPosition.AsVect()).Dot(screenScaling.CameraViewDirection), 0f);
			return width * depth * screenScaling.HalfHorizontalFovTangent;
		}

		foreach (var data in _primitiveMap[handle].Values) {
			if (data.Plane is { } plane) {
				var orientation = Direction.Backward >> plane.Normal;
				var converter = new DimensionConverter(
					orientation.Rotate(Direction.Left),
					orientation.Rotate(Direction.Up),
					plane.Normal,
					plane.PointClosestToOrigin
				);
				var nearestPointToCamera2d = converter.ConvertLocation(plane.PointClosestTo(cameraPosition));
				data.ModelInstance?.SetPosition(converter.ConvertLocation(nearestPointToCamera2d.Round<float, float>()));
				continue;
			}
			
			if (data.LinePoints?.Line is not { } lineBody || data.ModelInstance is not { } bodyInstance) continue;

			var segment = lineBody.Extent switch {
				PrimitiveLineExtent.Line => ComputeVisibleLineSegment(new Line(lineBody.Anchor, lineBody.Direction), cameraPosition, farDistance),
				PrimitiveLineExtent.Ray => ComputeVisibleLineSegment(new Ray(lineBody.Anchor, lineBody.Direction), cameraPosition, farDistance),
				_ => ComputeVisibleLineSegment(new BoundedRay(lineBody.Anchor, lineBody.Direction * lineBody.BoundedLength), cameraPosition, farDistance)
			};
			if (segment is not { } visibleSegment) {
				bodyInstance.SetTransform(new Transform(lineBody.Anchor.AsVect(), Rotation.None, Vect.Zero));
				continue;
			}
			UpdateLineBodyBillboard(bodyInstance, visibleSegment, lineBody.Width, lineBody.ConstantScreenSize, in nearPlane, in screenScaling);
		}
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
		if (data.OwnedResources is { } owned) owned.Dispose();
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
		_primitivePlaneResources?.Dispose();
		_primitiveSharedQuadMesh?.Dispose();
		_primitiveSharedMutableLineStripMesh?.Dispose();
	}
}
