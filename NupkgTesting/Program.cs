using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Environment.Input;

using var factory = new LocalTinyFfrFactory();
using var cubeMesh = factory.AssetLoader.MeshBuilder.CreateMesh(new Cuboid(1f));
var materialBuilder = factory.AssetLoader.MaterialBuilder;

using var colorMap = materialBuilder.CreateColorMap(
	TexturePattern.Rectangles(
		interiorSize: (64, 64),
		borderSize: (8, 8),
		paddingSize: (32, 32),
		interiorValue: new ColorVect(1f, 1f, 1f),
		borderRightValue: new ColorVect(1f, 1f, 0f),
		borderTopValue: new ColorVect(1f, 0f, 0f),
		borderLeftValue: new ColorVect(0f, 1f, 0f),
		borderBottomValue: new ColorVect(0f, 0f, 1f),
		paddingValue: new ColorVect(0f, 0f, 0f),
		repetitions: (2, 2)
	)
);

using var material = factory.AssetLoader.MaterialBuilder.CreateOpaqueMaterial(colorMap);
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