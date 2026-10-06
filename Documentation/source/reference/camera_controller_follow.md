---
title: Follow Camera Controller
description: Information on the FollowCameraController, a third-person "chase camera" that follows a moving target from behind at a set distance, height, and sideways offset.
---

## Summary

The `FollowCameraController` follows a moving `Target` from behind, positioning the camera at a configurable distance, height, and lateral offset and pointing it at a "lookahead" point in front of the target. It's well-suited to third-person chase cameras for vehicles or characters.

The user is expected to update the `Target` (and optionally `TargetForward` / `TargetUp`) every frame to track the followed object.

Additionally:

* `FollowDistance` controls how far behind the target the camera sits;
* `FollowHeight` controls how far above (or below) the target the camera sits;
* `FollowLateralOffset` controls how far to the side of the target the camera sits.

You can change the `Follow[...]` properties each frame (either directly or via `Adjust[...]()` methods) for a more dynamic camera; alternatively you can set these values once and just update the `Target[...]` properties to keep the camera at a fixed distance/offset from the target.

For an overview of how camera controllers work in general, see [Camera Controllers](camera_controllers.md).

### Example Usage

```csharp
// One time setup:
var controller = camera.CreateController<FollowCameraController>(); // (1)!
controller.LookaheadDistance = 2.4f; // (2)!

// Per-frame:
controller.Target = followedObject.Position; // (3)!
controller.TargetForward = followedObject.Forward; // (4)!
controller.TargetUp = Direction.Up; // (5)!

controller.AdjustAllViaDefaultControls(input.KeyboardAndMouse, deltaTime); // (6)!
controller.AdjustAllViaDefaultControls(input.GameControllersCombined, deltaTime); // (7)!
controller.Progress(deltaTime); // (8)!
```

1.	This creates the controller, attached to the given `camera`.

2.	This sets how far ahead of the `Target` the camera should look. Any value of zero or greater is permitted (negative values are ignored).

	This value could also be changed per-frame too if desired.
	
3.	This sets where the `Target` object is in the world.

4.	This sets which way is "forward" for the target. The camera will always be positioned behind the target (i.e. the opposite direction to this).

5.	This sets which way is "up" for the target; the camera will be oriented so its "up" direction matches.

	You may not need to set this value every frame if your target's "up" orientation is always the same direction. Because setting `TargetForward` re-orthogonalizes `TargetUp` against it, set `TargetForward` first when setting both.

6.	This manipulates the camera's offsets according to the default keyboard and mouse scheme.

	You can replace this with more specific control code if desired (see below); or remove it entirely if you do not wish to allow user keyboard/mouse input control.
	
7.	This manipulates the camera's offsets according to the default game controller scheme for all game controllers combined.

	You can replace this with more specific control code if desired (see below); or remove it entirely if you do not wish to allow user gamepad input control.
	
8.	Calling `Progress()` once per frame is required on all camera controllers in order for them to actually alter their target `Camera`'s parameters.

## Properties

#### Per-Frame Targets

<span class="def-icon">:material-card-bulleted-outline:</span> `Target`

:   The location in the world of the target the camera is following. 

	Typically updated every frame to match the moving object's position. Invalid values are ignored.

	Defaults to `Location.Origin`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `TargetForward`

:   The direction the followed object is facing. The camera positions itself behind this direction.
	
	Setting this also re-orthogonalizes `TargetUp` against the new direction. Invalid values (including `Direction.None`) are ignored.
	
	Defaults to `Direction.Forward`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `TargetUp`

:   The "up" direction relative to the followed object. The camera positions itself above the target along this direction.
	
	This value is automatically orthogonalized against `TargetForward`, so reading it back may not return exactly what you set. If the value is parallel to `TargetForward`, an arbitrary perpendicular direction is chosen instead. Invalid values (including `Direction.None`) are ignored.
	
	Defaults to `Direction.Up`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `FollowDistance`

:   How far behind the target (along `-TargetForward`) the camera sits.

	Must be zero or greater (negative and non-finite values are ignored).
	
	You do not need to change this value every frame if you want to maintain a fixed follow distance from the `Target`.

	Defaults to `0.6f`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `FollowHeight`

