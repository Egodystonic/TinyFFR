---
title: Reacting to User Input
description: An example showing how to react to keyboard and mouse input in TinyFFR; flying a camera around, adding and removing objects with key presses, and typing text in to the scene.
---

<div class="grid cards" markdown>

-   :octicons-beaker-16:{ : style="margin-right:0.3em" } __Overview__

    ![Image showing coloured cubes and a text label floating above a chequered ground plane.](reacting_to_user_input_preview.png){ align=right : style="max-width:65%; margin: 0em; margin-left: 1em;" }
    
    This example demonstrates how to react to keyboard and mouse input. 
    
    It's a small console application that lets you fly a camera around a scene, add and remove cubes, and type text in to the world.
    
    [:simple-github: View Code on Github](https://github.com/Egodystonic/TinyFFR/tree/main/Examples/UserInput){ : style="position: absolute; bottom: 1em;" }

</div>

This tutorial builds on [Hello Cube](hello_cube.md). In this example, we will:

* Fly a camera around with the mouse and keyboard, using a camera controller;
* Exit the application when Escape is pressed;
* Add a randomly-coloured cube in front of the camera when Space is pressed;
* Remove everything from the scene when Backspace is pressed;
* Let the user type a text label in to the scene when Enter is pressed;
* Grow and shrink the most recently added cube with the mouse wheel.

It's assumed that you've read the [Hello Cube](hello_cube.md) tutorial first, as things that were explained there won't be explained again in detail here.

## Project Setup

Unlike Hello Cube, this example is set up as an ordinary .NET console application project. You can create one in an empty folder with the following two commands:

```
dotnet new console
dotnet add package Egodystonic.TinyFFR --prerelease
```

Alternatively, create a folder containing a `UserInput.csproj` file with the following contents:

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<OutputType>Exe</OutputType>
		<TargetFramework>net10.0</TargetFramework>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Egodystonic.TinyFFR" Version="*-*" /> <!-- (1)! -->
	</ItemGroup>

</Project>
```

1.	This adds TinyFFR to the project. `*-*` selects the latest published version of TinyFFR (including pre-release versions). You can replace it with a specific version (e.g. `Version="1.0.0"`) to pin your project to that version.

Either way, replace the contents of `Program.cs` with the code shown below in [Annotated Code](#annotated-code).

#### Running the Application

Open a terminal or command prompt in your project folder and run: `dotnet run -c Release`.

A window will appear showing a chequered ground plane, and the mouse cursor will be captured by the window. The controls are:

| Input | Action |
| :-- | :-- |
| Mouse | Look around |
| Arrow keys | Fly forward/backward/left/right |
| Right Shift / Right Ctrl | Fly up / down |
| Space | Add a cube in front of the camera |
| Enter | Start typing a text label in front of the camera; press Enter again to confirm it |
| Backspace | Remove everything from the scene (or, while typing, delete the last character) |
| Mouse wheel | Grow / shrink the last cube you added |
| Escape | Exit |

## Annotated Code

This is the entirety of the "User Input" example's `Program.cs`. Click on the :material-message-question: icons next to each line to learn more.

```csharp
using System.Text;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

using var factory = new LocalTinyFfrFactory(); // (1)!
var primaryDisplay = factory.DisplayDiscoverer.Primary ?? throw new InvalidOperationException("No display connected!");
using var window = factory.WindowBuilder.CreateWindow(primaryDisplay);
window.LockCursor = true; // (2)!
using var scene = factory.SceneBuilder.CreateScene();
using var camera = factory.CameraBuilder.CreateCamera();
using var cameraController = camera.CreateController<FreeFlyingCameraController>(); // (3)!
cameraController.Position = (0f, 1f, -3f); // (4)!
using var renderer = factory.RendererBuilder.CreateRenderer(scene, camera, window);
using var appLoop = factory.ApplicationLoopBuilder.CreateLoop();

scene.AddPrimitiveShape(new Plane(Direction.Up, Location.Origin)); // (5)!

ScenePrimitive? textBeingTyped = null; // (6)!
var textLocation = Location.Origin;
var typedText = new StringBuilder();
const string PlaceholderText = "Awaiting Text Input...";

ScenePrimitive? lastCube = null; // (7)!
var lastCubeShape = PositionedRotatedCuboid.UnitCubeAtOriginUnrotated;

while (!appLoop.Input.UserQuitRequested) {
	var deltaTime = appLoop.IterateOnce().AsDeltaTime();
	var keyboardAndMouse = appLoop.Input.KeyboardAndMouse; // (8)!

	if (keyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) break; // (9)!

	var spawnLocation = camera.Position + camera.ViewDirection * 2f; // (10)!

	if (textBeingTyped is { } text) { // (11)!
		typedText.Append(keyboardAndMouse.TranscribedText); // (12)!
		if (keyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Backspace) && typedText.Length > 0) typedText.Length--; // (13)!
		text.SetGeometryString(textLocation, typedText.Length > 0 ? typedText.ToString() : PlaceholderText, ScenePrimitiveSize.VeryLarge); // (14)!

		if (keyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Return)) { // (15)!
			text.SetPaintbrush(new PrimitivePaintbrush(StandardColor.Green, StandardColor.Black));
			appLoop.EnableInputTextTranscription = false;
			textBeingTyped = null;
		}
	}
	else {
		if (keyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) { // (16)!
			lastCubeShape = new PositionedRotatedCuboid(0.5f, 0.5f, 0.5f, spawnLocation, Rotation.None);
			lastCube = scene.AddPrimitiveShape(lastCubeShape, new PrimitivePaintbrush(ColorVect.RandomOpaque())); // (17)!
		}

		if (keyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Backspace)) {
			scene.RemoveAll(); // (18)!
			scene.AddPrimitiveShape(new Plane(Direction.Up, Location.Origin)); // (19)!
			lastCube = null;
		}

		if (keyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Return)) {
			typedText.Clear();
			textLocation = spawnLocation;
			textBeingTyped = scene.AddPrimitiveString(textLocation, PlaceholderText, ScenePrimitiveSize.VeryLarge); // (20)!
			appLoop.EnableInputTextTranscription = true; // (21)!
		}
	}

	if (keyboardAndMouse.MouseScrollWheelDelta != 0 && lastCube is { } cube) { // (22)!
		lastCubeShape = lastCubeShape.ScaledBy(MathF.Pow(1.1f, -keyboardAndMouse.MouseScrollWheelDelta)); // (23)!
		cube.SetGeometryShape(lastCubeShape); // (24)!
	}

	cameraController.AdjustAllViaDefaultControls(keyboardAndMouse, deltaTime); // (25)!
	cameraController.Progress(deltaTime); // (26)!

	renderer.Render();
}
```

1.	The factory, display, window, scene, camera, renderer, and application loop are all created in the same way as in [Hello Cube](hello_cube.md#annotated-code).

2.	This *locks* the mouse cursor to the window: The cursor is hidden and can't leave the window, but mouse movements are still reported (via `MouseCursorDelta`). This lets the user look around freely by moving the mouse.

	Because the cursor can't leave the window, it's important to give the user a way to exit (in this example, the Escape key).

3.	This creates a `FreeFlyingCameraController` attached to our `camera`. 

	Camera controllers move and aim a camera for you according to a particular style of movement. The free-flying controller lets the camera fly freely around in 3D space, in any direction. There are several other controller types available (first-person, orbital, follow-cam, etc.); see [Camera Controllers](../reference/camera_controllers.md).
	
	Like most other resources in TinyFFR, camera controllers must be disposed when no longer needed.

4.	This sets the camera controller's starting position: 1m up and 3m back from the world centre.

	Once a controller is in charge of a camera, you set the controller's properties rather than the camera's (the controller overwrites the camera's position and orientation every frame). A new controller doesn't adopt its camera's current position, so we set its starting position here instead of passing one to `CreateCamera()`.
	
5.	This adds a plane primitive as the ground: A `Plane` facing upward (`Direction.Up`) that passes through the world centre (`Location.Origin`). Without something to look at, flying through an empty scene would give no sense of movement.

	Plane primitives are drawn as a large, translucent, chequered surface. `AddPrimitiveShape()` is also used to add the cubes below (see note 17); it accepts several different shape types.

	We never need to change the ground plane, so we don't keep the returned `ScenePrimitive`. Every primitive belongs to the scene it was added to, and will be disposed along with the `scene` at the end of the program.

6.	These variables keep track of the text label the user is currently typing (if any): 

	* `textBeingTyped` is the text primitive being edited, or `null` when the user isn't typing.
	* `textLocation` is where in the world the text label is being placed.
	* `typedText` holds the text typed so far.
	* `PlaceholderText` is shown on the label while nothing has been typed yet.

7.	These variables keep track of the most recently added cube, so that the mouse wheel can resize it:

	* `lastCube` is the cube's primitive, or `null` if there isn't one.
	* `lastCubeShape` is the cube's current shape (its size, position, and rotation). Its initial value here is never used, as it's replaced whenever a cube is added.

8.	`appLoop.Input` is updated every time the loop is iterated, and gives access to the latest state of every input device. Here we store its `KeyboardAndMouse` property for convenience, as we'll be using it a lot below.

	Similarly, `appLoop.Input.GameControllersCombined` provides the same kind of access for gamepads (see [Gamepad Support](#gamepad-support) below).

9.	`KeyWasPressedThisIteration()` returns `true` only on the single loop iteration in which the given key was pressed down. That makes it ideal for "one-off" actions like this one.

	If the user presses Escape, we `break` out of the render loop, which ends the program. (`UserQuitRequested` still works as well, for when the user closes the window or presses e.g. Alt+F4.)

10.	This calculates a location 2m in front of the camera, which is where we'll place any new cube or text label.

	`camera.ViewDirection` is a `Direction`, and multiplying a `Direction` by a distance gives a `Vect` (a movement of that distance in that direction). Adding the `Vect` to the camera's `Position` gives us the new `Location`.

11.	The application has two *modes*: Typing a text label (when `textBeingTyped` isn't `null`), or not.

	The same keys do different things in each mode. For example, while typing, Space should add a space to the text rather than add a cube, and Backspace should delete one character rather than empty the whole scene. See [Input Modes](#input-modes) below.

12.	`TranscribedText` contains the text the user typed this loop iteration (if any). We append it to `typedText`.

	Text transcription takes in to account things like the user's keyboard layout and the Shift key, which is much more reliable than trying to work out which characters were typed from individual key presses. Transcription must be enabled first (see note 21).

13.	Backspace keystrokes don't produce any transcribed text, so we handle them ourselves by removing the last character of `typedText`.

14.	This updates the text primitive to show the latest `typedText`, or `PlaceholderText` if `typedText` is empty (e.g. if the user deletes everything they typed).

	`ScenePrimitiveSize.VeryLarge` makes the label easy to read from a distance. Because `SetGeometryString()` replaces the primitive's geometry entirely, we pass the size again every time (otherwise it would revert to the default size).

15.	When Enter is pressed while typing, we confirm the text label:

	* `SetPaintbrush()` recolours the text green (with a black outline), to show it's been confirmed.
	* Text transcription is switched off again.
	* `textBeingTyped` is set back to `null`, ending typing mode. 
	
	The text primitive itself remains in the scene, but we no longer keep track of it; it will be removed when Backspace is next pressed, or when the scene is disposed.

16.	When Space is pressed (and the user isn't typing), we add a new cube.

	A `PositionedRotatedCuboid` was also used in Hello Cube; here we construct a 0.5m x 0.5m x 0.5m cube at the `spawnLocation` (2m in front of the camera), with no rotation. We store it in `lastCubeShape` so that we can resize it later.

17.	`AddPrimitiveShape()` adds a new primitive drawing the given shape to the scene.

	The second argument is a `PrimitivePaintbrush`, which sets the colours used to draw a primitive. For a solid shape like a cube, only its primary colour is used. `ColorVect.RandomOpaque()` returns a random, fully-opaque colour, so every cube gets a different colour.
	
	We store the returned `ScenePrimitive` in `lastCube`, replacing the previous cube (if any). The previous cube stays in the scene; we just no longer keep track of it.

18.	`scene.RemoveAll()` empties the scene, disposing every primitive in it at once (including every cube, every text label, and the ground plane).

	`RemoveAll()` also removes model instances and lights by default (we don't have any here). It has optional `includeModelInstances`, `includeLights`, and `includePrimitives` parameters that let you choose which of these to remove.

19.	As `RemoveAll()` also removed the ground plane, we immediately add a new one. 

	`RemoveAll()` has also disposed the cube that `lastCube` refers to, so we set `lastCube` back to `null`. Using a primitive after it's been disposed throws an exception, so it's important not to keep using it.

20.	When Enter is pressed (and the user isn't already typing), we start typing a new text label 2m in front of the camera.

	`AddPrimitiveString()` adds a text primitive to the scene. It starts out showing `PlaceholderText` at `VeryLarge` size; its text will be updated as the user types (see note 14). By default, text primitives are drawn in white with a black outline.

21.	This enables text transcription on the loop, so that `TranscribedText` will contain the characters the user types.

	Text transcription should only be enabled while the user is actually typing, as the operating system may consume some keystrokes as text input rather than reporting them as key presses. That's why we disable it again when the text is confirmed (see note 15).

22.	`MouseScrollWheelDelta` returns how many 'stops' the scroll wheel has moved this loop iteration: Positive values for scrolling down, negative for scrolling up, or `0` if the wheel hasn't moved.

	If the wheel has moved and there's a `lastCube` to resize, we resize it.

23.	`ScaledBy()` returns a copy of the shape scaled by the given amount, around its own centre (so the cube stays where it is).

	`MathF.Pow(1.1f, -keyboardAndMouse.MouseScrollWheelDelta)` makes each stop scrolled up grow the cube by 10%, and each stop scrolled down shrink it by the same amount.

24.	`SetGeometryShape()` changes the existing cube primitive to the new, resized shape. The primitive keeps its paintbrush, so the cube keeps its colour.

	Every primitive can have its geometry changed at any time via its `SetGeometry[...]()` methods; e.g. `SetGeometryString()` in note 14.

25.	This adjusts the camera controller's properties according to the controller's default keyboard and mouse controls: Moving the mouse turns the camera, the arrow keys move it, and Right Shift / Right Ctrl move it up and down.

	These controls are applied whether or not the user is typing, as the arrow keys don't produce any typed text.

26.	Finally, `Progress()` actually moves and aims the `camera` according to the controller's (newly adjusted) properties.

	`Progress()` must be called exactly once per frame, after adjusting the controller and before rendering. By default, the controller also applies some smoothing to the camera's movement, to make it feel more physical.


## Additional Notes

### Pressed vs. Held Keys

`KeyWasPressedThisIteration()` is only `true` for the one loop iteration in which a key went down, which suits one-off actions like adding a cube. 

For continuous actions (e.g. moving while a key is held), `KeyIsCurrentlyDown()` returns `true` for every iteration the key is held. That's how the camera controller moves the camera with the arrow keys. 

There's also `KeyWasReleasedThisIteration()`. See [Keyboard / Mouse Input](../reference/keyboard_and_mouse_input.md) for everything else that's available.

### Input Modes

Many applications need the same keys to do different things at different times; for example, the movement keys in a game shouldn't move the player while they're typing in a chat box. 

A simple way to handle this (as in this example) is to keep track of which mode the application is in, and only check the keys that are relevant to the current mode. 

### Gamepad Support

The free-flying camera controller also has default gamepad controls. Adding the following line just before `cameraController.Progress(deltaTime)` lets the user fly the camera with any connected gamepad as well:

```csharp
cameraController.AdjustAllViaDefaultControls(appLoop.Input.GameControllersCombined, deltaTime);
```

See [Gamepad Input](../reference/gamepad_input.md) for more about reading gamepad input.

## Stuff to Tinker With

Here are some things you can experiment with (with links to relevant documentation pages).

* [:material-book-open-page-variant: Keyboard / Mouse Input](../reference/keyboard_and_mouse_input.md): You could try making a mouse click add a cube, or letting the user move the cubes around while a key is held.
* [:material-book-open-page-variant: Free-Flying Camera Controller](../reference/camera_controller_free_flying.md): You could try writing your own control scheme instead of using the default controls, or changing the controller's smoothing.
* [:material-book-open-page-variant: Camera Controllers](../reference/camera_controllers.md): You could try swapping in a different camera controller, such as the first-person or orbital controller.
* [:material-book-open-page-variant: Gamepad Input](../reference/gamepad_input.md): You could try mapping gamepad buttons to adding and removing cubes.
* [:material-book-open-page-variant: Scene Primitives](../reference/scene_primitives.md): You could try adding spheres or arrows instead of cubes, making the mouse wheel resize whichever cube the camera is looking at, or changing the size and colours of the text labels.
* [:material-book-open-page-variant: Colour](../reference/colour.md): You could try picking colours from a fixed palette, or varying only the hue of each cube.

## Where to Get Support

As you work with TinyFFR you'll inevitably need some help; or maybe you'll want to request a feature or report a bug.

The [:simple-discord: Support](../support.md) page has links to the TinyFFR Discord server as well as the GitHub discussions + issues spaces; both are great places to seek help. Finally, every page on this site has a comments section at the bottom that is closely monitored-- check below to see if anyone's had the same issue as you!
