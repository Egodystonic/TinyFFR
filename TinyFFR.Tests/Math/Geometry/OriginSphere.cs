// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class OriginSphereTest {
	const float TestTolerance = 0.01f;
	static readonly OriginSphere TestSphere = new(7.4f);

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

		AssertToleranceEquals(TestSphere, OriginSphere.FromDiameter(14.8f), TestTolerance);
		AssertToleranceEquals(TestSphere, OriginSphere.FromCircumference(46.4956f), TestTolerance);
		AssertToleranceEquals(TestSphere, OriginSphere.FromSurfaceArea(688.134f), TestTolerance);
		AssertToleranceEquals(TestSphere, OriginSphere.FromVolume(1697.4f), TestTolerance);
		AssertToleranceEquals(TestSphere, OriginSphere.FromRadiusSquared(7.4f * 7.4f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		const string Expectation = "OriginSphere[Radius 7.4]";
		Assert.AreEqual(Expectation, TestSphere.ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		TestSphere.TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture);
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
	}

	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = "OriginSphere[Radius 7.4]";
		Assert.AreEqual(TestSphere, OriginSphere.Parse(Input, CultureInfo.InvariantCulture));
		Assert.AreEqual(true, OriginSphere.TryParse(Input, CultureInfo.InvariantCulture, out var result));
		Assert.AreEqual(TestSphere, result);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<OriginSphere>();
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(TestSphere);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(TestSphere, TestSphere.Radius);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		Assert.AreEqual(new OriginSphere(10f), OriginSphere.Interpolate(new(5f), new(15f), 0.5f));
		Assert.AreEqual(new OriginSphere(5f), OriginSphere.Interpolate(new(5f), new(15f), 0f));
		Assert.AreEqual(new OriginSphere(15f), OriginSphere.Interpolate(new(5f), new(15f), 1f));
		Assert.AreEqual(new OriginSphere(20f), OriginSphere.Interpolate(new(5f), new(15f), 1.5f));
		Assert.AreEqual(new OriginSphere(0f), OriginSphere.Interpolate(new(5f), new(15f), -0.5f));
	}

	[Test]
	public void ShouldCorrectlyCreateRandomObjects() {
		const int NumIterations = 10_000;
		
		for (var i = 0; i < NumIterations; ++i) {
			var val = OriginSphere.NewRandom(new OriginSphere(10f), new OriginSphere(20f));
			Assert.GreaterOrEqual(val.Radius, 10f);
			Assert.Less(val.Radius, 20f);

			val = OriginSphere.NewRandom();
			Assert.GreaterOrEqual(val.Radius, OriginSphere.DefaultRandomMin);
			Assert.Less(val.Radius, OriginSphere.DefaultRandomMax);
		}
	}
}