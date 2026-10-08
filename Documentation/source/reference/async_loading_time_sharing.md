---
title: Async Loading Time-Sharing
description: How to balance asynchronous loading speed against framerate with the application loop's per-frame time budget.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Part of every asynchronous load runs on the primary thread, sharing time with your frames. :material-arrow-right: [Per-Frame Time Fraction](#per-frame-time-fracion)
    * `TargetPerFrameAsyncCooperativeTaskTimeFraction` trades loading speed against framerate; the best value depends on what you're loading. :material-arrow-right: [Two Examples](#two-examples)
    * Some loads aren't affected by it at all. :material-arrow-right: [When It Makes Little Difference](#when-it-makes-little-difference)

</div>

## Per-Frame Time Fraction

[Asynchronous loading](asynchronous_loading.md) does most of its work on background threads, but the final steps of each load (such as creating its GPU resources) must be done on the primary thread. These steps are queued up and performed each time your application loop is iterated, in between rendering frames.

Every step performed makes the frame it's performed on take longer. The application loop's `TargetPerFrameAsyncCooperativeTaskTimeFraction` property sets how much time each iteration may spend on them, as a fraction of a normal frame:

```csharp
loop.TargetPerFrameAsyncCooperativeTaskTimeFraction = 1f; // (1)!
```

1.	Allows up to one whole frame's worth of time per iteration to be spent finishing loads. The default is `0.25f` (a quarter of a frame); `0f` performs only one step per iteration, and `null` removes the limit entirely.

The full details of each value are described in [Per-Frame Time Budget](asynchronous_loading.md#per-frame-time-budget).

How the value affects your application depends heavily on what you're loading (how many assets, how large they are, how much of their loading happens on background threads rather than the primary thread, what type of asset, etc). The following two examples show how differently the same values can behave:

### A Few Large Assets

Loading 60 high-resolution textures at once:

| Value | Total loading time | Average framerate while loading | Longest frame while loading |
| :-- | :-- | :-- | :-- |
| `0f` | 2.2x the default | Unaffected | ~2 normal frames |
| `0.25f` (default) | - | Unaffected | ~2 normal frames |
| `1f` | ~0.7x the default | Slightly reduced | ~2-4 normal frames |
| `3f` | ~0.65x the default | ~55% of normal | ~4 normal frames |
| `8f` | ~0.7x the default | ~35% of normal | ~8 normal frames |
| `null` | ~0.75x the default | ~30% of normal | ~25 normal frames |

Here, values above about `1f` didn't make loading any faster because most of the work (decoding the images) was happening on background threads, so the primary thread was mostly waiting for that work to finish. Raising the value further only made the frames longer.

### Many Tiny Assets

Loading 10,000 small textures at once:

| Value | Total loading time | Average framerate while loading | Longest frame while loading |
| :-- | :-- | :-- | :-- |
| `0f` | ~300x the default (several minutes) | Unaffected | ~1 normal frame |
| `0.25f` (default) | - | ~40% of normal | ~8 normal frames |
| `1f` | ~0.6x the default | ~25% of normal | ~9 normal frames |
| `3f` | ~0.35x the default | ~18% of normal | ~12 normal frames |
| `8f` | ~0.2x the default | ~15% of normal | ~8 normal frames |
| `null` | ~0.2x the default | Everything completed in one frame | ~12 normal frames |

Here, the background work for each texture was trivial, so the primary thread's share was what limited loading speed, and higher values made loading considerably faster. On the other hand, performing only one step per frame (`0f`) made loading take minutes, as each texture needs more than one step.

Notice also that even the default value reduced the framerate considerably in this example; see [What the Budget Doesn't Count](#what-the-budget-doesnt-count) below.

### Things to Consider

As the examples show, there's no single best value, but a few general observations may help when choosing one:

* While your application is running normally (e.g. streaming in assets in the background), the default is a reasonable place to start, as it aims to keep loading from noticeably affecting the framerate.
* Behind a loading screen, where finishing sooner may matter more than a smooth framerate, a higher value can shorten loading times, sometimes considerably. How much higher is worthwhile depends on your assets, so it may be worth trying a few values with your own loading workload. Remember to restore the previous value after your loading screen completes if you intend to stream assets during gameplay.
* `0f` keeps the cost to each frame as low as possible, but can make loading many assets take a very long time. 
* Conversely `null` makes loading as fast as possible, but a large backlog may freeze the application for a noticeable length of time.
* Any other value essentially sets a point on the scale between `0f` and `null`. Higher values can increase loading speed by sacrificing framerate.

### When It Makes Little Difference

The budget can only make a difference when several loading steps are waiting to be performed at once. In some cases that's rarely true:

* **A single large model file.** A bundled asset's meshes are finished one at a time, each waiting on background work before its final step is queued, so there's rarely more than one step waiting. In testing, loading a model with 10,000 separate meshes took the same time (roughly one mesh per frame) with every value, including `null`. For files like this, [baking](pre-baking_assets.md) them, or loading them synchronously behind a loading screen, may be faster.
* **A baked bundle, model, or resource group.** The primary-thread part of loading a baked file with several resources in it is currently performed as a single step. As the budget is only checked *between* steps, a large baked file can cause one long frame whatever the value (in testing, a 1.5-million-triangle scene caused one frame of over a third of a second).
* **Loads dominated by background work** (e.g. decoding and processing a large model file). The primary thread's share is small, so the value has little effect.

Measuring your own frame times while loading (see [Measuring Framerate](measuring_framerate.md)) is the most reliable way to see the effect of a given value on your application.

## What the Budget Doesn't Count

The budget limits the time spent performing loading steps during each loop iteration. However, creating a resource on the GPU can also cause additional work later, when the next frame is rendered, and that work isn't counted against the budget.

For a handful of large assets this extra cost is usually small. But when a single frame creates a very large number of resources (as in the [Many Tiny Assets](#many-tiny-assets) example above, where rendering each frame took several times longer than usual while loading), it can reduce the framerate considerably, even with a low budget. If that's a problem for your application, spreading the loads out over time (e.g. starting them in smaller batches) may help, as may [baking](pre-baking_assets.md) many small assets in to fewer, larger files.

## UI Frameworks

When TinyFFR is embedded in a UI framework (e.g. Avalonia, WPF, or Windows Forms), the framework's own message loop also performs pending steps, for up to `ThreadingConfig.HostPumpTimeCap` at a time (2 milliseconds by default). See [ThreadingConfig](asynchronous_loading.md#threadingconfig).
