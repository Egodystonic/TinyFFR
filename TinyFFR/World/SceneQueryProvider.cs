// Created on 2026-09-06 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.World;

public readonly record struct SceneQueryProvider {
	public Scene Scene { get; init; }

	public SceneQueryProvider(Scene scene) => Scene = scene;

	public ModelInstance? GetFirstIntersection(BoundedRay ray, float rayThickness = 0f, bool disallowCachedBoundingBoxes = false) {
		Unsafe.SkipInit(out ModelInstance result);
		return FindIntersections(ray, new Span<ModelInstance>(ref result), rayThickness, disallowCachedBoundingBoxes) > 0 ? result : null;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int FindIntersections(BoundedRay ray, Span<ModelInstance> resultsDest, float rayThickness = 0f, bool disallowCachedBoundingBoxes = false) {
		return Scene.FindIntersections(ray, resultsDest, rayThickness, disallowCachedBoundingBoxes);
	}

	public ModelInstance? GetFirstIntersection(Ray ray, float rayThickness = 0f, bool disallowCachedBoundingBoxes = false) {
		Unsafe.SkipInit(out ModelInstance result);
		return FindIntersections(ray, new Span<ModelInstance>(ref result), rayThickness, disallowCachedBoundingBoxes) > 0 ? result : null;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int FindIntersections(Ray ray, Span<ModelInstance> resultsDest, float rayThickness = 0f, bool disallowCachedBoundingBoxes = false) {
		return Scene.FindIntersections(ray, resultsDest, rayThickness, disallowCachedBoundingBoxes);
	}

	public ModelInstance? GetAnyIntersection(PositionedRotatedCuboid shape, bool disallowCachedBoundingBoxes = false) {
		Unsafe.SkipInit(out ModelInstance result);
		return FindIntersections(shape, new Span<ModelInstance>(ref result), disallowCachedBoundingBoxes) > 0 ? result : null;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int FindIntersections(PositionedRotatedCuboid shape, Span<ModelInstance> resultsDest, bool disallowCachedBoundingBoxes = false) {
		return Scene.FindIntersections(shape, resultsDest, disallowCachedBoundingBoxes);
	}

	public ModelInstance? GetAnyIntersection(PositionedCuboid shape, bool disallowCachedBoundingBoxes = false) {
		Unsafe.SkipInit(out ModelInstance result);
		return FindIntersections(shape, new Span<ModelInstance>(ref result), disallowCachedBoundingBoxes) > 0 ? result : null;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int FindIntersections(PositionedCuboid shape, Span<ModelInstance> resultsDest, bool disallowCachedBoundingBoxes = false) {
		return Scene.FindIntersections(shape, resultsDest, disallowCachedBoundingBoxes);
	}

	public ModelInstance? GetAnyIntersection(PositionedSphere shape, bool disallowCachedBoundingBoxes = false) {
		Unsafe.SkipInit(out ModelInstance result);
		return FindIntersections(shape, new Span<ModelInstance>(ref result), disallowCachedBoundingBoxes) > 0 ? result : null;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int FindIntersections(PositionedSphere shape, Span<ModelInstance> resultsDest, bool disallowCachedBoundingBoxes = false) {
		return Scene.FindIntersections(shape, resultsDest, disallowCachedBoundingBoxes);
	}
}
