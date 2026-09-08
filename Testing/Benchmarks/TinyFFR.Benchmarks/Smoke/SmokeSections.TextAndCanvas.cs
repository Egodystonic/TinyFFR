// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Benchmarks.Harness;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Benchmarks.Smoke;

static partial class SmokeSections {
	const string SampleText = "TinyFFR benchmark smoke text";
	const string MultiLineSampleText = "Short\nA much longer middle line\nMedium length line";
	const float TextHeight = 0.1f;

	public static void Fonts() {
		using var builtInFont = Factory.AssetLoader.LoadFont(BuiltInFont.Default, "Benchmark Built In Font");
		using var loadedFont = Factory.AssetLoader.LoadFont(BenchmarkAssets.SansFont, "Benchmark Loaded Font");

		var plainPen = loadedFont.CreatePen(ColorVect.WhiteOpaque);
		var outlinedPen = loadedFont.CreatePen(ColorVect.WhiteOpaque, ColorVect.BlackOpaque, 1f);
		var backgroundPen = loadedFont.CreatePen(ColorVect.BlackOpaque, new ColorVect(1f, 1f, 1f, 0.5f, true));
		var fullPen = loadedFont.CreatePen(ColorVect.BlackTransparent, StandardColor.Red, 0.35f, StandardColor.Green);
		var builtInPen = loadedFont.CreatePen(BuiltInFontPenStyle.Default);

		var singleLine = loadedFont.CreateString(SampleText);
		var leftJustified = loadedFont.CreateString(MultiLineSampleText, TextJustification.Left);
		var rightJustified = loadedFont.CreateString(MultiLineSampleText, TextJustification.Right);
	}

	public static void TextInstances() {
		using var font = Factory.AssetLoader.LoadFont(BenchmarkAssets.SansFont, "Benchmark Text Instance Font");
		var pen = font.CreatePen(ColorVect.WhiteOpaque, ColorVect.BlackOpaque, 1f);
		var text = font.CreateString(SampleText);

		using var scene = Factory.SceneBuilder.CreateScene(backdropColor: StandardColor.Black, name: "Benchmark Text Scene");
		using var textInstance = Factory.ObjectBuilder.CreateTextInstance(pen, text, new Location(0f, 0f, 1f), layout: new TextLayout(TextHeight), name: "Benchmark Text Instance");
		using var lockedTextInstance = Factory.ObjectBuilder.CreateCameraLockedTextInstance(pen, text, new Location(0f, 0.5f, 1f), layout: new TextLayout(TextHeight), name: "Benchmark Camera Locked Text Instance");

		scene.Add(textInstance);
		scene.Add(lockedTextInstance);

		textInstance.SetTransform(new Location(0f, 0f, 1.2f), Direction.Backward, uprightDirection: Direction.Up, new TextLayout(TextHeight, Orientation2D.UpLeft));
		textInstance.String = font.CreateString(MultiLineSampleText, TextJustification.Left);
		textInstance.SetPen(font.CreatePen(StandardColor.Yellow));

		scene.RemoveAll();
	}

	public static void Quads() {
		using var quadMesh = Factory.MeshBuilder.CreateQuadMesh(name: "Benchmark Quad Mesh");
		using var colorMap = Factory.TextureBuilder.CreateColorMap(BenchmarkAssets.ColorPattern, includeAlpha: false, "Benchmark Quad Color Map");
		using var material = Factory.MaterialBuilder.CreateLightingIgnoringMaterial(colorMap, name: "Benchmark Quad Material");
		using var scene = Factory.SceneBuilder.CreateScene(backdropColor: StandardColor.Black, name: "Benchmark Quad Scene");

		var quads = Allocator.GetSharedScratchList<QuadInstance>();
		var lockedQuads = Allocator.GetSharedScratchList<CameraLockedQuadInstance>();

		try {
			for (var pass = 0; pass < SmokeWorkload.QuadPassCount; ++pass)
			for (var i = 0; i < SmokeWorkload.QuadCount; ++i) {
				var quad = Factory.ObjectBuilder.CreateQuadInstance(quadMesh, material, new Location(i * 0.1f, 0f, 1f), (0.5f, 0.5f), Direction.Backward, Direction.Up, name: "Benchmark Quad Instance");
				var lockedQuad = Factory.ObjectBuilder.CreateCameraLockedQuadInstance(quadMesh, material, new Location(0.5f, i * 0.1f, 1f), (0.25f, 0.25f), name: "Benchmark Camera Locked Quad Instance");
				scene.Add(quad);
				scene.Add(lockedQuad);
				quads.Add(quad);
				lockedQuads.Add(lockedQuad);
			}
		}
		finally {
			scene.RemoveAll();
			for (var i = 0; i < quads.Count; ++i) {
				quads[i].Dispose();
				lockedQuads[i].Dispose();
			}
		}
	}

	public static void CanvasScenes() {
		using var font = Factory.AssetLoader.LoadFont(BenchmarkAssets.SansFont, "Benchmark Canvas Font");
		var pen = font.CreatePen(ColorVect.WhiteOpaque);
		using var canvasTexture = Factory.TextureBuilder.CreateCanvasTexture(BenchmarkAssets.ColorPattern, includeAlpha: true, "Benchmark Canvas Texture");

		using var canvas = Factory.SceneBuilder.CreateCanvasScene("Benchmark Canvas Scene");
		canvas.SetBackgroundColor(new ColorVect(0f, 0f, 0f, 0.5f));

		var texture = canvas.Add(canvasTexture);
		var text = canvas.Add(SampleText, pen);

		texture.SetPlacementFraction(Orientation2D.UpLeft, (0.05f, 0.05f), (0.3f, 0.3f));
		texture.SetLayer(1);
		text.SetPlacementFraction(Orientation2D.Down, (0f, 0.05f), 0.15f);
		text.SetLayer(2);
		text.SetDockParent<CanvasTexture>(texture);
		_ = canvas.SizePixels;
		_ = canvas.ConvertFractionToPixels((0.5f, 0.5f));

		using var renderer = Target.CreateRenderer(canvas);
		renderer.RenderAndWaitForGpu();

		text.Dispose();
		texture.Dispose();
	}
}
