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
	TopLevel? _scalingChangeSubscriptionTarget;

	public static readonly StyledProperty<Renderer?> RendererProperty = AvaloniaProperty.Register<TinyFfrSceneView, Renderer?>(
		nameof(Renderer), 
		null,
		validate: newValue => newValue == null || BindableRendererImplProvider.IsBindableRenderer(newValue.Value)
	);
	public static readonly StyledProperty<RendererCompositor?> CompositorProperty = AvaloniaProperty.Register<TinyFfrSceneView, RendererCompositor?>(
		nameof(Compositor),
		null,
		validate: newValue => newValue == null || BindableRendererCompositorImplProvider.IsBindableCompositor(newValue.Value)
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
	public RendererCompositor? Compositor {
		get => GetValue(CompositorProperty);
		set => SetValue(CompositorProperty, value);
	}
	public IBrush FallbackBrush {
		get => GetValue(FallbackBrushProperty);
		set => SetValue(FallbackBrushProperty, value);
	}
	public Size? InternalRenderResolution {
		get => GetValue(InternalRenderResolutionProperty);
		set => SetValue(InternalRenderResolutionProperty, value);
	}

	public TinyFfrSceneView() {
		Focusable = true;
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e) {
		base.OnPointerPressed(e);
		if (Focusable && !IsFocused) Focus();
	}

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
		// A scaling change usually leaves the device-independent Bounds untouched, so watching BoundsProperty alone is not enough
		_scalingChangeSubscriptionTarget = TopLevel.GetTopLevel(this);
		if (_scalingChangeSubscriptionTarget != null) _scalingChangeSubscriptionTarget.ScalingChanged += HandleScalingChanged;
		IdempotentlyUpdateRendererStateAccordingToControlState();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) {
		if (_scalingChangeSubscriptionTarget != null) {
			_scalingChangeSubscriptionTarget.ScalingChanged -= HandleScalingChanged;
			_scalingChangeSubscriptionTarget = null;
		}
		base.OnDetachedFromVisualTree(e);
		IdempotentlyUpdateRendererStateAccordingToControlState();
	}

	void HandleScalingChanged(object? sender, EventArgs e) => IdempotentlyUpdateRendererStateAccordingToControlState();

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
		else if (change.Property == CompositorProperty) {
			if (change.NewValue == null && change.OldValue is RendererCompositor oldCompositor) {
				BindableRendererCompositorImplProvider.StopHandlingFrames(oldCompositor);
			}
			IdempotentlyUpdateRendererStateAccordingToControlState();
		}
	}

	void IdempotentlyUpdateRendererStateAccordingToControlState() {
		var visualRoot = this.GetVisualRoot();

		var cursorCoordinateSpaceSize = Bounds.Size.AsXyPair().Cast<int>();
		var targetSize = InternalRenderResolution is { } explicitResolution
			? explicitResolution.AsXyPair().Cast<int>()
			: Bounds.Size.AsXyPair().ScaledBy(new XYPair<double>(visualRoot?.RenderScaling ?? 1d)).CastWithRoundingIfNecessary<double, int>();

		var targetSizeIsPermitted =
			(targetSize.X is >= MinTextureDimensionXY and <= MaxTextureDimensionXY)
			&& (targetSize.Y is >= MinTextureDimensionXY and <= MaxTextureDimensionXY);

		var rendererLocal = Renderer;
		var compositorLocal = Compositor;
		var shouldDisableFrameCapture = (rendererLocal == null && compositorLocal == null) || !IsVisible || visualRoot == null || !targetSizeIsPermitted;
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