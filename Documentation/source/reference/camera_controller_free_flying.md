---
title: Free-Flying Camera Controller
---

## Summary

The `FreeFlyingCameraController` controls a camera that can fly freely around in 3D space without being constrained to a ground plane (in contrast to the [first-person controller](camera_controller_first_person.md)). The camera is moved via `Position` and oriented via `Yaw` and `Pitch`.

* Adjusting `Position` moves the camera through space (in any direction).
* Adjusting `Yaw` rotates the camera left or right around the configured `WorldUp` axis.
* Adjusting `Pitch` tilts the camera up or down.

By default, `Pitch` is automatically clamped to ±90° each frame so the camera never flips upside-down. This can be disabled via `AllowUpsideDownFlip`.

### Example Usage

```csharp
// One time setup:
var controller = camera.CreateController<FreeFlyingCameraController>(); // (1)!
controller.WorldForward = Direction.Forward; // (2)!
controller.WorldUp = Direction.Up; // (3)!
controller.AllowUpsideDownFlip = false; // (4)!

// Per-frame:
controller.AdjustAllViaDefaultControls(input.KeyboardAndMouse, deltaTime); // (5)!
controller.AdjustAllViaDefaultControls(input.GameControllersCombined, deltaTime); // (6)!
controller.Progress(deltaTime); // (7)!
```

1.	This creates the controller, attached to the given `camera`.
2.	This sets the "forward" direction of the world. Any direction orthogonal to `WorldUp` is permitted.

3.	This sets the "up" direction of the world. Any direction orthogonal to `WorldForward` is permitted.

	When `AllowUpsideDownFlip` is `false` this value sets the maximum upward-pitch direction (and its reverse sets the maximum downward-pitch direction).
	
4.	This controls whether or not you're allowed to flip upside down while "flying around" with this camera controller.

5.	This manipulates the camera according to the default keyboard and mouse scheme. The position/yaw/pitch properties will change according to any registered user inputs for this frame.

	You can replace this with more specific control code if desired (see below); or remove it entirely if you do not wish to allow user keyboard/mouse input control.
	
6.	This manipulates the camera according to the default game controller scheme for all game controllers combined. The position/yaw/pitch properties will change according to any registered user inputs for this frame.

	You can replace this with more specific control code if desired (see below); or remove it entirely if you do not wish to allow user gamepad input control.
	
7.	Calling `Progress()` once per frame is required on all camera controllers in order for them to actually alter their target `Camera`'s parameters.

## Properties

#### Per-Frame Targets

<span class="def-icon">:material-card-bulleted-outline:</span> `Position`

:   Sets the location of the camera in the world.

	Defaults to `Location.Origin`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `Yaw`

:   Sets the left/right turn amount of the camera.

	Increasing this value turns the camera to the left; decreasing to the right.
	
	Defaults to `0°`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `Pitch`

:	Sets the up/down turn amount of the camera.
	
	A value of `0°` means the camera is looking horizontally; positive values tilt upward, negative values tilt downward.
	
	Unless `AllowUpsideDownFlip` is `true`, `Pitch` is automatically clamped to ±90° during `Progress()` so that the camera never flips upside-down.
	
	Defaults to `0°`.

#### Configuration

<span class="def-icon">:material-card-bulleted-outline:</span> `WorldForward`

:   The "forward" direction in the world that corresponds to the camera's view direction when both `Yaw` and `Pitch` are `0°`.
	
	Defaults to `Direction.Forward`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `WorldUp`

:   The "up" axis in the world. `Yaw` rotates the camera around this axis.
	
	When set, `WorldUp` is automatically orthogonalized against `WorldForward` — if you supply a value that is not perpendicular to `WorldForward`, the component parallel to `WorldForward` will be removed. Setting `WorldForward` after `WorldUp` will also re-orthogonalize `WorldUp`.
	
	Defaults to `Direction.Up`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `AllowUpsideDownFlip`

:   When `false` (the default), the controller automatically clamps `Pitch` to the range ±90° during `Progress()` so that the camera will never appear upside-down.
	
	When `true`, this clamp is removed and `Pitch` may take any angle, allowing the camera to flip upside-down (useful for flight-sim style controllers).
	
	Defaults to `false`.

## Reacting to Input

As camera controllers are often meant to be affected by user input, there are some convenience methods supplied for controlling the primary per-frame target properties:

### Adjusting Pitch

#### Keyboard / Mouse

<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPitchViaMouseCursor(...)`

:   Adjusts `Pitch` according to the captured mouse cursor movement for this frame.

	The `axis` sets which cursor movement direction will be used (defaults to `Y`, e.g. up/down).
	
	The `adjustmentPerPixel` value is the angle to add to `Pitch` for each pixel moved according to the given `axis`. If null, `DefaultPitchSensitivityMouseCursor` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPitchViaMouseWheel(...)`

