---
title: Homepage
description: This is the homepage/manual for Tiny FFR (C# Tiny Fixed Function Rendering Library).
icon: material/home
template: home.html
comments: false
search:
  exclude: true
hide:
  - toc
  - footer
---

<div style="font-size: 1.3em; position: relative; top: -1em; left: 0.5em;"><image src="logo.png" style="max-height: 24px; margin: 0px; position: relative; top: 0.2em; right: 0.2em; margin-right: 12px;" />TinyFFR is a <b>free</b> realtime 3D rendering library for modern .NET delivered via NuGet:</div>







<div class="tffr-showcase" markdown>

![](index_category_4.jpg){ .tffr-showcase__backdrop }

<div class="tffr-showcase__category" markdown>

:material-weather-sunset:{ .tffr-showcase__icon }

Built-in Beauty
{ .tffr-showcase__title }

Full PBR workflow :material-drag-vertical-variant: Post-processing enabled out-of-the-box
{ .tffr-showcase__tagline }

</div>

<div class="tffr-showcase__cards">
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_pbr.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">PBR Material System</p>
<p>Create materials with built-in support for various channels: Albedo, Normal, Ambient Occlusion, Rougness/Metallic, Reflectance, Absorption, Transmission, Emissive, Anisotropy, and Clearcoat.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_post_processing.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Ready-Made Post-Processing</p>
<p>Add screen-space refractivity + reflections, distance fog, bloom, shadowing, depth-of-field, and antialiasing; all with a single API call.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_versatile.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Versatile Rendering</p>
<p>Built-in support for picture-in-picture, splitscreen, render-to-texture, CPU readback streaming, screenshot capture, and scene compositing.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_shaders.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">No Shader Knowledge Required</p>
<p>Don't write a single shader. TinyFFR's build comes with pre-compiled shaders for each platform ready to support every texture/material combination you create.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_asset_import.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Effortless Asset Import</p>
<p>Support for tens of popular asset formats (incl. glTF, FBX, OBJ) including skeletal animations, all common texture formats, HDR/EXR backdrops, fonts, and more. Each loadable with a single line of C#.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_import_pipeline.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Customizable Import Pipeline</p>
<p>Rescale, translate, optimise, and error-correct imported meshes. Transform, swizzle, negate, compress, and process imported textures. Generate mip-maps, combine textures, and modify bounding boxes on the fly.</p>
</div>
</div></div>
</div>

</div>






<div class="tffr-showcase" markdown>

![](index_category_1.jpg){ .tffr-showcase__backdrop }

<div class="tffr-showcase__category" markdown>

:material-speedometer:{ .tffr-showcase__icon }

First-Class Performance
{ .tffr-showcase__title }

SIMD-enabled :material-drag-vertical-variant: Parallelized :material-drag-vertical-variant: Zero-GC Design
{ .tffr-showcase__tagline }

</div>

<div class="tffr-showcase__cards">
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_backend.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">State-of-the-Art Backend</p>
<p>Froxel forward lighting, cascaded shadows, and frustum culling with automatic bounding box calculation.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_memory_profile.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Flatline Memory Profile</p>
<p>Designed from day one to create <i>zero</i> GC pressure; from asset loading to the rendering loop. Backed up with a benchmark suite and profiling.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_quality.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Quality Control</p>
<p>Configure quality with a single API call; or use one of the built-in presets designed to target all ranges of hardware.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_compression.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Texture Compression</p>
<p>Rein in your VRAM usage with built-in texture compression.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_aot.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">AoT Ready</p>
<p>TinyFFR avoids any dynamic or runtime resource access; meaning consuming applications can enable AoT compilation.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_simd.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">SIMD Math Library</p>
<p>Every math primitive uses a fully SIMD-enabled abstracted plain-English API and can be converted to raw Vector/Matrix form for custom SIMD processing.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_low_level.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Low-Level Tweaking</p>
<p>For when you need to go deeper: Options exposed for controlling frame latency, rate limiting, FPS capping + measurement, vsync control, async load sharing, and more.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_fsr.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">FSR Upscaling</p>
<p>Enable AMD FidelityFX FSR upscaling with a single property edit.</p>
</div>
</div></div>
</div>

</div>






<div class="tffr-showcase" markdown>

![](index_category_2.jpg){ .tffr-showcase__backdrop }

<div class="tffr-showcase__category" markdown>

:material-battery-heart:{ .tffr-showcase__icon }

Batteries Included
{ .tffr-showcase__title }

Camera Control :material-drag-vertical-variant: Text Rendering :material-drag-vertical-variant: Animations :material-drag-vertical-variant: Hit-Testing
{ .tffr-showcase__tagline }

</div>

<div class="tffr-showcase__cards">
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_text.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">SDF Text Rendering</p>
<p>Import your own TTF files or use one of three built-in fonts. Render text that looks sharp at any size &amp; distance with optional outlines or background colours and multi-line text justification control.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_ibl.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Image-Based Lighting</p>
<p>Import an HDRi and get image-based lighting across your entire scene.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_skeletal.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Skeletal Animation Players</p>
<p>Enumerate packed animation data and play animations with target time frames, speed multipliers, blending, and node transform calculations.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_camera_control.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Camera Control</p>
<p>Manually set properties such as FoV, focus depth, exposure (aperture, shutter-speed, sensitivity) and use provided higher-level controllers; including first-person, follow/chase, free-flying, inspection, orbital, PTZ, and programmable keyframe types.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_texture_builders.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Texture Builders</p>
<p>Programmatically construct texture maps or use the built-in UV test material. Use texture pattern generators to create grids, circles, squares, chequerboards, and more.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_material_effects.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Material Effects</p>
<p>Transform and blend textures on any object in realtime; or control emissive strength for easy lighting simulation effects.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_vertex_mutation.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Vertex Mutation</p>
<p>Modify mesh vertices in realtime. Create dynamic VB/IB buffers and manage mesh views. Specialized API provided for mutable grids.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_billboarding.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Billboarding</p>
<p>Create quads and text instances in-world that stay facing the camera.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_canvas.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Canvas Scenes</p>
<p>Easily create 2D overlays, HUDs, or debug readouts for either your entire window or a target texture for in-world screens.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_input.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Simple Input</p>
<p>Mouse, keyboard, and game controller support out of the box; with a shared API between standalone and UI-framework-integrated scene views.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_primitives.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Primitive Rendering</p>
<p>Add basic objects to your scene without needing material or mesh definitions. Render wireframe data with optional face clearing.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_scene_queries.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Scene Queries</p>
<p>Query which object is under the cursor, or test what gets intersected by rays, cuboids, or spheres.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_math.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Plain-English Math</p>
<p>No trigonometry, linear algebra, or matrices required. Use plain-English APIs like "AngleBetween", "RotatedBy", "PointClosestTo", "DistanceFrom"; and shapes types like Sphere, Plane, Line, and more.</p>
</div>
</div></div>
</div>

</div>




<div class="tffr-showcase" markdown>

![](index_category_3.jpg){ .tffr-showcase__backdrop }

<div class="tffr-showcase__category" markdown>

:material-package-variant-closed:{ .tffr-showcase__icon }

Professionally Packaged
{ .tffr-showcase__title }

Avalonia, WPF, WinForms &amp; ImGui Integration :material-drag-vertical-variant: Cross-Platform :material-drag-vertical-variant: Robust Testing 
{ .tffr-showcase__tagline }

</div>

<div class="tffr-showcase__cards">
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_modern_dotnet.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Modern .NET</p>
<p>No interpreters, additional runtimes, external CLR hosts or rewriting compilers. Just a single NuGet package that supports every .NET version from 9+ (including the latest).</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_xplat.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Cross-Platform</p>
<p>Support for Windows, MacOS (Apple Silicon) and Linux (Debian/Ubuntu).</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_ui_integrations.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Avalonia, WPF, WinForms Integrations</p>
<p>A specialized control for each of Avalonia, WPF, and Windows Forms makes it easy to render 3D realtime scenes inside your existing applications.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_imgui.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">ImGui Integration</p>
<p>Support for Dear ImGui means you can build rich UIs without needing a framework at all.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_source.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Source Linked & Debug Symbols Embedded</p>
<p>The main library and all satellite libraries are debuggable from inside your IDE, with full external sources enabled.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_documentation.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Class-Leading Documentation</p>
<p>Every public API member documented; backed up by tutorials, example applications, and reference docs online. All actually written and reviewed by a real human.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_tested.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">Fully Tested</p>
<p>15,000 assertions, 50+ integration tests, 1.4K unit tests and growing.</p>
</div>
</div></div>
<div class="tffr-showcase__card"><div class="tffr-showcase__card-inner">
<img src="index_history.jpg" alt="" loading="lazy">
<div>
<p class="tffr-showcase__card-title">3 Years of History</p>
<p>No vibe-coded AI slop here. TinyFFR has been in development since before the AI era and was started as a passion project by a C#/.NET veteran with a diverse background.</p>
</div>
</div></div>
</div>

</div>
