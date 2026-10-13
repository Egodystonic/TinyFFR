---
title: Dear ImGui Integration
description: Tutorial on integrating TinyFFR with the open-source cross-platform immediate-mode GUI library "Dear ImGui".
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * ImGui support is enabled via an optional additional package (`Egodystonic.TinyFFR.ImGui`) which has a dependency on `Hexa.NET.ImGui`. :material-arrow-right: [Installation & Initialization](#installation-initialization)
    * TinyFFR integrates ImGui by adding a new scene type (`ImGuiScene`) and treating it similarly to a [Canvas Scene](canvas_scenes.md). :material-arrow-right: [ImGui Scene Setup](#imgui-scene-setup)
    * You can also render TinyFFR scenes inside ImGui windows. :material-arrow-right: [Rendering to an ImGui image](#rendering-to-an-imgui-image)

</div>

## Installation & Initialization

Install `Egodystonic.TinyFFR.ImGui` from Nuget in to your application project.(1)
{ .annotate }

1. This package contains a transitive reference to `Egodystonic.TinyFFR` so you won't need to explicitly install both, but if you do decide to reference both please make sure their version numbers match.

TinyFFR's ImGui integration depends on [Hexa.NET.ImGui](https://github.com/HexaEngine/Hexa.NET.ImGui) version `2.*`.

## ImGui Scene Setup

Similar to using a [Canvas Scene](canvas_scenes.md), ImGui is rendered as a 2D scene, typically on top of your 3D render using a [Compositor](compositing.md).

Firstly, set up the scene using `CreateImGuiScene(...)`(1), then create a `Renderer` for that scene using `CreateRenderer(...)`(2):
{ .annotate }

1.	This extension method is provided in the `Egodystonic.TinyFFR.ImGui` package under the `Egodystonic.TinyFFR.World` namespace (the same namespace as the `ISceneBuilder` type).
2.	This extension method is provided in the `Egodystonic.TinyFFR.ImGui` package under the `Egodystonic.TinyFFR.Rendering` namespace (the same namespace as the `IRendererBuilder` type).

```csharp
using var imguiScene = factory.SceneBuilder.CreateImGuiScene(
	factory, // (1)!
	new ImGuiSceneCreationConfig { EnableGamepadNavigation = true } // (2)!
);

using var imguiRenderer = factory.RendererBuilder.CreateRenderer( // (3)!
	imguiScene, 
	window
);
```

1.	The factory itself must be passed in to the scene builder as the ImGui API requires being able to build meshes, textures, objects, cameras etc. on demand.

2.	This second parameter is optional but recommended if you'd like to customize the ImGui integration; use your IDE to investigate the properties on this config type if desired.

3.	The renderer construction method does not require a camera (much like a `CanvasScene`).

Finally, add the scene renderer to a compositor:

```csharp
using var compositor = factory.RendererBuilder.CreateCompositor(window); // (1)!
compositor.Add(sceneRenderer, RenderCompositionType.Standard); // (2)!
compositor.Add(imguiRenderer, RenderCompositionType.RetainPreviousScenes);
```

1.	Compositors are a standard library feature of TinyFFR; they combine multiple renders in to a final output image. See [Compositing](compositing.md) for more information.

2.	In this line, `sceneRenderer` is assumed to be your 'main' 3D scene renderer.

Adding the ImGui renderer last is what lets it appear "on top" of everything else (compositors render every added renderer in the order they were added, meaning the last-added renderer renders on top of everything else). It's important to specify the `compositionType` as `RenderCompositionType.RetainPreviousScenes`, otherwise the ImGui scene render will completely wipe everything else already rendered.

### Rendering to a Sub-Area

By default the interface spans the whole render target (window). To confine it to part of the target instead, set a render sub-area on the ImGui `Renderer` (e.g. via `SetRenderSubAreaFraction(...)`) and use a `BeginFrame(...)` overload that takes that renderer. The interface will then adopt the sub-area as its drawing region, and ImGui's own coordinates and mouse positions will correctly be interpreted relative to that sub-area.

## Loop

Create an [Application Loop](application_loops.md) as standard. Inside the loop, each frame, you should build the immediate ImGui layout and then invoke `RenderAll(...)` on the `compositor`.

```csharp
while (!loop.Input.UserQuitRequested) {
	var deltaTime = loop.IterateOnce().AsDeltaTime();
	
	// ...Insert your 3D scene rendering loop code here...

	imguiScene.BeginFrame(deltaTime, loop, window, imguiRenderer); // (1)!

	ImGui.ShowDemoWindow(); // (2)!

	imguiScene.EndFrame(); // (3)!

	compositor.RenderAll(); // (4)!
}
```

1.	Every frame you wish to render the ImGui display should start with `imguiScene.BeginFrame(...)`.

	`BeginFrame(...)` has various overloads to support differing uses, in most cases you should prefer the overload that allows you to pass the target `Window` and `Renderer`. Supplying the `Window` also lets TinyFFR apply ImGui's requested mouse cursor to that window, so resize handles and text carets change the real cursor; the overloads that take explicit sizes instead of a `Window` leave the cursor alone.

	The `ApplicationLoop` is required as TinyFFR uses it to manage text transcription on your behalf (see below).

2.	Replace this line with your actual ImGui code. In this example we simply opt to show the demo window.

3.	Every frame you wish to render the ImGui display should end with `imguiScene.EndFrame(...)`.

4.	This line renders everything added to the `compositor`, including the latest ImGui draw data.

??? note "BeginFrame / EndFrame vs NewFrame / Render"
	Rather than invoking `ImGui.NewFrame()` and `ImGui.Render()` manually, TinyFFR requires you to use `BeginFrame(...)` and `EndFrame(...)` on the active `imguiScene` object.
	
	`BeginFrame(...)` internally invokes `ImGui.NewFrame()`, sets up the ImGui context, and sets ImGui's IO display size, scaling, and delta time. It also passes all of TinyFFR's captured input data to ImGui, allowing ImGui to correctly react to user input. 
	
	`EndFrame(...)` internally invokes `ImGui.Render()`. You must still invoke `compositor.RenderAll()` to take that rendered draw data from ImGui and turn it in to something visible on-screen.

??? tip "Text Input & Transcription"
	ImGui text fields need the *characters* a user's keystrokes actually produce rather than raw key codes; in TinyFFR terms that means [text transcription](keyboard_and_mouse_input.md) must be enabled on the application loop. `BeginFrame(...)` handles this for you: each frame it switches the loop's `EnableInputTextTranscription` on whenever ImGui reports that it wants text input (i.e. whilst one of its text fields is focused), and off again once it doesn't.

	This is why `BeginFrame(...)` takes the `ApplicationLoop` rather than just its `Input`. You can opt out by setting `AutoManageTextInputTranscription` to `false` on the `ImGuiSceneCreationConfig`, in which case the setting is left entirely to you — but remember that ImGui's text fields will silently accept nothing at all until you enable it yourself.

	Whilst transcription is enabled, the operating system's own text input handling may consume some keystrokes, meaning they're reported only as transcribed text and not as key events. Because transcription follows ImGui's focus, this is scoped to the times when a text field is actually focused — but it's worth bearing in mind if you also read alphanumeric key events directly. Separately (and independently of transcription), ImGui swallows keyboard and mouse events whenever it has focus or the cursor is over one of its windows; query `ImGui.GetIO().WantCaptureKeyboard` / `WantCaptureMouse` if your own code needs to defer to it.
	
## Rendering to an ImGui Image

You may additionally wish to render a TinyFFR scene *to* an ImGui viewport/window (e.g. for a diagnostic output, model inspector, alternate angle viewer, etc).

To do this, first you must create a [RenderOutputBuffer](capturing_render_output.md) to render this data to. Then, create a dynamic texture from that buffer, and register that texture in your ImGui scene via `imguiScene.RegisterTexture(...)`. This returns an `ImTextureID` that you can subsequently use in any call to `ImGui.Image(...)`:

```csharp
// Step 1: Setup / initialization
using var viewportBuffer = factory.RendererBuilder.CreateRenderOutputBuffer((640, 480)); // (1)!
using var viewportTexture = viewportBuffer.CreateDynamicTexture(); // (2)!
using var viewportRenderer = factory.RendererBuilder.CreateRenderer( // (3)!
	viewportScene, 
	viewportCamera, 
	viewportBuffer
);
var imguiViewportTextureId = imguiScene.RegisterTexture(viewportTexture); // (4)!

// Step 2: Per-frame loop
while (!loop.Input.UserQuitRequested) {
	var deltaTime = loop.IterateOnce().AsDeltaTime();
	
	// ...Insert your 3D scene rendering loop code here...
	
	viewportRenderer.Render(); // (5)!

	imguiScene.BeginFrame(deltaTime, loop, window, imguiRenderer);

	ImGui.SetNextWindowPos(new Vector2(20f, 20f), ImGuiCond.FirstUseEver); // (6)!
	ImGui.SetNextWindowSize(new Vector2(640f, 480f), ImGuiCond.FirstUseEver);
	if (ImGui.Begin("TinyFFR Scene View", ImGuiWindowFlags.NoSavedSettings)) {
		ImGui.Text("Offscreen scene sampled as an ImGui image:");
		var available = ImGui.GetContentRegionAvail(); // (7)!
		var imageHeight = MathF.Max(available.Y - 4f, 32f); // (8)!
		unsafe { 
			ImGui.Image( // (9)!
				new ImTextureRef(null, imguiViewportTextureId), 
				new Vector2(available.X, imageHeight)
			);
		}
	}
	ImGui.End();

	imguiScene.EndFrame();

	compositor.RenderAll();
}

// Step 3: Teardown / cleanup
imguiScene.UnregisterTexture(imguiViewportTextureId); // (10)!
```

1.	This creates a 640x480 output buffer. Create a buffer appropriately sized for your expected use-case. The output ImGui image can still be freely resized, but the backing texture's resolution will remain at this size. If in doubt, creating an image larger than any expected output ImGui image size will prevent blurring/stretching.

	See [Capturing Render Output](capturing_render_output.md) for more information on using render output buffers.
	
2.	This returns a TinyFFR `Texture` object whose contents always show the latest scene rendered to `viewportBuffer`.

	See [Capturing Render Output](capturing_render_output.md) for more information on using render output buffers.
	
3.	This creates a `Renderer` capable of rendering `viewportScene` using `viewportCamera` in to our `viewportBuffer` (it's assumed that `viewportScene` and `viewportCamera` are created earlier).

	See [Capturing Render Output](capturing_render_output.md) for more information on using render output buffers.
	
4.	This registers any TinyFFR `Texture` with ImGui and returns an ImGui `ImTextureID` that we can pass to `ImGui.Image(...)` later on.

	The returned `ImTextureID` is valid until you call `imguiScene.UnregisterTexture(...)` to unregister it. You must not dispose the TinyFFR `Texture` used to create it (or the `RenderOutputBuffer` it's associated with) before unregistering it.
	
5.	This instructs TinyFFR to render `viewportScene` in to `viewportBuffer`, which in turn updates the contents of `viewportTexture` (and thus the image represented by `imguiViewportTextureId`).

6.	This block of code creates a small ImGui window that ultimately displays the rendered scene via the `imguiViewportTextureId` image ID.

	Explaining ImGui's API is beyond the scope of this document, but in summary this block creates a window that starts at the same size as the `viewportBuffer` and displays the registered image below some sample text.
	
7.	`GetContentRegionAvail()` returns the space still unused inside the current ImGui window, measured from the current layout position (i.e. what's left underneath the `ImGui.Text(...)` line above). Deriving the image's size from this is what makes the image track the window as the user resizes it, rather than remaining a fixed size.

8.	Two safeguards on the height. Subtracting a few pixels leaves a little padding at the bottom of the window, preventing the image from butting up against the edge and pushing a scrollbar in; the `MathF.Max(..., 32f)` stops the height reaching zero (or going negative) if the user shrinks the window right down, which would otherwise ask ImGui to draw a degenerate image.

9.	`ImTextureRef`'s constructor takes a pointer to ImGui-managed texture data followed by an `ImTextureID`, which is why this call must sit inside an `unsafe` block.

	We pass `null` for the pointer because this texture isn't one of ImGui's own (e.g. its font atlas): it's ours, and it's identified solely by the `ImTextureID` that `RegisterTexture(...)` handed back.

10.	Before `viewportTexture` (or its associated `viewportBuffer`) can be disposed, the ImGui texture must be unregistered. Note that it's the `ImTextureID` that's passed to `UnregisterTexture(...)`, not the `Texture` itself.
