#:package Egodystonic.TinyFFR@*

using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Factory.Local;

using var factory = new LocalTinyFfrFactory();
var primaryDisplay = factory.DisplayDiscoverer.Primary ?? throw new InvalidOperationException("No display connected!");
using var window = factory.WindowBuilder.CreateWindow(primaryDisplay);
using var scene = factory.SceneBuilder.CreateScene();
using var camera = factory.CameraBuilder.CreateCamera(initialPosition: (0f, 0f, -2f));
using var scenePrimitive = scene.AddPrimitive();

using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window); 
using var appLoop = factory.ApplicationLoopBuilder.CreateLoop();

var cube = PositionedRotatedCuboid.UnitCubeAtOriginUnrotated;
var cubeRotationPerSec = new Rotation(60f, Direction.Up) + new Rotation(25f, Direction.Right);

while (!appLoop.Input.UserQuitRequested) {
	var deltaTime = appLoop.IterateOnce().AsDeltaTime();
	
	cube = cube.RotatedBy(cubeRotationPerSec * deltaTime);
	scenePrimitive.SetGeometryShape(cube);
	
	renderer.Render();
}