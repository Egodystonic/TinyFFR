// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using BenchmarkDotNet.Attributes;

namespace Egodystonic.TinyFFR.Benchmarks.Harness;

public abstract unsafe class TinyFfrBenchmark {
	[GlobalSetup]
	public void GlobalSetup() {
		BenchmarkEnvironment.RetainAcrossBenchmarks = true;
		TinyFfrThread.Start();
		TinyFfrThread.Invoke(&InitializeEnvironment);
	}

	[IterationCleanup]
	public void IterationCleanup() => TinyFfrThread.Invoke(&BenchmarkEnvironment.PumpGpuResourceReclamation);

	[GlobalCleanup]
	public void GlobalCleanup() { }

	static void InitializeEnvironment() => BenchmarkEnvironment.Initialize(BenchmarkRenderTargetKind.OutputBuffer);
}
