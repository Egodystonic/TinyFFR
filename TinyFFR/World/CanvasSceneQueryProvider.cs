// Created on 2026-09-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public readonly record struct CanvasSceneQueryProvider {
	public CanvasScene Canvas { get; init; }

	ISceneImplProvider Implementation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Canvas.UnderlyingScene.Implementation;
	}
	ResourceHandle<Scene> SceneHandle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Canvas.UnderlyingScene.GetHandleWithoutDisposeCheck();
	}

	public CanvasSceneQueryProvider(CanvasScene canvas) => Canvas = canvas;

	public TCanvasObject? GetTopmostObjectUnderRenderTargetCoord<TCanvasObject>(XYPair<int> renderTargetCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft, bool disableDpiScalingAdjustment = false) where TCanvasObject : struct, ICanvasObject<TCanvasObject, ModelInstance> {
		Unsafe.SkipInit(out TCanvasObject result);
		return FindObjectsUnderRenderTargetCoord(renderTargetCoord, new Span<TCanvasObject>(ref result), coordOrigin, disableDpiScalingAdjustment) > 0 ? result : null;
	}
	public int FindObjectsUnderRenderTargetCoord<TCanvasObject>(XYPair<int> renderTargetCoord, Span<TCanvasObject> resultsDest, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft, bool disableDpiScalingAdjustment = false) where TCanvasObject : struct, ICanvasObject<TCanvasObject, ModelInstance> {
		return FindObjectsUnderLocalCoord(Canvas.ConvertRenderTargetCoordToLocal(renderTargetCoord, coordOrigin, disableDpiScalingAdjustment), resultsDest, coordOrigin);
	}

	public TCanvasObject? GetTopmostObjectUnderLocalCoord<TCanvasObject>(XYPair<int> localCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft) where TCanvasObject : struct, ICanvasObject<TCanvasObject, ModelInstance> {
		Unsafe.SkipInit(out TCanvasObject result);
		return FindObjectsUnderLocalCoord(localCoord, new Span<TCanvasObject>(ref result), coordOrigin) > 0 ? result : null;
	}
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
