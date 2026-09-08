// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Benchmarks.Harness;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Benchmarks.Smoke;

static unsafe partial class SmokeSections {
	static readonly XYPair<int> ReadbackDimensions = (128, 72);
	static int _readbackTexelCount;

	public static void RenderFrame() {
		using var mesh = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Benchmark Render Mesh");
		using var material = Factory.MaterialBuilder.CreateTestMaterial();
		using var instance = Factory.ObjectBuilder.CreateModelInstance(mesh, material, Location.Origin + Direction.Forward * 2.2f, name: "Benchmark Render Instance");
		using var light = Factory.LightBuilder.CreatePointLight(Location.Origin, name: "Benchmark Render Light");
		using var scene = Factory.SceneBuilder.CreateScene(backdropColor: StandardColor.Aqua, name: "Benchmark Render Scene");
		using var camera = Factory.CameraBuilder.CreateCamera(Location.Origin, name: "Benchmark Render Camera");

		scene.Add(instance);
		scene.Add(light);

		using var renderer = Target.CreateRenderer(scene, camera, new RendererCreationConfig {
			Name = "Benchmark Renderer",
			Quality = new RenderQualityConfig(BuiltInQualityConfiguration.Medium)
		});

		for (var frame = 0; frame < SmokeWorkload.RenderFrameCount; ++frame) {
			instance.RotateBy(5f % Direction.Up);
			light.Color = light.Color.WithHueAdjustedBy(1f);
			renderer.RenderAndWaitForGpu();
		}
	}

	public static void FrameReadback() {
		using var mesh = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Benchmark Readback Mesh");
		using var material = Factory.MaterialBuilder.CreateTestMaterial();
		using var instance = Factory.ObjectBuilder.CreateModelInstance(mesh, material, Location.Origin + Direction.Forward * 2.2f, name: "Benchmark Readback Instance");
		using var scene = Factory.SceneBuilder.CreateScene(backdropColor: StandardColor.Green, name: "Benchmark Readback Scene");
		using var camera = Factory.CameraBuilder.CreateCamera(Location.Origin, name: "Benchmark Readback Camera");
		scene.Add(instance);

		using var buffer = Factory.RendererBuilder.CreateRenderOutputBuffer(ReadbackDimensions, "Benchmark Readback Buffer");
		using var renderer = Factory.RendererBuilder.CreateRenderer(scene, camera, buffer, "Benchmark Readback Renderer");

		for (var frame = 0; frame < SmokeWorkload.ReadbackFrameCount; ++frame) {
			buffer.ReadNextFrame(&CountReadbackTexels);
			renderer.RenderAndWaitForGpu();
			renderer.CaptureScreenshot(&CountReadbackTexels, captureResolution: ReadbackDimensions);
		}
	}

	static void CountReadbackTexels(XYPair<int> dimensions, ReadOnlySpan<TexelRgba32> texels) => _readbackTexelCount = texels.Length;

