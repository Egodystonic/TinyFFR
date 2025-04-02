using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Environment.Input;

using var factory = new LocalTinyFfrFactory();
using var cubeMesh = factory.AssetLoader.MeshBuilder.CreateMesh(new Cuboid(1f));
var materialBuilder = factory.AssetLoader.MaterialBuilder;

using var colorMap = materialBuilder.CreateColorMap(ColorVect.White.WithLightness(0.2f));
using var normalMap = materialBuilder.CreateNormalMap(TexturePattern.Rectangles(
	interiorSize: (64, 64),
	borderSize: (8, 8),
	paddingSize: (32, 32),
	interiorValue: new Direction(0f, 0f, 1f),
	borderRightValue: new Direction(-1f, 0f, 1f),
	borderTopValue: new Direction(0f, 1f, 1f),
	borderLeftValue: new Direction(1f, 0f, 1f),
	borderBottomValue: new Direction(0f, -1f, 1f),
	paddingValue: new Direction(0f, 0f, 1f),
	repetitions: (6, 6)
));

using var material = factory.AssetLoader.MaterialBuilder.CreateOpaqueMaterial(colorMap, normalMap);
using var cube = factory.ObjectBuilder.CreateModelInstance(cubeMesh, material, initialPosition: (0f, 0f, 2f));
using var light = factory.LightBuilder.CreatePointLight(Location.Origin);
using var scene = factory.SceneBuilder.CreateScene();

scene.Add(cube);
scene.Add(light);

using var window = factory.WindowBuilder.CreateWindow(factory.DisplayDiscoverer.Primary!.Value);
using var camera = factory.CameraBuilder.CreateCamera(initialPosition: Location.Origin, initialViewDirection: Direction.Forward);

using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
var input = loop.Input;
var kbm = input.KeyboardAndMouse;

while (!input.UserQuitRequested) {
	var deltaTime = (float) loop.IterateOnce().TotalSeconds;
	if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.Space)) cube.RotateBy(90f % Direction.Down * deltaTime);
	renderer.Render();
}