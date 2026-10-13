---
title: Ray Casting, Pixel-Picking, Projecting
description: Information on how to convert between positions on screen and locations in the world with TinyFFR; including creating rays, picking objects under the cursor, and projecting world locations on to the screen.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * A screen position can be turned in to a `Ray` travelling in to the world. :material-arrow-right: [Ray Casting](#ray-casting)
    * The renderer can also tell you exactly which object is drawn at a given pixel. :material-arrow-right: [Pixel-Picking](#pixel-picking)
    * Inversely, a location in the world can be projected on to the screen. :material-arrow-right: [Projecting](#projecting)

</div>

## Coordinate Spaces

```csharp
var cursor = loop.Input.KeyboardAndMouse.MouseCursorPosition; // (1)!

var ray = renderer.CreateRayFromRenderSurface(cursor); // (2)!
var pick = renderer.PickModelInstanceFromRenderSurface(cursor); // (3)!
var screenPosition = renderer.ProjectOnToRenderSurfacePixels(myObject.Position); // (4)!
```

1.	The mouse cursor's position in the window, in pixels from the top-left corner.

2.	A ray travelling out in to the world through the pixel under the cursor (see [Ray Casting](#ray-casting)).

3.	The object drawn under the cursor, if any (see [Pixel-Picking](#pixel-picking)).

4.	Where `myObject` appears in the window, in the same pixel coordinates as the cursor (see [Projecting](#projecting)).

The methods on this page convert between locations in the 3D world and positions on a renderer's output, which can be expressed in a few different ways:

* **Pixel coordinates** are a position on the renderer's window or buffer, in pixels. By default these are measured from the top-left corner, and are in the same coordinate space as the mouse cursor (`MouseCursorPosition`), so the cursor position can be passed in directly.

	When the operating system scales the display (e.g. at 150% or 200%), the window's size in cursor coordinates differs from its size in physical pixels; TinyFFR accounts for this automatically. Pass `disableDpiScalingAdjustment: true` if you're already working in physical pixels. The [WPF](wpf_integration.md), [Avalonia](avalonia_integration.md), and [WinForms](winforms_integration.md) integrations work the same way: The `MouseCursorPosition` they report can be passed straight in.

* **Fractions** are a position as a fraction of the renderer's output: `(0, 0)` is the top-left corner and `(1, 1)` the bottom-right. Fractions are unaffected by the window's size and the display's scaling, so they're often the simplest choice when placing things on a [canvas](canvas_scenes.md).

* **Normalized near-plane coordinates** are a position on the camera's near plane (i.e. its image), independent of any renderer: `(0, 0)` is the centre of the image, and each component runs from `-1` to `1` at the edges (with positive Y upwards). The methods on `Camera` (and `CameraUtils`) use these.

<span class="def-icon">:material-code-json:</span> `coordOrigin`

:   Every renderer method on this page takes an optional `coordOrigin`, which selects which corner of the render target is `(0, 0)` (`DiagonalOrientation2D.UpLeft` by default). `DiagonalOrientation2D.None` measures from the centre instead.

When a renderer draws to only part of its target (a [render sub-area](compositing.md#render-sub-areas)), each method comes in two versions: One measuring from the whole window or buffer (e.g. `CreateRayFromRenderSurface()`), and one measuring from the sub-area (e.g. `CreateRayFromRenderSubAreaSurface()`). Without a sub-area, the two are identical.

??? tip "`RenderSurface()` vs `SubAreaSurface()`: Which Variant to Use?"
	When a renderer draws to its entire render target (the default), the `[...]RenderSurface()` and `[...]SubAreaSurface()` variants of each method give identical results.

	When a renderer has a [render sub-area](compositing.md#render-sub-areas), choose the variant that matches the coordinates you're working with:

	* Use the `[...]RenderSurface()` variants with positions measured from the whole window, such as the mouse cursor's `MouseCursorPosition`, or a canvas covering the whole window. These methods account for the sub-area themselves.
	* Use the `[...]SubAreaSurface()` variants with positions measured from the sub-area itself, such as a canvas confined to the same sub-area.

	Either way, only the sub-area is considered: Picking a pixel outside it returns `null`, and projecting treats locations outside it as out of view.

??? info "Current State, Not Most Recent Frame"
	All of these methods use the *current* state of the camera, scene, and render target (including the window's current size and the renderer's sub-area), not the state when the last frame was rendered. So if you move the camera or resize the window and then cast a ray, the ray reflects the change even before the next frame is rendered.

## Ray Casting

```csharp
var centerRay = camera.CreateRayFromNearPlane(); // (1)!
var cornerRay = camera.CreateRayFromNearPlane(new XYPair<float>(-1f, 1f)); // (2)!
var clickRay = renderer.CreateRayFromRenderSurface(mousePixelCoord); // (3)!

var clickedObject = scene.QueryProvider.GetFirstIntersection(clickRay); // (4)!
```

1.	A ray starting at the centre of the camera's near plane and travelling straight forward (e.g. for finding what a first-person crosshair is pointing at or simply asking "what is the camera looking directly at?").

2.	A ray starting at the top-left corner of the camera's near plane, travelling out in to the scene through that corner of the image.

3.	A ray travelling out in to the scene through the given pixel of the renderer's output (e.g. for finding what the user clicked on).

4.	Finds the nearest object whose bounding box the ray passes through (see [Scene Queries](scenes.md#scene-queries)).

A ray ([`Ray`](lines.md#ray)) is a line that starts at a point and travels forever in one direction. Rays created from the camera start at a point on the camera's near plane and travel out in to the scene, passing through everything that would be drawn at that point on screen.

<span class="def-icon">:material-code-block-parentheses:</span> `camera.CreateRayFromNearPlane()`

:   Returns a ray travelling straight forward from the centre of the camera's near plane.

<span class="def-icon">:material-code-block-parentheses:</span> `camera.CreateRayFromNearPlane(normalizedNearPlaneCoord)`

:   Returns a ray starting at the given (normalized) point on the camera's near plane. For a perspective camera, rays fan outward as the field of view does; for an orthographic camera, every ray travels straight forward.

<span class="def-icon">:material-code-block-parentheses:</span> `renderer.CreateRayFromRenderSurface(pixelCoord)` / `renderer.CreateRayFromRenderSubAreaSurface(pixelCoord)`

:   Returns a ray travelling through the given pixel of the renderer's output (relative to the whole window/buffer, or to the renderer's sub-area). Use this to turn a mouse click in to a ray.

`CameraUtils` also offers static `CreateRayFromPerspectiveCameraParameters()` and `CreateRayFromOrthographicCameraParameters()` methods, which perform the same calculation as `camera.CreateRayFromNearPlane()` given a camera's matrices or parameters, without needing a `Camera`.

!!! note "Ray StartPoint is at Near Plane"
	A ray created from a camera starts at the camera's near plane, not at the camera's `Position`.

Rays are most often used with [scene queries](scenes.md#scene-queries), which find the objects a ray passes through. To see a ray while debugging, draw it with a [scene primitive](scene_primitives.md) (e.g. `scene.AddPrimitiveShape(ray)`).

## Pixel-Picking

```csharp
var kbm = loop.Input.KeyboardAndMouse;
if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.MouseLeft)) {
	if (renderer.PickModelInstanceFromRenderSurface(kbm.MouseCursorPosition) is { } pick) { // (1)!
		Console.WriteLine($"Clicked {pick.ModelInstance} at {pick.Position}"); // (2)!
	}
}
```

1.	Finds which model instance is drawn at the cursor's position; or `null` if there's nothing there.

2.	`pick.ModelInstance` is the object that was clicked, and `pick.Position` is the location in the world of the exact point that was clicked on its surface.

*Pixel-picking* asks the renderer which object is drawn at a given pixel. Unlike a ray query (which tests against objects' bounding boxes), it finds exactly the object visible at that pixel, taking the objects' actual shapes in to account.

<span class="def-icon">:material-code-block-parentheses:</span> `renderer.PickModelInstanceFromRenderSurface(pixelCoord)` / `renderer.PickModelInstanceFromRenderSubAreaSurface(pixelCoord)`

:   Returns a `PixelPickResult` describing the `ModelInstance` drawn at the given pixel (relative to the whole window/buffer, or to the renderer's sub-area), and the `Position` in the world of the picked point on its surface; or `null` if no model instance is drawn there.

	Objects with transparency are ignored by default (so you can pick through them); pass `includeTransparentObjects: true` to include them.

Picking works by rendering the scene again (in to a separate, off-screen buffer) and reading the result back, which blocks until the GPU has finished. Each pick therefore costs roughly a frame's worth of rendering plus a pause while the CPU waits for the GPU, so it's intended for discrete events such as mouse clicks rather than for every frame. Nothing is drawn to the window, so it's safe to pick with renderers that are part of a [compositor](compositing.md).

A few further points to note:

* As the scene is re-rendered at the moment you pick, the result reflects the scene's *current* state rather than what's currently displayed.
* Objects that wrap a model instance (such as [quads](quads.md) and [text instances](text_instances.md)) are picked as their underlying `ModelInstance`.
* [Scene primitives](scene_primitives.md) are never returned. A primitive drawn in front of an object still hides that object from the pick, so picking that pixel returns `null`.

### Ray Query or Pixel-Pick?

| | Ray + scene query | Pixel-pick |
| :-- | :-- | :-- |
| Tests against | Objects' bounding boxes | The rendered image (exact shapes) |
| Results | The nearest object, or every object along the ray | The single object visible at the pixel |
| Cost | Cheap enough to use every frame | A full extra render; use for discrete events |
| Hit position | Not given | The exact point on the object's surface |
| Transparent objects | Always included | Excluded by default |
| Scene primitives | Ignored | Never returned, but block objects behind them |

## Projecting

```csharp
var nearPlaneCoord = camera.ProjectOnToNearPlane(myObject.Position); // (1)!
var pixelCoord = renderer.ProjectOnToRenderSurfacePixels(myObject.Position); // (2)!
var fraction = renderer.ProjectOnToRenderSurfaceFractionClamped(myObject.Position, out var isOffScreen); // (3)!
```

1.	Where the object appears on the camera's near plane, as a normalized coordinate (the same coordinates `CreateRayFromNearPlane()` takes); or `null` if the object is behind the camera or off the edge of the image.

2.	Where the object appears on the renderer's output in pixels, measured from the top-left corner (the same coordinates `CreateRayFromRenderSurface()` takes); or `null` if it's out of view.

3.	Where the object appears as a fraction of the renderer's output, clamped to the edge of the image if it's out of view. `isOffScreen` is set to `true` when clamping occurred.

Projecting is the *opposite* of ray casting; it finds where a location in the world appears on screen. This is useful for drawing something on a [canvas](canvas_scenes.md) over an object in the world, such as a name tag, a target marker, or an off-screen indicator.

Every projection method comes in three forms:

* The standard form (e.g. `ProjectOnToNearPlane()`) returns `null` when the location is out of view (i.e. behind or beyond the edges of the camera's point-of-view).
* The `Clamped` form (e.g. `ProjectOnToNearPlaneClamped()`) never returns `null`. When the location is out of view, the result is given clamped to the edges of the view, according to which direction you'd need to turn the camera to most quickly "find" it. A location directly behind the camera is placed at the centre of the bottom edge.
* The `Clamped` form with an `out bool wasClamped` argument additionally tells you whether the location was out of view & clamped.

The camera's near and far plane distances are not taken in to account, so distant objects can still be tracked.

<span class="def-icon">:material-code-block-parentheses:</span> `camera.ProjectOnToNearPlane(location)`

:   Returns where the location appears on the camera's near plane, as a normalized coordinate.

<span class="def-icon">:material-code-block-parentheses:</span> `renderer.ProjectOnToRenderSurfacePixels(location)` / `renderer.ProjectOnToRenderSurfaceFraction(location)`

:   Return where the location appears on the renderer's window or buffer, in pixels or as a fraction of its size.

	If the renderer has a [render sub-area](compositing.md#render-sub-areas), only locations that appear within the sub-area count as in view (and clamped results lie on the sub-area's edge), but the result is still measured from the whole window or buffer.

<span class="def-icon">:material-code-block-parentheses:</span> `renderer.ProjectOnToRenderSubAreaSurfacePixels(location)` / `renderer.ProjectOnToRenderSubAreaSurfaceFraction(location)`

:   As above, but measured from the renderer's render sub-area rather than the whole window or buffer.

`CameraUtils` also offers static `ProjectOnToPerspectiveCameraNearPlane...()` and `ProjectOnToOrthographicCameraNearPlane...()` methods, which perform the same calculation as `camera.ProjectOnToNearPlane()` given a camera's matrices or parameters, without needing a `Camera`.

### Tracking Objects on a HUD

```csharp
marker.CanvasAnchor = Orientation2D.UpLeft; // (1)!
marker.ObjectAnchor = Orientation2D.None;

// Per-frame:
marker.PositionFraction = sceneRenderer.ProjectOnToRenderSurfaceFractionClamped(trackedObject.Position, out var isOffScreen); // (2)!
marker.Opacity = isOffScreen ? 0.5f : 1f; // (3)!
```

1.	`marker` is a `CanvasImage` on a canvas drawn over the 3D scene. Its position is measured from the canvas's top-left corner (matching the projected fraction), and the marker is centred on that position.

2.	Moves the marker to where the tracked object appears; or to the edge of the screen, on the side facing the object, when it's out of view.

3.	Fades the marker while it's acting as an off-screen indicator.

To show the marker only while the object is in view, use `ProjectOnToRenderSurfaceFraction()` instead and hide the marker when it returns `null`. See [Canvas Scenes: Tracking 3D Objects](canvas_scenes.md#tracking-3d-objects) for more on the canvas side.

## Canvas Hit-Testing

The methods above all deal with the 3D world. To find which [canvas](canvas_scenes.md) object (2D image or text) is under the cursor, use the canvas's own hit-testing methods instead (`canvas.ConvertRenderTargetCoordToLocal()`, `canvasObject.Contains()`, and `canvas.QueryProvider`); see [Canvas Scenes: Hit-Testing](canvas_scenes.md#hit-testing).
