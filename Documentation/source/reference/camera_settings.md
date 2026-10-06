---
title: Camera Settings
description: Information on how to create cameras, and on every setting that controls what a camera sees and how it turns the scene in to an image.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * `Camera`s have additional methods that make it easier to position + orient them. :material-arrow-right: [Position & Orientation](#position-orientation)
    * The `Camera`'s field of view, projection type, and near/far planes control how much of the scene it sees. :material-arrow-right: [Field of View](#field-of-view), [Projection Type](#projection-type), [Near & Far Planes](#near-far-planes)
    * Cameras can also blur out-of-focus objects. :material-arrow-right: [Depth of Field](#depth-of-field)

</div>

## Cameras

```csharp
using var camera = factory.CameraBuilder.CreateCamera( // (1)!
	new Location(0f, 1.5f, -3f), 
	Direction.Forward
);
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window); // (2)!

camera.LookAt(Location.Origin, Direction.Up); // (3)!
camera.VerticalFieldOfView = 45f; // (4)!
```

1.	Creates a camera 1.5m above the ground and 3m back from the origin, looking forward.

2.	Creates a renderer that captures `scene` from the camera's viewpoint and draws it in to `window`.

3.	Turns the camera to look at the origin, keeping "up" pointing upward.

4.	Narrows the camera's field of view to 45°, which "zooms in" slightly compared to the default of 60°.

A camera is given to a [`Renderer`](using_renderers.md) along with the scene it should capture. This means the same camera can be used to capture several different scenes, and the same scene can be captured by several cameras at once. Cameras are resources, so must be disposed when no longer needed.

### Creating Cameras

Cameras are created with `factory.CameraBuilder.CreateCamera()`, which accepts the following optional parameters:

<span class="def-icon">:material-code-json:</span> `initialPosition`

:   Where the camera is. Defaults to the origin. See [Position & Orientation](#position-orientation).

<span class="def-icon">:material-code-json:</span> `initialViewDirection`

:   Which way the camera is looking. Defaults to `Direction.Forward`. See [Position & Orientation](#position-orientation).

<span class="def-icon">:material-code-json:</span> `cameraRange`

:   A `CameraPlaneConfiguration` preset for how close and how far away an object may be and still be drawn. Defaults to `CameraPlaneConfiguration.Standard`. See [Near & Far Planes](#near-far-planes).

<span class="def-icon">:material-code-json:</span> `exposure`

:   A `CameraExposurePreset` matching the lighting in the scene the camera will capture. Defaults to `CameraExposurePreset.InsideBrightLighting`. See [Exposure](#exposure).

<span class="def-icon">:material-code-json:</span> `name`

:   An optional name for the camera.

Alternatively, you can pass a `CameraCreationConfig`, which exposes every setting:

```csharp
using var camera = factory.CameraBuilder.CreateCamera(new CameraCreationConfig {
	Position = new Location(0f, 10f, 0f),
	ViewDirection = Direction.Down,
	UpDirection = Direction.Forward,
	ProjectionType = CameraProjectionType.Orthographic,
	OrthographicHeight = 20f,
	NearPlaneDistance = 0.1f,
	FarPlaneDistance = 100f,
	InitialExposure = CameraExposurePreset.OutsideOvercast,
	Name = "Map Camera"
});
```

As well as the equivalents of the parameters above, `CameraCreationConfig` has the following properties:

<span class="def-icon">:material-card-bulleted-outline:</span> `UpDirection`

:   Which way is "up" for the camera. Defaults to `Direction.Up`.

<span class="def-icon">:material-card-bulleted-outline:</span> `FieldOfView` / `FieldOfViewIsVertical`

:   The camera's field of view, and whether it's measured vertically (`true`) or horizontally (`false`). Default to `60°` and `true` (i.e. a vertical field of view of 60°).

<span class="def-icon">:material-card-bulleted-outline:</span> `AspectRatio`

:   The width of the captured frame image divided by its height. Defaults to `16f / 9f`. You rarely need to set this, as renderers keep it up to date for you (see [Field of View](#field-of-view)).

<span class="def-icon">:material-card-bulleted-outline:</span> `NearPlaneDistance` / `FarPlaneDistance`

:   The exact near and far plane distances, in metres. Default to `0.03f` and `1,000f`.

<span class="def-icon">:material-card-bulleted-outline:</span> `ProjectionType`

:   Whether the camera uses a perspective or orthographic projection. Defaults to `CameraProjectionType.Perspective` (see [Projection Type](#projection-type)).

<span class="def-icon">:material-card-bulleted-outline:</span> `OrthographicHeight`

:   How tall a slice of the world the image shows, in metres, when `ProjectionType` is `Orthographic`. Defaults to `1f`.

<span class="def-icon">:material-card-bulleted-outline:</span> `InitialExposure`

:   The camera's starting exposure, as a `CameraExposureParams` (presets convert implicitly). Defaults to `CameraExposurePreset.InsideBrightLighting` (see [Exposure](#exposure)).

Every one of these settings can also be changed after creation via the camera's properties, described on the rest of this page. Invalid values in a `CameraCreationConfig` (e.g. a `Direction.None` view direction, or a far plane nearer than the near plane) throw an exception when the camera is created.

## Position & Orientation

```csharp
camera.Position = new Location(2f, 1f, -4f); // (1)!
camera.MoveBy(new Vect(0f, 0.5f, 0f)); // (2)!

camera.ViewDirection = Direction.Left; // (3)!
camera.LookAt(new Location(0f, 1f, 0f), Direction.Up); // (4)!
camera.SetViewAndUpDirection(Direction.Down, Direction.Forward); // (5)!
camera.RotateBy(30f % Direction.Up); // (6)!
```

1.	Moves the camera to (2, 1, -4).

2.	Moves the camera 0.5m upward from wherever it currently is.

3.	Turns the camera to look left.

4.	Turns the camera to look at the given point, with "up" pointing upward.

5.	Points the camera straight down, with the top of the image facing forward (useful for a top-down view).

6.	Turns the camera 30° around the up axis.

A camera's placement is described by three properties:

<span class="def-icon">:material-card-bulleted-outline:</span> `Position`

:   Where the camera is in the world.

<span class="def-icon">:material-card-bulleted-outline:</span> `ViewDirection`

:   Which way the camera is looking(1).
	{ .annotate }
	
	1.	Setting this causes `UpDirection` to be automatically orthogonalized against the new value.
	
		For this reason it's recommended to use `SetViewAndUpDirection(...)` to change this value; see below.

<span class="def-icon">:material-card-bulleted-outline:</span> `UpDirection`

:   Which way in the world points towards the top of the image. Changing this "rolls" the camera around its view direction(1).
	{ .annotate }
	
	1. 	Setting this causes `ViewDirection` to be automatically orthogonalized against the new value.
	
		For this reason it's recommended to use `SetViewAndUpDirection(...)` to change this value; see below.

The following methods are provided:

<span class="def-icon">:material-code-block-parentheses:</span> `SetViewAndUpDirection(viewDirection, upDirection)`

:   Sets `ViewDirection` and `UpDirection` together. 

	By default, the view and up directions are are maintained to be orthogonal; setting `ViewDirection` straightens `UpDirection` to match and vice-versa.

	Therefore using this method is the recommended approach to manually setting where the camera is looking, because it removes ambiguity about how TinyFFR should re-orthogonalize `UpDirection` when you change `ViewDirection`.
	
	This method also takes an optional `bool enforceOrthogonality` (default = `true`). Setting this to `false` disables the orthogonalization entirely which is mostly only useful for niche effects.

<span class="def-icon">:material-code-block-parentheses:</span> `LookAt(target, upDirection)`

:   Turns the camera to look at `target`, with `upDirection` straightened against the new view direction. There is also a single-argument overload, `LookAt(target)`, which leaves the up direction to be re-derived automatically. The two-argument overload is usually preferable, because it fixes the camera's roll rather than leaving it to chance. If `target` is the camera's own position, the view direction is left unchanged.

<span class="def-icon">:material-code-block-parentheses:</span> `MoveBy(translation)` / `RotateBy(rotation)`

:   Moves the camera by the given `Vect`, or turns its view direction by the given `Rotation` (or `Quaternion`).

<span class="def-icon">:material-code-block-parentheses:</span> `GetRelativeOrientationDirection(orientation)`

:   Converts an `Orientation` relative to the camera in to a direction in the world. For example, `Orientation.Forward` returns the camera's `ViewDirection`, and `Orientation.Right` returns whichever world direction is to the camera's right. This makes it easy to move a camera "forwards" or "sideways" without needing to know where it's pointing: `camera.MoveBy(camera.GetRelativeOrientationDirection(Orientation.Right) * 0.1f)`.

A camera can also be wrapped in a [`SceneObject`](scene_objects.md#sceneobject), which supports its position and rotation members.

## Field of View

```csharp
camera.VerticalFieldOfView = 75f; // (1)!
camera.HorizontalFieldOfView = 90f; // (2)!
```

1.	Sets the camera to take in 75° from the top of the image to the bottom.

2.	Sets the camera to take in 90° from the left of the image to the right (which also changes `VerticalFieldOfView`).

A camera's field of view is how wide an angle of the scene it takes in. A wider field of view fits more of the scene in to the image but makes everything in it smaller, and exaggerates perspective towards the edges of the image; a narrower one "zooms in". The default is a vertical field of view of `60°`.

`VerticalFieldOfView` and `HorizontalFieldOfView` are two aspects of the same setting, related by the camera's `AspectRatio`, so setting either one changes the other. It's usually best to set `VerticalFieldOfView`, because it keeps the framing consistent as the window is made wider or narrower.

The field of view only affects perspective cameras; see [Projection Type](#projection-type).

### Aspect Ratio

A camera's `AspectRatio` is the width of the image it produces divided by its height. When you create a renderer that targets a window, the renderer keeps the camera's aspect ratio in sync with the window as it's resized, so you normally never need to set it yourself.

However, if you're sharing one camera between several renderers with differently-shaped targets, you should disable this behaviour by setting `AutoUpdateCameraAspectRatio` to `false` in the `RendererCreationConfig` and setting `camera.AspectRatio` yourself appropriately.

Alternatively, create a copy of the `Camera` for each target renderer (camera objects are relatively cheap) and keep their position/orientation in sync.

## Projection Type

```csharp
camera.ProjectionType = CameraProjectionType.Orthographic;
camera.OrthographicHeight = 12f; // (1)!
```

1.	Makes the image show a 12m-tall slice of the world.

A camera's `ProjectionType` controls how it flattens the three-dimensional scene in to a two-dimensional image:

* 	__`CameraProjectionType.Perspective`:__ The default; and what most people expect when they think of 3D scenes. Objects further from the camera appear smaller, and parallel lines converge as they recede, as they would in a photograph or to the human eye. This is the right choice for most 3D rendering. The amount of the scene a perspective camera sees is set by its field of view.

* 	__`CameraProjectionType.Orthographic`:__ Objects appear the same size no matter how far away they are, and parallel lines stay parallel. This is the projection used for some technical and architectural drawings, isometric games, maps, and diagrams. This should be used anywhere it matters that two objects of the same size measure the same on screen regardless of their distance. 

	Because an orthographic camera's view does not converge, what it sees is a box of fixed size rather than a cone. Its `OrthographicHeight` sets the height of that box in metres (the width follows from the aspect ratio). Its field of view is ignored.

## Near & Far Planes

```csharp
camera.NearPlaneDistance = 0.01f; // (1)!
camera.FarPlaneDistance = 300f; // (2)!

using var outdoorCamera = factory.CameraBuilder.CreateCamera( 
	cameraRange: CameraPlaneConfiguration.LongRange // (3)!
);
```

1.	Allows objects as close as 1cm to the camera to be drawn.

2.	Stops drawing anything further than 300m away.

3.	Creates a camera suited to large outdoor scenes.

A camera draws nothing nearer than its `NearPlaneDistance` or further than its `FarPlaneDistance`. Anything closer than the near plane is clipped away; this is why an object appears to grow holes or disappear when the camera gets too close to it.

??? question "Why not just make the near plane distance zero (or really tiny)?"
	3D camera math works via something called the "perspective divide" which is what flattens all 3D space in to 2D (i.e. a perspective projection).
	
	The perspective projection doesn't store depth linearly. It maps view-space depth to the depth buffer in proportion to 1/z, so most of the buffer's precision is concentrated just beyond the near plane, and the far/near ratio determines how much precision is left for the rest of the scene. 
	
	As the near plane distance approaches zero, that ratio grows without bound and almost all depth resolution is spent on a sliver of space directly in front of the camera, leaving distant geometry to fight over a handful of values and causing ["Z-fighting"](https://en.wikipedia.org/wiki/Z-fighting). This means setting a tiny near-plane distance will cause distant objects to flicker.
	
	At *exactly* zero things get even worse. The projection degenerates entirely and every point maps to the same depth value, meaning the depth test can no longer tell surfaces apart, and the projection matrix itself becomes singular (meaning your scene won't render at all).
	
	In practice, you should set the near plane as far out as your scene allows, since pushing it out improves precision far more than pulling the far plane in.
	
??? question "Why not just make far plane distance huge?"
	It's tempting to make the near plane tiny and the far plane enormous, but the further apart the two planes are, the less precisely the renderer can tell which of two nearly-touching surfaces is in front. 	
	
	More specifically, the ratio between `NearPlaneDistance` and `FarPlaneDistance` directly determines the level of precision for the depth buffer. A ratio too high shows up as flickering where surfaces meet (i.e. ["Z-fighting"](https://en.wikipedia.org/wiki/Z-fighting)).
	
	Ultimately this all comes down to the finite precision of a floating-point depth buffer.
	
	Keep the near plane as far away as your scene allows, as it has much more influence on precision than the far plane. The far plane should be no further than necessary to correctly render your scene.

To prevent unusable configurations, the following limits are enforced:

* The near plane can be no closer than `Camera.NearPlaneDistanceMin` (`1E-5f`). Smaller or invalid values are raised to this minimum.
* The far plane must be further than the near plane.
* The far plane can be no more than `Camera.NearFarPlaneDistanceRatioMax` (`1,000,000`) times further than the near plane.

When setting one plane would break these limits, the *other* plane is moved to suit. For example, setting `NearPlaneDistance` to `0.0001f` lowers the far plane to at most `100f` (one million times further). Read the two properties back after setting them if you need to be sure of their values.

When creating a camera with `CreateCamera()`, the `cameraRange` parameter picks a sensible pairing for you:

| `CameraPlaneConfiguration` | Near Plane | Far Plane | Suits |
| :------------------------- | ---------: | --------: | :---- |
| `Standard` (default) | 0.03m | 1,000m | Most scenes. |
| `CloseRange` | 0.01m | 333m | Cameras that get right up against objects, such as a first-person view or an inspector that zooms in closely. |
| `LongRange` | 0.15m | 5,000m | Outdoor scenes with a distant horizon, where objects never come close to the camera. |

## Exposure

```csharp
camera.SetExposure(CameraExposurePreset.OutsideMidday); // (1)!
camera.Exposure *= 1.5f; // (2)!
```

1.	Exposes the camera for a sunny day.

2.	Makes the image 1.5× brighter.

A camera's `Exposure` (a `CameraExposureParams`, made up of an aperture, a shutter speed, and a sensitivity) controls how bright the final image is for a given amount of light in the scene. The default is `CameraExposurePreset.InsideBrightLighting`, which the default brightness of every light and backdrop is calibrated for.

Exposure is explained in full, along with how to calibrate it against the lights and backdrops in a scene, on its own page: [Exposure & Brightness](exposure_and_brightness.md#camera-exposure).

## Depth of Field

```csharp
camera.FocusDistance = 2.5f; // (1)!
camera.FocusDistance = null; // (2)!
```

1.	Keeps objects 2.5m from the camera sharp, and blurs objects nearer or further away.

2.	Disables the effect, making the whole image sharp again.

Setting a camera's `FocusDistance` enables the depth-of-field effect. Objects nearer or further than the given distance (in metres) are blurred, in the way a real lens blurs objects that are out of focus. This can make a shot feel more photographic, but costs rendering time. `FocusDistance` defaults to `null`, which disables the effect.

The strength of the blur depends on three things:

* How far an object is from the focus distance.
* The camera's aperture: A wider aperture (smaller f-number) gives a stronger blur, as on a real camera. See [Exposure & Brightness](exposure_and_brightness.md#camera-exposure). (Multiplying or dividing `camera.Exposure` only changes its sensitivity, so it doesn't alter the blur.)
* The renderer's `RenderQualityConfig`: `DepthOfFieldStrength` scales the effect (`0f` hides it entirely), and `DepthOfFieldQuality` trades its quality against performance. See [Render Quality](render_quality.md).

## Rays

It's possible to construct a [Ray](lines.md#ray) in the world pointing along the camera's point-of-view.

```csharp
var forwardRay = camera.CreateRayFromNearPlane(); // (1)!
var cornerRay = camera.CreateRayFromNearPlane(new XYPair<float>(-1f, 1f)); // (2)!
var clickRay = renderer.CreateRayFromRenderSurface(mousePixelCoord); // (3)!
```

1.	A ray starting at the centre of the camera's near plane and travelling straight forward (e.g. for finding what a first-person crosshair is pointing at).

2.	A ray starting at the top-left corner of the camera's near plane, travelling out in to the scene through that corner of the image.

3.	A ray travelling out in to the scene through the given pixel of the renderer's output (e.g. for finding what the user clicked on).

	See also `renderer.PickModelInstanceFromRenderSurface(...)`.

`CreateRayFromNearPlane()` with no arguments returns a ray travelling straight forward from the centre of the camera's near plane.

`CreateRayFromNearPlane(normalizedNearPlaneCoord)` returns a ray starting at a given point on the near plane, expressed as a normalized fraction of its size (i.e. `(0, 0)` is the centre of the image, and each component runs from `-1` to `1` at the edges). For a perspective camera the ray fans outward as the field of view does; for an orthographic camera every ray travels straight forward.

!!! note "Ray StartPoint is at Near Plane"
	Note that with both methods the returned `Ray`'s `StartPoint` is at the camera's near plane, not at the camera's `Position`.

To convert a pixel coordinate (such as a mouse click) in to a ray, use `Renderer.CreateRayFromRenderSurface(pixelCoord)` instead, which accounts for the size of the render target for you. See also: `renderer.PickModelInstanceFromRenderSurface(...)`.

Rays are often used with [scene queries](scenes.md#scene-queries) to find which objects lie along them.

## Matrices

For interoperating with other graphics code or allowing bespoke control, a camera exposes its three matrices:

<span class="def-icon">:material-code-block-parentheses:</span> `GetProjectionMatrix()` / `SetProjectionMatrix(matrix)`

:   Gets / sets the projection matrix.

<span class="def-icon">:material-code-block-parentheses:</span> `GetModelMatrix()` / `SetModelMatrix(matrix)`

:   Gets / sets the model matrix (i.e. the matrix that defines the transform of this camera).

<span class="def-icon">:material-code-block-parentheses:</span> `GetViewMatrix()` / `SetViewMatrix(matrix)`

:   Gets / sets the view matrix (i.e. the opposite of the model matrix).

Each getter also has an overload with an `out Matrix4x4` parameter.

???+ warning "Use Matrices or Properties; Not Both"
	Setting a matrix directly bypasses the properties that would otherwise determine it, and those properties are not updated to match. For example, after `SetModelMatrix()`, `camera.Position` still returns the camera's previous position.

	Furthermore, the next time you set a related property, the matrix is recalculated from the properties, discarding what you set. For example, setting `camera.VerticalFieldOfView` after `SetProjectionMatrix()` replaces your projection matrix. If you set matrices directly, avoid setting the related properties afterwards.

## Camera Controllers

Rather than positioning and aiming a camera by hand each frame, you can attach a *camera controller* to it. Camera controllers implement common styles of camera movement (first-person, orbital, follow-cam, etc.), and can be driven directly by user input:

```csharp
using var controller = camera.CreateController<OrbitalCameraController>();

// Per-frame:
controller.AdjustAllViaDefaultControls(input.KeyboardAndMouse, deltaTime);
controller.Progress(deltaTime);
```

See [Camera Controllers](camera_controllers.md) for more information.
