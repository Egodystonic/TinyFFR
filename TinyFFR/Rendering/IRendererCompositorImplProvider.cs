// Created on 2026-05-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Rendering;

public interface IRendererCompositorImplProvider : IDisposableResourceImplProvider<RendererCompositor> {
	void Add(ResourceHandle<RendererCompositor> handle, Renderer renderer, RenderCompositionType compositionType);
	void SetEnabledState(ResourceHandle<RendererCompositor> handle, Renderer renderer, bool newEnabledState);
	void SetRendererFrameRateCap(ResourceHandle<RendererCompositor> handle, Renderer renderer, int? maxFramesPerSecond);
	int? GetRendererFrameRateCap(ResourceHandle<RendererCompositor> handle, Renderer renderer);
	void SetRendererFrameRateRatio(ResourceHandle<RendererCompositor> handle, Renderer renderer, int renderOnceEveryNFrames);
	int GetRendererFrameRateRatio(ResourceHandle<RendererCompositor> handle, Renderer renderer);
	IndirectEnumerable<RendererCompositor, Renderer> GetAddedRenderers(ResourceHandle<RendererCompositor> handle);
	void RenderAll(ResourceHandle<RendererCompositor> handle);
	void WaitForGpu(ResourceHandle<RendererCompositor> handle);
	void Dispose(ResourceHandle<RendererCompositor> handle, bool disposeContainedRenderers);
}
