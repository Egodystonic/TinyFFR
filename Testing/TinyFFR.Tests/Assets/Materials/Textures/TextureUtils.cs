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

		AssertSequence(TextureProcessingConfig.Negate(false, false, false, false), 0, 1, 2, 3, 4, 5, 6, 7);
		AssertSequence(TextureProcessingConfig.Negate(), ~0, ~1, ~2, ~3, ~4, ~5, ~6, ~7);
		AssertTexels(TextureProcessingConfig.Negate(true, false, false, false), Inv(0, 0), Inv(1, 0), Inv(2, 0), Inv(3, 0), Inv(4, 0), Inv(5, 0), Inv(6, 0), Inv(7, 0));
		AssertTexels(TextureProcessingConfig.Negate(false, true, true, false), Inv(0, 1, 2), Inv(1, 1, 2), Inv(2, 1, 2), Inv(3, 1, 2), Inv(4, 1, 2), Inv(5, 1, 2), Inv(6, 1, 2), Inv(7, 1, 2));
		AssertTexels(TextureProcessingConfig.Negate(false, false, false, true), Inv(0, 3), Inv(1, 3), Inv(2, 3), Inv(3, 3), Inv(4, 3), Inv(5, 3), Inv(6, 3), Inv(7, 3));

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
}