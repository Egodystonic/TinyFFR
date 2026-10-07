// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Rendering;

/// <summary>
/// An <see cref="IResourceImplProvider{TResource}"/> for <see cref="Renderer"/> resources.
/// </summary>
public interface IRendererImplProvider : IDisposableResourceImplProvider<Renderer> {
	/// <summary>
	/// Invoked via <see cref="Renderer.TargetScene"/>.
	/// </summary>
	Scene GetScene(ResourceHandle<Renderer> handle);
	/// <summary>
	/// Invoked via <see cref="Renderer.TargetCamera"/>.
	/// </summary>
	Camera GetCamera(ResourceHandle<Renderer> handle);
	/// <summary>
	/// Invoked via <see cref="Renderer.TargetWindow"/>.
	/// </summary>
	Window? GetWindow(ResourceHandle<Renderer> handle);
	/// <summary>
	/// Invoked via <see cref="Renderer.TargetBuffer"/>.
	/// </summary>
	RenderOutputBuffer? GetBuffer(ResourceHandle<Renderer> handle);
	/// <summary>
	/// Invoked via <see cref="Renderer.Render"/>.
	/// </summary>
	void Render(ResourceHandle<Renderer> handle);
	/// <summary>
	/// Invoked via <see cref="Renderer.SetQuality(RenderQualityConfig)"/> (and, indirectly, <see cref="Renderer.SetQuality(BuiltInQualityConfiguration)"/>).
	/// </summary>
	void SetQualityConfig(ResourceHandle<Renderer> handle, RenderQualityConfig newConfig);
	/// <summary>
	/// Invoked via <see cref="Renderer.SetFrustumCullingEnabled"/>.
	/// </summary>
	void SetFrustumCullingEnabled(ResourceHandle<Renderer> handle, bool enabled);
	/// <summary>
	/// Invoked via <see cref="Renderer.WaitForGpu"/>.
	/// </summary>
	void WaitForGpu(ResourceHandle<Renderer> handle);
	/// <summary>
	/// Invoked via <see cref="Renderer.CaptureScreenshot(ReadOnlySpan{char},BitmapSaveConfig?,XYPair{int}?)"/>.
	/// </summary>
	void CaptureScreenshot(ResourceHandle<Renderer> handle, ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? saveConfig, XYPair<int>? captureResolution);
	/// <summary>
	/// Invoked via <see cref="Renderer.CaptureScreenshot(Action{XYPair{int},ReadOnlySpan{TexelRgba32}},XYPair{int}?,bool)"/>.
	/// </summary>
	void CaptureScreenshot(ResourceHandle<Renderer> handle, Action<XYPair<int>, ReadOnlySpan<TexelRgba32>> handler, XYPair<int>? captureResolution, bool lowestAddressesRepresentFrameTop);
	/// <summary>
	/// Invoked via <see cref="Renderer"/>'s function-pointer overload of <see cref="Renderer.CaptureScreenshot(Action{XYPair{int},ReadOnlySpan{TexelRgba32}},XYPair{int}?,bool)">CaptureScreenshot</see>.
	/// </summary>
	unsafe void CaptureScreenshot(ResourceHandle<Renderer> handle, delegate* managed<XYPair<int>, ReadOnlySpan<TexelRgba32>, void> handler, XYPair<int>? captureResolution, bool lowestAddressesRepresentFrameTop);
	/// <summary>
	/// Invoked via <see cref="Renderer.CreateRayFromRenderSurface"/>.
	/// </summary>
	Ray CreateRayFromRenderSurface(ResourceHandle<Renderer> handle, XYPair<int> pixelCoord, DiagonalOrientation2D coordOrigin, bool disableDpiScalingAdjustment);
	/// <summary>
	/// Invoked via <see cref="Renderer.PickModelInstanceFromRenderSurface"/>.
	/// </summary>
	PixelPickResult? PickModelInstanceFromRenderSurface(ResourceHandle<Renderer> handle, XYPair<int> pixelCoord, bool includeTransparentObjects, DiagonalOrientation2D coordOrigin, bool disableDpiScalingAdjustment);
	/// <summary>
	/// Invoked via <see cref="Renderer.CreateRayFromRenderSubAreaSurface"/>.
	/// </summary>
	Ray CreateRayFromViewportSurface(ResourceHandle<Renderer> handle, XYPair<int> pixelCoord, DiagonalOrientation2D coordOrigin, bool disableDpiScalingAdjustment);
	/// <summary>
	/// Invoked via <see cref="Renderer.PickModelInstanceFromRenderSubAreaSurface"/>.
	/// </summary>
	PixelPickResult? PickModelInstanceFromViewportSurface(ResourceHandle<Renderer> handle, XYPair<int> pixelCoord, bool includeTransparentObjects, DiagonalOrientation2D coordOrigin, bool disableDpiScalingAdjustment);
	/// <summary>
	/// Invoked via <see cref="Renderer.ProjectOnToRenderSurfacePixels"/> (<paramref name="asFraction"/> = <see langword="false"/>)
	/// and <see cref="Renderer.ProjectOnToRenderSurfaceFraction"/> (<paramref name="asFraction"/> = <see langword="true"/>).
	/// Pixel results are returned unfloored.
	/// </summary>
	XYPair<float>? ProjectOnToRenderSurface(ResourceHandle<Renderer> handle, Location location, DiagonalOrientation2D coordOrigin, bool disableDpiScalingAdjustment, bool asFraction);
	/// <summary>
	/// Invoked via the <c>Clamped</c> overloads of <see cref="Renderer.ProjectOnToRenderSurfacePixels"/> (<paramref name="asFraction"/> = <see langword="false"/>)
	/// and <see cref="Renderer.ProjectOnToRenderSurfaceFraction"/> (<paramref name="asFraction"/> = <see langword="true"/>).
	/// Pixel results are returned unfloored.
	/// </summary>
	XYPair<float> ProjectOnToRenderSurfaceClamped(ResourceHandle<Renderer> handle, Location location, DiagonalOrientation2D coordOrigin, bool disableDpiScalingAdjustment, bool asFraction, out bool wasClamped);
	/// <summary>
	/// Invoked via <see cref="Renderer.ProjectOnToRenderSubAreaSurfacePixels"/> (<paramref name="asFraction"/> = <see langword="false"/>)
	/// and <see cref="Renderer.ProjectOnToRenderSubAreaSurfaceFraction"/> (<paramref name="asFraction"/> = <see langword="true"/>).
	/// Pixel results are returned unfloored.
	/// </summary>
	XYPair<float>? ProjectOnToViewportSurface(ResourceHandle<Renderer> handle, Location location, DiagonalOrientation2D coordOrigin, bool disableDpiScalingAdjustment, bool asFraction);
	/// <summary>
	/// Invoked via the <c>Clamped</c> overloads of <see cref="Renderer.ProjectOnToRenderSubAreaSurfacePixels"/> (<paramref name="asFraction"/> = <see langword="false"/>)
	/// and <see cref="Renderer.ProjectOnToRenderSubAreaSurfaceFraction"/> (<paramref name="asFraction"/> = <see langword="true"/>).
	/// Pixel results are returned unfloored.
	/// </summary>
	XYPair<float> ProjectOnToViewportSurfaceClamped(ResourceHandle<Renderer> handle, Location location, DiagonalOrientation2D coordOrigin, bool disableDpiScalingAdjustment, bool asFraction, out bool wasClamped);
	/// <summary>
	/// Invoked via <see cref="Renderer.SetRenderSubAreaFraction"/>.
	/// </summary>
	void SetTargetViewportDimensionsByFraction(ResourceHandle<Renderer> handle, Orientation2D anchor, XYPair<float> fractionalOffset, XYPair<float> fractionalDimensions);
	/// <summary>
	/// Invoked via <see cref="Renderer.SetRenderSubAreaPixels"/>.
	/// </summary>
	void SetTargetViewportDimensionsByPixel(ResourceHandle<Renderer> handle, Orientation2D anchor, XYPair<int> fractionalLocation, XYPair<int> pixelDimensions);
	/// <summary>
	/// Invoked via <see cref="Renderer.GetRenderSubAreaPixelDimensions"/>.
	/// </summary>
	XYPair<int> GetTargetViewportDimensionsByPixel(ResourceHandle<Renderer> handle);
	/// <summary>
	/// Invoked via <see cref="Renderer.GetRenderSubAreaPixelOffset"/>.
	/// </summary>
	XYPair<int> GetTargetViewportOffsetByPixel(ResourceHandle<Renderer> handle, DiagonalOrientation2D coordOrigin);

	// Sub-area being handled downstream is required because of a quirk in filament's handling of viewports;
	// currently really only required by the ImGui integration but may in theory be required by other things
	// in future.
	// This flag, if true, *reports* the requested sub area as set by the user but actually passes a full target
	// size/offset to filament.
	/// <summary>
	/// Invoked internally via <see cref="Renderer"/>'s own sub-area viewport handling; not exposed on any public member.
	/// </summary>
	void MarkSubAreaAsHandledDownstream(ResourceHandle<Renderer> handle, bool isHandledDownstream);
}