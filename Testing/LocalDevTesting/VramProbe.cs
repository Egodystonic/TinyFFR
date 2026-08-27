// Created on 2026-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Diagnostics;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Testing.Local;

static class VramProbe {
	const int AbortAtUsedMib = 11_000;
	static readonly XYPair<int> RenderDimensions = new(1920, 1080);
	static readonly TimeSpan LoadTimeout = TimeSpan.FromMinutes(5d);

	static readonly string[] ProbeModels = {
		"BoxTextured.glb",
		"BoxTexturedNonPowerOfTwo.glb",
		"NormalTangentMirrorTest.glb",
		"NegativeScaleTest.glb",
		"TextureCoordinateTest.glb",
		"CompareNormal.glb",
		"CompareRoughness.glb",
		"CompareMetallic.glb",
		"MetalRoughSpheres.glb",
		"CompareAmbientOcclusion.glb",
		"AnisotropyStrengthTest.glb",
		"AnisotropyDiscTest.glb",
		"EmissiveStrengthTest.glb",
		"TransmissionTest.glb",
		"CompareTransmission.glb",
		"TransmissionRoughnessTest.glb",
		"AttenuationTest.glb",
		"CompareIor.glb",
		"ClearCoatTest.glb",
		"BarramundiFish.glb",
		"Avocado.glb",
		"DamagedHelmet.glb",
		"showcase_GlassHurricaneCandleHolder.glb",
		"showcase_MaterialsVariantsShoe.glb",
		"showcase_PotOfCoals.glb",
		"showcase_ToyCar.glb",
		"showcase_AnisotropyBarnLamp.glb",
		"showcase_CarConcept.glb",
		"showcase_ChronographWatch.glb",
		"showcase_CommercialRefrigerator.glb",
		"showcase_MosquitoInAmber.glb",
		"showcase_ABeautifulGame.glb",
		"NodePerformanceTest.glb"
	};

	static long _steadyBaselineMib;

	static long SampleVramMib() {
		try {
			using var process = Process.Start(new ProcessStartInfo {
				FileName = "nvidia-smi",
				Arguments = "--query-gpu=memory.used --format=csv,noheader,nounits",
				RedirectStandardOutput = true,
				UseShellExecute = false
			});
			if (process == null) return -1L;
			var output = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			return Int64.TryParse(output.Split('\n')[0].Trim(), out var result) ? result : -1L;
		}
		catch (Exception e) {
			Console.WriteLine($"Could not sample VRAM: {e.Message}");
			return -1L;
		}
	}

	static long Report(string label) {
		var used = SampleVramMib();
		var delta = _steadyBaselineMib > 0L && used > 0L ? used - _steadyBaselineMib : 0L;
		Console.WriteLine($"{label,-46} {used,7} MiB   ({delta,+6} vs steady baseline)");
		return used;
	}

	static bool ShouldAbort(long used) {
		if (used < AbortAtUsedMib) return false;
		Console.WriteLine($"*** ABORTING: {used} MiB used, close to the 12288 MiB ceiling. ***");
		return true;
	}

	public static void Execute(string[] args) {
		var useSyncLoad = args.Contains("sync", StringComparer.OrdinalIgnoreCase);
		var skipInstances = args.Contains("noinstances", StringComparer.OrdinalIgnoreCase);
		var renderFrames = 300;
		var modelLimit = ProbeModels.Length;
		foreach (var arg in args) {
			if (arg.StartsWith("frames=", StringComparison.OrdinalIgnoreCase)) Int32.TryParse(arg[7..], out renderFrames);
			if (arg.StartsWith("models=", StringComparison.OrdinalIgnoreCase)) Int32.TryParse(arg[7..], out modelLimit);
		}
		modelLimit = Int32.Clamp(modelLimit, 0, ProbeModels.Length);

		Console.WriteLine($"=== VRAM probe :: load={(useSyncLoad ? "SYNC" : "ASYNC")} models={modelLimit} frames={renderFrames} instances={!skipInstances} ===");
		Console.WriteLine();

		Console.WriteLine($"{"[before factory]",-46} {SampleVramMib(),7} MiB");

		using var factory = new LocalTinyFfrFactory();
		using var loop = factory.ApplicationLoopBuilder.CreateLoop();

		var backdrop = factory.AssetLoader.LoadPreprocessedBackdropTexture(
			CommonTestAssets.FindAsset(KnownTestAsset.MetroSkyKtx),
			CommonTestAssets.FindAsset(KnownTestAsset.MetroIblKtx)
		);
		var scene = factory.SceneBuilder.CreateScene(backdrop);
		var sunlight = factory.LightBuilder.CreateDirectionalLight(castsShadows: true);
		scene.Add(sunlight);

		var camera = factory.CameraBuilder.CreateCamera(new Location(0f, 0f, -3f), Direction.Forward, CameraPlaneConfiguration.CloseRange);
		var buffer = factory.RendererBuilder.CreateRenderOutputBuffer(textureDimensions: RenderDimensions);
		var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, buffer, new RendererCreationConfig {
			Quality = new(BuiltInQualityConfiguration.Ultra)
		});

