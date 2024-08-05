// Created on 2023-10-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[TestFixture]
class DirectionTest {
	const float TestTolerance = 0.001f;
	static readonly Direction OneTwoNegThree = new(1f, 2f, -3f);
	static readonly Vector3 NormalizedV3 = Vector3.Normalize(new(1f, 2f, -3f));

	[Test]
	public void ShouldBeCorrectStructSize() => AssertStructLayout<Direction>();

	[Test]
	public void ShouldCorrectlyInitializeStaticReadonlyMembers() {
		Assert.AreEqual(new Direction(0f, 0f, 0f), Direction.None);
		Assert.AreEqual(new Direction(0f, 0f, 1f), Direction.Forward);
		Assert.AreEqual(new Direction(0f, 0f, -1f), Direction.Backward);
		Assert.AreEqual(new Direction(0f, 1f, 0f), Direction.Up);
		Assert.AreEqual(new Direction(0f, -1f, 0f), Direction.Down);
		Assert.AreEqual(new Direction(1f, 0f, 0f), Direction.Left);
		Assert.AreEqual(new Direction(-1f, 0f, 0f), Direction.Right);

		Assert.IsTrue(Direction.AllCardinals.Contains(Direction.Backward));
		Assert.IsTrue(Direction.AllCardinals.Contains(Direction.Forward));
		Assert.IsTrue(Direction.AllCardinals.Contains(Direction.Left));
		Assert.IsTrue(Direction.AllCardinals.Contains(Direction.Right));
		Assert.IsTrue(Direction.AllCardinals.Contains(Direction.Up));
		Assert.IsTrue(Direction.AllCardinals.Contains(Direction.Down));
		Assert.AreEqual(6, Direction.AllCardinals.Length);

		foreach (var diagonal in OrientationUtils.AllDiagonals) {
			Assert.Contains(diagonal.ToDirection(), Direction.AllDiagonals.ToArray());
		}
		Assert.AreEqual(OrientationUtils.AllDiagonals.Length, Direction.AllDiagonals.Length);
		for (var i = 0; i < Direction.AllDiagonals.Length; ++i) {
			for (var j = i + 1; j < Direction.AllDiagonals.Length; ++j) {
				Assert.AreNotEqual(Direction.AllDiagonals[i], Direction.AllDiagonals[j]);
			}
		}

		foreach (var intercardinal in OrientationUtils.AllIntercardinals) {
			Assert.Contains(intercardinal.ToDirection(), Direction.AllIntercardinals.ToArray());
		}
		Assert.AreEqual(OrientationUtils.AllIntercardinals.Length, Direction.AllIntercardinals.Length);
		for (var i = 0; i < Direction.AllIntercardinals.Length; ++i) {
			for (var j = i + 1; j < Direction.AllIntercardinals.Length; ++j) {
				Assert.AreNotEqual(Direction.AllIntercardinals[i], Direction.AllIntercardinals[j]);
			}
		}

		foreach (var orientation in OrientationUtils.All3DOrientations) {
			Assert.Contains(orientation.ToDirection(), Direction.AllOrientations.ToArray());
		}
		Assert.AreEqual(OrientationUtils.All3DOrientations.Length, Direction.AllOrientations.Length);
		for (var i = 0; i < Direction.AllOrientations.Length; ++i) {
			for (var j = i + 1; j < Direction.AllOrientations.Length; ++j) {
				Assert.AreNotEqual(Direction.AllOrientations[i], Direction.AllOrientations[j]);
			}
		}
	}

	[Test]
	public void ShouldCorrectlyImplementProperties() {
		Assert.AreEqual(NormalizedV3.X, OneTwoNegThree.X);
		Assert.AreEqual(NormalizedV3.Y, OneTwoNegThree.Y);
		Assert.AreEqual(NormalizedV3.Z, OneTwoNegThree.Z);
	}

