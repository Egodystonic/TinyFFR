---
title: Keyboard / Mouse Input
description: Information on how to interact with the mouse and keyboard in TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Keyboard & mouse event data can be accessed via the `applicationLoop.Input.KeyboardAndMouse` property. :material-arrow-right: [Reading Input Event Data](#reading-input-event-data)
    * It can also be accessed via the tick callback of a UI framework's loop. :material-arrow-right: [Reading Input Event Data](#reading-input-event-data)
    * The `KeyboardAndMouse` interface provides utility methods (such as `KeyWasPressedThisIteration()`, `KeyIsCurrentlyDown()`, etc). :material-arrow-right: [ILatestKeyboardAndMouseInputRetriever](#ilatestkeyboardandmouseinputretriever)

</div>

## Reading Input Event Data

```csharp
// Standalone: Create a loop, iterate it, access the Input property
var loop = factory.ApplicationLoopBuilder.CreateLoop();
while (!loop.Input.UserQuitRequested) {
	var deltaTime = loop.IterateOnce().AsDeltaTime();
	
	var input = loop.Input;
	var kbm = input.KeyboardAndMouse;
	if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) {
		Console.WriteLine("Spacebar pressed!")
	}
	
	// ... Use input data as desired, render frames, etc
}
```

```csharp
// UI Framework Host: Create UI loop and capture framework input in Tick callback
var loopTerminationDisposable = factory.ApplicationLoopBuilder.StartAvaloniaUiLoop(
	mySceneView,
	Tick
);
void Tick(TimeSpan tickIterationTime, ILatestInputRetriever input) {
	var deltaTime = tickIterationTime.AsDeltaTime();

	var kbm = input.KeyboardAndMouse;
	if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) {
		Console.WriteLine("Spacebar pressed!");
	}
	
	// ... Use input data as desired, render frames, etc
}
```

All input data is accessed through an `ILatestInputRetriever` instance, either:

* Via an [`ApplicationLoop`](application_loops.md) (e.g. `loop.Input`), or,
* Via a UI framework integration loop callback (the example shown above is for Avalonia, but is very similar for other frameworks).

Every time the application loop is successfully iterated, the state of every input device (keyboard, mouse, gamepads) is updated.

## ILatestKeyboardAndMouseInputRetriever

The `ILatestKeyboardAndMouseInputRetriever` interface (accessed via the `input.KeyboardAndMouse` property) is how you can access keyboard and mouse input updates. It provides the following members.

All of the `New[...]` event collections enumerate their events in the order they occurred.


<span class="def-icon">:material-card-bulleted-outline:</span> `NewKeyEvents`

:   This returns an enumerable of `KeyboardOrMouseKeyEvent`s that can be used to discover all the new mouse/keyboard events in this loop iteration.

	Each `KeyboardOrMouseKeyEvent` contains two properties:  

	* A `KeyDown` bool indicating whether this event is for a key being pressed (`true`) or released (`false`);
	* A `Key` which is the `KeyboardOrMouseKey` that is being pressed or released.

	If there are no input updates this loop iteration, this iterator will be empty (0 `Count`).

<span class="def-icon">:material-card-bulleted-outline:</span> `NewKeyDownEvents`

:   This returns an enumerable of `KeyboardOrMouseKey`s that can be used to discover every key that was *pressed* in this loop iteration.

	If you only care about keys being pressed, not released, you can use this property to quickly iterate every new key press.

	This property returns exactly the same set of keys as you'd get iterating through `NewKeyEvents` and filtering for events where `KeyDown` is `true`.

	If no keys were pressed this loop iteration, this iterator will be empty (0 `Count`).

<span class="def-icon">:material-card-bulleted-outline:</span> `NewKeyUpEvents`

:   This returns an enumerable of `KeyboardOrMouseKey`s that can be used to discover every key that was *released* in this loop iteration.

	If you only care about keys being released, not pressed, you can use this property to quickly iterate every new key release.

	This property returns exactly the same set of keys as you'd get iterating through `NewKeyEvents` and filtering for events where `KeyDown` is `false`.

	If no keys were released this loop iteration, this iterator will be empty (0 `Count`).

<span class="def-icon">:material-card-bulleted-outline:</span> `CurrentlyPressedKeys`

:   This returns an enumerable of `KeyboardOrMouseKey`s that can be used to discover every key that is currently being pressed/held-down by the user.

	Note that this is not the same as `NewKeyDownEvents` as this iterator enumerates keys that were pressed in previous loop iterations but are still being pressed/held-down in this iteration.

	If no keys are currently being pressed in this loop iteration, this iterator will be empty (0 `Count`).

<span class="def-icon">:material-card-bulleted-outline:</span> `NewMouseClicks`

:   This returns an enumerable of `MouseClickEvent`s that can be used to discover a list of events detailing every mouse 'click' since the last loop iteration.

	Mouse clicks are "duplicated" in all the other properties (i.e. they count as `CurrentlyPressedKeys` and they emit keyup/keydown events). However, this iterator provides additional mouse-specific details for each mouse click.

	Each `MouseClickEvent` contains the following properties:

	* __Location__: An `XYPair<int>` indicating the pixel position of the cursor relative to the window when the click was made;
	* __Key__: Which `MouseKey` was clicked;
	* __ConsecutiveClickCount__: The number of consecutive clicks made with this button. For example, if this value is '2', this click can be considered a "double-click" operation. The timing of what makes a click "consecutive" is defined by the operating system.
	
		Note that a double-click produces *two* separate events (one with a `ConsecutiveClickCount` of `1`, followed by one with a count of `2`), rather than a single event with a count of `2`.

	If no mouse buttons have been clicked in this loop iteration, this iterator will be empty (0 `Count`).

<span class="def-icon">:material-card-bulleted-outline:</span> `MouseCursorPosition`

:   This returns an `XYPair<int>` indicating which pixel the cursor is currently in relative to the window bounds.

	`(0, 0)` is the top-left corner of the window.

<span class="def-icon">:material-card-bulleted-outline:</span> `MouseCursorDelta`

:   This returns an `XYPair<int>` indicating how many pixels the cursor moved this loop iteration.

	This value will be set even if the cursor is locked to the window, meaning you can use it to determine the user's mouse movements even though the cursor itself does not move.

<span class="def-icon">:material-card-bulleted-outline:</span> `MouseScrollWheelDelta`

:   This returns an `int` indicating how many 'stops' the scroll wheel has moved this loop iteration.

	Positive values indicate scrolling down, negative for up.

<span class="def-icon">:material-card-bulleted-outline:</span> `TranscribedText`

:   This returns a `ReadOnlySpan<char>` containing the text the user typed this loop iteration.

	This is empty unless text transcription has been enabled on the loop (see [Text Input](#text-input) below).

<span class="def-icon">:material-code-block-parentheses:</span> `KeyIsCurrentlyDown(KeyboardOrMouseKey key)`

:   This convenience method lets you quickly know whether a specific key is currently being pressed/held-down.

<span class="def-icon">:material-code-block-parentheses:</span> `KeyWasPressedThisIteration(KeyboardOrMouseKey key)`

:   This convenience method lets you quickly know whether a specific key was pressed this loop iteration.

<span class="def-icon">:material-code-block-parentheses:</span> `KeyWasReleasedThisIteration(KeyboardOrMouseKey key)`

:   This convenience method lets you quickly know whether a specific key was released this loop iteration.

### Text Input

```csharp
loop.EnableInputTextTranscription = true;
// ...
var typedText = loop.Input.KeyboardAndMouse.TranscribedText;
if (typedText.Length > 0) myTextBox.Append(typedText);
```

If you want to let the user type text into your application (e.g. a chat box, a name entry field), you should not attempt to reconstruct text from individual key events. Instead, set `EnableInputTextTranscription` to `true` on your `ApplicationLoop` and read `TranscribedText` each iteration.

Text transcription asks the operating system for the characters the user's keystrokes actually produce, taking in to account their keyboard layout, modifier keys, and any input method editor (used to type languages whose character set is larger than a keyboard).

Whilst transcription is enabled, some keystrokes may be reported *only* as transcribed text and not as key events, because the operating system's text input handling can consume them. Therefore, it is recommended to only enable transcription while the user is actually typing (e.g. while a text field has focus), and disable it again afterwards.

### KeyboardOrMouseKey Enum

This enum contains every keyboard key and mouse button supported by TinyFFR.

??? question "Why combined on one enum?"
	Where possible, the `ILatestKeyboardAndMouseInputRetriever` interface does not separate its API between keyboard keys and mouse buttons; opting instead to attempt to unify the way you consume events for both. 

	This choice was made in order to make it easier to swap/interoperate between bindings for both device types. For example, if you wish to let your users configure their control bindings, a user can now rebind a keyboard key to a mouse click and there's no difference for you in how that's handled in this API.

	If you wish to differentiate between keyboard and mouse events you can use the `GetCategory()` extension method described below.

#### Extension Methods

There are some extension methods defined on `KeyboardOrMouseKey` as follows:

<span class="def-icon">:material-code-block-parentheses:</span> `GetNumericValue()`

:   Returns an `int?` indicating the numeric value of the key (e.g. `3` for the __NumberRow3__ or __Numpad3__ keys).

	If the given key has no numeric representation, this method returns `null` instead.

	You can also reverse this method (i.e. convert an `int` to a `KeyboardOrMouseKey`) using the static method `InputUtils.KeyFromNumericValue()`.

<span class="def-icon">:material-code-block-parentheses:</span> `GetCharacterValue()`

:   Returns a `char?` indicating the character value of the key (e.g. `'a'` for the __A__ key, `' '` for __Space__, etc).

	Letter keys always return their lowercase form, as this reflects the key itself rather than any modifier (e.g. Shift) held at the time it was pressed.

	If the given key has no character representation, this method returns `null` instead.

	You can also reverse this method (i.e. convert a `char` to a `KeyboardOrMouseKey`) using the static method `InputUtils.KeyFromCharacterValue()`. Note that letters must be given in lowercase to match (e.g. `'a'` returns `KeyboardOrMouseKey.A`, but `'A'` returns `null`).

<span class="def-icon">:material-code-block-parentheses:</span> `GetCategory()`

:   Returns a `KeyboardOrMouseKeyCategory` indicating the 'category' of the key (see below for more information on the available categories).

### KeyboardOrMouseKeyCategory Enum

* __Alphabetic__ :material-arrow-right: Represents keyboard keys A through Z.
* __NumberRow__ :material-arrow-right: Represents keyboard keys on the top number row (1, 2, 3, 4, 5, 6, 7, 8, 9, 0).
* __Numpad__ :material-arrow-right: Represents all keyboard keys on the number pad (also known as the keypad), usually to the right of the main keyboard layout.
* __PunctuationAndSymbols__ :material-arrow-right: Represents all keyboard keys that are symbols or punctuation (including space).
* __Modifier__ :material-arrow-right: Represents control, alt, and shift (left and right) keyboard keys.
* __Function__ :material-arrow-right: Represents the F-row keys, typically located at the top of the keyboard.
* __Arrow__ :material-arrow-right: Represents the four arrow keyboard keys (left, right, up, down).
* __EditingAndNavigation__ :material-arrow-right: Represents the six text/page navigation/editing keyboard keys (insert, delete, home, end, page up, page down), usually found above the arrow keys.
* __Control__ :material-arrow-right: Represents the common system/application control keyboard keys (such as escape, return, caps lock, tab, backspace, etc).
* __Mouse__ :material-arrow-right: Represents all mouse buttons.
* __Other__ :material-arrow-right: Represents buttons that are not contained in any other category.

### MouseKey Enum

The `MouseKey` enum is a subset of the `KeyboardOrMouseKey` enum that contains only mouse "keys" (i.e. buttons). This enum is only really used for `MouseClickEvent`s; you shouldn't use it anywhere else.

You can convert a `MouseKey` to a `KeyboardOrMouseKey` by using the `ToKeyboardOrMouseKey()` extension method.

## InputUtils

The static `InputUtils` class also offers the following members:

<span class="def-icon">:material-card-bulleted-outline:</span> `AllKeys`

:   A `ReadOnlySpan<KeyboardOrMouseKey>` of every supported key (excluding `Unknown`). Useful, for example, when building a control rebinding UI.

<span class="def-icon">:material-card-bulleted-outline:</span> `AllCategories`

:   A `ReadOnlySpan<KeyboardOrMouseKeyCategory>` of every key category (excluding `Other`).

<span class="def-icon">:material-code-block-parentheses:</span> `KeyToNumericValue()` / `KeyToCharacterValue()` / `KeyFromNumericValue()` / `KeyFromCharacterValue()`

:   Static equivalents of (and inverses of) the `GetNumericValue()` and `GetCharacterValue()` extension methods described above. The `KeyFrom[...]()` methods return `null` if no key corresponds to the given value.
