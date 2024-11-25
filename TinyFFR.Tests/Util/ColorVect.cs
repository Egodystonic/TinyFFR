// Created on 2024-08-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Numerics;

namespace Egodystonic.TinyFFR;

[TestFixture]
class ColorVectTest {
	const float TestTolerance = 1E-5f;
	static readonly ColorVect ThreeSixNineHalf = new(0.3f, 0.6f, 0.9f, 0.5f);
	static readonly ((float H, float S, float L) Hsl, (float R, float G, float B) Rgb)[] HslToRgbTestValues = {
		((H: 000.000f, S: 1.000f, L: 0.500f),	(R: 1.000f, G: 0.000f, B: 0.000f)),
		((H: 120.000f, S: 1.000f, L: 0.500f),	(R: 0.000f, G: 1.000f, B: 0.000f)),
		((H: 240.000f, S: 1.000f, L: 0.500f),	(R: 0.000f, G: 0.000f, B: 1.000f)),
		((H: 360.000f, S: 1.000f, L: 0.500f),   (R: 1.000f, G: 0.000f, B: 0.000f)),
		((H: 480.000f, S: 1.000f, L: 0.500f),   (R: 0.000f, G: 1.000f, B: 0.000f)),
		((H: 600.000f, S: 1.000f, L: 0.500f),   (R: 0.000f, G: 0.000f, B: 1.000f)),
		((H:-360.000f, S: 1.000f, L: 0.500f),   (R: 1.000f, G: 0.000f, B: 0.000f)),
		((H:-240.000f, S: 1.000f, L: 0.500f),   (R: 0.000f, G: 1.000f, B: 0.000f)),
		((H:-120.000f, S: 1.000f, L: 0.500f),   (R: 0.000f, G: 0.000f, B: 1.000f)),
	}; // TODO lots more

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	void AssertAllHslRgbPairTuples(Action<(Angle Hue, float Sat, float Light), (float R, float G, float B)> assertionAction) {
		for (var i = 0; i < HslToRgbTestValues.Length; ++i) {
			var tuple = HslToRgbTestValues[i];
			try {
				assertionAction((tuple.Hsl.H, tuple.Hsl.S, tuple.Hsl.L), tuple.Rgb);
			}
			catch {
				Console.WriteLine($"Index: {i}");
				Console.WriteLine($"HSL: ({tuple.Hsl.H}, {tuple.Hsl.S}, {tuple.Hsl.L})");
				Console.WriteLine($"RGB: ({tuple.Rgb.R}, {tuple.Rgb.G}, {tuple.Rgb.B})");
				throw;
			}
		}
	}

	void AssertAllHslRgbPairVects(Action<ColorVect, ColorVect> assertionAction) {
		for (var i = 0; i < HslToRgbTestValues.Length; ++i) {
			var tuple = HslToRgbTestValues[i];
			var hslVect = ColorVect.FromHueSaturationLightness(tuple.Hsl.H, tuple.Hsl.S, tuple.Hsl.L);
			var rgbVect = new ColorVect(tuple.Rgb.R, tuple.Rgb.G, tuple.Rgb.B);
			try {
				assertionAction(hslVect, rgbVect);
			}
			catch {
				Console.WriteLine($"Index: {i}");
				Console.WriteLine($"HSL: ({tuple.Hsl.H}, {tuple.Hsl.S}, {tuple.Hsl.L}) => {hslVect}");
				Console.WriteLine($"RGB: ({tuple.Rgb.R}, {tuple.Rgb.G}, {tuple.Rgb.B}) => {rgbVect}");
				throw;
			}
		}
	}

