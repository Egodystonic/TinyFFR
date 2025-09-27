using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Rendering;
using System;
using System.ComponentModel;
using System.Drawing.Imaging;
using static Egodystonic.TinyFFR.Rendering.RenderOutputBufferCreationConfig;

namespace Egodystonic.TinyFFR.WinForms;

public partial class TinyFfrSceneView : UserControl {
	Bitmap? _bitmap;
	Renderer? _renderer;
	Size? _internalRenderResolution;

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

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public Brush FallbackBrush { get; set; } = new SolidBrush(Color.FromArgb(255, 30, 22, 22));
	
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

	public TinyFfrSceneView() {
		InitializeComponent();

		SetStyle(
			ControlStyles.AllPaintingInWmPaint |
			ControlStyles.UserPaint |
			ControlStyles.OptimizedDoubleBuffer, 
			true
		);
	}

	public unsafe void WriteFrame(XYPair<int> dimensions, ReadOnlySpan<TexelRgb24> texels) {
		if (_bitmap == null || _bitmap.Width != dimensions.X || _bitmap.Height != dimensions.Y) {
			_bitmap = new Bitmap(
				dimensions.X,
				dimensions.Y,
				PixelFormat.Format24bppRgb
			);
		}

		var data = _bitmap.LockBits(
			new Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
			ImageLockMode.WriteOnly,
			PixelFormat.Format24bppRgb
		);
		try {
			if (data.PixelFormat != PixelFormat.Format24bppRgb
				|| data.Width != dimensions.X
				|| data.Height != dimensions.Y
				|| data.Stride < dimensions.X * sizeof(TexelRgb24)) {
				throw new InvalidOperationException("Write to locked buffer failed safety check.");
			}
			if (data.Stride == dimensions.X * TexelRgb24.TexelSizeBytes) {
				var destSpan = new Span<TexelRgb24>((void*) data.Scan0, texels.Length);
				texels.CopyTo(destSpan);
			}
			else {
				for (var r = 0; r < dimensions.Y; ++r) {
					var destSpan = new Span<TexelRgb24>(((byte*) data.Scan0) + (r * data.Stride), dimensions.X);
					texels[(r * dimensions.X)..((r + 1) * dimensions.X)].CopyTo(destSpan);
				}
			}
		}
		finally {
			_bitmap.UnlockBits(data);
		}

		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e) {
		base.OnPaint(e);

		if (_bitmap == null) {
			e.Graphics.FillRectangle(FallbackBrush, e.ClipRectangle);
			return;
		}

		e.Graphics.DrawImage(_bitmap, e.ClipRectangle);
	}

	protected override void OnParentChanged(EventArgs e) {
		base.OnParentChanged(e);
		IdempotentlyUpdateRendererStateAccordingToControlState();
	}

	protected override void OnVisibleChanged(EventArgs e) {
		base.OnVisibleChanged(e);
		IdempotentlyUpdateRendererStateAccordingToControlState();
	}

	protected override void OnClientSizeChanged(EventArgs e) {
		base.OnClientSizeChanged(e);
		IdempotentlyUpdateRendererStateAccordingToControlState();
	}

	void IdempotentlyUpdateRendererStateAccordingToControlState() {
		var targetSize = (InternalRenderResolution ?? ClientSize).AsXyPair().Cast<int>();

		var targetSizeIsPermitted =
			(targetSize.X is >= MinTextureDimensionXY and <= MaxTextureDimensionXY)
			&& (targetSize.Y is >= MinTextureDimensionXY and <= MaxTextureDimensionXY);

		var rendererLocal = Renderer;
		var shouldDisableFrameCapture = rendererLocal == null || !Visible || Parent == null || !targetSizeIsPermitted;
		if (shouldDisableFrameCapture) {
			_bitmap = null;
			if (rendererLocal != null) {
				BindableRendererImplProvider.StopHandlingFrames(rendererLocal.Value);
			}
			Invalidate();
			return;
		}

#pragma warning disable CS8629 // Nullable value type may be null: Nope, it's checked above
		BindableRendererImplProvider.StartOrContinueHandlingFrames(rendererLocal.Value, targetSize, WriteFrame);
#pragma warning restore CS8629
	}
}

