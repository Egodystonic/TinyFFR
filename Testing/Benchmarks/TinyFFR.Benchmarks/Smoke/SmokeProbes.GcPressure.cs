// Created on 2026-10-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Reflection;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Benchmarks.Harness;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;
using static Egodystonic.TinyFFR.Benchmarks.Harness.AllocationProbe;

namespace Egodystonic.TinyFFR.Benchmarks.Smoke;

static partial class SmokeProbes {
	const int DynamicBufferChurnCount = 500;
	const int LiveChunkBufferCount = 500;
	const int ChunkVertexCount = 8_192;
	const int ChunkTriangleCount = 4_096;
	const int TextWarmupCount = 200;
	const int TextWarmupReportStride = 20;
	const int SceneGrowthCount = 4_000;
	const int NoRenderUploadCount = 20_000;
	static readonly int[] InterleavedVertexCounts = [64, 96, 128, 200, 500];

	static readonly (string Name, Action Run)[] AllSections = [
		("ProceduralMeshes", SmokeSections.ProceduralMeshes),
		("MeshVertexMutation", SmokeSections.MeshVertexMutation),
		("DynamicVertexBuffers", SmokeSections.DynamicVertexBuffers),
		("MutableGridMeshes", SmokeSections.MutableGridMeshes),
		("ProceduralTextures", SmokeSections.ProceduralTextures),
		("TextureMipmappingAndCompression", SmokeSections.TextureMipmappingAndCompression),
		("BuiltInTextures", SmokeSections.BuiltInTextures),
		("LoadTextureFiles", SmokeSections.LoadTextureFiles),
		("LoadCombinedTextures", SmokeSections.LoadCombinedTextures),
		("StandardMaterials", SmokeSections.StandardMaterials),
		("SpecialMaterials", SmokeSections.SpecialMaterials),
		("MaterialEffects", SmokeSections.MaterialEffects),
		("Lights", SmokeSections.Lights),
		("ShadowsAndFog", SmokeSections.ShadowsAndFog),
		("Backdrops", SmokeSections.Backdrops),
		("ModelInstances", SmokeSections.ModelInstances),
		("ScenePrimitives", SmokeSections.ScenePrimitives),
		("SceneQueries", SmokeSections.SceneQueries),
		("CameraAndControllers", SmokeSections.CameraAndControllers),
		("RenderFrame", SmokeSections.RenderFrame),
		("FrameReadback", SmokeSections.FrameReadback),
		("BufferAsDynamicTexture", SmokeSections.BufferAsDynamicTexture),
		("RenderQualityAndCulling", SmokeSections.RenderQualityAndCulling),
		("ViewportSubAreas", SmokeSections.ViewportSubAreas),
		("PixelPicking", SmokeSections.PixelPicking),
		("Compositing", SmokeSections.Compositing),
		("LoopIteration", SmokeSections.LoopIteration),
		("Fonts", SmokeSections.Fonts),
		("TextInstances", SmokeSections.TextInstances),
		("Quads", SmokeSections.Quads),
		("CanvasScenes", SmokeSections.CanvasScenes),
		("LoadMeshFile", SmokeSections.LoadMeshFile),
		("LoadModelFile", SmokeSections.LoadModelFile),
		("SkeletalAnimation", SmokeSections.SkeletalAnimation),
		("AsyncLoading", SmokeSections.AsyncLoading),
		("BakeAssets", SmokeSections.BakeAssets),
		("LoadBakedAssets", SmokeSections.LoadBakedAssets),
		("ResourceGroups", SmokeSections.ResourceGroups),
		("ResourceNamingAndDirectory", SmokeSections.ResourceNamingAndDirectory),
		("AllocatorCollections", SmokeSections.AllocatorCollections),
		("DisplayDiscovery", SmokeSections.DisplayDiscovery)
	];

	static void ProbeAllSections() {
		foreach (var (name, run) in AllSections) {
			try {
				Measure(name, run, 1, BenchmarkEnvironment.PumpGpuResourceReclamation);
			}
			catch (Exception e) {
				Console.WriteLine($"  {name}: {e.GetType().Name}: {e.Message}");
			}
			BenchmarkEnvironment.PumpGpuResourceReclamation();
		}
	}

	static void ProbeResidualSections() {
		(string Name, Action Run)[] targets = [
			("Quads", SmokeSections.Quads),
			("SceneQueries", SmokeSections.SceneQueries),
			("Backdrops", SmokeSections.Backdrops),
			("Fonts", SmokeSections.Fonts),
			("LoadFont+Dispose", static () => Factory.AssetLoader.LoadFont(BenchmarkAssets.SansFont, "Probe Font").Dispose()),
			("LoadFont(built-in)+Dispose", static () => Factory.AssetLoader.LoadFont(BuiltInFont.Default, "Probe Font").Dispose()),
			("LoadPreprocessedBackdropTexture+Dispose", static () => Factory.AssetLoader.LoadPreprocessedBackdropTexture(BenchmarkAssets.MetroSkyKtx, BenchmarkAssets.MetroIblKtx, "Probe Backdrop").Dispose()),
			("SetBackdrop(BuiltIn Clouds)", static () => {
				using var scene = Factory.SceneBuilder.CreateScene(name: "Probe Backdrop Scene");
				scene.SetBackdrop(BuiltInSceneBackdrop.Clouds);
			})
		];
		foreach (var (name, run) in targets) {
			for (var i = 0; i < 20; ++i) {
				run();
				BenchmarkEnvironment.PumpGpuResourceReclamation();
			}
			Span<long> series = stackalloc long[10];
			for (var i = 0; i < series.Length; ++i) {
				var before = GC.GetAllocatedBytesForCurrentThread();
				run();
				series[i] = GC.GetAllocatedBytesForCurrentThread() - before;
				BenchmarkEnvironment.PumpGpuResourceReclamation();
			}
			Console.WriteLine($"  {name}: {series[0]} {series[1]} {series[2]} {series[3]} {series[4]} {series[5]} {series[6]} {series[7]} {series[8]} {series[9]}");
		}
	}

