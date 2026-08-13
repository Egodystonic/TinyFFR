// Created on 2026-08-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

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
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalDynamicVertexBufferTest {
	const int SingleGridDim = 20;
	const int BlockGridDim = 10;
	const int NumBlocks = 4;
	const float GridExtent = 1f;
	const float InstructionFontHeight = 0.022f;
	const float StatusFontHeight = 0.026f;

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		DoAnimatedGridTest();
		DoSharedBufferViewsTest();
	}

	static int VertexCountFor(int dim) => dim * dim;
	static int IndexCountFor(int dim) => (dim - 1) * (dim - 1) * 6;

	static void WriteGridVertices(Span<MeshVertex> dest, int dim, float extent, float time, float frequency, float amplitude) {
		for (var z = 0; z < dim; ++z) {
			for (var x = 0; x < dim; ++x) {
				var u = x / (float) (dim - 1);
				var v = z / (float) (dim - 1);
				var worldX = (u - 0.5f) * 2f * extent;
				var worldZ = (v - 0.5f) * 2f * extent;
				var worldY = MathF.Sin((worldX + time) * frequency) * MathF.Cos((worldZ + time * 0.7f) * frequency) * amplitude;
				dest[z * dim + x] = new MeshVertex(
					new Location(worldX, worldY, worldZ),
					new XYPair<float>(u, v),
					Direction.Right,
					Direction.Forward,
					Direction.Up
				);
			}
		}
	}

	static void WriteGridIndices(Span<ushort> dest, int dim, int vertexOffset) {
		var i = 0;
		for (var z = 0; z < dim - 1; ++z) {
			for (var x = 0; x < dim - 1; ++x) {
				var topLeft = (ushort) (vertexOffset + z * dim + x);
				var topRight = (ushort) (topLeft + 1);
				var bottomLeft = (ushort) (vertexOffset + (z + 1) * dim + x);
				var bottomRight = (ushort) (bottomLeft + 1);
				dest[i++] = topLeft;
				dest[i++] = bottomLeft;
				dest[i++] = topRight;
				dest[i++] = topRight;
				dest[i++] = bottomLeft;
				dest[i++] = bottomRight;
			}
		}
	}

	public void DoAnimatedGridTest() {
		var vertexCount = VertexCountFor(SingleGridDim);
		var indexCount = IndexCountFor(SingleGridDim);

		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Dynamic Vertex Buffer Test Part 1/2 (Animated Grid)");
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		using var mat = factory.AssetLoader.MaterialBuilder.CreateTestMaterial();
		using var scene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		using var buffer = factory.MeshBuilder.CreateDynamicVertexBuffer(vertexCount, indexCount, "Animated Grid Buffer");
		using (var lease = buffer.BorrowIndicesSpan(recalculateBoundingBoxOnLeaseDispose: false, overwriteChildMeshBoundingBoxes: false)) {
			WriteGridIndices(lease.Span, SingleGridDim, 0);
		}
		using (var lease = buffer.BorrowVerticesSpan(recalculateBoundingBoxOnLeaseDispose: true, overwriteChildMeshBoundingBoxes: true)) {
			WriteGridVertices(lease.Span, SingleGridDim, GridExtent, 0f, 2f, 0.25f);
		}

		using var meshView = buffer.CreateMesh();
		using var instance = factory.ObjectBuilder.CreateModelInstance(meshView, mat);
		scene.Add(instance);

		using var font = factory.AssetLoader.LoadFont();
		using var fontPen = font.CreatePen(BuiltInFontPenStyle.WhiteWithOutline);
		using var canvas = factory.SceneBuilder.CreateCanvasScene();
		using var canvasRenderer = factory.RendererBuilder.CreateRenderer(canvas, window);

		using var instructionsText = canvas.Add(
			"PART 1/2   ANIMATED GRID\n" +
			"\n" +
			"One DynamicVertexBuffer, one mesh view, one instance.\n" +
			"Vertices are rewritten every frame with both bounding box flags set.\n" +
			"The wave amplitude grows and shrinks, so the box changes size.\n" +
			"\n" +
			"VERIFY   the surface animates smoothly\n" +
			"VERIFY   orbit so the grid sits at the very edge of the screen:\n" +
			"         it must never pop out of view\n" +
			"\n" +
			"[Escape] continue to part 2",
			fontPen,
			TextJustification.Left
		);
		instructionsText.SetPlacementFraction(Orientation2D.UpLeft, (0.015f, 0.015f), InstructionFontHeight);

		using var compositor = factory.RendererBuilder.CreateCompositor(window);
		compositor.Add(renderer, RenderCompositionType.Standard);
		compositor.Add(canvasRenderer, RenderCompositionType.RetainPreviousScenes);

		using var cameraController = camera.CreateController<FreeFlyingCameraController>();

		var totalTime = 0f;
		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();
			totalTime += dt;

			var amplitude = 0.15f + 0.35f * (0.5f + 0.5f * MathF.Sin(totalTime * 0.6f));
			using (var lease = buffer.BorrowVerticesSpan(recalculateBoundingBoxOnLeaseDispose: true, overwriteChildMeshBoundingBoxes: true)) {
				WriteGridVertices(lease.Span, SingleGridDim, GridExtent, totalTime, 2f, amplitude);
			}

			if (loop.Input.KeyboardAndMouse.NewMouseClicks.Any(mc => mc.Key == MouseKey.MouseLeft)) window.LockCursor = !window.LockCursor;

			cameraController.AdjustAllViaDefaultControls(loop.Input.KeyboardAndMouse, dt);
			cameraController.AdjustAllViaDefaultControls(loop.Input.GameControllersCombined, dt);
			cameraController.Progress(dt);

			compositor.RenderAll();
		}

		scene.Remove(instance);
	}

	public void DoSharedBufferViewsTest() {
		var blockVertexCount = VertexCountFor(BlockGridDim);
		var blockIndexCount = IndexCountFor(BlockGridDim);
		var vertexCount = blockVertexCount * NumBlocks;
		var indexCount = blockIndexCount * NumBlocks;

		using var factory = new LocalTinyFfrFactory();
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Dynamic Vertex Buffer Test Part 2/2 (Shared Buffer Views)");
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		using var mat = factory.AssetLoader.MaterialBuilder.CreateTestMaterial();
		using var scene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

		using var buffer = factory.MeshBuilder.CreateDynamicVertexBuffer(vertexCount, indexCount, "Shared Block Buffer");

		var indexShift = 0;
		void WriteAllIndices() {
			using var lease = buffer.BorrowIndicesSpan(recalculateBoundingBoxOnLeaseDispose: false, overwriteChildMeshBoundingBoxes: false);
			for (var b = 0; b < NumBlocks; ++b) {
				var sourceBlock = (b + indexShift) % NumBlocks;
				WriteGridIndices(lease.Span.Slice(b * blockIndexCount, blockIndexCount), BlockGridDim, sourceBlock * blockVertexCount);
			}
		}
		WriteAllIndices();

		var views = new Mesh[NumBlocks];
		var instances = new ModelInstance[NumBlocks];
		var positions = new Location[NumBlocks];
		for (var b = 0; b < NumBlocks; ++b) {
			positions[b] = new Vect(0f, 0f, 2.2f).RotatedBy((360f / NumBlocks) * b % Direction.Up).AsLocation();
		}

		void CreateViewsAndInstances() {
			for (var b = 0; b < NumBlocks; ++b) {
				views[b] = buffer.CreateMesh((b * blockIndexCount)..((b + 1) * blockIndexCount));
				instances[b] = factory.ObjectBuilder.CreateModelInstance(views[b], mat, positions[b]);
				scene.Add(instances[b]);
			}
		}
		void DestroyViewsAndInstances() {
			for (var b = 0; b < NumBlocks; ++b) {
				scene.Remove(instances[b]);
				instances[b].Dispose();
				views[b].Dispose();
			}
		}

		var recalculateBoundingBox = true;
		var overwriteChildBoxes = true;
		var totalTime = 0f;

		void AnimateBlocks() {
			for (var b = 0; b < NumBlocks; ++b) {
				using var lease = buffer.BorrowVerticesSpan(recalculateBoundingBox, overwriteChildBoxes, (b * blockVertexCount)..((b + 1) * blockVertexCount));
				WriteGridVertices(lease.Span, BlockGridDim, GridExtent * 0.7f, totalTime, 1.5f + b * 1.5f, 0.2f + b * 0.05f);
			}
		}
		AnimateBlocks();
		CreateViewsAndInstances();

		using var font = factory.AssetLoader.LoadFont();
		using var fontPen = font.CreatePen(BuiltInFontPenStyle.WhiteWithOutline);
		using var canvas = factory.SceneBuilder.CreateCanvasScene();
		using var canvasRenderer = factory.RendererBuilder.CreateRenderer(canvas, window);

		using var instructionsText = canvas.Add(
			"PART 2/2   SHARED BUFFER VIEWS\n" +
			"\n" +
			$"One buffer holds {NumBlocks} blocks, each with its own mesh view and instance.\n" +
			"\n" +
			"[R] toggle recalculateBoundingBoxOnLeaseDispose\n" +
			"[O] toggle overwriteChildMeshBoundingBoxes\n" +
			"[B] TriggerManualBoundingBoxRecalculation\n" +
			"[G] SetBoundingBox: giant box on all views\n" +
			"[H] SetBoundingBox: tiny box on first view only\n" +
			"[I] rotate which vertex block each view draws\n" +
			"[P] resize both buffers by 1.5x\n" +
			"[T] attempt a resize without disposing views\n" +
			"\n" +
			"VERIFY   turn R off, then orbit until the blocks reach the\n" +
			"         screen edge: they must pop out of view\n" +
			"VERIFY   B brings them back; H culls only the first block\n" +
			"VERIFY   I visibly swaps the shapes between blocks\n" +
			"VERIFY   P leaves the blocks identical and reports preserved\n" +
			"\n" +
			"[Escape] finish",
			fontPen,
			TextJustification.Left
		);
		instructionsText.SetPlacementFraction(Orientation2D.UpLeft, (0.015f, 0.015f), InstructionFontHeight);

		var lastAction = "Ready";
		using var statusText = canvas.Add("", fontPen, TextJustification.Left);
		statusText.SetPlacementFraction(Orientation2D.DownLeft, (0.015f, 0.015f), StatusFontHeight);
		void RefreshStatus() {
			statusText.SetText(
				$"recalculate={recalculateBoundingBox}   overwrite={overwriteChildBoxes}   indexShift={indexShift}\n" +
				$"buffer={buffer.VertexBufferSize} verts / {buffer.IndexBufferSize} indices\n" +
				lastAction,
				TextJustification.Left
			);
		}
		RefreshStatus();

		using var compositor = factory.RendererBuilder.CreateCompositor(window);
		compositor.Add(renderer, RenderCompositionType.Standard);
		compositor.Add(canvasRenderer, RenderCompositionType.RetainPreviousScenes);

		using var cameraController = camera.CreateController<InspectorCameraController>();
		cameraController.MaxDistance = 14f;
		cameraController.MinDistance = 2f;
		cameraController.Distance = 8f;

		try {
			using var loop = factory.ApplicationLoopBuilder.CreateLoop();
			while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
				var dt = loop.IterateOnce().AsDeltaTime();
				totalTime += dt;
				var kbm = loop.Input.KeyboardAndMouse;
				var statusRequiresRefresh = false;

				AnimateBlocks();

				if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.R)) {
					recalculateBoundingBox = !recalculateBoundingBox;
					lastAction = $"Set recalculateBoundingBoxOnLeaseDispose to {recalculateBoundingBox}";
					statusRequiresRefresh = true;
				}
				if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.O)) {
					overwriteChildBoxes = !overwriteChildBoxes;
					lastAction = $"Set overwriteChildMeshBoundingBoxes to {overwriteChildBoxes}";
					statusRequiresRefresh = true;
				}
				if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.B)) {
					buffer.TriggerManualBoundingBoxRecalculation(overwriteChildMeshBoundingBoxes: true);
					lastAction = $"Manual recalculation done, view box half-height is now {views[0].BoundingBox.HalfHeight:N3}";
					statusRequiresRefresh = true;
				}
				if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.G)) {
					buffer.SetBoundingBox(new PositionedCuboid(new Cuboid(40f, 40f, 40f), Location.Origin), overwriteChildMeshBoundingBoxes: true);
					lastAction = "Set a giant bounding box on every view";
					statusRequiresRefresh = true;
				}
				if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.H)) {
					buffer.SetBoundingBox(views[0], new PositionedCuboid(new Cuboid(0.05f, 0.05f, 0.05f), Location.Origin));
					lastAction = "Set a tiny bounding box on the first view only";
					statusRequiresRefresh = true;
				}
				if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.I)) {
					indexShift = (indexShift + 1) % NumBlocks;
					WriteAllIndices();
					lastAction = $"Rotated index assignment, shift is now {indexShift}";
					statusRequiresRefresh = true;
				}
				if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.T)) {
					try {
						buffer.ResizeVertexBuffer(buffer.VertexBufferSize + 1);
						lastAction = "UNEXPECTED: resize with live views did not throw";
					}
					catch (ResourceDependencyException) {
						lastAction = "Resize with live views correctly rejected (ResourceDependencyException)";
					}
					statusRequiresRefresh = true;
				}
				if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.P)) {
					MeshVertex[] before;
					using (var lease = buffer.BorrowVerticesSpanReadOnly()) before = lease.Span.ToArray();

					DestroyViewsAndInstances();
					buffer.ResizeVertexBuffer((int) (buffer.VertexBufferSize * 1.5f));
					buffer.ResizeIndexBuffer((int) (buffer.IndexBufferSize * 1.5f));
					CreateViewsAndInstances();

					var preserved = true;
					using (var lease = buffer.BorrowVerticesSpanReadOnly()) {
						for (var i = 0; i < before.Length; ++i) {
							if (lease.Span[i] == before[i]) continue;
							preserved = false;
							break;
						}
					}
					lastAction = $"Resized buffers. Contents preserved: {preserved}";
					statusRequiresRefresh = true;
				}

				if (statusRequiresRefresh) RefreshStatus();

				if (kbm.NewMouseClicks.Any(mc => mc.Key == MouseKey.MouseLeft)) window.LockCursor = !window.LockCursor;

				cameraController.AdjustAllViaDefaultControls(kbm, dt);
				cameraController.AdjustAllViaDefaultControls(loop.Input.GameControllersCombined, dt);
				cameraController.Progress(dt);

				compositor.RenderAll();
			}
		}
		finally {
			DestroyViewsAndInstances();
		}
	}
}
