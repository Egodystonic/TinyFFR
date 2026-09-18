// Created on 2025-08-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;
using static Egodystonic.TinyFFR.Rendering.RenderOutputBufferCreationConfig;

namespace Egodystonic.TinyFFR.Avalonia;

/// <summary>
/// An Avalonia control that displays a TinyFFR scene.
/// </summary>
/// <remarks>
/// <para>
/// Bind a renderer to <see cref="Renderer"/> (or a compositor to <see cref="Compositor"/>) and every call to its
/// <c>Render()</c>/<c>RenderAll()</c> function updates what this control shows.
/// The renderer must have been created with <c>IRendererBuilder.CreateBindableRenderer()</c>
/// (or compositor via <c>IRendererBuilder.CreateBindableCompositor()</c>); an ordinary renderer/compositor can not be bound.
/// </para>
/// <para>
/// Whilst nothing is bound, or before the first frame has been rendered, the control is filled with
/// <see cref="FallbackBrush"/>. Do not leave a disposed renderer bound; set the property to <see langword="null"/>
/// first.
/// </para>
/// </remarks>
public class TinyFfrSceneView : Control {
	WriteableBitmap? _bitmap;
	TopLevel? _scalingChangeSubscriptionTarget;

	/// <summary>
	/// Identifies the <see cref="Renderer"/> property.
	/// </summary>
	public static readonly StyledProperty<Renderer?> RendererProperty = AvaloniaProperty.Register<TinyFfrSceneView, Renderer?>(
		nameof(Renderer), 
		null,
		validate: newValue => newValue == null || BindableRendererImplProvider.IsBindableRenderer(newValue.Value)
	);
	/// <summary>
	/// Identifies the <see cref="Compositor"/> property.
	/// </summary>
	public static readonly StyledProperty<RendererCompositor?> CompositorProperty = AvaloniaProperty.Register<TinyFfrSceneView, RendererCompositor?>(
		nameof(Compositor),
		null,
		validate: newValue => newValue == null || BindableRendererCompositorImplProvider.IsBindableCompositor(newValue.Value)
	);
	/// <summary>
	/// Identifies the <see cref="FallbackBrush"/> property.
	/// </summary>
	public static readonly StyledProperty<IBrush> FallbackBrushProperty = AvaloniaProperty.Register<TinyFfrSceneView, IBrush>(
		nameof(FallbackBrush),
		new SolidColorBrush(new Color(255, 30, 22, 22))
	);
	/// <summary>
	/// Identifies the <see cref="InternalRenderResolution"/> property.
	/// </summary>
	public static readonly StyledProperty<Size?> InternalRenderResolutionProperty = AvaloniaProperty.Register<TinyFfrSceneView, Size?>(
		nameof(InternalRenderResolution),
		null,
		validate: newValue => newValue is not { } size || size is { Width: >= MinTextureDimensionXY and <= MaxTextureDimensionXY, Height: >= MinTextureDimensionXY and <= MaxTextureDimensionXY }
	);
	/// <summary>
	/// Identifies the <see cref="InternalRenderResolutionMax"/> property.
	/// </summary>
	public static readonly StyledProperty<Size?> InternalRenderResolutionMaxProperty = AvaloniaProperty.Register<TinyFfrSceneView, Size?>(
		nameof(InternalRenderResolutionMax),
		null,
		validate: newValue => newValue is not { } size || size is { Width: >= MinTextureDimensionXY and <= MaxTextureDimensionXY, Height: >= MinTextureDimensionXY and <= MaxTextureDimensionXY }
	);