	static void ProbeSectionTimings() {
		(string Name, Action Run)[] targets = [
			("Quads", SmokeSections.Quads),
			("SceneQueries", SmokeSections.SceneQueries),
			("ModelInstances", SmokeSections.ModelInstances),
			("ResourceGroups", SmokeSections.ResourceGroups),
			("Lights", SmokeSections.Lights),
			("DynamicVertexBuffers", SmokeSections.DynamicVertexBuffers),
			("TextInstances", SmokeSections.TextInstances),
			("CanvasScenes", SmokeSections.CanvasScenes),
			("LoadModelFile", SmokeSections.LoadModelFile),
			("FrameReadback", SmokeSections.FrameReadback),
			("BufferAsDynamicTexture", SmokeSections.BufferAsDynamicTexture),
			("LoadTextureFiles", SmokeSections.LoadTextureFiles),
			("TextureMipmappingAndCompression", SmokeSections.TextureMipmappingAndCompression)
		];
		const int Rounds = 15;
		Span<double> totals = stackalloc double[targets.Length];
		Span<double> totalsWithPump = stackalloc double[targets.Length];
		for (var i = 0; i < targets.Length; ++i) {
			for (var w = 0; w < 3; ++w) {
				targets[i].Run();
				BenchmarkEnvironment.PumpGpuResourceReclamation();
			}
		}
		for (var round = 0; round < Rounds; ++round) {
			for (var i = 0; i < targets.Length; ++i) {
				var start = System.Diagnostics.Stopwatch.GetTimestamp();
				targets[i].Run();
				totals[i] += System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
				BenchmarkEnvironment.PumpGpuResourceReclamation();
				totalsWithPump[i] += System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
			}
		}
		for (var i = 0; i < targets.Length; ++i) Console.WriteLine($"  timing {targets[i].Name}: {totals[i] / Rounds:N2} ms ({totalsWithPump[i] / Rounds:N2} ms incl. reclamation frames)");
		Console.WriteLine($"        after timings: {GetOutstandingUploadBufferCount()}");
	}

	static void ProbeUploadBursts() {
		const int Rounds = 5;
		const int TinyTextureCount = 1_000;
		var nodeModelPath = Testing.CommonTestAssets.FindAsset("models/NodePerformanceTest.glb");
		var textures = new Texture[TinyTextureCount];
		var tinyPattern = TexturePattern.PlainFill(new ColorVect(0.5f, 0.25f, 0.75f));
		Console.WriteLine($"        tiny texture pattern dimensions: {tinyPattern.Dimensions}");

		(string Name, Action Run)[] cases = [
			($"{TinyTextureCount} live 1x1 textures", () => {
				for (var i = 0; i < textures.Length; ++i) textures[i] = Factory.TextureBuilder.CreateColorMap(tinyPattern, includeAlpha: false);
				for (var i = 0; i < textures.Length; ++i) textures[i].Dispose();
			}),
			("LoadBundledAsset(NodePerformanceTest.glb)", () => Factory.AssetLoader.LoadBundledAsset(nodeModelPath).Dispose()),
			("LoadBundledAsset(NodePerformanceTest.glb) + CreateModelInstances", () => {
				using var group = Factory.AssetLoader.LoadBundledAsset(nodeModelPath);
				Factory.ObjectBuilder.CreateModelInstances(group.Models).Dispose();
			})
		];
		foreach (var (name, run) in cases) {
			run();
			BenchmarkEnvironment.PumpGpuResourceReclamation();
			var total = 0.0;
			var totalWithFrame = 0.0;
			for (var round = 0; round < Rounds; ++round) {
				var start = System.Diagnostics.Stopwatch.GetTimestamp();
				run();
				total += System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
				BenchmarkEnvironment.PumpGpuResourceReclamation();
				totalWithFrame += System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
			}
			Console.WriteLine($"  burst {name}: {total / Rounds:N1} ms ({totalWithFrame / Rounds:N1} ms incl. reclamation frames); {GetOutstandingUploadBufferCount()}");
		}
	}

