// Created on 2024-03-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class LineTest {
	const float TestTolerance = 0.001f;
	static readonly Line TestLine = Line.FromTwoPoints(new Location(1f, 2f, -3f), new Location(-1f, -2f, 3f));
	static readonly Direction TestLineDirection = new(-1f - 1f, -2f - 2f, 3f - -3f);

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldBeCorrectStructSize() => AssertStructLayout<Line>();

	[Test]
	public void ShouldCorrectlyCalculateProperties() {
		Assert.AreEqual(new Location(1f, 2f, -3f), TestLine.PointOnLine);
		Assert.AreEqual(new Direction(-1f - 1f, -2f - 2f, 3f - -3f), TestLine.Direction);
		Assert.AreEqual(TestLine.PointOnLine, ((ILineLike) TestLine).StartPoint);
		Assert.AreEqual(true, ((ILineLike) TestLine).IsUnboundedInBothDirections);
		Assert.AreEqual(null, ((ILineLike) TestLine).Length);
		Assert.AreEqual(null, ((ILineLike) TestLine).LengthSquared);
		Assert.AreEqual(null, ((ILineLike) TestLine).StartToEndVect);
		Assert.AreEqual(null, ((ILineLike) TestLine).EndPoint);
		Assert.AreEqual(false, ((ILineLike) TestLine).IsFiniteLength);
	}

	[Test]
	public void ConstructorsShouldCorrectlyConstruct() {
		Assert.AreEqual(TestLine, new Line(TestLine.PointOnLine, new Direction(-1f - 1f, -2f - 2f, 3f - -3f)));
		Assert.AreEqual(new Direction(-1f - 1f, -2f - 2f, 3f - -3f), new Line(TestLine.PointOnLine, new Direction(-1f - 1f, -2f - 2f, 3f - -3f)).Direction);
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		const string Expectation = "Line[PointOnLine <1.0, 2.0, -3.0> | Direction <-0.3, -0.5, 0.8>]";
		Assert.AreEqual(Expectation, TestLine.ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		TestLine.TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture);
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
	}

	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = "Line[PointOnLine <1.0, 2.0, -3.0> | Direction <-0.3, -0.5, 0.8>]";
		Assert.AreEqual(new Line(new Location(1f, 2f, -3f), new Direction(-0.3f, -0.5f, 0.8f)), Line.Parse(Input, CultureInfo.InvariantCulture));
		Assert.AreEqual(true, Line.TryParse(Input, CultureInfo.InvariantCulture, out var result));
		Assert.AreEqual(new Line(new Location(1f, 2f, -3f), new Direction(-0.3f, -0.5f, 0.8f)), result);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength(TestLine);
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(TestLine);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(TestLine, TestLine.PointOnLine.X, TestLine.PointOnLine.Y, TestLine.PointOnLine.Z, TestLine.Direction.X, TestLine.Direction.Y, TestLine.Direction.Z);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		var start = new Line(new Location(5f, 5f, 5f), Direction.Forward);
		var end = new Line(new Location(15f, 15f, 15f), Direction.Right);
		var startLoc = start.PointClosestTo(end);
		var startToEndLoc = start.PointClosestTo(end).VectTo(end.PointClosestTo(start));
		var startToEndDir = Direction.Forward >> Direction.Right;

		Assert.AreEqual(new Line(startLoc + startToEndLoc * -0.5f, Direction.Forward * (startToEndDir * -0.5f)), Line.Interpolate(start, end, -0.5f));
		Assert.AreEqual(new Line(startLoc + startToEndLoc * 0.5f, Direction.Forward * (startToEndDir * 0.5f)), Line.Interpolate(start, end, 0.5f));
		Assert.AreEqual(new Line(startLoc + startToEndLoc * 1.5f, Direction.Forward * (startToEndDir * 1.5f)), Line.Interpolate(start, end, 1.5f));

		Assert.AreEqual(start, Line.Interpolate(start, new Line(new Location(5f, 5f, 15f), Direction.Forward), 1f));
		Assert.AreEqual(start, Line.Interpolate(start, new Line(new Location(5f, 5f, 15f), Direction.Forward), 0f));
		Assert.AreEqual(start, Line.Interpolate(start, new Line(new Location(5f, 5f, 15f), Direction.Forward), 0.5f));
		Assert.AreEqual(start, Line.Interpolate(start, new Line(new Location(5f, 5f, 15f), Direction.Forward), -0.5f));

		AssertToleranceEquals(
			new Line(new Location(1f, 0f, 0f), Direction.Right),
			Line.Interpolate(
				new Line(new Location(1f, 0f, 0f), Direction.Right),
				new Line(new Location(1f, 0f, 0f), Direction.Forward),
				0f
			),
			TestTolerance
		);
		AssertToleranceEquals(
			new Line(new Location(1f, 0f, 0f), Direction.Forward),
			Line.Interpolate(
				new Line(new Location(1f, 0f, 0f), Direction.Right),
				new Line(new Location(1f, 0f, 0f), Direction.Forward),
				1f
			),
			TestTolerance
		);
		AssertToleranceEquals(
			new Line(new Location(1f, 0f, 0f), new Direction(-1f, 0f, 1f)),
			Line.Interpolate(
				new Line(new Location(1f, 0f, 0f), Direction.Right),
				new Line(new Location(1f, 0f, 0f), Direction.Forward),
				0.5f
			),
			TestTolerance
		);
		AssertToleranceEquals(
			new Line(new Location(1f, 0f, 1f), new Direction(-1f, 0f, 1f)),
			Line.Interpolate(
				new Line(new Location(1f, 0f, 1f), Direction.Right),
				new Line(new Location(1f, 0f, 0f), Direction.Forward),
				0.5f
			),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyCreateRandomObjects() {
		const int NumIterations = 10_000;

		var start = new Line(new Location(5f, 5f, 5f), Direction.Forward);
		var end = new Line(new Location(15f, 15f, 15f), Direction.Right);

		for (var i = 0; i < NumIterations; ++i) {
			var val = Line.NewRandom(start, end);
			Assert.GreaterOrEqual(val.PointOnLine.X, start.PointOnLine.X);
			Assert.GreaterOrEqual(val.PointOnLine.Y, start.PointOnLine.Y);
			Assert.GreaterOrEqual(val.PointOnLine.Z, start.PointOnLine.Z);
			Assert.Less(val.PointOnLine.X, end.PointOnLine.X);
			Assert.Less(val.PointOnLine.Y, end.PointOnLine.Y);
			Assert.Less(val.PointOnLine.Z, end.PointOnLine.Z);
			Assert.AreEqual(0f, val.Direction.Y);
			Assert.GreaterOrEqual(val.Direction.Z, 0f);
			Assert.LessOrEqual(val.Direction.Z, 1f);
			Assert.GreaterOrEqual(val.Direction.X, -1f);
			Assert.LessOrEqual(val.Direction.X, 0f);
		}
	}

	[Test]
	public void ShouldCorrectlyImplementEquality() {
		Assert.AreEqual(
			new Line(new Location(100f, 0f, 0f), Direction.Right),
			new Line(new Location(-100f, 0f, 0f), Direction.Left)
		);

		AssertToleranceEquals(
			new Line(new Location(100f, 0f, 0f), Direction.Right),
			new Line(new Location(-100f, 0f, 0.1f), Direction.Left),
			0.2f
		);

		AssertToleranceNotEquals(
			new Line(new Location(100f, 0f, 0f), Direction.Right),
			new Line(new Location(-100f, 0f, 0.1f), Direction.Left),
			0.05f
		);

		AssertToleranceEquals(
			new Line(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0.05f)),
			new Line(new Location(100f, 0f, 0f), Direction.Left),
			0.05f
		);

		AssertToleranceNotEquals(
			new Line(new Location(100f, 0f, 0f), new Direction(-1f, 0f, 0.05f)),
			new Line(new Location(100f, 0f, 0f), Direction.Left),
			0.03f
		);

		Assert.IsTrue(
			new Line(new Location(100f, 0f, 0f), Direction.Right).EqualsWithinDistanceAndAngle(
				new Line(new Location(-100f, 0f, 0f), Direction.Left),
				0f,
				Angle.Zero
			)
		);
		Assert.IsFalse(
			new Line(new Location(100f, 0f, 0f), Direction.Right).EqualsWithinDistanceAndAngle(
				new Line(new Location(-100f, 0f, 0.1f), Direction.Left),
				0f,
				Angle.Zero
			)
		);
		Assert.IsTrue(
			new Line(new Location(100f, 0f, 0f), Direction.Right).EqualsWithinDistanceAndAngle(
				new Line(new Location(-100f, 0f, 0.1f), Direction.Left),
				0.2f,
				Angle.Zero
			)
		);
		Assert.IsFalse(
			new Line(new Location(100f, 0f, 0f), Direction.Right).EqualsWithinDistanceAndAngle(
				new Line(new Location(-100f, 0f, 0.1f), Direction.Left),
				0.05f,
				Angle.Zero
			)
		);
		Assert.IsTrue(
			new Line(new Location(100f, 0f, 0f), Direction.Right).EqualsWithinDistanceAndAngle(
				new Line(new Location(-100f, 0f, 0.1f), new Direction(1f, 0f, 0.1f)),
				0.2f,
				(Direction.Left ^ new Direction(1f, 0f, 0.1f)) * 1.1f
			)
		);
		Assert.IsFalse(
			new Line(new Location(100f, 0f, 0f), Direction.Right).EqualsWithinDistanceAndAngle(
				new Line(new Location(-100f, 0f, 0f), new Direction(1f, 0f, 0.1f)),
				0f,
				(Direction.Left ^ new Direction(1f, 0f, 0.1f)) * 0.9f
			)
		);
	}
}