:   How far above (or below) the target (along `TargetUp`) the camera sits. 

	A positive value results in the camera sitting above the target. Can be negative to make the camera sit below the target, or zero for a neutral camera position.
	
	You do not need to change this value every frame if you want to maintain a fixed follow height relative to the `Target`.
	
	Defaults to `0.3f`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `FollowLateralOffset`

:   How far to the side of the target the camera sits, perpendicular to both `TargetForward` and `TargetUp`. Positive values offset to the left, negative values to the right.

	You do not need to change this value every frame if you want to maintain a fixed lateral distance relative to the `Target`.
	
	Defaults to `0.4f`.

#### Configuration

<span class="def-icon">:material-card-bulleted-outline:</span> `LookaheadDistance`

:   How far in front of the `Target` (along `TargetForward`) the camera will look. 

	Higher values give a "racing game" feel by aiming the camera ahead of the followed object.
	
	Lower values will make the camera look directly at the target which reduces the distance ahead of the target that can be seen. A value of `0f` aims the camera directly at the target.
	
	Must be zero or greater (negative and non-finite values are ignored).
	
	Defaults to `2.4f`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `HeightViewShiftMultiplier`

:   Determines how much the camera's look-at point shifts vertically when `FollowHeight` changes. 

	Increase this value if you want changes in `FollowHeight` to more greatly affect how much above/below the camera looks, decrease it to reduce the effect.
	
	A value of `1f` means the camera will look exactly as far above/below the target (along `TargetUp`) as specified by `FollowHeight`.
	
	A value of `0f` keeps the look-at point level with the target (along `TargetUp`), disabling this functionality entirely.
	
	Must be zero or greater (negative and non-finite values are ignored).
	
	Defaults to `0.44f`.
	
<span class="def-icon">:material-card-bulleted-outline:</span> `LateralOffsetViewShiftMultiplier`

:   Determines how much the camera's look-at point shifts laterally when `FollowLateralOffset` changes. 

	Increase this value if you want changes in `FollowLateralOffset` to more greatly affect how much to the left/right the camera looks, decrease it to reduce the effect.

	A value of `1f` means the camera will look exactly as far to the left/right of the target's forward direction as specified by `FollowLateralOffset`.

	A value of `0f` keeps the look-at point centered on the target's forward axis always, disabling this functionality entirely.
	
	Must be zero or greater (negative and non-finite values are ignored).
	
	Defaults to `0.28f`.

#### Convenience Methods

<span class="def-icon">:material-code-block-parentheses:</span> `Progress(deltaTime, target, targetForward, targetUp)`

:   Sets `Target`, `TargetForward`, and `TargetUp` (in that order), then calls `Progress(deltaTime)`.

<span class="def-icon">:material-code-block-parentheses:</span> `Progress(deltaTime, target, targetForward, targetUp, followDistance, followHeight, followLateralOffset)`

:   As above, but also sets `FollowDistance`, `FollowHeight`, and `FollowLateralOffset`.

<span class="def-icon">:material-code-block-parentheses:</span> `SetConstraints(lookaheadDistance, heightViewShiftMultiplier, lateralOffsetViewShiftMultiplier)`

:   Sets all of the configuration properties above in one call.

<span class="def-icon">:material-code-block-parentheses:</span> `ResetParametersToDefault()`

:   Resets every property (including smoothing) to its default value.

## Reacting to Input

As camera controllers are often meant to be affected by user input, there are some convenience methods supplied for controlling the primary per-frame target properties:

### Adjusting Follow Distance

#### Keyboard / Mouse

<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowDistanceViaMouseCursor(...)`

:   Adjusts `FollowDistance` according to the captured mouse cursor movement for this frame.

	The `axis` sets which cursor movement direction will be used (defaults to `Y`, e.g. up/down).
	
	The `adjustmentPerPixel` value is the amount to add to `FollowDistance` for each pixel moved according to the given `axis`. If null, `DefaultFollowDistanceSensitivityMouseCursor` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowDistanceViaMouseWheel(...)`

