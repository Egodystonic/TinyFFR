---
title: Camera Ray Casting (Pixel Picking)
description: Snippet demonstrating how to cast rays from a camera, optionally via picking a pixel/clicking on a window
---

## Code

Cast a ray from a window pixel:

```csharp
var clickLocation = loop.Input.KeyboardAndMouse.NewMouseClicks[0].Location; // (1)!
var ray = renderer.CastRayFromRenderSurface(clickLocation); // (2)!
```

1. 	Accessing `NewMouseClicks[0]` throws an exception if `NewMouseClicks.Count` is not at least `1`.

2.	The `clickLocation` parameter indicates which pixel on the render output surface (e.g. window) the ray should be cast from (it doesn't have to be a mouse click).

	It is assumed that the target `renderer` was created with the relevant `Camera` and `Window` for the ray cast operation.

Cast a ray from a camera's near plane:

```csharp
// With a camera instance
var ray = camera.CastRayFromNearPlane(); // (1)!
var ray = camera.CastRayFromNearPlane(new XYPair<float>(0f, 0f)); // (2)!

// Without a camera instance
var ray = CameraUtils.CreateRayFromCameraFrustumNearPlane(
	modelMatrix,
	projectionMatrix,
	rayCoord // (3)!
);
var ray = CameraUtils.CreateRayFromCameraFrustumNearPlane(
	cameraPosition,
	cameraViewDirection,
	cameraUpDirection,
	nearPlaneDistance,
	farPlaneDistance,
	verticalFov,
	aspectRatio,
	rayCoord
);
```

1.	This overload casts a ray from the very centre of the camera's near plane.

2.	This overload takes an `XYPair<float>` specifying where on the near plane the ray should originate from.

	This coordinate pair is expected as a normalized device coordinate, e.g. X and Y both in the range `-1` to `1` (where `(-1, -1)` is the bottom left corner).
	
3.	This argument specifies where on the near plane the ray should originate from; and is expected as a normalized device coordinate, e.g. X and Y both in the range `-1` to `1` (where `(-1, -1)` is the bottom left corner).


## Explanation

The code above demonstrates how to "pixel pick" in TinyFFR; this is where you convert a mouse click on a window to a ray cast out from the camera in to the 3D world.

The resultant `Ray` will have the following properties:

<span class="def-icon">:material-card-bulleted-outline:</span> `StartPoint`

:   This `Location` will indicate the point on the camera's near plane that the ray originates from.

<span class="def-icon">:material-card-bulleted-outline:</span> `Direction`

:   This `Direction` indicates where the ray points out from the `StartPoint` and is guaranteed to point within the camera's frustum.

A `Ray` has infinite length. If you want to limit its length to the camera's far plane, you can create a `BoundedRay` like so: `#!csharp var boundedRay = ray.ToBoundedRay(camera.FarPlaneDistance - camera.NearPlaneDistance);`
