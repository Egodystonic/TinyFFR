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

/// <summary>
/// An <see cref="IResourceImplProvider{TResource}"/> for <see cref="Scene"/> resources.
/// </summary>
/// <remarks>
/// This also backs <see cref="CanvasScene"/> and the canvas object types, since a canvas is itself a scene.
/// </remarks>
public interface ISceneImplProvider : IDisposableResourceImplProvider<Scene> {
	/// <summary>
	/// Invoked via <see cref="Scene.Add(ModelInstance)"/>.
	/// </summary>
	void Add(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	/// <summary>
	/// Invoked via <see cref="Scene.Remove(ModelInstance)"/>.
	/// </summary>
	void Remove(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	
	/// <summary>
	/// Invoked via <see cref="Scene.Add(ModelInstanceGroup)"/>.
	/// </summary>
	void Add(ResourceHandle<Scene> handle, ModelInstanceGroup modelInstanceGroup);
	/// <summary>
	/// Invoked via <see cref="Scene.Remove(ModelInstanceGroup)"/>.
	/// </summary>
	void Remove(ResourceHandle<Scene> handle, ModelInstanceGroup modelInstanceGroup);

	/// <summary>
	/// Invoked via <see cref="Scene.Add{TLight}(TLight)"/>.
	/// </summary>
	void Add<TLight>(ResourceHandle<Scene> handle, TLight light) where TLight : ILight<TLight>;
	/// <summary>
	/// Invoked via <see cref="Scene.Remove{TLight}(TLight)"/>.
	/// </summary>
	void Remove<TLight>(ResourceHandle<Scene> handle, TLight light) where TLight : ILight<TLight>;

	/// <summary>
	/// Invoked via <see cref="Scene.Add(CameraLockedQuadInstance)"/>.
	/// </summary>
	void Add(ResourceHandle<Scene> handle, CameraLockedQuadInstance quad);
	/// <summary>
	/// Invoked via <see cref="Scene.Remove(CameraLockedQuadInstance)"/>.
	/// </summary>
	void Remove(ResourceHandle<Scene> handle, CameraLockedQuadInstance quad);

	/// <summary>
	/// Invoked via <see cref="Scene.Add(CameraLockedTextInstance)"/>.
	/// </summary>
	void Add(ResourceHandle<Scene> handle, CameraLockedTextInstance text);
	/// <summary>
	/// Invoked via <see cref="Scene.Remove(CameraLockedTextInstance)"/>.
	/// </summary>
	void Remove(ResourceHandle<Scene> handle, CameraLockedTextInstance text);

	/// <summary>
	/// Invoked via <see cref="Scene.SetBackdrop(BuiltInSceneBackdrop, float, Rotation?)"/>.
	/// </summary>
	void SetBackdrop(ResourceHandle<Scene> handle, BuiltInSceneBackdrop backdrop, float indirectLightingIntensity, Rotation rotation);
	/// <summary>
	/// Invoked via <see cref="Scene.SetBackdrop(BackdropTexture, float, Rotation?)"/>.
	/// </summary>
	void SetBackdrop(ResourceHandle<Scene> handle, BackdropTexture backdrop, float indirectLightingIntensity, Rotation rotation);
	/// <summary>
	/// Invoked via <see cref="Scene.SetBackdrop(ColorVect, float)"/>.
	/// </summary>
	void SetBackdrop(ResourceHandle<Scene> handle, ColorVect color, float indirectLightingIntensity);
	/// <summary>
	/// Invoked via <see cref="Scene.SetBackdropWithoutIndirectLighting(BackdropTexture, float, Rotation?)"/>.
	/// </summary>
	void SetBackdropWithoutIndirectLighting(ResourceHandle<Scene> handle, BackdropTexture backdrop, float backdropIntensity, Rotation rotation);
	/// <summary>
	/// Invoked via <see cref="Scene.SetBackdropWithoutIndirectLighting(ColorVect)"/>.
	/// </summary>
	void SetBackdropWithoutIndirectLighting(ResourceHandle<Scene> handle, ColorVect color);
	/// <summary>
	/// Invoked via <see cref="Scene.RemoveBackdrop"/>.
	/// </summary>
	void RemoveBackdrop(ResourceHandle<Scene> handle);

	/// <summary>
	/// Invoked via <see cref="Scene.AddFog(in FogDescriptor)"/>.
	/// </summary>
	void AddFog(ResourceHandle<Scene> handle, in FogDescriptor fogDescriptor);
	/// <summary>
	/// Invoked via <see cref="Scene.RemoveFog"/>.
	/// </summary>
	void RemoveFog(ResourceHandle<Scene> handle);

	/// <summary>
	/// Invoked via <see cref="Scene.RemoveAll"/>.
	/// </summary>
	void RemoveAll(ResourceHandle<Scene> handle, bool includeModelInstances, bool includeLights, bool includePrimitives);
	
	/// <summary>
	/// Invoked via <see cref="Scene.AddPrimitive"/>.
	/// </summary>
	ScenePrimitive CreatePrimitive(ResourceHandle<Scene> handle);
	/// <summary>
	/// Invoked via <see cref="ScenePrimitive.SetPaintbrush"/>.
	/// </summary>
	void SetPrimitivePaintbrush(ResourceHandle<Scene> handle, nuint primitiveHandle, in PrimitivePaintbrush paintbrush);
	/// <summary>
	/// Invoked via <see cref="ScenePrimitive.Dispose"/>.
	/// </summary>
	void DisposePrimitive(ResourceHandle<Scene> handle, nuint primitiveHandle);
	/// <summary>
	/// Invoked via <see cref="ScenePrimitive.SetGeometryPoint(Location, float, bool)"/>.
	/// </summary>
	void SetPrimitiveGeometryPoint(ResourceHandle<Scene> handle, nuint primitiveHandle, Location point, float size, bool constantScreenSize);
	/// <summary>
	/// Invoked via <see cref="ScenePrimitive.SetGeometryString(Location, ReadOnlySpan{char}, float, bool)"/>.
	/// </summary>
	void SetPrimitiveGeometryString(ResourceHandle<Scene> handle, nuint primitiveHandle, Location position, ReadOnlySpan<char> str, float size, bool constantScreenSize);
	/// <summary>
	/// Invoked via <see cref="ScenePrimitive.SetGeometryShape(PositionedRotatedCuboid, bool)"/>.
	/// </summary>
	void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, nuint primitiveHandle, PositionedRotatedCuboid cuboid, bool wireframe);
	/// <summary>
	/// Invoked via <see cref="ScenePrimitive.SetGeometryShape(PositionedSphere, bool)"/>.
	/// </summary>
	void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, nuint primitiveHandle, PositionedSphere sphere, bool wireframe);
	/// <summary>
	/// Invoked via <see cref="ScenePrimitive.SetGeometryShape(BoundedRay, float, bool, bool)"/>.
	/// </summary>
	void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, nuint primitiveHandle, BoundedRay ray, float size, bool includeEndpoints, bool constantScreenSize);
	/// <summary>
	/// Invoked via <see cref="ScenePrimitive.SetGeometryShape(Ray, float, bool, bool)"/>.
	/// </summary>
	void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, nuint primitiveHandle, Ray ray, float size, bool includeStartPoint, bool constantScreenSize);
	/// <summary>
	/// Invoked via <see cref="ScenePrimitive.SetGeometryShape(Line, float, bool)"/>.
	/// </summary>
	void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, nuint primitiveHandle, Line line, float size, bool constantScreenSize);
	/// <summary>
	/// Invoked via <see cref="ScenePrimitive.SetGeometryShape(Plane)"/>.
	/// </summary>
	void SetPrimitiveGeometryShape(ResourceHandle<Scene> handle, nuint primitiveHandle, Plane plane);
	/// <summary>
	/// Invoked via <see cref="ScenePrimitive.SetGeometryGrid"/>.
	/// </summary>
	void SetPrimitiveGeometryGrid(ResourceHandle<Scene> handle, nuint primitiveHandle, Location gridCentre, Direction gridNormal, Direction gridX, float gridSize, float majorGridLineSpacing, float minorGridLineSpacing);
	
	/// <summary>
	/// Invoked via <see cref="Scene.ContainedModelInstances"/>.
	/// </summary>
	IndirectEnumerable<Scene, ModelInstance> GetModelInstances(ResourceHandle<Scene> handle);
	/// <summary>
	/// Invoked via <see cref="Scene.ContainedLights"/>.
	/// </summary>
	IndirectEnumerable<Scene, Light> GetLights(ResourceHandle<Scene> handle);

	/// <summary>
	/// Invoked via <see cref="SceneQueryProvider.FindIntersections(BoundedRay, Span{ModelInstance}, float, bool)"/>.
	/// </summary>
	int FindIntersections(ResourceHandle<Scene> handle, BoundedRay ray, Span<ModelInstance> resultsDest, float rayThickness, bool disallowCachedBoundingBoxes);
	/// <summary>
	/// Invoked via <see cref="SceneQueryProvider.FindIntersections(Ray, Span{ModelInstance}, float, bool)"/>.
	/// </summary>
	int FindIntersections(ResourceHandle<Scene> handle, Ray ray, Span<ModelInstance> resultsDest, float rayThickness, bool disallowCachedBoundingBoxes);
	/// <summary>
	/// Invoked via <see cref="SceneQueryProvider.FindIntersections(PositionedRotatedCuboid, Span{ModelInstance}, bool)"/>.
	/// </summary>
	int FindIntersections(ResourceHandle<Scene> handle, PositionedRotatedCuboid shape, Span<ModelInstance> resultsDest, bool disallowCachedBoundingBoxes);
	/// <summary>
	/// Invoked via <see cref="SceneQueryProvider.FindIntersections(PositionedCuboid, Span{ModelInstance}, bool)"/>.
	/// </summary>
	int FindIntersections(ResourceHandle<Scene> handle, PositionedCuboid shape, Span<ModelInstance> resultsDest, bool disallowCachedBoundingBoxes);
	/// <summary>
	/// Invoked via <see cref="SceneQueryProvider.FindIntersections(PositionedSphere, Span{ModelInstance}, bool)"/>.
	/// </summary>
	int FindIntersections(ResourceHandle<Scene> handle, PositionedSphere shape, Span<ModelInstance> resultsDest, bool disallowCachedBoundingBoxes);

	/// <summary>
	/// Invoked via <see cref="CanvasScene.Add(Texture, ReadOnlySpan{char})"/>.
	/// </summary>
	CanvasTexture AddCanvasObject(ResourceHandle<Scene> handle, Texture texture, ReadOnlySpan<char> name);
	/// <summary>
	/// Invoked via <see cref="CanvasScene.Add(Material, ReadOnlySpan{char})"/>.
	/// </summary>
	CanvasTexture AddCanvasObject(ResourceHandle<Scene> handle, Material material, ReadOnlySpan<char> name);
	/// <summary>
	/// Invoked via <see cref="CanvasScene.Add(FontString, FontPen)"/>.
	/// </summary>
	CanvasText AddCanvasObject(ResourceHandle<Scene> handle, FontString str, FontPen pen);
	/// <summary>
	/// Invoked via <see cref="CanvasScene.Add(ReadOnlySpan{char}, FontPen, TextJustification)"/>.
	/// </summary>
	CanvasText AddCanvasObject(ResourceHandle<Scene> handle, ReadOnlySpan<char> str, FontPen pen, TextJustification multiLineJustification);
	/// <summary>
	/// Invoked via <see cref="CanvasScene.Camera"/>.
	/// </summary>
	Camera GetCanvasCamera(ResourceHandle<Scene> handle);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.CanvasAnchor"/>.
	/// </summary>
	Orientation2D GetCanvasObjectCanvasAnchor(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.CanvasAnchor"/>.
	/// </summary>
	void SetCanvasObjectCanvasAnchor(ResourceHandle<Scene> handle, ModelInstance modelInstance, Orientation2D newValue);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.ObjectAnchor"/>.
	/// </summary>
	Orientation2D? GetCanvasObjectAnchor(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.ObjectAnchor"/>.
	/// </summary>
	void SetCanvasObjectAnchor(ResourceHandle<Scene> handle, ModelInstance modelInstance, Orientation2D? newValue);
	/// <summary>
	/// Invoked via <see cref="IOriented2DSceneObject.Rotation"/>.
	/// </summary>
	Angle GetCanvasObjectRotation(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	/// <summary>
	/// Invoked via <see cref="IOriented2DSceneObject.Rotation"/>.
	/// </summary>
	void SetCanvasObjectRotation(ResourceHandle<Scene> handle, ModelInstance modelInstance, Angle newValue);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.Layer"/>.
	/// </summary>
	int GetCanvasObjectLayer(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.Layer"/>.
	/// </summary>
	void SetCanvasObjectLayer(ResourceHandle<Scene> handle, ModelInstance modelInstance, int newValue);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.IsVisible"/>.
	/// </summary>
	bool GetCanvasObjectVisibility(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.IsVisible"/>.
	/// </summary>
	void SetCanvasObjectVisibility(ResourceHandle<Scene> handle, ModelInstance modelInstance, bool newValue);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.PositionPixels"/>.
	/// </summary>
	XYPair<int> GetCanvasObjectPositionPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.PositionPixels"/>.
	/// </summary>
	void SetCanvasObjectPositionPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<int> newValue);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.PositionFraction"/>.
	/// </summary>
	XYPair<float> GetCanvasObjectPositionFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.PositionFraction"/>.
	/// </summary>
	void SetCanvasObjectPositionFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> newValue);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.WidthPixels"/>.
	/// </summary>
	int? GetCanvasObjectWidthPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.WidthPixels"/>.
	/// </summary>
	void SetCanvasObjectWidthPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance, int? newValue);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.HeightPixels"/>.
	/// </summary>
	int? GetCanvasObjectHeightPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.HeightPixels"/>.
	/// </summary>
	void SetCanvasObjectHeightPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance, int? newValue);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.WidthFraction"/>.
	/// </summary>
	float? GetCanvasObjectWidthFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.WidthFraction"/>.
	/// </summary>
	void SetCanvasObjectWidthFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, float? newValue);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.HeightFraction"/>.
	/// </summary>
	float? GetCanvasObjectHeightFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.HeightFraction"/>.
	/// </summary>
	void SetCanvasObjectHeightFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, float? newValue);
	/// <summary>
	/// Invoked via <see cref="CanvasTexture.SetPlacementPixels"/>.
	/// </summary>
	void SetCanvasObjectPlacement(ResourceHandle<Scene> handle, ModelInstance modelInstance, Orientation2D canvasAnchor, Orientation2D? objectAnchor, XYPair<int> positionPixels, XYPair<float> positionFraction, int? widthPixels, int? heightPixels, float? widthFraction, float? heightFraction);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.MoveByPixels"/>.
	/// </summary>
	void MoveCanvasObjectByPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<int> translation);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.MoveByFraction"/>.
	/// </summary>
	void MoveCanvasObjectByFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> translation);
	/// <summary>
	/// Invoked via <see cref="IRescalable2DSceneObject.ScaleBy(float)"/>.
	/// </summary>
	void ScaleCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, float scalar);
	/// <summary>
	/// Invoked via <see cref="IRescalable2DSceneObject.ScaleBy(XYPair{float})"/>.
	/// </summary>
	void ScaleCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> vect);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.AdjustScaleByPixels(XYPair{int})"/>.
	/// </summary>
	void AdjustCanvasObjectScaleByPixels(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<int> vect);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.AdjustScaleByFraction(XYPair{float})"/>.
	/// </summary>
	void AdjustCanvasObjectScaleByFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> vect);
	/// <summary>
	/// Invoked via <see cref="IReorientable2DSceneObject.RotateBy(Angle)"/>.
	/// </summary>
	void RotateCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, Angle rotation);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.RotateBy(Angle, XYPair{int})"/>.
	/// </summary>
	void RotateCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, Angle rotation, XYPair<int> pivotPointPixels);
	/// <summary>
	/// Invoked via <see cref="ITransformed2DSceneObject.RotateBy(Angle, XYPair{float})"/>.
	/// </summary>
	void RotateCanvasObjectBy(ResourceHandle<Scene> handle, ModelInstance modelInstance, Angle rotation, XYPair<float> pivotPointFraction);
	/// <summary>
	/// Invoked via <see cref="CanvasTexture.SetTexture"/>.
	/// </summary>
	void SetCanvasObjectTexture(ResourceHandle<Scene> handle, QuadInstance quad, Texture newValue);
	/// <summary>
	/// Invoked via <see cref="CanvasTexture.FillFraction"/>.
	/// </summary>
	XYPair<float> GetCanvasObjectFillFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	/// <summary>
	/// Invoked via <see cref="CanvasTexture.FillFraction"/>.
	/// </summary>
	void SetCanvasObjectFillFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<float> newValue);
	/// <summary>
	/// Invoked via <see cref="CanvasTexture.TextureDimensions"/>.
	/// </summary>
	XYPair<int> GetCanvasObjectTextureDimensions(ResourceHandle<Scene> handle, QuadInstance quad);
	/// <summary>
	/// Invoked via <see cref="CanvasTexture.TextureOffsetFraction"/>.
	/// </summary>
	XYPair<float> GetCanvasObjectTextureOffset(ResourceHandle<Scene> handle, QuadInstance quad);
	/// <summary>
	/// Invoked via <see cref="CanvasTexture.TextureOffsetFraction"/>.
	/// </summary>
	void SetCanvasObjectTextureOffset(ResourceHandle<Scene> handle, QuadInstance quad, XYPair<float> newValue);
	/// <summary>
	/// Invoked via <see cref="CanvasTexture.TextureOffsetPixels"/>.
	/// </summary>
	void SetCanvasObjectTextureOffsetPixels(ResourceHandle<Scene> handle, QuadInstance quad, XYPair<int> newValue);
	/// <summary>
	/// Invoked via <see cref="CanvasTexture.TextureExtentFraction"/>.
	/// </summary>
	XYPair<float> GetCanvasObjectTextureExtent(ResourceHandle<Scene> handle, QuadInstance quad);
	/// <summary>
	/// Invoked via <see cref="CanvasTexture.TextureExtentFraction"/>.
	/// </summary>
	void SetCanvasObjectTextureExtent(ResourceHandle<Scene> handle, QuadInstance quad, XYPair<float> newValue);
	/// <summary>
	/// Invoked via <see cref="CanvasTexture.TextureExtentPixels"/>.
	/// </summary>
	void SetCanvasObjectTextureExtentPixels(ResourceHandle<Scene> handle, QuadInstance quad, XYPair<int> newValue);
	/// <summary>
	/// Invoked via <see cref="CanvasTexture.SetBlendTexture"/>.
	/// </summary>
	void SetCanvasBlendTexture(ResourceHandle<Scene> handle, QuadInstance quad, Texture blendTexture);
	/// <summary>
	/// Invoked via <see cref="CanvasTexture.SetBlendTextureDistance"/>.
	/// </summary>
	void SetCanvasBlendTextureDistance(ResourceHandle<Scene> handle, QuadInstance quad, float distance);
	/// <summary>
	/// Invoked via <see cref="CanvasScene.ConvertRenderTargetCoordToLocal"/>.
	/// </summary>
	XYPair<int> GetCanvasPrecisePixelCoord(ResourceHandle<Scene> handle, XYPair<int> renderTargetCoord, DiagonalOrientation2D coordOrigin, bool disableDpiScalingAdjustment);
	/// <summary>
	/// Invoked via <see cref="CanvasScene.SizePixels"/>.
	/// </summary>
	XYPair<int> GetCanvasSizePixels(ResourceHandle<Scene> handle);
	/// <summary>
	/// Invoked via <see cref="CanvasScene.ConvertFractionToPixels"/>.
	/// </summary>
	XYPair<int> ConvertCanvasFractionToPixels(ResourceHandle<Scene> handle, XYPair<float> fraction);
	/// <summary>
	/// Invoked via <see cref="CanvasScene.ConvertPixelsToFraction"/>.
	/// </summary>
	XYPair<float> ConvertCanvasPixelsToFraction(ResourceHandle<Scene> handle, XYPair<int> pixels);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.Contains(XYPair{int}, DiagonalOrientation2D)"/>.
	/// </summary>
	bool CanvasObjectContainsPixelCoord(ResourceHandle<Scene> handle, ModelInstance modelInstance, XYPair<int> coord, DiagonalOrientation2D coordOrigin);
	/// <summary>
	/// Invoked via <see cref="CanvasSceneQueryProvider.FindObjectsUnderLocalCoord{TCanvasObject}"/>.
	/// </summary>
	Ray? GetCanvasQueryRay(ResourceHandle<Scene> handle, XYPair<int> localCoord, DiagonalOrientation2D coordOrigin);
	/// <summary>
	/// Invoked via <see cref="CanvasSceneQueryProvider.FindObjectsUnderLocalCoord{TCanvasObject}"/>.
	/// </summary>
	Span<ModelInstance> GetCanvasQueryScratchBuffer(ResourceHandle<Scene> handle);
	/// <summary>
	/// Invoked via <see cref="CanvasSceneQueryProvider.FindObjectsUnderLocalCoord{TCanvasObject}"/>.
	/// </summary>
	bool IsCanvasObjectOfType<TCanvasObject>(ResourceHandle<Scene> handle, ModelInstance modelInstance) where TCanvasObject : struct, ICanvasObject<TCanvasObject, ModelInstance>;
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.ActualSizePixels"/>.
	/// </summary>
	XYPair<int> GetCanvasObjectActualSizePixels(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.ActualSizeFraction"/>.
	/// </summary>
	XYPair<float> GetCanvasObjectActualSizeFraction(ResourceHandle<Scene> handle, ModelInstance modelInstance);
	/// <summary>
	/// Invoked via <see cref="ICanvasObject.SetDockParent"/>.
	/// </summary>
	void SetCanvasObjectDockParent(ResourceHandle<Scene> handle, ModelInstance modelInstance, ModelInstance? parent);
	/// <summary>
	/// Invoked via <see cref="CanvasTexture.Opacity"/>.
	/// </summary>
	float GetCanvasObjectOpacity(ResourceHandle<Scene> handle, QuadInstance quad);
	/// <summary>
	/// Invoked via <see cref="CanvasTexture.Opacity"/>.
	/// </summary>
	void SetCanvasObjectOpacity(ResourceHandle<Scene> handle, QuadInstance quad, float newValue);
	/// <summary>
	/// Invoked via <see cref="CanvasText.SetText(FontString)"/>.
	/// </summary>
	void SetCanvasTextString(ResourceHandle<Scene> handle, TextInstance text, FontString newValue);
	/// <summary>
	/// Invoked via <see cref="CanvasText.SetText(ReadOnlySpan{char}, TextJustification)"/>.
	/// </summary>
	void SetCanvasTextString(ResourceHandle<Scene> handle, TextInstance text, ReadOnlySpan<char> str, TextJustification multiLineJustification);
	/// <summary>
	/// Invoked via <see cref="CanvasText.Layout"/>.
	/// </summary>
	TextLayout GetCanvasTextLayout(ResourceHandle<Scene> handle, TextInstance text);
	/// <summary>
	/// Invoked via <see cref="CanvasText.Layout"/>.
	/// </summary>
	void SetCanvasTextLayout(ResourceHandle<Scene> handle, TextInstance text, TextLayout newValue);
	/// <summary>
	/// Invoked via <see cref="CanvasText.DisableAutomaticLineCountBasedHeightScaling"/>.
	/// </summary>
	bool GetCanvasTextAutomaticLineCountScalingDisabled(ResourceHandle<Scene> handle, TextInstance text);
	/// <summary>
	/// Invoked via <see cref="CanvasText.DisableAutomaticLineCountBasedHeightScaling"/>.
	/// </summary>
	void SetCanvasTextAutomaticLineCountScalingDisabled(ResourceHandle<Scene> handle, TextInstance text, bool newValue);
	/// <summary>
	/// Invoked internally when a canvas object is disposed.
	/// </summary>
	void DisposeCanvasObject(ResourceHandle<Scene> handle, ModelInstance modelInstance);
}