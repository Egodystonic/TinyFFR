// Created on 2024-03-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class BoundedLineTest {
	const float TestTolerance = 0.001f;
	static readonly BoundedLine TestLine = new(new Location(1f, 2f, -3f), new Location(-1f, -2f, 3f));

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldBeCorrectStructSize() => AssertStructLayout<BoundedLine>();

	[Test]
	public void ShouldCorrectlyCalculateProperties() {
		Assert.AreEqual(new Location(1f, 2f, -3f), TestLine.StartPoint);
		Assert.AreEqual(new Direction(-1f, -2f, 3f), TestLine.Direction);
		Assert.AreEqual(MathF.Sqrt(4f + 16f + 36f), TestLine.Length, TestTolerance);
		Assert.AreEqual(4f + 16f + 36f, TestLine.LengthSquared, TestTolerance);
		Assert.AreEqual(new Vect(-2f, -4f, 6f), TestLine.StartToEndVect);
		Assert.AreEqual(new Location(-1f, -2f, 3f), TestLine.EndPoint);
		Assert.AreEqual(false, ((ILine) TestLine).IsUnboundedInBothDirections);
		Assert.AreEqual(TestLine.Length, ((ILine) TestLine).Length);
		Assert.AreEqual(TestLine.LengthSquared, ((ILine) TestLine).LengthSquared);
		Assert.AreEqual(TestLine.StartToEndVect, ((ILine) TestLine).StartToEndVect);
		Assert.AreEqual(TestLine.EndPoint, ((ILine) TestLine).EndPoint);
	}

	// TODO this test could be fleshed out a lot more
	[Test]
	public void ShouldCorrectlyConvertToString() {
		const string Expectation = "BoundedLine[StartPoint <1.0, 2.0, -3.0> | EndPoint <-1.0, -2.0, 3.0>]";
		Assert.AreEqual(Expectation, TestLine.ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		TestLine.TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture);
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
	}

	// TODO this test could be fleshed out a lot more
	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = "BoundedLine[StartPoint <1.0, 2.0, -3.0> | EndPoint <-1.0, -2.0, 3.0>]";
		Assert.AreEqual(TestLine, BoundedLine.Parse(Input, CultureInfo.InvariantCulture));
		Assert.AreEqual(true, BoundedLine.TryParse(Input, CultureInfo.InvariantCulture, out var result));
		Assert.AreEqual(TestLine, result);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength(TestLine);
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(TestLine);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(TestLine, TestLine.StartPoint.X, TestLine.StartPoint.Y, TestLine.StartPoint.Z, TestLine.EndPoint.X, TestLine.EndPoint.Y, TestLine.EndPoint.Z);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		var start = new BoundedLine(new Location(0f, 5f, 0f), new Location(5f, 0f, 0f));
		var end = new BoundedLine(new Location(0f, 0f, 5f), new Location(0f, 0f, -5f));
		var startPointVec = start.StartPoint >> end.StartPoint;
		var endPointVec = start.EndPoint >> end.EndPoint;

		Assert.AreEqual(new BoundedLine(start.StartPoint + startPointVec * -0.5f, start.EndPoint + (endPointVec * -0.5f)), BoundedLine.Interpolate(start, end, -0.5f));
		Assert.AreEqual(new BoundedLine(start.StartPoint + startPointVec * 0.5f, start.EndPoint + (endPointVec * 0.5f)), BoundedLine.Interpolate(start, end, 0.5f));
		Assert.AreEqual(new BoundedLine(start.StartPoint + startPointVec * 1.5f, start.EndPoint + (endPointVec * 1.5f)), BoundedLine.Interpolate(start, end, 1.5f));
		Assert.AreEqual(new BoundedLine(start.StartPoint, start.EndPoint), BoundedLine.Interpolate(start, end, 0f));
		Assert.AreEqual(new BoundedLine(end.StartPoint, end.EndPoint), BoundedLine.Interpolate(start, end, 1f));
	}

	[Test]
	public void ShouldCorrectlyCreateRandomObjects() {
		const int NumIterations = 10_000;

		var start = new BoundedLine(new Location(5f, 5f, 5f), new Location(0f, 0f, 0f));
		var end = new BoundedLine(new Location(15f, 15f, 15f), new Location(1f, 1f, 1f));

		for (var i = 0; i < NumIterations; ++i) {
			var val = BoundedLine.CreateNewRandom(start, end);
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
			new BoundedLine(new Location(100f, 0f, 0f), new Location(0f, 0f, 0f)),
			BoundedLine.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(-100f, 0f, 0f))
		);

		AssertToleranceEquals(
			new BoundedLine(new Location(100f, 0f, 0f), new Location(99.9f, 0f, 0f)),
			new BoundedLine(new Location(100f, 0f, 0.1f), new Location(100f, 0f, 0f)),
			0.2f
		);

		AssertToleranceNotEquals(
			new BoundedLine(new Location(100f, 0f, 0f), new Location(99.9f, 0f, 0f)),
			new BoundedLine(new Location(100f, 0f, 0.1f), new Location(100f, 0f, 0f)),
			0.05f
		);

		AssertToleranceEquals(
			BoundedLine.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(99.9f, 0f, 0f)),
			BoundedLine.FromStartPointAndVect(new Location(100f, 0f, 0.1f), new Vect(100f, 0f, 0f)),
			0.2f
		);

		AssertToleranceNotEquals(
			BoundedLine.FromStartPointAndVect(new Location(100f, 0f, 0f), new Vect(99.9f, 0f, 0f)),
			BoundedLine.FromStartPointAndVect(new Location(100f, 0f, 0.1f), new Vect(100f, 0f, 0f)),
			0.05f
		);

		Assert.AreEqual(
			true,
			TestLine.EqualsDisregardingDirection(new BoundedLine(TestLine.EndPoint, TestLine.StartPoint))
		);
		Assert.AreEqual(
			false,
			TestLine.EqualsDisregardingDirection(new BoundedLine(TestLine.EndPoint + (0.1f, 0f, 0f), TestLine.StartPoint + (-0.1f, 0f, 0f)))
		);
		Assert.AreEqual(
			true,
			TestLine.EqualsDisregardingDirection(new BoundedLine(TestLine.EndPoint + (0.1f, 0f, 0f), TestLine.StartPoint + (-0.1f, 0f, 0f)), 0.2f)
		);
		Assert.AreEqual(
			false,
			TestLine.EqualsDisregardingDirection(new BoundedLine(TestLine.EndPoint + (0.1f, 0f, 0f), TestLine.StartPoint + (-0.1f, 0f, 0f)), 0.05f)
		);
	}
}