	/// <summary>
	/// The renderer whose output this control displays, or <see langword="null"/> for none.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This must be a renderer created with <c>IRendererBuilder.CreateBindableRenderer()</c>; assigning any other renderer is rejected. Each call
	/// to its <c>Render()</c> function updates this control.
	/// </para>
	/// <para>
	/// Only one of this and <see cref="Compositor"/> may be set at a time. Setting this to <see langword="null"/> detaches
	/// the renderer and returns the control to <see cref="FallbackBrush"/>.
	/// </para>
	/// </remarks>
	public Renderer? Renderer {
		get => GetValue(RendererProperty);
		set => SetValue(RendererProperty, value);
	}
	/// <summary>
	/// The compositor whose combined output this control displays, or <see langword="null"/> for none.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This must be a compositor created with <c>IRendererBuilder.CreateBindableCompositor()</c>; assigning any other compositor is rejected. Each call
	/// to its <c>RenderAll()</c> function updates this control.
	/// </para>
	/// <para>
	/// Only one of this and <see cref="Renderer"/> may be set at a time. Setting this to <see langword="null"/> detaches
	/// the compositor and returns the control to <see cref="FallbackBrush"/>.
	/// </para>
	/// </remarks>
	public RendererCompositor? Compositor {
		get => GetValue(CompositorProperty);
		set => SetValue(CompositorProperty, value);
	}
	/// <summary>
	/// The brush used to fill this control whilst it has nothing to display.
	/// </summary>
	/// <remarks>
	/// This is what is shown before the first frame arrives or when no <see cref="Renderer"/> or <see cref="Compositor"/> is bound.
	/// </remarks>
	public IBrush FallbackBrush {
		get => GetValue(FallbackBrushProperty);
		set => SetValue(FallbackBrushProperty, value);
	}
	/// <summary>
	/// The resolution scenes are rendered at internally before being scaled to fit this control, or <see langword="null"/> to follow the control's own size.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is a literal count of pixels and is <i>not</i> multiplied by the display's scaling factor. Leaving it
	/// <see langword="null"/> renders at the control's size in physical pixels, which keeps the image sharp on displays
	/// scaled to something other than 100%.
	/// </para>
	/// <para>
	/// To set a maximum cap on render resolution that preserves aspect ratio, use <see cref="InternalRenderResolutionMax"/> instead.
	/// </para>
	/// </remarks>
	public Size? InternalRenderResolution {
		get => GetValue(InternalRenderResolutionProperty);
		set => SetValue(InternalRenderResolutionProperty, value);
	}
	/// <summary>
	/// An upper bound on the internal render resolution, or <see langword="null"/> for none.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Unlike <see cref="InternalRenderResolution"/> this does not fix the resolution; it caps it. Whenever the control
	/// would otherwise render at more than this on either axis, the resolution is scaled down to fit whilst keeping its
	/// aspect ratio: a control that would render at 2000x1500 with a maximum of 1000x1000 renders at 1000x750.
	/// </para>
	/// <para>
	/// If both properties are set, both apply: one selects the resolution and this then caps it. Like the other, this is a
	/// literal count of pixels, and both components must be in the range <c>1 &lt;= n &lt;= 32768</c>.
	/// </para>
	/// </remarks>
	public Size? InternalRenderResolutionMax {
		get => GetValue(InternalRenderResolutionMaxProperty);
		set => SetValue(InternalRenderResolutionMaxProperty, value);
	}

	/// <summary>
	/// Constructs a new <see cref="TinyFfrSceneView"/>.
	/// </summary>
	/// <remarks>
	/// The control is made focusable so that it can take keyboard focus when clicked, which is what allows keyboard input to
	/// reach TinyFFR rather than whatever else is on screen.
	/// </remarks>
	public TinyFfrSceneView() {
		Focusable = true;
	}

	/// <summary>
	/// Takes keyboard focus when the control is clicked, then defers to the base implementation.
	/// </summary>
	/// <remarks>
	/// Keyboard input only reaches TinyFFR whilst this control has focus, so that typing elsewhere in your application
	/// does not also drive your scene.
	/// </remarks>
	/// <param name="e">The event data.</param>
	protected override void OnPointerPressed(PointerPressedEventArgs e) {
		base.OnPointerPressed(e);
		if (Focusable && !IsFocused) Focus();
	}

	/// <summary>
	/// Receives a finished frame from the bound renderer and displays it.
	/// </summary>
	/// <remarks>
	/// This is called by the renderer this control is bound to; there is no need to call it yourself.
	/// </remarks>
	/// <param name="dimensions">The width and height of the frame, in pixels.</param>
	/// <param name="texels">The frame's pixels, laid out row by row.</param>
	public unsafe void WriteFrame(XYPair<int> dimensions, ReadOnlySpan<TexelRgba32> texels) {
		if (_bitmap == null || _bitmap.PixelSize.Width != dimensions.X || _bitmap.PixelSize.Height != dimensions.Y) {
			_bitmap?.Dispose();
			// This DPI must remain 96 regardless of the display's actual DPI: Bitmap.Size is PixelSize / (Dpi / 96), and
			// DrawImage derives its source rectangle from Bitmap.Size, so any other value would sample only part of the buffer
			_bitmap = new WriteableBitmap(
				new PixelSize(dimensions.X, dimensions.Y),
				new Vector(96d, 96d),
				PixelFormats.Rgba8888,
				AlphaFormat.Opaque
			);
		}

		using (var lockedBuffer = _bitmap.Lock()) {
			if (lockedBuffer.Format != PixelFormats.Rgba8888
				|| lockedBuffer.Size.Width != dimensions.X
				|| lockedBuffer.Size.Height != dimensions.Y
				|| lockedBuffer.RowBytes < dimensions.X * sizeof(TexelRgba32)) {
				throw new InvalidOperationException("Write to locked buffer failed safety check.");
			}
			if (lockedBuffer.RowBytes == dimensions.X * TexelRgba32.TexelSizeBytes) {
				var destSpan = new Span<TexelRgba32>((void*) lockedBuffer.Address, texels.Length);
				texels.CopyTo(destSpan);
			}
			else {
				for (var r = 0; r < dimensions.Y; ++r) {
					var destSpan = new Span<TexelRgba32>(((byte*) lockedBuffer.Address) + (r * lockedBuffer.RowBytes), dimensions.X);
					texels[(r * dimensions.X)..((r + 1) * dimensions.X)].CopyTo(destSpan);
				}
			}
		}

		InvalidateVisual();
	}

