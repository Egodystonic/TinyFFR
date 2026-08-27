// Created on 2026-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Testing.Local;

static class CompositorProbe {
	const string ProbeModel = "BoxTexturedNonPowerOfTwo.glb";
	const float OverlayYawDegreesPerSec = -130f;
	const float OverlayPitchDegreesPerSec = -80f;
	const float AssumedDeltaTime = 1f / 60f;
	static readonly XYPair<int> RenderDimensions = new(960, 540);

	static double MeanLuma(ReadOnlySpan<TexelRgba32> texels) {
		if (texels.Length == 0) return 0d;
		var total = 0d;
		for (var i = 0; i < texels.Length; ++i) {
			var texel = texels[i];
			total += 0.2126d * texel.R + 0.7152d * texel.G + 0.0722d * texel.B;
		}
		return total / texels.Length;
	}

	public static void Execute(string[] args) {
		var frameCount = 240;
		foreach (var arg in args) {
			if (arg.StartsWith("frames=", StringComparison.OrdinalIgnoreCase)) Int32.TryParse(arg[7..], out frameCount);
		}

		Console.WriteLine($"=== Compositor overlay probe :: frames={frameCount} per pass ===");
		Console.WriteLine("Renders a Standard main layer + a RetainPreviousScenes overlay layer through a");
		Console.WriteLine("compositor, measuring mean luma of every composited frame.");
		Console.WriteLine();

		using var factory = new LocalTinyFfrFactory();
		using var loop = factory.ApplicationLoopBuilder.CreateLoop();

		var backdrop = factory.AssetLoader.LoadPreprocessedBackdropTexture(
			CommonTestAssets.FindAsset(KnownTestAsset.MetroSkyKtx),
			CommonTestAssets.FindAsset(KnownTestAsset.MetroIblKtx)
		);
		var mainScene = factory.SceneBuilder.CreateScene(backdrop);
		var sunlight = factory.LightBuilder.CreateDirectionalLight(castsShadows: true);
		mainScene.Add(sunlight);

		var resources = factory.AssetLoader.LoadAll(
			CommonTestAssets.FindAsset("models/" + ProbeModel),
			new ModelCreationConfig { MeshConfig = new() { GenerateWireframeData = true } },
			new ModelReadConfig { MeshConfig = new() { LoadSkeletalAnimationDataIfPresent = false, CorrectFlippedOrientation = true }, HandleUriEscapedStrings = true }
		);
		var modelInstances = factory.ObjectBuilder.CreateModelInstances(resources.Models);
		mainScene.Add(modelInstances);

		var bounds = resources.Models.CalculateCombinedBoundingBox();
		var boundingRadius = MathF.Max(bounds.HalfWidth, MathF.Max(bounds.HalfHeight, bounds.HalfDepth));
		var mainCamera = factory.CameraBuilder.CreateCamera(
			bounds.Position + Direction.Backward * (boundingRadius * 3f),
			Direction.Forward,
			CameraPlaneConfiguration.CloseRange
		);

		// Mirrors ModelViewerScene.EnsureOverlayResourcesExistAsync
		var overlayCamera = factory.CameraBuilder.CreateCamera(Location.Origin);
		var overlayScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.None);
		var overlayMesh = factory.AssetLoader.MeshBuilder.CreateMesh(Cuboid.UnitCube);
		var overlayTexture = factory.AssetLoader.LoadColorMap(factory.AssetLoader.BuiltInTexturePaths.White);
		var overlayMaterial = factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(overlayTexture);
		var overlayInstance = factory.ObjectBuilder.CreateModelInstance(
			overlayMesh,
			overlayMaterial,
			initialPosition: overlayCamera.Position + Direction.Forward * 3f + Direction.Left * 0.9f + Direction.Up * 0.9f
		);
		var overlayLight = factory.LightBuilder.CreatePointLight(overlayCamera.Position, ColorVect.FromHueSaturationLightness(120f, 0.8f, 0.75f));
		overlayScene.Add(overlayInstance);
		overlayScene.Add(overlayLight);

		var buffer = factory.RendererBuilder.CreateRenderOutputBuffer(textureDimensions: RenderDimensions);
		var mainRenderer = factory.RendererBuilder.CreateRenderer(mainScene, mainCamera, buffer);
		mainRenderer.SetQuality(BuiltInQualityConfiguration.Ultra);
		var overlayRenderer = factory.RendererBuilder.CreateRenderer(overlayScene, overlayCamera, buffer);

