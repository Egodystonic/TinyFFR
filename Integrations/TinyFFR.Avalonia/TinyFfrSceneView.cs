// Created on 2025-08-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Avalonia;

public class TinyFfrSceneView : Control {
	readonly IPen _noRenderPen = new Pen(new SolidColorBrush(new Color(255, 0, 0, 0), 1d));
	WriteableBitmap? _bitmap;

	public static readonly StyledProperty<Renderer?> RendererProperty = AvaloniaProperty.Register<TinyFfrSceneView, Renderer?>(
		nameof(Renderer), 
		null,
		validate: newValue => newValue == null || BindableRendererImplProvider.IsBindableRenderer(newValue.Value)
	);

	public Renderer? Renderer {
		get => GetValue(RendererProperty);
		set => SetValue(RendererProperty, value);
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

		using var lockedBuffer = _bitmap.Lock();
		if (lockedBuffer.Format != PixelFormats.Rgb24
			|| lockedBuffer.Size.Width != dimensions.X
			|| lockedBuffer.Size.Height != dimensions.Y
			|| lockedBuffer.RowBytes != dimensions.X * TexelRgb24.TexelSizeBytes) {
			throw new InvalidOperationException("Write to locked buffer failed safety check.");
		}
		var destAsSpan = new Span<TexelRgb24>((void*) lockedBuffer.Address, dimensions.Area);
		texels.CopyTo(destAsSpan);

		InvalidateVisual();
	}

	public override void Render(DrawingContext context) {
		base.Render(context);

		if (_bitmap == null) {
			context.DrawRectangle(_noRenderPen, new Rect(Bounds.Size));
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
		if (change.Property == IsVisibleProperty || change.Property == BoundsProperty) {
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
		var rendererLocal = Renderer;
		var shouldDisableFrameCapture = rendererLocal == null || !IsVisible || this.GetVisualRoot() == null;
		if (shouldDisableFrameCapture) {
			_bitmap?.Dispose();
			_bitmap = null;
			if (rendererLocal != null) {
				BindableRendererImplProvider.StopHandlingFrames(rendererLocal.Value);
			}
			return;
		}
		
#pragma warning disable CS8629 // Nullable value type may be null: Nope, it's checked above
		BindableRendererImplProvider.StartOrContinueHandlingFrames(rendererLocal.Value, Bounds.Size.AsXyPair().Cast<int>(), WriteFrame);
#pragma warning restore CS8629 
	}
}