	static readonly string[] ModelLoadingTestFiles = [
		"BoxTextured.gltf", "BoxTextured.glb", "BoxTexturedSelfContained.gltf", "BoxTexturedNonPowerOfTwo.glb", "Box With Spaces.gltf",
		"NormalTangentMirrorTest.glb", "NegativeScaleTest.glb", "TextureCoordinateTest.glb", "CompareNormal.glb",
		"CompareRoughness.glb", "CompareMetallic.glb", "MetalRoughSpheres.glb", "CompareAmbientOcclusion.glb",
		"AnisotropyStrengthTest.glb", "AnisotropyDiscTest.glb", "EmissiveStrengthTest.glb",
		"TransmissionTest.glb", "CompareTransmission.glb", "TransmissionRoughnessTest.glb", "AttenuationTest.glb", "CompareIor.glb",
		"ClearCoatTest.glb", "BarramundiFish.glb", "Avocado.glb", "DamagedHelmet.glb",
		"showcase_ABeautifulGame.glb", "showcase_GlassHurricaneCandleHolder.glb", "showcase_MaterialsVariantsShoe.glb", "showcase_MosquitoInAmber.glb",
		"showcase_PotOfCoals.glb", "showcase_ToyCar.glb", "showcase_AnisotropyBarnLamp.glb", "showcase_CarConcept.glb",
		"showcase_ChronographWatch.glb", "showcase_CommercialRefrigerator.glb", "NodePerformanceTest.glb"
	];

	static void ProbeModelLoadingAllModels() {
		const int Passes = 3;
		const int FramesAfterLoad = 10;
		var creationConfig = new ModelCreationConfig { MeshConfig = new() { WireframeGenerationMode = WireframeGenerationMode.EnabledWithEdgeDeduplicationAndFaceClearing } };
		var readConfig = new ModelReadConfig { MeshConfig = new() { LoadSkeletalAnimationDataIfPresent = false, CorrectFlippedOrientation = true }, HandleUriEscapedStrings = true };
		using var scene = Factory.SceneBuilder.CreateScene(name: "Probe All Models Scene");
		using var camera = Factory.CameraBuilder.CreateCamera(new Location(0f, 0f, -1f), name: "Probe All Models Camera");
		using var renderer = BenchmarkEnvironment.RenderTarget.CreateRenderer(scene, camera, new RendererCreationConfig { Quality = new RenderQualityConfig(BuiltInQualityConfiguration.Ultra) });
		renderer.RenderAndWaitForGpu();

		ModelBundle? group = null;
		ModelInstanceGroup? instances = null;
		for (var pass = 0; pass < Passes; ++pass) {
			foreach (var file in ModelLoadingTestFiles) {
				var path = Testing.CommonTestAssets.FindAsset("models/" + file);
				var start = System.Diagnostics.Stopwatch.GetTimestamp();
				if (instances is { } i) {
					scene.Remove(i);
					i.Dispose();
				}
				group?.Dispose();
				var disposeMs = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;

				start = System.Diagnostics.Stopwatch.GetTimestamp();
				group = Factory.AssetLoader.LoadBundledAsset(path, creationConfig, readConfig);
				var loadMs = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;

				start = System.Diagnostics.Stopwatch.GetTimestamp();
				instances = Factory.ObjectBuilder.CreateModelInstances(group.Value.Models);
				scene.Add(instances.Value);
				var instanceMs = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;

				start = System.Diagnostics.Stopwatch.GetTimestamp();
				renderer.RenderAndWaitForGpu();
				var firstFrameMs = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;

				start = System.Diagnostics.Stopwatch.GetTimestamp();
				for (var f = 0; f < FramesAfterLoad; ++f) renderer.Render();
				renderer.WaitForGpu();
				var laterFramesMs = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds / FramesAfterLoad;

				Console.WriteLine($"MODELROW|{pass}|{file}|{disposeMs:F1}|{loadMs:F1}|{instanceMs:F1}|{firstFrameMs:F1}|{laterFramesMs:F2}");
			}
		}

		if (instances is { } last) {
			scene.Remove(last);
			last.Dispose();
		}
		group?.Dispose();
		renderer.RenderAndWaitForGpu();
	}

	static void ProbeModelLoadingPath() {
		const int Rounds = 4;
		const int FramesBetweenLoads = 10;
		var nodeModelPath = Testing.CommonTestAssets.FindAsset("models/NodePerformanceTest.glb");
		var creationConfig = new ModelCreationConfig { MeshConfig = new() { WireframeGenerationMode = WireframeGenerationMode.EnabledWithEdgeDeduplicationAndFaceClearing } };
		var readConfig = new ModelReadConfig { MeshConfig = new() { LoadSkeletalAnimationDataIfPresent = false, CorrectFlippedOrientation = true }, HandleUriEscapedStrings = true };
		using var scene = Factory.SceneBuilder.CreateScene(name: "Probe Model Path Scene");
		using var camera = Factory.CameraBuilder.CreateCamera(new Location(0f, 0f, -1f), name: "Probe Model Path Camera");
		using var renderer = BenchmarkEnvironment.RenderTarget.CreateRenderer(scene, camera, new RendererCreationConfig { Quality = new RenderQualityConfig(BuiltInQualityConfiguration.Ultra) });

		ModelBundle? group = null;
		ModelInstanceGroup? instances = null;
		var disposeTotal = 0.0;
		var loadTotal = 0.0;
		var instanceTotal = 0.0;
		var firstFramesTotal = 0.0;
		for (var round = 0; round <= Rounds; ++round) {
			for (var f = 0; f < FramesBetweenLoads; ++f) renderer.Render();

			var start = System.Diagnostics.Stopwatch.GetTimestamp();
			if (instances is { } i) {
				scene.Remove(i);
				i.Dispose();
			}
			group?.Dispose();
			var disposeMs = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;

			start = System.Diagnostics.Stopwatch.GetTimestamp();
			group = Factory.AssetLoader.LoadBundledAsset(nodeModelPath, creationConfig, readConfig);
			var loadMs = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;

			start = System.Diagnostics.Stopwatch.GetTimestamp();
			instances = Factory.ObjectBuilder.CreateModelInstances(group.Value.Models);
			scene.Add(instances.Value);
			var instanceMs = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;

			start = System.Diagnostics.Stopwatch.GetTimestamp();
			renderer.RenderAndWaitForGpu();
			var firstFrameMs = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;

			Console.WriteLine($"  path round {round}: dispose {disposeMs:N0} ms, load {loadMs:N0} ms, instances+add {instanceMs:N0} ms, first frame {firstFrameMs:N0} ms; {GetOutstandingUploadBufferCount()}");
			if (round == 0) continue;
			disposeTotal += disposeMs;
			loadTotal += loadMs;
			instanceTotal += instanceMs;
			firstFramesTotal += firstFrameMs;
		}
		Console.WriteLine($"  path mean (rounds 1-{Rounds}): dispose {disposeTotal / Rounds:N0} ms, load {loadTotal / Rounds:N0} ms, instances+add {instanceTotal / Rounds:N0} ms, first frame {firstFramesTotal / Rounds:N0} ms, total {(disposeTotal + loadTotal + instanceTotal + firstFramesTotal) / Rounds:N0} ms");

		if (instances is { } last) {
			scene.Remove(last);
			last.Dispose();
		}
		group?.Dispose();
		renderer.RenderAndWaitForGpu();
	}

