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



## Cursor Capture


## Spawning Model Instances


