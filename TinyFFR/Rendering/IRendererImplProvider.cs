// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.Rendering;

public interface IRendererImplProvider : IDisposableResourceImplProvider<Renderer> {
	void Render(ResourceHandle<Renderer> handle);
	void SetQualityConfig(ResourceHandle<Renderer> handle, RenderQualityConfig newConfig);
	void WaitForGpu(ResourceHandle<Renderer> handle);
	void CaptureScreenshot(ResourceHandle<Renderer> handle, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? saveConfig);
	void CaptureScreenshot(ResourceHandle<Renderer> handle, Action<XYPair<int>, ReadOnlySpan<TexelRgb24>> handler);
	unsafe void CaptureScreenshot(ResourceHandle<Renderer> handle, delegate* managed<XYPair<int>, ReadOnlySpan<TexelRgb24>, void> handler);
}