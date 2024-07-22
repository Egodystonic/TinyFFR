// Created on 2023-11-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Numerics;
using NUnit.Framework.Constraints;
using NUnit.Framework.Internal;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class DirectionTest {
	[Test]
	public void ShouldCorrectlyDetermineUnitLength() {
		Assert.AreEqual(true, OneTwoNegThree.IsUnitLength);
		Assert.AreEqual(false, Direction.None.IsUnitLength);
		Assert.AreEqual(true, Direction.FromVector3PreNormalized(0.707f, 0f, -0.707f).IsUnitLength);
		Assert.AreEqual(false, Direction.FromVector3PreNormalized(1, 1f, -1f).IsUnitLength);
	}

	[Test]
	public void ShouldUseAppropriateErrorMarginForUnitLengthTest() {
		Assert.AreEqual(true, Direction.FromVector3PreNormalized(0.707f, 0f, -0.707f).IsUnitLength);
		Assert.AreEqual(true, Direction.FromVector3PreNormalized(0.706f, 0f, -0.707f).IsUnitLength);
		Assert.AreEqual(true, Direction.FromVector3PreNormalized(0.707f, 0f, -0.706f).IsUnitLength);
		Assert.AreEqual(false, Direction.FromVector3PreNormalized(0.706f, 0f, -0.706f).IsUnitLength);
	}

	[Test]
	public void ShouldCorrectlyRenormalize() {
		AssertToleranceEquals(OneTwoNegThree, Direction.Renormalize(OneTwoNegThree), TestTolerance);
		AssertToleranceEquals(Direction.None, Direction.Renormalize(Direction.None), TestTolerance);
		AssertToleranceEquals(new Direction(0.707f, 0f, -0.707f), Direction.Renormalize(Direction.FromVector3PreNormalized(0.707f, 0f, -0.707f)), TestTolerance);
		AssertToleranceEquals(new Direction(1f, 1f, -1f), Direction.Renormalize(Direction.FromVector3PreNormalized(1, 1f, -1f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyReverse() {
		Assert.AreEqual(new Direction(-1f, -2f, 3f), -OneTwoNegThree);
		Assert.AreEqual(-OneTwoNegThree, OneTwoNegThree.Inverted);
		Assert.AreEqual(Direction.None, -Direction.None);

		Assert.AreEqual(Direction.Left, -Direction.Right);
		Assert.AreEqual(Direction.Forward, -Direction.Backward);
		Assert.AreEqual(Direction.Up, -Direction.Down);
	}

	[Test]
	public void ShouldCorrectlyConvertToVect() {
		Assert.AreEqual(new Vect(1f, 2f, -3f).AsUnitLength, OneTwoNegThree.AsVect());
		Assert.AreEqual(Vect.Zero, Direction.None.AsVect());
		Assert.AreEqual(Vect.WValue, OneTwoNegThree.AsVect().AsVector4.W);

		Assert.AreEqual(new Vect(1f, 2f, -3f).WithLength(10f), OneTwoNegThree.AsVect(10f));
		Assert.AreEqual(OneTwoNegThree.AsVect(10f), OneTwoNegThree * 10f);
		Assert.AreEqual(OneTwoNegThree.AsVect(10f), 10f * OneTwoNegThree);
		Assert.AreEqual(Vect.Zero, 10f * Direction.None);
		Assert.AreEqual(Vect.WValue, OneTwoNegThree.AsVect(10f).AsVector4.W);
		Assert.AreEqual(Vect.WValue, Direction.None.AsVect(10f).AsVector4.W);
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
					var perp = dir.AnyPerpendicular();

					if (dir == Direction.None) Assert.AreEqual(Direction.None, perp);
					else AssertToleranceEquals(90f, dir ^ perp, TestTolerance);
				}
			}
		}

		foreach (var cardinal in Direction.AllCardinals) {
			var perp = cardinal.AnyPerpendicular();
			AssertToleranceEquals(90f, cardinal ^ perp, TestTolerance);
			Assert.IsTrue(Direction.AllCardinals.Contains(perp));
		}
	}

	[Test]
	public void ShouldCorrectlyFindPerpendicularDirectionWithAdditionalConstrainingDirection() {
		Assert.AreEqual(Direction.Left, Direction.FromPerpendicular(Direction.Up, Direction.Forward));

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

			for (var j = i; j < testList.Count; ++j) {
				var dirB = testList[j];

				if (dirA == Direction.None || dirB == Direction.None) {
					continue;
				}

				var thirdOrthogonal = Direction.FromPerpendicular(dirA, dirB);
				AssertToleranceEquals(90f, dirA ^ thirdOrthogonal, 2f);
				AssertToleranceEquals(90f, dirB ^ thirdOrthogonal, 2f);
				Assert.IsTrue(thirdOrthogonal.IsUnitLength);
				thirdOrthogonal = Direction.FromPerpendicular(dirB, dirA);
				AssertToleranceEquals(90f, dirA ^ thirdOrthogonal, 2f);
				AssertToleranceEquals(90f, dirB ^ thirdOrthogonal, 2f);
				Assert.IsTrue(thirdOrthogonal.IsUnitLength);
			}
		}
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeAgainstAnotherDir() {
		foreach (var cardinal in Direction.AllCardinals) {
			var perp = cardinal.AnyPerpendicular();
			var thirdPerp = Direction.FromPerpendicular(cardinal, perp);
			Assert.AreEqual(cardinal, (20f % perp * cardinal).OrthogonalizedAgainst(thirdPerp));
			Assert.AreEqual(cardinal, (-20f % perp * cardinal).OrthogonalizedAgainst(thirdPerp));
			Assert.AreEqual(cardinal, (20f % thirdPerp * cardinal).OrthogonalizedAgainst(perp));
			Assert.AreEqual(cardinal, (-20f % thirdPerp * cardinal).OrthogonalizedAgainst(perp));
		}

		AssertToleranceEquals(null, OneTwoNegThree.OrthogonalizedAgainst(Direction.None), TestTolerance);
		AssertToleranceEquals(null, OneTwoNegThree.OrthogonalizedAgainst(OneTwoNegThree), TestTolerance);
		AssertToleranceEquals(null, Direction.None.OrthogonalizedAgainst(OneTwoNegThree), TestTolerance);
		AssertToleranceEquals(null, Direction.None.OrthogonalizedAgainst(Direction.None), TestTolerance);

		AssertToleranceEquals(null, OneTwoNegThree.OrthogonalizedAgainst(-OneTwoNegThree), TestTolerance);
		AssertToleranceEquals(null, -OneTwoNegThree.OrthogonalizedAgainst(OneTwoNegThree), TestTolerance);

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

			AssertToleranceEquals(null, dirA.OrthogonalizedAgainst(Direction.None), TestTolerance);
			AssertToleranceEquals(null, dirA.OrthogonalizedAgainst(dirA), TestTolerance);
			AssertToleranceEquals(null, Direction.None.OrthogonalizedAgainst(dirA), TestTolerance);

			for (var j = i; j < testList.Count; ++j) {
				var dirB = testList[j];

				if (dirA == Direction.None || dirB == Direction.None) continue;

				if ((dirA ^ dirB).Equals(180f, 1.5f)) {
					Assert.AreEqual(null, dirA.OrthogonalizedAgainst(dirB));
				}
				else if ((dirA ^ dirB).Equals(90f, 1.5f)) {
					AssertToleranceEquals(dirA, dirA.OrthogonalizedAgainst(dirB), 0.1f);
					AssertToleranceEquals(dirA, dirA.FastOrthogonalizedAgainst(dirB), 0.1f);
				}
				else if ((dirA ^ dirB).Equals(0f, 1.5f)) {
					Assert.AreEqual(null, dirA.OrthogonalizedAgainst(dirB));
				}
				else {
					AssertToleranceEquals(90f, dirA.OrthogonalizedAgainst(dirB) ^ dirB, 1.5f);
					AssertToleranceEquals(90f, dirB.OrthogonalizedAgainst(dirA) ^ dirA, 1.5f);
					AssertToleranceEquals(90f, dirA.FastOrthogonalizedAgainst(dirB) ^ dirB, 1.5f);
					AssertToleranceEquals(90f, dirB.FastOrthogonalizedAgainst(dirA) ^ dirA, 1.5f);
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
		AssertToleranceEquals(Direction.Left, Direction.Forward.RotatedBy(rot), TestTolerance);
		Assert.AreEqual(rot * Direction.Forward, Direction.Forward.RotatedBy(rot));
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
	public void ShouldCorrectlyCreateConicalRandomValues() {
		const int NumIterations = 10_000;

		for (var i = 0; i < NumIterations; ++i) {
			var centre = Direction.CreateNewRandom();
			var angle = Angle.CreateNewRandom(0f, 180f);

			var result = Direction.CreateNewRandom(centre, angle);
			Assert.LessOrEqual((result ^ centre).AsRadians, angle.AsRadians + TestTolerance);
		}

		for (var i = 0; i < NumIterations; ++i) {
			var centre = Direction.CreateNewRandom();

			var result = Direction.CreateNewRandom(centre, 180f, 90f);
			Assert.LessOrEqual((result ^ centre).AsRadians, Angle.HalfCircle.AsRadians + TestTolerance);
			Assert.GreaterOrEqual((result ^ centre).AsRadians, Angle.QuarterCircle.AsRadians - TestTolerance);
		}

		for (var i = 0; i < NumIterations; ++i) {
			AssertToleranceEquals(90f, Direction.CreateNewRandom(Direction.Up, 90f, 90f) ^ Direction.Up, TestTolerance);
		}
	}

	[Test]
	public void ShouldCorrectlyCreateArcPlanarRandomValues() {
		const int NumIterations = 10_000;

		for (var a = 0f; a < 360f; a += 45f) {
			for (var i = 0; i < NumIterations; ++i) {
				var centre = Direction.CreateNewRandom();
				var plane = Plane.CreateNewRandom();
				while (centre.ProjectedOnTo(plane) == null) centre = Direction.CreateNewRandom();
				
				var result = Direction.CreateNewRandom(plane, centre, a);
				try {
					Assert.LessOrEqual(plane.AngleTo(result).AsDegrees, 1f);
					Assert.LessOrEqual((result ^ centre.ProjectedOnTo(plane)!.Value).AsDegrees, a + 1f);
				}
				catch {
					Console.WriteLine(new Angle(a).ToString());
					Console.WriteLine(plane.ToStringDescriptive());
					Console.WriteLine(centre.ToStringDescriptive());
					Console.WriteLine(result.ToStringDescriptive());
					throw;
				}
			}
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

			Assert.AreEqual(nearestOrientationManualCheckVal, Direction.FromNearestDirectionInSpan(item, Direction.AllOrientations));
		}
	}

	[Test]
	public void ShouldCorrectlyGetNearestOrientations() {
		void AssertCardinal(Direction input, Direction expectedDirection, CardinalOrientation3D? expectedOrientation = null) {
			expectedOrientation ??= OrientationUtils.AllCardinals.ToArray().Single(c => c.ToDirection() == expectedDirection);
			var (actualOrientation, actualDirection) = input.NearestOrientationCardinal;
			Assert.AreEqual(expectedOrientation, actualOrientation);
			Assert.AreEqual(expectedDirection, actualDirection);
		}
		void AssertDiagonal(Direction input, Direction expectedDirection, DiagonalOrientation3D? expectedOrientation = null) {
			expectedOrientation ??= OrientationUtils.AllDiagonals.ToArray().Single(c => c.ToDirection() == expectedDirection);
			var (actualOrientation, actualDirection) = input.NearestOrientationDiagonal;
			Assert.AreEqual(expectedOrientation, actualOrientation);
			Assert.AreEqual(expectedDirection, actualDirection);
		}
		void AssertOrientation(Direction input, Direction expectedDirection, Orientation3D? expectedOrientation = null) {
			expectedOrientation ??= OrientationUtils.All3DOrientations.ToArray().Single(c => c.ToDirection() == expectedDirection);
			var (actualOrientation, actualDirection) = input.NearestOrientation;
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

			var expectedCardinalResult = Direction.FromNearestDirectionInSpan(item, Direction.AllCardinals);
			var expectedDiagonalResult = Direction.FromNearestDirectionInSpan(item, Direction.AllDiagonals);
			var expectedOrientationResult = Direction.FromNearestDirectionInSpan(item, Direction.AllOrientations);
			AssertCardinal(item, expectedCardinalResult);
			AssertDiagonal(item, expectedDiagonalResult);
			AssertOrientation(item, expectedOrientationResult);
		}
	}

	[Test]
	public void ShouldCorrectlyExposeDotProductAsSimilarity() {
		var testList = new List<Direction>();
		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					testList.Add(new(x, y, z));
				}
			}
		}

		for (var i = 0; i < testList.Count; ++i) {
			for (var j = i; j < testList.Count; ++j) {
				var a = testList[i];
				var b = testList[j];
				Assert.AreEqual(Vector3.Dot(a.ToVector3(), b.ToVector3()), a.Dot(b), TestTolerance);
				Assert.AreEqual(a.Dot(b), b.Dot(a));
			}
		}

		Assert.AreEqual(1f, Direction.Forward.Dot(Direction.Forward));
		Assert.AreEqual(0f, Direction.Forward.Dot(Direction.Up));
		Assert.AreEqual(0f, Direction.Forward.Dot(Direction.Down));
		Assert.AreEqual(0f, Direction.Forward.Dot(Direction.Left));
		Assert.AreEqual(0f, Direction.Forward.Dot(Direction.Right));
		Assert.AreEqual(-1f, Direction.Forward.Dot(Direction.Backward));

		Assert.AreEqual(0f, Direction.Forward.Dot(Direction.None));
		Assert.AreEqual(0f, Direction.None.Dot(Direction.Forward));
		Assert.AreEqual(0f, Direction.None.Dot(Direction.None));
	}

	[Test]
	public void ShouldCorrectlyClampBetweenTwoDirections() {
		void AssertCombination(Direction expectation, Direction min, Direction max, Direction input) {
			AssertToleranceEquals(expectation, input.Clamp(min, max), TestTolerance);
			AssertToleranceEquals(expectation, input.Clamp(max, min), TestTolerance);
			AssertToleranceEquals(expectation.Inverted, input.Inverted.Clamp(min.Inverted, max.Inverted), TestTolerance);
			AssertToleranceEquals(expectation.Inverted, input.Inverted.Clamp(max.Inverted, min.Inverted), TestTolerance);
		}

		// Within arc after projection
		AssertCombination((1f, 0f, 1f), Direction.Left, Direction.Forward, (1f, 0.3f, 1f));
		AssertCombination((1f, 0f, 1f), Direction.Left, Direction.Forward, (1f, -0.3f, 1f));
		AssertCombination((1f, 0f, 0f), Direction.Left, Direction.Forward, (1f, 0f, 0f));
		AssertCombination((0f, 0f, 1f), Direction.Left, Direction.Forward, (0f, 0f, 1f));

		AssertCombination((-1f, 0f, 1f), Direction.Right, Direction.Forward, (-1f, 0.3f, 1f));
		AssertCombination((-1f, 0f, 1f), Direction.Right, Direction.Forward, (-1f, -0.3f, 1f));
		AssertCombination((-1f, 0f, 0f), Direction.Right, Direction.Forward, (-1f, 0f, 0f));
		AssertCombination((0f, 0f, 1f), Direction.Right, Direction.Forward, (0f, 0f, 1f));

		AssertCombination((1f, 0f, 1f), (1f, 0f, 1f), Direction.Forward, (1f, 0.3f, 1f));
		AssertCombination((1f, 0f, 1f), (1f, 0f, 1f), Direction.Forward, (1f, -0.3f, 1f));
		AssertCombination((0f, 0f, 1f), (1f, 0f, 1f), Direction.Forward, (0f, 0f, 1f));

		AssertCombination((1f, 0f, 1f), (1f, 0f, 1f), Direction.Right, (1f, 0.3f, 1f));
		AssertCombination((0f, 0f, 1f), (1f, 0f, 1f), Direction.Right, (0f, -0.3f, 1f));
		AssertCombination((-1f, 0f, 0f), (1f, 0f, 1f), Direction.Right, (-1f, -1f, 0f));

		// Outside arc after projection
		AssertCombination((1f, 0f, 0f), Direction.Left, Direction.Forward, (1f, 0f, -0.2f));
		AssertCombination((0f, 0f, 1f), Direction.Left, Direction.Forward, (-0.2f, 0f, 1f));
		AssertCombination((-1f, 0f, 0f), Direction.Right, Direction.Forward, (-1f, 0f, -0.2f));
		AssertCombination((0f, 0f, 1f), Direction.Right, Direction.Forward, (0.2f, 0f, 1f));

		AssertCombination((1f, 0f, 0f), Direction.Left, (1f, 0f, 1f), (1f, 0f, -0.2f));
		AssertCombination((1f, 0f, 1f), Direction.Left, (1f, 0f, 1f), (-0.2f, 0f, 1f));
		AssertCombination((-1f, 0f, 0f), Direction.Right, (1f, 0f, 1f), (-1f, 0f, -0.2f));
		AssertCombination((1f, 0f, 1f), Direction.Right, (1f, 0f, 1f), (1.2f, 0f, 1f));

		// Min and max are antipodal
		AssertCombination(Direction.Down, Direction.Up, Direction.Down, Direction.Down);
		AssertCombination(Direction.Up, Direction.Up, Direction.Down, Direction.Up);
		AssertCombination(Direction.Left, Direction.Up, Direction.Down, Direction.Left);
		AssertCombination(Direction.Right, Direction.Up, Direction.Down, Direction.Right);

		// Min and max are the same
		AssertCombination(Direction.Down, Direction.Down, Direction.Down, Direction.Down);
		AssertCombination(Direction.Up, Direction.Up, Direction.Up, Direction.Down);

		// This is perpendicular to arc
		// ReSharper disable once JoinDeclarationAndInitializer
		Direction perpClampResult;
		perpClampResult = Direction.Down.Clamp(Direction.Left, Direction.Forward);
		Assert.IsTrue(perpClampResult == Direction.Left || perpClampResult == Direction.Forward);
		perpClampResult = Direction.Down.Clamp(Direction.Forward, Direction.Left);
		Assert.IsTrue(perpClampResult == Direction.Left || perpClampResult == Direction.Forward);
		perpClampResult = Direction.Up.Clamp(Direction.Left, Direction.Forward);
		Assert.IsTrue(perpClampResult == Direction.Left || perpClampResult == Direction.Forward);
		perpClampResult = Direction.Up.Clamp(Direction.Forward, Direction.Left);
		Assert.IsTrue(perpClampResult == Direction.Left || perpClampResult == Direction.Forward);
		
		// This is None
		Assert.AreEqual(Direction.None, Direction.None.Clamp(Direction.Left, Direction.Forward));
		Assert.AreEqual(Direction.None, Direction.None.Clamp(Direction.Left, Direction.Right));

		// Min or max are None
		Assert.Throws<ArgumentException>(() => Direction.Right.Clamp(Direction.Forward, Direction.None));
		Assert.Throws<ArgumentException>(() => Direction.Right.Clamp(Direction.None, Direction.Forward));
	}

	[Test]
	public void DirectionalClampShouldUseAppropriateFloatingPointErrorMargin() {
		Assert.AreNotEqual(new Direction(1f, 0f, 0f), Direction.Up.Clamp((1f, 0f, 0f), (0.999f, 0.001f, 0f)));
		Assert.AreEqual(new Direction(1f, 0f, 0f), Direction.Up.Clamp((1f, 0f, 0f), (0.9999f, 0.0001f, 0f)));

		const float MinDifferentiableAngleDegrees = 0.1f;
		var testList = new List<Direction>();
		for (var x = -4f; x <= 4f; x += 1f) {
			for (var y = -4f; y <= 4f; y += 1f) {
				for (var z = -4f; z <= 4f; z += 1f) {
					if (x == 0f && y == 0f && z == 0f) continue;
					testList.Add(new(x, y, z));
				}
			}
		}

		foreach (var dir in testList) {
			for (var i = 0; i < 3; ++i) {
				var offset = Direction.CreateNewRandom(dir, MinDifferentiableAngleDegrees, MinDifferentiableAngleDegrees);
				try {
					Assert.AreNotEqual(dir, ((dir >> offset) * 0.5f * dir).Clamp(dir, offset));
				}
				catch (Exception e) {
					if (e is AssertionException) Console.WriteLine("Margin is too coarse (dir and offset should be dissimilar enough)");
					else Console.WriteLine("Plane construction over triangle presumably threw exception for colinearity? See exception");
					Console.WriteLine("\t" + dir.ToStringDescriptive() + " to " + offset.ToStringDescriptive() + "; angle: " + (dir ^ offset));
					throw;
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyClampWithinCone() {
		AssertToleranceEquals(Direction.Forward, Direction.Forward.Clamp(Direction.Forward, 0f), TestTolerance);
		AssertToleranceEquals(Direction.Forward, Direction.Up.Clamp(Direction.Forward, 0f), TestTolerance);
		AssertToleranceEquals(Direction.Forward, Direction.Right.Clamp(Direction.Forward, 0f), TestTolerance);
		AssertToleranceEquals(Direction.Forward, Direction.Backward.Clamp(Direction.Forward, 0f), TestTolerance);
		AssertToleranceEquals(Direction.Forward, new Direction(0.1f, 0.1f, 1f).Clamp(Direction.Forward, 0f), TestTolerance);
		AssertToleranceEquals(new Direction(0.1f, 0.1f, 1f), new Direction(0.1f, 0.1f, 1f).Clamp(Direction.Forward, 10f), TestTolerance);
		AssertToleranceEquals(new Direction(-0.1f, -0.1f, 1f), new Direction(-0.1f, -0.1f, 1f).Clamp(Direction.Forward, 10f), TestTolerance);

		Assert.AreEqual(Direction.None, Direction.None.Clamp(Direction.Down, 0f));
		Assert.Throws<ArgumentException>(() => Direction.Forward.Clamp(Direction.None, 100f));

		var testList = new List<Direction>();
		for (var x = -4f; x <= 4f; x += 1f) {
			for (var y = -4f; y <= 4f; y += 1f) {
				for (var z = -4f; z <= 4f; z += 1f) {
					testList.Add(new(x, y, z));
				}
			}
		}

		for (var i = 0; i < testList.Count; ++i) {
			var input = testList[i];
			if (input == Direction.None) continue;
			for (var j = i; j < testList.Count; ++j) {
				var target = testList[j];
				if (target == Direction.None) continue;
				for (var angle = 0f; angle <= 180f; angle += 15f) {
					var clampedInput = input.Clamp(target, angle);
					Assert.LessOrEqual((clampedInput ^ target).AsDegrees, angle + TestTolerance);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyClampWithinArc() {
		void AssertCombination(Direction? expectation3D, Plane plane, Direction arcCentre, Angle arcMax, Direction input) {
			if (expectation3D == null) {
				AssertToleranceEquals(null, input.Clamp(plane, arcCentre, arcMax, true), TestTolerance);
				AssertToleranceEquals(null, input.Inverted.Clamp(plane, arcCentre.Inverted, arcMax, true), TestTolerance);
				plane = plane.Flipped;
				AssertToleranceEquals(null, input.Clamp(plane, arcCentre, arcMax, true), TestTolerance);
				AssertToleranceEquals(null, input.Inverted.Clamp(plane, arcCentre.Inverted, arcMax, true), TestTolerance);
				return;
			}

			AssertToleranceEquals(expectation3D.Value, input.Clamp(plane, arcCentre, arcMax, true), TestTolerance);
			AssertToleranceEquals(expectation3D.Value.Inverted, input.Inverted.Clamp(plane, arcCentre.Inverted, arcMax, true), TestTolerance);
			if (expectation3D != Direction.None) {
				AssertToleranceEquals(expectation3D.Value.ProjectedOnTo(plane), input.Clamp(plane, arcCentre, arcMax, false), TestTolerance);
				AssertToleranceEquals(expectation3D.Value.Inverted.ProjectedOnTo(plane), input.Inverted.Clamp(plane, arcCentre.Inverted, arcMax, false), TestTolerance);
			}

			plane = plane.Flipped;
			AssertToleranceEquals(expectation3D.Value, input.Clamp(plane, arcCentre, arcMax, true), TestTolerance);
			AssertToleranceEquals(expectation3D.Value.Inverted, input.Inverted.Clamp(plane, arcCentre.Inverted, arcMax, true), TestTolerance);
			if (expectation3D != Direction.None) {
				AssertToleranceEquals(expectation3D.Value.ProjectedOnTo(plane), input.Clamp(plane, arcCentre, arcMax, false), TestTolerance);
				AssertToleranceEquals(expectation3D.Value.Inverted.ProjectedOnTo(plane), input.Inverted.Clamp(plane, arcCentre.Inverted, arcMax, false), TestTolerance);
			}
		}

		var testPlane = new Plane(Direction.Up, (300f, -1.3f, -100f));

		// 0deg arc
		AssertCombination(Direction.Forward, testPlane, Direction.Forward, 0f, Direction.Forward);
		AssertCombination(Direction.Forward, testPlane, Direction.Forward, 0f, Direction.Backward);
		AssertCombination(Direction.Forward, testPlane, Direction.Forward, 0f, Direction.Left);
		AssertCombination(Direction.Forward, testPlane, Direction.Forward, 0f, Direction.Right);
		AssertCombination(
			Direction.Forward * (Direction.Forward >> Direction.Up).WithAngle(new Direction(1f, 1f, 1f).AngleTo(testPlane)), 
			testPlane, 
			Direction.Forward, 
			0f, 
			(1f, 1f, 1f)
		);
		AssertCombination(
			Direction.Forward * (Direction.Forward >> Direction.Down).WithAngle(new Direction(-1f, -1f, -1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			0f,
			(-1f, -1f, -1f)
		);
		AssertCombination(null, testPlane, Direction.Forward, 0f, Direction.Up);
		AssertCombination(null, testPlane, Direction.Forward, 0f, Direction.Down);
		AssertCombination(Direction.None, testPlane, Direction.Forward, 0f, Direction.None);

		// 45deg arc
		var forwardRight22 = Direction.Forward * (Direction.Forward >> Direction.Right).ScaledBy(0.25f);
		var forwardLeft22 = Direction.Forward * (Direction.Forward >> Direction.Left).ScaledBy(0.25f);
		AssertCombination(Direction.Forward, testPlane, Direction.Forward, 45f, Direction.Forward); // 0 deg
		AssertCombination((-0.01f, 0f, 1f), testPlane, Direction.Forward, 45f, (-0.01f, 0f, 1f)); // > 0deg
		AssertCombination((0.01f, 0f, 1f), testPlane, Direction.Forward, 45f, (0.01f, 0f, 1f)); // > 0deg
		AssertCombination(forwardRight22, testPlane, Direction.Forward, 45f, (-1f, 0f, 1f)); // 45deg 
		AssertCombination(forwardLeft22, testPlane, Direction.Forward, 45f, (1f, 0f, 1f)); // 45deg
		AssertCombination(forwardRight22, testPlane, Direction.Forward, 45f, Direction.Right); // 90deg 
		AssertCombination(forwardLeft22, testPlane, Direction.Forward, 45f, Direction.Left); // 90deg
		AssertCombination(forwardRight22, testPlane, Direction.Forward, 45f, (-1f, 0f, -1f)); // 135deg 
		AssertCombination(forwardLeft22, testPlane, Direction.Forward, 45f, (1f, 0f, -1f)); // 135deg
		AssertCombination(forwardRight22, testPlane, Direction.Forward, 45f, (-0.01f, 0f, -1f)); // < 180deg
		AssertCombination(forwardLeft22, testPlane, Direction.Forward, 45f, (0.01f, 0f, -1f)); // < 180deg
		AssertCombination( // > 0 deg + orthogonal
			new Direction(-0.01f, 0f, 1f) * (new Direction(-0.01f, 0f, 1f) >> Direction.Up).WithAngle(new Direction(-0.01f, 0.5f, 1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			45f,
			(-0.01f, 0.5f, 1f)
		);
		AssertCombination( // > 0 deg + orthogonal
			new Direction(0.01f, 0f, 1f) * (new Direction(0.01f, 0f, 1f) >> Direction.Up).WithAngle(new Direction(0.01f, 0.5f, 1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			45f,
			(0.01f, 0.5f, 1f)
		);
		AssertCombination( // 45 deg + orthogonal
			forwardRight22 * (forwardRight22 >> Direction.Up).WithAngle(new Direction(-1f, 0.5f, 1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			45f,
			(-1f, 0.5f, 1f)
		);
		AssertCombination( // 45 deg + orthogonal
			forwardLeft22 * (forwardLeft22 >> Direction.Up).WithAngle(new Direction(1f, 0.5f, 1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			45f,
			(1f, 0.5f, 1f)
		);
		AssertCombination( // 90 deg + orthogonal
			forwardRight22 * (forwardRight22 >> Direction.Up).WithAngle(new Direction(-1f, 0.5f, 0f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			45f,
			(-1f, 0.5f, 0f)
		);
		AssertCombination( // 90 deg + orthogonal
			forwardLeft22 * (forwardLeft22 >> Direction.Up).WithAngle(new Direction(1f, 0.5f, 0f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			45f,
			(1f, 0.5f, 0f)
		);
		AssertCombination( // 135 deg + orthogonal
			forwardRight22 * (forwardRight22 >> Direction.Up).WithAngle(new Direction(-1f, 0.5f, -1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			45f,
			(-1f, 0.5f, -1f)
		);
		AssertCombination( // 135 deg + orthogonal
			forwardLeft22 * (forwardLeft22 >> Direction.Up).WithAngle(new Direction(1f, 0.5f, -1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			45f,
			(1f, 0.5f, -1f)
		);
		AssertCombination( // < 180 deg + orthogonal
			forwardRight22 * (forwardRight22 >> Direction.Up).WithAngle(new Direction(-0.01f, 0.5f, -1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			45f,
			(-0.01f, 0.5f, -1f)
		);
		AssertCombination( // < 180 deg + orthogonal
			forwardLeft22 * (forwardLeft22 >> Direction.Up).WithAngle(new Direction(0.01f, 0.5f, -1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			45f,
			(0.01f, 0.5f, -1f)
		);
		AssertCombination(null, testPlane, Direction.Forward, 45f, Direction.Up);
		AssertCombination(null, testPlane, Direction.Forward, 45f, Direction.Down);
		AssertCombination(Direction.None, testPlane, Direction.Forward, 45f, Direction.None);

		// 90deg arc
		var forwardRight45 = Direction.Forward * (Direction.Forward >> Direction.Right).ScaledBy(0.5f);
		var forwardLeft45 = Direction.Forward * (Direction.Forward >> Direction.Left).ScaledBy(0.5f);
		AssertCombination(Direction.Forward, testPlane, Direction.Forward, 90f, Direction.Forward); // 0 deg
		AssertCombination((-0.01f, 0f, 1f), testPlane, Direction.Forward, 90f, (-0.01f, 0f, 1f)); // > 0deg
		AssertCombination((0.01f, 0f, 1f), testPlane, Direction.Forward, 90f, (0.01f, 0f, 1f)); // > 0deg
		AssertCombination((-1f, 0f, 1f), testPlane, Direction.Forward, 90f, (-1f, 0f, 1f)); // 45deg 
		AssertCombination((1f, 0f, 1f), testPlane, Direction.Forward, 90f, (1f, 0f, 1f)); // 45deg
		AssertCombination(forwardRight45, testPlane, Direction.Forward, 90f, Direction.Right); // 90deg 
		AssertCombination(forwardLeft45, testPlane, Direction.Forward, 90f, Direction.Left); // 90deg
		AssertCombination(forwardRight45, testPlane, Direction.Forward, 90f, (-1f, 0f, -1f)); // 135deg 
		AssertCombination(forwardLeft45, testPlane, Direction.Forward, 90f, (1f, 0f, -1f)); // 135deg
		AssertCombination(forwardRight45, testPlane, Direction.Forward, 90f, (-0.01f, 0f, -1f)); // < 180deg
		AssertCombination(forwardLeft45, testPlane, Direction.Forward, 90f, (0.01f, 0f, -1f)); // < 180deg
		AssertCombination( // > 0 deg + orthogonal
			new Direction(-0.01f, 0f, 1f) * (new Direction(-0.01f, 0f, 1f) >> Direction.Up).WithAngle(new Direction(-0.01f, 0.5f, 1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			90f,
			(-0.01f, 0.5f, 1f)
		);
		AssertCombination( // > 0 deg + orthogonal
			new Direction(0.01f, 0f, 1f) * (new Direction(0.01f, 0f, 1f) >> Direction.Up).WithAngle(new Direction(0.01f, 0.5f, 1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			90f,
			(0.01f, 0.5f, 1f)
		);
		AssertCombination( // 45 deg + orthogonal
			new Direction(-1f, 0f, 1f) * (new Direction(-1f, 0f, 1f) >> Direction.Up).WithAngle(new Direction(-1f, 0.5f, 1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			90f,
			(-1f, 0.5f, 1f)
		);
		AssertCombination( // 45 deg + orthogonal
			new Direction(1f, 0f, 1f) * (new Direction(1f, 0f, 1f) >> Direction.Up).WithAngle(new Direction(1f, 0.5f, 1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			90f,
			(1f, 0.5f, 1f)
		);
		AssertCombination( // 90 deg + orthogonal
			forwardRight45 * (forwardRight45 >> Direction.Up).WithAngle(new Direction(-1f, 0.5f, 0f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			90f,
			(-1f, 0.5f, 0f)
		);
		AssertCombination( // 90 deg + orthogonal
			forwardLeft45 * (forwardLeft45 >> Direction.Up).WithAngle(new Direction(1f, 0.5f, 0f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			90f,
			(1f, 0.5f, 0f)
		);
		AssertCombination( // 135 deg + orthogonal
			forwardRight45 * (forwardRight45 >> Direction.Up).WithAngle(new Direction(-1f, 0.5f, -1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			90f,
			(-1f, 0.5f, -1f)
		);
		AssertCombination( // 135 deg + orthogonal
			forwardLeft45 * (forwardLeft45 >> Direction.Up).WithAngle(new Direction(1f, 0.5f, -1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			90f,
			(1f, 0.5f, -1f)
		);
		AssertCombination( // < 180 deg + orthogonal
			forwardRight45 * (forwardRight45 >> Direction.Up).WithAngle(new Direction(-0.01f, 0.5f, -1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			90f,
			(-0.01f, 0.5f, -1f)
		);
		AssertCombination( // < 180 deg + orthogonal
			forwardLeft45 * (forwardLeft45 >> Direction.Up).WithAngle(new Direction(0.01f, 0.5f, -1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			90f,
			(0.01f, 0.5f, -1f)
		);
		AssertCombination(null, testPlane, Direction.Forward, 90f, Direction.Up);
		AssertCombination(null, testPlane, Direction.Forward, 90f, Direction.Down);
		AssertCombination(Direction.None, testPlane, Direction.Forward, 90f, Direction.None);

		// 180deg arc
		var forwardRight90 = Direction.Right;
		var forwardLeft90 = Direction.Left;
		AssertCombination(Direction.Forward, testPlane, Direction.Forward, 180f, Direction.Forward); // 0 deg
		AssertCombination((-0.01f, 0f, 1f), testPlane, Direction.Forward, 180f, (-0.01f, 0f, 1f)); // > 0deg
		AssertCombination((0.01f, 0f, 1f), testPlane, Direction.Forward, 180f, (0.01f, 0f, 1f)); // > 0deg
		AssertCombination((-1f, 0f, 1f), testPlane, Direction.Forward, 180f, (-1f, 0f, 1f)); // 45deg 
		AssertCombination((1f, 0f, 1f), testPlane, Direction.Forward, 180f, (1f, 0f, 1f)); // 45deg
		AssertCombination(Direction.Right, testPlane, Direction.Forward, 180f, Direction.Right); // 90deg 
		AssertCombination(Direction.Left, testPlane, Direction.Forward, 180f, Direction.Left); // 90deg
		AssertCombination(forwardRight90, testPlane, Direction.Forward, 180f, (-1f, 0f, -1f)); // 135deg 
		AssertCombination(forwardLeft90, testPlane, Direction.Forward, 180f, (1f, 0f, -1f)); // 135deg
		AssertCombination(forwardRight90, testPlane, Direction.Forward, 180f, (-0.01f, 0f, -1f)); // < 180deg
		AssertCombination(forwardLeft90, testPlane, Direction.Forward, 180f, (0.01f, 0f, -1f)); // < 180deg
		AssertCombination( // > 0 deg + orthogonal
			new Direction(-0.01f, 0f, 1f) * (new Direction(-0.01f, 0f, 1f) >> Direction.Up).WithAngle(new Direction(-0.01f, 0.5f, 1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			180f,
			(-0.01f, 0.5f, 1f)
		);
		AssertCombination( // > 0 deg + orthogonal
			new Direction(0.01f, 0f, 1f) * (new Direction(0.01f, 0f, 1f) >> Direction.Up).WithAngle(new Direction(0.01f, 0.5f, 1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			180f,
			(0.01f, 0.5f, 1f)
		);
		AssertCombination( // 90 deg + orthogonal
			new Direction(-1f, 0f, 1f) * (new Direction(-1f, 0f, 1f) >> Direction.Up).WithAngle(new Direction(-1f, 0.5f, 1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			180f,
			(-1f, 0.5f, 1f)
		);
		AssertCombination( // 90 deg + orthogonal
			new Direction(1f, 0f, 1f) * (new Direction(1f, 0f, 1f) >> Direction.Up).WithAngle(new Direction(1f, 0.5f, 1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			180f,
			(1f, 0.5f, 1f)
		);
		AssertCombination( // 90 deg + orthogonal
			forwardRight90 * (forwardRight90 >> Direction.Up).WithAngle(new Direction(-1f, 0.5f, 0f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			180f,
			(-1f, 0.5f, 0f)
		);
		AssertCombination( // 90 deg + orthogonal
			forwardLeft90 * (forwardLeft90 >> Direction.Up).WithAngle(new Direction(1f, 0.5f, 0f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			180f,
			(1f, 0.5f, 0f)
		);
		AssertCombination( // 135 deg + orthogonal
			forwardRight90 * (forwardRight90 >> Direction.Up).WithAngle(new Direction(-1f, 0.5f, -1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			180f,
			(-1f, 0.5f, -1f)
		);
		AssertCombination( // 135 deg + orthogonal
			forwardLeft90 * (forwardLeft90 >> Direction.Up).WithAngle(new Direction(1f, 0.5f, -1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			180f,
			(1f, 0.5f, -1f)
		);
		AssertCombination( // < 180 deg + orthogonal
			forwardRight90 * (forwardRight90 >> Direction.Up).WithAngle(new Direction(-0.01f, 0.5f, -1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			180f,
			(-0.01f, 0.5f, -1f)
		);
		AssertCombination( // < 180 deg + orthogonal
			forwardLeft90 * (forwardLeft90 >> Direction.Up).WithAngle(new Direction(0.01f, 0.5f, -1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			180f,
			(0.01f, 0.5f, -1f)
		);
		AssertCombination(null, testPlane, Direction.Forward, 180f, Direction.Up);
		AssertCombination(null, testPlane, Direction.Forward, 180f, Direction.Down);
		AssertCombination(Direction.None, testPlane, Direction.Forward, 180f, Direction.None);

		// 270deg arc
		var forwardRight135 = new Direction(-1f, 0f, -1f);
		var forwardLeft135 = new Direction(1f, 0f, -1f);
		AssertCombination(Direction.Forward, testPlane, Direction.Forward, 270f, Direction.Forward); // 0 deg
		AssertCombination((-0.01f, 0f, 1f), testPlane, Direction.Forward, 270f, (-0.01f, 0f, 1f)); // > 0deg
		AssertCombination((0.01f, 0f, 1f), testPlane, Direction.Forward, 270f, (0.01f, 0f, 1f)); // > 0deg
		AssertCombination((-1f, 0f, 1f), testPlane, Direction.Forward, 270f, (-1f, 0f, 1f)); // 45deg 
		AssertCombination((1f, 0f, 1f), testPlane, Direction.Forward, 270f, (1f, 0f, 1f)); // 45deg
		AssertCombination(Direction.Right, testPlane, Direction.Forward, 270f, Direction.Right); // 90deg 
		AssertCombination(Direction.Left, testPlane, Direction.Forward, 270f, Direction.Left); // 90deg
		AssertCombination(forwardRight135, testPlane, Direction.Forward, 270f, (-1f, 0f, -1f)); // 135deg 
		AssertCombination(forwardLeft135, testPlane, Direction.Forward, 270f, (1f, 0f, -1f)); // 135deg
		AssertCombination(forwardRight135, testPlane, Direction.Forward, 270f, (-0.01f, 0f, -1f)); // < 180deg
		AssertCombination(forwardLeft135, testPlane, Direction.Forward, 270f, (0.01f, 0f, -1f)); // < 180deg
		AssertCombination( // > 0 deg + orthogonal
			new Direction(-0.01f, 0f, 1f) * (new Direction(-0.01f, 0f, 1f) >> Direction.Up).WithAngle(new Direction(-0.01f, 0.5f, 1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			270f,
			(-0.01f, 0.5f, 1f)
		);
		AssertCombination( // > 0 deg + orthogonal
			new Direction(0.01f, 0f, 1f) * (new Direction(0.01f, 0f, 1f) >> Direction.Up).WithAngle(new Direction(0.01f, 0.5f, 1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			270f,
			(0.01f, 0.5f, 1f)
		);
		AssertCombination( // 90 deg + orthogonal
			new Direction(-1f, 0f, 1f) * (new Direction(-1f, 0f, 1f) >> Direction.Up).WithAngle(new Direction(-1f, 0.5f, 1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			270f,
			(-1f, 0.5f, 1f)
		);
		AssertCombination( // 90 deg + orthogonal
			new Direction(1f, 0f, 1f) * (new Direction(1f, 0f, 1f) >> Direction.Up).WithAngle(new Direction(1f, 0.5f, 1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			270f,
			(1f, 0.5f, 1f)
		);
		AssertCombination( // 90 deg + orthogonal
			Direction.Right * (Direction.Right >> Direction.Up).WithAngle(new Direction(-1f, 0.5f, 0f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			270f,
			(-1f, 0.5f, 0f)
		);
		AssertCombination( // 90 deg + orthogonal
			Direction.Left * (Direction.Left >> Direction.Up).WithAngle(new Direction(1f, 0.5f, 0f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			270f,
			(1f, 0.5f, 0f)
		);
		AssertCombination( // 135 deg + orthogonal
			forwardRight135 * (forwardRight135 >> Direction.Up).WithAngle(new Direction(-1f, 0.5f, -1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			270f,
			(-1f, 0.5f, -1f)
		);
		AssertCombination( // 135 deg + orthogonal
			forwardLeft135 * (forwardLeft135 >> Direction.Up).WithAngle(new Direction(1f, 0.5f, -1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			270f,
			(1f, 0.5f, -1f)
		);
		AssertCombination( // < 180 deg + orthogonal
			forwardRight135 * (forwardRight135 >> Direction.Up).WithAngle(new Direction(-0.01f, 0.5f, -1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			270f,
			(-0.01f, 0.5f, -1f)
		);
		AssertCombination( // < 180 deg + orthogonal
			forwardLeft135 * (forwardLeft135 >> Direction.Up).WithAngle(new Direction(0.01f, 0.5f, -1f).AngleTo(testPlane)),
			testPlane,
			Direction.Forward,
			270f,
			(0.01f, 0.5f, -1f)
		);
		AssertCombination(null, testPlane, Direction.Forward, 270f, Direction.Up);
		AssertCombination(null, testPlane, Direction.Forward, 270f, Direction.Down);
		AssertCombination(Direction.None, testPlane, Direction.Forward, 270f, Direction.None);
		
		// 360deg arc
		var testList = new List<Direction>();
		for (var x = -4f; x <= 4f; x += 1f) {
			for (var z = -4f; z <= 4f; z += 1f) {
				testList.Add(new(x, 0f, z));
			}
		}

		for (var i = 0; i < testList.Count; ++i) {
			var input = testList[i];
			if (input == Direction.None) continue;
			for (var j = i; j < testList.Count; ++j) {
				var target = testList[j];
				if (target == Direction.None) continue;
				AssertCombination(input, testPlane, target, 360f, input);
			}
		}

		// Parameter checks
		Assert.Throws<ArgumentException>(() => Direction.Left.Clamp(testPlane, Direction.Down, 45f, false));
		Assert.Throws<ArgumentException>(() => Direction.Left.Clamp(testPlane, Direction.None, 45f, false));
	}
}