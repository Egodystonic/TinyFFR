// Created on 2023-10-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class DirectionTest {
	const float TestTolerance = 0.001f;
	static readonly Direction OneTwoNegThree = new(1f, 2f, -3f);
	static readonly Vector3 NormalizedV3 = Vector3.Normalize(new(1f, 2f, -3f));

	[Test]
	public void ShouldCorrectlyInitializeStaticReadonlyMembers() {
		Assert.AreEqual(new Direction(0f, 0f, 0f), Direction.None);
		Assert.AreEqual(new Direction(0f, 0f, 1f), Direction.Forward);
		Assert.AreEqual(new Direction(0f, 0f, -1f), Direction.Backward);
		Assert.AreEqual(new Direction(0f, 1f, 0f), Direction.Up);
		Assert.AreEqual(new Direction(0f, -1f, 0f), Direction.Down);
		Assert.AreEqual(new Direction(1f, 0f, 0f), Direction.Left);
		Assert.AreEqual(new Direction(-1f, 0f, 0f), Direction.Right);

		Assert.IsTrue(Direction.CardinalMap.Contains(Direction.Backward));
		Assert.IsTrue(Direction.CardinalMap.Contains(Direction.Forward));
		Assert.IsTrue(Direction.CardinalMap.Contains(Direction.Left));
		Assert.IsTrue(Direction.CardinalMap.Contains(Direction.Right));
		Assert.IsTrue(Direction.CardinalMap.Contains(Direction.Up));
		Assert.IsTrue(Direction.CardinalMap.Contains(Direction.Down));
		Assert.AreEqual(6, Direction.CardinalMap.Count);
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

		Assert.AreEqual(new Direction(new Vector4(0.1f, 0.2f, 0.3f, Direction.WValue)), Direction.FromPreNormalizedComponents(0.1f, 0.2f, 0.3f));
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

		var prenormDirA = Direction.FromPreNormalizedComponents(7f, -1.2f, 0f);
		var prenormDirB = Direction.FromPreNormalizedComponents(new(0f, 0.707f, -0.707f));
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
	public void ShouldCorrectlyConvertToAndFromValueTuple() {
		Assert.AreEqual(OneTwoNegThree, (Direction) (1, 2, -3));
		var (x, y, z) = OneTwoNegThree;
		Assert.AreEqual(NormalizedV3.X, x);
		Assert.AreEqual(NormalizedV3.Y, y);
		Assert.AreEqual(NormalizedV3.Z, z);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		void AssertIteration(Direction input) {
			var span = Direction.ConvertToSpan(input);
			Assert.AreEqual(3, span.Length);
			Assert.AreEqual(input.X, span[0]);
			Assert.AreEqual(input.Y, span[1]);
			Assert.AreEqual(input.Z, span[2]);
			Assert.AreEqual(input, Direction.ConvertFromSpan(span));
		}

		AssertIteration(Direction.None);
		AssertIteration(OneTwoNegThree);
		AssertIteration(new Direction(-0.001f, 0f, 100000f));
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
		var perpVec = OneTwoNegThree.GetAnyPerpendicularDirection();

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
}