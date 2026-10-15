// Created on 2026-10-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Threading;

namespace Egodystonic.TinyFFR.Assets.Materials;

[TestFixture]
unsafe class TextureTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	sealed class CoverageContext {
		public required XYPair<int> RegionDimensions { get; init; }
		public required int[] VisitCounts { get; init; }
	}

	static void RecordCoverage<TTexel>(Span<TTexel> block, XYPair<int> offset, XYPair<int> dimensions, CoverageContext context) where TTexel : unmanaged {
		Assert.AreEqual(0, offset.X);
		Assert.AreEqual(context.RegionDimensions.X, dimensions.X);
		Assert.AreEqual(dimensions.Area, block.Length);
		for (var i = 0; i < block.Length; ++i) {
			Interlocked.Increment(ref context.VisitCounts[offset.Y * dimensions.X + i]);
			block[i] = default;
		}
	}

	static void ThrowOnBlock(Span<TexelRgba32> block, XYPair<int> offset, XYPair<int> dimensions, int throwRow) {
		if (throwRow >= offset.Y && throwRow < offset.Y + dimensions.Y) throw new InvalidOperationException("Expected test failure.");
	}

	static Texture CreateDynamicTexture<TTexel>(LocalTinyFfrFactory factory, XYPair<int> dimensions) where TTexel : unmanaged, ITexel<TTexel> {
		return factory.TextureBuilder.CreateTexture(
			new TTexel[dimensions.Area].AsSpan(),
			new TextureGenerationConfig { Dimensions = dimensions },
			new TextureCreationConfig { AllowsDynamicWrites = true, DataType = TextureDataType.ColorSrgb }
		);
	}

	[Test]
	public void ParallelizedOverwritesShouldVisitEveryTexelOfTheRegionExactlyOnce() {
		using var factory = new LocalTinyFfrFactory(factoryConfig: new LocalTinyFfrFactoryConfig { ThreadingConfig = new ThreadingConfig { WorkerThreadCount = 4 } });
		using var rgbaTexture = CreateDynamicTexture<TexelRgba32>(factory, (517, 301));
		using var rgbTexture = CreateDynamicTexture<TexelRgb24>(factory, (517, 301));

		var fullContext = new CoverageContext { RegionDimensions = rgbaTexture.Dimensions, VisitCounts = new int[rgbaTexture.Dimensions.Area] };
		rgbaTexture.OverwriteTexelsParallelized<TexelRgba32, CoverageContext>(&RecordCoverage, fullContext);
		Assert.IsTrue(fullContext.VisitCounts.All(c => c == 1));

		var convertingContext = new CoverageContext { RegionDimensions = rgbTexture.Dimensions, VisitCounts = new int[rgbTexture.Dimensions.Area] };
		rgbTexture.OverwriteTexelsParallelized<TexelRgba32, CoverageContext>(&RecordCoverage, convertingContext);
		Assert.IsTrue(convertingContext.VisitCounts.All(c => c == 1));

		var region = new XYPair<int>(200, 150);
		var regionContext = new CoverageContext { RegionDimensions = region, VisitCounts = new int[region.Area] };
		rgbTexture.OverwriteTexelsParallelized<TexelRgb24, CoverageContext>(&RecordCoverage, regionContext, region, (300, 100));
		Assert.IsTrue(regionContext.VisitCounts.All(c => c == 1));

		Assert.Throws<ArgumentOutOfRangeException>(() => rgbTexture.OverwriteTexelsParallelized<TexelRgb24, CoverageContext>(&RecordCoverage, regionContext, region, (400, 200)));
	}

	[Test]
	public void ParallelizedOverwriteExceptionsShouldPropagate() {
		using var factory = new LocalTinyFfrFactory(factoryConfig: new LocalTinyFfrFactoryConfig { ThreadingConfig = new ThreadingConfig { WorkerThreadCount = 4 } });
		using var texture = CreateDynamicTexture<TexelRgba32>(factory, (256, 256));
		for (var i = 0; i < 64; ++i) {
			Assert.Throws<InvalidOperationException>(() => texture.OverwriteTexelsParallelized<TexelRgba32, int>(&ThrowOnBlock, 200));
		}
	}

	[Test]
	public void ParallelizedOverwriteShouldThrowForNonDynamicTextures() {
		using var factory = new LocalTinyFfrFactory();
		using var texture = factory.TextureBuilder.CreateTexture(new TexelRgba32[64 * 64].AsSpan(), (64, 64), TextureDataType.ColorSrgb, generateMipMaps: false);
		var context = new CoverageContext { RegionDimensions = (64, 64), VisitCounts = new int[64 * 64] };
		Assert.Throws<InvalidOperationException>(() => texture.OverwriteTexelsParallelized<TexelRgba32, CoverageContext>(&RecordCoverage, context));
	}
}
