---
title: Input
description: This page explains the concept of application loops and input handling in TinyFFR.
---

## Application Loop

All input data is accessed via an `ApplicationLoop` (built via the factory's `ApplicationLoopBuilder`).

Every time the application loop is successfully iterated, the state of every input device (keyboard, mouse, gamepads) is updated.

??? tip "Multiple loops and IterationShouldRefreshGlobalInputStates"
	When creating an `ApplicationLoop` with the builder you can pass a config object and set `IterationShouldRefreshGlobalInputStates` to `false`.

	By default this property is `true`, but if set to `false` iterating the application loop will __not__ update input states.

	The purpose of this is to allow using multiple loops for setting different 'tick rates' for different parts/functions in your application whilst having only one loop globally update the input state of the system.

	Because input state is system/environment-wide, whenever any one loop iterates and updates the global input state that state will be updated/changed for *every* loop's `Input` view.

The `Input` property on the `loop` returns an `ILatestInputRetriever`. As its name implies, this object provides an API for accessing the __latest__ user input events & state since the last application loop iteration:

<span class="def-icon">:material-card-bulleted-outline:</span> `UserQuitRequested`

:   Will be `true` if the user has requested the application exits in this iteration.

	Typically users will request a quit via the window's close button or key combination (e.g. Alt+F4 on Windows).

<span class="def-icon">:material-card-bulleted-outline:</span> `KeyboardAndMouse`

:   Returns an `ILatestKeyboardAndMouseInputRetriever` that is used to access keyboard & mouse input updates.

	See below for usage guide.

<span class="def-icon">:material-card-bulleted-outline:</span> `GameControllers`

:   Returns a `ReadOnlySpan<ILatestGameControllerInputStateRetriever>`. Each `ILatestGameControllerInputStateRetriever` can be used to access input updates for one connected game controller.

	See below for usage guide.

<span class="def-icon">:material-card-bulleted-outline:</span> `GameControllersCombined`

:   Returns an `ILatestGameControllerInputStateRetriever` that represents an amalgam of all updates from every connected game controller this iteration.

	The main purpose of `GameControllersCombined` is to allow you to simply support anyone using your application to connect any controller and begin using it, without having to worry about configuring the "correct" controller.

	If no controllers are currently connected to the system, this property is still valid and the returned state retriever will simply have no state updates set. When the user connects a new controller, this property automatically incorporates its updates seamlessly. 

	See below for usage guide.

The instance returned by `loop.Input` is the same one every time, which means you can hold on to the same `ILatestInputRetriever` reference indefinitely and as long as the `ApplicationLoop` it came from is not disposed, the instance will remain valid and can be used to always access input data for the current frame.

The same lifetime and usage pattern applies to all members of the `ILatestInputRetriever`, including the `ILatestKeyboardAndMouseInputRetriever` returned via the `KeyboardAndMouse` property and the `ILatestGameControllerInputStateRetriever` returned by the `GameControllers`/`GameControllersCombined` properties.

The reason these interfaces are named as *input retrievers* should hopefully now be apparent: They help you retrieve the latest input from the application loop. They do not "contain" or "snapshot" input state.

## Keyboard & Mouse

### KeyboardOrMouseKey Enum

This enum contains every keyboard key and mouse button supported by TinyFFR.

??? question "Why combined on one enum?"
	Where possible, the `ILatestKeyboardAndMouseInputRetriever` interface does not separate its API between keyboard keys and mouse buttons; opting instead to attempt to unify the way you consume events for both. 

	This choice was made in order to make it easier to swap/interoperate between bindings for both device types. For example, if you wish to let your users configure their control bindings, a user can now rebind a keyboard key to a mouse click and there's no difference for you in how that's handled in this API.

	If you wish to differentiate between keyboard and mouse events you can use the `GetCategory()` extension method described below.

There is also an extension method defined for this enum type named `GetCategory()` which returns a `KeyboardOrMouseKeyCategory` indicating the 'category' of the key:

### KeyboardOrMouseKeyCategory Enum

* __Alphabetic__ :material-arrow-right: Represents keyboard keys A through Z.
* __NumberRow__ :material-arrow-right: Represents keyboard keys on the top number row (1, 2, 3, 4, 5, 6, 7, 8, 9, 0).
* __Numpad__ :material-arrow-right: Represents all keyboard keys on the number pad (also known as the keypad), usually to the right of the main keyboard layout.
* __PunctuationAndSymbols__ :material-arrow-right: Represents all keyboard keys that are symbols or punctuation (including space).
* __Modifier__ :material-arrow-right: Represents control, alt, and shift (left and right) keyboard keys.
* __Function__ :material-arrow-right: Represents the four arrow keyboard keys (left, right, up, down).
* __Arrow__ :material-arrow-right: Represents the six text/page navigation/editing keyboard keys (insert, delete, home, end, page up, page down), usually found above the arrow keys.
* __EditingAndNavigation__ :material-arrow-right: Represents the six text/page navigation/editing keyboard keys (insert, delete, home, end, page up, page down), usually found above the arrow keys.
* __Control__ :material-arrow-right: Represents the common system/application control keyboard keys (such as escape, return, caps lock, tab, backspace, etc).
* __Mouse__ :material-arrow-right: Represents all mouse buttons.
* __Other__ :material-arrow-right: Represents buttons that are not contained in any other category.

### MouseKey Enum

The `MouseKey` enum is a subset of the `KeyboardOrMouseKey` enum that contains only mouse "keys" (i.e. buttons). This enum is only really used for `MouseClickEvent`s; you shouldn't use it anywhere else.

You can convert a `MouseKey` to a `KeyboardOrMouseKey` by using the `ToKeyboardOrMouseKey()` extension method.

### ILatestKeyboardAndMouseInputRetriever

The `ILatestKeyboardAndMouseInputRetriever` interface (accessed via the `KeyboardAndMouse` property) is how you can access keyboard and mouse input updates. It provides the following members:

<span class="def-icon">:material-card-bulleted-outline:</span> `NewKeyEvents`

:   This returns a `ReadOnlySpan<KeyboardOrMouseKeyEvent>` containing all the new mouse/keyboard events in this loop iteration.

	Each `KeyboardOrMouseKeyEvent` contains two properties:  

	* A `KeyDown` bool indicating whether this event is for a key being pressed (`true`) or released (`false`);
	* A `Key` which is the `KeyboardOrMouseKey` that is being pressed or released.

	If there are no input updates this loop iteration, this span will have 0 length.

<span class="def-icon">:material-card-bulleted-outline:</span> `NewKeyDownEvents`

:   This returns a `ReadOnlySpan<KeyboardOrMouseKey>` containing every key that was *pressed* in this loop iteration.

	If you only care about keys being pressed, not released, you can use this property to quickly iterate every new key press.

	This property returns exactly the same set of keys as you'd get iterating through `NewKeyEvents` and filtering for events where `KeyDown` is `true`.

	If no keys were pressed this loop iteration, this span will have 0 length.

<span class="def-icon">:material-card-bulleted-outline:</span> `NewKeyUpEvents`

:   This returns a `ReadOnlySpan<KeyboardOrMouseKey>` containing every key that was *released* in this loop iteration.

	If you only care about keys being released, not pressed, you can use this property to quickly iterate every new key release.

	This property returns exactly the same set of keys as you'd get iterating through `NewKeyEvents` and filtering for events where `KeyDown` is `false`.

	If no keys were released this loop iteration, this span will have 0 length.

<span class="def-icon">:material-card-bulleted-outline:</span> `CurrentlyPressedKeys`

:   This returns a `ReadOnlySpan<KeyboardOrMouseKey>` containing every key that is currently being pressed/held-down by the user.

	Note that this is not the same as `NewKeyDownEvents` as this span contains keys that were pressed in previous loop iterations but are still being pressed/held-down in this iteration.

	If no keys are currently being pressed in this loop iteration, this span will have 0 length.

<span class="def-icon">:material-card-bulleted-outline:</span> `NewMouseClicks`

:   This returns a `ReadOnlySpan<MouseClickEvent>` containing a list of events detailing every mouse 'click' since the last loop iteration.

	Mouse clicks are "duplicated" in all the other properties (i.e. they count as `CurrentlyPressedKeys` and they emit keyup/keydown events). However, this span provides additional mouse-specific details for each mouse click.

	Each `MouseClickEvent` contains the following properties:

	* __Location__: An `XYPair<int>` indicating the pixel position of the cursor relative to the window when the click was made;
	* __MouseKey__: Which key was clicked;
	* __ConsecutiveClickCount__: The number of consecutive clicks made with this button. For example, if this value is '2', this click can be considered a "double-click" operation. The timing of what makes a click "consecutive" is defined by the operating system.

	If no mouse buttons have been clicked in this loop iteration, this span will have 0 length.

<span class="def-icon">:material-card-bulleted-outline:</span> `MouseCursorPosition`

:   This returns an `XYPair<int>` indicating which pixel the cursor is currently in relative to the window bounds.

	`(0, 0)` is the top-left corner of the window.

<span class="def-icon">:material-card-bulleted-outline:</span> `MouseCursorDelta`

:   This returns an `XYPair<int>` indicating how many pixels the cursor moved this loop iteration.

	This value will be set even if the cursor is locked to the window, meaning you can use it to determine the user's mouse movements even though the cursor itself does not move.

<span class="def-icon">:material-card-bulleted-outline:</span> `MouseScrollWheelDelta`

:   This returns an `int` indicating how many 'stops' the scroll wheel has moved this loop iteration.

	Positive values indicate scrolling down, negative for up.

<span class="def-icon">:material-code-block-parentheses:</span> `KeyIsCurrentlyDown(KeyboardOrMouseKey key)`

:   This convenience method lets you quickly know whether a specific key is currently being pressed/held-down.

<span class="def-icon">:material-code-block-parentheses:</span> `KeyWasPressedThisIteration(KeyboardOrMouseKey key)`

:   This convenience method lets you quickly know whether a specific key was pressed this loop iteration.

<span class="def-icon">:material-code-block-parentheses:</span> `KeyWasReleasedThisIteration(KeyboardOrMouseKey key)`

:   This convenience method lets you quickly know whether a specific key was released this loop iteration.

## Game Controller

### GameControllerButton Enum

This enum contains every supported game controller button. It also includes `LeftTrigger` and `RightTrigger`; although these are not technically buttons the API will emit events for them when they're pressed past a certain threshold.

### ILatestGameControllerInputStateRetriever

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

:   This returns a `ReadOnlySpan<GameControllerButtonEvent>` containing all the new button events in this loop iteration.

	Each `GameControllerButtonEvent` contains two properties:  

	* A `ButtonDown` bool indicating whether this event is for a button being pressed (`true`) or released (`false`);
	* A `Button` which is the `GameControllerButton` that is being pressed or released.

	If there are no input updates this loop iteration, this span will have 0 length.

<span class="def-icon">:material-card-bulleted-outline:</span> `NewButtonDownEvents`

:   This returns a `ReadOnlySpan<GameControllerButton>` containing every button that was *pressed* in this loop iteration.(1)
	{ .annotate }

	1. Unfortunately this property does *not* inform you about new formal-dress parties in your area.

	If you only care about buttons being pressed, not released, you can use this property to quickly iterate every new button press.

	This property returns exactly the same set of button as you'd get iterating through `NewButtonEvents` and filtering for events where `ButtonDown` is `true`.

	If no buttons were pressed this loop iteration, this span will have 0 length.

<span class="def-icon">:material-card-bulleted-outline:</span> `NewButtonUpEvents`

:   This returns a `ReadOnlySpan<GameControllerButton>` containing every button that was *released* in this loop iteration.

	If you only care about buttons being released, not pressed, you can use this property to quickly iterate every new button release.

	This property returns exactly the same set of buttons as you'd get iterating through `NewButtonEvents` and filtering for events where `ButtonDown` is `false`.

	If no buttons were released this loop iteration, this span will have 0 length.

<span class="def-icon">:material-card-bulleted-outline:</span> `CurrentlyPressedButtons`

:   This returns a `ReadOnlySpan<GameControllerButton>` containing every button that is currently being pressed/held-down by the user.

	Note that this is not the same as `NewButtonDownEvents` as this span contains buttons that were pressed in previous loop iterations but are still being pressed/held-down in this iteration.

	If no buttons are currently being pressed in this loop iteration, this span will have 0 length.

<span class="def-icon">:material-code-block-parentheses:</span> `ButtonIsCurrentlyDown(GameControllerButton button)`

:   This convenience method lets you quickly know whether a specific button is currently being pressed/held-down.

<span class="def-icon">:material-code-block-parentheses:</span> `ButtonWasPressedThisIteration(GameControllerButton button)`

:   This convenience method lets you quickly know whether a specific button was pressed this loop iteration.

<span class="def-icon">:material-code-block-parentheses:</span> `ButtonWasReleasedThisIteration(GameControllerButton button)`

:   This convenience method lets you quickly know whether a specific button was released this loop iteration.

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

	If `Displacement` is within the deadzone, returns `Orientation2D.None`. See [Math & Geometry](math_and_geometry.md) for more information on the `Orientation2D` enum.

	This method takes an optional parameter allowing you to set the size of the deadzone from `0f` to `1f`. If not specified, the default recommended size will be used.

<span class="def-icon">:material-code-block-parentheses:</span> `GetVerticalOrientation()`

:   Returns a `VerticalOrientation2D` enum value indicating which way the stick is being pushed along the up/down axis.

	If `DisplacementVertical` is within the deadzone, returns `VerticalOrientation2D.None`. See [Math & Geometry](math_and_geometry.md) for more information on the `VerticalOrientation2D` enum.

	This method takes an optional parameter allowing you to set the size of the deadzone from `0f` to `1f`. If not specified, the default recommended size will be used.

<span class="def-icon">:material-code-block-parentheses:</span> `GetHorizontalOrientation()`

:   Returns a `HorizontalOrientation2D` enum value indicating which way the stick is being pushed along the left/right axis.

	If `DisplacementHorizontal` is within the deadzone, returns `HorizontalOrientation2D.None`. See [Math & Geometry](math_and_geometry.md) for more information on the `HorizontalOrientation2D` enum.

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


