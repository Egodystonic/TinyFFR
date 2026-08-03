// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR;

// ReSharper disable AccessToDisposedClosure
[TestFixture, Explicit]
class LocalSceneCompositingTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }
	
	[Test]
	public void Execute() {
		TestTexture();
		TestWindow();
	}
	
	void TestTexture() {
		using var factory = new LocalTinyFfrFactory();
		using var rtt = factory.RendererBuilder.CreateRenderOutputBuffer((1920, 1080));
		var outputDir = SetUpCleanTestDir("rtt_test");
		Console.WriteLine("Outputting screenshots to " + outputDir);
		
		// Backdrop
		using var backdropScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		using var backdropCamera = factory.CameraBuilder.CreateCamera();
		using var backdropRenderer = factory.RendererBuilder.CreateRenderer(backdropScene, backdropCamera, rtt);
		
		// Red Cubes
		using var redCubeScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.None);
		using var redCubeCamera = factory.CameraBuilder.CreateCamera();
		using var redCubeRenderer = factory.RendererBuilder.CreateRenderer(redCubeScene, redCubeCamera, rtt);
		using var redCubeMesh = factory.MeshBuilder.CreateMesh(Cuboid.UnitCube);
		using var redCubeTex = factory.TextureBuilder.CreateColorMap(StandardColor.Red, false);
		using var redCubeMat = factory.MaterialBuilder.CreateStandardMaterial(redCubeTex);
		using var redCubeModelInstances = factory.ObjectBuilder.GroupModelInstances(
			Enumerable.Range(0, 6).Select(i => factory.ObjectBuilder.CreateModelInstance(redCubeMesh, redCubeMat, Location.Origin + Direction.Random(new Plane(Direction.Up)) * 3f)).ToArray()
		);
		using var redCubeLight = factory.LightBuilder.CreatePointLight(maxIlluminationRadius: 20f);
		redCubeScene.Add(redCubeModelInstances);
		redCubeScene.Add(redCubeLight);
		
		// Green Spheres
		using var greenSphereScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Starfield);
		using var greenSphereCamera = factory.CameraBuilder.CreateCamera();
		using var greenSphereRenderer = factory.RendererBuilder.CreateRenderer(greenSphereScene, greenSphereCamera, rtt);
		using var greenSphereMesh = factory.MeshBuilder.CreateMesh(Sphere.OneMeterCubedVolumeSphere);
		using var greenSphereTex = factory.TextureBuilder.CreateColorMap(StandardColor.Green, false);
		using var greenSphereMat = factory.MaterialBuilder.CreateStandardMaterial(greenSphereTex);
		using var greenSphereModelInstances = factory.ObjectBuilder.GroupModelInstances(
			Enumerable.Range(0, 6).Select(i => factory.ObjectBuilder.CreateModelInstance(greenSphereMesh, greenSphereMat, Location.Origin + Direction.Random(new Plane(Direction.Up)) * 3f)).ToArray()
		);
		using var greenSphereLight = factory.LightBuilder.CreatePointLight(maxIlluminationRadius: 20f);
		greenSphereScene.Add(greenSphereModelInstances);
		greenSphereScene.Add(greenSphereLight);
		
		// Blue Cubes
		using var blueCubeScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.None);
		using var blueCubeCamera = factory.CameraBuilder.CreateCamera();
		using var blueCubeRenderer = factory.RendererBuilder.CreateRenderer(blueCubeScene, blueCubeCamera, rtt);
		using var blueCubeMesh = factory.MeshBuilder.CreateMesh(Cuboid.UnitCube);
		using var blueCubeTex = factory.TextureBuilder.CreateColorMap(StandardColor.Blue, false);
		using var blueCubeMat = factory.MaterialBuilder.CreateStandardMaterial(blueCubeTex);
		using var blueCubeModelInstances = factory.ObjectBuilder.GroupModelInstances(
			Enumerable.Range(0, 6).Select(i => factory.ObjectBuilder.CreateModelInstance(blueCubeMesh, blueCubeMat, Location.Origin + Direction.Random(new Plane(Direction.Up)) * 5f)).ToArray()
		);
		using var blueCubeLight = factory.LightBuilder.CreatePointLight(maxIlluminationRadius: 20f);
		blueCubeScene.Add(blueCubeModelInstances);
		blueCubeScene.Add(blueCubeLight);
		
		using var compositor = factory.RendererBuilder.CreateCompositor(rtt);
		greenSphereRenderer.SetRenderSubAreaFraction(Orientation2D.Right, (0.1f, 0.0f), (0.2f, 0.2f));
		compositor.Add(backdropRenderer, RenderCompositionType.Standard);
		compositor.Add(redCubeRenderer, RenderCompositionType.RetainPreviousScenes);
		compositor.Add(greenSphereRenderer, RenderCompositionType.Standard);
		compositor.Add(blueCubeRenderer, RenderCompositionType.RetainPreviousScenes);
	
		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		var lastScreenDumpTime = 0;
		while (loop.TotalIteratedTime.AsDeltaTime() < 5f) {
			var dt = loop.IterateOnce().AsDeltaTime();
			
			redCubeCamera.RotateBy(36f % Direction.Up * dt); 
			greenSphereCamera.RotateBy(36f % Direction.Up * -dt); 
			blueCubeCamera.RotateBy(36f % Direction.Up * -dt); 
			
			var curDumpTime = (int) loop.TotalIteratedTime.TotalSeconds;
			if (curDumpTime != lastScreenDumpTime) {
				lastScreenDumpTime = curDumpTime;
				rtt.ReadNextFrame((XYPair<int> dim, ReadOnlySpan<TexelRgba32> pixels) => ImageUtils.SaveBitmap(Path.Combine(outputDir, curDumpTime + "_secs.bmp"), dim, pixels));
				compositor.RenderAllAndWaitForGpu();
			}
		}
		
		blueCubeScene.Remove(blueCubeLight);
		blueCubeScene.Remove(blueCubeModelInstances);
		greenSphereScene.Remove(greenSphereLight);
		greenSphereScene.Remove(greenSphereModelInstances);
		redCubeScene.Remove(redCubeLight);
		redCubeScene.Remove(redCubeModelInstances);
	}
	
	void TestWindow() {
		using var factory = new LocalTinyFfrFactory(rendererBuilderConfig: new RendererBuilderConfig{ EnableVSync = false});
		var display = factory.DisplayDiscoverer.Primary!.Value;
		using var window = factory.WindowBuilder.CreateWindow(display, title: "Local Scene Compositing Test");
		
		// Backdrop
		using var backdropScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Clouds);
		using var backdropCamera = factory.CameraBuilder.CreateCamera();
		using var backdropRenderer = factory.RendererBuilder.CreateRenderer(backdropScene, backdropCamera, window);
		
		// Red Cubes
		using var redCubeScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.None);
		using var redCubeCamera = factory.CameraBuilder.CreateCamera();
		using var redCubeRenderer = factory.RendererBuilder.CreateRenderer(redCubeScene, redCubeCamera, window);
		using var redCubeMesh = factory.MeshBuilder.CreateMesh(Cuboid.UnitCube);
		using var redCubeTex = factory.TextureBuilder.CreateColorMap(StandardColor.Red, false);
		using var redCubeMat = factory.MaterialBuilder.CreateStandardMaterial(redCubeTex);
		using var redCubeModelInstances = factory.ObjectBuilder.GroupModelInstances(
			Enumerable.Range(0, 6).Select(i => factory.ObjectBuilder.CreateModelInstance(redCubeMesh, redCubeMat, Location.Origin + Direction.Random(new Plane(Direction.Up)) * 3f)).ToArray()
		);
		using var redCubeLight = factory.LightBuilder.CreatePointLight(maxIlluminationRadius: 20f);
		redCubeScene.Add(redCubeModelInstances);
		redCubeScene.Add(redCubeLight);
		
		// Green Spheres
		using var greenSphereScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.Starfield);
		using var greenSphereCamera = factory.CameraBuilder.CreateCamera();
		using var greenSphereRenderer = factory.RendererBuilder.CreateRenderer(greenSphereScene, greenSphereCamera, window);
		using var greenSphereMesh = factory.MeshBuilder.CreateMesh(Sphere.OneMeterCubedVolumeSphere);
		using var greenSphereTex = factory.TextureBuilder.CreateColorMap(StandardColor.Green, false);
		using var greenSphereMat = factory.MaterialBuilder.CreateStandardMaterial(greenSphereTex);
		using var greenSphereModelInstances = factory.ObjectBuilder.GroupModelInstances(
			Enumerable.Range(0, 6).Select(i => factory.ObjectBuilder.CreateModelInstance(greenSphereMesh, greenSphereMat, Location.Origin + Direction.Random(new Plane(Direction.Up)) * 3f)).ToArray()
		);
		using var greenSphereLight = factory.LightBuilder.CreatePointLight(maxIlluminationRadius: 20f);
		greenSphereScene.Add(greenSphereModelInstances);
		greenSphereScene.Add(greenSphereLight);
		
		// Blue Cubes
		using var blueCubeScene = factory.SceneBuilder.CreateScene(BuiltInSceneBackdrop.None);
		using var blueCubeCamera = factory.CameraBuilder.CreateCamera();
		using var blueCubeRenderer = factory.RendererBuilder.CreateRenderer(blueCubeScene, blueCubeCamera, window);
		using var blueCubeMesh = factory.MeshBuilder.CreateMesh(Cuboid.UnitCube);
		using var blueCubeTex = factory.TextureBuilder.CreateColorMap(StandardColor.Blue, false);
		using var blueCubeMat = factory.MaterialBuilder.CreateStandardMaterial(blueCubeTex);
		using var blueCubeModelInstances = factory.ObjectBuilder.GroupModelInstances(
			Enumerable.Range(0, 6).Select(i => factory.ObjectBuilder.CreateModelInstance(blueCubeMesh, blueCubeMat, Location.Origin + Direction.Random(new Plane(Direction.Up)) * 5f)).ToArray()
		);
		using var blueCubeLight = factory.LightBuilder.CreatePointLight(maxIlluminationRadius: 20f);
		blueCubeScene.Add(blueCubeModelInstances);
		blueCubeScene.Add(blueCubeLight);
		
		using var compositor = factory.RendererBuilder.CreateCompositor(window);
		greenSphereRenderer.SetRenderSubAreaFraction(Orientation2D.Right, (0.1f, 0.0f), (0.2f, 0.2f));
		compositor.Add(backdropRenderer, RenderCompositionType.Standard);
		compositor.Add(redCubeRenderer, RenderCompositionType.RetainPreviousScenes);
		compositor.Add(greenSphereRenderer, RenderCompositionType.Standard);
		compositor.Add(blueCubeRenderer, RenderCompositionType.RetainPreviousScenes);
	
		using var loop = factory.ApplicationLoopBuilder.CreateLoop();
		while (!loop.Input.UserQuitRequested && !loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) {
			var dt = loop.IterateOnce().AsDeltaTime();
			
			redCubeCamera.RotateBy(36f % Direction.Up * dt); 
			greenSphereCamera.RotateBy(36f % Direction.Up * -dt); 
			blueCubeCamera.RotateBy(36f % Direction.Up * -dt); 
			
			compositor.RenderAll();
			
			window.SetTitle(
			    $"FPS: {loop.FramesPerSecondRecentAverage:0000} avg | " +
			    $"[{loop.FramesPerSecondRecentMin:0000} - {loop.FramesPerSecondRecentMax:0000}] range | " +
			    $"{loop.FramesPerSecondLatest:0000} current"
			);
		}
		
		blueCubeScene.Remove(blueCubeLight);
		blueCubeScene.Remove(blueCubeModelInstances);
		greenSphereScene.Remove(greenSphereLight);
		greenSphereScene.Remove(greenSphereModelInstances);
		redCubeScene.Remove(redCubeLight);
		redCubeScene.Remove(redCubeModelInstances);
	}
}