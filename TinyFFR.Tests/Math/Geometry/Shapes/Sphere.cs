// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
class SphereTest {
	const float TestTolerance = 0.01f;
	static readonly Sphere TestSphere = new(7.4f);

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyCalculateProperties() {
		// https://www.wolframalpha.com/input?i=volume%2C+surface+area%2C+circumference%2C+diameter+of+sphere+with+radius+7.4
		Assert.AreEqual(7.4f, TestSphere.Radius, TestTolerance);
		Assert.AreEqual(14.8f, TestSphere.Diameter, TestTolerance);
		Assert.AreEqual(46.4956f, TestSphere.Circumference, TestTolerance);
		Assert.AreEqual(688.134f, TestSphere.SurfaceArea, TestTolerance);
		Assert.AreEqual(1697.4f, TestSphere.Volume, TestTolerance);
		Assert.AreEqual(7.4f * 7.4f, TestSphere.RadiusSquared, TestTolerance);
	}

	[Test]
	public void StaticFactoryMethodsShouldCorrectlyConstruct() {
		// https://www.wolframalpha.com/input?i=volume%2C+surface+area%2C+circumference%2C+diameter+of+sphere+with+radius+7.4

		AssertToleranceEquals(TestSphere, Sphere.FromDiameter(14.8f), TestTolerance);
		AssertToleranceEquals(TestSphere, Sphere.FromCircumference(46.4956f), TestTolerance);
		AssertToleranceEquals(TestSphere, Sphere.FromSurfaceArea(688.134f), TestTolerance);
		AssertToleranceEquals(TestSphere, Sphere.FromVolume(1697.4f), TestTolerance);
		AssertToleranceEquals(TestSphere, Sphere.FromRadiusSquared(7.4f * 7.4f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDeterminePhysicalValidity() {
		Assert.AreEqual(true, new Sphere(1f).IsPhysicallyValid);
		Assert.AreEqual(false, new Sphere(-1f).IsPhysicallyValid);
		Assert.AreEqual(false, new Sphere(0f).IsPhysicallyValid);
		Assert.AreEqual(false, new Sphere(Single.NaN).IsPhysicallyValid);
		Assert.AreEqual(false, new Sphere(Single.PositiveInfinity).IsPhysicallyValid);
		Assert.AreEqual(false, new Sphere(Single.NegativeInfinity).IsPhysicallyValid);
		Assert.AreEqual(false, new Sphere(Single.NegativeZero).IsPhysicallyValid);
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		const string Expectation = "Sphere[Radius 7.4]";
		Assert.AreEqual(Expectation, TestSphere.ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		TestSphere.TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture);
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
	}

	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = "Sphere[Radius 7.4]";
		Assert.AreEqual(TestSphere, Sphere.Parse(Input, CultureInfo.InvariantCulture));
		Assert.AreEqual(true, Sphere.TryParse(Input, CultureInfo.InvariantCulture, out var result));
		Assert.AreEqual(TestSphere, result);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<Sphere>();
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(TestSphere);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(TestSphere, TestSphere.Radius);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		Assert.AreEqual(new Sphere(10f), Sphere.Interpolate(new(5f), new(15f), 0.5f));
		Assert.AreEqual(new Sphere(5f), Sphere.Interpolate(new(5f), new(15f), 0f));
		Assert.AreEqual(new Sphere(15f), Sphere.Interpolate(new(5f), new(15f), 1f));
		Assert.AreEqual(new Sphere(20f), Sphere.Interpolate(new(5f), new(15f), 1.5f));
		Assert.AreEqual(new Sphere(0f), Sphere.Interpolate(new(5f), new(15f), -0.5f));
	}

	[Test]
	public void ShouldCorrectlyCreateRandomObjects() {
		const int NumIterations = 10_000;
		
		for (var i = 0; i < NumIterations; ++i) {
			var val = Sphere.Random(new Sphere(10f), new Sphere(20f));
			Assert.GreaterOrEqual(val.Radius, 10f);
			Assert.Less(val.Radius, 20f);

			val = Sphere.Random();
			Assert.GreaterOrEqual(val.Radius, Sphere.DefaultRandomMin);
			Assert.Less(val.Radius, Sphere.DefaultRandomMax);
		}
	}

	[Test]
	public void ShouldCorrectlyScale() {
		AssertToleranceEquals(new Sphere(7.4f * 3f), new Sphere(7.4f).ScaledBy(3f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineCircleRadiusAtDistanceFromCentre() {
		// https://github.com/Egodystonic/EscapeLizards/blob/master/LosgapTests/Core/Math/Sphere.cs#L94

		Assert.AreEqual(7.4f, TestSphere.GetCircleRadiusAtDistanceFromCenter(0f));
		Assert.AreEqual(0f, TestSphere.GetCircleRadiusAtDistanceFromCenter(7.4f));
		Assert.AreEqual(0f, TestSphere.GetCircleRadiusAtDistanceFromCenter(10f));
		Assert.AreEqual(8.66025448f, new Sphere(10f).GetCircleRadiusAtDistanceFromCenter(5f), TestTolerance);
		Assert.AreEqual(0.1410673f, new Sphere(1f).GetCircleRadiusAtDistanceFromCenter(0.99f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromLocations() {
		Assert.AreEqual(0f, TestSphere.DistanceFrom((0f, 0f, 0f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom((0f, 1f, 0f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom((0f, -1f, 0f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom((0f, 7.4f, 0f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom((0f, -7.4f, 0f)));
		Assert.AreEqual(10f, TestSphere.DistanceFrom((0f, 17.4f, 0f)));
		Assert.AreEqual(10f, TestSphere.DistanceFrom((0f, -17.4f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineSurfaceDistanceFromLocations() {
		Assert.AreEqual(7.4f, TestSphere.SurfaceDistanceFrom((0f, 0f, 0f)));
		Assert.AreEqual(6.4f, TestSphere.SurfaceDistanceFrom((0f, 1f, 0f)));
		Assert.AreEqual(6.4f, TestSphere.SurfaceDistanceFrom((0f, -1f, 0f)));
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom((0f, 7.4f, 0f)));
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom((0f, -7.4f, 0f)));
		Assert.AreEqual(10f, TestSphere.SurfaceDistanceFrom((0f, 17.4f, 0f)));
		Assert.AreEqual(10f, TestSphere.SurfaceDistanceFrom((0f, -17.4f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromLines() {
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Line((0f, 0f, 0f), Direction.Backward)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Line((0f, 7.4f, 0f), Direction.Backward)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Line((0f, -7.4f, 0f), Direction.Backward)));
		Assert.AreEqual(10f, TestSphere.DistanceFrom(new Line((0f, 17.4f, 0f), Direction.Backward)));
		Assert.AreEqual(10f, TestSphere.DistanceFrom(new Line((0f, -17.4f, 0f), Direction.Backward)));

		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Ray((0f, 0f, 0f), Direction.Down)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Ray((0f, 7.4f, 0f), Direction.Down)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Ray((0f, -7.4f, 0f), Direction.Up)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Ray((0f, 17.4f, 0f), Direction.Down)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Ray((0f, -17.4f, 0f), Direction.Up)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Ray((0f, 7.4f, 0f), Direction.Up)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Ray((0f, -7.4f, 0f), Direction.Down)));
		Assert.AreEqual(10f, TestSphere.DistanceFrom(new Ray((0f, 17.4f, 0f), Direction.Up)));
		Assert.AreEqual(10f, TestSphere.DistanceFrom(new Ray((0f, -17.4f, 0f), Direction.Down)));

		Assert.AreEqual(0f, TestSphere.DistanceFrom(new BoundedRay((0f, 0f, 0f), Direction.Down * 100f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new BoundedRay((0f, 7.4f, 0f), Direction.Down * 100f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new BoundedRay((0f, -7.4f, 0f), Direction.Up * 100f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new BoundedRay((0f, 17.4f, 0f), Direction.Down * 100f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new BoundedRay((0f, -17.4f, 0f), Direction.Up * 100f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new BoundedRay((0f, 7.4f, 0f), Direction.Up * 100f)));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new BoundedRay((0f, -7.4f, 0f), Direction.Down * 100f)));
		Assert.AreEqual(10f, TestSphere.DistanceFrom(new BoundedRay((0f, 17.4f, 0f), Direction.Up * 100f)));
		Assert.AreEqual(10f, TestSphere.DistanceFrom(new BoundedRay((0f, -17.4f, 0f), Direction.Down * 100f)));
		Assert.AreEqual(11f, TestSphere.DistanceFrom(new BoundedRay((0f, 27.4f, 0f), Direction.Down * 9f)));
		Assert.AreEqual(9f, TestSphere.DistanceFrom(new BoundedRay((0f, 27.4f, 0f), Direction.Down * 11f)));

		Assert.AreEqual(0f, TestSphere.DistanceFrom(new BoundedRay(new Location(-1f, 0f, 0f), new Location(1f, 0f, 0f))));
	}

	[Test]
	public void ShouldCorrectlyDetermineSurfaceDistanceFromLines() {
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Line((0f, 0f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Line((0f, 7.4f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Line((0f, -7.4f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(10f, TestSphere.SurfaceDistanceFrom(new Line((0f, 17.4f, 0f), Direction.Backward)), TestTolerance);
		Assert.AreEqual(10f, TestSphere.SurfaceDistanceFrom(new Line((0f, -17.4f, 0f), Direction.Backward)), TestTolerance);

		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Ray((0f, 0f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Ray((0f, 7.4f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Ray((0f, -7.4f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Ray((0f, 17.4f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Ray((0f, -17.4f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Ray((0f, 7.4f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Ray((0f, -7.4f, 0f), Direction.Down)), TestTolerance);
		Assert.AreEqual(10f, TestSphere.SurfaceDistanceFrom(new Ray((0f, 17.4f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(10f, TestSphere.SurfaceDistanceFrom(new Ray((0f, -17.4f, 0f), Direction.Down)), TestTolerance);

		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new BoundedRay((0f, 0f, 0f), Direction.Down * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new BoundedRay((0f, 7.4f, 0f), Direction.Down * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new BoundedRay((0f, -7.4f, 0f), Direction.Up * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new BoundedRay((0f, 17.4f, 0f), Direction.Down * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new BoundedRay((0f, -17.4f, 0f), Direction.Up * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new BoundedRay((0f, 7.4f, 0f), Direction.Up * 100f)), TestTolerance);
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new BoundedRay((0f, -7.4f, 0f), Direction.Down * 100f)), TestTolerance);
		Assert.AreEqual(10f, TestSphere.SurfaceDistanceFrom(new BoundedRay((0f, 17.4f, 0f), Direction.Up * 100f)), TestTolerance);
		Assert.AreEqual(10f, TestSphere.SurfaceDistanceFrom(new BoundedRay((0f, -17.4f, 0f), Direction.Down * 100f)), TestTolerance);
		Assert.AreEqual(11f, TestSphere.SurfaceDistanceFrom(new BoundedRay((0f, 27.4f, 0f), Direction.Down * 9f)), TestTolerance);
		Assert.AreEqual(9f, TestSphere.SurfaceDistanceFrom(new BoundedRay((0f, 27.4f, 0f), Direction.Down * 11f)), TestTolerance);

		Assert.AreEqual(6.4f, TestSphere.SurfaceDistanceFrom(new BoundedRay(new Location(-1f, 0f, 0f), new Location(1f, 0f, 0f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineWhetherLocationIsContained() {
		Assert.AreEqual(true, TestSphere.Contains(new Location(0f, 0f, 0f)));
		Assert.AreEqual(true, TestSphere.Contains(new Location(0f, 7.4f, 0f)));
		Assert.AreEqual(true, TestSphere.Contains(new Location(0f, -7.4f, 0f)));
		Assert.AreEqual(false, TestSphere.Contains(new Location(0f, 7.5f, 0f)));
		Assert.AreEqual(false, TestSphere.Contains(new Location(0f, -7.5f, 0f)));
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToGivenLocation() {
		AssertToleranceEquals(new Location(0f, 0f, 0f), TestSphere.PointClosestTo(new Location(0f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, 7.4f, 0f), TestSphere.PointClosestTo(new Location(0f, 7.4f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, -7.4f, 0f), TestSphere.PointClosestTo(new Location(0f, -7.4f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, 7.4f, 0f), TestSphere.PointClosestTo(new Location(0f, 17.4f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, -7.4f, 0f), TestSphere.PointClosestTo(new Location(0f, -17.4f, 0f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnSurfaceToGivenLocation() {
		AssertToleranceEquals(new Location(0f, 7.4f, 0f), TestSphere.SurfacePointClosestTo(new Location(0f, 7.4f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, -7.4f, 0f), TestSphere.SurfacePointClosestTo(new Location(0f, -7.4f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, 7.4f, 0f), TestSphere.SurfacePointClosestTo(new Location(0f, 17.4f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, -7.4f, 0f), TestSphere.SurfacePointClosestTo(new Location(0f, -17.4f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, 7.4f, 0f), TestSphere.SurfacePointClosestTo(new Location(0f, 2f, 0f)), TestTolerance);
		AssertToleranceEquals(new Location(0f, -7.4f, 0f), TestSphere.SurfacePointClosestTo(new Location(0f, -2f, 0f)), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.SurfacePointClosestTo(new Location(0f, 0f, 0f)).DistanceFrom(Location.Origin), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointToLine() {
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.PointClosestTo(new Line((0f, 0f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, 2f, 0f), TestSphere.PointClosestTo(new Line((0f, 2f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -2f, 0f), TestSphere.PointClosestTo(new Line((0f, -2f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.PointClosestTo(new Line((0f, 7.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.PointClosestTo(new Line((0f, -7.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.PointClosestTo(new Line((0f, 17.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.PointClosestTo(new Line((0f, -17.4f, 0f), Direction.Backward)), TestTolerance);

		AssertToleranceEquals((0f, 0f, 0f), TestSphere.PointClosestTo(new Ray((0f, 0f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.PointClosestTo(new Ray((0f, 7.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.PointClosestTo(new Ray((0f, -7.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.PointClosestTo(new Ray((0f, 17.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.PointClosestTo(new Ray((0f, -17.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, 2f, 0f), TestSphere.PointClosestTo(new Ray((0f, 2f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -2f, 0f), TestSphere.PointClosestTo(new Ray((0f, -2f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.PointClosestTo(new Ray((0f, 7.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.PointClosestTo(new Ray((0f, -7.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.PointClosestTo(new Ray((0f, 17.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.PointClosestTo(new Ray((0f, -17.4f, 0f), Direction.Down)), TestTolerance);

		AssertToleranceEquals((0f, 0f, 0f), TestSphere.PointClosestTo(new BoundedRay((0f, 0f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.PointClosestTo(new BoundedRay((0f, 7.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.PointClosestTo(new BoundedRay((0f, -7.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.PointClosestTo(new BoundedRay((0f, 17.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.PointClosestTo(new BoundedRay((0f, -17.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.PointClosestTo(new BoundedRay((0f, 7.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.PointClosestTo(new BoundedRay((0f, -7.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.PointClosestTo(new BoundedRay((0f, 17.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.PointClosestTo(new BoundedRay((0f, -17.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.PointClosestTo(new BoundedRay((0f, 27.4f, 0f), Direction.Down * 9f)), TestTolerance);

		AssertToleranceEquals((0f, 0f, 0f), TestSphere.PointClosestTo(new BoundedRay(new Location(-1f, 0f, 0f), new Location(1f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals((-2f, 0f, 0f), TestSphere.PointClosestTo(new BoundedRay(new Location(-5f, 0f, 0f), new Location(-2f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals((-2f, 0f, 0f), TestSphere.PointClosestTo(new BoundedRay(new Location(-15f, 0f, 0f), new Location(-2f, 0f, 0f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnLine() {
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new Line((0f, 0f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, 2f, 0f), TestSphere.ClosestPointOn(new Line((0f, 2f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -2f, 0f), TestSphere.ClosestPointOn(new Line((0f, -2f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointOn(new Line((0f, 7.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointOn(new Line((0f, -7.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, 17.4f, 0f), TestSphere.ClosestPointOn(new Line((0f, 17.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -17.4f, 0f), TestSphere.ClosestPointOn(new Line((0f, -17.4f, 0f), Direction.Backward)), TestTolerance);

		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new Ray((0f, 0f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new Ray((0f, 7.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new Ray((0f, -7.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new Ray((0f, 17.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new Ray((0f, -17.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, 2f, 0f), TestSphere.ClosestPointOn(new Ray((0f, 2f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -2f, 0f), TestSphere.ClosestPointOn(new Ray((0f, -2f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointOn(new Ray((0f, 7.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointOn(new Ray((0f, -7.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 17.4f, 0f), TestSphere.ClosestPointOn(new Ray((0f, 17.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -17.4f, 0f), TestSphere.ClosestPointOn(new Ray((0f, -17.4f, 0f), Direction.Down)), TestTolerance);

		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new BoundedRay((0f, 0f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new BoundedRay((0f, 7.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new BoundedRay((0f, -7.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new BoundedRay((0f, 17.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new BoundedRay((0f, -17.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointOn(new BoundedRay((0f, 7.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointOn(new BoundedRay((0f, -7.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 17.4f, 0f), TestSphere.ClosestPointOn(new BoundedRay((0f, 17.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, -17.4f, 0f), TestSphere.ClosestPointOn(new BoundedRay((0f, -17.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 18.4f, 0f), TestSphere.ClosestPointOn(new BoundedRay((0f, 27.4f, 0f), Direction.Down * 9f)), TestTolerance);

		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new BoundedRay(new Location(-1f, 0f, 0f), new Location(1f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals((-2f, 0f, 0f), TestSphere.ClosestPointOn(new BoundedRay(new Location(-5f, 0f, 0f), new Location(-2f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals((-2f, 0f, 0f), TestSphere.ClosestPointOn(new BoundedRay(new Location(-15f, 0f, 0f), new Location(-2f, 0f, 0f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnSurfaceToLine() {
		Assert.AreEqual(7.4f, TestSphere.SurfacePointClosestTo(new Line((0f, 0f, 0f), Direction.Backward)).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.SurfacePointClosestTo(new Line((0f, 2f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.SurfacePointClosestTo(new Line((0f, -2f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.SurfacePointClosestTo(new Line((0f, 7.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.SurfacePointClosestTo(new Line((0f, -7.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.SurfacePointClosestTo(new Line((0f, 17.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.SurfacePointClosestTo(new Line((0f, -17.4f, 0f), Direction.Backward)), TestTolerance);

		Assert.AreEqual(7.4f, TestSphere.SurfacePointClosestTo(new Ray((0f, 0f, 0f), Direction.Down)).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.SurfacePointClosestTo(new Ray((0f, 6.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.SurfacePointClosestTo(new Ray((0f, -6.4f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.SurfacePointClosestTo(new Ray((0f, 17.4f, 0f), Direction.Down)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.SurfacePointClosestTo(new Ray((0f, -17.4f, 0f), Direction.Up)).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.SurfacePointClosestTo(new Ray((0f, 2f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.SurfacePointClosestTo(new Ray((0f, -2f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.SurfacePointClosestTo(new Ray((0f, 7.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.SurfacePointClosestTo(new Ray((0f, -7.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.SurfacePointClosestTo(new Ray((0f, 17.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.SurfacePointClosestTo(new Ray((0f, -17.4f, 0f), Direction.Down)), TestTolerance);

		Assert.AreEqual(7.4f, TestSphere.SurfacePointClosestTo(new BoundedRay((0f, 0f, 0f), Direction.Down * 100f)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.SurfacePointClosestTo(new BoundedRay((0f, 7.4f, 0f), Direction.Down * 100f)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.SurfacePointClosestTo(new BoundedRay((0f, -7.4f, 0f), Direction.Up * 100f)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.SurfacePointClosestTo(new BoundedRay((0f, 17.4f, 0f), Direction.Down * 100f)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.SurfacePointClosestTo(new BoundedRay((0f, -17.4f, 0f), Direction.Up * 100f)).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.SurfacePointClosestTo(new BoundedRay((0f, 7.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.SurfacePointClosestTo(new BoundedRay((0f, -7.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.SurfacePointClosestTo(new BoundedRay((0f, 17.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.SurfacePointClosestTo(new BoundedRay((0f, -17.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.SurfacePointClosestTo(new BoundedRay((0f, 27.4f, 0f), Direction.Down * 9f)), TestTolerance);

		Assert.AreEqual(7.4f, TestSphere.SurfacePointClosestTo(new BoundedRay(new Location(-1f, 0f, 0f), new Location(1f, 0f, 0f))).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((-7.4f, 0f, 0f), TestSphere.SurfacePointClosestTo(new BoundedRay(new Location(-5f, 0f, 0f), new Location(-2f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals((-7.4f, 0f, 0f), TestSphere.SurfacePointClosestTo(new BoundedRay(new Location(-15f, 0f, 0f), new Location(-2f, 0f, 0f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindClosestPointOnLineToSurface() {
		Assert.AreEqual(7.4f, TestSphere.ClosestPointToSurfaceOn(new Line((0f, 0f, 0f), Direction.Backward)).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Line((0f, 2f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Line((0f, -2f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Line((0f, 7.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Line((0f, -7.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, 17.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Line((0f, 17.4f, 0f), Direction.Backward)), TestTolerance);
		AssertToleranceEquals((0f, -17.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Line((0f, -17.4f, 0f), Direction.Backward)), TestTolerance);

		Assert.AreEqual(7.4f, TestSphere.ClosestPointToSurfaceOn(new Ray((0f, 0f, 0f), Direction.Down)).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Ray((0f, 6.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Ray((0f, -6.4f, 0f), Direction.Up)), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.ClosestPointToSurfaceOn(new Ray((0f, 17.4f, 0f), Direction.Down)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.ClosestPointToSurfaceOn(new Ray((0f, -17.4f, 0f), Direction.Up)).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Ray((0f, 2f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Ray((0f, -2f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Ray((0f, 7.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Ray((0f, -7.4f, 0f), Direction.Down)), TestTolerance);
		AssertToleranceEquals((0f, 17.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Ray((0f, 17.4f, 0f), Direction.Up)), TestTolerance);
		AssertToleranceEquals((0f, -17.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new Ray((0f, -17.4f, 0f), Direction.Down)), TestTolerance);

		Assert.AreEqual(7.4f, TestSphere.ClosestPointToSurfaceOn(new BoundedRay((0f, 0f, 0f), Direction.Down * 100f)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.ClosestPointToSurfaceOn(new BoundedRay((0f, 7.4f, 0f), Direction.Down * 100f)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.ClosestPointToSurfaceOn(new BoundedRay((0f, -7.4f, 0f), Direction.Up * 100f)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.ClosestPointToSurfaceOn(new BoundedRay((0f, 17.4f, 0f), Direction.Down * 100f)).DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(7.4f, TestSphere.ClosestPointToSurfaceOn(new BoundedRay((0f, -17.4f, 0f), Direction.Up * 100f)).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new BoundedRay((0f, 7.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, -7.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new BoundedRay((0f, -7.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 17.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new BoundedRay((0f, 17.4f, 0f), Direction.Up * 100f)), TestTolerance);
		AssertToleranceEquals((0f, -17.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new BoundedRay((0f, -17.4f, 0f), Direction.Down * 100f)), TestTolerance);
		AssertToleranceEquals((0f, 18.4f, 0f), TestSphere.ClosestPointToSurfaceOn(new BoundedRay((0f, 27.4f, 0f), Direction.Down * 9f)), TestTolerance);

		Assert.AreEqual(1f, TestSphere.ClosestPointToSurfaceOn(new BoundedRay(new Location(-1f, 0f, 0f), new Location(1f, 0f, 0f))).DistanceFromOrigin(), TestTolerance);
		AssertToleranceEquals((-5f, 0f, 0f), TestSphere.ClosestPointToSurfaceOn(new BoundedRay(new Location(-5f, 0f, 0f), new Location(-2f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals((-7.4f, 0f, 0f), TestSphere.ClosestPointToSurfaceOn(new BoundedRay(new Location(-15f, 0f, 0f), new Location(-2f, 0f, 0f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyFindLineIntersections() {
		ConvexShapeLineIntersection intersection;

		// Line
		Assert.AreEqual(null, TestSphere.IntersectionWith(new Line(new Location(0f, 10f, 0f), Direction.Right)));

		intersection = TestSphere.IntersectionWith(new Line(new Location(0f, 6f, 0f), Direction.Right))!.Value;
		Assert.AreEqual(7.4f, intersection.First.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(7.4f, intersection.Second!.Value.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.Second!.Value.Y, TestTolerance);
		Assert.AreEqual(-intersection.First.X, intersection.Second!.Value.X, TestTolerance);

		intersection = TestSphere.IntersectionWith(new Line(new Location(0f, 7.4f, 0f), Direction.Right))!.Value;
		AssertToleranceEquals((0f, 7.4f, 0f), intersection.First, TestTolerance);
		Assert.IsFalse(intersection.Second.HasValue);


		// Ray
		Assert.AreEqual(null, TestSphere.IntersectionWith(new Ray(new Location(0f, 10f, 0f), Direction.Right)));
		Assert.AreEqual(null, TestSphere.IntersectionWith(new Ray(new Location(10f, 10f, 0f), Direction.Right)));
		Assert.AreEqual(null, TestSphere.IntersectionWith(new Ray(new Location(-10f, 0f, 0f), Direction.Right)));

		intersection = TestSphere.IntersectionWith(new Ray(new Location(10f, 6f, 0f), Direction.Right))!.Value;
		Assert.AreEqual(7.4f, intersection.First.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(7.4f, intersection.Second!.Value.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.Second!.Value.Y, TestTolerance);
		Assert.AreEqual(-intersection.First.X, intersection.Second!.Value.X, TestTolerance);
		Assert.Less(
			new Ray(new Location(10f, 6f, 0f), Direction.Right).UnboundedDistanceAtPointClosestTo(intersection.First),
			new Ray(new Location(10f, 6f, 0f), Direction.Right).UnboundedDistanceAtPointClosestTo(intersection.Second!.Value)
		);
		intersection = TestSphere.IntersectionWith(new Ray(new Location(-10f, 6f, 0f), Direction.Left))!.Value;
		Assert.Less(
			new Ray(new Location(-10f, 6f, 0f), Direction.Left).UnboundedDistanceAtPointClosestTo(intersection.First),
			new Ray(new Location(-10f, 6f, 0f), Direction.Left).UnboundedDistanceAtPointClosestTo(intersection.Second!.Value)
		);

		intersection = TestSphere.IntersectionWith(new Ray(new Location(0f, 6f, 0f), Direction.Right))!.Value;
		Assert.AreEqual(7.4f, intersection.First.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(false, intersection.Second.HasValue);

		intersection = TestSphere.IntersectionWith(new Ray(new Location(0f, 7.4f, 0f), Direction.Right))!.Value;
		AssertToleranceEquals((0f, 7.4f, 0f), intersection.First, TestTolerance);
		Assert.IsFalse(intersection.Second.HasValue);


		// BoundedRay
		Assert.AreEqual(null, TestSphere.IntersectionWith(new BoundedRay(new Location(0f, 10f, 0f), Direction.Right * 100f)));
		Assert.AreEqual(null, TestSphere.IntersectionWith(new BoundedRay(new Location(10f, 10f, 0f), Direction.Right * 100f)));
		Assert.AreEqual(null, TestSphere.IntersectionWith(new BoundedRay(new Location(-10f, 0f, 0f), Direction.Right * 100f)));
		Assert.AreEqual(null, TestSphere.IntersectionWith(new BoundedRay(new Location(-10f, 0f, 0f), Direction.Left * 2.5f)));

		intersection = TestSphere.IntersectionWith(new BoundedRay(new Location(10f, 6f, 0f), Direction.Right * 100f))!.Value;
		Assert.AreEqual(7.4f, intersection.First.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(7.4f, intersection.Second!.Value.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.Second!.Value.Y, TestTolerance);
		Assert.AreEqual(-intersection.First.X, intersection.Second!.Value.X, TestTolerance);
		Assert.Less(
			new BoundedRay(new Location(10f, 6f, 0f), Direction.Right * 100f).UnboundedDistanceAtPointClosestTo(intersection.First),
			new BoundedRay(new Location(10f, 6f, 0f), Direction.Right * 100f).UnboundedDistanceAtPointClosestTo(intersection.Second!.Value)
		);
		intersection = TestSphere.IntersectionWith(new BoundedRay(new Location(-10f, 6f, 0f), Direction.Left * 100f))!.Value;
		Assert.Less(
			new BoundedRay(new Location(-10f, 6f, 0f), Direction.Left * 100f).UnboundedDistanceAtPointClosestTo(intersection.First),
			new BoundedRay(new Location(-10f, 6f, 0f), Direction.Left * 100f).UnboundedDistanceAtPointClosestTo(intersection.Second!.Value)
		);

		intersection = TestSphere.IntersectionWith(new BoundedRay(new Location(0f, 6f, 0f), Direction.Right * 100f))!.Value;
		Assert.AreEqual(7.4f, intersection.First.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(false, intersection.Second.HasValue);

		intersection = TestSphere.IntersectionWith(new BoundedRay(new Location(10f, 6f, 0f), Direction.Right * 10f))!.Value;
		Assert.AreEqual(7.4f, intersection.First.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(false, intersection.Second.HasValue);

		intersection = TestSphere.IntersectionWith(new BoundedRay(new Location(0f, 7.4f, 0f), Direction.Right * 100f))!.Value;
		AssertToleranceEquals((0f, 7.4f, 0f), intersection.First, TestTolerance);
		Assert.IsFalse(intersection.Second.HasValue);




		// Line, Fast
		intersection = TestSphere.FastIntersectionWith(new Line(new Location(0f, 6f, 0f), Direction.Right));
		Assert.AreEqual(7.4f, intersection.First.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(7.4f, intersection.Second!.Value.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.Second!.Value.Y, TestTolerance);
		Assert.AreEqual(-intersection.First.X, intersection.Second!.Value.X, TestTolerance);

		intersection = TestSphere.FastIntersectionWith(new Line(new Location(0f, 7.4f, 0f), Direction.Right));
		AssertToleranceEquals((0f, 7.4f, 0f), intersection.First, TestTolerance);
		Assert.IsFalse(intersection.Second.HasValue);


		// Ray, Fast
		intersection = TestSphere.FastIntersectionWith(new Ray(new Location(10f, 6f, 0f), Direction.Right));
		Assert.AreEqual(7.4f, intersection.First.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(7.4f, intersection.Second!.Value.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.Second!.Value.Y, TestTolerance);
		Assert.AreEqual(-intersection.First.X, intersection.Second!.Value.X, TestTolerance);

		intersection = TestSphere.FastIntersectionWith(new Ray(new Location(0f, 6f, 0f), Direction.Right));
		Assert.AreEqual(7.4f, intersection.First.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(false, intersection.Second.HasValue);

		intersection = TestSphere.FastIntersectionWith(new Ray(new Location(0f, 7.4f, 0f), Direction.Right));
		AssertToleranceEquals((0f, 7.4f, 0f), intersection.First, TestTolerance);
		Assert.IsFalse(intersection.Second.HasValue);


		// BoundedRay, Fast
		intersection = TestSphere.FastIntersectionWith(new BoundedRay(new Location(10f, 6f, 0f), Direction.Right * 100f));
		Assert.AreEqual(7.4f, intersection.First.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(7.4f, intersection.Second!.Value.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.Second!.Value.Y, TestTolerance);
		Assert.AreEqual(-intersection.First.X, intersection.Second!.Value.X, TestTolerance);

		intersection = TestSphere.FastIntersectionWith(new BoundedRay(new Location(0f, 6f, 0f), Direction.Right * 100f));
		Assert.AreEqual(7.4f, intersection.First.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(false, intersection.Second.HasValue);

		intersection = TestSphere.FastIntersectionWith(new BoundedRay(new Location(10f, 6f, 0f), Direction.Right * 10f));
		Assert.AreEqual(7.4f, intersection.First.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(6f, intersection.First.Y, TestTolerance);
		Assert.AreEqual(false, intersection.Second.HasValue);

		intersection = TestSphere.FastIntersectionWith(new BoundedRay(new Location(0f, 7.4f, 0f), Direction.Right * 100f));
		AssertToleranceEquals((0f, 7.4f, 0f), intersection.First, TestTolerance);
		Assert.IsFalse(intersection.Second.HasValue);
	}

	[Test]
	public void ShouldCorrectlyTestForLineIntersections() {
		// Line
		Assert.False(TestSphere.IsIntersectedBy(new Line(new Location(0f, 10f, 0f), Direction.Right)));
		Assert.True(TestSphere.IsIntersectedBy(new Line(new Location(0f, 6f, 0f), Direction.Right)));
		Assert.True(TestSphere.IsIntersectedBy(new Line(new Location(0f, 7.4f, 0f), Direction.Right)));


		// Ray
		Assert.False(TestSphere.IsIntersectedBy(new Ray(new Location(0f, 10f, 0f), Direction.Right)));
		Assert.False(TestSphere.IsIntersectedBy(new Ray(new Location(10f, 10f, 0f), Direction.Right)));
		Assert.False(TestSphere.IsIntersectedBy(new Ray(new Location(-10f, 0f, 0f), Direction.Right)));
		Assert.True(TestSphere.IsIntersectedBy(new Ray(new Location(10f, 6f, 0f), Direction.Right)));
		Assert.True(TestSphere.IsIntersectedBy(new Ray(new Location(0f, 6f, 0f), Direction.Right)));
		Assert.True(TestSphere.IsIntersectedBy(new Ray(new Location(0f, 7.4f, 0f), Direction.Right)));


		// BoundedRay
		Assert.False(TestSphere.IsIntersectedBy(new BoundedRay(new Location(0f, 10f, 0f), Direction.Right * 100f)));
		Assert.False(TestSphere.IsIntersectedBy(new BoundedRay(new Location(10f, 10f, 0f), Direction.Right * 100f)));
		Assert.False(TestSphere.IsIntersectedBy(new BoundedRay(new Location(-10f, 0f, 0f), Direction.Right * 100f)));
		Assert.False(TestSphere.IsIntersectedBy(new BoundedRay(new Location(-10f, 0f, 0f), Direction.Left * 2.5f)));
		Assert.True(TestSphere.IsIntersectedBy(new BoundedRay(new Location(10f, 6f, 0f), Direction.Right * 100f)));
		Assert.True(TestSphere.IsIntersectedBy(new BoundedRay(new Location(0f, 6f, 0f), Direction.Right * 100f)));
		Assert.True(TestSphere.IsIntersectedBy(new BoundedRay(new Location(10f, 6f, 0f), Direction.Right * 10f)));
		Assert.True(TestSphere.IsIntersectedBy(new BoundedRay(new Location(0f, 7.4f, 0f), Direction.Right * 100f)));
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestPointToPlanes() {
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.PointClosestTo(new Plane(Direction.Up, (0f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 1f, 0f), TestSphere.PointClosestTo(new Plane(Direction.Up, (0f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.PointClosestTo(new Plane(Direction.Up, (0f, 7.4f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.PointClosestTo(new Plane(Direction.Up, (0f, 10f, 0f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestPointOnPlanes() {
		AssertToleranceEquals((0f, 0f, 0f), TestSphere.ClosestPointOn(new Plane(Direction.Up, (0f, 0f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 1f, 0f), TestSphere.ClosestPointOn(new Plane(Direction.Up, (0f, 1f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 7.4f, 0f), TestSphere.ClosestPointOn(new Plane(Direction.Up, (0f, 7.4f, 0f))), TestTolerance);
		AssertToleranceEquals((0f, 10f, 0f), TestSphere.ClosestPointOn(new Plane(Direction.Up, (0f, 10f, 0f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineDistanceFromPlanes() {
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Plane(Direction.Up, (0f, 0f, 0f))));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Plane(Direction.Up, (0f, 7.4f, 0f))));
		Assert.AreEqual(0f, TestSphere.DistanceFrom(new Plane(Direction.Up, (0f, -7.4f, 0f))));
		Assert.AreEqual(2.6f, TestSphere.DistanceFrom(new Plane(Direction.Up, (0f, 10f, 0f))), TestTolerance);
		Assert.AreEqual(2.6f, TestSphere.DistanceFrom(new Plane(Direction.Up, (0f, -10f, 0f))), TestTolerance);

		Assert.AreEqual(0f, TestSphere.SignedDistanceFrom(new Plane(Direction.Up, (0f, 0f, 0f))));
		Assert.AreEqual(0f, TestSphere.SignedDistanceFrom(new Plane(Direction.Up, (0f, 7.4f, 0f))));
		Assert.AreEqual(0f, TestSphere.SignedDistanceFrom(new Plane(Direction.Up, (0f, -7.4f, 0f))));
		Assert.AreEqual(-2.6f, TestSphere.SignedDistanceFrom(new Plane(Direction.Up, (0f, 10f, 0f))), TestTolerance);
		Assert.AreEqual(2.6f, TestSphere.SignedDistanceFrom(new Plane(Direction.Up, (0f, -10f, 0f))), TestTolerance);


		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Plane(Direction.Up, (0f, 0f, 0f))));
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Plane(Direction.Up, (0f, 7.4f, 0f))));
		Assert.AreEqual(0f, TestSphere.SurfaceDistanceFrom(new Plane(Direction.Up, (0f, -7.4f, 0f))));
		Assert.AreEqual(2.6f, TestSphere.SurfaceDistanceFrom(new Plane(Direction.Up, (0f, 10f, 0f))), TestTolerance);
		Assert.AreEqual(2.6f, TestSphere.SurfaceDistanceFrom(new Plane(Direction.Up, (0f, -10f, 0f))), TestTolerance);

		Assert.AreEqual(0f, TestSphere.SignedSurfaceDistanceFrom(new Plane(Direction.Up, (0f, 0f, 0f))));
		Assert.AreEqual(0f, TestSphere.SignedSurfaceDistanceFrom(new Plane(Direction.Up, (0f, 7.4f, 0f))));
		Assert.AreEqual(0f, TestSphere.SignedSurfaceDistanceFrom(new Plane(Direction.Up, (0f, -7.4f, 0f))));
		Assert.AreEqual(-2.6f, TestSphere.SignedSurfaceDistanceFrom(new Plane(Direction.Up, (0f, 10f, 0f))), TestTolerance);
		Assert.AreEqual(2.6f, TestSphere.SignedSurfaceDistanceFrom(new Plane(Direction.Up, (0f, -10f, 0f))), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineRelationshipToPlanes() {
		Assert.AreEqual(PlaneObjectRelationship.PlaneIntersectsObject, TestSphere.RelationshipTo(new Plane(Direction.Up, (0f, 0f, 0f))));
		Assert.AreEqual(PlaneObjectRelationship.PlaneIntersectsObject, TestSphere.RelationshipTo(new Plane(Direction.Up, (0f, 7.4f, 0f))));
		Assert.AreEqual(PlaneObjectRelationship.PlaneIntersectsObject, TestSphere.RelationshipTo(new Plane(Direction.Up, (0f, -7.4f, 0f))));
		Assert.AreEqual(PlaneObjectRelationship.PlaneFacesAwayFromObject, TestSphere.RelationshipTo(new Plane(Direction.Up, (0f, 10f, 0f))));
		Assert.AreEqual(PlaneObjectRelationship.PlaneFacesTowardsObject, TestSphere.RelationshipTo(new Plane(Direction.Up, (0f, -10f, 0f))));
	}

	[Test]
	public void ShouldCorrectlyBeSplitByPlanes() {
		Assert.AreEqual(false, TestSphere.TrySplit(new Plane(Direction.Up, (0f, 10f, 0f)), out _, out _));

		Assert.AreEqual(true, new Sphere(10f).TrySplit(new Plane(Direction.Up, (0f, 5f, 0f)), out var circleCentrePoint, out var circleRadius));
		Assert.AreEqual(8.66025448f, circleRadius, TestTolerance);
		Assert.AreEqual(new Location(0f, 5f, 0f), circleCentrePoint);

		Assert.AreEqual(true, TestSphere.TrySplit(new Plane(Direction.Up, (0f, 0f, 0f)), out circleCentrePoint, out circleRadius));
		Assert.AreEqual(7.4f, circleRadius, TestTolerance);
		Assert.AreEqual(new Location(0f, 0f, 0f), circleCentrePoint);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestSurfacePointToPlanes() {
		var closestPoint = TestSphere.SurfacePointClosestTo(new Plane(Direction.Up, (0f, 1f, 0f)));

		Assert.AreEqual(1f, closestPoint.Y, TestTolerance);
		Assert.AreEqual(7.4f, closestPoint.DistanceFromOrigin(), TestTolerance);

		closestPoint = TestSphere.SurfacePointClosestTo(new Plane(Direction.Up, (0f, 0f, 0f)));

		Assert.AreEqual(0f, closestPoint.Y, TestTolerance);
		Assert.AreEqual(7.4f, closestPoint.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(true, MathF.Abs(closestPoint.X - 7.4f) < TestTolerance || MathF.Abs(closestPoint.Z - 7.4f) < TestTolerance);

		closestPoint = TestSphere.SurfacePointClosestTo(new Plane(Direction.Up, (0f, 10f, 0f)));
		AssertToleranceEquals((0f, 7.4f, 0f), closestPoint, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineClosestSurfacePointOnPlanes() {
		var closestPoint = TestSphere.ClosestPointToSurfaceOn(new Plane(Direction.Up, (0f, 1f, 0f)));

		Assert.AreEqual(1f, closestPoint.Y, TestTolerance);
		Assert.AreEqual(7.4f, closestPoint.DistanceFromOrigin(), TestTolerance);

		closestPoint = TestSphere.ClosestPointToSurfaceOn(new Plane(Direction.Up, (0f, 0f, 0f)));

		Assert.AreEqual(0f, closestPoint.Y, TestTolerance);
		Assert.AreEqual(7.4f, closestPoint.DistanceFromOrigin(), TestTolerance);
		Assert.AreEqual(true, MathF.Abs(closestPoint.X - 7.4f) < TestTolerance || MathF.Abs(closestPoint.Z - 7.4f) < TestTolerance);

		closestPoint = TestSphere.ClosestPointToSurfaceOn(new Plane(Direction.Up, (0f, 10f, 0f)));
		AssertToleranceEquals((0f, 10f, 0f), closestPoint, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyDetermineIncidentAngleWithLines() {
		const float LocalTestTolerance = 0.5f; // Needs to be a little higher as some of these calculations can be quite inaccurate

		void AssertAngle(Angle? expectation, Direction lineDir, Location originOffset) {
			var line = new Line(originOffset, lineDir);
			var ray = new Ray(originOffset - lineDir * 100f, lineDir);
			var boundedRay = new BoundedRay(originOffset - lineDir * 100f, originOffset + lineDir * 100f);

			AssertToleranceEquals(expectation, TestSphere.IncidentAngleWith(line), LocalTestTolerance);
			AssertToleranceEquals(expectation, TestSphere.IncidentAngleWith(ray), LocalTestTolerance);
			AssertToleranceEquals(expectation, TestSphere.IncidentAngleWith(boundedRay), LocalTestTolerance);
			Assert.AreEqual(null, TestSphere.IncidentAngleWith(boundedRay.WithLength(100f - (TestSphere.Radius + LocalTestTolerance))));
			if (expectation != null) {
				AssertToleranceEquals(expectation, TestSphere.FastIncidentAngleWith(line), LocalTestTolerance);
				AssertToleranceEquals(expectation, TestSphere.FastIncidentAngleWith(ray), LocalTestTolerance);
				AssertToleranceEquals(expectation, TestSphere.FastIncidentAngleWith(boundedRay), LocalTestTolerance);
			}

			line = new Line(originOffset + lineDir * -100f, -lineDir);
			ray = new Ray(originOffset + lineDir * 100f, -lineDir);
			boundedRay = boundedRay.Flipped;

			AssertToleranceEquals(expectation, TestSphere.IncidentAngleWith(line), LocalTestTolerance);
			AssertToleranceEquals(expectation, TestSphere.IncidentAngleWith(ray), LocalTestTolerance);
			AssertToleranceEquals(expectation, TestSphere.IncidentAngleWith(boundedRay), LocalTestTolerance);
			Assert.AreEqual(null, TestSphere.IncidentAngleWith(boundedRay.WithLength(100f - (TestSphere.Radius + LocalTestTolerance))));

			if (expectation != null) {
				AssertToleranceEquals(expectation, TestSphere.FastIncidentAngleWith(line), LocalTestTolerance);
				AssertToleranceEquals(expectation, TestSphere.FastIncidentAngleWith(ray), LocalTestTolerance);
				AssertToleranceEquals(expectation, TestSphere.FastIncidentAngleWith(boundedRay), LocalTestTolerance);
			}
		}

		AssertAngle(Angle.Zero, Direction.Down, Location.Origin);
		AssertAngle(Angle.Zero, (1f, 1f, 1f), Location.Origin);

		AssertAngle(30f, Direction.Down, (0f, 0f, TestSphere.Radius * 0.5f));
		AssertAngle(30f, Direction.Down, (0f, 0f, TestSphere.Radius * -0.5f));
		AssertAngle(30f, Direction.Down, (TestSphere.Radius * 0.5f, 0f, 0f));
		AssertAngle(30f, Direction.Down, (TestSphere.Radius * -0.5f, 0f, 0f));
		AssertAngle(30f, (-1f, -1f, -1f), Location.Origin + new Direction(-1f, -1f, -1f).AnyOrthogonal() * (TestSphere.Radius * 0.5f));

		AssertAngle(null, Direction.Left, (0f, 0f, TestSphere.Radius + LocalTestTolerance));

		Assert.IsNull(TestSphere.IncidentAngleWith(new Ray((0f, TestSphere.Radius + LocalTestTolerance, 0f), Direction.Up)));
		AssertToleranceEquals(0f, TestSphere.IncidentAngleWith(new Ray((0f, TestSphere.Radius - LocalTestTolerance, 0f), Direction.Up)), LocalTestTolerance);
		AssertToleranceEquals(0f, TestSphere.IncidentAngleWith(new Ray((0f, TestSphere.Radius + LocalTestTolerance, 0f), Direction.Down)), LocalTestTolerance);
		AssertToleranceEquals(0f, TestSphere.FastIncidentAngleWith(new Ray((0f, TestSphere.Radius - LocalTestTolerance, 0f), Direction.Up)), LocalTestTolerance);
		AssertToleranceEquals(0f, TestSphere.FastIncidentAngleWith(new Ray((0f, TestSphere.Radius + LocalTestTolerance, 0f), Direction.Down)), LocalTestTolerance);

		Assert.IsNull(TestSphere.IncidentAngleWith(new BoundedRay(new Location(0f, 100f, 0f), new Location(0f, TestSphere.Radius + LocalTestTolerance, 0f))));
		Assert.IsNull(TestSphere.IncidentAngleWith(new BoundedRay(new Location(0f, -(TestSphere.Radius - LocalTestTolerance), 0f), new Location(0f, TestSphere.Radius - LocalTestTolerance, 0f))));
		AssertToleranceEquals(0f, TestSphere.IncidentAngleWith(new BoundedRay(new Location(0f, 100f, 0f), new Location(0f, TestSphere.Radius - LocalTestTolerance, 0f))), LocalTestTolerance);
		AssertToleranceEquals(0f, TestSphere.IncidentAngleWith(new BoundedRay(new Location(0f, 100f, 0f), new Location(0f, TestSphere.Radius - LocalTestTolerance, 0f)).Flipped), LocalTestTolerance);
		AssertToleranceEquals(0f, TestSphere.IncidentAngleWith(new BoundedRay(new Location(0f, -100f, 0f), new Location(0f, -(TestSphere.Radius - LocalTestTolerance), 0f))), LocalTestTolerance);
		AssertToleranceEquals(0f, TestSphere.IncidentAngleWith(new BoundedRay(new Location(0f, -100f, 0f), new Location(0f, -(TestSphere.Radius - LocalTestTolerance), 0f)).Flipped), LocalTestTolerance);
		AssertToleranceEquals(30f, TestSphere.IncidentAngleWith(new BoundedRay(new Location(-100f, TestSphere.Radius * 0.5f, 0f), new Location(100f, TestSphere.Radius * 0.5f, 0f))), LocalTestTolerance);
		AssertToleranceEquals(30f, TestSphere.IncidentAngleWith(new BoundedRay(new Location(-100f, TestSphere.Radius * 0.5f, 0f), new Location(100f, TestSphere.Radius * 0.5f, 0f)).Flipped), LocalTestTolerance);
		AssertToleranceEquals(0f, TestSphere.FastIncidentAngleWith(new BoundedRay(new Location(0f, 100f, 0f), new Location(0f, TestSphere.Radius - LocalTestTolerance, 0f))), LocalTestTolerance);
		AssertToleranceEquals(0f, TestSphere.FastIncidentAngleWith(new BoundedRay(new Location(0f, 100f, 0f), new Location(0f, TestSphere.Radius - LocalTestTolerance, 0f)).Flipped), LocalTestTolerance);
		AssertToleranceEquals(0f, TestSphere.FastIncidentAngleWith(new BoundedRay(new Location(0f, -100f, 0f), new Location(0f, -(TestSphere.Radius - LocalTestTolerance), 0f))), LocalTestTolerance);
		AssertToleranceEquals(0f, TestSphere.FastIncidentAngleWith(new BoundedRay(new Location(0f, -100f, 0f), new Location(0f, -(TestSphere.Radius - LocalTestTolerance), 0f)).Flipped), LocalTestTolerance);
		AssertToleranceEquals(30f, TestSphere.FastIncidentAngleWith(new BoundedRay(new Location(-100f, TestSphere.Radius * 0.5f, 0f), new Location(100f, TestSphere.Radius * 0.5f, 0f))), LocalTestTolerance);
		AssertToleranceEquals(30f, TestSphere.FastIncidentAngleWith(new BoundedRay(new Location(-100f, TestSphere.Radius * 0.5f, 0f), new Location(100f, TestSphere.Radius * 0.5f, 0f)).Flipped), LocalTestTolerance);
	}

	[Test]
	public void ShouldCorrectlyReflectLines() {
		const float LocalTestTolerance = 0.5f; // Needs to be a little higher as some of these calculations can be quite inaccurate

		void AssertReflection(Ray? expectation, Direction lineDir, Location originOffset) {
			var ray = new Ray(originOffset - lineDir * 100f, lineDir);
			var boundedRay = new BoundedRay(originOffset - lineDir * 100f, originOffset + lineDir * 100f);

			AssertToleranceEquals(expectation, TestSphere.ReflectionOf(ray), LocalTestTolerance);
			AssertToleranceEquals(expectation?.ToBoundedRay(200f - boundedRay.StartPoint.DistanceFrom(expectation.Value.StartPoint)), TestSphere.ReflectionOf(boundedRay), LocalTestTolerance);
			Assert.AreEqual(null, TestSphere.ReflectionOf(boundedRay.WithLength(100f - (TestSphere.Radius + LocalTestTolerance))));
			if (expectation != null) {
				AssertToleranceEquals(expectation, TestSphere.FastReflectionOf(ray), LocalTestTolerance);
				AssertToleranceEquals(expectation.Value.ToBoundedRay(200f - boundedRay.StartPoint.DistanceFrom(expectation.Value.StartPoint)), TestSphere.FastReflectionOf(boundedRay), LocalTestTolerance);
			}
		}

		AssertReflection(new((0f, TestSphere.Radius, 0f), Direction.Up), Direction.Down, Location.Origin);
		AssertReflection(new(Location.Origin + new Direction(-1f, -1f, -1f) * TestSphere.Radius, (-1f, -1f, -1f)), (1f, 1f, 1f), Location.Origin);

		AssertReflection(
			new(TestSphere.FastIntersectionWith(new Ray((0f, 10f, TestSphere.Radius * 0.5f), Direction.Down)).First, Direction.Up * (Direction.Up >> Direction.Forward).WithAngle(60f)),
			Direction.Down,
			(0f, 0f, TestSphere.Radius * 0.5f)
		);
		AssertReflection(
			new(TestSphere.FastIntersectionWith(new Ray((0f, -10f, TestSphere.Radius * 0.5f), Direction.Up)).First, Direction.Down * (Direction.Down >> Direction.Forward).WithAngle(60f)),
			Direction.Up,
			(0f, 0f, TestSphere.Radius * 0.5f)
		);
		AssertReflection(
			new(TestSphere.FastIntersectionWith(new Ray((-20f, -20f, -20f), (1f, 1f, 1f))).First, (-1f, -1f, -1f)),
			(1f, 1f, 1f),
			Location.Origin
		);

		AssertReflection(null, Direction.Left, (0f, 0f, TestSphere.Radius + LocalTestTolerance));
		Assert.IsNull(TestSphere.ReflectionOf(new Ray((0f, TestSphere.Radius + LocalTestTolerance, 0f), Direction.Up)));
		AssertToleranceEquals(new Ray((0f, TestSphere.Radius, 0f), Direction.Down), TestSphere.ReflectionOf(new Ray((0f, TestSphere.Radius - LocalTestTolerance, 0f), Direction.Up)), LocalTestTolerance);
		AssertToleranceEquals(new Ray((0f, TestSphere.Radius, 0f), Direction.Up), TestSphere.ReflectionOf(new Ray((0f, TestSphere.Radius + LocalTestTolerance, 0f), Direction.Down)), LocalTestTolerance);
		AssertToleranceEquals(new Ray((0f, TestSphere.Radius, 0f), Direction.Down), TestSphere.FastReflectionOf(new Ray((0f, TestSphere.Radius - LocalTestTolerance, 0f), Direction.Up)), LocalTestTolerance);
		AssertToleranceEquals(new Ray((0f, TestSphere.Radius, 0f), Direction.Up), TestSphere.FastReflectionOf(new Ray((0f, TestSphere.Radius + LocalTestTolerance, 0f), Direction.Down)), LocalTestTolerance);

		Assert.IsNull(TestSphere.ReflectionOf(new BoundedRay(new Location(0f, 100f, 0f), new Location(0f, TestSphere.Radius + LocalTestTolerance, 0f))));
		Assert.IsNull(TestSphere.ReflectionOf(new BoundedRay(new Location(0f, -(TestSphere.Radius - LocalTestTolerance), 0f), new Location(0f, TestSphere.Radius - LocalTestTolerance, 0f))));
		AssertToleranceEquals(new BoundedRay(new Location(0f, TestSphere.Radius, 0f), new Location(0f, TestSphere.Radius + 1f, 0f)), TestSphere.ReflectionOf(new BoundedRay(new Location(0f, 100f, 0f), new Location(0f, TestSphere.Radius - 1f, 0f))), LocalTestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(0f, TestSphere.Radius, 0f), new Location(0f, TestSphere.Radius - (100f - (TestSphere.Radius - 1f) - 1f), 0f)), TestSphere.ReflectionOf(new BoundedRay(new Location(0f, 100f, 0f), new Location(0f, TestSphere.Radius - 1f, 0f)).Flipped), LocalTestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(0f, TestSphere.Radius, 0f), new Location(0f, TestSphere.Radius + 1f, 0f)), TestSphere.FastReflectionOf(new BoundedRay(new Location(0f, 100f, 0f), new Location(0f, TestSphere.Radius - 1f, 0f))), LocalTestTolerance);
		AssertToleranceEquals(new BoundedRay(new Location(0f, TestSphere.Radius, 0f), new Location(0f, TestSphere.Radius - (100f - (TestSphere.Radius - 1f) - 1f), 0f)), TestSphere.FastReflectionOf(new BoundedRay(new Location(0f, 100f, 0f), new Location(0f, TestSphere.Radius - 1f, 0f)).Flipped), LocalTestTolerance);
	}
}