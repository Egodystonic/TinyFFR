---
title: First-Person Camera Controller
---

## Summary

The `FirstPersonCameraController` controls a camera as if from a person walking around on a ground plane. The camera is moved via `Position` and oriented via `Yaw` and `Pitch`. Movement is parallel to the plane orthogonal to the given `WorldUp` direction.

* Adjusting `Position` moves the camera through space.
* Adjusting `Yaw` rotates the camera left or right around the `WorldUp` axis;
* Adjusting `Pitch` tilts the camera up or down.

`Pitch` is automatically clamped to ±90° each frame so the camera never flips upside-down.

### Example Usage

```csharp
// One time setup:
var controller = camera.CreateController<FirstPersonCameraController>(); // (1)!
controller.WorldUp = Direction.Up; // (2)!

// Per-frame:
controller.AdjustAllViaDefaultControls(input.KeyboardAndMouse, deltaTime); // (3)!
controller.AdjustAllViaDefaultControls(input.GameControllersCombined, deltaTime); // (4)!
controller.Progress(deltaTime); // (5)!
```

1.	This creates the controller, attached to the given `camera`.
2.	This sets the up direction of the camera. When `Pitch` is increased, the camera will tilt towards this direction. Movement will be constrained to be parallel to the plane that is orthogonal to this direction.

3.	This manipulates the camera according to the default keyboard and mouse scheme. The position/yaw/pitch properties will change according to any registered user inputs for this frame.

	You can replace this with more specific control code if desired (see below); or remove it entirely if you do not wish to allow user keyboard/mouse input control.
	
4.	This manipulates the camera according to the default game controller scheme for all game controllers combined. The position/yaw/pitch properties will change according to any registered user inputs for this frame.

	You can replace this with more specific control code if desired (see below); or remove it entirely if you do not wish to allow user gamepad input control.
	
5.	Calling `Progress()` once per frame is required on all camera controllers in order for them to actually alter their target `Camera`'s parameters.

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

:   Sets the up/down turn amount of the camera.
	
	A value of `0°` means the camera is looking horizontally; positive values tilt upward, negative values tilt downward.
	
	Pitch is automatically clamped to ±90° during `Progress()` so that the camera never flips upside-down.
	
	Defaults to `0°`.

#### Configuration

<span class="def-icon">:material-card-bulleted-outline:</span> `WorldUp`

:   The "up" direction for this camera. 

	Changing `Yaw` rotates the camera around this direction.

	Increasing `Pitch` tilts the camera towards this direction, decreasing it tilts the camera in the opposite direction.

	Movement adjustments are projected onto a plane with this direction as its normal.

	Defaults to `Direction.Up`.

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
	
	If `leftTriggerPitchesUp` is true, the left trigger will pitch up and the right trigger pitch down; otherwise these directions will be reversed. Defaults to `true`.
	
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

Position adjustments are constrained to be orthogonal to `WorldUp` and are specified using either an `Orientation2D` (e.g. `Up`/`Down`/`Left`/`Right` etc.) or a polar `Angle`.

If specifying an orientation, it is interpreted relative to the camera's current view direction (e.g. `Up` always points camera-forward (orthogonalized against `WorldUp`), `Left` always points camera-left (orthogonalized against `WorldUp`), etc).

