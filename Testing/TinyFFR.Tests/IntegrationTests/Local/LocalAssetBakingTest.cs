// Created on 2026-08-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Baking;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.Threading;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalAssetBakingTest {
	enum LoadPhase { Normal, Prebaked }

	const string BakedAssetDirName = "baked_assets";
	static readonly TimeSpan MaxSceneDuration = TimeSpan.FromMinutes(10d);

	abstract class BakedAssetEntry : IDisposable {
		public abstract string DisplayName { get; }
		public abstract string BakedFileName { get; }
		public abstract void AddPendingOperationsTo(List<TinyFfrAsyncOperation> dest);
		public abstract void BeginLoadFromSource(LocalTinyFfrFactory factory);
		public abstract void BeginLoadFromBakedFile(LocalTinyFfrFactory factory, string filePath);
		public abstract void CompletePendingLoad(LocalTinyFfrFactory factory);
		public abstract void BakeToFile(LocalTinyFfrFactory factory, string filePath);
		public abstract void AddToScene(LocalTinyFfrFactory factory, Scene scene, CanvasScene canvas);
		public abstract void RemoveFromScene(Scene scene, CanvasScene canvas);
		public virtual void Update(float totalSeconds) { }
		public abstract void Dispose();
	}

	sealed class BackdropTextureEntry : BakedAssetEntry {
		BackdropTexture? _texture;
		TinyFfrAsyncOperation<BackdropTexture>? _pendingOperation;

		public override string DisplayName => "Backdrop Texture";
		public override string BakedFileName => "backdrop_texture.tinyffr";

		public override void AddPendingOperationsTo(List<TinyFfrAsyncOperation> dest) {
			if (_pendingOperation is { } op) dest.Add(op);
		}

		public override void BeginLoadFromSource(LocalTinyFfrFactory factory) {
			_pendingOperation = factory.AssetLoader.LoadBackdropTextureAsync(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr));
		}

		public override void BeginLoadFromBakedFile(LocalTinyFfrFactory factory, string filePath) {
			_pendingOperation = factory.AssetLoader.LoadBakedBackdropTextureAsync(filePath);
		}

		public override void CompletePendingLoad(LocalTinyFfrFactory factory) {
			if (_pendingOperation is not { } op) throw new InvalidOperationException($"No pending load for '{DisplayName}'.");
			_texture = op.GetResultAndDisposeOperation();
			_pendingOperation = null;
		}

		public override void BakeToFile(LocalTinyFfrFactory factory, string filePath) {
			factory.AssetBakery.Bake(_texture!.Value, filePath);
		}

		public override void AddToScene(LocalTinyFfrFactory factory, Scene scene, CanvasScene canvas) => scene.SetBackdrop(_texture!.Value, 1f);

		public override void RemoveFromScene(Scene scene, CanvasScene canvas) => scene.RemoveBackdrop();

		public override void Dispose() {
			_texture?.Dispose();
			_texture = null;
		}
	}

	abstract class TextureEntry : BakedAssetEntry {
		Texture? _texture;
		TinyFfrAsyncOperation<Texture>? _pendingOperation;
		CanvasTexture? _canvasObject;

		protected abstract XYPair<float> CanvasPosition { get; }
		protected abstract TinyFfrAsyncOperation<Texture> DispatchSourceLoad(LocalTinyFfrFactory factory);

		public override void AddPendingOperationsTo(List<TinyFfrAsyncOperation> dest) {
			if (_pendingOperation is { } op) dest.Add(op);
		}

		public override void BeginLoadFromSource(LocalTinyFfrFactory factory) {
			_pendingOperation = DispatchSourceLoad(factory);
		}

		public override void BeginLoadFromBakedFile(LocalTinyFfrFactory factory, string filePath) {
			_pendingOperation = factory.AssetLoader.LoadBakedTextureAsync(filePath);
		}

		public override void CompletePendingLoad(LocalTinyFfrFactory factory) {
			if (_pendingOperation is not { } op) throw new InvalidOperationException($"No pending load for '{DisplayName}'.");
			_texture = op.GetResultAndDisposeOperation();
			_pendingOperation = null;
		}

		public override void BakeToFile(LocalTinyFfrFactory factory, string filePath) {
			factory.AssetBakery.Bake(_texture!.Value, filePath);
		}

		public override void AddToScene(LocalTinyFfrFactory factory, Scene scene, CanvasScene canvas) {
			var canvasObject = canvas.Add(_texture!.Value);
			canvasObject.SetPlacementFraction(Orientation2D.UpLeft, CanvasPosition, (0.16f, 0.16f));
			_canvasObject = canvasObject;
		}

		public override void RemoveFromScene(Scene scene, CanvasScene canvas) {
			_canvasObject?.Dispose();
			_canvasObject = null;
		}

		public override void Dispose() {
			_canvasObject?.Dispose();
			_canvasObject = null;
			_texture?.Dispose();
			_texture = null;
		}
	}

	sealed class FileTextureEntry : TextureEntry {
		public override string DisplayName => "File Texture";
		public override string BakedFileName => "file_texture.tinyffr";
		protected override XYPair<float> CanvasPosition => (0.80f, 0.04f);

		protected override TinyFfrAsyncOperation<Texture> DispatchSourceLoad(LocalTinyFfrFactory factory) {
			return factory.AssetLoader.LoadTextureAsync(CommonTestAssets.FindAsset(KnownTestAsset.BrickAlbedoTex), TextureDataType.ColorSrgb);
		}
	}

	sealed class BuiltInTextureEntry : TextureEntry {
		public override string DisplayName => "Built-In Texture";
		public override string BakedFileName => "built_in_texture.tinyffr";
		protected override XYPair<float> CanvasPosition => (0.80f, 0.24f);

		protected override TinyFfrAsyncOperation<Texture> DispatchSourceLoad(LocalTinyFfrFactory factory) {
			return factory.AssetLoader.LoadColorMapAsync(factory.AssetLoader.BuiltInTexturePaths.UvTestingTexture, Quality.VeryHigh);
		}
	}

	sealed class AnisotropyMapEntry : TextureEntry {
		public override string DisplayName => "Anisotropy Map";
		public override string BakedFileName => "anisotropy_map.tinyffr";
		protected override XYPair<float> CanvasPosition => (0.80f, 0.44f);

		protected override TinyFfrAsyncOperation<Texture> DispatchSourceLoad(LocalTinyFfrFactory factory) {
			return factory.AssetLoader.LoadAnisotropyMapRadialAngleFormattedAsync(
				CommonTestAssets.FindAsset("aniso_metal/aniso_angle.jpg"),
				CommonTestAssets.FindAsset("aniso_metal/aniso_strength.jpg"),
				Orientation2D.Up,
				AnisotropyRadialAngleRange.ZeroTo360,
				encodedAnticlockwise: true
			);
		}
	}

	sealed class MaterialEntry : BakedAssetEntry {
		const int CubeCount = 7;
		const float CubeRingRadius = 3.6f;
		const float CubeSize = 1.3f;

		Material? _material;
		Mesh? _mesh;
		ResourceGroup? _loadedGroup;
		readonly List<Texture> _ownedTextures = new();
		readonly List<ModelInstance> _instances = new();
		TinyFfrAsyncOperation<Texture>? _pendingColorMap;
		TinyFfrAsyncOperation<Texture>? _pendingNormalMap;
		TinyFfrAsyncOperation<Texture>? _pendingOrmrMap;
		TinyFfrAsyncOperation<ResourceGroup>? _pendingBakedOperation;

		public override string DisplayName => "Material (6 maps)";
		public override string BakedFileName => "material.tinyffr";

		public override void AddPendingOperationsTo(List<TinyFfrAsyncOperation> dest) {
			if (_pendingColorMap is { } colorOp) dest.Add(colorOp);
			if (_pendingNormalMap is { } normalOp) dest.Add(normalOp);
			if (_pendingOrmrMap is { } ormrOp) dest.Add(ormrOp);
			if (_pendingBakedOperation is { } bakedOp) dest.Add(bakedOp);
		}

		public override void BeginLoadFromSource(LocalTinyFfrFactory factory) {
			_pendingColorMap = factory.AssetLoader.LoadTextureAsync(CommonTestAssets.FindAsset(KnownTestAsset.BrickAlbedoTex), TextureCreationConfig.ForColorMap(Quality.VeryLow));
			_pendingNormalMap = factory.AssetLoader.LoadTextureAsync(CommonTestAssets.FindAsset(KnownTestAsset.BrickNormalTex), TextureCreationConfig.ForNormalMap(Quality.VeryLow));
			_pendingOrmrMap = factory.AssetLoader.LoadTextureAsync(CommonTestAssets.FindAsset(KnownTestAsset.BrickOrmTex), TextureCreationConfig.ForOrmrMap(Quality.VeryLow));
		}

		public override void BeginLoadFromBakedFile(LocalTinyFfrFactory factory, string filePath) {
			_pendingBakedOperation = factory.AssetLoader.LoadBakedMaterialAsync(filePath);
		}

		public override void CompletePendingLoad(LocalTinyFfrFactory factory) {
			if (_pendingBakedOperation is { } bakedOp) {
				var group = bakedOp.GetResultAndDisposeOperation();
				_pendingBakedOperation = null;
				_loadedGroup = group;
				foreach (var material in group.Materials) {
					_material = material;
					break;
				}
			}
			else if (_pendingColorMap is { } colorOp && _pendingNormalMap is { } normalOp && _pendingOrmrMap is { } ormrOp) {
				var colorMap = colorOp.GetResultAndDisposeOperation();
				var normalMap = normalOp.GetResultAndDisposeOperation();
				var ormrMap = ormrOp.GetResultAndDisposeOperation();
				_pendingColorMap = null;
				_pendingNormalMap = null;
				_pendingOrmrMap = null;
				_ownedTextures.Add(colorMap);
				_ownedTextures.Add(normalMap);
				_ownedTextures.Add(ormrMap);

				var texBuilder = factory.AssetLoader.MaterialBuilder.TextureBuilder;

				var anisotropyMap = texBuilder.CreateAnisotropyMap(
					TexturePattern.Lines(
						Angle.From2DPolarAngle(Orientation2D.Right)!.Value,
						Angle.From2DPolarAngle(Orientation2D.Up)!.Value,
						Angle.From2DPolarAngle(Orientation2D.UpLeft)!.Value,
						Angle.From2DPolarAngle(Orientation2D.DownLeft)!.Value,
						horizontal: false,
						numRepeats: 4
					),
					TexturePattern.Lines<Real>(1f, 1f, 0f, 0f, horizontal: false, numRepeats: 2),
					TextureCreationConfig.ForAnisotropyMap(Quality.VeryLow)
				);
				_ownedTextures.Add(anisotropyMap);

				var emissiveMap = texBuilder.CreateEmissiveMap(
					TexturePattern.Rectangles(
						interiorSize: TexturePatternDefaultValues.RectanglesDefaultInteriorSize,
						borderSize: new XYPair<int>(16, 16),
						paddingSize: TexturePatternDefaultValues.RectanglesDefaultPaddingSize,
						interiorValue: ColorVect.BlackOpaque,
						borderRightValue: new ColorVect(1f, 0f, 0f),
						borderTopValue: new ColorVect(1f, 1f, 0f),
						borderLeftValue: new ColorVect(0f, 1f, 0f),
						borderBottomValue: new ColorVect(0f, 0f, 1f),
						paddingValue: ColorVect.BlackOpaque,
						repetitions: (2, 2)
					),
					TexturePattern.Rectangles<Real>(
						interiorValue: 0f,
						borderValue: 1f,
						paddingValue: 0f,
						repetitions: (2, 2),
						borderSize: (16, 16)
					),
					TextureCreationConfig.ForEmissiveMap(Quality.VeryLow)
				);
				_ownedTextures.Add(emissiveMap);

				var clearCoatMap = texBuilder.CreateClearCoatMap(
					TexturePattern.Chequerboard<Real>(0.9f, 0.1f, repetitionCount: (4, 4)),
					TexturePattern.Chequerboard<Real>(0.05f, 0.6f, repetitionCount: (4, 4)),
					TextureCreationConfig.ForClearCoatMap(Quality.VeryLow)
				);
				_ownedTextures.Add(clearCoatMap);

				_material = factory.AssetLoader.MaterialBuilder.CreateStandardMaterial(new StandardMaterialCreationConfig {
					ColorMap = colorMap,
					NormalMap = normalMap,
					OcclusionRoughnessMetallicReflectanceMap = ormrMap,
					AnisotropyMap = anisotropyMap,
					EmissiveMap = emissiveMap,
					ClearCoatMap = clearCoatMap,
					Name = "Baked Test Material"
				});
			}
			else throw new InvalidOperationException($"No pending load for '{DisplayName}'.");

			_mesh = factory.MeshBuilder.CreateMesh(new Cuboid(CubeSize), name: "Baked Material Cuboid");
		}

		public override void BakeToFile(LocalTinyFfrFactory factory, string filePath) {
			factory.AssetBakery.Bake(_material!.Value, filePath);
		}

		public override void AddToScene(LocalTinyFfrFactory factory, Scene scene, CanvasScene canvas) {
			for (var i = 0; i < CubeCount; ++i) {
				var angle = new Angle(360f * i / CubeCount);
				var offset = Direction.Forward.RotatedBy(Direction.Up % angle) * CubeRingRadius;
				var instance = factory.ObjectBuilder.CreateModelInstance(
					_mesh!.Value,
					_material!.Value,
					Location.Origin + offset,
					Direction.Up % new Angle(37f * i),
					name: "Baked Material Instance " + i.ToString(CultureInfo.InvariantCulture)
				);
				scene.Add(instance);
				_instances.Add(instance);
			}
		}

		public override void RemoveFromScene(Scene scene, CanvasScene canvas) {
			foreach (var instance in _instances) {
				scene.Remove(instance);
				instance.Dispose();
			}
			_instances.Clear();
		}

		public override void Dispose() {
			foreach (var instance in _instances) instance.Dispose();
			_instances.Clear();

			_mesh?.Dispose();
			_mesh = null;

			if (_loadedGroup is { } group) {
				group.Dispose();
				_loadedGroup = null;
			}
			else {
				_material?.Dispose();
				foreach (var texture in _ownedTextures) texture.Dispose();
			}

			_ownedTextures.Clear();
			_material = null;
		}
	}

	sealed class FontEntry : BakedAssetEntry {
		const string SampleText = "Sphinx of black quartz, judge my vow!\nnaïve café — “quoted” 0123456789";

		Font? _font;
		FontPen? _pen;
		CanvasText? _canvasObject;
		TinyFfrAsyncOperation<Font>? _pendingOperation;

		public override string DisplayName => "Font";
		public override string BakedFileName => "font.tinyffr";

		public override void AddPendingOperationsTo(List<TinyFfrAsyncOperation> dest) {
			if (_pendingOperation is { } op) dest.Add(op);
		}

		public override void BeginLoadFromSource(LocalTinyFfrFactory factory) {
			_pendingOperation = factory.AssetLoader.LoadFontAsync(CommonTestAssets.FindAsset("EHSMB.TTF"), "EHSMB");
		}

		public override void BeginLoadFromBakedFile(LocalTinyFfrFactory factory, string filePath) {
			_pendingOperation = factory.AssetLoader.LoadBakedFontAsync(filePath);
		}

		public override void CompletePendingLoad(LocalTinyFfrFactory factory) {
			if (_pendingOperation is not { } op) throw new InvalidOperationException($"No pending load for '{DisplayName}'.");
			_font = op.GetResultAndDisposeOperation();
			_pendingOperation = null;
			_pen = _font.Value.CreatePen(BuiltInFontPenStyle.WhiteWithOutline);
		}

		public override void BakeToFile(LocalTinyFfrFactory factory, string filePath) {
			factory.AssetBakery.Bake(_font!.Value, filePath);
		}

		public override void AddToScene(LocalTinyFfrFactory factory, Scene scene, CanvasScene canvas) {
			var canvasObject = canvas.Add(SampleText, _pen!.Value, TextJustification.Right);
			canvasObject.SetPlacementFraction(Orientation2D.DownRight, (0.012f, 0.06f), 0.030f);
			_canvasObject = canvasObject;
		}

		public override void RemoveFromScene(Scene scene, CanvasScene canvas) {
			_canvasObject?.Dispose();
			_canvasObject = null;
		}

		public override void Dispose() {
			_canvasObject?.Dispose();
			_canvasObject = null;
			_pen?.Dispose();
			_pen = null;
			_font?.Dispose();
			_font = null;
		}
	}

	sealed class MeshEntry : BakedAssetEntry {
		const float MeshScaling = 0.016f;

		Mesh? _mesh;
		Material? _material;
		ModelInstance? _instance;
		ResourceGroup? _loadedGroup;
		TinyFfrAsyncOperation<ResourceGroup>? _pendingSourceOperation;
		TinyFfrAsyncOperation<Mesh>? _pendingBakedOperation;

		public override string DisplayName => "Skeletal Mesh";
		public override string BakedFileName => "mesh.tinyffr";

		public override void AddPendingOperationsTo(List<TinyFfrAsyncOperation> dest) {
			if (_pendingSourceOperation is { } sourceOp) dest.Add(sourceOp);
			if (_pendingBakedOperation is { } bakedOp) dest.Add(bakedOp);
		}

		public override void BeginLoadFromSource(LocalTinyFfrFactory factory) {
			_pendingSourceOperation = factory.AssetLoader.LoadAllAsync(CommonTestAssets.FindAsset("models/Fox.glb"));
		}

		public override void BeginLoadFromBakedFile(LocalTinyFfrFactory factory, string filePath) {
			_pendingBakedOperation = factory.AssetLoader.LoadBakedMeshAsync(filePath);
		}

		public override void CompletePendingLoad(LocalTinyFfrFactory factory) {
			if (_pendingSourceOperation is { } sourceOp) {
				var group = sourceOp.GetResultAndDisposeOperation();
				_pendingSourceOperation = null;
				_loadedGroup = group;
				_mesh = group.Meshes[0];
			}
			else if (_pendingBakedOperation is { } bakedOp) {
				_mesh = bakedOp.GetResultAndDisposeOperation();
				_pendingBakedOperation = null;
			}
			else throw new InvalidOperationException($"No pending load for '{DisplayName}'.");

			_material = factory.AssetLoader.MaterialBuilder.CreateTestMaterial();
		}

		public override void BakeToFile(LocalTinyFfrFactory factory, string filePath) {
			factory.AssetBakery.Bake(_mesh!.Value, filePath);
		}

		public override void AddToScene(LocalTinyFfrFactory factory, Scene scene, CanvasScene canvas) {
			var instance = factory.ObjectBuilder.CreateModelInstance(
				_mesh!.Value,
				_material!.Value,
				new Location(0f, -1.1f, 2.4f),
				Direction.Up % new Angle(180f),
				new Vect(MeshScaling, MeshScaling, MeshScaling),
				"Baked Mesh Instance"
			);
			scene.Add(instance);
			_instance = instance;
		}

		public override void RemoveFromScene(Scene scene, CanvasScene canvas) {
			if (_instance is { } instance) {
				scene.Remove(instance);
				instance.Dispose();
			}
			_instance = null;
		}

		public override void Update(float totalSeconds) {
			if (_instance is not { } instance || _mesh is not { } mesh) return;
			if (mesh.Animations.Count == 0) return;
			instance.GetAnimationPlayer(mesh.Animations[0]).SetTimePoint(totalSeconds, AnimationWrapStyle.Loop);
		}

		public override void Dispose() {
			_instance?.Dispose();
			_instance = null;
			_material?.Dispose();
			_material = null;

			if (_loadedGroup is { } group) {
				group.Dispose();
				_loadedGroup = null;
			}
			else _mesh?.Dispose();

			_mesh = null;
		}
	}

	static BakedAssetEntry[] CreateEntries() => new BakedAssetEntry[] {
		new BackdropTextureEntry(),
		new FileTextureEntry(),
		new BuiltInTextureEntry(),
		new AnisotropyMapEntry(),
		new MaterialEntry(),
		new FontEntry(),
		new MeshEntry()
	};

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		var entries = CreateEntries();
		var bakedDir = SetUpCleanTestDir(BakedAssetDirName);
		Console.WriteLine($"Baked asset directory: {bakedDir}");

		var normalMilliseconds = new double?[entries.Length];
		var prebakedMilliseconds = new double?[entries.Length];
		var bakedFileSizes = new long[entries.Length];
		for (var i = 0; i < entries.Length; ++i) bakedFileSizes[i] = -1L;

		var allPendingOperations = new List<TinyFfrAsyncOperation>();
		var entryPendingOperations = new List<TinyFfrAsyncOperation>();
		var entryHasCompleted = new bool[entries.Length];
		var entryElapsedMilliseconds = new double[entries.Length];

		using var factory = new LocalTinyFfrFactory(assetBakeryConfig: new AssetBakeryConfig { Enabled = true });
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Asset Baking Test | SPACE = toggle normal/prebaked load | ESC = quit");
		using var camera = factory.CameraBuilder.CreateCamera();
		using var scene = factory.SceneBuilder.CreateScene();
		using var sceneRenderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		using var canvas = factory.SceneBuilder.CreateCanvasScene();
		using var canvasRenderer = factory.RendererBuilder.CreateRenderer(canvas, window);
		using var compositor = factory.RendererBuilder.CreateCompositor(window);
		compositor.Add(sceneRenderer, RenderCompositionType.Standard);
		compositor.Add(canvasRenderer, RenderCompositionType.RetainPreviousScenes);

		using var font = factory.AssetLoader.LoadFont(BuiltInFont.Monospace);
		using var pen = font.CreatePen(BuiltInFontPenStyle.WhiteWithOutline);
		using var metricsDisplay = canvas.Add("", pen, TextJustification.Left);
		using var statusDisplay = canvas.Add("", pen, TextJustification.Left);
		metricsDisplay.SetPlacementFraction(Orientation2D.UpLeft, (0.012f, 0.015f), 0.024f);
		statusDisplay.SetPlacementFraction(Orientation2D.DownLeft, (0.012f, 0.015f), 0.024f);

		var currentPhase = LoadPhase.Normal;
		var loadInProgress = false;
		var loadStartTimestamp = 0L;

		void RefreshMetrics() {
			var metricsText = BuildMetricsText(currentPhase, entries, normalMilliseconds, prebakedMilliseconds, bakedFileSizes);
			metricsDisplay.SetText(metricsText, TextJustification.Left);
			Console.WriteLine(metricsText);
		}

		void BeginPhaseLoad() {
			for (var i = 0; i < entries.Length; ++i) {
				entryHasCompleted[i] = false;
				entryElapsedMilliseconds[i] = 0d;
				if (currentPhase == LoadPhase.Normal) entries[i].BeginLoadFromSource(factory);
				else entries[i].BeginLoadFromBakedFile(factory, Path.Combine(bakedDir, entries[i].BakedFileName));
			}
			loadStartTimestamp = Stopwatch.GetTimestamp();
			loadInProgress = true;
		}

		void FinishPhaseLoad() {
			for (var i = 0; i < entries.Length; ++i) {
				var completionStartTimestamp = Stopwatch.GetTimestamp();
				entries[i].CompletePendingLoad(factory);
				entryElapsedMilliseconds[i] += Stopwatch.GetElapsedTime(completionStartTimestamp).TotalMilliseconds;

				var filePath = Path.Combine(bakedDir, entries[i].BakedFileName);
				if (currentPhase == LoadPhase.Normal) {
					normalMilliseconds[i] = entryElapsedMilliseconds[i];
					entries[i].BakeToFile(factory, filePath);
				}
				else prebakedMilliseconds[i] = entryElapsedMilliseconds[i];

				bakedFileSizes[i] = File.Exists(filePath) ? new FileInfo(filePath).Length : -1L;
			}
			foreach (var entry in entries) entry.AddToScene(factory, scene, canvas);
			loadInProgress = false;
			RefreshMetrics();
		}

		try {
			RefreshMetrics();
			BeginPhaseLoad();

			using var loop = factory.ApplicationLoopBuilder.CreateLoop();
			while (!loop.Input.UserQuitRequested && loop.TotalIteratedTime < MaxSceneDuration && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
				_ = loop.IterateOnce();

				if (loadInProgress) {
					allPendingOperations.Clear();
					for (var i = 0; i < entries.Length; ++i) {
						entryPendingOperations.Clear();
						entries[i].AddPendingOperationsTo(entryPendingOperations);
						allPendingOperations.AddRange(entryPendingOperations);

						if (entryHasCompleted[i]) continue;
						var entryIsComplete = true;
						foreach (var op in entryPendingOperations) {
							if (op.IsCompleted) continue;
							entryIsComplete = false;
							break;
						}
						if (!entryIsComplete) continue;

						entryHasCompleted[i] = true;
						entryElapsedMilliseconds[i] = Stopwatch.GetElapsedTime(loadStartTimestamp).TotalMilliseconds;
					}

					var stats = TinyFfrAsyncOperation.GetCompletionStats(allPendingOperations);
					statusDisplay.SetText(
						$"Loading {DescribePhase(currentPhase)}: {stats.CompletedCount} of {stats.OperationCount} complete " +
						$"[{PercentageUtils.ConvertFractionToPercentageString(stats.CompletedFraction, "N0")}]",
						TextJustification.Left
					);

					if (stats.CompletedCount == stats.OperationCount) FinishPhaseLoad();
				}
				else {
					var nextPhase = currentPhase == LoadPhase.Normal ? LoadPhase.Prebaked : LoadPhase.Normal;
					statusDisplay.SetText($"Idle - press SPACE to reload as {DescribePhase(nextPhase)}", TextJustification.Left);

					if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) {
						foreach (var entry in entries) entry.RemoveFromScene(scene, canvas);
						foreach (var entry in entries) entry.Dispose();
						currentPhase = nextPhase;
						BeginPhaseLoad();
					}
				}

				camera.SetViewAndUpDirection(
					Direction.Forward.RotatedBy(Direction.Up % (12f * (float) loop.TotalIteratedTime.TotalSeconds)),
					Direction.Up
				);
				if (!loadInProgress) {
					foreach (var entry in entries) entry.Update((float) loop.TotalIteratedTime.TotalSeconds);
				}

				compositor.RenderAll();
			}
		}
		finally {
			foreach (var entry in entries) entry.RemoveFromScene(scene, canvas);
			foreach (var entry in entries) entry.Dispose();
		}
	}

	static string DescribePhase(LoadPhase phase) => phase == LoadPhase.Normal ? "NORMAL" : "PREBAKED";

	static string BuildMetricsText(LoadPhase currentPhase, BakedAssetEntry[] entries, double?[] normalMilliseconds, double?[] prebakedMilliseconds, long[] bakedFileSizes) {
		var nameColumnWidth = "TOTAL".Length;
		foreach (var entry in entries) nameColumnWidth = Int32.Max(nameColumnWidth, entry.DisplayName.Length);

		var builder = new StringBuilder();
		builder.Append("ASSET BAKING TEST - CURRENTLY SHOWING ").Append(DescribePhase(currentPhase)).Append(" LOAD").Append('\n');

		var totalNormalMs = 0d;
		var totalPrebakedMs = 0d;
		var allNormalTimesKnown = true;
		var allPrebakedTimesKnown = true;

		for (var i = 0; i < entries.Length; ++i) {
			if (normalMilliseconds[i] is { } normalMs) totalNormalMs += normalMs;
			else allNormalTimesKnown = false;
			if (prebakedMilliseconds[i] is { } prebakedMs) totalPrebakedMs += prebakedMs;
			else allPrebakedTimesKnown = false;

			builder.Append(FormatRow(entries[i].DisplayName.PadRight(nameColumnWidth), normalMilliseconds[i], prebakedMilliseconds[i], bakedFileSizes[i]));
		}

		if (entries.Length > 1) {
			builder.Append(FormatRow(
				"TOTAL".PadRight(nameColumnWidth),
				allNormalTimesKnown ? totalNormalMs : null,
				allPrebakedTimesKnown ? totalPrebakedMs : null,
				-1L
			));
		}

		return builder.ToString();
	}

	static string FormatRow(string paddedName, double? normalMs, double? prebakedMs, long bakedFileSizeBytes) {
		var builder = new StringBuilder();
		builder.Append(paddedName).Append("  ");
		builder.Append("normal ").Append(FormatMilliseconds(normalMs));
		builder.Append("   prebaked ").Append(FormatMilliseconds(prebakedMs));

		if (normalMs is { } knownNormalMs && prebakedMs is { } knownPrebakedMs && knownPrebakedMs > 0d) {
			builder.Append("   ").Append((knownNormalMs / knownPrebakedMs).ToString("N1", CultureInfo.InvariantCulture)).Append('x');
		}

		if (bakedFileSizeBytes >= 0L) {
			builder.Append("   (").Append((bakedFileSizeBytes / (1024d * 1024d)).ToString("N1", CultureInfo.InvariantCulture)).Append(" MB)");
		}

		return builder.Append('\n').ToString();
	}

	static string FormatMilliseconds(double? milliseconds) {
		return (milliseconds is { } knownMilliseconds ? knownMilliseconds.ToString("N1", CultureInfo.InvariantCulture) : "?").PadLeft(8) + " ms";
	}
}
