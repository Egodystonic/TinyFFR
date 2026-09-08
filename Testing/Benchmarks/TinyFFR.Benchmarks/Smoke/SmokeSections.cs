// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Benchmarks.Harness;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR.Benchmarks.Smoke;

static partial class SmokeSections {
	static ILocalTinyFfrFactory Factory => BenchmarkEnvironment.Factory;
	static BenchmarkRenderTarget Target => BenchmarkEnvironment.RenderTarget;
	static IResourceAllocator Allocator => BenchmarkEnvironment.Allocator;

}
