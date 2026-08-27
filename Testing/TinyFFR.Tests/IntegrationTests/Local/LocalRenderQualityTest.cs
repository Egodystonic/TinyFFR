// Created on 2026-07-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalRenderQualityTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory(rendererBuilderConfig: new RendererBuilderConfig { EnableVSync = false });
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Render Quality Test");
		using var camera = factory.CameraBuilder.CreateCamera(new Location(0f, 1.5f, -3f));
		using var cameraController = camera.CreateController<InspectorCameraController>();

		using var sunlight = factory.LightBuilder.CreateDirectionalLight(direction: new Direction(-0.3f, -1f, 0.2f), castsShadows: true);
		using var backdrop = factory.AssetLoader.LoadPreprocessedBackdropTexture(
			CommonTestAssets.FindAsset(KnownTestAsset.MetroSkyKtx),
			CommonTestAssets.FindAsset(KnownTestAsset.MetroIblKtx)
		);
		using var scene = factory.SceneBuilder.CreateScene(backdrop);
		scene.Add(sunlight);

		using var loadedResources = factory.AssetLoader.LoadAll(
			CommonTestAssets.FindAsset("models/showcase_ABeautifulGame.glb"),
			new ModelCreationConfig(),
			new ModelReadConfig() { MeshConfig = new() { LoadSkeletalAnimationDataIfPresent = false, CorrectFlippedOrientation = true }, HandleUriEscapedStrings = true }
		);
		using var modelInstances = factory.ObjectBuilder.CreateModelInstances(loadedResources.Models);
		scene.Add(modelInstances);
		cameraController.SetConstraints(loadedResources.Models.CalculateCombinedBoundingBox());

		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		var shadowQuality = new RenderQualityConfig(BuiltInQualityConfiguration.Medium).ShadowQuality;
		var screenSpaceEffectsQuality = new RenderQualityConfig(BuiltInQualityConfiguration.Medium).ScreenSpaceEffectsQuality;
		var antiAliasing = new RenderQualityConfig(BuiltInQualityConfiguration.Medium).AntiAliasingMode;
		var ambientOcclusionQuality = new RenderQualityConfig(BuiltInQualityConfiguration.Medium).AmbientOcclusionQuality;
		var postProcessingEnabled = new RenderQualityConfig(BuiltInQualityConfiguration.Medium).PostProcessingEnabled;
		var internalResolutionScalar = new RenderQualityConfig(BuiltInQualityConfiguration.Medium).InternalResolutionScalar;
		var hdrColorPrecision = new RenderQualityConfig(BuiltInQualityConfiguration.Medium).HdrColorPrecision;
		var shadowsEnabled = new RenderQualityConfig(BuiltInQualityConfiguration.Medium).ShadowsEnabled;
		var bloomQuality = new RenderQualityConfig(BuiltInQualityConfiguration.Medium).BloomQuality;
		var dofQuality = new RenderQualityConfig(BuiltInQualityConfiguration.Medium).DepthOfFieldQuality;
		var dithering = new RenderQualityConfig(BuiltInQualityConfiguration.Medium).DitheringEnabled;
		
		var fogEnabled = false;
		var dofEnabled = false;
		var lastDefaultQualityLevel = BuiltInQualityConfiguration.Medium;
		void UpdateAllAccordingToBuiltIn(BuiltInQualityConfiguration q) {
			shadowQuality = new RenderQualityConfig(q).ShadowQuality;
			screenSpaceEffectsQuality = new RenderQualityConfig(q).ScreenSpaceEffectsQuality;
			antiAliasing = new RenderQualityConfig(q).AntiAliasingMode;
			ambientOcclusionQuality = new RenderQualityConfig(q).AmbientOcclusionQuality;
			postProcessingEnabled = new RenderQualityConfig(q).PostProcessingEnabled;
			internalResolutionScalar = new RenderQualityConfig(q).InternalResolutionScalar;
			hdrColorPrecision = new RenderQualityConfig(q).HdrColorPrecision;
			shadowsEnabled = new RenderQualityConfig(q).ShadowsEnabled;
			bloomQuality = new RenderQualityConfig(q).BloomQuality;
			dofQuality = new RenderQualityConfig(q).DepthOfFieldQuality;
			dithering = new RenderQualityConfig(q).DitheringEnabled;
		}

		static Quality CycleQuality(Quality q) => q >= Quality.VeryHigh ? Quality.VeryLow : (Quality) ((int) q + 1);
		static BuiltInQualityConfiguration CycleBuiltInQuality(BuiltInQualityConfiguration q) {
			return q switch {
				BuiltInQualityConfiguration.Medium => BuiltInQualityConfiguration.High,
				BuiltInQualityConfiguration.High => BuiltInQualityConfiguration.Ultra,
				BuiltInQualityConfiguration.Ultra => BuiltInQualityConfiguration.Lowest,
				BuiltInQualityConfiguration.Lowest => BuiltInQualityConfiguration.Low,
				_ => BuiltInQualityConfiguration.Medium,
			};
		}
		static AntiAliasingMode CycleAa(AntiAliasingMode m) => m >= AntiAliasingMode.TaaIncreasedSharpening ? AntiAliasingMode.None : (AntiAliasingMode) ((int) m + 1);
		static float CycleResScale(float s) => s <= 0.25f ? 1f : s - 0.25f; // 1.0 -> 0.75 -> 0.5 -> 0.25 -> 1.0

		RenderQualityConfig BuildConfig() => new() {
			ShadowQuality = shadowQuality,
			ScreenSpaceEffectsQuality = screenSpaceEffectsQuality,
			AntiAliasingMode = antiAliasing,
			AmbientOcclusionQuality = ambientOcclusionQuality,
			PostProcessingEnabled = postProcessingEnabled,
			InternalResolutionScalar = internalResolutionScalar,
			HdrColorPrecision = hdrColorPrecision,
			ShadowsEnabled = shadowsEnabled,
			BloomQuality = bloomQuality,
			DepthOfFieldQuality = dofQuality,
			DitheringEnabled = dithering
		};

		var summary = "";
		static string QualityShortString(Quality q) => q switch {
			Quality.VeryLow => "--",
			Quality.Low => "-",
			Quality.Standard => "=",
			Quality.High => "+",
			Quality.VeryHigh => "++",
			_ => q.ToString()
		};
		static string BoolShortString(bool b) => b ? "✔️" : "❌";
		void ApplyAndReport() {
			renderer.SetQuality(BuildConfig());
			summary =
				$"[1]AA:{antiAliasing} [2]AO:{QualityShortString(ambientOcclusionQuality)} [3]PP:{BoolShortString(postProcessingEnabled)} [4]Scale:{internalResolutionScalar:0.00} " +
				$"[5]HDR:{QualityShortString(hdrColorPrecision)} [6]Shad:{BoolShortString(shadowsEnabled)} [7]Bloom:{QualityShortString(bloomQuality)} [8]Dthr:{BoolShortString(dithering)} " +
				$"[9]ShadQ:{QualityShortString(shadowQuality)} [0]SSE:{QualityShortString(screenSpaceEffectsQuality)} [-]DoF:{QualityShortString(dofQuality)} [Q]All";
		}

		ApplyAndReport();

		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();
			var kbm = loop.Input.KeyboardAndMouse;

			var changed = true;
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow1)) antiAliasing = CycleAa(antiAliasing);
			else if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow2)) ambientOcclusionQuality = CycleQuality(ambientOcclusionQuality);
			else if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow3)) postProcessingEnabled = !postProcessingEnabled;
			else if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow4)) internalResolutionScalar = CycleResScale(internalResolutionScalar);
			else if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow5)) hdrColorPrecision = CycleQuality(hdrColorPrecision);
			else if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow6)) shadowsEnabled = !shadowsEnabled;
			else if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow7)) bloomQuality = CycleQuality(bloomQuality);
			else if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow8)) dithering = !dithering;
			else if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow9)) shadowQuality = CycleQuality(shadowQuality);
			else if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow0)) screenSpaceEffectsQuality = CycleQuality(screenSpaceEffectsQuality);
			else if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.Minus)) dofQuality = CycleQuality(dofQuality);
			else if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.Q)) {
				lastDefaultQualityLevel = CycleBuiltInQuality(lastDefaultQualityLevel);
				UpdateAllAccordingToBuiltIn(lastDefaultQualityLevel);
			}
			else changed = false;

			if (changed) ApplyAndReport();
			
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.F)) {
				fogEnabled = !fogEnabled;
				if (fogEnabled) scene.AddFog(new FogDescriptor(FogDensity.VeryThick) { StartDistance = 0.05f });
				else scene.RemoveFog();
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.D)) {
				dofEnabled = !dofEnabled;
				camera.FocusDistance = dofEnabled ? 0.1f : null;
			}

			window.SetTitle(summary + $" [D]DoF:{BoolShortString(dofEnabled)} [F]Fog:{BoolShortString(fogEnabled)}" + " | " + loop.FramesPerSecondRecentAverage.ToString("0000") + " FPS");

			DefaultCameraInputHandler.TickKbm(kbm, cameraController, dt, window);
			DefaultCameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, cameraController, dt);
			DefaultCameraInputHandler.Progress(cameraController, dt);

			renderer.Render();
		}
		
		scene.RemoveAll();
	}
}