	[Test]
	public void ShouldCorrectlyAssignStaticMembers() {
		AssertToleranceEquals(new ColorVect(1f, 0f, 0f), ColorVect.FromHueSaturationLightness(ColorVect.RedHueAngle, 1f, 0.5f), TestTolerance);
		AssertToleranceEquals(new ColorVect(0f, 1f, 0f), ColorVect.FromHueSaturationLightness(ColorVect.GreenHueAngle, 1f, 0.5f), TestTolerance);
		AssertToleranceEquals(new ColorVect(0f, 0f, 1f), ColorVect.FromHueSaturationLightness(ColorVect.BlueHueAngle, 1f, 0.5f), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyImplementRgbaProperties() {
		Assert.AreEqual(0.3f, ThreeSixNineHalf.Red);
		Assert.AreEqual(0.6f, ThreeSixNineHalf.Green);
		Assert.AreEqual(0.9f, ThreeSixNineHalf.Blue);
		Assert.AreEqual(0.5f, ThreeSixNineHalf.Alpha);

		Assert.AreEqual(0.3f, ThreeSixNineHalf.AsVector4.X);
		Assert.AreEqual(0.6f, ThreeSixNineHalf.AsVector4.Y);
		Assert.AreEqual(0.9f, ThreeSixNineHalf.AsVector4.Z);
		Assert.AreEqual(0.5f, ThreeSixNineHalf.AsVector4.W);

		Assert.AreEqual(0.3f, ((IVect) ThreeSixNineHalf).X);
		Assert.AreEqual(0.6f, ((IVect) ThreeSixNineHalf).Y);
		Assert.AreEqual(0.9f, ((IVect) ThreeSixNineHalf).Z);

		var lessOne = ThreeSixNineHalf with {
			Red = 0.2f,
			Green = 0.5f,
			Blue = 0.8f,
			Alpha = 0.4f
		};

		Assert.AreEqual(0.2f, lessOne.Red);
		Assert.AreEqual(0.5f, lessOne.Green);
		Assert.AreEqual(0.8f, lessOne.Blue);
		Assert.AreEqual(0.4f, lessOne.Alpha);
	}

	[Test]
	public void ShouldCorrectlyImplementHslProperties() {
		AssertToleranceEquals(ColorVect.RedHueAngle, new ColorVect(1f, 0f, 0f).Hue, TestTolerance);
		AssertToleranceEquals(ColorVect.GreenHueAngle, new ColorVect(0f, 1f, 0f).Hue, TestTolerance);
		AssertToleranceEquals(ColorVect.BlueHueAngle, new ColorVect(0f, 0f, 1f).Hue, TestTolerance);

		Assert.AreEqual(1f, new ColorVect(1f, 0f, 0f).Saturation, TestTolerance);
		Assert.AreEqual(1f, new ColorVect(0f, 1f, 0f).Saturation, TestTolerance);
		Assert.AreEqual(1f, new ColorVect(0f, 0f, 1f).Saturation, TestTolerance);

		Assert.AreEqual(0.5f, new ColorVect(1f, 0f, 0f).Lightness, TestTolerance);
		Assert.AreEqual(0.5f, new ColorVect(0f, 1f, 0f).Lightness, TestTolerance);
		Assert.AreEqual(0.5f, new ColorVect(0f, 0f, 1f).Lightness, TestTolerance);

		AssertAllHslRgbPairTuples(
			(hsl, rgb) => {
				var v = (ColorVect) rgb;
				Assert.IsTrue(hsl.Hue.EqualsWithinCircle(v.Hue, TestTolerance));
				Assert.AreEqual(hsl.Sat, v.Saturation, TestTolerance);
				Assert.AreEqual(hsl.Light, v.Lightness, TestTolerance);
			}
		);
	}

	[Test]
	public void ShouldCorrectlyImplementIndexers() {
		Assert.AreEqual(ThreeSixNineHalf.Red, ThreeSixNineHalf[ColorChannel.R]);
		Assert.AreEqual(ThreeSixNineHalf.Green, ThreeSixNineHalf[ColorChannel.G]);
		Assert.AreEqual(ThreeSixNineHalf.Blue, ThreeSixNineHalf[ColorChannel.B]);
		Assert.AreEqual(ThreeSixNineHalf.Alpha, ThreeSixNineHalf[ColorChannel.A]);

		Assert.AreEqual(
			new XYPair<float>(ThreeSixNineHalf.Red, ThreeSixNineHalf.Green),
			ThreeSixNineHalf[ColorChannel.R, ColorChannel.G]
		);
		Assert.AreEqual(
			new XYPair<float>(ThreeSixNineHalf.Blue, ThreeSixNineHalf.Alpha),
			ThreeSixNineHalf[ColorChannel.B, ColorChannel.A]
		);

		Assert.AreEqual(
			ThreeSixNineHalf with { Alpha = 1f },
			ThreeSixNineHalf[ColorChannel.R, ColorChannel.G, ColorChannel.B]
		);
		Assert.AreEqual(
			new ColorVect(0.6f, 0.9f, 0.5f),
			ThreeSixNineHalf[ColorChannel.G, ColorChannel.B, ColorChannel.A]
		);

		Assert.AreEqual(
			new ColorVect(0.6f, 0.9f, 0.5f, 0.3f),
			ThreeSixNineHalf[ColorChannel.G, ColorChannel.B, ColorChannel.A, ColorChannel.R]
		);


		var asVect = (IVect<ColorVect>) ThreeSixNineHalf;

		Assert.AreEqual(ThreeSixNineHalf.Red, asVect[Axis.X]);
		Assert.AreEqual(ThreeSixNineHalf.Green, asVect[Axis.Y]);
		Assert.AreEqual(ThreeSixNineHalf.Blue, asVect[Axis.Z]);

		Assert.AreEqual(
			new XYPair<float>(ThreeSixNineHalf.Red, ThreeSixNineHalf.Green),
			asVect[Axis.X, Axis.Y]
		);
		Assert.AreEqual(
			new XYPair<float>(ThreeSixNineHalf.Blue, ThreeSixNineHalf.Red),
			asVect[Axis.Z, Axis.X]
		);

		Assert.AreEqual(
			ThreeSixNineHalf with { Alpha = 1f },
			asVect[Axis.X, Axis.Y, Axis.Z]
		);
		Assert.AreEqual(
			new ColorVect(0.6f, 0.9f, 0.3f),
			asVect[Axis.Y, Axis.Z, Axis.X]
		);
	}

	[Test]
	public void ShouldCorrectlyConstruct() {
		Assert.AreEqual(new ColorVect(0f, 0f, 0f, 1f), new ColorVect());
		Assert.AreEqual(new ColorVect(0.1f, 0.2f, 0.3f, 1f), new ColorVect(0.1f, 0.2f, 0.3f));
		Assert.AreEqual(ThreeSixNineHalf, new ColorVect(0.3f, 0.6f, 0.9f, 0.5f));
		Assert.AreEqual(ThreeSixNineHalf, new ColorVect(new Vector4(0.3f, 0.6f, 0.9f, 0.5f)));
	}

	[Test]
	public void ShouldCorrectlyPremultiplyAlpha() {
		AssertToleranceEquals(
			new ColorVect(0.15f, 0.3f, 0.45f, 0.5f),
			ColorVect.PremultiplyAlpha(ThreeSixNineHalf),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyConvertFromRgba32() {
		// Firstly, we want to specifically ensure that 0f and 1f are representable
		Assert.AreEqual(1f, ColorVect.FromRgba32(0xFFFFFFFFU).Red);
		Assert.AreEqual(1f, ColorVect.FromRgba32(0xFFFFFFFFU).Green);
		Assert.AreEqual(1f, ColorVect.FromRgba32(0xFFFFFFFFU).Blue);
		Assert.AreEqual(1f, ColorVect.FromRgba32(0xFFFFFFFFU).Alpha);
		Assert.AreEqual(1f, ColorVect.FromRgba32(unchecked((int) 0xFFFFFFFF)).Red);
		Assert.AreEqual(1f, ColorVect.FromRgba32(unchecked((int) 0xFFFFFFFF)).Green);
		Assert.AreEqual(1f, ColorVect.FromRgba32(unchecked((int) 0xFFFFFFFF)).Blue);
		Assert.AreEqual(1f, ColorVect.FromRgba32(unchecked((int) 0xFFFFFFFF)).Alpha);
		Assert.AreEqual(1f, ColorVect.FromRgba32(0xFF, 0xFF, 0xFF, 0xFF).Red);
		Assert.AreEqual(1f, ColorVect.FromRgba32(0xFF, 0xFF, 0xFF, 0xFF).Green);
		Assert.AreEqual(1f, ColorVect.FromRgba32(0xFF, 0xFF, 0xFF, 0xFF).Blue);
		Assert.AreEqual(1f, ColorVect.FromRgba32(0xFF, 0xFF, 0xFF, 0xFF).Alpha);

		Assert.AreEqual(0f, ColorVect.FromRgba32(0U).Red);
		Assert.AreEqual(0f, ColorVect.FromRgba32(0U).Green);
		Assert.AreEqual(0f, ColorVect.FromRgba32(0U).Blue);
		Assert.AreEqual(0f, ColorVect.FromRgba32(0U).Alpha);
		Assert.AreEqual(0f, ColorVect.FromRgba32(0).Red);
		Assert.AreEqual(0f, ColorVect.FromRgba32(0).Green);
		Assert.AreEqual(0f, ColorVect.FromRgba32(0).Blue);
		Assert.AreEqual(0f, ColorVect.FromRgba32(0).Alpha);
		Assert.AreEqual(0f, ColorVect.FromRgba32(0, 0, 0, 0).Red);
		Assert.AreEqual(0f, ColorVect.FromRgba32(0, 0, 0, 0).Green);
		Assert.AreEqual(0f, ColorVect.FromRgba32(0, 0, 0, 0).Blue);
		Assert.AreEqual(0f, ColorVect.FromRgba32(0, 0, 0, 0).Alpha);

		// Now, just make sure all values are assigned as expected
		AssertToleranceEquals(
			new ColorVect(32f / 255f, 64f / 255f, 96f / 255f, 128f / 255f),
			ColorVect.FromRgba32(0x20406080),
			TestTolerance
		);
		AssertToleranceEquals(
			new ColorVect(32f / 255f, 64f / 255f, 96f / 255f, 128f / 255f),
			ColorVect.FromRgba32(0x20406080U),
			TestTolerance
		);
		AssertToleranceEquals(
			new ColorVect(32f / 255f, 64f / 255f, 96f / 255f, 128f / 255f),
			ColorVect.FromRgba32(0x20, 0x40, 0x60, 0x80),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyConvertFromRgb24() {
		// Firstly, we want to specifically ensure that 0f and 1f are representable
		Assert.AreEqual(1f, ColorVect.FromRgb24(0xFFFFFFU).Red);
		Assert.AreEqual(1f, ColorVect.FromRgb24(0xFFFFFFU).Green);
		Assert.AreEqual(1f, ColorVect.FromRgb24(0xFFFFFFU).Blue);
		Assert.AreEqual(1f, ColorVect.FromRgb24(0xFFFFFFU).Alpha);
		Assert.AreEqual(1f, ColorVect.FromRgb24(0xFFFFFF).Red);
		Assert.AreEqual(1f, ColorVect.FromRgb24(0xFFFFFF).Green);
		Assert.AreEqual(1f, ColorVect.FromRgb24(0xFFFFFF).Blue);
		Assert.AreEqual(1f, ColorVect.FromRgb24(0xFFFFFF).Alpha);
		Assert.AreEqual(1f, ColorVect.FromRgb24(0xFF, 0xFF, 0xFF).Red);
		Assert.AreEqual(1f, ColorVect.FromRgb24(0xFF, 0xFF, 0xFF).Green);
		Assert.AreEqual(1f, ColorVect.FromRgb24(0xFF, 0xFF, 0xFF).Blue);
		Assert.AreEqual(1f, ColorVect.FromRgb24(0xFF, 0xFF, 0xFF).Alpha);

		Assert.AreEqual(0f, ColorVect.FromRgb24(0U).Red);
		Assert.AreEqual(0f, ColorVect.FromRgb24(0U).Green);
		Assert.AreEqual(0f, ColorVect.FromRgb24(0U).Blue);
		Assert.AreEqual(1f, ColorVect.FromRgb24(0U).Alpha);
		Assert.AreEqual(0f, ColorVect.FromRgb24(0).Red);
		Assert.AreEqual(0f, ColorVect.FromRgb24(0).Green);
		Assert.AreEqual(0f, ColorVect.FromRgb24(0).Blue);
		Assert.AreEqual(1f, ColorVect.FromRgb24(0).Alpha);
		Assert.AreEqual(0f, ColorVect.FromRgb24(0, 0, 0).Red);
		Assert.AreEqual(0f, ColorVect.FromRgb24(0, 0, 0).Green);
		Assert.AreEqual(0f, ColorVect.FromRgb24(0, 0, 0).Blue);
		Assert.AreEqual(1f, ColorVect.FromRgb24(0, 0, 0).Alpha);

		// Now, just make sure all values are assigned as expected
		AssertToleranceEquals(
			new ColorVect(32f / 255f, 64f / 255f, 96f / 255f),
			ColorVect.FromRgb24(0x204060),
			TestTolerance
		);
		AssertToleranceEquals(
			new ColorVect(32f / 255f, 64f / 255f, 96f / 255f),
			ColorVect.FromRgb24(0x204060U),
			TestTolerance
		);
		AssertToleranceEquals(
			new ColorVect(32f / 255f, 64f / 255f, 96f / 255f),
			ColorVect.FromRgb24(0x20, 0x40, 0x60),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromHsl() {
		// TODO
	}
}