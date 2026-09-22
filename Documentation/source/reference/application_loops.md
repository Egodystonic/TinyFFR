---
title: Application Loops
description: Information on how to create and manage application loops (frame timing/tick loops) with TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * `ApplicationLoop`s are built via the `factory.ApplicationLoopBuilder` and help set & maintain a target framerate. :material-arrow-right: [Purpose](#purpose)
    * You can set a framerate cap via their `TargetFramerate` property. :material-arrow-right: [Controlling Framerate](#controlling-framerate)
    * Multiple `ApplicationLoop`s can be used to set up sub-loops at varying tickrates. :material-arrow-right: [Multiplexing Loops](#multiplexing-loops)

</div>

## Purpose

```csharp
var loop = factory.ApplicationLoopBuilder.CreateLoop();
while (!loop.Input.UserQuitRequested) {
	var deltaTime = loop.IterateOnce().AsDeltaTime();
	
	// Do per-frame stuff here
	
	// Typically: Render frames at the end of each tick
	renderer.Render(); 
}
```

`ApplicationLoop`s are the backbone of any standalone (1) realtime (animated) rendering application built with TinyFFR; and are provided to help you produce a constant, consistent render framerate.
{ .annotate }

1.	Standalone in this context means you are not using a hosting UI framework such as Avalonia, WPF, Windows Forms, etc.

	Using ImGui integration still means you're writing a standalone application (TinyFFR hosts ImGui, not the other way around).
	
In other words:
	
* You will typically want to use an app loop when you want to drive one or more [Window](creating_and_managing_windows.md)s at an interactive framerate.
* You do __not__ need an app loop if you're using TinyFFR to render stills or output images only on-demand, ad-hoc.
* You also should not need an app loop if you're using TinyFFR within a host/environment that already provides an event loop to hook in to (e.g. a UI framework such as Avalonia, WPF, WinForms, etc).
	
## Mechanism

### Iteration

A typical usage of an `ApplicationLoop` should wrap it in a `while()` loop, invoking `IterateOnce()` or a related method at the beginning of each loop iteration:

<span class="def-icon">:material-code-block-parentheses:</span> `IterateOnce(...)`

:	This method blocks until the next frame should begin; according to the target framerate (more information on setting this target [below](#controlling-framerate)).

	When it returns, it passes back a `TimeSpan` telling you how much time has passed since the start of the previous iteration (commonly referred to as a "frame delta").
	
	The method returns a `TimeSpan` as it is a higher-resolution measurement of elapsed time, but most real-world rendering code works with a `float` (commonly passed around as `deltaTime`), indicating the fractional number of total seconds elapsed since the previous frame. TinyFFR provides an extension method to quickly convert the returned `TimeSpan` to a `deltaTime` `float`: `var deltaTime = loop.IterateOnce().AsDeltaTime();`.
	
<span class="def-icon">:material-code-block-parentheses:</span> `TryIterateOnce(...)`

:	This method returns `true` if enough time has elapsed since the last frame according to the target framerate; `false` if not.

	When it returns `true`, it passes back a `TimeSpan` representing the frame delta since the *last* time it returned `true`, and iterates system input states; in an identical fashion to `IterateOnce(...)`.
	
	This method is provided as an alternative to `IterateOnce(...)` for workflows where you don't want to block the calling thread waiting for the next frame to be ready.
	
### User Input and Application Exit
	
By default, when a new frame is about to begin(1), TinyFFR will gather the latest input events (e.g. keyboard, mouse, and game controller events) from the system, and update the `Input` property on the target loop to reflect the current state of all input devices.
{ .annotate }

1.	I.e. when `IterateOnce()` is about to return or `TryIterateOnce()` is about to return `true`.

One of the things reported by the input system is whether the user has requested an application exit (i.e. by pressing a window's ❌ button, entering a keybind such as Alt+F4, etc). We use this as the condition for exiting the application loop (and therefore likely proceeding to the teardown/exit portion of our application): `while (!loop.Input.UserQuitRequested) { ... }`.

The input API is described in more detail in [Keyboard / Mouse Input](keyboard_and_mouse_input.md) and [Gamepad Input](gamepad_input.md).

## Controlling Framerate

By default, `ApplicationLoop`s set an unlimited framerate, resulting in your application rendering as many frames as it can (tempered on the GPU side by [vsync](controlling_render_behaviour.md)). 

<span class="def-icon">:material-card-bulleted-outline:</span> `TargetFrameRate`

:   Set to any positive value to set that as your target FPS (framerate) cap. For example, setting this to `30` means your application loop will never exceed thirty iterations per second.

	Setting this to `null` removes the limit entirely (in practice, this means your framerate will be capped by the target display's current refresh rate, known as 'vsync'; this [can also be disabled](controlling_render_behaviour.md) for a truly-unlimited framerate).

## Multiplexing Loops

Claude: Please show example using TryIterateOnce to create a sub-tick inside the primary loop for some subsystem e.g. physics and networking

???+ tip "Multiple loops and IterationShouldPumpSystemEventQueue"
	When creating an `ApplicationLoop` with `CreateLoop(...)` you can optionally provide a config object and set its `IterationShouldPumpSystemEventQueue` property.
	
	By default, the value for `IterationShouldPumpSystemEventQueue` is `null`. If left as `null`: 
	
	* The first application loop you create(1) will be responsible for pumping the OS event queue, collecting application-wide input events, and making sure any active windows are not registered as "non-responsive" by the OS. 
	{ .annotate }
	
		1.	Technically this is actually the first *non-disposed* loop. Disposing all `ApplicationLoop`s and recreating them triggers this logic again each time. 
		
			If you're not disposing your loops until application shut-down this has the same outcome anyway.
	
	* Every *subsequent* loop you create will then forego these operations.
	
	
	Because input state is system/environment-wide, whenever any one loop iterates and updates the global input state that state will be updated/changed for *every* loop's `Input` view; this can actually result in lost input events between the iteration/tick of each individual loop, and you should generally try to avoid this scenario.

	The consequence is that the first loop you create should be the one you use as your "main" or "primary" loop with every other loop being created afterwards. If you want to break this rule, you must set the `IterationShouldPumpSystemEventQueue` property on each loop you create to make sure only one is created with a `true` value and all others are created with `false`.

	
	
