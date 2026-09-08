// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Runtime.CompilerServices;
using BenchmarkDotNet.Running;
using Egodystonic.TinyFFR.Benchmarks.Smoke;
using Egodystonic.TinyFFR.Benchmarks.Soak;
using Egodystonic.TinyFFR.Testing;

namespace Egodystonic.TinyFFR.Benchmarks.Harness;

static unsafe class BenchmarkHost {
	static void TearDownEnvironment() => BenchmarkEnvironment.TearDown(force: true);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Run(string[] args) {
		CommonTestSupportFunctions.ResolveNativeAssembliesFromBuildOutputDir();

		var options = BenchmarkCommandLine.Parse(args);
		switch (options.RunMode) {
			case BenchmarkRunMode.Inspect:
				InspectRunner.Run(options.TargetName);
				return;
			case BenchmarkRunMode.Soak:
				SoakRunner.Run(options.SoakMinutes);
				return;
			case BenchmarkRunMode.ProbeAlloc:
				AllocationProbe.Run(options.TargetName, SmokeProbes.All);
				return;
			default:
				try {
					BenchmarkSwitcher
						.FromAssembly(typeof(BenchmarkHost).Assembly)
						.Run(options.RemainingArgs, TinyFfrBenchmarkConfig.Create(options.JobKind));
				}
				finally {
					TinyFfrThread.Invoke(&TearDownEnvironment);
					TinyFfrThread.Stop();
				}
				return;
		}
	}
}
