# TinyFFR Benchmarks

This folder contains performance benchmarks for TinyFFR. They measure how long library operations take and
how much memory they allocate, so that performance regressions show up as a number changing rather than as
someone noticing the renderer feels sluggish six months later.

Benchmarks are built on [BenchmarkDotNet](https://benchmarkdotnet.org/). You do not need to know anything
about BenchmarkDotNet to run them — everything you need is below.

## Quick start

```bash
dotnet run --project Testing/Benchmarks/TinyFFR.Benchmarks -c Release -p:Platform=x64
```

That runs every benchmark and prints a results table. It takes several minutes.

While iterating, this is usually what you want instead — same benchmarks, far fewer repetitions:

```bash
dotnet run --project Testing/Benchmarks/TinyFFR.Benchmarks -c Release -p:Platform=x64 -- --job dry
```

### Release is mandatory

`-c Release` is not optional. In Debug the compiler skips optimisations that the real library ships with,
so the numbers would describe code nobody runs. BenchmarkDotNet detects this and refuses to run rather than
report misleading results. (The one exception is `--inspect` below, which does not measure anything and
therefore works fine in Debug.)

`-p:Platform=x64` is needed because the solution defaults to ARM64 on x64 machines.

## Reading the results table

```
| Method           | Mean      | Error     | StdDev    | Median    |
|----------------- |----------:|----------:|----------:|----------:|
| ProceduralMeshes | 105.32 ms |  2.114 ms |  0.115 ms | 105.28 ms |
```

* **Method** — the benchmark section that was measured.
* **Mean** — average time for one invocation. This is the number you compare against a previous run.
* **Error** — half of the 99.9% confidence interval. Treat it as the margin of error on `Mean`. **If `Error`
  is anywhere near as large as `Mean`, the measurement is noise and you should not draw conclusions from
  it.** A healthy row has an `Error` far smaller than its `Mean`.
* **StdDev** — how much individual measurements varied. Large values mean something inconsistent is
  happening (background load, GPU driver scheduling, thermal throttling).
* **Median** — middle measurement. If it differs a lot from `Mean`, the distribution is lopsided and a few
  outliers are dragging the average around.

### The Alloc Rate column

`Alloc Rate` is `Allocated / Mean` — managed bytes allocated per second of wall-clock time. It is the best
available proxy for **GC pressure**, and it exists because the `Gen0`/`Gen1`/`Gen2` columns cannot answer
that question here.

Those Gen columns are real (they come from `MemoryDiagnoser`, and they do appear when a section triggers a
collection — an earlier build of `SceneQueries` allocated 25.5 MB per operation and reported
`Gen0 = 1000`, i.e. one collection per operation). But BenchmarkDotNet **forces a GC around every
iteration**, so with `InvocationCount=1` each operation begins with a freshly reset Gen0 budget. Any section
allocating less than one Gen0 budget inside a single ~100 ms operation reports `-` no matter how much churn
the same code would generate in a real render loop. The counts are also quantised: collections ÷ operations
× 1000, over ~15 operations, so the smallest non-zero value is around 67.

`Alloc Rate` has neither problem. It is derived from measurements already being taken, never quantises to
zero, and ranks sections against each other directly. Use it to find *what to look at*; use `--probe-alloc`
to find *why*; and use `--soak` for what the GC actually does when nothing forces it.

### The Allocated column

`Allocated` is managed bytes allocated per invocation, measured process-wide — it counts everything the
process allocated during the measurement, including work on TinyFFR's internal threads. That is the right
measure for GC pressure, but it means a figure cannot always be attributed to the section's own code.

**It also carries a noise floor, depending where the section sits in the run** (~4-5 KB at the end of a run as of 2026-09-08; it was ~21 KB before the collections moved off `ArrayPool<T>.Shared` onto `TinyFfrArrayPool<T>`, which registers no Gen2 trim callback). That
floor is not TinyFFR allocating. `ArrayPool<T>.Shared` registers a Gen2 trim callback for every element type
it has ever pooled; each callback allocates ~336 B per garbage collection (288 B of it a single
`GC.GetGCMemoryInfo()` call), and BenchmarkDotNet forces a collection around every measurement. A run
touches more pooled types as it progresses, so later sections carry more of it — the first section measures
essentially clean, the last carries a few KB.

Practical consequences:

* A section allocating **megabytes** — `SceneQueries`, `LoadBakedAssets`, `Quads` — is unaffected; the floor
  is a rounding error.
* A section that should allocate **nothing** will still read a few KB if it runs late. Do not read a small
  non-zero figure as a regression without checking it against the same section's previous value.
* **Compare like with like.** The column is a reliable *relative* signal run-to-run, because section
  ordering is fixed, so each section carries the same floor every time.

## Options

All options go after the `--` separator, which is what tells `dotnet run` to pass them to the benchmark
program rather than interpret them itself.

### `--job <kind>` — how many times to repeat each measurement

| Kind | Warmups | Measured iterations | Use it when |
|---|---|---|---|
| `dry` | 1 | 1 | You just want to know everything still runs. Fastest. |
| `standard` | 3 | 15 | The default. Trustworthy numbers. |
| `long` | 5 | 50 | You are chasing a small difference and need tight confidence intervals. |

Warmup iterations are run and thrown away, so that one-off costs (JIT compilation, buffer growth, GPU
driver setup) do not pollute the real measurements.

### `--filter <pattern> [pattern ...]` — run only some sections

```bash
dotnet run --project Testing/Benchmarks/TinyFFR.Benchmarks -c Release -p:Platform=x64 -- --filter "*RenderFrame*"
```

**Gotcha:** to run several, put every pattern after a *single* `--filter`. Repeating the switch makes
BenchmarkDotNet print its help screen instead of running anything.

```bash
# right
-- --filter "*RenderFrame*" "*PixelPicking*"
# wrong - prints help and exits
-- --filter "*RenderFrame*" --filter "*PixelPicking*"
```

### `--soak [minutes]` — long-running steady-state loop

```bash
dotnet run --project Testing/Benchmarks/TinyFFR.Benchmarks -c Release -p:Platform=x64 -- --soak 10
```

Runs the **Soak** benchmark: one persistent scene rendered in a continuous loop for the given number of
minutes (10 by default), doing a little of everything each frame — instance transforms, light mutation,
scene queries, and periodic resource churn. Every 15 seconds it samples allocation, GC collection counts by
generation, and heap size, then prints a table and a verdict on whether allocation per frame is trending
upward.

It exists to answer questions the Smoke benchmark structurally cannot, because **`--soak` never forces a
garbage collection**. BenchmarkDotNet forces a Gen2 GC around every measurement, which is nothing like a
real application; a soak run shows what actually happens over time when the GC is left alone. That makes it
the right tool for questions about drift, leaks, and pool behaviour.

Being outside BenchmarkDotNet, it reports no Mean/Error columns — it is an experiment, not a throughput
benchmark.

### `--probe-alloc [name]` — exact per-operation allocation

```bash
dotnet run --project Testing/Benchmarks/TinyFFR.Benchmarks -c Release -p:Platform=x64 -- --probe-alloc
dotnet run --project Testing/Benchmarks/TinyFFR.Benchmarks -c Release -p:Platform=x64 -- --probe-alloc LoadBakedAssets
```

Measures how many managed bytes a single library call allocates, isolated from everything else. Each probe
runs 3 warmup iterations then reports the **minimum of 5** measured runs, divided by the operation count.

This is the instrument to use when a section's `Allocated` figure looks wrong, because it answers a
different question from the results table:

* It uses `GC.GetAllocatedBytesForCurrentThread()` — per-thread and exact — so it carries **none** of the
  `ArrayPool` trim floor described above. A zero here really is zero.
* It attributes cost to one named call rather than to a whole section.

The distinction matters: a section can report hundreds of KB while every operation in it probes at 0 B. That
means the section is measuring *retained* memory — the collections that grow as it holds resources live — not
a per-call cost, and chasing it as a leak is wasted effort.

Probe groups live in `Smoke/SmokeProbes.cs`, next to the sections they diagnose. Add one by writing a method
that calls `AllocationProbe.Measure(label, action, operationCount)` and registering it in `SmokeProbes.All`.

### `--inspect [name]` — watch the benchmarks run in a window

```bash
dotnet run --project Testing/Benchmarks/TinyFFR.Benchmarks -c Debug -p:Platform=x64 -- --inspect
```

Normally benchmarks render off-screen with no window at all. `--inspect` instead opens a window and runs
each section once so you can see what it actually draws — useful when a section's timing changes and you
want to check it is still rendering the right thing. Press **Space** to skip to the next section, **Escape**
to quit. Nothing is measured in this mode, so Debug is fine.

## How the benchmarks are organised

```
Testing/Benchmarks/TinyFFR.Benchmarks/
  Harness/          Shared infrastructure - used by every benchmark
  Smoke/            The "Smoke" benchmark - a broad sweep, run under BenchmarkDotNet
  Soak/             The "Soak" benchmark - a long steady-state loop, run outside BenchmarkDotNet
```

Each benchmark lives in its own folder. `Smoke` is a broad sweep over as much of the library as possible —
meshes, textures, materials, lighting, rendering, text, asset loading and resource management — intended to
catch a regression anywhere rather than to study one thing in depth.

A **section** is one measured operation, written as a method marked `[Benchmark]` in `Smoke/SmokeBenchmark.cs`
that delegates to a static method in one of the `Smoke/SmokeSections.*.cs` files. Each section creates
everything it needs, exercises it, and disposes it, all inside the measured region — so resource creation
and teardown are part of what is being measured, and nothing leaks between runs.

### Why the sections do so much work

Sections deliberately operate on large inputs — thousands of vertices, hundreds of model instances — rather
than the minimum needed to exercise an API. Two reasons:

1. **Precision.** A measurement that takes 20 microseconds is dominated by timer noise. BenchmarkDotNet
   warns below 100 ms per measurement, and sections are sized to clear that.
2. **Finding accidental O(n²).** An algorithm that scales badly looks fine on five items and terrible on
   five thousand. Large inputs make that visible.

All the sizes live in one place, `Smoke/SmokeWorkload.cs`, so they can be tuned without hunting through
section code.

**There is a ceiling, though.** GPU resources created inside a section are not reclaimed until the harness
flushes the GPU, which happens between iterations — so everything a single invocation creates is live at
once. Push a section too far and Filament runs out of video memory and aborts the process
(`Unable to allocate image memory`). Textures are the tightest constraint: roughly 20,000 live texture
allocations in one invocation is fine, 50,000 is not. If you need a section to do more work than that
allows, scale it along an axis that does not allocate GPU resources — `BuiltInTextures`, for example, gets
most of its time from `ReadTexture` calls that decode into a CPU buffer rather than from creating more
textures.

### Adding a section

1. Write a static method in the relevant `Smoke/SmokeSections.*.cs` file (or add a new one for a new area).
2. Add a one-line `[Benchmark]` method to `Smoke/SmokeBenchmark.cs` that calls it.
3. Size it via a new constant in `Smoke/SmokeWorkload.cs` so it takes at least ~100 ms.

`--inspect` picks it up automatically — the `[Benchmark]` methods are the single source of
truth for what sections exist.

### Adding a whole new benchmark

Create a folder next to `Smoke/`, and put in it a `public` class deriving from `TinyFfrBenchmark` (do not
mark it `sealed` — BenchmarkDotNet rejects sealed benchmark classes). It inherits setup, teardown and all
three run modes automatically; there is no harness code to write.

## Things worth knowing

* **The benchmarks share one factory per section.** `LocalTinyFfrFactory` only permits one live instance,
  so the harness creates it once per benchmark section and disposes it afterwards.
* **A GPU reclamation pump runs between iterations.** Filament reclaims GPU resources lazily, and headless
  runs never present frames to trigger that. Without intervention its handle arena fills up, silently falls
  back to a slower heap, and measurements drift upward over a run — we measured a 21% climb. The harness
  therefore flushes the GPU between iterations, outside the measured region.
* **Benchmarks run in-process.** BenchmarkDotNet normally builds and launches a separate process per
  benchmark; that cannot cope with this project's platform settings and native library reference, so the
  harness pins the in-process toolchain. This is why `--job` is handled by our own code rather than passed
  through to BenchmarkDotNet, whose own `--job` would silently discard that setting.
* **One `arena is full` message per full run is expected.** Filament's native engine is created once per
  process and outlives the per-section factories, so handle pressure accumulates across all 41 sections and
  eventually trips a one-time notice that it is falling back to a slower heap. It is not fatal and does not
  destabilise the timings — every section's `Error` stays within a few percent of its `Mean`.
* **A section that cannot be scaled honestly should not be.** If the only way to make a section take
  100 ms is to repeat a trivial call hundreds of thousands of times, say so in a comment on the constant
  rather than pretending the number means more than it does.
* **Native libraries resolve from `build_output/`.** No copying or `LD_LIBRARY_PATH` needed, but a Release
  benchmark run needs `build_output/Release/` populated — build the native library in Release first.
