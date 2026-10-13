---
title: Measuring Framerate
description: How to measure and display your application's framerate using the ApplicationLoop's framerate statistics, and how to interpret the results.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Every `ApplicationLoop` tracks its recent framerate (average, minimum, maximum, and latest). :material-arrow-right: [Framerate Statistics](#framerate-statistics)
    * It also tracks the equivalent frame *times* (how long each frame takes), which are often more useful than frames per second. :material-arrow-right: [Frame Times](#frame-times)
    * With vsync enabled, the framerate is capped at the display's refresh rate, which hides how much headroom you have. :material-arrow-right: [Interpreting Measurements](#interpreting-measurements)

</div>

## Framerate Statistics

Every [`ApplicationLoop`](application_loops.md) keeps a record of how long each of its most recent iterations took, and offers the following properties for inspecting its recent framerate. Each returns `0f` if the loop hasn't been iterated yet.

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

	This fluctuates from iteration to iteration; `FramesPerSecondRecentAverage` is the steadier figure.

??? info "Configuring the Statistics Window"
	By default, the `FramesPerSecondRecentXyz` and `FrameTimeRecentXyz` properties are calculated over the most recent 256 iterations. This can be changed when creating the factory by setting `FrameRateBufferSizeLog2` on a `LocalApplicationLoopBuilderConfig`:

	```csharp
	var factory = new LocalTinyFfrFactory(
		localLoopBuilderConfig: new LocalApplicationLoopBuilderConfig { 
			FrameRateBufferSizeLog2 = 10 // 2^10 = 1024 iterations
		}
	);
	```

	The value is the base-2 logarithm of the number of iterations retained, and must be between `1` and `16` (i.e. 2 to 65,536 iterations). A larger window gives steadier figures that react more slowly to changes in performance.

## Frame Times

Frames per second is a familiar measure, but many developers like to talk about *frame time* (or frame 'budget'); specifically how long each frame takes, usually in milliseconds. The loop offers the same statistics as frame times, each as a `TimeSpan` (and `TimeSpan.Zero` if the loop hasn't been iterated yet):

<span class="def-icon">:material-card-bulleted-outline:</span> `FrameTimeRecentAverage`

:   The mean time taken by each of the most recent iterations. This is the reciprocal of `FramesPerSecondRecentAverage`.

<span class="def-icon">:material-card-bulleted-outline:</span> `FrameTimeRecentMax`

:   The time taken by the longest of the most recent iterations. This is the reciprocal of `FramesPerSecondRecentMin`, and is the figure to watch for stutter.

<span class="def-icon">:material-card-bulleted-outline:</span> `FrameTimeRecentMin`

:   The time taken by the shortest of the most recent iterations. This is the reciprocal of `FramesPerSecondRecentMax`.

<span class="def-icon">:material-card-bulleted-outline:</span> `FrameTimeLatest`

:   The time taken by the most recent iteration (the same value as the frame delta returned by `IterateOnce()`). This is the reciprocal of `FramesPerSecondLatest`.

Note that the minimum frame time corresponds to the maximum framerate, and vice versa.

```csharp
var averageMs = loop.FrameTimeRecentAverage.TotalMilliseconds; // (1)!
var worstMs = loop.FrameTimeRecentMax.TotalMilliseconds; // (2)!
```

1.	How long frames have been taking on average, in milliseconds.

2.	How long the slowest recent frame took, in milliseconds.

A target framerate is really a time budget for each frame:

| Target framerate | Frame time budget |
| :-: | :-: |
| 30 FPS | 33.3ms |
| 60 FPS | 16.7ms |
| 120 FPS | 8.3ms |
| 144 FPS | 6.9ms |

???+ tip "Frame Times are Often More Useful"
	Frame times add up in a way framerates don't. If a feature adds 2ms to every frame, it costs the same 2ms whether you're running at 30 or 300 FPS (even though it changes the framerate by very different amounts). Comparing `FrameTimeRecentAverage` against your frame time budget shows how much headroom you have.
	
	Therefore when testing the impact of changes to a scene, frame time is often a good place to start measuring.

## Displaying the Framerate

A simple way to keep an eye on the framerate while developing is to show it in the window's title:

```csharp
var timeSinceTitleUpdate = 0f;
while (!loop.Input.UserQuitRequested) {
	var deltaTime = loop.IterateOnce().AsDeltaTime();

	timeSinceTitleUpdate += deltaTime;
	if (timeSinceTitleUpdate >= 0.5f) { // (1)!
		timeSinceTitleUpdate = 0f;
		Span<char> title = stackalloc char[64];
		if (title.TryWrite($"{loop.FramesPerSecondRecentAverage:N0} FPS (slowest frame {loop.FrameTimeRecentMax.TotalMilliseconds:N1}ms)", out var titleLength)) { // (2)!
			window.SetTitle(title[..titleLength]);
		}
	}

	renderer.Render();
}
```

1.	Updates the title twice per second. Updating it every frame would make the number flicker too quickly to read.

2.	Writes the text in to a stack-allocated buffer rather than creating a new `string`, so updating the title doesn't create any garbage.

To show the framerate inside your application instead, use a [canvas](canvas_scenes.md) text object.

## Interpreting Measurements

* **VSync caps the framerate.** With [vsync](render_throughput_and_latency.md#vsync) enabled (the default), the loop runs no faster than the display refreshes, so a 60Hz display shows 60 FPS whether each frame takes 2ms or 16ms. To see how much headroom you really have, temporarily disable vsync (or compare your frame times against the refresh interval).
* **So does `TargetFrameRate`.** If you've set a [framerate cap](application_loops.md#controlling-framerate) on the loop, the framerate can't exceed it.
* **Watch the minimum, not just the average.** A steady average can hide occasional long frames (e.g. while loading assets or when the garbage collector runs), which users notice as stutter. `FrameTimeRecentMax` (or `FramesPerSecondRecentMin`) shows the worst recent frame.
* **Measure Release builds.** Debug builds are considerably slower, so their framerates aren't representative.
* **Minimized windows are paced separately.** Nothing is rendered while a window is minimized; instead, each skipped frame waits as though it had been shown at the display's refresh rate (see [Minimized Windows](render_throughput_and_latency.md#minimized-windows)), so the framerate doesn't reflect normal performance.
