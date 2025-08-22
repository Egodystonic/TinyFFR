// Created on 2025-08-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Avalonia;

public class TinyFfrSceneView : Control {
	readonly IPen _noRenderPen = new Pen(new SolidColorBrush(new Color(255, 0, 0, 0), 1d));
	WriteableBitmap? _bitmap;

	public static readonly StyledProperty<Renderer?> RendererProperty = AvaloniaProperty.Register<TinyFfrSceneView, Renderer?>(nameof(Renderer), null); // TODO validation

	public Renderer? Renderer {
		get => GetValue(RendererProperty);
		set => SetValue(RendererProperty, value);
	}

	public TinyFfrSceneView() {
		_iterateRendererFunc = IterateRenderer;
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

	public void RenderFrame(TimeSpan? deltaTime = null) {
		if (_renderer is not { } renderer) return;

		_preFrameRender?.Invoke(deltaTime);
		renderer.Render();
		_postFrameRender?.Invoke(deltaTime);
	}

	public override void Render(DrawingContext context) {
		base.Render(context);

		if (_bitmap == null) {
			context.DrawRectangle(_noRenderPen, new Rect(Bounds.Size));
			return;
		}

		context.DrawImage(_bitmap, new Rect(Bounds.Size));
	}

	bool IterateRenderer() {
		if (Loop is not { } loop) return false;
		if (loop.TryIterateOnce(out var dt)) RenderFrame(dt);
		return true;
	}

	unsafe void RecreateRendererAccordingToProperties() {
		if (_renderer is { } r) {
			r.Dispose();
			_renderer = null;
			_tickTimerDisposable?.Dispose();
			_tickTimerDisposable = null;
			_outputBuffer?.Dispose();
			_outputBuffer = null;
			_bitmap?.Dispose();
			_bitmap = null;
		}

		if (Loop is not { } loop
			|| Scene is not { } scene
			|| Camera is not { } camera
			|| RendererBuilder is not { } builder
			|| !IsVisible || VisualRoot == null) {
			return;
		}

		var boundsAsXyPair = Bounds.Size.AsXyPair().Cast<int>();
		_outputBuffer = builder.CreateRenderOutputBuffer(boundsAsXyPair);
		_renderer = builder.CreateRenderer(scene, camera, _outputBuffer.Value);
		_bitmap = new WriteableBitmap(
			new PixelSize(boundsAsXyPair.X, boundsAsXyPair.Y), 
			new Vector(96d, 96d), 
			PixelFormats.Rgb24, 
			AlphaFormat.Opaque 
		);
		_outputBuffer.Value.StartReadingFrames((dimensions, texels) => {
			using var lockedBuffer = _bitmap.Lock();
			if (lockedBuffer.Format != PixelFormats.Rgb24
				|| lockedBuffer.Size.Width != dimensions.X 
				|| lockedBuffer.Size.Height != dimensions.Y
				|| lockedBuffer.RowBytes != dimensions.X * TexelRgb24.TexelSizeBytes) return;
			var destAsSpan = new Span<TexelRgb24>((void*) lockedBuffer.Address, dimensions.Area);
			texels.CopyTo(destAsSpan);
		});
		_tickTimerDisposable = DispatcherTimer.Run(
			_iterateRendererFunc, loop.DesiredIterationInterval, DispatcherPriority.Render
		);
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) {
		base.OnAttachedToVisualTree(e);
		RecreateRendererAccordingToProperties();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) {
		base.OnDetachedFromVisualTree(e);
		RecreateRendererAccordingToProperties();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
		base.OnPropertyChanged(change);
		// TODO handle renderer changed
		if (change.Property != IsVisibleProperty) return;
		RecreateRendererAccordingToProperties();
	}
}