---
title: Changelog
description: Abridged changelog for TinyFFR.
icon: material/source-branch-plus
search:
  exclude: true
---

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