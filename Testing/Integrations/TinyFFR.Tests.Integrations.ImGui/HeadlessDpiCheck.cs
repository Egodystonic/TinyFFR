// Created on 2026-08-12 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Numerics;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.DearImGui;
using Egodystonic.TinyFFR.Factory.Local;
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