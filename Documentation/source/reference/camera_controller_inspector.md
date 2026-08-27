---
title: Inspector Camera Controller
---

## Summary

The `InspectorCameraController` orbits a camera around a `Target` location on a sphere defined by `Yaw` and `Pitch` angles, always pointing at the target. This is useful for "model viewer" or "inspector" style cameras that allow a user to inspect an object from any angle.

* Adjusting `Yaw` rotates the camera around the target;
* Adjusting `Pitch` tilts the camera up or down relative to the target;
* Adjusting `Distance` moves the camera toward or away from the target.

### Example Usage

```csharp
// One time setup:
var controller = camera.CreateController<InspectorCameraController>(); // (1)!
controller.WorldUp = Direction.Up; // (2)!
controller.SetConstraints(myMesh.BoundingBox); // (3)!

// Per-frame:
controller.AdjustAllViaDefaultControls(input.KeyboardAndMouse, deltaTime); // (4)!
controller.AdjustAllViaDefaultControls(input.GameControllersCombined, deltaTime); // (5)!
controller.Progress(deltaTime); // (6)!
```

1.	This creates the controller, attached to the given `camera`.
2.	This sets which way is "up" — the axis the camera yaws around, and the reference for what "up" means when tilting via `Pitch`.

3.	This sets the camera's `Target`, `MinDistance`, `MaxDistance` and `Distance` properties according to the bounding box of the mesh you want to inspect.

	For multiple models/meshes (e.g. as loaded by `LoadAll()`) you can calculate an amalgamated bounding box via e.g. `loadedAssetData.Models.CalculateCombinedBoundingBox()`.

	Using this method is optional; you can set those properties manually if preferred.

4.	This manipulates the camera according to the default keyboard and mouse scheme. The yaw/pitch/distance properties will change according to any registered user inputs for this frame.

	You can replace this with more specific control code if desired (see below); or remove it entirely if you do not wish to allow user keyboard/mouse input control.
	
5.	This manipulates the camera according to the default game controller scheme for all game controllers combined. The yaw/pitch/distance properties will change according to any registered user inputs for this frame.

	You can replace this with more specific control code if desired (see below); or remove it entirely if you do not wish to allow user gamepad input control.
	
6.	Calling `Progress()` once per frame is required on all camera controllers in order for them to actually alter their target `Camera`'s parameters.

## Properties

#### Per-Frame Targets

<span class="def-icon">:material-card-bulleted-outline:</span> `Yaw`

:   Sets how far around the target the camera has rotated, around the `WorldUp` axis.

	Increasing this value turns the camera clockwise around the target; decreasing turns anticlockwise.

	Defaults to `0°`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `Pitch`

:   Sets how high or low the camera is tilted relative to the target.
	
	A value of `0°` places the camera level with the target on the horizontal plane; positive values tilt upward, negative values tilt downward.
	
	By default, this value is automatically clamped to ±90° during `Progress()` so that the camera never flips upside-down. Set `AllowUpsideDownFlip` to `true` to disable this behavior.
	
	Defaults to `0°`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `Distance`

:   Sets how far from the `Target` the camera is.

	This value will be clamped between `MinDistance` and `MaxDistance` when either are non-null.
	
	Defaults to `0.6f`.
	
#### Configuration

<span class="def-icon">:material-card-bulleted-outline:</span> `Target`

:   The location in the world that the camera is orbiting around, and always looking at.

	Defaults to `Location.Origin`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `WorldUp`

:   The world's "up" direction. The camera yaws around this axis, and `Pitch` is interpreted relative to it.
	
	Defaults to `Direction.Up`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `AllowUpsideDownFlip`

:   When `false` (the default), `Pitch` is automatically clamped to ±90° each frame so the camera never tilts past straight-up or straight-down.

	When `true`, no such clamp is applied — the camera is free to flip upside-down.
	
	Defaults to `false`.
	
----

<span class="def-icon">:material-card-bulleted-outline:</span> `MinDistance`

:   The minimum permitted value for `Distance`. Can be `null` to remove the lower bound.
	
	Defaults to `0.6f`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `MaxDistance`

:   The maximum permitted value for `Distance`. Can be `null` to remove the upper bound.
	
	Defaults to `2f`.

----

<span class="def-icon">:material-code-block-parentheses:</span> `SetConstraints(...)`

