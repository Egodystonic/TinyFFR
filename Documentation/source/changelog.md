---
title: Changelog
description: Abridged changelog for TinyFFR.
icon: material/source-branch-plus
search:
  exclude: true
---

## 0.7

__Github: [Issues](https://github.com/Egodystonic/TinyFFR/milestone/9?closed=1) | [Code](https://github.com/Egodystonic/TinyFFR/releases/tag/v0.7.0)__

### Major Features

* Added support for combined-asset resource files (such as `.gltf`, `.glb`, etc.)
* Enabled Vulkan for Windows & Linux; made it the default rendering API on those platforms.

### Improvements

* Added support for pixel picking (i.e. camera ray casting).
* Added support for orthographic camera projection.
* Greatly improved Linux stability. First-class support for Wayland (X11 support is removed for now).

### Bug Fixes

* Fixed mipmap generation not actually being applied in previous versions.

## 0.6

__Github: [Issues](https://github.com/Egodystonic/TinyFFR/milestone/6?closed=1) | [Code](https://github.com/Egodystonic/TinyFFR/releases/tag/v0.6.0)__

### Major Features

* Addition of three new material types:
	* **Simple** material: Just uses a colour/colourmap, bypasses lighting
	* **Standard** material: Support for:
		* Color/albedo/diffuse maps
		* Normal maps
		* Ambient Occlusion, Roughness, Metallic, Reflectance maps
		* Anisotropy maps
		* Emissive maps
		* Clearcoat maps
		* Alpha blending
	* **Transmissive** material: Support for:
		* Color/albedo/diffuse maps
		* Absorption, Transmission maps
		* Screenspace reflection, refraction
		* Normal maps
		* Ambient Occlusion, Roughness, Metallic, Reflectance maps
		* Anisotropy maps
		* Emissive maps
		* Alpha blending
		* Variable refractive thickness
* Added rudimentary per-object material effects:
	* When enabled, the following effects can be applied individually for any model instance's material:
		* Texture transform (scale, rotation, translation)
		* Blending between base color map and a blend target color map
		* Blending between base ORM/ORMR map and a blend target ORM/ORMR map
		* Blending between base emissive map and a blend target emissive map
		* Blending between base absorption/transmission map and a blend target absorption/transmission map

### Improvements

* `IAssetLoader` interface:
	* Unified texture processing interface (i.e. you can specify a `TextureProcessingConfig` for any texture being loaded/read; allowing you to swizzle, invert, or flip textures)
	* Can now inline load/parse texture combinations (up to four textures simultaneously) via `LoadCombinedTexture` / `ReadCombinedTexture`
	* Specialized functions for loading specific texture maps (e.g. `LoadColorMap()`, `LoadAnisotropyMap()`, etc.); often with useful parameters for combining image files or loading alternative formats
	* Much better layout of config structs in general
	* Methods that read textures or meshes now return the number of elements written to the destination span as a convenience
	* Provision of `BuiltInTexturePaths` property that provides some built-in greyscale textures and default maps for convenience
* `ITextureBuilder` interface:
	* This is a new builder added specifically to deal with building various textures and maps that TinyFFR uses
	* `TexturePatternPrinter` added- new static class that prints `TexturePattern`s to a span or bitmap
* Added ability to create icosphere meshes to `IMeshBuilder`
* Test material:
	* Improved test material (now shows a UV test pattern)
	* Must now be created, meaning resource lifetime is easier to reason about
* Added function to convert `Transform2D` to `Matrix3x2`
* Added option to set memory usage rubric in factory setup

## 0.5

__Github: [Issues](https://github.com/Egodystonic/TinyFFR/milestone/5?closed=1) | [Code](https://github.com/Egodystonic/TinyFFR/releases/tag/v0.5.0)__

### Improvements

* VSync control is now supported on Windows and Linux (MacOS support will come later).
* Improved handling of resolution / size changes of windows across fullscreen/borderless modes
* Replaced the ambiguous "Recommended" display property on the display discoverer with separate "HighestResolution" and "HighestRefreshRate" properties
* Texture patterns:
	* Normal map texture patterns now use unit spherical coordinates which are more intuitive than unit vectors for creating patterns
	* Texture pattern translation is now relative to total texture size (rather than pixel-based)
* Math:
	* Added `Triangularize`/`TriangularizeRectified` methods to `Angle` to help create triangle functions
	* Added `UnitSphericalCoordinate` to represent unit-length spherical coord (azimuthal + polar angle); can be converted to a `Direction` given two basis directions
	* Added interpolation type for `Angle` that interpolates through the shortest path around a circle

### Bug Fixes

* Fixed a build error with v0.4 that resulted in a `DllNotFoundException` on Linux or MacOS
* Switching between `Fullscreen` and `FullscreenBorderless` modes is now more reliable and the correct mode should be reported back via the `FullscreenStyle` property.
* Resolution modes of displays are now correctly reported on DPI-scaled Windows systems.
* Fixed a small resource leak that occurred when disposing a scene without first removing its backdrop.
* `UserQuitRequested` flag is now set when user requests a quit in a multi-window application.

## 0.4

__Github: [Issues](https://github.com/Egodystonic/TinyFFR/milestone/4?closed=1) | [Code](https://github.com/Egodystonic/TinyFFR/releases/tag/v0.4.0)__

### Major Features

* Integration added for popular .NET UI Frameworks:
	* WPF
	* WinForms
	* Avalonia
* Can now render directly to textures and/or capture screenshots on any Renderer
	* Can also render to a callback function that handles the texel buffer arbitrarily

### Improvements

* Support for sRGB colourspace added when loading textures
* Added `ImageUtils` static class with methods to convert spans of texels to bitmaps
* Added support for marshalling ref-struct-based config objects to/from heap

### Bug Fixes

* Fixed an issue where attempting to add model instances or light objects that were previously added to a now-disposed scene to a new scene would not actually add them.
* Fixed an issue where mipmaps were not being generated even when requested

## 0.3

__Github: [Issues](https://github.com/Egodystonic/TinyFFR/milestone/2?closed=1) | [Code](https://github.com/Egodystonic/TinyFFR/releases/tag/v0.3.0)__

### Major Features

* Added support for MacOS (ARM64 only); Linux (x64 Debian-based only)

### Improvements

* Made it possible to change window icon

### Bug Fixes

* Fixed a small bug with `BoundedRay` that could cause `NaN` or `Infinity` in resultant values when calculating closest point to zero-length ray (or when attempting to resize zero-length ray)

----

## 0.2

__Github: [Issues](https://github.com/Egodystonic/TinyFFR/issues?q=is%3Aissue%20milestone%3A%22Release%20v0.2%22%20) | [Code](https://github.com/Egodystonic/TinyFFR/releases/tag/v0.2.0)__

### Major Features

* Added new lighting models:
	* Spotlights
	* Sunlight/directional lights
	* Shadows
* Added alpha channel support to materials

### Improvements

* Closed some holes in the API that made it easy to accidentally leak disposed or re-used pooled/unmanaged memory

### Bug Fixes

* Added CPU/GPU synchronization (fixes stuttering when using `null` framerate cap)
* Fixed some image types not being loadable as textures (incorrect usage of `stb_image` header)
* Fixed incorrect import of mesh data in multi-mesh files
* Fixed memory leak with resource naming
