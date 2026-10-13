---
title: Asset Bakery
description: Information on how to bake assets in to TinyFFR's fast-loading format, and how to load them again.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * The asset bakery saves loaded resources in a format that TinyFFR can load again much faster than the original asset files. :material-arrow-right: [The Asset Bakery](#the-asset-bakery)
    * Enable the bakery, load (or create) your resources, then bake them. :material-arrow-right: [Baking Rules](#baking-rules)
    * Baked assets are loaded with the `LoadBaked[...]()` methods on the asset loader. :material-arrow-right: [Loading Baked Assets](#loading-baked-assets)

</div>

## The Asset Bakery

```csharp
factory.AssetBakery.Enabled = true; // (1)!

using (var bricks = factory.AssetLoader.LoadColorMap(@"Assets/bricks.png", compressionQuality: Quality.VeryHigh)) // (2)!
using (var car = factory.AssetLoader.LoadBundledAsset(@"Assets/Models/ToyCar.glb")) {
	factory.AssetBakery.Bake(bricks, @"Baked/bricks.tffr"); // (3)!
	factory.AssetBakery.Bake(car, @"Baked/car.tffr");
}

factory.AssetBakery.Enabled = false; // (4)!
```

1.	Enables the asset bakery. It must be enabled *before* the resources you want to bake are loaded or created.

2.	Loads the resources to be baked. Here the texture is also compressed at the highest quality level; as baking is done ahead of time, the slow compression only has to be done once.

3.	Bakes each resource to a file. Baking a bundled asset writes every mesh, material, texture, model, and animation in it in to a single file.

4.	Disables the asset bakery again, releasing the memory it was using.

Later (e.g. in your released application), the baked files are loaded again:

```csharp
using var bricks = factory.AssetLoader.LoadBakedTexture(@"Baked/bricks.tffr"); // (1)!
using var car = factory.AssetLoader.LoadBakedBundledAsset(@"Baked/car.tffr"); // (2)!
```

1.	Loads the baked texture. The texture data was already decoded, processed, and compressed when it was baked, so it's simply uploaded to the GPU.

2.	Loads the baked bundled asset, returning a `ModelBundle` exactly like the one that was baked.

Loading an ordinary asset file involves a lot of work: images must be decoded and possibly [compressed](texture_compression.md), model files must be parsed and their meshes processed, fonts must have their characters drawn in to an atlas, [backdrop textures](backdrop_textures.md) must be converted from HDR images, and so on.

The *asset bakery* performs all of that work once and writes out the result in a bespoke format. Loading a *baked* asset then skips almost all of the work, making it considerably faster than loading the original file. Baking is intended to be done ahead of time (e.g. as part of packaging your application, or on its first run), rather than while your application is running normally.

### What Can Be Baked

| Resource                     | Loaded Back With                                               | Includes                                                         |
| :--------------------------- | :------------------------------------------------------------- | :--------------------------------------------------------------- |
| `Texture`                    | `LoadBakedTexture()` (returns a `Texture`)                     | The texture's (processed and possibly compressed) data.            |
| `Mesh`                       | `LoadBakedMesh()` (returns a `Mesh`)                           | The mesh, including any skeleton and animations.                   |
| `Material`                   | `LoadBakedMaterial()` (returns a `ResourceGroup`)              | The material and every texture it uses.                            |
| `Model`                      | `LoadBakedModel()` (returns a `ResourceGroup`)                 | The model and its mesh, material, and textures.                    |
| `ResourceGroup`              | `LoadBakedResourceGroup()` (returns a `ResourceGroup`)         | Every resource in the group (and everything they use) (see note below).             |
| `ModelBundle`                | `LoadBakedBundledAsset()` (returns a `ModelBundle`)            | Every texture, material, mesh, model, and animation in the bundle. |
| `Font`                       | `LoadBakedFont()` (returns a `Font`)                           | The font, including its prepared character atlas.                  |
| `BackdropTexture`            | `LoadBakedBackdropTexture()` (returns a `BackdropTexture`)     | The backdrop's (already-converted) skybox and lighting data.       |

Every resource is baked with the `factory.AssetBakery.Bake()` overload for its type. Resources can be baked whether they were loaded from files or created programmatically (e.g. with the [texture builder](creating_textures.md)).

Note: ResourceGroups, when baked, will only bake instances of other resource types supported in the table above (i.e. textures, meshes, etc will be added, but a "Camera" or a "Light" will not).

## Baking Rules

???+ success "Enable the Bakery First"
	The bakery can only bake resources that were loaded or created *while it was enabled*. Attempting to bake a resource that was created while the bakery was disabled throws an `AssetBakeException`.

	Note that the bakery is disabled by default; and that enabling it costs significant memory and slightly slows down loading and creating resources (even ones you don't bake), so it's best to only enable it while baking.
	
	Disable it again once you are finished baking.

The following rules apply when baking:

* The bakery keeps the data it needs to bake each resource in memory, up to a limit of `MaxResourcesInBakeryMemory` resources (500 by default; see [AssetBakeryConfig](#assetbakeryconfig) below). Once that limit is reached, the data for the least-recently-used resources is discarded, and attempting to bake them (or anything that uses them) throws an `AssetBakeException`. If you're baking very large assets (with hundreds of meshes, materials, or textures), you may need to increase this limit.
* Disabling the bakery (setting `Enabled` to `false`) discards all the data it's holding, so bake everything you need before disabling it. You can also discard the data without disabling the bakery with `factory.AssetBakery.ClearBakeryMemory()`, which is worthwhile between unrelated batches of baking.
* Because the bakery retains memory of every resource you load/create (until baked) its RAM cost can grow fast. Disable/clear memory between usages, or immediately bake every resource after it's loaded to reduce memory pressure.
* Baking writes to the given file path, overwriting any existing file. The `.tffr` extension shown in the example above is just a suggestion, you can use any file extension you choose.

???+ tip "Shared Dependencies"
	Any dependency a resource has is baked in to the target file alongside it. If you bake a `ModelBundle`, every `Texture`, `Material`, `Mesh`, `Model` etc it uses are written to the output file.
	
	When you later load that file, a copy of each sub-resource is loaded on to the GPU. This means that if you have shared resources, you may be needlessly wasting VRAM (and disc space).
	
	You can fix this by either baking each sub-resource manually (i.e. each `Texture`, `Mesh`, etc) and then reconstructing them as needed (i.e. `LoadBakedTexture()`, `LoadBakedMesh()`); or by grouping shared assets together in a `ResourceGroup`. For example, if you have a set of `Texture`s used by many `Material`s, it can be prudent to bake those `Material`s as part of a `ResourceGroup` so the dependent `Texture`s are only baked and re-loaded back in once.

## Loading Baked Assets

The bakery doesn't need to be enabled to load baked assets.

Baked assets are loaded via the `LoadBaked[...]()` methods on the asset loader. Each also has an asynchronous counterpart (e.g. `LoadBakedTextureAsync()`; see [Asynchronous Loading](asynchronous_loading.md)) for more information.

Methods that load more than one resource (`LoadBakedMaterial()`, `LoadBakedModel()`, and `LoadBakedResourceGroup()`) return a `ResourceGroup` containing every loaded resource; disposing the group disposes all of them. Use the group's enumeration properties to access the target resource (e.g. `var materialGroup = assetLoader.LoadBakedMaterial(@"Assets/mymat.tffr"); var mat = materialGroup.Materials[0];`).

`LoadBakedBundledAsset()` returns a `ModelBundle`. Disposing the bundle disposes every resource in it.

## Versioning & Compatibility

Every baked file records the version of the TinyFFR bakery schemata that baked it. Files baked by a version of TinyFFR with a large enough difference in schemata can not be loaded, and attempting to do so throws an `AssetBakeException`. Similarly, an `AssetBakeException` is thrown if a file isn't a baked asset, has been corrupted, or is loaded with the wrong method for the type of resource it contains.

By default, files baked by slightly older or newer versions of TinyFFR with the same major version are still loaded wherever possible. Any data the current version doesn't understand is simply ignored. If you'd rather reject any file that wasn't baked by exactly the same version of TinyFFR, set `RequireStrictAssetBakeSchemaMatch` to `true` (see below).

Either way, it's a good idea to re-bake your assets whenever you upgrade TinyFFR.

## AssetBakeryConfig

```csharp
using var factory = new LocalTinyFfrFactory(
	assetBakeryConfig: new AssetBakeryConfig {
		Enabled = true,
		MaxResourcesInBakeryMemory = 2000
	}
);
```

An `AssetBakeryConfig` can be supplied when creating the factory, with the following properties:

<span class="def-icon">:material-card-bulleted-outline:</span> `Enabled`

:   Whether the bakery is enabled when the factory is created. Defaults to `false`. The bakery can also be enabled or disabled at any time afterwards via `factory.AssetBakery.Enabled`.

<span class="def-icon">:material-card-bulleted-outline:</span> `MaxResourcesInBakeryMemory`

:   How many resources the bakery keeps the data for at once (see [Baking Rules](#baking-rules) above). Defaults to `500`. Must be at least `1`.

<span class="def-icon">:material-card-bulleted-outline:</span> `RequireStrictAssetBakeSchemaMatch`

:   Whether baked files must have been baked by exactly the same version of TinyFFR in order to be loaded (see [Versioning & Compatibility](#versioning-compatibility) above). Defaults to `false`. This affects *loading* baked assets, so it applies even when the bakery itself is disabled.
