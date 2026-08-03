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

public class TinyFfrSceneView : Control {
	WriteableBitmap? _bitmap;

	public static readonly DependencyProperty RendererProperty = DependencyProperty.Register(
		nameof(Renderer), 
		typeof(Renderer?),
		typeof(TinyFfrSceneView),
		new PropertyMetadata(null),
		validateValueCallback: newValue => newValue == null || (newValue is Renderer r && BindableRendererImplProvider.IsBindableRenderer(r))
	);
	public static readonly DependencyProperty CompositorProperty = DependencyProperty.Register(
		nameof(Compositor),
		typeof(RendererCompositor?),
		typeof(TinyFfrSceneView),
		new PropertyMetadata(null),
		validateValueCallback: newValue => newValue == null || (newValue is RendererCompositor c && BindableRendererCompositorImplProvider.IsBindableCompositor(c))
	);
	public static readonly DependencyProperty FallbackBrushProperty = DependencyProperty.Register(
		nameof(FallbackBrush),
		typeof(Brush),
		typeof(TinyFfrSceneView),
		new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 30, 22, 22)))
	);
	public static readonly DependencyProperty InternalRenderResolutionProperty = DependencyProperty.Register(
		nameof(InternalRenderResolution),
		typeof(Size?),
		typeof(TinyFfrSceneView),
		new PropertyMetadata(null),
		validateValueCallback: newValue => newValue is null or Size { Width: >= MinTextureDimensionXY and <= MaxTextureDimensionXY, Height: >= MinTextureDimensionXY and <= MaxTextureDimensionXY }
	);

	public Renderer? Renderer {
		get => (Renderer?)GetValue(RendererProperty);
		set => SetValue(RendererProperty, value);
	}
	public RendererCompositor? Compositor {
		get => (RendererCompositor?)GetValue(CompositorProperty);
		set => SetValue(CompositorProperty, value);
	}
	public Brush FallbackBrush {
		get => (Brush)GetValue(FallbackBrushProperty);
		set => SetValue(FallbackBrushProperty, value);
	}
	public Size? InternalRenderResolution {
		get => (Size?)GetValue(InternalRenderResolutionProperty);
		set => SetValue(InternalRenderResolutionProperty, value);
	}
	Size BoundsSize => new(ActualWidth, ActualHeight);

	public TinyFfrSceneView() {
		Focusable = true;
		RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
	}

	protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi) {
		base.OnDpiChanged(oldDpi, newDpi);
		IdempotentlyUpdateRendererStateAccordingToControlState();
	}

	protected override void OnMouseDown(MouseButtonEventArgs e) {
		base.OnMouseDown(e);
		if (Focusable && !IsKeyboardFocusWithin) Focus();
	}

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



	protected override void OnRender(DrawingContext context) {
		base.OnRender(context);

		if (_bitmap == null) {
			context.DrawRectangle(FallbackBrush, null, new Rect(BoundsSize));
			return;
		}

		context.DrawImage(_bitmap, new Rect(BoundsSize));
	}

	protected override void OnVisualParentChanged(DependencyObject oldParent) {
		base.OnVisualParentChanged(oldParent);
		IdempotentlyUpdateRendererStateAccordingToControlState();
	}

	protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
		base.OnPropertyChanged(e);
		if (e.Property == IsVisibleProperty || e.Property == ActualWidthProperty || e.Property == ActualHeightProperty || e.Property == InternalRenderResolutionProperty) {
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
		var targetSize = InternalRenderResolution is { } explicitResolution
			? explicitResolution.AsXyPair().Cast<int>()
			: BoundsSize.AsXyPair().ScaledBy(new XYPair<double>(dpi.DpiScaleX, dpi.DpiScaleY)).CastWithRoundingIfNecessary<double, int>();

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