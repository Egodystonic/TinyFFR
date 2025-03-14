# TinyFFR

A **Tiny** **F**ixed **F**unction **R**enderer library for C#/.NET 9.

* Distributed via NuGet
* Free for commercial and non-commercial use (see license)
* Features:
  * Physically-based rendering (via [filament](https://github.com/google/filament))
  * Asset loading (via [assimp](https://github.com/assimp/assimp) and [stb_image](https://github.com/nothings/stb))
  * Window management and input handling (via [SDL](https://github.com/libsdl-org/SDL))
  * Math & geometry API

> [!CAUTION]
> TinyFFR is currently in early prerelease. There will be bugs. Please have patience and consider reporting any issues you find in this repository.

## Manual

Manual is available at [tinyffr.dev](https://tinyffr.dev).

## Examples

### Hello Cube

This is a complete example; the code shown is all that is required to render the given image (and rotate it when holding space):

![Image of rendered cube](hello_cube.jpg)

```csharp
using var factory = new LocalTinyFfrFactory();
using var window = factory.WindowBuilder.CreateWindow(factory.DisplayDiscoverer.Primary!.Value);
using var loop = factory.ApplicationLoopBuilder.CreateLoop(60); // 60hz Loop
using var camera = factory.CameraBuilder.CreateCamera();
using var mesh = factory.AssetLoader.MeshBuilder.CreateMesh(new Cuboid(1f)); // 1m cube
var material = factory.AssetLoader.MaterialBuilder.TestMaterial;
using var instance = factory.ObjectBuilder.CreateModelInstance(mesh, material);
using var light = factory.LightBuilder.CreatePointLight(Location.Origin);
using var scene = factory.SceneBuilder.CreateScene();
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);

scene.Add(instance);
scene.Add(light);

instance.SetPosition(new Location(0f, 0f, 2f));

while (!loop.Input.UserQuitRequested) {
	var dt = (float) loop.IterateOnce().TotalSeconds;

	if (loop.Input.KeyboardAndMouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.Space)) {
		instance.RotateBy(new Rotation(angle: 90f, axis: Direction.Down) * dt);
	}

	renderer.Render();
}
```

----

### Asset Loading

This snippet demonstrates how to load texture and mesh files:

![Image of imported crate and sky texture](asset_import.jpg)

```csharp
var loader = factory.AssetLoader;

// Load albedo, normal, and occlusion/roughness/metallic map from specular-model PNG files
using var albedo = loader.LoadTexture("Crate.png");
using var normal = loader.LoadTexture("CreateNormals.png");
using var orm = loader.LoadAndCombineOrmTextures(roughnessMapFilePath: "CrateSpecular.png", metallicMapFilePath: "CrateSpecular.png", config: new() { InvertYGreenChannel = true });

// Create material
using var mat = loader.MaterialBuilder.CreateOpaqueMaterial(albedo, normal, orm);

// Load .obj mesh and scale it down
using var mesh = loader.LoadMesh("Crate.obj", new MeshCreationConfig { LinearRescalingFactor = 0.03f });

// Load HDR cubemap
using var cubemap = loader.LoadEnvironmentCubemap("SkyClouds.hdr");
```