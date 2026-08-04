// Created on 2026-08-04 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.World;

public readonly record struct CanvasDock {
	public Orientation2D CanvasAnchor { get; init; } = Orientation2D.None;
	public Orientation2D? ElementAnchor { get; init; } = null;
	public XYPair<int> PixelOffset { get; init; } = XYPair<int>.Zero;
	public XYPair<float> FractionalOffset { get; init; } = XYPair<float>.Zero;
	public XYPair<int> PixelSize { get; init; } = XYPair<int>.Zero;
	public XYPair<float> FractionalSize { get; init; } = XYPair<float>.Zero;
	public int Layer { get; init; } = 0;

	public CanvasDock() { }

	public CanvasDock(Orientation2D canvasAnchor, XYPair<int> pixelOffset, XYPair<int> pixelSize, int layer = 0) {
		CanvasAnchor = canvasAnchor;
		PixelOffset = pixelOffset;
		PixelSize = pixelSize;
		Layer = layer;
	}

	public CanvasDock(Orientation2D canvasAnchor, XYPair<float> fractionalOffset, XYPair<float> fractionalSize, int layer = 0) {
		CanvasAnchor = canvasAnchor;
		FractionalOffset = fractionalOffset;
		FractionalSize = fractionalSize;
		Layer = layer;
	}

	public Orientation2D EffectiveElementAnchor => ElementAnchor ?? CanvasAnchor;

	public XYPair<int> ResolveOffset(XYPair<int> canvasSizePixels) {
		return PixelOffset + canvasSizePixels.ScaledByReal(FractionalOffset);
	}

	public XYPair<float> ResolveSize(XYPair<int> canvasSizePixels) {
		return PixelSize.Cast<float>() + canvasSizePixels.Cast<float>().ScaledBy(FractionalSize);
	}
}
