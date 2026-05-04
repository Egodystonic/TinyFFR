---
title: Handling Input
description: Examples of how to manage user input.
---

TinyFFR comes with a built-in API for reacting to user input via keyboard, mouse, and gamepad. This page will demonstrate how to use those devices to control a free-flying camera, toggle capturing the cursor, and spawn extra model instances.

!!! example "Continuing "Hello Cube""
	This tutorial will mostly be concerned with showing you how to move a `Camera` according to input captured via keyboard & mouse and/or gamepad.

	If you wish you can integrate these examples directly with the hello cube tutorial and/or the asset tutorial from the previous pages. To do that you can simply manipulate the `camera` resource that's already created + added to the `scene`.
	
## Camera Control

Firstly, we need to create a camera controller for our camera (1). We will pick the `FreeFlyingCameraController` for this tutorial which lets us freely fly the camera around in 3D space. You are welcome to experiment with other camera controllers if you wish (see the reference docs section under **Camera** > **Controllers**).
{ .annotate }

1.	It is not strictly necessary to use a camera controller to map input to camera control, you can use the input API to adjust the camera's position/orientation manually if you wish.

	However, camera controllers make the somewhat complex maths of camera control easier and expose methods specifically for working with user input for convenience.
	
```csharp
using var controller = camera.CreateController<FreeFlyingCameraController>();
```

The line above creates the controller (which must be disposed when no longer in use).

Then, *inside the tick loop*, we can use the captured user input each frame from `loop.Input` to control the camera:

```csharp
while (!loop.Input.UserQuitRequested) { 
	var deltaTime = loop.IterateOnce().AsDeltaTime();
	var input = loop.Input;
	
	// Mouse control
	controller.AdjustYawViaMouseCursor(input.KeyboardAndMouse);
	controller.AdjustPitchViaMouseCursor(input.KeyboardAndMouse);
	
	// Keyboard control
	controller.AdjustPositionViaKeyPress(input.KeyboardAndMouse, deltaTime, KeyboardOrMouseKey.ArrowLeft, Orientation.Left);
	controller.AdjustPositionViaKeyPress(input.KeyboardAndMouse, deltaTime, KeyboardOrMouseKey.ArrowRight, Orientation.Right);
	controller.AdjustPositionViaKeyPress(input.KeyboardAndMouse, deltaTime, KeyboardOrMouseKey.ArrowUp, Orientation.Forward);
	controller.AdjustPositionViaKeyPress(input.KeyboardAndMouse, deltaTime, KeyboardOrMouseKey.ArrowDown, Orientation.Backward);
	controller.AdjustPositionViaKeyPress(input.KeyboardAndMouse, deltaTime, KeyboardOrMouseKey.RightShift, Orientation.Up);
	controller.AdjustPositionViaKeyPress(input.KeyboardAndMouse, deltaTime, KeyboardOrMouseKey.RightControl, Orientation.Down);
	
	// Gamepad control
	controller.AdjustYawViaControllerStick(input.GameControllersCombined, deltaTime);
	controller.AdjustPitchViaControllerStick(input.GameControllersCombined, deltaTime);
	controller.AdjustPositionViaControllerStick(input.GameControllersCombined, deltaTime, Orientation.Forward, axis: Axis2D.Y);
	controller.AdjustPositionViaControllerStick(input.GameControllersCombined, deltaTime, Orientation.Right, axis: Axis2D.X);
	
	// Progress the camera + render the scene
	controller.Progress(deltaTime);
	renderer.Render(); 
}
```

The code snippet shown above is explained as follows:

#### Mouse Control

In the first step we capture the mouse cursor movement to adjust the camera's *yaw* and *pitch*. Yaw/pitch refers to the left/right tilt and up/down tilt of the camera respectively.

If you prefer, you can invert pitch and/or yaw control by setting the optional `invertMouseControl` parameter in either method to `true`.

Also, you can adjust the sensitivity of these controls by specifying the `adjustmentPerPixel` parameter. 

In the following example, we double the sensitivity of mouse control for both axes and also invert the pitch control:

