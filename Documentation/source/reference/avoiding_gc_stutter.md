---
title: Avoiding GC Stutter
description: How to avoid creating garbage in a TinyFFR application, using the IResourceAllocator's pooled memory and collections, SpanUtils, and allocation-free APIs.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Creating garbage every frame makes the .NET garbage collector run regularly, which can cause stutters. :material-arrow-right: [Garbage & Stutter](#garbage-stutter)
    * Borrow temporary buffers and collections from the `IResourceAllocator` instead of creating new arrays. :material-arrow-right: [Borrowing Memory](#borrowing-memory), [Collections](#collections)
    * Work with names and text as spans of `char`s, using `SpanUtils` and TinyFFR's span-based overloads. :material-arrow-right: [Text & Names](#text-names)

</div>

## Garbage & Stutter

Every time your code creates a new object or array on the managed heap (e.g. with `new int[100]`, a `new` class instance, a `string`, or a lambda that captures variables), it creates work for the .NET garbage collector (GC), which must later find and reclaim that memory. When enough garbage builds up, the GC pauses your entire application to clean it up. In a realtime application, that shows up as an occasional long frame which users typically report as 'stuttering'(1).
{ .annotate }

1.	Note that frame stuttering or inconsistent frame timing is not *always* caused by GC churn; it can also be caused by inconsistencies in workloads from frame to frame (i.e. different numbers of polygons, more expensive materials from shot-to-shot, more physics/math calculations on the CPU, etc) or even the host OS itself scheduling your application in certain ways.

The way to avoid this is to create as little garbage as possible in code that runs every frame. TinyFFR is designed so that its own APIs create no garbage in normal use (e.g. rendering frames, moving objects, reading input, or doing math), and it provides the tools on this page to help you do the same.

??? tip "Measuring Allocations"
	`GC.GetAllocatedBytesForCurrentThread()` returns how many bytes the current thread has allocated so far. Calling it before and after a section of code (e.g. one iteration of your application loop) tells you exactly how much garbage that section created:

	```csharp
	var before = GC.GetAllocatedBytesForCurrentThread();
	DoFrame(); // (1)!
	var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
	```

	1.	Your per-frame code. Measure after a few frames have already run, as .NET does some one-off allocation the first few times code runs.

	Watching the minimum framerate (see [Measuring Framerate](measuring_framerate.md)) also helps you spot the stutters themselves; but be aware that not all stuttering is caused by GC churn.
	
### When it Matters

You may or may not need to manage GC churn depending on your typical user's sensitivity to frame stutters. For a complex 3D tool or simulation program, users may not need or even notice smooth frame pacing. You may be targeting a low framerate anyway to reduce power consumption. 

Conversely, for applications like video games users will typically be much more sensitive to stuttering and you will probably want to aim for zero or near-zero allocations on the per-frame path.

No matter your application, a general approach to controlling GC churn is usually beneficial and has a positive performance + power impact. The *level* to which you control GC churn should probably be tempered according to user expectations.

## Borrowing Memory

Every factory has an `IResourceAllocator` (`factory.ResourceAllocator`), which lends out memory from a pool. Borrowed memory is reused from one borrow to the next, so borrowing it creates no garbage.

### Short-Lived Buffers

```csharp
using var texelsLease = factory.ResourceAllocator.BorrowSpan<TexelRgba32>(dimensions.Area); // (1)!
var texels = texelsLease.Span; // (2)!
```

1.	Borrows a span of `dimensions.Area` texels. The `using` returns the memory to the pool when the lease goes out of scope.

2.	The borrowed `Span<TexelRgba32>`, exactly `dimensions.Area` elements long. Don't use it after the lease has been disposed.

<span class="def-icon">:material-code-block-parentheses:</span> `BorrowSpan<T>(numElements, clearMemoryOnLeaseEnd)`

:   Borrows a span of `numElements` elements, returned as a `ScopedSpanLease<T>`. Dispose the lease (e.g. with `using`) when you've finished with it; forgetting to do so leaks memory. Unless you pass `clearMemoryOnLeaseEnd: false`, the memory is zeroed when it's returned. `BorrowReadOnlySpan<T>()` does the same, but returns a read-only span.

For small buffers, C#'s `stackalloc` (e.g. `Span<char> buffer = stackalloc char[64];`) is an alternative that doesn't touch the pool at all. Use it only for small, fixed sizes (a few kilobytes at most), as the stack is limited and running out of it crashes the application.

??? tip "Set `clearMemoryOnLeaseEnd` to `false` Where Possible"
	If your span type `T` does not contain managed references or security-sensitive data, setting `clearMemoryOnLeaseEnd` to `false` is recommended.
	
	The memory clear operation is not zero-cost and if you're borrowing large spans multiple times per frame those costs can definitely add up.
	
	:warning: Beware: Setting `clearMemoryOnLeaseEnd` to `false` when your span type `T` *does* contain managed object references means those references will be kept live by the shared memory pool, meaning those referred-to objects can never be collected.

### Long-Lived Buffers

A `ScopedSpanLease<T>`, like any span, can't be stored in a field. For a buffer you need to keep for longer (e.g. across frames), borrow a `Memory<T>` instead:

```csharp
_vertexBuffer = factory.ResourceAllocator.CreatePooledMemoryBuffer<MeshVertex>(1024); // (1)!
var vertices = _vertexBuffer.Span; // (2)!
factory.ResourceAllocator.ReturnPooledMemoryBuffer(_vertexBuffer); // (3)!
```

1.	Borrows a `Memory<MeshVertex>` of 1024 elements, which can be stored in a field.

2.	Its contents, as a span.

3.	Returns the memory to the pool when you no longer need it. Don't use the buffer after returning it.

## Collections

`List<T>`, `Dictionary<TKey, TValue>`, and `HashSet<T>` create garbage when they're created and whenever they grow. The `IResourceAllocator` offers alternatives that don't.

### Scratch Collections

For a collection you only need during a single method call, borrow a *scratch* collection:

```csharp
var nearbyObjects = factory.ResourceAllocator.GetSharedScratchList<ModelInstance>(); // (1)!
nearbyObjects.Add(instance);
foreach (var nearbyObject in nearbyObjects) { // (2)!
	nearbyObject.MoveBy(Direction.Up * 0.1f);
}
```

1.	Returns an empty, reusable `INonDisposableArrayPoolBackedList<ModelInstance>` (which ultimately implements `IList<ModelInstance>`). `GetSharedScratchDictionary<TKey, TValue>()` and `GetSharedScratchSet<T>()` return an `INonDisposableArrayPoolBackedDictionary<TKey, TValue>` or `INonDisposableArrayPoolBackedSet<T>` in the same way.

2.	Looping with `foreach` creates no garbage either (see [Enumerating Collections](#enumerating-collections) below).

Scratch collections are *shared*. Every call with the same type returns the same instance, cleared and ready to use (pass `clearBuffer: false` to keep its contents). They belong to the allocator, so you never dispose them. That makes them free to use, but it also means you must never keep using one across a call to other code that might ask for the same scratch collection, as that call will clear it and reuse it. 

If one method needs two scratch collections of the same type at once, pass a different `bufferIndex` to each (e.g. `GetSharedScratchList<ModelInstance>(bufferIndex: 1)`).

### Long-Lived Collections

For a collection you need to keep (e.g. in a field), create a new collection. Creating one allocates the collection object itself, but using it never creates any GC presure. Adding, removing, growing, and clearing all reuse pooled memory.

```csharp
_activeEnemies = factory.ResourceAllocator.CreateNewList<ModelInstance>(); // (1)!
_activeEnemies.Add(enemy);
_activeEnemies.Dispose(); // (2)!
```

1.	Creates an `IArrayPoolBackedList<ModelInstance>` (an `IList<ModelInstance>` that you must dispose). `CreateNewDictionary<TKey, TValue>()`, `CreateNewSet<T>()`, and `CreateNewLruCache<TKey, TValue>(maxValuesInCache)` create a dictionary, set, or least-recently-used cache in the same way.

2.	Returns the collection's memory to the pool when you no longer need it. Don't use the collection after disposing it.

If you pre-allocate all collections you need at init-time and never allocate/dispose during your application's runtime, there is no GC cost at all.

### Enumerating Collections

Looping over any of these collections with `foreach` creates no garbage, as long as you keep them as the type the allocator returned (e.g. with `var`). If you pass one around as a plain `IList<T>`, `IDictionary<TKey, TValue>`, `ISet<T>`, or `IEnumerable<T>` instead, each `foreach` creates a small amount of garbage (around 40 bytes), just as it would for any collection accessed through those interfaces. Adding, removing, and looking up elements never create garbage.

## Text & Names

Strings are objects too, so creating them creates garbage. TinyFFR's APIs therefore work with text as spans of `char`s (`ReadOnlySpan<char>`) wherever possible:

* Methods that take a name or text (e.g. `window.SetTitle()`, or the `name` parameter of most creation methods) take a `ReadOnlySpan<char>`, so you can pass part of a larger buffer without creating a `string`.
* To read a resource's name without creating a `string`, use `CopyName()` (with `GetNameLength()` to find how long a buffer it needs) rather than `GetNameAsNewStringObject()`.
* To build text without creating a `string`, write it in to a `Span<char>` with `TryWrite()` or `TryFormat()` (see [Displaying the Framerate](measuring_framerate.md#displaying-the-framerate) for an example).

`SpanUtils` has a few more helpers for working with spans:

```csharp
ReadOnlySpan<char> firstName = "Ben";
ReadOnlySpan<char> lastName = "Bowen";
Span<char> fullName = stackalloc char[SpanUtils.GetConcatenatedLength(firstName, " ", lastName)]; // (1)!
SpanUtils.Concatenate(fullName, firstName, " ", lastName); // (2)!

ReadOnlySpan<byte> utf8Text = "Hello"u8; // (3)!
using var charsLease = factory.ResourceAllocator.BorrowSpan<char>(SpanUtils.GetUtf16Length(utf8Text)); // (4)!
var text = SpanUtils.ConvertUtf8ToUtf16(charsLease.Span, utf8Text); // (5)!
```

1.	A buffer exactly long enough to hold the three spans one after another. `GetConcatenatedLength()` accepts up to eight spans.

2.	Writes the three spans in to `fullName`: "Ben Bowen". `Concatenate()` also accepts up to eight spans, of any element type.

3.	Some UTF-8 encoded text (e.g. read from a file or network stream).

4.	Borrows a buffer exactly long enough to hold the text as .NET's UTF-16 `char`s.

5.	Converts the text, returning the part of the buffer that was written to.

## Other Common Sources of Garbage

* **Lambdas that capture variables** create a new object every time the code that creates them runs (even if the lambda is never called). Where TinyFFR offers an overload taking a function pointer (`delegate*`) instead of a delegate (e.g. `RenderOutputBuffer.ReadNextFrame()`), use it with a `static` method.
* **Passing structs as interfaces** (e.g. a parameter of type `IComparable` or `ITranslatable<Location>`) copies them to the heap ("boxing"). Use generic constraints instead (see [Trait Interfaces](trait_interfaces.md#writing-generic-code)).
* **LINQ** (e.g. `.Where()`, `.Select()`, `.ToList()`) creates garbage for almost every operation. Use ordinary loops in per-frame code (or third-party low-allocation LINQ alternatives).
* **`async` and `await`** create garbage for each operation. Prefer the non-async/await API if you want to absolutely minimize GC churn (see [Asynchronous Loading](asynchronous_loading.md)).

## Configuring the GC

Even with care, some garbage is sometimes unavoidable (e.g. from other libraries). .NET lets you change how and when the GC runs, which can hide whatever garbage is left. These options complement, rather than replace, avoiding garbage in the first place. Most of them only work well when your application creates very little garbage per frame.

### Latency Modes

`System.Runtime.GCSettings.LatencyMode` tells the GC how much it should prioritize short pauses over everything else:

| Mode | Effect |
| :-- | :-- |
| `GCLatencyMode.Interactive` | The default. Most collections of long-lived objects run in the background, alongside your application. |
| `GCLatencyMode.SustainedLowLatency` | Avoids the longest ("blocking full") collections for as long as possible, at the cost of using more memory. Suitable for leaving on while your application runs. |
| `GCLatencyMode.LowLatency` | Avoids collecting long-lived objects at all, unless the system is running out of memory. Only intended for short periods, as memory use can grow quickly. |
| `GCLatencyMode.Batch` | Disables background collections, favoring overall throughput over short pauses. Not suitable for realtime applications. |

```csharp
var previousLatencyMode = GCSettings.LatencyMode;
GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
try {
	RunGame(); // (1)!
}
finally {
	GCSettings.LatencyMode = previousLatencyMode;
}
```

1.	Your application loop.

### Pausing the GC Entirely

You can stop the GC from running at all for a while by starting a *no-GC region*. While it's active, the GC won't collect as long as your code allocates no more than the number of bytes you specify:

```csharp
GC.TryStartNoGCRegion(64 * 1024 * 1024); // (1)!

RunLevel(); // (2)!

if (GCSettings.LatencyMode == GCLatencyMode.NoGCRegion) { // (3)!
	GC.EndNoGCRegion();
}
```

1.	Starts a no-GC region with a budget of 64MB: Your code can allocate up to 64MB in total before the GC has to run again. Before it returns, this method performs a full, blocking collection (to free up as much space as possible), so call it somewhere a short pause won't be noticed, e.g. behind a loading screen.

2.	The time-critical part of your application, e.g. one level of a game.

3.	Ends the region, if it's still active (see the warning below).

???+ warning "Exceeding the Budget"
	If your code allocates more than the budget, the region ends silently and the GC goes back to running normally. Calling `GC.EndNoGCRegion()` after that throws an `InvalidOperationException`, which is why the example checks `GCSettings.LatencyMode` first. Starting a region while one is already active also throws.

	`GC.RegisterNoGCRegionCallback(totalSize, callback)` registers a callback that's invoked once `totalSize` bytes of the budget have been used; you can use it to find out when (and how often) your application runs out of budget.

The GC sets aside (commits) the entire budget as soon as the region starts, so choose a budget only as large as you need. If you've followed the advice on this page, your application should create little or no garbage per frame, so a modest budget can last a long time.

### Collecting at a Good Moment

Loading assets, building scenes, and similar work often create garbage, which the GC might otherwise collect a few seconds later in the middle of gameplay. Triggering a collection yourself, at a moment where a short pause won't be noticed (e.g. at the end of a loading screen, in a pause menu, or between levels), cleans that garbage up while nobody's watching:

```csharp
GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce; // (1)!
GC.Collect(); // (2)!
```

1.	Optional. Also compacts the *large object heap*, where .NET keeps large arrays (85,000 bytes or larger). It isn't compacted by default, so over time it can become fragmented, making new large allocations more likely to trigger collections.

2.	Performs a full, blocking collection immediately. `GC.Collect(2, GCCollectionMode.Aggressive, blocking: true, compacting: true)` additionally returns as much free memory as possible to the operating system.

### Runtime Configuration

Some GC settings can only be chosen at startup, via properties in your application's `.csproj` file (or equivalently, `runtimeconfig.json` entries or `DOTNET_` environment variables). For a realtime application, the defaults are usually the right choice:

| `.csproj` property | Default | Effect |
| :-- | :-- | :-- |
| `ServerGarbageCollection` | `false` | `true` switches to the *server* GC, which is designed for throughput on servers rather than short pauses, and uses considerably more memory and threads. Leave this `false`. |
| `ConcurrentGarbageCollection` | `true` | `true` lets the GC collect long-lived objects in the background, alongside your application, rather than pausing it. Leave this `true`. |
| `RetainVMGarbageCollection` | `false` | `true` makes the GC keep memory it no longer needs, rather than returning it to the operating system, so it can reuse that memory without asking for it again. Can help applications whose memory use rises and falls repeatedly. |

```xml
<PropertyGroup>
	<RetainVMGarbageCollection>true</RetainVMGarbageCollection>
</PropertyGroup>
```

Many more advanced settings (e.g. limits on heap size) are listed in Microsoft's [garbage collector configuration documentation](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector).

### Monitoring the GC

To check whether the GC is responsible for a stutter, record how many collections have happened and how long they've paused your application, e.g. once per frame alongside the [framerate statistics](measuring_framerate.md):

<span class="def-icon">:material-code-block-parentheses:</span> `GC.CollectionCount(generation)`

:   The number of collections of the given *generation* since your application started. Generation `0` collections (of recently-created objects) are frequent and usually short; generation `2` collections (of everything) are rarer and usually the ones that cause stutters.

<span class="def-icon">:material-code-block-parentheses:</span> `GC.GetTotalPauseDuration()`

:   The total time (as a `TimeSpan`) that the GC has paused your application since it started. If this jumps on the same frame as a stutter, the GC was responsible.

<span class="def-icon">:material-code-block-parentheses:</span> `GC.GetGCMemoryInfo()`

:   Detailed information about the most recent collection, including its `PauseDurations`, the `Generation` it collected, and the current heap size.

## UI Frameworks

UI frameworks (e.g. WPF, Avalonia, Windows Forms) tend to allocate and generate garbage as part of their typical usage loop; this is unavoidable, though there are strategies for reducing GC churn with those libraries too (review the respective framework's documentation for more details).
