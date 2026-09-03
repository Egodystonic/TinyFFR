using System.IO;
using System.Linq;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.Threading;
using Egodystonic.TinyFFR.World;
using static Egodystonic.TinyFFR.Assets.Materials.TextureCombinationSourceTexture;
using static Egodystonic.TinyFFR.ColorChannel;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
unsafe class LocalAsyncLoadingTest {
	static readonly TimeSpan AsyncTimeout = TimeSpan.FromSeconds(60d);

	static string CrateColorFile => CommonTestAssets.FindAsset("ELCrate.png");
	static string CrateNormalFile => CommonTestAssets.FindAsset("ELCrate_Normal.png");
	static string CrateOrmFile => CommonTestAssets.FindAsset("ELCrate_Specular.png");
	static string AlphaLogoFile => CommonTestAssets.FindAsset("egdLogo.png");
	static string CrateFile => CommonTestAssets.FindAsset("ELCrate.obj");
	static string RiggedFile => CommonTestAssets.FindAsset("models/RiggedSimple.glb");
	static string CesiumManFile => CommonTestAssets.FindAsset("models/CesiumMan.glb");
	static string HelmetFile => CommonTestAssets.FindAsset("models/DamagedHelmet.glb");
	static string CarConceptFile => CommonTestAssets.FindAsset("models/showcase_CarConcept.glb");
	
	static readonly (string Filename, float ScalingFactor)[] InteractiveTestFiles = {
		("BoxTextured.gltf", 1f),
		("BoxTextured.glb", 1f),
		("BoxTexturedSelfContained.gltf", 1f),
		("BoxTexturedNonPowerOfTwo.glb", 1f),
		("Box With Spaces.gltf", 1f),
		("NormalTangentMirrorTest.glb", 1f),
		("NegativeScaleTest.glb", 1f),
		("TextureCoordinateTest.glb", 1f),
		("CompareNormal.glb", 1f),
		("CompareRoughness.glb", 1f),
		("CompareMetallic.glb", 1f),
		("MetalRoughSpheres.glb", 1f),
		("CompareAmbientOcclusion.glb", 1f),
		("AnisotropyStrengthTest.glb", 1f),
		("AnisotropyDiscTest.glb", 1f),
		("EmissiveStrengthTest.glb", 1f),
		("TransmissionTest.glb", 1f),
		("CompareTransmission.glb", 1f),
		("TransmissionRoughnessTest.glb", 1f),
		("AttenuationTest.glb", 1f),
		("CompareIor.glb", 1f),
		("ClearCoatTest.glb", 1f),
		("BarramundiFish.glb", 1f),
		("Avocado.glb", 1f),
		("DamagedHelmet.glb", 1f),
		("showcase_ABeautifulGame.glb", 1f),
		("showcase_GlassHurricaneCandleHolder.glb", 1f),
		("showcase_MaterialsVariantsShoe.glb", 1f),
		("showcase_MosquitoInAmber.glb", 1f),
		("showcase_PotOfCoals.glb", 1f),
		("showcase_ToyCar.glb", 1f),
		("showcase_AnisotropyBarnLamp.glb", 1f),
		("showcase_CarConcept.glb", 1f),
		("showcase_ChronographWatch.glb", 1f),
		("showcase_CommercialRefrigerator.glb", 1f),
		("NodePerformanceTest.glb", 1f),
		("SimpleSkin.gltf", 1f),
		("RiggedSimple.glb", 0.25f),
		("RiggedFigure.glb", 1f),
		("CesiumMan.glb", 1f),
		("BrainStem.glb", 0.8f),
		("Fox.glb", 0.01f),
		("Mixamo.fbx", 0.01f),
	};

	static T AwaitLoad<T>(TinyFfrAsyncOperation<T> op) {
		Assert.IsTrue(op.WaitForCompletion(AsyncTimeout), "Async load timed out.");
		return op.GetResultAndDisposeOperation();
	}

	static void AssertTexturesEquivalent(Texture expected, Texture actual, string context) {
		Assert.AreEqual(expected.Dimensions, actual.Dimensions, $"{context}: dimensions differ.");
		Assert.AreEqual(expected.TexelType, actual.TexelType, $"{context}: texel type differs.");
		Assert.AreEqual(expected.ContainsMipMaps, actual.ContainsMipMaps, $"{context}: mip-map presence differs.");
		Assert.AreEqual(expected.AllowsDynamicWrites, actual.AllowsDynamicWrites, $"{context}: dynamic-write flag differs.");
		Assert.AreEqual(expected.RenderingConfig, actual.RenderingConfig, $"{context}: rendering config differs.");
		Console.WriteLine($"  {context}: {actual.Dimensions} {actual.TexelType} mips={actual.ContainsMipMaps} aniso={actual.RenderingConfig.AnisotropyLevel}");
	}
	
	[Test]
	public void Execute() {
		RunAllAutomatedTests();
		RunInteractiveTest();
	}

	static string FormatProgressBar(int completed, int total) {
		// const int BarWidth = 24;
		// var filled = total <= 0 ? 0 : (int) MathF.Round(BarWidth * (completed / (float) total));
		// return "[" + new String('#', filled) + new String('-', BarWidth - filled) + "]";
		return "[" + PercentageUtils.ConvertFractionToPercentageString(completed / (float) total, "N0") + "]";
	}

