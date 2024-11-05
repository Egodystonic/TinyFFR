// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Scene;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class NativeCuboidRenderTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void Execute() {
		using var factory = (ILocalRendererFactory) new LocalRendererFactory();

		var display = factory.DisplayDiscoverer.Recommended ?? throw new AssertionException("This test requires at least one connected display.");
		using var window = factory.WindowBuilder.Build(display, WindowFullscreenStyle.NotFullscreen);
		using var loop = factory.ApplicationLoopBuilder.BuildLoop(new LocalApplicationLoopConfig { FrameRateCapHz = 30, Name = "Larry the Loop" });

		using var camera = factory.CameraBuilder.CreateCamera(new() {
			Position = (Direction.Backward * 1f).AsLocation(),
			ViewDirection = Direction.Forward,
			UpDirection = Direction.Up,
			Name = "Carl the Camera"
		});

		using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(new CuboidDescriptor(10f, 7f, 2f), new() { Name = "Clive the Cuboid" });
		using var mat = factory.AssetLoader.MaterialBuilder.CreateBasicSolidColorMat(0x00FF00, new() { Name = "Matthew the Material" });
		using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, mat, new() { Name = "Iain the Instance" });

		using var scene = factory.SceneBuilder.CreateScene(new() { Name = "Sean the Scene" });
		scene.Add(instance);
		
		Console.WriteLine(display);
		Console.WriteLine(window);
		Console.WriteLine(loop);
		Console.WriteLine(camera);
		Console.WriteLine(mesh);
		Console.WriteLine(mat);
		Console.WriteLine(instance);
		Console.WriteLine(scene);

		while (!loop.Input.UserQuitRequested) {
			_ = loop.IterateOnce();
			scene.Render(camera, window);
			camera.Rotate(new Rotation(3f, Direction.Up));
			instance.Rotate(new Rotation(7f, Direction.Up));
		}
	}
}