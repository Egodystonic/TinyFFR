---
title: Orbital Camera Controller
---

## Summary

The `OrbitalCameraController` orbits a camera around a `Target` location, always pointing at it. The camera's position relative to the target is set via the `Angle`, `Height`, and `Distance` properties. This is useful for "satellite" or "track-cam" style cameras that revolve around a fixed point.

* Adjusting `Angle` rotates the camera around the target;
* Adjusting `Height` raises or lowers the camera along the `UpDirection` axis;
* Adjusting `Distance` moves the camera toward or away from the target on the orbital plane.

### Example Usage

```csharp
// One time setup:
var controller = camera.CreateController<OrbitalCameraController>(); // (1)!
controller.Target = Location.Origin; // (2)!
controller.UpDirection = Direction.Up; // (3)!
controller.ZeroAngleDirection = Direction.Forward; // (4)!

// Per-frame:
controller.AdjustAllViaDefaultControls(input.KeyboardAndMouse, deltaTime); // (5)!
controller.AdjustAllViaDefaultControls(input.GameControllersCombined, deltaTime); // (6)!
controller.Progress(deltaTime); // (7)!
```

1.	This creates the controller, attached to the given `camera`.
2.	This sets the location the camera will orbit around (and always look at).
3.	This sets which way is "up" — the axis the camera orbits around when adjusting `Angle`, and the direction the camera moves along when adjusting `Height`.
4.	This sets the direction *from `Target` to the camera* when `Angle` is zero. In other words, it controls where the camera starts on its orbit.

5.	This manipulates the camera according to the default keyboard and mouse scheme. The angle/height/distance properties will change according to any registered user inputs for this frame.

	You can replace this with more specific control code if desired (see below); or remove it entirely if you do not wish to allow user keyboard/mouse input control.
	
6.	This manipulates the camera according to the default game controller scheme for all game controllers combined. The angle/height/distance properties will change according to any registered user inputs for this frame.

	You can replace this with more specific control code if desired (see below); or remove it entirely if you do not wish to allow user gamepad input control.
	
7.	Calling `Progress()` once per frame is required on all camera controllers in order for them to actually alter their target `Camera`'s parameters.

## Properties

#### Per-Frame Targets

<span class="def-icon">:material-card-bulleted-outline:</span> `Angle`

:   Sets how far around the orbit the camera has rotated, around the `UpDirection` axis, starting from `ZeroAngleDirection`.

	Increasing this value rotates the camera anticlockwise around `UpDirection`; decreasing rotates it clockwise.
	
	Defaults to `0°`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `Height`

:   Sets how far above (or below) the `Target` the camera is, along the `UpDirection` axis.
	
	Defaults to `0.1f`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `Distance`

:   Sets how far from the `Target` the camera is on the orbital plane (the plane perpendicular to `UpDirection`).
	
	Defaults to `0.6f`.
	
#### Configuration

<span class="def-icon">:material-card-bulleted-outline:</span> `Target`

:   The location in the world that the camera is orbiting around, and always looking at.

	Defaults to `Location.Origin`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `UpDirection`

:   The axis the camera orbits around when adjusting `Angle`, and the direction the camera moves along when adjusting `Height`.

	Defaults to `Direction.Up`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `ZeroAngleDirection`

:   The direction from `Target` to the camera when `Angle` is zero.
	
	This value is automatically orthogonalized against `UpDirection`; if the result is invalid (e.g. parallel to `UpDirection`), an arbitrary orthogonal direction is chosen instead.
	
	Defaults to an arbitrary direction orthogonal to `UpDirection`.
	
----

<span class="def-icon">:material-card-bulleted-outline:</span> `AngleRange`

:   Can be set to any value between `0°` and `360°` to set the maximum amount the camera is permitted to rotate from its `ZeroAngleDirection`.

	This value is applied 50% in either direction equally (i.e. a value of `160°` results in the camera being able to orbit 80° anticlockwise and 80° clockwise).
	
	Can be set to `null` to remove any limit (allowing full 360° orbits).
	
	Defaults to `null`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `MinHeight`