	public static void BufferAsDynamicTexture() {
		using var mesh = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Benchmark Dynamic Texture Mesh");
		using var sourceMaterial = Factory.MaterialBuilder.CreateTestMaterial();
		using var sourceInstance = Factory.ObjectBuilder.CreateModelInstance(mesh, sourceMaterial, Location.Origin + Direction.Forward * 2.2f, name: "Benchmark Dynamic Texture Source Instance");
		using var sourceScene = Factory.SceneBuilder.CreateScene(backdropColor: StandardColor.Red, name: "Benchmark Dynamic Texture Source Scene");
		using var sourceCamera = Factory.CameraBuilder.CreateCamera(Location.Origin, name: "Benchmark Dynamic Texture Source Camera");
		sourceScene.Add(sourceInstance);

		using var buffer = Factory.RendererBuilder.CreateRenderOutputBuffer(ReadbackDimensions, "Benchmark Dynamic Texture Buffer");
		using var sourceRenderer = Factory.RendererBuilder.CreateRenderer(sourceScene, sourceCamera, buffer, "Benchmark Dynamic Texture Source Renderer");
		sourceRenderer.RenderAndWaitForGpu();

		using var dynamicTexture = buffer.CreateDynamicTexture();
		using var dynamicMaterial = Factory.MaterialBuilder.CreateStandardMaterial(dynamicTexture, name: "Benchmark Dynamic Texture Material");
		using var dynamicInstance = Factory.ObjectBuilder.CreateModelInstance(mesh, dynamicMaterial, Location.Origin + Direction.Forward * 1.8f, name: "Benchmark Dynamic Texture Instance");
		using var scene = Factory.SceneBuilder.CreateScene(backdropColor: StandardColor.White, name: "Benchmark Dynamic Texture Scene");
		using var camera = Factory.CameraBuilder.CreateCamera(Location.Origin, name: "Benchmark Dynamic Texture Camera");
		scene.Add(dynamicInstance);

		using var renderer = Target.CreateRenderer(scene, camera);
		for (var frame = 0; frame < SmokeWorkload.DynamicTextureFrameCount; ++frame) {
			sourceRenderer.RenderAndWaitForGpu();
			renderer.RenderAndWaitForGpu();
		}
	}

	public static void RenderQualityAndCulling() {
		using var mesh = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Benchmark Quality Mesh");
		using var material = Factory.MaterialBuilder.CreateTestMaterial();
		using var instance = Factory.ObjectBuilder.CreateModelInstance(mesh, material, Location.Origin + Direction.Forward * 2.2f, name: "Benchmark Quality Instance");
		using var scene = Factory.SceneBuilder.CreateScene(backdropColor: StandardColor.Black, name: "Benchmark Quality Scene");
		using var camera = Factory.CameraBuilder.CreateCamera(Location.Origin, name: "Benchmark Quality Camera");
		scene.Add(instance);

		using var renderer = Target.CreateRenderer(scene, camera);

		for (var cycle = 0; cycle < SmokeWorkload.QualityCycleCount; ++cycle) {
			renderer.SetQuality(BuiltInQualityConfiguration.Lowest);
			renderer.SetFrustumCullingEnabled(false);
			renderer.RenderAndWaitForGpu();
			renderer.SetQuality(BuiltInQualityConfiguration.Medium);
			renderer.SetFrustumCullingEnabled(true);
			renderer.RenderAndWaitForGpu();
			renderer.SetQuality(BuiltInQualityConfiguration.High);
			renderer.RenderAndWaitForGpu();
		}
	}

	public static void ViewportSubAreas() {
		using var mesh = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Benchmark Viewport Mesh");
		using var material = Factory.MaterialBuilder.CreateTestMaterial();
		using var instance = Factory.ObjectBuilder.CreateModelInstance(mesh, material, Location.Origin + Direction.Forward * 2.2f, name: "Benchmark Viewport Instance");
		using var scene = Factory.SceneBuilder.CreateScene(backdropColor: StandardColor.Black, name: "Benchmark Viewport Scene");
		using var camera = Factory.CameraBuilder.CreateCamera(Location.Origin, name: "Benchmark Viewport Camera");
		scene.Add(instance);

		using var renderer = Target.CreateRenderer(scene, camera);

		for (var config = 0; config < SmokeWorkload.ViewportConfigCount; ++config) {
			var fraction = 0.25f + (config % 8) * 0.05f;
			renderer.SetRenderSubAreaFraction(Orientation2D.UpLeft, (0.1f, 0.1f), (fraction, fraction));
			_ = renderer.GetRenderSubAreaPixelDimensions();
			_ = renderer.GetRenderSubAreaPixelOffset();
			for (var ray = 0; ray < 32; ++ray) {
				_ = renderer.CreateRayFromRenderSurface((ray, ray));
				_ = renderer.CreateRayFromRenderSubAreaSurface((ray, ray));
			}
			renderer.RenderAndWaitForGpu();
		}

		renderer.SetRenderSubAreaPixels(Orientation2D.None, XYPair<int>.Zero, BenchmarkRenderTarget.DefaultDimensions);
	}

