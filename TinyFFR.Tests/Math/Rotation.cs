// Created on 2023-10-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

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

		AssertToleranceEquals(Rotation.None, Rotation.None * 1f, TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.None * 0f, TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.None * -1f, TestTolerance);
		AssertToleranceEquals(Rotation.None, Rotation.None * 100f, TestTolerance);

		AssertToleranceEquals(180f ^ Direction.Up, NinetyAroundUp * 2f, TestTolerance);

		// TODO more

		AssertToleranceEquals(Direction.Backward, Direction.Forward * (NinetyAroundUp * 2f), TestTolerance);
	}
}