:   Adjusts `Pitch` according to the captured mouse wheel movement for this frame.
	
	The `adjustmentPerWheelIncrement` value is the angle to add to `Pitch` for each scroll increment on the mouse wheel. If null, `DefaultPitchSensitivityMouseWheel` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPitchViaKeyPress(...)`

:   Adjusts `Pitch` according to whether a certain key is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `keyToTestFor` is the key that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. Defaults to `false`. This parameter lets you specify two keys in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the angle to add to `Pitch` for each second this key is depressed. If null, `DefaultPitchSensitivityKeyOrButtonPress` will be used.
	
#### Gamepad
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPitchViaControllerStick(...)`

:   Adjusts `Pitch` according to the captured controller stick position for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `axis` sets which stick movement direction will be used (defaults to `Y`, e.g. up/down).
	
	The `maxAdjustmentPerSec` value is the angle to add to `Pitch` when the stick is fully displaced along the given `axis`. If null, `DefaultPitchSensitivityControllerStick` will be used.
	
	If `useLeftStick` is true, the left controller stick will be measured; otherwise the right stick will be measured. Defaults to `false`.
	
	If `invertStickControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPitchViaControllerTriggers(...)`

:   Adjusts `Pitch` according to the captured controller trigger positions for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `maxAdjustmentPerSec` value is the angle to add to `Pitch` when the trigger is fully displaced. If null, `DefaultPitchSensitivityControllerTrigger` will be used.
	
	If `rightTriggerPitchesUp` is true, the right trigger will pitch up and the left trigger pitch down; otherwise these directions will be reversed. Defaults to `true`.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPitchViaButtonPress(...)`

:   Adjusts `Pitch` according to whether a certain button is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `buttonToTestFor` is the button that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. Defaults to `false`. This parameter lets you specify two buttons in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the angle to add to `Pitch` for each second this button is depressed. If null, `DefaultPitchSensitivityKeyOrButtonPress` will be used.
	
#### Other
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPitch(...)`

:   Adjusts `Pitch` according to the given turn rate (`adjustmentPerSec`) and time step (`deltaTime`).

	This method does not inspect any user input data but is provided as a convenience for building custom per-frame control code.
	
### Adjusting Yaw

#### Keyboard / Mouse

<span class="def-icon">:material-code-block-parentheses:</span> `AdjustYawViaMouseCursor(...)`

:   Adjusts `Yaw` according to the captured mouse cursor movement for this frame.

	The `axis` sets which cursor movement direction will be used (defaults to `X`, e.g. left/right).
	
	The `adjustmentPerPixel` value is the angle to add to `Yaw` for each pixel moved according to the given `axis`. If null, `DefaultYawSensitivityMouseCursor` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustYawViaMouseWheel(...)`

:   Adjusts `Yaw` according to the captured mouse wheel movement for this frame.
	
	The `adjustmentPerWheelIncrement` value is the angle to add to `Yaw` for each scroll increment on the mouse wheel. If null, `DefaultYawSensitivityMouseWheel` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustYawViaKeyPress(...)`

:   Adjusts `Yaw` according to whether a certain key is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `keyToTestFor` is the key that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. Defaults to `false`. This parameter lets you specify two keys in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the angle to add to `Yaw` for each second this key is depressed. If null, `DefaultYawSensitivityKeyOrButtonPress` will be used.
	
#### Gamepad
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustYawViaControllerStick(...)`

:   Adjusts `Yaw` according to the captured controller stick position for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `axis` sets which stick movement direction will be used (defaults to `X`, e.g. left/right).
	
	The `maxAdjustmentPerSec` value is the angle to add to `Yaw` when the stick is fully displaced along the given `axis`. If null, `DefaultYawSensitivityControllerStick` will be used.
	
	If `useLeftStick` is true, the left controller stick will be measured; otherwise the right stick will be measured. Defaults to `false`.
	
	If `invertStickControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustYawViaControllerTriggers(...)`

:   Adjusts `Yaw` according to the captured controller trigger positions for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `maxAdjustmentPerSec` value is the angle to add to `Yaw` when the trigger is fully displaced. If null, `DefaultYawSensitivityControllerTrigger` will be used.
	
	If `leftTriggerYawsLeft` is true, the left trigger will yaw left and the right trigger yaw right; otherwise these directions will be reversed. Defaults to `true`.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustYawViaButtonPress(...)`

