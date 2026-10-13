---
title: Pan-Tilt-Zoom Camera Controller
description: Information on the PanTiltZoomCameraController, which holds a camera at a fixed position and aims it by panning, tilting, and zooming, like a mounted security or television camera.
---

## Summary

The `PanTiltZoomCameraController` allows you to place a camera in a specific `Position` and then control where it's looking at via the `Pan`, `Tilt`, and `Zoom` properties. This controller type is well suited for simulating a fixed "security" or "TV set" camera.

* Adjusting `Pan` turns the camera left or right;
* Adjusting `Tilt` tilts the camera up or down;
* Adjusting `Zoom` zooms the camera in or out.

For an overview of how camera controllers work in general, see [Camera Controllers](camera_controllers.md).

### Example Usage

```csharp
// One time setup:
var controller = camera.CreateController<PanTiltZoomCameraController>(); // (1)!
controller.Position = Location.Origin; // (2)!
controller.ZeroPanTiltDirection = Direction.Forward; // (3)!
controller.UpDirection = Direction.Up; // (4)!

// Per-frame:
controller.AdjustAllViaDefaultControls(input.KeyboardAndMouse, deltaTime); // (5)!
controller.AdjustAllViaDefaultControls(input.GameControllersCombined, deltaTime); // (6)!
controller.Progress(deltaTime); // (7)!
```

1.	This creates the controller, attached to the given `camera`.
2.	This sets the camera's position in the world.
3.	This sets the direction the camera should point when `Pan` and `Tilt` are zero (i.e. this is the default look direction of the camera).
4.	This sets which way is "up" for the camera. The camera pans around this axis, and when tilting upwards this is the direction the camera will tilt towards.

5.	This manipulates the camera according to the default keyboard and mouse scheme. The pan/tilt/zoom properties will change according to any registered user inputs for this frame.

	You can replace this with more specific control code if desired (see below); or remove it entirely if you do not wish to allow user keyboard/mouse input control.
	
6.	This manipulates the camera according to the default game controller scheme for all game controllers combined. The pan/tilt/zoom properties will change according to any registered user inputs for this frame.

	You can replace this with more specific control code if desired (see below); or remove it entirely if you do not wish to allow user gamepad input control.
	
7.	Calling `Progress()` once per frame is required on all camera controllers in order for them to actually alter their target `Camera`'s parameters.

## Properties

#### Per-Frame Targets

<span class="def-icon">:material-card-bulleted-outline:</span> `Pan`

:   Sets the left/right turn amount of the camera.

	Increasing this value turns the camera to the left; decreasing to the right.
	
	The leftward direction is derived from the configured `ZeroPanTiltDirection` and `UpDirection` properties. The value is constrained by `PanRange`, and the value read back is always normalized to between `0°` and `360°` (e.g. setting `-30°` reads back as `330°`).

	Defaults to `0°`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `Tilt`

:   Sets the up/down turn amount of the camera.

	Increasing this value tilts the camera upward; decreasing downward.
	
	The upward direction is set via the `UpDirection` property. The value is clamped to between `-MaxTiltDown` and `MaxTiltUp` as it's set, and is read back in the range `-180°` to `180°` (so a downward tilt reads back as a negative angle).

	Defaults to `0°`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `Zoom`

:   A normalized value between `0f` and `1f` (representing 0% to 100% zoom). Sets how zoomed-in the camera is.

	A value of `0f` is 0% zoomed in (e.g. fully zoomed out); a value of `1f` is 100% zoomed in.
	
	Zoom is simulated by altering the camera's field of view; see `MaxZoomInFov`/`MaxZoomOutFov` below. Values outside `0f` to `1f` are clamped.

	Defaults to `0.5f` (i.e. 50%).
	
#### Configuration

<span class="def-icon">:material-card-bulleted-outline:</span> `Position`

:   The location of the camera in the world.

	Defaults to `Location.Origin`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `UpDirection`

:   The local "up" direction of this camera. The camera pans around this axis, and this is the direction the camera will tilt towards when increasing `Tilt`. Invalid values (including `Direction.None`) are ignored.

	Defaults to `Direction.Up`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `ZeroPanTiltDirection`

:   This is the direction the camera should point when `Pan` and `Tilt` are at zero. Invalid values (including `Direction.None`) are ignored.

	Defaults to `Direction.Forward`.
	
----

<span class="def-icon">:material-card-bulleted-outline:</span> `PanRange`

:   Can be set to any value between `0°` and `360°` to set the maximum amount the camera is permitted to pan left/right from its `ZeroPanTiltDirection`.

	This value is applied 50% in either direction equally (i.e. a value of `160°` results in the camera being able to pan 80° left and 80° right).

	Can be set to `null` to remove any limit. Values larger than `360°` are also treated as `null`, negative values are treated as their positive equivalent, and a value of `0°` locks the camera facing its `ZeroPanTiltDirection`.
	
	Defaults to `160°`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `MaxTiltUp`

