---
title: Materials
description: This page explains the concept of materials in TinyFFR.
---

TinyFFR (short for "Tiny **Fixed-Function** Renderer") doesn’t use a programmable material pipeline; instead, it provides a small set of prebuilt material models based on predefined texture map types.

The following page explains the supported map types and material types and documents how to load/import/generate them.

## Map Types

All materials in TinyFFR are comprised of one or more texture "map" files. Texture files can have their data interpreted differently according to their map type. The map type defines how a texture's texel data is interpreted when rendered as part of a material.

The `AssetLoader` makes it easy to load texture files for all supported map types, detailed below:

### Color Maps

![Image depicting a color map texture](materials_map_color.jpg){ align=left : style="max-height:128px;" }

| AssetLoader Method           | `LoadColorMap(...)`                           |
| ---------------------------: | :-------------------------------------------- |
| Expected Channel Count       | 3 (RGB) or 4 (RGBA)                           |
| Expected Linear or sRGB      | Assumed sRGB                                  |

Also known as albedo or diffuse maps. When a texture is interpreted as a color map its data will be used to set the albedo/diffuse (e.g. the "base") color of a material.

When alpha data is present, the RGB channels are expected to be premultiplied. Additionally, the way the alpha data is used depends on the material type and configuration (see corresponding material documentation below).

### Normal Maps

![Image depicting a normal map texture](materials_map_normal.jpg){ align=left : style="max-height:128px;" }

| AssetLoader Method           | `LoadNormalMap(...)`                          |
| ---------------------------: | :-------------------------------------------- |
| Expected Channel Count       | 3 (RGB)                                       |
| Expected Linear or sRGB      | Assumed linear                                |

