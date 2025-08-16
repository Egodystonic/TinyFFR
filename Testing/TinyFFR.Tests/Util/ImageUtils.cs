// Created on 2025-08-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR;

[TestFixture]
class ImageUtilsTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlySaveBitmaps() {
		const int InputWidth = 50;
		const int InputHeight = 100;
		var rgbaInput = new TexelRgba32[InputWidth * InputHeight];
		var rgbInput = new TexelRgb24[rgbaInput.Length];
		for (var i = 0; i < rgbaInput.Length; ++i) {
			rgbaInput[i] = TexelRgba32.ConvertFrom(ColorVect.Random());
			rgbInput[i] = rgbaInput[i].AsRgb24;
		}

		var testDir = SetUpCleanTestDir("bitmap_save");
		Console.WriteLine("Writing bitmaps to " + testDir);
		var bitmaps = new Dictionary<string, (bool RgbaIn, BitmapSaveConfig Config)>();
		var expectations = new Dictionary<BitmapSaveConfig, TexelRgba32[]>();
		for (var i = 0b000U; i < 0b1000U; ++i) {
			var config = new BitmapSaveConfig((i & 0b100U) != 0U, (i & 0b10U) != 0U, (i & 0b1U) != 0U);
			bitmaps.Add(Path.Combine(testDir, $"rgba_{i:b3}.bmp"), (true, config));
			bitmaps.Add(Path.Combine(testDir, $"rgb_{i:b3}.bmp"), (false, config));

			var expectation = new TexelRgba32[rgbaInput.Length];
			for (var y = 0; y < InputHeight; ++y) {
				var expectationY = config.FlipVertical ? InputHeight - (y + 1) : y;
				for (var x = 0; x < InputWidth; ++x) {
					var expectationX = config.FlipHorizontal ? InputWidth - (x + 1) : x;
					var expectationIndex = expectationY * InputWidth + expectationX;
					expectation[expectationIndex] = rgbaInput[y * InputWidth + x];
					if (!config.IncludeAlphaChannel) expectation[expectationIndex] = expectation[expectationIndex] with { A = 255 };
				}
			}
			expectations.Add(config, expectation);
		}
		foreach (var kvp in bitmaps) {
			if (kvp.Value.RgbaIn) {
				ImageUtils.SaveBitmap(kvp.Key, (InputWidth, InputHeight), (ReadOnlySpan<TexelRgba32>) rgbaInput.AsSpan(), kvp.Value.Config);
			}
			else {
				ImageUtils.SaveBitmap(kvp.Key, (InputWidth, InputHeight), (ReadOnlySpan<TexelRgb24>) rgbInput.AsSpan(), kvp.Value.Config);
			}
		}
		
		using var factory = new LocalTinyFfrFactory();
		var destBuffer = new TexelRgba32[InputWidth * InputHeight];
		foreach (var kvp in bitmaps) {
			try {
				var metadata = factory.AssetLoader.ReadTextureMetadata(kvp.Key);
				Assert.AreEqual(InputWidth, metadata.Width);
				Assert.AreEqual(InputHeight, metadata.Height);
				factory.AssetLoader.ReadTexture(kvp.Key, destBuffer.AsSpan());
				if (Path.GetFileName(kvp.Key).StartsWith("rgb_", StringComparison.OrdinalIgnoreCase)) {
					Assert.IsTrue(destBuffer.SequenceEqual(expectations[kvp.Value.Config with { IncludeAlphaChannel = false }]));
				}
				else {
					Assert.IsTrue(destBuffer.SequenceEqual(expectations[kvp.Value.Config]));
				}
			}
			catch {
				Console.WriteLine($"Failed for file {kvp.Key}.");
				throw;
			}
		}
	}
}