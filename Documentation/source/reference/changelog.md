---
title: Changelog
description: Abridged changelog for TinyFFR.
search:
  exclude: true
---

## 0.2

__Github: [Issues](https://github.com/Egodystonic/TinyFFR/issues?q=is%3Aissue%20milestone%3A%22Release%20v0.2%22%20) | [Code](https://github.com/Egodystonic/TinyFFR/releases/tag/v0.2.0)__

### Features

* Added transparent material support
* Added new lighting models:
	* Spotlights
	* Sunlight/directional lights
	* Shadows

### Bug Fixes

* Fixed issue with gaming laptops crashing when initializing renderer due to hybrid GPU swapping
* Added CPU/GPU synchronization (fixes stuttering when using `null` framerate cap)
* Fixed some image types not being loadable as textures (incorrect usage of `stb_image` header)
* Fixed incorrect import of mesh data in multi-mesh files
