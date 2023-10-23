// Created on 2023-10-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Numerics;

namespace Egodystonic.TinyFFR;

[TestFixture]
class RotationTest {
	const float TestTolerance = 0.001f;
	static readonly Rotation NinetyAroundUp = 90f ^ Direction.Up;
	static readonly Rotation NinetyAroundDown = 90f ^ Direction.Down;
	static readonly Rotation NegativeNinetyAroundUp = -90f ^ Direction.Up;
	static readonly Rotation NegativeNinetyAroundDown = -90f ^ Direction.Down;

	[Test]
	public void ShouldCorrectlyScaleRotations() {
		AssertToleranceEquals(NegativeNinetyAroundUp, NinetyAroundUp * -1f, TestTolerance);
		AssertToleranceEquals(NinetyAroundUp, NegativeNinetyAroundUp * -1f, TestTolerance);
		AssertToleranceEquals(NegativeNinetyAroundDown, NinetyAroundDown * -1f, TestTolerance);
		AssertToleranceEquals(NinetyAroundDown, NegativeNinetyAroundDown * -1f, TestTolerance);

		AssertToleranceEquals(Rotation.None, NinetyAroundUp * 0f, TestTolerance);
		AssertToleranceEquals(Rotation.None, NegativeNinetyAroundUp * 0f, TestTolerance);
		AssertToleranceEquals(Rotation.None, NinetyAroundDown * 0f, TestTolerance);
		AssertToleranceEquals(Rotation.None, NegativeNinetyAroundDown * 0f, TestTolerance);

		AssertToleranceEquals(Rotation.None, Rotation.None * 0.5f, TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.None * 1f, TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.None * 0f, TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.None * -0.5f, TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.None * -1f, TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.None * 100f, TestTolerance);

		AssertToleranceEquals(180f ^ Direction.Up, NinetyAroundUp * 2f, TestTolerance);
		AssertToleranceEquals(180f ^ Direction.Up, NinetyAroundDown * -2f, TestTolerance);
		AssertToleranceEquals(180f ^ Direction.Down, NegativeNinetyAroundUp * 2f, TestTolerance);
		AssertToleranceEquals(180f ^ Direction.Down, NegativeNinetyAroundDown * -2f, TestTolerance);

		for (var f = -12f; f <= 12f; f += 4f) {
			AssertToleranceEquals((Direction.Forward.ToVect() + Direction.Right.ToVect()).Direction, Direction.Forward * (NinetyAroundDown * (0.5f + f)), TestTolerance);
			AssertToleranceEquals(Direction.Right, Direction.Forward * (NinetyAroundDown * (1f + f)), TestTolerance);
			AssertToleranceEquals((Direction.Right.ToVect() + Direction.Backward.ToVect()).Direction, Direction.Forward * (NinetyAroundDown * (1.5f + f)), TestTolerance);
			AssertToleranceEquals(Direction.Backward, Direction.Forward * (NinetyAroundDown * (2f + f)), TestTolerance);
			AssertToleranceEquals((Direction.Backward.ToVect() + Direction.Left.ToVect()).Direction, Direction.Forward * (NinetyAroundDown * (2.5f + f)), TestTolerance);
			AssertToleranceEquals(Direction.Left, Direction.Forward * (NinetyAroundDown * (3f + f)), TestTolerance);
			AssertToleranceEquals((Direction.Left.ToVect() + Direction.Forward.ToVect()).Direction, Direction.Forward * (NinetyAroundDown * (3.5f + f)), TestTolerance);
			AssertToleranceEquals(Direction.Forward, Direction.Forward * (NinetyAroundDown * (4f + f)), TestTolerance);
		}

		Assert.AreEqual(Rotation.None, default(Rotation) * 0f);
		Assert.AreEqual(Rotation.None, default(Rotation) * -2f);
		Assert.AreEqual(Rotation.None, default(Rotation) * -1f);
		Assert.AreEqual(Rotation.None, default(Rotation) * -0.5f);
		Assert.AreEqual(Rotation.None, default(Rotation) * 0.5f);
		Assert.AreEqual(Rotation.None, default(Rotation) * 1f);
		Assert.AreEqual(Rotation.None, default(Rotation) * 2f);

		Assert.AreEqual(Rotation.None, new Rotation(new(0f, 0f, 0f, -1f)) * 0f);
		Assert.AreEqual(Rotation.None, new Rotation(new(0f, 0f, 0f, -1f)) * -2f);
		Assert.AreEqual(Rotation.None, new Rotation(new(0f, 0f, 0f, -1f)) * -1f);
		Assert.AreEqual(Rotation.None, new Rotation(new(0f, 0f, 0f, -1f)) * -0.5f);
		Assert.AreEqual(Rotation.None, new Rotation(new(0f, 0f, 0f, -1f)) * 0.5f);
		Assert.AreEqual(Rotation.None, new Rotation(new(0f, 0f, 0f, -1f)) * 1f);
		Assert.AreEqual(Rotation.None, new Rotation(new(0f, 0f, 0f, -1f)) * 2f);
	}
}