If specifying an angle, it is interpreted as a [polar angle](/tutorials/conventions.md#2d-handedness-orientation) (e.g. `0°` points camera-right (orthogonalized against `WorldUp`), `90°` points camera-forward (orthogonalized against `WorldUp`), etc).

#### Keyboard / Mouse

<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPositionViaMouseCursor(...)`

:   Adjusts `Position` according to the captured mouse cursor movement for this frame.

	The `axis` sets which cursor movement direction will be used (defaults to `X`, e.g. left/right). When `axis` is `X`, motion is along the camera's right; when `Y`, motion is along the camera's forward.
	
	The `distancePerPixel` value is the distance (in world units) to move for each pixel of cursor movement. If null, `DefaultPositionSensitivityMouseCursor` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPositionViaMouseWheel(...)`

:   Adjusts `Position` according to the captured mouse wheel movement for this frame.
	
	The `positiveOrientation` parameter sets which direction the camera moves when the wheel is scrolled forward (defaults to `Orientation2D.Up`, i.e. the camera's forward direction).
	
	The `distancePerWheelIncrement` value is the distance (in world units) to move per scroll increment. If null, `DefaultPositionSensitivityMouseWheel` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPositionViaKeyPress(...)`

:   Adjusts `Position` according to whether a certain key is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `keyToTestFor` is the key that, when pressed, will move the camera.
	
	The `orientation` (an `Orientation2D`) is the direction in which the camera will move when the key is depressed.
	
	The `speed` value is the distance per second to move while the key is depressed. If null, `DefaultPositionSensitivityKeyOrButtonPress` will be used.
	
#### Gamepad
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPositionViaControllerStick(...)`

:   Adjusts `Position` according to the captured controller stick position for this frame.
	
	The stick's polar angle is used to determine which direction (in `Orientation2D` space) to move; the stick's displacement scales the speed.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `maxSpeed` value is the maximum distance per second when the stick is fully displaced. If null, `DefaultPositionSensitivityControllerStick` will be used.
	
	If `useLeftStick` is true, the left controller stick will be measured; otherwise the right stick will be measured. Defaults to `true`.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPositionViaControllerTriggers(...)`

:   Adjusts `Position` according to the captured controller trigger positions for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `positiveOrientation` parameter sets which direction the camera moves when the "positive" trigger is held (defaults to `Orientation2D.Up`).
	
	The `maxSpeed` value is the maximum distance per second when the trigger is fully displaced. If null, `DefaultPositionSensitivityControllerTrigger` will be used.
	
	If `leftTriggerMovesPositive` is true, the left trigger will move in `positiveOrientation` and the right trigger in the opposite direction; otherwise these directions will be reversed. Defaults to `true`.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPositionViaButtonPress(...)`

:   Adjusts `Position` according to whether a certain button is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `buttonToTestFor` is the button that, when pressed, will move the camera.
	
	The `orientation` (an `Orientation2D`) is the direction in which the camera will move when the button is depressed.
	
	The `speed` value is the distance per second to move while the button is depressed. If null, `DefaultPositionSensitivityKeyOrButtonPress` will be used.
	
#### Other
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustPosition(...)`

:   Several overloads are provided to directly adjust `Position` without inspecting any user input data, for building custom per-frame control code:
	
	* `AdjustPosition(Angle polarOrientation, float distance)` — moves a fixed distance along the given polar angle (relative to the camera's forward on the ground plane).
	* `AdjustPosition(Angle polarOrientation, float moveSpeed, float deltaTime)` — speed/time-based version of the above.
	* `AdjustPosition(Orientation2D orientation, float distance)` — moves a fixed distance in the given orientation.
	* `AdjustPosition(Orientation2D orientation, float moveSpeed, float deltaTime)` — speed/time-based version of the above.
	
### Default Controls

The following snippets show the implementation of `AdjustAllViaDefaultControls(...)` for keyboard/mouse and gamepad respectively:

```csharp
// AdjustAllViaDefaultControls(input.KeyboardAndMouse, deltaTime):

AdjustPitchViaMouseCursor(input, pitchAdjustmentPerPixel, invertMouseControl: invertPitchControl);
AdjustYawViaMouseCursor(input, yawAdjustmentPerPixel, invertMouseControl: invertYawControl);

AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowLeft, Orientation2D.Left, moveSpeed);
AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowRight, Orientation2D.Right, moveSpeed);
AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowUp, Orientation2D.Up, moveSpeed);
AdjustPositionViaKeyPress(input, deltaTime, KeyboardOrMouseKey.ArrowDown, Orientation2D.Down, moveSpeed);
```

```csharp
// AdjustAllViaDefaultControls(input.GameControllersCombined, deltaTime):

AdjustPitchViaControllerStick(input, deltaTime, maxPitchAdjustmentPerSec, invertStickControl: invertPitchControl);
AdjustYawViaControllerStick(input, deltaTime, maxYawAdjustmentPerSec, invertStickControl: invertYawControl);

AdjustPositionViaControllerStick(input, deltaTime, maxMoveSpeed);
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
