// Created on 2024-02-20 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

[TestFixture]
class Orientation2DTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyCombineBits() {
		Assert.AreEqual((int) HorizontalOrientation2D.Left, (int) Orientation2D.Left);
		Assert.AreEqual((int) HorizontalOrientation2D.Right, (int) Orientation2D.Right);
		Assert.AreEqual((int) VerticalOrientation2D.Up, (int) Orientation2D.Up);
		Assert.AreEqual((int) VerticalOrientation2D.Down, (int) Orientation2D.Down);

		Assert.AreEqual((int) HorizontalOrientation2D.Left | (int) VerticalOrientation2D.Up, (int) Orientation2D.UpLeft);
		Assert.AreEqual((int) HorizontalOrientation2D.Right | (int) VerticalOrientation2D.Up, (int) Orientation2D.UpRight);
		Assert.AreEqual((int) HorizontalOrientation2D.Left | (int) VerticalOrientation2D.Down, (int) Orientation2D.DownLeft);
		Assert.AreEqual((int) HorizontalOrientation2D.Right | (int) VerticalOrientation2D.Down, (int) Orientation2D.DownRight);
	}

	[Test]
	public void ShouldCorrectlyCombineVerticalAndHorizontalDirections() {
		Assert.AreEqual(Orientation2D.None, VerticalOrientation2D.None.Plus(HorizontalOrientation2D.None));
		Assert.AreEqual(Orientation2D.None, HorizontalOrientation2D.None.Plus(VerticalOrientation2D.None));

		Assert.AreEqual(Orientation2D.Up, VerticalOrientation2D.Up.Plus(HorizontalOrientation2D.None));
		Assert.AreEqual(Orientation2D.UpRight, VerticalOrientation2D.Up.Plus(HorizontalOrientation2D.Right));
		Assert.AreEqual(Orientation2D.UpLeft, VerticalOrientation2D.Up.Plus(HorizontalOrientation2D.Left));
		Assert.AreEqual(Orientation2D.Down, VerticalOrientation2D.Down.Plus(HorizontalOrientation2D.None));
		Assert.AreEqual(Orientation2D.DownRight, VerticalOrientation2D.Down.Plus(HorizontalOrientation2D.Right));
		Assert.AreEqual(Orientation2D.DownLeft, VerticalOrientation2D.Down.Plus(HorizontalOrientation2D.Left));

		Assert.AreEqual(Orientation2D.Left, HorizontalOrientation2D.Left.Plus(VerticalOrientation2D.None));
		Assert.AreEqual(Orientation2D.UpLeft, HorizontalOrientation2D.Left.Plus(VerticalOrientation2D.Up));
		Assert.AreEqual(Orientation2D.DownLeft, HorizontalOrientation2D.Left.Plus(VerticalOrientation2D.Down));
		Assert.AreEqual(Orientation2D.Right, HorizontalOrientation2D.Right.Plus(VerticalOrientation2D.None));
		Assert.AreEqual(Orientation2D.UpRight, HorizontalOrientation2D.Right.Plus(VerticalOrientation2D.Up));
		Assert.AreEqual(Orientation2D.DownRight, HorizontalOrientation2D.Right.Plus(VerticalOrientation2D.Down));
	}

	[Test]
	public void ShouldCorrectlyExtract1DDirections() {
		Assert.AreEqual(HorizontalOrientation2D.Right, Orientation2D.Right.GetHorizontalComponent());
		Assert.AreEqual(VerticalOrientation2D.None, Orientation2D.Right.GetVerticalComponent());

		Assert.AreEqual(HorizontalOrientation2D.Right, Orientation2D.UpRight.GetHorizontalComponent());
		Assert.AreEqual(VerticalOrientation2D.Up, Orientation2D.UpRight.GetVerticalComponent());

		Assert.AreEqual(HorizontalOrientation2D.None, Orientation2D.Up.GetHorizontalComponent());
		Assert.AreEqual(VerticalOrientation2D.Up, Orientation2D.Up.GetVerticalComponent());

		Assert.AreEqual(HorizontalOrientation2D.Left, Orientation2D.UpLeft.GetHorizontalComponent());
		Assert.AreEqual(VerticalOrientation2D.Up, Orientation2D.UpLeft.GetVerticalComponent());

		Assert.AreEqual(HorizontalOrientation2D.Left, Orientation2D.Left.GetHorizontalComponent());
		Assert.AreEqual(VerticalOrientation2D.None, Orientation2D.Left.GetVerticalComponent());

		Assert.AreEqual(HorizontalOrientation2D.Left, Orientation2D.DownLeft.GetHorizontalComponent());
		Assert.AreEqual(VerticalOrientation2D.Down, Orientation2D.DownLeft.GetVerticalComponent());

		Assert.AreEqual(HorizontalOrientation2D.None, Orientation2D.Down.GetHorizontalComponent());
		Assert.AreEqual(VerticalOrientation2D.Down, Orientation2D.Down.GetVerticalComponent());

		Assert.AreEqual(HorizontalOrientation2D.Right, Orientation2D.DownRight.GetHorizontalComponent());
		Assert.AreEqual(VerticalOrientation2D.Down, Orientation2D.DownRight.GetVerticalComponent());
	}
}