```csharp
controller.AdjustYawViaMouseCursor(
	input.KeyboardAndMouse, 
	adjustmentPerPixel: FreeFlyingCameraController.DefaultYawSensitivityMouseCursor * 2f
);
controller.AdjustPitchViaMouseCursor(
	input.KeyboardAndMouse, 
	adjustmentPerPixel: FreeFlyingCameraController.DefaultPitchSensitivityMouseCursor * 2f, 
	invertMouseControl: true
);
```

#### Keyboard Control

In the next step we also set up using the keyboard arrow keys + ctrl/shift to fly the camera through the world. Each invocation of `AdjustPositionViaKeyPress(...)` sets up the binding from one specific key to one specific *orientation* of movement. The orientation specified will always be interpreted as relative to the camera's facing direction for each frame (so e.g. `Orientation.Forward` will always move the camera in the direction it's facing at that moment).

You can also alter the camera speed by specifying an optional `speed` parameter. In the following example, we double the camera's speed from the default:

```csharp
controller.AdjustPositionViaKeyPress(
	input.KeyboardAndMouse, 
	deltaTime, 
	KeyboardOrMouseKey.ArrowLeft, 
	Orientation.Left, 
	speed: FreeFlyingCameraController.DefaultPositionSensitivityKeyOrButtonPress * 2f
);
controller.AdjustPositionViaKeyPress(
	input.KeyboardAndMouse, 
	deltaTime, 
	KeyboardOrMouseKey.ArrowRight, 
	Orientation.Right, 
	speed: FreeFlyingCameraController.DefaultPositionSensitivityKeyOrButtonPress * 2f
);
controller.AdjustPositionViaKeyPress(
	input.KeyboardAndMouse, 
	deltaTime, 
	KeyboardOrMouseKey.ArrowUp, 
	Orientation.Forward, 
	speed: FreeFlyingCameraController.DefaultPositionSensitivityKeyOrButtonPress * 2f
);
controller.AdjustPositionViaKeyPress(
	input.KeyboardAndMouse, 
	deltaTime, 
	KeyboardOrMouseKey.ArrowDown, 
	Orientation.Backward,
	speed: FreeFlyingCameraController.DefaultPositionSensitivityKeyOrButtonPress * 2f
);
controller.AdjustPositionViaKeyPress(
	input.KeyboardAndMouse, 
	deltaTime, 
	KeyboardOrMouseKey.RightShift, 
	Orientation.Up,
	speed: FreeFlyingCameraController.DefaultPositionSensitivityKeyOrButtonPress * 2f
);
controller.AdjustPositionViaKeyPress(
	input.KeyboardAndMouse, 
	deltaTime, 
	KeyboardOrMouseKey.RightControl, 
	Orientation.Down,
	speed: FreeFlyingCameraController.DefaultPositionSensitivityKeyOrButtonPress * 2f
);
```

#### Gamepad

Next, we set up gamepad control. By default, `AdjustYawViaControllerStick` and `AdjustPitchViaControllerStick` read the *right* stick (yaw on its X axis, pitch on its Y axis), and `AdjustPositionViaControllerStick` reads the *left* stick, wiring its Y axis to forward/backward and its X axis to left/right. As with keyboard movement, the orientations are interpreted relative to the camera's facing direction at each frame.

??? success "GameControllersCombined"
	`input.GameControllersCombined` is a virtual aggregate that merges the input state of every connected gamepad into a single source. This is provided as an easy way to simply accept input from any connected game controller. Most of the time this is fine- you'll only need to be more specific if supporting multiple users per device (e.g. local multiplayer games).
	
	If you *do* need to react to a specific physical controller, iterate `input.GameControllers` and use the per-controller retriever instead.

You can swap the sticks by specifying `true` or `false` for the `useLeftStick` parameter supplied to each method as desired.

Each method also accepts an `invertStickControl` flag and a `maxAdjustmentPerSec` / `maxSpeed` parameter. Below we double the camera's movement speed and invert the pitch axis:

