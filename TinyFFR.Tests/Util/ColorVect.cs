// Created on 2024-08-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

[TestFixture]
class ColorVectTest {
	const float TestTolerance = 1E-3f;
	const float TestToleranceHueDegrees = 0.5f;
	static readonly ColorVect ThreeSixNineHalf = new(0.3f, 0.6f, 0.9f, 0.5f);
	static readonly ((float H, float S, float L) Hsl, (float R, float G, float B) Rgb)[] HslToRgbTestValues = {
		// Primary colours with angles wrapped around 360deg
		((H: 000.000f, S: 1.000f, L: 0.500f),		(R: 1.000f,			G: 0.000f,			B: 0.000f)),
		((H: 120.000f, S: 1.000f, L: 0.500f),		(R: 0.000f,			G: 1.000f,			B: 0.000f)),
		((H: 240.000f, S: 1.000f, L: 0.500f),		(R: 0.000f,			G: 0.000f,			B: 1.000f)),
		((H: 360.000f, S: 1.000f, L: 0.500f),		(R: 1.000f,			G: 0.000f,			B: 0.000f)),
		((H: 480.000f, S: 1.000f, L: 0.500f),		(R: 0.000f,			G: 1.000f,			B: 0.000f)),
		((H: 600.000f, S: 1.000f, L: 0.500f),		(R: 0.000f,			G: 0.000f,			B: 1.000f)),
		((H:-360.000f, S: 1.000f, L: 0.500f),		(R: 1.000f,			G: 0.000f,			B: 0.000f)),
		((H:-240.000f, S: 1.000f, L: 0.500f),		(R: 0.000f,			G: 1.000f,			B: 0.000f)),
		((H:-120.000f, S: 1.000f, L: 0.500f),		(R: 0.000f,			G: 0.000f,			B: 1.000f)),

		// Shades of grey (not 50 thankfully)
		((H: 000.000f, S: 0.000f, L: 0.000f),		(R: 0.000f,			G: 0.000f,			B: 0.000f)),
		((H: 000.000f, S: 0.000f, L: 0.001f),		(R: 0.001f,			G: 0.001f,			B: 0.001f)),
		((H: 000.000f, S: 0.000f, L: 0.004f),		(R: 0.004f,			G: 0.004f,			B: 0.004f)),
		((H: 000.000f, S: 0.000f, L: 0.996f),		(R: 0.996f,			G: 0.996f,			B: 0.996f)),
		((H: 000.000f, S: 0.000f, L: 0.999f),		(R: 0.999f,			G: 0.999f,			B: 0.999f)),
		((H: 000.000f, S: 0.000f, L: 1.000f),		(R: 1.000f,			G: 1.000f,			B: 1.000f)),

		// Random RGB colours, in 0..255 range, using https://www.rapidtables.com/convert/color/rgb-to-hsl.html to produce HSL output.
		// Not perfectly accurate as the tool doesn't show decimal places for Hue, so some corrections made below.
		((H: 114.500f, S: 1.000f, L: 0.257f),		(R: 012f / 255f,	G: 131f / 255f,		B: 000f / 255f)),
		((H: 345.760f, S: 1.000f, L: 0.578f),		(R: 255f / 255f,	G: 040f / 255f,		B: 091f / 255f)),
		((H: 088.000f, S: 0.086f, L: 0.433f),		(R: 111f / 255f,	G: 120f / 255f,		B: 101f / 255f)),
		((H: 189.791f, S: 1.000f, L: 0.469f),		(R: 000f / 255f,	G: 200f / 255f,		B: 239f / 255f)),

		// Random HSL colours, using https://www.rapidtables.com/convert/color/hsl-to-rgb.html to produce RGB output.
		// Not perfectly accurate as the tool rounds to nearest byte integer, so some corrections made below.
		((H: 018.000f, S: 0.250f, L: 0.250f),		(R: 0.313f,			G: 0.225f,			B: 0.188f)),
		((H: 054.000f, S: 1.000f, L: 0.970f),		(R: 1.000f,			G: 0.994f,			B: 0.940f)),
		((H: 090.000f, S: 0.660f, L: 0.100f),		(R: 0.100f,			G: 0.166f,			B: 0.034f)),
		((H: 126.000f, S: 0.250f, L: 0.750f),		(R: 0.688f,			G: 0.813f,			B: 0.700f)),
		((H: 162.000f, S: 0.500f, L: 0.500f),		(R: 0.251f,			G: 0.750f,			B: 0.600f)),
		((H: 198.000f, S: 0.900f, L: 0.330f),		(R: 0.033f,			G: 0.449f,			B: 0.627f)),
		((H: 234.000f, S: 0.450f, L: 0.400f),		(R: 0.220f,			G: 0.255f,			B: 0.580f)),
		((H: 270.000f, S: 0.550f, L: 0.600f),		(R: 0.600f,			G: 0.380f,			B: 0.820f)),
		((H: 306.000f, S: 0.500f, L: 0.500f),		(R: 0.750f,			G: 0.250f,			B: 0.700f)),
		((H: 342.000f, S: 0.600f, L: 0.350f),		(R: 0.560f,			G: 0.140f,			B: 0.266f)),
	}; 

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	void AssertAllHslRgbPairs(Action<(Angle Hue, float Sat, float Light, ColorVect Vect), (float R, float G, float B, ColorVect Vect)> assertionAction) {
		for (var i = 0; i < HslToRgbTestValues.Length; ++i) {
			var tuple = HslToRgbTestValues[i];
			var hslVect = ColorVect.FromHueSaturationLightness(tuple.Hsl.H, tuple.Hsl.S, tuple.Hsl.L);
			var rgbVect = new ColorVect(tuple.Rgb.R, tuple.Rgb.G, tuple.Rgb.B);
			try {
				assertionAction((tuple.Hsl.H, tuple.Hsl.S, tuple.Hsl.L, hslVect), (tuple.Rgb.R, tuple.Rgb.G, tuple.Rgb.B, rgbVect));
			}
			catch {
				Console.WriteLine($"Index: {i}");
				Console.WriteLine($"HSL: ({tuple.Hsl.H}, {tuple.Hsl.S}, {tuple.Hsl.L}) => {hslVect}");
				Console.WriteLine($"RGB: ({tuple.Rgb.R}, {tuple.Rgb.G}, {tuple.Rgb.B}) => {rgbVect} (.H = {rgbVect.Hue}) (.S = {rgbVect.Saturation}) (.L = {rgbVect.Lightness})");
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

		AssertAllHslRgbPairs(
			(hsl, rgb) => {
				var v = rgb.Vect;
				Assert.IsTrue(hsl.Hue.EqualsWithinCircle(v.Hue, TestToleranceHueDegrees));
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
		AssertAllHslRgbPairs((hsl, rgb) => {
			AssertToleranceEquals(hsl.Vect, rgb.Vect, TestTolerance);
			rgb.Vect.ToHueSaturationLightness(out var hue, out var sat, out var light);
			Assert.IsTrue(hsl.Hue.EqualsWithinCircle(hue, TestToleranceHueDegrees));
			Assert.AreEqual(hsl.Sat, sat, TestTolerance);
			Assert.AreEqual(hsl.Light, light, TestTolerance);
		});

		var blackVect = new ColorVect(0f, 0f, 0f);
		var greyVect = new ColorVect(0.5f, 0.5f, 0.5f);
		var whiteVect = new ColorVect(1f, 1f, 1f);
		for (var i = -360f; i < 720f; i += 36f) {
			Assert.AreEqual(blackVect, ColorVect.FromHueSaturationLightness(i, 0f, 0f));
			Assert.AreEqual(blackVect, ColorVect.FromHueSaturationLightness(i, 1f, 0f));

			Assert.AreEqual(greyVect, ColorVect.FromHueSaturationLightness(i, 0f, 0.5f));

			Assert.AreEqual(whiteVect, ColorVect.FromHueSaturationLightness(i, 0f, 1f));
			Assert.AreEqual(whiteVect, ColorVect.FromHueSaturationLightness(i, 1f, 1f));
		}
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromRgba32() {
		void AssertPair(ColorVect v, uint rgba) {
			var r = (byte) ((rgba & 0xFF000000) >> 24);
			var g = (byte) ((rgba & 0xFF0000) >> 16);
			var b = (byte) ((rgba & 0xFF00) >> 8);
			var a = (byte) (rgba & 0xFF);

			AssertToleranceEquals(v, (ColorVect) rgba, TestTolerance);
			AssertToleranceEquals(v, ColorVect.FromRgba32(rgba), TestTolerance);
			AssertToleranceEquals(v, ColorVect.FromRgba32((int) rgba), TestTolerance);
			AssertToleranceEquals(v, ColorVect.FromRgba32(r, g, b, a), TestTolerance);

			Assert.AreEqual(rgba, v.ToRgba32());
			v.ToRgba32(out var outR, out var outG, out var outB, out var outA);
			Assert.AreEqual(r, outR);
			Assert.AreEqual(g, outG);
			Assert.AreEqual(b, outB);
			Assert.AreEqual(a, outA);
		}

		AssertPair(new(0f, 0f, 0f, 0f), 0);
		AssertPair(new(0f, 0f, 0f, 1f), 0x000000FF);
		AssertPair(new(1f, 1f, 1f, 0f), 0xFFFFFF00);
		AssertPair(new(1f, 1f, 1f, 1f), 0xFFFFFFFF);
		AssertPair(new(1f, 0f, 0f, 1f), 0xFF0000FF);
		AssertPair(new(0f, 1f, 0f, 1f), 0x00FF00FF);
		AssertPair(new(0f, 0f, 1f, 1f), 0x0000FFFF);
		AssertPair(new(1f, 0f, 0f, 0f), 0xFF000000);
		AssertPair(new(0f, 1f, 0f, 0f), 0x00FF0000);
		AssertPair(new(0f, 0f, 1f, 0f), 0x0000FF00);
		AssertPair(new(0.2f, 0.4f, 0.6f, 0.8f), 0x336699CC);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromRgb24() {
		void AssertPair(ColorVect v, uint rgb) {
			var r = (byte) ((rgb & 0xFF0000) >> 16);
			var g = (byte) ((rgb & 0xFF00) >> 8);
			var b = (byte) (rgb & 0xFF);

			AssertToleranceEquals(v, ColorVect.FromRgb24(rgb), TestTolerance);
			AssertToleranceEquals(v, ColorVect.FromRgb24((int) rgb), TestTolerance);
			AssertToleranceEquals(v, ColorVect.FromRgb24(r, g, b), TestTolerance);

			Assert.AreEqual(rgb, v.ToRgb24());
			v.ToRgb24(out var outR, out var outG, out var outB);
			Assert.AreEqual(r, outR);
			Assert.AreEqual(g, outG);
			Assert.AreEqual(b, outB);
		}

		AssertPair(new(0f, 0f, 0f, 1f), 0x000000);
		AssertPair(new(1f, 1f, 1f, 1f), 0xFFFFFF);
		AssertPair(new(1f, 0f, 0f, 1f), 0xFF0000);
		AssertPair(new(0f, 1f, 0f, 1f), 0x00FF00);
		AssertPair(new(0f, 0f, 1f, 1f), 0x0000FF);
		AssertPair(new(0.2f, 0.4f, 0.6f), 0x336699);
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromVectors() {
		Assert.AreEqual(0.3f, ThreeSixNineHalf.ToVector3().X);
		Assert.AreEqual(0.6f, ThreeSixNineHalf.ToVector3().Y);
		Assert.AreEqual(0.9f, ThreeSixNineHalf.ToVector3().Z);

		Assert.AreEqual(0.3f, ((IVect) ThreeSixNineHalf).AsVect().X);
		Assert.AreEqual(0.6f, ((IVect) ThreeSixNineHalf).AsVect().Y);
		Assert.AreEqual(0.9f, ((IVect) ThreeSixNineHalf).AsVect().Z);

		Assert.AreEqual(0.3f, ThreeSixNineHalf.ToVector4().X);
		Assert.AreEqual(0.6f, ThreeSixNineHalf.ToVector4().Y);
		Assert.AreEqual(0.9f, ThreeSixNineHalf.ToVector4().Z);
		Assert.AreEqual(0.5f, ThreeSixNineHalf.ToVector4().W);

		Assert.AreEqual(ThreeSixNineHalf with { Alpha = 1f }, ColorVect.FromVector3(new(0.3f, 0.6f, 0.9f)));
		Assert.AreEqual(ThreeSixNineHalf, ColorVect.FromVector4(new(0.3f, 0.6f, 0.9f, 0.5f)));
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromTuple() {
		var (r, g, b, a) = ThreeSixNineHalf;

		Assert.AreEqual(0.3f, r);
		Assert.AreEqual(0.6f, g);
		Assert.AreEqual(0.9f, b);
		Assert.AreEqual(0.5f, a);
		Assert.AreEqual(ThreeSixNineHalf, (ColorVect) (0.3f, 0.6f, 0.9f, 0.5f));
		Assert.AreEqual(ThreeSixNineHalf with { Alpha = 1f }, (ColorVect) (0.3f, 0.6f, 0.9f));
	}

	[Test]
	public void ShouldCorrectlyGenerateRandomColors() {
		const int NumIterations = 30_000;

		var min = new ColorVect(0.15f, 0.25f, 0.35f, 0.45f);
		var max = new ColorVect(0.35f, 0.65f, 0.75f, 0.50f);

		for (var i = 0; i < NumIterations; ++i) {
			var random = ColorVect.Random();
			var randomOpaque = ColorVect.RandomOpaque();
			var randomClamped = ColorVect.Random(min, max);

			Assert.GreaterOrEqual(random.Red, 0f);
			Assert.LessOrEqual(random.Red, 1f);
			Assert.GreaterOrEqual(random.Green, 0f);
			Assert.LessOrEqual(random.Green, 1f);
			Assert.GreaterOrEqual(random.Blue, 0f);
			Assert.LessOrEqual(random.Blue, 1f);
			Assert.GreaterOrEqual(random.Alpha, 0f);
			Assert.LessOrEqual(random.Alpha, 1f);

			Assert.GreaterOrEqual(randomOpaque.Red, 0f);
			Assert.LessOrEqual(randomOpaque.Red, 1f);
			Assert.GreaterOrEqual(randomOpaque.Green, 0f);
			Assert.LessOrEqual(randomOpaque.Green, 1f);
			Assert.GreaterOrEqual(randomOpaque.Blue, 0f);
			Assert.LessOrEqual(randomOpaque.Blue, 1f);
			Assert.AreEqual(1f, randomOpaque.Alpha);

			Assert.GreaterOrEqual(randomClamped.Red, min.Red);
			Assert.LessOrEqual(randomClamped.Red, max.Red);
			Assert.GreaterOrEqual(randomClamped.Green, min.Green);
			Assert.LessOrEqual(randomClamped.Green, max.Green);
			Assert.GreaterOrEqual(randomClamped.Blue, min.Blue);
			Assert.LessOrEqual(randomClamped.Blue, max.Blue);
			Assert.GreaterOrEqual(randomClamped.Alpha, min.Alpha);
			Assert.LessOrEqual(randomClamped.Alpha, max.Alpha);
		}
	}

	[Test]
	public void ShouldCorrectlyConvertToAndFromSpan() {
		ByteSpanSerializationTestUtils.AssertDeclaredSpanLength<ColorVect>();

		AssertAllHslRgbPairs((_, rgb) => {
			var a = RandomUtils.NextSingleZeroToOneInclusive();
			var expected = rgb.Vect with { Alpha = a };
			ByteSpanSerializationTestUtils.AssertSpanRoundTripConversion(expected);
			ByteSpanSerializationTestUtils.AssertLittleEndianSingles(expected, rgb.R, rgb.G, rgb.B, a);
		});
	}

	[Test]
	public void ShouldCorrectlyConvertToString() {
		void AssertIteration(ColorVect input, string expectedValue) {
			var testCulture = CultureInfo.InvariantCulture;
			var testFormat = "N1";

			Span<char> formatSpan = stackalloc char[expectedValue.Length];
			Assert.IsTrue(input.TryFormat(formatSpan, out var charsWritten, testFormat, testCulture));
			Assert.AreEqual(formatSpan.Length, charsWritten);

			Assert.AreEqual(expectedValue, input.ToString(testFormat, testCulture));
			Assert.AreEqual(expectedValue, new String(formatSpan));
		}

		AssertIteration(new(0f, 0f, 0f, 0f), "<R 0.0%, G 0.0%, B 0.0%, A 0.0%>");
		AssertIteration(ThreeSixNineHalf, "<R 30.0%, G 60.0%, B 90.0%, A 50.0%>");
		AssertIteration(new(-3f, -6f, -9f, -5f), "<R -300.0%, G -600.0%, B -900.0%, A -500.0%>");
	}

	[Test]
	public void ShouldCorrectlyFormatToString() {
		void AssertFail(ColorVect input, Span<char> destination, ReadOnlySpan<char> format, IFormatProvider? provider) {
			Assert.AreEqual(false, input.TryFormat(destination, out _, format, provider));
		}

		void AssertSuccess(
			ColorVect input,
			Span<char> destination,
			ReadOnlySpan<char> format,
			IFormatProvider? provider,
			ReadOnlySpan<char> expectedDestSpanValue
		) {
			var actualReturnValue = input.TryFormat(destination, out var numCharsWritten, format, provider);
			Assert.AreEqual(true, actualReturnValue);
			Assert.AreEqual(expectedDestSpanValue.Length, numCharsWritten);
			Assert.IsTrue(
				expectedDestSpanValue.SequenceEqual(destination[..expectedDestSpanValue.Length]),
				$"Destination as string was {new String(destination)}"
			);
		}

		var fractionalVect = new ColorVect(1.211f, 3.422f, -5.633f, 7.811f);

		AssertFail(new(0f, 0f, 0f, 0f), Array.Empty<char>(), "N0", null);
		AssertFail(new(0f, 0f, 0f, 0f), new char[23], "N0", null);
		AssertSuccess(new(0f, 0f, 0f, 0f), new char[24], "N0", null, "<R 0%, G 0%, B 0%, A 0%>");
		AssertFail(fractionalVect, new char[32], "N0", null);
		AssertSuccess(fractionalVect, new char[33], "N0", null, "<R 121%, G 342%, B -563%, A 781%>");
		AssertFail(fractionalVect, new char[40], "N1", null);
		AssertSuccess(fractionalVect, new char[41], "N1", null, "<R 121.1%, G 342.2%, B -563.3%, A 781.1%>");
		AssertSuccess(fractionalVect, new char[41], "N1", CultureInfo.CreateSpecificCulture("de-DE"), "<R 121,1%. G 342,2%. B -563,3%. A 781,1%>");
		AssertSuccess(fractionalVect, new char[49], "N3", null, "<R 121.100%, G 342.200%, B -563.300%, A 781.100%>");
	}

	[Test]
	public void ShouldCorrectlyParseFromString() {
		var testCulture = CultureInfo.InvariantCulture;

		void AssertSuccess(string input, ColorVect expectedResult) {
			AssertToleranceEquals(expectedResult, ColorVect.Parse(input, testCulture), TestTolerance);
			AssertToleranceEquals(expectedResult, ColorVect.Parse(input.AsSpan(), testCulture), TestTolerance);
			Assert.IsTrue(ColorVect.TryParse(input, testCulture, out var parseResult));
			AssertToleranceEquals(expectedResult, parseResult, TestTolerance);
			Assert.IsTrue(ColorVect.TryParse(input.AsSpan(), testCulture, out parseResult));
			AssertToleranceEquals(expectedResult, parseResult, TestTolerance);
		}

		void AssertFail(string input) {
			Assert.Catch(() => ColorVect.Parse(input, testCulture));
			Assert.Catch(() => ColorVect.Parse(input.AsSpan(), testCulture));
			Assert.False(ColorVect.TryParse(input, testCulture, out _));
			Assert.False(ColorVect.TryParse(input.AsSpan(), testCulture, out _));
		}

		AssertFail("");
		AssertFail("<>");
		AssertFail("<R %, G %, B %, A %>");
		AssertFail("R 1%, G 2%, B 3%, A 4%");
		AssertFail("<R 1%, G 2%, B 3%, A 4");
		AssertFail("R 1%, G 2%, B 3%, A 4%>");
		AssertFail("<R 1%, G 2%, B 3%>");
		AssertFail("<R 1%, G 2%, B 3%, A>");
		AssertFail("<R 1%, G 2%, B 3%, A 4");
		AssertSuccess("<R 1%, G 2%, B 3%, A 4%>", new(0.01f, 0.02f, 0.03f, 0.04f));
		AssertSuccess("<R 10.5%, G 20.3%, B 30.1%, A 40.9%>", new(0.105f, 0.203f, 0.301f, 0.409f));
	}

	[Test]
	public void ShouldCorrectlyImplementEqualityMembers() {
		Assert.AreEqual(ThreeSixNineHalf, ThreeSixNineHalf);
		Assert.AreEqual(new ColorVect(0f, 0f, 0f, 0f), new ColorVect(-0f, -0f, -0f, -0f));
		Assert.IsTrue(new ColorVect(0.309f, 0.609f, 0.909f, 0.509f).Equals(ThreeSixNineHalf, 0.01f));
		Assert.IsFalse(new ColorVect(0.309f, 0.609f, 0.909f, 0.509f).Equals(ThreeSixNineHalf, 0.008f));
		
		Assert.IsTrue(ThreeSixNineHalf == new ColorVect(0.3f, 0.6f, 0.9f, 0.5f));
		Assert.IsFalse(ThreeSixNineHalf != new ColorVect(0.3f, 0.6f, 0.9f, 0.5f));
		Assert.IsFalse(ThreeSixNineHalf == new ColorVect(0.3f, 0.6f, 0.9f, 0.51f));
		Assert.IsFalse(ThreeSixNineHalf == new ColorVect(0.3f, 0.6f, 0.91f, 0.5f));
		Assert.IsFalse(ThreeSixNineHalf == new ColorVect(0.3f, 0.61f, 0.9f, 0.5f));
		Assert.IsFalse(ThreeSixNineHalf == new ColorVect(0.31f, 0.6f, 0.9f, 0.5f));
		Assert.IsTrue(ThreeSixNineHalf != new ColorVect(0.3f, 0.6f, 0.9f, 0.51f));
		Assert.IsTrue(ThreeSixNineHalf != new ColorVect(0.3f, 0.6f, 0.91f, 0.5f));
		Assert.IsTrue(ThreeSixNineHalf != new ColorVect(0.3f, 0.61f, 0.9f, 0.5f));
		Assert.IsTrue(ThreeSixNineHalf != new ColorVect(0.31f, 0.6f, 0.9f, 0.5f));
	}

	[Test]
	public void ShouldCorrectlyImplementWithMethods() {
		Assert.AreEqual(Angle.QuarterCircle.Radians, ThreeSixNineHalf.WithHue(Angle.QuarterCircle).Hue.Radians, TestTolerance);
		Assert.AreEqual(Angle.ThreeQuarterCircle.Radians, ThreeSixNineHalf.WithHue(Angle.ThreeQuarterCircle).Hue.Radians, TestTolerance);
		
		Assert.AreEqual(0.2f, ThreeSixNineHalf.WithSaturation(0.2f).Saturation, TestTolerance);
		Assert.AreEqual(0.8f, ThreeSixNineHalf.WithSaturation(0.8f).Saturation, TestTolerance);
		
		Assert.AreEqual(0.2f, ThreeSixNineHalf.WithLightness(0.2f).Lightness, TestTolerance);
		Assert.AreEqual(0.8f, ThreeSixNineHalf.WithLightness(0.8f).Lightness, TestTolerance);

		var testVect = ColorVect.FromHueSaturationLightness(Angle.QuarterCircle, 0.3f, 0.7f, 0.5f);
		Assert.IsTrue(Angle.HalfCircle.EqualsWithinCircle(testVect.WithHueAdjustedBy(Angle.QuarterCircle).Hue, TestTolerance));
		Assert.IsTrue(Angle.Zero.EqualsWithinCircle(testVect.WithHueAdjustedBy(-Angle.QuarterCircle).Hue, TestTolerance));
		Assert.IsTrue((-Angle.QuarterCircle).EqualsWithinCircle(testVect.WithHueAdjustedBy(-Angle.HalfCircle).Hue, TestTolerance));

		Assert.AreEqual(0.5f, testVect.WithSaturationAdjustedBy(0.2f).Saturation, TestTolerance);
		Assert.AreEqual(0.1f, testVect.WithSaturationAdjustedBy(-0.2f).Saturation, TestTolerance);
		Assert.AreEqual(1.0f, testVect.WithSaturationAdjustedBy(0.8f).Saturation, TestTolerance);
		Assert.AreEqual(0.0f, testVect.WithSaturationAdjustedBy(-0.8f).Saturation, TestTolerance);

		Assert.AreEqual(0.9f, testVect.WithLightnessAdjustedBy(0.2f).Lightness, TestTolerance);
		Assert.AreEqual(0.5f, testVect.WithLightnessAdjustedBy(-0.2f).Lightness, TestTolerance);
		Assert.AreEqual(1.0f, testVect.WithLightnessAdjustedBy(0.8f).Lightness, TestTolerance);
		Assert.AreEqual(0.0f, testVect.WithLightnessAdjustedBy(-0.8f).Lightness, TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyAddAndSubtract() {
		AssertToleranceEquals(ThreeSixNineHalf, ThreeSixNineHalf + new ColorVect(0f, 0f, 0f, 0f), TestTolerance);
		AssertToleranceEquals(ThreeSixNineHalf, ThreeSixNineHalf - new ColorVect(0f, 0f, 0f, 0f), TestTolerance);
		
		AssertToleranceEquals(new(1f, 1f, 1f, 1f), ThreeSixNineHalf + new ColorVect(1f, 1f, 1f, 1f), TestTolerance);
		AssertToleranceEquals(new(0f, 0f, 0f, 0f), ThreeSixNineHalf - new ColorVect(1f, 1f, 1f, 1f), TestTolerance);

		AssertToleranceEquals(new(0.5f, 1f, 1f, 1f), ThreeSixNineHalf + new ColorVect(0.2f, 0.4f, 0.6f, 0.5f), TestTolerance);
		AssertToleranceEquals(new(0.35f, 0.65f, 0.95f, 0.55f), ThreeSixNineHalf + new ColorVect(0.05f, 0.05f, 0.05f, 0.05f), TestTolerance);
		AssertToleranceEquals(new(0.1f, 0.2f, 0.3f, 0f), ThreeSixNineHalf - new ColorVect(0.2f, 0.4f, 0.6f, 0.5f), TestTolerance);
		AssertToleranceEquals(new(0.25f, 0.55f, 0.85f, 0.45f), ThreeSixNineHalf - new ColorVect(0.05f, 0.05f, 0.05f, 0.05f), TestTolerance);

		AssertToleranceEquals(ThreeSixNineHalf, ThreeSixNineHalf.Plus(new ColorVect(0f, 0f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals(ThreeSixNineHalf, ThreeSixNineHalf.Minus(new ColorVect(0f, 0f, 0f, 0f)), TestTolerance);

		AssertToleranceEquals(new(1f, 1f, 1f, 1f), ThreeSixNineHalf.Plus(new ColorVect(1f, 1f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(new(0f, 0f, 0f, 0f), ThreeSixNineHalf.Minus(new ColorVect(1f, 1f, 1f, 1f)), TestTolerance);

		AssertToleranceEquals(new(0.5f, 1f, 1f, 1f), ThreeSixNineHalf.Plus(new ColorVect(0.2f, 0.4f, 0.6f, 0.5f)), TestTolerance);
		AssertToleranceEquals(new(0.35f, 0.65f, 0.95f, 0.55f), ThreeSixNineHalf.Plus(new ColorVect(0.05f, 0.05f, 0.05f, 0.05f)), TestTolerance);
		AssertToleranceEquals(new(0.1f, 0.2f, 0.3f, 0f), ThreeSixNineHalf.Minus(new ColorVect(0.2f, 0.4f, 0.6f, 0.5f)), TestTolerance);
		AssertToleranceEquals(new(0.25f, 0.55f, 0.85f, 0.45f), ThreeSixNineHalf.Minus(new ColorVect(0.05f, 0.05f, 0.05f, 0.05f)), TestTolerance);

		AssertToleranceEquals(ThreeSixNineHalf, ThreeSixNineHalf.PlusWithoutNormalization(new ColorVect(0f, 0f, 0f, 0f)), TestTolerance);
		AssertToleranceEquals(ThreeSixNineHalf, ThreeSixNineHalf.MinusWithoutNormalization(new ColorVect(0f, 0f, 0f, 0f)), TestTolerance);

		AssertToleranceEquals(new(1.3f, 1.6f, 1.9f, 1.5f), ThreeSixNineHalf.PlusWithoutNormalization(new ColorVect(1f, 1f, 1f, 1f)), TestTolerance);
		AssertToleranceEquals(new(-0.7f, -0.4f, -0.1f, -0.5f), ThreeSixNineHalf.MinusWithoutNormalization(new ColorVect(1f, 1f, 1f, 1f)), TestTolerance);

		AssertToleranceEquals(new(0.5f, 1f, 1.5f, 1f), ThreeSixNineHalf.PlusWithoutNormalization(new ColorVect(0.2f, 0.4f, 0.6f, 0.5f)), TestTolerance);
		AssertToleranceEquals(new(0.35f, 0.65f, 0.95f, 0.55f), ThreeSixNineHalf.PlusWithoutNormalization(new ColorVect(0.05f, 0.05f, 0.05f, 0.05f)), TestTolerance);
		AssertToleranceEquals(new(0.1f, 0.2f, 0.3f, 0f), ThreeSixNineHalf.MinusWithoutNormalization(new ColorVect(0.2f, 0.4f, 0.6f, 0.5f)), TestTolerance);
		AssertToleranceEquals(new(0.25f, 0.55f, 0.85f, 0.45f), ThreeSixNineHalf.MinusWithoutNormalization(new ColorVect(0.05f, 0.05f, 0.05f, 0.05f)), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyScale() {
		AssertToleranceEquals(new ColorVect(0.15f, 0.3f, 0.45f, 0.5f), ThreeSixNineHalf * 0.5f, TestTolerance);
		AssertToleranceEquals(new ColorVect(0f, 0f, 0f, 0.5f), ThreeSixNineHalf * -1f, TestTolerance);
		AssertToleranceEquals(new ColorVect(0.6f, 1f, 1f, 0.5f), ThreeSixNineHalf * 2f, TestTolerance);
		AssertToleranceEquals(new ColorVect(0.15f, 0.3f, 0.45f, 0.5f), ThreeSixNineHalf.ScaledBy(0.5f), TestTolerance);
		AssertToleranceEquals(new ColorVect(0f, 0f, 0f, 0.5f), ThreeSixNineHalf.ScaledBy(-1f), TestTolerance);
		AssertToleranceEquals(new ColorVect(0.6f, 1f, 1f, 0.5f), ThreeSixNineHalf.ScaledBy(2f), TestTolerance);
		AssertToleranceEquals(new ColorVect(0.15f, 0.3f, 0.45f, 0.5f), ThreeSixNineHalf.ScaledWithoutNormalizationBy(0.5f), TestTolerance);
		AssertToleranceEquals(new ColorVect(-0.3f, -0.6f, -0.9f, 0.5f), ThreeSixNineHalf.ScaledWithoutNormalizationBy(-1f), TestTolerance);
		AssertToleranceEquals(new ColorVect(0.6f, 1.2f, 1.8f, 0.5f), ThreeSixNineHalf.ScaledWithoutNormalizationBy(2f), TestTolerance);
		AssertToleranceEquals(new ColorVect(0.15f, 0.3f, 0.45f, 0.5f), ThreeSixNineHalf.ScaledBy(0.5f, includeAlpha: false), TestTolerance);
		AssertToleranceEquals(new ColorVect(0f, 0f, 0f, 0.5f), ThreeSixNineHalf.ScaledBy(-1f, includeAlpha: false), TestTolerance);
		AssertToleranceEquals(new ColorVect(0.6f, 1f, 1f, 0.5f), ThreeSixNineHalf.ScaledBy(2f, includeAlpha: false), TestTolerance);
		AssertToleranceEquals(new ColorVect(0.15f, 0.3f, 0.45f, 0.5f), ThreeSixNineHalf.ScaledWithoutNormalizationBy(0.5f, includeAlpha: false), TestTolerance);
		AssertToleranceEquals(new ColorVect(-0.3f, -0.6f, -0.9f, 0.5f), ThreeSixNineHalf.ScaledWithoutNormalizationBy(-1f, includeAlpha: false), TestTolerance);
		AssertToleranceEquals(new ColorVect(0.6f, 1.2f, 1.8f, 0.5f), ThreeSixNineHalf.ScaledWithoutNormalizationBy(2f, includeAlpha: false), TestTolerance);
		AssertToleranceEquals(new ColorVect(0.15f, 0.3f, 0.45f, 0.25f), ThreeSixNineHalf.ScaledBy(0.5f, includeAlpha: true), TestTolerance);
		AssertToleranceEquals(new ColorVect(0f, 0f, 0f, 0f), ThreeSixNineHalf.ScaledBy(-1f, includeAlpha: true), TestTolerance);
		AssertToleranceEquals(new ColorVect(0.6f, 1f, 1f, 1f), ThreeSixNineHalf.ScaledBy(2f, includeAlpha: true), TestTolerance);
		AssertToleranceEquals(new ColorVect(0.15f, 0.3f, 0.45f, 0.25f), ThreeSixNineHalf.ScaledWithoutNormalizationBy(0.5f, includeAlpha: true), TestTolerance);
		AssertToleranceEquals(new ColorVect(-0.3f, -0.6f, -0.9f, -0.5f), ThreeSixNineHalf.ScaledWithoutNormalizationBy(-1f, includeAlpha: true), TestTolerance);
		AssertToleranceEquals(new ColorVect(0.6f, 1.2f, 1.8f, 1f), ThreeSixNineHalf.ScaledWithoutNormalizationBy(2f, includeAlpha: true), TestTolerance);
		AssertToleranceEquals(new ColorVect(0.9f, 1.8f, 2.7f, 1.5f), ThreeSixNineHalf.ScaledWithoutNormalizationBy(3f, includeAlpha: true), TestTolerance);
	}

	[Test]
	public void ShouldCorrectlyClamp() {
		var oneVect = new ColorVect(0.1f, 0.1f, 0.1f, 0.1f);
		var threeVect = new ColorVect(0.3f, 0.3f, 0.3f, 0.3f);
		var fiveVect = new ColorVect(0.5f, 0.5f, 0.5f, 0.5f);
		var sevenVect = new ColorVect(0.7f, 0.7f, 0.7f, 0.7f);
		var nineVect = new ColorVect(0.9f, 0.9f, 0.9f, 0.9f);

		Assert.AreEqual(
			threeVect,
			oneVect.Clamp(threeVect, sevenVect)
		);
		Assert.AreEqual(
			sevenVect,
			nineVect.Clamp(threeVect, sevenVect)
		);
		Assert.AreEqual(
			fiveVect,
			fiveVect.Clamp(threeVect, sevenVect)
		);
		
		Assert.AreEqual(
			new ColorVect(1f, 1f, 1f, 1f),
			new ColorVect(2f, 2f, 2f, 2f).ClampToNormalizedRange()
		);
		Assert.AreEqual(
			new ColorVect(0f, 0f, 0f, 0f),
			new ColorVect(-1f, -1f, -1f, -1f).ClampToNormalizedRange()
		);
		Assert.AreEqual(
			fiveVect,
			fiveVect.ClampToNormalizedRange()
		);
		Assert.AreEqual(
			new ColorVect(1f, 1f, 1f, 1f),
			new ColorVect(2f, 2f, 2f, 2f).ClampToNormalizedRange(includeAlpha: true)
		);
		Assert.AreEqual(
			new ColorVect(0f, 0f, 0f, 0f),
			new ColorVect(-1f, -1f, -1f, -1f).ClampToNormalizedRange(includeAlpha: true)
		);
		Assert.AreEqual(
			fiveVect,
			fiveVect.ClampToNormalizedRange(includeAlpha: true)
		);
		Assert.AreEqual(
			new ColorVect(1f, 1f, 1f, 2f),
			new ColorVect(2f, 2f, 2f, 2f).ClampToNormalizedRange(includeAlpha: false)
		);
		Assert.AreEqual(
			new ColorVect(0f, 0f, 0f, -1f),
			new ColorVect(-1f, -1f, -1f, -1f).ClampToNormalizedRange(includeAlpha: false)
		);
		Assert.AreEqual(
			fiveVect,
			fiveVect.ClampToNormalizedRange(includeAlpha: false)
		);

		Assert.AreEqual(
			new ColorVect(0.5f, 0.6f, 0.7f, 0.5f),
			ThreeSixNineHalf.Clamp(fiveVect, sevenVect)
		);
		Assert.AreEqual(
			new ColorVect(0.3f, 0.5f, 0.5f, 0.5f),
			ThreeSixNineHalf.Clamp(threeVect, fiveVect)
		);
		Assert.AreEqual(
			new ColorVect(0.3f, 0.3f, 0.3f, 0.3f),
			ThreeSixNineHalf.Clamp(oneVect, threeVect)
		);
		Assert.AreEqual(
			fiveVect,
			ThreeSixNineHalf.Clamp(fiveVect, fiveVect)
		);
		Assert.AreEqual(
			ThreeSixNineHalf.Clamp(fiveVect, sevenVect),
			ThreeSixNineHalf.Clamp(sevenVect, fiveVect)
		);
		Assert.AreEqual(
			ThreeSixNineHalf.Clamp(threeVect, fiveVect),
			ThreeSixNineHalf.Clamp(fiveVect, threeVect)
		);
		Assert.AreEqual(
			ThreeSixNineHalf.Clamp(oneVect, threeVect),
			ThreeSixNineHalf.Clamp(threeVect, oneVect)
		);
	}

	[Test]
	public void ShouldCorrectlyInterpolate() {
		var oneVect = new ColorVect(0.1f, 0.1f, 0.1f, 0.1f);
		var threeVect = new ColorVect(0.3f, 0.3f, 0.3f, 0.3f);
		var fiveVect = new ColorVect(0.5f, 0.5f, 0.5f, 0.5f);
		var sevenVect = new ColorVect(0.7f, 0.7f, 0.7f, 0.7f);
		var nineVect = new ColorVect(0.9f, 0.9f, 0.9f, 0.9f);

		AssertToleranceEquals(
			fiveVect,
			ColorVect.Interpolate(threeVect, sevenVect, 0.5f),
			TestTolerance
		);
		AssertToleranceEquals(
			threeVect,
			ColorVect.Interpolate(threeVect, sevenVect, 0f),
			TestTolerance
		);
		AssertToleranceEquals(
			sevenVect,
			ColorVect.Interpolate(threeVect, sevenVect, 1f),
			TestTolerance
		);
		AssertToleranceEquals(
			oneVect,
			ColorVect.Interpolate(threeVect, sevenVect, -0.5f),
			TestTolerance
		);
		AssertToleranceEquals(
			nineVect,
			ColorVect.Interpolate(threeVect, sevenVect, 1.5f),
			TestTolerance
		);
		AssertToleranceEquals(
			new(0.2f, 0.3f, 0.4f, 0.5f),
			ColorVect.Interpolate(new(0.1f, 0.2f, 0.3f, 0.4f), new(0.3f, 0.4f, 0.5f, 0.6f), 0.5f),
			TestTolerance
		);
	}

	[Test]
	public void ShouldCorrectlyConvertFromStandardColor() {
		void AssertColor(StandardColor c, ColorVect expectation) {
			AssertToleranceEquals(expectation, ColorVect.FromStandardColor(c), 0.01f);

			Assert.AreEqual(ColorVect.FromStandardColor(c), (ColorVect) c);
			Assert.AreEqual(ColorVect.FromStandardColor(c), new ColorVect(c));
			Assert.AreEqual(ColorVect.FromStandardColor(c), c.ToColorVect());
		}

		AssertColor(StandardColor.Black, new(0f, 0f, 0f, 1f));
		AssertColor(StandardColor.White, new(1f, 1f, 1f, 1f));
		AssertColor(StandardColor.Red, new(1f, 0f, 0f, 1f));
		AssertColor(StandardColor.Lime, new(0f, 1f, 0f, 1f));
		AssertColor(StandardColor.Blue, new(0f, 0f, 1f, 1f));
		AssertColor(StandardColor.Olive, new(0.5f, 0.5f, 0f, 1f));
		AssertColor(StandardColor.Teal, new(0f, 0.5f, 0.5f, 1f));
		AssertColor(StandardColor.Purple, new(0.5f, 0f, 0.5f, 1f));
	}
}