:   Can be set to any value between `0°` and `180°` to set the maximum amount the camera is permitted to tilt upward from its `ZeroPanTiltDirection`.
	
	Defaults to `35°`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `MaxTiltDown`

:   Can be set to any value between `0°` and `180°` to set the maximum amount the camera is permitted to tilt downward from its `ZeroPanTiltDirection`.
	
	Defaults to `55°`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `MaxZoomInFov`

:   Determines the vertical camera FOV at max zoom in (i.e. when `Zoom` is `1f`). This should be smaller than or equal to `MaxZoomOutFov`; if set larger, `MaxZoomOutFov` is raised to match. Must be positive (invalid values are ignored).

	Defaults to `15°`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `MaxZoomOutFov`

:   Determines the vertical camera FOV at max zoom out (i.e. when `Zoom` is `0f`). This should be larger than or equal to `MaxZoomInFov`; if set smaller, `MaxZoomInFov` is lowered to match. Must be positive (invalid values are ignored).

	Defaults to `90°`.

#### Convenience Methods

<span class="def-icon">:material-code-block-parentheses:</span> `Progress(deltaTime, pan, tilt, zoom)`

:   Sets `Pan`, `Tilt`, and `Zoom`, then calls `Progress(deltaTime)`.

<span class="def-icon">:material-code-block-parentheses:</span> `SetConstraints(position, upDirection, zeroPanTiltDirection, panRange, maxTiltUp, maxTiltDown, maxZoomInFov, maxZoomOutFov)`

:   Sets all of the configuration properties above in one call.

<span class="def-icon">:material-code-block-parentheses:</span> `ResetParametersToDefault()`

:   Resets every property (including smoothing) to its default value.

## Reacting to Input

As camera controllers are often meant to be affected by user input, there are some convenience methods supplied for controlling the primary per-frame target properties:

### Adjusting Pan

#### Keyboard / Mouse

<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPanViaMouseCursor(...)`

:   Adjusts `Pan` according to the captured mouse cursor movement for this frame.

	The `axis` sets which cursor movement direction will be used (defaults to `X`, e.g. left/right).
	
	The `adjustmentPerPixel` value is the angle to add to `Pan` for each pixel moved according to the given `axis`. If null, `DefaultPanSensitivityMouseCursor` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPanViaMouseWheel(...)`

:   Adjusts `Pan` according to the captured mouse wheel movement for this frame.
	
	The `adjustmentPerWheelIncrement` value is the angle to add to `Pan` for each scroll increment on the mouse wheel. If null, `DefaultPanSensitivityMouseWheel` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPanViaKeyPress(...)`

:   Adjusts `Pan` according to whether a certain key is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `keyToTestFor` is the key that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. This parameter is required, and lets you specify two keys in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the angle to add to `Pan` for each second this key is depressed. If null, `DefaultPanSensitivityKeyOrButtonPress` will be used.
	
#### Gamepad
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPanViaControllerStick(...)`

:   Adjusts `Pan` according to the captured controller stick position for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `axis` sets which stick movement direction will be used (defaults to `X`, e.g. left/right).
	
	The `maxAdjustmentPerSec` value is the angle to add to `Pan` when the stick is fully displaced along the given `axis`. If null, `DefaultPanSensitivityControllerStick` will be used.
	
	If `useLeftStick` is true, the left controller stick will be measured; otherwise the right stick will be measured. Defaults to `false`.
	
	If `invertStickControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPanViaControllerTriggers(...)`

:   Adjusts `Pan` according to the captured controller trigger positions for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `maxAdjustmentPerSec` value is the angle to add to `Pan` when the trigger is fully displaced. If null, `DefaultPanSensitivityControllerTrigger` will be used.
	
	If `leftTriggerPansAnticlockwise` is true, the left trigger will pan left and the right trigger pan right; otherwise these directions will be reversed. Defaults to `true`.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPanViaButtonPress(...)`

:   Adjusts `Pan` according to whether a certain button is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `buttonToTestFor` is the button that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. This parameter is required, and lets you specify two buttons in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the angle to add to `Pan` for each second this button is depressed. If null, `DefaultPanSensitivityKeyOrButtonPress` will be used.
	
#### Other
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPan(...)`

:   Adjusts `Pan` according to the given turn rate (`adjustmentPerSec`) and time step (`deltaTime`).

	This method does not inspect any user input data but is provided as a convenience for building custom per-frame control code.
	
### Adjusting Tilt

#### Keyboard / Mouse