	public static void PixelPicking() {
		using var mesh = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Benchmark Picking Mesh");
		using var material = Factory.MaterialBuilder.CreateTestMaterial();
		using var instance = Factory.ObjectBuilder.CreateModelInstance(mesh, material, Location.Origin + Direction.Forward * 2.2f, name: "Benchmark Picking Instance");
		using var light = Factory.LightBuilder.CreatePointLight(Location.Origin, name: "Benchmark Picking Light");
		using var scene = Factory.SceneBuilder.CreateScene(backdropColor: StandardColor.Black, name: "Benchmark Picking Scene");
		using var camera = Factory.CameraBuilder.CreateCamera(Location.Origin, name: "Benchmark Picking Camera");
		scene.Add(instance);
		scene.Add(light);

		using var buffer = Factory.RendererBuilder.CreateRenderOutputBuffer(ReadbackDimensions, "Benchmark Picking Buffer");
		using var renderer = Factory.RendererBuilder.CreateRenderer(scene, camera, buffer, "Benchmark Picking Renderer");
		renderer.RenderAndWaitForGpu();

		for (var pick = 0; pick < SmokeWorkload.PickCount; ++pick) {
			var coord = new XYPair<int>(ReadbackDimensions.X * (pick + 1) / (SmokeWorkload.PickCount + 1), ReadbackDimensions.Y / 2);
			_ = renderer.PickModelInstanceFromRenderSurface(coord);
		}
	}

	public static void Compositing() {
		using var mesh = Factory.MeshBuilder.CreateMesh(Cuboid.UnitCube, name: "Benchmark Compositing Mesh");
		using var material = Factory.MaterialBuilder.CreateTestMaterial();
		using var instance = Factory.ObjectBuilder.CreateModelInstance(mesh, material, Location.Origin + Direction.Forward * 2.2f, name: "Benchmark Compositing Instance");
		using var backgroundScene = Factory.SceneBuilder.CreateScene(backdropColor: StandardColor.Aqua, name: "Benchmark Compositing Background Scene");
		using var foregroundScene = Factory.SceneBuilder.CreateScene(name: "Benchmark Compositing Foreground Scene");
		using var camera = Factory.CameraBuilder.CreateCamera(Location.Origin, name: "Benchmark Compositing Camera");
		foregroundScene.Add(instance);

		using var backgroundRenderer = Target.CreateRenderer(backgroundScene, camera, new RendererCreationConfig {
			Name = "Benchmark Compositing Background Renderer",
			Quality = RenderQualityConfig.Default
		});
		using var foregroundRenderer = Target.CreateRenderer(foregroundScene, camera, new RendererCreationConfig {
			Name = "Benchmark Compositing Foreground Renderer",
			Quality = RenderQualityConfig.Default
		});
		using var compositor = Target.CreateCompositor("Benchmark Compositor");

		compositor.Add(backgroundRenderer, RenderCompositionType.Standard);
		compositor.Add(foregroundRenderer, RenderCompositionType.Standard);
		compositor.SetRendererFrameRateRatio(backgroundRenderer, 2);
		_ = compositor.GetRendererFrameRateRatio(backgroundRenderer);
		compositor.SetEnabledState(backgroundRenderer, true);
		for (var frame = 0; frame < SmokeWorkload.CompositingFrameCount; ++frame) {
			instance.RotateBy(3f % Direction.Up);
			compositor.RenderAllAndWaitForGpu();
		}
	}

	public static void LoopIteration() {
		var loop = BenchmarkEnvironment.Loop;
		for (var i = 0; i < SmokeWorkload.LoopIterationCount; ++i) {
			_ = loop.IterateOnce();
			_ = loop.FramesPerSecondLatest;
			_ = loop.FramesPerSecondRecentAverage;
			_ = loop.FramesPerSecondRecentMin;
			_ = loop.FramesPerSecondRecentMax;
			_ = loop.TotalIteratedTime;
			_ = loop.TimeUntilNextIteration;
		}
	}
}
