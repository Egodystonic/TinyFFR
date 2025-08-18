// Created on 2025-08-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Runtime.InteropServices;
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

namespace TinyFFR.Avalonia;

public class TinyFfrSceneView : Control {
	readonly IPen _noRenderPen = new Pen(new SolidColorBrush(new Color(255, 0, 0, 0), 1d));
	readonly Func<bool> _iterateRendererFunc;
	Action<TimeSpan>? _preFrameRender;
	Action<TimeSpan>? _postFrameRender;
	WriteableBitmap? _bitmap;
	RenderOutputBuffer? _outputBuffer;
	Renderer? _renderer;
	IDisposable? _tickTimerDisposable;

	public static readonly StyledProperty<ApplicationLoop?> LoopProperty = AvaloniaProperty.Register<TinyFfrSceneView, ApplicationLoop?>(nameof(Loop), null);
	public static readonly StyledProperty<Scene?> SceneProperty = AvaloniaProperty.Register<TinyFfrSceneView, Scene?>(nameof(Scene), null);
	public static readonly StyledProperty<Camera?> CameraProperty = AvaloniaProperty.Register<TinyFfrSceneView, Camera?>(nameof(Camera), null);
	public static readonly StyledProperty<IRendererBuilder?> RendererBuilderProperty = AvaloniaProperty.Register<TinyFfrSceneView, IRendererBuilder?>(nameof(RendererBuilder), null);

	public event Action<TimeSpan> PreFrameRender {
		add => _preFrameRender += value;
		remove => _preFrameRender -= value;
	}
	public event Action<TimeSpan> PostFrameRender {
		add => _postFrameRender += value;
		remove => _postFrameRender -= value;
	}

	public ApplicationLoop? Loop {
		get => GetValue(LoopProperty);
		set => SetValue(LoopProperty, value);
	}

	public Scene? Scene {
		get => GetValue(SceneProperty);
		set => SetValue(SceneProperty, value);
	}

	public Camera? Camera {
		get => GetValue(CameraProperty);
		set => SetValue(CameraProperty, value);
	}

	public IRendererBuilder? RendererBuilder {
		get => GetValue(RendererBuilderProperty);
		set => SetValue(RendererBuilderProperty, value);
	}

	public TinyFfrSceneView() {
		_iterateRendererFunc = IterateRenderer;
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
		if (Loop is not { } loop
			|| _renderer is not { } renderer) {
			return false;
		}

		if (!loop.TryIterateOnce(out var dt)) return true;
		_preFrameRender?.Invoke(dt);
		renderer.Render();
		_postFrameRender?.Invoke(dt);
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
		if (change.Property != IsVisibleProperty) return;
		RecreateRendererAccordingToProperties();
	}
}