<span class="def-icon">:material-code-block-parentheses:</span> `AdjustTiltViaMouseCursor(...)`

:   Adjusts `Tilt` according to the captured mouse cursor movement for this frame.

	The `axis` sets which cursor movement direction will be used (defaults to `Y`, e.g. up/down).
	
	The `adjustmentPerPixel` value is the angle to add to `Tilt` for each pixel moved according to the given `axis`. If null, `DefaultTiltSensitivityMouseCursor` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustTiltViaMouseWheel(...)`

:   Adjusts `Tilt` according to the captured mouse wheel movement for this frame.
	
	The `adjustmentPerWheelIncrement` value is the angle to add to `Tilt` for each scroll increment on the mouse wheel. If null, `DefaultTiltSensitivityMouseWheel` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustTiltViaKeyPress(...)`

:   Adjusts `Tilt` according to whether a certain key is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `keyToTestFor` is the key that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. This parameter is required, and lets you specify two keys in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the angle to add to `Tilt` for each second this key is depressed. If null, `DefaultTiltSensitivityKeyOrButtonPress` will be used.
	
#### Gamepad
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustTiltViaControllerStick(...)`

:   Adjusts `Tilt` according to the captured controller stick position for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `axis` sets which stick movement direction will be used (defaults to `Y`, e.g. up/down).
	
	The `maxAdjustmentPerSec` value is the angle to add to `Tilt` when the stick is fully displaced along the given `axis`. If null, `DefaultTiltSensitivityControllerStick` will be used.
	
	If `useLeftStick` is true, the left controller stick will be measured; otherwise the right stick will be measured. Defaults to `false`.
	
	If `invertStickControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustTiltViaControllerTriggers(...)`

:   Adjusts `Tilt` according to the captured controller trigger positions for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `maxAdjustmentPerSec` value is the angle to add to `Tilt` when the trigger is fully displaced. If null, `DefaultTiltSensitivityControllerTrigger` will be used.
	
	If `leftTriggerTiltsUpward` is true, the left trigger will tilt up and the right trigger tilt down; otherwise these directions will be reversed. Defaults to `true`.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustTiltViaButtonPress(...)`

:   Adjusts `Tilt` according to whether a certain button is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `buttonToTestFor` is the button that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. This parameter is required, and lets you specify two buttons in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the angle to add to `Tilt` for each second this button is depressed. If null, `DefaultTiltSensitivityKeyOrButtonPress` will be used.
	
#### Other
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustTilt(...)`

:   Adjusts `Tilt` according to the given turn rate (`adjustmentPerSec`) and time step (`deltaTime`).

	This method does not inspect any user input data but is provided as a convenience for building custom per-frame control code.
	
### Adjusting Zoom

Note that `Zoom` is a normalized `0f`–`1f` value, so all of the sensitivity parameters below are `float?` rather than `Angle?`.

#### Keyboard / Mouse

<span class="def-icon">:material-code-block-parentheses:</span> `AdjustZoomViaMouseCursor(...)`

:   Adjusts `Zoom` according to the captured mouse cursor movement for this frame.

	The `axis` sets which cursor movement direction will be used (defaults to `Y`, e.g. up/down).
	
	The `adjustmentPerPixel` value is the amount to add to `Zoom` for each pixel moved according to the given `axis`. If null, `DefaultZoomSensitivityMouseCursor` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustZoomViaMouseWheel(...)`

:   Adjusts `Zoom` according to the captured mouse wheel movement for this frame.
	
	The `adjustmentPerWheelIncrement` value is the amount to add to `Zoom` for each scroll increment on the mouse wheel. If null, `DefaultZoomSensitivityMouseWheel` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustZoomViaKeyPress(...)`

:   Adjusts `Zoom` according to whether a certain key is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `keyToTestFor` is the key that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. This parameter is required, and lets you specify two keys in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the amount to add to `Zoom` for each second this key is depressed. If null, `DefaultZoomSensitivityKeyOrButtonPress` will be used.
	
#### Gamepad
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustZoomViaControllerStick(...)`

:   Adjusts `Zoom` according to the captured controller stick position for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `axis` sets which stick movement direction will be used (defaults to `Y`, e.g. up/down).
	
	The `maxAdjustmentPerSec` value is the amount to add to `Zoom` when the stick is fully displaced along the given `axis`. If null, `DefaultZoomSensitivityControllerStick` will be used.
	
	If `useLeftStick` is true, the left controller stick will be measured; otherwise the right stick will be measured. Defaults to `false`.
	
	If `invertStickControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustZoomViaControllerTriggers(...)`

