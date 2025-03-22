// Created on 2023-10-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[TestFixture]
class LocationTest {
	const float TestTolerance = 0.001f;
	static readonly Location OneTwoNegThree = new(1f, 2f, -3f);

	[Test]
	public void ShouldBeCorrectStructSize() => AssertStructLayout<Location>(16);

	[Test]
	public void ShouldCorrectlyInitializeStaticReadonlyMembers() {
		Assert.AreEqual(new Location(0f, 0f, 0f), Location.Origin);
	}

	[Test]
	public void ShouldCorrectlyImplementProperties() {
		Assert.AreEqual(1f, OneTwoNegThree.X);
		Assert.AreEqual(2f, OneTwoNegThree.Y);
		Assert.AreEqual(-3f, OneTwoNegThree.Z);

		Assert.AreEqual(1.5f, (OneTwoNegThree with { X = 1.5f }).X);
		Assert.AreEqual(2.5f, (OneTwoNegThree with { Y = 2.5f }).Y);
		Assert.AreEqual(-3.5f, (OneTwoNegThree with { Z = -3.5f }).Z);

		Assert.AreEqual(new Location(4f, 5f, -6f), OneTwoNegThree with { X = 4f, Y = 5f, Z = -6f });
	}

	[Test]
	public void ConstructorsShouldCorrectlyConstruct() {
		Assert.AreEqual(Location.Origin, new Location());
		Assert.AreEqual(Location.WValue, new Location().AsVector4.W);

		Assert.AreEqual(new Location(new Vector4(0.1f, 0.2f, 0.3f, Location.WValue)), new Location(0.1f, 0.2f, 0.3f));
		Assert.AreEqual(Location.WValue, new Location(0.1f, 0.2f, 0.3f).AsVector4.W);
	}

