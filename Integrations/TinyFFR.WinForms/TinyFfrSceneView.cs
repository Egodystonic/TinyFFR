using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Rendering;
using System;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using static Egodystonic.TinyFFR.Rendering.RenderOutputBufferCreationConfig;

namespace Egodystonic.TinyFFR.WinForms;

/// <summary>
/// A Windows Forms control that displays a TinyFFR scene.
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
public partial class TinyFfrSceneView : UserControl {
	Bitmap? _bitmap;
	Renderer? _renderer;
	RendererCompositor? _compositor;
	Size? _internalRenderResolution;
	Size? _internalRenderResolutionMax;

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
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Renderer? Renderer {
		get => _renderer;
		set {
			if (value != null && !BindableRendererImplProvider.IsBindableRenderer(value.Value)) {
				throw new ArgumentException($"{nameof(Renderer)} must be bindable.", nameof(Renderer));
			}


			if (value == null && _renderer is { } oldRenderer) {
				BindableRendererImplProvider.StopHandlingFrames(oldRenderer);
			}
			_renderer = value;

			IdempotentlyUpdateRendererStateAccordingToControlState();
		}
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
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public RendererCompositor? Compositor {
		get => _compositor;
		set {
			if (value != null && !BindableRendererCompositorImplProvider.IsBindableCompositor(value.Value)) {
				throw new ArgumentException($"{nameof(Compositor)} must be bindable.", nameof(Compositor));
			}


			if (value == null && _compositor is { } oldCompositor) {
				BindableRendererCompositorImplProvider.StopHandlingFrames(oldCompositor);
			}
			_compositor = value;

			IdempotentlyUpdateRendererStateAccordingToControlState();
		}
	}

	/// <summary>
	/// The brush used to fill this control whilst it has nothing to display.
	/// </summary>
	/// <remarks>
	/// This is what is shown before the first frame arrives or when no <see cref="Renderer"/> or <see cref="Compositor"/> is bound.
	/// </remarks>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public Brush FallbackBrush { get; set; } = new SolidBrush(Color.FromArgb(255, 30, 22, 22));
	
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
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public Size? InternalRenderResolution {
		get => _internalRenderResolution;
		set {
			if (value is not (null or { Width: >= MinTextureDimensionXY and <= MaxTextureDimensionXY, Height: >= MinTextureDimensionXY and <= MaxTextureDimensionXY })) {
				throw new ArgumentException($"{nameof(InternalRenderResolution)} Width/Height must be between {MinTextureDimensionXY} and {MaxTextureDimensionXY}.", nameof(InternalRenderResolution));
			}

			_internalRenderResolution = value;
			IdempotentlyUpdateRendererStateAccordingToControlState();
		}
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
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public Size? InternalRenderResolutionMax {
		get => _internalRenderResolutionMax;
		set {
			if (value is not (null or { Width: >= MinTextureDimensionXY and <= MaxTextureDimensionXY, Height: >= MinTextureDimensionXY and <= MaxTextureDimensionXY })) {
				throw new ArgumentException($"{nameof(InternalRenderResolutionMax)} Width/Height must be between {MinTextureDimensionXY} and {MaxTextureDimensionXY}.", nameof(InternalRenderResolutionMax));
			}

			_internalRenderResolutionMax = value;
			IdempotentlyUpdateRendererStateAccordingToControlState();
		}
	}

	/// <summary>
	/// Constructs a new <see cref="TinyFfrSceneView"/>.
	/// </summary>
	/// <remarks>
	/// The control is made focusable so that it can take keyboard focus when clicked, which is what allows keyboard input to
	/// reach TinyFFR rather than whatever else is on screen.
	/// </remarks>
	public TinyFfrSceneView() {
		InitializeComponent();

		SetStyle(
			ControlStyles.AllPaintingInWmPaint |
			ControlStyles.UserPaint |
			ControlStyles.OptimizedDoubleBuffer,
			true
		);

		SetStyle(ControlStyles.Selectable, true);
		TabStop = true;
	}

	/// <summary>
	/// Takes keyboard focus when the control is clicked, then defers to the base implementation.
	/// </summary>
	/// <remarks>
	/// Keyboard input only reaches TinyFFR whilst this control has focus, so that typing elsewhere in your application
	/// does not also drive your scene.
	/// </remarks>
	/// <param name="e">The event data.</param>
	protected override void OnMouseDown(MouseEventArgs e) {
		base.OnMouseDown(e);
		if (CanFocus && !Focused) Focus();
	}

	/// <summary>
	/// Reports that the arrow keys and similar navigation keys should be delivered to this control rather than used to move between controls.
	/// </summary>
	/// <remarks>
	/// Without this the arrow keys would move focus around the form instead of reaching TinyFFR, which would make them unusable
	/// for camera movement.
	/// </remarks>
	/// <param name="keyData">The key being tested.</param>
	protected override bool IsInputKey(Keys keyData) {
		return (keyData & Keys.KeyCode) switch {
			Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Tab => true,
			_ => base.IsInputKey(keyData)
		};
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
		if (_bitmap == null || _bitmap.Width != dimensions.X || _bitmap.Height != dimensions.Y) {
			_bitmap?.Dispose();
			_bitmap = new Bitmap(
				dimensions.X,
				dimensions.Y,
				PixelFormat.Format32bppRgb
			);
		}

		var data = _bitmap.LockBits(
			new Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
			ImageLockMode.WriteOnly,
			PixelFormat.Format32bppRgb
		);
		try {
			if (data.PixelFormat != PixelFormat.Format32bppRgb
				|| data.Width != dimensions.X
				|| data.Height != dimensions.Y
				|| data.Stride < dimensions.X * sizeof(TexelRgba32)) {
				throw new InvalidOperationException("Write to locked buffer failed safety check.");
			}
			if (data.Stride == dimensions.X * TexelRgba32.TexelSizeBytes) {
				var destSpan = new Span<byte>((void*) data.Scan0, texels.Length * TexelRgba32.TexelSizeBytes);
				IntegrationUtils.BlitRgbaToBgra(texels, destSpan);
			}
			else {
				for (var r = 0; r < dimensions.Y; ++r) {
					var destSpan = new Span<byte>(((byte*) data.Scan0) + (r * data.Stride), dimensions.X * TexelRgba32.TexelSizeBytes);
					IntegrationUtils.BlitRgbaToBgra(texels[(r * dimensions.X)..((r + 1) * dimensions.X)], destSpan);
				}
			}
		}
		finally {
			_bitmap.UnlockBits(data);
		}

		Invalidate();
	}

	/// <summary>
	/// Draws the most recent frame, or fills the control with <see cref="FallbackBrush"/> if there is not one yet.
	/// </summary>
	/// <param name="e">The event data, including the graphics surface to draw on to.</param>
	protected override void OnPaint(PaintEventArgs e) {
		base.OnPaint(e);

		if (_bitmap == null) {
			e.Graphics.FillRectangle(FallbackBrush, ClientRectangle);
			return;
		}

		if (_bitmap.Width == ClientSize.Width && _bitmap.Height == ClientSize.Height) {
			e.Graphics.DrawImageUnscaled(_bitmap, 0, 0);
			return;
		}

		e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
		e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
		e.Graphics.DrawImage(_bitmap, new Rectangle(Point.Empty, ClientSize));
	}

	/// <summary>
	/// Starts or stops receiving frames when the control is re-parented, then defers to the base implementation.
	/// </summary>
	/// <param name="e">The event data.</param>
	protected override void OnParentChanged(EventArgs e) {
		base.OnParentChanged(e);
		IdempotentlyUpdateRendererStateAccordingToControlState();
	}

	/// <summary>
	/// Starts or stops receiving frames when the control is shown or hidden, then defers to the base implementation.
	/// </summary>
	/// <param name="e">The event data.</param>
	protected override void OnVisibleChanged(EventArgs e) {
		base.OnVisibleChanged(e);
		IdempotentlyUpdateRendererStateAccordingToControlState();
	}

	/// <summary>
	/// Re-evaluates the internal render resolution when the control is resized, then defers to the base implementation.
	/// </summary>
	/// <param name="e">The event data.</param>
	protected override void OnClientSizeChanged(EventArgs e) {
		base.OnClientSizeChanged(e);
		IdempotentlyUpdateRendererStateAccordingToControlState();
	}

	/// <summary>
	/// Re-evaluates the internal render resolution when the display's scaling factor changes, then defers to the base implementation.
	/// </summary>
	/// <param name="e">The event data.</param>
	protected override void OnDpiChangedAfterParent(EventArgs e) {
		base.OnDpiChangedAfterParent(e);
		IdempotentlyUpdateRendererStateAccordingToControlState();
	}

	void IdempotentlyUpdateRendererStateAccordingToControlState() {
		var cursorCoordinateSpaceSize = ClientSize.AsXyPair().Cast<int>();
		var targetSize = IntegrationUtils.ClampToMaxPreservingAspectRatio(
			(InternalRenderResolution ?? ClientSize).AsXyPair().Cast<int>(),
			InternalRenderResolutionMax?.AsXyPair().Cast<int>()
		);

		var targetSizeIsPermitted =
			(targetSize.X is >= MinTextureDimensionXY and <= MaxTextureDimensionXY)
			&& (targetSize.Y is >= MinTextureDimensionXY and <= MaxTextureDimensionXY);

		var rendererLocal = Renderer;
		var compositorLocal = Compositor;
		var shouldDisableFrameCapture = (rendererLocal == null && compositorLocal == null) || !Visible || Parent == null || !targetSizeIsPermitted;
		if (shouldDisableFrameCapture) {
			_bitmap?.Dispose();
			_bitmap = null;
			if (rendererLocal != null) {
				BindableRendererImplProvider.StopHandlingFrames(rendererLocal.Value);
			}
			if (compositorLocal != null) {
				BindableRendererCompositorImplProvider.StopHandlingFrames(compositorLocal.Value);
			}
			Invalidate();
			return;
		}

		if (rendererLocal != null && compositorLocal != null) {
			throw new InvalidOperationException($"Only one of {nameof(Renderer)} or {nameof(Compositor)} may be set on a {nameof(TinyFfrSceneView)}.");
		}

		if (rendererLocal != null) BindableRendererImplProvider.StartOrContinueHandlingFrames(rendererLocal.Value, targetSize, cursorCoordinateSpaceSize, WriteFrame);
		else BindableRendererCompositorImplProvider.StartOrContinueHandlingFrames(compositorLocal!.Value, targetSize, cursorCoordinateSpaceSize, WriteFrame);
	}
}

