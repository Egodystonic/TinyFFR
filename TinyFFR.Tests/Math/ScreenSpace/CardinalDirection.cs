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
		Assert.AreEqual((int) Orientation2DHorizontal.Left, (int) Orientation2D.Left);
		Assert.AreEqual((int) Orientation2DHorizontal.Right, (int) Orientation2D.Right);
		Assert.AreEqual((int) Orientation2DVertical.Up, (int) Orientation2D.Up);
		Assert.AreEqual((int) Orientation2DVertical.Down, (int) Orientation2D.Down);

		Assert.AreEqual((int) Orientation2DHorizontal.Left | (int) Orientation2DVertical.Up, (int) Orientation2D.UpLeft);
		Assert.AreEqual((int) Orientation2DHorizontal.Right | (int) Orientation2DVertical.Up, (int) Orientation2D.UpRight);
		Assert.AreEqual((int) Orientation2DHorizontal.Left | (int) Orientation2DVertical.Down, (int) Orientation2D.DownLeft);
		Assert.AreEqual((int) Orientation2DHorizontal.Right | (int) Orientation2DVertical.Down, (int) Orientation2D.DownRight);
	}

	[Test]
	public void ShouldCorrectlyCombineVerticalAndHorizontalDirections() {
		Assert.AreEqual(Orientation2D.None, Orientation2DVertical.None.Plus(Orientation2DHorizontal.None));
		Assert.AreEqual(Orientation2D.None, Orientation2DHorizontal.None.Plus(Orientation2DVertical.None));

		Assert.AreEqual(Orientation2D.Up, Orientation2DVertical.Up.Plus(Orientation2DHorizontal.None));
		Assert.AreEqual(Orientation2D.UpRight, Orientation2DVertical.Up.Plus(Orientation2DHorizontal.Right));
		Assert.AreEqual(Orientation2D.UpLeft, Orientation2DVertical.Up.Plus(Orientation2DHorizontal.Left));
		Assert.AreEqual(Orientation2D.Down, Orientation2DVertical.Down.Plus(Orientation2DHorizontal.None));
		Assert.AreEqual(Orientation2D.DownRight, Orientation2DVertical.Down.Plus(Orientation2DHorizontal.Right));
		Assert.AreEqual(Orientation2D.DownLeft, Orientation2DVertical.Down.Plus(Orientation2DHorizontal.Left));

		Assert.AreEqual(Orientation2D.Left, Orientation2DHorizontal.Left.Plus(Orientation2DVertical.None));
		Assert.AreEqual(Orientation2D.UpLeft, Orientation2DHorizontal.Left.Plus(Orientation2DVertical.Up));
		Assert.AreEqual(Orientation2D.DownLeft, Orientation2DHorizontal.Left.Plus(Orientation2DVertical.Down));
		Assert.AreEqual(Orientation2D.Right, Orientation2DHorizontal.Right.Plus(Orientation2DVertical.None));
		Assert.AreEqual(Orientation2D.UpRight, Orientation2DHorizontal.Right.Plus(Orientation2DVertical.Up));
		Assert.AreEqual(Orientation2D.DownRight, Orientation2DHorizontal.Right.Plus(Orientation2DVertical.Down));
	}

	[Test]
	public void ShouldCorrectlyExtract1DDirections() {
		Assert.AreEqual(Orientation2DHorizontal.Right, Orientation2D.Right.GetHorizontalComponent());
		Assert.AreEqual(Orientation2DVertical.None, Orientation2D.Right.GetVerticalComponent());

		Assert.AreEqual(Orientation2DHorizontal.Right, Orientation2D.UpRight.GetHorizontalComponent());
		Assert.AreEqual(Orientation2DVertical.Up, Orientation2D.UpRight.GetVerticalComponent());

		Assert.AreEqual(Orientation2DHorizontal.None, Orientation2D.Up.GetHorizontalComponent());
		Assert.AreEqual(Orientation2DVertical.Up, Orientation2D.Up.GetVerticalComponent());

		Assert.AreEqual(Orientation2DHorizontal.Left, Orientation2D.UpLeft.GetHorizontalComponent());
		Assert.AreEqual(Orientation2DVertical.Up, Orientation2D.UpLeft.GetVerticalComponent());

		Assert.AreEqual(Orientation2DHorizontal.Left, Orientation2D.Left.GetHorizontalComponent());
		Assert.AreEqual(Orientation2DVertical.None, Orientation2D.Left.GetVerticalComponent());

		Assert.AreEqual(Orientation2DHorizontal.Left, Orientation2D.DownLeft.GetHorizontalComponent());
		Assert.AreEqual(Orientation2DVertical.Down, Orientation2D.DownLeft.GetVerticalComponent());

		Assert.AreEqual(Orientation2DHorizontal.None, Orientation2D.Down.GetHorizontalComponent());
		Assert.AreEqual(Orientation2DVertical.Down, Orientation2D.Down.GetVerticalComponent());

		Assert.AreEqual(Orientation2DHorizontal.Right, Orientation2D.DownRight.GetHorizontalComponent());
		Assert.AreEqual(Orientation2DVertical.Down, Orientation2D.DownRight.GetVerticalComponent());
	}
}