:   Adjusts `Zoom` according to the captured controller trigger positions for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `maxAdjustmentPerSec` value is the amount to add to `Zoom` when the trigger is fully displaced. If null, `DefaultZoomSensitivityControllerTrigger` will be used.
	
	If `rightTriggerZoomsIn` is true, the right trigger will zoom in and the left trigger zoom out; otherwise these directions will be reversed. Defaults to `true`.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustZoomViaButtonPress(...)`

:   Adjusts `Zoom` according to whether a certain button is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `buttonToTestFor` is the button that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. This parameter is required, and lets you specify two buttons in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the amount to add to `Zoom` for each second this button is depressed. If null, `DefaultZoomSensitivityKeyOrButtonPress` will be used.
	
#### Other
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustZoom(...)`

:   Adjusts `Zoom` according to the given rate (`adjustmentPerSec`) and time step (`deltaTime`).

	This method does not inspect any user input data but is provided as a convenience for building custom per-frame control code.
	
### Default Controls

The following snippets show the implementation of `AdjustAllViaDefaultControls(...)` for keyboard/mouse and gamepad respectively. Every parameter after `deltaTime` is optional: `invertPanControl`, `invertTiltControl`, and `invertZoomControl` reverse each control, and the sensitivity parameters (`panAdjustmentPerPixel`, `tiltAdjustmentPerPixel`, and `zoomAdjustmentPerWheelIncrement` for keyboard/mouse; `maxPanAdjustmentPerSec`, `maxTiltAdjustmentPerSec`, and `maxZoomAdjustmentPerSec` for gamepad) fall back to the default sensitivity constants when `null`:

```csharp
// AdjustAllViaDefaultControls(input.KeyboardAndMouse, deltaTime):

AdjustPanViaMouseCursor(input, panAdjustmentPerPixel, invertMouseControl: invertPanControl);
AdjustTiltViaMouseCursor(input, tiltAdjustmentPerPixel, invertMouseControl: invertTiltControl);
AdjustZoomViaMouseWheel(input, zoomAdjustmentPerWheelIncrement, invertMouseControl: invertZoomControl);
```

```csharp
// AdjustAllViaDefaultControls(input.GameControllersCombined, deltaTime):

AdjustPanViaControllerStick(input, deltaTime, maxPanAdjustmentPerSec, invertStickControl: invertPanControl);
AdjustTiltViaControllerStick(input, deltaTime, maxTiltAdjustmentPerSec, invertStickControl: invertTiltControl);
AdjustZoomViaControllerTriggers(input, deltaTime, maxZoomAdjustmentPerSec, rightTriggerZoomsIn: !invertZoomControl);
```

## Smoothing

Smoothing changes how quickly the controller adjusts the camera to match the current target properties.

The `Pan`, `Tilt`, and `Zoom` target properties can have smoothing applied.

```csharp
// Set properties' smoothing individually:
controller.PanSmoothingStrength = SmoothingStrength.VeryMild;
controller.TiltSmoothingStrength = SmoothingStrength.VeryMild;
controller.ZoomSmoothingStrength = SmoothingStrength.VeryMild;

// Set all properties' smoothing simultaneously:
controller.SetGlobalSmoothing(SmoothingStrength.VeryMild);
```

The default smoothing for all properties is `VeryMild`. You can choose from `VeryMild`, `Mild`, `Moderate`, `Strong`, `VeryStrong`, or `None`. 

* Smoothing makes the camera feel more 'real' or physical.
* Higher strengths increase this feeling but also increase the latency between setting a target value and the camera actually meeting that target.
* Setting the smoothing to `None` disables smoothing entirely. This means the camera will always be updated to meet exactly the target value of each property on each frame; reducing latency to 0 but making the camera feel less physical.

??? abstract "Custom Smoothing Values"
	If the enum-based approach is not specific enough for your needs, every property can instead have a custom smoothing strength applied via `SetCustomPanSmoothingStrength()`, `SetCustomTiltSmoothingStrength()`, or `SetCustomZoomSmoothingStrength()`.
	
	Each method takes a single `float` parameter that indicates the *half-life* of decay between the current value of a property and its target value. A half-life of `0f` disables smoothing. (Note that zoom smoothing is applied to the camera's field of view, rather than to the `Zoom` value itself.) 
	
	The half-life is the time (in seconds) the camera takes to cover half the distance to a new target value, starting from rest. For example, if the current value of X is 50 and the target value of X is 100, a half-life of `1f` would move X to 75 after one second. Because the smoothing carries momentum (see below), X would then be at roughly 92 after the next second, rather than 87.5.
	
	Advanced: Smoothing is implemented via a critically-damped spring, which is why it carries momentum. The `smoothingHalfLife` parameter is translated to become the Ω of the spring equation via the formula `Ω = 1.6783469f / smoothingHalfLife`.
