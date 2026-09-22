---
title: Gamepad Input
description: Information on how to interact with gamepads (game controllers) in TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Gamepad (game controller) states can be accessed via the `applicationLoop.Input.GameControllers` property. :material-arrow-right: [Reading Input Event Data](#reading-input-event-data)
    * You can also read all gamepads' states as one via the `applicationLoop.Input.GameControllersCombined` property. :material-arrow-right: [Reading Input Event Data](#reading-input-event-data)
    * The `GameControllers`/`GameControllersCombined` interfaces provide useful ways to query gamepad state (e.g. stick positions, trigger offsets, button states). :material-arrow-right: [ILatestKeyboardAndMouseInputRetriever](#ilatestkeyboardandmouseinputretriever)

</div>

## Reading Input Event Data

```csharp
// Create a loop, iterate it, access the Input property
var loop = factory.ApplicationLoopBuilder.CreateLoop();
while (!loop.Input.UserQuitRequested) {
	var deltaTime = loop.IterateOnce().AsDeltaTime();
	
	var input = loop.Input;
	var gamepad = input.GameControllersCombined;
	if (gamepad.LeftStickPosition.IsOutsideDeadzone()) {
		MoveCharacter(gamepad);
	}
	
	// ... Use input data as desired, render frames, etc
}
```

All input data is accessed through an `ILatestInputRetriever` instance, via an `ApplicationLoop` (built via the factory's `ApplicationLoopBuilder`).

??? question "Gamepad Support on UI Frameworks"
	When hosting TinyFFR inside a UI framework (such as Avalonia, WPF, WinForms, etc) it defers its input event collection to that host framework (so as not to 'steal' or conflict with that framework's event processing).
	
	Unfortunately, no UI framework today has support for gamepad input retrieval, and therefore gamepads are not supported via TinyFFR when hosted in those frameworks.

Every time the application loop is successfully iterated, the state of every input device (keyboard, mouse, gamepads) is updated.

## ILatestGameControllerInputStateRetriever

The `ILatestGameControllerInputStateRetriever` interface (accessed via the `GameControllers` or `GameControllersCombined` properties) is how you can access gamepad updates. It provides the following members:

<span class="def-icon">:material-card-bulleted-outline:</span> `LeftStickPosition`

:   This returns a `GameControllerStickPosition` indicating the current position of the left analog stick.

	See below for more information about using this value.

<span class="def-icon">:material-card-bulleted-outline:</span> `RightStickPosition`

:   This returns a `GameControllerStickPosition` indicating the current position of the right analog stick.

	See below for more information about using this value.

<span class="def-icon">:material-card-bulleted-outline:</span> `RightTriggerPosition`

:   This returns a `GameControllerTriggerPosition` indicating the current position of the right trigger.

	See below for more information about using this value.

<span class="def-icon">:material-card-bulleted-outline:</span> `LeftTriggerPosition`

:   This returns a `GameControllerTriggerPosition` indicating the current position of the left trigger.

	See below for more information about using this value.

<span class="def-icon">:material-card-bulleted-outline:</span> `RightTriggerPosition`

:   This returns a `GameControllerTriggerPosition` indicating the current position of the right trigger.

	See below for more information about using this value.

<span class="def-icon">:material-card-bulleted-outline:</span> `NewButtonEvents`

:   This returns an enumerable of `GameControllerButtonEvent`s that can be used to discover all the new button events in this loop iteration.

	Each `GameControllerButtonEvent` contains two properties:  

	* A `ButtonDown` bool indicating whether this event is for a button being pressed (`true`) or released (`false`);
	* A `Button` which is the `GameControllerButton` that is being pressed or released.

	If there are no input updates this loop iteration, this iterator will be empty (0 `Count`).

<span class="def-icon">:material-card-bulleted-outline:</span> `NewButtonDownEvents`

:   This returns an enumerable of `GameControllerButton`s that can be used to discover every button that was *pressed* in this loop iteration.(1)
	{ .annotate }

	1. Unfortunately this property does *not* inform you about new formal-dress parties in your area.

	If you only care about buttons being pressed, not released, you can use this property to quickly iterate every new button press.

	This property returns exactly the same set of button as you'd get iterating through `NewButtonEvents` and filtering for events where `ButtonDown` is `true`.

	If no buttons were pressed this loop iteration, this iterator will be empty (0 `Count`).

<span class="def-icon">:material-card-bulleted-outline:</span> `NewButtonUpEvents`

:   This returns an enumerable of `GameControllerButton`s that can be used to discover every button that was *released* in this loop iteration.

	If you only care about buttons being released, not pressed, you can use this property to quickly iterate every new button release.

	This property returns exactly the same set of buttons as you'd get iterating through `NewButtonEvents` and filtering for events where `ButtonDown` is `false`.

	If no buttons were released this loop iteration, this iterator will be empty (0 `Count`).

<span class="def-icon">:material-card-bulleted-outline:</span> `CurrentlyPressedButtons`

:   This returns an enumerable of `GameControllerButton`s that can be used to discover every button that is currently being pressed/held-down by the user.

	Note that this is not the same as `NewButtonDownEvents` as this iterator enumerates buttons that were pressed in previous loop iterations but are still being pressed/held-down in this iteration.

	If no buttons are currently being pressed in this loop iteration, this iterator will be empty (0 `Count`).

<span class="def-icon">:material-code-block-parentheses:</span> `ButtonIsCurrentlyDown(GameControllerButton button)`

:   This convenience method lets you quickly know whether a specific button is currently being pressed/held-down.

<span class="def-icon">:material-code-block-parentheses:</span> `ButtonWasPressedThisIteration(GameControllerButton button)`

:   This convenience method lets you quickly know whether a specific button was pressed this loop iteration.

<span class="def-icon">:material-code-block-parentheses:</span> `ButtonWasReleasedThisIteration(GameControllerButton button)`

:   This convenience method lets you quickly know whether a specific button was released this loop iteration.

### GameControllerButton Enum

This enum contains every supported game controller button. It also includes `LeftTrigger` and `RightTrigger`; although these are not technically buttons the API will emit events for them when they're pressed past a certain threshold.

???+ warning "Enum Value Names Refer to XBOX Layout"
	The buttons are named according to the XBOX controller layout.
	
	For example, `GameControllerButton.X` refers to the "X" button on an XBOX controller, but refers to the square button on a PlayStation controller, and the "Y" button on a Nintendo-style one.

### GameControllerStickPosition Struct

When using the `LeftStickPosition` or `RightStickPosition` property on an `ILatestGameControllerInputStateRetriever` you will be returned a `GameControllerStickPosition` with the following members:

<span class="def-icon">:material-card-bulleted-outline:</span> `Displacement`

:   This returns a `float` in the range `0f` to `1f`, indicating how far the stick is currently moved away from its centre position in any direction.

	A value of `1f` indicates fully moved away from the centre; a value of `0f` indicates the stick is centered (both vertically and horizontally).

<span class="def-icon">:material-card-bulleted-outline:</span> `DisplacementHorizontal`

:   This returns a `float` in the range `1f` to `-1f`, indicating how far the stick is currently moved horizontally.

	A value of `1f` indicates fully moved to the right; a value of `-1f` indicates fully moved to the left; a value of `0f` indicates the stick is centered horizontally.

	You will only see a value of `1f`/`-1f` when the stick is pushed hard to the left/right, not in a diagonal position.

	A value of `0f` indicates that there is no horizontal displacement, but not necessarily that the stick is in its centre position- it may be pushed up or down.

<span class="def-icon">:material-card-bulleted-outline:</span> `DisplacementVertical`

:   This returns a `float` in the range `1f` to `-1f`, indicating how far the stick is currently moved vertically.

	A value of `1f` indicates fully moved to the top; a value of `-1f` indicates fully moved to the bottom; a value of `0f` indicates the stick is centered vertically.

	You will only see a value of `1f`/`-1f` when the stick is pushed hard to the top/bottom, not in a diagonal position.

	A value of `0f` indicates that there is no vertical displacement, but not necessarily that the stick is in its centre position- it may be pushed left or right.

<span class="def-icon">:material-card-bulleted-outline:</span> `DisplacementLevel`

:   This returns an `AnalogDisplacementLevel` enum value that can be used to quickly determine the rough level of displacement. See below for the possible values.

<span class="def-icon">:material-card-bulleted-outline:</span> `DisplacementLevelHorizontal`

:   This returns an `AnalogDisplacementLevel` enum value that can be used to quickly determine the rough level of *horizontal* displacement. See below for the possible values.

<span class="def-icon">:material-card-bulleted-outline:</span> `DisplacementLevelVertical`

:   This returns an `AnalogDisplacementLevel` enum value that can be used to quickly determine the rough level of *vertical* displacement. See below for the possible values.

<span class="def-icon">:material-code-block-parentheses:</span> `IsOutsideDeadzone()`

:   Returns `true` or `false` depending on whether the `Displacement` is outside a deadzone. 

	This method takes an optional parameter allowing you to set the size of the deadzone from `0f` to `1f`. If not specified, the default recommended size will be used.

<span class="def-icon">:material-code-block-parentheses:</span> `IsOutsideDeadzoneVertical()`

:   Returns `true` or `false` depending on whether the `DisplacementVertical` is outside a deadzone. 

	This method takes an optional parameter allowing you to set the size of the deadzone from `0f` to `1f`. If not specified, the default recommended size will be used.

<span class="def-icon">:material-code-block-parentheses:</span> `IsOutsideDeadzoneHorizontal()`

:   Returns `true` or `false` depending on whether the `DisplacementHorizontal` is outside a deadzone. 

	This method takes an optional parameter allowing you to set the size of the deadzone from `0f` to `1f`. If not specified, the default recommended size will be used.

<span class="def-icon">:material-code-block-parentheses:</span> `GetDisplacementWithDeadzone()`

:   Returns `Displacement` adjusted according to a deadzone.

	If `Displacement` is below the deadzone value this method returns `0f`. Otherwise, it returns a value from `0f` to `1f` which is `Displacement` rescaled to the remaining non-deadzone area (e.g. if the deadzone is `0.2f` and `Displacement` is `0.6f`, this method will return `0.5f`, indicating the displacement is half way between the deadzone and the max value.)

	This method takes an optional parameter allowing you to set the size of the deadzone from `0f` to `1f`. If not specified, the default recommended size will be used.

<span class="def-icon">:material-code-block-parentheses:</span> `GetDisplacementVerticalWithDeadzone()`

:   Returns `DisplacementVertical` adjusted according to a deadzone.

	If `DisplacementVertical` is below the deadzone value this method returns `0f`. Otherwise, it returns a value from `0f` to `1f` which is `DisplacementVertical` rescaled to the remaining non-deadzone area (e.g. if the deadzone is `0.2f` and `DisplacementVertical` is `0.6f`, this method will return `0.5f`, indicating the displacement is half way between the deadzone and the max value.)

	This method takes an optional parameter allowing you to set the size of the deadzone from `0f` to `1f`. If not specified, the default recommended size will be used.

<span class="def-icon">:material-code-block-parentheses:</span> `GetDisplacementHorizontalWithDeadzone()`

:   Returns `DisplacementHorizontal` adjusted according to a deadzone.

	If `DisplacementHorizontal` is below the deadzone value this method returns `0f`. Otherwise, it returns a value from `0f` to `1f` which is `DisplacementHorizontal` rescaled to the remaining non-deadzone area (e.g. if the deadzone is `0.2f` and `DisplacementHorizontal` is `0.6f`, this method will return `0.5f`, indicating the displacement is half way between the deadzone and the max value.)

	This method takes an optional parameter allowing you to set the size of the deadzone from `0f` to `1f`. If not specified, the default recommended size will be used.

<span class="def-icon">:material-code-block-parentheses:</span> `AsXYPair()`

:   Returns an `XYPair<float>` containing `GetDisplacementHorizontalWithDeadzone()` as `X` and `GetDisplacementVerticalWithDeadzone()` as `Y`.

	This method takes an optional parameter allowing you to set the size of the deadzone from `0f` to `1f`. If not specified, the default recommended size will be used.

<span class="def-icon">:material-code-block-parentheses:</span> `GetPolarAngle()`

:   Returns an `Angle?` indicating which direction the stick is being pushed towards, or `null` if the stick is within the deadzone.

	A value of `0°` indicates the stick is being pushed exactly to the right; `90°` to the top; `180°` to the left; `270°` to the bottom. This follows the [polar co-ordinate / unit circle convention](conventions.md#2d-handedness-orientation).

	This method takes an optional parameter allowing you to set the size of the deadzone from `0f` to `1f`. If not specified, the default recommended size will be used.

<span class="def-icon">:material-code-block-parentheses:</span> `GetOrientation()`

:   Returns an `Orientation2D` enum value indicating which way the stick is being pushed.

	If `Displacement` is within the deadzone, returns `Orientation2D.None`.

	This method takes an optional parameter allowing you to set the size of the deadzone from `0f` to `1f`. If not specified, the default recommended size will be used.

<span class="def-icon">:material-code-block-parentheses:</span> `GetVerticalOrientation()`

:   Returns a `VerticalOrientation2D` enum value indicating which way the stick is being pushed along the up/down axis.

	If `DisplacementVertical` is within the deadzone, returns `VerticalOrientation2D.None`.

	This method takes an optional parameter allowing you to set the size of the deadzone from `0f` to `1f`. If not specified, the default recommended size will be used.

<span class="def-icon">:material-code-block-parentheses:</span> `GetHorizontalOrientation()`

:   Returns a `HorizontalOrientation2D` enum value indicating which way the stick is being pushed along the left/right axis.

	If `DisplacementHorizontal` is within the deadzone, returns `HorizontalOrientation2D.None`.

	This method takes an optional parameter allowing you to set the size of the deadzone from `0f` to `1f`. If not specified, the default recommended size will be used.

<span class="def-icon">:material-code-block-parentheses:</span> `GetRawDisplacementValues(out short horizontal, out short vertical)`

:   The raw input API used by TinyFFR emits controller data as two signed 16-bit values, one each for the horizontal and vertical axes.

	If you wish to bypass TinyFFR's abstractions and use these values directly, this method allows you to do that.

	These are the values reported by SDL, specifically [SDL_ControllerAxisEvent](https://wiki.libsdl.org/SDL2/SDL_ControllerAxisEvent).

### GameControllerTriggerPosition Struct

<span class="def-icon">:material-card-bulleted-outline:</span> `Displacement`

:   This returns a `float` in the range `0f` to `1f`, indicating how far the trigger has been pulled.

	A value of `1f` indicates fully squeezed down; a value of `0f` indicates the trigger is in its default un-pulled state.

<span class="def-icon">:material-card-bulleted-outline:</span> `DisplacementLevel`

:   This returns an `AnalogDisplacementLevel` enum value that can be used to quickly determine the rough level of displacement. See below for the possible values.

<span class="def-icon">:material-code-block-parentheses:</span> `GetDisplacementWithDeadzone()`

:   Returns `Displacement` adjusted according to a deadzone.

	If `Displacement` is below the deadzone value this method returns `0f`. Otherwise, it returns a value from `0f` to `1f` which is `Displacement` rescaled to the remaining non-deadzone area (e.g. if the deadzone is `0.2f` and `Displacement` is `0.6f`, this method will return `0.5f`, indicating the displacement is half way between the deadzone and the max value.)

	This method takes an optional parameter allowing you to set the size of the deadzone from `0f` to `1f`. If not specified, the default recommended size will be used.

<span class="def-icon">:material-code-block-parentheses:</span> `GetRawDisplacementValue()`

:   The raw input API used by TinyFFR emits the trigger displacement value as a signed 16-bit integer.

	If you wish to bypass TinyFFR's abstractions and use this value directly, this method allows you to do that.

	This is the value reported by SDL, specifically [SDL_ControllerAxisEvent](https://wiki.libsdl.org/SDL2/SDL_ControllerAxisEvent).

### AnalogDisplacementLevel Enum

Some gamepad events will let you quantize the level a trigger is squeezed to an `AnalogDisplacementLevel`. This can be useful if you want to quickly differentiate logic depending how far a user has squeezed the trigger to a few different levels:

* __Full__ :material-arrow-right: Indicates a trigger has been squeezed more than 75% of its max travel distance.

* __Moderate__ :material-arrow-right: Indicates a trigger has been squeezed between 40% and 75% of its max travel distance.

* __Slight__ :material-arrow-right: Indicates a trigger has been squeezed between 15% and 40% of its max travel distance.

* __None__ :material-arrow-right: Indicates a trigger has been squeezed less than 15% of is max travel distance.(1)
{ .annotate }

	1. The reason this is 15% and below (rather than 0%) is to incorporate a small natural deadzone.