	static void ProbeDynamicBuffers() {
		var meshBuilder = Factory.MeshBuilder;
		var localMeshBuilder = (LocalMeshBuilder) meshBuilder;

		MeasureCold($"create+dispose 64v same size x{DynamicBufferChurnCount}", () => {
			for (var i = 0; i < DynamicBufferChurnCount; ++i) meshBuilder.CreateDynamicVertexBuffer(64, 64).Dispose();
		}, DynamicBufferChurnCount);
		Measure($"create+dispose 64v same size x{DynamicBufferChurnCount}", () => {
			for (var i = 0; i < DynamicBufferChurnCount; ++i) meshBuilder.CreateDynamicVertexBuffer(64, 64).Dispose();
		}, DynamicBufferChurnCount);

		Measure($"create+dispose interleaved sizes x{DynamicBufferChurnCount}", () => {
			for (var i = 0; i < DynamicBufferChurnCount; ++i) {
				var size = InterleavedVertexCounts[i % InterleavedVertexCounts.Length];
				meshBuilder.CreateDynamicVertexBuffer(size, size).Dispose();
			}
		}, DynamicBufferChurnCount);
		Measure($"create+dispose interleaved sizes, ImGui (no mirror) x{DynamicBufferChurnCount}", () => {
			for (var i = 0; i < DynamicBufferChurnCount; ++i) {
				var size = InterleavedVertexCounts[i % InterleavedVertexCounts.Length];
				localMeshBuilder.CreateImGuiDynamicVertexBuffer(size, size * 3).Dispose();
			}
		}, DynamicBufferChurnCount);

		var live = new DynamicVertexBuffer[LiveChunkBufferCount];
		MeasureCold($"hold {LiveChunkBufferCount} live {ChunkVertexCount}v buffers", () => {
			for (var i = 0; i < live.Length; ++i) live[i] = meshBuilder.CreateDynamicVertexBuffer(ChunkVertexCount, ChunkTriangleCount);
			for (var i = 0; i < live.Length; ++i) live[i].Dispose();
		}, LiveChunkBufferCount);
		Measure($"hold {LiveChunkBufferCount} live {ChunkVertexCount}v buffers", () => {
			for (var i = 0; i < live.Length; ++i) live[i] = meshBuilder.CreateDynamicVertexBuffer(ChunkVertexCount, ChunkTriangleCount);
			for (var i = 0; i < live.Length; ++i) live[i].Dispose();
		}, LiveChunkBufferCount);
		BenchmarkEnvironment.PumpGpuResourceReclamation();

		using var resized = meshBuilder.CreateDynamicVertexBuffer(256, 256);
		Measure($"resize vertices+triangles up/down x{DynamicBufferChurnCount}", () => {
			for (var i = 0; i < DynamicBufferChurnCount; ++i) {
				var size = (i & 1) == 0 ? 1_024 : 256;
				resized.ResizeVertexBuffer(size);
				resized.ResizeTriangleBuffer(size);
			}
		}, DynamicBufferChurnCount);
		BenchmarkEnvironment.PumpGpuResourceReclamation();
	}

