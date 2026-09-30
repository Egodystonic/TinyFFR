---
title: Loading Textures
description: Information on how to load texture data/files in TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * TODO :material-arrow-right: [Mesh Files](#mesh-files)

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

The easiest way to load a texture is to use one of the many `Load[...]Map()` functions (e.g. `LoadColorMap()`, `LoadNormalMap()`) or `LoadCanvasTexture()` for canvas textures. These methods set the `TextureDataType`, `TextureCreationConfig`, and `TextureReadConfig` (all explained below) correctly for the indicated usage.

The alternative is `assetLoader.LoadTexture(...)`. However, when loading a texture this way you must specify at least its `TextureDataType`; and some of the default load options may not be fully appropriate for your intended usage (i.e. the `TextureCreationConfig` and `TextureReadConfig`). Therefore, `LoadTexture(...)` can be considered a "raw" API for when you want maximum control on how an image asset is loaded in to the system.

## Texture Data Types

Texture files (such as `.bmp`, `.jpeg`, `.png` etc) only store colour channel information; but to effectively *use* that information TinyFFR needs to know how what the intended *use* of the texture is.

??? question "Why is the TextureDataType Important?"
	It can be tempting to think of a texture on the GPU as just "colour channel data". However, to effecively work with texture data we need to know its intended purpose for the following reasons:
	
	* Compression: When texture data is compressed it's important to know how its data should be interpreted so that we can compress it in a non-destructive way. The approach to compressing colour data differs from that of, say, vector data (such as for a normal map).
	* CLAUDE please complete this list
	
<span class="def-icon">:material-card-bulleted-outline:</span> `ColorSrgb`

:   Claude please finish this list too

## Customizing the Load Operation

Claude please copy the style of this header in loading_meshes.md, but for textures (i.e. talk about TextureReadConfig, TextureCreationConfig, etc)
