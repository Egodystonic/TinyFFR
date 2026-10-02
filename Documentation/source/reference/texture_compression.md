---
title: Texture Compression
description: Details on how and when to compress textures with TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Textures can be compressed as they're loaded or created, typically reducing their video memory use by 4x (or more). :material-arrow-right: [Texture Compression](#texture-compression)
    * Compression is slow, so it's best done once ahead of time, either by baking assets or via the `TextureCompressor`. :material-arrow-right: [Pre-Compressing Textures](#pre-compressing-textures)
    * Pre-compressed data can be turned back in to a texture with `TextureBuilder.CreateTextureFromCompressedData()`. :material-arrow-right: [Creating Textures From Compressed Data](#creating-textures-from-compressed-data)

</div>

## Texture Compression

```csharp
using var bricks = factory.AssetLoader.LoadColorMap( // (1)!
	@"Assets/bricks.png", 
	compressionQuality: Quality.High
); 

using var carBundle = factory.AssetLoader.LoadBundledAsset( // (2)!
	@"Assets/Models/ToyCar.glb",
	new ModelCreationConfig {
		TextureConfig = new TextureCreationConfig {
			DataType = TextureDataType.LinearData,
			CompressionQuality = Quality.Standard
		}
	}
);
```

1.	Loads a color map and compresses it as it's loaded. Every `Load[...]Map()` function has an optional `compressionQuality` argument.

2.	Loads a bundled asset (e.g. a glTF file) and compresses every texture within it as they're loaded. 

	(The `DataType` property must be set as it's required, but it's overridden for any texture whose purpose is known; see [Bundled Assets](bundled_assets.md).)

By default, textures are uploaded to the GPU exactly as they are stored in memory: three or four bytes for every texel. *Texture compression* instead encodes the texture in a GPU-native "block compressed" format, where every 4x4 block of texels is stored together in just 8 or 16 bytes.

Compressed textures use a fraction of the video memory of uncompressed ones (typically a quarter of the memory of an uncompressed RGBA texture, or an eighth for `Bc1Srgb`), and are also faster for the GPU to sample. In exchange:

* Compression is *lossy*, meaning a small amount of image detail is lost (though usually not noticeably at the higher quality levels).
* Compressing a texture takes time, especially at the higher quality levels.
* Mipmaps for compressed textures must be generated on the CPU before compression, rather than by the GPU, which adds further time to the load.
* Compressed textures can not allow dynamic writes (i.e. their contents can not be changed after they're created).

Compression can be requested anywhere a texture is created or loaded:

* Via the `compressionQuality` argument on the `Load[...]Map()` functions;
* Via the `CompressionQuality` property of a `TextureCreationConfig` (explained [here](loading_textures.md#compression));
* Via the `TextureConfig` property of a `ModelCreationConfig` for bundled assets.

Compression is disabled by default.

When loading a texture asynchronously, the compression is performed on a background thread; so combining compression with [async asset loading](asynchronous_loading.md) avoids stalling your application while the texture is compressed.

### Compression Formats

TinyFFR supports the following compression formats (the `TextureCompressionFormat` enum):

| Format      | Bytes per 4x4 Block | Bytes per Texel | Reduction vs. RGBA | Used For                                                          |
| :---------- | ------------------: | --------------: | -----------------: | :---------------------------------------------------------------- |
| `Bc1Srgb`   | 8                   | 0.5             | 8x                 | Colour data with no alpha channel, at the lowest quality level     |
| `Bc3Srgb`   | 16                  | 1               | 4x                 | Colour data with an alpha channel, at the lowest quality level     |
| `Bc5`       | 16                  | 1               | 4x                 | Two-channel data (normal maps and clearcoat maps)                  |
| `Bc7Srgb`   | 16                  | 1               | 4x                 | Colour data (with or without alpha), at every other quality level  |
| `Bc7Linear` | 16                  | 1               | 4x                 | Non-colour data (e.g. ORM(R) maps and anisotropy maps)             |

For comparison, an uncompressed texture uses 3 bytes per texel (RGB) or 4 bytes per texel (RGBA).(1)
{ .annotate }

1.	Textures of any size can be compressed. Textures whose dimensions aren't multiples of 4 are simply padded up to a whole number of blocks.

### Choosing a Format

When you specify a compression quality, TinyFFR chooses the format automatically according to the texture's [TextureDataType](loading_textures.md#texture-data-types):

| TextureDataType           | At `Quality.VeryLow`                                         | At Every Other Quality |
| :------------------------ | :----------------------------------------------------------- | :--------------------- |
| `ColorSrgb`               | `Bc1Srgb` (no alpha channel) / `Bc3Srgb` (with alpha channel) | `Bc7Srgb`              |
| `LinearData`              | `Bc7Linear`                                                  | `Bc7Linear`            |
| `LinearDataUnitVector`    | `Bc5`                                                        | `Bc5`                  |
| `LinearDataTwoChannelMax` | `Bc5`                                                        | `Bc5`                  |

The quality level also controls how much effort the compressor spends searching for the best encoding of each block: `Quality.VeryLow` is the fastest and loses the most detail, whereas `Quality.VeryHigh` is by far the slowest and loses the least.

A few further rules apply:

* If the chosen format isn't supported by the current GPU, TinyFFR falls back to another format (`Bc7Srgb` falls back to `Bc1Srgb`/`Bc3Srgb`; `Bc5` falls back to `Bc7Linear`), or leaves the texture uncompressed if no fallback is supported. Note that almost all modern hardware supports all formats listed here, so in practice this rule almost never is actually effected.
* If compressing the texture wouldn't actually make it smaller (e.g. for a tiny 1x1 texture, which would still occupy an entire 4x4 block) it's left uncompressed.
* Textures that allow dynamic writes are never compressed.

If you want to know which format a given texture would be compressed to, `TextureCompressor.GetRecommendedFormat()` applies exactly these rules and returns the result.

## Pre-Compressing Textures

Because compression takes time (sometimes a *lot* of time at the higher quality levels), it's usually best to compress textures once, ahead of time, rather than every time your application loads them. TinyFFR offers two ways to do this.

### Baking

The asset bakery is the recommended way to pre-process, pre-prepare, and pre-compress all asset data (not just textures). This is because as well as compressing the texture data, baked asset information is stored in a binary format that makes it much faster for TinyFFR to load (compression or not).

Baking is described in full detail here: [Asset Bakery](asset_bakery.md).

### The TextureCompressor

If you'd rather store compressed texture data in your own format, the static `TextureCompressor` class lets you compress texel data yourself:

```csharp
var metadata = factory.AssetLoader.ReadTextureMetadata(@"Assets/bricks.png");
var dimensions = metadata.Dimensions;
using var texelsLease = factory.ResourceAllocator.BorrowSpan<TexelRgba32>(dimensions.Area); // (6)!
var texels = texelsLease.Span;
factory.AssetLoader.ReadTexture(@"Assets/bricks.png", texels); // (1)!

var format = TextureCompressor.GetRecommendedFormat( // (2)!
	dimensions,
	TexelType.Rgba32,
	TextureDataType.ColorSrgb,
	Quality.VeryHigh,
	textureAllowsDynamicWrites: false,
	includesMipMapGeneration: true
);

using var compressedDataLease = factory.ResourceAllocator.BorrowSpan<byte>(TextureCompressor.GetCompressedSizeBytes(dimensions, format, includeMipChain: true)); // (3)!
var compressedData = compressedDataLease.Span;
TextureCompressor.Compress<TexelRgba32>( // (4)!
	texels,
	dimensions,
	format,
	Quality.VeryHigh,
	TextureDataType.ColorSrgb,
	includeMipMapGeneration: true,
	compressedData
);

SaveToMyAssetFormat(compressedData, dimensions, format); // (5)!
```

1.	Reads the texture file's texels in to a buffer (see [Reading Texture Data Without Loading](loading_textures.md#reading-texture-data-without-loading)). The texels could just as well come from anywhere else, such as another image library or your own generated data.

2.	Asks TinyFFR which format this texture should be compressed to, using the same rules described in [Choosing a Format](#choosing-a-format) above. You can also simply pick a format yourself.

	Note that this may return `TextureCompressionFormat.None` (e.g. if the texture is too small to benefit from compression), in which case you should store the texture uncompressed instead.

3.	Calculates the size of the compressed data (including the full chain of mipmaps) and borrows a buffer for it (see note 6 below).

4.	Compresses the texels (and generates and compresses each mip level) in to the buffer.

5.	Saves the compressed data using some hypothetical method of your own. 

	The compressed data contains nothing but the compressed blocks themselves, so you must also store everything you'll need to recreate the texture later (see below).

6.	This borrows a span without allocating on the managed heap, thus preventing GC stutter. Explained further in [Avoiding GC Stutter](avoiding_gc_stutter.md).

`TextureCompressor` can be used from any thread and doesn't require a factory to exist at all. That means you can use it in a standalone tool (e.g. as part of your build pipeline) to compress textures for your application ahead of time.(1)
{ .annotate }

1.	One caveat to this is format support querying; see the [note below](#format-support).

The compressed data is laid out as the full-size texture's blocks first, followed by the blocks of each successively smaller mip level in turn (if mipmaps were generated). It doesn't include any description of itself, so alongside it you must keep the texture's dimensions, its compression format, how many levels it contains, the texel type it was compressed from, and its `TextureDataType`.

### Creating Textures From Compressed Data

```csharp
var stored = LoadFromMyAssetFormat(@"Assets/bricks.mydata"); // (1)!

using var bricks = factory.TextureBuilder.CreateTextureFromCompressedData(
	stored.CompressedData,
	stored.Dimensions,
	stored.Format,
	levelCount: TextureUtils.GetMipLevelCount(stored.Dimensions), // (2)!
	sourceTexelType: TexelType.Rgba32, // (3)!
	dataType: TextureDataType.ColorSrgb,
	renderingConfig: new TextureRenderingConfig()
);
```

1.	Loads the data saved in the previous example, using some hypothetical method of your own.

2.	The number of levels in the data, counting the full-size texture as one. As we compressed with the full mip chain, this is the total number of mip levels for a texture of this size. If the data was compressed without mipmaps, specify `1`.

3.	The texel type the data was originally compressed from, and the data type it represents. These must match the values used when compressing.

`factory.TextureBuilder.CreateTextureFromCompressedData()` uploads already-compressed data straight to the GPU, so no compression work is done at all. Every argument must exactly match the data being supplied; the method throws an exception if:

* The `compressionFormat` is `TextureCompressionFormat.None`, or is not supported by the current GPU (see below);
* The `levelCount` is less than 1 or greater than the number of mip levels a texture of the given dimensions can have;
* The `compressedData` is too short to contain the given number of levels at the given dimensions and format.

Like the rest of the texture builder, this method must be called on the primary thread. Textures created this way never allow dynamic writes.

### Decompressing

```csharp
var level = 2;
var levelDimensions = TextureUtils.GetMipLevelDimensions(dimensions, level);
var levelStart = TextureCompressor.GetCompressedSizeBytes(dimensions, format, levelCount: level); // (1)!
var levelLength = TextureCompressor.GetCompressedSizeBytes(levelDimensions, format, levelCount: 1);

using var decompressedLease = factory.ResourceAllocator.BorrowSpan<TexelRgba32>(levelDimensions.Area); // (2)!
var decompressed = decompressedLease.Span;
TextureCompressor.Decompress<TexelRgba32>(compressedData.Slice(levelStart, levelLength), levelDimensions, format, decompressed);
```

1.	The data for mip level *n* starts immediately after the data for every level before it, so its offset is the size of the first *n* levels.

2.	This borrows a span without allocating on the managed heap, thus preventing GC stutter. Explained further in [Avoiding GC Stutter](avoiding_gc_stutter.md).

`TextureCompressor.Decompress()` decodes a single level of compressed data back in to texels. This can be useful for inspecting the result of compression (e.g. to compare it with the original texels) or for processing compressed data further. Remember that compression is lossy, so the decompressed texels won't exactly match the originals.

### Format Support

`TextureCompressor.FormatIsSupported()` reports whether the current GPU supports a given compression format. Support for every format is near-universal on desktop GPUs, so this check is largely a formality.

???+ danger "Format Support Requires a Live Factory"
	Note that format support is only detected once a factory has been created. This is because factory initialization queries the GPU driver for its feature flags. Until that query is executed, no format support is known.
	
	With no live factory, `FormatIsSupported()` reports every format (except `None`) as unsupported, and `GetRecommendedFormat()` will therefore recommend `None`. When using `GetRecommendedFormat()` in a standalone tool without a factory, pass `includeUnsupportedFormats: true` to get the ideal format regardless of the current machine's support.
