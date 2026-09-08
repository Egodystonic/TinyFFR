// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using Perfolizer.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Egodystonic.TinyFFR.Benchmarks.Harness;

static class TinyFfrBenchmarkConfig {
	public static IConfig Create(BenchmarkJobKind jobKind) {
		return ManualConfig
			.Create(DefaultConfig.Instance)
			.AddDiagnoser(MemoryDiagnoser.Default)
			.AddColumn(AllocationRateColumn.Default)
			.AddJob(CreateJob(jobKind));
	}

	const double SectionTargetIterationTimeMs = 100d;

	static Job CreateJob(BenchmarkJobKind jobKind) {
		var result = Job.Default
			.WithToolchain(InProcessEmitToolchain.Instance)
			.WithIterationTime(TimeInterval.FromMilliseconds(SectionTargetIterationTimeMs));

		return jobKind switch {
			BenchmarkJobKind.Dry => result.WithWarmupCount(1).WithIterationCount(1),
			BenchmarkJobKind.Long => result.WithWarmupCount(5).WithIterationCount(50),
			_ => result.WithWarmupCount(3).WithIterationCount(15)
		};
	}
}
