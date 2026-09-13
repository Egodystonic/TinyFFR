# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## General Code Guidelines

* Do not add comments to code ever.
* Avoid code that create GC pressure in the main library code (TinyFFR). Generating garbage is absolutely fine in the test projects.
* The benchmarking projects under `Testing/Benchmarks` are the exception to the above: their scaffolding must generate no GC pressure at all, because BenchmarkDotNet's `MemoryDiagnoser` would otherwise report the harness's allocations instead of the library's. Use `IResourceAllocator.GetSharedScratchList/Dictionary/Set` or `CreatePooledMemoryBuffer` for scratch storage, function-pointer callback overloads rather than lambdas, and pre-resolved `static readonly` fields for asset paths and texture patterns.
* Don't try to run any tests marked as [Explicit]. These tests often spin up windows on the desktop and expect user interaction for verification.

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

### Run Benchmarks
Benchmarks live in `Testing/Benchmarks/TinyFFR.Benchmarks`, one folder per benchmark. Full usage is in `Testing/Benchmarks/README.md`. They must be run in Release:
```bash
dotnet run --project Testing/Benchmarks/TinyFFR.Benchmarks -c Release -p:Platform=x64
```
`--job dry` (1 warmup + 1 iteration) is the quick pass while iterating; `--job standard` is the default; `--job long` tightens confidence intervals. Run a subset with `--filter "*RenderFrame*"` — note BenchmarkDotNet wants multiple globs after a *single* `--filter`, not one switch per glob.

The results table includes an `Alloc Rate` column (`Allocated / Mean`, managed MB per second) — the proxy for GC pressure. BenchmarkDotNet's `Gen0/1/2` columns exist but are near-useless here: BDN forces a GC around every iteration, so with `InvocationCount=1` every operation starts with a reset Gen0 budget and anything allocating less than one budget per ~100ms operation reports `-` regardless of the churn it would cause in a real loop. `Alloc Rate` never quantises to zero. Use it to rank sections, `--probe-alloc` to find the cause, and `--soak` for what the GC actually does unforced.

The results table includes an `Allocated` column (process-wide managed bytes per invocation). It carries a small noise floor (~4-5 KB as of 2026-09-08, down from ~21 KB): a Gen2 trim callback is registered per pooled element type and allocates ~336 B per GC, and BenchmarkDotNet forces a GC around every measurement. Most of that floor disappeared when the collections moved to `TinyFfrArrayPool<T>` (`Resources/Memory/TinyFfrArrayPool.cs`), which registers no such callback; what remains comes from `ArrayPool<T>.Shared`, still used for buffers above `TinyFfrArrayPool.LargeBufferThresholdBytes`. That floor is not TinyFFR. Treat the column as a relative signal - section ordering is fixed, so a section carries the same floor every run - and do not read a few KB on a should-be-zero section as a regression.

Pass `--probe-alloc [name]` to measure exact per-operation allocation for a library call, isolated from the results table's noise floor (per-thread `GC.GetAllocatedBytesForCurrentThread`, 3 warmup then min of 5). Use it whenever an `Allocated` figure needs explaining — a section can report hundreds of KB while every operation in it probes at 0 B, which means it is measuring retained memory (collections growing as it holds resources live), not a per-call cost. Probe groups live in `Smoke/SmokeProbes.cs`.
```bash
dotnet run --project Testing/Benchmarks/TinyFFR.Benchmarks -c Release -p:Platform=x64 -- --probe-alloc LoadBakedAssets
```

Pass `--inspect [name]` to run each Smoke section once against a real window for visual checking (works in Debug too):
```bash
dotnet run --project Testing/Benchmarks/TinyFFR.Benchmarks -c Debug -p:Platform=x64 -- --inspect Smoke
```
Pass `--soak [minutes]` for the Soak benchmark: a persistent scene rendered in a continuous loop, sampling allocation and GC counts every 15s and reporting whether allocation per frame trends upward. It runs *outside* BenchmarkDotNet precisely so no GC is ever forced, which makes it the right tool for drift, leak and pool-behaviour questions that Smoke structurally cannot answer.
```bash
dotnet run --project Testing/Benchmarks/TinyFFR.Benchmarks -c Release -p:Platform=x64 -- --soak 10
```

Sections are deliberately sized to take at least ~100ms so their measurements are precise and non-linear-complexity algorithms surface; all the sizes live in `Smoke/SmokeWorkload.cs`.

## Architecture

### Core Library (`TinyFFR/`)
- **Factory/**: `LocalTinyFfrFactory` is the singleton entry point. Only one instance can exist at a time. Provides builders for all resource types (Camera, Light, Object, Scene, Renderer, Mesh, Material).
- **Math/**: Immutable readonly structs for 3D math (Location, Direction, Vect, Angle, Rotation, Transform). Custom operator overloading: `%` for rotations, `^` for angle magnitude, `>>` for location transitions.
- **Assets/**: Asset loading via Assimp (models), stb_image (textures) and stb_truetype (fonts, rendered to SDF atlases). Materials use PBR configs (LightingIgnoring, ColorKeyed, Standard, Transmissive).
- **Rendering/**: Filament-backed renderer. `LocalRendererBuilder` creates renderers. `RenderOutputBuffer` for off-screen rendering.
- **World/**: Scene graph with ModelInstance, ModelInstanceGroup, lights (Point/Directional/Spot), cameras.
- **Environment/**: SDL2-based window management, input handling (keyboard/mouse/gamepad), application loop.
- **Resources/**: Handle-based resource lifetime management. `ResourceHandle<T>` wraps native resources. `ResourceGroup` for grouped disposal.

### Native Layer (`TinyFFR.Native/`)
C++20 code built with CMake. Bridges C# to Filament, SDL2, and Assimp via P/Invoke. Entry points: `on_factory_build()`, `on_factory_teardown()`, `native_impl_init` class, etc.

### Implementation Provider Pattern
Public resource types are structs containing a `ResourceHandle<T>` and an `IXxxImplProvider` interface. This separates the public API from internal implementation and enables unit testing via NSubstitute mocking without heavy abstractions.

### Integration Projects (`Integrations/`)
Embed TinyFFR rendering into UI frameworks (WPF, Avalonia, WinForms). Each provides a custom control that renders to an output buffer and copies to the framework's texture system. `TinyFFR.ImGui` inverts this: TinyFFR acts as Dear ImGui's platform and renderer backend, drawing ImGui draw data over a scene.

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
Do not modify the documentation in the `Documentation` folder or add/modify XMLDoc without permission-- this will often be the last step after validation of a session's output. Writing/changing documentation before final validation is wasteful as the changeset will likely be in churn.
