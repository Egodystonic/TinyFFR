---
title: Clipboard Access
description: Information on how to read and write the system clipboard with TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * The system clipboard can be accessed via the `applicationLoop.Input.Clipboard` property. :material-arrow-right: [Accessing the Clipboard](#accessing-the-clipboard)

</div>

## Accessing the Clipboard

```csharp
while (!loop.Input.UserQuitRequested) {
	var deltaTime = loop.IterateOnce().AsDeltaTime();
	
	var kbm = loop.Input.KeyboardAndMouse;
	var clipboard = loop.Input.Clipboard;

	if (kbm.KeyIsCurrentlyDown(KeyboardOrMouseKey.LeftControl)) {
		if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.C)) {
			clipboard.SetClipboardText("Hello from TinyFFR!");
		}
		if (kbm.KeyWasPressedThisIteration(KeyboardOrMouseKey.V)) {
			Console.WriteLine($"Clipboard contains: {clipboard.GetClipboardTextAsNewStringObject()}");
		}
	}

	// ... Render frames, etc
}
```

The system clipboard is accessed through the `Clipboard` property on an `ILatestInputRetriever` instance, via an `ApplicationLoop` (built via the factory's `ApplicationLoopBuilder`).

Unlike the rest of the input API, the clipboard is not updated once per loop iteration: every read queries the system clipboard directly, and every write takes effect immediately.

Only text is supported; TinyFFR does not currently offer a way to read or write other clipboard content (such as images or files).

??? failure "Not Supported in Headless Mode"
	The clipboard API only works when the factory is __not__ created in "headless mode", e.g.:
	
	```csharp
	var factory = new LocalTinyFfrFactory(
		// ⚠️ Enabling "HeadlessMode" like this disables window creation
		factoryConfig: new LocalTinyFfrFactoryConfig { HeadlessMode = true }
	);
	```
	
	By default, the factory is *not* created in headless mode, but headless mode *is* suggested for UI framework integrations (e.g. Avalonia, WPF, WinForms, etc).
	When using those frameworks, it's recommended to use their APIs for clipboard management instead.

## IInputClipboard

The `IInputClipboard` interface (accessed via the `input.Clipboard` property) provides the following members:

<span class="def-icon">:material-code-block-parentheses:</span> `GetClipboardTextAsNewStringObject()`

:   Returns the text currently on the system clipboard as a newly-allocated `string`.

	If the clipboard holds no text, returns an empty string.

<span class="def-icon">:material-code-block-parentheses:</span> `GetClipboardTextLength()`

:   Returns the length (in `char`s) of the text currently on the system clipboard, or `0` if it holds no text.

<span class="def-icon">:material-code-block-parentheses:</span> `CopyClipboardText(Span<char> destinationBuffer)`

:   Copies the text currently on the system clipboard in to `destinationBuffer` without allocating, and returns the number of `char`s written.

	Use this in combination with `GetClipboardTextLength()` if you want to avoid allocating a new `string`:

	```csharp
	var length = clipboard.GetClipboardTextLength();
	Span<char> buffer = stackalloc char[length];
	var numCharsWritten = clipboard.CopyClipboardText(buffer);
	var clipboardText = buffer[..numCharsWritten];
	```

	The clipboard is shared with every other application on the machine and can therefore change at any moment, including between your calls to `GetClipboardTextLength()` and `CopyClipboardText()`. Therefore, this method never writes more than `destinationBuffer.Length` characters, and you should always use its return value to determine how much of the buffer was actually written.

	??? tip "Large clipboard contents"
		The clipboard may contain an arbitrarily large amount of text (it's entirely up to the user and other applications), so be careful when using `stackalloc` as in the example above. For potentially large contents, consider using a [pooled buffer](avoiding_gc_stutter.md) instead (or setting a maximum length you're willing to read).

<span class="def-icon">:material-code-block-parentheses:</span> `SetClipboardText(ReadOnlySpan<char> newText)`

:   Replaces the contents of the system clipboard with `newText`.
