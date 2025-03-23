// The factory object is used to create all other resources
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Environment.Input;

using var factory = new LocalTinyFfrFactory();

// Create a cuboid mesh and load an instance of it in to the world with a test material
using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(new Cuboid(1f)); // 1m cube
using var instance = factory.ObjectBuilder.CreateModelInstance(
  mesh,
  factory.AssetLoader.MaterialBuilder.TestMaterial
);

// Create a light to illuminate the cube
using var light = factory.LightBuilder.CreatePointLight(Location.Origin);

// Create a window to render to, 
// a scene to render, 
// a camera to capture the scene, 
// and a renderer to render it all
using var window = factory.WindowBuilder.CreateWindow(factory.DisplayDiscoverer.Primary!.Value);
using var scene = factory.SceneBuilder.CreateScene();
using var camera = factory.CameraBuilder.CreateCamera();
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

// Add the cube instance and light to the scene
scene.Add(instance);
scene.Add(light);

// Put the cube 2m in front of the camera
instance.SetPosition(new Location(0f, 0f, 2f));

// Keep rendering at 60Hz until the user closes the window
using var loop = factory.ApplicationLoopBuilder.CreateLoop(60);
while (!loop.Input.UserQuitRequested) {
	var dt = (float) loop.IterateOnce().TotalSeconds;

	// If we're holding space down, rotate the cube
	if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Space)) {
		instance.RotateBy(new Rotation(angle: 90f, axis: Direction.Down) * dt);
	}

	renderer.Render();
}