:   Adjusts `FollowDistance` according to the captured mouse wheel movement for this frame.
	
	The `adjustmentPerWheelIncrement` value is the amount to add to `FollowDistance` for each scroll increment on the mouse wheel. If null, `DefaultFollowDistanceSensitivityMouseWheel` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowDistanceViaKeyPress(...)`

:   Adjusts `FollowDistance` according to whether a certain key is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `keyToTestFor` is the key that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. This parameter is required, and lets you specify two keys in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the amount to add to `FollowDistance` for each second this key is depressed. If null, `DefaultFollowDistanceSensitivityKeyOrButtonPress` will be used.
	
#### Gamepad
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowDistanceViaControllerStick(...)`

:   Adjusts `FollowDistance` according to the captured controller stick position for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `axis` sets which stick movement direction will be used (defaults to `Y`, e.g. up/down).
	
	The `maxAdjustmentPerSec` value is the amount to add to `FollowDistance` when the stick is fully displaced along the given `axis`. If null, `DefaultFollowDistanceSensitivityControllerStick` will be used.
	
	If `useLeftStick` is true, the left controller stick will be measured; otherwise the right stick will be measured. Defaults to `false`.
	
	If `invertStickControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowDistanceViaControllerTriggers(...)`

:   Adjusts `FollowDistance` according to the captured controller trigger positions for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `maxAdjustmentPerSec` value is the amount to add to `FollowDistance` when the trigger is fully displaced. If null, `DefaultFollowDistanceSensitivityControllerTrigger` will be used.
	
	If `leftTriggerIncreasesDistance` is true, the left trigger will increase the follow distance and the right trigger decrease it; otherwise these directions will be reversed. Defaults to `true`.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowDistanceViaButtonPress(...)`

:   Adjusts `FollowDistance` according to whether a certain button is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `buttonToTestFor` is the button that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. This parameter is required, and lets you specify two buttons in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the amount to add to `FollowDistance` for each second this button is depressed. If null, `DefaultFollowDistanceSensitivityKeyOrButtonPress` will be used.
	
#### Other
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowDistance(...)`

:   Adjusts `FollowDistance` according to the given rate (`adjustmentPerSec`) and time step (`deltaTime`).

	This method does not inspect any user input data but is provided as a convenience for building custom per-frame control code.
	
### Adjusting Follow Height

#### Keyboard / Mouse

<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowHeightViaMouseCursor(...)`

:   Adjusts `FollowHeight` according to the captured mouse cursor movement for this frame.

	The `axis` sets which cursor movement direction will be used (defaults to `Y`, e.g. up/down).
	
	The `adjustmentPerPixel` value is the amount to add to `FollowHeight` for each pixel moved according to the given `axis`. If null, `DefaultFollowHeightSensitivityMouseCursor` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowHeightViaMouseWheel(...)`

:   Adjusts `FollowHeight` according to the captured mouse wheel movement for this frame.
	
	The `adjustmentPerWheelIncrement` value is the amount to add to `FollowHeight` for each scroll increment on the mouse wheel. If null, `DefaultFollowHeightSensitivityMouseWheel` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowHeightViaKeyPress(...)`

:   Adjusts `FollowHeight` according to whether a certain key is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `keyToTestFor` is the key that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. This parameter is required, and lets you specify two keys in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the amount to add to `FollowHeight` for each second this key is depressed. If null, `DefaultFollowHeightSensitivityKeyOrButtonPress` will be used.
	
#### Gamepad
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowHeightViaControllerStick(...)`

:   Adjusts `FollowHeight` according to the captured controller stick position for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `axis` sets which stick movement direction will be used (defaults to `Y`, e.g. up/down).
	
	The `maxAdjustmentPerSec` value is the amount to add to `FollowHeight` when the stick is fully displaced along the given `axis`. If null, `DefaultFollowHeightSensitivityControllerStick` will be used.
	
	If `useLeftStick` is true, the left controller stick will be measured; otherwise the right stick will be measured. Defaults to `false`.
	
	If `invertStickControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowHeightViaControllerTriggers(...)`

