---
title: Camera Controllers
description: An overview of camera controllers, which drive a camera according to a particular style of movement (first-person, orbital, follow-cam, etc.), and a guide to choosing one.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * A camera controller moves and aims a `Camera` for you, according to a particular style of movement. :material-arrow-right: [Camera Controllers](#camera-controllers)
    * You adjust a controller's properties (often via user input), then call `Progress()` once per frame. :material-arrow-right: [The Per-Frame Loop](#the-per-frame-loop), [Reacting to Input](#reacting-to-input)
    * Controllers can smooth the camera's movement to make it feel more physical. :material-arrow-right: [Smoothing](#smoothing)

</div>

## Camera Controllers

```csharp
using var controller = camera.CreateController<InspectorCameraController>(); // (1)!
controller.Target = new Location(0f, 1f, 0f); // (2)!

while (!loop.Input.UserQuitRequested) {
	var deltaTime = loop.IterateOnce().AsDeltaTime();

	controller.AdjustAllViaDefaultControls(loop.Input.KeyboardAndMouse, deltaTime); // (3)!
	controller.Progress(deltaTime); // (4)!

	renderer.Render();
}
```

1.	Creates an inspector ("model viewer") controller attached to `camera`.

2.	Sets the point the camera orbits around and looks at.

3.	Adjusts the controller's properties according to the mouse movement this frame.

4.	Moves and aims the camera according to the controller's (newly adjusted) properties.

Positioning and aiming a camera by hand every frame can be laborious. Instead, it's possible to use a camera controller: Rather than setting the camera's `Position` and `ViewDirection` yourself, you set more abstract target values and constraints on the controller (how far it should be from its target, which way it's turned, how far it's zoomed in, etc.) and the controller does the rest.

Every controller implements a different style of movement, such as a first-person view, a camera orbiting an object, or a chase camera following a vehicle. See [Choosing a Controller](#choosing-a-controller) for the full list.

### Creating & Disposing Controllers

Controllers are created from the camera they control, by specifying the type of controller you want:

```csharp
var controller = camera.CreateController<FirstPersonCameraController>();
// ...
controller.Dispose();
```

The following details apply:

* Camera controllers must be disposed when no longer required. Disposing a controller does not dispose its camera.
* A new controller starts with its default settings. It does not adopt the camera's current position or orientation, so the camera will jump to the controller's arrangement on the first call to `Progress()`. Therefore, the first thing you should do set up the controller (i.e. via `SetContraints()`, see [Constraints & Resetting](#constraints-resetting)).
* It's permitted to have multiple controllers active for the same camera, but you should only call `Progress()` on the one that's actually in charge of controlling the camera at this point in time.

## The Per-Frame Loop

Every controller follows the same pattern each frame:

1. Set or adjust its properties, usually (but not necessarily) according to user input.
2. Call `Progress(deltaTime)`, exactly once per frame.

Nothing adjusts the target camera until `Progress()` is called. Setting properties (or calling any of the `Adjust...()` methods) only updates the controller's target parameters.

???+ tip "Progress Convenience Methods"
	Most controllers also offer an overload of `Progress()` that takes their main per-frame values directly. For example, `orbitalController.Progress(deltaTime, angle, height, distance)` is exactly equivalent to setting the `Angle`, `Height`, and `Distance` properties and then calling `Progress(deltaTime)`.
	
	These are more useful if you are changing the controller's values programmatically (rather than via user input).

## Reacting to Input

Camera controllers are often driven by user input, so every controller (except the [Programmed Camera Controller](camera_controller_programmed.md)) supplies helper methods for adjusting its properties according to the keyboard, mouse, or gamepad.

### Default Controls

The simplest option is `AdjustAllViaDefaultControls()`, which applies the controller's default control scheme for either the keyboard and mouse or a gamepad:

```csharp
controller.AdjustAllViaDefaultControls(input.KeyboardAndMouse, deltaTime); // (1)!
controller.AdjustAllViaDefaultControls(input.GameControllersCombined, deltaTime); // (2)!
```

1.	Applies the default keyboard and mouse controls. See [Keyboard & Mouse Input](keyboard_and_mouse_input.md).

2.	Applies the default gamepad controls, reading every connected gamepad as one. See [Gamepad Input](gamepad_input.md).

Each controller's page describes its default control scheme. Both overloads also take optional parameters for inverting each control and for changing its sensitivity, e.g. `AdjustAllViaDefaultControls(input.KeyboardAndMouse, deltaTime, invertPitchControl: true)`.

The default controls methods are provided as a quick way to get working with any given controller, but generally as your application becomes more complex you'll probably want to use custom control schemes.

### Custom Controls

Every one of a controller's main properties has a family of methods that adjust it according to one specific input, named like `Adjust[Property]Via[Input](...)`:

```csharp
controller.AdjustYawViaMouseCursor(input.KeyboardAndMouse); // (1)!
controller.AdjustPitchViaControllerStick(input.GameControllersCombined, deltaTime, useLeftStick: true); // (2)!
controller.AdjustDistanceViaKeyPress(input.KeyboardAndMouse, deltaTime, KeyboardOrMouseKey.W, reverse: true); // (3)!
controller.AdjustDistanceViaKeyPress(input.KeyboardAndMouse, deltaTime, KeyboardOrMouseKey.S, reverse: false); // (4)!
```

1.	Turns the camera target yaw according to the mouse's horizontal movement this frame.

2.	Tilts the camera target pitch according to the left stick's position.

3.	Moves the camera target distance closer whilst the W key is held...

4.	...and further away whilst the S key is held.

The available inputs are `...ViaMouseCursor`, `...ViaMouseWheel`, `...ViaKeyPress`, `...ViaControllerStick`, `...ViaControllerTriggers`, and `...ViaButtonPress`. Each method takes an optional sensitivity (falling back to a `Default[Property]Sensitivity[Input]` constant on the controller when omitted), and most take a flag for reversing the adjustment.

The `ViaKeyPress` and `ViaButtonPress` methods adjust their property in one direction only, chosen by their required `reverse` parameter. To make a pair of keys that work against one another, call the method twice (once with `reverse: false` and once with `reverse: true`, as shown in the example above).

Finally, each property has a plain `Adjust[Property](deltaTime, adjustmentPerSec)` method that applies a steady rate of change for one frame without reading any input. This is useful when you want to drive a controller from your own input handling (or from anything else).

You can mix and match any of these approaches, or simply set the controller's properties yourself (i.e. just plainly set `controller.Yaw`, etc).

## Smoothing

```csharp
controller.SetGlobalSmoothing(SmoothingStrength.Strong); // (1)!
controller.RotationSmoothingStrength = SmoothingStrength.None; // (2)!
controller.SetCustomDistanceSmoothingStrength(0.3f); // (3)!
```

1.	Sets every one of the controller's smoothing settings to `Strong`.

2.	Disables smoothing for the controller's rotation only.

3.	Sets a custom half-life of 0.3 seconds for the controller's distance smoothing.

By default, controllers don't move the camera straight to the values you give them; instead they "ease" towards them over a number of frames. This makes the camera feel like a physical object rather than something teleporting around, and takes the edge off jerky input.

Each smoothed property (or group of properties) has a `[Property]SmoothingStrength` property taking a `SmoothingStrength`:

| `SmoothingStrength` | Effect |
| :------------------ | :----- |
| `None` | No smoothing; the camera reaches each value on the very next `Progress()`. |
| `VeryMild` (default) | The least smoothing that still takes the edge off sudden movements. |
| `Mild` | A small amount of smoothing. |
| `Moderate` | A middling amount of smoothing. |
| `Strong` | A large amount of smoothing. |
| `VeryStrong` | The most smoothing; very fluid, but with a noticeable lag behind the values you set. |

Stronger smoothing feels more fluid and physical but adds more lag between setting a value and the camera actually getting there. `SetGlobalSmoothing()` sets every smoothing property on a controller at once. Setting a smoothing strength of `None` disables smoothing entirely.

The [Programmed Camera Controller](camera_controller_programmed.md) is the exception. It has no smoothing settings because its keyframes' interpolation algorithms determine how it moves instead.

??? abstract "Custom Smoothing Values"
	The exact amount of smoothing each `SmoothingStrength` applies varies per controller and per property. If none of them suit you, every smoothed property can instead be given a custom *half-life* via a method named like `SetCustom[Property]SmoothingStrength(smoothingHalfLife)`. A half-life of `0f` disables smoothing.

	The half-life is the time (in seconds) the camera takes to cover half the distance to a new target value, starting from rest. For example, if a property's current value is 50 and you set it to 100, a half-life of `1f` brings the camera to 75 one second later.

	Smoothing is implemented as a [critically-damped spring](https://mitchaldichter.com/under_over_critically_damped.html), so the camera carries momentum, meaning after the first half-life it's already moving towards the target, and closes the remaining distance faster than simply halving it again (in the example above, it's at roughly 92 after two seconds rather than 87.5).

	Advanced: The `smoothingHalfLife` parameter is translated to become the Ω of the spring equation via the formula `Ω = 1.6783469f / smoothingHalfLife`.

## Constraints & Resetting

Most controllers have properties that are typically set once rather than every frame, such as the target to orbit around, the limits of movement, or which way is "up". Each controller with such properties has a `SetConstraints(...)` method that sets all of them in one call. These are described on each controller's page.

Calling `ResetParametersToDefault()` returns every property of a controller to its default value (exactly as though the controller had just been created), including setting all smoothing back to `SmoothingStrength.VeryMild`. Any smoothing in progress is also cancelled, so the camera jumps straight to the default arrangement on the next `Progress()`.

## Choosing a Controller

<div class="grid cards" markdown>

-   :material-walk:{ .lg .middle : style="margin-right:0.3em" } __First-Person__

    ---

    The camera is the eyes of a person walking around on the ground. Movement never leaves the ground plane, however far up or down the camera looks.

    [:octicons-arrow-right-24: First-Person Camera Controller](camera_controller_first_person.md)

-   :material-airplane:{ .lg .middle : style="margin-right:0.3em" } __Free-Flying__

    ---

    The camera flies freely through space in any direction, including wherever it's looking. Ideal for spectator, debug, and fly-through cameras.

    [:octicons-arrow-right-24: Free-Flying Camera Controller](camera_controller_free_flying.md)

-   :material-rotate-3d-variant:{ .lg .middle : style="margin-right:0.3em" } __Inspector__

    ---

    The camera orbits a target on the surface of a sphere, always looking at it. The classic "model viewer" camera for inspecting a single object from any angle.

    [:octicons-arrow-right-24: Inspector Camera Controller](camera_controller_inspector.md)

-   :material-orbit:{ .lg .middle : style="margin-right:0.3em" } __Orbital__

    ---

    The camera circles a target at a set height and distance, always looking at it. Suits "satellite", turntable, and "track-cam" shots.

    [:octicons-arrow-right-24: Orbital Camera Controller](camera_controller_orbital.md)

-   :material-cctv:{ .lg .middle : style="margin-right:0.3em" } __Pan-Tilt-Zoom__

    ---

    The camera stays in one place and pans, tilts, and zooms, like a mounted security or television camera.

    [:octicons-arrow-right-24: Pan-Tilt-Zoom Camera Controller](camera_controller_pan_tilt_zoom.md)

-   :material-car:{ .lg .middle : style="margin-right:0.3em" } __Follow__

    ---

    The camera chases a moving target from behind and above. The classic third-person camera for vehicles and characters.

    [:octicons-arrow-right-24: Follow Camera Controller](camera_controller_follow.md)

-   :material-movie-open:{ .lg .middle : style="margin-right:0.3em" } __Programmed__

    ---

    The camera plays back a pre-scripted path of keyframes rather than reacting to input. For cinematics, cut-scenes, fly-throughs, and replays.

    [:octicons-arrow-right-24: Programmed Camera Controller](camera_controller_programmed.md)

</div>

| If you want to... | Use |
| :---------------- | :-- |
| Let the user walk around a level | [First-Person](camera_controller_first_person.md) |
| Let the user fly anywhere (e.g. a debug or editor camera) | [Free-Flying](camera_controller_free_flying.md) |
| Let the user turn an object around to look at it from every side | [Inspector](camera_controller_inspector.md) |
| Circle an object at a steady height, like a turntable | [Orbital](camera_controller_orbital.md) |
| Look around from a fixed spot, with zoom | [Pan-Tilt-Zoom](camera_controller_pan_tilt_zoom.md) |
| Chase a moving vehicle or character | [Follow](camera_controller_follow.md) |
| Play a scripted camera move | [Programmed](camera_controller_programmed.md) |

??? question "Inspector or Orbital?"
	The two are closely related; both keep the camera aimed at a target. The `InspectorCameraController` places the camera by two angles on a sphere around the target (so it can look down from directly above), whereas the `OrbitalCameraController` places it by an angle around a circle plus a separate height above the target (so it moves like a camera on a crane circling the subject).
