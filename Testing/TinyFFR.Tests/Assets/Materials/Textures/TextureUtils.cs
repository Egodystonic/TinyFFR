// Created on 2025-11-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Assets.Materials;

[TestFixture]
class TextureUtilsTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyProcessTextures() {
		var testTexture = new TexelRgba32[] {
			// 0                        1                        2                        3
			new(255, 000, 000, 255), new(255, 000, 000, 255), new(255, 000, 000, 255), new(255, 000, 000, 255),
			// 4                        5                        6                        7
			new(255, 000, 000, 000), new(000, 255, 000, 000), new(000, 000, 255, 000), new(000, 000, 000, 255),
		};

		TexelRgba32 Inv(int idx, params int[] channels) => channels.Aggregate(testTexture[idx], (t, c) => t.WithInvertedChannelIfPresent(c));
		TexelRgba32 Swiz(int idx, int r, int g, int b, int a) => new(testTexture[idx][r], testTexture[idx][g], testTexture[idx][b], testTexture[idx][a]);

		void AssertSequence(TextureProcessingConfig c, params int[] texelIndices) {
			static TexelRgba32 Invert(TexelRgba32 t) => new((byte) (255 - t.R), (byte) (255 - t.G), (byte) (255 - t.B), (byte) (255 - t.A));

			var processedTexture = testTexture.ToArray();
			TextureUtils.ProcessTexture(processedTexture, (4, 2), c);

			for (var i = 0; i < 8; ++i) {
				var expectedIndex = texelIndices[i];
				var expectation = expectedIndex is < 0 or > 7
					? Invert(testTexture[~expectedIndex])
					: testTexture[expectedIndex];
				
				Assert.AreEqual(expectation, processedTexture[i]);
			}
		}

		void AssertTexels(TextureProcessingConfig c, params TexelRgba32[] texels) {
			var processedTexture = testTexture.ToArray();
			TextureUtils.ProcessTexture(processedTexture, (4, 2), c);

			for (var i = 0; i < texels.Length; ++i) {
				Assert.AreEqual(texels[i], processedTexture[i]);
			}
		}

		AssertSequence(TextureProcessingConfig.None, 0, 1, 2, 3, 4, 5, 6, 7);

		AssertSequence(TextureProcessingConfig.Flip(true, false), 3, 2, 1, 0, 7, 6, 5, 4);
		AssertSequence(TextureProcessingConfig.Flip(false, true), 4, 5, 6, 7, 0, 1, 2, 3);
		AssertSequence(TextureProcessingConfig.Flip(true, true), 7, 6, 5, 4, 3, 2, 1, 0);

		AssertSequence(TextureProcessingConfig.Invert(false, false, false, false), 0, 1, 2, 3, 4, 5, 6, 7);
		AssertSequence(TextureProcessingConfig.Invert(), ~0, ~1, ~2, ~3, ~4, ~5, ~6, ~7);
		AssertTexels(TextureProcessingConfig.Invert(true, false, false, false), Inv(0, 0), Inv(1, 0), Inv(2, 0), Inv(3, 0), Inv(4, 0), Inv(5, 0), Inv(6, 0), Inv(7, 0));
		AssertTexels(TextureProcessingConfig.Invert(false, true, true, false), Inv(0, 1, 2), Inv(1, 1, 2), Inv(2, 1, 2), Inv(3, 1, 2), Inv(4, 1, 2), Inv(5, 1, 2), Inv(6, 1, 2), Inv(7, 1, 2));
		AssertTexels(TextureProcessingConfig.Invert(false, false, false, true), Inv(0, 3), Inv(1, 3), Inv(2, 3), Inv(3, 3), Inv(4, 3), Inv(5, 3), Inv(6, 3), Inv(7, 3));

		AssertSequence(TextureProcessingConfig.Swizzle(), 0, 1, 2, 3, 4, 5, 6, 7);
		AssertTexels(TextureProcessingConfig.Swizzle(ColorChannel.G, ColorChannel.B, ColorChannel.A, ColorChannel.R), Enumerable.Range(0, 8).Select(i => Swiz(i, 1, 2, 3, 0)).ToArray());