:   Adjusts `FollowHeight` according to the captured controller trigger positions for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `maxAdjustmentPerSec` value is the amount to add to `FollowHeight` when the trigger is fully displaced. If null, `DefaultFollowHeightSensitivityControllerTrigger` will be used.
	
	If `rightTriggerRaisesHeight` is true, the right trigger will raise the camera and the left trigger lower it; otherwise these directions will be reversed. Defaults to `true`.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowHeightViaButtonPress(...)`

:   Adjusts `FollowHeight` according to whether a certain button is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `buttonToTestFor` is the button that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. This parameter is required, and lets you specify two buttons in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the amount to add to `FollowHeight` for each second this button is depressed. If null, `DefaultFollowHeightSensitivityKeyOrButtonPress` will be used.
	
#### Other
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowHeight(...)`

:   Adjusts `FollowHeight` according to the given rate (`adjustmentPerSec`) and time step (`deltaTime`).

	This method does not inspect any user input data but is provided as a convenience for building custom per-frame control code.

### Adjusting Follow Lateral Offset

#### Keyboard / Mouse

<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowLateralOffsetViaMouseCursor(...)`

:   Adjusts `FollowLateralOffset` according to the captured mouse cursor movement for this frame.

	The `axis` sets which cursor movement direction will be used (defaults to `X`, e.g. left/right).
	
	The `adjustmentPerPixel` value is the amount to add to `FollowLateralOffset` for each pixel moved according to the given `axis`. If null, `DefaultFollowLateralOffsetSensitivityMouseCursor` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowLateralOffsetViaMouseWheel(...)`

:   Adjusts `FollowLateralOffset` according to the captured mouse wheel movement for this frame.
	
	The `adjustmentPerWheelIncrement` value is the amount to add to `FollowLateralOffset` for each scroll increment on the mouse wheel. If null, `DefaultFollowLateralOffsetSensitivityMouseWheel` will be used.
	
	If `invertMouseControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowLateralOffsetViaKeyPress(...)`

:   Adjusts `FollowLateralOffset` according to whether a certain key is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `keyToTestFor` is the key that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. This parameter is required, and lets you specify two keys in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the amount to add to `FollowLateralOffset` for each second this key is depressed. If null, `DefaultFollowLateralOffsetSensitivityKeyOrButtonPress` will be used.
	
#### Gamepad
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowLateralOffsetViaControllerStick(...)`

:   Adjusts `FollowLateralOffset` according to the captured controller stick position for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `axis` sets which stick movement direction will be used (defaults to `X`, e.g. left/right).
	
	The `maxAdjustmentPerSec` value is the amount to add to `FollowLateralOffset` when the stick is fully displaced along the given `axis`. If null, `DefaultFollowLateralOffsetSensitivityControllerStick` will be used.
	
	If `useLeftStick` is true, the left controller stick will be measured; otherwise the right stick will be measured. Defaults to `false`.
	
	If `invertStickControl` is `true`, the calculated adjustment will be reversed.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowLateralOffsetViaControllerTriggers(...)`

:   Adjusts `FollowLateralOffset` according to the captured controller trigger positions for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `maxAdjustmentPerSec` value is the amount to add to `FollowLateralOffset` when the trigger is fully displaced. If null, `DefaultFollowLateralOffsetSensitivityControllerTrigger` will be used.
	
	If `leftTriggerOffsetsLeft` is true, the left trigger will shift the camera leftward and the right trigger rightward; otherwise these directions will be reversed. Defaults to `true`.
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowLateralOffsetViaButtonPress(...)`

:   Adjusts `FollowLateralOffset` according to whether a certain button is depressed for this frame.
	
	The `deltaTime` value is expected to be the time in seconds of this frame iteration.
	
	The `buttonToTestFor` is the button that, when pressed, will adjust this property.
	
	If `reverse` is `true`, the calculated adjustment will be reversed. This parameter is required, and lets you specify two buttons in a pair that mirror each other by invoking this method twice (once with `reverse` as `false` and once with `reverse` as `true`).
	
	The `adjustmentPerSec` value is the amount to add to `FollowLateralOffset` for each second this button is depressed. If null, `DefaultFollowLateralOffsetSensitivityKeyOrButtonPress` will be used.
	
#### Other
	
<span class="def-icon">:material-code-block-parentheses:</span> `AdjustFollowLateralOffset(...)`

:   Adjusts `FollowLateralOffset` according to the given rate (`adjustmentPerSec`) and time step (`deltaTime`).

	This method does not inspect any user input data but is provided as a convenience for building custom per-frame control code.
	
### Default Controls

The following snippets show the implementation of `AdjustAllViaDefaultControls(...)` for keyboard/mouse and gamepad respectively. Every parameter after `deltaTime` is optional: `invertDistanceControl`, `invertHeightControl`, and `invertLateralControl` reverse each control, and the sensitivity parameters (`distanceAdjustmentPerWheelIncrement`, `heightAdjustmentPerPixel`, and `lateralAdjustmentPerPixel` for keyboard/mouse; `maxDistanceAdjustmentPerSec`, `maxHeightAdjustmentPerSec`, and `maxLateralAdjustmentPerSec` for gamepad) fall back to the default sensitivity constants when `null`:

```csharp
// AdjustAllViaDefaultControls(input.KeyboardAndMouse, deltaTime):

