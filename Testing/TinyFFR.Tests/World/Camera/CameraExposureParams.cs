// Created on 2026-10-05 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Globalization;

namespace Egodystonic.TinyFFR.World.Camera;

[TestFixture]
class CameraExposureParamsTest {
	const float TestTolerance = 0.001f;
	static readonly CameraExposureParams TestParams = new(4f, 0.5f, 200f);

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<CameraExposureParams>();
		ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(
			TestParams,
			default,
			CameraExposurePreset.OutsideMidday.ToExposureParams(),
			CameraExposurePreset.OutsideStarlight.ToExposureParams(),
			new CameraExposureParams(-1f, Single.NaN, Single.PositiveInfinity)
		);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(default(CameraExposureParams), 0f, 0f, 0f);
		ByteSpanSerializationTestUtils.AssertLittleEndianSingles(TestParams, 4f, 0.5f, 200f);
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		const string Expectation = "CameraExposureParams[Aperture 4.0 | ShutterSpeed 0.5 | Sensitivity 200.0]";
		Assert.AreEqual(Expectation, TestParams.ToString("N1", CultureInfo.InvariantCulture));
		Span<char> dest = stackalloc char[Expectation.Length * 2];
		Assert.IsTrue(TestParams.TryFormat(dest, out var numCharsWritten, "N1", CultureInfo.InvariantCulture));
		Assert.AreEqual(Expectation.Length, numCharsWritten);
		Assert.AreEqual(Expectation, new String(dest[..numCharsWritten]));
		Assert.IsFalse(TestParams.TryFormat(dest[..10], out _, "N1", CultureInfo.InvariantCulture));
		Assert.That(TestParams.ToString(), Does.StartWith("CameraExposureParams[Aperture "));
	}

	[Test]
	public void ShouldCorrectlyParse() {
		const string Input = "CameraExposureParams[Aperture 4.0 | ShutterSpeed 0.5 | Sensitivity 200.0]";
		Assert.AreEqual(TestParams, CameraExposureParams.Parse(Input, CultureInfo.InvariantCulture));
		Assert.AreEqual(TestParams, CameraExposureParams.Parse(Input.AsSpan(), CultureInfo.InvariantCulture));
		Assert.IsTrue(CameraExposureParams.TryParse(Input, CultureInfo.InvariantCulture, out var result));
		Assert.AreEqual(TestParams, result);
		Assert.IsTrue(CameraExposureParams.TryParse(Input.AsSpan(), CultureInfo.InvariantCulture, out result));
		Assert.AreEqual(TestParams, result);

		Assert.IsFalse(CameraExposureParams.TryParse("not exposure params", CultureInfo.InvariantCulture, out _));
		Assert.IsFalse(CameraExposureParams.TryParse((string?) null, CultureInfo.InvariantCulture, out _));

		var roundTripped = CameraExposureParams.Parse(CameraExposurePreset.OutsideTwilight.ToExposureParams().ToString("N8", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
		Assert.IsTrue(CameraExposurePreset.OutsideTwilight.ToExposureParams().Equals(roundTripped, TestTolerance));
	}

	[Test]
	public void ShouldCorrectlyImplementEqualityMembers() {
		Assert.AreNotEqual(default(CameraExposureParams), TestParams);
		Assert.IsTrue(TestParams.Equals(new CameraExposureParams(4f, 0.5f, 200f)));
		Assert.IsTrue(TestParams == new CameraExposureParams(4f, 0.5f, 200f));
		Assert.IsFalse(TestParams != new CameraExposureParams(4f, 0.5f, 200f));
		Assert.IsFalse(TestParams == new CameraExposureParams(4f, 0.5f, 201f));
		Assert.IsTrue(TestParams != default);
		Assert.AreEqual(TestParams.GetHashCode(), new CameraExposureParams(4f, 0.5f, 200f).GetHashCode());

		Assert.IsTrue(TestParams.Equals(TestParams, 0f));
		Assert.IsTrue(TestParams.Equals(new CameraExposureParams(4.09f, 0.41f, 200.09f), 0.1f));
		Assert.IsFalse(TestParams.Equals(new CameraExposureParams(4.11f, 0.5f, 200f), 0.1f));
		Assert.IsFalse(TestParams.Equals(new CameraExposureParams(4f, 0.39f, 200f), 0.1f));
		Assert.IsFalse(TestParams.Equals(new CameraExposureParams(4f, 0.5f, 199.89f), 0.1f));
	}

	[Test]
	public void ShouldCorrectlyCalculateEv100() {
		Assert.AreEqual(15f, CameraExposurePreset.OutsideMidday.ToExposureParams().Ev100, 0.05f);
		Assert.AreEqual(TestParams.Ev100 - 1f, (TestParams with { ShutterSpeed = 1f }).Ev100, TestTolerance);
		Assert.AreEqual(TestParams.Ev100 + 2f, (TestParams with { Aperture = 8f }).Ev100, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyMultiplyAndDivide() {
		Assert.AreEqual(new CameraExposureParams(4f, 0.5f, 400f), TestParams * 2f);
		Assert.AreEqual(new CameraExposureParams(4f, 0.5f, 400f), 2f * TestParams);
		Assert.AreEqual(new CameraExposureParams(4f, 0.5f, 50f), TestParams / 4f);
		Assert.AreEqual(TestParams * 3f, TestParams.WithSensitivityScaledBy(3f));

		static TResult MultipliedBy<T, TResult>(T value, float other) where T : IMultiplicative<T, float, TResult> => value.MultipliedBy(other);
		static TResult DividedBy<T, TResult>(T value, float other) where T : IMultiplicative<T, float, TResult> => value.DividedBy(other);
		Assert.AreEqual(TestParams * 2f, MultipliedBy<CameraExposureParams, CameraExposureParams>(TestParams, 2f));
		Assert.AreEqual(TestParams / 2f, DividedBy<CameraExposureParams, CameraExposureParams>(TestParams, 2f));
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		var start = new CameraExposureParams(2f, 1f / 30f, 100f);
		var end = new CameraExposureParams(8f, 1f / 120f, 6_400f);

		Assert.IsTrue(start.Equals(CameraExposureParams.Interpolate(start, end, 0f), TestTolerance));
		Assert.IsTrue(end.Equals(CameraExposureParams.Interpolate(start, end, 1f), TestTolerance));
		Assert.IsTrue(new CameraExposureParams(4f, 1f / 60f, 800f).Equals(CameraExposureParams.Interpolate(start, end, 0.5f), TestTolerance));
		Assert.IsTrue(new CameraExposureParams(16f, 1f / 240f, 51_200f).Equals(CameraExposureParams.Interpolate(start, end, 1.5f), 0.01f));
		Assert.IsTrue(new CameraExposureParams(1f, 1f / 15f, 12.5f).Equals(CameraExposureParams.Interpolate(start, end, -0.5f), TestTolerance));
		Assert.AreEqual(start, CameraExposureParams.Interpolate(start, start, 0.7f));

		for (var d = -1f; d <= 2f; d += 0.25f) {
			var expectedEv = Single.Lerp(start.Ev100, end.Ev100, d);
			Assert.AreEqual(expectedEv, CameraExposureParams.Interpolate(start, end, d).Ev100, TestTolerance);
		}

		Assert.AreEqual(new CameraExposureParams(5f, (1f / 30f + 1f / 120f) * 0.5f, 3_250f), CameraExposureParams.InterpolateArithmetically(start, end, 0.5f));
		Assert.AreEqual(start, CameraExposureParams.InterpolateArithmetically(start, end, 0f));
		Assert.AreEqual(end, CameraExposureParams.InterpolateArithmetically(start, end, 1f));

		static T Blend<T>(T start, T end, float distance) where T : IBlendable<T> => T.Blend(start, end, distance);
		Assert.AreEqual(CameraExposureParams.Interpolate(start, end, 0.3f), Blend(start, end, 0.3f));
	}

	[Test]
	public void ShouldCorrectlyClamp() {
		var min = new CameraExposureParams(2f, 0.1f, 100f);
		var max = new CameraExposureParams(8f, 1f, 800f);

		Assert.AreEqual(TestParams, TestParams.Clamp(min, max));
		Assert.AreEqual(TestParams, TestParams.Clamp(max, min));
		Assert.AreEqual(min, new CameraExposureParams(1f, 0.01f, 10f).Clamp(min, max));
		Assert.AreEqual(max, new CameraExposureParams(16f, 2f, 1_600f).Clamp(min, max));
		Assert.AreEqual(new CameraExposureParams(2f, 0.5f, 800f), new CameraExposureParams(1f, 0.5f, 1_600f).Clamp(min, max));
		Assert.AreEqual(new CameraExposureParams(2f, 0.5f, 800f), new CameraExposureParams(1f, 0.5f, 1_600f).Clamp(min with { Sensitivity = 800f }, max with { Sensitivity = 100f }));
	}

	[Test]
	public void ShouldCorrectlyCreateRandomValues() {
		const int NumIterations = 10_000;
		var min = new CameraExposureParams(2f, 1f / 1_000f, 100f);
		var max = new CameraExposureParams(16f, 1f, 6_400f);
		var numSensitivitiesBelowGeometricMidpoint = 0;

		for (var i = 0; i < NumIterations; ++i) {
			var bounded = CameraExposureParams.Random(min, max);
			Assert.That(bounded.Aperture, Is.GreaterThanOrEqualTo(min.Aperture).And.LessThan(max.Aperture));
			Assert.That(bounded.ShutterSpeed, Is.GreaterThanOrEqualTo(min.ShutterSpeed).And.LessThan(max.ShutterSpeed));
			Assert.That(bounded.Sensitivity, Is.GreaterThanOrEqualTo(min.Sensitivity).And.LessThan(max.Sensitivity));
			if (bounded.Sensitivity < 800f) ++numSensitivitiesBelowGeometricMidpoint;

			var unbounded = CameraExposureParams.Random();
			Assert.That(unbounded.Aperture, Is.InRange(CameraExposureParams.ApertureMin, CameraExposureParams.ApertureMax));
			Assert.That(unbounded.ShutterSpeed, Is.InRange(CameraExposureParams.ShutterSpeedMin, CameraExposureParams.ShutterSpeedMax));
			Assert.That(unbounded.Sensitivity, Is.InRange(CameraExposureParams.SensitivityMin, CameraExposureParams.SensitivityMax));
		}

		Assert.That(numSensitivitiesBelowGeometricMidpoint, Is.InRange(NumIterations * 0.45f, NumIterations * 0.55f));
		Assert.AreEqual(TestParams, CameraExposureParams.Random(TestParams, TestParams));
	}

	[Test]
	public void ShouldConstructFromPreset() {
		foreach (var preset in Enum.GetValues<CameraExposurePreset>()) {
			Assert.AreEqual(preset.ToExposureParams(), new CameraExposureParams(preset));
			CameraExposureParams converted = preset;
			Assert.AreEqual(preset.ToExposureParams(), converted);
		}
	}
}
