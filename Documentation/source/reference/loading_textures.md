---
title: Loading Textures
description: Information on how to load texture data/files in TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * You can load a texture for a specific purpose via one of the `Load[...]Map()` functions, e.g. `assetLoader.LoadColorMap(@"Assets/bricks.png")`. :material-arrow-right: [Texture Files](#texture-files)
    * If you load a texture via the lower-level `LoadTexture()` instead, you must tell TinyFFR what its data represents. :material-arrow-right: [Texture Data Types](#texture-data-types)
    * Compression, mipmaps, sampling, and processing of the texture data can all be customized. :material-arrow-right: [Customizing the Load Operation](#customizing-the-load-operation)

</div>

## Texture Files

```csharp
using var bricks = factory.AssetLoader.LoadColorMap(@"Assets/bricks.png"); // (1)!
using var normalPattern = factory.AssetLoader.LoadNormalMap(@"Assets/normalPattern1.bmp"); // (2)!
using var warningSymbol = factory.AssetLoader.LoadCanvasTexture(@"Assets/warning.tga"); // (3)!
using var noiseTex = factory.AssetLoader.LoadTexture(@"Assets/noise.jpg", TextureDataType.LinearData); // (4)!
```

1.	Loads a brick texture that can later be used as a color map (albedo map) for any material.

2.	Loads a texture as a normal pattern. Normal maps are explained on the next page.

3.	Loads a warning symbol that can be used in a [Canvas Scene](canvas_scenes.md).

4.	Loads a texture via the manual `LoadTexture()` API which requires at the least an indicated `TextureDataType` (explained below).

All materials (and canvas images) in TinyFFR are comprised of one or more texture files. The `AssetLoader` makes it easy to load texture files for all supported types.

??? abstract "Supported Formats"
	TinyFFR can load texture data from the following asset formats.

	- JPEG/JPG
	- PNG
	- GIF
	- BMP
	- TGA
	- PSD
	- HDR
	- PIC
	- PNM

	TinyFFR does not currently support some more esoteric formats or compression types for the image formats listed above.	
	
### Load[...]Map() Functions vs LoadTexture()

The easiest way to load a texture is to use one of the many `Load[...]Map()` functions (e.g. `LoadColorMap()`, `LoadNormalMap()`) or `LoadCanvasTexture()` for canvas textures. These methods set the `TextureDataType`, `TextureCreationConfig`, and `TextureReadConfig` (all explained below) correctly for the indicated usage. Each map type is described in detail on the [next page](texture_map_types.md).

The alternative is `assetLoader.LoadTexture(...)`. However, when loading a texture this way you must specify at least its `TextureDataType`; and some of the default load options may not be fully appropriate for your intended usage (i.e. the `TextureCreationConfig` and `TextureReadConfig`). Therefore, `LoadTexture(...)` can be considered a "raw" API for when you want maximum control on how an image asset is loaded in to the system.

??? info "Texture Combination (and `LoadCombinedTexture(...)`)"
	In some cases you may have two or more disparate texture files that you want to combine to create a single texture map (for example, you might have a separate occlusion, roughness, and metallic texture file that you want to load together to make one ORM map).
	
	The various `Load[...]Map()` functions accommodate for this where possible by having overloads that let you specify a file for each individual data component.
	
	If you want more control on how exactly the files are combined, `LoadCombinedTexture()` lets you specify a `TextureCombinationConfig`; further explained in [Creating Textures](creating_textures.md).

## Texture Data Types

Texture files (such as `.bmp`, `.jpeg`, `.png` etc) only store colour channel information; but to effectively *use* that information TinyFFR needs to know what the intended *use* of the texture is.

??? question "Why is the TextureDataType Important?"
	It can be tempting to think of a texture on the GPU as just "colour channel data". However, to effectively work with texture data we need to know its intended purpose for the following reasons:
	
	* **Compression:** When texture data is compressed it's important to know how its data should be interpreted so that we can compress it in a non-destructive way. The approach to compressing colour data differs from that of, say, vector data (such as for a normal map). For example, colour data is compressed in a way that minimises the *visible* error, whereas normal map data is compressed in to a two-channel format that preserves the precision of the vector's direction. Choosing the wrong approach can corrupt the data severely.
	* **Colour Space:** Most image files store colour with a non-linear encoding (sRGB) that matches human vision, and colour textures must be converted back to linear values before the GPU does any lighting math with them. Doing the same to data textures would be wrong: A roughness value of "half" is genuinely half, but a colour of "half brightness" is not stored as half. Getting this wrong makes surfaces look washed-out or too dark, and breaks any calculations made with the data.
	* **Mipmaps:** When smaller copies of a texture are generated for use at a distance (see [Mipmaps](#mipmaps) below), neighbouring texels are averaged together. Colour data must be averaged in linear space (not in its stored sRGB encoding) to avoid distant surfaces getting darker, and direction vectors must be re-normalised after averaging (because the average of two unit vectors is not itself a unit vector).
	* **Channel Usage:** Some data types only use some of the texture's channels (e.g. only red and green), and TinyFFR can store those textures more efficiently.
	
The `TextureDataType` enum has the following values:
	
<span class="def-icon">:material-card-bulleted-outline:</span> `ColorSrgb`

:   The texels are colour, stored with the usual non-linear (sRGB) encoding of image files. This is the correct type for almost any image that produces or alters colour data: Colour maps, emissive maps, and absorption-transmission maps all use this data type.

	Texels are converted from sRGB to linear space when sampled by the GPU, and mipmaps are averaged in linear space. When compressed, these textures use the BC7 format (or BC1/BC3 at the lowest compression quality).

<span class="def-icon">:material-card-bulleted-outline:</span> `LinearData`

:   The texels are plain numeric data rather than colour, and are used exactly as stored. ORM/ORMR maps and anisotropy maps use this data type. Canvas textures also use this data type.

	Mipmaps are generated by plain averaging. When compressed, these textures use the BC7 format (without any colour interpretation).
	
	??? question "Why do Canvas Textures use LinearData?"
		`LoadCanvasTexture()` loads textures as `LinearData` (even though they usually contain colour) because [canvas scenes](canvas_scenes.md) are drawn without the post-processing pipeline that a regular scene uses (which includes the conversion from linear space back to sRGB for display).
		
		If for some reason you decide to manually enable post-processing for 2D canvas scenes you should probably load canvas textures via `LoadTexture()` and customize the data type to `ColorSrgb`.

<span class="def-icon">:material-card-bulleted-outline:</span> `LinearDataUnitVector`

:   The texels are plain numeric data encoding unit-length direction vectors. Normal maps use this data type.

	Mipmaps are re-normalised rather than simply averaged. When compressed, these textures use the two-channel BC5 format: only the X (red) and Y (green) components are stored, and the Z (blue) component is reconstructed from them.

<span class="def-icon">:material-card-bulleted-outline:</span> `LinearDataTwoChannelMax`

:   The texels are plain numeric data using only the red and green channels. Clearcoat maps use this data type.

	Mipmaps are generated by plain averaging. When compressed, these textures use the two-channel BC5 format; any blue or alpha data is discarded.

## Customizing the Load Operation

You can pass a `TextureCreationConfig` and `TextureReadConfig` to `LoadTexture(...)` to customize how the texture data should be loaded. Most `Load[...]Map()` functions also accept an optional `compressionQuality` argument.

???+ tip "Start from a Preset"
	Rather than setting every property of a `TextureCreationConfig` manually, it's easiest to start from one of its static `For[...]()` factory methods and then alter the properties you're interested in via a `with` expression:
	
	```csharp
	var config = TextureCreationConfig.ForColorMap(name: "Bricks") with { 
		GenerateMipMaps = false 
	};
	```
	
	Each one (except `ForCanvasTexture()`) also has an overload that accepts a compression quality.

### Compression

```csharp
var bricks = factory.AssetLoader.LoadColorMap(
	@"Assets/bricks.png", 
	compressionQuality: Quality.Standard
);

var noise = factory.AssetLoader.LoadTexture(
	@"Assets/noise.jpg", 
	new TextureCreationConfig {
		DataType = TextureDataType.LinearData,
		CompressionQuality = Quality.Standard
	}
);
```

By default, textures are uploaded to the GPU uncompressed. Setting a compression quality (via `textureCreationConfig.CompressionQuality`, or the `compressionQuality` argument on the `Load[...]Map()` functions) instead compresses the texture as it is loaded. Compressed textures take up a fraction of the video memory and are faster for the GPU to sample, at the cost of some loss of image quality.

TinyFFR picks the most appropriate compression format automatically from the texture's `TextureDataType` and the requested quality. Higher quality levels preserve more detail but take longer to compress.

???+ tip "Compression is not Always Worthwhile"
	Compressing a texture can take a *very* long time, especially at the higher quality levels. It's expected that you'll generally use the higher levels only when preparing textures ahead of time for a packaged distributed final build of your application.
	
	If you're not expecting to exhaust the host VRAM it's usually not worth stalling your application for the long time it takes to compress a texture. For this reason, by default all texture/map load functions have compression *disabled* by default. 
	
	If you're loading lots of large texture files dynamically you may have no option but to compress on demand-- in these cases a lower quality level is recommended (as these complete much faster). [Async asset loading](asynchronous_loading.md) is almost certainly worthwhile in these cases too.
	
	You can pre-compress textures ahead-of-time via the [the TextureCompressor](texture_compression.md#the-texturecompressor) or by baking textures in the [asset bakery](asset_bakery.md).
	
There's more information on texture compression in [Texture Compression](texture_compression.md).

Compression can not be used for textures that allow dynamic writes (see [Writable Textures](#writable-textures) below).

### Mipmaps

```csharp
var uiTexture = factory.AssetLoader.LoadTexture(
	@"Assets/hud_overlay.png", 
	TextureCreationConfig.ForColorMap() with { GenerateMipMaps = false }
);
```

By default, TinyFFR generates *mipmaps* for every texture it loads; a "mipmap" is an industry-standard term for a chain of successively half-sized copies of the texture that the GPU uses when a surface is far from the camera. Mipmaps make distant surfaces look steadier (rather than shimmering as the camera moves) and are faster for the GPU to read, at the cost of about a third more video memory.

You can disable mipmap generation by setting `textureCreationConfig.GenerateMipMaps` to `false`. You should only do this when a texture must stay pin-sharp at every distance, or when video memory is genuinely scarce.

### RenderingConfig (Sampling)

```csharp
var pixelArt = factory.AssetLoader.LoadTexture(
	@"Assets/sprite.png", 
	TextureCreationConfig.ForColorMap() with {
		RenderingConfig = new TextureRenderingConfig {
			DisableTexelBlending = true,
			DisableTextureRepeat = true
		}
	}
);
```

`textureCreationConfig.RenderingConfig` controls how the GPU *samples* the texture once it's created (i.e. how its texels are turned in to the pixels that actually appear on an object's surface in a 3D scene). The defaults suit almost every texture:

<span class="def-icon">:material-card-bulleted-outline:</span> `DisableTextureRepeat`

:   By default, a texture repeats indefinitely in every direction when a surface's texture coordinates go outside the range 0 to 1 (which is how a small tile of brickwork can cover a large wall). Setting this to `true` instead stretches the texture's edge texels outwards to fill whatever lies beyond it.

	This can also help in cases where floating-point inaccuracy when sampling near a surface's edges "leaks" the wrapped texture data on to those edges.

<span class="def-icon">:material-card-bulleted-outline:</span> `DisableTexelBlending`

:   By default, a texture viewed from close up is smoothed so that individual texels are not visible as hard-edged squares. Set this to `true` for deliberately blocky artwork (e.g. pixel art), or where the texel values are data for which an average of two neighbouring values would be meaningless.

	Setting this to `true` also disables anisotropic filtering (see below).

<span class="def-icon">:material-card-bulleted-outline:</span> `AnisotropicFilteringQuality`

:   A texture on a surface receding away from the camera (e.g. a road stretching to the horizon) turns to a blur in the distance. Raising this quality level keeps such surfaces legible much further away, at some cost in performance. Defaults to `Quality.Standard`.

### Writable Textures

```csharp
var drawingSurface = factory.AssetLoader.LoadTexture(
	@"Assets/blank_canvas.png", 
	TextureCreationConfig.ForColorMap() with { AllowsDynamicWrites = true }
);

// Later...
drawingSurface.OverwriteTexels(newTexels);
```

By default, a texture's contents are fixed once it's been created. Setting `textureCreationConfig.AllowsDynamicWrites` to `true` enables the `texture.OverwriteTexels()` API, which lets you replace all (or a rectangular region) of the texture's texels at any time after it's been created.

Mipmaps and compression can not be maintained for data that changes, so setting `AllowsDynamicWrites` to `true` also disables `GenerateMipMaps` and sets `CompressionQuality` to `null` (so it's fine to use with a preset such as `ForColorMap()`, as above). However, explicitly enabling either of those *after* setting `AllowsDynamicWrites` to `true` will throw an exception.

For more information on using writable textures, see [Writable Textures](writable_textures.md).

### Processing

```csharp
var roughness = factory.AssetLoader.LoadTexture(
	@"Assets/glossiness.png", 
	TextureCreationConfig.ForOrmMap() with {
		ProcessingToApply = TextureProcessingConfig.Invert( // (1)!
			includeRedChannel: false, 
			includeGreenChannel: true, 
			includeBlueChannel: false, 
			includeAlphaChannel: false
		)
	}
);
```

1.	This file stores *glossiness* (where max value means smooth) in its green channel, but TinyFFR expects *roughness* (where max value means rough). Inverting the green channel converts one to the other.

Texture files are often authored under a different convention to the one TinyFFR expects (e.g. a normal map whose green channel is inverted, an image stored upside-down, or channels in a different order). `textureCreationConfig.ProcessingToApply` lets you fix these problems as the texture is loaded, rather than having to edit the file itself.

A `TextureProcessingConfig` supports the following alterations:

<span class="def-icon">:material-card-bulleted-outline:</span> `FlipX` / `FlipY`

:   Mirror the texture left-to-right or top-to-bottom respectively.

<span class="def-icon">:material-card-bulleted-outline:</span> `InvertXRedChannel` / `InvertYGreenChannel` / `InvertZBlueChannel` / `InvertWAlphaChannel`

:   Invert the given channel, so that its strongest values become its weakest and vice versa.

<span class="def-icon">:material-card-bulleted-outline:</span> `MultiplyAlpha`

:   Multiply each texel's colour channels by its alpha channel (i.e. "premultiply" the alpha). Materials that blend with the scene expect their colour maps in this form; without it, partially-transparent texels typically show a dark fringe around the edges of the visible parts of the image.

	Alpha premultiplication is explained in further detail on the next page.

<span class="def-icon">:material-card-bulleted-outline:</span> `XRedFinalOutputSource` / `YGreenFinalOutputSource` / `ZBlueFinalOutputSource` / `WAlphaFinalOutputSource`

:   Choose which source channel supplies each output channel (sometimes known as "swizzling"). For example, setting `XRedFinalOutputSource` to `ColorChannel.G` copies the source's green data in to the output's red channel. All channels are copied simultaneously, so channels can be swapped without one overwriting the other partway through.

<span class="def-icon">:material-card-bulleted-outline:</span> `PostProcessingFunction` / `PostProcessingArgument`

:   An arbitrary function to run over the texture's texels, for alterations the built-in options can not express. Create one via `TexelProcessingFunction.Create<TTexel>(&MyFunction)`, where `MyFunction` is a static method taking a `Span<TTexel>` (which it may modify in place) and an `object?` (which receives the `PostProcessingArgument`). The function is invoked on a worker thread as part of loading.

	The function is bound to the texel type it was created for. It can still be applied to textures of a different texel type, but the texels will be converted to and from the expected type (which is lossy and not free), so it's best to match the texel type where possible.

The alterations are always applied in the following order, regardless of how the config was created: Flips first, then channel inversions, then alpha premultiplication, then channel copies, and finally the `PostProcessingFunction` (if one is set).

For simple cases, the static helper methods `TextureProcessingConfig.Flip()`, `.Invert()`, `.Swizzle()`, and `.PremultiplyAlpha()` create a config that performs only that alteration. `TextureProcessingConfig.None` performs no processing at all (and is the default).

### TextureCreationConfig

For reference, the following is every property on the `TextureCreationConfig`:

<span class="def-icon">:material-card-bulleted-outline:</span> `DataType`

:   What the texture's texels represent; see [Texture Data Types](#texture-data-types). This property is required.

<span class="def-icon">:material-card-bulleted-outline:</span> `CompressionQuality`

:   How aggressively to compress the texture, or `null` to leave it uncompressed; see [Compression](#compression). Defaults to `null`.

<span class="def-icon">:material-card-bulleted-outline:</span> `GenerateMipMaps`

:   Whether to generate mipmaps; see [Mipmaps](#mipmaps). Defaults to `true`.

<span class="def-icon">:material-card-bulleted-outline:</span> `AllowsDynamicWrites`

:   Whether the texture's contents can be overwritten after creation; see [Writable Textures](#writable-textures). Defaults to `false`.

<span class="def-icon">:material-card-bulleted-outline:</span> `RenderingConfig`

:   How the texture should be sampled by the GPU; see [Sampling](#sampling). Defaults to a `TextureRenderingConfig` with default values.

<span class="def-icon">:material-card-bulleted-outline:</span> `ProcessingToApply`

:   Alterations to make to the texture's data before it is uploaded; see [Processing](#processing). Defaults to `TextureProcessingConfig.None`.

<span class="def-icon">:material-card-bulleted-outline:</span> `Name`

:   The name to give the texture. May be left empty.

### TextureReadConfig

The `TextureReadConfig` controls how a texture file's contents are read from disk:

<span class="def-icon">:material-card-bulleted-outline:</span> `IncludeWAlphaChannel`

:   Whether to keep the file's alpha channel, where it has one. Defaults to `true`.

	Setting this to `false` discards alpha data even when the file carries it, which saves a quarter of the texture's memory for images whose transparency you do not intend to use.

<span class="def-icon">:material-card-bulleted-outline:</span> `ForceWAlphaChannelPresence`

:   Whether to give the texture an alpha channel even when the file has none. Defaults to `false`.

	The added alpha channel is fully opaque. This is useful when a texture must have a four-channel layout regardless of what the source file contains. Setting this to `true` while `IncludeWAlphaChannel` is `false` will throw an exception.

## Reading Texture Data Without Loading

```csharp
var metadata = factory.AssetLoader.ReadTextureMetadata(@"Assets/bricks.png");
var texels = new TexelRgba32[metadata.Dimensions.X * metadata.Dimensions.Y];
factory.AssetLoader.ReadTexture(@"Assets/bricks.png", texels.AsSpan());
```

If you want to inspect or alter a texture's data in your own code before it reaches the GPU, you can read it in to a buffer without creating a texture resource at all:

* `factory.AssetLoader.ReadTextureMetadata()` reports the file's dimensions and whether it includes an alpha channel, which lets you size a buffer for its texels.
* `factory.AssetLoader.ReadTexture()` reads the file's texels in to the given buffer, converting them to the buffer's texel type (e.g. `TexelRgb24` or `TexelRgba32`) as it does so. It returns the number of texels written. An overload also accepts a `TextureProcessingConfig` to apply as the data is read.

Note that the data is written row-by-row, from the bottom row of the image to the top.

For information on how to create a `Texture` with the modified texel data, see [Creating Textures](creating_textures.md).
