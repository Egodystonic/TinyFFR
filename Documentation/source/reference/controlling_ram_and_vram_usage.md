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

The `LocalTinyFfrFactoryConfig` passed to the factory controls the largest fixed amounts of RAM that TinyFFR uses:

```csharp
using var factory = new LocalTinyFfrFactory(
	factoryConfig: new LocalTinyFfrFactoryConfig {
		MemoryUsageRubric = MemoryUsageRubric.UseLessMemory, // (1)!
		MaxCpuToGpuAssetTransferSizeBytes = 64 * 1024 * 1024 // (2)!
	}
);
```

1.	Reduces the memory reserved by the rendering engine.

2.	Reduces the largest asset that can be uploaded to the GPU (and the buffer reserved for doing so) from 100 MB to 64 MB.

<span class="def-icon">:material-card-bulleted-outline:</span> `MemoryUsageRubric`

:   How much RAM the rendering engine reserves for preparing frames:

	* `MemoryUsageRubric.Standard` (the default): Reserves plenty of space for complex frames.
	* `MemoryUsageRubric.UseLessMemory`: Reserves less. In testing, the factory uses around 30% less RAM than `Standard` when idle.
	* `MemoryUsageRubric.UseSignificantlyLessMemory`: Reserves as little as possible. In testing, the factory uses around 60% less RAM than `Standard` when idle.

	In testing, a detailed scene rendered identically and at the same speed with all three settings. However, the reserved space is used to record each frame's work for the GPU, and a frame that needs more than has been reserved (e.g. one with an extremely large number of objects or animated meshes) will crash the application. Only lower this setting if your scenes are relatively simple, and test with your most demanding scene.

<span class="def-icon">:material-card-bulleted-outline:</span> `MaxCpuToGpuAssetTransferSizeBytes`

:   The size of the largest single piece of data (e.g. one texture or mesh) that can be uploaded to the GPU, in bytes. Defaults to 100 MB; the maximum is 512 MB.

	TinyFFR reserves a buffer of this size for staging uploads when the factory is created, so lowering it saves RAM. Loading anything larger throws an `InvalidOperationException` explaining that the limit should be raised.

Some other settings also affect RAM usage:

* `AssetBakeryConfig.MaxResourcesInBakeryMemory` limits how many resources the [asset bakery](asset_bakery.md) holds data for while it's enabled.
* `LocalAssetLoaderConfig.MaxCachedTextMeshesPerFont` limits how many prepared pieces of text each [font](loading_fonts.md) keeps for reuse (64 by default).

## What Makes RAM Usage Grow

TinyFFR reuses memory wherever it can rather than returning it to the operating system, so that it doesn't have to allocate it again (to avoid [GC stutter](avoiding_gc_stutter.md)). This means your application's RAM usage tends to rise to its peak requirement and stay there.

The following can make that peak higher:

* **Loading large assets.** Loading needs temporary space for the file's data, the decoded images, the processed meshes, and so on. The space is kept for reuse afterwards.
* **Loading many assets at once.** Each [asynchronous load](asynchronous_loading.md) in progress needs its own temporary space, and the buffer used for uploading data to the GPU grows to hold every upload that's happening at once. In testing, loading the same model 8 times concurrently raised RAM usage around 5.5x as much as loading it once. Limit how many large loads you run at once if RAM is tight.
* **Enabling the asset bakery.** While enabled, the bakery keeps a copy of every resource's data until it's baked or discarded. In testing, loading a large model with the bakery enabled used around 70% more RAM. Only enable the bakery while baking (see [Asset Bakery](asset_bakery.md#baking-rules)).
* **Large collections and buffers.** Memory borrowed from the factory's `ResourceAllocator` (see [Avoiding GC Stutter](avoiding_gc_stutter.md)) is pooled, so the largest amount you've borrowed at once stays reserved for reuse.

Disposing the factory releases all of TinyFFR's pooled memory.
