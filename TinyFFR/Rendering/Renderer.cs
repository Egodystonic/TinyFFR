// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Rendering;

/// <summary>
/// Returned from <see cref="Renderer.PickModelInstanceFromRenderSurface"/>.
/// </summary>
/// <param name="ModelInstance">The model instance that was rendered at the requested co-ordinate.</param>
/// <param name="Position">The position in the scene of the pixel.</param>
public readonly record struct PixelPickResult(ModelInstance ModelInstance, Location Position);

/// <summary>
/// A renderer is what actually produces a visualization of your target scene in a window or render buffer.
/// </summary>
public readonly struct Renderer : IDisposableResource<Renderer, IRendererImplProvider> {
	readonly ResourceHandle<Renderer> _handle;
	readonly IRendererImplProvider _impl;

	internal IRendererImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Renderer>();
	internal ResourceHandle<Renderer> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Renderer)) : _handle;

	IRendererImplProvider IResource<Renderer, IRendererImplProvider>.Implementation => Implementation;
	ResourceHandle<Renderer> IResource<Renderer>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	internal Renderer(ResourceHandle<Renderer> handle, IRendererImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}
	
	/// <summary>
	/// The <see cref="Scene"/> this renderer was created to capture.
	/// </summary>
	public Scene TargetScene {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetScene(_handle);
	}
	/// <summary>
	/// The <see cref="Camera"/> this renderer uses to capture the <see cref="TargetScene"/>.
	/// </summary>
	public Camera TargetCamera {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCamera(_handle);
	}
	/// <summary>
	/// The <see cref="Window"/> this renderer renders in to, or <c>null</c> if it does not target a window.
	/// </summary>
	public Window? TargetWindow {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetWindow(_handle);
	}
	/// <summary>
	/// The <see cref="RenderOutputBuffer"/> this renderer renders in to, or <c>null</c> if it does not target an output buffer. 
	/// </summary>
	public RenderOutputBuffer? TargetBuffer {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetBuffer(_handle);
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static Renderer IResource<Renderer>.CreateFromHandleAndImpl(ResourceHandle<Renderer> handle, IResourceImplProvider impl) {
		return new Renderer(handle, impl as IRendererImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<Renderer> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<Renderer> IResource<Renderer>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	/// <summary>
	/// Captures a snapshot of the <see cref="TargetScene"/> with the <see cref="TargetCamera"/> right now and queues a frame to be
	/// rendered on the GPU. As soon as the GPU is ready, the frame will be displayed on the target window/buffer at some point in the
	/// future.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Note that this method will block the caller if the GPU's command queue is full, forcing a wait until the next frame can be queued. This is
	/// ultimately the manifestation of a GPU bottleneck and is somewhat normal/expected.
	/// </para>
	/// <para>
	/// This method will <i>also</i> block the caller if <see cref="RendererBuilderConfig.EnableVSync"/> is <c>true</c> until the <see cref="TargetWindow"/>
	/// is ready to refresh.
	/// </para>
	/// <para>
	/// The actual time between this invocation and the frame being rendered depends on the complexity of the frame, the existing workload
	/// of the GPU, the power of the local hardware, and various configuration options (most noticably the <see cref="RendererCreationConfig.GpuSynchronizationFrameBufferCount"/>
	/// used to create this renderer with).
	/// </para>
	/// </remarks>
	/// <seealso cref="RenderAndWaitForGpu"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Render() => Implementation.Render(_handle);

	/// <summary>
	/// Waits for all previously-submitted <see cref="Render"/> invocations to complete. This blocks the caller until the GPU render
	/// queue has caught up and output all previously-queued frames.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WaitForGpu() => Implementation.WaitForGpu(_handle);

	/// <summary>
	/// Invokes <see cref="Render"/> and <see cref="WaitForGpu"/>, blocking the caller of this method until the frame is actually fully rendered to the target window/buffer.
	/// </summary>
	/// <remarks>
	/// <para>
	/// In cases where you need the render data before continuing (i.e. you're capturing the data) this function provides the simplest API surface. Once this method
	/// returns you can be sure the target window/buffer has been written to with the snapshot of the <see cref="TargetScene"/>. 
	/// </para>
	/// <para>
	/// Note however that this method severely impacts overall throughput (i.e. your maximum framerate) and should not be used for the majority of realtime rendering
	/// pipeline renders. 
	/// </para>
	/// </remarks>
	public void RenderAndWaitForGpu() {
		Render();
		WaitForGpu();
	}

	/// <summary>
	/// Sets the quality level that all future <see cref="Render"/> invocations will use.
	/// </summary>
	/// <param name="qualityPreset">The quality level to set. The default is <see cref="BuiltInQualityConfiguration.High"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetQuality(BuiltInQualityConfiguration qualityPreset) => SetQuality(new RenderQualityConfig(qualityPreset));
	/// <summary>
	/// Sets the quality level that all future <see cref="Render"/> invocations will use.
	/// </summary>
	/// <param name="newQualityConfig">The new quality configuration.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetQuality(RenderQualityConfig newQualityConfig) => Implementation.SetQualityConfig(_handle, newQualityConfig);

	/// <summary>
	/// Enables or disables "frustum culling", a performance optimisation that skips the rendering of objects deemed to be entirely outside the camera's view.
	/// By default, frustum culling is enabled.
	/// </summary>
	/// <remarks>
	/// Usually you'll want to leave this enabled unless you're working with mutable vertices or objects whose bounding boxes are otherwise incalculable.
	/// </remarks>
	/// <param name="enabled">Whether or not to enable this performance optimisation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetFrustumCullingEnabled(bool enabled) => Implementation.SetFrustumCullingEnabled(_handle, enabled);

	/// <summary>
	/// Captures a single frame of the <see cref="TargetScene"/> and renders it to a bitmap file at the requested <paramref name="bitmapFilePath"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// For performance reasons it's not usually possible to actually read back the render target's buffer data; therefore this method re-renders the entire target/scene in to a temporary
	/// readback buffer. This means that the state of the scene/camera as they are <i>now</i> are captured, not as they were at the most recent <see cref="Render"/>.
	/// </para>
	/// <para>
	/// Because of this it should follow naturally that this method incurs a steep performance penalty and should be used sparingly. If you want continuous CPU-readable streaming, you should
	/// be rendering in to a <see cref="RenderOutputBuffer"/> instead.
	/// </para>
	/// </remarks>
	/// <param name="bitmapFilePath">The file path to write the bitmap to.</param>
	/// <param name="saveConfig">Configuration options for saving the bitmap. If <c>null</c> a 24-bit, 3-channel bitmap will be saved with no flipping.</param>
	/// <param name="captureResolution">The resolution to capture a screenshot at. If <c>null</c> this renderer's target window/buffer's size will be copied.</param>
	/// <exception cref="System.IO.IOException">Thrown if TinyFFR could not write to the requested <paramref name="bitmapFilePath"/>.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CaptureScreenshot(ReadOnlySpan<char> bitmapFilePath, BitmapSaveConfig? saveConfig = null, XYPair<int>? captureResolution = null) => Implementation.CaptureScreenshot(_handle, bitmapFilePath, saveConfig, captureResolution);

	/// <summary>
	/// Captures a single frame of the <see cref="TargetScene"/> and passes the captured texel data to the given <paramref name="handler"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The handler must accept two arguments:
	/// <ul>
	/// <li>The first argument is an <see cref="XYPair{T}"/> that details the dimensions of the captured screenshot data</li>
	/// <li>The second argument is a <see cref="ReadOnlySpan{T}"/> containing the texel data. The span's length will be equal to the <see cref="XYPair{T}.Area">Area</see> of the first argument.</li>
	/// </ul>
	/// The given span's referred memory is only valid to use/access until <paramref name="handler"/> returns. The memory is laid out in rows (i.e. for a 1920x1080 screenshot, the first 1920 texels are row 1,
	/// the texels from 1920 to 3840 are row 2, etc). 
	/// </para>
	/// </remarks>
	/// <param name="handler">The function that will be invoked when the rendered screenshot is ready for processing. Must not be null.</param>
	/// <param name="captureResolution">The resolution to capture a screenshot at. If <c>null</c> this renderer's target window/buffer's size will be copied.</param>
	/// <param name="presentFrameTopToBottom">If <c>true</c>, the first row in the given texel data will be the top of the screenshot, if <c>false</c> it will be the bottom.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CaptureScreenshot(Action<XYPair<int>, ReadOnlySpan<TexelRgba32>> handler, XYPair<int>? captureResolution = null, bool presentFrameTopToBottom = false) => Implementation.CaptureScreenshot(_handle, handler, captureResolution, presentFrameTopToBottom);

	/// <inheritdoc cref="CaptureScreenshot(System.Action{XYPair{System.Int32}, System.ReadOnlySpan{TexelRgba32}}, System.Nullable{XYPair{System.Int32}}, System.Boolean)" />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe void CaptureScreenshot(delegate* managed<XYPair<int>, ReadOnlySpan<TexelRgba32>, void> handler, XYPair<int>? captureResolution = null, bool presentFrameTopToBottom = false) => Implementation.CaptureScreenshot(_handle, handler, captureResolution, presentFrameTopToBottom);

	/// <summary>
	/// Creates a <see cref="Ray"/> whose <see cref="Ray.StartPoint">StartPoint</see> starts at the requested <paramref name="pixelCoord"/>;
	/// looking "out" in to the scene from the <see cref="TargetCamera"/>'s view.
	/// </summary>
	/// <remarks>
	/// This function creates a ray as follows:
	/// <ul>
	/// <li>Its <see cref="Ray.StartPoint"/> is located at the point on the <see cref="TargetCamera"/>'s current near plane linked to the requested
	/// <paramref name="pixelCoord"/></li>
	/// <li>Its <see cref="Ray.Direction"/> points out in to the scene away from the camera's near plane. The direction is a continuation of the line from the camera's
	/// actual position to its near plane.</li>
	/// </ul>
	/// </remarks>
	/// <param name="pixelCoord">The co-ordinate of the pixel on the target window/buffer to generate a ray from.
	/// The co-ordinate is specified relative to the target window/buffer's dimensions; use <see cref="CreateRayFromRenderSubAreaSurface"/>
	/// to specify an offset within the render sub-area specifically.</param>
	/// <param name="coordOrigin">Which corner of the target window/buffer should be considered as <c>(0, 0)</c>. Defaults to <see cref="DiagonalOrientation2D.UpLeft"/>.</param>
	/// <param name="disableDpiScalingAdjustment">If <c>true</c>, host/OS DPI adjustment will be disabled for this calculation. This is useful if you've already pre-adjusted for DPI
	/// before invoking this method; but in most cases this should be left at its default value of <c>false</c>.</param>
	/// <seealso cref="CreateRayFromRenderSubAreaSurface"/>
	/// <seealso cref="PickModelInstanceFromRenderSurface"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray CreateRayFromRenderSurface(XYPair<int> pixelCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft, bool disableDpiScalingAdjustment = false) => Implementation.CreateRayFromRenderSurface(_handle, pixelCoord, coordOrigin, disableDpiScalingAdjustment);

	/// <summary>
	/// Executes the same function as <see cref="CreateRayFromRenderSurface"/> but the requested <paramref name="pixelCoord"/> is specified relative to
	/// the render sub-area of this Renderer. If no sub-area has been set (or the sub-area is exactly 100% of the target window/buffer), this function
	/// produces an identical result. 
	/// </summary>
	/// <remarks>
	/// This function creates a ray as follows:
	/// <ul>
	/// <li>Its <see cref="Ray.StartPoint"/> is located at the point on the <see cref="TargetCamera"/>'s current near plane linked to the requested
	/// <paramref name="pixelCoord"/></li>
	/// <li>Its <see cref="Ray.Direction"/> points out in to the scene away from the camera's near plane. The direction is a continuation of the line from the camera's
	/// actual position to its near plane.</li>
	/// </ul>
	/// </remarks>
	/// <param name="pixelCoord">The co-ordinate of the pixel in this renderer's render sub-area to generate a ray from.
	/// The co-ordinate is specified relative to the sub-area; use <see cref="CreateRayFromRenderSurface"/>
	/// to specify an offset within the entire target window/buffer.</param>
	/// <param name="coordOrigin">Which corner of the sub-area should be considered as <c>(0, 0)</c>. Defaults to <see cref="DiagonalOrientation2D.UpLeft"/>.</param>
	/// <param name="disableDpiScalingAdjustment">If <c>true</c>, host/OS DPI adjustment will be disabled for this calculation. This is useful if you've already pre-adjusted for DPI
	/// before invoking this method; but in most cases this should be left at its default value of <c>false</c>.</param>
	/// <seealso cref="CreateRayFromRenderSurface"/>
	/// <seealso cref="PickModelInstanceFromRenderSurface"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray CreateRayFromRenderSubAreaSurface(XYPair<int> pixelCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft, bool disableDpiScalingAdjustment = false) => Implementation.CreateRayFromViewportSurface(_handle, pixelCoord, coordOrigin, disableDpiScalingAdjustment);
	
	/// <summary>
	/// Attempts to determine which <see cref="ModelInstance"/> was rendered under the selected <paramref name="pixelCoord"/> (in the most recently-rendered frame).
	/// </summary>
	/// <param name="pixelCoord">The co-ordinate of the pixel on the target window/buffer to pick.
	/// The co-ordinate is specified relative to the target window/buffer's dimensions; use <see cref="CreateRayFromRenderSubAreaSurface"/>
	/// to specify an offset within the render sub-area specifically.</param>
	/// <param name="includeTransparentObjects">If <c>true</c> objects with alpha transparency will also be considered for picking.
	/// <c>false</c> by default as it has a performance cost and you may often actually wish to ignore objects with transparency.</param>
	/// <param name="coordOrigin">Which corner of the target window/buffer should be considered as <c>(0, 0)</c>. Defaults to <see cref="DiagonalOrientation2D.UpLeft"/>.</param>
	/// <param name="disableDpiScalingAdjustment">If <c>true</c>, host/OS DPI adjustment will be disabled for this calculation. This is useful if you've already pre-adjusted for DPI
	/// before invoking this method; but in most cases this should be left at its default value of <c>false</c>.</param>
	/// <returns>A <see cref="PixelPickResult"/> detailing which <see cref="ModelInstance"/> was picked (and where in the <see cref="TargetScene"/> the picked pixel
	/// lies); or <c>null</c> if no <see cref="ModelInstance"/> was found at that pixel location.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PixelPickResult? PickModelInstanceFromRenderSurface(XYPair<int> pixelCoord, bool includeTransparentObjects = false, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft, bool disableDpiScalingAdjustment = false) => Implementation.PickModelInstanceFromRenderSurface(_handle, pixelCoord, includeTransparentObjects, coordOrigin, disableDpiScalingAdjustment);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PixelPickResult? PickModelInstanceFromRenderSubAreaSurface(XYPair<int> pixelCoord, bool includeTransparentObjects = false, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft, bool disableDpiScalingAdjustment = false) => Implementation.PickModelInstanceFromViewportSurface(_handle, pixelCoord, includeTransparentObjects, coordOrigin, disableDpiScalingAdjustment);

	/// <summary>
	/// Sets the sub-area of the <see cref="TargetWindow"/> or <see cref="TargetBuffer"/> this renderer should actually render in to.
	/// This is useful for creating picture-in-picture effects or splitscreen effects, often when using this renderer inside a <see cref="RendererCompositor"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method specifies the sub-area via specific pixel values. If the render target is a window, and the window is resized, the sub area
	/// will not change in either size or offset, maintaining the strictly specified <paramref name="pixelOffset"/> and <paramref name="pixelDimensions"/> at
	/// all times.
	/// </para>
	/// <para>
	/// By contrast, <see cref="SetRenderSubAreaFraction"/> works with fractional values and therefore can dynamically update with window resizes.
	/// </para>
	/// </remarks>
	/// <param name="anchor">Which edge or corner of the target window/buffer the <paramref name="pixelOffset"/> is specified relative to.
	/// Can specify <see cref="Orientation2D.None"/> to mean the centre of the window/buffer.</param>
	/// <param name="pixelOffset">
	/// How far from the given <paramref name="anchor"/> the sub-area should "start" at.
	/// Ambiguous values (e.g. a non-zero Y component for a <see cref="Orientation2D.Left">Left</see>-edge <paramref name="anchor"/>) move rightward/upward for positive X/Y and leftward/downward for negative X/Y.
	/// </param>
	/// <param name="pixelDimensions">The size of the sub-area in exact pixel dimensions. Both <c>X</c> and <c>Y</c> must be positive.</param>
	/// <seealso cref="SetRenderSubAreaFraction"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetRenderSubAreaPixels(Orientation2D anchor, XYPair<int> pixelOffset, XYPair<int> pixelDimensions) => Implementation.SetTargetViewportDimensionsByPixel(_handle, anchor, pixelOffset, pixelDimensions);
	
	/// <summary>
	/// Sets the sub-area of the <see cref="TargetWindow"/> or <see cref="TargetBuffer"/> this renderer should actually render in to.
	/// This is useful for creating picture-in-picture effects or splitscreen effects, often when using this renderer inside a <see cref="RendererCompositor"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method specifies the sub-area via fractional values. If the render target is a window, and the window is resized, the sub area
	/// will change in both actual pixel size and offset, maintaining the requested <paramref name="fractionalOffset"/> and <paramref name="fractionalDimensions"/> at
	/// all times.
	/// </para>
	/// <para>
	/// By contrast, <see cref="SetRenderSubAreaPixels"/> works with exact pixel values and therefore remains stable with window resizes.
	/// </para>
	/// </remarks>
	/// <param name="anchor">Which edge or corner of the target window/buffer the <paramref name="fractionalOffset"/> is specified relative to.
	/// Can specify <see cref="Orientation2D.None"/> to mean the centre of the window/buffer.</param>
	/// <param name="fractionalOffset">
	/// How far from the given <paramref name="anchor"/> the sub-area should "start" at, as a fraction of the target window/buffer (e.g. <c>0f</c> means 0%, <c>1f</c> means 100%).
	/// Ambiguous values (e.g. a non-zero Y component for a <see cref="Orientation2D.Left">Left</see>-edge <paramref name="anchor"/>) move rightward/upward for positive X/Y and leftward/downward for negative X/Y.
	/// </param>
	/// <param name="fractionalDimensions">The size of the sub-area as a fraction of the target window/buffer dimensions. Both <c>X</c> and <c>Y</c> must be positive.</param>
	/// <seealso cref="SetRenderSubAreaFraction"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetRenderSubAreaFraction(Orientation2D anchor, XYPair<float> fractionalOffset, XYPair<float> fractionalDimensions) => Implementation.SetTargetViewportDimensionsByFraction(_handle, anchor, fractionalOffset, fractionalDimensions);

	/// <summary>
	/// Gets the current size of the render sub-area for this Renderer, in pixels.
	/// </summary>
	/// <returns>The calculated sub-area dimensions according to previously set sub-area pixel/fraction values and the target window/buffer's current dimensions.
	/// If no sub-area has been set, this will simply return the target window/buffer's dimensions.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<int> GetRenderSubAreaPixelDimensions() => Implementation.GetTargetViewportDimensionsByPixel(_handle);

	/// <summary>
	/// Gets the current offset of the render sub-area for this Renderer, in pixels.
	/// </summary>
	/// <returns>The calculated sub-area offset according to previously set sub-area pixel/fraction values and the target window/buffer's current dimensions.
	/// If no sub-area has been set, this will simply return the target window/buffer's dimensions.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<int> GetRenderSubAreaPixelOffset(DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft) => Implementation.GetTargetViewportOffsetByPixel(_handle, coordOrigin);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void MarkSubAreaAsHandledDownstream(bool isHandledDownstream) => Implementation.MarkSubAreaAsHandledDownstream(_handle, isHandledDownstream);

	/// <inheritdoc />
	public override string ToString() => $"Renderer {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Disposal
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	#region Equality
	/// <inheritdoc />
	public bool Equals(Renderer other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Renderer other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	/// <see cref="Equals(Renderer)"/>
	public static bool operator ==(Renderer left, Renderer right) => left.Equals(right);
	/// <see cref="Equals(Renderer)"/>
	public static bool operator !=(Renderer left, Renderer right) => !left.Equals(right);
	#endregion
}