:   A convenience method for inspecting a single object or set of models: sets `Target`, `MinDistance`, `MaxDistance`, and `Distance` together from the supplied `boundingBox`.

	Specifically, the smallest sphere enclosing the cuboid is calculated; then:
	
	* `Target` is set to the centre of that sphere.
	* `MinDistance` is set to the smaller of the sphere's radius and the cuboid's smallest half-extent (so the camera can never end up inside the model, even for very flat objects).
	* `MaxDistance` is set to three times the sphere's radius.
	* `Distance` is set to one-and-a-half times the sphere's radius (a sensible default starting offset).
	
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
	
	If `leftTriggerYawsClockwise` is true, the left trigger will yaw anticlockwise and the right trigger yaw clockwise; otherwise these directions will be reversed. Defaults to `true`.
	
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
	
### Adjusting Distance

#### Keyboard / Mouse

<span class="def-icon">:material-code-block-parentheses:</span> `AdjustDistanceViaMouseCursor(...)`

:   Adjusts `Distance` according to the captured mouse cursor movement for this frame.

	The `axis` sets which cursor movement direction will be used (defaults to `Y`, e.g. up/down).
	
	The `adjustmentPerPixel` value is the amount to add to `Distance` for each pixel moved according to the given `axis`. If null, `DefaultDistanceSensitivityMouseCursor` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustDistanceViaMouseWheel(...)`

:   Adjusts `Distance` according to the captured mouse wheel movement for this frame.
	
	The `adjustmentPerWheelIncrement` value is the amount to add to `Distance` for each scroll increment on the mouse wheel. If null, `DefaultDistanceSensitivityMouseWheel` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustDistanceViaKeyPress(...)`

:   Adjusts `Distance` according to whether a certain key is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `keyToTestFor` is the key that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. Defaults to `false`. This parameter lets you specify two keys in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the amount to add to `Distance` for each second this key is depressed. If null, `DefaultDistanceSensitivityKeyOrButtonPress` will be used.
	
#### Gamepad
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustDistanceViaControllerStick(...)`

:   Adjusts `Distance` according to the captured controller stick position for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `axis` sets which stick movement direction will be used (defaults to `Y`, e.g. up/down).
	
	The `maxAdjustmentPerSec` value is the amount to add to `Distance` when the stick is fully displaced along the given `axis`. If null, `DefaultDistanceSensitivityControllerStick` will be used.
	
	If `useLeftStick` is true, the left controller stick will be measured; otherwise the right stick will be measured. Defaults to `false`.
	
	If `invertStickControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustDistanceViaControllerTriggers(...)`

:   Adjusts `Distance` according to the captured controller trigger positions for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `maxAdjustmentPerSec` value is the amount to add to `Distance` when the trigger is fully displaced. If null, `DefaultDistanceSensitivityControllerTrigger` will be used.
	
	If `leftTriggerIncreasesDistance` is true, the left trigger will increase distance from the target and the right trigger decrease it; otherwise these directions will be reversed. Defaults to `true`.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustDistanceViaButtonPress(...)`

:   Adjusts `Distance` according to whether a certain button is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `buttonToTestFor` is the button that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. Defaults to `false`. This parameter lets you specify two buttons in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the amount to add to `Distance` for each second this button is depressed. If null, `DefaultDistanceSensitivityKeyOrButtonPress` will be used.
	
#### Other
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustDistance(...)`

:   Adjusts `Distance` according to the given rate (`adjustmentPerSec`) and time step (`deltaTime`).

	This method does not inspect any user input data but is provided as a convenience for building custom per-frame control code.
	
### Adjusting Distance (Percentage)

These methods are direct counterparts to the `AdjustDistance...` family above, but each adjustment is interpreted as a fraction of the current `MaxDistance - MinDistance` range rather than as an absolute world-units delta.

This is generally preferable when inspecting an object whose size is not known up-front (e.g. after calling `SetConstraints(...)`) — the controls feel consistent regardless of how large or small the framed object is.

If either `MinDistance` or `MaxDistance` is `null`, the methods fall back to adding the adjustment directly (i.e. they degenerate to using the same units as the equivalent `AdjustDistance...` methods).

#### Keyboard / Mouse

<span class="def-icon">:material-code-block-parentheses:</span> `AdjustDistancePercentageViaMouseCursor(...)`

:   Adjusts `Distance` according to the captured mouse cursor movement for this frame, scaled by the current distance range.

	The `axis` sets which cursor movement direction will be used (defaults to `Y`, e.g. up/down).
	
	The `adjustmentPerPixel` value is the percentage of the distance range to add for each pixel moved according to the given `axis`. If null, `DefaultDistancePercentageSensitivityMouseCursor` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustDistancePercentageViaMouseWheel(...)`

