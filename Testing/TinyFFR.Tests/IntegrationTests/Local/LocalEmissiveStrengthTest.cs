// Created on 2026-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;
using System;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalEmissiveStrengthTest {
	static readonly XYPair<int> RenderDimensions = (640, 640);
	const byte ClippedChannelThreshold = 250;
	const double MaxClippedWhiteFraction = 0.005d;

	static double RenderModelAndMeasureClippedWhiteFraction(string modelFileName) {
		using var factory = new LocalTinyFfrFactory();
		using var loop = factory.ApplicationLoopBuilder.CreateLoop(null);
		using var backdrop = factory.AssetLoader.LoadPreprocessedBackdropTexture(
			CommonTestAssets.FindAsset(KnownTestAsset.MetroSkyKtx),
			CommonTestAssets.FindAsset(KnownTestAsset.MetroIblKtx)
		);
		using var scene = factory.SceneBuilder.CreateScene(backdrop);
		using var sunlight = factory.LightBuilder.CreateDirectionalLight(castsShadows: false);
		scene.Add(sunlight);

		using var loadedResources = factory.AssetLoader.LoadAll(
			CommonTestAssets.FindAsset("models/" + modelFileName),
			new ModelCreationConfig(),
			new ModelReadConfig {
				MeshConfig = new() { LoadSkeletalAnimationDataIfPresent = false, CorrectFlippedOrientation = true },
				HandleUriEscapedStrings = true
			}
		);
		using var modelInstances = factory.ObjectBuilder.CreateModelInstances(loadedResources.Models);
		scene.Add(modelInstances);

		var bounds = loadedResources.Models.CalculateCombinedBoundingBox();
		var boundingRadius = MathF.Max(bounds.HalfWidth, MathF.Max(bounds.HalfHeight, bounds.HalfDepth));
		using var camera = factory.CameraBuilder.CreateCamera(
			bounds.Position + Direction.Backward * (boundingRadius * 3f),
			Direction.Forward,
			CameraPlaneConfiguration.CloseRange
		);

		using var buffer = factory.RendererBuilder.CreateRenderOutputBuffer(textureDimensions: RenderDimensions);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, buffer);

		var clippedWhiteFraction = -1d;
		buffer.ReadNextFrame((size, texels) => {
			Assert.AreEqual(RenderDimensions, size);
			var clippedCount = 0;
			for (var i = 0; i < texels.Length; ++i) {
				var texel = texels[i];
				if (texel.R >= ClippedChannelThreshold && texel.G >= ClippedChannelThreshold && texel.B >= ClippedChannelThreshold) {
					++clippedCount;
				}
			}
			clippedWhiteFraction = clippedCount / (double) texels.Length;
		});

		_ = loop.IterateOnce();
		renderer.RenderAndWaitForGpu();

		Assert.GreaterOrEqual(clippedWhiteFraction, 0d, "Readback callback was never invoked");
		return clippedWhiteFraction;
	}

	[Test]
	public void GltfEmissiveMapsShouldNotClipToWhite() {
		foreach (var modelFileName in new[] { "DamagedHelmet.glb", "EmissiveStrengthTest.glb" }) {
			var clippedWhiteFraction = RenderModelAndMeasureClippedWhiteFraction(modelFileName);
			Console.WriteLine($"{modelFileName}: clipped-white fraction = {clippedWhiteFraction:P4} (max {MaxClippedWhiteFraction:P4})");
			Assert.LessOrEqual(clippedWhiteFraction, MaxClippedWhiteFraction, modelFileName + " renders a blown-out emissive region");
		}
	}
}