AdjustFollowHeightViaMouseCursor(input, heightAdjustmentPerPixel, invertMouseControl: invertHeightControl);
AdjustFollowDistanceViaMouseWheel(input, distanceAdjustmentPerWheelIncrement, invertMouseControl: invertDistanceControl);
AdjustFollowLateralOffsetViaMouseCursor(input, lateralAdjustmentPerPixel, invertMouseControl: invertLateralControl);
```

```csharp
// AdjustAllViaDefaultControls(input.GameControllersCombined, deltaTime):

AdjustFollowDistanceViaControllerTriggers(input, deltaTime, maxDistanceAdjustmentPerSec, leftTriggerIncreasesDistance: !invertDistanceControl);
AdjustFollowHeightViaControllerStick(input, deltaTime, maxHeightAdjustmentPerSec, invertStickControl: invertHeightControl);
AdjustFollowLateralOffsetViaControllerStick(input, deltaTime, maxLateralAdjustmentPerSec, invertStickControl: invertLateralControl);
```

## Smoothing

Smoothing changes how quickly the controller adjusts the camera to match the current target properties.

The `FollowCameraController` exposes two smoothing strengths:

* `PositionSmoothingStrength` controls how quickly the camera moves to match its target offset (the combination of `FollowDistance`, `FollowHeight`, and `FollowLateralOffset`).
* `TrackingSmoothingStrength` controls how quickly the camera's look-at point catches up to the target. Higher values make the camera "lag" behind the followed object's direction changes, which produces a more cinematic feel.

```csharp
// Set properties' smoothing individually:
controller.PositionSmoothingStrength = SmoothingStrength.VeryMild;
controller.TrackingSmoothingStrength = SmoothingStrength.VeryMild;

// Set all properties' smoothing simultaneously:
controller.SetGlobalSmoothing(SmoothingStrength.VeryMild);
```

The default smoothing for all properties is `VeryMild`. You can choose from `VeryMild`, `Mild`, `Moderate`, `Strong`, `VeryStrong`, or `None`. 

* Smoothing makes the camera feel more 'real' or physical.
* Higher strengths increase this feeling but also increase the latency between setting a target value and the camera actually meeting that target.
* Setting the smoothing to `None` disables smoothing entirely. This means the camera will always be updated to meet exactly the target value of each property on each frame; reducing latency to 0 but making the camera feel less physical.

??? abstract "Custom Smoothing Values"
	If the enum-based approach is not specific enough for your needs, every property can instead have a custom smoothing strength applied via `SetCustomPositionSmoothingStrength()` or `SetCustomTrackingSmoothingStrength()`.
	
	Each method takes a single `float` parameter that indicates the *half-life* of decay between the current value of a property and its target value. A half-life of `0f` disables smoothing. 
	
	The half-life is the time (in seconds) the camera takes to cover half the distance to a new target value, starting from rest. For example, if the current value of X is 50 and the target value of X is 100, a half-life of `1f` would move X to 75 after one second. Because the smoothing carries momentum (see below), X would then be at roughly 92 after the next second, rather than 87.5.
	
	Advanced: Smoothing is implemented via a critically-damped spring, which is why it carries momentum. The `smoothingHalfLife` parameter is translated to become the Ω of the spring equation via the formula `Ω = 1.6783469f / smoothingHalfLife`.
