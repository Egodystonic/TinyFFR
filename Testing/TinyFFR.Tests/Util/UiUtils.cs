// Created on 2026-05-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR;

[TestFixture]
class UiUtilsTest {
	[Test]
	public void ShouldThrowForInvalidCanvasOrigin() {
		Assert.Throws<ArgumentOutOfRangeException>(() => UiUtils.TranslateAnchoredCanvasOffset(XYPair<int>.One, (DiagonalOrientation2D) 1234567, Orientation2D.UpRight, XYPair<int>.Zero));
		Assert.Throws<ArgumentOutOfRangeException>(() => UiUtils.GetAnchoredCanvasAreaStartCoord(XYPair<int>.One, (DiagonalOrientation2D) 1234567, Orientation2D.UpRight, XYPair<int>.Zero, XYPair<int>.One));
	}

	[Test]
	public void ShouldCorrectlyTranslateAnchoredCanvasOffsetForCentreOrigin() {
		var canvasSize = new XYPair<int>(100, 80);

		void AssertCombination(XYPair<int> expectation, Orientation2D anchor, XYPair<int> offset) {
			Assert.AreEqual(
				expectation,
				UiUtils.TranslateAnchoredCanvasOffset(canvasSize, DiagonalOrientation2D.None, anchor, offset)
			);
		}

		AssertCombination((0, 0), Orientation2D.None, (0, 0));

		AssertCombination((50, 40), Orientation2D.UpRight, (0, 0));
		AssertCombination((-50, -40), Orientation2D.DownLeft, (0, 0));
		AssertCombination((-50, 40), Orientation2D.UpLeft, (0, 0));
		AssertCombination((50, -40), Orientation2D.DownRight, (0, 0));
		AssertCombination((0, 40), Orientation2D.Up, (0, 0));
		AssertCombination((0, -40), Orientation2D.Down, (0, 0));
		AssertCombination((50, 0), Orientation2D.Right, (0, 0));
		AssertCombination((-50, 0), Orientation2D.Left, (0, 0));

		var offset = new XYPair<int>(10, 5);
		AssertCombination((10, 5), Orientation2D.None, offset);
		AssertCombination((40, 35), Orientation2D.UpRight, offset);
		AssertCombination((-40, -35), Orientation2D.DownLeft, offset);
		AssertCombination((-40, 35), Orientation2D.UpLeft, offset);
		AssertCombination((40, -35), Orientation2D.DownRight, offset);
		AssertCombination((10, 35), Orientation2D.Up, offset);
		AssertCombination((10, -35), Orientation2D.Down, offset);
		AssertCombination((40, 5), Orientation2D.Right, offset);
		AssertCombination((-40, 5), Orientation2D.Left, offset);
	}

	[Test]
	public void ShouldCorrectlyGetAnchoredCanvasAreaStartCoordForCentreOrigin() {
		var canvasSize = new XYPair<int>(100, 80);
		var offset = new XYPair<int>(10, 5);
		var area = new XYPair<int>(20, 12);

		void AssertCombination(XYPair<int> expectation, Orientation2D anchor) {
			Assert.AreEqual(
				expectation,
				UiUtils.GetAnchoredCanvasAreaStartCoord(canvasSize, DiagonalOrientation2D.None, anchor, offset, area)
			);
		}

		AssertCombination((20, 23), Orientation2D.UpRight);
		AssertCombination((-40, -35), Orientation2D.DownLeft);
		AssertCombination((-40, 23), Orientation2D.UpLeft);
		AssertCombination((20, -35), Orientation2D.DownRight);
		AssertCombination((0, 23), Orientation2D.Up);
		AssertCombination((0, -35), Orientation2D.Down);
		AssertCombination((20, -1), Orientation2D.Right);
		AssertCombination((-40, -1), Orientation2D.Left);
		AssertCombination((0, -1), Orientation2D.None);
	}

	[Test]
	public void ShouldCorrectlyTranslateAnchoredCanvasOffsetNormalizedForCentreOrigin() {
		void AssertCombination(XYPair<float> expectation, Orientation2D anchor) {
			Assert.AreEqual(
				expectation,
				UiUtils.TranslateAnchoredCanvasOffsetNormalized(DiagonalOrientation2D.None, anchor)
			);
		}

		AssertCombination((0f, 0f), Orientation2D.None);
		AssertCombination((0.5f, 0.5f), Orientation2D.UpRight);
		AssertCombination((-0.5f, -0.5f), Orientation2D.DownLeft);
		AssertCombination((-0.5f, 0.5f), Orientation2D.UpLeft);
		AssertCombination((0.5f, -0.5f), Orientation2D.DownRight);
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
