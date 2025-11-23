// Created on 2025-11-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Factory.Local;
using static Egodystonic.TinyFFR.Assets.Materials.TexturePatternPrinter;

namespace Egodystonic.TinyFFR.Assets.Materials;

[TestFixture]
unsafe class TexturePatternPrinterTest {
	static readonly TexelRgb24 Red = new(255, 0, 0);
	static readonly TexelRgb24 Blue = new(0, 0, 255);
	static readonly byte Min = 0;
	static readonly byte Max = 255;

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	TexturePattern<TexelRgb24> Pattern(int xSize, int ySize) => AlternatingPattern<TexelRgb24>(xSize, ySize, default, default);

	TexturePattern<T> AlternatingPattern<T>(int xSize, int ySize, T a, T b) where T : unmanaged {
		return TexturePattern.Chequerboard(
			a, b, (xSize, ySize), cellResolution: 1
		);
	}

	TexturePattern<T> HalfWidthPattern<T>(int xSize, int ySize, T a, T b) where T : unmanaged {
		return TexturePattern.Lines(
			a, b, horizontal: false, numRepeats: 1, lineThickness: xSize / 2, colinearSize: ySize
		);
	}

	void AssertRedBlueChequerboard(Span<TexelRgb24> span, XYPair<int> dimensions) {
		for (var y = 0; y < dimensions.Y; ++y) {
			for (var x = 0; x < dimensions.X; ++x) {
				var i = (dimensions.X * y) + x;
				if (((x + y) & 1) == 1) {
					Assert.AreEqual(Blue, span[i], $"At index {i} ({x} x {y})");
				}
				else Assert.AreEqual(Red, span[i], $"At index {i} ({x} x {y})");
			}
		}
	}

	void AssertRedBlueChequerboardWithMinMax(Span<TexelRgb24> span, XYPair<int> rbDimensions, XYPair<int> gDimensions) {
		for (var y = 0; y < rbDimensions.Y; ++y) {
			for (var x = 0; x < rbDimensions.X; ++x) {
				var i = (rbDimensions.X * y) + x;
				if (((x + y) & 1) == 1) {
					Assert.AreEqual(Blue with { G = (x % gDimensions.X) >= gDimensions.X / 2 ? Max : Min }, span[i], $"At index {i} ({x} x {y})");
				}
				else Assert.AreEqual(Red with { G = (x % gDimensions.X) >= gDimensions.X / 2 ? Max : Min }, span[i], $"At index {i} ({x} x {y})");
			}
		}
	}

	void AssertAlternatingAllOver(Span<TexelRgb24> span, XYPair<int> dimensions) {
		for (var y = 0; y < dimensions.Y; ++y) {
			for (var x = 0; x < dimensions.X; ++x) {
				var i = (dimensions.X * y) + x;
				if (((x + y) & 1) == 1) {
					Assert.AreEqual(new TexelRgb24(Max, Max, Max), span[i], $"At index {i} ({x} x {y})");
				}
				else Assert.AreEqual(new TexelRgb24(Min, Min, Min), span[i], $"At index {i} ({x} x {y})");
			}
		}
	}

	void AssertAlternatingAllOver(Span<TexelRgba32> span, XYPair<int> dimensions) {
		for (var y = 0; y < dimensions.Y; ++y) {
			for (var x = 0; x < dimensions.X; ++x) {
				var i = (dimensions.X * y) + x;
				if (((x + y) & 1) == 1) {
					Assert.AreEqual(new TexelRgba32(Max, Max, Max, Max), span[i], $"At index {i} ({x} x {y})");
				}
				else Assert.AreEqual(new TexelRgba32(Min, Min, Min, Min), span[i], $"At index {i} ({x} x {y})");
			}
		}
	}

