---
title: Changelog
description: Abridged changelog for TinyFFR.
icon: material/source-branch-plus
search:
  exclude: true
hide:
  - footer
---

## <span style="font-size: 1.2em; font-weight: bold;">1.0</span> <span style="font-size: 0.7em; opacity: 0.5;">:material-calendar: Oct 2026</span>

__:simple-github: [Issues](https://github.com/Egodystonic/TinyFFR/milestone/8?closed=1) | [Code](https://github.com/Egodystonic/TinyFFR/releases/tag/v1.0.0)__

### Major Features

* Added scene compositing: multiple scenes/cameras can be layered, split-screened or picture-in-pictured into one output, each with its own viewport
	* Composited layers can be individually framerate-limited
	* Compositors can target a window or a render output buffer
* Added customizable viewports for renderers
* Added Metal support on MacOS
* Added text rendering: fonts (TTF loading plus three built-in DejaVu fonts) rendered as SDF atlases, with text instances, pens and strings, justification and layout
* Added 2D canvas scenes for HUDs and overlays (images, text, shapes, anchoring), including canvas scene querying
* Added a Dear ImGui integration (`TinyFFR.ImGui`), with gamepad, clipboard and viewport support
* Added asynchronous loading for all asset types, with configurable per-frame time sharing, plus a TinyFFR synchronization context so `await` resumes on the primary thread
* Added the asset bakery: pre-bake meshes, textures, backdrops, fonts and whole model bundles into a fast-loading format (roughly 10–1000x faster to load depending on content)
* Added GPU texture compression (BC formats)
* Added CPU-controlled vertex mutation for non-skeletal meshes (`DynamicVertexBuffer`), plus mutable grid meshes
* Added scene primitives for debug/visualisation drawing (lines, rays, arrows, shapes, wireframes), plus arrow meshes
* Added scene querying (ray and shape intersection tests against scene contents) and improved GPU pixel picking, including sub-area picking
* Added camera-locked objects (HUD-style objects fixed relative to the camera, with several lock and scaling styles)
* Added fog
* Added headless rendering mode (render without creating any window or SDL context)
* Added model bundles and mesh groups (`LoadMeshGroup()`), including shared animation tables across many instances
* Added two new material types:
	* **Lighting-ignoring** material (replaces the old **Simple** material)
	* **Color-keyed** material

### Improvements

* Rendering:
	* Many more render quality controls, including depth of field and strength controls for DoF, bloom and ambient occlusion
	* Per-renderer framerate limits
	* Reworked lighting and exposure so they are more explicit, with real-world light defaults and named presets
	* Transmissive quality switch for transmissive materials on loaded models
	* Projection from world space to screen space
	* Big reduction in VRAM garbage when rendering headlessly or disposing render targets
	* Sharper, steadier directional-light shadows in large scenes, plus per-light `MaxShadowDistance` and optional shadow stabilization
* Assets:
	* Faster asset loading in general
	* Better ergonomics for texture combination methods
	* Building textures from pre-compressed data
	* Better emissive import from model files (four-channel emissive textures are no longer required)
	* Axis-aligned bounding box and bounding sphere support for meshes, with merging of AABBs
* Scenes & objects:
	* Added `SceneObject`, a single type that can represent any object kind; `Scene.Add`/`Remove` accept it, plus range add/remove
	* Added `SetSize()` for scene objects
	* Quad meshes
	* Resource groups now support specialized resources (text, canvas items, etc.)
	* Canvases now own their canvas items
* Camera controllers:
	* Better inspector camera controller
	* Pivot and focus point for the programmed camera controller
	* Convenience APIs on all camera controllers; default mouse sensitivity doubled
	* Standardized camera range overload on the camera builder
* UI framework integrations (WPF, WinForms, Avalonia):
	* Shared input abstraction across frameworks
	* Large performance improvements (faster RGBA readback path)
	* Control over maximum internal render resolution
* Input & windows:
	* Clipboard access API
	* Mouse cursor styles
	* Updated default window icon
* Performance & memory:
	* Benchmark-driven allocation work: most hot-path APIs now allocate nothing once warm
	* New internal array pool to reduce GC pressure
	* `IResourceAllocator` exposes span leases and shared scratch collections for user code
	* Larger internal handle arena so the most complex models fit
	* Converted internal native memory allocator to a slab allocator for better memory usage performance
* Fully XML-documented public API, plus a completely rewritten documentation site with tutorials and showcase example applications

### Bug Fixes

* Fixed black squares flickering under sharpening TAA with bloom (cherry-picked FSR/bloom fix)
* Fixed swapchain flickering on Windows
* Fixed a crash on Windows when a window was minimized or dragged to zero width/height
* Fixed inverted render output buffer contents on non-OpenGL backends
* Fixed screenshot capture issues; screenshots now render warm-up frames so SSR/TAA show correctly
* Fixed switching from the wireframe renderer back to the full material renderer not restoring the correct mesh geometry
* Fixed bounding box issues with skeletal meshes (bounds now come from the bind pose)
* Fixed a glTF import issue
* Fixed a native buffer double-free
* Fixed segfaults caused by passing resources from a different factory to some APIs
* Fixed the `Optimized` build configuration not actually enabling optimizations

## <span style="font-size: 1.2em; font-weight: bold;">0.8</span> <span style="font-size: 0.7em; opacity: 0.5;">:material-calendar: May 2026</span>

__:simple-github: [Issues](https://github.com/Egodystonic/TinyFFR/milestone/7?closed=1) | [Code](https://github.com/Egodystonic/TinyFFR/releases/tag/v0.8.0)__

### Major Features

* Added camera controllers
* Enabled frustum culling by default, auto-calculate axis-aligned bounding box for all geometry
* Added control for camera exposure
* Added resource directory (allowing finding active resources at runtime)

### Improvements

* Added FPS metrics to application loop
* Added translation / rotation for shapes
* Added ability to generate random locations inside convex shapes
* Improved error messages when exceeding internal buffer size
* Made it possible to create mock resources for testing
* (0.8.1) Increased internal handle allocator size to improve speed of loading complex models

## <span style="font-size: 1.2em; font-weight: bold;">0.7</span> <span style="font-size: 0.7em; opacity: 0.5;">:material-calendar: Mar 2026</span>

__:simple-github: [Issues](https://github.com/Egodystonic/TinyFFR/milestone/9?closed=1) | [Code](https://github.com/Egodystonic/TinyFFR/releases/tag/v0.7.0)__

### Major Features

* Added support for loading entire models from combined-asset resource files (such as `.gltf`, `.glb`, etc.)
* Added support for skeletal mesh animations (e.g. vertex skinning); including node transform tracking & animation blending
* Enabled Vulkan for Windows & Linux; made it the default rendering API on those platforms

### Improvements

* Added support for pixel picking (i.e. camera ray casting)
* Added support for orthographic camera projection
* Greatly improved Linux stability. First-class support for Wayland (X11 support is removed for now).
* Improved loading speed for skybox textures on Linux
* Added built-in scene backdrops

### Bug Fixes

* Fixed mipmap generation not actually being applied in previous versions

## <span style="font-size: 1.2em; font-weight: bold;">0.6</span> <span style="font-size: 0.7em; opacity: 0.5;">:material-calendar: Dec 2025</span>

__:simple-github: [Issues](https://github.com/Egodystonic/TinyFFR/milestone/6?closed=1) | [Code](https://github.com/Egodystonic/TinyFFR/releases/tag/v0.6.0)__

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

## <span style="font-size: 1.2em; font-weight: bold;">0.5</span> <span style="font-size: 0.7em; opacity: 0.5;">:material-calendar: Nov 2025</span>

__:simple-github: [Issues](https://github.com/Egodystonic/TinyFFR/milestone/5?closed=1) | [Code](https://github.com/Egodystonic/TinyFFR/releases/tag/v0.5.0)__

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

## <span style="font-size: 1.2em; font-weight: bold;">0.4</span> <span style="font-size: 0.7em; opacity: 0.5;">:material-calendar: Oct 2025</span>

__:simple-github: [Issues](https://github.com/Egodystonic/TinyFFR/milestone/4?closed=1) | [Code](https://github.com/Egodystonic/TinyFFR/releases/tag/v0.4.0)__

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

## <span style="font-size: 1.2em; font-weight: bold;">0.3</span> <span style="font-size: 0.7em; opacity: 0.5;">:material-calendar: Jul 2025</span>

__:simple-github: [Issues](https://github.com/Egodystonic/TinyFFR/milestone/2?closed=1) | [Code](https://github.com/Egodystonic/TinyFFR/releases/tag/v0.3.0)__

### Major Features

* Added support for MacOS (ARM64 only); Linux (x64 Debian-based only)

### Improvements

* Made it possible to change window icon

### Bug Fixes

* Fixed a small bug with `BoundedRay` that could cause `NaN` or `Infinity` in resultant values when calculating closest point to zero-length ray (or when attempting to resize zero-length ray)

----

## <span style="font-size: 1.2em; font-weight: bold;">0.2</span> <span style="font-size: 0.7em; opacity: 0.5;">:material-calendar: May 2025</span>

__:simple-github: [Issues](https://github.com/Egodystonic/TinyFFR/issues?q=is%3Aissue%20milestone%3A%22Release%20v0.2%22%20) | [Code](https://github.com/Egodystonic/TinyFFR/releases/tag/v0.2.0)__

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

## <span style="font-size: 1.2em; font-weight: bold;">0.1</span> <span style="font-size: 0.7em; opacity: 0.5;">:material-calendar: Mar 2025</span>

Initial release
