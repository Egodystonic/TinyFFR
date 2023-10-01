// Created on 2023-09-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
class AngleTest {
	const float TestTolerance = 0.001f;

	[SetUp]
	public void SetUpTest() {

	}

	[Test]
	public void StaticReadonlyMembersShouldBeCorrectlyInitialized() {
		AssertToleranceEquals(Angle.FromRadians(0f), Angle.None, 0f);
		AssertToleranceEquals(Angle.FromRadians(MathF.PI * 0.5f), Angle.QuarterCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(MathF.PI), Angle.HalfCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(MathF.PI * 1.5f), Angle.ThreeQuarterCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(MathF.PI * 2f), Angle.FullCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(-MathF.PI * 0.5f), -Angle.QuarterCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(-MathF.PI), -Angle.HalfCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(-MathF.PI * 1.5f), -Angle.ThreeQuarterCircle, TestTolerance);
		AssertToleranceEquals(Angle.FromRadians(-MathF.PI * 2f), -Angle.FullCircle, TestTolerance);
	}

	[Test]
	public void PropertiesShouldCorrectlyConvertToAndFromRadians() {
		Assert.AreEqual(0f, Angle.None.Radians, 0f);
		Assert.AreEqual(0f, Angle.None.Degrees, 0f);
		Assert.AreEqual(0f, Angle.None.CoefficientOfFullCircle, 0f);

		Assert.AreEqual(MathF.PI * 0.5f, Angle.QuarterCircle.Radians, 0f);
		Assert.AreEqual(90f, Angle.QuarterCircle.Degrees, TestTolerance);
		Assert.AreEqual(0.25f, Angle.QuarterCircle.CoefficientOfFullCircle, TestTolerance);
		Assert.AreEqual(-MathF.PI * 0.5f, -Angle.QuarterCircle.Radians, 0f);
		Assert.AreEqual(-90f, -Angle.QuarterCircle.Degrees, TestTolerance);
		Assert.AreEqual(-0.25f, -Angle.QuarterCircle.CoefficientOfFullCircle, TestTolerance);

		Assert.AreEqual(MathF.PI, Angle.HalfCircle.Radians, 0f);
		Assert.AreEqual(180f, Angle.HalfCircle.Degrees, TestTolerance);
		Assert.AreEqual(0.5f, Angle.HalfCircle.CoefficientOfFullCircle, TestTolerance);
		Assert.AreEqual(-MathF.PI, -Angle.HalfCircle.Radians, 0f);
		Assert.AreEqual(-180f, -Angle.HalfCircle.Degrees, TestTolerance);
		Assert.AreEqual(-0.5f, -Angle.HalfCircle.CoefficientOfFullCircle, TestTolerance);

		Assert.AreEqual(MathF.PI * 1.5f, Angle.ThreeQuarterCircle.Radians, 0f);
		Assert.AreEqual(270f, Angle.ThreeQuarterCircle.Degrees, TestTolerance);
		Assert.AreEqual(0.75f, Angle.ThreeQuarterCircle.CoefficientOfFullCircle, TestTolerance);
		Assert.AreEqual(-MathF.PI * 1.5f, -Angle.ThreeQuarterCircle.Radians, 0f);
		Assert.AreEqual(-270f, -Angle.ThreeQuarterCircle.Degrees, TestTolerance);
		Assert.AreEqual(-0.75f, -Angle.ThreeQuarterCircle.CoefficientOfFullCircle, TestTolerance);

		Assert.AreEqual(MathF.PI * 2f, Angle.FullCircle.Radians, 0f);
		Assert.AreEqual(360f, Angle.FullCircle.Degrees, TestTolerance);
		Assert.AreEqual(1f, Angle.FullCircle.CoefficientOfFullCircle, TestTolerance);
		Assert.AreEqual(-MathF.PI * 2f, -Angle.FullCircle.Radians, 0f);
		Assert.AreEqual(-360f, -Angle.FullCircle.Degrees, TestTolerance);
		Assert.AreEqual(-1f, -Angle.FullCircle.CoefficientOfFullCircle, TestTolerance);

		for (var f = -4f; f <= 4.05f; f += 0.05f) {
			Assert.AreEqual(MathF.PI * 2f * f, new Angle(f).Radians, TestTolerance);
			Assert.AreEqual(360f * f, new Angle(f).Degrees, TestTolerance);
			Assert.AreEqual(f, new Angle(f).CoefficientOfFullCircle, TestTolerance);
		}		
	}

