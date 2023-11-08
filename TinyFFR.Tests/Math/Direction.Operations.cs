// Created on 2023-11-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class DirectionTest {
	[Test]
	public void ShouldCorrectlyFindPerpendicularDirectionWithAdditionalConstrainingDirection() {
		Assert.AreEqual(Direction.Left, Direction.Up.GetAnyPerpendicularDirection(Direction.Forward));

		var testList = new List<Direction>();
		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					testList.Add(new(x, y, z));
				}
			}
		}

		for (var i = 0; i < testList.Count; ++i) {
			var dirA = testList[i];

			Assert.AreEqual(Direction.None, Direction.None.GetAnyPerpendicularDirection(dirA));
			Assert.AreEqual(Direction.None, dirA.GetAnyPerpendicularDirection(Direction.None));

			for (var j = i; j < testList.Count; ++j) {
				var dirB = testList[j];
				var angle = dirA ^ dirB;

				Assert.AreEqual(dirA.GetAnyPerpendicularDirection(dirB), -dirB.GetAnyPerpendicularDirection(dirA));

				if (angle.Equals(0f, TestTolerance) || angle.Equals(180f, TestTolerance)) {
					var thirdOrthogonal = dirA.GetAnyPerpendicularDirection(dirB);
					Assert.AreEqual(Direction.None, thirdOrthogonal);
				}
				else {
					var thirdOrthogonal = dirA.GetAnyPerpendicularDirection(dirB);
					AssertToleranceEquals(90f, dirA ^ thirdOrthogonal, TestTolerance);
					AssertToleranceEquals(90f, dirB ^ thirdOrthogonal, TestTolerance);
					Assert.IsTrue(thirdOrthogonal.IsUnitLength);
					thirdOrthogonal = dirB.GetAnyPerpendicularDirection(dirA);
					AssertToleranceEquals(90f, dirA ^ thirdOrthogonal, TestTolerance);
					AssertToleranceEquals(90f, dirB ^ thirdOrthogonal, TestTolerance);
					Assert.IsTrue(thirdOrthogonal.IsUnitLength);
				}
			}
		}
	}
}