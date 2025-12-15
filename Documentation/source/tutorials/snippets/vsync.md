---
title: Controlling VSync
description: Snippet demonstrating how to enable or disable vsync
---

## Code

```csharp
var factory = new LocalTinyFfrFactory(
	rendererBuilderConfig: new() {
		EnableVSync = true // (1)!
	}
);
```

1. 	Change this value to `true` or `false` according to your desired behaviour.

??? failure "MacOS Support"
	This setting currently has no effect on MacOS (VSync is always **on**).

	Support will be added at a later date.

## Explanation

"VSync", short for [Vertical Synchronization](https://en.wikipedia.org/wiki/Screen_tearing#Vertical_synchronization), is a configuration setting that controls whether or not your rendered frames must wait for the monitor's refresh rate.

By default, `EnableVSync` is **true**.

#### When to enable VSync

When VSync is enabled, each frame you render to a `Window` will not actually be displayed until the parent `Display`'s next screen update(1). 
{ .annotate }

1. Most monitors refresh at 60Hz, some gaming monitors can be higher, TV screens may be lower.

For most applications this is desirable for two reasons:

* 	"Rendering" frames faster than the display can actually update is a waste of resources/energy. If your display has a 60Hz refresh rate but you're rendering 240 frames per second, 75% of those frames will never be seen.

* 	Updating the display's data buffer mid-refresh usually results in [screen tearing](https://en.wikipedia.org/wiki/Screen_tearing). Keeping VSync enabled eliminates this problem.

#### When to disable VSync

Because VSync blocks the renderer until the monitor cycles, it can reduce throughput in your application. This also means your application loop's maximum frequency will be capped by the target monitor's refresh rate(1).
{ .annotate }

1. Assuming you are rendering to a `Window` at least once per loop.

Relatedly, VSync introduces additional delay between a frame being rendered and it actually being displayed (e.g. "frames" must wait for the next display update refresh cycle). In applications(1) that demand minimal input latency, this can be problematic.
{ .annotate }

1. Such as video games.

If VSync is disabled, TinyFFR will write each rendered frame to the display's pixel buffer as soon as it's ready, with no delay. This will introduce screen tearing, but reduce input latency and increase throughput.
