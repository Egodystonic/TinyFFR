---
title: What's New (Changelog)
description: Abridged changelog for TinyFFR.
search:
  exclude: true
---

## 0.4

__Github: [Issues](https://github.com/Egodystonic/TinyFFR/milestone/4?closed=1) | [Code](https://github.com/Egodystonic/TinyFFR/releases/tag/v0.4.0)__

### Major Features

* Integration added for popular .NET UI Frameworks:
	* WPF
	* WinForms
	* Avalonia
* Can now render directly to textures and/or capture screenshots on any Renderer
	* Can also render to a callback function that handles texel streams

### Improvements

* Support for sRGB colourspace added when loading textures
* Added support for serializing ref-struct-based config objects to/from heap

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