---
title: Controlling RAM & VRAM Usage
description: What uses RAM and VRAM in a TinyFFR application, when GPU memory is allocated and freed, the factory options that control RAM usage, and what makes memory usage grow.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Textures, meshes, and renderers use graphics memory; TinyFFR itself uses RAM (which can be controlled). :material-arrow-right: [Where Memory Goes](#where-memory-goes), [Controlling RAM Usage](#controlling-ram-usage)
    * Compressing textures cuts their VRAM usage by around 3-4x. :material-arrow-right: [Texture Compression](#texture-compression)
    * GPU memory is allocated when a resource is created, and freed a few frames after it's disposed. :material-arrow-right: [When VRAM Is Used & Freed](#when-vram-is-used-freed)

</div>

## Where Memory Goes

A TinyFFR application uses two kinds of memory; ordinary RAM and the GPU's video memory (VRAM)(1).
{ .annotate }

1.	On some architectures these memory types are shared between both GPU and GPU (i.e. UMA), TinyFFR does not currently take advantage of this and treats the system as having discrete memory types.

#### VRAM Consumers:

* Textures (including those in materials, fonts, and backdrops)
* Meshes / Models
* Renderers (their internal render targets, shadow maps, and so on)
* `RenderOutputBuffer`s and rate-limited [compositor](composited_render_framerate_limiting.md) layers
* **The number of meshes matters more than their size.** Every mesh (including each sub-mesh in a model file) has a fixed overhead on the GPU that can far exceed the size of its actual vertex data. A model made of thousands of tiny pieces can use more VRAM than one with a few large, detailed meshes.
* **Renderers use VRAM in proportion to their resolution and quality.** In testing, one renderer drawing to a 2560x1440 buffer used around 4.5x as much VRAM at `VeryHigh` quality as at `VeryLow`. See [Render Quality](render_quality.md).

Some things cost (almost) nothing in VRAM:

* **Model instances.** Placing a model in a scene a thousand times uses the same VRAM as placing it once; only the model's mesh, material, and textures take up space.
* **Scenes, cameras, and lights** (although shadow-casting lights add to the renderer's shadow map memory).

#### RAM Consumers:

* TinyFFR's rendering engine itself (see [Controlling RAM Usage](#controlling-ram-usage))
* Data being loaded, processed, or uploaded to the GPU
* Memory pools that TinyFFR reuses to avoid creating garbage
* The asset bakery, when enabled

## Texture Compression

Textures are often the largest user of VRAM. [Compressing them](texture_compression.md) stores them in formats the GPU can read directly while using considerably less memory:

| Asset | VRAM with compressed textures (vs uncompressed) |
| :-- | :-- |
| A detailed glTF model (5 textures) | ~26% |
| A large glTF scene (33 textures) | ~30% |

Compression costs some image quality (usually unnoticeable at the higher quality levels), and compressing is slow. [Baking](pre-baking_assets.md) your assets lets you pay that cost once, ahead of time, rather than every time they're loaded.

## When VRAM Is Used & Freed

Dispose of resources you no longer need.

VRAM is allocated as soon as a resource is created or loaded. Loading a texture or mesh uploads it to the GPU straight away, whether or not anything is using it yet.

However, VRAM is freed a few frames after a resource is disposed. Disposing a resource doesn't free its GPU memory immediately, as the GPU may still be using it for frames that are in flight. Instead, it's freed once the last frame that was invoked with `Render()` before disposal happened has completed. Calling `WaitForGpu()` on your `Renderer`/`RendererCompositor` lets this clean-up happen instantly (and is recommended after disposal of large groups of resources).

## Controlling RAM Usage

The `LocalTinyFfrFactoryConfig` passed to the factory controls the largest amounts of RAM that TinyFFR reserves or keeps for reuse:

```csharp
using var factory = new LocalTinyFfrFactory(
	factoryConfig: new LocalTinyFfrFactoryConfig {
		MemoryUsageRubric = MemoryUsageRubric.UseLessMemory, // (1)!
		MaxRetainedUploadMemoryBytes = 128 * 1024 * 1024 // (2)!
	}
);
```

1.	Reduces the memory reserved by the rendering engine.

2.	Reduces how much memory TinyFFR keeps for reuse after uploading data to the GPU, from 512 MB to 128 MB.

<span class="def-icon">:material-card-bulleted-outline:</span> `MemoryUsageRubric`

:   How much RAM the rendering engine reserves for preparing frames:

	* `MemoryUsageRubric.Standard` (the default): Reserves plenty of space for complex frames.
	* `MemoryUsageRubric.UseLessMemory`: Reserves less. In testing, the factory uses around 30% less RAM than `Standard` when idle.
	* `MemoryUsageRubric.UseSignificantlyLessMemory`: Reserves as little as possible. In testing, the factory uses around 60% less RAM than `Standard` when idle.

	In testing, a detailed scene rendered identically and at the same speed with all three settings. However, the reserved space is used to record each frame's work for the GPU, and a frame that needs more than has been reserved (e.g. one with an extremely large number of objects or animated meshes) will crash the application. Only lower this setting if your scenes are relatively simple, and test with your most demanding scene.

<span class="def-icon">:material-card-bulleted-outline:</span> `MaxRetainedUploadMemoryBytes`

:   The most memory, in bytes, that TinyFFR keeps for reuse after uploading data to the GPU. Defaults to 512 MB.

	Everything uploaded to the GPU (meshes, textures, and frames being read back) is held in RAM until the GPU has received it, which normally happens by the next rendered frame. Afterwards, TinyFFR keeps some of that memory so that similar uploads can reuse it (as a performance optimisation), up to this limit. Memory that goes unused for `UploadMemoryBufferMaxRetentionTime` (4 seconds by default) is returned.

	Lowering this saves RAM, but setting it too low can slow down large uploads that repeat every frame (e.g. reading back every frame at 4K). Set it to `0` to never keep memory for reuse.

<span class="def-icon">:material-card-bulleted-outline:</span> `UploadMemoryBufferMaxRetentionTime`

:   How long memory kept for reuse after uploading data to the GPU (see `MaxRetainedUploadMemoryBytes`) can go unused before it's released. Defaults to 4 seconds. 

	If you have an allocation pattern where you're loading large bursts of assets once every N seconds, it might help to increase this to something just larger than N for performance.

<span class="def-icon">:material-card-bulleted-outline:</span> `UploadMemoryBufferFlushThresholdBytes`

:   Once this many bytes of data are waiting to be uploaded to the GPU, TinyFFR asks the GPU driver to start processing them rather than waiting for the next rendered frame. Defaults to 256 MB.

<span class="def-icon">:material-card-bulleted-outline:</span> `UploadMemoryBufferStallThresholdBytes`

:   Once this many bytes of data are waiting to be uploaded to the GPU, TinyFFR waits for the GPU to catch up before continuing. This is what limits how much RAM pending uploads can use. Defaults to 1 GB, and must not be lower than `UploadMemoryBufferFlushThresholdBytes`.

	Both thresholds are normally only reached when loading a lot of data without rendering any frames in between. Lowering them uses less RAM during large loads, but can make those loads take longer.

Some other settings also affect RAM usage:

* `AssetBakeryConfig.MaxResourcesInBakeryMemory` limits how many resources the [asset bakery](asset_bakery.md) holds data for while it's enabled.
* `LocalAssetLoaderConfig.MaxCachedTextMeshesPerFont` limits how many prepared pieces of text each [font](loading_fonts.md) keeps for reuse (64 by default).

## What Makes RAM Usage Grow

TinyFFR reuses memory wherever it can rather than returning it to the operating system, so that it doesn't have to allocate it again (to avoid [GC stutter](avoiding_gc_stutter.md)). This means your application's RAM usage tends to rise to its peak requirement and stay there.

The following can make that peak higher:

* **Loading large assets.** Loading needs temporary space for the file's data, the decoded images, the processed meshes, and so on. The space is kept for reuse afterwards.
* **Loading many assets at once.** Each [asynchronous load](asynchronous_loading.md) in progress needs its own temporary space, and data waiting to be uploaded to the GPU uses memory until the GPU has received it (normally by the next rendered frame). If you load a lot without rendering, this can reach `UploadMemoryBufferStallThresholdBytes` (1 GB by default) before TinyFFR waits for the GPU to catch up. Limit how many large loads you run at once if RAM is tight.
* **Enabling the asset bakery.** While enabled, the bakery keeps a copy of every resource's data until it's baked or discarded. In testing, loading a large model with the bakery enabled used around 70% more RAM. Only enable the bakery while baking (see [Asset Bakery](asset_bakery.md#baking-rules)).
* **Large collections and buffers.** Memory borrowed from the factory's `ResourceAllocator` (see [Avoiding GC Stutter](avoiding_gc_stutter.md)) is pooled, so the largest amount you've borrowed at once stays reserved for reuse.

Disposing the factory releases all of TinyFFR's pooled memory.

## Releasing Pooled Memory

To release the memory TinyFFR is keeping for reuse without disposing the factory (e.g. after loading a large level), call `TrimPooledMemory()`:

```csharp
factory.ResourceAllocator.TrimPooledMemory();
```

This releases pooled arrays, collections, and upload buffers that aren't currently in use. Nothing in use is affected, and the pools grow again as they're needed, so calling it often costs performance over the following frames. It also releases memory back to the GC so you may see GC stuttering.

`TrimPooledMemory()` must be called from the main thread, and does nothing while any [asynchronous operation](asynchronous_loading.md) is still in progress.
