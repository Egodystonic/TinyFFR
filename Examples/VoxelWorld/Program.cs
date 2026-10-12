using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;
using VoxelWorld;

var seed = args.Length > 0 && Int32.TryParse(args[0], out var seedArgument) ? seedArgument : WorldSettings.DefaultSeed;

using var factory = new LocalTinyFfrFactory(rendererBuilderConfig: new RendererBuilderConfig { EnableVSync = false });
var primaryDisplay = factory.DisplayDiscoverer.Primary ?? throw new InvalidOperationException("No display connected!");
using var window = factory.WindowBuilder.CreateWindow(primaryDisplay, title: "TinyFFR Voxel World", fullscreenStyle: WindowFullscreenStyle.FullscreenBorderless);
using var camera = factory.CameraBuilder.CreateCamera(exposure: CameraExposurePreset.OutsideVeryBright);
using var cameraController = camera.CreateController<FirstPersonCameraController>();
camera.FarPlaneDistance = WorldSettings.RenderDistance * 1.5f;

Console.WriteLine("Loading block textures...");
using var blockAtlas = factory.AssetLoader.LoadColorMap("block_atlas.png");
using var blockMaterial = factory.MaterialBuilder.CreateStandardMaterial(blockAtlas);
using var sun = factory.LightBuilder.CreateDirectionalLight(
	direction: new Direction(0.35f, -1f, 0.45f),
	brightnessPreset: DirectionalLightBrightnessPreset.Midday,
	castsShadows: true,
	showSunDisc: true
);

using var scene = factory.SceneBuilder.CreateScene();
using var canvas = factory.SceneBuilder.CreateCanvasScene();
using var sceneRenderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
using var canvasRenderer = factory.RendererBuilder.CreateRenderer(canvas, window);
using var compositor = factory.RendererBuilder.CreateCompositor(window);
using var loop = factory.ApplicationLoopBuilder.CreateLoop();

var skyColor = new ColorVect(0.3f, 0.55f, 0.95f);
scene.SetBackdrop(BuiltInSceneBackdrop.Clouds, SceneBackdropBrightnessPreset.Midday);
scene.Add(sun);
scene.AddFog(new FogDescriptor {
	Color = skyColor with { Alpha = 0.9f },
	StartDistance = WorldSettings.NearDetailDistance,
	DensityMultiplier = 0.5f,
	SkywardDensityFalloffMultiplier = 0f
});
compositor.Add(sceneRenderer, RenderCompositionType.Standard);
compositor.Add(canvasRenderer, RenderCompositionType.RetainPreviousScenes);

var generator = new TerrainGenerator(seed);
using var world = new WorldStreamer(factory, scene, blockMaterial, generator);
var player = new Player(camera, cameraController, world, generator);
using var hud = new DebugHud(factory, canvas);

window.LockCursor = true;

while (!loop.Input.UserQuitRequested) {
	var deltaTime = MathF.Min(loop.IterateOnce().AsDeltaTime(), WorldSettings.MaxDeltaTime);
	if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) break;

	world.Update(player.Position);
	player.Update(loop.Input, deltaTime);
	hud.Update(deltaTime, player, world, generator);

	compositor.RenderAll();
}
