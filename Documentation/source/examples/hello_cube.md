---
title: Hello Cube
description: An example on how to make a simple cube appear using TinyFFR.
---

This tutorial will show you how to get started with the basics of TinyFFR. In this example, we will:

* Create a window
* Create a cube and a light source
* Create a camera
* Create a 'scene' to hold the cube, light, and camera
* Create a renderer to take the scene and render it through the camera to the window
* Handle the user holding the space-bar to rotate the cube

## Code

### Namespaces

We will need to import the following namespaces:

```csharp
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Environment.Input;
```

All namespaces in the library start with `Egodystonic.TinyFFR`.

### Creating the Factory

The next thing we need to do is create the *factory object*. This is the object that must be used to create all other rendering resources in TinyFFR:

```csharp
using var factory = new LocalTinyFfrFactory();
```

Most resources in TinyFFR implement the `IDisposable` interface, and they must be disposed by the user (you) when no longer needed. The factory object is no exception to this. For this example, we will use [C#'s `using` syntax](https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-idisposable#the-c-f-and-visual-basic-using-statement) to automatically dispose the factory at the end of the example. You may wish to manually dispose the factory yourself instead, depending on your application's architecture.

For more information about the factory object, see: [:octicons-arrow-right-24: The Factory](/concepts/factory.md)

### Creating the Cube Mesh

Every object that is eventually rendered to the screen in a 3D scene is made up of a *mesh* of polygons. You do not need to understand how these meshes are formed or even what a polygon is; all you need to know is that in order to create a cube for our scene we firstly need a cube *mesh*. (1)
{ .annotate }

1. 	A mesh specifies to the renderer firstly (and most fundamentally) how an object's polygons are laid out in space. The mesh builder that we will use below can help create a list of polygons laid out in a cube/cuboid shape. 

	As well as each polygon's position, a mesh specifies some geometric properties such as the direction each vertex (corner) faces, and how to lay out textures on the object's surface. The mesh builder will also specify these properties for us behind-the-scenes.

We can use the factory's *mesh builder* to build such a mesh:

```csharp
var meshBuilder = factory.AssetLoader.MeshBuilder; // (1)!

var cubeDesc = new Cuboid(1f); // (2)!
using var cubeMesh = meshBuilder.CreateMesh(cubeDesc); // (3)!
```

1. A mesh is a type of *asset*; assets are basically anything we store on the GPU's memory (i.e. VRAM). Because all assets are ultimately loaded on to the GPU by a single *asset loader*, the mesh builder is a property of the factory's `AssetLoader`.
2. 	This line creates a 1m x 1m x 1m cube. All scalar (e.g. floating-point) values in TinyFFR are generally specified in meters.
	
	The constructor for Cuboid can take three parameters instead of one if you prefer a separate width, depth, and height.
	
3. `CreateMesh` can take a variety of different parameters, for now we just want to supply a description of a cuboid to generate a polygon mesh in that shape.


Because the resultant `mesh` is a disposable resource, we once again use the `using` pattern to make sure it's disposed when we're done.

For more information about the mesh builder, see: [:octicons-arrow-right-24: Meshes](/concepts/meshes.md)

### Creating a Material for the Cube

You may think that we've specified everything we need to put our cube in front of a camera, but hold on! All we've done so far is create a *mesh* for the cube, i.e. a description of a layout of polygons that defines the __shape__ of the cube. 

We will also need a *material* that describes the __surface__ of the cube. At the most fundamental level, we can create a material that describes the cube's surface as a single colour.

We can use the factory's *material builder* to build such a material:

```csharp
var materialBuilder = factory.AssetLoader.MaterialBuilder; // (1)!

using var colorMap = materialBuilder.CreateColorMap(StandardColor.Maroon); // (2)!
using var material = materialBuilder.CreateOpaqueMaterial(colorMap); // (3)!
```

1. Just like with the mesh before, materials are also assets stored on the GPU's memory (i.e. VRAM). And just like with the mesh builder, this means the material builder is a property of the factory's `AssetLoader`.
2. 	A color map is basically a 2D texture (e.g. an image/bitmap) that will be applied to the surface of a mesh.

	`CreateColorMap()` takes a variety of parameters, but in this example we're using an [implicit conversion](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators) from `StandardColor` to `ColorVect` to specify a single uniform color. If you want to experiment, you can try providing a `ColorVect` directly instead, the type is fairly simple to use!

3. "Opaque" means non-transparent, i.e. we're creating a material that you can not see through. Most real-world materials are opaque, and because transparent materials have a higher performance cost to render, it makes sense that you'll be creating opaque materials most of the time.

You might wonder why there's a two-step process to creating a material: We create a "color map" first, and then use that to create an "opaque material". 

In actuality, a material is more than just the colour of something; objects generally have an array of parameters used to describe their surface such as roughness, metallicness, and distortions on their surface. `CreateOpaqueMaterial()` can take more parameters to specify these values with more texture maps. However, for this initial example, we only care to specify a colour, so we just supply a `colorMap`.

Finally, the `colorMap` and `material` are both disposable resources, so again we use the `using` pattern to make sure they get disposed.

For more information about the material builder, see: [:octicons-arrow-right-24: Materials](/concepts/materials.md)

### Creating a Cube Instance

Now that we have a cube *mesh* and a *material* loaded on to the GPU, we can use these two assets to make a single *instance* of a cube to put in our rendered scene:

```csharp
var objectBuilder = factory.ObjectBuilder;

using var cube = objectBuilder.CreateModelInstance(cubeMesh, material);
```

The *object builder* is another interface exposed via our factory object that helps us build 'objects' to put in our scene. `CreateModelInstance()` allows us to pass in a mesh and a material and returns one "model instance" that uses them both together.

The returned `cube` instance is, of course, a disposable resource again (hopefully you're spotting a pattern by now!).

### Illuminating the Cube

To make things feel "3D" we generally need to simulate light sources in scenes. Therefore we will add a single point-light(1) to our scene.
{ .annotate }

1. A point light is the simplest form of light source. Imagine a single "point" in space emitting light evenly all around itself in a sphere: That's a point light!

To create a light, we use the *light builder*:

```csharp
var lightBuilder = factory.LightBuilder;

using var light = lightBuilder.CreatePointLight(Location.Origin); // (1)!
```

1. 	`CreatePointLight()` requires only one parameter by default: Where in the world to place the light. 

	We can specify any `Location` in the world we like, but for now we'll place the light at the very centre of our 3D world, otherwise known as the world's `Origin`.

As always, the `light` is disposable.

For more information about lights, see: [:octicons-arrow-right-24: Lighting](/concepts/lighting.md)

### Putting Together a Scene

Now that we have a cube and a light, we need to place them in a *scene*. A scene is just an abstract concept that can be thought of as a "space" or "world" to place objects. Scenes also have backdrops, and you can have multiple scenes at any time. 

For now, we'll just create one scene, and add our light and our cube to it:

```csharp
var sceneBuilder = factory.SceneBuilder; // (1)!

using var scene = sceneBuilder.CreateScene(); // (2)!

scene.Add(cube); // (3)!
scene.Add(light); // (4)!
```

1. The `SceneBuilder` is a simple interface we get from the factory that allows us to create new scenes.
2. 	You can specify a different backdrop colour for your scene here if you wish.
	
	For example, for a red backdrop, use `CreateScene(backdropColor: ColorVect.FromRgb24(0xFF0000))`.

3. 	The cube instance we created earlier will not be shown until it's added to a scene (and then the scene must be rendered, more on that below). 
	
	The scene tracks which objects have been added to itself already; attempting to add the same object twice will result in an exception being thrown.

4. 	Just like the cube, our light will have no effect until it's added to a rendered scene.

	Although you can not add the same object or light to a scene more than once, you can add and remove each object/light to and from multiple scenes freely.

Of course, the `scene` is a disposable resource, just like the other resources so far.

### Creating a Window

Before we can render anything, we need a window to render it all in to. Let's create the window now:

```csharp
var displayDiscoverer = factory.DisplayDiscoverer; // (1)!
var windowBuilder = factory.WindowBuilder; // (2)!

var primaryDisplay = displayDiscoverer.Primary?.Value 
	?? throw new InvalidOperationException("No displays connected!"); // (3)!

using var window = windowBuilder.CreateWindow(primaryDisplay); // (4)!
```

1. The `DisplayDiscoverer` does exactly as its name describes and helps us find connected displays on the current machine. It can also show you the supported resolutions and refresh rates of each display.
2. The `WindowBuilder` helps you build windows-- pretty self-explanatory!
3. 	This line sets `primaryDisplay` to the `Primary` display as discovered by the display discoverer. If the machine only has one display, that display is always the primary display; otherwise it will usually be the "main" monitor on a multi-monitor setup.

	However, it is possible that the machine is running in a "headless" state (i.e. no displays at all are connected). In this case, `displayDiscoverer.Primary` will actually be `null`. 
	
	Therefore, this line of code is checking that there *is* a primary display by checking `Primary` against `null`, and if there is no display whatsoever it throws an exception for now (using [the C# null-coalescing operator](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/null-coalescing-operator)).

4. 	Here we simply create a new window on the primary display. 
	
	You can provide more parameters to `CreateWindow()` if you want to control things like the window title, size, position etc. The only required parameter is the target display-- the library will fill in sensible defaults for everything else for you for now.

Unlike other resources, the `primaryDisplay` is not disposable as you can not 'dispose' or otherwise destroy a display-- it's just a part of the application's environment. 

The `window`, of course, *is* disposable so we instantiate it with the `using` pattern as usual.

For more information about displays and windows, see: [:octicons-arrow-right-24: Displays & Windows](/concepts/displays_and_windows.md)

### Creating a Camera and Renderer

We now have:

* A cube instance;
* A point light;
* A scene that we've placed them both in, and;
* A window to render everything to.

The final things we need to create are a *camera* to render the scene from, and a *renderer* that handles the rendering of the scene.

```csharp
var cameraBuilder = factory.CameraBuilder;

using var camera = cameraBuilder.CreateCamera();
```

The camera is not added to the scene as it does not actually affect the scene in any way; it is only used as a parameter to the renderer ()

### Complete Example

This example is written as a single file (e.g. using C#'s [top-level statements](https://learn.microsoft.com/en-us/dotnet/csharp/tutorials/top-level-statements)). You may need to move the actual code in to a method if necessary.

```csharp
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Environment.Input;


// "Creating the Factory" (1)
using var factory = new LocalTinyFfrFactory();


// "Creating the Cube Mesh" (2)
var meshBuilder = factory.AssetLoader.MeshBuilder;

var cubeDest = new Cuboid(1f);
using var cubeMesh = meshBuilder.CreateMesh(cubeDesc);


// "Creating a Material for the Cube" (3)
var materialBuilder = factory.AssetLoader.MaterialBuilder;

using var colorMap = materialBuilder.CreateColorMap(StandardColor.Maroon);
using var material = materialBuilder.CreateOpaqueMaterial(colorMap);


// "Creating a Cube Instance" (4)
var objectBuilder = factory.ObjectBuilder;

using var cube = objectBuilder.CreateModelInstance(cubeMesh, material);


// "Illuminating the Cube" (5)
var lightBuilder = factory.LightBuilder;

using var light = lightBuilder.CreatePointLight(Location.Origin);


// "Putting Together a Scene" (6)
var sceneBuilder = factory.SceneBuilder;

using var scene = sceneBuilder.CreateScene();

scene.Add(cube);
scene.Add(light);


// "Creating a Window" (87)
var displayDiscoverer = factory.DisplayDiscoverer;
var windowBuilder = factory.WindowBuilder;

var primaryDisplay = displayDiscoverer.Primary?.Value 
	?? throw new InvalidOperationException("No displays connected!");

using var window = windowBuilder.CreateWindow(primaryDisplay);


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
```

1. [:material-arrow-up: Scroll up to "Creating the Factory"](#creating-the-factory)
2. [:material-arrow-up: Scroll up to "Creating the Cube Mesh"](#creating-the-cube-mesh)
3. [:material-arrow-up: Scroll up to "Creating a Material for the Cube"](#creating-a-material-for-the-cube)
4. [:material-arrow-up: Scroll up to "Creating a Cube Instance"](#creating-a-cube-instance)
5. [:material-arrow-up: Scroll up to "Illuminating the Cube"](#illuminating-the-cube)
6. [:material-arrow-up: Scroll up to "Putting Together a Scene"](#putting-together-a-scene)
7. [:material-arrow-up: Scroll up to "Creating a Window"](#creating-a-window)

## Result

![Image showing a standard cube displayed on a window.](hello_cube_cube.png)