:   The minimum permitted value for `Height`. Can be `null` to remove the lower bound.
	
	Defaults to `0.1f`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `MaxHeight`

:   The maximum permitted value for `Height`. Can be `null` to remove the upper bound.
	
	Defaults to `0.5f`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `MinDistance`

:   The minimum permitted value for `Distance`. Can be `null` to remove the lower bound.
	
	Defaults to `0.6f`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `MaxDistance`

:   The maximum permitted value for `Distance`. Can be `null` to remove the upper bound.
	
	Defaults to `2f`.

## Reacting to Input

As camera controllers are often meant to be affected by user input, there are some convenience methods supplied for controlling the primary per-frame target properties:

### Adjusting Angle

#### Keyboard / Mouse

<span class="def-icon">:material-code-block-parentheses:</span> `AdjustAngleViaMouseCursor(...)`

:   Adjusts `Angle` according to the captured mouse cursor movement for this frame.

	The `axis` sets which cursor movement direction will be used (defaults to `X`, e.g. left/right).
	
	The `adjustmentPerPixel` value is the angle to add to `Angle` for each pixel moved according to the given `axis`. If null, `DefaultAngleSensitivityMouseCursor` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustAngleViaMouseWheel(...)`

:   Adjusts `Angle` according to the captured mouse wheel movement for this frame.
	
	The `adjustmentPerWheelIncrement` value is the angle to add to `Angle` for each scroll increment on the mouse wheel. If null, `DefaultAngleSensitivityMouseWheel` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustAngleViaKeyPress(...)`

:   Adjusts `Angle` according to whether a certain key is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `keyToTestFor` is the key that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. Defaults to `false`. This parameter lets you specify two keys in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the angle to add to `Angle` for each second this key is depressed. If null, `DefaultAngleSensitivityKeyOrButtonPress` will be used.
	
#### Gamepad
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustAngleViaControllerStick(...)`

:   Adjusts `Angle` according to the captured controller stick position for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `axis` sets which stick movement direction will be used (defaults to `X`, e.g. left/right).
	
	The `maxAdjustmentPerSec` value is the angle to add to `Angle` when the stick is fully displaced along the given `axis`. If null, `DefaultAngleSensitivityControllerStick` will be used.
	
	If `useLeftStick` is true, the left controller stick will be measured; otherwise the right stick will be measured. Defaults to `false`.
	
	If `invertStickControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustAngleViaControllerTriggers(...)`

:   Adjusts `Angle` according to the captured controller trigger positions for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `maxAdjustmentPerSec` value is the angle to add to `Angle` when the trigger is fully displaced. If null, `DefaultAngleSensitivityControllerTrigger` will be used.
	
	If `leftTriggerRotatesClockwise` is true, the left trigger will rotate clockwise and the right trigger anticlockwise; otherwise these directions will be reversed. Defaults to `true`.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustAngleViaButtonPress(...)`

:   Adjusts `Angle` according to whether a certain button is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `buttonToTestFor` is the button that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. Defaults to `false`. This parameter lets you specify two buttons in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the angle to add to `Angle` for each second this button is depressed. If null, `DefaultAngleSensitivityKeyOrButtonPress` will be used.
	
#### Other
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustAngle(...)`

:   Adjusts `Angle` according to the given turn rate (`adjustmentPerSec`) and time step (`deltaTime`).

	This method does not inspect any user input data but is provided as a convenience for building custom per-frame control code.
	
### Adjusting Height

#### Keyboard / Mouse

<span class="def-icon">:material-code-block-parentheses:</span> `AdjustHeightViaMouseCursor(...)`

:   Adjusts `Height` according to the captured mouse cursor movement for this frame.

	The `axis` sets which cursor movement direction will be used (defaults to `Y`, e.g. up/down).
	
	The `adjustmentPerPixel` value is the amount to add to `Height` for each pixel moved according to the given `axis`. If null, `DefaultHeightSensitivityMouseCursor` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustHeightViaMouseWheel(...)`

