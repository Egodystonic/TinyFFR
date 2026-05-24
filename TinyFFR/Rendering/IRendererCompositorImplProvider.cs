// Created on 2026-05-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Rendering;

public interface IRendererCompositorImplProvider : IDisposableResourceImplProvider<RendererCompositor> {
	void Add(ResourceHandle<RendererCompositor> handle, Renderer renderer, RenderCompositionType compositionType);
	void SetEnabledState(ResourceHandle<RendererCompositor> handle, Renderer renderer, bool newEnabledState);
	IndirectEnumerable<RendererCompositor, Renderer> GetAddedRenderers(ResourceHandle<RendererCompositor> handle);
	void RenderAll(ResourceHandle<RendererCompositor> handle);
	void WaitForGpu(ResourceHandle<RendererCompositor> handle);
	void Dispose(ResourceHandle<RendererCompositor> handle, bool disposeContainedRenderers);
}