	[Test]
	public void StaticFactoryMethodsShouldCorrectlyConstruct() {
		Assert.AreEqual(new Location(-1.2f, 2.4f, 0f), Location.FromVector3(new(-1.2f, 2.4f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyConvertToVector3() {
		Assert.AreEqual(new Vector3(1f, 2f, -3f), OneTwoNegThree.ToVector3());
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromValueTuple() {
		Assert.AreEqual(OneTwoNegThree, (Location) (1, 2, -3));
		var (x, y, z) = OneTwoNegThree;
		Assert.AreEqual(1f, x);
		Assert.AreEqual(2f, y);
		Assert.AreEqual(-3f, z);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength(OneTwoNegThree);
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(Location.Origin, OneTwoNegThree, new(-0.001f, 0f, 100000f));
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(Location.Origin, 0f, 0f, 0f);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(OneTwoNegThree, 1f, 2f, -3f);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(new Location(-0.001f, 0f, 100000f), -0.001f, 0f, 100000f);
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		void AssertIteration(Location input, string expectedValue) {
			var testCulture = CultureInfo.InvariantCulture;
			var testFormat = "N1";

			Span<char> formatSpan = stackalloc char[expectedValue.Length];
			Assert.IsTrue(input.TryFormat(formatSpan, out var charsWritten, testFormat, testCulture));
			Assert.AreEqual(formatSpan.Length, charsWritten);

			Assert.AreEqual(expectedValue, input.ToString(testFormat, testCulture));
			Assert.AreEqual(expectedValue, new String(formatSpan));
		}

		AssertIteration(Location.Origin, "<0.0, 0.0, 0.0>");
		AssertIteration(OneTwoNegThree, "<1.0, 2.0, -3.0>");
		AssertIteration(new Location(0.5f, 0f, -1.6f), "<0.5, 0.0, -1.6>");
	}

	[Test]
	public void ShouldCorrectlyFormatToString() {
		void AssertFail(Location input, Span<char> destination, ReadOnlySpan<char> format, IFormatProvider? provider) {
			Assert.AreEqual(false, input.TryFormat(destination, out _, format, provider));
		}

		void AssertSuccess(
			Location input,
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

		var fractionalVect = new Location(1.211f, 3.422f, -5.633f);

		AssertFail(Location.Origin, Array.Empty<char>(), "N0", null);
		AssertFail(Location.Origin, new char[8], "N0", null);
		AssertSuccess(Location.Origin, new char[9], "N0", null, "<0, 0, 0>");
		AssertFail(fractionalVect, new char[8], "N0", null);
		AssertSuccess(fractionalVect, new char[10], "N0", null, "<1, 3, -6>");
		AssertFail(fractionalVect, new char[10], "N1", null);
		AssertSuccess(fractionalVect, new char[16], "N1", null, "<1.2, 3.4, -5.6>");
		AssertSuccess(fractionalVect, new char[16], "N1", CultureInfo.CreateSpecificCulture("de-DE"), "<1,2. 3,4. -5,6>");
		AssertSuccess(fractionalVect, new char[22], "N3", null, "<1.211, 3.422, -5.633>");
	}

	[Test]
	public void ShouldCorrectlyParseFromString() {
		var testCulture = CultureInfo.InvariantCulture;

		void AssertSuccess(string input, Location expectedResult) {
			AssertToleranceEquals(expectedResult, Location.Parse(input, testCulture), TestTolerance);
			AssertToleranceEquals(expectedResult, Location.Parse(input.AsSpan(), testCulture), TestTolerance);
			Assert.IsTrue(Location.TryParse(input, testCulture, out var parseResult));
			AssertToleranceEquals(expectedResult, parseResult, TestTolerance);
			Assert.IsTrue(Location.TryParse(input.AsSpan(), testCulture, out parseResult));
			AssertToleranceEquals(expectedResult, parseResult, TestTolerance);
		}

		void AssertFail(string input) {
			Assert.Catch(() => Location.Parse(input, testCulture));
			Assert.Catch(() => Location.Parse(input.AsSpan(), testCulture));
			Assert.False(Location.TryParse(input, testCulture, out _));
			Assert.False(Location.TryParse(input.AsSpan(), testCulture, out _));
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
		Assert.AreEqual(Location.Origin, new Location(-0f, -0f, -0f));
		Assert.AreNotEqual(Location.Origin, OneTwoNegThree);
		Assert.IsTrue(OneTwoNegThree.Equals(OneTwoNegThree));
		Assert.IsFalse(OneTwoNegThree.Equals(Location.Origin));
		Assert.IsTrue(OneTwoNegThree == new Location(1f, 2f, -3f));
		Assert.IsFalse(Location.Origin == OneTwoNegThree);
		Assert.IsFalse(Location.Origin != new Location(0f, 0f, 0f));
		Assert.IsTrue(OneTwoNegThree != Location.Origin);
		Assert.IsTrue(new Location(1f, 2f, 3f) != new Location(0f, 2f, 3f));
		Assert.IsTrue(new Location(1f, 2f, 3f) != new Location(1f, 0f, 3f));
		Assert.IsTrue(new Location(1f, 2f, 3f) != new Location(1f, 2f, 0f));

		Assert.IsTrue(Location.Origin.Equals(Location.Origin, 0f));
		Assert.IsTrue(OneTwoNegThree.Equals(OneTwoNegThree, 0f));
		Assert.IsTrue(new Location(0.5f, 0.6f, 0.7f).Equals(new Location(0.4f, 0.5f, 0.6f), 0.11f));
		Assert.IsFalse(new Location(0.5f, 0.6f, 0.7f).Equals(new Location(0.4f, 0.5f, 0.6f), 0.09f));
		Assert.IsTrue(new Location(-0.5f, -0.5f, -0.5f).Equals(new Location(-0.4f, -0.4f, -0.4f), 0.11f));
		Assert.IsFalse(new Location(-0.5f, -0.5f, -0.5f).Equals(new Location(-0.4f, -0.4f, -0.4f), 0.09f));
		Assert.IsFalse(new Location(-0.5f, -0.5f, -0.5f).Equals(new Location(0.4f, -0.4f, -0.4f), 0.11f));
	}

	[Test]
	public void ShouldCorrectlyImplementEqualityWithDistanceTolerance() {
		Assert.AreEqual(true, OneTwoNegThree.IsWithinDistanceOf(OneTwoNegThree, 0f));
		Assert.AreEqual(true, OneTwoNegThree.IsWithinDistanceOf(new(1f, 2f, -2f), 1f + TestTolerance));
		Assert.AreEqual(false, OneTwoNegThree.IsWithinDistanceOf(new(1f, 2f, -1.5f), 1f + TestTolerance));
		Assert.AreEqual(true, Location.Origin.IsWithinDistanceOf(new(1f, 1f, 0f), 1.42f));
		Assert.AreEqual(false, Location.Origin.IsWithinDistanceOf(new(1f, 1f, 0f), 1.40f));
	}

	[Test]
	public void ShouldCorrectlyCombineWithVect() {
		void AssertCombination(Location loc, Vect vec, Location expectedAdditiveResult, Location expectedSubtractiveResult) {
			AssertToleranceEquals(expectedAdditiveResult, loc + vec, TestTolerance);
			AssertToleranceEquals(loc + vec, vec + loc, TestTolerance);
			AssertToleranceEquals(vec + loc, loc.MovedBy(vec), TestTolerance);
			AssertToleranceEquals(expectedSubtractiveResult, loc - vec, TestTolerance);
			AssertToleranceEquals(loc - vec, loc.MovedBy(-vec), TestTolerance);
		}

		AssertCombination(Location.Origin, Vect.Zero, Location.Origin, Location.Origin);
		AssertCombination(Location.Origin, new(1f, -2f, 3.5f), new(1f, -2f, 3.5f), new(-1f, 2f, -3.5f));
		AssertCombination(OneTwoNegThree, Vect.Zero, OneTwoNegThree, OneTwoNegThree);
		AssertCombination(OneTwoNegThree, new(0.1f, -0.2f, 0.3f), new(1.1f, 1.8f, -2.7f), new(0.9f, 2.2f, -3.3f));
	}

	[Test]
	public void ShouldCorrectlyCreateVectsBetweenLocations() {
		void AssertCombination(Location startPoint, Location endPoint, Vect fromStartToEndExpectation) {
			AssertToleranceEquals(fromStartToEndExpectation, startPoint >> endPoint, TestTolerance);
			AssertToleranceEquals(-fromStartToEndExpectation, startPoint << endPoint, TestTolerance);
			AssertToleranceEquals(-(startPoint >> endPoint), endPoint >> startPoint, TestTolerance);
			AssertToleranceEquals(-(endPoint >> startPoint), startPoint >> endPoint, TestTolerance);
			AssertToleranceEquals(fromStartToEndExpectation, endPoint - startPoint, TestTolerance);
			AssertToleranceEquals(-fromStartToEndExpectation, startPoint - endPoint, TestTolerance);
			AssertToleranceEquals(fromStartToEndExpectation, startPoint.VectTo(endPoint), TestTolerance);
			AssertToleranceEquals(-fromStartToEndExpectation, endPoint.VectTo(startPoint), TestTolerance);
			AssertToleranceEquals(-fromStartToEndExpectation, startPoint.VectFrom(endPoint), TestTolerance);
			AssertToleranceEquals(fromStartToEndExpectation, endPoint.VectFrom(startPoint), TestTolerance);
		}

		AssertCombination(Location.Origin, Location.Origin, Vect.Zero);
		AssertCombination(Location.Origin, OneTwoNegThree, new(1f, 2f, -3f));
		AssertCombination(OneTwoNegThree, Location.Origin, new(-1f, -2f, 3f));
		AssertCombination(new(0.5f, -14f, 7.6f), new(9.2f, 17f, -0.1f), new(8.7f, 31f, -7.7f));
	}

	[Test]
	public void ShouldCorrectlyReturnDirectionBetweenLocations() {
		void AssertCombination(Location startPoint, Location endPoint, Direction fromStartToEndExpectation) {
			AssertToleranceEquals(fromStartToEndExpectation, startPoint.DirectionTo(endPoint), TestTolerance);
			AssertToleranceEquals(-fromStartToEndExpectation, startPoint.DirectionFrom(endPoint), TestTolerance);
		}

		AssertCombination(Location.Origin, Location.Origin, Direction.None);
		AssertCombination(Location.Origin, OneTwoNegThree, new(1f, 2f, -3f));
		AssertCombination(OneTwoNegThree, Location.Origin, new(-1f, -2f, 3f));
		AssertCombination(new(0.5f, -14f, 7.6f), new(9.2f, 17f, -0.1f), new(8.7f, 31f, -7.7f));

		Assert.AreEqual(OneTwoNegThree.DistanceFrom(Location.Origin), OneTwoNegThree.DistanceFromOrigin());
		Assert.AreEqual(OneTwoNegThree.DistanceSquaredFrom(Location.Origin), OneTwoNegThree.DistanceSquaredFromOrigin());
	}

	[Test]
	public void ShouldCorrectlyRotateAroundPoints() {
		void AssertCombination(Location expectation, Location startPoint, Location pivotPoint, Rotation rotation) {
			AssertToleranceEquals(expectation, startPoint.RotatedBy(rotation, pivotPoint), TestTolerance);
			Assert.AreEqual(startPoint.RotatedBy(rotation, pivotPoint), startPoint * (pivotPoint, rotation));
			Assert.AreEqual(startPoint.RotatedBy(rotation, pivotPoint), startPoint * (rotation, pivotPoint));
			Assert.AreEqual(startPoint.RotatedBy(rotation, pivotPoint), (pivotPoint, rotation) * startPoint);
			Assert.AreEqual(startPoint.RotatedBy(rotation, pivotPoint), (rotation, pivotPoint) * startPoint);
		}

		AssertCombination((0f, 0f, 10f), (0f, 0f, 0f), (0f, 0f, 5f), Direction.Down % 180f);
		AssertCombination((-10f, 0f, 0f), (0f, 10f, 0f), (0f, 0f, -10f), Direction.Forward % 90f);

		AssertToleranceEquals(
			OneTwoNegThree.RotatedBy(16f % Direction.Right, Location.Origin),
			OneTwoNegThree.RotatedAroundOriginBy(16f % Direction.Right),
			TestTolerance
		);
		AssertToleranceEquals(
			OneTwoNegThree.RotatedBy(-37f % Direction.Up, Location.Origin),
			OneTwoNegThree.RotatedAroundOriginBy(-37f % Direction.Up),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyScale() {
		AssertToleranceEquals(
			new Location(2f, 4f, -6f),
			OneTwoNegThree.ScaledFromOriginBy(2f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(-0.5f, -1f, 1.5f),
			OneTwoNegThree.ScaledFromOriginBy(-0.5f),
			TestTolerance
		);

		AssertToleranceEquals(
			new Location(2f, -1f, -3f),
			OneTwoNegThree.ScaledFromOriginBy((2f, -0.5f, 1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(-1f, 4f, -1.5f),
			OneTwoNegThree.ScaledFromOriginBy((-1f, 2f, 0.5f)),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyTransform() {
		var transform = new Transform(
			(1f, 2f, 3f),
			40f % Direction.Right,
			(0.5f, 1.1f, -0.3f)
		);

		AssertToleranceEquals(
			OneTwoNegThree.ScaledFromOriginBy(transform.Scaling).RotatedAroundOriginBy(transform.Rotation) + transform.Translation,
			OneTwoNegThree.TransformedAroundOriginBy(transform),
			TestTolerance
		);
		AssertToleranceEquals(
			OneTwoNegThree.TransformedAroundOriginBy(transform),
			OneTwoNegThree.TransformedBy(transform, Location.Origin),
			TestTolerance
		);

		var transformOrigin = new Location(-1f, 0f, 2f);
		AssertToleranceEquals(
			((transformOrigin >> OneTwoNegThree).ScaledBy(transform.Scaling) + transformOrigin).RotatedBy(transform.Rotation, transformOrigin) + transform.Translation,
			OneTwoNegThree.TransformedBy(transform, transformOrigin),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		AssertToleranceEquals(OneTwoNegThree, Location.Interpolate(OneTwoNegThree, Location.Origin, 0f), TestTolerance);
		AssertToleranceEquals(Location.Origin, Location.Interpolate(OneTwoNegThree, Location.Origin, 1f), TestTolerance);
		AssertToleranceEquals(Location.FromVector3(OneTwoNegThree.ToVector3() * 0.5f), Location.Interpolate(OneTwoNegThree, Location.Origin, 0.5f), TestTolerance);
		AssertToleranceEquals(Location.FromVector3(OneTwoNegThree.ToVector3() * 2f), Location.Interpolate(OneTwoNegThree, Location.Origin, -1f), TestTolerance);
		AssertToleranceEquals(Location.FromVector3(OneTwoNegThree.ToVector3() * -1f), Location.Interpolate(OneTwoNegThree, Location.Origin, 2f), TestTolerance);

		var testList = new List<Location>();
		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					testList.Add(new(x, y, z));
				}
			}
		}
		for (var i = 0; i < testList.Count; ++i) {
			for (var j = i; j < testList.Count; ++j) {
				var start = testList[i];
				var end = testList[j];

				for (var f = -1f; f <= 2f; f += 0.1f) {
					AssertToleranceEquals(new(Single.Lerp(start.X, end.X, f), Single.Lerp(start.Y, end.Y, f), Single.Lerp(start.Z, end.Z, f)), Location.Interpolate(start, end, f), TestTolerance);
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyCreateNonBoundedRandomValues() {
		const int NumIterations = 50_000;

		for (var i = 0; i < NumIterations; ++i) {
			var val = Location.Random();
			Assert.GreaterOrEqual(val.X, -Location.DefaultRandomRange);
			Assert.LessOrEqual(val.X, Location.DefaultRandomRange);
			Assert.GreaterOrEqual(val.Y, -Location.DefaultRandomRange);
			Assert.LessOrEqual(val.Y, Location.DefaultRandomRange);
			Assert.GreaterOrEqual(val.Z, -Location.DefaultRandomRange);
			Assert.LessOrEqual(val.Z, Location.DefaultRandomRange);
		}
	}

	[Test]
	public void ShouldCorrectlyCreateBoundedRandomValues() {
		const int NumIterations = 50_000;

		for (var i = 0; i < NumIterations; ++i) {
			var start = Location.Random();
			var end = Location.Random();
			var val = Location.Random(start, end);

			var startToEnd = start >> end;
			var startToVal = start >> val;
			var valToEnd = val >> end;

			Assert.IsTrue(startToEnd.Direction.IsWithinAngleTo(startToVal.Direction, 5f));
			Assert.IsTrue(startToEnd.Direction.IsWithinAngleTo(valToEnd.Direction, 5f));
			Assert.LessOrEqual(startToVal.LengthSquared, startToEnd.LengthSquared);
			Assert.LessOrEqual(valToEnd.LengthSquared, startToEnd.LengthSquared);
		}
	}

	[Test]
	public void ShouldCorrectlyCalculateDistanceBetweenLocations() {
		void AssertCombination(Location startPoint, Location endPoint, float expectedDistance) {
			Assert.AreEqual(expectedDistance, startPoint.DistanceFrom(endPoint), TestTolerance);
			Assert.AreEqual(expectedDistance, endPoint.DistanceFrom(startPoint), TestTolerance);
			Assert.AreEqual(expectedDistance * expectedDistance, startPoint.DistanceSquaredFrom(endPoint), TestTolerance);
			Assert.AreEqual(expectedDistance * expectedDistance, endPoint.DistanceSquaredFrom(startPoint), TestTolerance);
		}

		AssertCombination(Location.Origin, Location.Origin, 0f);
		AssertCombination(Location.Origin, OneTwoNegThree, new Vect(1f, 2f, -3f).Length);
		AssertCombination(OneTwoNegThree, Location.Origin, new Vect(-1f, -2f, 3f).Length);
		AssertCombination(new(0.5f, -14f, 7.6f), new(9.2f, 17f, -0.1f), new Vect(8.7f, 31f, -7.7f).Length);
	}

	[Test]
	public void ShouldCorrectlyClamp() {
		var min = new Location(-3f, 1f, 3f);
		var max = new Location(3f, -1f, -3f);

		AssertToleranceEquals(
			new Location(0f, 0f, 0f),
			new Location(0f, 0f, 0f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(-3f, 1f, 3f),
			new Location(-3f, 1f, 3f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(3f, -1f, -3f),
			new Location(3f, -1f, -3f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(-3f, 1f, 3f),
			new Location(-4f, 2f, 4f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(3f, -1f, -3f),
			new Location(4f, -2f, -4f).Clamp(min, max),
			TestTolerance
		);


		AssertToleranceEquals(
			new Location(-0.158f, 0.0526f, 0.158f),
			new Location(0f, 1f, 0f).Clamp(min, max),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(0.158f, -0.0526f, -0.158f),
			new Location(0f, -1f, 0f).Clamp(min, max),
			TestTolerance
		);
	}
}