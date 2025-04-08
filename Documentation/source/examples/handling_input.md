---
title: Handling Input
description: Examples of how to manage user input.
---

TinyFFR comes with a built-in API for reacting to user input via keyboard, mouse, and gamepad. This page will demonstrate how to use those devices to control a free-flying camera.

??? example "Continuing "Hello Cube""
	This tutorial will mostly be concerned with showing you how to move the `camera` according to input captured via keyboard & mouse and/or gamepad.

	If you wish you can integrate these camera controls directly with the hello cube example and/or the treasure chest example from the previous page; just replace any pre-existing camera manipulation code.

## Preamble / Plumbing

For all of the code below we will handle our input in a dedicated method named `HandleInputForCamera()`. `HandleInputForCamera()` will be invoked once per frame, and we'll pass it three arguments: Our `camera`, the loop's `ILatestInputRetriever`, and the time elapsed this frame (in seconds):

```csharp
while (!loop.Input.UserQuitRequested) {
	var deltaTime = (float) loop.IterateOnce().TotalSeconds;

	HandleInputForCamera(loop.Input, camera, deltaTime);

	renderer.Render();
}

// We'll be putting all our input handling code on this page inside this method
static void HandleInputForCamera(ILatestInputRetriever input, Camera camera, float deltaTime) {

}
```

??? question "What is the `ILatestInputRetriever`?"
	The `ILatestInputRetriever` interface provides an API for capturing the __latest__ user input events & state.

	You might be wondering what exactly "latest" means: The answer is that the state and events retrieved by this interface are updated every time `loop.IterateOnce()` is invoked (hence why it's a property of the `ApplicationLoop`).

	This means you can hold on to the same `ILatestInputRetriever` reference indefinitely and as long as the `ApplicationLoop` is not disposed, the instance will remain valid and can be used to always access input data for the current frame.

	The same lifetime and usage pattern applies to all members of the `ILatestInputRetriever`, including the `ILatestKeyboardAndMouseInputRetriever` returned via the `KeyboardAndMouse` property and the `ILatestGameControllerInputStateRetriever` returned by the `GameControllers`/`GameControllersCombined` properties.

## Keyboard: Camera Flight

In this section we'll use the keyboard's arrow keys to make the camera fly around:

```csharp
static void HandleInputForCamera(ILatestInputRetriever input, Camera camera, float deltaTime) {
	const float CameraMovementSpeed = 1f;
	var kbm = input.KeyboardAndMouse;

	// === Adjust camera position ===
	var cameraMovementModifiers = XYPair<float>.Zero;
	foreach (var currentKey in kbm.CurrentlyPressedKeys) {
		switch (currentKey) {
			case KeyboardOrMouseKey.ArrowLeft:
				cameraMovementModifiers += (1f, 0f);
				break;
			case KeyboardOrMouseKey.ArrowRight:
				cameraMovementModifiers += (-1f, 0f);
				break;
			case KeyboardOrMouseKey.ArrowUp:
				cameraMovementModifiers += (0f, 1f);
				break;
			case KeyboardOrMouseKey.ArrowDown:
				cameraMovementModifiers += (0f, -1f);
				break;
		}
	}

	var positiveYDir = camera.ViewDirection;
	var positiveXDir = Direction.FromDualOrthogonalization(camera.UpDirection, positiveYDir);

	camera.Position += ((positiveXDir * cameraMovementModifiers.X) + (positiveYDir * cameraMovementModifiers.Y)).WithLength(CameraMovementSpeed * deltaTime);
}
```

## Mouse: Camera Look

TODO mention Window.LockCursor