	void RunInteractiveTest() {
		using var factory = new LocalTinyFfrFactory(factoryConfig: new LocalTinyFfrFactoryConfig { ThreadingConfig = new ThreadingConfig { MaxShutdownWaitTime = TimeSpan.FromSeconds(2d) }});
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Async Model Viewer | SPACE = next loaded model | A = cycle anim | S = play/stop anim | L = camera light | ESC = quit");
		using var camera = factory.CameraBuilder.CreateCamera(new Location(0f, 0f, -1f), cameraRange: CameraPlaneConfiguration.CloseRange);
		using var cameraController = camera.CreateController<InspectorCameraController>();
		using var light = factory.LightBuilder.CreateSpotLight(position: camera.Position, coneDirection: camera.ViewDirection, highQuality: true, brightness: 0f);
		using var sunlight = factory.LightBuilder.CreateDirectionalLight(castsShadows: true);
		using var backdrop = factory.AssetLoader.LoadBackdropTexture(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr));
		using var scene = factory.SceneBuilder.CreateScene(backdrop);
		using var sceneRenderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		sceneRenderer.SetQuality(new RenderQualityConfig(BuiltInQualityConfiguration.Ultra));
		scene.Add(light);
		scene.Add(sunlight);

		using var canvas = factory.SceneBuilder.CreateCanvasScene();
		using var canvasRenderer = factory.RendererBuilder.CreateRenderer(canvas, window);
		using var compositor = factory.RendererBuilder.CreateCompositor(window);
		compositor.Add(sceneRenderer, RenderCompositionType.Standard);
		compositor.Add(canvasRenderer, RenderCompositionType.RetainPreviousScenes);

		using var font = factory.AssetLoader.LoadFont(BuiltInFont.Monospace);
		using var pen = font.CreatePen(BuiltInFontPenStyle.WhiteWithOutline);
		using var progressText = canvas.Add("Dispatching loads...", pen, TextJustification.Left);
		using var inFlightText = canvas.Add("", pen, TextJustification.Left);
		using var modelText = canvas.Add("", pen, TextJustification.Left);
		progressText.SetPlacementFraction(Orientation2D.UpLeft, (0.012f, 0.015f), 0.032f);
		inFlightText.SetPlacementFraction(Orientation2D.UpLeft, (0.012f, 0.065f), 0.022f);
		modelText.SetPlacementFraction(Orientation2D.DownLeft, (0.012f, 0.015f), 0.024f);

		var fileCount = InteractiveTestFiles.Length;
		var ops = new TinyFfrAsyncOperation<ResourceGroup>?[fileCount];
		var groups = new ResourceGroup?[fileCount];
		var failures = new string?[fileCount];
		var loadDurations = new TimeSpan[fileCount];
		var loadedCount = 0;
		var failedCount = 0;

		using var loop = factory.ApplicationLoopBuilder.CreateLoop();

		// Everything is dispatched up front; the point of the test is that the loop below stays
		// responsive while all of it streams in on worker threads.
		for (var i = 0; i < fileCount; ++i) {
			ops[i] = factory.AssetLoader.LoadAllAsync(
				CommonTestAssets.FindAsset("models/" + InteractiveTestFiles[i].Filename),
				new ModelCreationConfig {
					MeshConfig = new() { LinearRescalingFactor = InteractiveTestFiles[i].ScalingFactor },
					TextureConfig = new() { 
						DataType = TextureDataType.ColorSrgb, 
						CompressionQuality = InteractiveTestFiles[i].Filename.Equals("NodePerformanceTest.glb", StringComparison.Ordinal) ? null : Quality.VeryHigh
					}
				},
				new ModelReadConfig {
					MeshConfig = new() { CorrectFlippedOrientation = true },
					HandleUriEscapedStrings = true
				}
			);
		}
		Console.WriteLine($"Dispatched {fileCount} asynchronous model loads.");

		var currentIndex = -1;
		var curAnimIndex = 0;
		var playingAnim = false;
		var lightBrightnessStage = 0;
		var textRefreshTimer = 0f;
		ModelInstanceGroup? modelInstances = null;

		void UpdateModelText() {
			if (currentIndex < 0 || groups[currentIndex] is not { } g) {
				modelText.SetText("No model displayed yet - waiting for the first load to complete.", TextJustification.Left);
				return;
			}
			var animCount = modelInstances is { } mig && mig.Count > 0 ? mig.Max(m => m.Mesh.Animations.All.Count) : 0;
			modelText.SetText(
				$"{InteractiveTestFiles[currentIndex].Filename}  ({currentIndex + 1} of {fileCount})\n" +
				$"{g.Models.Count} models / {g.Meshes.Count} meshes / {g.Materials.Count} materials / {g.Textures.Count} textures\n" +
				$"loaded in {loadDurations[currentIndex].TotalSeconds:N1}s  |  " +
				(animCount > 0 ? $"anim {curAnimIndex + 1} of {animCount} [{(playingAnim ? "playing" : "stopped")}]" : "no animations"),
				TextJustification.Left
			);
		}

		void ShowModel(int index) {
			if (modelInstances is { } previous) {
				scene.Remove(previous);
				previous.Dispose();
				modelInstances = null;
			}
			if (index < 0 || groups[index] is not { } g) return;

			currentIndex = index;
			curAnimIndex = 0;
			playingAnim = false;
			modelInstances = factory.ObjectBuilder.CreateModelInstances(g.Models);
			scene.Add(modelInstances.Value);
			cameraController.SetConstraints(g.Models.CalculateCombinedBoundingBox());
			UpdateModelText();
		}

