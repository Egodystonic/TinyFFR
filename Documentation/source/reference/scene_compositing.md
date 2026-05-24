---
title: Scene Compositing
description: Snippet demonstrating how to create splitscreen, overlays, or picture-in-picture effects
---

## Code

Create a vertical splitscreen effect:

```csharp
var leftSideRenderer = factory.RendererBuilder.CreateRenderer(scene, leftCamera, window); // (1)!
leftSideRenderer.SetRenderSubAreaFraction(Orientation2D.Left, (0f, 0f), (0.5f, 1f)); // (2)!

var rightSideRenderer = factory.RendererBuilder.CreateRenderer(scene, rightCamera, window); // (3)!
rightSideRenderer.SetRenderSubAreaFraction(Orientation2D.Right, (0f, 0f), (0.5f, 1f)); // (4)!

var compositor = factory.RendererBuilder.CreateCompositor(window); // (5)!
compositor.Add(leftSideRenderer, RenderCompositionType.Standard);
compositor.Add(rightSideRenderer, RenderCompositionType.Standard);

// In render loop:
compositor.RenderAll(); // (6)!
```

1. 	This is a standard renderer targeting `window` and `scene` with its own camera (`leftCamera`).

2.	This sets a sub-area of the render target (the `window`) that this renderer will render to.

	The first parameter (`Orientation2D.Left`) sets the anchor side/corner of the sub-area (see [Anchors](#anchors) below for more information).
	
	The second parameter (`(0f, 0f)`) sets an `XYPair<float>` that determines the offset from the left-side for this sub-area (in this case we're setting 0% offset horizontally and vertically).
	
	The third parameter (`(0.5f, 1f)`) sets the size of the sub-area (in this case, 50% horizontal and 100% vertical).
	
3. 	This is a standard renderer targeting `window` and `scene` with its own camera (`rightCamera`) (e.g. same `scene` and `window` as the `leftSideRenderer` but with its own camera).

4.	This sets a sub-area of the render target (the `window`) that this renderer will render to. The parameters here are identical to those set on the `leftSideRenderer` except the sub-area is anchored to `Orientation2D.Right` instead of `Orientation2D.Left`.

5.	`CreateCompositor` returns a `RendererCompositor` which is used to composite (combine) multiple renders on to a single render target (`window`).

	Firstly, we create the compositor, and then add every individual renderer via `Add()`. The composition type is described in further detail below (see [RendererCompositor](#renderercompositor)).
	
6.	Finally, in the render loop, invoke `RenderAll()` on the compositor *instead of* calling `Render()` on each individual renderer.

Create a horizontal splitscreen effect:

```csharp
var topSideRenderer = factory.RendererBuilder.CreateRenderer(scene, topCamera, window); // (1)!
topSideRenderer.SetRenderSubAreaFraction(Orientation2D.Up, (0f, 0f), (1f, 0.5f)); // (2)!

var bottomSideRenderer = factory.RendererBuilder.CreateRenderer(scene, bottomCamera, window); // (3)!
bottomSideRenderer.SetRenderSubAreaFraction(Orientation2D.Down, (0f, 0f), (1f, 0.5f)); // (4)!

var compositor = factory.RendererBuilder.CreateCompositor(window); // (5)!
compositor.Add(topSideRenderer, RenderCompositionType.Standard);
compositor.Add(bottomSideRenderer, RenderCompositionType.Standard);

// In render loop:
compositor.RenderAll(); // (6)!
```

1. 	This is a standard renderer targeting `window` and `scene` with its own camera (`topCamera`).

2.	This sets a sub-area of the render target (the `window`) that this renderer will render to.

	The first parameter (`Orientation2D.Up`) sets the anchor side/corner of the sub-area (see [Anchors](#anchors) below for more information).
	
	The second parameter (`(0f, 0f)`) sets an `XYPair<float>` that determines the offset from the top-side for this sub-area (in this case we're setting 0% offset horizontally and vertically).
	
	The third parameter (`(1f, 0.5f)`) sets the size of the sub-area (in this case, 100% horizontal and 50% vertical).
	
3. 	This is a standard renderer targeting `window` and `scene` with its own camera (`bottomCamera`) (e.g. same `scene` and `window` as the `topSideRenderer` but with its own camera).

4.	This sets a sub-area of the render target (the `window`) that this renderer will render to. The parameters here are identical to those set on the `topSideRenderer` except the sub-area is anchored to `Orientation2D.Down` instead of `Orientation2D.Up`.

5.	`CreateCompositor` returns a `RendererCompositor` which is used to composite (combine) multiple renders on to a single render target (`window`).

	Firstly, we create the compositor, and then add every individual renderer via `Add()`. The composition type is described in further detail below (see [RendererCompositor](#renderercompositor)).
	
6.	Finally, in the render loop, invoke `RenderAll()` on the compositor *instead of* calling `Render()` on each individual renderer.

Create a picture-in-picture effect:

```csharp
var mainSceneRenderer = factory.RendererBuilder.CreateRenderer(scene, primaryCamera, window); // (1)!
var pipRenderer = factory.RendererBuilder.CreateRenderer(scene, pipCamera, window); // (2)!
pipRenderer.SetRenderSubAreaPixels(Orientation2D.DownRight, (300, 300), (568, 320)); // (3)!

var compositor = factory.RendererBuilder.CreateCompositor(window); // (4)!
compositor.Add(mainSceneRenderer, RenderCompositionType.Standard);
compositor.Add(pipRenderer, RenderCompositionType.Standard);

// In render loop:
compositor.RenderAll(); // (5)!
```

1. 	This is a standard renderer targeting `window` and `scene` with its own camera (`primaryCamera`).

2. 	This is a standard renderer targeting the same `window` and `scene` with its own camera (`pipCamera`). It does not need to be the same scene, we're just using the same scene for this example (i.e. maybe we're adding a "rear view" camera).

3.	This sets a sub-area of the render target (the `window`) that `pipRenderer` will render to.

	The first parameter (`Orientation2D.DownRight`) sets the anchor side/corner of the sub-area (see [Anchors](#anchors) below for more information).
	
	The second parameter (`(300, 300)`) sets an `XYPair<int>` that determines the offset from the bottom-right corner for this sub-area, in pixels.
	
	The third parameter (`(568, 320)`) sets the size of the sub-area (in this case, 568x320 pixels).

4.	`CreateCompositor` returns a `RendererCompositor` which is used to composite (combine) multiple renders on to a single render target (`window`).

	Firstly, we create the compositor, and then add every individual renderer via `Add()`. The ordering is important -- renderers are invoked in the order they're added, meaning that for the picture-in-picture overlay to appear over the top of our main scene, it must be added *after* the main scene renderer.
	
	The composition type is described in further detail below (see [RendererCompositor](#renderercompositor)).
	
5.	Finally, in the render loop, invoke `RenderAll()` on the compositor *instead of* calling `Render()` on each individual renderer.

Create a "portrait"/"inspector" overlay:

```csharp
var mainSceneRenderer = factory.RendererBuilder.CreateRenderer(mainScene, mainCamera, window); // (1)!
var itemRenderer = factory.RendererBuilder.CreateRenderer(itemScene, itemCamera, window); // (2)!
itemRenderer.SetRenderSubAreaPixels(Orientation2D.None, (300, -300), (568, 320)); // (3)!

var compositor = factory.RendererBuilder.CreateCompositor(window); // (4)!
compositor.Add(mainSceneRenderer, RenderCompositionType.Standard);
compositor.Add(itemRenderer, RenderCompositionType.RetainPreviousScenes);

// In render loop:
compositor.RenderAll(); // (5)!
```

1. 	This is a standard renderer targeting `window` and `mainScene` with its own camera (`mainCamera`).

2. 	This is a standard renderer targeting the same `window` but with a separate scene (`itemScene`) and camera (`itemCamera`).

3.	This sets a sub-area of the render target (the `window`) that `itemRenderer` will render to.

	The first parameter (`Orientation2D.None`) sets the anchor side/corner of the sub-area (see [Anchors](#anchors) below for more information).
	
	The second parameter (`(300, -300)`) sets an `XYPair<int>` that determines the offset from center for this sub-area, in pixels.
	
	The third parameter (`(568, 320)`) sets the size of the sub-area (in this case, 568x320 pixels).

4.	`CreateCompositor` returns a `RendererCompositor` which is used to composite (combine) multiple renders on to a single render target (`window`).

	Firstly, we create the compositor, and then add every individual renderer via `Add()`. The ordering is important -- renderers are invoked in the order they're added, meaning that for the rendered item to appear over the top of our main scene, it must be added *after* the main scene renderer.
	
	The composition type is described in further detail below (see [RendererCompositor](#renderercompositor)).
	
5.	Finally, in the render loop, invoke `RenderAll()` on the compositor *instead of* calling `Render()` on each individual renderer.

## Explanation

The code snippets above show various techniques to combine multiple scene captures/renders on to a single render target (e.g. a window or texture). The examples above all show rendering to a window, but rendering to a target texture works the same way.

### RendererCompositor

When compositing (combining) multiple renders together on to a single target window or texture, you should use a `RendererCompositor`. `CreateCompositor()` has a single required parameter: The render target that will be shared by all added renderers.

You can then add renderers to be composited via the `Add()` method. The order of addition is important: For each frame, renderers added first will be rendererd first, and renderers added later will overwrite any pre-existing pixel data created by the previous renderers. Generally speaking, this means that renderers added last will have their output always "on top" of renderers added earlier.

Every renderer added must have its own render target set to the same target object as the compositor.

When adding renderers you must set the `RenderCompositionType`:

* `Standard` is the standard composition type-- pixels written by the renderer will overwrite the current target's pixel buffer. If the renderer's target scene has no backdrop set, the render sub-area will be backfilled with black pixels. Use this composition type when you don't need layering as it enables the most optimisation.
* `RetainPreviousScenes` has the same effect as `Standard` *unless* the rendered `scene` has no backdrop set(1). When no backdrop is set, the rendered objects will be composited *on top* of previously-rendered pixels; allowing you to create compositional effects (i.e. rendering an object viewer on top of a UI, or creating a debug overlay, etc). Note that this composition type implies a heavier framerate cost.
	{ .annotate }

	1.	E.g. either the scene was created with `CreateScene(BuiltInSceneBackdrop.None)` or you set `scene.RemoveBackdrop()`.

??? question "Why can't I just call `Render()` on each individual `Renderer` in sequence?"
	When invoking `Render()` on each renderer individually, TinyFFR emits a frame start/end fence and flushes the back buffer each time.
	
	This means that the entire render target surface will be blanked for each individual `Render()` invocation, most likely resulting in a "flickering" artefact.
	
	Furthermore, when using the `RendererCompositor` TinyFFR is more free to make optimisations around skipping rendering certain pixel areas in the scene.
	
	Therefore, when rendering multiple scenes to a single target window/texture it's recommended to use a `RendererCompositor`.

### SetRenderSubAreaFraction vs SetRenderSubAreaPixel

When you want to render to a subsection of the render target you must invoke `SetRenderSubAreaFraction` or `SetRenderSubAreaPixel`.

Both methods take an *anchor* (explained further below) and two further parameters, the *offset* and the *dimensions*:

* The `anchor` param sets which 'side' or 'corner' of the render target the sub-area should be offset from. Anchors are described in more detail in the section below.
* The `offset` param sets how far displaced/offset the sub-area should be from the anchor side/corner. Negative/zero values are permitted.
* The `dimensions` param sets the size of the sub-area (e.g. width & height). Values are expected to be positive.

The difference between `SetRenderSubAreaFraction` and `SetRenderSubAreaPixel` is simply in how the `offset` and `dimensions` are defined:

* `SetRenderSubAreaFraction` sets the offset and dimensions of the sub-area as fractions of the render target's size. An offset of `(0.5f, -0.3f)` offsets the sub-area from the anchor horizontally by 50% of the render target's width and vertically by 30% of the render target's height. A size of `(0.25f, 0.1f)` sets the sub-area to take 25% of the render target's width and 10% of its height.
	* Defining a sub-area by fraction means the sub-area's actual pixel size + offset will be updated dynamically if/when the render target size changes.
* `SetRenderSubAreaPixels` sets the offset and dimensions of the sub-area as exact pixel values. An offset of `(500, -300)` offsets the sub-area from the anchor horizontally by 500 pixels and vertically by 300 pixels. A size of `(250, 100)` sets the sub-area to a 250x100 pixel viewport size.
	* Defining a sub-area by pixels means the sub-area's size and offset will not change as the render target's size changes.

### Anchors

The `Orientation2D` specified as the sub-area anchor defines where the sub-area will be offset *from* relative to the target window or texture. Broadly speaking, there are three 'subtypes' of anchor:

* **Corner anchors**: `UpLeft`, `UpRight`, `DownLeft`, `DownRight`:
	* When using a corner anchor, positive offset values will push the sub-area away from the target corner. 
	* Zero offset values will keep the sub-area perfectly in the corner. 
	* Negative values are supported but somewhat pointless: They simply result in a shrinking of the sub-area's total size.
* **Side anchors**: `Up`, `Down`, `Left`, `Right`:
	* When using a side anchor, the offset component matching the side's axis will push/pull the sub-area to/from the side. For example, when using an `Up` anchor a positive offset Y-component will push the sub-area down; when using a `Right` anchor a positive offset X-component will push the sub-area to the left. 
	* Conversely, the non-matching offset component will freely adjust the sub-area according to the [standard 2D TinyFFR convention](/tutorials/conventions.md#2d-handedness-orientation). For example, when using an `Up` anchor a positive X-component will push the sub-area rightward, a negative X-component will push the sub-area leftward.
* **Centralized**: `None`:
	* When using `Orientation2D.None` as your anchor the sub-area will be centralized to the render target by default, and then adjusted by the given offset.
	* Offset values will freely adjust the sub-area's position according to the [standard 2D TinyFFR convention](/tutorials/conventions.md#2d-handedness-orientation); i.e. positive X-values map to a rightward adjustment and positive Y-values map to an upward adjustment; negative X-values map to a leftward adjustment and negative Y-values map to a downward adjustment.
