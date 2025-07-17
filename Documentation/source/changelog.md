---
title: What's New (Changelog)
description: Abridged changelog for TinyFFR.
search:
  exclude: true
---

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