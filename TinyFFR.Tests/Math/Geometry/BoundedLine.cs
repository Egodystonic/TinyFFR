// Created on 2024-03-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class BoundedRayTest {
	const float TestTolerance = 0.001f;
	static readonly BoundedRay TestRay = new(new Location(1f, 2f, -3f), new Location(-1f, -2f, 3f));

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldBeCorrectStructSize() => AssertStructLayout<BoundedRay>();

	[Test]
	public void ShouldCorrectlyCalculateProperties() {
		Assert.AreEqual(new Location(1f, 2f, -3f), TestRay.StartPoint);
		Assert.AreEqual(new Direction(-1f, -2f, 3f), TestRay.Direction);
		Assert.AreEqual(MathF.Sqrt(4f + 16f + 36f), TestRay.Length, TestTolerance);
		Assert.AreEqual(4f + 16f + 36f, TestRay.LengthSquared, TestTolerance);
		Assert.AreEqual(new Vect(-2f, -4f, 6f), TestRay.StartToEndVect);
		Assert.AreEqual(new Location(-1f, -2f, 3f), TestRay.EndPoint);
		Assert.AreEqual(false, ((ILineLike) TestRay).IsUnboundedInBothDirections);
		Assert.AreEqual(TestRay.Length, ((ILineLike) TestRay).Length);
		Assert.AreEqual(TestRay.LengthSquared, ((ILineLike) TestRay).LengthSquared);
		Assert.AreEqual(TestRay.StartToEndVect, ((ILineLike) TestRay).StartToEndVect);
		Assert.AreEqual(TestRay.EndPoint, ((ILineLike) TestRay).EndPoint);
	}

	// TODO this test could be fleshed out a lot more
	[Test]
	public void ShouldCorrectlyConvertToString() {
		const string Expectation = "BoundedRay[StartPoint <1.0, 2.0, -3.0> | EndPoint <-1.0, -2.0, 3.0>]";
		Assert.AreEqual(Expectation, TestRay.ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		TestRay.TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture);
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
	}

	// TODO this test could be fleshed out a lot more
	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = "BoundedRay[StartPoint <1.0, 2.0, -3.0> | EndPoint <-1.0, -2.0, 3.0>]";
		Assert.AreEqual(TestRay, BoundedRay.Parse(Input, CultureInfo.InvariantCulture));
		Assert.AreEqual(true, BoundedRay.TryParse(Input, CultureInfo.InvariantCulture, out var result));
		Assert.AreEqual(TestRay, result);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength(TestRay);
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(TestRay);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(TestRay, TestRay.StartPoint.X, TestRay.StartPoint.Y, TestRay.StartPoint.Z, TestRay.EndPoint.X, TestRay.EndPoint.Y, TestRay.EndPoint.Z);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		var start = new BoundedRay(new Location(0f, 5f, 0f), new Location(5f, 0f, 0f));
		var end = new BoundedRay(new Location(0f, 0f, 5f), new Location(0f, 0f, -5f));
		var startPointVec = start.StartPoint >> end.StartPoint;
		var endPointVec = start.EndPoint >> end.EndPoint;

		Assert.AreEqual(new BoundedRay(start.StartPoint + startPointVec * -0.5f, start.EndPoint + (endPointVec * -0.5f)), BoundedRay.Interpolate(start, end, -0.5f));
		Assert.AreEqual(new BoundedRay(start.StartPoint + startPointVec * 0.5f, start.EndPoint + (endPointVec * 0.5f)), BoundedRay.Interpolate(start, end, 0.5f));
		Assert.AreEqual(new BoundedRay(start.StartPoint + startPointVec * 1.5f, start.EndPoint + (endPointVec * 1.5f)), BoundedRay.Interpolate(start, end, 1.5f));
		Assert.AreEqual(new BoundedRay(start.StartPoint, start.EndPoint), BoundedRay.Interpolate(start, end, 0f));
		Assert.AreEqual(new BoundedRay(end.StartPoint, end.EndPoint), BoundedRay.Interpolate(start, end, 1f));
	}

	[Test]
	public void ShouldCorrectlyCreateRandomObjects() {
		const int NumIterations = 10_000;

		var start = new BoundedRay(new Location(5f, 5f, 5f), new Location(0f, 0f, 0f));
		var end = new BoundedRay(new Location(15f, 15f, 15f), new Location(1f, 1f, 1f));

		for (var i = 0; i < NumIterations; ++i) {
			var val = BoundedRay.CreateNewRandom(start, end);
			Assert.GreaterOrEqual(val.StartPoint.X, start.StartPoint.X);
			Assert.GreaterOrEqual(val.StartPoint.Y, start.StartPoint.Y);
			Assert.GreaterOrEqual(val.StartPoint.Z, start.StartPoint.Z);
			Assert.Less(val.StartPoint.X, end.StartPoint.X);
			Assert.Less(val.StartPoint.Y, end.StartPoint.Y);
			Assert.Less(val.StartPoint.Z, end.StartPoint.Z);

			Assert.GreaterOrEqual(val.EndPoint.X, start.EndPoint.X);
			Assert.GreaterOrEqual(val.EndPoint.Y, start.EndPoint.Y);
			Assert.GreaterOrEqual(val.EndPoint.Z, start.EndPoint.Z);
			Assert.Less(val.EndPoint.X, end.EndPoint.X);
			Assert.Less(val.EndPoint.Y, end.EndPoint.Y);
			Assert.Less(val.EndPoint.Z, end.EndPoint.Z);
		}
	}

	[Test]
	public void ShouldCorrectlyImplementEquality() {
		Assert.AreEqual(
			new BoundedRay(new Location(100f, 0f, 0f), new Location(0f, 0f, 0f)),
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(-100f, 0f, 0f))
		);

		AssertToleranceEquals(
			new BoundedRay(new Location(100f, 0f, 0f), new Location(99.9f, 0f, 0f)),
			new BoundedRay(new Location(100f, 0f, 0.1f), new Location(100f, 0f, 0f)),
			0.2f
		);

		AssertToleranceNotEquals(
			new BoundedRay(new Location(100f, 0f, 0f), new Location(99.9f, 0f, 0f)),
			new BoundedRay(new Location(100f, 0f, 0.1f), new Location(100f, 0f, 0f)),
			0.05f
		);

		AssertToleranceEquals(
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(99.9f, 0f, 0f)),
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0.1f), new Vect(100f, 0f, 0f)),
			0.2f
		);

		AssertToleranceNotEquals(
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(99.9f, 0f, 0f)),
			BoundedRay.FromStartPointAndVect(new Location(100f, 0f, 0.1f), new Vect(100f, 0f, 0f)),
			0.05f
		);

		Assert.AreEqual(
			true,
			TestRay.EqualsDisregardingDirection(new BoundedRay(TestRay.EndPoint, TestRay.StartPoint))
		);
		Assert.AreEqual(
			false,
			TestRay.EqualsDisregardingDirection(new BoundedRay(TestRay.EndPoint + (0.1f, 0f, 0f), TestRay.StartPoint + (-0.1f, 0f, 0f)))
		);
		Assert.AreEqual(
			true,
			TestRay.EqualsDisregardingDirection(new BoundedRay(TestRay.EndPoint + (0.1f, 0f, 0f), TestRay.StartPoint + (-0.1f, 0f, 0f)), 0.2f)
		);
		Assert.AreEqual(
			false,
			TestRay.EqualsDisregardingDirection(new BoundedRay(TestRay.EndPoint + (0.1f, 0f, 0f), TestRay.StartPoint + (-0.1f, 0f, 0f)), 0.05f)
		);
	}
}