	[Test]
	public void ConstructorsShouldCorrectlyConstruct() {
		Assert.AreEqual(Direction.None, new Direction());
		Assert.AreEqual(Direction.WValue, new Direction().AsVector4.W);

		Assert.AreEqual(new Direction(new Vector4(0.1f, 0.2f, 0.3f, Direction.WValue)), Direction.FromVector3PreNormalized(0.1f, 0.2f, 0.3f));
		Assert.AreEqual(Direction.WValue, new Direction(0.1f, 0.2f, 0.3f).AsVector4.W);

		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					var expectation = (x == 0f && y == 0f && z == 0f) ? Vector3.Zero : Vector3.Normalize(new(x, y, z));
					var actual = new Direction(x, y, z);
					Assert.AreEqual(expectation.X, actual.X);
					Assert.AreEqual(expectation.Y, actual.Y);
					Assert.AreEqual(expectation.Z, actual.Z);
				}
			}
		}
	}

	[Test]
	public void StaticFactoryMethodsShouldCorrectlyConstruct() {
		Assert.AreEqual(new Direction(-1.2f, 2.4f, 0f), Direction.FromVector3(new(-1.2f, 2.4f, 0f)));

		var prenormDirA = Direction.FromVector3PreNormalized(7f, -1.2f, 0f);
		var prenormDirB = Direction.FromVector3PreNormalized(new(0f, 0.707f, -0.707f));
		Assert.AreEqual(7f, prenormDirA.X);
		Assert.AreEqual(-1.2f, prenormDirA.Y);
		Assert.AreEqual(0f, prenormDirA.Z);
		Assert.AreEqual(Direction.WValue, prenormDirA.AsVector4.W);
		Assert.AreEqual(0f, prenormDirB.X);
		Assert.AreEqual(0.707f, prenormDirB.Y);
		Assert.AreEqual(-0.707f, prenormDirB.Z);
		Assert.AreEqual(Direction.WValue, prenormDirB.AsVector4.W);
	}

	[Test]
	public void ShouldCorrectlyConvertToVector3() {
		Assert.AreEqual(NormalizedV3, OneTwoNegThree.ToVector3());
	}

	[Test]
	public void ShouldCorrectlyConstructFromPlaneAndPolarAngle() {
		var plane = new Plane(Direction.Down, (100f, 4f, -3f));
		AssertToleranceEquals(Direction.Forward, Direction.FromPlaneAndPolarAngle(plane, Direction.Forward, 0f), TestTolerance);
		AssertToleranceEquals(Direction.Backward, Direction.FromPlaneAndPolarAngle(plane, Direction.Backward, 0f), TestTolerance);
		AssertToleranceEquals(Direction.Backward, Direction.FromPlaneAndPolarAngle(plane, Direction.Forward, 180f), TestTolerance);
		AssertToleranceEquals(Direction.Forward, Direction.FromPlaneAndPolarAngle(plane, Direction.Backward, 180f), TestTolerance);
		AssertToleranceEquals(Direction.Right, Direction.FromPlaneAndPolarAngle(plane, Direction.Forward, 90f), TestTolerance);
		AssertToleranceEquals(Direction.Left, Direction.FromPlaneAndPolarAngle(plane, Direction.Forward, -90f), TestTolerance);
		AssertToleranceEquals(Direction.Right, Direction.FromPlaneAndPolarAngle(plane, Direction.Backward, 270f), TestTolerance);
		AssertToleranceEquals(Direction.Left, Direction.FromPlaneAndPolarAngle(plane, Direction.Backward, -270f), TestTolerance);
		plane = plane.Flipped;
		AssertToleranceEquals(Direction.Left, Direction.FromPlaneAndPolarAngle(plane, Direction.Forward, 90f), TestTolerance);
		AssertToleranceEquals(Direction.Right, Direction.FromPlaneAndPolarAngle(plane, Direction.Forward, -90f), TestTolerance);
		AssertToleranceEquals(Direction.Left, Direction.FromPlaneAndPolarAngle(plane, Direction.Backward, 270f), TestTolerance);
		AssertToleranceEquals(Direction.Right, Direction.FromPlaneAndPolarAngle(plane, Direction.Backward, -270f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyConvertFromOrientation3D() {
		foreach (var orientation in OrientationUtils.All3DOrientations) {
			var result = Direction.FromOrientation(orientation);
			try {
				Assert.AreEqual(orientation.GetAxisSign(Axis.X), Single.Sign(result.X));
				Assert.AreEqual(orientation.GetAxisSign(Axis.Y), Single.Sign(result.Y));
				Assert.AreEqual(orientation.GetAxisSign(Axis.Z), Single.Sign(result.Z));
			}
			catch (Exception) {
				Console.WriteLine(orientation + " => " + result);
				throw;
			}
		}

		Assert.AreEqual(Direction.None, Orientation3D.None.ToDirection());
		Assert.AreEqual(Direction.Left, Orientation3D.Left.ToDirection());
		Assert.AreEqual(Direction.Right, Orientation3D.Right.ToDirection());
		Assert.AreEqual(Direction.Up, Orientation3D.Up.ToDirection());
		Assert.AreEqual(Direction.Down, Orientation3D.Down.ToDirection());
		Assert.AreEqual(Direction.Forward, Orientation3D.Forward.ToDirection());
		Assert.AreEqual(Direction.Backward, Orientation3D.Backward.ToDirection());
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromValueTuple() {
		Assert.AreEqual(OneTwoNegThree, (Direction) (1, 2, -3));
		var (x, y, z) = OneTwoNegThree;
		Assert.AreEqual(NormalizedV3.X, x);
		Assert.AreEqual(NormalizedV3.Y, y);
		Assert.AreEqual(NormalizedV3.Z, z);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<Direction>();
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(Direction.None, OneTwoNegThree, new(-0.001f, 0f, 100000f));
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(Direction.None, 0f, 0f, 0f);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(OneTwoNegThree, 1f / MathF.Sqrt(14f), 2f / MathF.Sqrt(14f), -3f / MathF.Sqrt(14f));
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(new Direction(-0.001f, 0f, 100000f), new Direction(-0.001f, 0f, 100000f).X, new Direction(-0.001f, 0f, 100000f).Y, new Direction(-0.001f, 0f, 100000f).Z);
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		void AssertIteration(Direction input, string expectedValue) {
			var testCulture = CultureInfo.InvariantCulture;
			var testFormat = "N1";

			Span<char> formatSpan = stackalloc char[expectedValue.Length];
			Assert.IsTrue(input.TryFormat(formatSpan, out var charsWritten, testFormat, testCulture));
			Assert.AreEqual(formatSpan.Length, charsWritten);

			Assert.AreEqual(expectedValue, input.ToString(testFormat, testCulture));
			Assert.AreEqual(expectedValue, new String(formatSpan));
		}

		AssertIteration(Direction.None, "<0.0, 0.0, 0.0>");
		AssertIteration(OneTwoNegThree, NormalizedV3.ToString("N1", CultureInfo.InvariantCulture));
		AssertIteration(new Direction(-0.813f, -0.273f, -0.515f), "<-0.8, -0.3, -0.5>");
	}

	[Test]
	public void ShouldCorrectlyFormatToString() {
		void AssertFail(Direction input, Span<char> destination, ReadOnlySpan<char> format, IFormatProvider? provider) {
			Assert.AreEqual(false, input.TryFormat(destination, out _, format, provider));
		}

		void AssertSuccess(
			Direction input,
			Span<char> destination,
			ReadOnlySpan<char> format,
			IFormatProvider? provider,
			ReadOnlySpan<char> expectedDestSpanValue
		) {
			var actualReturnValue = input.TryFormat(destination, out var numCharsWritten, format, provider);
			Assert.AreEqual(true, actualReturnValue);
			Assert.AreEqual(expectedDestSpanValue.Length, numCharsWritten);
			Assert.IsTrue(
				expectedDestSpanValue.SequenceEqual(destination[..expectedDestSpanValue.Length]),
				$"Destination as string was {new String(destination)}"
			);
		}

		var fractionalVect = new Direction(0.180711f, 0.510648f, -0.840584f);

		AssertFail(Direction.None, Array.Empty<char>(), "N0", null);
		AssertFail(Direction.None, new char[8], "N0", null);
		AssertSuccess(Direction.None, new char[9], "N0", null, "<0, 0, 0>");
		AssertFail(fractionalVect, new char[8], "N0", null);
		AssertSuccess(fractionalVect, new char[10], "N0", null, "<0, 1, -1>");
		AssertFail(fractionalVect, new char[10], "N1", null);
		AssertSuccess(fractionalVect, new char[16], "N1", null, "<0.2, 0.5, -0.8>");
		AssertSuccess(fractionalVect, new char[16], "N1", CultureInfo.CreateSpecificCulture("de-DE"), "<0,2. 0,5. -0,8>");
		AssertSuccess(fractionalVect, new char[22], "N3", null, "<0.181, 0.511, -0.841>");
	}

	[Test]
	public void ShouldCorrectlyParseFromString() {
		var testCulture = CultureInfo.InvariantCulture;

		void AssertSuccess(string input, Direction expectedResult) {
			AssertToleranceEquals(expectedResult, Direction.Parse(input, testCulture), TestTolerance);
			AssertToleranceEquals(expectedResult, Direction.Parse(input.AsSpan(), testCulture), TestTolerance);
			Assert.IsTrue(Direction.TryParse(input, testCulture, out var parseResult));
			AssertToleranceEquals(expectedResult, parseResult, TestTolerance);
			Assert.IsTrue(Direction.TryParse(input.AsSpan(), testCulture, out parseResult));
			AssertToleranceEquals(expectedResult, parseResult, TestTolerance);
		}

		void AssertFail(string input) {
			Assert.Catch(() => Direction.Parse(input, testCulture));
			Assert.Catch(() => Direction.Parse(input.AsSpan(), testCulture));
			Assert.False(Direction.TryParse(input, testCulture, out _));
			Assert.False(Direction.TryParse(input.AsSpan(), testCulture, out _));
		}

		AssertFail("");
		AssertFail("<>");
		AssertFail("1, 2, 3");
		AssertFail("<1, 2, 3");
		AssertFail("1, 2, 3>");
		AssertFail("<1, 2>");
		AssertFail("<1, 2,>");
		AssertFail("<1, 2, >");
		AssertFail("<1 2 3>");
		AssertFail("<a, 1, 2>");
		AssertFail("<, 1, 2>");
		AssertFail("<1, c, 2>");
		AssertFail("<1, 2, ->");
		AssertSuccess("<1, 2, 3>", new(1f, 2f, 3f));
		AssertSuccess("<1,2,3>", new(1f, 2f, 3f));
		AssertSuccess("<1.1, 2.2, 3.3>", new(1.1f, 2.2f, 3.3f));
		AssertSuccess("<1,2,3>", new(1f, 2f, 3f));
		AssertSuccess("<-1.1, 2.2,3.3>", new(-1.1f, 2.2f, 3.3f));
	}

	[Test]
	public void ShouldCorrectlyImplementEqualityMembers() {
		Assert.AreEqual(Direction.None, new Direction(-0f, -0f, -0f));
		Assert.AreNotEqual(Direction.None, OneTwoNegThree);
		Assert.IsTrue(OneTwoNegThree.Equals(OneTwoNegThree));
		Assert.IsFalse(OneTwoNegThree.Equals(Direction.None));
		Assert.IsTrue(OneTwoNegThree == new Direction(1f, 2f, -3f));
		Assert.IsFalse(Direction.None == OneTwoNegThree);
		Assert.IsFalse(Direction.None != new Direction(0f, 0f, 0f));
		Assert.IsTrue(OneTwoNegThree != Direction.None);
		Assert.IsTrue(new Direction(1f, 2f, 3f) != new Direction(0f, 2f, 3f));
		Assert.IsTrue(new Direction(1f, 2f, 3f) != new Direction(1f, 0f, 3f));
		Assert.IsTrue(new Direction(1f, 2f, 3f) != new Direction(1f, 2f, 0f));

		Assert.IsTrue(Direction.None.Equals(Direction.None, 0f));
		Assert.IsTrue(OneTwoNegThree.Equals(OneTwoNegThree, 0f));
		Assert.IsTrue(new Direction(0.5f, 0.6f, 0.7f).Equals(new Direction(0.4f, 0.5f, 0.6f), 0.05f));
		Assert.IsFalse(new Direction(0.5f, 0.6f, 0.7f).Equals(new Direction(0.4f, 0.5f, 0.6f), 0.02f));
		Assert.IsTrue(new Direction(-0.5f, -0.5f, -0.5f).Equals(new Direction(-0.4f, -0.4f, -0.4f), 0f));
		Assert.IsFalse(new Direction(-0.5f, -0.6f, -0.7f).Equals(new Direction(-0.4f, -0.5f, -0.6f), 0.02f));
		Assert.IsTrue(new Direction(0.5f, 0.5f, 0.5f).Equals(new Direction(0.4f, 0.4f, 0.4f), 0f));

	}

	[Test]
	public void ShouldCorrectlyImplementEqualityWithAngleTolerance() {
		var perpVec = OneTwoNegThree.AnyOrthogonal();

		Assert.AreEqual(true, OneTwoNegThree.EqualsWithinAngle(OneTwoNegThree, 0f));
		Assert.AreEqual(true, OneTwoNegThree.EqualsWithinAngle(30f % perpVec * OneTwoNegThree, 30f + TestTolerance));
		Assert.AreEqual(false, OneTwoNegThree.EqualsWithinAngle(30f % perpVec * OneTwoNegThree, 28f));

		var testList = new List<Direction>();
		for (var x = -5f; x <= 6f; x += 1.1f) {
			for (var y = -5f; y <= 6f; y += 1.1f) {
				for (var z = -5f; z <= 6f; z += 1.1f) {
					testList.Add(new(x, y, z));
				}
			}
		}

		for (var i = 0; i < testList.Count; ++i) {
			var dirA = testList[i];

			for (var j = i; j < testList.Count; ++j) {
				var dirB = testList[j];
				var angle = dirA ^ dirB;

				Assert.IsFalse(dirA.EqualsWithinAngle(dirB, angle - TestTolerance));
				Assert.IsTrue(dirA.EqualsWithinAngle(dirB, angle + TestTolerance));
			}
		}
	}

	[Test]
	public void ShouldCorrectlyDetermineUnitLength() {
		Assert.AreEqual(true, OneTwoNegThree.IsUnitLength);
		Assert.AreEqual(false, Direction.None.IsUnitLength);
		Assert.AreEqual(true, Direction.FromVector3PreNormalized(0.707f, 0f, -0.707f).IsUnitLength);
		Assert.AreEqual(false, Direction.FromVector3PreNormalized(1, 1f, -1f).IsUnitLength);
	}

	[Test]
	public void ShouldUseAppropriateErrorMarginForUnitLengthTest() {
		const int NumNonNormalizedRotations = 200;

		Assert.AreEqual(true, Direction.FromVector3PreNormalized(0.707f, 0f, -0.707f).IsUnitLength);
		Assert.AreEqual(true, Direction.FromVector3PreNormalized(0.706f, 0f, -0.707f).IsUnitLength);
		Assert.AreEqual(true, Direction.FromVector3PreNormalized(0.707f, 0f, -0.706f).IsUnitLength);
		Assert.AreEqual(false, Direction.FromVector3PreNormalized(0.706f, 0f, -0.706f).IsUnitLength);

		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					var dir = new Direction(x, y, z);
					if (dir == Direction.None) {
						Assert.IsFalse(dir.IsUnitLength);
						continue;
					}

					var rot = (dir >> dir.AnyOrthogonal()) * 0.1f;
					for (var i = 0; i < NumNonNormalizedRotations; ++i) dir = rot.RotateWithoutRenormalizing(dir);
					Assert.IsTrue(dir.IsUnitLength);
				}
			}
		}
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
		Assert.AreEqual(-OneTwoNegThree, OneTwoNegThree.Flipped);
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
	public void ShouldCorrectlyFindAnyOrthogonalDirection() {
		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					var dir = new Direction(x, y, z);
					var perp = dir.AnyOrthogonal();

					if (dir == Direction.None) Assert.AreEqual(Direction.None, perp);
					else AssertToleranceEquals(90f, dir ^ perp, TestTolerance);
				}
			}
		}

		foreach (var cardinal in Direction.AllCardinals) {
			var perp = cardinal.AnyOrthogonal();
			AssertToleranceEquals(90f, cardinal ^ perp, TestTolerance);
			Assert.IsTrue(Direction.AllCardinals.Contains(perp));
		}
	}

	[Test]
	public void ShouldCorrectlyFindOrthogonalDirectionWithAdditionalConstrainingDirection() {
		Assert.AreEqual(Direction.Left, Direction.FromOrthogonal(Direction.Up, Direction.Forward));

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
					Assert.AreEqual(Direction.None, Direction.FromOrthogonal(dirA, dirB));
					continue;
				}

				var thirdOrthogonal = Direction.FromOrthogonal(dirA, dirB);
				AssertToleranceEquals(90f, dirA ^ thirdOrthogonal, 2f);
				AssertToleranceEquals(90f, dirB ^ thirdOrthogonal, 2f);
				Assert.IsTrue(thirdOrthogonal.IsUnitLength);
				thirdOrthogonal = Direction.FromOrthogonal(dirB, dirA);
				AssertToleranceEquals(90f, dirA ^ thirdOrthogonal, 2f);
				AssertToleranceEquals(90f, dirB ^ thirdOrthogonal, 2f);
				Assert.IsTrue(thirdOrthogonal.IsUnitLength);
			}
		}
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeAgainstAnotherDir() {
		foreach (var cardinal in Direction.AllCardinals) {
			var perp = cardinal.AnyOrthogonal();
			var thirdPerp = Direction.FromOrthogonal(cardinal, perp);
			Assert.AreEqual(cardinal, (20f % perp * cardinal).OrthogonalizedAgainst(thirdPerp));
			Assert.AreEqual(cardinal, (-20f % perp * cardinal).OrthogonalizedAgainst(thirdPerp));
			Assert.AreEqual(cardinal, (20f % thirdPerp * cardinal).OrthogonalizedAgainst(perp));
			Assert.AreEqual(cardinal, (-20f % thirdPerp * cardinal).OrthogonalizedAgainst(perp));
		}

		AssertToleranceEquals(null, OneTwoNegThree.OrthogonalizedAgainst(OneTwoNegThree), TestTolerance);
		AssertToleranceEquals(Direction.None, Direction.None.OrthogonalizedAgainst(OneTwoNegThree), TestTolerance);

		AssertToleranceEquals(null, OneTwoNegThree.OrthogonalizedAgainst(-OneTwoNegThree), TestTolerance);
		AssertToleranceEquals(null, -OneTwoNegThree.OrthogonalizedAgainst(OneTwoNegThree), TestTolerance);

		Assert.AreEqual(OneTwoNegThree, OneTwoNegThree.OrthogonalizedAgainst(Direction.None));
		Assert.AreEqual(Direction.None, Direction.None.OrthogonalizedAgainst(Direction.None));

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

			if (dirA == Direction.None) continue;

			AssertToleranceEquals(null, dirA.OrthogonalizedAgainst(dirA), TestTolerance);
			AssertToleranceEquals(Direction.None, Direction.None.OrthogonalizedAgainst(dirA), TestTolerance);

			for (var j = i; j < testList.Count; ++j) {
				var dirB = testList[j];

				if (dirB == Direction.None) continue;

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
	public void OrthogonalizationErrorMarginShouldNotBeTooCoarse() {
		const float MinPermissibleAngleDegrees = 0.01f;

		var testList = new List<Direction>();
		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					testList.Add(new(x, y, z));
				}
			}
		}

		for (var i = 0; i < testList.Count; ++i) {
			var dir = testList[i];
			if (dir == Direction.None) continue;

			var orthoTarget = dir;
			var perp = dir.AnyOrthogonal();
			var angleToTestWith = Angle.Zero;
			while ((dir ^ orthoTarget) < MinPermissibleAngleDegrees) {
				angleToTestWith += MinPermissibleAngleDegrees * 0.25f;
				orthoTarget = (dir >> perp).WithAngle(angleToTestWith) * dir;
			}

			Assert.IsNull(dir.OrthogonalizedAgainst(dir));
			Assert.IsNull(dir.OrthogonalizedAgainst(-dir));
			Assert.IsNull((-dir).OrthogonalizedAgainst(dir));
			Assert.IsNotNull(dir.OrthogonalizedAgainst(orthoTarget));
		}
	}

	[Test]
	public void ShouldCorrectlyParallelizeWithAnotherDir() {
		foreach (var cardinal in Direction.AllCardinals) {
			var perp = cardinal.AnyOrthogonal();
			var thirdPerp = Direction.FromOrthogonal(cardinal, perp);
			Assert.AreEqual(cardinal, (20f % perp * cardinal).ParallelizedWith(cardinal));
			Assert.AreEqual(cardinal, (-20f % perp * cardinal).ParallelizedWith(cardinal));
			Assert.AreEqual(cardinal, (20f % thirdPerp * cardinal).ParallelizedWith(cardinal));
			Assert.AreEqual(cardinal, (-20f % thirdPerp * cardinal).ParallelizedWith(cardinal));

			Assert.AreEqual(cardinal, (20f % perp * cardinal).ParallelizedWith(-cardinal));
			Assert.AreEqual(cardinal, (-20f % perp * cardinal).ParallelizedWith(-cardinal));
			Assert.AreEqual(cardinal, (20f % thirdPerp * cardinal).ParallelizedWith(-cardinal));
			Assert.AreEqual(cardinal, (-20f % thirdPerp * cardinal).ParallelizedWith(-cardinal));
		}

		AssertToleranceEquals(null, OneTwoNegThree.ParallelizedWith(OneTwoNegThree.AnyOrthogonal()), TestTolerance);
		AssertToleranceEquals(Direction.None, Direction.None.ParallelizedWith(OneTwoNegThree), TestTolerance);

		AssertToleranceEquals(null, OneTwoNegThree.ParallelizedWith(-OneTwoNegThree.AnyOrthogonal()), TestTolerance);
		AssertToleranceEquals(null, -OneTwoNegThree.ParallelizedWith(OneTwoNegThree.AnyOrthogonal()), TestTolerance);

		Assert.AreEqual(OneTwoNegThree, OneTwoNegThree.ParallelizedWith(Direction.None));
		Assert.AreEqual(Direction.None, Direction.None.ParallelizedWith(Direction.None));

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

			if (dirA == Direction.None) continue;

			AssertToleranceEquals(dirA, dirA.ParallelizedWith(dirA), TestTolerance);
			AssertToleranceEquals(dirA, dirA.ParallelizedWith(-dirA), TestTolerance);
			AssertToleranceEquals(-dirA, (-dirA).ParallelizedWith(dirA), TestTolerance);
			AssertToleranceEquals(-dirA, (-dirA).ParallelizedWith(-dirA), TestTolerance);
			AssertToleranceEquals(Direction.None, Direction.None.ParallelizedWith(dirA), TestTolerance);

			for (var j = i; j < testList.Count; ++j) {
				var dirB = testList[j];

				if (dirB == Direction.None) continue;

				try {
					if ((dirA ^ dirB).Equals(180f, TestTolerance) || (dirA ^ dirB).Equals(0f, TestTolerance)) {
						AssertToleranceEquals(dirA, dirA.ParallelizedWith(dirB), TestTolerance);
						AssertToleranceEquals(dirB, dirB.ParallelizedWith(dirA), TestTolerance);
						AssertToleranceEquals(dirA, dirA.FastParallelizedWith(dirB), TestTolerance);
						AssertToleranceEquals(dirB, dirB.FastParallelizedWith(dirA), TestTolerance);
					}
					else if ((dirA ^ dirB).Equals(90f, TestTolerance)) {
						Assert.AreEqual(null, dirA.ParallelizedWith(dirB));
					}
					else {
						var angle = dirA.ParallelizedWith(dirB)!.Value ^ dirB;
						Assert.IsTrue(angle.Equals(0f, TestTolerance) || angle.Equals(180f, TestTolerance));
						angle = dirB.ParallelizedWith(dirA)!.Value ^ dirA;
						Assert.IsTrue(angle.Equals(0f, TestTolerance) || angle.Equals(180f, TestTolerance));
						angle = dirA.FastParallelizedWith(dirB) ^ dirB;
						Assert.IsTrue(angle.Equals(0f, TestTolerance) || angle.Equals(180f, TestTolerance));
						angle = dirB.FastParallelizedWith(dirA) ^ dirA;
						Assert.IsTrue(angle.Equals(0f, TestTolerance) || angle.Equals(180f, TestTolerance));
					}
				}
				catch {
					Console.WriteLine(dirA);
					Console.WriteLine(dirB);
					Console.WriteLine(dirA ^ dirB);
					Console.WriteLine(Vector4.Dot(dirA.AsVector4, dirB.AsVector4));
					throw;
				}
			}
		}
	}

	[Test]
	public void ParallelizationErrorMarginShouldNotBeTooCoarse() {
		const float MinPermissibleAngleDegrees = 0.01f;

		var testList = new List<Direction>();
		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					testList.Add(new(x, y, z));
				}
			}
		}

		for (var i = 0; i < testList.Count; ++i) {
			var dir = testList[i];
			if (dir == Direction.None) continue;

			var perp = dir.AnyOrthogonal();
			var thirdPerp = Direction.FromOrthogonal(dir, perp);
			var parallelTarget = perp;
			var angleToTestWith = Angle.Zero;
			while ((dir ^ parallelTarget).Equals(Angle.QuarterCircle, MinPermissibleAngleDegrees)) {
				angleToTestWith += MinPermissibleAngleDegrees * 0.25f;
				parallelTarget = (perp >> dir).WithAngle(angleToTestWith) * perp;
			}

			Assert.IsNull(dir.ParallelizedWith(perp));
			Assert.IsNull(dir.ParallelizedWith(-perp));
			Assert.IsNull((-dir).ParallelizedWith(perp));
			Assert.IsNull((-dir).ParallelizedWith(-perp));

			Assert.IsNull(dir.ParallelizedWith(thirdPerp));
			Assert.IsNull(dir.ParallelizedWith(-thirdPerp));
			Assert.IsNull((-dir).ParallelizedWith(thirdPerp));
			Assert.IsNull((-dir).ParallelizedWith(-thirdPerp));

			Assert.IsNotNull(dir.ParallelizedWith(parallelTarget));
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
			var val = Direction.Random();
			Assert.IsTrue(val.IsUnitLength);
		}
	}

	[Test]
	public void ShouldCorrectlyCreateBoundedRandomValues() {
		const int NumIterations = 10_000;

		for (var i = 0; i < NumIterations; ++i) {
			var start = Direction.Random();
			var end = Direction.Random();

			var angle = start ^ end;
			if (angle > 179f) continue;

			var val = Direction.Random(start, end);
			if (val.Equals(start, 0.1f) || val.Equals(end, 0.1f)) continue;

			AssertToleranceEquals((start >> val).Axis, (start >> end).Axis, 0.1f);
			AssertToleranceEquals((start >> end), (start >> val) + (val >> end), 0.1f);
		}
	}

	[Test]
	public void ShouldCorrectlyCreateConicalRandomValues() {
		const int NumIterations = 10_000;

		for (var i = 0; i < NumIterations; ++i) {
			var centre = Direction.Random();
			var angle = Angle.Random(0f, 180f);

			var result = Direction.Random(centre, angle);
			Assert.LessOrEqual((result ^ centre).AsRadians, angle.AsRadians + TestTolerance);
		}

		for (var i = 0; i < NumIterations; ++i) {
			var centre = Direction.Random();

			var result = Direction.Random(centre, 180f, 90f);
			Assert.LessOrEqual((result ^ centre).AsRadians, Angle.HalfCircle.AsRadians + TestTolerance);
			Assert.GreaterOrEqual((result ^ centre).AsRadians, Angle.QuarterCircle.AsRadians - TestTolerance);
		}

		for (var i = 0; i < NumIterations; ++i) {
			AssertToleranceEquals(90f, Direction.Random(Direction.Up, 90f, 90f) ^ Direction.Up, TestTolerance);
		}
	}

	[Test]
	public void ShouldCorrectlyCreateArcPlanarRandomValues() {
		const int NumIterations = 10_000;

		for (var a = 0f; a < 360f; a += 45f) {
			for (var i = 0; i < NumIterations; ++i) {
				var centre = Direction.Random();
				var plane = Plane.Random();
				while (centre.ParallelizedWith(plane) == null) centre = Direction.Random();

				var result = Direction.Random(plane, centre, a);
				try {
					Assert.LessOrEqual(plane.AngleTo(result).AsDegrees, 1f);
					Assert.LessOrEqual((result ^ centre.ParallelizedWith(plane)!.Value).AsDegrees, a + 1f);
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
	public void ShouldCorrectlyClampDotProduct() {
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

		Assert.GreaterOrEqual(1f, new Direction(-1f, -1f, -1f).Dot(new Direction(-1f, -1f, -1f)));
		Assert.LessOrEqual(-1f, new Direction(-1f, -1f, -1f).Dot(new Direction(1f, 1f, 1f)));
		Assert.GreaterOrEqual(1f, new Direction(1f, 1f, 1f).Dot(new Direction(1f, 1f, 1f)));
		Assert.LessOrEqual(-1f, new Direction(1f, 1f, 1f).Dot(new Direction(-1f, -1f, -1f)));

		// Specific case that failed in other testing that caused me to add the clamp:
		Assert.LessOrEqual(-1f, new Direction(-3f, -3f, -3f).Flipped.Dot(new Direction(-3f, -3f, -3f)));
	}

	[Test]
	public void ShouldCorrectlyClampBetweenTwoDirections() {
		void AssertCombination(Direction expectation, Direction min, Direction max, Direction input) {
			AssertToleranceEquals(expectation, input.Clamp(min, max), TestTolerance);
			AssertToleranceEquals(expectation, input.Clamp(max, min), TestTolerance);
			AssertToleranceEquals(expectation.Flipped, input.Flipped.Clamp(min.Flipped, max.Flipped), TestTolerance);
			AssertToleranceEquals(expectation.Flipped, input.Flipped.Clamp(max.Flipped, min.Flipped), TestTolerance);
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
		Assert.AreEqual(Direction.Forward, Direction.Forward.Clamp(Direction.Forward, Direction.None));
		Assert.AreEqual(Direction.Forward, Direction.Forward.Clamp(Direction.None, Direction.Forward));
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
		Assert.AreEqual(Direction.Forward, Direction.Forward.Clamp(Direction.None, 100f));

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
				AssertToleranceEquals(input, input.Clamp(plane, arcCentre, arcMax, true), TestTolerance);
				AssertToleranceEquals(input.Flipped, input.Flipped.Clamp(plane, arcCentre.Flipped, arcMax, true), TestTolerance);
				plane = plane.Flipped;
				AssertToleranceEquals(input, input.Clamp(plane, arcCentre, arcMax, true), TestTolerance);
				AssertToleranceEquals(input.Flipped, input.Flipped.Clamp(plane, arcCentre.Flipped, arcMax, true), TestTolerance);
				return;
			}

			AssertToleranceEquals(expectation3D.Value, input.Clamp(plane, arcCentre, arcMax, true), TestTolerance);
			AssertToleranceEquals(expectation3D.Value.Flipped, input.Flipped.Clamp(plane, arcCentre.Flipped, arcMax, true), TestTolerance);
			if (expectation3D != Direction.None) {
				AssertToleranceEquals(expectation3D.Value.ParallelizedWith(plane), input.Clamp(plane, arcCentre, arcMax, false), TestTolerance);
				AssertToleranceEquals(expectation3D.Value.Flipped.ParallelizedWith(plane), input.Flipped.Clamp(plane, arcCentre.Flipped, arcMax, false), TestTolerance);
			}

			plane = plane.Flipped;
			AssertToleranceEquals(expectation3D.Value, input.Clamp(plane, arcCentre, arcMax, true), TestTolerance);
			AssertToleranceEquals(expectation3D.Value.Flipped, input.Flipped.Clamp(plane, arcCentre.Flipped, arcMax, true), TestTolerance);
			if (expectation3D != Direction.None) {
				AssertToleranceEquals(expectation3D.Value.ParallelizedWith(plane), input.Clamp(plane, arcCentre, arcMax, false), TestTolerance);
				AssertToleranceEquals(expectation3D.Value.Flipped.ParallelizedWith(plane), input.Flipped.Clamp(plane, arcCentre.Flipped, arcMax, false), TestTolerance);
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
		Assert.AreEqual(Direction.Left, Direction.Left.Clamp(testPlane, Direction.Down, 45f, false));
		Assert.AreEqual(Direction.Left, Direction.Left.Clamp(testPlane, Direction.None, 45f, false));
		Assert.AreEqual(Direction.None, Direction.None.Clamp(testPlane, Direction.Left, 45f, false));
	}

	[Test]
	public void ShouldCorrectlyDetermineOrthogonalityToOtherDirectionsAndVects() {
		void AssertCombinationExactly(bool expectation, Direction d1, Direction d2) {
			var v1 = d1 * 10f;
			var v2 = d2 * 10f;

			Assert.AreEqual(expectation, d1.IsOrthogonalTo(d2));
			Assert.AreEqual(expectation, d2.IsOrthogonalTo(d1));
			Assert.AreEqual(expectation, d1.IsOrthogonalTo(-d2));
			Assert.AreEqual(expectation, d2.IsOrthogonalTo(-d1));
			Assert.AreEqual(expectation, (-d1).IsOrthogonalTo(d2));
			Assert.AreEqual(expectation, (-d2).IsOrthogonalTo(d1));
			Assert.AreEqual(expectation, (-d1).IsOrthogonalTo(-d2));
			Assert.AreEqual(expectation, (-d2).IsOrthogonalTo(-d1));

			Assert.AreEqual(expectation, d1.IsOrthogonalTo(v2));
			Assert.AreEqual(expectation, d2.IsOrthogonalTo(v1));
			Assert.AreEqual(expectation, d1.IsOrthogonalTo(-v2));
			Assert.AreEqual(expectation, d2.IsOrthogonalTo(-v1));
			Assert.AreEqual(expectation, (-d1).IsOrthogonalTo(v2));
			Assert.AreEqual(expectation, (-d2).IsOrthogonalTo(v1));
			Assert.AreEqual(expectation, (-d1).IsOrthogonalTo(-v2));
			Assert.AreEqual(expectation, (-d2).IsOrthogonalTo(-v1));
		}
		void AssertCombination(bool expectation, Direction d1, Direction d2, Angle? tolerance) {
			var v1 = d1 * 10f;
			var v2 = d2 * 10f;

			if (tolerance == null) {
				Assert.AreEqual(expectation, d1.IsApproximatelyOrthogonalTo(d2));
				Assert.AreEqual(expectation, d2.IsApproximatelyOrthogonalTo(d1));
				Assert.AreEqual(expectation, d1.IsApproximatelyOrthogonalTo(-d2));
				Assert.AreEqual(expectation, d2.IsApproximatelyOrthogonalTo(-d1));
				Assert.AreEqual(expectation, (-d1).IsApproximatelyOrthogonalTo(d2));
				Assert.AreEqual(expectation, (-d2).IsApproximatelyOrthogonalTo(d1));
				Assert.AreEqual(expectation, (-d1).IsApproximatelyOrthogonalTo(-d2));
				Assert.AreEqual(expectation, (-d2).IsApproximatelyOrthogonalTo(-d1));

				Assert.AreEqual(expectation, d1.IsApproximatelyOrthogonalTo(v2));
				Assert.AreEqual(expectation, d2.IsApproximatelyOrthogonalTo(v1));
				Assert.AreEqual(expectation, d1.IsApproximatelyOrthogonalTo(-v2));
				Assert.AreEqual(expectation, d2.IsApproximatelyOrthogonalTo(-v1));
				Assert.AreEqual(expectation, (-d1).IsApproximatelyOrthogonalTo(v2));
				Assert.AreEqual(expectation, (-d2).IsApproximatelyOrthogonalTo(v1));
				Assert.AreEqual(expectation, (-d1).IsApproximatelyOrthogonalTo(-v2));
				Assert.AreEqual(expectation, (-d2).IsApproximatelyOrthogonalTo(-v1));
			}
			else {
				Assert.AreEqual(expectation, d1.IsApproximatelyOrthogonalTo(d2, tolerance.Value));
				Assert.AreEqual(expectation, d2.IsApproximatelyOrthogonalTo(d1, tolerance.Value));
				Assert.AreEqual(expectation, d1.IsApproximatelyOrthogonalTo(-d2, tolerance.Value));
				Assert.AreEqual(expectation, d2.IsApproximatelyOrthogonalTo(-d1, tolerance.Value));
				Assert.AreEqual(expectation, (-d1).IsApproximatelyOrthogonalTo(d2, tolerance.Value));
				Assert.AreEqual(expectation, (-d2).IsApproximatelyOrthogonalTo(d1, tolerance.Value));
				Assert.AreEqual(expectation, (-d1).IsApproximatelyOrthogonalTo(-d2, tolerance.Value));
				Assert.AreEqual(expectation, (-d2).IsApproximatelyOrthogonalTo(-d1, tolerance.Value));

				Assert.AreEqual(expectation, d1.IsApproximatelyOrthogonalTo(v2, tolerance.Value));
				Assert.AreEqual(expectation, d2.IsApproximatelyOrthogonalTo(v1, tolerance.Value));
				Assert.AreEqual(expectation, d1.IsApproximatelyOrthogonalTo(-v2, tolerance.Value));
				Assert.AreEqual(expectation, d2.IsApproximatelyOrthogonalTo(-v1, tolerance.Value));
				Assert.AreEqual(expectation, (-d1).IsApproximatelyOrthogonalTo(v2, tolerance.Value));
				Assert.AreEqual(expectation, (-d2).IsApproximatelyOrthogonalTo(v1, tolerance.Value));
				Assert.AreEqual(expectation, (-d1).IsApproximatelyOrthogonalTo(-v2, tolerance.Value));
				Assert.AreEqual(expectation, (-d2).IsApproximatelyOrthogonalTo(-v1, tolerance.Value));
			}
		}

		AssertCombination(true, Direction.Left, Direction.Down, null);
		AssertCombination(true, Direction.Left, Direction.Up, null);
		AssertCombination(false, Direction.Left, Direction.Right, null);
		AssertCombination(false, Direction.Forward, Direction.Backward, null);
		AssertCombination(false, Direction.Forward, Direction.None, null);
		AssertCombination(false, Direction.None, Direction.Right, null);
		AssertCombination(false, Direction.None, Direction.None, null);

		AssertCombination(true, Direction.Left, Direction.Down, 0f);
		AssertCombination(true, Direction.Left, Direction.Up, 0f);
		AssertCombination(false, Direction.Left, Direction.Right, 89f);
		AssertCombination(false, Direction.Forward, Direction.Backward, 89f);

		AssertCombination(true, Direction.Left, Direction.Down, 90f);

		AssertCombinationExactly(false, Direction.Left, Direction.Right);
		AssertCombinationExactly(false, Direction.Left, Direction.None);

		var testList = new List<Direction>();
		for (var x = -3f; x <= 3f; x += 1f) {
			for (var y = -3f; y <= 3f; y += 1f) {
				for (var z = -3f; z <= 3f; z += 1f) {
					testList.Add(new(x, y, z));
				}
			}
		}

		for (var i = 0; i < testList.Count; ++i) {
			AssertCombinationExactly(false, testList[i], testList[i]);
			if (testList[i] != Direction.None) AssertCombinationExactly(true, testList[i], testList[i].AnyOrthogonal());
			for (var j = i; j < testList.Count; ++j) {
				var a = testList[i];
				var b = testList[j];
				if (a == Direction.None || b == Direction.None) {
					AssertCombination(false, a, b, null);
					AssertCombination(false, a, b, 0f);
					AssertCombination(false, a, b, 45f);
					AssertCombination(false, a, b, 90f);
					AssertCombination(false, a, b, 180f);
					AssertCombinationExactly(false, a, b);
					continue;
				}

				var angle = a ^ b;
				var diffToOrthogonality = (angle - Angle.QuarterCircle).Absolute;
				if (diffToOrthogonality == Angle.Zero) {
					AssertCombination(true, a, b, null);
					AssertCombination(true, a, b, 0f);
				}
				else {
					AssertCombination(true, a, b, diffToOrthogonality + 0.1f);
					AssertCombination(false, a, b, diffToOrthogonality - 0.1f);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyDetermineParallelismToOtherDirectionsAndVects() {
		void AssertCombinationExactly(bool expectation, Direction d1, Direction d2) {
			var v1 = d1 * 10f;
			var v2 = d2 * 10f;

			Assert.AreEqual(expectation, d1.IsParallelTo(d2));
			Assert.AreEqual(expectation, d2.IsParallelTo(d1));
			Assert.AreEqual(expectation, d1.IsParallelTo(-d2));
			Assert.AreEqual(expectation, d2.IsParallelTo(-d1));
			Assert.AreEqual(expectation, (-d1).IsParallelTo(d2));
			Assert.AreEqual(expectation, (-d2).IsParallelTo(d1));
			Assert.AreEqual(expectation, (-d1).IsParallelTo(-d2));
			Assert.AreEqual(expectation, (-d2).IsParallelTo(-d1));

			Assert.AreEqual(expectation, d1.IsParallelTo(v2));
			Assert.AreEqual(expectation, d2.IsParallelTo(v1));
			Assert.AreEqual(expectation, d1.IsParallelTo(-v2));
			Assert.AreEqual(expectation, d2.IsParallelTo(-v1));
			Assert.AreEqual(expectation, (-d1).IsParallelTo(v2));
			Assert.AreEqual(expectation, (-d2).IsParallelTo(v1));
			Assert.AreEqual(expectation, (-d1).IsParallelTo(-v2));
			Assert.AreEqual(expectation, (-d2).IsParallelTo(-v1));
		}
		void AssertCombination(bool expectation, Direction d1, Direction d2, Angle? tolerance) {
			var v1 = d1 * 10f;
			var v2 = d2 * 10f;

			if (tolerance == null) {
				Assert.AreEqual(expectation, d1.IsApproximatelyParallelTo(d2));
				Assert.AreEqual(expectation, d2.IsApproximatelyParallelTo(d1));
				Assert.AreEqual(expectation, d1.IsApproximatelyParallelTo(-d2));
				Assert.AreEqual(expectation, d2.IsApproximatelyParallelTo(-d1));
				Assert.AreEqual(expectation, (-d1).IsApproximatelyParallelTo(d2));
				Assert.AreEqual(expectation, (-d2).IsApproximatelyParallelTo(d1));
				Assert.AreEqual(expectation, (-d1).IsApproximatelyParallelTo(-d2));
				Assert.AreEqual(expectation, (-d2).IsApproximatelyParallelTo(-d1));

				Assert.AreEqual(expectation, d1.IsApproximatelyParallelTo(v2));
				Assert.AreEqual(expectation, d2.IsApproximatelyParallelTo(v1));
				Assert.AreEqual(expectation, d1.IsApproximatelyParallelTo(-v2));
				Assert.AreEqual(expectation, d2.IsApproximatelyParallelTo(-v1));
				Assert.AreEqual(expectation, (-d1).IsApproximatelyParallelTo(v2));
				Assert.AreEqual(expectation, (-d2).IsApproximatelyParallelTo(v1));
				Assert.AreEqual(expectation, (-d1).IsApproximatelyParallelTo(-v2));
				Assert.AreEqual(expectation, (-d2).IsApproximatelyParallelTo(-v1));
			}
			else {
				Assert.AreEqual(expectation, d1.IsApproximatelyParallelTo(d2, tolerance.Value));
				Assert.AreEqual(expectation, d2.IsApproximatelyParallelTo(d1, tolerance.Value));
				Assert.AreEqual(expectation, d1.IsApproximatelyParallelTo(-d2, tolerance.Value));
				Assert.AreEqual(expectation, d2.IsApproximatelyParallelTo(-d1, tolerance.Value));
				Assert.AreEqual(expectation, (-d1).IsApproximatelyParallelTo(d2, tolerance.Value));
				Assert.AreEqual(expectation, (-d2).IsApproximatelyParallelTo(d1, tolerance.Value));
				Assert.AreEqual(expectation, (-d1).IsApproximatelyParallelTo(-d2, tolerance.Value));
				Assert.AreEqual(expectation, (-d2).IsApproximatelyParallelTo(-d1, tolerance.Value));

				Assert.AreEqual(expectation, d1.IsApproximatelyParallelTo(v2, tolerance.Value));
				Assert.AreEqual(expectation, d2.IsApproximatelyParallelTo(v1, tolerance.Value));
				Assert.AreEqual(expectation, d1.IsApproximatelyParallelTo(-v2, tolerance.Value));
				Assert.AreEqual(expectation, d2.IsApproximatelyParallelTo(-v1, tolerance.Value));
				Assert.AreEqual(expectation, (-d1).IsApproximatelyParallelTo(v2, tolerance.Value));
				Assert.AreEqual(expectation, (-d2).IsApproximatelyParallelTo(v1, tolerance.Value));
				Assert.AreEqual(expectation, (-d1).IsApproximatelyParallelTo(-v2, tolerance.Value));
				Assert.AreEqual(expectation, (-d2).IsApproximatelyParallelTo(-v1, tolerance.Value));
			}
		}

		AssertCombination(false, Direction.Left, Direction.Down, null);
		AssertCombination(false, Direction.Left, Direction.Up, null);
		AssertCombination(true, Direction.Left, Direction.Right, null);
		AssertCombination(true, Direction.Forward, Direction.Backward, null);
		AssertCombination(true, Direction.Up, Direction.Up, null);
		AssertCombination(true, Direction.Down, Direction.Down, null);
		AssertCombination(false, Direction.Forward, Direction.None, null);
		AssertCombination(false, Direction.None, Direction.Right, null);
		AssertCombination(false, Direction.None, Direction.None, null);

		AssertCombination(false, Direction.Left, Direction.Down, 89f);
		AssertCombination(false, Direction.Left, Direction.Up, 89f);
		AssertCombination(true, Direction.Left, Direction.Down, 90f);
		AssertCombination(true, Direction.Left, Direction.Up, 90f);
		AssertCombination(true, Direction.Up, Direction.Up, 0f);
		AssertCombination(true, Direction.Down, Direction.Down, 0f);

		AssertCombinationExactly(false, Direction.Left, Direction.Down);
		AssertCombinationExactly(false, Direction.Left, Direction.None);

		var testList = new List<Direction>();
		for (var x = -3f; x <= 3f; x += 1f) {
			for (var y = -3f; y <= 3f; y += 1f) {
				for (var z = -3f; z <= 3f; z += 1f) {
					testList.Add(new(x, y, z));
				}
			}
		}

		for (var i = 0; i < testList.Count; ++i) {
			if (testList[i] != Direction.None) AssertCombinationExactly(true, testList[i], testList[i]);
			for (var j = i; j < testList.Count; ++j) {
				var a = testList[i];
				var b = testList[j];
				if (a == Direction.None || b == Direction.None) {
					AssertCombination(false, a, b, null);
					AssertCombination(false, a, b, 0f);
					AssertCombination(false, a, b, 45f);
					AssertCombination(false, a, b, 90f);
					AssertCombination(false, a, b, 180f);
					AssertCombinationExactly(false, a, b);
					continue;
				}

				var angle = (a ^ b) < (a ^ -b) ? (a ^ b) : (a ^ -b);
				if (angle == Angle.Zero) {
					AssertCombination(true, a, b, null);
					AssertCombination(true, a, b, 0f);
				}
				else {
					AssertCombination(true, a, b, angle + 0.1f);
					AssertCombination(false, a, b, angle - 0.1f);
				}
			}
		}
	}

	[Test]
	public void ExactParallelAndOrthogonalCheckFunctionsShouldIndicateSuccessOfOrthogonalizationAndParallelizationFunctions() {
		const int NumIterations = 200_000;

		for (var i = 0; i < NumIterations; ++i) {
			var dir1 = Direction.Random();
			var dir2 = Direction.Random();

			Assert.AreEqual(dir1.IsParallelTo(dir2), dir1.OrthogonalizedAgainst(dir2) == null);
			Assert.AreEqual(dir1.IsOrthogonalTo(dir2), dir1.ParallelizedWith(dir2) == null);
		}
	}
}