:   Adjusts `Distance` according to the captured mouse wheel movement for this frame, scaled by the current distance range.
	
	The `adjustmentPerWheelIncrement` value is the percentage of the distance range to add for each scroll increment on the mouse wheel. If null, `DefaultDistancePercentageSensitivityMouseWheel` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustDistancePercentageViaKeyPress(...)`

:   Adjusts `Distance` according to whether a certain key is depressed for this frame, scaled by the current distance range.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `keyToTestFor` is the key that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. Defaults to `false`. This parameter lets you specify two keys in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the percentage of the distance range to add for each second this key is depressed. If null, `DefaultDistancePercentageSensitivityKeyOrButtonPress` will be used.
	
#### Gamepad
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustDistancePercentageViaControllerStick(...)`

:   Adjusts `Distance` according to the captured controller stick position for this frame, scaled by the current distance range.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `axis` sets which stick movement direction will be used (defaults to `Y`, e.g. up/down).
	
	The `maxAdjustmentPerSec` value is the percentage of the distance range to add when the stick is fully displaced along the given `axis`. If null, `DefaultDistancePercentageSensitivityControllerStick` will be used.
	
	If `useLeftStick` is true, the left controller stick will be measured; otherwise the right stick will be measured. Defaults to `false`.
	
	If `invertStickControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustDistancePercentageViaControllerTriggers(...)`

:   Adjusts `Distance` according to the captured controller trigger positions for this frame, scaled by the current distance range.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `maxAdjustmentPerSec` value is the percentage of the distance range to add when the trigger is fully displaced. If null, `DefaultDistancePercentageSensitivityControllerTrigger` will be used.
	
	If `leftTriggerIncreasesDistance` is true, the left trigger will increase distance from the target and the right trigger decrease it; otherwise these directions will be reversed. Defaults to `true`.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustDistancePercentageViaButtonPress(...)`

:   Adjusts `Distance` according to whether a certain button is depressed for this frame, scaled by the current distance range.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `buttonToTestFor` is the button that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. Defaults to `false`. This parameter lets you specify two buttons in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the percentage of the distance range to add for each second this button is depressed. If null, `DefaultDistancePercentageSensitivityKeyOrButtonPress` will be used.
	
#### Other
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustDistancePercentage(...)`

:   Adjusts `Distance` according to the given rate (`adjustmentPerSec`) and time step (`deltaTime`), scaled by the current distance range.

	This method does not inspect any user input data but is provided as a convenience for building custom per-frame control code.
	
### Default Controls

The following snippets show the implementation of `AdjustAllViaDefaultControls(...)` for keyboard/mouse and gamepad respectively:

```csharp
// AdjustAllViaDefaultControls(input.KeyboardAndMouse, deltaTime):

AdjustPitchViaMouseCursor(input, pitchAdjustmentPerPixel, invertMouseControl: invertPitchControl);
AdjustYawViaMouseCursor(input, yawAdjustmentPerPixel, invertMouseControl: invertYawControl);
AdjustDistancePercentageViaMouseWheel(input, distancePercentageAdjustmentPerWheelIncrement, invertMouseControl: invertDistanceControl);
```

```csharp
// AdjustAllViaDefaultControls(input.GameControllersCombined, deltaTime):

AdjustPitchViaControllerStick(input, deltaTime, maxPitchAdjustmentPerSec, invertStickControl: invertPitchControl);
AdjustYawViaControllerStick(input, deltaTime, maxYawAdjustmentPerSec, invertStickControl: invertYawControl);
AdjustDistancePercentageViaControllerTriggers(input, deltaTime, maxDistancePercentageAdjustmentPerSec, leftTriggerIncreasesDistance: !invertDistanceControl);
```

## Smoothing

Smoothing changes how quickly the controller adjusts the camera to match the current target properties.

The `Yaw`, `Pitch`, and `Distance` target properties can have smoothing applied. Note that `Yaw` and `Pitch` share a single `RotationSmoothingStrength` setting — they cannot be smoothed independently of one another.

```csharp
// Set properties' smoothing individually:
controller.RotationSmoothingStrength = Strength.VeryMild; // Applies to both Yaw and Pitch
controller.DistanceSmoothingStrength = Strength.VeryMild;

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
