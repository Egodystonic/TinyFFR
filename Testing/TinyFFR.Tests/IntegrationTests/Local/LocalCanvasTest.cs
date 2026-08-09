// Created on 2026-08-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalCanvasTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Canvas Test");

		using var scene = factory.SceneBuilder.CreateScene();
		using var camera = factory.CameraBuilder.CreateCamera();
		using var cameraController = camera.CreateController<OrbitalCameraController>();
		cameraController.MaxDistance = null;
		cameraController.Distance = 2f;
		cameraController.Target = Location.Origin;
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		using var primitive = scene.AddPrimitiveShape(new PositionedRotatedCuboid(0.5f, 0.5f, 0.5f, Location.Origin, 45f % Direction.Right + 45f % Direction.Down));

		using var canvas = factory.SceneBuilder.CreateCanvasScene();
		using var canvasRenderer = factory.RendererBuilder.CreateRenderer(canvas, window);
		
		using var uvTex = factory.AssetLoader.LoadTexture(factory.AssetLoader.BuiltInTexturePaths.UvTestingTexture, new TextureCreationConfig { IsLinearColorspace = true }, new TextureReadConfig { ForceWAlphaChannelPresence = true, IncludeWAlphaChannel = true });
		using var blendTex = factory.TextureBuilder.CreateCanvasTexture(
			TexturePattern.GradientHorizontal(ColorVect.RedOpaque, ColorVect.RedTransparent),
			includeAlpha: true
		);
		using var canvasTex = canvas.Add(uvTex);
		var arrowsAdjustPixelPos = true;
		var sizeStates = new (float? Fraction, int? Pixels, string Label)[] {
			(0.05f, null, "Frac 5%"),
			(0.2f, null, "Frac 20%"),
			(null, null, "Frac <null>"),
			(null, 100, "Px 100"),
			(null, 600, "Px 600"),
			(null, null, "Px <null>")
		};
		canvasTex.SetBlendTexture(blendTex);
		var layerStates = new[] { -1, 0, 1 };
		var blendDistanceStates = new[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
		var fillStates = new[] { 1f, 0.75f, 0.5f, 0.25f, 0f };
		const int MaxTextureCropStep = 8;
		const float TextureCropStepSize = 0.05f;
		var widthStateIdx = 0;
		var heightStateIdx = 2;
		var layerStateIdx = 1;
		var blendDistanceStateIdx = 0;
		var fillStateIdx = 0;
		var textureCropStep = 0;
		canvasTex.CanvasAnchor = Orientation2D.None;
		canvasTex.PositionPixels = XYPair<int>.Zero;
		canvasTex.Rotation = Angle.Zero;
		ApplyWidthState();
		ApplyHeightState();
		ApplyLayerState();
		ApplyTextureCrop();
		ApplyBlendDistance();
		ApplyFillFraction();

		float TextureCropExtent() => 1f - textureCropStep * TextureCropStepSize * 2f;
		void ApplyBlendDistance() {
			canvasTex.SetBlendTextureDistance(blendDistanceStates[blendDistanceStateIdx]);
		}
		void ApplyFillFraction() {
			canvasTex.FillFraction = new XYPair<float>(fillStates[fillStateIdx], 1f);
		}
		void ApplyWidthState() {
			canvasTex.WidthFraction = sizeStates[widthStateIdx].Fraction;
			canvasTex.WidthPixels = sizeStates[widthStateIdx].Pixels;
		}
		void ApplyHeightState() {
			canvasTex.HeightFraction = sizeStates[heightStateIdx].Fraction;
			canvasTex.HeightPixels = sizeStates[heightStateIdx].Pixels;
		}
		void ApplyLayerState() {
			canvasTex.Layer = layerStates[layerStateIdx];
		}
		void ApplyTextureCrop() {
			canvasTex.SetTextureExtentFraction(new XYPair<float>(TextureCropExtent()));
		}

		Orientation2D Cycle(Orientation2D o) {
			var values = Enum.GetValues<Orientation2D>();
			var curIdx = Array.IndexOf(values, o);
			if (curIdx == (values.Length - 1)) return values[0];
			else return values[curIdx + 1];
		}

		using var font = factory.AssetLoader.LoadFont();
		using var fontPen = font.CreatePen(BuiltInFontPenStyle.Default);
		
		var accumulatedText = "";
		using var textInputCanvasText = canvas.Add("[Return] Text Input Off", fontPen);
		textInputCanvasText.SetPlacementFraction(
			Orientation2D.UpLeft,
			(0.01f, 0.01f),
			0.02f
		);
		
		string BuildTextureOffsetText() {
			return "[Arrows / Ctrl] Position " + 
				(arrowsAdjustPixelPos ? "→" : "") + "Px" + canvasTex.PositionPixels + " " + 
				(arrowsAdjustPixelPos ? "" : "→") + "Frac" + 
				PercentageUtils.ConvertFractionToPercentageString(canvasTex.PositionFraction, "N0", null); 
		}
		using var textureOffsetText = canvas.Add(BuildTextureOffsetText(), fontPen);
		textureOffsetText.SetPlacementFraction(
			Orientation2D.UpLeft,
			(0.01f, 0.05f),
			0.02f	
		);
		
		string BuildTextureAnchorText() {
			return "[C / O] Anchors " + canvasTex.CanvasAnchor + " / " + (canvasTex.ObjectAnchor?.ToString() ?? "<null>"); 
		}
		using var textureAnchorText = canvas.Add(BuildTextureAnchorText(), fontPen);
		textureAnchorText.SetPlacementFraction(
			Orientation2D.UpLeft,
			(0.01f, 0.09f),
			0.02f
		);

		string BuildTextureSizeText() {
			return "[W / H] Size " + sizeStates[widthStateIdx].Label + " x " + sizeStates[heightStateIdx].Label;
		}
		using var textureSizeText = canvas.Add(BuildTextureSizeText(), fontPen);
		textureSizeText.SetPlacementFraction(
			Orientation2D.UpLeft,
			(0.01f, 0.13f),
			0.02f
		);

		string BuildTextureRotationText() {
			return "[R Held] Rotation " + canvasTex.Rotation.ToString("N0", null);
		}
		using var textureRotationText = canvas.Add(BuildTextureRotationText(), fontPen);
		textureRotationText.SetPlacementFraction(
			Orientation2D.UpLeft,
			(0.01f, 0.17f),
			0.02f
		);

		string BuildTextureLayerText() {
			return "[L] Layer " + canvasTex.Layer;
		}
		using var textureLayerText = canvas.Add(BuildTextureLayerText(), fontPen);
		textureLayerText.SetPlacementFraction(
			Orientation2D.UpLeft,
			(0.01f, 0.21f),
			0.02f
		);

		string BuildTextureCropText() {
			return "[PgUp / PgDn] Tex Offset " +
				PercentageUtils.ConvertFractionToPercentageString(canvasTex.TextureOffsetFraction, "N0", null) + " Extent " +
				PercentageUtils.ConvertFractionToPercentageString(canvasTex.TextureExtentFraction, "N0", null);
		}
		using var textureCropText = canvas.Add(BuildTextureCropText(), fontPen);
		textureCropText.SetPlacementFraction(
			Orientation2D.UpLeft,
			(0.01f, 0.25f),
			0.02f
		);

		string BuildBlendDistanceText() {
			return "[B] Blend Distance " + PercentageUtils.ConvertFractionToPercentageString(blendDistanceStates[blendDistanceStateIdx]);
		}
		using var blendDistanceText = canvas.Add(BuildBlendDistanceText(), fontPen);
		blendDistanceText.SetPlacementFraction(
			Orientation2D.UpLeft,
			(0.01f, 0.29f),
			0.02f
		);

		string BuildFillFractionText() {
			return "[F] Fill " + PercentageUtils.ConvertFractionToPercentageString(canvasTex.FillFraction);
		}
		using var fillFractionText = canvas.Add(BuildFillFractionText(), fontPen);
		fillFractionText.SetPlacementFraction(
			Orientation2D.UpLeft,
			(0.01f, 0.33f),
			0.02f
		);

		using var compositor = factory.RendererBuilder.CreateCompositor(window);
		compositor.Add(renderer, RenderCompositionType.Standard);
		compositor.Add(canvasRenderer, RenderCompositionType.RetainPreviousScenes);

		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();
			var kbm = loop.Input.KeyboardAndMouse;
			
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.Return)) {
				// ReSharper disable once AssignmentInConditionalExpression Deliberate actually
				if (loop.EnableInputTextTranscription = !loop.EnableInputTextTranscription) {
					textInputCanvasText.SetText("[Return] Text Input On | ");
					accumulatedText = "";
				}
				else {
					textInputCanvasText.SetText("[Return] Text Input Off");
				}
			}
			if (loop.EnableInputTextTranscription && !kbm.TranscribedText.IsEmpty) {
				accumulatedText += kbm.TranscribedText.ToString();
				textInputCanvasText.SetText("[Return] Text Input On | " + accumulatedText);
			}
			if (loop.EnableInputTextTranscription && kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.Backspace) && accumulatedText.Length > 0) {
				accumulatedText = accumulatedText[..^1];
				textInputCanvasText.SetText("[Return] Text Input On | " + accumulatedText);
			}
			
			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowLeft)) {
				if (arrowsAdjustPixelPos) {
					canvasTex.MoveByPixels(new XYPair<int>(-100, 0).ScaledByReal(dt));
				}
				else {
					canvasTex.MoveByFraction(new XYPair<float>(-0.1f, 0f).ScaledByReal(dt));
				}
				textureOffsetText.SetText(BuildTextureOffsetText());
			}
			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowRight)) {
				if (arrowsAdjustPixelPos) {
					canvasTex.MoveByPixels(new XYPair<int>(100, 0).ScaledByReal(dt));
				}
				else {
					canvasTex.MoveByFraction(new XYPair<float>(0.1f, 0f).ScaledByReal(dt));
				}
				textureOffsetText.SetText(BuildTextureOffsetText());
			}
			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowDown)) {
				if (arrowsAdjustPixelPos) {
					canvasTex.MoveByPixels(new XYPair<int>(0, -100).ScaledByReal(dt));
				}
				else {
					canvasTex.MoveByFraction(new XYPair<float>(0f, -0.1f).ScaledByReal(dt));
				}
				textureOffsetText.SetText(BuildTextureOffsetText());
			}
			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.ArrowUp)) {
				if (arrowsAdjustPixelPos) {
					canvasTex.MoveByPixels(new XYPair<int>(0, 100).ScaledByReal(dt));
				}
				else {
					canvasTex.MoveByFraction(new XYPair<float>(0f, 0.1f).ScaledByReal(dt));
				}
				textureOffsetText.SetText(BuildTextureOffsetText());
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.RightControl)) {
				arrowsAdjustPixelPos = !arrowsAdjustPixelPos;
				textureOffsetText.SetText(BuildTextureOffsetText());
			}
			
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.C)) {
				canvasTex.CanvasAnchor = Cycle(canvasTex.CanvasAnchor);
				textureAnchorText.SetText(BuildTextureAnchorText());
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.O)) {
				if (canvasTex.ObjectAnchor == null) canvasTex.ObjectAnchor = Enum.GetValues<Orientation2D>()[0];
				else {
					canvasTex.ObjectAnchor = Cycle(canvasTex.ObjectAnchor.Value);
					if (canvasTex.ObjectAnchor == Enum.GetValues<Orientation2D>()[0]) canvasTex.ObjectAnchor = null;
				}
				textureAnchorText.SetText(BuildTextureAnchorText());
			}

			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.W)) {
				widthStateIdx = (widthStateIdx + 1) % sizeStates.Length;
				ApplyWidthState();
				textureSizeText.SetText(BuildTextureSizeText());
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.H)) {
				heightStateIdx = (heightStateIdx + 1) % sizeStates.Length;
				ApplyHeightState();
				textureSizeText.SetText(BuildTextureSizeText());
			}
			if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.R)) {
				canvasTex.RotateBy(90f * dt);
				textureRotationText.SetText(BuildTextureRotationText());
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.L)) {
				layerStateIdx = (layerStateIdx + 1) % layerStates.Length;
				ApplyLayerState();
				textureLayerText.SetText(BuildTextureLayerText());
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.PageUp) && textureCropStep < MaxTextureCropStep) {
				textureCropStep++;
				ApplyTextureCrop();
				textureCropText.SetText(BuildTextureCropText());
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.PageDown) && textureCropStep > 0) {
				textureCropStep--;
				ApplyTextureCrop();
				textureCropText.SetText(BuildTextureCropText());
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.B)) {
				blendDistanceStateIdx = (blendDistanceStateIdx + 1) % blendDistanceStates.Length;
				ApplyBlendDistance();
				blendDistanceText.SetText(BuildBlendDistanceText());
			}
			if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.F)) {
				fillStateIdx = (fillStateIdx + 1) % fillStates.Length;
				ApplyFillFraction();
				fillFractionText.SetText(BuildFillFractionText());
			}

			cameraController.Angle += dt * 30f;
			cameraController.Progress(dt);
			
			compositor.RenderAll();
		}
	}
}
