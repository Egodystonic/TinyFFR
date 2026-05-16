// Created on 2026-05-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR;

[TestFixture]
class UiUtilsTest {
	[Test]
	public void ShouldThrowForInvalidCanvasOrigin() {
		Assert.Throws<ArgumentOutOfRangeException>(() => UiUtils.TranslateAnchoredCanvasOffset(XYPair<int>.One, DiagonalOrientation2D.None, Orientation2D.UpRight, XYPair<int>.Zero));
		Assert.Throws<ArgumentOutOfRangeException>(() => UiUtils.TranslateAnchoredCanvasOffset(XYPair<int>.One, (DiagonalOrientation2D) 1234567, Orientation2D.UpRight, XYPair<int>.Zero));
		Assert.Throws<ArgumentOutOfRangeException>(() => UiUtils.GetAnchoredCanvasAreaStartCoord(XYPair<int>.One, DiagonalOrientation2D.None, Orientation2D.UpRight, XYPair<int>.Zero, XYPair<int>.One));
		Assert.Throws<ArgumentOutOfRangeException>(() => UiUtils.GetAnchoredCanvasAreaStartCoord(XYPair<int>.One, (DiagonalOrientation2D) 1234567, Orientation2D.UpRight, XYPair<int>.Zero, XYPair<int>.One));
	}
	
	[Test]
	public void ShouldCorrectlyTranslateAnchoredCanvasOffset() {
		var canvasSize = new XYPair<int>(100, 80);
		var offset = new XYPair<int>(10, 5);
		
		void AssertCombination(XYPair<int> expectation, DiagonalOrientation2D origin, Orientation2D anchor) {
			Assert.AreEqual(
				expectation,
				UiUtils.TranslateAnchoredCanvasOffset(canvasSize, origin, anchor, offset)
			);
		}

		AssertCombination((90, 75), DiagonalOrientation2D.DownLeft, Orientation2D.UpRight);
		AssertCombination((90, 5), DiagonalOrientation2D.UpLeft, Orientation2D.UpRight);
		AssertCombination((10, 75), DiagonalOrientation2D.DownRight, Orientation2D.UpRight);
		AssertCombination((10, 5), DiagonalOrientation2D.UpRight, Orientation2D.UpRight);

		AssertCombination((10, 5), DiagonalOrientation2D.DownLeft, Orientation2D.DownLeft);
		AssertCombination((10, 75), DiagonalOrientation2D.UpLeft, Orientation2D.DownLeft);
		AssertCombination((90, 5), DiagonalOrientation2D.DownRight, Orientation2D.DownLeft);
		AssertCombination((90, 75), DiagonalOrientation2D.UpRight, Orientation2D.DownLeft);

		AssertCombination((10, 75), DiagonalOrientation2D.DownLeft, Orientation2D.UpLeft);
		AssertCombination((10, 5), DiagonalOrientation2D.UpLeft, Orientation2D.UpLeft);
		AssertCombination((90, 75), DiagonalOrientation2D.DownRight, Orientation2D.UpLeft);
		AssertCombination((90, 5), DiagonalOrientation2D.UpRight, Orientation2D.UpLeft);

		AssertCombination((90, 5), DiagonalOrientation2D.DownLeft, Orientation2D.DownRight);
		AssertCombination((90, 75), DiagonalOrientation2D.UpLeft, Orientation2D.DownRight);
		AssertCombination((10, 5), DiagonalOrientation2D.DownRight, Orientation2D.DownRight);
		AssertCombination((10, 75), DiagonalOrientation2D.UpRight, Orientation2D.DownRight);
		
		AssertCombination((60, 75), DiagonalOrientation2D.DownLeft, Orientation2D.Up);
		AssertCombination((60, 5), DiagonalOrientation2D.UpLeft, Orientation2D.Up);
		AssertCombination((40, 75), DiagonalOrientation2D.DownRight, Orientation2D.Up);
		AssertCombination((40, 5), DiagonalOrientation2D.UpRight, Orientation2D.Up);
		
		AssertCombination((60, 5), DiagonalOrientation2D.DownLeft, Orientation2D.Down);
		AssertCombination((60, 75), DiagonalOrientation2D.UpLeft, Orientation2D.Down);
		
		AssertCombination((90, 45), DiagonalOrientation2D.DownLeft, Orientation2D.Right);
		AssertCombination((10, 45), DiagonalOrientation2D.DownRight, Orientation2D.Right);
		AssertCombination((90, 35), DiagonalOrientation2D.UpLeft, Orientation2D.Right);
		
		AssertCombination((10, 45), DiagonalOrientation2D.DownLeft, Orientation2D.Left);
		AssertCombination((90, 35), DiagonalOrientation2D.UpRight, Orientation2D.Left);
		
		AssertCombination((60, 45), DiagonalOrientation2D.DownLeft, Orientation2D.None);
		AssertCombination((60, 35), DiagonalOrientation2D.UpLeft, Orientation2D.None);
		AssertCombination((40, 45), DiagonalOrientation2D.DownRight, Orientation2D.None);
		AssertCombination((40, 35), DiagonalOrientation2D.UpRight, Orientation2D.None);
	}

	[Test]
	public void ShouldCorrectlyGetAnchoredCanvasAreaStartCoord() {
		var canvasSize = new XYPair<int>(100, 80);
		var offset = new XYPair<int>(10, 5);

		void AssertCombination(XYPair<int> expectation, DiagonalOrientation2D origin, Orientation2D anchor, XYPair<int> area) {
			Assert.AreEqual(
				expectation,
				UiUtils.GetAnchoredCanvasAreaStartCoord(canvasSize, origin, anchor, offset, area)
			);
		}

		AssertCombination((70, 63), DiagonalOrientation2D.DownLeft, Orientation2D.UpRight, (20, 12));
		AssertCombination((70, 63), DiagonalOrientation2D.UpLeft, Orientation2D.DownRight, (20, 12));

		AssertCombination((10, 5), DiagonalOrientation2D.DownLeft, Orientation2D.DownLeft, (20, 12));
		AssertCombination((10, 5), DiagonalOrientation2D.UpLeft, Orientation2D.UpLeft, (20, 12));
		AssertCombination((10, 5), DiagonalOrientation2D.DownRight, Orientation2D.DownRight, (20, 12));
		AssertCombination((10, 5), DiagonalOrientation2D.UpRight, Orientation2D.UpRight, (20, 12));

		AssertCombination((50, 39), DiagonalOrientation2D.DownLeft, Orientation2D.None, (20, 12));

		AssertCombination((30, 63), DiagonalOrientation2D.DownRight, Orientation2D.Up, (20, 12));

		AssertCombination((50, 39), DiagonalOrientation2D.DownLeft, Orientation2D.None, (21, 13));
	}
}