:   Adjusts `Yaw` according to whether a certain button is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `buttonToTestFor` is the button that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. Defaults to `false`. This parameter lets you specify two buttons in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the angle to add to `Yaw` for each second this button is depressed. If null, `DefaultYawSensitivityKeyOrButtonPress` will be used.
	
#### Other
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustYaw(...)`

:   Adjusts `Yaw` according to the given turn rate (`adjustmentPerSec`) and time step (`deltaTime`).

	This method does not inspect any user input data but is provided as a convenience for building custom per-frame control code.
	
### Adjusting Position

Position adjustments can be specified in one of two equivalent ways:

* Using a camera-relative `Orientation` (e.g. `Orientation.Forward`, `Orientation.Right`, `Orientation.Up`, etc.) and a scalar `speed`/`distance`. The `Orientation` is interpreted relative to the camera's current view direction.
* Using a `Vect` (a world-space vector) directly. This bypasses camera-relative interpretation entirely.

Each `AdjustPositionVia*` method below provides both overloads. The `Orientation`-based form is generally more convenient; the `Vect`-based form is provided for cases where you wish to specify a fixed world-space movement vector.

#### Keyboard / Mouse

<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPositionViaMouseCursor(...)`

:   Adjusts `Position` according to the captured mouse cursor movement for this frame.

	The `axis` sets which cursor movement direction will be used (defaults to `X`, e.g. left/right).
	
	*`Orientation`-form:* The `cameraRelativeOrientation` parameter sets the camera-relative direction the camera moves in when the cursor is moved positively along `axis`. The `speed` value is the distance (in world units) to move per pixel of cursor movement. If null, `DefaultPositionSensitivityMouseCursor` will be used.
	
	*`Vect`-form:* The `adjustmentPerPixel` parameter is a world-space `Vect` representing the movement applied per pixel of cursor movement.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPositionViaMouseWheel(...)`

:   Adjusts `Position` according to the captured mouse wheel movement for this frame.
	
	*`Orientation`-form:* The `cameraRelativeOrientation` parameter sets the camera-relative direction the camera moves in when the wheel is scrolled forward. The `speed` value is the distance (in world units) to move per scroll increment. If null, `DefaultPositionSensitivityMouseWheel` will be used.
	
	*`Vect`-form:* The `adjustmentPerWheelIncrement` parameter is a world-space `Vect` representing the movement applied per scroll increment.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPositionViaKeyPress(...)`

:   Adjusts `Position` according to whether a certain key is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `keyToTestFor` is the key that, when pressed, will move the camera.
	
	*`Orientation`-form:* The `cameraRelativeOrientation` parameter is the camera-relative direction the camera moves in while the key is depressed. The `speed` value is the distance per second to move while the key is depressed. If null, `DefaultPositionSensitivityKeyOrButtonPress` will be used.
	
	*`Vect`-form:* The `adjustmentPerSec` parameter is a world-space `Vect` representing the movement applied per second while the key is depressed.
	
#### Gamepad
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPositionViaControllerStick(...)`

:   Adjusts `Position` according to the captured controller stick position for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `axis` sets which stick movement direction will be used (defaults to `X`, e.g. left/right).
	
	*`Orientation`-form:* The `cameraRelativeOrientation` parameter sets the camera-relative direction the camera moves in when the stick is fully displaced along `axis`. The `maxSpeed` value is the maximum distance per second when the stick is fully displaced. If null, `DefaultPositionSensitivityControllerStick` will be used.
	
	*`Vect`-form:* The `maxAdjustmentPerSec` parameter is a world-space `Vect` representing the maximum movement per second.
	
	If `useLeftStick` is true, the left controller stick will be measured; otherwise the right stick will be measured. Defaults to `true`.
	
	If `invertStickControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPositionViaControllerTriggers(...)`

:   Adjusts `Position` according to the captured controller trigger positions for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	*`Orientation`-form:* The `cameraRelativeOrientation` parameter sets the camera-relative direction the camera moves in when the "positive" trigger is held. The `maxSpeed` value is the maximum distance per second when the trigger is fully displaced. If null, `DefaultPositionSensitivityControllerTrigger` will be used.
	
	*`Vect`-form:* The `maxAdjustmentPerSec` parameter is a world-space `Vect` representing the maximum movement per second when the "positive" trigger is fully displaced.
	
	If `leftTriggerMovesPositive` is true, the left trigger moves in the configured direction and the right trigger in the opposite direction; otherwise these directions will be reversed. Defaults to `true`.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPositionViaButtonPress(...)`