		// Filament defers GPU allocation until work is submitted, so everything must be flushed
		// before a sample means anything
		void Flush() {
			_ = loop.IterateOnce();
			renderer.RenderAndWaitForGpu();
		}

		for (var i = 0; i < 30; ++i) Flush();
		_steadyBaselineMib = SampleVramMib();
		Console.WriteLine($"{"[STEADY BASELINE: engine + backdrop + renderer]",-46} {_steadyBaselineMib,7} MiB");
		Console.WriteLine();

		Console.WriteLine("--- PHASE 1: model loading (flushed after each) ---");
		var groups = new List<ResourceGroup>();
		var instanceGroups = new List<ModelInstanceGroup>();
		var loadStopwatch = Stopwatch.StartNew();
		var aborted = false;

		for (var i = 0; i < modelLimit; ++i) {
			var fileName = ProbeModels[i];
			var path = CommonTestAssets.FindAsset("models/" + fileName);

			ResourceGroup group;
			if (useSyncLoad) {
				group = factory.AssetLoader.LoadAll(
					path,
					new ModelCreationConfig { MeshConfig = new() { GenerateWireframeData = true } },
					new ModelReadConfig { MeshConfig = new() { LoadSkeletalAnimationDataIfPresent = false, CorrectFlippedOrientation = true }, HandleUriEscapedStrings = true }
				);
			}
			else {
				var operation = factory.AssetLoader.LoadAllAsync(
					path,
					new ModelCreationConfig { MeshConfig = new() { GenerateWireframeData = true } },
					new ModelReadConfig { MeshConfig = new() { LoadSkeletalAnimationDataIfPresent = false, CorrectFlippedOrientation = true }, HandleUriEscapedStrings = true }
				);
				if (!operation.WaitForCompletion(LoadTimeout)) {
					Console.WriteLine($"*** {fileName} timed out ***");
					continue;
				}
				group = operation.GetResultAndDisposeOperation();
			}

			groups.Add(group);
			if (!skipInstances) instanceGroups.Add(factory.ObjectBuilder.CreateModelInstances(group.Models));
			Flush();

			var used = Report($"[{i + 1,2}/{modelLimit}] {fileName}");
			if (!ShouldAbort(used)) continue;
			aborted = true;
			break;
		}

		loadStopwatch.Stop();
		Console.WriteLine();
		Console.WriteLine($"Loaded {groups.Count} models in {loadStopwatch.Elapsed.TotalSeconds:N1}s");
		var afterLoad = Report("[ALL MODELS LOADED]");
		Console.WriteLine();

		if (!aborted && renderFrames > 0) {
			Console.WriteLine("--- PHASE 2: steady-state rendering (nothing loading) ---");
			if (instanceGroups.Count > 0) scene.Add(instanceGroups[0]);
			var beforeRender = Report("[render loop start]");

			for (var frame = 1; frame <= renderFrames; ++frame) {
				Flush();
				if (frame % 100 == 0) Report($"[rendered {frame,5} frames]");
			}

			var afterRender = Report("[render loop done]");
			Console.WriteLine($"Growth across {renderFrames} frames with nothing loading: {afterRender - beforeRender:+#;-#;0} MiB");
			Console.WriteLine();
			if (instanceGroups.Count > 0) scene.Remove(instanceGroups[0]);
		}

		Console.WriteLine("--- PHASE 3: disposal (should return to steady baseline) ---");
		foreach (var instanceGroup in instanceGroups) instanceGroup.Dispose();
		foreach (var group in groups) group.Dispose();
		Flush();
		Report("[disposed, 1 flush]");
		for (var i = 0; i < 60; ++i) Flush();
		Report("[disposed, 61 flushes]");
		for (var i = 0; i < 240; ++i) Flush();
		Report("[disposed, 301 flushes]");
		Thread.Sleep(2000);
		for (var i = 0; i < 60; ++i) Flush();
		var afterDisposal = Report("[disposed, +2s and 361 flushes]");
		Console.WriteLine();
		Console.WriteLine($"Model load cost:      {afterLoad - _steadyBaselineMib,+6} MiB");
		Console.WriteLine($"Reclaimed on dispose: {afterLoad - afterDisposal,+6} MiB");
		Console.WriteLine($"NOT RECLAIMED:        {afterDisposal - _steadyBaselineMib,+6} MiB   <-- leak if materially above zero");

		renderer.Dispose();
		buffer.Dispose();
		camera.Dispose();
		scene.RemoveBackdrop();
		scene.Remove(sunlight);
		scene.Dispose();
		sunlight.Dispose();
		backdrop.Dispose();

		Console.WriteLine();
		Console.WriteLine("Probe complete.");
	}
}
