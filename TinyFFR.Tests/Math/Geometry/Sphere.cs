// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
class SphereTest {
	const float TestTolerance = 0.01f;

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyCalculateProperties() {
		// https://www.wolframalpha.com/input?i=volume%2C+surface+area%2C+circumference%2C+diameter+of+sphere+with+radius+7.4
		var sphere = new Sphere(7.4f);
		Assert.AreEqual(7.4f, sphere.Radius, TestTolerance);
		Assert.AreEqual(14.8f, sphere.Diameter, TestTolerance);
		Assert.AreEqual(46.4956f, sphere.Circumference, TestTolerance);
		Assert.AreEqual(688.134f, sphere.SurfaceArea, TestTolerance);
		Assert.AreEqual(1697.4f, sphere.Volume, TestTolerance);
		Assert.AreEqual(7.4f * 7.4f, sphere.RadiusSquared, TestTolerance);
	}

	[Test]
	public void StaticFactoryMethodsShouldCorrectlyConstruct() {
		// https://www.wolframalpha.com/input?i=volume%2C+surface+area%2C+circumference%2C+diameter+of+sphere+with+radius+7.4
		var sphere = new Sphere(7.4f);

		AssertToleranceEquals(sphere, Sphere.FromDiameter(14.8f), TestTolerance);
		AssertToleranceEquals(sphere, Sphere.FromCircumference(46.4956f), TestTolerance);
		AssertToleranceEquals(sphere, Sphere.FromSurfaceArea(688.134f), TestTolerance);
		AssertToleranceEquals(sphere, Sphere.FromVolume(1697.4f), TestTolerance);
		AssertToleranceEquals(sphere, Sphere.FromRadiusSquared(7.4f * 7.4f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyScale() {
		AssertToleranceEquals(new Sphere(7.4f * 3f), new Sphere(7.4f).ScaledBy(3f), TestTolerance);
	}

	// TODO this test could be fleshed out a lot more
	[Test]
	public void ShouldCorrectlyConvertToString() {
		const string Expectation = "Sphere[Radius 7.4]";
		Assert.AreEqual(Expectation, new Sphere(7.4f).ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		new Sphere(7.4f).TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture);
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
	}

	// TODO this test could be fleshed out a lot more
	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = "Sphere[Radius 7.4]";
		Assert.AreEqual(new Sphere(7.4f), Sphere.Parse(Input, CultureInfo.InvariantCulture));
		Assert.AreEqual(true, Sphere.TryParse(Input, CultureInfo.InvariantCulture, out var result));
		Assert.AreEqual(new Sphere(7.4f), result);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		Assert.AreEqual(1, Sphere.ConvertToSpan(new Sphere(7.4f)).Length);
		Assert.AreEqual(7.4f, Sphere.ConvertToSpan(new Sphere(7.4f))[0]);
		Assert.AreEqual(new Sphere(7.4f), Sphere.ConvertFromSpan(Sphere.ConvertToSpan(new Sphere(7.4f))));
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
			var val = Sphere.CreateNewRandom(new Sphere(10f), new Sphere(20f));
			Assert.GreaterOrEqual(val.Radius, 10f);
			Assert.Less(val.Radius, 20f);

			val = Sphere.CreateNewRandom();
			Assert.GreaterOrEqual(val.Radius, Sphere.DefaultRandomMin);
			Assert.Less(val.Radius, Sphere.DefaultRandomMax);
		}
	}
}