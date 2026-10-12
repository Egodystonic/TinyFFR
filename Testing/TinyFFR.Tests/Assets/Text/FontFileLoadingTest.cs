// Created on 2026-10-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Testing;

namespace Egodystonic.TinyFFR.Assets.Text;

[TestFixture]
class FontFileLoadingTest {
	const int WarmupLoadCount = 3;

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void LoadingFontFromFileShouldNotAllocateOnceWarm() {
		using var factory = new LocalTinyFfrFactory();
		var fontPath = CommonTestAssets.FindAsset("DejaVuSans.ttf");

		for (var i = 0; i < WarmupLoadCount; ++i) factory.AssetLoader.LoadFont(fontPath).Dispose();
		var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
		factory.AssetLoader.LoadFont(fontPath).Dispose();
		var allocatedDuring = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

		Assert.AreEqual(0L, allocatedDuring, $"Loading a font from '{fontPath}' allocated {allocatedDuring} bytes.");
	}

	[Test]
	public void LoadingPreprocessedBackdropFromFilesShouldNotAllocateOnceWarm() {
		using var factory = new LocalTinyFfrFactory();
		var skyboxPath = CommonTestAssets.FindAsset(KnownTestAsset.MetroSkyKtx);
		var iblPath = CommonTestAssets.FindAsset(KnownTestAsset.MetroIblKtx);

		for (var i = 0; i < WarmupLoadCount; ++i) factory.AssetLoader.LoadPreprocessedBackdropTexture(skyboxPath, iblPath).Dispose();
		var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
		factory.AssetLoader.LoadPreprocessedBackdropTexture(skyboxPath, iblPath).Dispose();
		var allocatedDuring = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

		Assert.AreEqual(0L, allocatedDuring, $"Loading a preprocessed backdrop allocated {allocatedDuring} bytes.");
	}

	[Test]
	public void EvictedTextShouldBeRebuiltIdentically() {
		const int DistinctTextCount = LocalAssetLoaderConfig.DefaultMaxCachedTextMeshesPerFont * 3;
		using var factory = new LocalTinyFfrFactory();
		using var font = factory.AssetLoader.LoadFont();
		var firstPassSizes = new XYPair<float>[DistinctTextCount];

		for (var i = 0; i < DistinctTextCount; ++i) firstPassSizes[i] = font.MeasureString($"Text number {i}\nSecond line {i * 7}");
		for (var i = 0; i < DistinctTextCount; ++i) {
			var text = $"Text number {i}\nSecond line {i * 7}";
			Assert.AreEqual(firstPassSizes[i], font.MeasureString(text));
			using var str = font.CreateString(text);
			Assert.AreEqual(firstPassSizes[i], str.Size);
		}
	}
}
