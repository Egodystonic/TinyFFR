// Created on 2026-08-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Numerics;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.DearImGui;
using Egodystonic.TinyFFR.DearImGui.Input;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Hexa.NET.ImGui;

namespace Egodystonic.TinyFFR;

static class HeadlessDpiCheck {
	const int WindowW = 200;
	const int WindowH = 100;
	const int Margin = 10;
	const int OutsideBand = 8;

	public static int Run(string? dumpPath) {
		var logicalSize = new XYPair<int>(500, 300);
		var framebufferSize = new XYPair<int>(1000, 600);

		using var factory = new LocalTinyFfrFactory();
		using var buffer = factory.RendererBuilder.CreateRenderOutputBuffer(framebufferSize);
		using var imgui = factory.SceneBuilder.CreateImGuiScene(factory);
		using var renderer = factory.RendererBuilder.CreateRenderer(imgui, buffer);
		using var loop = factory.ApplicationLoopBuilder.CreateLoop();

		var expected = new[] {
			new XYPair<int>(Margin, Margin),
			new XYPair<int>(framebufferSize.X - WindowW - Margin, Margin),
			new XYPair<int>(Margin, framebufferSize.Y - WindowH - Margin),
			new XYPair<int>(framebufferSize.X - WindowW - Margin, framebufferSize.Y - WindowH - Margin)
		};

		var capturedSize = default(XYPair<int>);
		TexelRgba32[]? captured = null;

		for (var frame = 0; frame < 5; ++frame) {
			var deltaTime = loop.IterateOnce();
			imgui.BeginFrame(deltaTime, loop.Input, logicalSize, framebufferSize);

			var displaySize = ImGui.GetIO().DisplaySize;
			var placements = new[] {
				new Vector2(Margin, Margin),
				new Vector2(displaySize.X - WindowW - Margin, Margin),
				new Vector2(Margin, displaySize.Y - WindowH - Margin),
				new Vector2(displaySize.X - WindowW - Margin, displaySize.Y - WindowH - Margin)
			};

			ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0f, 0f, 1f));
			for (var i = 0; i < placements.Length; ++i) {
				ImGui.SetNextWindowPos(placements[i]);
				ImGui.SetNextWindowSize(new Vector2(WindowW, WindowH));
				ImGui.Begin(
					"w" + i,
					ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
					| ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings
				);
				ImGui.End();
			}
			ImGui.PopStyleColor();

			imgui.EndFrame();

