// Created on 2023-11-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using NUnit.Framework.Constraints;
using NUnit.Framework.Internal;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class DirectionTest {
	[Test]
	public void ShouldCorrectlyDetermineUnitLength() {
		Assert.AreEqual(true, OneTwoNegThree.IsUnitLength);
		Assert.AreEqual(false, Direction.None.IsUnitLength);
		Assert.AreEqual(true, Direction.FromPreNormalizedComponents(0.707f, 0f, -0.707f).IsUnitLength);
		Assert.AreEqual(false, Direction.FromPreNormalizedComponents(1, 1f, -1f).IsUnitLength);
	}

	[Test]
	public void ShouldCorrectlyRenormalize() {
		AssertToleranceEquals(OneTwoNegThree, OneTwoNegThree.Renormalized, TestTolerance);
		AssertToleranceEquals(Direction.None, Direction.None.Renormalized, TestTolerance);
		AssertToleranceEquals(new Direction(0.707f, 0f, -0.707f), Direction.FromPreNormalizedComponents(0.707f, 0f, -0.707f).Renormalized, TestTolerance);
		AssertToleranceEquals(new Direction(1f, 1f, -1f), Direction.FromPreNormalizedComponents(1, 1f, -1f).Renormalized, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyReverse() {
		Assert.AreEqual(new Direction(-1f, -2f, 3f), -OneTwoNegThree);
		Assert.AreEqual(-OneTwoNegThree, OneTwoNegThree.Reversed);
		Assert.AreEqual(Direction.None, -Direction.None);

		Assert.AreEqual(Direction.Left, -Direction.Right);
		Assert.AreEqual(Direction.Forward, -Direction.Backward);
		Assert.AreEqual(Direction.Up, -Direction.Down);
	}

	[Test]
	public void ShouldCorrectlyConvertToVect() {
		Assert.AreEqual(new Vect(1f, 2f, -3f).Normalized, OneTwoNegThree.ToVect());
		Assert.AreEqual(Vect.Zero, Direction.None.ToVect());
		Assert.AreEqual(Vect.WValue, OneTwoNegThree.ToVect().AsVector4.W);

		Assert.AreEqual(new Vect(1f, 2f, -3f).WithLength(10f), OneTwoNegThree.ToVect(10f));
		Assert.AreEqual(OneTwoNegThree.ToVect(10f), OneTwoNegThree * 10f);
		Assert.AreEqual(OneTwoNegThree.ToVect(10f), 10f * OneTwoNegThree);
		Assert.AreEqual(Vect.Zero, 10f * Direction.None);
		Assert.AreEqual(Vect.WValue, OneTwoNegThree.ToVect(10f).AsVector4.W);
		Assert.AreEqual(Vect.WValue, Direction.None.ToVect(10f).AsVector4.W);
	}

	[Test]
	public void ShouldCorrectlyProvideAngleBetweenDirections() {
		Assert.AreEqual((Angle) 180f, Direction.Up.AngleTo(Direction.Down));
		Assert.AreEqual((Angle) 90f, Direction.Up.AngleTo(Direction.Right));
		Assert.AreEqual((Angle) 90f, Direction.Right ^ Direction.Up);
		Assert.AreEqual((Angle) 90f, Direction.Right ^ Direction.Down);
		Assert.AreEqual((Angle) 0f, Direction.Down ^ Direction.Down);
	}

	[Test]
	public void ShouldCorrectlyFindAnyPerpendicularDirection() {
		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					var dir = new Direction(x, y, z);
					var perp = dir.GetAnyPerpendicularDirection();

					if (dir == Direction.None) Assert.AreEqual(Direction.None, perp);
					else AssertToleranceEquals(90f, dir ^ perp, TestTolerance);
				}
			}
		}

		foreach (var cardinal in Direction.AllCardinals) {
			var perp = cardinal.GetAnyPerpendicularDirection();
			AssertToleranceEquals(90f, cardinal ^ perp, TestTolerance);
			Assert.IsTrue(Direction.AllCardinals.Contains(perp));
		}
	}

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

				if (dirA == Direction.None || dirB == Direction.None) {
					Assert.AreEqual(Direction.None, dirA.GetAnyPerpendicularDirection(dirB));
					Assert.AreEqual(Direction.None, dirB.GetAnyPerpendicularDirection(dirA));
					continue;
				}

				var thirdOrthogonal = dirA.GetAnyPerpendicularDirection(dirB);
				AssertToleranceEquals(90f, dirA ^ thirdOrthogonal, 2f);
				AssertToleranceEquals(90f, dirB ^ thirdOrthogonal, 2f);
				Assert.IsTrue(thirdOrthogonal.IsUnitLength);
				thirdOrthogonal = dirB.GetAnyPerpendicularDirection(dirA);
				AssertToleranceEquals(90f, dirA ^ thirdOrthogonal, 2f);
				AssertToleranceEquals(90f, dirB ^ thirdOrthogonal, 2f);
				Assert.IsTrue(thirdOrthogonal.IsUnitLength);
			}
		}
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeAgainstAnotherDir() {
		foreach (var cardinal in Direction.AllCardinals) {
			var perp = cardinal.GetAnyPerpendicularDirection();
			var thirdPerp = cardinal.GetAnyPerpendicularDirection(perp);
			Assert.AreEqual(cardinal, (20f % perp * cardinal).OrthogonalizedAgainst(thirdPerp));
			Assert.AreEqual(cardinal, (-20f % perp * cardinal).OrthogonalizedAgainst(thirdPerp));
			Assert.AreEqual(cardinal, (20f % thirdPerp * cardinal).OrthogonalizedAgainst(perp));
			Assert.AreEqual(cardinal, (-20f % thirdPerp * cardinal).OrthogonalizedAgainst(perp));
		}

		AssertToleranceEquals(OneTwoNegThree, OneTwoNegThree.OrthogonalizedAgainst(Direction.None), TestTolerance);
		AssertToleranceEquals(Direction.None, OneTwoNegThree.OrthogonalizedAgainst(OneTwoNegThree), TestTolerance);
		AssertToleranceEquals(Direction.None, Direction.None.OrthogonalizedAgainst(OneTwoNegThree), TestTolerance);
		AssertToleranceEquals(Direction.None, Direction.None.OrthogonalizedAgainst(Direction.None), TestTolerance);

		AssertToleranceEquals(Direction.None, OneTwoNegThree.OrthogonalizedAgainst(-OneTwoNegThree), TestTolerance);
		AssertToleranceEquals(Direction.None, -OneTwoNegThree.OrthogonalizedAgainst(OneTwoNegThree), TestTolerance);

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

			AssertToleranceEquals(dirA, dirA.OrthogonalizedAgainst(Direction.None), TestTolerance);
			AssertToleranceEquals(Direction.None, dirA.OrthogonalizedAgainst(dirA), TestTolerance);
			AssertToleranceEquals(Direction.None, Direction.None.OrthogonalizedAgainst(dirA), TestTolerance);

			for (var j = i; j < testList.Count; ++j) {
				var dirB = testList[j];

				if (dirA == Direction.None || dirB == Direction.None) continue;

				if ((dirA ^ dirB).Equals(180f, 1.5f)) {
					Assert.AreEqual(Direction.None, dirA.OrthogonalizedAgainst(dirB));
				}
				else if ((dirA ^ dirB).Equals(90f, 1.5f)) {
					AssertToleranceEquals(dirA, dirA.OrthogonalizedAgainst(dirB), 0.1f);
				}
				else if ((dirA ^ dirB).Equals(0f, 1.5f)) {
					Assert.AreEqual(Direction.None, dirA.OrthogonalizedAgainst(dirB));
				}
				else {
					AssertToleranceEquals(90f, dirA.OrthogonalizedAgainst(dirB) ^ dirB, 1.5f);
					AssertToleranceEquals(90f, dirA.OrthogonalizedAgainst(dirB) ^ dirB, 1.5f);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyConstructRotations() {
		void AssertPair(Direction startDir, Direction endDir, Rotation expectation) {
			Assert.AreEqual(expectation, startDir.RotationTo(endDir));
			Assert.AreEqual(-expectation, startDir.RotationFrom(endDir));
			Assert.AreEqual(expectation, endDir.RotationFrom(startDir));
			Assert.AreEqual(-expectation, endDir.RotationTo(startDir));

			Assert.AreEqual(startDir.RotationTo(endDir), startDir >> endDir);
			Assert.AreEqual(startDir.RotationTo(endDir), endDir << startDir);
			Assert.AreEqual(startDir.RotationFrom(endDir), startDir << endDir);
			Assert.AreEqual(startDir.RotationFrom(endDir), endDir >> startDir);
		}

		AssertPair(OneTwoNegThree, OneTwoNegThree, Rotation.None);
		AssertPair(Direction.None, OneTwoNegThree, Rotation.None);
		AssertPair(OneTwoNegThree, Direction.None, Rotation.None);
		AssertPair(Direction.None, Direction.None, Rotation.None);

		AssertPair(Direction.Forward, Direction.Left, new Rotation(90f, Direction.Up));

		Assert.AreEqual(new Rotation(90f, Direction.Up), 90f % Direction.Up);
		Assert.AreEqual(new Rotation(90f, Direction.Up), Direction.Up % 90f);
	}

	[Test]
	public void ShouldCorrectlyRotateDirections() {
		var rot = new Rotation(90f, Direction.Up);
		AssertToleranceEquals(Direction.Left, Direction.Forward.RotateBy(rot), TestTolerance);
		Assert.AreEqual(rot * Direction.Forward, Direction.Forward.RotateBy(rot));
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		void AssertCombination(Direction start, Direction end, float distance, Direction expectation) {
			var result = Direction.Interpolate(start, end, distance);
			AssertToleranceEquals(expectation, result, TestTolerance);
			Assert.IsTrue(result.IsUnitLength);
			var precomputation = Direction.CreateInterpolationPrecomputation(start, end);
			result = Direction.InterpolateUsingPrecomputation(start, end, precomputation, distance);
			AssertToleranceEquals(expectation, result, TestTolerance);
			Assert.IsTrue(result.IsUnitLength);
		}

		AssertCombination(Direction.Right, Direction.Right, 0f, Direction.Right);
		AssertCombination(Direction.Right, Direction.Right, 1f, Direction.Right);
		AssertCombination(Direction.Right, Direction.Right, -1f, Direction.Right);
		AssertCombination(Direction.Right, Direction.Right, 0.5f, Direction.Right);
		AssertCombination(Direction.Right, Direction.Right, 2f, Direction.Right);

		AssertCombination(Direction.Up, Direction.Right, 0f, Direction.Up);
		AssertCombination(Direction.Up, Direction.Right, 1f, Direction.Right);
		AssertCombination(Direction.Up, Direction.Right, -1f, Direction.Left);
		AssertCombination(Direction.Up, Direction.Right, 0.5f, Direction.FromVector3(Direction.Up.ToVector3() + Direction.Right.ToVector3()));
		AssertCombination(Direction.Up, Direction.Right, 2f, Direction.Down);

		var testList = new List<Direction>();
		for (var x = -3f; x <= 3f; x += 1f) {
			for (var y = -3f; y <= 3f; y += 1f) {
				for (var z = -3f; z <= 3f; z += 1f) {
					if (x == 0f && y == 0f && z == 0f) continue;
					testList.Add(new(x, y, z));
				}
			}
		}
		for (var i = 0; i < testList.Count; ++i) {
			for (var j = i; j < testList.Count; ++j) {
				var start = testList[i];
				var end = testList[j];
				AssertCombination(start, end, 0f, start);
				AssertCombination(start, end, 1f, end);

				for (var f = -1f; f <= 2f; f += 0.1f) {
					AssertCombination(start, end, f, Rotation.FromStartAndEndDirection(start, end).ScaledBy(f) * start);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyCreateNonBoundedRandomValues() {
		const int NumIterations = 10_000;

		for (var i = 0; i < NumIterations; ++i) {
			var val = Direction.CreateNewRandom();
			Assert.IsTrue(val.IsUnitLength);
		}
	}

	[Test]
	public void ShouldCorrectlyCreateBoundedRandomValues() {
		const int NumIterations = 10_000;

		for (var i = 0; i < NumIterations; ++i) {
			var start = Direction.CreateNewRandom();
			var end = Direction.CreateNewRandom();

			var angle = start ^ end;
			if (angle > 179f) continue;

			var val = Direction.CreateNewRandom(start, end);
			if (val.Equals(start, 0.1f) || val.Equals(end, 0.1f)) continue;

			AssertToleranceEquals((start >> val).Axis, (start >> end).Axis, 0.1f);
			AssertToleranceEquals((start >> end), (start >> val) + (val >> end), 0.1f);
		}
	}

	[Test]
	public void ShouldCorrectlyFindNearestDirectionInSpan() {
		var testList = new List<Direction>();
		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					if (x == 0f && y == 0f && z == 0f) continue;
					testList.Add(new(x, y, z));
				}
			}
		}

		foreach (var item in testList) {
			var nearestOrientationManualCheckVal = Direction.None;
			var nearestOrientationManualCheckAngle = Angle.FullCircle;
			for (var i = 0; i < Direction.AllOrientations.Length; ++i) {
				if ((Direction.AllOrientations[i] ^ item) >= nearestOrientationManualCheckAngle) continue;
				nearestOrientationManualCheckAngle = Direction.AllOrientations[i] ^ item;
				nearestOrientationManualCheckVal = Direction.AllOrientations[i];
			}

			Assert.AreEqual(nearestOrientationManualCheckVal, item.GetNearestDirectionInSpan(Direction.AllOrientations));
		}
	}

	[Test]
	public void ShouldCorrectlyGetNearestOrientations() {
		void AssertCardinal(Direction input, Direction expectedDirection, CardinalOrientation3D? expectedOrientation = null) {
			expectedOrientation ??= OrientationUtils.AllCardinals.ToArray().Single(c => c.ToDirection() == expectedDirection);
			input.GetNearestOrientationCardinal(out var actualOrientation, out var actualDirection);
			Assert.AreEqual(expectedOrientation, actualOrientation);
			Assert.AreEqual(expectedDirection, actualDirection);
		}
		void AssertDiagonal(Direction input, Direction expectedDirection, DiagonalOrientation3D? expectedOrientation = null) {
			expectedOrientation ??= OrientationUtils.AllDiagonals.ToArray().Single(c => c.ToDirection() == expectedDirection);
			input.GetNearestOrientationDiagonal(out var actualOrientation, out var actualDirection);
			Assert.AreEqual(expectedOrientation, actualOrientation);
			Assert.AreEqual(expectedDirection, actualDirection);
		}
		void AssertOrientation(Direction input, Direction expectedDirection, Orientation3D? expectedOrientation = null) {
			expectedOrientation ??= OrientationUtils.All3DOrientations.ToArray().Single(c => c.ToDirection() == expectedDirection);
			input.GetNearestOrientation(out var actualOrientation, out var actualDirection);
			Assert.AreEqual(expectedOrientation, actualOrientation);
			Assert.AreEqual(expectedDirection, actualDirection);
		}

		AssertCardinal(Direction.None, Direction.None, CardinalOrientation3D.None);
		AssertDiagonal(Direction.None, Direction.None, DiagonalOrientation3D.None);
		AssertOrientation(Direction.None, Direction.None, Orientation3D.None);
		foreach (var d in Direction.AllCardinals) {
			AssertCardinal(d, d);
		}
		foreach (var d in Direction.AllDiagonals) {
			AssertDiagonal(d, d);
		}
		foreach (var d in Direction.AllOrientations) {
			AssertOrientation(d, d);
		}

		var testList = new List<Direction>();
		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					testList.Add(new(x, y, z));
				}
			}
		}
		foreach (var item in testList) {
			//Console.WriteLine(item.ToStringDescriptive());
			if (item == Direction.None) {
				AssertCardinal(item, Direction.None, CardinalOrientation3D.None);
				AssertDiagonal(item, Direction.None, DiagonalOrientation3D.None);
				AssertOrientation(item, Direction.None, Orientation3D.None);
				continue;
			}

			var expectedCardinalResult = item.GetNearestDirectionInSpan(Direction.AllCardinals);
			var expectedDiagonalResult = item.GetNearestDirectionInSpan(Direction.AllDiagonals);
			var expectedOrientationResult = item.GetNearestDirectionInSpan(Direction.AllOrientations);
			AssertCardinal(item, expectedCardinalResult);
			AssertDiagonal(item, expectedDiagonalResult);
			AssertOrientation(item, expectedOrientationResult);
		}
	}
}