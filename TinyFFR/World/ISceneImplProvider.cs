// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using static Egodystonic.TinyFFR.World.Scene;

namespace Egodystonic.TinyFFR.World;

public interface ISceneImplProvider : IDisposableResourceImplProvider<Scene> {
	void Add(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	void Remove(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	
	void Add(ResourceHandle<Scene> handle, ModelInstanceGroup modelInstanceGroup);
	void Remove(ResourceHandle<Scene> handle, ModelInstanceGroup modelInstanceGroup);

	void Add<TLight>(ResourceHandle<Scene> handle, TLight light) where TLight : ILight<TLight>;
	void Remove<TLight>(ResourceHandle<Scene> handle, TLight light) where TLight : ILight<TLight>;

	void Add(ResourceHandle<Scene> handle, CameraLockedQuadInstance quad);
	void Remove(ResourceHandle<Scene> handle, CameraLockedQuadInstance quad);

	void Add(ResourceHandle<Scene> handle, CameraLockedTextInstance text);
	void Remove(ResourceHandle<Scene> handle, CameraLockedTextInstance text);

	void SetBackdrop(ResourceHandle<Scene> handle, BuiltInSceneBackdrop backdrop, float indirectLightingIntensity, Rotation rotation);
	void SetBackdrop(ResourceHandle<Scene> handle, BackdropTexture backdrop, float indirectLightingIntensity, Rotation rotation);
	void SetBackdrop(ResourceHandle<Scene> handle, ColorVect color, float indirectLightingIntensity);
	void SetBackdropWithoutIndirectLighting(ResourceHandle<Scene> handle, BackdropTexture backdrop, float backdropIntensity, Rotation rotation);
	void SetBackdropWithoutIndirectLighting(ResourceHandle<Scene> handle, ColorVect color);
	void RemoveBackdrop(ResourceHandle<Scene> handle);

	void AddFog(ResourceHandle<Scene> handle, in FogDescriptor fogDescriptor);
	void RemoveFog(ResourceHandle<Scene> handle);

	void RemoveAll(ResourceHandle<Scene> handle, bool includeModelInstances, bool includeLights, bool includePrimitives);
	
	ScenePrimitive CreatePrimitive(ResourceHandle<Scene> handle);
	void SetPrimitivePaintbrush(ResourceHandle<Scene> handle, nuint primitiveHandle, in PrimitivePaintbrush paintbrush);
	void DisposePrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle);
	void SetPrimitiveGeometryPoint(ResourceHandle<Scene> handle, nuint primitiveHandle, Location point, float size, bool constantScreenSize);
	void SetPrimitiveGeometryString(ResourceHandle<Scene> handle, nuint primitiveHandle, Location position, ReadOnlySpan<char> str, float size, bool constantScreenSize);
	void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, nuint primitiveHandle, PositionedRotatedCuboid cuboid, bool wireframe);
	void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, nuint primitiveHandle, PositionedSphere sphere, bool wireframe);
	void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, nuint primitiveHandle, BoundedRay ray, float size, bool includeEndpoints, bool constantScreenSize);
	void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, nuint primitiveHandle, Ray ray, float size, bool includeStartPoint, bool constantScreenSize);
	void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, nuint primitiveHandle, Line line, float size, bool constantScreenSize);
	void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, nuint primitiveHandle, Plane plane);
	void SetPrimitiveGeometryGrid(ResourceHandle<Scene> handle, nuint primitiveHandle, Location gridCentre, Direction gridNormal, Direction gridX, float gridSize, float majorGridLineSpacing, float minorGridLineSpacing);
	
	IndirectEnumerable<Scene, ModelInstance> GetModelInstances(ResourceHandle<Scene> handle);
	IndirectEnumerable<Scene, Light> GetLights(ResourceHandle<Scene> handle);

	CanvasTexture AddCanvasObject(ResourceHandle<Scene> handle, Texture texture);
	CanvasTexture AddCanvasObject(ResourceHandle<Scene> handle, Material material);
	CanvasText AddCanvasObject(ResourceHandle<Scene> handle, FontString str, FontPen pen);
	CanvasText AddCanvasObject(ResourceHandle<Scene> handle, ReadOnlySpan<char> str, FontPen pen, TextJustification multiLineJustification);
	Camera GetCanvasCamera(ResourceHandle<Scene> handle);
	Orientation2D GetCanvasObjectCanvasAnchor(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	void SetCanvasObjectCanvasAnchor(ResourceHandle<Scene> handle, ModelInstance modelInstance, Orientation2D newValue);
	Orientation2D? GetCanvasObjectAnchor(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	void SetCanvasObjectAnchor(ResourceHandle<Scene> handle, ModelInstance modelInstance, Orientation2D? newValue);
	Angle GetCanvasObjectRotation(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	void SetCanvasObjectRotation(ResourceHandle<Scene> handle, ModelInstance modelInstance, Angle newValue);
	int GetCanvasObjectLayer(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	void SetCanvasObjectLayer(ResourceHandle<Scene> handle, ModelInstance modelInstance, int newValue);
	bool GetCanvasObjectVisibility(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	void SetCanvasObjectVisibility(ResourceHandle<Scene> handle, ModelInstance modelInstance, bool newValue);
	XYPair<int> GetCanvasObjectPositionPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	void SetCanvasObjectPositionPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<int> newValue);
	XYPair<float> GetCanvasObjectPositionFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	void SetCanvasObjectPositionFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> newValue);
	int? GetCanvasObjectWidthPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	void SetCanvasObjectWidthPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance, int? newValue);
	int? GetCanvasObjectHeightPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	void SetCanvasObjectHeightPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance, int? newValue);
	float? GetCanvasObjectWidthFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	void SetCanvasObjectWidthFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, float? newValue);
	float? GetCanvasObjectHeightFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	void SetCanvasObjectHeightFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, float? newValue);
	void SetCanvasObjectPlacement(ResourceHandle<Scene> handle, ModelInstance modelInstance, Orientation2D canvasAnchor, Orientation2D? objectAnchor, XYPair<int> positionPixels, XYPair<float> positionFraction, int? widthPixels, int? heightPixels, float? widthFraction, float? heightFraction);
	void MoveCanvasObjectByPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<int> translation);
	void MoveCanvasObjectByFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> translation);
	void ScaleCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, float scalar);
	void ScaleCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> vect);
	void AdjustCanvasObjectScaleByPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<int> vect);
	void AdjustCanvasObjectScaleByFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> vect);
	void RotateCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, Angle rotation);
	void RotateCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, Angle rotation, XYPair<int> pivotPointPixels);
	void RotateCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, Angle rotation, XYPair<float> pivotPointFraction);
	void SetCanvasObjectTexture(ResourceHandle<Scene> handle, QuadInstance quad, Texture newValue);
	XYPair<float> GetCanvasObjectFillFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	void SetCanvasObjectFillFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> newValue);
	XYPair<int> GetCanvasObjectTextureDimensions(ResourceHandle<Scene> handle, QuadInstance quad);
	XYPair<float> GetCanvasObjectTextureOffset(ResourceHandle<Scene> handle, QuadInstance quad);
	void SetCanvasObjectTextureOffset(ResourceHandle<Scene> handle, QuadInstance quad, XYPair<float> newValue);
	void SetCanvasObjectTextureOffsetPixels(ResourceHandle<Scene> handle, QuadInstance quad, XYPair<int> newValue);
	XYPair<float> GetCanvasObjectTextureExtent(ResourceHandle<Scene> handle, QuadInstance quad);
	void SetCanvasObjectTextureExtent(ResourceHandle<Scene> handle, QuadInstance quad, XYPair<float> newValue);
	void SetCanvasObjectTextureExtentPixels(ResourceHandle<Scene> handle, QuadInstance quad, XYPair<int> newValue);
	void SetCanvasBlendTexture(ResourceHandle<Scene> handle, QuadInstance quad, Texture blendTexture);
	void SetCanvasBlendTextureDistance(ResourceHandle<Scene> handle, QuadInstance quad, float distance);
	XYPair<int> GetCanvasObjectActualSizePixels(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	XYPair<float> GetCanvasObjectActualSizeFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	void SetCanvasObjectDockParent(ResourceHandle<Scene> handle, ModelInstance modelInstance, ModelInstance? parent);
	float GetCanvasObjectOpacity(ResourceHandle<Scene> handle, QuadInstance quad);
	void SetCanvasObjectOpacity(ResourceHandle<Scene> handle, QuadInstance quad, float newValue);
	void SetCanvasTextString(ResourceHandle<Scene> handle, TextInstance text, FontString newValue);
	void SetCanvasTextString(ResourceHandle<Scene> handle, TextInstance text, ReadOnlySpan<char> str, TextJustification multiLineJustification);
	TextLayout GetCanvasTextLayout(ResourceHandle<Scene> handle, TextInstance text);
	void SetCanvasTextLayout(ResourceHandle<Scene> handle, TextInstance text, TextLayout newValue);
	bool GetCanvasTextAutomaticLineCountScalingDisabled(ResourceHandle<Scene> handle, TextInstance text);
	void SetCanvasTextAutomaticLineCountScalingDisabled(ResourceHandle<Scene> handle, TextInstance text, bool newValue);
	void DisposeCanvasObject(ResourceHandle<Scene> handle, ModelInstance modelInstance);
}