---
title: Pre-Baking Assets
description: Why loading asset files is slow, how much faster baked assets load, and a workflow for shipping baked assets with your application.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Loading an ordinary asset file does a lot of processing every time it's loaded. :material-arrow-right: [Why Loading Is Slow](#why-loading-is-slow)
    * Baked assets skip that processing, and typically load tens to thousands of times faster. :material-arrow-right: [How Much Faster](#how-much-faster)
    * Bake your assets ahead of time and ship the baked files, optionally falling back to the originals. :material-arrow-right: [A Baking Workflow](#a-baking-workflow)

</div>

## Why Loading Is Slow

Loading an asset file isn't just a matter of reading it from disk. Every time a file is loaded, TinyFFR has to convert it in to something the GPU can use:

* **Textures** must be decoded from their file format (e.g. PNG or JPEG). If [compression](texture_compression.md) is requested, every mip level must also be generated and compressed, which is by far the slowest step.
* **Models and bundled assets** must be parsed, then every mesh is processed. That includes triangulation, normals and tangent calculation, de-duplication, cache optimization, and so on. Every texture embedded in the file must also be decoded (and possibly compressed) as above.
* **Fonts** must have every character rendered in to a texture atlas.
* **Backdrops** loaded from `.hdr`/`.exr` images must be converted in to a skybox and lighting data.

The [asset bakery](asset_bakery.md) does all of that work *once*, ahead of time, and saves the result in a format that's ready to be given straight to the GPU. Loading a baked asset skips almost all of the processing.

## How Much Faster

These figures compare loading each original file with loading its baked equivalent. "Cached" means the file had recently been read (so the operating system already held it in memory); "Uncached" means it was read from disk (an NVMe SSD) for the load:

| Asset | Baked load speedup (cached) | Baked load speedup (uncached) | Baked file size vs original |
| :-- | :-- | :-- | :-- |
| 2048x2048 photo texture (JPEG) | ~16x | ~12x | ~6x larger |
| The same texture, compressed at `Quality.VeryHigh` | ~12,000x | ~2,300x | ~3x larger |
| A detailed glTF model (5 textures) | ~17x | ~10x | ~16x larger |
| The same model, textures compressed at `Quality.VeryHigh` | ~3,100x | ~1,300x | ~6x larger |
| A small animated glTF model | ~18x | ~4x | ~27x larger |
| A large glTF scene (1.5 million triangles, 33 textures) | ~18x | ~8x | ~15x larger |
| The same scene, textures compressed at `Quality.Standard` | ~1,400x | ~490x | ~7x larger |
| A TrueType font | ~110x | ~44x | ~17x larger |
| A 4K `.hdr` backdrop | ~1,100x | ~420x | ~0.7x (smaller) |
| A preprocessed `.ktx` backdrop | No difference | - | ~2x larger |

A few things stand out:

* **Compression is where baking matters most.** Compressing textures can take seconds (or, for a large model, minutes) every time the asset is loaded. When baked, the compressed data is simply loaded as-is. Because the cost of compressing is only paid once, at bake time, you can use the highest compression quality.
* **Baked files are larger than the originals.** Formats like PNG, JPEG, and glTF store data compactly in ways the GPU can't use directly; baked files store it in a ready-to-use (and therefore larger) form. Reading a larger file from disk takes longer, which is why the uncached speedups are lower. They're still large, but on slower storage (e.g. hard drives) the gap will narrow further.
* **Compressing textures also shrinks baked files**, which means they load faster still from disk. Baking a model with compressed textures produced a file less than half the size of the uncompressed bake.
* **Already-preprocessed data gains little.** Preprocessed `.ktx` backdrops are already in a ready-to-use form, so baking them makes no difference to load times.

The same speedups apply to [asynchronous loading](asynchronous_loading.md): A baked model loaded asynchronously was around 17x faster than the original file loaded asynchronously. Baking and asynchronous loading complement each other: Baking reduces how much work loading takes, while asynchronous loading lets your application keep running while it happens.

??? info "What Baking Doesn't Change"
	Baking only affects how long *loading* takes. A baked asset uses exactly as much GPU memory once it's loaded as the original would, and it renders identically.

## A Baking Workflow

A common approach is to keep the original asset files in your project, bake them as a separate step (e.g. with a small console program, or a command-line switch in your application), and ship only the baked files.

```csharp
using var factory = new LocalTinyFfrFactory(
	assetBakeryConfig: new AssetBakeryConfig { Enabled = true } // (1)!
);
Directory.CreateDirectory("Baked");

using (var bricks = factory.AssetLoader.LoadColorMap(@"Assets/bricks.png", compressionQuality: Quality.VeryHigh)) { // (2)!
	factory.AssetBakery.Bake(bricks, @"Baked/bricks.tffr"); // (3)!
}

var modelConfig = new ModelCreationConfig {
	TextureConfig = new TextureCreationConfig {
		DataType = TextureDataType.ColorSrgb,
		CompressionQuality = Quality.VeryHigh
	}
};
using (var car = factory.AssetLoader.LoadBundledAsset(@"Assets/Models/ToyCar.glb", modelConfig)) {
	factory.AssetBakery.Bake(car, @"Baked/car.tffr");
}
```

1.	Enables the bakery as soon as the factory is created, so every resource loaded afterwards can be baked.

2.	Loads each asset with the settings you want in your released application. Compression is slow, but here it only happens once.

3.	Bakes each resource straight after loading it, so the bakery doesn't have to keep its data in memory for long (see [Baking Rules](asset_bakery.md#baking-rules)).

Your application then loads the baked files. If you'd like it to keep working when a baked file is missing (e.g. during development) or out of date, it can fall back to the original asset:

```csharp
static ModelBundle LoadCar(ILocalTinyFfrFactory factory) {
	if (File.Exists(@"Baked/car.tffr")) {
		try {
			return factory.AssetLoader.LoadBakedBundledAsset(@"Baked/car.tffr");
		}
		catch (AssetBakeException) { } // (1)!
	}
	return factory.AssetLoader.LoadBundledAsset(@"Assets/Models/ToyCar.glb"); // (2)!
}
```

1.	An `AssetBakeException` is thrown if the file can't be loaded as a baked asset, e.g. because it's corrupt or was baked by an incompatible version of TinyFFR (see [Versioning & Compatibility](asset_bakery.md#versioning-compatibility)).

2.	Falls back to loading (and processing) the original file.

??? tip "Re-Bake When Upgrading"
	Baked files record the version of TinyFFR that baked them, and files baked by a sufficiently different version can't be loaded. Re-bake your assets whenever you upgrade TinyFFR.

??? tip "Shared Resources"
	Every resource a baked asset depends on is included in its file. If several assets share the same textures, bake them together in one [resource group](resource_groups.md) to avoid storing (and loading) duplicate copies; see [Asset Bakery](asset_bakery.md#baking-rules).

## File Compression

The produced baked files are given in an uncompressed binary format (after all, the point of these files are to be as quick as possible to load; having to decompress them first would defeat that purpose). 

However, for transmission/storage/distribution as part of your application you may want to compress them with a generic compression algorithm (e.g. `.zip`, `.7z`, `.gz`, etc) and then do a one-time decompression to disc on first run or during an installation process.
