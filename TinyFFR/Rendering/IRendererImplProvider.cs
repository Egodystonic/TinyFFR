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
	void CaptureScreenshot(ResourceHandle<Renderer> handle, Action<XYPair<int>, ReadOnlySpan<TexelRgba32>> handler, XYPair<int>? captureResolution, bool lowestAddressesRepresentFrameTop);
	unsafe void CaptureScreenshot(ResourceHandle<Renderer> handle, delegate* managed<XYPair<int>, ReadOnlySpan<TexelRgba32>, void> handler, XYPair<int>? captureResolution, bool lowestAddressesRepresentFrameTop);
	Ray CastRayFromRenderSurface(ResourceHandle<Renderer> handle, XYPair<int> pixelCoord, DiagonalOrientation2D coordOrigin, bool disableDpiScalingAdjustment);
	Ray CastRayFromViewportSurface(ResourceHandle<Renderer> handle, XYPair<int> pixelCoord, DiagonalOrientation2D coordOrigin, bool disableDpiScalingAdjustment);
	void SetTargetViewportDimensionsByFraction(ResourceHandle<Renderer> handle, Orientation2D anchor, XYPair<float> fractionalOffset, XYPair<float> fractionalDimensions);
	void SetTargetViewportDimensionsByPixel(ResourceHandle<Renderer> handle, Orientation2D anchor, XYPair<int> fractionalLocation, XYPair<int> pixelDimensions);
	XYPair<int> GetTargetViewportDimensions(ResourceHandle<Renderer> handle);
	XYPair<int> GetTargetViewportOffset(ResourceHandle<Renderer> handle, DiagonalOrientation2D coordOrigin);
	
	// Sub-area being handled downstream is required because of a quirk in filament's handling of viewports;
	// currently really only required by the ImGui integration but may in theory be required by other things 
	// in future.
	// This flag, if true, *reports* the requested sub area as set by the user but actually passes a full target
	// size/offset to filament.
	void MarkSubAreaAsHandledDownstream(ResourceHandle<Renderer> handle, bool isHandledDownstream);
}