---
title: Homepage
description: This is the homepage/manual for Tiny FFR (C# Tiny Fixed Function Rendering Library).
icon: fontawesome/solid/house
---

# 3D Rendering in C\# Made Easy

![Rotating Cube](tffrcube.webp){ align=right : style="height:200px;width:200px;border-radius:12px"}

TinyFFR (*Tiny* *F*ixed *F*unction *R*enderer) is a C# .NET9 library designed to help you render things in 3D:

* Delivered via [NuGet](https://www.nuget.org/packages/Egodystonic.TinyFFR/)
* Free for commercial and non-commercial use ([see license](https://github.com/Egodystonic/TinyFFR/blob/main/LICENSE.md))
* Physically-based rendering (via [filament](https://github.com/google/filament))
* Asset loading (via [assimp](https://github.com/assimp/assimp) and [stb_image](https://github.com/nothings/stb))
* Window management and input handling (via [SDL](https://github.com/libsdl-org/SDL))
* Fully-abstracted math & geometry API - no pre-existing 3D or linear algebra knowledge required
* Zero-GC design

## Key Features

<div class="grid cards" markdown>

-   :material-puzzle:{ .lg .middle : style="margin-right:0.3em" } __Lightweight__

    ---

    TinyFFR is primarily designed for C#/.NET programmers that want to render 3D scenes or objects without needing to integrate a game engine or write against a raw graphics API.

-   :material-shape-polygon-plus:{ .lg .middle : style="margin-right:0.3em" } __Integrated Asset Loading__

    ---

    TinyFFR makes it easy to load assets such as images, 3D models, textures and HDR cubemaps by integrating popular open-source asset loading libraries such as `assimp` and `stb_image`. 

-   :fontawesome-solid-window-restore:{ .lg .middle : style="margin-right:0.3em" } __Window & Input Handling__

    ---

    TinyFFR helps you discover connected displays, create & manage windows, and capture/process user input with a rich API that supports keyboard, mouse, and gamepad.

-   :octicons-device-camera-video-16:{ .lg .middle : style="margin-right:0.3em" } __Easy Scene Building__

    ---

    TinyFFR provides ways to quickly organise and build 3D scenes (including lights, objects, and backdrops) all with a few lines of code. The library does not require writing shader code or even understanding modern material models. 

-   :material-vector-polygon:{ .lg .middle : style="margin-right:0.3em" } __No Math Required__

    ---

    TinyFFR comes integrated with an abstracted math API that uses plain-English terminology. No prior knowledge of 3D rendering or linear algebra is required.

-   :fontawesome-solid-stopwatch-20:{ .lg .middle : style="margin-right:0.3em" } __Zero-GC Design__

    ---

    TinyFFR is designed from the ground-up as a non-garbage-generating library; the API works primarily with struct-based handles around unmanaged or pooled resources, resulting in a zero-jitter, smooth rendering experience.

</div>  

## Is TinyFFR For Me?

* __:fontawesome-solid-info: TinyFFR is just a renderer.__
    * <span class="tffr-affirmative">:octicons-check-16:</span> Consider TinyFFR if you don't need "game engine" features such as physics, audio, and level editing or you wish to add your own implementations for those functionalities yourself. 
    * <span class="tffr-negative">:octicons-x-12:</span> TinyFFR may not be for you if you need everything a modern game engine provides and you're not willing or able to add those features using other libraries.

* __:fontawesome-solid-info: TinyFFR is currently in very early prerelease.__ 
    * <span class="tffr-affirmative">:octicons-check-16:</span> Consider TinyFFR if you're okay with using a library that may be missing key features or have performance issues and bugs at this early stage. 
    * <span class="tffr-negative">:octicons-x-12:</span> TinyFFR may not be for you if you need a mature, battle-tested offering.

* The "FFR" in TinyFFR stands for *F*ixed *F*unction *R*enderer. TinyFFR doe

# Where to Start

* Learn by Example
* Concepts
* Reference Documentation