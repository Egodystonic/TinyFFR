// Created on 2026-09-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Answers questions about which objects on a <see cref="CanvasScene"/> lie under a given pixel, such as which element the user just clicked.
/// </summary>
/// <remarks>
/// <para>
/// Obtained from <see cref="CanvasScene.QueryProvider"/>. Each query comes in two flavours: one taking a <i>render target</i> coordinate (such as a mouse cursor
/// position within a window), and one taking a <i>local</i> canvas coordinate. The render target flavour converts via
/// <see cref="CanvasScene.ConvertRenderTargetCoordToLocal"/> first, so it accounts for render sub-area viewports (e.g. splitscreen/PiP) and DPI scaling.
/// </para>
/// <para>
/// Queries are generic over the kind of canvas object being searched for (<see cref="CanvasTexture"/> or <see cref="CanvasText"/>); objects of any other kind are
/// skipped rather than reported, but still sit in their layer (they do not "block" the query). Results are ordered topmost first, i.e. from the highest
/// <see cref="ICanvasObject.Layer"/> down. The order of objects sharing a layer is unspecified.
/// </para>
/// <para>
/// These queries are built on <see cref="World.Scene.QueryProvider"/> and share its rules: objects are tested by their <i>bounding volumes</i>. For a
/// <see cref="CanvasTexture"/> that is exactly its drawn rectangle (rotation included), but a <see cref="CanvasText"/>'s bounds include some padding around its glyphs,
/// so a point slightly outside its <see cref="ICanvasObject.ActualSizePixels"/> may still report a hit. Use <see cref="ICanvasObject.Contains(XYPair{int}, DiagonalOrientation2D)"/>
/// on the results if you need the stricter answer.
/// </para>
/// <para>
/// Invisible objects (see <see cref="ICanvasObject.IsVisible"/>) are never reported, and a coordinate that falls outside the canvas reports nothing.
/// </para>
/// </remarks>
public readonly record struct CanvasSceneQueryProvider {
	/// <summary>
	/// The canvas these queries run against.
	/// </summary>
	public CanvasScene Canvas { get; init; }

	ISceneImplProvider Implementation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Canvas.UnderlyingScene.Implementation;
	}
	ResourceHandle<Scene> SceneHandle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Canvas.UnderlyingScene.GetHandleWithoutDisposeCheck();
	}

	/// <summary>
	/// Constructs a new <see cref="CanvasSceneQueryProvider"/> for the given canvas.
	/// </summary>
	/// <remarks>
	/// Usually obtained from <see cref="CanvasScene.QueryProvider"/> rather than constructed directly.
	/// </remarks>
	/// <param name="canvas">The canvas to query.</param>
	public CanvasSceneQueryProvider(CanvasScene canvas) => Canvas = canvas;

	/// <summary>
	/// Returns the topmost object of the given kind under the given render target coordinate, or <see langword="null"/> if there is none.
	/// </summary>
	/// <remarks>
	/// Objects of other kinds are ignored, so a <see cref="CanvasText"/> drawn over a <see cref="CanvasTexture"/> does not prevent that texture being returned when
	/// searching for textures. Note that this still has to test every object on the canvas, so it is no cheaper than asking for all of them.
	/// </remarks>
	/// <typeparam name="TCanvasObject">The kind of canvas object to search for (<see cref="CanvasTexture"/> or <see cref="CanvasText"/>).</typeparam>
	/// <param name="renderTargetCoord">The coordinate to test, such as a mouse cursor position within a window.</param>
	/// <param name="coordOrigin">Which corner of the render target <paramref name="renderTargetCoord"/> is measured from (or the centre if <see cref="DiagonalOrientation2D.None"/>). Defaults to <see cref="DiagonalOrientation2D.UpLeft"/>, which matches the convention used for window and cursor coordinates.</param>
	/// <param name="disableDpiScalingAdjustment">Pass <see langword="true"/> to skip the adjustment made for displays with scaling enabled, if the supplied coordinate is already in real pixels. Defaults to <see langword="false"/>.</param>
	public TCanvasObject? GetTopmostObjectUnderRenderTargetCoord<TCanvasObject>(XYPair<int> renderTargetCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft, bool disableDpiScalingAdjustment = false) where TCanvasObject : struct, ICanvasObject<TCanvasObject, ModelInstance> {
		Unsafe.SkipInit(out TCanvasObject result);
		return FindObjectsUnderRenderTargetCoord(renderTargetCoord, new Span<TCanvasObject>(ref result), coordOrigin, disableDpiScalingAdjustment) > 0 ? result : null;
	}
	/// <summary>
	/// Writes every object of the given kind under the given render target coordinate in to <paramref name="resultsDest"/>, topmost first, and returns how many were written.
	/// </summary>
	/// <remarks>
	/// The return value is the number of objects <i>written</i>, which is capped at the length of <paramref name="resultsDest"/>. It is not the total number of
	/// objects under the coordinate. A caller therefore cannot tell from the result alone whether the buffer was too small, so size it generously if that matters.
	/// </remarks>
	/// <typeparam name="TCanvasObject">The kind of canvas object to search for (<see cref="CanvasTexture"/> or <see cref="CanvasText"/>).</typeparam>
	/// <param name="renderTargetCoord">The coordinate to test, such as a mouse cursor position within a window.</param>
	/// <param name="resultsDest">The buffer to write the results in to. Results are ordered topmost-first.</param>
	/// <param name="coordOrigin">Which corner of the render target <paramref name="renderTargetCoord"/> is measured from (or the centre if <see cref="DiagonalOrientation2D.None"/>). Defaults to <see cref="DiagonalOrientation2D.UpLeft"/>, which matches the convention used for window and cursor coordinates.</param>
	/// <param name="disableDpiScalingAdjustment">Pass <see langword="true"/> to skip the adjustment made for displays with scaling enabled, if the supplied coordinate is already in real pixels. Defaults to <see langword="false"/>.</param>
	/// <returns>The number of objects written to <paramref name="resultsDest"/>.</returns>
	public int FindObjectsUnderRenderTargetCoord<TCanvasObject>(XYPair<int> renderTargetCoord, Span<TCanvasObject> resultsDest, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft, bool disableDpiScalingAdjustment = false) where TCanvasObject : struct, ICanvasObject<TCanvasObject, ModelInstance> {
		return FindObjectsUnderLocalCoord(Canvas.ConvertRenderTargetCoordToLocal(renderTargetCoord, coordOrigin, disableDpiScalingAdjustment), resultsDest, coordOrigin);
	}

	/// <summary>
	/// Returns the topmost object of the given kind under the given canvas coordinate, or <see langword="null"/> if there is none.
	/// </summary>
	/// <remarks>
	/// Objects of other kinds are ignored, so a <see cref="CanvasText"/> drawn over a <see cref="CanvasTexture"/> does not prevent that texture being returned when
	/// searching for textures. Note that this still has to test every object on the canvas, so it is no cheaper than asking for all of them.
	/// </remarks>
	/// <typeparam name="TCanvasObject">The kind of canvas object to search for (<see cref="CanvasTexture"/> or <see cref="CanvasText"/>).</typeparam>
	/// <param name="localCoord">The coordinate to test, in canvas pixels.</param>
	/// <param name="coordOrigin">Which corner of the canvas <paramref name="localCoord"/> is measured from (or the centre if <see cref="DiagonalOrientation2D.None"/>). Defaults to <see cref="DiagonalOrientation2D.UpLeft"/>.</param>
	public TCanvasObject? GetTopmostObjectUnderLocalCoord<TCanvasObject>(XYPair<int> localCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft) where TCanvasObject : struct, ICanvasObject<TCanvasObject, ModelInstance> {
		Unsafe.SkipInit(out TCanvasObject result);
		return FindObjectsUnderLocalCoord(localCoord, new Span<TCanvasObject>(ref result), coordOrigin) > 0 ? result : null;
	}
	/// <summary>
	/// Writes every object of the given kind under the given canvas coordinate in to <paramref name="resultsDest"/>, topmost first, and returns how many were written.
	/// </summary>
	/// <remarks>
	/// The return value is the number of objects <i>written</i>, which is capped at the length of <paramref name="resultsDest"/>. It is not the total number of
	/// objects under the coordinate. A caller therefore cannot tell from the result alone whether the buffer was too small, so size it generously if that matters.
	/// </remarks>
	/// <typeparam name="TCanvasObject">The kind of canvas object to search for (<see cref="CanvasTexture"/> or <see cref="CanvasText"/>).</typeparam>
	/// <param name="localCoord">The coordinate to test, in canvas pixels.</param>
	/// <param name="resultsDest">The buffer to write the results in to. Results are ordered topmost-first.</param>
	/// <param name="coordOrigin">Which corner of the canvas <paramref name="localCoord"/> is measured from (or the centre if <see cref="DiagonalOrientation2D.None"/>). Defaults to <see cref="DiagonalOrientation2D.UpLeft"/>.</param>
	/// <returns>The number of objects written to <paramref name="resultsDest"/>.</returns>
	public int FindObjectsUnderLocalCoord<TCanvasObject>(XYPair<int> localCoord, Span<TCanvasObject> resultsDest, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft) where TCanvasObject : struct, ICanvasObject<TCanvasObject, ModelInstance> {
		if (resultsDest.IsEmpty) return 0;
		if (Implementation.GetCanvasQueryRay(SceneHandle, localCoord, coordOrigin) is not { } ray) return 0;

		var scratch = Implementation.GetCanvasQueryScratchBuffer(SceneHandle);
		var hitCount = Canvas.UnderlyingScene.QueryProvider.FindIntersections(ray, scratch);
		var resultCount = 0;
		for (var i = 0; i < hitCount; ++i) {
			if (!TryConvertToCanvasObject<TCanvasObject>(scratch[i], out var canvasObject)) continue;
			resultsDest[resultCount++] = canvasObject;
			if (resultCount == resultsDest.Length) break;
		}
		return resultCount;
	}

	bool TryConvertToCanvasObject<TCanvasObject>(ModelInstance modelInstance, out TCanvasObject result) where TCanvasObject : struct, ICanvasObject<TCanvasObject, ModelInstance> {
		if (!Implementation.IsCanvasObjectOfType<TCanvasObject>(SceneHandle, modelInstance)) {
			result = default;
			return false;
		}
		result = TCanvasObject.DeSmuggle(modelInstance, ReadOnlySpan<byte>.Empty, TypeUtils.ResourceToStub(Canvas.UnderlyingScene));
		return true;
	}
}
