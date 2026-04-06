// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
class PositionableShapeTest {
	const float TestTolerance = 0.01f;
	static readonly PositionableConvexShape<Sphere> TestShape = new(new Sphere(7.4f), new Location(1f, -2f, 3f));

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConstruct() {
		Assert.AreEqual(new Sphere(7.4f), TestShape.BaseShape);
		Assert.AreEqual(new Location(1f, -2f, 3f), TestShape.Position);
	}

	[Test]
	public void ShouldCorrectlyTranslateBetweenWorldAndShapeSpace() {
		Assert.AreEqual(Location.Origin, TestShape.TranslateToShapeSpace(new Location(1f, -2f, 3f)));
		Assert.AreEqual(new Location(1f, -2f, 3f), TestShape.TranslateToWorldSpace(Location.Origin));
	}

	[Test]
	public void ShouldCorrectlyDeterminePhysicalValidity() {
		Assert.AreEqual(true, new PositionableConvexShape<Sphere>(new Sphere(1f), Location.Origin).IsPhysicallyValid);
		Assert.AreEqual(false, new PositionableConvexShape<Sphere>(new Sphere(-1f), Location.Origin).IsPhysicallyValid);
		Assert.AreEqual(false, new PositionableConvexShape<Sphere>(new Sphere(0f), Location.Origin).IsPhysicallyValid);
		Assert.AreEqual(false, new PositionableConvexShape<Sphere>(new Sphere(Single.NaN), Location.Origin).IsPhysicallyValid);
		Assert.AreEqual(false, new PositionableConvexShape<Sphere>(new Sphere(Single.PositiveInfinity), Location.Origin).IsPhysicallyValid);
		Assert.AreEqual(false, new PositionableConvexShape<Sphere>(new Sphere(Single.NegativeInfinity), Location.Origin).IsPhysicallyValid);
		Assert.AreEqual(false, new PositionableConvexShape<Sphere>(new Sphere(Single.NegativeZero), Location.Origin).IsPhysicallyValid);
		Assert.AreEqual(false, new PositionableConvexShape<Sphere>(new Sphere(1f), new Location(Single.NaN, 0f, 0f)).IsPhysicallyValid);
		Assert.AreEqual(false, new PositionableConvexShape<Sphere>(new Sphere(1f), new Location(0f, Single.PositiveInfinity, 0f)).IsPhysicallyValid);
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		const string Expectation = "Sphere[Radius 7.4] @ <1.0, -2.0, 3.0>";
		Assert.AreEqual(Expectation, TestShape.ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		TestShape.TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture);
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
	}

	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = "Sphere[Radius 7.4] @ <1.0, -2.0, 3.0>";
		Assert.AreEqual(TestShape, PositionableConvexShape<Sphere>.Parse(Input, CultureInfo.InvariantCulture));
		Assert.AreEqual(true, PositionableConvexShape<Sphere>.TryParse(Input, CultureInfo.InvariantCulture, out var result));
		Assert.AreEqual(TestShape, result);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength(TestShape);
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(TestShape);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(TestShape, TestShape.BaseShape.Radius, TestShape.Position.X, TestShape.Position.Y, TestShape.Position.Z);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		var start = new PositionableConvexShape<Sphere>(new Sphere(5f), new Location(0f, 0f, 0f));
		var end = new PositionableConvexShape<Sphere>(new Sphere(15f), new Location(10f, 10f, 10f));
		Assert.AreEqual(new PositionableConvexShape<Sphere>(new Sphere(10f), new Location(5f, 5f, 5f)), PositionableConvexShape<Sphere>.Interpolate(start, end, 0.5f));
		Assert.AreEqual(start, PositionableConvexShape<Sphere>.Interpolate(start, end, 0f));
		Assert.AreEqual(end, PositionableConvexShape<Sphere>.Interpolate(start, end, 1f));
		Assert.AreEqual(new PositionableConvexShape<Sphere>(new Sphere(20f), new Location(15f, 15f, 15f)), PositionableConvexShape<Sphere>.Interpolate(start, end, 1.5f));
		Assert.AreEqual(new PositionableConvexShape<Sphere>(new Sphere(0f), new Location(-5f, -5f, -5f)), PositionableConvexShape<Sphere>.Interpolate(start, end, -0.5f));
	}

	[Test]
	public void ShouldCorrectlyClamp() {
		var min = new PositionableConvexShape<Sphere>(new Sphere(5f), new Location(-5f, -5f, -5f));
		var max = new PositionableConvexShape<Sphere>(new Sphere(15f), new Location(5f, 5f, 5f));
		var mid = new PositionableConvexShape<Sphere>(new Sphere(10f), new Location(0f, 0f, 0f));
		Assert.AreEqual(mid, mid.Clamp(min, max));
		Assert.AreEqual(mid, mid.Clamp(max, min));
		Assert.AreEqual(max, new PositionableConvexShape<Sphere>(new Sphere(20f), new Location(10f, 10f, 10f)).Clamp(min, max));
		Assert.AreEqual(max, new PositionableConvexShape<Sphere>(new Sphere(20f), new Location(10f, 10f, 10f)).Clamp(max, min));
		Assert.AreEqual(min, new PositionableConvexShape<Sphere>(new Sphere(0f), new Location(-10f, -10f, -10f)).Clamp(min, max));
		Assert.AreEqual(min, new PositionableConvexShape<Sphere>(new Sphere(0f), new Location(-10f, -10f, -10f)).Clamp(max, min));
	}

	[Test]
	public void ShouldCorrectlyCreateRandomObjects() {
		const int NumIterations = 10_000;

		var min = new PositionableConvexShape<Sphere>(new Sphere(10f), new Location(-10f, -10f, -10f));
		var max = new PositionableConvexShape<Sphere>(new Sphere(20f), new Location(10f, 10f, 10f));
		for (var i = 0; i < NumIterations; ++i) {
			var val = PositionableConvexShape<Sphere>.Random(min, max);
			Assert.GreaterOrEqual(val.BaseShape.Radius, 10f);
			Assert.Less(val.BaseShape.Radius, 20f);
			Assert.GreaterOrEqual(val.Position.X, -10f);
			Assert.Less(val.Position.X, 10f);
			Assert.GreaterOrEqual(val.Position.Y, -10f);
			Assert.Less(val.Position.Y, 10f);
			Assert.GreaterOrEqual(val.Position.Z, -10f);
			Assert.Less(val.Position.Z, 10f);
		}
	}

	[Test]
	public void ShouldCorrectlyScale() {
		var scaled = TestShape.ScaledBy(3f);
		AssertToleranceEquals(new Sphere(7.4f * 3f), scaled.BaseShape, TestTolerance);
		Assert.AreEqual(TestShape.Position, scaled.Position);
	}

	[Test]
	public void ShouldCorrectlyDetermineContainmentOfBoundedRay() {
		Assert.AreEqual(true, TestShape.Contains(new BoundedRay(new Location(1f, -2f, 3f), Direction.Up * 3f)));
		Assert.AreEqual(true, TestShape.Contains(new BoundedRay(new Location(1f, -2f, 3f), Direction.Right * 7f)));
		Assert.AreEqual(false, TestShape.Contains(new BoundedRay(new Location(1f, -2f, 3f), Direction.Up * 8f)));
		Assert.AreEqual(false, TestShape.Contains(new BoundedRay(new Location(1f, 15.4f, 3f), Direction.Up * 1f)));
		Assert.AreEqual(false, TestShape.Contains(new BoundedRay(new Location(20f, 20f, 20f), Direction.Right * 1f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromLocations() {
		Assert.AreEqual(0f, TestShape.DistanceFrom((1f, -2f, 3f)));
		Assert.AreEqual(0f, TestShape.DistanceFrom((1f, -1f, 3f)));
		Assert.AreEqual(0f, TestShape.DistanceFrom((1f, -3f, 3f)));
		Assert.AreEqual(0f, TestShape.DistanceFrom((1f, 5.4f, 3f)));
		Assert.AreEqual(0f, TestShape.DistanceFrom((1f, -9.4f, 3f)));
		Assert.AreEqual(10f, TestShape.DistanceFrom((1f, 15.4f, 3f)));
		Assert.AreEqual(10f, TestShape.DistanceFrom((1f, -19.4f, 3f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineSurfaceDistanceFromLocations() {
		Assert.AreEqual(7.4f, TestShape.SurfaceDistanceFrom((1f, -2f, 3f)));
		Assert.AreEqual(6.4f, TestShape.SurfaceDistanceFrom((1f, -1f, 3f)));
		Assert.AreEqual(6.4f, TestShape.SurfaceDistanceFrom((1f, -3f, 3f)));
		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom((1f, 5.4f, 3f)), TestTolerance);
		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom((1f, -9.4f, 3f)), TestTolerance);
		Assert.AreEqual(10f, TestShape.SurfaceDistanceFrom((1f, 15.4f, 3f)));
		Assert.AreEqual(10f, TestShape.SurfaceDistanceFrom((1f, -19.4f, 3f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromLines() {
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Line((1f, -2f, 3f), Direction.Backward)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Line((1f, 5.4f, 3f), Direction.Backward)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Line((1f, -9.4f, 3f), Direction.Backward)));
		Assert.AreEqual(10f, TestShape.DistanceFrom(new Line((1f, 15.4f, 3f), Direction.Backward)));
		Assert.AreEqual(10f, TestShape.DistanceFrom(new Line((1f, -19.4f, 3f), Direction.Backward)));

		Assert.AreEqual(0f, TestShape.DistanceFrom(new Ray((1f, -2f, 3f), Direction.Down)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Ray((1f, 5.4f, 3f), Direction.Down)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Ray((1f, -9.4f, 3f), Direction.Up)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Ray((1f, 15.4f, 3f), Direction.Down)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Ray((1f, -19.4f, 3f), Direction.Up)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Ray((1f, 5.4f, 3f), Direction.Up)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Ray((1f, -9.4f, 3f), Direction.Down)));
		Assert.AreEqual(10f, TestShape.DistanceFrom(new Ray((1f, 15.4f, 3f), Direction.Up)));
		Assert.AreEqual(10f, TestShape.DistanceFrom(new Ray((1f, -19.4f, 3f), Direction.Down)));

		Assert.AreEqual(0f, TestShape.DistanceFrom(new BoundedRay((1f, -2f, 3f), Direction.Down * 100f)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new BoundedRay((1f, 5.4f, 3f), Direction.Down * 100f)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new BoundedRay((1f, -9.4f, 3f), Direction.Up * 100f)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new BoundedRay((1f, 15.4f, 3f), Direction.Down * 100f)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new BoundedRay((1f, -19.4f, 3f), Direction.Up * 100f)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new BoundedRay((1f, 5.4f, 3f), Direction.Up * 100f)));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new BoundedRay((1f, -9.4f, 3f), Direction.Down * 100f)));
		Assert.AreEqual(10f, TestShape.DistanceFrom(new BoundedRay((1f, 15.4f, 3f), Direction.Up * 100f)));
		Assert.AreEqual(10f, TestShape.DistanceFrom(new BoundedRay((1f, -19.4f, 3f), Direction.Down * 100f)));
		Assert.AreEqual(11f, TestShape.DistanceFrom(new BoundedRay((1f, 25.4f, 3f), Direction.Down * 9f)));
		Assert.AreEqual(9f, TestShape.DistanceFrom(new BoundedRay((1f, 25.4f, 3f), Direction.Down * 11f)));

		Assert.AreEqual(0f, TestShape.DistanceFrom(new BoundedRay(new Location(0f, -2f, 3f), new Location(2f, -2f, 3f))));
	}

	[Test]
	public void ShouldCorrectlyDetermineSurfaceDistanceFromLines() {
		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new Line((1f, -2f, 3f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new Line((1f, 5.4f, 3f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new Line((1f, -9.4f, 3f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(10f, TestShape.SurfaceDistanceFrom(new Line((1f, 15.4f, 3f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(10f, TestShape.SurfaceDistanceFrom(new Line((1f, -19.4f, 3f), Direction.Backward)), TestTolerance);

		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new Ray((1f, -2f, 3f), Direction.Down)), TestTolerance);
		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new Ray((1f, 5.4f, 3f), Direction.Down)), TestTolerance);
		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new Ray((1f, -9.4f, 3f), Direction.Up)), TestTolerance);
		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new Ray((1f, 15.4f, 3f), Direction.Down)), TestTolerance);
		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new Ray((1f, -19.4f, 3f), Direction.Up)), TestTolerance);
		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new Ray((1f, 5.4f, 3f), Direction.Up)), TestTolerance);
		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new Ray((1f, -9.4f, 3f), Direction.Down)), TestTolerance);
		Assert.AreEqual(10f, TestShape.SurfaceDistanceFrom(new Ray((1f, 15.4f, 3f), Direction.Up)), TestTolerance);
		Assert.AreEqual(10f, TestShape.SurfaceDistanceFrom(new Ray((1f, -19.4f, 3f), Direction.Down)), TestTolerance);

		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new BoundedRay((1f, -2f, 3f), Direction.Down * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new BoundedRay((1f, 5.4f, 3f), Direction.Down * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new BoundedRay((1f, -9.4f, 3f), Direction.Up * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new BoundedRay((1f, 15.4f, 3f), Direction.Down * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new BoundedRay((1f, -19.4f, 3f), Direction.Up * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new BoundedRay((1f, 5.4f, 3f), Direction.Up * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new BoundedRay((1f, -9.4f, 3f), Direction.Down * 100f)), TestTolerance);
		Assert.AreEqual(10f, TestShape.SurfaceDistanceFrom(new BoundedRay((1f, 15.4f, 3f), Direction.Up * 100f)), TestTolerance);
		Assert.AreEqual(10f, TestShape.SurfaceDistanceFrom(new BoundedRay((1f, -19.4f, 3f), Direction.Down * 100f)), TestTolerance);
		Assert.AreEqual(11f, TestShape.SurfaceDistanceFrom(new BoundedRay((1f, 25.4f, 3f), Direction.Down * 9f)), TestTolerance);
		Assert.AreEqual(9f, TestShape.SurfaceDistanceFrom(new BoundedRay((1f, 25.4f, 3f), Direction.Down * 11f)), TestTolerance);

		Assert.AreEqual(6.4f, TestShape.SurfaceDistanceFrom(new BoundedRay(new Location(0f, -2f, 3f), new Location(2f, -2f, 3f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineWhetherLocationIsContained() {
		Assert.AreEqual(true, TestShape.Contains(new Location(1f, -2f, 3f)));
		Assert.AreEqual(true, TestShape.Contains(new Location(1f, 5.4f, 3f)));
		Assert.AreEqual(true, TestShape.Contains(new Location(1f, -9.4f, 3f)));
		Assert.AreEqual(false, TestShape.Contains(new Location(1f, 5.5f, 3f)));
		Assert.AreEqual(false, TestShape.Contains(new Location(1f, -9.5f, 3f)));
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToGivenLocation() {
		AssertToleranceEquals(new Location(1f, -2f, 3f), TestShape.PointClosestTo(new Location(1f, -2f, 3f)), TestTolerance);
		AssertToleranceEquals(new Location(1f, 5.4f, 3f), TestShape.PointClosestTo(new Location(1f, 5.4f, 3f)), TestTolerance);
		AssertToleranceEquals(new Location(1f, -9.4f, 3f), TestShape.PointClosestTo(new Location(1f, -9.4f, 3f)), TestTolerance);
		AssertToleranceEquals(new Location(1f, 5.4f, 3f), TestShape.PointClosestTo(new Location(1f, 15.4f, 3f)), TestTolerance);
		AssertToleranceEquals(new Location(1f, -9.4f, 3f), TestShape.PointClosestTo(new Location(1f, -19.4f, 3f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnSurfaceToGivenLocation() {
		AssertToleranceEquals(new Location(1f, 5.4f, 3f), TestShape.SurfacePointClosestTo(new Location(1f, 5.4f, 3f)), TestTolerance);
		AssertToleranceEquals(new Location(1f, -9.4f, 3f), TestShape.SurfacePointClosestTo(new Location(1f, -9.4f, 3f)), TestTolerance);
		AssertToleranceEquals(new Location(1f, 5.4f, 3f), TestShape.SurfacePointClosestTo(new Location(1f, 15.4f, 3f)), TestTolerance);
		AssertToleranceEquals(new Location(1f, -9.4f, 3f), TestShape.SurfacePointClosestTo(new Location(1f, -19.4f, 3f)), TestTolerance);
		AssertToleranceEquals(new Location(1f, 5.4f, 3f), TestShape.SurfacePointClosestTo(new Location(1f, 0f, 3f)), TestTolerance);
		AssertToleranceEquals(new Location(1f, -9.4f, 3f), TestShape.SurfacePointClosestTo(new Location(1f, -4f, 3f)), TestTolerance);
		Assert.AreEqual(7.4f, TestShape.SurfacePointClosestTo(new Location(1f, -2f, 3f)).DistanceFrom(TestShape.Position), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToLine() {
		AssertToleranceEquals((1f, -2f, 3f), TestShape.PointClosestTo(new Line((1f, -2f, 3f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((1f, 0f, 3f), TestShape.PointClosestTo(new Line((1f, 0f, 3f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((1f, -4f, 3f), TestShape.PointClosestTo(new Line((1f, -4f, 3f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.PointClosestTo(new Line((1f, 5.4f, 3f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.PointClosestTo(new Line((1f, -9.4f, 3f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.PointClosestTo(new Line((1f, 15.4f, 3f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.PointClosestTo(new Line((1f, -19.4f, 3f), Direction.Backward)), TestTolerance);

		AssertToleranceEquals((1f, -2f, 3f), TestShape.PointClosestTo(new Ray((1f, -2f, 3f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((1f, -2f, 3f), TestShape.PointClosestTo(new Ray((1f, 5.4f, 3f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((1f, -2f, 3f), TestShape.PointClosestTo(new Ray((1f, -9.4f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((1f, -2f, 3f), TestShape.PointClosestTo(new Ray((1f, 15.4f, 3f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((1f, -2f, 3f), TestShape.PointClosestTo(new Ray((1f, -19.4f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((1f, 0f, 3f), TestShape.PointClosestTo(new Ray((1f, 0f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((1f, -4f, 3f), TestShape.PointClosestTo(new Ray((1f, -4f, 3f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.PointClosestTo(new Ray((1f, 5.4f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.PointClosestTo(new Ray((1f, -9.4f, 3f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.PointClosestTo(new Ray((1f, 15.4f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.PointClosestTo(new Ray((1f, -19.4f, 3f), Direction.Down)), TestTolerance);

		AssertToleranceEquals((1f, -2f, 3f), TestShape.PointClosestTo(new BoundedRay((1f, -2f, 3f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((1f, -2f, 3f), TestShape.PointClosestTo(new BoundedRay((1f, 5.4f, 3f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((1f, -2f, 3f), TestShape.PointClosestTo(new BoundedRay((1f, -9.4f, 3f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((1f, -2f, 3f), TestShape.PointClosestTo(new BoundedRay((1f, 15.4f, 3f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((1f, -2f, 3f), TestShape.PointClosestTo(new BoundedRay((1f, -19.4f, 3f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.PointClosestTo(new BoundedRay((1f, 5.4f, 3f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.PointClosestTo(new BoundedRay((1f, -9.4f, 3f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.PointClosestTo(new BoundedRay((1f, 15.4f, 3f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.PointClosestTo(new BoundedRay((1f, -19.4f, 3f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.PointClosestTo(new BoundedRay((1f, 25.4f, 3f), Direction.Down * 9f)), TestTolerance);

		AssertToleranceEquals((1f, -2f, 3f), TestShape.PointClosestTo(new BoundedRay(new Location(0f, -2f, 3f), new Location(2f, -2f, 3f))), TestTolerance);
		AssertToleranceEquals((-1f, -2f, 3f), TestShape.PointClosestTo(new BoundedRay(new Location(-4f, -2f, 3f), new Location(-1f, -2f, 3f))), TestTolerance);
		AssertToleranceEquals((-1f, -2f, 3f), TestShape.PointClosestTo(new BoundedRay(new Location(-14f, -2f, 3f), new Location(-1f, -2f, 3f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnLine() {
		AssertToleranceEquals((1f, -2f, 3f), TestShape.ClosestPointOn(new Line((1f, -2f, 3f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((1f, 0f, 3f), TestShape.ClosestPointOn(new Line((1f, 0f, 3f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((1f, -4f, 3f), TestShape.ClosestPointOn(new Line((1f, -4f, 3f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.ClosestPointOn(new Line((1f, 5.4f, 3f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.ClosestPointOn(new Line((1f, -9.4f, 3f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((1f, 15.4f, 3f), TestShape.ClosestPointOn(new Line((1f, 15.4f, 3f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((1f, -19.4f, 3f), TestShape.ClosestPointOn(new Line((1f, -19.4f, 3f), Direction.Backward)), TestTolerance);

		AssertToleranceEquals((1f, -2f, 3f), TestShape.ClosestPointOn(new Ray((1f, -2f, 3f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((1f, -2f, 3f), TestShape.ClosestPointOn(new Ray((1f, 5.4f, 3f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((1f, -2f, 3f), TestShape.ClosestPointOn(new Ray((1f, -9.4f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((1f, -2f, 3f), TestShape.ClosestPointOn(new Ray((1f, 15.4f, 3f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((1f, -2f, 3f), TestShape.ClosestPointOn(new Ray((1f, -19.4f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((1f, 0f, 3f), TestShape.ClosestPointOn(new Ray((1f, 0f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((1f, -4f, 3f), TestShape.ClosestPointOn(new Ray((1f, -4f, 3f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.ClosestPointOn(new Ray((1f, 5.4f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.ClosestPointOn(new Ray((1f, -9.4f, 3f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((1f, 15.4f, 3f), TestShape.ClosestPointOn(new Ray((1f, 15.4f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((1f, -19.4f, 3f), TestShape.ClosestPointOn(new Ray((1f, -19.4f, 3f), Direction.Down)), TestTolerance);

		AssertToleranceEquals((1f, -2f, 3f), TestShape.ClosestPointOn(new BoundedRay((1f, -2f, 3f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((1f, -2f, 3f), TestShape.ClosestPointOn(new BoundedRay((1f, 5.4f, 3f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((1f, -2f, 3f), TestShape.ClosestPointOn(new BoundedRay((1f, -9.4f, 3f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((1f, -2f, 3f), TestShape.ClosestPointOn(new BoundedRay((1f, 15.4f, 3f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((1f, -2f, 3f), TestShape.ClosestPointOn(new BoundedRay((1f, -19.4f, 3f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.ClosestPointOn(new BoundedRay((1f, 5.4f, 3f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.ClosestPointOn(new BoundedRay((1f, -9.4f, 3f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((1f, 15.4f, 3f), TestShape.ClosestPointOn(new BoundedRay((1f, 15.4f, 3f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((1f, -19.4f, 3f), TestShape.ClosestPointOn(new BoundedRay((1f, -19.4f, 3f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((1f, 16.4f, 3f), TestShape.ClosestPointOn(new BoundedRay((1f, 25.4f, 3f), Direction.Down * 9f)), TestTolerance);

		AssertToleranceEquals((1f, -2f, 3f), TestShape.ClosestPointOn(new BoundedRay(new Location(0f, -2f, 3f), new Location(2f, -2f, 3f))), TestTolerance);
		AssertToleranceEquals((-1f, -2f, 3f), TestShape.ClosestPointOn(new BoundedRay(new Location(-4f, -2f, 3f), new Location(-1f, -2f, 3f))), TestTolerance);
		AssertToleranceEquals((-1f, -2f, 3f), TestShape.ClosestPointOn(new BoundedRay(new Location(-14f, -2f, 3f), new Location(-1f, -2f, 3f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnSurfaceToLine() {
		Assert.AreEqual(7.4f, TestShape.SurfacePointClosestTo(new Line((1f, -2f, 3f), Direction.Backward)).DistanceFrom(TestShape.Position), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.SurfacePointClosestTo(new Line((1f, 0f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.SurfacePointClosestTo(new Line((1f, -4f, 3f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.SurfacePointClosestTo(new Line((1f, 5.4f, 3f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.SurfacePointClosestTo(new Line((1f, -9.4f, 3f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.SurfacePointClosestTo(new Line((1f, 15.4f, 3f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.SurfacePointClosestTo(new Line((1f, -19.4f, 3f), Direction.Backward)), TestTolerance);

		Assert.AreEqual(7.4f, TestShape.SurfacePointClosestTo(new Ray((1f, -2f, 3f), Direction.Down)).DistanceFrom(TestShape.Position), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.SurfacePointClosestTo(new Ray((1f, 4.4f, 3f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.SurfacePointClosestTo(new Ray((1f, -8.4f, 3f), Direction.Up)), TestTolerance);
		Assert.AreEqual(7.4f, TestShape.SurfacePointClosestTo(new Ray((1f, 15.4f, 3f), Direction.Down)).DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(7.4f, TestShape.SurfacePointClosestTo(new Ray((1f, -19.4f, 3f), Direction.Up)).DistanceFrom(TestShape.Position), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.SurfacePointClosestTo(new Ray((1f, 0f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.SurfacePointClosestTo(new Ray((1f, -4f, 3f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.SurfacePointClosestTo(new Ray((1f, 5.4f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.SurfacePointClosestTo(new Ray((1f, -9.4f, 3f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.SurfacePointClosestTo(new Ray((1f, 15.4f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.SurfacePointClosestTo(new Ray((1f, -19.4f, 3f), Direction.Down)), TestTolerance);

		Assert.AreEqual(7.4f, TestShape.SurfacePointClosestTo(new BoundedRay((1f, -2f, 3f), Direction.Down * 100f)).DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(7.4f, TestShape.SurfacePointClosestTo(new BoundedRay((1f, 5.4f, 3f), Direction.Down * 100f)).DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(7.4f, TestShape.SurfacePointClosestTo(new BoundedRay((1f, -9.4f, 3f), Direction.Up * 100f)).DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(7.4f, TestShape.SurfacePointClosestTo(new BoundedRay((1f, 15.4f, 3f), Direction.Down * 100f)).DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(7.4f, TestShape.SurfacePointClosestTo(new BoundedRay((1f, -19.4f, 3f), Direction.Up * 100f)).DistanceFrom(TestShape.Position), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.SurfacePointClosestTo(new BoundedRay((1f, 5.4f, 3f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.SurfacePointClosestTo(new BoundedRay((1f, -9.4f, 3f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.SurfacePointClosestTo(new BoundedRay((1f, 15.4f, 3f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.SurfacePointClosestTo(new BoundedRay((1f, -19.4f, 3f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.SurfacePointClosestTo(new BoundedRay((1f, 25.4f, 3f), Direction.Down * 9f)), TestTolerance);

		Assert.AreEqual(7.4f, TestShape.SurfacePointClosestTo(new BoundedRay(new Location(0f, -2f, 3f), new Location(2f, -2f, 3f))).DistanceFrom(TestShape.Position), TestTolerance);
		AssertToleranceEquals((-6.4f, -2f, 3f), TestShape.SurfacePointClosestTo(new BoundedRay(new Location(-4f, -2f, 3f), new Location(-1f, -2f, 3f))), TestTolerance);
		AssertToleranceEquals((-6.4f, -2f, 3f), TestShape.SurfacePointClosestTo(new BoundedRay(new Location(-14f, -2f, 3f), new Location(-1f, -2f, 3f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnLineToSurface() {
		Assert.AreEqual(7.4f, TestShape.ClosestPointToSurfaceOn(new Line((1f, -2f, 3f), Direction.Backward)).DistanceFrom(TestShape.Position), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.ClosestPointToSurfaceOn(new Line((1f, 0f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.ClosestPointToSurfaceOn(new Line((1f, -4f, 3f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.ClosestPointToSurfaceOn(new Line((1f, 5.4f, 3f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.ClosestPointToSurfaceOn(new Line((1f, -9.4f, 3f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((1f, 15.4f, 3f), TestShape.ClosestPointToSurfaceOn(new Line((1f, 15.4f, 3f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((1f, -19.4f, 3f), TestShape.ClosestPointToSurfaceOn(new Line((1f, -19.4f, 3f), Direction.Backward)), TestTolerance);

		Assert.AreEqual(7.4f, TestShape.ClosestPointToSurfaceOn(new Ray((1f, -2f, 3f), Direction.Down)).DistanceFrom(TestShape.Position), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.ClosestPointToSurfaceOn(new Ray((1f, 4.4f, 3f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.ClosestPointToSurfaceOn(new Ray((1f, -8.4f, 3f), Direction.Up)), TestTolerance);
		Assert.AreEqual(7.4f, TestShape.ClosestPointToSurfaceOn(new Ray((1f, 15.4f, 3f), Direction.Down)).DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(7.4f, TestShape.ClosestPointToSurfaceOn(new Ray((1f, -19.4f, 3f), Direction.Up)).DistanceFrom(TestShape.Position), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.ClosestPointToSurfaceOn(new Ray((1f, 0f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.ClosestPointToSurfaceOn(new Ray((1f, -4f, 3f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.ClosestPointToSurfaceOn(new Ray((1f, 5.4f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.ClosestPointToSurfaceOn(new Ray((1f, -9.4f, 3f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((1f, 15.4f, 3f), TestShape.ClosestPointToSurfaceOn(new Ray((1f, 15.4f, 3f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((1f, -19.4f, 3f), TestShape.ClosestPointToSurfaceOn(new Ray((1f, -19.4f, 3f), Direction.Down)), TestTolerance);

		Assert.AreEqual(7.4f, TestShape.ClosestPointToSurfaceOn(new BoundedRay((1f, -2f, 3f), Direction.Down * 100f)).DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(7.4f, TestShape.ClosestPointToSurfaceOn(new BoundedRay((1f, 5.4f, 3f), Direction.Down * 100f)).DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(7.4f, TestShape.ClosestPointToSurfaceOn(new BoundedRay((1f, -9.4f, 3f), Direction.Up * 100f)).DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(7.4f, TestShape.ClosestPointToSurfaceOn(new BoundedRay((1f, 15.4f, 3f), Direction.Down * 100f)).DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(7.4f, TestShape.ClosestPointToSurfaceOn(new BoundedRay((1f, -19.4f, 3f), Direction.Up * 100f)).DistanceFrom(TestShape.Position), TestTolerance);
		AssertToleranceEquals((1f, 5.4f, 3f), TestShape.ClosestPointToSurfaceOn(new BoundedRay((1f, 5.4f, 3f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((1f, -9.4f, 3f), TestShape.ClosestPointToSurfaceOn(new BoundedRay((1f, -9.4f, 3f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((1f, 15.4f, 3f), TestShape.ClosestPointToSurfaceOn(new BoundedRay((1f, 15.4f, 3f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((1f, -19.4f, 3f), TestShape.ClosestPointToSurfaceOn(new BoundedRay((1f, -19.4f, 3f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((1f, 16.4f, 3f), TestShape.ClosestPointToSurfaceOn(new BoundedRay((1f, 25.4f, 3f), Direction.Down * 9f)), TestTolerance);

		Assert.AreEqual(1f, TestShape.ClosestPointToSurfaceOn(new BoundedRay(new Location(0f, -2f, 3f), new Location(2f, -2f, 3f))).DistanceFrom(TestShape.Position), TestTolerance);
		AssertToleranceEquals((-4f, -2f, 3f), TestShape.ClosestPointToSurfaceOn(new BoundedRay(new Location(-4f, -2f, 3f), new Location(-1f, -2f, 3f))), TestTolerance);
		AssertToleranceEquals((-6.4f, -2f, 3f), TestShape.ClosestPointToSurfaceOn(new BoundedRay(new Location(-14f, -2f, 3f), new Location(-1f, -2f, 3f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindLineIntersections() {
		ConvexShapeLineIntersection intersection;

		// Line
		Assert.AreEqual(null, TestShape.IntersectionWith(new Line(new Location(1f, 8f, 3f), Direction.Right)));

		intersection = TestShape.IntersectionWith(new Line(new Location(1f, 4f, 3f), Direction.Right))!.Value;
		Assert.AreEqual(7.4f, intersection.First.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(4f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(7.4f, intersection.Second!.Value.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(4f, intersection.Second!.Value.Y, TestTolerance);
		Assert.AreEqual(2f - intersection.First.X, intersection.Second!.Value.X, TestTolerance);

		intersection = TestShape.IntersectionWith(new Line(new Location(1f, 5.4f, 3f), Direction.Right))!.Value;
		AssertToleranceEquals((1f, 5.4f, 3f), intersection.First, TestTolerance);
		Assert.IsFalse(intersection.Second.HasValue);


		// Ray
		Assert.AreEqual(null, TestShape.IntersectionWith(new Ray(new Location(1f, 8f, 3f), Direction.Right)));
		Assert.AreEqual(null, TestShape.IntersectionWith(new Ray(new Location(11f, 8f, 3f), Direction.Right)));
		Assert.AreEqual(null, TestShape.IntersectionWith(new Ray(new Location(-9f, -2f, 3f), Direction.Right)));

		intersection = TestShape.IntersectionWith(new Ray(new Location(11f, 4f, 3f), Direction.Right))!.Value;
		Assert.AreEqual(7.4f, intersection.First.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(4f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(7.4f, intersection.Second!.Value.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(4f, intersection.Second!.Value.Y, TestTolerance);
		Assert.AreEqual(2f - intersection.First.X, intersection.Second!.Value.X, TestTolerance);
		Assert.Less(
			new Ray(new Location(11f, 4f, 3f), Direction.Right).UnboundedDistanceAtPointClosestTo(intersection.First),
			new Ray(new Location(11f, 4f, 3f), Direction.Right).UnboundedDistanceAtPointClosestTo(intersection.Second!.Value)
		);
		intersection = TestShape.IntersectionWith(new Ray(new Location(-9f, 4f, 3f), Direction.Left))!.Value;
		Assert.Less(
			new Ray(new Location(-9f, 4f, 3f), Direction.Left).UnboundedDistanceAtPointClosestTo(intersection.First),
			new Ray(new Location(-9f, 4f, 3f), Direction.Left).UnboundedDistanceAtPointClosestTo(intersection.Second!.Value)
		);

		intersection = TestShape.IntersectionWith(new Ray(new Location(1f, 4f, 3f), Direction.Right))!.Value;
		Assert.AreEqual(7.4f, intersection.First.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(4f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(false, intersection.Second.HasValue);

		intersection = TestShape.IntersectionWith(new Ray(new Location(1f, 5.4f, 3f), Direction.Right))!.Value;
		AssertToleranceEquals((1f, 5.4f, 3f), intersection.First, TestTolerance);
		Assert.IsFalse(intersection.Second.HasValue);


		// BoundedRay
		Assert.AreEqual(null, TestShape.IntersectionWith(new BoundedRay(new Location(1f, 8f, 3f), Direction.Right * 100f)));
		Assert.AreEqual(null, TestShape.IntersectionWith(new BoundedRay(new Location(11f, 8f, 3f), Direction.Right * 100f)));
		Assert.AreEqual(null, TestShape.IntersectionWith(new BoundedRay(new Location(-9f, -2f, 3f), Direction.Right * 100f)));
		Assert.AreEqual(null, TestShape.IntersectionWith(new BoundedRay(new Location(-9f, -2f, 3f), Direction.Left * 2.5f)));

		intersection = TestShape.IntersectionWith(new BoundedRay(new Location(11f, 4f, 3f), Direction.Right * 100f))!.Value;
		Assert.AreEqual(7.4f, intersection.First.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(4f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(7.4f, intersection.Second!.Value.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(4f, intersection.Second!.Value.Y, TestTolerance);
		Assert.AreEqual(2f - intersection.First.X, intersection.Second!.Value.X, TestTolerance);
		Assert.Less(
			new BoundedRay(new Location(11f, 4f, 3f), Direction.Right * 100f).UnboundedDistanceAtPointClosestTo(intersection.First),
			new BoundedRay(new Location(11f, 4f, 3f), Direction.Right * 100f).UnboundedDistanceAtPointClosestTo(intersection.Second!.Value)
		);
		intersection = TestShape.IntersectionWith(new BoundedRay(new Location(-9f, 4f, 3f), Direction.Left * 100f))!.Value;
		Assert.Less(
			new BoundedRay(new Location(-9f, 4f, 3f), Direction.Left * 100f).UnboundedDistanceAtPointClosestTo(intersection.First),
			new BoundedRay(new Location(-9f, 4f, 3f), Direction.Left * 100f).UnboundedDistanceAtPointClosestTo(intersection.Second!.Value)
		);

		intersection = TestShape.IntersectionWith(new BoundedRay(new Location(1f, 4f, 3f), Direction.Right * 100f))!.Value;
		Assert.AreEqual(7.4f, intersection.First.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(4f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(false, intersection.Second.HasValue);

		intersection = TestShape.IntersectionWith(new BoundedRay(new Location(11f, 4f, 3f), Direction.Right * 10f))!.Value;
		Assert.AreEqual(7.4f, intersection.First.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(4f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(false, intersection.Second.HasValue);

		intersection = TestShape.IntersectionWith(new BoundedRay(new Location(1f, 5.4f, 3f), Direction.Right * 100f))!.Value;
		AssertToleranceEquals((1f, 5.4f, 3f), intersection.First, TestTolerance);
		Assert.IsFalse(intersection.Second.HasValue);




		// Line, Fast (FastIntersectionWith expects shape-space inputs, returns world-space outputs)
		intersection = TestShape.FastIntersectionWith(new Line(new Location(0f, 6f, 0f), Direction.Right));
		Assert.AreEqual(7.4f, intersection.First.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(4f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(7.4f, intersection.Second!.Value.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(4f, intersection.Second!.Value.Y, TestTolerance);
		Assert.AreEqual(2f - intersection.First.X, intersection.Second!.Value.X, TestTolerance);

		intersection = TestShape.FastIntersectionWith(new Line(new Location(0f, 7.4f, 0f), Direction.Right));
		AssertToleranceEquals((1f, 5.4f, 3f), intersection.First, TestTolerance);
		Assert.IsFalse(intersection.Second.HasValue);


		// Ray, Fast (FastIntersectionWith expects shape-space inputs, returns world-space outputs)
		intersection = TestShape.FastIntersectionWith(new Ray(new Location(10f, 6f, 0f), Direction.Right));
		Assert.AreEqual(7.4f, intersection.First.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(4f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(7.4f, intersection.Second!.Value.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(4f, intersection.Second!.Value.Y, TestTolerance);
		Assert.AreEqual(2f - intersection.First.X, intersection.Second!.Value.X, TestTolerance);

		intersection = TestShape.FastIntersectionWith(new Ray(new Location(0f, 6f, 0f), Direction.Right));
		Assert.AreEqual(7.4f, intersection.First.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(4f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(false, intersection.Second.HasValue);

		intersection = TestShape.FastIntersectionWith(new Ray(new Location(0f, 7.4f, 0f), Direction.Right));
		AssertToleranceEquals((1f, 5.4f, 3f), intersection.First, TestTolerance);
		Assert.IsFalse(intersection.Second.HasValue);


		// BoundedRay, Fast (FastIntersectionWith expects shape-space inputs, returns world-space outputs)
		intersection = TestShape.FastIntersectionWith(new BoundedRay(new Location(10f, 6f, 0f), Direction.Right * 100f));
		Assert.AreEqual(7.4f, intersection.First.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(4f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(7.4f, intersection.Second!.Value.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(4f, intersection.Second!.Value.Y, TestTolerance);
		Assert.AreEqual(2f - intersection.First.X, intersection.Second!.Value.X, TestTolerance);

		intersection = TestShape.FastIntersectionWith(new BoundedRay(new Location(0f, 6f, 0f), Direction.Right * 100f));
		Assert.AreEqual(7.4f, intersection.First.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(4f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(false, intersection.Second.HasValue);

		intersection = TestShape.FastIntersectionWith(new BoundedRay(new Location(10f, 6f, 0f), Direction.Right * 10f));
		Assert.AreEqual(7.4f, intersection.First.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(4f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(false, intersection.Second.HasValue);

		intersection = TestShape.FastIntersectionWith(new BoundedRay(new Location(0f, 7.4f, 0f), Direction.Right * 100f));
		AssertToleranceEquals((1f, 5.4f, 3f), intersection.First, TestTolerance);
		Assert.IsFalse(intersection.Second.HasValue);
	}

	[Test]
	public void ShouldCorrectlyTestForLineIntersections() {
		// Line
		Assert.False(TestShape.IsIntersectedBy(new Line(new Location(1f, 8f, 3f), Direction.Right)));
		Assert.True(TestShape.IsIntersectedBy(new Line(new Location(1f, 4f, 3f), Direction.Right)));
		Assert.True(TestShape.IsIntersectedBy(new Line(new Location(1f, 5.4f, 3f), Direction.Right)));


		// Ray
		Assert.False(TestShape.IsIntersectedBy(new Ray(new Location(1f, 8f, 3f), Direction.Right)));
		Assert.False(TestShape.IsIntersectedBy(new Ray(new Location(11f, 8f, 3f), Direction.Right)));
		Assert.False(TestShape.IsIntersectedBy(new Ray(new Location(-9f, -2f, 3f), Direction.Right)));
		Assert.True(TestShape.IsIntersectedBy(new Ray(new Location(11f, 4f, 3f), Direction.Right)));
		Assert.True(TestShape.IsIntersectedBy(new Ray(new Location(1f, 4f, 3f), Direction.Right)));
		Assert.True(TestShape.IsIntersectedBy(new Ray(new Location(1f, 5.4f, 3f), Direction.Right)));


		// BoundedRay
		Assert.False(TestShape.IsIntersectedBy(new BoundedRay(new Location(1f, 8f, 3f), Direction.Right * 100f)));
		Assert.False(TestShape.IsIntersectedBy(new BoundedRay(new Location(11f, 8f, 3f), Direction.Right * 100f)));
		Assert.False(TestShape.IsIntersectedBy(new BoundedRay(new Location(-9f, -2f, 3f), Direction.Right * 100f)));
		Assert.False(TestShape.IsIntersectedBy(new BoundedRay(new Location(-9f, -2f, 3f), Direction.Left * 2.5f)));
		Assert.True(TestShape.IsIntersectedBy(new BoundedRay(new Location(11f, 4f, 3f), Direction.Right * 100f)));
		Assert.True(TestShape.IsIntersectedBy(new BoundedRay(new Location(1f, 4f, 3f), Direction.Right * 100f)));
		Assert.True(TestShape.IsIntersectedBy(new BoundedRay(new Location(11f, 4f, 3f), Direction.Right * 10f)));
		Assert.True(TestShape.IsIntersectedBy(new BoundedRay(new Location(1f, 5.4f, 3f), Direction.Right * 100f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestPointToPlanes() {
		AssertToleranceEquals((0f, 0f, 0f), TestShape.PointClosestTo(new Plane(Direction.Up, (1f, -2f, 3f))), TestTolerance);
		AssertToleranceEquals((0f, 1f, 0f), TestShape.PointClosestTo(new Plane(Direction.Up, (1f, -1f, 3f))), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestShape.PointClosestTo(new Plane(Direction.Up, (1f, 5.4f, 3f))), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestShape.PointClosestTo(new Plane(Direction.Up, (1f, 8f, 3f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestPointOnPlanes() {
		AssertToleranceEquals((0f, 0f, 0f), TestShape.ClosestPointOn(new Plane(Direction.Up, (1f, -2f, 3f))), TestTolerance);
		AssertToleranceEquals((0f, 1f, 0f), TestShape.ClosestPointOn(new Plane(Direction.Up, (1f, -1f, 3f))), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestShape.ClosestPointOn(new Plane(Direction.Up, (1f, 5.4f, 3f))), TestTolerance);
		AssertToleranceEquals((0f, 10f, 0f), TestShape.ClosestPointOn(new Plane(Direction.Up, (1f, 8f, 3f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromPlanes() {
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Plane(Direction.Up, (1f, -2f, 3f))));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Plane(Direction.Up, (1f, 5.4f, 3f))));
		Assert.AreEqual(0f, TestShape.DistanceFrom(new Plane(Direction.Up, (1f, -9.4f, 3f))));
		Assert.AreEqual(2.6f, TestShape.DistanceFrom(new Plane(Direction.Up, (1f, 8f, 3f))), TestTolerance);
		Assert.AreEqual(2.6f, TestShape.DistanceFrom(new Plane(Direction.Up, (1f, -12f, 3f))), TestTolerance);

		Assert.AreEqual(0f, TestShape.SignedDistanceFrom(new Plane(Direction.Up, (1f, -2f, 3f))));
		Assert.AreEqual(0f, TestShape.SignedDistanceFrom(new Plane(Direction.Up, (1f, 5.4f, 3f))));
		Assert.AreEqual(0f, TestShape.SignedDistanceFrom(new Plane(Direction.Up, (1f, -9.4f, 3f))));
		Assert.AreEqual(-2.6f, TestShape.SignedDistanceFrom(new Plane(Direction.Up, (1f, 8f, 3f))), TestTolerance);
		Assert.AreEqual(2.6f, TestShape.SignedDistanceFrom(new Plane(Direction.Up, (1f, -12f, 3f))), TestTolerance);


		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new Plane(Direction.Up, (1f, -2f, 3f))));
		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new Plane(Direction.Up, (1f, 5.4f, 3f))));
		Assert.AreEqual(0f, TestShape.SurfaceDistanceFrom(new Plane(Direction.Up, (1f, -9.4f, 3f))));
		Assert.AreEqual(2.6f, TestShape.SurfaceDistanceFrom(new Plane(Direction.Up, (1f, 8f, 3f))), TestTolerance);
		Assert.AreEqual(2.6f, TestShape.SurfaceDistanceFrom(new Plane(Direction.Up, (1f, -12f, 3f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineRelationshipToPlanes() {
		Assert.AreEqual(PlaneObjectRelationship.PlaneIntersectsObject, TestShape.RelationshipTo(new Plane(Direction.Up, (1f, -2f, 3f))));
		Assert.AreEqual(PlaneObjectRelationship.PlaneIntersectsObject, TestShape.RelationshipTo(new Plane(Direction.Up, (1f, 5.4f, 3f))));
		Assert.AreEqual(PlaneObjectRelationship.PlaneIntersectsObject, TestShape.RelationshipTo(new Plane(Direction.Up, (1f, -9.4f, 3f))));
		Assert.AreEqual(PlaneObjectRelationship.PlaneFacesAwayFromObject, TestShape.RelationshipTo(new Plane(Direction.Up, (1f, 8f, 3f))));
		Assert.AreEqual(PlaneObjectRelationship.PlaneFacesTowardsObject, TestShape.RelationshipTo(new Plane(Direction.Up, (1f, -12f, 3f))));
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceSquaredFromLocations() {
		Assert.AreEqual(0f, TestShape.DistanceSquaredFrom((1f, -2f, 3f)));
		Assert.AreEqual(0f, TestShape.DistanceSquaredFrom((1f, -1f, 3f)));
		Assert.AreEqual(0f, TestShape.DistanceSquaredFrom((1f, 5.4f, 3f)));
		Assert.AreEqual(100f, TestShape.DistanceSquaredFrom((1f, 15.4f, 3f)), TestTolerance);
		Assert.AreEqual(100f, TestShape.DistanceSquaredFrom((1f, -19.4f, 3f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestSurfacePointToPlanes() {
		var closestPoint = TestShape.SurfacePointClosestTo(new Plane(Direction.Up, (1f, -1f, 3f)));

		Assert.AreEqual(-1f, closestPoint.Y, TestTolerance);
		Assert.AreEqual(7.4f, closestPoint.DistanceFrom(TestShape.Position), TestTolerance);

		closestPoint = TestShape.SurfacePointClosestTo(new Plane(Direction.Up, (1f, -2f, 3f)));

		Assert.AreEqual(-2f, closestPoint.Y, TestTolerance);
		Assert.AreEqual(7.4f, closestPoint.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(true, MathF.Abs(closestPoint.X - (1f + 7.4f)) < TestTolerance || MathF.Abs(closestPoint.Z - (3f + 7.4f)) < TestTolerance);

		closestPoint = TestShape.SurfacePointClosestTo(new Plane(Direction.Up, (1f, 8f, 3f)));
		AssertToleranceEquals((1f, 5.4f, 3f), closestPoint, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestSurfacePointOnPlanes() {
		var closestPoint = TestShape.ClosestPointToSurfaceOn(new Plane(Direction.Up, (1f, -1f, 3f)));

		Assert.AreEqual(-1f, closestPoint.Y, TestTolerance);
		Assert.AreEqual(7.4f, closestPoint.DistanceFrom(TestShape.Position), TestTolerance);

		closestPoint = TestShape.ClosestPointToSurfaceOn(new Plane(Direction.Up, (1f, -2f, 3f)));

		Assert.AreEqual(-2f, closestPoint.Y, TestTolerance);
		Assert.AreEqual(7.4f, closestPoint.DistanceFrom(TestShape.Position), TestTolerance);
		Assert.AreEqual(true, MathF.Abs(closestPoint.X - (1f + 7.4f)) < TestTolerance || MathF.Abs(closestPoint.Z - (3f + 7.4f)) < TestTolerance);

		closestPoint = TestShape.ClosestPointToSurfaceOn(new Plane(Direction.Up, (1f, 8f, 3f)));
		AssertToleranceEquals((1f, 8f, 3f), closestPoint, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineIncidentAngleWithLines() {
		const float LocalTestTolerance = 0.5f; // Needs to be a little higher as some of these calculations can be quite inaccurate

		void AssertAngle(Angle? expectation, Direction lineDir, Location originOffset) {
			var ray = new Ray(originOffset - lineDir * 100f, lineDir);
			var boundedRay = new BoundedRay(originOffset - lineDir * 100f, originOffset + lineDir * 100f);

			AssertToleranceEquals(expectation, TestShape.IncidentAngleWith(ray), LocalTestTolerance);
			AssertToleranceEquals(expectation, TestShape.IncidentAngleWith(boundedRay), LocalTestTolerance);
			Assert.AreEqual(null, TestShape.IncidentAngleWith(boundedRay.WithLength(100f - (TestShape.BaseShape.Radius + LocalTestTolerance))));
			if (expectation != null) {
				AssertToleranceEquals(expectation, TestShape.FastIncidentAngleWith(ray), LocalTestTolerance);
				AssertToleranceEquals(expectation, TestShape.FastIncidentAngleWith(boundedRay), LocalTestTolerance);
			}

			ray = new Ray(originOffset + lineDir * 100f, -lineDir);
			boundedRay = boundedRay.Flipped;

			AssertToleranceEquals(expectation, TestShape.IncidentAngleWith(ray), LocalTestTolerance);
			AssertToleranceEquals(expectation, TestShape.IncidentAngleWith(boundedRay), LocalTestTolerance);
			Assert.AreEqual(null, TestShape.IncidentAngleWith(boundedRay.WithLength(100f - (TestShape.BaseShape.Radius + LocalTestTolerance))));

			if (expectation != null) {
				AssertToleranceEquals(expectation, TestShape.FastIncidentAngleWith(ray), LocalTestTolerance);
				AssertToleranceEquals(expectation, TestShape.FastIncidentAngleWith(boundedRay), LocalTestTolerance);
			}
		}

		AssertAngle(Angle.Zero, Direction.Down, TestShape.Position);
		AssertAngle(Angle.Zero, (1f, 1f, 1f), TestShape.Position);

		AssertAngle(30f, Direction.Down, (1f, -2f, 3f + TestShape.BaseShape.Radius * 0.5f));
		AssertAngle(30f, Direction.Down, (1f, -2f, 3f + TestShape.BaseShape.Radius * -0.5f));
		AssertAngle(30f, Direction.Down, (1f + TestShape.BaseShape.Radius * 0.5f, -2f, 3f));
		AssertAngle(30f, Direction.Down, (1f + TestShape.BaseShape.Radius * -0.5f, -2f, 3f));
		AssertAngle(30f, (-1f, -1f, -1f), TestShape.Position + new Direction(-1f, -1f, -1f).AnyOrthogonal() * (TestShape.BaseShape.Radius * 0.5f));

		AssertAngle(null, Direction.Left, (1f, -2f, 3f + TestShape.BaseShape.Radius + LocalTestTolerance));

		Assert.IsNull(TestShape.IncidentAngleWith(new Ray((1f, -2f + TestShape.BaseShape.Radius + LocalTestTolerance, 3f), Direction.Up)));
		AssertToleranceEquals(0f, TestShape.IncidentAngleWith(new Ray((1f, -2f + TestShape.BaseShape.Radius - LocalTestTolerance, 3f), Direction.Up)), LocalTestTolerance);
		AssertToleranceEquals(0f, TestShape.IncidentAngleWith(new Ray((1f, -2f + TestShape.BaseShape.Radius + LocalTestTolerance, 3f), Direction.Down)), LocalTestTolerance);
		AssertToleranceEquals(0f, TestShape.FastIncidentAngleWith(new Ray((1f, -2f + TestShape.BaseShape.Radius - LocalTestTolerance, 3f), Direction.Up)), LocalTestTolerance);
		AssertToleranceEquals(0f, TestShape.FastIncidentAngleWith(new Ray((1f, -2f + TestShape.BaseShape.Radius + LocalTestTolerance, 3f), Direction.Down)), LocalTestTolerance);

		Assert.IsNull(TestShape.IncidentAngleWith(new BoundedRay(new Location(1f, 98f, 3f), new Location(1f, -2f + TestShape.BaseShape.Radius + LocalTestTolerance, 3f))));
		Assert.IsNull(TestShape.IncidentAngleWith(new BoundedRay(new Location(1f, -2f - (TestShape.BaseShape.Radius - LocalTestTolerance), 3f), new Location(1f, -2f + TestShape.BaseShape.Radius - LocalTestTolerance, 3f))));
		AssertToleranceEquals(0f, TestShape.IncidentAngleWith(new BoundedRay(new Location(1f, 98f, 3f), new Location(1f, -2f + TestShape.BaseShape.Radius - LocalTestTolerance, 3f))), LocalTestTolerance);
		AssertToleranceEquals(0f, TestShape.IncidentAngleWith(new BoundedRay(new Location(1f, 98f, 3f), new Location(1f, -2f + TestShape.BaseShape.Radius - LocalTestTolerance, 3f)).Flipped), LocalTestTolerance);
		AssertToleranceEquals(0f, TestShape.IncidentAngleWith(new BoundedRay(new Location(1f, -102f, 3f), new Location(1f, -2f - (TestShape.BaseShape.Radius - LocalTestTolerance), 3f))), LocalTestTolerance);
		AssertToleranceEquals(0f, TestShape.IncidentAngleWith(new BoundedRay(new Location(1f, -102f, 3f), new Location(1f, -2f - (TestShape.BaseShape.Radius - LocalTestTolerance), 3f)).Flipped), LocalTestTolerance);
		AssertToleranceEquals(30f, TestShape.IncidentAngleWith(new BoundedRay(new Location(-99f, -2f + TestShape.BaseShape.Radius * 0.5f, 3f), new Location(101f, -2f + TestShape.BaseShape.Radius * 0.5f, 3f))), LocalTestTolerance);
		AssertToleranceEquals(30f, TestShape.IncidentAngleWith(new BoundedRay(new Location(-99f, -2f + TestShape.BaseShape.Radius * 0.5f, 3f), new Location(101f, -2f + TestShape.BaseShape.Radius * 0.5f, 3f)).Flipped), LocalTestTolerance);
		AssertToleranceEquals(0f, TestShape.FastIncidentAngleWith(new BoundedRay(new Location(1f, 98f, 3f), new Location(1f, -2f + TestShape.BaseShape.Radius - LocalTestTolerance, 3f))), LocalTestTolerance);
		AssertToleranceEquals(0f, TestShape.FastIncidentAngleWith(new BoundedRay(new Location(1f, 98f, 3f), new Location(1f, -2f + TestShape.BaseShape.Radius - LocalTestTolerance, 3f)).Flipped), LocalTestTolerance);
		AssertToleranceEquals(0f, TestShape.FastIncidentAngleWith(new BoundedRay(new Location(1f, -102f, 3f), new Location(1f, -2f - (TestShape.BaseShape.Radius - LocalTestTolerance), 3f))), LocalTestTolerance);
		AssertToleranceEquals(0f, TestShape.FastIncidentAngleWith(new BoundedRay(new Location(1f, -102f, 3f), new Location(1f, -2f - (TestShape.BaseShape.Radius - LocalTestTolerance), 3f)).Flipped), LocalTestTolerance);
		AssertToleranceEquals(30f, TestShape.FastIncidentAngleWith(new BoundedRay(new Location(-99f, -2f + TestShape.BaseShape.Radius * 0.5f, 3f), new Location(101f, -2f + TestShape.BaseShape.Radius * 0.5f, 3f))), LocalTestTolerance);
		AssertToleranceEquals(30f, TestShape.FastIncidentAngleWith(new BoundedRay(new Location(-99f, -2f + TestShape.BaseShape.Radius * 0.5f, 3f), new Location(101f, -2f + TestShape.BaseShape.Radius * 0.5f, 3f)).Flipped), LocalTestTolerance);
	}

	[Test]
	public void ShouldCorrectlyReflectLines() {
		const float LocalTestTolerance = 0.5f; // Needs to be a little higher as some of these calculations can be quite inaccurate

		void AssertReflection(Ray? expectation, Direction lineDir, Location originOffset) {
			var ray = new Ray(originOffset - lineDir * 100f, lineDir);
			var boundedRay = new BoundedRay(originOffset - lineDir * 100f, originOffset + lineDir * 100f);

			AssertToleranceEquals(expectation, TestShape.ReflectionOf(ray), LocalTestTolerance);
			AssertToleranceEquals(expectation?.ToBoundedRay(200f - boundedRay.StartPoint.DistanceFrom(expectation.Value.StartPoint)), TestShape.ReflectionOf(boundedRay), LocalTestTolerance);
			Assert.AreEqual(null, TestShape.ReflectionOf(boundedRay.WithLength(100f - (TestShape.BaseShape.Radius + LocalTestTolerance))));
			if (expectation != null) {
				AssertToleranceEquals(expectation, TestShape.FastReflectionOf(ray), LocalTestTolerance);
				AssertToleranceEquals(expectation.Value.ToBoundedRay(200f - boundedRay.StartPoint.DistanceFrom(expectation.Value.StartPoint)), TestShape.FastReflectionOf(boundedRay), LocalTestTolerance);
			}
		}

		AssertReflection(new((1f, -2f + TestShape.BaseShape.Radius, 3f), Direction.Up), Direction.Down, TestShape.Position);
		AssertReflection(new(TestShape.Position + new Direction(-1f, -1f, -1f) * TestShape.BaseShape.Radius, (-1f, -1f, -1f)), (1f, 1f, 1f), TestShape.Position);

		AssertReflection(
			new(TestShape.FastIntersectionWith(new Ray((0f, 10f, TestShape.BaseShape.Radius * 0.5f), Direction.Down)).First, Direction.Up * (Direction.Up >> Direction.Forward) with { Angle = 60f }),
			Direction.Down,
			(1f, -2f, 3f + TestShape.BaseShape.Radius * 0.5f)
		);
		AssertReflection(
			new(TestShape.FastIntersectionWith(new Ray((0f, -10f, TestShape.BaseShape.Radius * 0.5f), Direction.Up)).First, Direction.Down * (Direction.Down >> Direction.Forward) with { Angle = 60f }),
			Direction.Up,
			(1f, -2f, 3f + TestShape.BaseShape.Radius * 0.5f)
		);
		AssertReflection(
			new(TestShape.FastIntersectionWith(new Ray((-20f, -20f, -20f), (1f, 1f, 1f))).First, (-1f, -1f, -1f)),
			(1f, 1f, 1f),
			TestShape.Position
		);

		AssertReflection(null, Direction.Left, (1f, -2f, 3f + TestShape.BaseShape.Radius + LocalTestTolerance));
		Assert.IsNull(TestShape.ReflectionOf(new Ray((1f, -2f + TestShape.BaseShape.Radius + LocalTestTolerance, 3f), Direction.Up)));
		AssertToleranceEquals(new Ray((1f, -2f + TestShape.BaseShape.Radius, 3f), Direction.Down), TestShape.ReflectionOf(new Ray((1f, -2f + TestShape.BaseShape.Radius - LocalTestTolerance, 3f), Direction.Up)), LocalTestTolerance);
		AssertToleranceEquals(new Ray((1f, -2f + TestShape.BaseShape.Radius, 3f), Direction.Up), TestShape.ReflectionOf(new Ray((1f, -2f + TestShape.BaseShape.Radius + LocalTestTolerance, 3f), Direction.Down)), LocalTestTolerance);
		AssertToleranceEquals(new Ray((1f, -2f + TestShape.BaseShape.Radius, 3f), Direction.Down), TestShape.FastReflectionOf(new Ray((1f, -2f + TestShape.BaseShape.Radius - LocalTestTolerance, 3f), Direction.Up)), LocalTestTolerance);
		AssertToleranceEquals(new Ray((1f, -2f + TestShape.BaseShape.Radius, 3f), Direction.Up), TestShape.FastReflectionOf(new Ray((1f, -2f + TestShape.BaseShape.Radius + LocalTestTolerance, 3f), Direction.Down)), LocalTestTolerance);

		Assert.IsNull(TestShape.ReflectionOf(new BoundedRay(new Location(1f, 98f, 3f), new Location(1f, -2f + TestShape.BaseShape.Radius + LocalTestTolerance, 3f))));
		Assert.IsNull(TestShape.ReflectionOf(new BoundedRay(new Location(1f, -2f - (TestShape.BaseShape.Radius - LocalTestTolerance), 3f), new Location(1f, -2f + TestShape.BaseShape.Radius - LocalTestTolerance, 3f))));
		AssertToleranceEquals(new BoundedRay(new Location(1f, -2f + TestShape.BaseShape.Radius, 3f), new Location(1f, -2f + TestShape.BaseShape.Radius + 1f, 3f)), TestShape.ReflectionOf(new BoundedRay(new Location(1f, 98f, 3f), new Location(1f, -2f + TestShape.BaseShape.Radius - 1f, 3f))), LocalTestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(1f, -2f + TestShape.BaseShape.Radius, 3f), new Location(1f, -2f + TestShape.BaseShape.Radius - (100f - (TestShape.BaseShape.Radius - 1f) - 1f), 3f)), TestShape.ReflectionOf(new BoundedRay(new Location(1f, 98f, 3f), new Location(1f, -2f + TestShape.BaseShape.Radius - 1f, 3f)).Flipped), LocalTestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(1f, -2f + TestShape.BaseShape.Radius, 3f), new Location(1f, -2f + TestShape.BaseShape.Radius + 1f, 3f)), TestShape.FastReflectionOf(new BoundedRay(new Location(1f, 98f, 3f), new Location(1f, -2f + TestShape.BaseShape.Radius - 1f, 3f))), LocalTestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(1f, -2f + TestShape.BaseShape.Radius, 3f), new Location(1f, -2f + TestShape.BaseShape.Radius - (100f - (TestShape.BaseShape.Radius - 1f) - 1f), 3f)), TestShape.FastReflectionOf(new BoundedRay(new Location(1f, 98f, 3f), new Location(1f, -2f + TestShape.BaseShape.Radius - 1f, 3f)).Flipped), LocalTestTolerance);
	}
}
