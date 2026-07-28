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
}