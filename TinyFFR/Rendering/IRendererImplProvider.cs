// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Rendering;

public interface IRendererImplProvider : IDisposableResourceImplProvider<Renderer> {
	Scene GetScene(ResourceHandle<Renderer> handle);
	Camera GetCamera(ResourceHandle<Renderer> handle);
	Window? GetWindow(ResourceHandle<Renderer> handle);
	RenderOutputBuffer? GetBuffer(ResourceHandle<Renderer> handle);
	void Render(ResourceHandle<Renderer> handle);
	void SetQualityConfig(ResourceHandle<Renderer> handle, RenderQualityConfig newConfig);
	void SetFrustumCullingEnabled(ResourceHandle<Renderer> handle, bool enabled);
	void WaitForGpu(ResourceHandle<Renderer> handle);
	void CaptureScreenshot(ResourceHandle<Renderer> handle, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? saveConfig, XYPair<int>? captureResolution);
	void CaptureScreenshot(ResourceHandle<Renderer> handle, Action<XYPair<int>, ReadOnlySpan<TexelRgb24>> handler, XYPair<int>? captureResolution, bool lowestAddressesRepresentFrameTop);
	unsafe void CaptureScreenshot(ResourceHandle<Renderer> handle, delegate* managed<XYPair<int>, ReadOnlySpan<TexelRgb24>, void> handler, XYPair<int>? captureResolution, bool lowestAddressesRepresentFrameTop);
	Ray CastRayFromRenderSurface(ResourceHandle<Renderer> handle, XYPair<int> pixelCoord, bool yZeroOriginAtBottom);
}