	static void ProbeTextWarmup() {
		using var font = Factory.AssetLoader.LoadFont(BenchmarkAssets.SansFont, "Probe Warmup Font");
		using var pen = font.CreatePen(ColorVect.WhiteOpaque);
		using var canvas = Factory.SceneBuilder.CreateCanvasScene("Probe Warmup Canvas");
		using var text = canvas.Add("", pen);
		text.SetPlacementFraction(Orientation2D.Down, (0f, 0.05f), 0.15f);
		using var renderer = BenchmarkEnvironment.RenderTarget.CreateRenderer(canvas);
		renderer.RenderAndWaitForGpu();

		var strideBytes = 0L;
		for (var i = 0; i < TextWarmupCount; ++i) {
			var before = GC.GetAllocatedBytesForCurrentThread();
			SetDistinctText(text, i);
			renderer.Render();
			strideBytes += GC.GetAllocatedBytesForCurrentThread() - before;
			if ((i + 1) % TextWarmupReportStride != 0) continue;
			Console.WriteLine($"  {strideBytes,12:N0} B ({strideBytes / (double) TextWarmupReportStride,9:N1} B/op)  SetText+Render calls {i + 2 - TextWarmupReportStride}-{i + 1} [cold]");
			strideBytes = 0L;
		}
		Measure($"SetText+Render {TextWarmupCount} distinct strings", () => {
			for (var i = 0; i < TextWarmupCount; ++i) {
				SetDistinctText(text, i);
				renderer.Render();
			}
		}, TextWarmupCount);
		Measure($"Render only x{TextWarmupCount}", () => {
			for (var i = 0; i < TextWarmupCount; ++i) renderer.Render();
		}, TextWarmupCount);
		Measure($"SetText same string (cache hit) + Render x{TextWarmupCount}", () => {
			for (var i = 0; i < TextWarmupCount; ++i) {
				SetDistinctText(text, (i & 1) * 7);
				renderer.Render();
			}
		}, TextWarmupCount);
		Measure($"SetText 32 distinct (all cache hits) + Render x{TextWarmupCount}", () => {
			for (var i = 0; i < TextWarmupCount; ++i) {
				SetDistinctText(text, i & 31);
				renderer.Render();
			}
		}, TextWarmupCount);
		Measure($"font.MeasureString {TextWarmupCount} distinct (cache misses)", () => {
			Span<char> buffer = stackalloc char[32];
			for (var i = 0; i < TextWarmupCount; ++i) {
				"Probe text ".CopyTo(buffer);
				i.TryFormat(buffer[11..], out var written);
				_ = font.MeasureString(buffer[..(11 + written)]);
			}
		}, TextWarmupCount);
		Measure($"font.CreateString+Dispose {TextWarmupCount} distinct + Render", () => {
			Span<char> buffer = stackalloc char[32];
			for (var i = 0; i < TextWarmupCount; ++i) {
				"Probe text ".CopyTo(buffer);
				i.TryFormat(buffer[11..], out var written);
				font.CreateString(buffer[..(11 + written)]).Dispose();
				renderer.Render();
			}
		}, TextWarmupCount);
		renderer.WaitForGpu();
	}

	static void ProbeRenderVariants() {
		using var emptyCanvas = Factory.SceneBuilder.CreateCanvasScene("Probe Empty Canvas");
		using (var canvasRenderer = BenchmarkEnvironment.RenderTarget.CreateRenderer(emptyCanvas)) {
			canvasRenderer.RenderAndWaitForGpu();
			Measure($"empty canvas Render x{TextWarmupCount}", () => {
				for (var i = 0; i < TextWarmupCount; ++i) canvasRenderer.Render();
			}, TextWarmupCount);
			Measure($"empty canvas RenderAndWaitForGpu x{TextWarmupCount}", () => {
				for (var i = 0; i < TextWarmupCount; ++i) canvasRenderer.RenderAndWaitForGpu();
			}, TextWarmupCount);
		}
		using var scene = Factory.SceneBuilder.CreateScene(name: "Probe Empty Scene");
		using var camera = Factory.CameraBuilder.CreateCamera(Location.Origin, name: "Probe Camera");
		using (var sceneRenderer = BenchmarkEnvironment.RenderTarget.CreateRenderer(scene, camera)) {
			sceneRenderer.RenderAndWaitForGpu();
			Measure($"empty 3D scene Render x{TextWarmupCount}", () => {
				for (var i = 0; i < TextWarmupCount; ++i) sceneRenderer.Render();
			}, TextWarmupCount);
			Measure($"empty 3D scene RenderAndWaitForGpu x{TextWarmupCount}", () => {
				for (var i = 0; i < TextWarmupCount; ++i) sceneRenderer.RenderAndWaitForGpu();
			}, TextWarmupCount);
		}
		using (var canvasQualityRenderer = BenchmarkEnvironment.RenderTarget.CreateRenderer(scene, camera, new RendererCreationConfig { Quality = new RenderQualityConfig(BuiltInQualityConfiguration.Canvas) })) {
			canvasQualityRenderer.RenderAndWaitForGpu();
			Measure($"empty 3D scene with Canvas quality Render x{TextWarmupCount}", () => {
				for (var i = 0; i < TextWarmupCount; ++i) canvasQualityRenderer.Render();
			}, TextWarmupCount);
		}
		using (var canvasDefaultQualityRenderer = BenchmarkEnvironment.RenderTarget.CreateRenderer(emptyCanvas, new RendererCreationConfig { Quality = new RenderQualityConfig(BuiltInQualityConfiguration.VeryLow) })) {
			canvasDefaultQualityRenderer.RenderAndWaitForGpu();
			Measure($"empty canvas with VeryLow quality Render x{TextWarmupCount}", () => {
				for (var i = 0; i < TextWarmupCount; ++i) canvasDefaultQualityRenderer.Render();
			}, TextWarmupCount);
		}
	}

