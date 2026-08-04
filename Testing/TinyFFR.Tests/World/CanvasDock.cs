// Created on 2026-08-04 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.World;

[TestFixture]
class CanvasDockTest {
	const float TestTolerance = 0.001f;
	static readonly XYPair<int> CanvasSize = new(640, 480);

	[Test]
	public void ShouldDefaultToZeroOffsetAndSize() {
		var dock = new CanvasDock();

		Assert.AreEqual(Orientation2D.None, dock.CanvasAnchor);
		Assert.AreEqual(Orientation2D.None, dock.EffectiveElementAnchor);
		Assert.AreEqual(XYPair<int>.Zero, dock.ResolveOffset(CanvasSize));
		AssertToleranceEquals(XYPair<float>.Zero, dock.ResolveSize(CanvasSize), TestTolerance);
		Assert.AreEqual(0, dock.Layer);
	}

	[Test]
	public void ShouldResolvePixelOnlyDocks() {
		var dock = new CanvasDock(Orientation2D.UpLeft, new XYPair<int>(16, 24), new XYPair<int>(128, 64), layer: 3);

		Assert.AreEqual(new XYPair<int>(16, 24), dock.ResolveOffset(CanvasSize));
		AssertToleranceEquals(new XYPair<float>(128f, 64f), dock.ResolveSize(CanvasSize), TestTolerance);
		Assert.AreEqual(3, dock.Layer);
	}

	[Test]
	public void ShouldResolveFractionOnlyDocks() {
		var dock = new CanvasDock(Orientation2D.Right, new XYPair<float>(0.1f, 0.25f), new XYPair<float>(0.5f, 1f));

		Assert.AreEqual(new XYPair<int>(64, 120), dock.ResolveOffset(CanvasSize));
		AssertToleranceEquals(new XYPair<float>(320f, 480f), dock.ResolveSize(CanvasSize), TestTolerance);
	}

	[Test]
	public void ShouldSumPixelAndFractionalComponents() {
		var dock = new CanvasDock {
			CanvasAnchor = Orientation2D.Down,
			PixelOffset = (10, 20),
			FractionalOffset = (0.5f, 0.25f),
			PixelSize = (0, 32),
			FractionalSize = (0.5f, 0f)
		};

		Assert.AreEqual(new XYPair<int>(10 + 320, 20 + 120), dock.ResolveOffset(CanvasSize));
		AssertToleranceEquals(new XYPair<float>(320f, 32f), dock.ResolveSize(CanvasSize), TestTolerance);
	}

	[Test]
	public void ShouldFallBackToAnchorForElementAnchor() {
		Assert.AreEqual(Orientation2D.UpLeft, new CanvasDock { CanvasAnchor = Orientation2D.UpLeft }.EffectiveElementAnchor);
		Assert.AreEqual(
			Orientation2D.DownRight,
			new CanvasDock { CanvasAnchor = Orientation2D.UpLeft, ElementAnchor = Orientation2D.DownRight }.EffectiveElementAnchor
		);
	}

	[Test]
	public void ShouldScaleFractionalComponentsWithCanvasSize() {
		var dock = new CanvasDock(Orientation2D.None, new XYPair<float>(0.5f, 0.5f), new XYPair<float>(0.5f, 0.5f));

		AssertToleranceEquals(new XYPair<float>(320f, 240f), dock.ResolveSize(CanvasSize), TestTolerance);
		AssertToleranceEquals(new XYPair<float>(640f, 480f), dock.ResolveSize(CanvasSize * 2), TestTolerance);
	}
}
