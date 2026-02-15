# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## General Code Guidelines

* Do not add comments to code unless doing something unusual or non-conventional.
* Avoid code that create GC pressure in the main library code (TinyFFR). Generating garbage is absolutely fine in the test projects.

## Project Overview

TinyFFR (Tiny Fixed Function Renderer) is a C#/.NET 9 rendering library providing physically-based rendering (PBR) via Google's Filament engine. It targets developers who want simple 3D rendering without needing a full game engine. Cross-platform: Windows (x64), Linux (x64), macOS (ARM64).

### Build Third-Party Dependencies (first time only)
To rebuild a single dependency (e.g. assimp debug):
```bash
cd ThirdParty && dotnet run build_and_copy_all_third_party.cs -- assimp debug
```

### Build Native Library (macOS/Linux)
To rebuild first-party native library (e.g. debug):
```bash
cd TinyFFR.Native/build && dotnet run build.cs -- debug
```
On Windows, build TinyFFR.Native through Visual Studio.

### Build C# Projects
```bash
dotnet build TinyFFR.slnx -c Debug
```

### Run Tests
```bash
dotnet test Testing/TinyFFR.Tests
```
Run a single test:
```bash
dotnet test Testing/TinyFFR.Tests --filter "FullyQualifiedName~TestClassName.TestMethodName"
```
Integration tests are marked `[Explicit]` and require manual invocation. Do not run these.

## Architecture

### Core Library (`TinyFFR/`)
- **Factory/**: `LocalTinyFfrFactory` is the singleton entry point. Only one instance can exist at a time. Provides builders for all resource types (Camera, Light, Object, Scene, Renderer, Mesh, Material).
- **Math/**: Immutable readonly structs for 3D math (Location, Direction, Vect, Angle, Rotation, Transform). Custom operator overloading: `%` for rotations, `^` for angle magnitude, `>>` for location transitions.
- **Assets/**: Asset loading via Assimp (models) and stb_image (textures). Materials use PBR configs (Simple, Standard, Transmissive).
- **Rendering/**: Filament-backed renderer. `LocalRendererBuilder` creates renderers. `RenderOutputBuffer` for off-screen rendering.
- **World/**: Scene graph with ModelInstance, ModelInstanceGroup, lights (Point/Directional/Spot), cameras.
- **Environment/**: SDL2-based window management, input handling (keyboard/mouse/gamepad), application loop.
- **Resources/**: Handle-based resource lifetime management. `ResourceHandle<T>` wraps native resources. `ResourceGroup` for grouped disposal.

### Native Layer (`TinyFFR.Native/`)
C++20 code built with CMake. Bridges C# to Filament, SDL2, and Assimp via P/Invoke. Entry points: `on_factory_build()`, `on_factory_teardown()`, `native_impl_init` class, etc.

### Implementation Provider Pattern
Public resource types are structs containing a `ResourceHandle<T>` and an `IXxxImplProvider` interface. This separates the public API from internal implementation and enables unit testing via NSubstitute mocking without heavy abstractions.

### Integration Projects (`Integrations/`)
Embed TinyFFR rendering into UI frameworks (WPF, Avalonia, WinForms). Each provides a custom control that renders to an output buffer and copies to the framework's texture system.

## Key Design Conventions

- **Immutable by default**: All public math types are readonly structs. Mutation quarantined to internal/private classes.
- **Zero-GC**: Use `Span<T>` over `IEnumerable<T>`, ArrayPool-backed collections, struct configs passed by `in` reference.
- **Modifier methods use past-participle**: `.RotatedBy()`, `.ProjectedOnTo()`, `.ReflectedBy()`.
- **`ToXyz()` for transformations, `AsXyz()` for reinterpretations**.
- **`Equals()` for structural equality, `IsEquivalent...To()` for mathematical equivalence**.
- **`With()` methods when mutation ordering matters; `init` properties when it doesn't**.
- **Constructors for intrinsic parameters; static factory methods for derived constructions** (e.g., `Rotation.FromStartAndEndDirection()`).
- **Degenerate inputs**: Return nullable when invalid input is natural; throw only for clear API misuse. Offer `Fast` variants that skip validation.
- **Floating point**: Clamp/correct FP inaccuracies proactively. Implement `IToleranceEquatable<T>` for new numeric types.
- **No "Entity" in public API** to avoid conflicting with user ECS implementations.
- **US English** for all identifiers and documentation.

## Testing Conventions

- NUnit 3 with NSubstitute for mocking.
- Unit tests: fast, parallelizable, automatable.
- Integration tests: `[Explicit]` attribute, manual/human-verified.
- When adding new resource types, update integration tests that verify disposal and dependency protections.

## Build Configurations

- **Debug**: No warnings-as-errors, minimal analysis.
- **Release**: Warnings as errors, full analysis.
- **Optimized**: Release optimizations without strict error treatment.
- **Platforms**: x64, ARM64.
- **Solution format**: Modern `.slnx`.

## Documentation

Additional documentation in markdown format is in the `Documentation` folder. Consult this if necessary for additional context.
