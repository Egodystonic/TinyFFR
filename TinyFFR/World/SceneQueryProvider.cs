// Created on 2026-09-06 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Rendering;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Answers questions about what is where in a <see cref="World.Scene"/>, such as which object a ray strikes.
/// </summary>
/// <remarks>
/// <para>
/// Obtained from <see cref="World.Scene.QueryProvider"/>. Two facts govern every query here. First, objects are tested by their <i>bounding volumes</i> rather than
/// their actual triangles, so a hit means "the ray passed through the box enclosing this object", which for an irregularly-shaped object is a looser answer than it
/// may appear. Second, debug primitives are excluded from every query, so a line drawn to visualise a ray does not itself register as a hit along that ray.
/// </para>
/// <para>
/// If you want actual vertex-perfect hit-detection, you must implement that yourself (or use <see cref="Renderer.PickModelInstanceFromRenderSurface"/>).
/// </para>
/// <para>
/// Canvas objects and camera-locked objects are <i>not</i> excluded, so querying a canvas scene will return them. To hit-test a canvas in pixel terms, use
/// <see cref="CanvasScene.QueryProvider"/> instead.
/// </para>
/// </remarks>
public readonly record struct SceneQueryProvider {
	/// <summary>
	/// The scene these queries run against.
	/// </summary>
	public Scene Scene { get; init; }

	/// <summary>
	/// Constructs a new <see cref="SceneQueryProvider"/> for the given scene.
	/// </summary>
	/// <remarks>
	/// Usually obtained from <see cref="World.Scene.QueryProvider"/> rather than constructed directly.
	/// </remarks>
	/// <param name="scene">The scene to query.</param>
	public SceneQueryProvider(Scene scene) => Scene = scene;

	/// <summary>
	/// Returns the nearest object the given ray strikes, or <see langword="null"/> if it strikes nothing.
	/// </summary>
	/// <remarks>
	/// "Nearest" is measured from the start of the ray. The ray is limited to its own length, so objects beyond its end point are not reported. An object containing the ray's start point counts as nearest of all. Note that this still has to
	/// test every object in the scene in order to establish which is nearest, so it is no cheaper than asking for all of them.
	/// </remarks>
	/// <param name="ray">The ray to test against the scene.</param>
	/// <param name="rayThickness">Optionally sweeps the ray in to a cylinder of this radius, so that objects the ray passes close to are also reported. Defaults to <c>0f</c>, i.e. an infinitely thin ray.</param>
	/// <param name="disallowCachedBoundingBoxes">Pass <see langword="true"/> for objects whose geometry has been altered since it was loaded, whose cached bounds may therefore be out of date. This makes the query slower but accurate for such objects. Defaults to <see langword="false"/>.</param>
	public ModelInstance? GetFirstIntersection(BoundedRay ray, float rayThickness = 0f, bool disallowCachedBoundingBoxes = false) {
		Unsafe.SkipInit(out ModelInstance result);
		return FindIntersections(ray, new Span<ModelInstance>(ref result), rayThickness, disallowCachedBoundingBoxes) > 0 ? result : null;
	}
	/// <summary>
	/// Writes every object the given ray strikes in to <paramref name="resultsDest"/>, nearest first, and returns how many were written.
	/// </summary>
	/// <remarks>
	/// The return value is the number of objects <i>written</i>, which is capped at the length of <paramref name="resultsDest"/>. It is not the total number of
	/// objects hit. A caller therefore cannot tell from the result alone whether the buffer was too small, so size it generously if that matters.
	/// </remarks>
	/// <param name="ray">The ray to test against the scene.</param>
	/// <param name="resultsDest">The buffer to write the results in to. Results are ordered nearest-first.</param>
	/// <param name="rayThickness">Optionally sweeps the ray in to a cylinder of this radius, so that objects the ray passes close to are also reported. Defaults to <c>0f</c>, i.e. an infinitely thin ray.</param>
	/// <param name="disallowCachedBoundingBoxes">Pass <see langword="true"/> for objects whose geometry has been altered since it was loaded, whose cached bounds may therefore be out of date. This makes the query slower but accurate for such objects. Defaults to <see langword="false"/>.</param>
	/// <returns>The number of objects written to <paramref name="resultsDest"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int FindIntersections(BoundedRay ray, Span<ModelInstance> resultsDest, float rayThickness = 0f, bool disallowCachedBoundingBoxes = false) {
		return Scene.FindIntersections(ray, resultsDest, rayThickness, disallowCachedBoundingBoxes);
	}

	/// <summary>
	/// Returns the nearest object the given ray strikes, or <see langword="null"/> if it strikes nothing.
	/// </summary>
	/// <remarks>
	/// "Nearest" is measured from the start of the ray. The ray continues indefinitely. An object containing the ray's start point counts as nearest of all. Note that this still has to
	/// test every object in the scene in order to establish which is nearest, so it is no cheaper than asking for all of them.
	/// </remarks>
	/// <param name="ray">The ray to test against the scene.</param>
	/// <param name="rayThickness">Optionally sweeps the ray in to a cylinder of this radius, so that objects the ray passes close to are also reported. Defaults to <c>0f</c>, i.e. an infinitely thin ray.</param>
	/// <param name="disallowCachedBoundingBoxes">Pass <see langword="true"/> for objects whose geometry has been altered since it was loaded, whose cached bounds may therefore be out of date. This makes the query slower but accurate for such objects. Defaults to <see langword="false"/>.</param>
	public ModelInstance? GetFirstIntersection(Ray ray, float rayThickness = 0f, bool disallowCachedBoundingBoxes = false) {
		Unsafe.SkipInit(out ModelInstance result);
		return FindIntersections(ray, new Span<ModelInstance>(ref result), rayThickness, disallowCachedBoundingBoxes) > 0 ? result : null;
	}
	/// <summary>
	/// Writes every object the given ray strikes in to <paramref name="resultsDest"/>, nearest first, and returns how many were written.
	/// </summary>
	/// <remarks>
	/// The return value is the number of objects <i>written</i>, which is capped at the length of <paramref name="resultsDest"/>. It is not the total number of
	/// objects hit. A caller therefore cannot tell from the result alone whether the buffer was too small, so size it generously if that matters.
	/// </remarks>
	/// <param name="ray">The ray to test against the scene.</param>
	/// <param name="resultsDest">The buffer to write the results in to. Results are ordered nearest-first.</param>
	/// <param name="rayThickness">Optionally sweeps the ray in to a cylinder of this radius, so that objects the ray passes close to are also reported. Defaults to <c>0f</c>, i.e. an infinitely thin ray.</param>
	/// <param name="disallowCachedBoundingBoxes">Pass <see langword="true"/> for objects whose geometry has been altered since it was loaded, whose cached bounds may therefore be out of date. This makes the query slower but accurate for such objects. Defaults to <see langword="false"/>.</param>
	/// <returns>The number of objects written to <paramref name="resultsDest"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int FindIntersections(Ray ray, Span<ModelInstance> resultsDest, float rayThickness = 0f, bool disallowCachedBoundingBoxes = false) {
		return Scene.FindIntersections(ray, resultsDest, rayThickness, disallowCachedBoundingBoxes);
	}

	/// <summary>
	/// Returns one of the objects overlapping the given box, or <see langword="null"/> if none do.
	/// </summary>
	/// <remarks>
	/// Which object is returned when several overlap is unspecified. Unlike the ray queries, shape queries are not ordered, so there is no meaningful "first". Use
	/// this where you only need to know whether <i>anything</i> is in a region.
	/// </remarks>
	/// <param name="shape">The box to test against the scene.</param>
	/// <param name="disallowCachedBoundingBoxes">Pass <see langword="true"/> for objects whose geometry has been altered since it was loaded, whose cached bounds may therefore be out of date. This makes the query slower but accurate for such objects. Defaults to <see langword="false"/>.</param>
	public ModelInstance? GetAnyIntersection(PositionedRotatedCuboid shape, bool disallowCachedBoundingBoxes = false) {
		Unsafe.SkipInit(out ModelInstance result);
		return FindIntersections(shape, new Span<ModelInstance>(ref result), disallowCachedBoundingBoxes) > 0 ? result : null;
	}
	/// <summary>
	/// Writes every object overlapping the given box in to <paramref name="resultsDest"/> and returns how many were written.
	/// </summary>
	/// <remarks>
	/// The results are in no particular order. The return value is the number of objects <i>written</i>, which is capped at the length of
	/// <paramref name="resultsDest"/>. It is not the total number of objects overlapping, so a caller cannot tell from the result alone whether the buffer was too
	/// small.
	/// </remarks>
	/// <param name="shape">The box to test against the scene.</param>
	/// <param name="resultsDest">The buffer to write the results in to.</param>
	/// <param name="disallowCachedBoundingBoxes">Pass <see langword="true"/> for objects whose geometry has been altered since it was loaded, whose cached bounds may therefore be out of date. This makes the query slower but accurate for such objects. Defaults to <see langword="false"/>.</param>
	/// <returns>The number of objects written to <paramref name="resultsDest"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int FindIntersections(PositionedRotatedCuboid shape, Span<ModelInstance> resultsDest, bool disallowCachedBoundingBoxes = false) {
		return Scene.FindIntersections(shape, resultsDest, disallowCachedBoundingBoxes);
	}

	/// <summary>
	/// Returns one of the objects overlapping the given axis-aligned box, or <see langword="null"/> if none do.
	/// </summary>
	/// <remarks>
	/// Which object is returned when several overlap is unspecified. Unlike the ray queries, shape queries are not ordered, so there is no meaningful "first". Use
	/// this where you only need to know whether <i>anything</i> is in a region.
	/// </remarks>
	/// <param name="shape">The axis-aligned box to test against the scene.</param>
	/// <param name="disallowCachedBoundingBoxes">Pass <see langword="true"/> for objects whose geometry has been altered since it was loaded, whose cached bounds may therefore be out of date. This makes the query slower but accurate for such objects. Defaults to <see langword="false"/>.</param>
	public ModelInstance? GetAnyIntersection(PositionedCuboid shape, bool disallowCachedBoundingBoxes = false) {
		Unsafe.SkipInit(out ModelInstance result);
		return FindIntersections(shape, new Span<ModelInstance>(ref result), disallowCachedBoundingBoxes) > 0 ? result : null;
	}
	/// <summary>
	/// Writes every object overlapping the given axis-aligned box in to <paramref name="resultsDest"/> and returns how many were written.
	/// </summary>
	/// <remarks>
	/// The results are in no particular order. The return value is the number of objects <i>written</i>, which is capped at the length of
	/// <paramref name="resultsDest"/>. It is not the total number of objects overlapping, so a caller cannot tell from the result alone whether the buffer was too
	/// small.
	/// </remarks>
	/// <param name="shape">The axis-aligned box to test against the scene.</param>
	/// <param name="resultsDest">The buffer to write the results in to.</param>
	/// <param name="disallowCachedBoundingBoxes">Pass <see langword="true"/> for objects whose geometry has been altered since it was loaded, whose cached bounds may therefore be out of date. This makes the query slower but accurate for such objects. Defaults to <see langword="false"/>.</param>
	/// <returns>The number of objects written to <paramref name="resultsDest"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int FindIntersections(PositionedCuboid shape, Span<ModelInstance> resultsDest, bool disallowCachedBoundingBoxes = false) {
		return Scene.FindIntersections(shape, resultsDest, disallowCachedBoundingBoxes);
	}

	/// <summary>
	/// Returns one of the objects overlapping the given sphere, or <see langword="null"/> if none do.
	/// </summary>
	/// <remarks>
	/// Which object is returned when several overlap is unspecified. Unlike the ray queries, shape queries are not ordered, so there is no meaningful "first". Use
	/// this where you only need to know whether <i>anything</i> is in a region.
	/// </remarks>
	/// <param name="shape">The sphere to test against the scene.</param>
	/// <param name="disallowCachedBoundingBoxes">Pass <see langword="true"/> for objects whose geometry has been altered since it was loaded, whose cached bounds may therefore be out of date. This makes the query slower but accurate for such objects. Defaults to <see langword="false"/>.</param>
	public ModelInstance? GetAnyIntersection(PositionedSphere shape, bool disallowCachedBoundingBoxes = false) {
		Unsafe.SkipInit(out ModelInstance result);
		return FindIntersections(shape, new Span<ModelInstance>(ref result), disallowCachedBoundingBoxes) > 0 ? result : null;
	}
	/// <summary>
	/// Writes every object overlapping the given sphere in to <paramref name="resultsDest"/> and returns how many were written.
	/// </summary>
	/// <remarks>
	/// The results are in no particular order. The return value is the number of objects <i>written</i>, which is capped at the length of
	/// <paramref name="resultsDest"/>. It is not the total number of objects overlapping, so a caller cannot tell from the result alone whether the buffer was too
	/// small.
	/// </remarks>
	/// <param name="shape">The sphere to test against the scene.</param>
	/// <param name="resultsDest">The buffer to write the results in to.</param>
	/// <param name="disallowCachedBoundingBoxes">Pass <see langword="true"/> for objects whose geometry has been altered since it was loaded, whose cached bounds may therefore be out of date. This makes the query slower but accurate for such objects. Defaults to <see langword="false"/>.</param>
	/// <returns>The number of objects written to <paramref name="resultsDest"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int FindIntersections(PositionedSphere shape, Span<ModelInstance> resultsDest, bool disallowCachedBoundingBoxes = false) {
		return Scene.FindIntersections(shape, resultsDest, disallowCachedBoundingBoxes);
	}
}