		AssertTexels(
			new TextureProcessingConfig {
				FlipX = true,
				FlipY = true,
				InvertXRedChannel = true,
				InvertYGreenChannel = true,
				InvertZBlueChannel = true,
				InvertWAlphaChannel = true,
				XRedFinalOutputSource = ColorChannel.A,
				YGreenFinalOutputSource = ColorChannel.R,
				ZBlueFinalOutputSource = ColorChannel.G,
				WAlphaFinalOutputSource = ColorChannel.B,
			},
			 new(000, 255, 255, 255), new(255, 255, 255, 000), new(255, 255, 000, 255), new(255, 000, 255, 255),
			 new(000, 000, 255, 255), new(000, 000, 255, 255), new(000, 000, 255, 255), new(000, 000, 255, 255)
		);
	}

	[Test]
	public void ShouldCorrectlyConvertRgbaTextures() {
		var testTexture = new TexelRgba32[8 * 8];
		for (var i = 0; i < testTexture.Length; ++i) testTexture[i] = new TexelRgba32(ColorVect.Random());
		
		var rgbDest = new TexelRgb24[testTexture.Length];
		var rgbaDest = new TexelRgba32[testTexture.Length];
		TextureUtils.Convert(testTexture, rgbDest);
		TextureUtils.Convert(testTexture, rgbaDest);
		
		for (var i = 0; i < testTexture.Length; ++i) {
			Assert.AreEqual(testTexture[i], rgbaDest[i]);
			Assert.AreEqual(testTexture[i].ToRgb24(), rgbDest[i]);
		}
	}
	
	[Test]
	public void ShouldCorrectlyConvertRgbTextures() {
		var testTexture = new TexelRgb24[8 * 8];
		for (var i = 0; i < testTexture.Length; ++i) testTexture[i] = new TexelRgb24(ColorVect.Random());
		
		var rgbDest = new TexelRgb24[testTexture.Length];
		var rgbaDest = new TexelRgba32[testTexture.Length];
		TextureUtils.Convert(testTexture, rgbDest);
		TextureUtils.Convert(testTexture, rgbaDest);
		
		for (var i = 0; i < testTexture.Length; ++i) {
			Assert.AreEqual(testTexture[i].ToRgba32(), rgbaDest[i]);
			Assert.AreEqual(testTexture[i], rgbDest[i]);
		}
	}

	[Test]
	public void TextureCombinationConfigShouldCorrectlySelectTexels() {
		var texels = new TexelRgba32[] {
			new(0xA0, 0xA1, 0xA2, 0xA3),
			new(0xB0, 0xB1, 0xB2, 0xB3),
			new(0xC0, 0xC1, 0xC2, 0xC3),
			new(0xD0, 0xD1, 0xD2, 0xD3)
		};

		void AssertCombination(string combination) {
			var comboConfig = new TextureCombinationConfig(combination);
			Assert.AreEqual(
				new TexelRgba32(
					Convert.ToByte(combination[0..2], 16), 
					Convert.ToByte(combination[2..4], 16), 
					Convert.ToByte(combination[4..6], 16), 
					Convert.ToByte(combination[6..8], 16)
				), 
				comboConfig.SelectTexel<TexelRgba32, TexelRgba32, byte>(texels)
			);
		}
		
		AssertCombination("a0a1a2a3");
		AssertCombination("d3c2b1a0");
		AssertCombination("b1a3d0c2");
	}

	[Test]
	public void ShouldCorrectlyCombineTextures() {
		var dimensions = new XYPair<int>(8, 8); 
		TexelRgba32[] CreateRandomRgbaTexture() => Enumerable.Range(0, dimensions.Area).Select(_ => new TexelRgba32(ColorVect.Random())).ToArray();
		TexelRgb24[] CreateRandomRgbTexture() => Enumerable.Range(0, dimensions.Area).Select(_ => new TexelRgb24(ColorVect.Random())).ToArray();
		
		var rgbaA = CreateRandomRgbaTexture();
		var rgbaB = CreateRandomRgbaTexture();
		var rgbaC = CreateRandomRgbaTexture();
		var rgbaD = CreateRandomRgbaTexture();
		
		var rgbA = CreateRandomRgbTexture();
		var rgbB = CreateRandomRgbTexture();
		var rgbC = CreateRandomRgbTexture();
		var rgbD = CreateRandomRgbTexture();
		
		void AssertCombinationRgbaToRgba(int numInputs, string combinationStr) {
			var destBuffer = new TexelRgba32[dimensions.Area];
			var comboConf = new TextureCombinationConfig(combinationStr);
			if (numInputs == 2) TextureUtils.CombineTextures(rgbaA, dimensions, rgbaB, dimensions, comboConf, destBuffer);
			else if (numInputs == 3) TextureUtils.CombineTextures(rgbaA, dimensions, rgbaB, dimensions, rgbaC, dimensions, comboConf, destBuffer);
			else TextureUtils.CombineTextures(rgbaA, dimensions, rgbaB, dimensions, rgbaC, dimensions, rgbaD, dimensions, comboConf, destBuffer);

			for (var i = 0; i < destBuffer.Length; ++i) {
				var inputs = numInputs switch {
					2 => new[] { rgbaA[i], rgbaB[i] },
					3 => new[] { rgbaA[i], rgbaB[i], rgbaC[i] },
					_ => new[] { rgbaA[i], rgbaB[i], rgbaC[i], rgbaD[i] }
				};
				var expectation = comboConf.SelectTexel<TexelRgba32, TexelRgba32, byte>(inputs);
				Assert.AreEqual(expectation, destBuffer[i]);
			}
		}
		void AssertCombinationRgbToRgb(int numInputs, string combinationStr) {
			var destBuffer = new TexelRgb24[dimensions.Area];
			var comboConf = new TextureCombinationConfig(combinationStr);
			if (numInputs == 2) TextureUtils.CombineTextures(rgbA, dimensions, rgbB, dimensions, comboConf, destBuffer);
			else if (numInputs == 3) TextureUtils.CombineTextures(rgbA, dimensions, rgbB, dimensions, rgbC, dimensions, comboConf, destBuffer);
			else TextureUtils.CombineTextures(rgbA, dimensions, rgbB, dimensions, rgbC, dimensions, rgbD, dimensions, comboConf, destBuffer);

			for (var i = 0; i < destBuffer.Length; ++i) {
				var inputs = numInputs switch {
					2 => new[] { rgbA[i], rgbB[i] },
					3 => new[] { rgbA[i], rgbB[i], rgbC[i] },
					_ => new[] { rgbA[i], rgbB[i], rgbC[i], rgbD[i] }
				};
				var expectation = comboConf.SelectTexel<TexelRgb24, TexelRgb24, byte>(inputs);
				Assert.AreEqual(expectation, destBuffer[i]);
			}
		}
		void AssertCombinationRgbaToRgb(int numInputs, string combinationStr) {
			var destBuffer = new TexelRgb24[dimensions.Area];
			var comboConf = new TextureCombinationConfig(combinationStr);
			if (numInputs == 2) TextureUtils.CombineTextures(rgbaA, dimensions, rgbaB, dimensions, comboConf, destBuffer);
			else if (numInputs == 3) TextureUtils.CombineTextures(rgbaA, dimensions, rgbaB, dimensions, rgbaC, dimensions, comboConf, destBuffer);
			else TextureUtils.CombineTextures(rgbaA, dimensions, rgbaB, dimensions, rgbaC, dimensions, rgbaD, dimensions, comboConf, destBuffer);

			for (var i = 0; i < destBuffer.Length; ++i) {
				var inputs = numInputs switch {
					2 => new[] { rgbaA[i], rgbaB[i] },
					3 => new[] { rgbaA[i], rgbaB[i], rgbaC[i] },
					_ => new[] { rgbaA[i], rgbaB[i], rgbaC[i], rgbaD[i] }
				};
				var expectation = comboConf.SelectTexel<TexelRgba32, TexelRgb24, byte>(inputs);
				Assert.AreEqual(expectation, destBuffer[i]);
			}
		}
		void AssertCombinationRgbToRgba(int numInputs, string combinationStr) {
			var destBuffer = new TexelRgba32[dimensions.Area];
			var comboConf = new TextureCombinationConfig(combinationStr);
			if (numInputs == 2) TextureUtils.CombineTextures(rgbA, dimensions, rgbB, dimensions, comboConf, destBuffer);
			else if (numInputs == 3) TextureUtils.CombineTextures(rgbA, dimensions, rgbB, dimensions, rgbC, dimensions, comboConf, destBuffer);
			else TextureUtils.CombineTextures(rgbA, dimensions, rgbB, dimensions, rgbC, dimensions, rgbD, dimensions, comboConf, destBuffer);

			for (var i = 0; i < destBuffer.Length; ++i) {
				var inputs = numInputs switch {
					2 => new[] { rgbA[i], rgbB[i] },
					3 => new[] { rgbA[i], rgbB[i], rgbC[i] },
					_ => new[] { rgbA[i], rgbB[i], rgbC[i], rgbD[i] }
				};
				var expectation = comboConf.SelectTexel<TexelRgb24, TexelRgba32, byte>(inputs);
				Assert.AreEqual(expectation, destBuffer[i]);
			}
		}
		
		void AssertAll(int numInputs, string combinationStr) {
			AssertCombinationRgbToRgb(numInputs, combinationStr);
			AssertCombinationRgbToRgba(numInputs, combinationStr);
			AssertCombinationRgbaToRgb(numInputs, combinationStr);
			AssertCombinationRgbaToRgba(numInputs, combinationStr);
		}
		
		AssertAll(2, "0R1G0B1B");
		AssertAll(2, "1A0R1B0G");
		AssertAll(3, "0R1G2B1B");
		AssertAll(3, "1A2R1B0G");
		AssertAll(4, "0R3G2B1B");
		AssertAll(4, "1A2R3B0G");
	}
}