Normal maps define how light bounces off a surface by defining which way the surface is 'facing' at each texel (relative to the surface's underlying polygon normal).

Normal maps are expected in OpenGL format. If using DirectX-formatted texture files, you can supply an optional `isDirectXFormat: true` argument to `LoadNormalMap()`. This adds a small increase in load time as the library will correct the data before passing it to the GPU.

The normal map is interpreted as a unit-length 3D vector where R, G, and B channels map to X, Y, and Z components in a normalized range (e.g. \[0 to 255\] maps to \[-1, 1\]). +X points towards the positive mesh tangent direction; +Y points towards the positive mesh bitangent direction; +Z points out of the texture, up away from the surface. Normal maps' Z component should always be positive or 0, never negative (i.e. the blue channel should always be >= 128).

### ORM(R) Maps

![Image depicting an ORM map texture](materials_map_orm.jpg){ align=left : style="max-height:128px;" }

| AssetLoader Method           | `LoadOcclusionRoughnessMetallicMap(...)` / `LoadOcclusionRoughnessMetallicReflectanceMap(...)` |
| ---------------------------: | :--------------------------------------------------------------------------------------------- |
| Expected Channel Count       | 3 (RGB) or 4 (RGBA)                                                                            |
| Expected Linear or sRGB      | Assumed linear                                                                                 |

ORM maps (also known as ARM maps) combine the Ambient-**O**clusion, **R**oughness and **M**etallic information about a surface in to the red, green, and blue texture channels of a single texture.

ORMR maps are the same, but with the addition of **R**eflectance data in the alpha channel.

* The **Ambient Occlusion** data is used to determine how strongly ambient lighting (from the skybox/scene background) illuminates the material surface. Max value (255 or 1.0) indicates full ambient illumination, min value (0) indicates none whatsoever (e.g. fully occluded).
* The **Roughness** data is used to indicate how rough or smooth the material surface is; which in turn is used to determine how "glossy" or "shiny" it looks under lighting. Max value (255 or 1.0) indicates an extremely rough surface, min value (0) indicates a perfectly smooth one.
* The **Metallic** data is used to determine which parts of a material surface are comprised of metal (or metal-like substances). For realistic-looking materials every texel in a metallic map should either be max (255 or 1.0) to indicate metal or min (0) to indicate non-metal (also known as dielectric).
* The **Reflectance** data is optional (except for transmissive materials). 
	* For opaque surfaces this data indicates how much of the surrounding specular light is reflected back off the material. Max value (255 or 1.0) indicates the surface reflects a high amount of specular highlights, min value (0) indicates the surface reflects none.
	* For transmissive surfaces this data indicates the index of refraction of the surface, with higher values translating to a higher IoR.
	* Real-world materials tend to be in the range \[35% - 100%\], so for realistic surfaces stay within that range. The 50% value will behave like most common materials.

Sometimes ORM data will be supplied as multiple separate texture files (usually monochromatic, i.e. single-channel). The `AssetLoader` offers overloads of `LoadOcclusionRoughnessMetallicMap(...)` / `LoadOcclusionRoughnessMetallicReflectanceMap()` that make it easy to combine multiple files in to a single ORM/ORMR map.

Additionally, sometimes you may have only one or two of the required data textures (e.g. only roughness data, or no occlusion data, etc). In these instances it's possible to pass sensible default built-in texture files via `AssetLoader.BuiltInTexturePaths`:

* `AssetLoader.BuiltInTexturePaths.DefaultOcclusionMap`: Resolves to an occlusion texture that simply allows all ambient light to affect a material surface.
* `AssetLoader.BuiltInTexturePaths.DefaultRoughnessMap`: Resolves to a roughness texture that represents 40% roughness, a common 'default' value for materials that otherwise lack roughness information.
* `AssetLoader.BuiltInTexturePaths.DefaultMetallicMap`: Resolves to a metallic texture that represents a non-metallic surface (i.e. entirely dielectric).
* `AssetLoader.BuiltInTexturePaths.DefaultReflectanceMap`: Resolves to a reflectance texture that represents a typical reflectance of common materials (50% reflectance).

### Absorption-Transmission Maps

![Image depicting an AT map texture](materials_map_at.jpg){ align=left : style="max-height:128px;" }

| AssetLoader Method           | `LoadAbsorptionTransmissionMap(...)`          |
| ---------------------------: | :-------------------------------------------- |
| Expected Channel Count       | 4 (RGBA)                                      |
| Expected Linear or sRGB      | Assumed sRGB                                  |

Absorption-transmission (AT) maps are only used for transmissive materials. They define how light passes through the surface of an object via two properties:

* The RGB channels define the absorption of the material: This indicates which light wavelengths (colours) are absorbed. The inverse of this value is therefore "seen" through the material; e.g. if the absorption map is pure yellow (255/255/0) only blue light will pass through the material surface.
* The alpha channel defines the transmission of the material: This indicates the intensity of light overall permitted through the material surface. A max value (255 or 1.0) indicates the surface is fully transparent and a min value (0) indicates the surface is fully opaque.

Commonly, absorption and transmission data may be delievered as two separate texture files; an overload of `LoadAbsorptionTransmissionMap()` allows passing a separate file path for each.

Additionally, you may wish to use a traditional colour map as an "inverse" absorption map: `LoadAbsorptionTransmissionMap()` allows you to specify an optional `invertAbsorption: true` argument for this purpose.

Finally, in cases where you do not have absorption or transmission data, some sensible defaults are provided via `AssetLoader.BuiltInTexturePaths`:

* `AssetLoader.BuiltInTexturePaths.DefaultAbsorptionMap`: Resolves to an absorption texture that allows all light colours to pass through (i.e. the texture itself is completely black, indicating no wavelengths are absorbed by the material surface).
* `AssetLoader.BuiltInTexturePaths.DefaultTransmissionMap`: Resolves to a transmission texture that allows 50% of light to pass through.
* `AssetLoader.BuiltInTexturePaths.DefaultAbsorptionTransmissionMap`: This resolves to an RGBA texture map that combines `DefaultAbsorptionMap` in the RGB channels and `DefaultTransmissionMap` in the alpha channel.

### Emissive Maps

![Image depicting an emissive map texture](materials_map_emissive.jpg){ align=left : style="max-height:128px;" }

| AssetLoader Method           | `LoadEmissiveMap(...)`                        |
| ---------------------------: | :-------------------------------------------- |
| Expected Channel Count       | 3 (RGB) or 4 (RGBA)                           |
| Expected Linear or sRGB      | Assumed sRGB                                  |

Emissive maps are used to create materials whose surfaces appear to emit light. The colour of each texel determines the colour of the light emitted at the corresponding point on the material surface.

* If a 3-channel (RGB) texture is provided, all emissive parts of the material surface are shown with maximum intensity. 
* If a 4-channel (RGBA) texture is provided, the alpha channel is used to control the emissive intensity; where a max value (255 or 1.0) indicates full intensity and a min value (0) indicates no emissive light at all.

If a separate intensity texture is desired, an overload of `LoadEmissiveMap()` allows provision of a separate colour and intensity map.

### Anisotropy Maps

![Image depicting an anisotropy map texture](materials_map_aniso.jpg){ align=left : style="max-height:128px;" }

| AssetLoader Method           | `LoadAnisotropyMapVectorFormatted(...)` / `LoadAnisotropyMapRadialAngleFormatted(...)` |
| ---------------------------: | :------------------------------------------------------------------------------------- |
| Expected Channel Count       | 3 (RGB)                                                                                |
| Expected Linear or sRGB      | Assumed linear                                                                         |

Anisotropy maps are used to indicate non-uniformity in the way a (typically metallic) surface reflects specular light highlights. A common example of this kind of effect in the real world can be seen in brushed metals.

TinyFFR internally stores anisotropy data as a 3-channel map where the red & green channels are the X and Y components of a unit vector pointing in the anisotropic direction of the surface in tangent-space and the blue channel indicates the strength of the anisotropy.

=== ":material-arrow-expand: Vector-formatted data"

	If your anisotropy data is stored in a texture already in the tangent-vector format, you can use `LoadAnisotropyMapVectorFormatted()`. 
	
	If you have only the X/Y tangent-vector data in the red & green channels but **not** strength data in the blue or alpha channel, you can still use `LoadAnisotropyMapVectorFormatted()` and pass `null` for the `strengthChannel` argument: All the data will be interpreted as maximum strength.
	
	Alternatively, if you have separate strength and tangent-vector data files, an overload of `LoadAnisotropyMapVectorFormatted()` is provided that lets you supply both file paths separately to be combined.

=== ":material-radar: Angle-formatted data"

	Your anisotropy data may instead be stored as radial angle data (where a single channel's data indicates the 'angle' of the anisotropy). In this case, you should use `LoadAnisotropyMapRadialAngleFormatted()`. 
 
	The arguments to `LoadAnisotropyMapRadialAngleFormatted()` are used to specify exactly how the data should be interpreted:
	
	<span class="def-icon">:material-code-json:</span> `zeroDirection`

	:   This argument specifies which direction on the 2D plane is considered '0'/'255' in the texel data.
	
	<span class="def-icon">:material-code-json:</span> `encodedRange`

	:   This argument specifies whether the \[0-255\] range in the texel data maps to \[0°-360°\] or \[0°-180°\].
	
	<span class="def-icon">:material-code-json:</span> `encodedAnticlockwise`

	:   This argument specifies the angle moves anticlockwise from 0 to 255, or whether it moves clockwise.
	
	<span class="def-icon">:material-code-json:</span> `strengthChannel`

	:   This argument specifies which channel in the texture file indicates anisotropic strength. You may pass `null` if no such data is present (in which case all the data will be interpreted as maximum strength).
	
		Alternatively, if you have separate strength and tangent-vector data files, an overload of `LoadAnisotropyMapRadialAngleFormatted()` is provided that lets you supply both file paths separately to be combined.
		
	Note that preprocessing of angle-formatted data to the vector-format that TinyFFR uses internally can take some time. A static method `IAssetLoader.ConvertRadialAngleToVectorFormatAnisotropy()` is provided in case you wish to offline-process angle-formatted data for faster consumption; this method allows you to convert a span of texels from the former format to the latter.
	
### Clearcoat Maps

![Image depicting a clearcoat map texture](materials_map_cc.jpg){ align=left : style="max-height:128px;" }

| AssetLoader Method           | `LoadClearCoatMap(...)`                       |
| ---------------------------: | :-------------------------------------------- |
| Expected Channel Count       | 2 (RG)                                        |
| Expected Linear or sRGB      | Assumed linear                                |

Clearcoat maps are used to create an "overcoat" of plastic/wax over some types of materials.

TinyFFR uses a two-channel texture internally to support clearcoat maps.

* The first (red) channel represents the coat thickness; where a max value (255 or 1.0) indicates full thickness and a min value (0) indicates no clearcoat at all.
* The second (green) channel represents the coat roughness; where a max value (255 or 1.0) indicates a fully rough coat and a min value (0) indicates a fully glossy/smooth coat.

In the case where you have a separate thickness and roughness texture, an overload of `LoadClearCoatMap()` is available that can combine two separate texture files.

If you only have one data channel (e.g. only roughness or only thickness), you can use sensible default built-in texture files via `AssetLoader.BuiltInTexturePaths`:

* `AssetLoader.BuiltInTexturePaths.DefaultClearCoatThicknessMap`: Resolves to a clearcoat thickness texture that indicates a maximally-thick coat.
* `AssetLoader.BuiltInTexturePaths.DefaultClearCoatRoughnessMap`: Resolves to a clearcoat roughness texture that indicates a fully glossy/smooth coat.

## Loading Textures

### Built-In Textures

## Generating Textures

### Texture Patterns

## Material Types










---------

1. `FlipX`, if set to `true`, will mirror/flip the image along its horizontal/X-axis.
2. `FlipY`, if set to `true`, will mirror/flip the image along its vertical/Y-axis.
3. 	`GenerateMipMaps` should generally be left as `true` unless the image/texture needs to retain maximum quality at all distances from the camera. 

	[Mipmaps](https://en.wikipedia.org/wiki/Mipmap) are a technique used to improve performance and reduce aliasing of objects at distance.

	You may also wish to disable mipmap generation to reduce video RAM consumption in constrained scenarios.

4. 	`InvertXRedChannel`, if set to `true`, will negate the red channel of the image. Likewise, `InvertYGreenChannel`, `InvertZBlueChannel`, and `InvertWAlphaChannel` will invert the green, blue, and alpha colour channels.

	Negation in this context means inverting the strength of the colour for each pixel; i.e. if a pixel had 100% strength of this colour it will now have 0%; if it had 80% it will have 20%; and so on.

	This is mostly useful when dealing with image maps that are defining things other than colour/albedo/diffuse values (such as ORM maps and normal maps). If the channel for a given value in such a map was exported with a reversed meaning, this parameter can be used to invert/reverse it back.

	For example, if we want to use a "metallic" map that defines `0f` as metallic and `1f` as non-metallic we will need to invert that value for use in TinyFFR.
	
	
	
	
	
	
	
	
	
Sometimes you may be dealing with assets that use [an older material model](https://en.wikipedia.org/wiki/Blinn%E2%80%93Phong_reflection_model) and you may be given a "specularity"/"specular" map.

Although there is no 1:1 conversion from a specularity map to a PBR model (which is what TinyFFR uses), you can import the specular map as an approximate roughness map by inverting it:

```csharp
using var roughnessTex = factory.AssetLoader.LoadTexture(
	@"Path\To\specular.bmp",
	new TextureCreationConfig {
		InvertXRedChannel = true,
		InvertYGreenChannel = true,
		InvertZBlueChannel = true,
	}
);
```











```csharp
var assLoad = factory.AssetLoader;

var textureMetadata = assLoad.ReadTextureMetadata(@"Path\To\tex.jpg"); // (1)!
var texelBuffer = factory.ResourceAllocator
	.CreatePooledMemoryBuffer<TexelRgb24>(textureMetadata.Width * textureMetadata.Height);

assLoad.ReadTexture(@"Path\To\tex.jpg", texelBuffer.Span); // (2)!

// Do stuff with texelBuffer here

// Optional: Load the texture on to the GPU with the material builder
using var colorMap = assLoad.MaterialBuilder.CreateTexture(
	texelBuffer.Span, 
	new TextureGenerationConfig { Height = textureMetadata.Height, Width = textureMetadata.Width}, 
	new TextureCreationConfig()
);

// Don't forget to return the rented buffer
factory.ResourceAllocator.ReturnPooledMemoryBuffer(texelBuffer);
```

1. `ReadTextureMetadata()` will tell you the width and height of the texture in texels. You can then use this data to allocate a texel buffer.
2. `ReadTexture()` will read a texture's texel data in to a preallocated buffer.
