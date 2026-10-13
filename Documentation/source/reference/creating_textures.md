---
title: Creating Textures
description: Information on how to programmatically create textures in TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * You can create textures of every map type programmatically via `factory.TextureBuilder`, from single values or procedural patterns. :material-arrow-right: [Map Creation Functions](#map-creation-functions)
    * You can also create textures from your own raw texel data. :material-arrow-right: [Creating Textures From Texel Data](#creating-textures-from-texel-data)
    * `TextureUtils` provides helpers for processing, combining, and mipmapping texel data in memory. :material-arrow-right: [Texture Utilities](#texture-utilities)

</div>

## The Texture Builder

```csharp
using var plainRed = factory.TextureBuilder.CreateColorMap( // (1)!
	new ColorVect(1f, 0f, 0f), 
	includeAlpha: false
);

using var polishedMetal = factory.TextureBuilder.CreateOcclusionRoughnessMetallicReflectanceMap( // (2)!
	roughness: 0.2f, 
	metallic: 1f
); 

using var chequerboard = factory.TextureBuilder.CreateColorMap( // (3)!
	TexturePattern.Chequerboard(new ColorVect(1f, 1f, 1f), new ColorVect(0.1f, 0.1f, 0.1f), repetitionCount: (8, 8)),
	includeAlpha: false
);

var functionTexels = Generate(out var functionDimensions);
using var functionTex = factory.TextureBuilder.CreateTexture<TexelRgba32>( // (4)!
	functionTexels, 
	functionDimensions, 
	TextureDataType.LinearData
); 
```

1.	Creates a color map that is entirely red.

	Creating a map from a single value results in a 1x1 texture, which is the cheapest possible way to give a material a uniform colour (or any other uniform property).

2.	Creates an ORMR map with 20% roughness and 100% metallic, leaving the occlusion and reflectance values at their defaults.

3.	Creates a black-and-white chequerboard color map from a `TexturePattern`. Texture patterns are explained in detail on the [Texture Patterns](texture_patterns.md) page.

4.	Creates a texture directly from an array of texels (in this case, from some hypothetical `Generate()` method that returns a `TexelRgba32[]`).

	When creating a texture this way you must specify its `TextureDataType` ([explained here](loading_textures.md#texture-data-types)).

As well as loading textures from files, it's possible to create them programmatically using the `factory.TextureBuilder` (the same object is also accessible via `factory.AssetLoader.TextureBuilder`). Every `Create[...]()` method returns a new `Texture` resource which, like any loaded texture, should be disposed when you're done with it.

### Map Creation Functions

The texture builder provides a function to create each [map type](texture_map_types.md). Each map type requires data of a specific value type:

| Map Type                | Value Type(s)                    | Function                                                                                     |
| ----------------------: | :------------------------------- | :------------------------------------------------------------------------------------------  |
| Color                   | `ColorVect`                      | `CreateColorMap()`                                                                           |
| Normal                  | `SphericalTranslation`           | `CreateNormalMap()`                                                                          |
| ORM(R)                  | `Real` (one per component)       | `CreateOcclusionRoughnessMetallicMap()` / `CreateOcclusionRoughnessMetallicReflectanceMap()` |
| Absorption-Transmission | `ColorVect` + `Real`             | `CreateAbsorptionTransmissionMap()`                                                          |
| Emissive                | `ColorVect` + `Real`             | `CreateEmissiveMap()`                                                                        |
| Anisotropy              | `Angle` + `Real`                 | `CreateAnisotropyMap()`                                                                      |
| Clearcoat               | `Real` + `Real`                  | `CreateClearCoatMap()`                                                                       |
| Canvas texture          | `ColorVect`                      | `CreateCanvasTexture()`                                                                      |

??? abstract "Normal Map SphericalTranslation Explanation"
	Each `SphericalTranslation` is comprised of two angle parameters: An `AzimuthalOffset` and a `PolarOffset`.

	Any value greater than `0°` for `PolarOffset` (the second parameter) will "bend" the texture normal towards the direction determined by the `AzimuthalOffset` (the first parameter).
	
	* 	The first parameter (`AzimuthalOffset`) can be any angle and it represents the 2D orientation of the texel's normal direction. In other words, this parameter specifies the **direction of distortion** on the surface. 
	
		A value of `0°` points along the mesh surface's "U" axis (also known as its **tangent** direction). 
		
		A value of `90°` points along the mesh surface's "V" axis (also known as its **bitangent** direction).
		
		A value of `180°` points opposite to the mesh surface's "U" axis.
		
		A value of `270°` points opposite to the mesh surface's "V" axis.

	* 	The second parameter (`PolarOffset`) should be an angle between `0°`and `90°` and it represents **how distorted** the surface is. 
	
		A value of `0°` means the texel normal direction will point perfectly straight out from the surface (indicating a perfectly flat surface at this point). 
		
		A value of `90°` means the texel normal direction will be completely flattened against the surface (indicating a 100% distorted surface).

Each function has overloads accepting the map's data in the following forms:

* __Single values__: Each data component is supplied as a single value (e.g. `CreateEmissiveMap(color: myColor, intensity: 0.5f)`), producing a 1x1 texture. Every argument is optional: Any you omit (or pass `null` for) is set to its default value. These defaults are the same as the [built-in default textures](built-in_textures.md#default-map-textures), and are also exposed as static properties on the interface (e.g. `ITextureBuilder.DefaultRoughness`).

* __Texture patterns__: Each data component is supplied as a `TexturePattern<T>` (see [Texture Patterns](#texture-patterns) below), where `T` is the component's value type. The patterns are combined in to one map (and needn't all be the same size; the map takes the largest width and height of all the given patterns).

#### TextureCreationConfig

Both of the above forms also have an overload that accepts a `TextureCreationConfig` in place of the name, giving you full control over how the texture is created (e.g. mipmaps, compression, sampling). The `TextureCreationConfig` is explained [here](loading_textures.md#texturecreationconfig).

The map functions create their textures with the correct `TextureDataType` for the map type, and generate mipmaps for any texture larger than 1x1. Additionally, color maps and canvas textures created with `includeAlpha: true` have their alpha [premultiplied](texture_map_types.md#alpha-premultiplication) automatically (unless you supply your own `TextureCreationConfig`).

### Texture Patterns

```csharp
var normalPattern = TexturePattern.Circles(
	SphericalTranslation.ZeroZero,
	new SphericalTranslation(0f, 45f),
	new SphericalTranslation(90f, 45f),
	new SphericalTranslation(180f, 45f),
	new SphericalTranslation(270f, 45f),
	SphericalTranslation.ZeroZero,
	interiorRadius: 96, borderSize: 24, paddingSize: TexturePatternDefaultValues.CirclesDefaultPaddingSize / 3
);
using var normalMap = factory.AssetLoader.TextureBuilder.CreateNormalMap(normalPattern);
```

A `TexturePattern<T>` is a procedural description of an image, such as a chequerboard, a gradient, a set of circles, or a grid of lines. Each pattern defines its own dimensions and produces a value of type `T` for every texel; meaning the same pattern types can be used to generate colours (`ColorVect`), normal directions (`SphericalTranslation`), angles (`Angle`), or plain numeric data (`Real`).

Patterns can be passed to every map creation function (and to `CreateTexture()`, below). They're a convenient way to create test textures, simple procedural surfaces, or textures for user interfaces without needing any image files.

The available pattern types and their options are explained in full on the [Texture Patterns](texture_patterns.md) page.

### Creating Textures From Texel Data

```csharp
var dimensions = new XYPair<int>(512, 512);
using var texelsLease = factory.ResourceAllocator.BorrowSpan<TexelRgb24>(dimensions.Area); // (2)!
var texels = texelsLease.Span;
for (var y = 0; y < dimensions.Y; ++y) {
	for (var x = 0; x < dimensions.X; ++x) {
		texels[y * dimensions.X + x] = TexelRgb24.FromNormalizedFloats(x / 511f, y / 511f, 0f); // (1)!
	}
}

using var gradient = factory.TextureBuilder.CreateTexture<TexelRgb24>(texels, dimensions, TextureDataType.ColorSrgb);
```

1.	Texels are laid out row-by-row, starting from the **bottom** row of the image. So in this example the texture is black in its bottom-left corner, becomes redder towards its right edge, and greener towards its top edge.

2.	This borrows a span without allocating on the managed heap, thus preventing GC stutter. Explained further in [Avoiding GC Stutter](avoiding_gc_stutter.md).

If none of the map creation functions suit your needs, `CreateTexture()` creates a texture from any span of texels. TinyFFR supports two texel types:

* `TexelRgb24`: Three channels (red, green, blue) of one byte each.
* `TexelRgba32`: Four channels (red, green, blue, alpha) of one byte each.

Both types offer helpers for working with their values, including `FromNormalizedFloats()` (to create a texel from values in the range \[0, 1\]), `ToColorVect()`, and `ToRgb24()` / `ToRgba32()` (to convert between the two).

`CreateTexture()` has a few overloads:

* `CreateTexture(texels, dimensions, dataType, generateMipMaps, name)` is the simplest form. You must specify the [TextureDataType](loading_textures.md#texture-data-types); mipmaps are generated by default for any texture larger than 1x1.
* `CreateTexture(texels, generationConfig, creationConfig)` takes the dimensions via a `TextureGenerationConfig` and gives you full control over the creation via a [`TextureCreationConfig`](loading_textures.md#texturecreationconfig) (including any processing to apply to the texels as the texture is created).
* Further overloads accept a `TexturePattern<TTexel>` or a single plain-fill texel in place of the texel span.

Note that when passing a `Span<TTexel>` or an array (rather than a `ReadOnlySpan<TTexel>`) you will usually need to specify the texel type explicitly, as in the example above (`CreateTexture<TexelRgb24>(...)`); the C# compiler can not infer the generic type through the implicit conversion to `ReadOnlySpan<TTexel>`.

???+ tip "Encoding Map Data"
	Some map types encode their data in ways that aren't obvious from the raw channel values (normal maps and anisotropy maps in particular). If you're building texel data for a specific map type yourself, use the static helper methods on `ITextureBuilder` to encode each texel exactly as the map creation functions would: `CreateColorTexel()`, `CreateNormalTexel()`, `CreateOcclusionRoughnessMetallicTexel()`, `CreateOcclusionRoughnessMetallicReflectanceTexel()`, `CreateAbsorptionTransmissionTexel()`, `CreateEmissiveTexel()`, `CreateAnisotropyTexel()`, and `CreateClearCoatTexel()`.

If you have texture data that has already been compressed (e.g. via the `TextureCompressor`), use `CreateTextureFromCompressedData()` instead; this is explained on the [Texture Compression](texture_compression.md) page.

### Generating Texels Yourself

```csharp
var occlusion = TexturePattern.Chequerboard<Real>(1f, 0.6f, repetitionCount: (8, 8));
var roughness = TexturePattern.Chequerboard<Real>(0.3f, 0.7f, repetitionCount: (8, 8));
var metallic = TexturePattern.Chequerboard<Real>(0f, 1f, repetitionCount: (8, 8));

var dimensions = TexturePatternPrinter.GetCompositePatternDimensions(occlusion, roughness, metallic); // (1)!
using var texelsLease = factory.ResourceAllocator.BorrowSpan<TexelRgb24>(dimensions.Area); // (5)!
var texels = texelsLease.Span;
ITextureBuilder.PrintOcclusionRoughnessMetallicMap(occlusion, roughness, metallic, texels); // (2)!

TextureUtils.FlipTexture(texels, dimensions, aroundVerticalCentre: true, aroundHorizontalCentre: false); // (3)!

using var orm = factory.TextureBuilder.CreateTexture<TexelRgb24>(
	texels,
	new TextureGenerationConfig { Dimensions = dimensions },
	ITextureBuilder.GetOcclusionRoughnessMetallicMapCreationConfig(dimensions) // (4)!
);
```

1.	Calculates the dimensions of the map that combining these three patterns will produce, so that we can size our texel buffer.

2.	Writes the texels of the ORM map in to our buffer, exactly as `CreateOcclusionRoughnessMetallicMap()` would have.

	This method is static and doesn't touch the texture builder, so this step (and the previous one) can be done on any thread.

3.	Makes any alterations we want to the texel data (in this case, mirroring it left-to-right; see [Processing](#processing) below).

4.	Gets the exact same `TextureCreationConfig` that `CreateOcclusionRoughnessMetallicMap()` would have used, and creates the texture with it.

5.	This borrows a span without allocating on the managed heap, thus preventing GC stutter. Explained further in [Avoiding GC Stutter](avoiding_gc_stutter.md).

Every map creation function is built from two static halves that are also exposed publicly on `ITextureBuilder`:

* `Print[...]Map()` (e.g. `PrintColorMap()`, `PrintNormalMap()`) writes the map's texels in to a buffer of your choosing.
* `Get[...]MapCreationConfig()` (e.g. `GetColorMapCreationConfig()`, `GetNormalMapCreationConfig()`) returns the `TextureCreationConfig` that the map creation function would use for a map of the given dimensions.

Using these yourself is useful when you want to modify the generated texels before creating the texture, or when you want to generate texel data on a background thread (only the final `CreateTexture()` call needs to happen on the primary thread). Because these are the same methods the map creation functions use internally, the resultant texture is guaranteed to be identical to one created the normal way.

## Texture Utilities

The static `TextureUtils` class provides helpers for manipulating texel data held in memory, whether it was generated by your code, printed from patterns, or read from a file via `assetLoader.ReadTexture()` ([explained here](loading_textures.md#reading-texture-data-without-loading)). `TextureUtils` can be used from any thread and doesn't require a factory to exist at all. That means you can use it in a standalone tool (e.g. as part of your build pipeline).

### Processing

```csharp
TextureUtils.FlipTexture(...);
TextureUtils.NegateTexture(...);
TextureUtils.SwizzleTexture(...);
TextureUtils.PremultiplyAlphaForTexture(...);
TextureUtils.ProcessTexture(...);
```

These methods modify a span of texels in place:

<span class="def-icon">:material-card-bulleted-outline:</span> `FlipTexture()`

:   Mirrors the texture left-to-right and/or top-to-bottom.

<span class="def-icon">:material-card-bulleted-outline:</span> `NegateTexture()`

:   Inverts the chosen channels, so that their strongest values become their weakest and vice versa (e.g. converting glossiness data in to roughness data).

<span class="def-icon">:material-card-bulleted-outline:</span> `SwizzleTexture()`

:   Rearranges the texture's channels, choosing which source channel supplies each output channel.

<span class="def-icon">:material-card-bulleted-outline:</span> `PremultiplyAlphaForTexture()`

:   Multiplies each texel's colour channels by its alpha (see [Alpha Premultiplication](texture_map_types.md#alpha-premultiplication)).

<span class="def-icon">:material-card-bulleted-outline:</span> `ProcessTexture()`

:   Applies a `TextureProcessingConfig` to the texels; this is the same config that can be applied when loading a texture, and it's explained fully [here](loading_textures.md#processing). It lets you apply several alterations in one pass (each of the methods above is simply a shortcut for `ProcessTexture()` with a single alteration).

### Combination

```csharp
var dimensions = TextureUtils.GetCombinedTextureDimensions( // (1)!
	occlusionDimensions, 
	roughnessDimensions, 
	metallicDimensions
);
using var ormTexelsLease = factory.ResourceAllocator.BorrowSpan<TexelRgba32>(dimensions.Area); // (3)!
var ormTexels = ormTexelsLease.Span;

TextureUtils.CombineTextures(
	occlusionTexels, occlusionDimensions,
	roughnessTexels, roughnessDimensions,
	metallicTexels, metallicDimensions,
	new TextureCombinationConfig("aRbRcR"), // (2)!
	ormTexels
);
```

1.	Calculates the dimensions of the combined texture (the largest width and height of all the inputs), so that we can size our output buffer.

2.	This config specifies that the output's red channel should come from the red channel of texture A (the occlusion data), the green channel from the red channel of texture B (the roughness data), and the blue channel from the red channel of texture C (the metallic data).

3.	This borrows a span without allocating on the managed heap, thus preventing GC stutter. Explained further in [Avoiding GC Stutter](avoiding_gc_stutter.md).

`TextureUtils.CombineTextures()` packs the channels of two, three, or four textures in to a single output texture. This is most commonly used to combine separate single-channel data textures in to one map (e.g. separate occlusion, roughness, and metallic data in to an ORM map).

The `TextureCombinationConfig` specifies which channel of which input texture supplies each channel of the output:

* The simplest way to create one is with a selection string, as above. The string is read two characters at a time, giving the source for the output's red, green, blue, and (optionally) alpha channels in that order. The first character of each pair names the source texture (`a` to `d`) and the second names its channel (`r`, `g`, `b`, or `a`). Case is ignored, so `"aRbRcR"` and `"arbrcr"` are equivalent. A six-character string creates a three-channel output; an eight-character string creates a four-channel output.
* Alternatively, each output channel's source can be specified explicitly as a `TextureCombinationSource`, e.g. `new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R)`.

When the input textures are different sizes, the output takes the largest width and height of all of them, and any smaller inputs are enlarged according to the config's `ScalingStrategy`:

<span class="def-icon">:material-card-bulleted-outline:</span> `PixelUpscale`

:   Enlarges the input by repeating each of its texels, preserving its exact values. This is the default, and is the right choice for data textures (where blending neighbouring values together would invent meaningless data).

<span class="def-icon">:material-card-bulleted-outline:</span> `BilinearUpscale`

:   Enlarges the input by blending between neighbouring texels. This usually looks better for colour data, but creates values that weren't in the original input.

<span class="def-icon">:material-card-bulleted-outline:</span> `RepeatingTile`

:   Keeps the input at its original size and tiles it across the output.

<span class="def-icon">:material-card-bulleted-outline:</span> `ExtendEdges`

:   Keeps the input at its original size, places it in the centre of the output, and fills the remaining border by stretching the input's outermost texels outwards.

???+ tip "Combining Texture Files"
	If the textures you want to combine are files on disk, you don't need to read them in to memory yourself. `assetLoader.LoadCombinedTexture()` loads and combines up to four files directly in to a single texture using a `TextureCombinationConfig` (optionally applying a separate `TextureProcessingConfig` to each input file first). Similarly, `assetLoader.ReadCombinedTexture()` reads and combines files in to a texel buffer, and `assetLoader.ReadCombinedTextureMetadata()` reports the dimensions of the combined result.

	Many of the [`Load[...]Map()` functions](texture_map_types.md) also offer overloads that combine separate files for you.

### Mipmaps

TinyFFR generates mipmaps automatically for any texture created with `GenerateMipMaps` enabled, so you will rarely need to do so yourself. However, if you're preparing your own mip chains (for example, for [texture compression](texture_compression.md)), `TextureUtils` exposes the same helpers TinyFFR uses internally:

* `GetMipLevelCount()` returns how many mip levels a texture of the given dimensions has (including the full-size level).
* `GetMipLevelDimensions()` returns the dimensions of a given mip level.
* `GenerateNextMipLevel()` generates the next (half-sized) mip level from the given level. It takes a `TextureDataType` so that the data is averaged correctly (e.g. colour data is averaged in linear space, and unit vectors are re-normalised).

### Conversion

Finally, `TextureUtils.Convert()` converts a span of texels from one texel type to another (e.g. from `TexelRgb24` to `TexelRgba32`).