```csharp
controller.AdjustPitchViaControllerStick(
	input.GameControllersCombined,
	deltaTime,
	maxAdjustmentPerSec: FreeFlyingCameraController.DefaultPitchSensitivityControllerStick,
	invertStickControl: true
);
controller.AdjustPositionViaControllerStick(
	input.GameControllersCombined,
	deltaTime,
	Orientation.Forward,
	maxSpeed: FreeFlyingCameraController.DefaultPositionSensitivityControllerStick * 2f,
	axis: Axis2D.Y
);
controller.AdjustPositionViaControllerStick(
	input.GameControllersCombined,
	deltaTime,
	Orientation.Right,
	maxSpeed: FreeFlyingCameraController.DefaultPositionSensitivityControllerStick * 2f,
	axis: Axis2D.X
);
```

If you'd rather drive an axis from the *triggers* or from individual *buttons*, the controller exposes parallel methods: `AdjustPitchViaControllerTriggers`, `AdjustYawViaControllerTriggers`, `AdjustPositionViaControllerTriggers`, and a `…ViaButtonPress` family that takes a `GameControllerButton` value. These follow the same shape as the stick methods — see the [camera-controller reference docs](/reference/camera_controller_free_flying.md) for full parameter details.

#### Progression

Finally, we *progress* the camera controller-- this is what actually moves/mutates the attached `camera` for this frame. If you don't call `Progress()` on the camera controller nothing will actually *happen* to the camera!

??? tip "Default Controls Shortcut"
	If you're happy with the default control scheme for a camera controller you can invoke everything described above with two lines:
	
	```csharp
	controller.AdjustAllViaDefaultControls(input.KeyboardAndMouse, deltaTime);
	controller.AdjustAllViaDefaultControls(input.GameControllersCombined, deltaTime);
	```
	
	Note that every camera controller exposes `AdjustAllViaDefaultControls()` (in fact, it's part of the camera controller interface, `ICameraController`)- this means you can offer a default control rubric for any camera controller without even knowing what controller type it is.

## Cursor Capture

When using mouse-cursor controls in practice the cursor will quickly drift outside the window or get pinned against a screen edge-- at which point the operating system stops generating cursor-movement events and your camera stops turning. The fix is to *capture* the cursor to the application window, which hides it and pins it "inside" the window frame.

Cursor capture is controlled via a single boolean property on the `Window` resource:

```csharp
window.LockCursor = true;
```

Setting `LockCursor` to `true` both pins the cursor in the centre of the window and hides it. The `MouseCursorDelta` values that drive `AdjustYawViaMouseCursor` and `AdjustPitchViaMouseCursor` continue to be reported normally while the cursor is locked, so your existing camera control code keeps working without any changes.

For our example we'll use the left mouse button as a way to toggle cursor capture on and off. **Inside your tick loop** add the following lines:

```csharp
if (input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.MouseLeft)) {
	window.LockCursor = !window.LockCursor;
}
```

Now, test what happens when you press the left mouse button while inside the application window.

## Spawning Model Instances

For our final example, we'll dynamically spawn new model instances into the scene each time the user presses a key.

First, before the tick loop, we'll create a *sphere* mesh & material that every spawned instance will reference. We'll also need a list to track our spawned spheres so we can dispose them later:

```csharp
using var sphereMesh = factory.MeshBuilder.CreateMesh(new Sphere(radius: 0.25f));
using var sphereMaterial = factory.MaterialBuilder.CreateTestMaterial();
var spawnedSpheres = new List<ModelInstance>();
```

Now, *inside the tick loop*, we react to the `Return` (i.e. 'Enter') key being pressed by spawning a new sphere two units in front of the camera:

```csharp
if (input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Return)) {
	var newSphere = factory.ObjectBuilder.CreateModelInstance(
		sphereMesh,
		sphereMaterial,
		initialPosition: camera.Position + camera.ViewDirection * 2f // (1)!
	);
	scene.Add(newSphere);
	spawnedSpheres.Add(newSphere);
}
```

1.	This sets the initial position of the sphere to be at the camera's current `Position` *plus* two meters in front of it in the direction of its current `ViewDirection` (e.g. camera-forward).

Finally, *after the tick loop has exited* (so below the `while` loop) we remove each sphere from the scene and dispose it:

```csharp
foreach (var sphere in spawnedSpheres) {
	scene.Remove(sphere);
	sphere.Dispose();
}
```
