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

Even with care, some garbage is sometimes unavoidable (e.g. from other libraries). Setting `System.Runtime.GCSettings.LatencyMode` to `GCLatencyMode.SustainedLowLatency` asks .NET to avoid its longest ("blocking full") collections while your application runs, at the cost of using more memory. It doesn't stop collections from happening, so it complements, rather than replaces, avoiding garbage.	
	
## UI Frameworks

UI frameworks (e.g. WPF, Avalonia, Windows Forms) tend to allocate and generate garbage as part of their typical usage loop; this is unavoidable, though there are strategies for reducing GC churn with those libraries too (review the respective framework's documentation for more details).
