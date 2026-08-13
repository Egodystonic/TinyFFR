// Created on 2026-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Rendering.Local;

// This is a private embedded 'delegating' object to help provide distinction between some default interface methods
// on both IRendererCompositorImplProvider and IRendererImplProvider.
sealed class LocalRendererCompositorImplProvider : IRendererCompositorImplProvider {
	public LocalRendererBuilder Owner { get; }

	public LocalRendererCompositorImplProvider(LocalRendererBuilder owner) => Owner = owner;

	public void SetRendererFrameRateCap(ResourceHandle<RendererCompositor> handle, Renderer renderer, int? maxFramesPerSecond) => Owner.SetRendererFrameRateCap(handle, renderer, maxFramesPerSecond);
	public int? GetRendererFrameRateCap(ResourceHandle<RendererCompositor> handle, Renderer renderer) => Owner.GetRendererFrameRateCap(handle, renderer);
	public void SetRendererFrameRateRatio(ResourceHandle<RendererCompositor> handle, Renderer renderer, int ratioDenominator) => Owner.SetRendererFrameRateRatio(handle, renderer, ratioDenominator);
	public int GetRendererFrameRateRatio(ResourceHandle<RendererCompositor> handle, Renderer renderer) => Owner.GetRendererFrameRateRatio(handle, renderer);
	public string GetNameAsNewStringObject(ResourceHandle<RendererCompositor> handle) => Owner.GetNameAsNewStringObject(handle);
	public int GetNameLength(ResourceHandle<RendererCompositor> handle) => Owner.GetNameLength(handle);
	public void CopyName(ResourceHandle<RendererCompositor> handle, Span<char> destinationBuffer) => Owner.CopyName(handle, destinationBuffer);
	public bool IsDisposed(ResourceHandle<RendererCompositor> handle) => Owner.IsDisposed(handle);
	public void Dispose(ResourceHandle<RendererCompositor> handle) => Owner.Dispose(handle);
	public void Dispose(ResourceHandle<RendererCompositor> handle, bool disposeContainedRenderers) => Owner.Dispose(handle, disposeContainedRenderers);
	public IndirectEnumerable<RendererCompositor, Renderer> GetAddedRenderers(ResourceHandle<RendererCompositor> handle) => Owner.GetAddedRenderers(handle);
	public void Add(ResourceHandle<RendererCompositor> handle, Renderer renderer, RenderCompositionType compositionType) => Owner.Add(handle, renderer, compositionType);
	public void SetEnabledState(ResourceHandle<RendererCompositor> handle, Renderer renderer, bool newEnabledState) => Owner.SetEnabledState(handle, renderer, newEnabledState);
	public void RenderAll(ResourceHandle<RendererCompositor> handle) => Owner.RenderAll(handle);
	public void WaitForGpu(ResourceHandle<RendererCompositor> handle) => Owner.WaitForGpu(handle);
	public override string ToString() => Owner.ToString();
}
