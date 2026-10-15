// Created on 2026-10-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Threading;

namespace Egodystonic.TinyFFR.Rendering;

[TestFixture]
unsafe class RenderOutputBufferTest {
	const int FrameNoiseTolerance = 6;

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	sealed class FrameCapture {
		public TexelRgba32[]? Texels;
		public int FramesSeen;
	}

	static FrameCapture? _capture;

	static void Capture(XYPair<int> dimensions, ReadOnlySpan<TexelRgba32> texels) {
		_capture!.Texels = texels.ToArray();
		_capture.FramesSeen++;
	}

	static int MaxChannelDifference(ReadOnlySpan<TexelRgba32> a, ReadOnlySpan<TexelRgba32> b) {
		Assert.AreEqual(a.Length, b.Length);
		var result = 0;
		for (var i = 0; i < a.Length; ++i) {
			result = Int32.Max(result, Math.Abs(a[i].R - b[i].R));
			result = Int32.Max(result, Math.Abs(a[i].G - b[i].G));
			result = Int32.Max(result, Math.Abs(a[i].B - b[i].B));
			result = Int32.Max(result, Math.Abs(a[i].A - b[i].A));
		}
		return result;
	}

	[Test]
	public void ReadFramesShouldBeMirroredBetweenOrientations() {
		var dimensions = new XYPair<int>(1024, 601);
		using var factory = new LocalTinyFfrFactory(factoryConfig: new LocalTinyFfrFactoryConfig { ThreadingConfig = new ThreadingConfig { WorkerThreadCount = 12 } });
		using var buffer = factory.RendererBuilder.CreateRenderOutputBuffer(dimensions);
		using var camera = factory.CameraBuilder.CreateCamera(Location.Origin);
		using var material = factory.MaterialBuilder.CreateTestMaterial();
		using var mesh = factory.MeshBuilder.CreateCuboid(new Cuboid(0.5f));
		using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, material, initialPosition: Location.Origin + Direction.Forward * 1.5f + Direction.Up * 0.4f, initialRotation: Direction.Up % 30f + Direction.Right % 14f);
		using var light = factory.LightBuilder.CreateDirectionalLight(direction: new Direction(-0.3f, -1f, 0.5f));
		using var scene = factory.SceneBuilder.CreateScene(backdropColor: ColorVect.WhiteOpaque);
		scene.Add(instance);
		scene.Add(light);
		using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, buffer);
		renderer.SetQuality(new RenderQualityConfig(BuiltInQualityConfiguration.Medium) { AntiAliasingMode = AntiAliasingMode.None });

		for (var i = 0; i < 4; ++i) renderer.RenderAndWaitForGpu();
		var capturesByOrientation = new TexelRgba32[2][];
		foreach (var topToBottom in new[] { false, true }) {
			_capture = new FrameCapture();
			buffer.ReadNextFrame(&Capture, topToBottom);
			renderer.RenderAndWaitForGpu();
			renderer.RenderAndWaitForGpu();
			Assert.AreEqual(1, _capture.FramesSeen);
			capturesByOrientation[topToBottom ? 1 : 0] = _capture.Texels!;
		}

		var mirrored = new TexelRgba32[dimensions.Area];
		for (var y = 0; y < dimensions.Y; ++y) {
			capturesByOrientation[1].AsSpan((dimensions.Y - 1 - y) * dimensions.X, dimensions.X).CopyTo(mirrored.AsSpan(y * dimensions.X));
		}
		Assert.Greater(MaxChannelDifference(capturesByOrientation[0], capturesByOrientation[1]), FrameNoiseTolerance * 4, "Test scene is vertically symmetric so can not validate row order.");
		Assert.LessOrEqual(MaxChannelDifference(capturesByOrientation[0], mirrored), FrameNoiseTolerance, "Rows were not mirrored between orientations.");
	}
}
