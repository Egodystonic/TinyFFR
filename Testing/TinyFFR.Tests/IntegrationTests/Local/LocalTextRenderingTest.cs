// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Numerics;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Testing;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalTextRenderingTest {
	const float TextHeight = 0.1f;
	const float TextWidth = 1.5f;
	
	[SetUp]
	public void SetUpTest() {
		
	}

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Text Rendering Test (Space / 1 / 2 / 3 / 4 / A / S)");
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		using var font = factory.AssetLoader.LoadFont(CommonTestAssets.FindAsset("DejaVuSans.ttf"));
		var pen1 = font.CreatePen(ColorVect.WhiteOpaque, ColorVect.BlackOpaque, 1f);
		var pen2 = font.CreatePen(new ColorVect(1f, 1f, 0f, 0.3f, true), new ColorVect(0f, 1f, 1f, 0.75f, true), 0.5f);
		var pen3 = font.CreatePen(ColorVect.BlackOpaque, new ColorVect(1f, 1f, 1f, 0.04f, true));
		var pen4 = font.CreatePen(ColorVect.BlackTransparent, ColorVect.RedOpaque, 0.35f, ColorVect.GreenOpaque);
		var strings = new[] {
			// ReSharper disable StringLiteralTypo
			font.CreateString("AaBbÉé Ññ Œœ Δλ ΣΩ \"…\"—•† ‰ ™€ x⁴H₂O → ∑≠√∞ ┌┐■●◆ ▓ � ⁂"), // Smoke test (last char deliberately chosen to not be included -- tests fallback to �)
			font.CreateString("AVAST To Wave, Ye Types LTa VA WA T. rn cl — jpqgy bdfklh"), // Kerning test
			font.CreateString("Café résumé naïve · Zürich Køben Łódź Škoda piñata façade ¡Añejo! ¿Qué? Œuvre Ægir þorn ð ß · āčēģīķļņōšūž ąężźćń őű ğıİ"), // Latin test
			font.CreateString("ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩ αβγδεζηθικλμνξοπρςστυφχψω"), // Greek test
			font.CreateString("\"curly\" 'singles' – en — em … • † ‡ ‰ ‹s› «d» ™ €99 ©®§¶"), // Punctuation test
			font.CreateString("x⁴+y⁵=z⁶ aⁿ⁺¹ ⁰¹²³⁴⁵⁶⁷⁸⁹ · H₂O CO₂ C₆H₁₂O₆ ₀₁₂₃₄₅₆₇₈₉"), // Super/subscript test
			font.CreateString("← ↑ → ↓ ↔ ↕ ↖ ↗ ↘ ↙ ⇐ ⇒ ⇑ ⇓ ⇔ ↩ ↪"), // Arrow test
			font.CreateString("∀x: x ≤ y ∧ x ≠ ∅ · ∑ ∏ ∫ √ ∞ ∂ ∇ ∈ ∉ ⊂ ⊃ ∪ ∩ ≈ ≡ ≥ ± ∓ ⊕ ⊗ ⊥ ∠ ∴ ∝"), // Maths test
			font.CreateString("■ □ ▲ △ ▶ ▷ ▼ ▽ ◀ ◁ ● ○ ◆ ◇ ◢ ◣ ▪ ▫ ◉ ◐ ◑ ┌──┬──┐  ╔══╦══╗   ░▒▓█ ▁▂▃▄▅▆▇█"), // Shapes test
			font.CreateString("■■■"), // Anchor test
			font.CreateString("Short\nA much longer middle line\nMedium length line", TextJustification.Left), // Multi-line left-justify test
			font.CreateString("Short\nA much longer middle line\nMedium length line"), // Multi-line centre-justify test (default)
			font.CreateString("Short\nA much longer middle line\nMedium length line", TextJustification.Right), // Multi-line right-justify test
			font.CreateString("Trailing break + kerning per line:\nAVAST To Wave\nYe Types LTa VA\n"), // Multi-line trailing-break test
			font.CreateString("\nStarting break"), // Multi-line starting-break test
			// ReSharper restore StringLiteralTypo
		};
		using var scene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
		using var camController = camera.CreateController<FreeFlyingCameraController>();
		using var textInstance = factory.ObjectBuilder.CreateTextInstance(pen1, strings[0], position: new(0f, 0f, 1f), textInstanceHeight: TextHeight);
		var anchors = new[] {
			Orientation2D.UpRight, Orientation2D.Up, Orientation2D.UpLeft,
			Orientation2D.Right, Orientation2D.None, Orientation2D.Left,
			Orientation2D.DownRight, Orientation2D.Down, Orientation2D.DownLeft,
		};
		var curAnchorIdx = Array.IndexOf(anchors, Orientation2D.None);
		var curStringIdx = 0;
		var useDefaultScalingParams = true;
		scene.Add(textInstance.UnderlyingModelInstance);
		
		void UpdateInstanceTransform() {
			if (useDefaultScalingParams) {
				textInstance.SetTransform(camera.Position + camera.ViewDirection * 1f, -camera.ViewDirection, TextHeight, uprightDirection: camera.UpDirection, positionAnchor: anchors[curAnchorIdx]);
			}
			else {
				textInstance.SetTransform(camera.Position + camera.ViewDirection * 1f, -camera.ViewDirection, textInstanceHeight: null, textInstanceWidth: TextWidth, uprightDirection: camera.UpDirection, positionAnchor: anchors[curAnchorIdx], rescaleHeightAccordingToLineCount: false);
			}
		}
		
		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();

			DefaultCameraInputHandler.TickKbm(loop.Input.KeyboardAndMouse, camController, dt, window);
			DefaultCameraInputHandler.TickGamepad(loop.Input.GameControllersCombined, camController, dt);
			DefaultCameraInputHandler.Progress(camController, dt);
			
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) {
				++curStringIdx;
				if (curStringIdx >= strings.Length) curStringIdx = 0;
				textInstance.String = strings[curStringIdx];
				UpdateInstanceTransform();
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow1)) textInstance.SetPen(pen1);
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow2)) textInstance.SetPen(pen2);
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow3)) textInstance.SetPen(pen3);
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.NumberRow4)) textInstance.SetPen(pen4);
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.A)) {
				++curAnchorIdx;
				if (curAnchorIdx >= anchors.Length) curAnchorIdx = 0;
				UpdateInstanceTransform();
			}
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.S)) {
				useDefaultScalingParams = !useDefaultScalingParams;
				UpdateInstanceTransform();
			}
			
			renderer.Render();
		}
		
		scene.Remove(textInstance.UnderlyingModelInstance);
	}
}