// Created on 2025-08-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;
using static Egodystonic.TinyFFR.Rendering.RenderOutputBufferCreationConfig;

namespace Egodystonic.TinyFFR.Wpf;

/// <summary>
/// A WPF control that displays a TinyFFR scene.
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

	/// <summary>
	/// Identifies the <see cref="Renderer"/> property.
	/// </summary>
	public static readonly DependencyProperty RendererProperty = DependencyProperty.Register(
		nameof(Renderer), 
		typeof(Renderer?),
		typeof(TinyFfrSceneView),
		new PropertyMetadata(null),
		validateValueCallback: newValue => newValue == null || (newValue is Renderer r && BindableRendererImplProvider.IsBindableRenderer(r))
	);
	/// <summary>
	/// Identifies the <see cref="Compositor"/> property.
	/// </summary>
	public static readonly DependencyProperty CompositorProperty = DependencyProperty.Register(
		nameof(Compositor),
		typeof(RendererCompositor?),
		typeof(TinyFfrSceneView),
		new PropertyMetadata(null),
		validateValueCallback: newValue => newValue == null || (newValue is RendererCompositor c && BindableRendererCompositorImplProvider.IsBindableCompositor(c))
	);
	/// <summary>
	/// Identifies the <see cref="FallbackBrush"/> property.
	/// </summary>
	public static readonly DependencyProperty FallbackBrushProperty = DependencyProperty.Register(
		nameof(FallbackBrush),
		typeof(Brush),
		typeof(TinyFfrSceneView),
		new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 30, 22, 22)))
	);
	/// <summary>
	/// Identifies the <see cref="InternalRenderResolution"/> property.
	/// </summary>
	public static readonly DependencyProperty InternalRenderResolutionProperty = DependencyProperty.Register(
		nameof(InternalRenderResolution),
		typeof(Size?),
		typeof(TinyFfrSceneView),
		new PropertyMetadata(null),
		validateValueCallback: newValue => newValue is null or Size { Width: >= MinTextureDimensionXY and <= MaxTextureDimensionXY, Height: >= MinTextureDimensionXY and <= MaxTextureDimensionXY }
	);
	/// <summary>
	/// Identifies the <see cref="InternalRenderResolutionMax"/> property.
	/// </summary>
	public static readonly DependencyProperty InternalRenderResolutionMaxProperty = DependencyProperty.Register(
		nameof(InternalRenderResolutionMax),
		typeof(Size?),
		typeof(TinyFfrSceneView),
		new PropertyMetadata(null),
		validateValueCallback: newValue => newValue is null or Size { Width: >= MinTextureDimensionXY and <= MaxTextureDimensionXY, Height: >= MinTextureDimensionXY and <= MaxTextureDimensionXY }
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
		get => (Renderer?)GetValue(RendererProperty);
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
		get => (RendererCompositor?)GetValue(CompositorProperty);
		set => SetValue(CompositorProperty, value);
	}
	/// <summary>
	/// The brush used to fill this control whilst it has nothing to display.
	/// </summary>
	/// <remarks>
	/// This is what is shown before the first frame arrives or when no <see cref="Renderer"/> or <see cref="Compositor"/> is bound.
	/// </remarks>
	public Brush FallbackBrush {
		get => (Brush)GetValue(FallbackBrushProperty);
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
		get => (Size?)GetValue(InternalRenderResolutionProperty);
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
		get => (Size?)GetValue(InternalRenderResolutionMaxProperty);
		set => SetValue(InternalRenderResolutionMaxProperty, value);
	}
	Size BoundsSize => new(ActualWidth, ActualHeight);

	/// <summary>
	/// Constructs a new <see cref="TinyFfrSceneView"/>.
	/// </summary>
	/// <remarks>
	/// The control is made focusable so that it can take keyboard focus when clicked, which is what allows keyboard input to
	/// reach TinyFFR rather than whatever else is on screen.
	/// </remarks>
	public TinyFfrSceneView() {
		Focusable = true;
		RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
	}

	/// <summary>
	/// Re-evaluates the internal render resolution when the display's scaling factor changes, then defers to the base implementation.
	/// </summary>
	/// <param name="oldDpi">The previous scaling factor.</param>
	/// <param name="newDpi">The new scaling factor.</param>
	protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi) {
		base.OnDpiChanged(oldDpi, newDpi);
		IdempotentlyUpdateRendererStateAccordingToControlState();
	}

	/// <summary>
	/// Takes keyboard focus when the control is clicked, then defers to the base implementation.
	/// </summary>
	/// <remarks>
	/// Keyboard input only reaches TinyFFR whilst this control has focus, so that typing elsewhere in your application
	/// does not also drive your scene.
	/// </remarks>
	/// <param name="e">The event data.</param>
	protected override void OnMouseDown(MouseButtonEventArgs e) {
		base.OnMouseDown(e);
		if (Focusable && !IsKeyboardFocusWithin) Focus();
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
		if (_bitmap == null || _bitmap.PixelWidth != dimensions.X || _bitmap.PixelHeight != dimensions.Y) {
			// This DPI must remain 96 regardless of the display's actual DPI: it is what makes the bitmap's device-independent
			// Width/Height equal its PixelWidth/PixelHeight, which is what DrawImage uses to map the source into the destination rect
			_bitmap = new WriteableBitmap(
				dimensions.X,
				dimensions.Y,
				96d,
				96d,
				PixelFormats.Bgra32,
				null
			);
		}

		try {
			_bitmap.Lock();

			if (_bitmap.Format != PixelFormats.Bgra32
				|| _bitmap.PixelWidth != dimensions.X
				|| _bitmap.PixelHeight != dimensions.Y
				|| _bitmap.BackBufferStride < dimensions.X * sizeof(TexelRgba32)) {
				throw new InvalidOperationException("Write to locked buffer failed safety check.");
			}
			if (_bitmap.BackBufferStride == dimensions.X * TexelRgba32.TexelSizeBytes) {
				var destSpan = new Span<byte>((void*) _bitmap.BackBuffer, texels.Length * TexelRgba32.TexelSizeBytes);
				IntegrationUtils.BlitRgbaToBgra(texels, destSpan);
			}
			else {
				for (var r = 0; r < dimensions.Y; ++r) {
					var destSpan = new Span<byte>(((byte*) _bitmap.BackBuffer) + (r * _bitmap.BackBufferStride), dimensions.X * TexelRgba32.TexelSizeBytes);
					IntegrationUtils.BlitRgbaToBgra(texels[(r * dimensions.X)..((r + 1) * dimensions.X)], destSpan);
				}
			}
			_bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
		}
		finally {
			_bitmap.Unlock();
		}

		InvalidateVisual();
	}



	/// <summary>
	/// Draws the most recent frame, or fills the control with <see cref="FallbackBrush"/> if there is not one yet.
	/// </summary>
	/// <param name="context">The drawing context to draw in to.</param>
	protected override void OnRender(DrawingContext context) {
		base.OnRender(context);

		if (_bitmap == null) {
			context.DrawRectangle(FallbackBrush, null, new Rect(BoundsSize));
			return;
		}

		context.DrawImage(_bitmap, new Rect(BoundsSize));
	}

	/// <summary>
	/// Starts or stops receiving frames when the control is added to or removed from the visual tree, then defers to the base implementation.
	/// </summary>
	/// <param name="oldParent">The previous parent, if any.</param>
	protected override void OnVisualParentChanged(DependencyObject oldParent) {
		base.OnVisualParentChanged(oldParent);
		IdempotentlyUpdateRendererStateAccordingToControlState();
	}

	/// <summary>
	/// Re-evaluates which renderer to receive frames from, and at what resolution, when a relevant property changes.
	/// </summary>
	/// <remarks>
	/// Changing the bound renderer or compositor to <see langword="null"/> also detaches the previous one.
	/// </remarks>
	/// <param name="e">The event data.</param>
	protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
		base.OnPropertyChanged(e);
		if (e.Property == IsVisibleProperty || e.Property == ActualWidthProperty || e.Property == ActualHeightProperty || e.Property == InternalRenderResolutionProperty || e.Property == InternalRenderResolutionMaxProperty) {
			IdempotentlyUpdateRendererStateAccordingToControlState();
		}
		else if (e.Property == RendererProperty) {
			if (e.NewValue == null && e.OldValue is Renderer oldRenderer) {
				BindableRendererImplProvider.StopHandlingFrames(oldRenderer);
			}
			IdempotentlyUpdateRendererStateAccordingToControlState();
		}
		else if (e.Property == CompositorProperty) {
			if (e.NewValue == null && e.OldValue is RendererCompositor oldCompositor) {
				BindableRendererCompositorImplProvider.StopHandlingFrames(oldCompositor);
			}
			IdempotentlyUpdateRendererStateAccordingToControlState();
		}
	}

	void IdempotentlyUpdateRendererStateAccordingToControlState() {
		var dpi = VisualTreeHelper.GetDpi(this);
		var cursorCoordinateSpaceSize = BoundsSize.AsXyPair().Cast<int>();
		var targetSize = IntegrationUtils.ClampToMaxPreservingAspectRatio(
			InternalRenderResolution is { } explicitResolution
				? explicitResolution.AsXyPair().Cast<int>()
				: BoundsSize.AsXyPair().ScaledBy(new XYPair<double>(dpi.DpiScaleX, dpi.DpiScaleY)).CastWithRoundingIfNecessary<double, int>(),
			InternalRenderResolutionMax?.AsXyPair().Cast<int>()
		);

		var targetSizeIsPermitted =
			(targetSize.X is >= MinTextureDimensionXY and <= MaxTextureDimensionXY)
			&& (targetSize.Y is >= MinTextureDimensionXY and <= MaxTextureDimensionXY);

		var rendererLocal = Renderer;
		var compositorLocal = Compositor;
		var shouldDisableFrameCapture = (rendererLocal == null && compositorLocal == null) || !IsVisible || VisualParent == null || !targetSizeIsPermitted;
		if (shouldDisableFrameCapture) {
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