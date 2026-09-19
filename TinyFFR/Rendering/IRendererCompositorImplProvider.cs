// Created on 2026-05-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Rendering;

/// <summary>
/// An <see cref="IResourceImplProvider{TResource}"/> for <see cref="RendererCompositor"/> resources.
/// </summary>
public interface IRendererCompositorImplProvider : IDisposableResourceImplProvider<RendererCompositor> {
	/// <summary>
	/// Invoked via <see cref="RendererCompositor.Add"/>.
	/// </summary>
	void Add(ResourceHandle<RendererCompositor> handle, Renderer renderer, RenderCompositionType compositionType);
	/// <summary>
	/// Invoked via <see cref="RendererCompositor.SetEnabledState"/>.
	/// </summary>
	void SetEnabledState(ResourceHandle<RendererCompositor> handle, Renderer renderer, bool newEnabledState);
	/// <summary>
	/// Invoked via <see cref="RendererCompositor.SetRendererFrameRateCap"/>.
	/// </summary>
	void SetRendererFrameRateCap(ResourceHandle<RendererCompositor> handle, Renderer renderer, int? maxFramesPerSecond);
	/// <summary>
	/// Invoked via <see cref="RendererCompositor.GetRendererFrameRateCap"/>.
	/// </summary>
	int? GetRendererFrameRateCap(ResourceHandle<RendererCompositor> handle, Renderer renderer);
	/// <summary>
	/// Invoked via <see cref="RendererCompositor.SetRendererFrameRateRatio"/>.
	/// </summary>
	void SetRendererFrameRateRatio(ResourceHandle<RendererCompositor> handle, Renderer renderer, int ratioDenominator);
	/// <summary>
	/// Invoked via <see cref="RendererCompositor.GetRendererFrameRateRatio"/>.
	/// </summary>
	int GetRendererFrameRateRatio(ResourceHandle<RendererCompositor> handle, Renderer renderer);
	/// <summary>
	/// Invoked via <see cref="RendererCompositor.AddedRenderers"/>.
	/// </summary>
	IndirectEnumerable<RendererCompositor, Renderer> GetAddedRenderers(ResourceHandle<RendererCompositor> handle);
	/// <summary>
	/// Invoked via <see cref="RendererCompositor.TargetWindow"/>.
	/// </summary>
	Window? GetWindow(ResourceHandle<RendererCompositor> handle);
	/// <summary>
	/// Invoked via <see cref="RendererCompositor.TargetBuffer"/>.
	/// </summary>
	RenderOutputBuffer? GetBuffer(ResourceHandle<RendererCompositor> handle);
	/// <summary>
	/// Invoked via <see cref="RendererCompositor.RenderAll"/>.
	/// </summary>
	void RenderAll(ResourceHandle<RendererCompositor> handle);
	/// <summary>
	/// Invoked via <see cref="RendererCompositor.WaitForGpu"/>.
	/// </summary>
	void WaitForGpu(ResourceHandle<RendererCompositor> handle);
	/// <summary>
	/// Invoked via <see cref="RendererCompositor.Dispose(bool)"/>.
	/// </summary>
	void Dispose(ResourceHandle<RendererCompositor> handle, bool disposeContainedRenderers);
}