		var compositor = factory.RendererBuilder.CreateCompositor(buffer);
		compositor.Add(mainRenderer, RenderCompositionType.Standard);
		compositor.Add(overlayRenderer, RenderCompositionType.RetainPreviousScenes);

		double RunPass(string label, AntiAliasingMode overlayAaMode) {
			overlayRenderer.SetQuality(new RenderQualityConfig(BuiltInQualityConfiguration.High) {
				AntiAliasingMode = overlayAaMode
			});

			var lumas = new List<double>(frameCount);
			for (var frame = 0; frame < frameCount; ++frame) {
				overlayInstance.RotateBy((OverlayYawDegreesPerSec * AssumedDeltaTime) % Direction.Up);
				overlayInstance.RotateBy((OverlayPitchDegreesPerSec * AssumedDeltaTime) % Direction.Right);

				var captured = -1d;
				buffer.ReadNextFrame((_, texels) => captured = MeanLuma(texels));
				_ = loop.IterateOnce();
				compositor.RenderAll();
				compositor.WaitForGpu();
				if (captured >= 0d) lumas.Add(captured);
			}

			if (lumas.Count == 0) {
				Console.WriteLine($"{label}: no frames captured!");
				return 0d;
			}

			// Ignore the first handful while any temporal history converges from cold
			var settled = lumas.Skip(Int32.Min(20, lumas.Count - 1)).ToList();
			var min = settled.Min();
			var max = settled.Max();
			var mean = settled.Average();
			var swing = max - min;
			var swingPct = mean > 0d ? swing / mean * 100d : 0d;

			// A dip is a frame materially below the running mean
			var dips = settled.Count(v => v < mean * 0.85d);

			Console.WriteLine($"--- {label} (overlay AA = {overlayAaMode}) ---");
			Console.WriteLine($"  frames captured : {lumas.Count} ({settled.Count} after warm-up)");
			Console.WriteLine($"  mean luma       : {mean,7:N2}");
			Console.WriteLine($"  min / max       : {min,7:N2} / {max,7:N2}");
			Console.WriteLine($"  peak-to-trough  : {swing,7:N2}  ({swingPct:N1}% of mean)");
			Console.WriteLine($"  frames < 85%    : {dips}");
			Console.Write("  series          : ");
			for (var i = 0; i < settled.Count; i += Int32.Max(1, settled.Count / 40)) Console.Write($"{settled[i]:N0} ");
			Console.WriteLine();
			Console.WriteLine();
			return swingPct;
		}

		var taaSwing = RunPass("PASS 1: default overlay quality", AntiAliasingMode.TaaBalanced);
		var fxaaSwing = RunPass("PASS 2: overlay without temporal AA", AntiAliasingMode.Fxaa);

		Console.WriteLine("=== RESULT ===");
		Console.WriteLine($"TAA overlay  swing: {taaSwing,6:N1}% of mean");
		Console.WriteLine($"FXAA overlay swing: {fxaaSwing,6:N1}% of mean");
		Console.WriteLine(taaSwing > fxaaSwing * 2d && taaSwing > 5d
			? "CONFIRMED: temporal AA on the translucent overlay layer causes the brightness excursions."
			: "NOT CONFIRMED: TAA is not the differentiator here. Do not ship the AA change on this evidence.");

		compositor.Dispose();
		overlayRenderer.Dispose();
		mainRenderer.Dispose();
		buffer.Dispose();
		overlayScene.Remove(overlayInstance);
		overlayScene.Remove(overlayLight);
		overlayScene.Dispose();
		overlayInstance.Dispose();
		overlayLight.Dispose();
		overlayMaterial.Dispose();
		overlayTexture.Dispose();
		overlayMesh.Dispose();
		overlayCamera.Dispose();
		mainScene.Remove(modelInstances);
		modelInstances.Dispose();
		resources.Dispose();
		mainCamera.Dispose();
		mainScene.RemoveBackdrop();
		mainScene.Remove(sunlight);
		mainScene.Dispose();
		sunlight.Dispose();
		backdrop.Dispose();
	}
}