	[Test]
	public void ShouldCorrectlyDetermineCompositePatternDimensions() {
		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(Pattern(10, 5), Pattern(5, 10)));
		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(Pattern(5, 10), Pattern(10, 5)));

		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(Pattern(10, 5), Pattern(5, 10), Pattern(7, 7)));
		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(Pattern(7, 7), Pattern(10, 5), Pattern(5, 10)));
		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(Pattern(5, 10), Pattern(7, 7), Pattern(10, 5)));

		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(Pattern(10, 5), Pattern(5, 10), Pattern(7, 7), Pattern(7, 7)));
		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(Pattern(7, 7), Pattern(10, 5), Pattern(5, 10), Pattern(7, 7)));
		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(Pattern(5, 10), Pattern(7, 7), Pattern(10, 5), Pattern(7, 7)));
		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(Pattern(7, 7), Pattern(7, 7), Pattern(5, 10), Pattern(10, 5)));
		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(Pattern(10, 5), Pattern(7, 7), Pattern(7, 7), Pattern(5, 10)));
	}

	[Test]
	public void ShouldCorrectlyPrintPatternsWithDelegatePointerOverloads() {
		static TexelRgb24 T(TexelRgb24 t) => new(t.G, t.B, t.R);
		static TexelRgb24 TB(TexelRgb24 t, byte b) => new(t.R, b, t.B);
		static TexelRgb24 BBB(byte r, byte g, byte b) => new(r, g, b);
		static TexelRgba32 BBBB(byte r, byte g, byte b, byte a) => new(r, g, b, a);

		var buffer = new TexelRgb24[60];

		Assert.Throws<ArgumentException>(() => PrintPattern(AlternatingPattern(20, 3, Red, Blue), buffer[..^1]));
		Assert.AreEqual(60, PrintPattern(AlternatingPattern(20, 3, Red, Blue), buffer));
		AssertRedBlueChequerboard(buffer, (20, 3));

		Assert.Throws<ArgumentException>(() => PrintPattern(AlternatingPattern(20, 3, Red, Blue), &T, buffer[..^1]));
		Assert.AreEqual(60, PrintPattern(AlternatingPattern(20, 3, new TexelRgb24(0, 255, 0), new TexelRgb24(255, 0, 0)), &T, buffer));
		AssertRedBlueChequerboard(buffer, (20, 3));

		Assert.Throws<ArgumentException>(() => PrintPattern(AlternatingPattern(20, 3, Red, Blue), HalfWidthPattern(10, 1, Min, Max), &TB, buffer[..^1]));
		Assert.AreEqual(60, PrintPattern(AlternatingPattern(20, 3, Red, Blue), HalfWidthPattern(10, 1, Min, Max), &TB, buffer));
		AssertRedBlueChequerboardWithMinMax(buffer, (20, 3), (10, 1));

		Assert.Throws<ArgumentException>(() => PrintPattern(AlternatingPattern(20, 3, Min, Max), AlternatingPattern(10, 2, Min, Max), AlternatingPattern(10, 3, Min, Max), &BBB, buffer[..^1]));
		Assert.AreEqual(60, PrintPattern(AlternatingPattern(20, 3, Min, Max), AlternatingPattern(10, 3, Min, Max), AlternatingPattern(10, 3, Min, Max), &BBB, buffer));
		AssertAlternatingAllOver(buffer, (20, 3));

		var rgbaBuffer = new TexelRgba32[60];
		Assert.Throws<ArgumentException>(() => PrintPattern(AlternatingPattern(20, 3, Min, Max), AlternatingPattern(10, 2, Min, Max), AlternatingPattern(10, 3, Min, Max), AlternatingPattern(20, 3, Min, Max), &BBBB, rgbaBuffer[..^1]));
		Assert.AreEqual(60, PrintPattern(AlternatingPattern(20, 3, Min, Max), AlternatingPattern(10, 3, Min, Max), AlternatingPattern(10, 3, Min, Max), AlternatingPattern(20, 3, Min, Max), &BBBB, rgbaBuffer));
		AssertAlternatingAllOver(rgbaBuffer, (20, 3));
	}

	[Test]
	public void ShouldCorrectlyPrintPatternsWithFuncOverloads() {
		static TexelRgb24 T(TexelRgb24 t) => new(t.G, t.B, t.R);
		static TexelRgb24 TB(TexelRgb24 t, byte b) => new(t.R, b, t.B);
		static TexelRgb24 BBB(byte r, byte g, byte b) => new(r, g, b);
		static TexelRgba32 BBBB(byte r, byte g, byte b, byte a) => new(r, g, b, a);

		var buffer = new TexelRgb24[60];

		Assert.Throws<ArgumentException>(() => PrintPattern(AlternatingPattern(20, 3, Red, Blue), T, buffer[..^1]));
		Assert.AreEqual(60, PrintPattern(AlternatingPattern(20, 3, new TexelRgb24(0, 255, 0), new TexelRgb24(255, 0, 0)), T, buffer));
		AssertRedBlueChequerboard(buffer, (20, 3));

		Assert.Throws<ArgumentException>(() => PrintPattern(AlternatingPattern(20, 3, Red, Blue), HalfWidthPattern(10, 1, Min, Max), TB, buffer[..^1]));
		Assert.AreEqual(60, PrintPattern(AlternatingPattern(20, 3, Red, Blue), HalfWidthPattern(10, 1, Min, Max), TB, buffer));
		AssertRedBlueChequerboardWithMinMax(buffer, (20, 3), (10, 1));

		Assert.Throws<ArgumentException>(() => PrintPattern(AlternatingPattern(20, 3, Min, Max), AlternatingPattern(10, 2, Min, Max), AlternatingPattern(5, 1, Min, Max), BBB, buffer[..^1]));
		Assert.AreEqual(60, PrintPattern(AlternatingPattern(20, 3, Min, Max), AlternatingPattern(10, 3, Min, Max), AlternatingPattern(10, 3, Min, Max), BBB, buffer));
		AssertAlternatingAllOver(buffer, (20, 3));

		var rgbaBuffer = new TexelRgba32[60];
		Assert.Throws<ArgumentException>(() => PrintPattern(AlternatingPattern(20, 3, Min, Max), AlternatingPattern(10, 2, Min, Max), AlternatingPattern(5, 1, Min, Max), AlternatingPattern(20, 3, Min, Max), BBBB, rgbaBuffer[..^1]));
		Assert.AreEqual(60, PrintPattern(AlternatingPattern(20, 3, Min, Max), AlternatingPattern(10, 3, Min, Max), AlternatingPattern(10, 3, Min, Max), AlternatingPattern(20, 3, Min, Max), BBBB, rgbaBuffer));
		AssertAlternatingAllOver(rgbaBuffer, (20, 3));
	}

	[Test]
	public void ShouldCorrectlySavePatternsWithDelegatePointerOverloads() {
		var targetDir = SetUpCleanTestDir("bmp_delegates");
		using var f = new LocalTinyFfrFactory();

		static TexelRgb24 T(TexelRgb24 t) => new(t.G, t.B, t.R);
		static TexelRgb24 TB(TexelRgb24 t, byte b) => new(t.R, b, t.B);
		static TexelRgb24 BBB(byte r, byte g, byte b) => new(r, g, b);
		static TexelRgba32 BBBB(byte r, byte g, byte b, byte a) => new(r, g, b, a);

		var buffer = new TexelRgb24[60];

		SavePattern(AlternatingPattern(20, 3, Red, Blue), Path.Combine(targetDir, "0.bmp"));
		f.AssetLoader.ReadTexture(Path.Combine(targetDir, "0.bmp"), buffer);
		AssertRedBlueChequerboard(buffer, (20, 3));

		SavePattern(AlternatingPattern(20, 3, new TexelRgb24(0, 255, 0), new TexelRgb24(255, 0, 0)), &T, Path.Combine(targetDir, "1.bmp"));
		f.AssetLoader.ReadTexture(Path.Combine(targetDir, "1.bmp"), buffer);
		AssertRedBlueChequerboard(buffer, (20, 3));
		
		SavePattern(AlternatingPattern(20, 3, Red, Blue), HalfWidthPattern(10, 1, Min, Max), &TB, Path.Combine(targetDir, "2.bmp"));
		f.AssetLoader.ReadTexture(Path.Combine(targetDir, "2.bmp"), buffer);
		AssertRedBlueChequerboardWithMinMax(buffer, (20, 3), (10, 1));

		SavePattern(AlternatingPattern(20, 3, Min, Max), AlternatingPattern(10, 3, Min, Max), AlternatingPattern(10, 3, Min, Max), &BBB, Path.Combine(targetDir, "3.bmp"));
		f.AssetLoader.ReadTexture(Path.Combine(targetDir, "3.bmp"), buffer);
		AssertAlternatingAllOver(buffer, (20, 3));
		
		var rgbaBuffer = new TexelRgba32[60];
		SavePattern(AlternatingPattern(20, 3, Min, Max), AlternatingPattern(10, 3, Min, Max), AlternatingPattern(10, 3, Min, Max), AlternatingPattern(20, 3, Min, Max), &BBBB, Path.Combine(targetDir, "4.bmp"));
		f.AssetLoader.ReadTexture(Path.Combine(targetDir, "4.bmp"), rgbaBuffer);
		AssertAlternatingAllOver(rgbaBuffer, (20, 3));
	}

	[Test]
	public void ShouldCorrectlySavePatternsWithFuncOverloads() {
		var targetDir = SetUpCleanTestDir("bmp_funcs");
		using var f = new LocalTinyFfrFactory();

		static TexelRgb24 T(TexelRgb24 t) => new(t.G, t.B, t.R);
		static TexelRgb24 TB(TexelRgb24 t, byte b) => new(t.R, b, t.B);
		static TexelRgb24 BBB(byte r, byte g, byte b) => new(r, g, b);
		static TexelRgba32 BBBB(byte r, byte g, byte b, byte a) => new(r, g, b, a);

		var buffer = new TexelRgb24[60];


		SavePattern(AlternatingPattern(20, 3, new TexelRgb24(0, 255, 0), new TexelRgb24(255, 0, 0)), T, Path.Combine(targetDir, "1.bmp"));
		f.AssetLoader.ReadTexture(Path.Combine(targetDir, "1.bmp"), buffer);
		AssertRedBlueChequerboard(buffer, (20, 3));

		SavePattern(AlternatingPattern(20, 3, Red, Blue), HalfWidthPattern(10, 1, Min, Max), TB, Path.Combine(targetDir, "2.bmp"));
		f.AssetLoader.ReadTexture(Path.Combine(targetDir, "2.bmp"), buffer);
		AssertRedBlueChequerboardWithMinMax(buffer, (20, 3), (10, 1));

		SavePattern(AlternatingPattern(20, 3, Min, Max), AlternatingPattern(10, 3, Min, Max), AlternatingPattern(10, 3, Min, Max), BBB, Path.Combine(targetDir, "3.bmp"));
		f.AssetLoader.ReadTexture(Path.Combine(targetDir, "3.bmp"), buffer);
		AssertAlternatingAllOver(buffer, (20, 3));

		var rgbaBuffer = new TexelRgba32[60];
		SavePattern(AlternatingPattern(20, 3, Min, Max), AlternatingPattern(10, 3, Min, Max), AlternatingPattern(10, 3, Min, Max), AlternatingPattern(20, 3, Min, Max), BBBB, Path.Combine(targetDir, "4.bmp"));
		f.AssetLoader.ReadTexture(Path.Combine(targetDir, "4.bmp"), rgbaBuffer);
		AssertAlternatingAllOver(rgbaBuffer, (20, 3));
	}
}