	sealed class AllocationTypeListener : System.Diagnostics.Tracing.EventListener {
		public readonly Dictionary<string, int> Counts = new();
		protected override void OnEventSourceCreated(System.Diagnostics.Tracing.EventSource source) {
			if (source.Name == "Microsoft-Windows-DotNETRuntime") EnableEvents(source, System.Diagnostics.Tracing.EventLevel.Verbose, (System.Diagnostics.Tracing.EventKeywords) 0x1);
		}
		protected override void OnEventWritten(System.Diagnostics.Tracing.EventWrittenEventArgs e) {
			if (e.EventName == null || !e.EventName.StartsWith("GCAllocationTick")) return;
			var idx = e.PayloadNames!.IndexOf("TypeName");
			var sizeIdx = e.PayloadNames!.IndexOf("ObjectSize");
			var name = (idx >= 0 ? e.Payload![idx]?.ToString() ?? "?" : "?") + (sizeIdx >= 0 ? $" ({e.Payload![sizeIdx]} B)" : "");
			lock (Counts) Counts[name] = Counts.GetValueOrDefault(name) + 1;
		}
	}

	static void ProbeAllocationTypes() {
		using var canvas = Factory.SceneBuilder.CreateCanvasScene("Probe Alloc Type Canvas");
		using var renderer = BenchmarkEnvironment.RenderTarget.CreateRenderer(canvas);
		renderer.RenderAndWaitForGpu();
		using var listener = new AllocationTypeListener();
		var before = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 0; i < 50_000; ++i) renderer.Render();
		Console.WriteLine($"  {GC.GetAllocatedBytesForCurrentThread() - before:N0} B over 50,000 canvas renders");
		renderer.WaitForGpu();
		Thread.Sleep(500);
		lock (listener.Counts) foreach (var kvp in listener.Counts.OrderByDescending(k => k.Value)) Console.WriteLine($"  tick {kvp.Value,5}  {kvp.Key}");

