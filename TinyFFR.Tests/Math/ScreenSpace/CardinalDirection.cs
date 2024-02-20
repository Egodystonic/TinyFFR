// Created on 2024-02-20 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

[TestFixture]
class CardinalDirectionTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyCombineBits() {
		Assert.AreEqual((int) HorizontalDirection.Left, (int) CardinalDirection.Left);
		Assert.AreEqual((int) HorizontalDirection.Right, (int) CardinalDirection.Right);
		Assert.AreEqual((int) VerticalDirection.Up, (int) CardinalDirection.Up);
		Assert.AreEqual((int) VerticalDirection.Down, (int) CardinalDirection.Down);

		Assert.AreEqual((int) HorizontalDirection.Left | (int) VerticalDirection.Up, (int) CardinalDirection.UpLeft);
		Assert.AreEqual((int) HorizontalDirection.Right | (int) VerticalDirection.Up, (int) CardinalDirection.UpRight);
		Assert.AreEqual((int) HorizontalDirection.Left | (int) VerticalDirection.Down, (int) CardinalDirection.DownLeft);
		Assert.AreEqual((int) HorizontalDirection.Right | (int) VerticalDirection.Down, (int) CardinalDirection.DownRight);
	}

	[Test]
	public void ShouldCorrectlyCombineVerticalAndHorizontalDirections() {
		Assert.AreEqual(CardinalDirection.None, VerticalDirection.None.Plus(HorizontalDirection.None));
		Assert.AreEqual(CardinalDirection.None, HorizontalDirection.None.Plus(VerticalDirection.None));

		Assert.AreEqual(CardinalDirection.Up, VerticalDirection.Up.Plus(HorizontalDirection.None));
		Assert.AreEqual(CardinalDirection.UpRight, VerticalDirection.Up.Plus(HorizontalDirection.Right));
		Assert.AreEqual(CardinalDirection.UpLeft, VerticalDirection.Up.Plus(HorizontalDirection.Left));
		Assert.AreEqual(CardinalDirection.Down, VerticalDirection.Down.Plus(HorizontalDirection.None));
		Assert.AreEqual(CardinalDirection.DownRight, VerticalDirection.Down.Plus(HorizontalDirection.Right));
		Assert.AreEqual(CardinalDirection.DownLeft, VerticalDirection.Down.Plus(HorizontalDirection.Left));

		Assert.AreEqual(CardinalDirection.Left, HorizontalDirection.Left.Plus(VerticalDirection.None));
		Assert.AreEqual(CardinalDirection.UpLeft, HorizontalDirection.Left.Plus(VerticalDirection.Up));
		Assert.AreEqual(CardinalDirection.DownLeft, HorizontalDirection.Left.Plus(VerticalDirection.Down));
		Assert.AreEqual(CardinalDirection.Right, HorizontalDirection.Right.Plus(VerticalDirection.None));
		Assert.AreEqual(CardinalDirection.UpRight, HorizontalDirection.Right.Plus(VerticalDirection.Up));
		Assert.AreEqual(CardinalDirection.DownRight, HorizontalDirection.Right.Plus(VerticalDirection.Down));
	}

	[Test]
	public void ShouldCorrectlyExtract1DDirections() {
		Assert.AreEqual(HorizontalDirection.Right, CardinalDirection.Right.GetHorizontalComponent());
		Assert.AreEqual(VerticalDirection.None, CardinalDirection.Right.GetVerticalComponent());

		Assert.AreEqual(HorizontalDirection.Right, CardinalDirection.UpRight.GetHorizontalComponent());
		Assert.AreEqual(VerticalDirection.Up, CardinalDirection.UpRight.GetVerticalComponent());

		Assert.AreEqual(HorizontalDirection.None, CardinalDirection.Up.GetHorizontalComponent());
		Assert.AreEqual(VerticalDirection.Up, CardinalDirection.Up.GetVerticalComponent());

		Assert.AreEqual(HorizontalDirection.Left, CardinalDirection.UpLeft.GetHorizontalComponent());
		Assert.AreEqual(VerticalDirection.Up, CardinalDirection.UpLeft.GetVerticalComponent());

		Assert.AreEqual(HorizontalDirection.Left, CardinalDirection.Left.GetHorizontalComponent());
		Assert.AreEqual(VerticalDirection.None, CardinalDirection.Left.GetVerticalComponent());

		Assert.AreEqual(HorizontalDirection.Left, CardinalDirection.DownLeft.GetHorizontalComponent());
		Assert.AreEqual(VerticalDirection.Down, CardinalDirection.DownLeft.GetVerticalComponent());

		Assert.AreEqual(HorizontalDirection.None, CardinalDirection.Down.GetHorizontalComponent());
		Assert.AreEqual(VerticalDirection.Down, CardinalDirection.Down.GetVerticalComponent());

		Assert.AreEqual(HorizontalDirection.Right, CardinalDirection.DownRight.GetHorizontalComponent());
		Assert.AreEqual(VerticalDirection.Down, CardinalDirection.DownRight.GetVerticalComponent());
	}
}