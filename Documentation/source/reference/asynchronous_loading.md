---
title: Asynchronous Loading
description: Information on how to load assets asynchronously (in the background) in TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Every asset-loading method has an `Async` counterpart that does the slow work in the background, so your application keeps running while assets load. :material-arrow-right: [Asynchronous Loading](#asynchronous-loading)
    * Each returned `TinyFfrAsyncOperation` must be consumed __exactly once__, either by fetching its result or by awaiting it. :material-arrow-right: [Working With Async Operations](#working-with-async-operations)
    * How much time each frame spends finishing async work, and how many background threads are used, can both be configured. :material-arrow-right: [Controlling Async Work](#controlling-async-work)

</div>

## Asynchronous Loading

```csharp
var bricksLoad = factory.AssetLoader.LoadColorMapAsync(@"Assets/bricks.png"); // (1)!
var carLoad = factory.AssetLoader.LoadBundledAssetAsync(@"Assets/Models/ToyCar.glb");
Texture? bricks = null;
ModelBundle? car = null;

while (!loop.Input.UserQuitRequested) {
	_ = loop.IterateOnce(); // (2)!

	if (bricksLoad.IsResultAvailable) bricks = bricksLoad.GetResultAndDisposeOperation(); // (3)!
	if (carLoad.IsResultAvailable) car = carLoad.GetResultAndDisposeOperation();

	renderer.Render(); // (4)!
}
```

1.	Starts loading a texture and a bundled asset in the background. These calls return immediately, each with a `TinyFfrAsyncOperation<T>` representing the ongoing load.

2.	Iterating the application loop also gives TinyFFR a chance to finish off any async work that must be done on this thread (see below).

3.	Once an operation's result is available, fetching it with `GetResultAndDisposeOperation()` returns the loaded asset immediately.

4.	Meanwhile, the application keeps rendering frames (e.g. showing a loading screen) rather than freezing until the assets have loaded.

Loading an asset can take a long time (files must be read, image data decoded, textures [compressed](texture_compression.md), meshes processed, etc). When an asset is loaded normally, all of that work happens on the calling thread, which means your application can't render or respond to input until the load completes.

*Asynchronous* loading moves that work on to background (worker) threads instead. You start an async load, carry on running your application as normal, and collect the loaded asset when it's ready.

!!! danger "Thread Safety"
	Throughout this page we will refer to a "primary" thread. This is the thread that you created the factory from (i.e. it's set from the thread that invoked `new LocalTinyFfrFactory()`).
	
	Except where explicitly mentioned, TinyFFR's entire API is single-threaded only. __You can not and must not access most of TinyFFR's API surface from anything *other* than the primary thread__ (and in fact various checks throughout the library are present internally to throw exceptions if you attempt otherwise).
	
	__This includes the `Load[...]Async()` API described on this page.__ Although this API does work internally on worker threads, you must still only ever *invoke* these methods and *collect their results* from the primary thread.

!!! danger "Thread Cooperation"
	The final parts of loading an asset (such as creating its GPU resources) can only be done on the primary thread (this is a restriction of the graphics driver). An async load therefore does as much as possible on worker threads, then queues up these final steps as *cooperative tasks* for the primary thread to complete.

	The primary thread only performs cooperative tasks when it's given the chance to, which normally happens each time your application loop is iterated (via `loop.IterateOnce()` or `loop.TryIterateOnce()`). The amount of time each iteration spends on them is controllable (see [Per-Frame Time Budget](#per-frame-time-budget) below). Cooperative tasks also run while the primary thread is blocked waiting for an async operation, and if TinyFFR is embedded in a UI framework such as Avalonia, WPF, or Windows Forms, whenever the framework's own message loop runs.

	**If nothing gives the primary thread a chance to perform cooperative tasks, async operations will never complete.**

### Supported Operations

Asynchronous counterparts are available for every method on the asset loader that loads an asset, including:

* **Textures:** `LoadTextureAsync()`, every `Load[...]MapAsync()` function (e.g. `LoadColorMapAsync()`, `LoadNormalMapAsync()`), `LoadCanvasTextureAsync()`, `LoadCombinedTextureAsync()`, and `LoadBakedTextureAsync()`;
* **Meshes and models:** `LoadMeshAsync()`, `LoadMeshGroupAsync()`, `LoadBundledAssetAsync()`, `LoadBakedMeshAsync()`, `LoadBakedModelAsync()`, `LoadBakedResourceGroupAsync()`, and `LoadBakedBundledAssetAsync()`;
* **Materials:** `LoadBakedMaterialAsync()`;
* **Fonts:** `LoadFontAsync()` and `LoadBakedFontAsync()`;
* **Backdrop textures:** `LoadBackdropTextureAsync()`, `LoadBackdropTextureFromPreprocessedDirectoryAsync()`, `LoadPreprocessedBackdropTextureAsync()`, `LoadBakedBackdropTextureAsync()`, and `PreprocessHdrOrExrTextureToBackdropTextureDirectoryAsync()`.

Async methods must themselves be *called* from the primary thread. Everything else (including the texture, mesh, material, and object builders) is synchronous, and must also only be used from the primary thread.

## Working With Async Operations

Every async method returns a `TinyFfrAsyncOperation<T>`, where `T` is the type of the asset being loaded.

### Consuming Operations

**Every `TinyFfrAsyncOperation<T>` must be consumed exactly once.** Consuming an operation is what releases the internal data TinyFFR uses to track it; an operation that's never consumed keeps hold of that data for as long as your application runs.

An operation can be consumed in one of two ways:

* By calling `GetResultAndDisposeOperation()`, which returns the operation's result. If the operation hasn't completed yet, this blocks the calling thread until it has. There are also overloads that take a timeout and/or a `CancellationToken`; if the timeout elapses or the token is cancelled before the operation completes, the operation is *not* consumed, and must still be consumed later.
* By `await`ing it (see [Using await](#using-await) below).

Non-generic `TinyFfrAsyncOperation`s (such as the one returned by `PreprocessHdrOrExrTextureToBackdropTextureDirectoryAsync()`, or any `TinyFfrAsyncOperation<T>` converted to one; see [below](#waiting-progress)) have no result to fetch. Instead, they are consumed either by `await`ing them or by calling `DisposeOperation()`, which blocks until the operation completes (with overloads taking a timeout and/or `CancellationToken`, just like `GetResultAndDisposeOperation()`).

The following properties can be used to check on an operation without consuming it:

<span class="def-icon">:material-card-bulleted-outline:</span> `IsResultAvailable`

:   `true` if the operation has completed but hasn't been consumed yet, i.e. calling `GetResultAndDisposeOperation()` now will return immediately.

<span class="def-icon">:material-card-bulleted-outline:</span> `IsCompleted`

:   `true` if the operation has completed. This remains `true` even after the operation has been consumed.

<span class="def-icon">:material-card-bulleted-outline:</span> `IsDisposed`

:   `true` if the operation has been consumed.

### Using await

```csharp
async Task LoadLevelAsync() {
	using var bricks = await factory.AssetLoader.LoadColorMapAsync(@"Assets/bricks.png"); // (1)!
	using var crate = await factory.AssetLoader.LoadMeshAsync(@"Assets/Models/crate.obj");
	// ...
}
```

1.	Awaiting an operation consumes it and returns its result. Execution always resumes on the primary thread, so the loaded asset can be used immediately.

Async operations can also be used with C#'s `async` / `await` keywords. After an `await`, execution always continues on the primary thread, so it's safe to use the loaded asset (or anything else in TinyFFR) straight away.

!!! warning "Await Pitfalls"
	There are a few things to be aware of when using `await`:

	* **Something must give the primary thread a chance to continue.** In a console application this is your application loop; so start iterating your loop before (or while) awaiting. When TinyFFR is embedded in a UI framework, the framework's message loop does this automatically.
	* **`await` creates garbage.** Each `await` allocates objects that the garbage collector must later clean up, which can cause frame-rate stutters. In performance-sensitive code, prefer checking `IsResultAvailable` and calling `GetResultAndDisposeOperation()`, as in the [first example](#asynchronous-loading).
	* **Exceptions in fire-and-forget methods are lost.** If an `async` method that nothing awaits (e.g. `_ = LoadLevelAsync()`) throws an exception after its first `await`, that exception is logged but can not be observed by your code. Catch exceptions inside such methods yourself.

### Waiting & Progress

```csharp
var texLoad = factory.AssetLoader.LoadColorMapAsync(@"Assets/bricks.png");
var meshLoad = factory.AssetLoader.LoadMeshAsync(@"Assets/Models/crate.obj");
var fontLoad = factory.AssetLoader.LoadFontAsync();

while (true) {
	_ = loop.IterateOnce();
	var progress = TinyFfrAsyncOperation.GetCompletionStats(texLoad, meshLoad, fontLoad); // (1)!
	DrawLoadingBar(progress.CompletedFraction);
	renderer.Render();
	if (progress.CompletedCount == progress.OperationCount) break;
}

using var bricks = texLoad.GetResultAndDisposeOperation(); // (2)!
using var crate = meshLoad.GetResultAndDisposeOperation();
using var font = fontLoad.GetResultAndDisposeOperation();
```

1.	Gets a snapshot of how many of the operations have completed so far, without consuming any of them. Here we use it to draw a loading bar (using some hypothetical `DrawLoadingBar()` method).

2.	Once every operation has completed, each is consumed to obtain its result.

Any `TinyFfrAsyncOperation<T>` can be implicitly converted to a non-generic `TinyFfrAsyncOperation`, which allows operations with different result types to be grouped together. The non-generic type offers the following:

<span class="def-icon">:material-card-bulleted-outline:</span> `TinyFfrAsyncOperation.GetCompletionStats()`

:   A static method that returns how many of the given operations have completed (`CompletedCount`) out of the total (`OperationCount`), and the fraction completed (`CompletedFraction`), without consuming any of them. Useful for progress bars and loading screens.

<span class="def-icon">:material-card-bulleted-outline:</span> `operation.WaitForCompletion()` / `TinyFfrAsyncOperation.WaitForAllToComplete()`

:   Blocks the calling (primary) thread until the given operation(s) have completed, optionally with a timeout and/or `CancellationToken`. These do *not* consume the operations; you must still consume each one afterwards.

### Errors

If an async operation fails, the exception is thrown when the operation is consumed (i.e. from `GetResultAndDisposeOperation()` or the `await`). The exception thrown is always an `AggregateException` wrapping the original exception; use `exception.GetBaseException()` (or `exception.InnerException`) to get to the original cause.

## Controlling Async Work

### Per-Frame Time Budget

```csharp
loop.TargetPerFrameAsyncCooperativeTaskTimeFraction = 3f; // (1)!
ShowLoadingScreenUntilEverythingIsLoaded();
loop.TargetPerFrameAsyncCooperativeTaskTimeFraction = 0.25f; // (2)!
```

1.	While showing a loading screen, devotes most of each frame (roughly three quarters) to finishing async work, so loading completes faster.

2.	Restores the default once loading is done, so any further background loading has only a small impact on the frame rate.

Each time the application loop is iterated, it performs pending cooperative tasks (see [above](#asynchronous-loading)) for up to a limited amount of time, set by the loop's `TargetPerFrameAsyncCooperativeTaskTimeFraction` property:

#### Any Positive Value:

The time spent on cooperative tasks each iteration is limited to that fraction of the loop's normal iteration time. Because this is measured from the loop's actual iteration rate, it automatically adapts to the display's refresh rate (including when frame pacing comes from vsync).

The easiest way to think about this value is its effect on frame rate while there's pending work:

| Fraction | Frame Rate While Loading    | Share of Each Frame Spent on Async Work |
| :------- | :-------------------------- | :-------------------------------------- |
| `0.25f`  | ~80% of normal              | ~20%                                    |
| `1f`     | ~50% of normal              | ~50%                                    |
| `3f`     | ~25% of normal              | ~75%                                    |
| `8f`     | ~11% of normal              | ~89%                                    |

Values above `1f` are intended for loading screens, where spending most of each frame on loading is desirable while still rendering at a steady (if reduced) frame rate. Returns diminish above roughly `8f`.

#### Zero:

When the fraction value is `0f` exactly one cooperative task is performed per iteration. Loading still progresses, but never takes up more than one task's worth of any frame.

#### Null:

A value of `null` means no limit: Every pending cooperative task is performed before the iteration continues. This completes all the work in the shortest total time, but a large backlog can stall your application for many frames.

At least one pending task is always performed per iteration (and exactly one on a loop's very first iteration), and the time limit is only checked *between* tasks; so a single long task can still overrun a frame.

`loop.TryIterateOnce()` performs pending cooperative tasks on every call, even when it returns `false` because the next iteration isn't due yet. Passing `executePendingPrimaryThreadCooperativeTasks: false` to `IterateOnce()` or `TryIterateOnce()` skips cooperative tasks entirely for that iteration (meaning async operations won't progress).

The initial value for every loop created by the factory can be set via the `TargetPerFrameAsyncCooperativeTaskTimeFraction` property of the `LocalApplicationLoopBuilderConfig` supplied when creating the factory. By default, this is `0.25f`.

### ThreadingConfig

```csharp
using var factory = new LocalTinyFfrFactory(
	factoryConfig: new LocalTinyFfrFactoryConfig {
		ThreadingConfig = new ThreadingConfig {
			WorkerThreadCount = 2,
			MaxShutdownWaitTime = TimeSpan.FromSeconds(30d)
		}
	}
);
```

The factory's threading behaviour is configured via the `ThreadingConfig` property of the `LocalTinyFfrFactoryConfig` supplied when creating the factory:

<span class="def-icon">:material-card-bulleted-outline:</span> `WorkerThreadCount`

:   The maximum number of worker threads the factory may create to perform async operations. Defaults to `null`, which lets the factory choose an appropriate number for the host machine.

	Setting this to `0` disables asynchrony entirely: Every `Async` method then performs the entire load before returning (blocking the caller, exactly as the non-async methods do), and returns an operation that has already completed. This can be useful for debugging, or on machines where background threads are undesirable.

<span class="def-icon">:material-card-bulleted-outline:</span> `MaxShutdownWaitTime`

:   How long disposing the factory waits for any outstanding async operations to complete before forcibly cancelling them. Defaults to 5 minutes.

	Forcibly cancelling operations can cause worker threads to access resources or memory that has already been freed; so it's best to make sure every async operation has completed (and been consumed) before disposing the factory.

<span class="def-icon">:material-card-bulleted-outline:</span> `InstallTinyFfrSynchronizationContextIfNonePreExisting`

:   If `true` (the default), and the thread creating the factory has no `SynchronizationContext` already, TinyFFR installs its own (and removes it again when the factory is disposed). This is what makes `await` resume on the primary thread in applications that don't use a UI framework.

	If `false`, code following an `await` may resume on a thread-pool thread, which must not use the factory or any TinyFFR resource. If a synchronization context already exists (e.g. because TinyFFR is embedded in a UI framework), this setting has no effect.

<span class="def-icon">:material-card-bulleted-outline:</span> `WakeHostMessageLoopForPrimaryThreadWork`

:   If `true` (the default), and TinyFFR is embedded in a UI framework (i.e. the framework's `SynchronizationContext` is present when the factory is created), TinyFFR wakes the framework's message loop as soon as cooperative tasks are pending, rather than waiting for the next time your application iterates its loop.

<span class="def-icon">:material-card-bulleted-outline:</span> `HostPumpTimeCap`

:   The maximum time each such wake-up may spend performing cooperative tasks before returning control to the UI framework. Defaults to 2 milliseconds.

??? info "Large Bundled Assets"
	When loading a bundled asset asynchronously, TinyFFR finishes each of the asset's sub-meshes (and its materials and textures) in a separate cooperative task, so that loading even a very large asset never causes a single long stall.

	For assets with a realistic number of sub-meshes this makes no noticeable difference to the total loading time. However, for assets with many thousands of sub-meshes, loading asynchronously can take noticeably longer in total than loading synchronously, as roughly one sub-mesh is finished per frame. If total load time matters more than keeping the application responsive, consider loading such assets synchronously instead (e.g. behind a static loading screen).
	
	Additionally, consider [asset baking](asset_bakery.md) for a large speedup (sync or async).
