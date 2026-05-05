---
title: Measuring Framerate
description: Snippet demonstrating how to measure FPS
---

## Code

```csharp
var deltaTime = loop.IterateOnce().AsDeltaTime();
window.SetTitle(
	$"FPS: {loop.FramesPerSecondRecentAverage:N0} avg | " +
	$"[{loop.FramesPerSecondRecentMin:N0} - {loop.FramesPerSecondRecentMax:N0}] range | " +
	$"{loop.FramesPerSecondLatest:N0} current"
);
```

## Explanation

The snippet above demonstrates using the reported FPS (frames-per-second) metrics on an `ApplicationLoop` to set a window's title each frame.

The `loop` exposes the following properties:

<span class="def-icon">:material-card-bulleted-outline:</span> `FramesPerSecondRecentAverage`

:   Returns the average framerate of the most recent N frames.

<span class="def-icon">:material-card-bulleted-outline:</span> `FramesPerSecondRecentMin`

:   Returns the lowest recorded framerate of the most recent N frames.

<span class="def-icon">:material-card-bulleted-outline:</span> `FramesPerSecondRecentMax`

:   Returns the highest recorded framerate of the most recent N frames.

<span class="def-icon">:material-card-bulleted-outline:</span> `FramesPerSecondLatest`

:   Returns the framerate of the most recent frame.

### Setting the Framerate Buffer Size

The framerate buffer size (e.g. "`N`" in the property examples above) can be set by supplying a custom `LocalApplicationLoopBuilderConfig` object when creating a `LocalTinyFfrFactory` at initialization-time.


