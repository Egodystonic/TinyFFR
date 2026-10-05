---
title: Backdrop Textures
description: Information on how to load backdrop textures (sky images and the ambient lighting derived from them) in TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * A `BackdropTexture` supplies both the sky drawn behind a scene and the ambient light that sky casts on everything in that scene. :material-arrow-right: [Backdrop Textures](#backdrop-textures)
    * Backdrops can be created from high-dynamic-range (`.hdr`/`.exr`) images. :material-arrow-right: [From HDR or EXR Files](#from-hdr-or-exr-files)
    * Preprocess or bake your backdrops once ahead of time so they load quickly. :material-arrow-right: [Preprocessing](#preprocessing) / [Baking](#baking)

</div>

## Backdrop Textures

```csharp
using var sunset = factory.AssetLoader.LoadBackdropTexture(@"Assets/sunset.hdr"); // (1)!
scene.SetBackdrop(sunset, backdropIntensity: 0.8f, rotation: 130f % Direction.Up); // (2)!
```

1.	Loads a backdrop texture from a high-dynamic-range image file. Note that this can take a long time; see [Loading Backdrop Textures](#loading-backdrop-textures) below for faster alternatives.

2.	Sets the scene's backdrop to the loaded texture, slightly dimmed and turned 130° around the up axis. An intensity of `1f` shows the backdrop as authored; `sunset.MeasuredLux` gives the illuminance that represents, and a `SceneBackdropBrightnessPreset` can be passed instead to light the scene to a real-world brightness (see [Exposure & Brightness](exposure_and_brightness.md#backdrops)).

A `BackdropTexture` describes the surroundings of a scene. It does two jobs at once:

* It provides the *skybox*: The image drawn behind everything in the scene, filling every part of the view that no object covers.
* It provides *image-based lighting*: Ambient light derived from the image, which lights every object in the scene from every direction (so, for example, a scene set at sunset picks up warm light from the horizon without you having to place any lights by hand).

The second part is what makes objects look as though they genuinely belong in their surroundings, and a scene with a backdrop tends to look markedly more natural than one lit only by its own lights. Because of these two roles, every backdrop is ultimately made of two parts; a skybox texture and an image-based lighting texture.

??? info "Other Ways to Set a Scene's Backdrop"
	As well as `scene.SetBackdrop()`, the following methods are available on a `Scene`:

	* `SetBackdropWithoutIndirectLighting()` draws the backdrop behind the scene but doesn't use it to light anything. This is useful when you want to light the scene entirely with your own lights, or when the backdrop is purely decorative.
	* `SetBackdrop(ColorVect color)` fills the background with a flat colour, and also lights the scene in that colour from every direction (a mid-grey gives soft, even lighting similar to an overcast sky). `SetBackdropWithoutIndirectLighting(ColorVect color)` fills the background without lighting anything.
	* `RemoveBackdrop()` removes the backdrop (and the light it was contributing) entirely.
	
	See [Scenes](scenes.md) for more information.

Like every other resource, a `BackdropTexture` should be disposed when you're done with it; but every scene using it must have stopped doing so first.

## Loading Backdrop Textures

There are three ways to load a backdrop texture:

| Source                                       | Load Speed                            | Notes                                                                 |
| :------------------------------------------- | :------------------------------------ | :-------------------------------------------------------------------- |
| An `.hdr` or `.exr` image                    | Very slow (can take minutes)          | Converts the image every time; only available on a local factory     |
| A preprocessed pair of `.ktx` files          | Fast                                  | Portable files that can be produced once from an `.hdr`/`.exr` image |
| A baked asset file                           | Fastest                               | Produced by the [asset bakery](asset_bakery.md)                      |

### From HDR or EXR Files

```csharp
using var sunset = factory.AssetLoader.LoadBackdropTexture(
	@"Assets/sunset.hdr", 
	backdropTextureResolution: Quality.High // (1)!
);
```

1.	Specifies the resolution of the resultant backdrop. Higher resolutions look sharper but take proportionally longer to create (and use more video memory).

`assetLoader.LoadBackdropTexture()` creates a backdrop texture directly from a high-dynamic-range image in `.hdr` or `.exr` format. To do so, TinyFFR converts the image in to a skybox and image-based lighting texture using a tool (`cmgen`, from [Filament](https://github.com/google/filament)) that's bundled with TinyFFR, run as a separate process on the local machine.

The `backdropTextureResolution` argument sets the size of each face of the backdrop's cubemap:

| Quality              | Resolution (per cubemap face) |
| :------------------- | :---------------------------- |
| `Quality.VeryLow`    | 64x64                         |
| `Quality.Low`        | 256x256                       |
| `Quality.Standard`   | 512x512 (default)             |
| `Quality.High`       | 2048x2048                     |
| `Quality.VeryHigh`   | 4096x4096                     |

???+ warning "Converting HDR Images is Slow"
	Converting a high-dynamic-range image in to a backdrop is **very** slow; it can take minutes for a large image at a high resolution, and it allocates heavily while it runs. It's not something you should do while your application is meant to be responsive.
	
	Unless you're only loading backdrops while developing your application, you should preprocess or bake your backdrops ahead of time instead (see below). If you must load from an `.hdr`/`.exr` file at runtime, use the asynchronous version of the method (see [Asynchronous Loading](#asynchronous-loading)).

If a conversion takes longer than 15 minutes it is aborted with an exception. You can change this time limit via the `MaxHdrProcessingTime` property of the `LocalAssetLoaderConfig` supplied when creating your factory:

```csharp
using var factory = new LocalTinyFfrFactory(
	assetLoaderConfig: new LocalAssetLoaderConfig { MaxHdrProcessingTime = TimeSpan.FromMinutes(30d) }
);
```

### Preprocessing

```csharp
factory.AssetLoader.PreprocessHdrOrExrTextureToBackdropTextureDirectory( // (1)!
	@"Assets/sunset.hdr", 
	@"Assets/Backdrops/Sunset", 
	backdropTextureResolution: Quality.High
);

using var sunset = factory.AssetLoader.LoadBackdropTextureFromPreprocessedDirectory(@"Assets/Backdrops/Sunset"); // (2)!
```

1.	Converts the `.hdr` image to a pair of `.ktx` files and writes them in to the `Assets/Backdrops/Sunset` directory (creating it if it doesn't exist). This is the slow part, but it only needs to be done once (e.g. while developing your application).

2.	Loads the backdrop texture from the preprocessed files. This is much faster than loading from the `.hdr` file, and can be done every time your application runs.

Rather than converting a high-dynamic-range image every time it's loaded, you can convert it once with `PreprocessHdrOrExrTextureToBackdropTextureDirectory()` and ship the converted files with your application.

The conversion writes two files to the destination directory: A skybox texture (named `*_skybox.ktx`) and an image-based lighting texture (named `*_ibl.ktx`). `LoadBackdropTextureFromPreprocessedDirectory()` then loads a backdrop from a directory containing one of each.

If you'd prefer to specify the two files individually (e.g. if you've renamed them, or you obtained them from somewhere else), use `LoadPreprocessedBackdropTexture()` instead:

```csharp
using var sunset = factory.AssetLoader.LoadPreprocessedBackdropTexture(
	@"Assets/Backdrops/sunset_skybox.ktx", 
	@"Assets/Backdrops/sunset_ibl.ktx"
);
```

The `.ktx` files are in the standard format produced by Filament's `cmgen` tool, so they're portable: You can keep them alongside your other source assets, share them between projects, or produce them yourself with `cmgen`.

### Baking

```csharp
factory.AssetBakery.Enabled = true; // (1)!
using (var sunset = factory.AssetLoader.LoadBackdropTextureFromPreprocessedDirectory(@"Assets/Backdrops/Sunset")) {
	factory.AssetBakery.Bake(sunset, @"Assets/sunset.tinyffr"); // (2)!
}
factory.AssetBakery.Enabled = false;

using var bakedSunset = factory.AssetLoader.LoadBakedBackdropTexture(@"Assets/sunset.tinyffr"); // (3)!
```

1.	The asset bakery must be enabled *before* the resources you want to bake are loaded.

2.	The backdrop texture can be loaded via any of the methods above, and then baked in to a single asset file.

3.	Later (e.g. in your released application), the baked backdrop texture is loaded. This is the fastest way to load a backdrop.

The fastest way to load a backdrop texture is to bake it with the [asset bakery](asset_bakery.md) and then load it with `LoadBakedBackdropTexture()`. The only reasons to prefer the preprocessed `.ktx` files over a baked asset are if that's the format you were already given, or if you want to keep your converted backdrops in a portable format.

### Asynchronous Loading

```csharp
using var sunset = await factory.AssetLoader.LoadBackdropTextureAsync(@"Assets/sunset.hdr", Quality.High); // (1)!
scene.SetBackdrop(sunset);
```

1.	Converts and loads the backdrop on a background thread. Once it's ready, execution resumes on the primary thread, so the backdrop can be used immediately.

Every method on this page has an asynchronous counterpart (e.g. `LoadBackdropTextureAsync()`, `LoadBakedBackdropTextureAsync()`, `PreprocessHdrOrExrTextureToBackdropTextureDirectoryAsync()`). These are especially worthwhile when converting `.hdr`/`.exr` images, as they let your application keep running (and rendering) while the slow conversion takes place. Asynchronous loading is explained in more detail on the [Async asset loading](asynchronous_loading.md) page.