		using var mesh = Factory.MeshBuilder.CreateCuboid(Cuboid.UnitCube, name: "Probe Alloc Type Mesh");
		using var material = Factory.MaterialBuilder.CreateTestMaterial();
		var instances = new ModelInstance[SceneGrowthCount];
		for (var i = 0; i < instances.Length; ++i) instances[i] = Factory.ObjectBuilder.CreateModelInstance(mesh, material);
		using var scene = Factory.SceneBuilder.CreateScene(name: "Probe Alloc Type Scene");
		using var sceneListener = new AllocationTypeListener();
		before = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 0; i < instances.Length; ++i) scene.Add(instances[i]);
		Console.WriteLine($"  {GC.GetAllocatedBytesForCurrentThread() - before:N0} B over {SceneGrowthCount} cold scene.Add");
		Thread.Sleep(500);
		lock (sceneListener.Counts) foreach (var kvp in sceneListener.Counts.OrderByDescending(k => k.Value)) Console.WriteLine($"  tick {kvp.Value,5}  {kvp.Key}");
		scene.RemoveAll();
		for (var i = 0; i < instances.Length; ++i) instances[i].Dispose();

		for (var i = 0; i < 20; ++i) SmokeSections.Quads();
		using var quadsListener = new AllocationTypeListener();
		before = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 0; i < 150; ++i) SmokeSections.Quads();
		Console.WriteLine($"  {GC.GetAllocatedBytesForCurrentThread() - before:N0} B over 150 warm Quads sections");
		Thread.Sleep(500);
		lock (quadsListener.Counts) foreach (var kvp in quadsListener.Counts.OrderByDescending(k => k.Value)) Console.WriteLine($"  tick {kvp.Value,5}  {kvp.Key}");

		for (var i = 0; i < 20; ++i) SmokeSections.SceneQueries();
		using var queriesListener = new AllocationTypeListener();
		before = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 0; i < 400; ++i) SmokeSections.SceneQueries();
		Console.WriteLine($"  {GC.GetAllocatedBytesForCurrentThread() - before:N0} B over 400 warm SceneQueries sections");
		Thread.Sleep(500);
		lock (queriesListener.Counts) foreach (var kvp in queriesListener.Counts.OrderByDescending(k => k.Value)) Console.WriteLine($"  tick {kvp.Value,5}  {kvp.Key}");
	}

	static void SetDistinctText(CanvasText text, int index) {
		Span<char> buffer = stackalloc char[32];
		"Probe text ".CopyTo(buffer);
		index.TryFormat(buffer[11..], out var written);
		text.SetText(buffer[..(11 + written)], TextJustification.Left);
	}

	static void ProbeSceneGrowth() {
		using var mesh = Factory.MeshBuilder.CreateCuboid(Cuboid.UnitCube, name: "Probe Growth Mesh");
		using var material = Factory.MaterialBuilder.CreateTestMaterial();
		var instances = new ModelInstance[SceneGrowthCount];
		for (var i = 0; i < instances.Length; ++i) instances[i] = Factory.ObjectBuilder.CreateModelInstance(mesh, material);

		using (var scene = Factory.SceneBuilder.CreateScene(name: "Probe Growth Scene")) {
			MeasureCold($"scene.Add to new high-water mark x{SceneGrowthCount}", () => {
				for (var i = 0; i < instances.Length; ++i) scene.Add(instances[i]);
			}, SceneGrowthCount);
			scene.RemoveAll();
			Measure($"scene.Add to same size x{SceneGrowthCount}", () => {
				for (var i = 0; i < instances.Length; ++i) scene.Add(instances[i]);
				scene.RemoveAll();
			}, SceneGrowthCount);
		}
		using (var freshScene = Factory.SceneBuilder.CreateScene(name: "Probe Growth Scene 2")) {
			MeasureCold($"scene.Add in second fresh scene x{SceneGrowthCount}", () => {
				for (var i = 0; i < instances.Length; ++i) freshScene.Add(instances[i]);
				freshScene.RemoveAll();
			}, SceneGrowthCount);
		}

		for (var i = 0; i < instances.Length; ++i) instances[i].Dispose();
	}

	static void ProbeNoRenderUploads() {
		using var font = Factory.AssetLoader.LoadFont(BenchmarkAssets.SansFont, "Probe No-Render Font");
		BenchmarkEnvironment.PumpGpuResourceReclamation();
		Console.WriteLine($"        outstanding upload buffers at start: {GetOutstandingUploadBufferCount()}");
		MeasureCold($"CreateString+Dispose with no render x{NoRenderUploadCount}", () => {
			for (var i = 0; i < NoRenderUploadCount; ++i) font.CreateString("Probe no-render text").Dispose();
		}, NoRenderUploadCount);
		Console.WriteLine($"        outstanding upload buffers after: {GetOutstandingUploadBufferCount()}");
		Measure($"CreateString+Dispose with no render x{NoRenderUploadCount}", () => {
			for (var i = 0; i < NoRenderUploadCount; ++i) font.CreateString("Probe no-render text").Dispose();
		}, NoRenderUploadCount);
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		for (var i = 0; i < NoRenderUploadCount; ++i) font.CreateString("Probe no-render text").Dispose();
		Console.WriteLine($"        {stopwatch.Elapsed.TotalMicroseconds / NoRenderUploadCount:N2} us per CreateString+Dispose with no render; {GetOutstandingUploadBufferCount()}");
		BenchmarkEnvironment.PumpGpuResourceReclamation();
		Console.WriteLine($"        outstanding upload buffers after a rendered frame: {GetOutstandingUploadBufferCount()}");
	}

	static string GetOutstandingUploadBufferCount() {
		const BindingFlags Instance = BindingFlags.NonPublic | BindingFlags.Instance;
		var map = typeof(LocalNativeUtils).GetField("_activeTemporaryBuffers", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
		var outstanding = (int) map.GetType().GetProperty("Count")!.GetValue(map)!;
		var result = $"{outstanding} buffers";
		foreach (var fieldName in new[] { "_gpuHoldingBufferPool", "_gpuHoldingSlabPool" }) {
			var pool = Factory.GetType().GetField(fieldName, Instance)?.GetValue(Factory);
			if (pool == null) continue;
			var poolType = pool.GetType();
			if (poolType.GetField("_numAllocatedSpaces", Instance) is { } spacesField) {
				var spaces = (int) spacesField.GetValue(pool)!;
				var spaceBytes = (int) poolType.GetProperty("MaxBufferSizeBytes")!.GetValue(pool)!;
				result += $"; block pool {spaces} spaces ({spaces * (long) spaceBytes / (1024 * 1024)} MiB reserved)";
			}
			else {
				var liveSlabs = (int) poolType.GetField("_slabRecordCount", Instance)!.GetValue(pool)! - (int) poolType.GetField("_freeSlabRecordCount", Instance)!.GetValue(pool)!;
				var outstandingBytes = (long) poolType.GetProperty("OutstandingBytes")!.GetValue(pool)!;
				result += $"; slab pool {liveSlabs} live slabs, {outstandingBytes / 1024:N0} KiB outstanding";
			}
		}
		return result + $"; RSS {ReadResidentSetMiB():N0} MiB";
	}

	static long ReadResidentSetMiB() {
		foreach (var line in File.ReadLines("/proc/self/status")) {
			if (!line.StartsWith("VmRSS:")) continue;
			return Int64.Parse(line.AsSpan(6).Trim().TrimEnd("kB").Trim()) / 1024;
		}
		return -1;
	}

	static long _largeReadbackTexelCount;
	static void CountLargeReadbackTexels(XYPair<int> dimensions, ReadOnlySpan<TexelRgba32> texels) => _largeReadbackTexelCount += texels.Length;

	static unsafe void ProbeLargeUploadsPerFrame() {
		const int WarmupFrames = 20;
		const int MeasuredFrames = 120;
		using var mesh = Factory.MeshBuilder.CreateCuboid(Cuboid.UnitCube, name: "Probe Large Upload Mesh");
		using var material = Factory.MaterialBuilder.CreateTestMaterial();
		using var instance = Factory.ObjectBuilder.CreateModelInstance(mesh, material, Location.Origin + Direction.Forward * 2.2f, name: "Probe Large Upload Instance");
		using var scene = Factory.SceneBuilder.CreateScene(name: "Probe Large Upload Scene");
		using var camera = Factory.CameraBuilder.CreateCamera(Location.Origin, name: "Probe Large Upload Camera");
		scene.Add(instance);

		foreach (var dimensions in new XYPair<int>[] { (1920, 1080), (3840, 2160) }) {
			using var buffer = Factory.RendererBuilder.CreateRenderOutputBuffer(dimensions, "Probe Readback Buffer");
			using var renderer = Factory.RendererBuilder.CreateRenderer(scene, camera, buffer, "Probe Readback Renderer");
			buffer.StartReadingFrames(&CountLargeReadbackTexels);
			for (var f = 0; f < WarmupFrames; ++f) renderer.Render();
			renderer.WaitForGpu();
			var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
			var start = System.Diagnostics.Stopwatch.GetTimestamp();
			for (var f = 0; f < MeasuredFrames; ++f) renderer.Render();
			renderer.WaitForGpu();
			var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
			var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
			buffer.StopReadingFrames(cancelQueuedFrames: false);
			renderer.WaitForGpu();
			Console.WriteLine($"  large readback {dimensions.X}x{dimensions.Y}: {elapsed / MeasuredFrames:N2} ms/frame, {allocated / (double) MeasuredFrames:N1} B/frame; {GetOutstandingUploadBufferCount()}");
		}

		using var mainRenderer = BenchmarkEnvironment.RenderTarget.CreateRenderer(scene, camera);
		foreach (var side in new[] { 1024, 2048 }) {
			var texels = new TexelRgba32[side * side];
			using var texture = Factory.TextureBuilder.CreateTexture(
				texels.AsSpan(),
				new TextureGenerationConfig { Dimensions = (side, side) },
				new TextureCreationConfig { RenderingConfig = new(true, false, Quality.Standard), AllowsDynamicWrites = true, DataType = TextureDataType.ColorSrgb }
			);
			using var textureMaterial = Factory.MaterialBuilder.CreateStandardMaterial(texture, name: "Probe Dynamic Texture Material");
			instance.SetMaterial(textureMaterial);
			for (var f = 0; f < WarmupFrames; ++f) {
				texels[f % texels.Length] = new TexelRgba32((byte) f, 0, 0, 255);
				texture.OverwriteTexels<TexelRgba32>(texels);
				mainRenderer.Render();
			}
			mainRenderer.WaitForGpu();
			var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
			var start = System.Diagnostics.Stopwatch.GetTimestamp();
			for (var f = 0; f < MeasuredFrames; ++f) {
				texels[f % texels.Length] = new TexelRgba32((byte) f, 0, 0, 255);
				texture.OverwriteTexels<TexelRgba32>(texels);
				mainRenderer.Render();
			}
			mainRenderer.WaitForGpu();
			var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
			var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
			instance.SetMaterial(material);
			mainRenderer.WaitForGpu();
			Console.WriteLine($"  dynamic texture {side}x{side} rewrite: {elapsed / MeasuredFrames:N2} ms/frame, {allocated / (double) MeasuredFrames:N1} B/frame; {GetOutstandingUploadBufferCount()}");
		}
	}

	static void ProbeModelLoadingWithoutNodePerf() {
		const int Passes = 2;
		var creationConfig = new ModelCreationConfig { MeshConfig = new() { WireframeGenerationMode = WireframeGenerationMode.EnabledWithEdgeDeduplicationAndFaceClearing } };
		var readConfig = new ModelReadConfig { MeshConfig = new() { LoadSkeletalAnimationDataIfPresent = false, CorrectFlippedOrientation = true }, HandleUriEscapedStrings = true };
		using var scene = Factory.SceneBuilder.CreateScene(name: "Probe No NodePerf Scene");
		using var camera = Factory.CameraBuilder.CreateCamera(new Location(0f, 0f, -1f), name: "Probe No NodePerf Camera");
		using var renderer = BenchmarkEnvironment.RenderTarget.CreateRenderer(scene, camera, new RendererCreationConfig { Quality = new RenderQualityConfig(BuiltInQualityConfiguration.Ultra) });
		ModelBundle? group = null;
		ModelInstanceGroup? instances = null;
		var total = 0.0;
		for (var pass = 0; pass < Passes; ++pass) {
			foreach (var file in ModelLoadingTestFiles) {
				if (file == "NodePerformanceTest.glb") continue;
				var path = Testing.CommonTestAssets.FindAsset("models/" + file);
				var start = System.Diagnostics.Stopwatch.GetTimestamp();
				if (instances is { } i) {
					scene.Remove(i);
					i.Dispose();
				}
				group?.Dispose();
				group = Factory.AssetLoader.LoadBundledAsset(path, creationConfig, readConfig);
				instances = Factory.ObjectBuilder.CreateModelInstances(group.Value.Models);
				scene.Add(instances.Value);
				renderer.RenderAndWaitForGpu();
				if (pass > 0) total += System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
				for (var f = 0; f < 5; ++f) renderer.Render();
			}
		}
		Console.WriteLine($"  models (no NodePerf) warm pass: {total:N0} ms total; {GetOutstandingUploadBufferCount()}");
		if (instances is { } last) {
			scene.Remove(last);
			last.Dispose();
		}
		group?.Dispose();
		renderer.RenderAndWaitForGpu();
		Console.WriteLine($"        after unloading: {GetOutstandingUploadBufferCount()}");
		for (var f = 0; f < 300; ++f) renderer.Render();
		renderer.WaitForGpu();
		Console.WriteLine($"        after 300 idle frames: {GetOutstandingUploadBufferCount()}");
	}
}
