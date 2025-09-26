// Created on 2025-08-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
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
	public Brush FallbackBrush {
		get => (Brush)GetValue(FallbackBrushProperty);
		set => SetValue(FallbackBrushProperty, value);
	}
	public Size? InternalRenderResolution {
		get => (Size?)GetValue(InternalRenderResolutionProperty);
		set => SetValue(InternalRenderResolutionProperty, value);
	}
	Size BoundsSize => new(ActualWidth, ActualHeight);

	public unsafe void WriteFrame(XYPair<int> dimensions, ReadOnlySpan<TexelRgb24> texels) {
		if (_bitmap == null || _bitmap.PixelWidth != dimensions.X || _bitmap.PixelHeight != dimensions.Y) {
			_bitmap = new WriteableBitmap(
				dimensions.X,
				dimensions.Y,
				96d,
				96d,
				PixelFormats.Rgb24,
				null
			);
		}

		try {
			_bitmap.Lock();

			if (_bitmap.Format != PixelFormats.Rgb24
				|| _bitmap.PixelWidth != dimensions.X
				|| _bitmap.PixelHeight != dimensions.Y
				|| _bitmap.BackBufferStride < dimensions.X * sizeof(TexelRgb24)) {
				throw new InvalidOperationException("Write to locked buffer failed safety check.");
			}
			if (_bitmap.BackBufferStride == dimensions.X * TexelRgb24.TexelSizeBytes) {
				var destSpan = new Span<TexelRgb24>((void*) _bitmap.BackBuffer, texels.Length);
				texels.CopyTo(destSpan);
			}
			else {
				for (var r = 0; r < dimensions.Y; ++r) {
					var destSpan = new Span<TexelRgb24>(((byte*) _bitmap.BackBuffer) + (r * _bitmap.BackBufferStride), dimensions.X);
					texels[(r * dimensions.X)..((r + 1) * dimensions.X)].CopyTo(destSpan);
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
	}

	void IdempotentlyUpdateRendererStateAccordingToControlState() {
		var targetSize = (InternalRenderResolution ?? BoundsSize).AsXyPair().Cast<int>();
		
		var targetSizeIsPermitted = 
			(targetSize.X is >= MinTextureDimensionXY and <= MaxTextureDimensionXY)
			&& (targetSize.Y is >= MinTextureDimensionXY and <= MaxTextureDimensionXY);

		var rendererLocal = Renderer;
		var shouldDisableFrameCapture = rendererLocal == null || !IsVisible || VisualParent == null || !targetSizeIsPermitted;
		if (shouldDisableFrameCapture) {
			_bitmap = null;
			if (rendererLocal != null) {
				BindableRendererImplProvider.StopHandlingFrames(rendererLocal.Value);
			}
			InvalidateVisual();
			return;
		}
		
#pragma warning disable CS8629 // Nullable value type may be null: Nope, it's checked above
		BindableRendererImplProvider.StartOrContinueHandlingFrames(rendererLocal.Value, targetSize, WriteFrame);
#pragma warning restore CS8629 
	}
}