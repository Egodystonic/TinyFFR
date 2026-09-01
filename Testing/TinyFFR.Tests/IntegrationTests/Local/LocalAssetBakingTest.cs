// Created on 2026-08-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Egodystonic.TinyFFR.Assets;
using Egodystonic.TinyFFR.Assets.Baking;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Text;
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
		public abstract TinyFfrAsyncOperation? PendingOperation { get; }
		public abstract void BeginLoadFromSource(LocalTinyFfrFactory factory);
		public abstract void BeginLoadFromBakedFile(LocalTinyFfrFactory factory, string filePath);
		public abstract void CompletePendingLoad();
		public abstract void BakeToFile(LocalTinyFfrFactory factory, string filePath);
		public abstract void AddToScene(Scene scene, CanvasScene canvas);
		public abstract void RemoveFromScene(Scene scene, CanvasScene canvas);
		public abstract void Dispose();
	}

	sealed class BackdropTextureEntry : BakedAssetEntry {
		BackdropTexture? _texture;
		TinyFfrAsyncOperation<BackdropTexture>? _pendingOperation;

		public override string DisplayName => "Backdrop Texture";
		public override string BakedFileName => "backdrop_texture.tinyffr";
		public override TinyFfrAsyncOperation? PendingOperation => _pendingOperation is { } op ? (TinyFfrAsyncOperation) op : null;

		public override void BeginLoadFromSource(LocalTinyFfrFactory factory) {
			_pendingOperation = factory.AssetLoader.LoadBackdropTextureAsync(CommonTestAssets.FindAsset(KnownTestAsset.CloudsHdr));
		}

		public override void BeginLoadFromBakedFile(LocalTinyFfrFactory factory, string filePath) {
			_pendingOperation = factory.AssetLoader.LoadBakedBackdropTextureAsync(filePath);
		}

		public override void CompletePendingLoad() {
			if (_pendingOperation is not { } op) throw new InvalidOperationException($"No pending load for '{DisplayName}'.");
			_texture = op.GetResultAndDisposeOperation();
			_pendingOperation = null;
		}

		public override void BakeToFile(LocalTinyFfrFactory factory, string filePath) {
			factory.AssetBakery.Bake(_texture!.Value, filePath);
		}

		public override void AddToScene(Scene scene, CanvasScene canvas) => scene.SetBackdrop(_texture!.Value, 1f);

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

		public override TinyFfrAsyncOperation? PendingOperation => _pendingOperation is { } op ? (TinyFfrAsyncOperation) op : null;

		public override void BeginLoadFromSource(LocalTinyFfrFactory factory) {
			_pendingOperation = DispatchSourceLoad(factory);
		}

		public override void BeginLoadFromBakedFile(LocalTinyFfrFactory factory, string filePath) {
			_pendingOperation = factory.AssetLoader.LoadBakedTextureAsync(filePath);
		}

		public override void CompletePendingLoad() {
			if (_pendingOperation is not { } op) throw new InvalidOperationException($"No pending load for '{DisplayName}'.");
			_texture = op.GetResultAndDisposeOperation();
			_pendingOperation = null;
		}

		public override void BakeToFile(LocalTinyFfrFactory factory, string filePath) {
			factory.AssetBakery.Bake(_texture!.Value, filePath);
		}

		public override void AddToScene(Scene scene, CanvasScene canvas) {
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

	static BakedAssetEntry[] CreateEntries() => new BakedAssetEntry[] {
		new BackdropTextureEntry(),
		new FileTextureEntry(),
		new BuiltInTextureEntry(),
		new AnisotropyMapEntry()
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

		var pendingOperations = new TinyFfrAsyncOperation[entries.Length];
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
				entries[i].CompletePendingLoad();

				var filePath = Path.Combine(bakedDir, entries[i].BakedFileName);
				if (currentPhase == LoadPhase.Normal) {
					normalMilliseconds[i] = entryElapsedMilliseconds[i];
					entries[i].BakeToFile(factory, filePath);
				}
				else prebakedMilliseconds[i] = entryElapsedMilliseconds[i];

				bakedFileSizes[i] = File.Exists(filePath) ? new FileInfo(filePath).Length : -1L;
			}
			foreach (var entry in entries) entry.AddToScene(scene, canvas);
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
					for (var i = 0; i < entries.Length; ++i) {
						pendingOperations[i] = entries[i].PendingOperation!.Value;
						if (entryHasCompleted[i] || !pendingOperations[i].IsCompleted) continue;
						entryHasCompleted[i] = true;
						entryElapsedMilliseconds[i] = Stopwatch.GetElapsedTime(loadStartTimestamp).TotalMilliseconds;
					}

					var stats = TinyFfrAsyncOperation.GetCompletionStats(pendingOperations);
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
