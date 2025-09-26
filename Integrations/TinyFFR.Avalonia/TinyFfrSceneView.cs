// Created on 2025-08-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;
using static Egodystonic.TinyFFR.Rendering.RenderOutputBufferCreationConfig;

namespace Egodystonic.TinyFFR.Avalonia;

public class TinyFfrSceneView : Control {
	WriteableBitmap? _bitmap;

	public static readonly StyledProperty<Renderer?> RendererProperty = AvaloniaProperty.Register<TinyFfrSceneView, Renderer?>(
		nameof(Renderer), 
		null,
		validate: newValue => newValue == null || BindableRendererImplProvider.IsBindableRenderer(newValue.Value)
	);
	public static readonly StyledProperty<IBrush> FallbackBrushProperty = AvaloniaProperty.Register<TinyFfrSceneView, IBrush>(
		nameof(FallbackBrush),
		new SolidColorBrush(new Color(255, 30, 22, 22))
	);
	public static readonly StyledProperty<Size?> InternalRenderResolutionProperty = AvaloniaProperty.Register<TinyFfrSceneView, Size?>(
		nameof(InternalRenderResolution),
		null,
		validate: newValue => newValue is not { } size || size is { Width: >= MinTextureDimensionXY and <= MaxTextureDimensionXY, Height: >= MinTextureDimensionXY and <= MaxTextureDimensionXY }
	);

	public Renderer? Renderer {
		get => GetValue(RendererProperty);
		set => SetValue(RendererProperty, value);
	}
	public IBrush FallbackBrush {
		get => GetValue(FallbackBrushProperty);
		set => SetValue(FallbackBrushProperty, value);
	}
	public Size? InternalRenderResolution {
		get => GetValue(InternalRenderResolutionProperty);
		set => SetValue(InternalRenderResolutionProperty, value);
	}

	public unsafe void WriteFrame(XYPair<int> dimensions, ReadOnlySpan<TexelRgb24> texels) {
		if (_bitmap == null || _bitmap.PixelSize.Width != dimensions.X || _bitmap.PixelSize.Height != dimensions.Y) {
			_bitmap?.Dispose();
			_bitmap = new WriteableBitmap(
				new PixelSize(dimensions.X, dimensions.Y),
				new Vector(96d, 96d),
				PixelFormats.Rgb24,
				AlphaFormat.Opaque
			);
		}

		using (var lockedBuffer = _bitmap.Lock()) {
			if (lockedBuffer.Format != PixelFormats.Rgb24
				|| lockedBuffer.Size.Width != dimensions.X
				|| lockedBuffer.Size.Height != dimensions.Y
				|| lockedBuffer.RowBytes < dimensions.X * sizeof(TexelRgb24)) {
				throw new InvalidOperationException("Write to locked buffer failed safety check.");
			}
			if (lockedBuffer.RowBytes == dimensions.X * TexelRgb24.TexelSizeBytes) {
				var destSpan = new Span<TexelRgb24>((void*) lockedBuffer.Address, texels.Length);
				texels.CopyTo(destSpan);
			}
			else {
				for (var r = 0; r < dimensions.Y; ++r) {
					var destSpan = new Span<TexelRgb24>(((byte*) lockedBuffer.Address) + (r * lockedBuffer.RowBytes), dimensions.X);
					texels[(r * dimensions.X)..((r + 1) * dimensions.X)].CopyTo(destSpan);
				}
			}
		}

		InvalidateVisual();
	}

	public override void Render(DrawingContext context) {
		base.Render(context);

		if (_bitmap == null) {
			context.DrawRectangle(FallbackBrush, null, new Rect(Bounds.Size));
			return;
		}

		context.DrawImage(_bitmap, new Rect(Bounds.Size));
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) {
		base.OnAttachedToVisualTree(e);
		IdempotentlyUpdateRendererStateAccordingToControlState();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) {
		base.OnDetachedFromVisualTree(e);
		IdempotentlyUpdateRendererStateAccordingToControlState();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
		base.OnPropertyChanged(change);
		if (change.Property == IsVisibleProperty || change.Property == BoundsProperty || change.Property == InternalRenderResolutionProperty) {
			IdempotentlyUpdateRendererStateAccordingToControlState();
		}
		else if (change.Property == RendererProperty) {
			if (change.NewValue == null && change.OldValue is Renderer oldRenderer) {
				BindableRendererImplProvider.StopHandlingFrames(oldRenderer);
			}
			IdempotentlyUpdateRendererStateAccordingToControlState();
		}
	}

	void IdempotentlyUpdateRendererStateAccordingToControlState() {
		var targetSize = (InternalRenderResolution ?? Bounds.Size).AsXyPair().Cast<int>();
		
		var targetSizeIsPermitted = 
			(targetSize.X is >= MinTextureDimensionXY and <= MaxTextureDimensionXY)
			&& (targetSize.Y is >= MinTextureDimensionXY and <= MaxTextureDimensionXY);

		var rendererLocal = Renderer;
		var shouldDisableFrameCapture = rendererLocal == null || !IsVisible || this.GetVisualRoot() == null || !targetSizeIsPermitted;
		if (shouldDisableFrameCapture) {
			_bitmap?.Dispose();
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