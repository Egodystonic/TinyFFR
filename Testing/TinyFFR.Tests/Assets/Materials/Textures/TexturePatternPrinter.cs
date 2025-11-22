// Created on 2025-11-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using static Egodystonic.TinyFFR.Assets.Materials.TexturePatternPrinter;

namespace Egodystonic.TinyFFR.Assets.Materials;

[TestFixture]
class TexturePatternPrinterTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	TexturePattern<TexelRgb24> CreateTestPattern(int xSize, int ySize) => CreateTestPattern<TexelRgb24>(xSize, ySize, default, default);

	TexturePattern<T> CreateTestPattern<T>(int xSize, int ySize, T a, T b) where T : unmanaged {
		return TexturePattern.Chequerboard(
			a, b, (xSize, ySize), cellResolution: 1
		);
	}

	[Test]
	public void ShouldCorrectlyDetermineCompositePatternDimensions() {
		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(CreateTestPattern(10, 5), CreateTestPattern(5, 10)));
		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(CreateTestPattern(5, 10), CreateTestPattern(10, 5)));

		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(CreateTestPattern(10, 5), CreateTestPattern(5, 10), CreateTestPattern(7, 7)));
		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(CreateTestPattern(7, 7), CreateTestPattern(10, 5), CreateTestPattern(5, 10)));
		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(CreateTestPattern(5, 10), CreateTestPattern(7, 7), CreateTestPattern(10, 5)));

		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(CreateTestPattern(10, 5), CreateTestPattern(5, 10), CreateTestPattern(7, 7), CreateTestPattern(7, 7)));
		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(CreateTestPattern(7, 7), CreateTestPattern(10, 5), CreateTestPattern(5, 10), CreateTestPattern(7, 7)));
		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(CreateTestPattern(5, 10), CreateTestPattern(7, 7), CreateTestPattern(10, 5), CreateTestPattern(7, 7)));
		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(CreateTestPattern(7, 7), CreateTestPattern(7, 7), CreateTestPattern(5, 10), CreateTestPattern(10, 5)));
		Assert.AreEqual(new XYPair<int>(10, 10), GetCompositePatternDimensions(CreateTestPattern(10, 5), CreateTestPattern(7, 7), CreateTestPattern(7, 7), CreateTestPattern(5, 10)));
	}

	[Test]
	public void ShouldCorrectlyPrintPatternsWithDelegatePointerOverloads() {
		
	}
}