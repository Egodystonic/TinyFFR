using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Environment.Input;

using var factory = new LocalTinyFfrFactory();
using var cubeMesh = factory.AssetLoader.MeshBuilder.CreateMesh(new Cuboid(1f));
using var colorMap = factory.AssetLoader.MaterialBuilder.CreateColorMap(StandardColor.Maroon);
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

while (!loop.Input.UserQuitRequested) {
	var deltaTime = (float) loop.IterateOnce().TotalSeconds;

	if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Space)) {
		cube.RotateBy(90f % Direction.Down * deltaTime);
	}

	renderer.Render();
}