:   Adjusts `Height` according to the captured mouse wheel movement for this frame.
	
	The `adjustmentPerWheelIncrement` value is the amount to add to `Height` for each scroll increment on the mouse wheel. If null, `DefaultHeightSensitivityMouseWheel` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustHeightViaKeyPress(...)`

:   Adjusts `Height` according to whether a certain key is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `keyToTestFor` is the key that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. Defaults to `false`. This parameter lets you specify two keys in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the amount to add to `Height` for each second this key is depressed. If null, `DefaultHeightSensitivityKeyOrButtonPress` will be used.
	
#### Gamepad
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustHeightViaControllerStick(...)`

:   Adjusts `Height` according to the captured controller stick position for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `axis` sets which stick movement direction will be used (defaults to `Y`, e.g. up/down).
	
	The `maxAdjustmentPerSec` value is the amount to add to `Height` when the stick is fully displaced along the given `axis`. If null, `DefaultHeightSensitivityControllerStick` will be used.
	
	If `useLeftStick` is true, the left controller stick will be measured; otherwise the right stick will be measured. Defaults to `true`.
	
	If `invertStickControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustHeightViaControllerTriggers(...)`

:   Adjusts `Height` according to the captured controller trigger positions for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `maxAdjustmentPerSec` value is the amount to add to `Height` when the trigger is fully displaced. If null, `DefaultHeightSensitivityControllerTrigger` will be used.
	
	If `leftTriggerRaisesHeight` is true, the left trigger will raise the camera and the right trigger lower it; otherwise these directions will be reversed. Defaults to `true`.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustHeightViaButtonPress(...)`

:   Adjusts `Height` according to whether a certain button is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `buttonToTestFor` is the button that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. Defaults to `false`. This parameter lets you specify two buttons in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the amount to add to `Height` for each second this button is depressed. If null, `DefaultHeightSensitivityKeyOrButtonPress` will be used.
	
#### Other
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustHeight(...)`

:   Adjusts `Height` according to the given rate (`adjustmentPerSec`) and time step (`deltaTime`).

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
	
	If `useLeftStick` is true, the left controller stick will be measured; otherwise the right stick will be measured. Defaults to `true`.
	
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
	
### Default Controls

The following snippets show the implementation of `AdjustAllViaDefaultControls(...)` for keyboard/mouse and gamepad respectively:

```csharp
// AdjustAllViaDefaultControls(input.KeyboardAndMouse, deltaTime):

AdjustAngleViaMouseCursor(input, angleAdjustmentPerPixel, invertMouseControl: invertAngleControl);
AdjustHeightViaMouseCursor(input, heightAdjustmentPerPixel, invertMouseControl: invertHeightControl);
AdjustDistanceViaMouseWheel(input, distanceAdjustmentPerWheelIncrement, invertMouseControl: invertDistanceControl);
```

```csharp
// AdjustAllViaDefaultControls(input.GameControllersCombined, deltaTime):

AdjustAngleViaControllerStick(input, deltaTime, maxAngleAdjustmentPerSec, invertStickControl: invertAngleControl);
AdjustHeightViaControllerTriggers(input, deltaTime, maxHeightAdjustmentPerSec, leftTriggerRaisesHeight: !invertHeightControl);
AdjustDistanceViaControllerStick(input, deltaTime, maxDistanceAdjustmentPerSec, invertStickControl: invertDistanceControl);
```

## Smoothing

Smoothing changes how quickly the controller adjusts the camera to match the current target properties.

The `Angle`, `Height`, and `Distance` target properties can have smoothing applied.

```csharp
// Set properties' smoothing individually:
controller.AngleSmoothingStrength = Strength.VeryMild;
controller.HeightSmoothingStrength = Strength.VeryMild;
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
