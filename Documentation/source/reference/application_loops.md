---
title: Application Loops
description: Information on how to create and manage application loops (frame timing/tick loops) with TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * `ApplicationLoop`s are built via the `factory.ApplicationLoopBuilder` and help set & maintain a target framerate. :material-arrow-right: [Purpose](#purpose)
    * You can set a framerate cap via their `TargetFrameRate` property. :material-arrow-right: [Controlling Framerate](#controlling-framerate)
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

## Total Iterated Time

<span class="def-icon">:material-card-bulleted-outline:</span> `TotalIteratedTime`

:   Returns a `TimeSpan` that is the sum of every frame delta the loop has returned so far; i.e. how long the loop has been running, as measured by the loop itself.

	Because this value only advances when the loop is iterated, it is a good clock for anything that should stay in step with your application's simulation (e.g. animation phases, timed events, shader time inputs).

	This property is settable, meaning you can offset or rewind the clock as desired.

<span class="def-icon">:material-code-block-parentheses:</span> `ResetTotalIteratedTime()`

:   Sets `TotalIteratedTime` back to `TimeSpan.Zero`.

## Framerate Statistics

Every `ApplicationLoop` keeps a record of how long each of its most recent iterations took, and offers the following properties for inspecting its recent framerate. Each returns `0f` if the loop has not yet been iterated.

<span class="def-icon">:material-card-bulleted-outline:</span> `FramesPerSecondRecentAverage`

:   The mean framerate across the most recent iterations.

	Because this is calculated as a mean of frame *times* rather than of frame *rates*, an occasional long frame moves it less than you might expect. Consult `FramesPerSecondRecentMin` for the worst case.

<span class="def-icon">:material-card-bulleted-outline:</span> `FramesPerSecondRecentMin`

:   The lowest framerate observed across the most recent iterations (i.e. derived from the single longest iteration).

	This is usually the most informative figure when judging perceived smoothness, as it reflects the worst stutter a user would have noticed.

<span class="def-icon">:material-card-bulleted-outline:</span> `FramesPerSecondRecentMax`

:   The highest framerate observed across the most recent iterations (i.e. derived from the single shortest iteration).

<span class="def-icon">:material-card-bulleted-outline:</span> `FramesPerSecondLatest`

:   The framerate implied by the single most recent iteration (i.e. the reciprocal of the last frame delta).

	This fluctuates from iteration to iteration and is rarely what you want to show a user directly; `FramesPerSecondRecentAverage` is the steadier figure.

??? info "Configuring the Statistics Window"
	By default, the `FramesPerSecondRecentXyz` properties are calculated over the most recent 256 iterations. This can be changed when creating the factory by setting `FrameRateBufferSizeLog2` on a `LocalApplicationLoopBuilderConfig`:

	```csharp
	var factory = new LocalTinyFfrFactory(
		localLoopBuilderConfig: new LocalApplicationLoopBuilderConfig { 
			FrameRateBufferSizeLog2 = 10 // 2^10 = 1024 iterations
		}
	);
	```

	The value is the base-2 logarithm of the number of iterations retained, and must be between `1` and `16` (i.e. 2 to 65,536 iterations). A larger window gives steadier figures that react more slowly to changes in performance.

## Multiplexing Loops

Sometimes you may want certain subsystems of your application to tick at a different (usually lower) rate than your render loop; for example, stepping a physics simulation 30 times per second or sending network updates 10 times per second, whilst still rendering as fast as the display allows.

One way to achieve this is to create additional "sub-loops" with their own framerate caps, and poll them with `TryIterateOnce()` from inside your primary loop:

```csharp
var loop = factory.ApplicationLoopBuilder.CreateLoop();
var physicsLoop = factory.ApplicationLoopBuilder.CreateLoop(frameRateCapHz: 30);
var networkLoop = factory.ApplicationLoopBuilder.CreateLoop(frameRateCapHz: 10);

while (!loop.Input.UserQuitRequested) {
	var deltaTime = loop.IterateOnce().AsDeltaTime();

	if (physicsLoop.TryIterateOnce(out var physicsDelta)) {
		physicsWorld.Step(physicsDelta));
	}
	if (networkLoop.TryIterateOnce(out _)) {
		networkClient.SendStateUpdate();
	}

	UpdateWorld(deltaTime);
	
	renderer.Render();
}
```

Some things to note about this pattern:

* `TryIterateOnce()` never blocks, so the primary loop (and its `IterateOnce()` call) remains in charge of the overall frame pacing. Each sub-loop simply returns `true` whenever its own interval has elapsed, and passes back the delta since *it* last returned `true`.
* Because each sub-loop is only polled once per primary iteration, it can tick *at most* once per frame. Its framerate cap should therefore be at or below the primary loop's framerate. 
* Sub-loops do not "catch up" on missed ticks: if a frame takes long enough that a sub-loop's interval elapses twice, it will still only tick once (with a correspondingly larger delta). If you need a strictly fixed timestep (e.g. for a deterministic physics simulation), accumulate the primary loop's delta yourself and step the simulation in a `while` loop instead.
* Because the sub-loops are created after the primary loop, they will not pump the system event queue (see below); all input is still gathered by the primary loop.

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

	
	