:   Adjusts `Position` according to whether a certain button is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `buttonToTestFor` is the button that, when pressed, will move the camera.
	
	*`Orientation`-form:* The `cameraRelativeOrientation` parameter is the camera-relative direction the camera moves in while the button is depressed. The `speed` value is the distance per second to move while the button is depressed. If null, `DefaultPositionSensitivityKeyOrButtonPress` will be used.
	
	*`Vect`-form:* The `adjustmentPerSec` parameter is a world-space `Vect` representing the movement applied per second while the button is depressed.
	
#### Other
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPosition(...)`

:   Several overloads are provided to directly adjust `Position` without inspecting any user input data, for building custom per-frame control code:
	
	* `AdjustPosition(Orientation cameraRelativeOrientation, float distance)` — moves a fixed distance in the given camera-relative orientation.
	* `AdjustPosition(float deltaTime, Orientation cameraRelativeOrientation, float speed)` — speed/time-based version of the above.
	* `AdjustPosition(float deltaTime, Vect adjustmentPerSec)` — moves at the given world-space rate scaled by `deltaTime`.
	
### Default Controls

The following snippets show the implementation of `AdjustAllViaDefaultControls(...)` for keyboard/mouse and gamepad respectively:

```csharp
// AdjustAllViaDefaultControls(input.KeyboardAndMouse, deltaTime):

AdjustPitchViaMouseCursor(input, pitchAdjustmentPerPixel, invertMouseControl: invertPitchControl);
AdjustYawViaMouseCursor(input, yawAdjustmentPerPixel, invertMouseControl: invertYawControl);

AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowLeft, Orientation.Left, positionAdjustmentSpeed);
AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowRight, Orientation.Right, positionAdjustmentSpeed);
AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowUp, Orientation.Forward, positionAdjustmentSpeed);
AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowDown, Orientation.Backward, positionAdjustmentSpeed);
AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.RightShift, invertUpDownPositionalControl ? Orientation.Down : Orientation.Up, positionAdjustmentSpeed);
AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.RightControl, invertUpDownPositionalControl ? Orientation.Up : Orientation.Down, positionAdjustmentSpeed);
```

```csharp
// AdjustAllViaDefaultControls(input.GameControllersCombined, deltaTime):

AdjustPitchViaControllerStick(input, deltaTime, maxPitchAdjustmentPerSec, invertStickControl: invertPitchControl);
AdjustYawViaControllerStick(input, deltaTime, maxYawAdjustmentPerSec, invertStickControl: invertYawControl);

AdjustPositionViaControllerStick(input, deltaTime, Orientation.Forward, maxPositionAdjustmentSpeed, axis: Axis2D.Y);
AdjustPositionViaControllerStick(input, deltaTime, Orientation.Right, maxPositionAdjustmentSpeed, axis: Axis2D.X);
AdjustPositionViaControllerTriggers(input, deltaTime, invertUpDownPositionalControl ? Orientation.Up : Orientation.Down, maxPositionAdjustmentSpeed);
```

## Smoothing

Smoothing changes how quickly the controller adjusts the camera to match the current target properties.

The `Position`, `Yaw`, and `Pitch` target properties can have smoothing applied. Note that `Yaw` and `Pitch` share a single `RotationSmoothingStrength` setting — they cannot be smoothed independently of one another.

```csharp
// Set properties' smoothing individually:
controller.PositionSmoothingStrength = Strength.VeryMild;
controller.RotationSmoothingStrength = Strength.VeryMild; // Applies to both Yaw and Pitch

// Set all properties' smoothing simutaneously:
controller.SetGlobalSmoothing(Strength.VeryMild);
```

The default smoothing for all properties is `VeryMild`. You can choose from `VeryMild`, `Mild`, `Moderate`, `Strong`, `VeryStrong`, or `None`. 

* Smoothing makes the camera feel more 'real' or physical.
* Higher strengths increase this feeling but also increase the latency between setting a target value and the camera actually meeting that target.
* Setting the smoothing to `None` disables smoothing entirely. This means the camera will always be updated to meet exactly the target value of each property on each frame; reducing latency to 0 but making the camera feel less physical.

??? abstract "Custom Smoothing Values"
	If the enum-based approach is not specific enough for your needs, every property can instead have a custom smoothing strength applied via a method named like "`SetCustom[...]SmoothingStrength`".
	
	This method takes a single `float` parameter that indicates the *half-life* of decay between the current value of a property and its target value. 
	
	For example, if the current value of X is 50 and the target value of X is 100, a half-life of `1f` would move X to 75 after one second, and then 87.5 after the next second, and so-on.
	
	Advanced: Smoothing is implemented via critically-damped spring. The `smoothingHalfLife` parameter is translated to become the Ω of the spring equation via the formula `Ω = 1.6783469f / smoothingHalfLife`.
