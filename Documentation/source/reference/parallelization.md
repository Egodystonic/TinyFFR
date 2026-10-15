---
title: Parallelization
description: How to split your own CPU work across TinyFFR's worker threads with the ParallelizationProvider, and which TinyFFR methods do so for you.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * `ParallelizationProvider` splits CPU work in to blocks and runs them at the same time on TinyFFR's worker threads, without allocating. :material-arrow-right: [Parallelizing Work](#parallelizing-work)
    * Textures, dynamic meshes, and mutable grids have `...Parallelized()` methods that also use the provider under-the-hood. :material-arrow-right: [Parallelized Resource Methods](#parallelized-resource-methods)

</div>

## Parallelizing Work

```csharp
readonly record struct TerrainSettings(float Frequency, float Amplitude); // (1)!

static void GenerateHeights(Span<float> band, XYPair<int> bandOffset, XYPair<int> bandDimensions, TerrainSettings settings) { // (2)!
	for (var i = 0; i < band.Length; ++i) {
		var x = i % bandDimensions.X;
		var y = bandOffset.Y + i / bandDimensions.X; // (3)!
		band[i] = MathF.Sin(x * settings.Frequency) * MathF.Cos(y * settings.Frequency) * settings.Amplitude;
	}
}

var dimensions = new XYPair<int>(2048, 2048);
using var heightsLease = factory.ResourceAllocator.BorrowSpan<float>(dimensions.Area); // (4)!

ParallelizationProvider.ExecuteRowsOverSpan( // (5)!
	factory.ResourceAllocator.ParallelizingFacade,
	heightsLease.Span,
	dimensions,
	&GenerateHeights,
	new TerrainSettings(0.01f, 3f)
);

// Every height has been written by this point
```

1.	Anything the callback needs is passed in as a *context* value. This can be any type. Structs are copied to every block; class instances are shared between them. Any reference/shared data must be threadsafe.

2.	The block callback must be a `static` method. It's passed as a function pointer (`&GenerateHeights`), so the calling code must be in an `unsafe` context. Because nothing is captured in a closure, calling `ExecuteRowsOverSpan()` allocates nothing.

3.	Two-dimensional data is split in to *bands* of whole rows. `bandOffset.Y` is the row the band starts on; `bandOffset.X` is always `0`, and `bandDimensions.X` is always the full row width.

4.	This borrows a span without allocating on the managed heap, thus preventing GC stutter. Explained further in [Avoiding GC Stutter](avoiding_gc_stutter.md).

5.	Splits the heightmap in to bands, which are filled at the same time on TinyFFR's worker threads. The calling thread fills bands too, and the method only returns once every band is done.

`ParallelizationProvider` (in the `Egodystonic.TinyFFR.Threading` namespace) splits work in to *blocks* and runs them at the same time on TinyFFR's worker thread pool. This is the same pool used for [asynchronous loading](asynchronous_loading.md), with one worker thread per CPU core by default.

Every method takes an `IJobExecutionFacade` as its first argument, which determines where the work runs:

* `factory.ResourceAllocator.ParallelizingFacade` spreads the blocks across TinyFFR's worker threads.
* `IJobExecutionFacade.Synchronous` runs everything on the calling thread. This is useful for debugging, or for comparing timings.

Every method blocks the calling thread until all blocks are finished; there's no asynchronous version. The calling thread processes blocks as well as waiting for them.

## The Execute Methods

<span class="def-icon">:material-code-block-parentheses:</span> `Execute(facade, itemCount, blockCallback, context)`

:   Splits the range `0` to `itemCount` in to contiguous blocks. `blockCallback` is invoked with `(start, count, context)`, where `start` is the index of the block's first item and `count` is the number of items in the block. What an "item" is is up to you (e.g. elements of an array referred to by the context).

<span class="def-icon">:material-code-block-parentheses:</span> `ExecuteRows(facade, dimensions, blockCallback, context)`

:   Splits a two-dimensional area (`dimensions.Y` rows of `dimensions.X` items) in to bands of whole rows. `blockCallback` is invoked with `(bandOffset, bandDimensions, context)`.

<span class="def-icon">:material-code-block-parentheses:</span> `ExecuteOverSpan(facade, span, blockCallback, context)`

:   Splits `span` in to contiguous blocks. `blockCallback` is invoked with `(block, start, context)`, where `block` is a slice of `span` and `start` is the index of its first element within `span`. `ExecuteOverReadOnlySpan()` does the same for a `ReadOnlySpan<T>`.

<span class="def-icon">:material-code-block-parentheses:</span> `ExecuteRowsOverSpan(facade, span, dimensions, blockCallback, context)`

:   Treats `span` as `dimensions.Y` rows of `dimensions.X` elements (laid out row by row), and splits it in to bands of whole rows. `blockCallback` is invoked with `(band, bandOffset, bandDimensions, context)`, where `band` is a slice of `span`; the element at `(x, y)` within the band is `band[y * bandDimensions.X + x]`. `ExecuteRowsOverReadOnlySpan()` does the same for a `ReadOnlySpan<T>`.

	Both throw an `ArgumentException` if `span` contains fewer than `dimensions.X * dimensions.Y` elements.

Every method also has two optional arguments for tuning (see [Tuning](#tuning)): a minimum block size (`minimumItemsPerBlock`, `minimumElementsPerBlock`, or `minimumRowsPerBlock`), and `maxWorkerThreads`.

## Thread Safety Rules

* **Your context object must be threadsafe.** The given context object will be copied to every worker thread if it's a struct; or the same reference will be passed to every worker if it's a class instance. In either case you should expect and design for concurrent access (read/write) to your context data. If possible, use an immutable object (but this is not always realistic, especially if you're using the context to collect data).
* **Callbacks may never call TinyFFR's API.** They run on worker threads, and (except where explicitly stated) TinyFFR's API may only be used from the primary thread (i.e. thread that created the factory). Copy anything you need from TinyFFR (such as a mutable grid's normalized vertex coordinates) in to your context *before* calling the method.
* **Each block only writes to its own part of the data.** Blocks never overlap, so writing only within your block (or only to the elements its indices refer to) needs no locking. Reading data that no block is writing to is always fine. The `ParallelizationProvider` ensures all writes are correctly fenced and every mutation will be visible to your calling thread once your parallel execution function returns.
* **Combine results afterwards.** To compute a single result (such as a total or a maximum), have each block write its partial result to its own slot in an array, then combine the slots on the calling thread once the method returns:

	```csharp
	sealed class MaximumSearch {
		public required float[] Values { get; init; }
		public required float[] RowMaximums { get; init; }
	}

	static void FindRowMaximums(XYPair<int> bandOffset, XYPair<int> bandDimensions, MaximumSearch search) {
		for (var y = bandOffset.Y; y < bandOffset.Y + bandDimensions.Y; ++y) {
			var rowMaximum = Single.NegativeInfinity;
			foreach (var value in search.Values.AsSpan(y * bandDimensions.X, bandDimensions.X)) rowMaximum = MathF.Max(rowMaximum, value);
			search.RowMaximums[y] = rowMaximum;
		}
	}

	ParallelizationProvider.ExecuteRows(factory.ResourceAllocator.ParallelizingFacade, dimensions, &FindRowMaximums, search);
	var maximum = Single.NegativeInfinity;
	foreach (var rowMaximum in search.RowMaximums) maximum = MathF.Max(maximum, rowMaximum);
	```

* **Exceptions are passed back to the caller.** If a callback throws, blocks that haven't started yet are skipped, and the first exception is rethrown on the calling thread once every running block has finished.
* **Callbacks may not run in parallel at all.** Small amounts of work (fewer than two blocks' worth), facades without worker threads, and factories created with `ThreadingConfig.WorkerThreadCount` set to `0` (see [ThreadingConfig](asynchronous_loading.md#threadingconfig)) run everything on the calling thread as a single block. Code must give the same result either way.

## Tuning

<span class="def-icon">:material-code-json:</span> `minimumItemsPerBlock` / `minimumElementsPerBlock` / `minimumRowsPerBlock`

:   The fewest items (or rows) any one block may contain. Defaults to `1`. Handing a block to another thread has a small fixed cost, so raise this when each item is quick to process (so that every block has enough work to be worth handing off). For the row-based methods, `ParallelizationProvider.GetMinimumRowsPerBand(rowWidth, minimumElementsPerBlock)` works out how many rows make up a given number of elements.

<span class="def-icon">:material-code-json:</span> `maxWorkerThreads`

:   The most worker threads that may help with the call (in addition to the calling thread). Defaults to no limit. `0` runs everything on the calling thread.

???+ tip "Performance"
	Cache/memory contention and false sharing can force cores to waste a lot of time co-ordinating a coherent view of the system memory. In the simplest cases a single-threaded approach can often be better.

	Parallelization helps most when each item takes real computation (evaluating a function, calculating normals, converting colours), and helps least (or even slows things down) when each item is just copied. Copying is often limited by the speed of memory rather than of the CPU, and a single thread can often already use most of it; adding threads then only adds overhead. Memory-bound work is often fastest with a modest `maxWorkerThreads` (such as `8`) rather than every core.	

## Parallelized Resource Methods

Several TinyFFR resources have methods that parallelize writing their data for you, using the same rules for callbacks as above:

| Resource | Methods | More info |
|---|---|---|
| Writable textures | `OverwriteTexelsParallelized()` | [Overwriting Texels in Parallel](writable_textures.md#overwriting-texels-in-parallel) |
| Mutable grids | `WriteVerticesParallelized()`, `ReadVerticesParallelized()` | [Writing in Parallel](dynamic_meshes_and_mutable_grids.md#writing-in-parallel) |
| Model instances (per-instance vertex mutation) | `WriteVerticesParallelized()`, `ReadVerticesParallelized()` | [Writing in Parallel](dynamic_meshes_and_mutable_grids.md#writing-in-parallel) |
| Dynamic vertex buffers | `WriteVerticesParallelized()`, `WriteTrianglesParallelized()`, and their `Read...` equivalents | [Writing in Parallel](dynamic_meshes_and_mutable_grids.md#writing-in-parallel) |

These methods are always called from the primary thread, like the rest of TinyFFR's API; only their callbacks run on worker threads. `TextureCompressor.Compress()` also accepts `ParallelizingFacade` to compress textures in parallel (see [The TextureCompressor](texture_compression.md#the-texturecompressor)).