	[Test]
	public void ConstructorsAndConversionsShouldCorrectlyInitializeValue() {
		for (var f = -4f; f <= 4.05f; f += 0.05f) {
			AssertToleranceEquals(Angle.FromCoefficientOfFullCircle(f), new Angle(f), 0f);
			AssertToleranceEquals(Angle.FromCoefficientOfFullCircle(f), f, 0f);
		}
	}

	[Test]
	public void FactoryMethodsShouldCorrectlyInitializeValue() {
		// radians
		for (var f = -MathF.Tau * 2f; f < MathF.Tau * 2.05f; f += MathF.Tau * 0.05f) {
			Assert.AreEqual(f, Angle.FromRadians(f).Radians);
		}

		// degrees
		for (var f = -720f; f < 720f + 36f; f += 36f) {
			Assert.AreEqual(f, Angle.FromDegrees(f).Degrees, TestTolerance);
		}

		// coefficient
		for (var f = -2f; f < 2.05f; f += 0.05f) {
			Assert.AreEqual(f, Angle.FromCoefficientOfFullCircle(f).CoefficientOfFullCircle, TestTolerance);
			Assert.AreEqual(f, Angle.FromCoefficientOfFullCircle(Fraction.FromCoefficient(f)).CoefficientOfFullCircle, TestTolerance);
		}
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		void AssertIteration(Angle input) {
			var span = Angle.ConvertToSpan(input);
			Assert.AreEqual(1, span.Length);
			Assert.AreEqual(input.Radians, span[0]);
			Assert.AreEqual(input, Angle.ConvertFromSpan(span));
		}

		for (var f = -2f; f < 2.05f; f += 0.05f) AssertIteration(f);

		var noneSpan = Angle.ConvertToSpan(Angle.None);
		var quarterSpan = Angle.ConvertToSpan(Angle.QuarterCircle);
		var halfSpan = Angle.ConvertToSpan(Angle.HalfCircle);
		var threeQuarterSpan = Angle.ConvertToSpan(Angle.ThreeQuarterCircle);
		var fullSpan = Angle.ConvertToSpan(Angle.FullCircle);
		
		Assert.AreEqual(0f, noneSpan[0]);
		Assert.AreEqual(MathF.PI * 0.5f, quarterSpan[0]);
		Assert.AreEqual(MathF.PI, halfSpan[0]);
		Assert.AreEqual(MathF.PI * 1.5f, threeQuarterSpan[0]);
		Assert.AreEqual(MathF.PI * 2f, fullSpan[0]);

		Assert.AreEqual(Angle.None, Angle.ConvertFromSpan(new ReadOnlySpan<float>(0f)));
		Assert.AreEqual(Angle.QuarterCircle, Angle.ConvertFromSpan(new ReadOnlySpan<float>(MathF.PI * 0.5f)));
		Assert.AreEqual(Angle.HalfCircle, Angle.ConvertFromSpan(new ReadOnlySpan<float>(MathF.PI)));
		Assert.AreEqual(Angle.ThreeQuarterCircle, Angle.ConvertFromSpan(new ReadOnlySpan<float>(MathF.PI * 1.5f)));
		Assert.AreEqual(Angle.FullCircle, Angle.ConvertFromSpan(new ReadOnlySpan<float>(MathF.PI * 2f)));
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		void AssertIteration(Angle input, string expectedStringValuePart) {
			var testCulture = CultureInfo.InvariantCulture;
			var testFormat = "N0";
			var expectedValue = $"{expectedStringValuePart}{Angle.StringSuffix}";

			Span<char> formatSpan = stackalloc char[expectedValue.Length];
			Assert.IsTrue(input.TryFormat(formatSpan, out var charsWritten, testFormat, testCulture));
			Assert.AreEqual(formatSpan.Length, charsWritten);
			
			Assert.AreEqual(expectedValue, input.ToString(testFormat, testCulture));
			Assert.AreEqual(expectedValue, new String(formatSpan));
		}

		AssertIteration(Angle.None, "0");
		AssertIteration(Angle.QuarterCircle, "90");
		AssertIteration(Angle.HalfCircle, "180");
		AssertIteration(Angle.ThreeQuarterCircle, "270");
		AssertIteration(Angle.FullCircle, "360");
		AssertIteration(Angle.FullCircle * 2f, "720");
		AssertIteration(-Angle.QuarterCircle, "-90");
		AssertIteration(-Angle.HalfCircle, "-180");
		AssertIteration(-Angle.ThreeQuarterCircle, "-270");
		AssertIteration(-Angle.FullCircle, "-360");
		AssertIteration(-Angle.FullCircle * 2f, "-720");
	}

	// TODO next step I want to test bad inputs to TryFormat are handled correctly
}