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

	[Test]
	public void ShouldCorrectlyApplyUpwardRescaling() {
		var srcDimensions = new XYPair<int>(8, 8);
		var rygbBorderedTex = new TexelRgb24[srcDimensions.Area];
		for (var quadrant = 0; quadrant < 4; ++quadrant) {
			var (xOffset, yOffset) = quadrant switch {
				0 => (0, 0),
				1 => (4, 0),
				2 => (0, 4),
				_ => (4, 4),
			};
			for (var x = 0; x < 4; ++x) {
				for (var y = 0; y < 4; ++y) {
					var q = quadrant;
					if (x + xOffset == 0 || x + xOffset == 7 || y + yOffset == 0 || y + yOffset == 7) {
						q++;
						if (q > 3) q = 0;
					}
					
					rygbBorderedTex[srcDimensions.Index(xOffset + x, yOffset + y)] = q switch {
						0 => new TexelRgb24(255, 0, 0),
						1 => new TexelRgb24(255, 255, 0),
						2 => new TexelRgb24(0, 255, 0),
						_ => new TexelRgb24(0, 0, 255)
					};
				}
			}
		}
		
		var testDir = SetUpCleanTestDir("upward_rescaling");
		Console.WriteLine("Saving generated images to '" + testDir + "'");
		ImageUtils.SaveBitmap(Path.Combine(testDir, "input.bmp"), srcDimensions, rygbBorderedTex);

		void AssertStrategy(Func<int, int, TexelRgb24> expectationBuilder, XYPair<int> destDimensions, TextureCombinationScalingStrategy strategy) {
			var centralOffset = TextureUtils.CalculateCentralizingOffsetForCenterClampSampling(srcDimensions, destDimensions);
			var expected = new TexelRgb24[destDimensions.Area];
			var actual = new TexelRgb24[destDimensions.Area];
			
			for (var y = 0; y < destDimensions.Y; ++y) {
				for (var x = 0; x < destDimensions.X; ++x) {
					expected[destDimensions.Index(x, y)] = expectationBuilder(x, y);
				}
			}
			
			for (var y = 0; y < destDimensions.Y; ++y) {
				for (var x = 0; x < destDimensions.X; ++x) {
					actual[destDimensions.Index(x, y)] = TextureUtils.CalculateUpwardRescaledValue(
						x, y, (ReadOnlySpan<TexelRgb24>) rygbBorderedTex, srcDimensions, destDimensions, centralOffset, strategy
					);
				}
			}
			
			Assert.IsTrue(expected.SequenceEqual(actual));
			ImageUtils.SaveBitmap(Path.Combine(testDir, destDimensions.X + "x" + destDimensions.Y + "_" + strategy + ".bmp"), destDimensions, actual);
		}

		foreach (var destDim in new[] { (128, 128), (64, 128), (128, 64), srcDimensions }) {
			AssertStrategy(
				(x, y) => {
					var wrappedX = x - x / srcDimensions.X * srcDimensions.X;
					var wrappedY = y - y / srcDimensions.Y * srcDimensions.Y;
					return rygbBorderedTex[wrappedY * srcDimensions.X + wrappedX];
				},
				destDim,
				TextureCombinationScalingStrategy.RepeatingTile
			);

			AssertStrategy(
				(x, y) => {
					var halfGapX = (destDim.X - srcDimensions.X) / 2;
					var halfGapY = (destDim.Y - srcDimensions.Y) / 2;
					var srcX = Math.Max(0, Math.Min(x - halfGapX, srcDimensions.X - 1));
					var srcY = Math.Max(0, Math.Min(y - halfGapY, srcDimensions.Y - 1));
					return rygbBorderedTex[srcY * srcDimensions.X + srcX];
				},
				destDim,
				TextureCombinationScalingStrategy.ExtendEdges
			);

			AssertStrategy(
				(x, y) => {
					var srcX = Math.Max(0, Math.Min((int) ((x + 0.5f) * srcDimensions.X / destDim.X), srcDimensions.X - 1));
					var srcY = Math.Max(0, Math.Min((int) ((y + 0.5f) * srcDimensions.Y / destDim.Y), srcDimensions.Y - 1));
					return rygbBorderedTex[srcY * srcDimensions.X + srcX];
				},
				destDim,
				TextureCombinationScalingStrategy.PixelUpscale
			);

			AssertStrategy(
				(x, y) => {
					var srcXf = (x + 0.5f) * srcDimensions.X / (float) destDim.X - 0.5f;
					var srcYf = (y + 0.5f) * srcDimensions.Y / (float) destDim.Y - 0.5f;

					var x0 = (int) MathF.Floor(srcXf);
					var y0 = (int) MathF.Floor(srcYf);
					var x1 = x0 + 1;
					var y1 = y0 + 1;

					var xFrac = srcXf - MathF.Floor(srcXf);
					var yFrac = srcYf - MathF.Floor(srcYf);

					int ClampCoord(int v, int max) => Math.Max(0, Math.Min(v, max));
					int Idx(int cx, int cy) => ClampCoord(cy, srcDimensions.Y - 1) * srcDimensions.X + ClampCoord(cx, srcDimensions.X - 1);

					var rowLow = TexelRgb24.Blend(rygbBorderedTex[Idx(x0, y0)], rygbBorderedTex[Idx(x1, y0)], xFrac);
					var rowHigh = TexelRgb24.Blend(rygbBorderedTex[Idx(x0, y1)], rygbBorderedTex[Idx(x1, y1)], xFrac);
					return TexelRgb24.Blend(rowLow, rowHigh, yFrac);
				},
				destDim,
				TextureCombinationScalingStrategy.BilinearUpscale
			);
		}
	}
}