	/// <summary>
	/// Draws the most recent frame, or fills the control with <see cref="FallbackBrush"/> if there is not one yet.
	/// </summary>
	/// <param name="context">The drawing context to draw in to.</param>
	public override void Render(DrawingContext context) {
		base.Render(context);

		if (_bitmap == null) {
			context.DrawRectangle(FallbackBrush, null, new Rect(Bounds.Size));
			return;
		}

		context.DrawImage(_bitmap, new Rect(Bounds.Size));
	}

	/// <summary>
	/// Starts receiving frames when the control is added to the visual tree, then defers to the base implementation.
	/// </summary>
	/// <param name="e">The event data.</param>
	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) {
		base.OnAttachedToVisualTree(e);
		// A scaling change usually leaves the device-independent Bounds untouched, so watching BoundsProperty alone is not enough
		_scalingChangeSubscriptionTarget = TopLevel.GetTopLevel(this);
		if (_scalingChangeSubscriptionTarget != null) _scalingChangeSubscriptionTarget.ScalingChanged += HandleScalingChanged;
		IdempotentlyUpdateRendererStateAccordingToControlState();
	}

	/// <summary>
	/// Stops receiving frames when the control is removed from the visual tree, then defers to the base implementation.
	/// </summary>
	/// <param name="e">The event data.</param>
	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) {
		if (_scalingChangeSubscriptionTarget != null) {
			_scalingChangeSubscriptionTarget.ScalingChanged -= HandleScalingChanged;
			_scalingChangeSubscriptionTarget = null;
		}
		base.OnDetachedFromVisualTree(e);
		IdempotentlyUpdateRendererStateAccordingToControlState();
	}

	void HandleScalingChanged(object? sender, EventArgs e) => IdempotentlyUpdateRendererStateAccordingToControlState();

	/// <summary>
	/// Re-evaluates which renderer to receive frames from, and at what resolution, when a relevant property changes.
	/// </summary>
	/// <remarks>
	/// Changing the bound renderer or compositor to <see langword="null"/> also detaches the previous one.
	/// </remarks>
	/// <param name="change">The event data.</param>
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
		base.OnPropertyChanged(change);
		if (change.Property == IsVisibleProperty || change.Property == BoundsProperty || change.Property == InternalRenderResolutionProperty || change.Property == InternalRenderResolutionMaxProperty) {
			IdempotentlyUpdateRendererStateAccordingToControlState();
		}
		else if (change.Property == RendererProperty) {
			if (change.NewValue == null && change.OldValue is Renderer oldRenderer) {
				BindableRendererImplProvider.StopHandlingFrames(oldRenderer);
			}
			IdempotentlyUpdateRendererStateAccordingToControlState();
		}
		else if (change.Property == CompositorProperty) {
			if (change.NewValue == null && change.OldValue is RendererCompositor oldCompositor) {
				BindableRendererCompositorImplProvider.StopHandlingFrames(oldCompositor);
			}
			IdempotentlyUpdateRendererStateAccordingToControlState();
		}
	}

	void IdempotentlyUpdateRendererStateAccordingToControlState() {
		var topLevel = TopLevel.GetTopLevel(this);

		var cursorCoordinateSpaceSize = Bounds.Size.AsXyPair().Cast<int>();
		var targetSize = IntegrationUtils.ClampToMaxPreservingAspectRatio(
			InternalRenderResolution is { } explicitResolution
				? explicitResolution.AsXyPair().Cast<int>()
				: Bounds.Size.AsXyPair().ScaledBy(new XYPair<double>(topLevel?.RenderScaling ?? 1d)).CastWithRoundingIfNecessary<double, int>(),
			InternalRenderResolutionMax?.AsXyPair().Cast<int>()
		);

		var targetSizeIsPermitted =
			(targetSize.X is >= MinTextureDimensionXY and <= MaxTextureDimensionXY)
			&& (targetSize.Y is >= MinTextureDimensionXY and <= MaxTextureDimensionXY);

		var rendererLocal = Renderer;
		var compositorLocal = Compositor;
		var shouldDisableFrameCapture = (rendererLocal == null && compositorLocal == null) || !IsVisible || topLevel == null || !targetSizeIsPermitted;
		if (shouldDisableFrameCapture) {
			_bitmap?.Dispose();
			_bitmap = null;
			if (rendererLocal != null) {
				BindableRendererImplProvider.StopHandlingFrames(rendererLocal.Value);
			}
			if (compositorLocal != null) {
				BindableRendererCompositorImplProvider.StopHandlingFrames(compositorLocal.Value);
			}
			InvalidateVisual();
			return;
		}

		if (rendererLocal != null && compositorLocal != null) {
			throw new InvalidOperationException($"Only one of {nameof(Renderer)} or {nameof(Compositor)} may be set on a {nameof(TinyFfrSceneView)}.");
		}

		if (rendererLocal != null) BindableRendererImplProvider.StartOrContinueHandlingFrames(rendererLocal.Value, targetSize, cursorCoordinateSpaceSize, WriteFrame);
		else BindableRendererCompositorImplProvider.StartOrContinueHandlingFrames(compositorLocal!.Value, targetSize, cursorCoordinateSpaceSize, WriteFrame);
	}
}