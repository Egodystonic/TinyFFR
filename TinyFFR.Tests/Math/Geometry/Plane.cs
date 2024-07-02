// Created on 2024-04-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class PlaneTest {
	const float TestTolerance = 0.001f;
	public static readonly Plane TestPlane = new(Direction.Up, (0f, -1f, 0f));

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void PropertiesShouldBeCorrectlyImplemented() {
		Assert.AreEqual(Direction.Up, TestPlane.Normal);
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.ClosestPointToOrigin);
	}

	[Test]
	public void ConstructorShouldCorrectlyCalculateProperties() {
		Assert.AreEqual(TestPlane, new Plane(Direction.Up, (100f, -1f, 0f)));
	}

	[Test]
	public void FactoryMethodsShouldCorrectlyConstructPlane() {
		Assert.AreEqual(TestPlane, new Plane(Direction.Up, -1f));
		Assert.AreEqual(new Plane(Direction.Backward, Location.Origin), new Plane(Direction.Backward, 0f));

		Assert.AreEqual(TestPlane, Plane.FromPointClosestToOrigin((0f, -1f, 0f), true));
		Assert.AreEqual(new Plane(Direction.Backward, (0f, 0f, -3f)), Plane.FromPointClosestToOrigin((0f, 0f, -3f), false));
		Assert.Throws<ArgumentException>(() => Plane.FromPointClosestToOrigin(Location.Origin, true));

		Assert.AreEqual(TestPlane, Plane.FromTriangleOnSurface((100f, -1f, 100f), (0f, -1f, 0f), (-100f, -1f, 0f)));
		Assert.Throws<ArgumentException>(() => Plane.FromTriangleOnSurface((0f, 0f, 0f), (0f, 1f, 0f), (0f, 2f, 0f)));
		Assert.Throws<ArgumentException>(() => Plane.FromTriangleOnSurface((0f, 0f, 0f), (0f, 0f, 0f), (0f, 0f, 0f)));
	}

	// TODO this test could be fleshed out a lot more
	[Test]
	public void ShouldCorrectlyConvertToString() {
		const string Expectation = "Plane[Normal <0.0, 1.0, 0.0> | PointClosestToOrigin <-0.0, -1.0, -0.0>]"; // Negative zero can be removed at a later date if we expunge it from formatting
		Assert.AreEqual(Expectation, TestPlane.ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		TestPlane.TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture);
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
	}

	// TODO this test could be fleshed out a lot more
	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = "Plane[Normal <0.0, 1.0, 0.0> | PointClosestToOrigin <0.0, -1.0, 0.0>]";
		Assert.AreEqual(TestPlane, Plane.Parse(Input, CultureInfo.InvariantCulture));
		Assert.AreEqual(true, Plane.TryParse(Input, CultureInfo.InvariantCulture, out var result));
		Assert.AreEqual(TestPlane, result);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<Plane>();
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(TestPlane);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(TestPlane, TestPlane.Normal.X, TestPlane.Normal.Y, TestPlane.Normal.Z, TestPlane.ClosestPointToOrigin.X, TestPlane.ClosestPointToOrigin.Y, TestPlane.ClosestPointToOrigin.Z);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		var start = new Plane(Direction.Forward, new Location(5f, 0f, 0f));
		var end = new Plane(Direction.Up, new Location(0f, 0f, -5f));
		var normalTransition = start.Normal >> end.Normal;
		var pointTransition = start.ClosestPointToOrigin >> end.ClosestPointToOrigin;

		AssertToleranceEquals(new Plane(start.Normal * (normalTransition * -0.5f), start.ClosestPointToOrigin + (pointTransition * -0.5f)), Plane.Interpolate(start, end, -0.5f), TestTolerance);
		AssertToleranceEquals(new Plane(start.Normal * (normalTransition * 0.5f), start.ClosestPointToOrigin + (pointTransition * 0.5f)), Plane.Interpolate(start, end, 0.5f), TestTolerance);
		AssertToleranceEquals(new Plane(start.Normal * (normalTransition * 1.5f), start.ClosestPointToOrigin + (pointTransition * 1.5f)), Plane.Interpolate(start, end, 1.5f), TestTolerance);
		AssertToleranceEquals(new Plane(start.Normal, start.ClosestPointToOrigin), Plane.Interpolate(start, end, 0f), TestTolerance);
		AssertToleranceEquals(new Plane(end.Normal, end.ClosestPointToOrigin), Plane.Interpolate(start, end, 1f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyCreateRandomObjects() {
		const int NumIterations = 10_000;

		var start = new Plane(Direction.Forward, new Location(5f, 0f, 0f));
		var end = new Plane(Direction.Up, new Location(0f, 0f, -5f));

		for (var i = 0; i < NumIterations; ++i) {
			var val = Plane.CreateNewRandom(start, end);
			Assert.AreEqual(0f, val.Normal.X);
			Assert.GreaterOrEqual(val.Normal.Y, 0f);
			Assert.LessOrEqual(val.Normal.Y, 1f);
			Assert.GreaterOrEqual(val.Normal.Z, 0f);
			Assert.LessOrEqual(val.Normal.Z, 1f);

			Assert.LessOrEqual(val.ClosestPointToOrigin.X, start.ClosestPointToOrigin.X);
			Assert.GreaterOrEqual(val.ClosestPointToOrigin.X, 0f);
			Assert.AreEqual(0f, val.ClosestPointToOrigin.Y);
			Assert.LessOrEqual(val.ClosestPointToOrigin.Z, 0f);
			Assert.GreaterOrEqual(val.ClosestPointToOrigin.Z, -5f);
		}
	}

	[Test]
	public void ShouldCorrectlyImplementEquality() {
		Assert.AreEqual(TestPlane, new Plane(Direction.Up, new Location(10000f, -1f, -113513f)));

		AssertToleranceEquals(
			TestPlane,
			new Plane(new Direction(0f, 1f, 0.1f), new Location(0f, -1.1f, 0f)),
			0.11f
		);
		AssertToleranceNotEquals(
			TestPlane,
			new Plane(new Direction(0f, 1f, 0.1f), new Location(0f, -1.1f, 0f)),
			0.09f
		);

		Assert.IsTrue(
			TestPlane.EqualsWithinAngleAndLocation(
				new Plane(Direction.Up * ((Direction.Up >> Direction.Forward) * 0.1f), TestPlane.ClosestPointToOrigin),
				10f,
				Single.MaxValue
			)
		);
		Assert.IsFalse(
			TestPlane.EqualsWithinAngleAndLocation(
				new Plane(Direction.Up * ((Direction.Up >> Direction.Forward) * 0.1f), TestPlane.ClosestPointToOrigin),
				8f,
				Single.MaxValue
			)
		);
		Assert.IsTrue(
			TestPlane.EqualsWithinAngleAndLocation(
				new Plane(Direction.Up, (0f, -1.1f, 0f)),
				0f,
				0.15f
			)
		);
		Assert.IsFalse(
			TestPlane.EqualsWithinAngleAndLocation(
				new Plane(Direction.Up, (0f, -1.1f, 0f)),
				0f,
				0.05f
			)
		);
		Assert.IsFalse(
			TestPlane.EqualsWithinAngleAndLocation(
				new Plane(Direction.Up, (0f, 1f, 0f)),
				0f,
				1f
			)
		);
		Assert.IsTrue(
			TestPlane.EqualsWithinAngleAndLocation(
				new Plane(Direction.Down, (0f, -1f, 0f)),
				Angle.FullCircle,
				0f
			)
		);
	}
}