		int NextLoadedIndex(int from) {
			for (var n = 1; n <= fileCount; ++n) {
				var idx = (from + n) % fileCount;
				if (groups[idx].HasValue) return idx;
			}
			return -1;
		}

		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			var deltaTime = loop.IterateOnce().AsDeltaTime();
			var somethingCompleted = false;

			for (var i = 0; i < fileCount; ++i) {
				if (ops[i] is not { } op || !op.IsCompleted) continue;
				ops[i] = null;
				somethingCompleted = true;
				loadDurations[i] = loop.TotalIteratedTime;
				try {
					groups[i] = op.GetResultAndDisposeOperation();
					++loadedCount;
				}
#pragma warning disable CA1031 // A single bad asset must not take down the viewer; it's reported on the canvas instead
				catch (Exception e) {
#pragma warning restore CA1031
					failures[i] = e.InnerException?.Message ?? e.Message;
					++failedCount;
					Console.WriteLine($"FAILED to load '{InteractiveTestFiles[i].Filename}': {failures[i]}");
				}
				if (currentIndex < 0 && groups[i].HasValue) ShowModel(i);
			}

			textRefreshTimer -= deltaTime;
			if (somethingCompleted || textRefreshTimer <= 0f) {
				textRefreshTimer = 0.25f;
				var doneCount = loadedCount + failedCount;
				progressText.SetText(
					$"{FormatProgressBar(doneCount, fileCount)}  {doneCount} / {fileCount} loaded" +
					(failedCount > 0 ? $"  ({failedCount} failed)" : "") +
					$"  |  {loop.FramesPerSecondRecentAverage:N0} FPS",
					TextJustification.Left
				);

				var inFlightDescription = "";
				var shown = 0;
				for (var i = 0; i < fileCount && shown < 8; ++i) {
					if (ops[i] == null) continue;
					inFlightDescription += (shown > 0 ? "\n" : "") + "loading  " + InteractiveTestFiles[i].Filename;
					++shown;
				}
				var stillPending = fileCount - loadedCount - failedCount;
				if (stillPending > shown) inFlightDescription += $"\n...and {stillPending - shown} more";
				inFlightText.SetText(inFlightDescription, TextJustification.Left);
			}

			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) {
				scene.RemoveAll(false, false, true);
				var next = NextLoadedIndex(currentIndex);
				if (next >= 0) ShowModel(next);
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.A) && modelInstances is { } animCycleGroup) {
				var animCount = animCycleGroup.Count > 0 ? animCycleGroup.Max(m => m.Mesh.Animations.All.Count) : 0;
				if (animCount > 0) {
					curAnimIndex = (curAnimIndex + 1) % animCount;
					UpdateModelText();
				}
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.S) && modelInstances.HasValue) {
				playingAnim = !playingAnim;
				UpdateModelText();
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.L)) {
				lightBrightnessStage = (lightBrightnessStage + 1) % 4;
				light.SetBrightness(lightBrightnessStage switch {
					0 => 0f,
					1 => 0.33f,
					2 => 0.66f,
					_ => 1f
				});
			}

			if (playingAnim && modelInstances is { } playingGroup) {
				foreach (var instance in playingGroup) {
					if (curAnimIndex >= instance.Mesh.Animations.All.Count) continue;
					instance.GetAnimationPlayer(instance.Animations[curAnimIndex])
						.SetTimePoint((float) loop.TotalIteratedTime.TotalSeconds, AnimationWrapStyle.Loop);
				}
			}

			DefaultCameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, cameraController, deltaTime, window);
			DefaultCameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, cameraController, deltaTime);
			DefaultCameraInputHandler.Progress(cameraController, deltaTime);

			light.Position = camera.Position;
			light.ConeDirection = camera.ViewDirection;

			compositor.RenderAll();
		}
		
		if (modelInstances is { } finalGroup) {
			scene.Remove(finalGroup);
			finalGroup.Dispose();
		}
		// Anything still in flight has to be settled before the factory (and the loader behind it) goes away.
		for (var i = 0; i < fileCount; ++i) {
			if (ops[i] is not { } op) continue;
			try {
				if (!op.GetResultAndDisposeOperation(TimeSpan.FromSeconds(2d), out var rg)) {
					throw new InvalidOperationException("Closing test early to prevent tedious wait.");
				}
				groups[i] = rg;
			}
#pragma warning disable CA1031 // Shutting down; a load that failed on the way out is not interesting
			catch (Exception e) {
#pragma warning restore CA1031
				Console.WriteLine($"Load of '{InteractiveTestFiles[i].Filename}' faulted during shutdown: {e.Message}");
				throw;
			}
		}
		foreach (var group in groups) group?.Dispose();
	}

	void RunAllAutomatedTests() {
		SyncAndAsyncShouldProduceEquivalentTextures();
		SyncAndAsyncShouldApplyProcessingIdentically();
		SyncAndAsyncShouldProduceEquivalentCombinedTextures();
		CanvasTextureShouldKeepZeroAnisotropyThroughBothPaths();
		BuiltInTexturesShouldLoadThroughBothPaths();
		MapLoadersShouldWorkThroughBothPaths();
		ConcurrentAsyncTextureLoadsShouldAllSucceed();
		RepeatedAlternatingLoadsShouldRemainConsistent();
		SyncAndAsyncShouldProduceIdenticalNonSkeletalMeshes();
		SyncAndAsyncShouldApplyCreationConfigTransformsIdentically();
		SyncAndAsyncShouldProduceIdenticalSkeletalMeshes();
		ConcurrentAsyncMeshLoadsShouldAllSucceed();
		RepeatedLoadsShouldNotLeakOrCorrupt();
		LoadAllShouldStillWorkAfterModelsRework();
		SyncAndAsyncShouldProduceEquivalentModelGroups();
		AssetMapSlotsShouldBeWiredToTheCorrectMapTypes();
		ConcurrentAsyncModelLoadsShouldAllSucceed();
		FailedModelLoadShouldNotLeakPartialResources();
		ReadMeshShouldStillPopulateCallerBuffers();
	}

	void SyncAndAsyncShouldProduceEquivalentTextures() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		var metadata = loader.ReadTextureMetadata(CrateColorFile);
		Console.WriteLine($"ELCrate.png: {metadata.Dimensions}, alpha={metadata.IncludesAlphaChannel}");

		using var syncTex = loader.LoadTexture(CrateColorFile, dataType: TextureDataType.ColorSrgb, "tex-sync");
		using var asyncTex = AwaitLoad(loader.LoadTextureAsync(CrateColorFile, dataType: TextureDataType.ColorSrgb, "tex-async"));

		AssertTexturesEquivalent(syncTex, asyncTex, "plain load");
		Assert.AreEqual(metadata.Dimensions, syncTex.Dimensions);
		Assert.AreEqual("tex-sync", syncTex.GetNameAsNewStringObject());
		Assert.AreEqual("tex-async", asyncTex.GetNameAsNewStringObject());
	}

	void SyncAndAsyncShouldApplyProcessingIdentically() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		var config = new TextureCreationConfig {
			DataType = TextureDataType.LinearData,
			Name = "processed",
			ProcessingToApply = new TextureProcessingConfig {
				FlipX = true,
				FlipY = true,
				InvertYGreenChannel = true,
				ZBlueFinalOutputSource = A
			}
		};

		using var syncTex = loader.LoadTexture(CrateColorFile, in config);
		using var asyncTex = AwaitLoad(loader.LoadTextureAsync(CrateColorFile, in config));
		AssertTexturesEquivalent(syncTex, asyncTex, "processed load");
	}

	void SyncAndAsyncShouldProduceEquivalentCombinedTextures() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		var twoSource = new TextureCombinationConfig {
			OutputTextureXRedChannelSource = new(TextureA, R),
			OutputTextureYGreenChannelSource = new(TextureA, G),
			OutputTextureZBlueChannelSource = new(TextureB, R)
		};
		using var sync2 = loader.LoadCombinedTexture(CrateColorFile, CrateNormalFile, twoSource, TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, "combine2"));
		using var async2 = AwaitLoad(loader.LoadCombinedTextureAsync(CrateColorFile, CrateNormalFile, twoSource, TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, "combine2")));
		AssertTexturesEquivalent(sync2, async2, "2-source combine");

		var threeSource = new TextureCombinationConfig {
			OutputTextureXRedChannelSource = new(TextureA, R),
			OutputTextureYGreenChannelSource = new(TextureB, R),
			OutputTextureZBlueChannelSource = new(TextureC, R)
		};
		using var sync3 = loader.LoadCombinedTexture(CrateColorFile, CrateNormalFile, CrateOrmFile, threeSource, TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, "combine3"));
		using var async3 = AwaitLoad(loader.LoadCombinedTextureAsync(CrateColorFile, CrateNormalFile, CrateOrmFile, threeSource, TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, "combine3")));
		AssertTexturesEquivalent(sync3, async3, "3-source combine");

		var fourSource = new TextureCombinationConfig {
			OutputTextureXRedChannelSource = new(TextureA, R),
			OutputTextureYGreenChannelSource = new(TextureB, R),
			OutputTextureZBlueChannelSource = new(TextureC, R),
			OutputTextureWAlphaChannelSource = new(TextureD, R)
		};
		using var sync4 = loader.LoadCombinedTexture(CrateColorFile, CrateNormalFile, CrateOrmFile, CrateColorFile, fourSource, TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, "combine4"));
		using var async4 = AwaitLoad(loader.LoadCombinedTextureAsync(CrateColorFile, CrateNormalFile, CrateOrmFile, CrateColorFile, fourSource, TextureCreationConfig.ForDataTexture(TextureDataType.LinearData, "combine4")));
		AssertTexturesEquivalent(sync4, async4, "4-source combine");
		Assert.AreEqual(TexelType.Rgba32, sync4.TexelType, "A combine with an alpha source should produce an Rgba32 texture.");
		Assert.AreEqual(TexelType.Rgb24, sync3.TexelType, "A combine without an alpha source should produce an Rgb24 texture.");
	}

	void CanvasTextureShouldKeepZeroAnisotropyThroughBothPaths() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		using var syncTex = loader.LoadCanvasTexture(CrateColorFile);
		using var asyncTex = AwaitLoad(loader.LoadCanvasTextureAsync(CrateColorFile));

		Console.WriteLine($"  sync aniso={syncTex.RenderingConfig.AnisotropyLevel} async aniso={asyncTex.RenderingConfig.AnisotropyLevel}");
		Assert.AreEqual(0f, syncTex.RenderingConfig.AnisotropyLevel, "Sync canvas texture lost its explicit zero anisotropy.");
		Assert.AreEqual(0f, asyncTex.RenderingConfig.AnisotropyLevel, "Async canvas texture lost its explicit zero anisotropy (the config round-trip dropped AnisotropyLevel).");
		AssertTexturesEquivalent(syncTex, asyncTex, "canvas texture");
	}

	void BuiltInTexturesShouldLoadThroughBothPaths() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		using var syncTex = loader.LoadTexture(loader.BuiltInTexturePaths.DefaultReflectanceMap, dataType: TextureDataType.LinearData, "builtin-sync");
		using var asyncTex = AwaitLoad(loader.LoadTextureAsync(loader.BuiltInTexturePaths.DefaultReflectanceMap, dataType: TextureDataType.LinearData, "builtin-async"));
		AssertTexturesEquivalent(syncTex, asyncTex, "built-in texel");

		using var syncEmbedded = loader.LoadTexture(loader.BuiltInTexturePaths.UvTestingTexture, dataType: TextureDataType.ColorSrgb, "embedded-sync");
		using var asyncEmbedded = AwaitLoad(loader.LoadTextureAsync(loader.BuiltInTexturePaths.UvTestingTexture, dataType: TextureDataType.ColorSrgb, "embedded-async"));
		AssertTexturesEquivalent(syncEmbedded, asyncEmbedded, "built-in embedded resource");
	}

	void MapLoadersShouldWorkThroughBothPaths() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		using var syncNormal = loader.LoadNormalMap(CrateNormalFile);
		using var asyncNormal = AwaitLoad(loader.LoadNormalMapAsync(CrateNormalFile));
		AssertTexturesEquivalent(syncNormal, asyncNormal, "normal map");

		using var syncOrm = loader.LoadOcclusionRoughnessMetallicMap(CrateOrmFile);
		using var asyncOrm = AwaitLoad(loader.LoadOcclusionRoughnessMetallicMapAsync(CrateOrmFile));
		AssertTexturesEquivalent(syncOrm, asyncOrm, "ORM map");

		using var syncOrmr = loader.LoadOcclusionRoughnessMetallicReflectanceMap(CrateOrmFile);
		using var asyncOrmr = AwaitLoad(loader.LoadOcclusionRoughnessMetallicReflectanceMapAsync(CrateOrmFile));
		AssertTexturesEquivalent(syncOrmr, asyncOrmr, "ORMR map (metadata-branching)");

		using var syncAniso = loader.LoadAnisotropyMapRadialAngleFormatted(CrateOrmFile, Orientation2D.Right, AnisotropyRadialAngleRange.ZeroTo360, true, R);
		using var asyncAniso = AwaitLoad(loader.LoadAnisotropyMapRadialAngleFormattedAsync(CrateOrmFile, Orientation2D.Right, AnisotropyRadialAngleRange.ZeroTo360, true, R));
		AssertTexturesEquivalent(syncAniso, asyncAniso, "radial-angle anisotropy map");
		AssertRadialAngleAnisotropyTexelType(loader, CrateOrmFile, syncAniso);

		Assert.IsTrue(loader.ReadTextureMetadata(AlphaLogoFile).IncludesAlphaChannel, "Expected an alpha-bearing asset; the Rgba32 anisotropy branch would otherwise go untested.");
		using var syncAnisoAlpha = loader.LoadAnisotropyMapRadialAngleFormatted(AlphaLogoFile, Orientation2D.Up, AnisotropyRadialAngleRange.ZeroTo180, false, A);
		using var asyncAnisoAlpha = AwaitLoad(loader.LoadAnisotropyMapRadialAngleFormattedAsync(AlphaLogoFile, Orientation2D.Up, AnisotropyRadialAngleRange.ZeroTo180, false, A));
		AssertTexturesEquivalent(syncAnisoAlpha, asyncAnisoAlpha, "radial-angle anisotropy map (alpha strength channel)");
		AssertRadialAngleAnisotropyTexelType(loader, AlphaLogoFile, syncAnisoAlpha);

		using var syncAniso2 = loader.LoadAnisotropyMapRadialAngleFormatted(CrateOrmFile, CrateColorFile, Orientation2D.Right, AnisotropyRadialAngleRange.ZeroTo180, false);
		using var asyncAniso2 = AwaitLoad(loader.LoadAnisotropyMapRadialAngleFormattedAsync(CrateOrmFile, CrateColorFile, Orientation2D.Right, AnisotropyRadialAngleRange.ZeroTo180, false));
		AssertTexturesEquivalent(syncAniso2, asyncAniso2, "radial-angle anisotropy map (two-source)");
		Assert.AreEqual(TexelType.Rgb24, syncAniso2.TexelType, "A two-source radial-angle anisotropy map has no alpha source and must always be Rgb24.");
	}

	static void AssertRadialAngleAnisotropyTexelType(IAssetLoader loader, string filePath, Texture texture) {
		var expected = loader.ReadTextureMetadata(filePath).IncludesAlphaChannel ? TexelType.Rgba32 : TexelType.Rgb24;
		Assert.AreEqual(
			expected,
			texture.TexelType,
			$"Radial-angle anisotropy load of '{Path.GetFileName(filePath)}' produced the wrong texel type; " +
			"the metadata probe and the pinned read config must agree with the texel type the conversion callback was created for."
		);
	}

	void ConcurrentAsyncTextureLoadsShouldAllSucceed() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		var ops = new[] {
			loader.LoadTextureAsync(CrateColorFile, TextureDataType.ColorSrgb, "c0"),
			loader.LoadTextureAsync(CrateNormalFile, TextureDataType.LinearDataUnitVector, "c1"),
			loader.LoadTextureAsync(CrateOrmFile, TextureDataType.LinearData, "c2"),
			loader.LoadTextureAsync(CrateColorFile, TextureDataType.ColorSrgb, "c3")
		};

		var textures = ops.Select(AwaitLoad).ToList();
		for (var i = 0; i < textures.Count; ++i) {
			Console.WriteLine($"  concurrent[{i}] '{textures[i].GetNameAsNewStringObject()}' {textures[i].Dimensions}");
			Assert.AreEqual($"c{i}", textures[i].GetNameAsNewStringObject());
		}
		foreach (var texture in textures) texture.Dispose();
	}

	void RepeatedAlternatingLoadsShouldRemainConsistent() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		XYPair<int>? expectedDimensions = null;
		for (var i = 0; i < 12; ++i) {
			using var texture = (i % 2 == 0)
				? loader.LoadTexture(CrateColorFile, dataType: TextureDataType.ColorSrgb, "repeat")
				: AwaitLoad(loader.LoadTextureAsync(CrateColorFile, dataType: TextureDataType.ColorSrgb, "repeat"));
			expectedDimensions ??= texture.Dimensions;
			Assert.AreEqual(expectedDimensions.Value, texture.Dimensions, $"Iteration {i} produced different dimensions.");
		}
		Console.WriteLine($"  12 alternating sync/async loads all produced {expectedDimensions}");
	}
	
	static void DescribeMesh(string label, IAssetLoader loader, Mesh mesh) {
		Console.WriteLine($"  {label}: name='{mesh.GetNameAsNewStringObject()}' bounds={mesh.BoundingBox} animations={mesh.Animations.Count()} nodes={mesh.Skeleton.Nodes.Count()}");
	}

	void SyncAndAsyncShouldProduceIdenticalNonSkeletalMeshes() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		var metadata = loader.ReadMeshMetadata(CrateFile);
		Console.WriteLine($"ELCrate.obj metadata: {metadata.TotalVertexCount} verts, {metadata.TotalTriangleCount} tris, {metadata.SubMeshCount} sub-meshes");
		Assert.Greater(metadata.TotalVertexCount, 0);
		Assert.Greater(metadata.TotalTriangleCount, 0);

		using var syncMesh = loader.LoadMesh(CrateFile, "crate-sync");
		using var asyncMesh = AwaitLoad(loader.LoadMeshAsync(CrateFile, "crate-async"));

		DescribeMesh("sync ", loader, syncMesh);
		DescribeMesh("async", loader, asyncMesh);

		Assert.AreEqual(syncMesh.BoundingBox, asyncMesh.BoundingBox, "Bounding boxes differ between sync and async load.");
		Assert.AreEqual("crate-sync", syncMesh.GetNameAsNewStringObject());
		Assert.AreEqual("crate-async", asyncMesh.GetNameAsNewStringObject());
	}

	void SyncAndAsyncShouldApplyCreationConfigTransformsIdentically() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		var config = new MeshCreationConfig {
			Name = "transformed",
			OriginTranslation = new Vect(1.5f, -2f, 0.25f),
			LinearRescalingFactor = 3f,
			InvertTextureU = true,
			InvertTextureV = true,
			FlipTriangles = true,
			BoundingBoxAdditionalMargin = 0.05f
		};

		using var plainMesh = loader.LoadMesh(CrateFile, "plain");
		using var syncMesh = loader.LoadMesh(CrateFile, config);
		using var asyncMesh = AwaitLoad(loader.LoadMeshAsync(CrateFile, config));

		Console.WriteLine($"  plain bounds:       {plainMesh.BoundingBox}");
		Console.WriteLine($"  sync transformed:   {syncMesh.BoundingBox}");
		Console.WriteLine($"  async transformed:  {asyncMesh.BoundingBox}");

		Assert.AreEqual(syncMesh.BoundingBox, asyncMesh.BoundingBox, "Transformed bounding boxes differ between sync and async load.");
		Assert.AreNotEqual(plainMesh.BoundingBox, syncMesh.BoundingBox, "Transform config had no effect at all; test is vacuous.");
	}

	void SyncAndAsyncShouldProduceIdenticalSkeletalMeshes() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		foreach (var file in new[] { RiggedFile, CesiumManFile }) {
			Console.WriteLine($"--- {Path.GetFileName(file)} ---");
			using var syncMesh = loader.LoadMesh(file, "skel-sync");
			using var asyncMesh = AwaitLoad(loader.LoadMeshAsync(file, "skel-async"));

			DescribeMesh("sync ", loader, syncMesh);
			DescribeMesh("async", loader, asyncMesh);

			Assert.AreEqual(syncMesh.BoundingBox, asyncMesh.BoundingBox, $"{file}: bounding boxes differ.");

			var syncAnims = syncMesh.Animations.Select(a => a.GetNameAsNewStringObject()).ToList();
			var asyncAnims = asyncMesh.Animations.Select(a => a.GetNameAsNewStringObject()).ToList();
			Console.WriteLine($"  animations sync={String.Join(",", syncAnims)} async={String.Join(",", asyncAnims)}");
            Assert.That(asyncAnims, Is.EqualTo(syncAnims).AsCollection, $"{file}: animation names differ.");

			var syncNodes = syncMesh.Skeleton.Nodes.Select(n => n.GetNameAsNewStringObject()).ToList();
			var asyncNodes = asyncMesh.Skeleton.Nodes.Select(n => n.GetNameAsNewStringObject()).ToList();
			Console.WriteLine($"  node names sync=[{String.Join(",", syncNodes)}]");
			Console.WriteLine($"  node names async=[{String.Join(",", asyncNodes)}]");
            Assert.That(asyncNodes, Is.EqualTo(syncNodes).AsCollection, $"{file}: skeleton node names differ.");
			Assert.Greater(syncNodes.Count, 0, $"{file}: expected skeletal nodes but found none; test is vacuous.");
		}
	}

	void ConcurrentAsyncMeshLoadsShouldAllSucceed() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		var ops = new[] {
			loader.LoadMeshAsync(CrateFile, "c0"),
			loader.LoadMeshAsync(RiggedFile, "c1"),
			loader.LoadMeshAsync(CesiumManFile, "c2"),
			loader.LoadMeshAsync(CrateFile, "c3")
		};

		var meshes = ops.Select(AwaitLoad).ToList();
		for (var i = 0; i < meshes.Count; ++i) {
			Console.WriteLine($"  concurrent[{i}] name='{meshes[i].GetNameAsNewStringObject()}' bounds={meshes[i].BoundingBox}");
			Assert.AreEqual($"c{i}", meshes[i].GetNameAsNewStringObject());
		}
		foreach (var mesh in meshes) mesh.Dispose();
	}

	void RepeatedLoadsShouldNotLeakOrCorrupt() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		PositionedCuboid? expectedBounds = null;
		for (var i = 0; i < 12; ++i) {
			using var mesh = (i % 2 == 0)
				? loader.LoadMesh(CrateFile, "repeat")
				: AwaitLoad(loader.LoadMeshAsync(CrateFile, "repeat"));
			expectedBounds ??= mesh.BoundingBox;
			Assert.AreEqual(expectedBounds.Value, mesh.BoundingBox, $"Iteration {i} produced different geometry.");
		}
		Console.WriteLine($"  12 alternating sync/async loads all produced bounds {expectedBounds}");
	}

	void LoadAllShouldStillWorkAfterModelsRework() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		foreach (var file in new[] { CesiumManFile, CommonTestAssets.FindAsset("models/DamagedHelmet.glb") }) {
			using var group = loader.LoadAll(file, Path.GetFileNameWithoutExtension(file));
			Console.WriteLine($"  {Path.GetFileName(file)}: {group.Meshes.Count} meshes, {group.Materials.Count} materials, {group.Textures.Count} textures");
			Assert.Greater(group.Meshes.Count, 0, $"{file}: LoadAll produced no meshes.");
			Assert.Greater(group.Materials.Count, 0, $"{file}: LoadAll produced no materials.");
		}
	}

	static void AssertGroupsEquivalent(ResourceGroup expected, ResourceGroup actual, string label) {
		Assert.AreEqual(expected.Meshes.Count, actual.Meshes.Count, $"{label}: mesh count differs.");
		Assert.AreEqual(expected.Materials.Count, actual.Materials.Count, $"{label}: material count differs.");
		Assert.AreEqual(expected.Textures.Count, actual.Textures.Count, $"{label}: texture count differs.");
		Assert.AreEqual(expected.Models.Count, actual.Models.Count, $"{label}: model count differs.");
		Assert.AreEqual(expected.ResourceCount, actual.ResourceCount, $"{label}: total resource count differs.");

		for (var i = 0; i < expected.Meshes.Count; ++i) {
			Assert.AreEqual(expected.Meshes[i].BoundingBox, actual.Meshes[i].BoundingBox, $"{label}: mesh {i} bounding box differs.");
			Assert.AreEqual(expected.Meshes[i].GetNameAsNewStringObject(), actual.Meshes[i].GetNameAsNewStringObject(), $"{label}: mesh {i} name differs.");
			Assert.AreEqual(expected.Meshes[i].Animations.Count(), actual.Meshes[i].Animations.Count(), $"{label}: mesh {i} animation count differs.");
			Assert.AreEqual(expected.Meshes[i].Skeleton.Nodes.Count(), actual.Meshes[i].Skeleton.Nodes.Count(), $"{label}: mesh {i} skeleton node count differs.");
		}
		for (var i = 0; i < expected.Textures.Count; ++i) {
			AssertTexturesEquivalent(expected.Textures[i], actual.Textures[i], $"{label} texture {i}");
		}
	}

	void SyncAndAsyncShouldProduceEquivalentModelGroups() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		foreach (var file in new[] { CesiumManFile, HelmetFile, CarConceptFile }) {
			var label = Path.GetFileName(file);
			using var syncGroup = loader.LoadAll(file, "grp");
			using var asyncGroup = AwaitLoad(loader.LoadAllAsync(file, "grp"));

			Assert.Greater(syncGroup.Meshes.Count, 0, $"{label}: LoadAll produced no meshes.");
			Assert.Greater(syncGroup.Materials.Count, 0, $"{label}: LoadAll produced no materials.");
			Console.WriteLine($"  {label}: {syncGroup.Meshes.Count} meshes / {syncGroup.Materials.Count} materials / {syncGroup.Textures.Count} textures / {syncGroup.Models.Count} models");

			AssertGroupsEquivalent(syncGroup, asyncGroup, label);
		}
	}

	void AssetMapSlotsShouldBeWiredToTheCorrectMapTypes() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		static void AssertMapTypes(ResourceGroup group, string label, params string[] expectedSuffixes) {
			var actual = group.Textures.Select(t => t.GetNameAsNewStringObject()).ToArray();
			Assert.AreEqual(expectedSuffixes.Length, actual.Length, $"{label}: expected {expectedSuffixes.Length} textures but got {actual.Length} ({String.Join(", ", actual)}).");
			for (var i = 0; i < expectedSuffixes.Length; ++i) {
				Assert.IsTrue(
					actual[i].EndsWith(expectedSuffixes[i], StringComparison.Ordinal),
					$"{label}: texture {i} is '{actual[i]}' but should be a '{expectedSuffixes[i]}' map. Full set: {String.Join(", ", actual)}"
				);
			}
			Console.WriteLine($"  {label}: {String.Join(", ", actual)}");
		}

		using (var cesiumGroup = loader.LoadAll(CesiumManFile, "grp")) {
			AssertMapTypes(cesiumGroup, "CesiumMan", "texture map_color", "texture map_orm");
		}
		using (var helmetGroup = AwaitLoad(loader.LoadAllAsync(HelmetFile, "grp"))) {
			AssertMapTypes(helmetGroup, "DamagedHelmet", "texture map_color", "texture map_norm", "texture map_orm", "texture map_emissive");
		}
	}

	void ConcurrentAsyncModelLoadsShouldAllSucceed() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		var expectedMeshCounts = new List<int>();
		foreach (var file in new[] { CesiumManFile, HelmetFile, CarConceptFile }) {
			using var syncGroup = loader.LoadAll(file, "expected");
			expectedMeshCounts.Add(syncGroup.Meshes.Count);
		}

		var operations = new[] {
			loader.LoadAllAsync(CesiumManFile, "concurrent0"),
			loader.LoadAllAsync(HelmetFile, "concurrent1"),
			loader.LoadAllAsync(CarConceptFile, "concurrent2")
		};

		var groups = operations.Select(AwaitLoad).ToArray();
		try {
			for (var i = 0; i < groups.Length; ++i) {
				Assert.AreEqual(expectedMeshCounts[i], groups[i].Meshes.Count, $"Concurrent load {i} produced a different mesh count.");
			}
			Console.WriteLine($"  3 concurrent LoadAllAsync operations produced mesh counts {String.Join(", ", groups.Select(g => g.Meshes.Count))}");
		}
		finally {
			foreach (var group in groups) group.Dispose();
		}
	}

	static int _postProcessInvocationsBeforeFailure;

	static void ThrowAfterFirstInvocation(Span<TexelRgba32> texels, object? argument) {
		if (_postProcessInvocationsBeforeFailure-- <= 0) throw new InvalidOperationException("Deliberate failure raised by the test's post-processing hook.");
	}

	void FailedModelLoadShouldNotLeakPartialResources() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;
		var directory = factory.ResourceDirectory;

		var baselineMeshes = directory.GetAllActiveInstances<Mesh>().Count;
		var baselineTextures = directory.GetAllActiveInstances<Texture>().Count;
		var baselineMaterials = directory.GetAllActiveInstances<Material>().Count;
		var baselineModels = directory.GetAllActiveInstances<Model>().Count;

		for (var allowedInvocations = 0; allowedInvocations < 4; ++allowedInvocations) {
			_postProcessInvocationsBeforeFailure = allowedInvocations;

			Assert.Catch(() => {
				using var _ = loader.LoadAll(
					CarConceptFile,
					new ModelCreationConfig {
						Name = "doomed",
						TextureConfig = new TextureCreationConfig {
							DataType = TextureDataType.LinearData,
							ProcessingToApply = new TextureProcessingConfig {
								PostProcessingFunction = TexelProcessingFunction.Create<TexelRgba32>(&ThrowAfterFirstInvocation)
							}
						}
					}
				);
			}, $"Expected the deliberate post-processing failure to propagate (allowedInvocations={allowedInvocations}).");

			Assert.AreEqual(baselineMeshes, directory.GetAllActiveInstances<Mesh>().Count, $"A failed load leaked meshes (allowedInvocations={allowedInvocations}).");
			Assert.AreEqual(baselineTextures, directory.GetAllActiveInstances<Texture>().Count, $"A failed load leaked textures (allowedInvocations={allowedInvocations}).");
			Assert.AreEqual(baselineMaterials, directory.GetAllActiveInstances<Material>().Count, $"A failed load leaked materials (allowedInvocations={allowedInvocations}).");
			Assert.AreEqual(baselineModels, directory.GetAllActiveInstances<Model>().Count, $"A failed load leaked models (allowedInvocations={allowedInvocations}).");
		}

		Console.WriteLine("  4 deliberately-failed loads left no orphaned meshes, textures, materials or models");

		using var recoveryGroup = loader.LoadAll(CarConceptFile, "recovered");
		Assert.Greater(recoveryGroup.Meshes.Count, 0, "The loader did not recover after a failed load.");
		Console.WriteLine($"  a subsequent load still produced {recoveryGroup.Meshes.Count} meshes");
	}

	void ReadMeshShouldStillPopulateCallerBuffers() {
		using var factory = new LocalTinyFfrFactory();
		var loader = factory.AssetLoader;

		var metadata = loader.ReadMeshMetadata(CrateFile);
		var vertices = new MeshVertex[metadata.TotalVertexCount];
		var triangles = new VertexTriangle[metadata.TotalTriangleCount];
		var counts = loader.ReadMesh(CrateFile, vertices, triangles);

		Console.WriteLine($"  ReadMesh wrote {counts.NumVerticesWritten} verts / {counts.NumTrianglesWritten} tris");
		Assert.AreEqual(metadata.TotalVertexCount, counts.NumVerticesWritten);
		Assert.AreEqual(metadata.TotalTriangleCount, counts.NumTrianglesWritten);
		Assert.IsTrue(vertices.Any(v => v.Location != Location.Origin), "ReadMesh left the vertex buffer empty.");
		Assert.IsTrue(triangles.Any(t => t.IndexA != 0 || t.IndexB != 0 || t.IndexC != 0), "ReadMesh left the triangle buffer empty.");
	}
}