			if (frame == 4) {
				buffer.ReadNextFrame((size, texels) => {
					capturedSize = size;
					captured = texels.ToArray();
				}, presentFrameTopToBottom: true);
			}
			renderer.Render();
		}

		if (captured == null) {
			Console.WriteLine("FAIL: no frame was captured.");
			return 1;
		}
		Console.WriteLine($"Captured {capturedSize.X}x{capturedSize.Y} (expected {framebufferSize.X}x{framebufferSize.Y})");

		if (dumpPath != null) WritePpm(dumpPath, capturedSize, captured);

		var failures = 0;
		for (var i = 0; i < expected.Length; ++i) {
			var origin = expected[i];
			var inside = CountRed(captured, capturedSize, origin.X + 4, origin.Y + 4, WindowW - 8, WindowH - 8);
			var insideTotal = (WindowW - 8) * (WindowH - 8);
			var insideFraction = inside / (float) insideTotal;

			var outside = CountRed(captured, capturedSize, origin.X - OutsideBand, origin.Y - OutsideBand, WindowW + OutsideBand * 2, OutsideBand)
				+ CountRed(captured, capturedSize, origin.X - OutsideBand, origin.Y + WindowH, WindowW + OutsideBand * 2, OutsideBand)
				+ CountRed(captured, capturedSize, origin.X - OutsideBand, origin.Y, OutsideBand, WindowH)
				+ CountRed(captured, capturedSize, origin.X + WindowW, origin.Y, OutsideBand, WindowH);

			var geometryOk = insideFraction > 0.9f;
			var clipOk = outside == 0;
			if (!geometryOk) ++failures;
			if (!clipOk) ++failures;
			Console.WriteLine(
				$"  window {i} at ({origin.X},{origin.Y}): fill={insideFraction:P1} [{(geometryOk ? "PASS" : "FAIL")}], " +
				$"bleed={outside}px [{(clipOk ? "PASS" : "FAIL")}]"
			);
		}

		var totalRed = CountRed(captured, capturedSize, 0, 0, capturedSize.X, capturedSize.Y);
		var expectedRed = expected.Length * WindowW * WindowH;
		Console.WriteLine($"  total painted={totalRed}px, expected about {expectedRed}px");

		Console.WriteLine(failures == 0 ? "RESULT: PASS" : $"RESULT: FAIL ({failures} assertion(s))");
		return failures == 0 ? 0 : 1;
	}

	public static int RunUiDump(string dumpPath, float scale) {
		var logicalSize = new XYPair<int>(800, 600);
		var framebufferSize = new XYPair<int>((int) (800 * scale), (int) (600 * scale));

		using var factory = new LocalTinyFfrFactory();
		using var buffer = factory.RendererBuilder.CreateRenderOutputBuffer(framebufferSize);
		using var imgui = factory.SceneBuilder.CreateImGuiScene(factory);
		using var renderer = factory.RendererBuilder.CreateRenderer(imgui, buffer);
		using var loop = factory.ApplicationLoopBuilder.CreateLoop();

		var capturedSize = default(XYPair<int>);
		TexelRgba32[]? captured = null;
		var sliderValue = 2.5f;
		var checkboxValue = true;

		for (var frame = 0; frame < 6; ++frame) {
			var deltaTime = loop.IterateOnce();
			imgui.BeginFrame(deltaTime, loop.Input, logicalSize, framebufferSize);

			var displaySize = ImGui.GetIO().DisplaySize;
			ImGui.SetNextWindowPos(new Vector2(displaySize.X * 0.03f, displaySize.Y * 0.04f));
			ImGui.SetNextWindowSize(new Vector2(displaySize.X * 0.55f, displaySize.Y * 0.8f));
			ImGui.Begin("TinyFFR Controls", ImGuiWindowFlags.NoSavedSettings);
			ImGui.Text("Sharpness check: AVWij 0123");
			ImGui.SliderFloat("Rotation", ref sliderValue, 0f, 5f);
			ImGui.Checkbox("Show Demo Window", ref checkboxValue);
			ImGui.Separator();
			ImGui.Text("Scroll region (tests clipping):");
			if (ImGui.BeginChild("scrolltest", new Vector2(0f, displaySize.Y * 0.35f), ImGuiChildFlags.Borders)) {
				for (var i = 0; i < 40; ++i) ImGui.Text($"Clipped line {i}");
			}
			ImGui.EndChild();
			ImGui.End();

			imgui.EndFrame();

			if (frame == 5) {
				buffer.ReadNextFrame((size, texels) => {
					capturedSize = size;
					captured = texels.ToArray();
				}, presentFrameTopToBottom: true);
			}
			renderer.Render();
		}

		if (captured == null) {
			Console.WriteLine("FAIL: no frame captured.");
			return 1;
		}
		Console.WriteLine($"UI dump at scale {scale}: {capturedSize.X}x{capturedSize.Y}");
		WritePpm(dumpPath, capturedSize, captured);
		return 0;
	}

	public static unsafe int RunTextureChurn(int frames) {
		var logicalSize = new XYPair<int>(800, 600);
		var scales = new[] { 1f, 2f, 3f, 4f, 2f, 1.25f, 5f, 1f };

		using var factory = new LocalTinyFfrFactory();
		using var buffer = factory.RendererBuilder.CreateRenderOutputBuffer(new XYPair<int>(1600, 1200));
		using var imgui = factory.SceneBuilder.CreateImGuiScene(factory);
		using var renderer = factory.RendererBuilder.CreateRenderer(imgui, buffer);
		using var loop = factory.ApplicationLoopBuilder.CreateLoop();

		var scaleChanges = 0;
		var lastScale = -1f;
		var seenTextures = new HashSet<string>();

		for (var frame = 0; frame < frames; ++frame) {
			var scale = scales[(frame / 4) % scales.Length];
			if (scale != lastScale) {
				++scaleChanges;
				lastScale = scale;
			}
			var framebufferSize = new XYPair<int>((int) (logicalSize.X * scale), (int) (logicalSize.Y * scale));

			var deltaTime = loop.IterateOnce();
			imgui.BeginFrame(deltaTime, loop.Input, logicalSize, framebufferSize);

			var displaySize = ImGui.GetIO().DisplaySize;
			ImGui.SetNextWindowPos(new Vector2(displaySize.X * 0.05f, displaySize.Y * 0.05f));
			ImGui.SetNextWindowSize(new Vector2(displaySize.X * 0.9f, displaySize.Y * 0.9f));
			ImGui.Begin("Churn", ImGuiWindowFlags.NoSavedSettings);
			for (var line = 0; line < 20; ++line) {
				ImGui.Text($"Frame {frame} line {line}: glyphs {(char) ('A' + (frame + line) % 26)}{(char) ('a' + (frame * 3 + line) % 26)} !@#$%^&*()");
			}
			ImGui.End();

			imgui.EndFrame();

			var platformTextures = ImGui.GetPlatformIO().Textures;
			for (var t = 0; t < platformTextures.Size; ++t) {
				var td = platformTextures.Data[t];
				if (td.IsNull) continue;
				var key = $"{td.UniqueID}:{td.Width}x{td.Height}";
				if (seenTextures.Add(key)) Console.WriteLine($"  frame {frame} scale {scale}: NEW texture {key}");
				if (td.Status == ImTextureStatus.WantDestroy) Console.WriteLine($"  frame {frame}: WANT_DESTROY {key} unused={td.UnusedFrames}");
			}

			renderer.Render();
		}

		Console.WriteLine($"Survived {frames} frames across {scaleChanges} DPI scale changes with no exception.");
		Console.WriteLine("RESULT: PASS");
		return 0;
	}

	public static int RunDynamicBufferCheck() {
		using var factory = new LocalTinyFfrFactory();
		var failures = 0;

		using var buffer = factory.MeshBuilder.CreateDynamicVertexBuffer(8, 12, "Check Buffer");

		static void WriteCube(Span<MeshVertex> verts, float extent) {
			for (var i = 0; i < verts.Length; ++i) {
				verts[i] = new MeshVertex {
					Location = new Location(
						(i & 0b001) == 0 ? -extent : extent,
						(i & 0b010) == 0 ? -extent : extent,
						(i & 0b100) == 0 ? -extent : extent
					)
				};
			}
		}

		using (var lease = buffer.BorrowVerticesSpan(recalculateBoundingBoxOnLeaseDispose: true, overwriteChildMeshBoundingBoxes: false)) {
			WriteCube(lease.Span, 1f);
		}
		using (var lease = buffer.BorrowIndicesSpan(recalculateBoundingBoxOnLeaseDispose: false, overwriteChildMeshBoundingBoxes: false)) {
			for (var i = 0; i < lease.Span.Length; ++i) lease.Span[i] = (ushort) (i % 8);
		}

		using var viewA = buffer.CreateMesh();
		failures += ExpectExtent(viewA.BoundingBox, 1.015f, "new view picks up recalculated box");

		using (var lease = buffer.BorrowVerticesSpanReadOnly()) {
			failures += Expect(lease.Span.Length == 8, "readonly lease spans the whole buffer");
			failures += Expect(lease.Span[7].Location == new Location(1f, 1f, 1f), "readonly lease reads back written data");
		}

		using (var lease = buffer.BorrowVerticesSpan(recalculateBoundingBoxOnLeaseDispose: true, overwriteChildMeshBoundingBoxes: false)) {
			WriteCube(lease.Span, 5f);
		}
		failures += ExpectExtent(viewA.BoundingBox, 1.015f, "existing view untouched when overwriteChildMeshBoundingBoxes is false");
		using var viewB = buffer.CreateMesh();
		failures += ExpectExtent(viewB.BoundingBox, 5.015f, "later view picks up the newer box");

		buffer.TriggerManualBoundingBoxRecalculation(overwriteChildMeshBoundingBoxes: true);
		failures += ExpectExtent(viewA.BoundingBox, 5.015f, "existing view updated when overwriteChildMeshBoundingBoxes is true");

		buffer.SetBoundingBox(new PositionedCuboid(new Cuboid(20f, 20f, 20f), Location.Origin), overwriteChildMeshBoundingBoxes: true);
		failures += ExpectExtent(viewA.BoundingBox, 10f, "explicit SetBoundingBox propagates to views");

		buffer.SetBoundingBox(viewB, new PositionedCuboid(new Cuboid(4f, 4f, 4f), Location.Origin));
		failures += ExpectExtent(viewB.BoundingBox, 2f, "per-mesh SetBoundingBox targets one view");
		failures += ExpectExtent(viewA.BoundingBox, 10f, "per-mesh SetBoundingBox leaves other views alone");

		using var material = factory.MaterialBuilder.CreateTestMaterial();
		using var instance = factory.ObjectBuilder.CreateModelInstance(viewA, material);
		try {
			buffer.SetBoundingBox(new PositionedCuboid(new Cuboid(6f, 6f, 6f), Location.Origin), overwriteChildMeshBoundingBoxes: true);
			failures += Expect(true, "propagation to a bound ModelInstance succeeds");
		}
		catch (Exception e) {
			failures += Expect(false, "propagation to a bound ModelInstance succeeds (threw " + e.GetType().Name + ")");
		}

		Console.WriteLine(failures == 0 ? "RESULT: PASS" : $"RESULT: FAIL ({failures} assertion(s))");
		return failures == 0 ? 0 : 1;
	}

	public static int RunGamepadMappingCheck() {
		Console.WriteLine("Gamepad mapping check:");
		var failures = 0;

		var mappedButtons = ImGuiKeyMap.MappedGamepadButtons;
		var buttonKeys = new List<ImGuiKey>();
		for (var i = 0; i < mappedButtons.Length; ++i) buttonKeys.Add(ImGuiKeyMap.TranslateGamepadButton(mappedButtons[i]));

		failures += Expect(buttonKeys.All(k => k != ImGuiKey.None), "every mapped button translates to a real ImGuiKey");
		failures += Expect(buttonKeys.Distinct().Count() == buttonKeys.Count, "no two buttons translate to the same ImGuiKey");

		failures += Expect(ImGuiKeyMap.TranslateGamepadButton(GameControllerButton.A) == ImGuiKey.GamepadFaceDown, "A maps to the positional bottom face button");
		failures += Expect(ImGuiKeyMap.TranslateGamepadButton(GameControllerButton.B) == ImGuiKey.GamepadFaceRight, "B maps to the positional right face button");
		failures += Expect(ImGuiKeyMap.TranslateGamepadButton(GameControllerButton.X) == ImGuiKey.GamepadFaceLeft, "X maps to the positional left face button");
		failures += Expect(ImGuiKeyMap.TranslateGamepadButton(GameControllerButton.Y) == ImGuiKey.GamepadFaceUp, "Y maps to the positional top face button");
		failures += Expect(ImGuiKeyMap.TranslateGamepadButton(GameControllerButton.LeftTrigger) == ImGuiKey.None, "left trigger is not mapped as a button");
		failures += Expect(ImGuiKeyMap.TranslateGamepadButton(GameControllerButton.RightTrigger) == ImGuiKey.None, "right trigger is not mapped as a button");

		var analogKeys = new[] {
			ImGuiKey.GamepadLStickLeft, ImGuiKey.GamepadLStickRight, ImGuiKey.GamepadLStickUp, ImGuiKey.GamepadLStickDown,
			ImGuiKey.GamepadRStickLeft, ImGuiKey.GamepadRStickRight, ImGuiKey.GamepadRStickUp, ImGuiKey.GamepadRStickDown,
			ImGuiKey.GamepadL2, ImGuiKey.GamepadR2
		};
		var covered = buttonKeys.Concat(analogKeys).ToList();
		var allGamepadKeys = Enum.GetValues<ImGuiKey>().Where(k => Enum.GetName(k)?.StartsWith("Gamepad", StringComparison.Ordinal) ?? false).Distinct().ToList();

		failures += Expect(covered.Distinct().Count() == covered.Count, "button and analog key sets do not overlap");
		failures += Expect(allGamepadKeys.Count == 24, $"ImGui exposes 24 gamepad keys (found {allGamepadKeys.Count})");
		var uncovered = allGamepadKeys.Except(covered).ToList();
		failures += Expect(uncovered.Count == 0, $"every ImGui gamepad key is driven (uncovered: {(uncovered.Count == 0 ? "none" : String.Join(", ", uncovered))})");

		const float Deadzone = GameControllerStickPosition.RecommendedDeadzoneSize;

		ImGuiKeyMap.DecomposeStick(GameControllerStickPosition.Centered, Deadzone, out var l, out var r, out var u, out var d);
		failures += Expect(l == 0f && r == 0f && u == 0f && d == 0f, "centred stick produces no displacement on any direction");

		ImGuiKeyMap.DecomposeStick(new GameControllerStickPosition(Int16.MinValue, 0), Deadzone, out l, out r, out u, out d);
		failures += Expect(MathF.Abs(l - 1f) < 0.001f && r == 0f, $"full left gives left=1 right=0 (got {l:N3}/{r:N3})");

		ImGuiKeyMap.DecomposeStick(new GameControllerStickPosition(Int16.MaxValue, 0), Deadzone, out l, out r, out u, out d);
		failures += Expect(MathF.Abs(r - 1f) < 0.001f && l == 0f, $"full right gives right=1 left=0 (got {r:N3}/{l:N3})");

		ImGuiKeyMap.DecomposeStick(new GameControllerStickPosition(0, Int16.MaxValue), Deadzone, out l, out r, out u, out d);
		failures += Expect(MathF.Abs(u - 1f) < 0.001f && d == 0f, $"positive vertical means UP, not down (got up={u:N3} down={d:N3})");

		ImGuiKeyMap.DecomposeStick(new GameControllerStickPosition(0, Int16.MinValue), Deadzone, out l, out r, out u, out d);
		failures += Expect(MathF.Abs(d - 1f) < 0.001f && u == 0f, $"negative vertical means DOWN (got down={d:N3} up={u:N3})");

		ImGuiKeyMap.DecomposeStick(new GameControllerStickPosition(4000, 4000), Deadzone, out l, out r, out u, out d);
		failures += Expect(l == 0f && r == 0f && u == 0f && d == 0f, "displacement inside the deadzone is fully suppressed");

		ImGuiKeyMap.DecomposeStick(new GameControllerStickPosition(Int16.MaxValue / 2, 0), Deadzone, out l, out r, out u, out d);
		failures += Expect(r > 0f && r < 1f, $"partial displacement is renormalized into (0, 1) (got {r:N3})");

		using (var factory = new LocalTinyFfrFactory()) {
			using (var imgui = factory.SceneBuilder.CreateImGuiScene(factory)) {
				ImGui.SetCurrentContext(imgui.Context);
				var flags = ImGui.GetIO().ConfigFlags;
				failures += Expect(!flags.HasFlag(ImGuiConfigFlags.NavEnableGamepad), "gamepad nav is off by default");
				failures += Expect(flags.HasFlag(ImGuiConfigFlags.DockingEnable), "docking remains on by default");
			}

			using (var imgui = factory.SceneBuilder.CreateImGuiScene(factory, new ImGuiSceneCreationConfig { EnableGamepadNavigation = true })) {
				ImGui.SetCurrentContext(imgui.Context);
				var flags = ImGui.GetIO().ConfigFlags;
				var backendFlags = ImGui.GetIO().BackendFlags;
				failures += Expect(flags.HasFlag(ImGuiConfigFlags.NavEnableGamepad), "EnableGamepadNavigation sets NavEnableGamepad");
				failures += Expect(flags.HasFlag(ImGuiConfigFlags.DockingEnable), "docking is unaffected by the gamepad option");
				failures += Expect(backendFlags.HasFlag(ImGuiBackendFlags.RendererHasTextures), "renderer texture support is still advertised");
			}

			var threw = false;
			try {
				using var imgui = factory.SceneBuilder.CreateImGuiScene(factory, new ImGuiSceneCreationConfig { GamepadStickDeadzone = 1.5f });
			}
			catch (ArgumentOutOfRangeException) {
				threw = true;
			}
			failures += Expect(threw, "an out-of-range deadzone is rejected");
		}

		Console.WriteLine(failures == 0 ? "RESULT: PASS" : $"RESULT: FAIL ({failures} assertion(s))");
		return failures == 0 ? 0 : 1;
	}

	public static int RunSubAreaCheck() {
		Console.WriteLine("ImGui sub-area check:");
		var failures = 0;

		var logicalSize = new XYPair<int>(1000, 600);
		var targetSize = new XYPair<int>(1000, 600);
		var subAreaOffsetFromTopRight = new XYPair<int>(40, 30);
		var subAreaSize = new XYPair<int>(400, 200);
		const int RedWindowW = 100;
		const int RedWindowH = 50;

		using var factory = new LocalTinyFfrFactory();
		using var buffer = factory.RendererBuilder.CreateRenderOutputBuffer(targetSize);
		using var imgui = factory.SceneBuilder.CreateImGuiScene(factory);
		using var renderer = factory.RendererBuilder.CreateRenderer(imgui, buffer);
		using var loop = factory.ApplicationLoopBuilder.CreateLoop();

		renderer.SetRenderSubAreaPixels(Orientation2D.UpRight, subAreaOffsetFromTopRight, subAreaSize);

		failures += Expect(renderer.GetRenderSubAreaPixelDimensions() == subAreaSize, $"reported sub-area size matches what was set (got {renderer.GetRenderSubAreaPixelDimensions()})");
		var offsetUpRight = renderer.GetRenderSubAreaPixelOffset(DiagonalOrientation2D.UpRight);
		failures += Expect(offsetUpRight == subAreaOffsetFromTopRight, $"UpRight offset round-trips the anchored offset (got {offsetUpRight})");

		var offsetUpLeft = renderer.GetRenderSubAreaPixelOffset(DiagonalOrientation2D.UpLeft);
		var offsetDownLeft = renderer.GetRenderSubAreaPixelOffset(DiagonalOrientation2D.DownLeft);
		var offsetDownRight = renderer.GetRenderSubAreaPixelOffset(DiagonalOrientation2D.DownRight);
		failures += Expect(offsetUpLeft.X == offsetDownLeft.X, $"left distance is origin-independent ({offsetUpLeft.X} vs {offsetDownLeft.X})");
		failures += Expect(offsetUpRight.Y == offsetUpLeft.Y, $"top distance is origin-independent ({offsetUpRight.Y} vs {offsetUpLeft.Y})");
		failures += Expect(offsetUpLeft.Y + subAreaSize.Y + offsetDownLeft.Y == targetSize.Y, $"vertical offsets and size span the target ({offsetUpLeft.Y} + {subAreaSize.Y} + {offsetDownLeft.Y} vs {targetSize.Y})");
		failures += Expect(offsetUpLeft.X + subAreaSize.X + offsetDownRight.X == targetSize.X, $"horizontal offsets and size span the target ({offsetUpLeft.X} + {subAreaSize.X} + {offsetDownRight.X} vs {targetSize.X})");

		var capturedSize = default(XYPair<int>);
		TexelRgba32[]? captured = null;

		for (var frame = 0; frame < 5; ++frame) {
			var deltaTime = loop.IterateOnce();
			imgui.BeginFrame(deltaTime, loop.Input, logicalSize, targetSize, renderer);

			var displaySize = ImGui.GetIO().DisplaySize;
			if (frame == 0) {
				failures += Expect(
					(int) displaySize.X == subAreaSize.X && (int) displaySize.Y == subAreaSize.Y,
					$"io.DisplaySize is the sub-area, not the target (got {displaySize.X}x{displaySize.Y}, wanted {subAreaSize.X}x{subAreaSize.Y})"
				);
			}

			ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0f, 0f, 1f));
			ImGui.SetNextWindowPos(new Vector2(Margin, Margin));
			ImGui.SetNextWindowSize(new Vector2(RedWindowW, RedWindowH));
			ImGui.Begin(
				"subarea",
				ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
				| ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings
			);
			ImGui.End();
			ImGui.PopStyleColor();

			imgui.EndFrame();

			if (frame == 4) {
				buffer.ReadNextFrame((size, texels) => {
					capturedSize = size;
					captured = texels.ToArray();
				}, presentFrameTopToBottom: true);
			}
			renderer.Render();
		}

		if (captured == null) {
			Console.WriteLine("  FAIL: no frame was captured.");
			return failures + 1;
		}

		var totalRed = CountRed(captured, capturedSize, 0, 0, capturedSize.X, capturedSize.Y);
		var insideSubArea = CountRed(captured, capturedSize, offsetUpLeft.X, offsetUpLeft.Y, subAreaSize.X, subAreaSize.Y);
		var expectedRect = CountRed(captured, capturedSize, offsetUpLeft.X + Margin, offsetUpLeft.Y + Margin, RedWindowW, RedWindowH);
		var expectedArea = RedWindowW * RedWindowH;

		failures += Expect(totalRed > 0, $"the window rendered at all ({totalRed} red px)");
		failures += Expect(totalRed == insideSubArea, $"no pixels rendered outside the sub-area ({totalRed - insideSubArea} stray px)");
		failures += Expect(
			expectedRect > expectedArea * 0.9f,
			$"the {RedWindowW}x{RedWindowH} window occupies its full unscaled pixel footprint inside the sub-area ({expectedRect}/{expectedArea} px)"
		);
		failures += Expect(
			totalRed < expectedArea * 1.1f,
			$"the window is not drawn larger than requested ({totalRed}/{expectedArea} px)"
		);

		Console.WriteLine(failures == 0 ? "RESULT: PASS" : $"RESULT: FAIL ({failures} assertion(s))");
		return failures == 0 ? 0 : 1;
	}

	public static int RunSubAreaCompositorCheck() {
		Console.WriteLine("ImGui sub-area via compositor check:");
		var failures = 0;

		var targetSize = new XYPair<int>(1000, 600);
		var subAreaSize = new XYPair<int>(300, 600);
		var subAreaOffset = new XYPair<int>(700, 0);
		const int RedWindowW = 100;
		const int RedWindowH = 50;

		using var factory = new LocalTinyFfrFactory();
		using var buffer = factory.RendererBuilder.CreateRenderOutputBuffer(targetSize);

		using var cubeMesh = factory.MeshBuilder.CreateMesh(new Cuboid(1f));
		using var cubeMaterial = factory.MaterialBuilder.CreateTestMaterial();
		using var cube = factory.ObjectBuilder.CreateModelInstance(cubeMesh, cubeMaterial);
		using var light = factory.LightBuilder.CreatePointLight(new Location(2f, 2f, -2f));
		using var backdropScene = factory.SceneBuilder.CreateScene(backdropColor: ColorVect.BlackOpaque);
		backdropScene.Add(cube);
		backdropScene.Add(light);
		using var backdropCamera = factory.CameraBuilder.CreateCamera(initialPosition: new Location(0f, 0f, -3f));
		using var backdropRenderer = factory.RendererBuilder.CreateRenderer(backdropScene, backdropCamera, buffer);

		using var imgui = factory.SceneBuilder.CreateImGuiScene(factory);
		var imguiRenderer = factory.RendererBuilder.CreateRenderer(imgui, buffer);
		imguiRenderer.SetRenderSubAreaPixels(Orientation2D.Right, (0, 0), subAreaSize);

		var compositor = factory.RendererBuilder.CreateCompositor(buffer);
		compositor.Add(backdropRenderer, RenderCompositionType.Standard);
		compositor.Add(imguiRenderer, RenderCompositionType.RetainPreviousScenes);

		using var loop = factory.ApplicationLoopBuilder.CreateLoop();

		var reportedOffset = imguiRenderer.GetRenderSubAreaPixelOffset();
		failures += Expect(reportedOffset == subAreaOffset, $"sub-area offset is as expected (got {reportedOffset}, wanted {subAreaOffset})");

		var capturedSize = default(XYPair<int>);
		TexelRgba32[]? captured = null;

		for (var frame = 0; frame < 5; ++frame) {
			var deltaTime = loop.IterateOnce();
			imgui.BeginFrame(deltaTime, loop.Input, targetSize / 2, targetSize, imguiRenderer);

			ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0f, 0f, 1f));
			ImGui.SetNextWindowPos(new Vector2(Margin, Margin));
			ImGui.SetNextWindowSize(new Vector2(RedWindowW, RedWindowH));
			ImGui.Begin(
				"compositorsub",
				ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
				| ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings
			);
			ImGui.End();
			ImGui.PopStyleColor();

			imgui.EndFrame();

			if (frame == 4) {
				buffer.ReadNextFrame((size, texels) => {
					capturedSize = size;
					captured = texels.ToArray();
				}, presentFrameTopToBottom: true);
			}
			compositor.RenderAll();
		}

		if (captured == null) {
			Console.WriteLine("  FAIL: no frame was captured.");
			return failures + 1;
		}

		var expectedRect = CountRed(captured, capturedSize, subAreaOffset.X + Margin, subAreaOffset.Y + Margin, RedWindowW, RedWindowH);
		var totalRed = CountRed(captured, capturedSize, 0, 0, capturedSize.X, capturedSize.Y);
		var expectedArea = RedWindowW * RedWindowH;

		failures += Expect(totalRed > 0, $"the ImGui window rendered at all through the compositor ({totalRed} red px)");
		failures += Expect(
			expectedRect > expectedArea * 0.9f,
			$"the window lands at its correct place inside the sub-area ({expectedRect}/{expectedArea} px)"
		);

		compositor.Dispose();
		imguiRenderer.Dispose();

		Console.WriteLine(failures == 0 ? "RESULT: PASS" : $"RESULT: FAIL ({failures} assertion(s))");
		return failures == 0 ? 0 : 1;
	}

	public static int RunImGuiTextureCheck() {
		Console.WriteLine("ImGui external texture check:");
		var failures = 0;

		var targetSize = new XYPair<int>(600, 400);
		const int ImageW = 160;
		const int ImageH = 120;

		using var factory = new LocalTinyFfrFactory();
		using var buffer = factory.RendererBuilder.CreateRenderOutputBuffer(targetSize);
		using var imgui = factory.SceneBuilder.CreateImGuiScene(factory);
		var renderer = factory.RendererBuilder.CreateRenderer(imgui, buffer);
		using var loop = factory.ApplicationLoopBuilder.CreateLoop();

		const int FixtureDim = 16;
		var splitTexels = new TexelRgba32[FixtureDim * FixtureDim];
		for (var y = 0; y < FixtureDim; ++y) {
			for (var x = 0; x < FixtureDim; ++x) {
				splitTexels[y * FixtureDim + x] = y < FixtureDim / 2 ? new TexelRgba32(255, 0, 0, 255) : new TexelRgba32(0, 0, 255, 255);
			}
		}
		var splitTexture = factory.TextureBuilder.CreateTexture(
			splitTexels,
			new TextureGenerationConfig { Dimensions = new(FixtureDim, FixtureDim) },
			new TextureCreationConfig { DataType = TextureDataType.LinearData, GenerateMipMaps = false, Name = "Split Test Texture" }
		);

		var splitId = imgui.RegisterTexture(splitTexture);
		var secondId = imgui.RegisterTexture(splitTexture);
		failures += Expect((long) splitId.Handle < 0L, $"registered ids are negative so they cannot collide with ImGui atlas ids (got {(long) splitId.Handle})");
		failures += Expect((long) splitId.Handle != 0L, "registered ids are never the invalid id 0");
		failures += Expect((long) secondId.Handle != (long) splitId.Handle, "each registration yields a distinct id");
		imgui.UnregisterTexture(secondId);

		var viewportBuffer = factory.RendererBuilder.CreateRenderOutputBuffer((256, 256));
		var viewportTexture = viewportBuffer.CreateDynamicTexture();
		var viewportId = imgui.RegisterTexture(viewportTexture);

		using var viewportMesh = factory.MeshBuilder.CreateMesh(new Cuboid(0.8f));
		using var viewportMaterial = factory.MaterialBuilder.CreateTestMaterial();
		using var viewportCube = factory.ObjectBuilder.CreateModelInstance(viewportMesh, viewportMaterial, new Location(0f, 0.9f, 0f));
		using var viewportLight = factory.LightBuilder.CreatePointLight(new Location(1.5f, 2.5f, -2f));
		using var viewportCamera = factory.CameraBuilder.CreateCamera(initialPosition: new Location(0f, 0f, -3f));
		using var viewportScene = factory.SceneBuilder.CreateScene(backdropColor: ColorVect.BlackOpaque);
		viewportScene.Add(viewportCube);
		viewportScene.Add(viewportLight);
		var viewportRenderer = factory.RendererBuilder.CreateRenderer(viewportScene, viewportCamera, viewportBuffer);

		var bogusId = new ImTextureID(unchecked((nint) (-999999)));

		var capturedSize = default(XYPair<int>);
		TexelRgba32[]? captured = null;
		var threw = false;

		try {
			for (var frame = 0; frame < 5; ++frame) {
				var deltaTime = loop.IterateOnce();
				viewportRenderer.Render();
				imgui.BeginFrame(deltaTime, loop.Input, targetSize, targetSize);

				ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f, 0f));
				ImGui.SetNextWindowPos(new Vector2(Margin, Margin));
				ImGui.SetNextWindowSize(new Vector2(ImageW, ImageH));
				ImGui.Begin(
					"img",
					ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
					| ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBackground
				);
				TinyFfrImGuiExtensions.Image(splitId, new Vector2(ImageW, ImageH));
				ImGui.End();

				ImGui.SetNextWindowPos(new Vector2(Margin, Margin + ImageH + Margin));
				ImGui.SetNextWindowSize(new Vector2(ImageW, ImageH));
				ImGui.Begin(
					"vp",
					ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
					| ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBackground
				);
				TinyFfrImGuiExtensions.Image(viewportId, new Vector2(ImageW, ImageH));
				TinyFfrImGuiExtensions.Image(bogusId, new Vector2(8f, 8f));
				ImGui.End();
				ImGui.PopStyleVar();

				imgui.EndFrame();

				if (frame == 4) {
					buffer.ReadNextFrame((size, texels) => {
						capturedSize = size;
						captured = texels.ToArray();
					}, presentFrameTopToBottom: true);
				}
				renderer.Render();
			}
		}
		catch (Exception e) {
			threw = true;
			Console.WriteLine($"  (threw {e.GetType().Name}: {e.Message})");
		}

		failures += Expect(!threw, "drawing registered and unregistered texture ids does not throw");

		if (captured == null) {
			Console.WriteLine("  FAIL: no frame was captured.");
			++failures;
		}
		else {
			var halfH = ImageH / 2;
			var redInTopHalf = CountWhere(captured, capturedSize, Margin, Margin, ImageW, halfH, IsRed);
			var blueInTopHalf = CountWhere(captured, capturedSize, Margin, Margin, ImageW, halfH, IsBlue);
			var redInBottomHalf = CountWhere(captured, capturedSize, Margin, Margin + halfH, ImageW, halfH, IsRed);
			var blueInBottomHalf = CountWhere(captured, capturedSize, Margin, Margin + halfH, ImageW, halfH, IsBlue);
			var halfArea = ImageW * halfH;

			failures += Expect(redInTopHalf + blueInTopHalf > halfArea * 0.5f, $"a registered texture is actually sampled by the ImGui material ({redInTopHalf + blueInTopHalf}/{halfArea} px)");
			failures += Expect(
				redInTopHalf > blueInTopHalf && redInTopHalf > halfArea * 0.5f,
				$"uploaded texture is not vertically flipped: first texel rows appear at the TOP (red {redInTopHalf} vs blue {blueInTopHalf} in top half)"
			);
			failures += Expect(
				blueInBottomHalf > redInBottomHalf && blueInBottomHalf > halfArea * 0.5f,
				$"uploaded texture is not vertically flipped: last texel rows appear at the BOTTOM (blue {blueInBottomHalf} vs red {redInBottomHalf} in bottom half)"
			);

			var vpTop = Margin + ImageH + Margin;
			var litInTopHalf = CountWhere(captured, capturedSize, Margin, vpTop, ImageW, halfH, IsLit);
			var litInBottomHalf = CountWhere(captured, capturedSize, Margin, vpTop + halfH, ImageW, halfH, IsLit);
			failures += Expect(litInTopHalf + litInBottomHalf > 0, $"the offscreen scene rendered into the buffer at all ({litInTopHalf + litInBottomHalf} lit px)");
			failures += Expect(
				litInTopHalf > litInBottomHalf * 4,
				$"render target texture is not vertically flipped: the raised cube appears in the TOP half ({litInTopHalf} lit px top vs {litInBottomHalf} bottom)"
			);
		}

		imgui.UnregisterTexture(viewportId);
		viewportRenderer.Dispose();
		var disposeThrew = false;
		try {
			viewportBuffer.Dispose();
		}
		catch (Exception e) {
			disposeThrew = true;
			Console.WriteLine($"  (dispose threw {e.GetType().Name}: {e.Message})");
		}
		failures += Expect(!disposeThrew, "a render output buffer can be disposed after its texture is unregistered");

		renderer.Dispose();
		imgui.Dispose();
		var textureSurvived = true;
		try {
			_ = splitTexture.Dimensions;
		}
		catch (ObjectDisposedException) {
			textureSurvived = false;
		}
		failures += Expect(textureSurvived, "ImGuiScene does not dispose textures it does not own");
		splitTexture.Dispose();

		Console.WriteLine(failures == 0 ? "RESULT: PASS" : $"RESULT: FAIL ({failures} assertion(s))");
		return failures == 0 ? 0 : 1;
	}

	static bool IsRed(TexelRgba32 t) => t.R > 100 && t.G < 100 && t.B < 100;
	static bool IsBlue(TexelRgba32 t) => t.B > 100 && t.R < 100 && t.G < 100;
	static bool IsLit(TexelRgba32 t) => t.R > 60 || t.G > 60 || t.B > 60;

	static int CountWhere(TexelRgba32[] texels, XYPair<int> size, int x0, int y0, int w, int h, Func<TexelRgba32, bool> predicate) {
		var count = 0;
		for (var y = Math.Max(0, y0); y < Math.Min(size.Y, y0 + h); ++y) {
			for (var x = Math.Max(0, x0); x < Math.Min(size.X, x0 + w); ++x) {
				if (predicate(texels[y * size.X + x])) ++count;
			}
		}
		return count;
	}

	static int ExpectExtent(PositionedCuboid box, float expectedHalfExtent, string description) {
		var ok = MathF.Abs(box.HalfWidth - expectedHalfExtent) < 0.001f
			  && MathF.Abs(box.HalfHeight - expectedHalfExtent) < 0.001f
			  && MathF.Abs(box.HalfDepth - expectedHalfExtent) < 0.001f;
		Console.WriteLine($"  {description}: half-extent {box.HalfWidth:N3} (expected {expectedHalfExtent:N3}) [{(ok ? "PASS" : "FAIL")}]");
		return ok ? 0 : 1;
	}
	static int Expect(bool condition, string description) {
		Console.WriteLine($"  {description} [{(condition ? "PASS" : "FAIL")}]");
		return condition ? 0 : 1;
	}

	static int CountRed(TexelRgba32[] texels, XYPair<int> size, int x0, int y0, int w, int h) {
		var count = 0;
		for (var y = Math.Max(0, y0); y < Math.Min(size.Y, y0 + h); ++y) {
			for (var x = Math.Max(0, x0); x < Math.Min(size.X, x0 + w); ++x) {
				var t = texels[y * size.X + x];
				if (t.R > 100 && t.G < 100 && t.B < 100) ++count;
			}
		}
		return count;
	}

	static void WritePpm(string path, XYPair<int> size, TexelRgba32[] texels) {
		using var stream = new FileStream(path, FileMode.Create);
		using var writer = new StreamWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);
		writer.Write($"P6\n{size.X} {size.Y}\n255\n");
		writer.Flush();
		var bytes = new byte[size.X * size.Y * 3];
		for (var i = 0; i < texels.Length; ++i) {
			bytes[i * 3 + 0] = texels[i].R;
			bytes[i * 3 + 1] = texels[i].G;
			bytes[i * 3 + 2] = texels[i].B;
		}
		stream.Write(bytes, 0, bytes.Length);
		Console.WriteLine($"Wrote {path}");
	}
}