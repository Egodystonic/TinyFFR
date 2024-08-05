// Created on 2024-04-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
class PlaneTest {
	const float TestTolerance = 0.001f;
	public static readonly Plane TestPlane = new(Direction.Up, (0f, -1f, 0f));

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void PropertiesShouldBeCorrectlyImplemented() {
		Assert.AreEqual(Direction.Up, TestPlane.Normal);
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.PointClosestToOrigin);
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
		Assert.AreEqual(null, Plane.FromPointClosestToOrigin(Location.Origin, true));

		Assert.AreEqual(TestPlane, Plane.FromTriangleOnSurface((100f, -1f, 100f), (0f, -1f, 0f), (-100f, -1f, 0f)));
		Assert.AreEqual(null, Plane.FromTriangleOnSurface((0f, 0f, 0f), (0f, 1f, 0f), (0f, 2f, 0f)));
		Assert.AreEqual(null, Plane.FromTriangleOnSurface((0f, 0f, 0f), (0f, 0f, 0f), (0f, 0f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		const string Expectation = "Plane[Normal <0.0, 1.0, 0.0> | PointClosestToOrigin <-0.0, -1.0, -0.0>]"; // Negative zero can be removed at a later date if we expunge it from formatting
		Assert.AreEqual(Expectation, TestPlane.ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		TestPlane.TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture);
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
	}

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
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(TestPlane, TestPlane.Normal.X, TestPlane.Normal.Y, TestPlane.Normal.Z, TestPlane.PointClosestToOrigin.X, TestPlane.PointClosestToOrigin.Y, TestPlane.PointClosestToOrigin.Z);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		var start = new Plane(Direction.Forward, new Location(5f, 0f, 0f));
		var end = new Plane(Direction.Up, new Location(0f, 0f, -5f));
		var normalTransition = start.Normal >> end.Normal;
		var pointTransition = start.PointClosestToOrigin >> end.PointClosestToOrigin;

		AssertToleranceEquals(new Plane(start.Normal * (normalTransition * -0.5f), start.PointClosestToOrigin + (pointTransition * -0.5f)), Plane.Interpolate(start, end, -0.5f), TestTolerance);
		AssertToleranceEquals(new Plane(start.Normal * (normalTransition * 0.5f), start.PointClosestToOrigin + (pointTransition * 0.5f)), Plane.Interpolate(start, end, 0.5f), TestTolerance);
		AssertToleranceEquals(new Plane(start.Normal * (normalTransition * 1.5f), start.PointClosestToOrigin + (pointTransition * 1.5f)), Plane.Interpolate(start, end, 1.5f), TestTolerance);
		AssertToleranceEquals(new Plane(start.Normal, start.PointClosestToOrigin), Plane.Interpolate(start, end, 0f), TestTolerance);
		AssertToleranceEquals(new Plane(end.Normal, end.PointClosestToOrigin), Plane.Interpolate(start, end, 1f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyCreateRandomObjects() {
		const int NumIterations = 10_000;

		var start = new Plane(Direction.Forward, new Location(5f, 0f, 0f));
		var end = new Plane(Direction.Up, new Location(0f, 0f, -5f));

		for (var i = 0; i < NumIterations; ++i) {
			var val = Plane.Random(start, end);
			Assert.AreEqual(0f, val.Normal.X);
			Assert.GreaterOrEqual(val.Normal.Y, 0f);
			Assert.LessOrEqual(val.Normal.Y, 1f);
			Assert.GreaterOrEqual(val.Normal.Z, 0f);
			Assert.LessOrEqual(val.Normal.Z, 1f);

			Assert.LessOrEqual(val.PointClosestToOrigin.X, start.PointClosestToOrigin.X);
			Assert.GreaterOrEqual(val.PointClosestToOrigin.X, 0f);
			Assert.AreEqual(0f, val.PointClosestToOrigin.Y);
			Assert.LessOrEqual(val.PointClosestToOrigin.Z, 0f);
			Assert.GreaterOrEqual(val.PointClosestToOrigin.Z, -5f);
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
			TestPlane.EqualsWithinDistanceAndAngle(
				new Plane(Direction.Up * ((Direction.Up >> Direction.Forward) * 0.1f), TestPlane.PointClosestToOrigin),
				Single.MaxValue, 10f)
		);
		Assert.IsFalse(
			TestPlane.EqualsWithinDistanceAndAngle(
				new Plane(Direction.Up * ((Direction.Up >> Direction.Forward) * 0.1f), TestPlane.PointClosestToOrigin),
				Single.MaxValue, 8f)
		);
		Assert.IsTrue(
			TestPlane.EqualsWithinDistanceAndAngle(
				new Plane(Direction.Up, (0f, -1.1f, 0f)),
				0.15f, 0f)
		);
		Assert.IsFalse(
			TestPlane.EqualsWithinDistanceAndAngle(
				new Plane(Direction.Up, (0f, -1.1f, 0f)),
				0.05f, 0f)
		);
		Assert.IsFalse(
			TestPlane.EqualsWithinDistanceAndAngle(
				new Plane(Direction.Up, (0f, 1f, 0f)),
				1f, 0f)
		);
		Assert.IsTrue(
			TestPlane.EqualsWithinDistanceAndAngle(
				new Plane(Direction.Down, (0f, -1f, 0f)),
				0f, Angle.FullCircle)
		);
	}

	[Test]
	public void ShouldCorrectlyFlipPlanes() {
		Assert.AreEqual(new Plane(Direction.Down, (0f, -1f, 0f)), TestPlane.Flipped);
		Assert.AreEqual(TestPlane.Flipped, -TestPlane);
	}

	[Test]
	public void ShouldCorrectlyMovePlanes() {
		Assert.AreEqual(new Plane(Direction.Up, (0f, -1f, 0f)), TestPlane + new Vect(100f, 0f, -100f));
		Assert.AreEqual(new Plane(Direction.Up, (0f, -11f, 0f)), TestPlane + new Vect(100f, -10f, -100f));
		Assert.AreEqual(new Plane(Direction.Up, (0f, 9f, 0f)), TestPlane + new Vect(100f, 10f, -100f));
	}

	[Test]
	public void ShouldCorrectlyRotateAroundPivot() {
		AssertToleranceEquals(new Plane(Direction.Down, (0f, 1f, 0f)), TestPlane * (180f % Direction.Left, Location.Origin), TestTolerance);
		AssertToleranceEquals(new Plane(Direction.Down, (0f, -3f, 0f)), TestPlane * (180f % Direction.Left, (0f, -2f, 0f)), TestTolerance);

		AssertToleranceEquals(TestPlane, TestPlane * (90f % Direction.Up, (43f, -123f, 0.9f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineAngleToDirections() {
		AssertToleranceEquals(Angle.Zero, TestPlane.AngleTo(Direction.Forward), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.AngleTo(Direction.Backward), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.AngleTo(Direction.Right), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.AngleTo(Direction.Left), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle, TestPlane.AngleTo(Direction.Up), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle, TestPlane.AngleTo(Direction.Down), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.AngleTo(new Direction(1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.AngleTo(new Direction(-1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.AngleTo(new Direction(-1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.AngleTo(new Direction(1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(MathF.Atan(0.5f)), TestPlane.AngleTo(new Direction(2f, -1f, 0f)), TestTolerance);

		AssertToleranceEquals(Angle.Zero, TestPlane.SignedAngleTo(Direction.Forward), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.SignedAngleTo(Direction.Backward), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.SignedAngleTo(Direction.Right), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.SignedAngleTo(Direction.Left), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle, TestPlane.SignedAngleTo(Direction.Up), TestTolerance);
		AssertToleranceEquals(-Angle.QuarterCircle, TestPlane.SignedAngleTo(Direction.Down), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.SignedAngleTo(new Direction(1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.SignedAngleTo(new Direction(-1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(-Angle.EighthCircle, TestPlane.SignedAngleTo(new Direction(-1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(-Angle.EighthCircle, TestPlane.SignedAngleTo(new Direction(1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(-Angle.FromRadians(MathF.Atan(0.5f)), TestPlane.SignedAngleTo(new Direction(2f, -1f, 0f)), TestTolerance);

		// Incident angle
		AssertToleranceEquals(null, TestPlane.IncidentAngleWith(Direction.Forward), TestTolerance);
		AssertToleranceEquals(null, TestPlane.IncidentAngleWith(Direction.Backward), TestTolerance);
		AssertToleranceEquals(null, TestPlane.IncidentAngleWith(Direction.Right), TestTolerance);
		AssertToleranceEquals(null, TestPlane.IncidentAngleWith(Direction.Left), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.IncidentAngleWith(Direction.Up), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.IncidentAngleWith(Direction.Down), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.IncidentAngleWith(new Direction(1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.IncidentAngleWith(new Direction(-1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.IncidentAngleWith(new Direction(-1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.IncidentAngleWith(new Direction(1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle - Angle.FromRadians(MathF.Atan(0.5f)), TestPlane.IncidentAngleWith(new Direction(2f, -1f, 0f)), TestTolerance);

		AssertToleranceEquals(Angle.QuarterCircle, TestPlane.FastIncidentAngleWith(Direction.Forward), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle, TestPlane.FastIncidentAngleWith(Direction.Backward), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle, TestPlane.FastIncidentAngleWith(Direction.Right), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle, TestPlane.FastIncidentAngleWith(Direction.Left), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.FastIncidentAngleWith(Direction.Up), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.FastIncidentAngleWith(Direction.Down), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.FastIncidentAngleWith(new Direction(1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.FastIncidentAngleWith(new Direction(-1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.FastIncidentAngleWith(new Direction(-1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.FastIncidentAngleWith(new Direction(1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle - Angle.FromRadians(MathF.Atan(0.5f)), TestPlane.FastIncidentAngleWith(new Direction(2f, -1f, 0f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineAngleToVects() {
		AssertToleranceEquals(Angle.Zero, TestPlane.AngleTo(10f * Direction.Forward), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.AngleTo(10f * Direction.Backward), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.AngleTo(10f * Direction.Right), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.AngleTo(10f * Direction.Left), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle, TestPlane.AngleTo(10f * Direction.Up), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle, TestPlane.AngleTo(10f * Direction.Down), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.AngleTo(10f * new Direction(1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.AngleTo(10f * new Direction(-1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.AngleTo(10f * new Direction(-1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.AngleTo(10f * new Direction(1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(MathF.Atan(0.5f)), TestPlane.AngleTo(10f * new Direction(2f, -1f, 0f)), TestTolerance);

		AssertToleranceEquals(Angle.Zero, TestPlane.SignedAngleTo(10f * Direction.Forward), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.SignedAngleTo(10f * Direction.Backward), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.SignedAngleTo(10f * Direction.Right), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.SignedAngleTo(10f * Direction.Left), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle, TestPlane.SignedAngleTo(10f * Direction.Up), TestTolerance);
		AssertToleranceEquals(-Angle.QuarterCircle, TestPlane.SignedAngleTo(10f * Direction.Down), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.SignedAngleTo(10f * new Direction(1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.SignedAngleTo(10f * new Direction(-1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(-Angle.EighthCircle, TestPlane.SignedAngleTo(10f * new Direction(-1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(-Angle.EighthCircle, TestPlane.SignedAngleTo(10f * new Direction(1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(-Angle.FromRadians(MathF.Atan(0.5f)), TestPlane.SignedAngleTo(10f * new Direction(2f, -1f, 0f)), TestTolerance);

		// Incident angle
		AssertToleranceEquals(null, TestPlane.IncidentAngleWith(10f * Direction.Forward), TestTolerance);
		AssertToleranceEquals(null, TestPlane.IncidentAngleWith(10f * Direction.Backward), TestTolerance);
		AssertToleranceEquals(null, TestPlane.IncidentAngleWith(10f * Direction.Right), TestTolerance);
		AssertToleranceEquals(null, TestPlane.IncidentAngleWith(10f * Direction.Left), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.IncidentAngleWith(10f * Direction.Up), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.IncidentAngleWith(10f * Direction.Down), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.IncidentAngleWith(10f * new Direction(1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.IncidentAngleWith(10f * new Direction(-1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.IncidentAngleWith(10f * new Direction(-1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.IncidentAngleWith(10f * new Direction(1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle - Angle.FromRadians(MathF.Atan(0.5f)), TestPlane.IncidentAngleWith(10f * new Direction(2f, -1f, 0f)), TestTolerance);

		AssertToleranceEquals(Angle.QuarterCircle, TestPlane.FastIncidentAngleWith(10f * Direction.Forward), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle, TestPlane.FastIncidentAngleWith(10f * Direction.Backward), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle, TestPlane.FastIncidentAngleWith(10f * Direction.Right), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle, TestPlane.FastIncidentAngleWith(10f * Direction.Left), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.FastIncidentAngleWith(10f * Direction.Up), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.FastIncidentAngleWith(10f * Direction.Down), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.FastIncidentAngleWith(10f * new Direction(1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.FastIncidentAngleWith(10f * new Direction(-1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.FastIncidentAngleWith(10f * new Direction(-1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.FastIncidentAngleWith(10f * new Direction(1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle - Angle.FromRadians(MathF.Atan(0.5f)), TestPlane.FastIncidentAngleWith(10f * new Direction(2f, -1f, 0f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineAngleToPlanes() {
		AssertToleranceEquals(Angle.Zero, TestPlane.AngleTo(TestPlane), TestTolerance);
		AssertToleranceEquals(Angle.Zero, TestPlane.AngleTo(-TestPlane), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle, TestPlane.AngleTo(new Plane(Direction.Backward, Location.Origin)), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle, TestPlane.AngleTo(new Plane(Direction.Forward, Location.Origin)), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle, TestPlane.AngleTo(new Plane(Direction.Left, Location.Origin)), TestTolerance);
		AssertToleranceEquals(Angle.QuarterCircle, TestPlane.AngleTo(new Plane(Direction.Right, Location.Origin)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.AngleTo(new Plane((1f, 1f, 0f), Location.Origin)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.AngleTo(new Plane((-1f, -1f, 0f), Location.Origin)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.AngleTo(new Plane((1f, -1f, 0f), Location.Origin)), TestTolerance);
		AssertToleranceEquals(Angle.EighthCircle, TestPlane.AngleTo(new Plane((-1f, 1f, 0f), Location.Origin)), TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(MathF.Atan(2f)), TestPlane.AngleTo(new Plane((2f, -1f, 0f), Location.Origin)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyReflectDirections() {
		Assert.AreEqual(Direction.Up, TestPlane.ReflectionOf(Direction.Down));
		Assert.AreEqual(Direction.Down, TestPlane.ReflectionOf(Direction.Up));
		Assert.AreEqual(null, TestPlane.ReflectionOf(Direction.Left));
		Assert.AreEqual(null, TestPlane.ReflectionOf(Direction.Right));
		Assert.AreEqual(null, TestPlane.ReflectionOf(Direction.Forward));
		Assert.AreEqual(null, TestPlane.ReflectionOf(Direction.Backward));
		AssertToleranceEquals(new Direction(1f, -1f, 0f), TestPlane.ReflectionOf(new Direction(1f, 1f, 0f)), TestTolerance);

		Assert.AreEqual(Direction.Up, TestPlane.Flipped.ReflectionOf(Direction.Down));
		Assert.AreEqual(Direction.Down, TestPlane.Flipped.ReflectionOf(Direction.Up));
		Assert.AreEqual(null, TestPlane.Flipped.ReflectionOf(Direction.Left));
		Assert.AreEqual(null, TestPlane.Flipped.ReflectionOf(Direction.Right));
		Assert.AreEqual(null, TestPlane.Flipped.ReflectionOf(Direction.Forward));
		Assert.AreEqual(null, TestPlane.Flipped.ReflectionOf(Direction.Backward));
		AssertToleranceEquals(new Direction(1f, -1f, 0f), TestPlane.Flipped.ReflectionOf(new Direction(1f, 1f, 0f)), TestTolerance);

		// Fast
		Assert.AreEqual(Direction.Up, TestPlane.FastReflectionOf(Direction.Down));
		Assert.AreEqual(Direction.Down, TestPlane.FastReflectionOf(Direction.Up));
		Assert.AreEqual(Direction.Left, TestPlane.FastReflectionOf(Direction.Left));
		Assert.AreEqual(Direction.Right, TestPlane.FastReflectionOf(Direction.Right));
		Assert.AreEqual(Direction.Forward, TestPlane.FastReflectionOf(Direction.Forward));
		Assert.AreEqual(Direction.Backward, TestPlane.FastReflectionOf(Direction.Backward));
		AssertToleranceEquals(new Direction(1f, -1f, 0f), TestPlane.FastReflectionOf(new Direction(1f, 1f, 0f)), TestTolerance);

		Assert.AreEqual(Direction.Up, TestPlane.Flipped.FastReflectionOf(Direction.Down));
		Assert.AreEqual(Direction.Down, TestPlane.Flipped.FastReflectionOf(Direction.Up));
		Assert.AreEqual(Direction.Left, TestPlane.Flipped.FastReflectionOf(Direction.Left));
		Assert.AreEqual(Direction.Right, TestPlane.Flipped.FastReflectionOf(Direction.Right));
		Assert.AreEqual(Direction.Forward, TestPlane.Flipped.FastReflectionOf(Direction.Forward));
		Assert.AreEqual(Direction.Backward, TestPlane.Flipped.FastReflectionOf(Direction.Backward));
		AssertToleranceEquals(new Direction(1f, -1f, 0f), TestPlane.Flipped.FastReflectionOf(new Direction(1f, 1f, 0f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyReflectVectors() {
		Assert.AreEqual(Direction.Up * 10f, TestPlane.ReflectionOf(Direction.Down * 10f));
		Assert.AreEqual(Direction.Down * 10f, TestPlane.ReflectionOf(Direction.Up * 10f));
		Assert.AreEqual(null, TestPlane.ReflectionOf(Direction.Left * 10f));
		Assert.AreEqual(null, TestPlane.ReflectionOf(Direction.Right * 10f));
		Assert.AreEqual(null, TestPlane.ReflectionOf(Direction.Forward * 10f));
		Assert.AreEqual(null, TestPlane.ReflectionOf(Direction.Backward * 10f));
		AssertToleranceEquals(new Direction(1f, -1f, 0f) * 10f, TestPlane.ReflectionOf(new Direction(1f, 1f, 0f) * 10f), TestTolerance);

		Assert.AreEqual(Direction.Up * 10f, TestPlane.Flipped.ReflectionOf(Direction.Down * 10f));
		Assert.AreEqual(Direction.Down * 10f, TestPlane.Flipped.ReflectionOf(Direction.Up * 10f));
		Assert.AreEqual(null, TestPlane.Flipped.ReflectionOf(Direction.Left * 10f));
		Assert.AreEqual(null, TestPlane.Flipped.ReflectionOf(Direction.Right * 10f));
		Assert.AreEqual(null, TestPlane.Flipped.ReflectionOf(Direction.Forward * 10f));
		Assert.AreEqual(null, TestPlane.Flipped.ReflectionOf(Direction.Backward * 10f));
		AssertToleranceEquals(new Direction(1f, -1f, 0f) * 10f, TestPlane.Flipped.ReflectionOf(new Direction(1f, 1f, 0f) * 10f), TestTolerance);

		// Fast
		Assert.AreEqual(Direction.Up * 10f, TestPlane.FastReflectionOf(Direction.Down * 10f));
		Assert.AreEqual(Direction.Down * 10f, TestPlane.FastReflectionOf(Direction.Up * 10f));
		Assert.AreEqual(Direction.Left * 10f, TestPlane.FastReflectionOf(Direction.Left * 10f));
		Assert.AreEqual(Direction.Right * 10f, TestPlane.FastReflectionOf(Direction.Right * 10f));
		Assert.AreEqual(Direction.Forward * 10f, TestPlane.FastReflectionOf(Direction.Forward * 10f));
		Assert.AreEqual(Direction.Backward * 10f, TestPlane.FastReflectionOf(Direction.Backward * 10f));
		AssertToleranceEquals(new Direction(1f, -1f, 0f) * 10f, TestPlane.FastReflectionOf(new Direction(1f, 1f, 0f) * 10f), TestTolerance);

		Assert.AreEqual(Direction.Up * 10f, TestPlane.Flipped.FastReflectionOf(Direction.Down * 10f));
		Assert.AreEqual(Direction.Down * 10f, TestPlane.Flipped.FastReflectionOf(Direction.Up * 10f));
		Assert.AreEqual(Direction.Left * 10f, TestPlane.Flipped.FastReflectionOf(Direction.Left * 10f));
		Assert.AreEqual(Direction.Right * 10f, TestPlane.Flipped.FastReflectionOf(Direction.Right * 10f));
		Assert.AreEqual(Direction.Forward * 10f, TestPlane.Flipped.FastReflectionOf(Direction.Forward * 10f));
		Assert.AreEqual(Direction.Backward * 10f, TestPlane.Flipped.FastReflectionOf(Direction.Backward * 10f));
		AssertToleranceEquals(new Direction(1f, -1f, 0f) * 10f, TestPlane.Flipped.FastReflectionOf(new Direction(1f, 1f, 0f) * 10f), TestTolerance);
	}

	[Test]
	public void ShouldUseAppropriateVectReflectionErrorMargin() {
		const float MinDifferentiableAngleDegrees = 0.1f;

		for (var i = 0; i < 10_000; ++i) {
			var randomVect = Direction.Random(Direction.Up, 90f - MinDifferentiableAngleDegrees, 90f - MinDifferentiableAngleDegrees) * 1f;
			try {
				Assert.IsNotNull(TestPlane.ReflectionOf(randomVect));
			}
			catch {
				Console.WriteLine(randomVect + " (" + randomVect.AngleTo(TestPlane) + " angle to plane)");
				throw;
			}
		}
	}

	[Test]
	public void ShouldUseAppropriateDirectionReflectionErrorMargin() {
		const float MinDifferentiableAngleDegrees = 0.1f;

		for (var i = 0; i < 10_000; ++i) {
			var randomDir = Direction.Random(Direction.Up, 90f - MinDifferentiableAngleDegrees, 90f - MinDifferentiableAngleDegrees);
			try {
				Assert.IsNotNull(TestPlane.ReflectionOf(randomDir));
			}
			catch {
				Console.WriteLine(randomDir + " (" + randomDir.AngleTo(TestPlane) + " angle to plane)");
				throw;
			}
		}
	}

	[Test]
	public void ShouldUseAppropriateDirectionIncidentAngleErrorMargin() {
		const float MinDifferentiableAngleDegrees = 0.1f;

		for (var i = 0; i < 10_000; ++i) {
			var randomDir = Direction.Random(Direction.Up, 90f - MinDifferentiableAngleDegrees, 90f - MinDifferentiableAngleDegrees);
			try {
				Assert.IsNotNull(TestPlane.ReflectionOf(randomDir));
			}
			catch {
				Console.WriteLine(randomDir + " (" + randomDir.AngleTo(TestPlane) + " angle to plane)");
				throw;
			}
		}
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToGivenLocation() {
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.PointClosestTo((0f, -1f, 0f)));
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.PointClosestTo((0f, -1000f, 0f)));
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.PointClosestTo((0f, 1000f, 0f)));
		Assert.AreEqual(new Location(100f, -1f, -100f), TestPlane.PointClosestTo((100f, -1000f, -100f)));
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.Flipped.PointClosestTo((0f, -1f, 0f)));
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.Flipped.PointClosestTo((0f, -1000f, 0f)));
		Assert.AreEqual(new Location(0f, -1f, 0f), TestPlane.Flipped.PointClosestTo((0f, 1000f, 0f)));
		Assert.AreEqual(new Location(100f, -1f, -100f), TestPlane.Flipped.PointClosestTo((100f, -1000f, -100f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromGivenLocation() {
		void AssertDistanceFromTestPlane(float expectedSignedDistance, Location location) {
			Assert.AreEqual(expectedSignedDistance, TestPlane.SignedDistanceFrom(location), TestTolerance);
			Assert.AreEqual(-expectedSignedDistance, TestPlane.Flipped.SignedDistanceFrom(location), TestTolerance);
			Assert.AreEqual(MathF.Abs(expectedSignedDistance), TestPlane.DistanceFrom(location), TestTolerance);
			Assert.AreEqual(MathF.Abs(expectedSignedDistance), TestPlane.Flipped.DistanceFrom(location), TestTolerance);
		}

		AssertDistanceFromTestPlane(0f, (0f, -1f, 0f));
		AssertDistanceFromTestPlane(2f, (0f, 1f, 0f));
		AssertDistanceFromTestPlane(-2f, (0f, -3f, 0f));

		AssertDistanceFromTestPlane(0f, (100f, -1f, 0f));
		AssertDistanceFromTestPlane(2f, (-100f, 1f, 100f));
		AssertDistanceFromTestPlane(-2f, (0f, -3f, -100f));

		Assert.AreEqual(1f, TestPlane.DistanceFromOrigin());
		Assert.AreEqual(1f, TestPlane.SignedDistanceFromOrigin());
		Assert.AreEqual(1f, TestPlane.Flipped.DistanceFromOrigin());
		Assert.AreEqual(-1f, TestPlane.Flipped.SignedDistanceFromOrigin());
	}

	[Test]
	public void ShouldCorrectlyDetermineWhetherPlaneFacesTowardsOrAwayFromLocations() {
		Assert.AreEqual(true, TestPlane.FacesTowards((100f, 1f, -100f)));
		Assert.AreEqual(true, TestPlane.FacesTowards((100f, 0f, -100f)));
		Assert.AreEqual(false, TestPlane.FacesTowards((100f, -1f, -100f)));
		Assert.AreEqual(false, TestPlane.FacesTowards((100f, -2f, -100f)));
		Assert.AreEqual(false, TestPlane.FacesTowards((100f, -3f, -100f)));

		Assert.AreEqual(false, TestPlane.FacesAwayFrom((100f, 1f, -100f)));
		Assert.AreEqual(false, TestPlane.FacesAwayFrom((100f, 0f, -100f)));
		Assert.AreEqual(false, TestPlane.FacesAwayFrom((100f, -1f, -100f)));
		Assert.AreEqual(true, TestPlane.FacesAwayFrom((100f, -2f, -100f)));
		Assert.AreEqual(true, TestPlane.FacesAwayFrom((100f, -3f, -100f)));

		Assert.AreEqual(true, TestPlane.FacesTowards((100f, 1f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(false, TestPlane.FacesTowards((100f, 0f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(false, TestPlane.FacesTowards((100f, -1f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(false, TestPlane.FacesTowards((100f, -2f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(false, TestPlane.FacesTowards((100f, -3f, -100f), planeThickness: 1.5f));

		Assert.AreEqual(false, TestPlane.FacesAwayFrom((100f, 1f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(false, TestPlane.FacesAwayFrom((100f, 0f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(false, TestPlane.FacesAwayFrom((100f, -1f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(false, TestPlane.FacesAwayFrom((100f, -2f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(true, TestPlane.FacesAwayFrom((100f, -3f, -100f), planeThickness: 1.5f));

		Assert.AreEqual(false, TestPlane.FacesAwayFromOrigin());
		Assert.AreEqual(true, TestPlane.FacesTowardsOrigin());
		Assert.AreEqual(false, TestPlane.FacesAwayFromOrigin(planeThickness: 1.5f));
		Assert.AreEqual(false, TestPlane.FacesTowardsOrigin(planeThickness: 1.5f));
	}

	[Test]
	public void ShouldCorrectlyDetermineWhetherAPointIsOnThePlane() {
		Assert.AreEqual(false, TestPlane.Contains((100f, 1f, -100f)));
		Assert.AreEqual(false, TestPlane.Contains((100f, 0f, -100f)));
		Assert.AreEqual(true, TestPlane.Contains((100f, -1f, -100f)));
		Assert.AreEqual(false, TestPlane.Contains((100f, -2f, -100f)));
		Assert.AreEqual(false, TestPlane.Contains((100f, -3f, -100f)));

		Assert.AreEqual(false, TestPlane.Contains((100f, 1f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(true, TestPlane.Contains((100f, 0f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(true, TestPlane.Contains((100f, -1f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(true, TestPlane.Contains((100f, -2f, -100f), planeThickness: 1.5f));
		Assert.AreEqual(false, TestPlane.Contains((100f, -3f, -100f), planeThickness: 1.5f));
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromOtherPlanes() {
		Assert.AreEqual(0f, TestPlane.DistanceFrom(new Plane(Direction.Backward, Location.Origin)));
		Assert.AreEqual(0f, TestPlane.DistanceFrom(new Plane(Direction.Forward, Location.Origin)));
		Assert.AreEqual(0f, TestPlane.DistanceFrom(new Plane(Direction.Left, Location.Origin)));
		Assert.AreEqual(0f, TestPlane.DistanceFrom(new Plane(Direction.Right, Location.Origin)));
		Assert.AreEqual(0f, TestPlane.DistanceFrom(new Plane(Direction.Up, (0f, -1f, 0f))));
		Assert.AreEqual(0f, TestPlane.DistanceFrom(new Plane(Direction.Down, (0f, -1f, 0f))));

		Assert.AreEqual(10f, TestPlane.DistanceFrom(new Plane(Direction.Up, (0f, -11f, 0f))));
		Assert.AreEqual(10f, TestPlane.DistanceFrom(new Plane(Direction.Down, (0f, 9f, 0f))));
		Assert.AreEqual(10f, TestPlane.DistanceFrom(new Plane(Direction.Down, (0f, -11f, 0f))));
		Assert.AreEqual(10f, TestPlane.DistanceFrom(new Plane(Direction.Up, (0f, 9f, 0f))));

		Assert.AreEqual(0f, TestPlane.DistanceFrom(new Plane((0.001f, 1f, 0f), Location.Origin)));

		// Squared
		Assert.AreEqual(0f, TestPlane.DistanceSquaredFrom(new Plane(Direction.Backward, Location.Origin)));
		Assert.AreEqual(0f, TestPlane.DistanceSquaredFrom(new Plane(Direction.Forward, Location.Origin)));
		Assert.AreEqual(0f, TestPlane.DistanceSquaredFrom(new Plane(Direction.Left, Location.Origin)));
		Assert.AreEqual(0f, TestPlane.DistanceSquaredFrom(new Plane(Direction.Right, Location.Origin)));
		Assert.AreEqual(0f, TestPlane.DistanceSquaredFrom(new Plane(Direction.Up, (0f, -1f, 0f))));
		Assert.AreEqual(0f, TestPlane.DistanceSquaredFrom(new Plane(Direction.Down, (0f, -1f, 0f))));

		Assert.AreEqual(100f, TestPlane.DistanceSquaredFrom(new Plane(Direction.Up, (0f, -11f, 0f))));
		Assert.AreEqual(100f, TestPlane.DistanceSquaredFrom(new Plane(Direction.Down, (0f, 9f, 0f))));
		Assert.AreEqual(100f, TestPlane.DistanceSquaredFrom(new Plane(Direction.Down, (0f, -11f, 0f))));
		Assert.AreEqual(100f, TestPlane.DistanceSquaredFrom(new Plane(Direction.Up, (0f, 9f, 0f))));

		Assert.AreEqual(0f, TestPlane.DistanceSquaredFrom(new Plane((0.001f, 1f, 0f), Location.Origin)));
	}

	[Test]
	public void ShouldCorrectlyTestForIntersectionWithOtherPlanes() {
		Assert.False(TestPlane.IsIntersectedBy(new Plane(Direction.Down, Location.Origin)));
		Assert.False(TestPlane.IsIntersectedBy(new Plane(Direction.Down, TestPlane.PointClosestToOrigin)));
		Assert.False(TestPlane.IsIntersectedBy(new Plane(Direction.Up, TestPlane.PointClosestToOrigin)));

		Assert.True(
			TestPlane.IsIntersectedBy(new Plane(Direction.Right, (-1f, 0f, 0f)))
		);
		Assert.True(
			TestPlane.IsIntersectedBy(new Plane((0f, 1f, 1f), (0f, 99f, 0f)))
		);
		Assert.True(
			new Plane((1f, 0f, 1f), Location.Origin).IsIntersectedBy(new Plane((-1f, 0f, 1f), Location.Origin))
		);
	}

	[Test]
	public void ShouldCorrectlyCalculateIntersectionWithOtherPlanes() {
		void AssertIntersection(Line expectedLine, Plane planeA, Plane planeB) {
			AssertToleranceEquals(expectedLine, planeA.IntersectionWith(planeB), TestTolerance);
			AssertToleranceEquals(expectedLine, planeA.Flipped.IntersectionWith(planeB), TestTolerance);
			AssertToleranceEquals(expectedLine, planeB.IntersectionWith(planeA), TestTolerance);
			AssertToleranceEquals(expectedLine, planeB.Flipped.IntersectionWith(planeA), TestTolerance);
		}

		Assert.AreEqual(null, TestPlane.IntersectionWith(new Plane(Direction.Down, Location.Origin)));
		Assert.AreEqual(null, TestPlane.IntersectionWith(new Plane(Direction.Down, TestPlane.PointClosestToOrigin)));
		Assert.AreEqual(null, TestPlane.IntersectionWith(new Plane(Direction.Up, TestPlane.PointClosestToOrigin)));

		AssertIntersection(
			new Line((-1f, -1f, 0f), Direction.Forward),
			TestPlane,
			new Plane(Direction.Right, (-1f, 0f, 0f))
		);
		AssertIntersection(
			new Line((0f, -1f, 100f), Direction.Left),
			TestPlane,
			new Plane((0f, 1f, 1f), (0f, 99f, 0f))
		);
		AssertIntersection(
			new Line(Location.Origin, Direction.Down),
			new Plane((1f, 0f, 1f), Location.Origin),
			new Plane((-1f, 0f, 1f), Location.Origin)
		);
	}

	[Test]
	public void ShouldCorrectlyProjectVectors() {
		Assert.AreEqual(new Vect(10f, 0f, -10f), TestPlane.ProjectionOf(new Vect(10f, 0f, -10f)));
		Assert.AreEqual(new Vect(10f, 0f, -10f), TestPlane.ProjectionOf(new Vect(10f, -20f, -10f)));
		Assert.AreEqual(new Vect(10f, 0f, -10f), TestPlane.ProjectionOf(new Vect(10f, 20f, -10f)));
		Assert.AreEqual(Vect.Zero, TestPlane.ProjectionOf(Vect.Zero));
		Assert.AreEqual(Vect.Zero, TestPlane.ProjectionOf(new Vect(0f, 1f, 0f)));
		Assert.AreEqual(Vect.Zero, TestPlane.ProjectionOf(new Vect(0f, -1f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyParallelizeVectors() {
		Assert.AreEqual(new Vect(10f, 0f, 10f).WithLength(MathF.Sqrt(300f)), TestPlane.ParallelizationOf(new Vect(10f, 10f, 10f)));
		Assert.AreEqual(new Vect(10f, 0f, 10f).WithLength(MathF.Sqrt(300f)), TestPlane.ParallelizationOf(new Vect(10f, -10f, 10f)));
		Assert.AreEqual(new Vect(-10f, 0f, -10f).WithLength(MathF.Sqrt(300f)), TestPlane.ParallelizationOf(new Vect(-10f, 10f, -10f)));
		Assert.AreEqual(new Vect(-10f, 0f, -10f).WithLength(MathF.Sqrt(300f)), TestPlane.ParallelizationOf(new Vect(-10f, -10f, -10f)));
		Assert.AreEqual(new Vect(10f, 0f, 10f).WithLength(MathF.Sqrt(300f)), TestPlane.FastParallelizationOf(new Vect(10f, 10f, 10f)));
		Assert.AreEqual(new Vect(10f, 0f, 10f).WithLength(MathF.Sqrt(300f)), TestPlane.FastParallelizationOf(new Vect(10f, -10f, 10f)));
		Assert.AreEqual(new Vect(-10f, 0f, -10f).WithLength(MathF.Sqrt(300f)), TestPlane.FastParallelizationOf(new Vect(-10f, 10f, -10f)));
		Assert.AreEqual(new Vect(-10f, 0f, -10f).WithLength(MathF.Sqrt(300f)), TestPlane.FastParallelizationOf(new Vect(-10f, -10f, -10f)));
		Assert.AreEqual(Vect.Zero, TestPlane.ParallelizationOf(Vect.Zero));
		Assert.AreEqual(null, TestPlane.ParallelizationOf(new Vect(0f, 1f, 0f)));
		Assert.AreEqual(null, TestPlane.ParallelizationOf(new Vect(0f, -1f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyParallelizeDirections() {
		AssertToleranceEquals(Direction.Left, TestPlane.ParallelizationOf(new Direction(1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Left, TestPlane.ParallelizationOf(new Direction(1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Right, TestPlane.ParallelizationOf(new Direction(-1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Right, TestPlane.ParallelizationOf(new Direction(-1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Forward, TestPlane.ParallelizationOf(new Direction(0f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Forward, TestPlane.ParallelizationOf(new Direction(0f, -1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Backward, TestPlane.ParallelizationOf(new Direction(0f, 1f, -1f)), TestTolerance);
		AssertToleranceEquals(Direction.Backward, TestPlane.ParallelizationOf(new Direction(0f, -1f, -1f)), TestTolerance);

		AssertToleranceEquals(Direction.Left, TestPlane.FastParallelizationOf(new Direction(1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Left, TestPlane.FastParallelizationOf(new Direction(1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Right, TestPlane.FastParallelizationOf(new Direction(-1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Right, TestPlane.FastParallelizationOf(new Direction(-1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Forward, TestPlane.FastParallelizationOf(new Direction(0f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Forward, TestPlane.FastParallelizationOf(new Direction(0f, -1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Backward, TestPlane.FastParallelizationOf(new Direction(0f, 1f, -1f)), TestTolerance);
		AssertToleranceEquals(Direction.Backward, TestPlane.FastParallelizationOf(new Direction(0f, -1f, -1f)), TestTolerance);

		AssertToleranceEquals(new Direction(1f, 0f, 1f), TestPlane.ParallelizationOf(new Direction(1f, 0f, 1f)), TestTolerance);
		AssertToleranceEquals(new Direction(1f, 0f, 1f), TestPlane.ParallelizationOf(new Direction(1f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(new Direction(1f, 0f, 1f), TestPlane.ParallelizationOf(new Direction(1f, -1f, 1f)), TestTolerance);
		AssertToleranceEquals(new Direction(-1f, 0f, -1f), TestPlane.ParallelizationOf(new Direction(-1f, 0f, -1f)), TestTolerance);
		AssertToleranceEquals(new Direction(-1f, 0f, -1f), TestPlane.ParallelizationOf(new Direction(-1f, 1f, -1f)), TestTolerance);
		AssertToleranceEquals(new Direction(-1f, 0f, -1f), TestPlane.ParallelizationOf(new Direction(-1f, -1f, -1f)), TestTolerance);

		AssertToleranceEquals(new Direction(1f, 0f, 1f), TestPlane.FastParallelizationOf(new Direction(1f, 0f, 1f)), TestTolerance);
		AssertToleranceEquals(new Direction(1f, 0f, 1f), TestPlane.FastParallelizationOf(new Direction(1f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(new Direction(1f, 0f, 1f), TestPlane.FastParallelizationOf(new Direction(1f, -1f, 1f)), TestTolerance);
		AssertToleranceEquals(new Direction(-1f, 0f, -1f), TestPlane.FastParallelizationOf(new Direction(-1f, 0f, -1f)), TestTolerance);
		AssertToleranceEquals(new Direction(-1f, 0f, -1f), TestPlane.FastParallelizationOf(new Direction(-1f, 1f, -1f)), TestTolerance);
		AssertToleranceEquals(new Direction(-1f, 0f, -1f), TestPlane.FastParallelizationOf(new Direction(-1f, -1f, -1f)), TestTolerance);

		AssertToleranceEquals(null, TestPlane.ParallelizationOf(Direction.Up), TestTolerance);
		AssertToleranceEquals(null, TestPlane.ParallelizationOf(Direction.Down), TestTolerance);
		AssertToleranceEquals(Direction.None, TestPlane.ParallelizationOf(Direction.None), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeVectors() {
		Assert.AreEqual(new Vect(0f, 10f, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.OrthogonalizationOf(new Vect(10f, 10f, 10f)));
		Assert.AreEqual(new Vect(0f, -10f, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.OrthogonalizationOf(new Vect(10f, -10f, 10f)));
		Assert.AreEqual(new Vect(0f, 10, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.OrthogonalizationOf(new Vect(-10f, 10f, -10f)));
		Assert.AreEqual(new Vect(0f, -10f, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.OrthogonalizationOf(new Vect(-10f, -10f, -10f)));
		Assert.AreEqual(new Vect(0f, 1f, 0f), TestPlane.OrthogonalizationOf(new Vect(0f, 1f, 0f)));
		Assert.AreEqual(new Vect(0f, -1f, 0f), TestPlane.OrthogonalizationOf(new Vect(0f, -1f, 0f)));

		Assert.AreEqual(new Vect(0f, 10f, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.FastOrthogonalizationOf(new Vect(10f, 10f, 10f)));
		Assert.AreEqual(new Vect(0f, -10f, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.FastOrthogonalizationOf(new Vect(10f, -10f, 10f)));
		Assert.AreEqual(new Vect(0f, 10, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.FastOrthogonalizationOf(new Vect(-10f, 10f, -10f)));
		Assert.AreEqual(new Vect(0f, -10f, 0f).WithLength(MathF.Sqrt(300f)), TestPlane.FastOrthogonalizationOf(new Vect(-10f, -10f, -10f)));
		Assert.AreEqual(new Vect(0f, 1f, 0f), TestPlane.FastOrthogonalizationOf(new Vect(0f, 1f, 0f)));
		Assert.AreEqual(new Vect(0f, -1f, 0f), TestPlane.FastOrthogonalizationOf(new Vect(0f, -1f, 0f)));

		Assert.AreEqual(Vect.Zero, TestPlane.OrthogonalizationOf(Vect.Zero));
		Assert.AreEqual(null, TestPlane.OrthogonalizationOf(new Vect(1f, 0f, 0f)));
		Assert.AreEqual(null, TestPlane.OrthogonalizationOf(new Vect(0f, 0f, 1f)));
		Assert.AreEqual(null, TestPlane.OrthogonalizationOf(new Vect(-1f, 0f, 0f)));
		Assert.AreEqual(null, TestPlane.OrthogonalizationOf(new Vect(0f, 0f, -1f)));
		Assert.AreEqual(null, TestPlane.OrthogonalizationOf(new Vect(1f, 0f, -1f)));
		Assert.AreEqual(null, TestPlane.OrthogonalizationOf(new Vect(-1f, 0f, 1f)));
	}

	[Test]
	public void ShouldCorrectlyOrthogonalizeDirections() {
		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(new Direction(1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.OrthogonalizationOf(new Direction(1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(new Direction(-1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.OrthogonalizationOf(new Direction(-1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(new Direction(0f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.OrthogonalizationOf(new Direction(0f, -1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(new Direction(0f, 1f, -1f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.OrthogonalizationOf(new Direction(0f, -1f, -1f)), TestTolerance);

		AssertToleranceEquals(null, TestPlane.OrthogonalizationOf(new Direction(1f, 0f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(new Direction(1f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.OrthogonalizationOf(new Direction(1f, -1f, 1f)), TestTolerance);
		AssertToleranceEquals(null, TestPlane.OrthogonalizationOf(new Direction(-1f, 0f, -1f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(new Direction(-1f, 1f, -1f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.OrthogonalizationOf(new Direction(-1f, -1f, -1f)), TestTolerance);

		AssertToleranceEquals(Direction.Up, TestPlane.FastOrthogonalizationOf(new Direction(1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.FastOrthogonalizationOf(new Direction(1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.FastOrthogonalizationOf(new Direction(-1f, 1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.FastOrthogonalizationOf(new Direction(-1f, -1f, 0f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.FastOrthogonalizationOf(new Direction(0f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.FastOrthogonalizationOf(new Direction(0f, -1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.FastOrthogonalizationOf(new Direction(0f, 1f, -1f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.FastOrthogonalizationOf(new Direction(0f, -1f, -1f)), TestTolerance);

		AssertToleranceEquals(Direction.Up, TestPlane.FastOrthogonalizationOf(new Direction(1f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.FastOrthogonalizationOf(new Direction(1f, -1f, 1f)), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.FastOrthogonalizationOf(new Direction(-1f, 1f, -1f)), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.FastOrthogonalizationOf(new Direction(-1f, -1f, -1f)), TestTolerance);

		AssertToleranceEquals(Direction.Up, TestPlane.OrthogonalizationOf(Direction.Up), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.OrthogonalizationOf(Direction.Down), TestTolerance);
		AssertToleranceEquals(Direction.Up, TestPlane.FastOrthogonalizationOf(Direction.Up), TestTolerance);
		AssertToleranceEquals(Direction.Down, TestPlane.FastOrthogonalizationOf(Direction.Down), TestTolerance);
		AssertToleranceEquals(Direction.None, TestPlane.OrthogonalizationOf(Direction.None), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyConvertBetween3DAnd2D() {
		// 3D -> 2D
		AssertToleranceEquals(
			new XYPair<float>(1f, -1f),
			TestPlane.CreateDimensionConverter(Location.Origin, new(1f, 0f, 0f), new(0f, 0f, 1f)).Convert((1f, 1f, -1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(-2f, 2f),
			TestPlane.CreateDimensionConverter(Location.Origin, new(-1f, 0f, 0f), new(0f, 0f, -1f)).Convert((2f, -100f, -2f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(-1f, 1f),
			TestPlane.CreateDimensionConverter(Location.Origin, new(-1f, 0f, 0f), new(0f, 0f, -1f)).Convert((1f, 1f, -1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(2f, -2f),
			TestPlane.CreateDimensionConverter(Location.Origin, new(1f, 0f, 0f), new(0f, 0f, 1f)).Convert((2f, -100f, -2f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new XYPair<float>(1f, -1f),
			TestPlane.CreateDimensionConverter(Location.Origin, new(1f, 1f, 0f), new(0f, -1f, 1f)).Convert((1f, 1f, -1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(-2f, 2f),
			TestPlane.CreateDimensionConverter(Location.Origin, new(-1f, 1f, 0f), new(0f, -1f, -1f)).Convert((2f, -100f, -2f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(-1f, 1f),
			TestPlane.CreateDimensionConverter(Location.Origin, new(-1f, -1f, 0f), new(0f, 1f, -1f)).Convert((1f, 1f, -1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(2f, -2f),
			TestPlane.CreateDimensionConverter(Location.Origin, new(1f, -1f, 0f), new(0f, 1f, 1f)).Convert((2f, -100f, -2f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new XYPair<float>(-2f, -4f),
			TestPlane.CreateDimensionConverter(new Location(3f, 0f, 3f), new(1f, 0f, 0f), new(0f, 0f, 1f)).Convert((1f, 1f, -1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(-5f, -1f),
			TestPlane.CreateDimensionConverter(new Location(-3f, 0f, -3f), new(-1f, 0f, 0f), new(0f, 0f, -1f)).Convert((2f, -100f, -2f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(2f, 4f),
			TestPlane.CreateDimensionConverter(new Location(3f, 0f, 3f), new(-1f, 0f, 0f), new(0f, 0f, -1f)).Convert((1f, 1f, -1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(5f, 1f),
			TestPlane.CreateDimensionConverter(new Location(-3f, 0f, -3f), new(1f, 0f, 0f), new(0f, 0f, 1f)).Convert((2f, -100f, -2f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new XYPair<float>(-2f, -4f),
			TestPlane.CreateDimensionConverter(new Location(3f, 10f, 3f), new(1f, 1f, 0f), new(0f, -1f, 1f)).Convert((1f, 1f, -1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(-5f, -1f),
			TestPlane.CreateDimensionConverter(new Location(-3f, 10f, -3f), new(-1f, 1f, 0f), new(0f, -1f, -1f)).Convert((2f, -100f, -2f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(2f, 4f),
			TestPlane.CreateDimensionConverter(new Location(3f, -10f, 3f), new(-1f, -1f, 0f), new(0f, 1f, -1f)).Convert((1f, 1f, -1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(5f, 1f),
			TestPlane.CreateDimensionConverter(new Location(-3f, -10f, -3f), new(1f, -1f, 0f), new(0f, 1f, 1f)).Convert((2f, -100f, -2f)),
			TestTolerance
		);

		AssertToleranceEquals(
			new XYPair<float>(0f, 0f),
			new Plane(new Direction(1f, 1f, 1f), 10f).CreateDimensionConverter(new Vect(1f, 1f, 1f).WithLength(10f).AsLocation(), new Direction(1f, -1f, 0f), new Direction(-1f, 0f, 1f)).Convert((20f, 20f, 20f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(1f, 0f),
			new Plane(new Direction(1f, 1f, 1f), 10f)
				.CreateDimensionConverter(
					new Vect(1f, 1f, 1f).WithLength(10f).AsLocation(),
					new Direction(1f, -1f, 0f),
					new Direction(-0.408f, -0.408f, 0.816f)
				).Convert(new Location(20f, 20f, 20f) + new Vect(1f, -1f, 0f).WithLength(1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(0f, 1f),
			new Plane(new Direction(1f, 1f, 1f), 10f)
				.CreateDimensionConverter(
					new Vect(1f, 1f, 1f).WithLength(10f).AsLocation(),
					new Direction(1f, -1f, 0f),
					new Direction(-0.408f, -0.408f, 0.816f)
				).Convert(new Location(20f, 20f, 20f) + new Vect(-0.408f, -0.408f, 0.816f).WithLength(1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(-3f, 0f),
			new Plane(new Direction(1f, 1f, 1f), 10f)
				.CreateDimensionConverter(
					new Vect(1f, 1f, 1f).WithLength(10f).AsLocation(),
					new Direction(1f, -1f, 0f),
					new Direction(-0.408f, -0.408f, 0.816f)
				).Convert(new Location(20f, 20f, 20f) + new Vect(1f, -1f, 0f).WithLength(-3f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(0f, -3f),
			new Plane(new Direction(1f, 1f, 1f), 10f)
				.CreateDimensionConverter(
					new Vect(1f, 1f, 1f).WithLength(10f).AsLocation(),
					new Direction(1f, -1f, 0f),
					new Direction(-0.408f, -0.408f, 0.816f)
				).Convert(new Location(20f, 20f, 20f) + new Vect(-0.408f, -0.408f, 0.816f).WithLength(-3f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(0f, 0f),
			new Plane(new Direction(1f, 1f, 1f), 10f)
				.CreateDimensionConverter(
					new Vect(1f, 1f, 1f).WithLength(10f).AsLocation() + new Direction(1f, -1f, 0f) * -3f,
					new Direction(1f, -1f, 0f),
					new Direction(-0.408f, -0.408f, 0.816f)
				).Convert(new Location(20f, 20f, 20f) + new Vect(1f, -1f, 0f).WithLength(-3f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new XYPair<float>(3f, -3f),
			new Plane(new Direction(1f, 1f, 1f), 10f)
				.CreateDimensionConverter(
					new Vect(1f, 1f, 1f).WithLength(10f).AsLocation() + new Direction(1f, -1f, 0f) * -3f,
					new Direction(1f, -1f, 0f),
					new Direction(-0.408f, -0.408f, 0.816f)
				).Convert(new Location(20f, 20f, 20f) + new Vect(-0.408f, -0.408f, 0.816f).WithLength(-3f)),
			TestTolerance
		);

		// 2D -> 3D
		AssertToleranceEquals(
			new Location(1f, -1f, -1f),
			TestPlane.CreateDimensionConverter(new Location(3f, 10f, 3f), new(1f, 1f, 0f), new(0f, -1f, 1f)).Convert((-2f, -4f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(2f, -1f, -2f),
			TestPlane.CreateDimensionConverter(new Location(-3f, 10f, -3f), new(-1f, 1f, 0f), new(0f, -1f, -1f)).Convert((-5f, -1f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(1f, -1f, -1f),
			TestPlane.CreateDimensionConverter(new Location(3f, -10f, 3f), new(-1f, -1f, 0f), new(0f, 1f, -1f)).Convert((2f, 4f)),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(2f, -1f, -2f),
			TestPlane.CreateDimensionConverter(new Location(-3f, -10f, -3f), new(1f, -1f, 0f), new(0f, 1f, 1f)).Convert((5f, 1f)),
			TestTolerance
		);
	}

	[Test]
	public void ConvenienceMethodsShouldCorrectlyConvertBetween2DAnd3D() {
		Assert.AreEqual(MathF.Sqrt(200f), new Location(10f, 10f, 10f).ProjectedTo2DOn(TestPlane).ToVector2().Length(), TestTolerance);
		Assert.AreEqual(MathF.Sqrt(200f), new Location(-10f, -10f, -10f).ProjectedTo2DOn(TestPlane).ToVector2().Length(), TestTolerance);
		Assert.AreEqual(new Location(10f, 10f, 10f).ProjectedTo2DOn(TestPlane).X, -new Location(-10f, -10f, -10f).ProjectedTo2DOn(TestPlane).X, TestTolerance);
		Assert.AreEqual(new Location(10f, 10f, 10f).ProjectedTo2DOn(TestPlane).Y, -new Location(-10f, -10f, -10f).ProjectedTo2DOn(TestPlane).Y, TestTolerance);

		AssertToleranceEquals(
			new Location(10f, -1f, 10f),
			new Location(10f, 10f, 10f).ProjectedTo2DOn(TestPlane).HolographedTo3DOn(TestPlane),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(-10f, -1f, -10f),
			new Location(-10f, -10f, -10f).ProjectedTo2DOn(TestPlane).HolographedTo3DOn(TestPlane),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(10f, 10f, 10f),
			new Location(10f, 10f, 10f).ProjectedTo2DOn(TestPlane).HolographedTo3DOn(TestPlane, 11f),
			TestTolerance
		);
		AssertToleranceEquals(
			new Location(-10f, -10f, -10f),
			new Location(-10f, -10f, -10f).ProjectedTo2DOn(TestPlane).HolographedTo3DOn(TestPlane, -9f),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyConstructDimensionConverters() {
		var converter = TestPlane.CreateDimensionConverter();
		Assert.AreEqual(TestPlane.PointClosestToOrigin, converter.Origin);
		Assert.AreEqual(TestPlane.Normal, converter.PlaneNormal);
		AssertToleranceEquals(new Angle(90f), converter.XBasis ^ converter.YBasis, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.XBasis ^ TestPlane.Normal, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.YBasis ^ TestPlane.Normal, TestTolerance);

		converter = TestPlane.CreateDimensionConverter(new Location(-100f, 1000f, 33f));
		Assert.AreEqual(new Location(-100f, -1f, 33f), converter.Origin);
		Assert.AreEqual(TestPlane.Normal, converter.PlaneNormal);
		AssertToleranceEquals(new Angle(90f), converter.XBasis ^ converter.YBasis, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.XBasis ^ TestPlane.Normal, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.YBasis ^ TestPlane.Normal, TestTolerance);

		converter = TestPlane.CreateDimensionConverter(new Location(-100f, 1000f, 33f), new Direction(1f, 1f, -1f));
		Assert.AreEqual(new Location(-100f, -1f, 33f), converter.Origin);
		Assert.AreEqual(TestPlane.Normal, converter.PlaneNormal);
		AssertToleranceEquals(new Direction(1f, 0f, -1f), converter.XBasis, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.XBasis ^ converter.YBasis, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.XBasis ^ TestPlane.Normal, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.YBasis ^ TestPlane.Normal, TestTolerance);

		converter = TestPlane.CreateDimensionConverter(new Location(-100f, 1000f, 33f), new Direction(1f, 1f, -1f), new Direction(1f, -1f, 0.8f));
		Assert.AreEqual(new Location(-100f, -1f, 33f), converter.Origin);
		Assert.AreEqual(TestPlane.Normal, converter.PlaneNormal);
		AssertToleranceEquals(new Direction(1f, 0f, -1f), converter.XBasis, TestTolerance);
		AssertToleranceEquals(new Direction(1f, 0f, 1f), converter.YBasis, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.XBasis ^ converter.YBasis, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.XBasis ^ TestPlane.Normal, TestTolerance);
		AssertToleranceEquals(new Angle(90f), converter.YBasis ^ TestPlane.Normal, TestTolerance);

		var testList = new List<Direction>();
		for (var x = -5f; x <= 5f; x += 1f) {
			for (var y = -5f; y <= 5f; y += 1f) {
				for (var z = -5f; z <= 5f; z += 1f) {
					if (x == 0f && y == 0f && z == 0f) continue;
					testList.Add(new(x, y, z));
				}
			}
		}

		for (var i = 0; i < testList.Count; ++i) {
			var x = testList[i];
			if (x.Equals(Direction.None, TestTolerance)) continue;
			for (var j = i + 1; j < testList.Count; ++j) {
				var y = testList[j];
				if (y.Equals(Direction.None, TestTolerance)) continue;
				if (x.AngleTo(TestPlane).Equals(90f, TestTolerance)) continue;
				if (y.AngleTo(TestPlane).Equals(90f, TestTolerance)) continue;
				if (1f - MathF.Abs(x.ParallelizedWith(TestPlane)!.Value.Dot(y.ParallelizedWith(TestPlane)!.Value)) < TestTolerance) continue;

				try {
					converter = TestPlane.CreateDimensionConverter(TestPlane.PointClosestToOrigin, x, y);
					AssertToleranceEquals(new Angle(90f), converter.XBasis ^ converter.YBasis, TestTolerance);
					AssertToleranceEquals(new Angle(90f), converter.XBasis ^ TestPlane.Normal, TestTolerance);
					AssertToleranceEquals(new Angle(90f), converter.YBasis ^ TestPlane.Normal, TestTolerance);
				}
				catch {
					Console.WriteLine("X: " + x.ToStringDescriptive() + " | Y: " + y.ToStringDescriptive());
					throw;
				}
			}
		}

		// This section checks that the dimension converters are always constructed with perpendicular basis directions even if we pass the most degenerate possible arguments
		void AssertOrthogonality(Plane.DimensionConverter dc) {
			Assert.IsTrue(dc.XBasis.IsOrthogonalTo(dc.PlaneNormal));
			Assert.IsTrue(dc.YBasis.IsOrthogonalTo(dc.PlaneNormal));
			Assert.IsTrue(dc.XBasis.IsOrthogonalTo(dc.YBasis));
		}

		AssertOrthogonality(TestPlane.CreateDimensionConverter(TestPlane.PointClosestToOrigin, Direction.Up));
		AssertOrthogonality(TestPlane.CreateDimensionConverter(TestPlane.PointClosestToOrigin, Direction.Down));
		AssertOrthogonality(TestPlane.CreateDimensionConverter(TestPlane.PointClosestToOrigin, Direction.Up, Direction.Right));
		AssertOrthogonality(TestPlane.CreateDimensionConverter(TestPlane.PointClosestToOrigin, Direction.Down, Direction.Right));
		AssertOrthogonality(TestPlane.CreateDimensionConverter(TestPlane.PointClosestToOrigin, Direction.Right, Direction.Right));
		AssertOrthogonality(TestPlane.CreateDimensionConverter(TestPlane.PointClosestToOrigin, Direction.Left, Direction.Right));
		AssertOrthogonality(TestPlane.CreateDimensionConverter(TestPlane.PointClosestToOrigin, Direction.Left, Direction.Up));
		AssertOrthogonality(TestPlane.CreateDimensionConverter(TestPlane.PointClosestToOrigin, Direction.Left, Direction.Down));
	}

	[Test]
	public void ShouldCorrectlyClamp() {
		AssertToleranceEquals(
			new Plane((0f, 1f, 1f), (0f, 5f, 5f)),
			new Plane((0f, 1f, 1f), (0f, 10f, 10f)).Clamp(new Plane(Direction.Up, (0f, 10f, 0f)), new Plane(Direction.Forward, (0f, 0f, 10f))),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineParallelismWithDirectionsAndVects() {
		void AssertCombination(bool expectation, Direction normal, Direction d, Angle? tolerance) {
			var p = new Plane(normal, Location.Origin);
			var v = d * 10f;

			if (tolerance == null) {
				Assert.AreEqual(expectation, p.IsApproximatelyParallelTo(d));
				Assert.AreEqual(expectation, p.IsApproximatelyParallelTo(v));
				Assert.AreEqual(expectation, p.IsApproximatelyParallelTo(-d));
				Assert.AreEqual(expectation, p.IsApproximatelyParallelTo(-v));
				Assert.AreEqual(expectation, (-p).IsApproximatelyParallelTo(d));
				Assert.AreEqual(expectation, (-p).IsApproximatelyParallelTo(v));
				Assert.AreEqual(expectation, (-p).IsApproximatelyParallelTo(-d));
				Assert.AreEqual(expectation, (-p).IsApproximatelyParallelTo(-v));

				Assert.AreEqual(expectation, p.IsParallelTo(d));
				Assert.AreEqual(expectation, p.IsParallelTo(v));
				Assert.AreEqual(expectation, p.IsParallelTo(-d));
				Assert.AreEqual(expectation, p.IsParallelTo(-v));
				Assert.AreEqual(expectation, (-p).IsParallelTo(d));
				Assert.AreEqual(expectation, (-p).IsParallelTo(v));
				Assert.AreEqual(expectation, (-p).IsParallelTo(-d));
				Assert.AreEqual(expectation, (-p).IsParallelTo(-v));
			}
			else {
				Assert.AreEqual(expectation, p.IsApproximatelyParallelTo(d, tolerance.Value));
				Assert.AreEqual(expectation, p.IsApproximatelyParallelTo(v, tolerance.Value));
				Assert.AreEqual(expectation, p.IsApproximatelyParallelTo(-d, tolerance.Value));
				Assert.AreEqual(expectation, p.IsApproximatelyParallelTo(-v, tolerance.Value));
				Assert.AreEqual(expectation, (-p).IsApproximatelyParallelTo(d, tolerance.Value));
				Assert.AreEqual(expectation, (-p).IsApproximatelyParallelTo(v, tolerance.Value));
				Assert.AreEqual(expectation, (-p).IsApproximatelyParallelTo(-d, tolerance.Value));
				Assert.AreEqual(expectation, (-p).IsApproximatelyParallelTo(-v, tolerance.Value));
			}
		}

		AssertCombination(false, Direction.Up, Direction.Down, null);
		AssertCombination(false, (1f, 1f, 1f), Direction.Up, null);
		AssertCombination(true, Direction.Up, Direction.Right, null);
		AssertCombination(true, Direction.Down, Direction.Backward, null);
		AssertCombination(true, Direction.Left, Direction.Up, null);
		AssertCombination(true, Direction.Left, Direction.Down, null);
		AssertCombination(false, Direction.Forward, Direction.None, null);

		AssertCombination(false, Direction.Up, Direction.Down, 89f);
		AssertCombination(false, Direction.Up, Direction.Up, 89f);
		AssertCombination(true, Direction.Up, Direction.Down, 90f);
		AssertCombination(true, Direction.Up, Direction.Up, 90f);
		AssertCombination(true, Direction.Left, Direction.Up, 0f);
		AssertCombination(true, Direction.Left, Direction.Down, 0f);

		var testList = new List<Direction>();
		for (var x = -3f; x <= 3f; x += 1f) {
			for (var y = -3f; y <= 3f; y += 1f) {
				for (var z = -3f; z <= 3f; z += 1f) {
					testList.Add(new(x, y, z));
				}
			}
		}

		for (var i = 0; i < testList.Count; ++i) {
			var normal = testList[i];
			if (normal == Direction.None) continue;

			for (var j = i; j < testList.Count; ++j) {
				var dir = testList[j];
				if (dir == Direction.None) {
					AssertCombination(false, normal, dir, null);
					AssertCombination(false, normal, dir, 0f);
					AssertCombination(false, normal, dir, 45f);
					AssertCombination(false, normal, dir, 90f);
					AssertCombination(false, normal, dir, 180f);
					continue;
				}

				var angle = new Plane(normal, Location.Origin).AngleTo(dir);
				try {
					if (angle == Angle.Zero) {
						AssertCombination(true, normal, dir, null);
						AssertCombination(true, normal, dir, TestTolerance);
					}
					else {
						AssertCombination(true, normal, dir, angle + 0.1f);
						AssertCombination(false, normal, dir, angle - 0.1f);
					}
				}
				catch {
					Console.WriteLine("Normal: " + normal.ToStringDescriptive());
					Console.WriteLine("Dir: " + dir.ToStringDescriptive());
					Console.WriteLine("Angle: " + angle);
					throw;
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyDetermineOrthogonalityWithDirectionsAndVects() {
		void AssertCombination(bool expectation, Direction normal, Direction d, Angle? tolerance) {
			var p = new Plane(normal, Location.Origin);
			var v = d * 10f;

			if (tolerance == null) {
				Assert.AreEqual(expectation, p.IsApproximatelyOrthogonalTo(d));
				Assert.AreEqual(expectation, p.IsApproximatelyOrthogonalTo(v));
				Assert.AreEqual(expectation, p.IsApproximatelyOrthogonalTo(-d));
				Assert.AreEqual(expectation, p.IsApproximatelyOrthogonalTo(-v));
				Assert.AreEqual(expectation, (-p).IsApproximatelyOrthogonalTo(d));
				Assert.AreEqual(expectation, (-p).IsApproximatelyOrthogonalTo(v));
				Assert.AreEqual(expectation, (-p).IsApproximatelyOrthogonalTo(-d));
				Assert.AreEqual(expectation, (-p).IsApproximatelyOrthogonalTo(-v));

				Assert.AreEqual(expectation, p.IsOrthogonalTo(d));
				Assert.AreEqual(expectation, p.IsOrthogonalTo(v));
				Assert.AreEqual(expectation, p.IsOrthogonalTo(-d));
				Assert.AreEqual(expectation, p.IsOrthogonalTo(-v));
				Assert.AreEqual(expectation, (-p).IsOrthogonalTo(d));
				Assert.AreEqual(expectation, (-p).IsOrthogonalTo(v));
				Assert.AreEqual(expectation, (-p).IsOrthogonalTo(-d));
				Assert.AreEqual(expectation, (-p).IsOrthogonalTo(-v));
			}
			else {
				Assert.AreEqual(expectation, p.IsApproximatelyOrthogonalTo(d, tolerance.Value));
				Assert.AreEqual(expectation, p.IsApproximatelyOrthogonalTo(v, tolerance.Value));
				Assert.AreEqual(expectation, p.IsApproximatelyOrthogonalTo(-d, tolerance.Value));
				Assert.AreEqual(expectation, p.IsApproximatelyOrthogonalTo(-v, tolerance.Value));
				Assert.AreEqual(expectation, (-p).IsApproximatelyOrthogonalTo(d, tolerance.Value));
				Assert.AreEqual(expectation, (-p).IsApproximatelyOrthogonalTo(v, tolerance.Value));
				Assert.AreEqual(expectation, (-p).IsApproximatelyOrthogonalTo(-d, tolerance.Value));
				Assert.AreEqual(expectation, (-p).IsApproximatelyOrthogonalTo(-v, tolerance.Value));
			}
		}

		AssertCombination(false, Direction.Left, Direction.Down, null);
		AssertCombination(false, (1f, 1f, 1f), Direction.Up, null);
		AssertCombination(true, Direction.Right, Direction.Right, null);
		AssertCombination(true, Direction.Forward, Direction.Backward, null);
		AssertCombination(true, Direction.Down, Direction.Up, null);
		AssertCombination(true, Direction.Down, Direction.Down, null);
		AssertCombination(false, Direction.Forward, Direction.None, null);

		AssertCombination(false, Direction.Left, Direction.Down, 89f);
		AssertCombination(false, Direction.Left, Direction.Up, 89f);
		AssertCombination(true, Direction.Left, Direction.Down, 90f);
		AssertCombination(true, Direction.Left, Direction.Up, 90f);
		AssertCombination(true, Direction.Up, Direction.Down, 0f);
		AssertCombination(true, Direction.Down, Direction.Up, 0f);

		var testList = new List<Direction>();
		for (var x = -3f; x <= 3f; x += 1f) {
			for (var y = -3f; y <= 3f; y += 1f) {
				for (var z = -3f; z <= 3f; z += 1f) {
					testList.Add(new(x, y, z));
				}
			}
		}

		for (var i = 0; i < testList.Count; ++i) {
			var normal = testList[i];
			if (normal == Direction.None) continue;

			for (var j = i; j < testList.Count; ++j) {
				var dir = testList[j];
				if (dir == Direction.None) {
					AssertCombination(false, normal, dir, null);
					AssertCombination(false, normal, dir, 0f);
					AssertCombination(false, normal, dir, 45f);
					AssertCombination(false, normal, dir, 90f);
					AssertCombination(false, normal, dir, 180f);
					continue;
				}

				var angle = new Plane(normal, Location.Origin).AngleTo(dir);
				try {
					if (angle == Angle.QuarterCircle) {
						AssertCombination(true, normal, dir, null);
						AssertCombination(true, normal, dir, 0.1f);
					}
					else {
						AssertCombination(true, normal, dir, (Angle.QuarterCircle - angle) + 0.1f);
						AssertCombination(false, normal, dir, (Angle.QuarterCircle - angle) - 0.1f);
					}
				}
				catch {
					Console.WriteLine("Normal: " + normal.ToStringDescriptive());
					Console.WriteLine("Dir: " + dir.ToStringDescriptive());
					Console.WriteLine("Angle: " + angle);
					throw;
				}
			}
		}
	}

	[Test]
	public void ShouldCorrectlyImplementMirrorMethods() {
		AssertMirrorMethod<Plane, Location>((a, b) => a.DistanceFrom(b));
		AssertMirrorMethod<Plane, Location, IDistanceMeasurable<Location>, IDistanceMeasurable<Plane>>((p, l) => p.DistanceSquaredFrom(l), (l, p) => l.DistanceSquaredFrom(p));
		AssertMirrorMethod<Plane, Location>((a, b) => a.SignedDistanceFrom(b));
		AssertMirrorMethod<Plane, Location>((p, l) => p.Contains(l), (l, p) => l.IsContainedWithin(p));
		AssertMirrorMethod<Plane, Location>((p, l) => p.Contains(l, 10f), (l, p) => l.IsContainedWithin(p, 10f));
		AssertMirrorMethod<Plane, Location>((p, l) => p.PointClosestTo(l), (l, p) => l.ClosestPointOn(p));
		AssertMirrorMethod<Plane, Location>((p, l) => p.ProjectionTo2DOf(l), (l, p) => l.ProjectedTo2DOn(p));

		AssertMirrorMethod<Plane, Direction>((a, b) => a.AngleTo(b));
		AssertMirrorMethod<Plane, Direction>((a, b) => a.SignedAngleTo(b));
		AssertMirrorMethod<Plane, Direction>((a, b) => a.IncidentAngleWith(b));
		AssertMirrorMethod<Plane, Direction>((a, b) => a.FastIncidentAngleWith(b));
		AssertMirrorMethod<Plane, Direction>((p, d) => p.ReflectionOf(d), (d, p) => d.ReflectedBy(p));
		AssertMirrorMethod<Plane, Direction>((p, d) => p.FastReflectionOf(d), (d, p) => d.FastReflectedBy(p));
		AssertMirrorMethod<Plane, Direction>((p, d) => p.ParallelizationOf(d), (d, p) => d.ParallelizedWith(p));
		AssertMirrorMethod<Plane, Direction>((p, d) => p.FastParallelizationOf(d), (d, p) => d.FastParallelizedWith(p));
		AssertMirrorMethod<Plane, Direction>((p, d) => p.OrthogonalizationOf(d), (d, p) => d.OrthogonalizedAgainst(p));
		AssertMirrorMethod<Plane, Direction>((p, d) => p.FastOrthogonalizationOf(d), (d, p) => d.FastOrthogonalizedAgainst(p));
		AssertMirrorMethod<Plane, Direction>((a, b) => a.IsParallelTo(b));
		AssertMirrorMethod<Plane, Direction>((a, b) => a.IsApproximatelyParallelTo(b));
		AssertMirrorMethod<Plane, Direction>((a, b) => a.IsApproximatelyParallelTo(b, new Angle(10f)));
		AssertMirrorMethod<Plane, Direction>((a, b) => a.IsOrthogonalTo(b));
		AssertMirrorMethod<Plane, Direction>((a, b) => a.IsApproximatelyOrthogonalTo(b));
		AssertMirrorMethod<Plane, Direction>((a, b) => a.IsApproximatelyOrthogonalTo(b, new Angle(10f)));

		AssertMirrorMethod<Plane, Vect>((a, b) => a.AngleTo(b));
		AssertMirrorMethod<Plane, Vect>((a, b) => a.SignedAngleTo(b));
		AssertMirrorMethod<Plane, Vect>((a, b) => a.IncidentAngleWith(b));
		AssertMirrorMethod<Plane, Vect>((a, b) => a.FastIncidentAngleWith(b));
		AssertMirrorMethod<Plane, Vect>((p, v) => p.ReflectionOf(v), (v, p) => v.ReflectedBy(p));
		AssertMirrorMethod<Plane, Vect>((p, v) => p.FastReflectionOf(v), (v, p) => v.FastReflectedBy(p));
		AssertMirrorMethod<Plane, Vect>((p, v) => p.ParallelizationOf(v), (v, p) => v.ParallelizedWith(p));
		AssertMirrorMethod<Plane, Vect>((p, v) => p.FastParallelizationOf(v), (v, p) => v.FastParallelizedWith(p));
		AssertMirrorMethod<Plane, Vect>((p, v) => p.OrthogonalizationOf(v), (v, p) => v.OrthogonalizedAgainst(p));
		AssertMirrorMethod<Plane, Vect>((p, v) => p.FastOrthogonalizationOf(v), (v, p) => v.FastOrthogonalizedAgainst(p));
		AssertMirrorMethod<Plane, Vect>((p, v) => p.ProjectionOf(v), (v, p) => v.ProjectedOnTo(p));
		AssertMirrorMethod<Plane, Vect, IProjectionTarget<Vect>, IProjectable<Vect, Plane>>((p, v) => p.FastProjectionOf(v), (v, p) => v.FastProjectedOnTo(p));
		AssertMirrorMethod<Plane, Vect, IProjectionTarget<Vect>, IProjectable<Vect, Plane>>((p, v) => p.ProjectionOf(v), (v, p) => v.ProjectedOnTo(p));
		AssertMirrorMethod<Plane, Vect>((a, b) => a.IsParallelTo(b));
		AssertMirrorMethod<Plane, Vect>((a, b) => a.IsApproximatelyParallelTo(b));
		AssertMirrorMethod<Plane, Vect>((a, b) => a.IsApproximatelyParallelTo(b, new Angle(10f)));
		AssertMirrorMethod<Plane, Vect>((a, b) => a.IsOrthogonalTo(b));
		AssertMirrorMethod<Plane, Vect>((a, b) => a.IsApproximatelyOrthogonalTo(b));
		AssertMirrorMethod<Plane, Vect>((a, b) => a.IsApproximatelyOrthogonalTo(b, new Angle(10f)));

		AssertMirrorMethod<Plane, XYPair<float>>((p, xy) => p.HolographTo3DOf(xy), (xy, p) => xy.HolographedTo3DOn(p));
		AssertMirrorMethod<Plane, XYPair<float>>((p, xy) => p.HolographTo3DOf(xy, 100f), (xy, p) => xy.HolographedTo3DOn(p, 100f));
	}
}