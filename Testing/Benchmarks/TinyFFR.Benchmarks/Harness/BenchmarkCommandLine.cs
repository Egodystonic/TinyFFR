// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Benchmarks.Harness;

enum BenchmarkJobKind {
	Dry,
	Standard,
	Long
}

enum BenchmarkRunMode {
	Benchmark,
	Inspect,
	Soak,
	ProbeAlloc
}

sealed record BenchmarkCommandLineOptions(BenchmarkRunMode RunMode, string? TargetName, BenchmarkJobKind JobKind, double SoakMinutes, string[] RemainingArgs);

static class BenchmarkCommandLine {
	public const string InspectSwitch = "--inspect";
	public const string SoakSwitch = "--soak";
	public const string ProbeAllocSwitch = "--probe-alloc";
	public const string JobSwitch = "--job";
	public const string FilterSwitch = "--filter";

	public static BenchmarkCommandLineOptions Parse(string[] args) {
		var runMode = BenchmarkRunMode.Benchmark;
		string? targetName = null;
		var jobKind = BenchmarkJobKind.Standard;
		var soakMinutes = 0d;
		var remainingArgs = new List<string>(args.Length);

		for (var i = 0; i < args.Length; ++i) {
			if (args[i].Equals(SoakSwitch, StringComparison.OrdinalIgnoreCase)) {
				runMode = BenchmarkRunMode.Soak;
				if (i + 1 < args.Length && Double.TryParse(args[i + 1], out var parsedMinutes)) {
					soakMinutes = parsedMinutes;
					++i;
				}
				continue;
			}

			if (args[i].Equals(ProbeAllocSwitch, StringComparison.OrdinalIgnoreCase)) {
				runMode = BenchmarkRunMode.ProbeAlloc;
				if (i + 1 < args.Length && !args[i + 1].StartsWith('-')) targetName = args[++i];
				continue;
			}

			if (args[i].Equals(InspectSwitch, StringComparison.OrdinalIgnoreCase)) {
				runMode = BenchmarkRunMode.Inspect;
				if (i + 1 < args.Length && !args[i + 1].StartsWith('-')) targetName = args[++i];
				continue;
			}

			if (args[i].Equals(JobSwitch, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) {
				jobKind = ParseJobKind(args[++i]);
				continue;
			}

			remainingArgs.Add(args[i]);
		}

		if (runMode == BenchmarkRunMode.Benchmark && !remainingArgs.Any(a => a.Equals(FilterSwitch, StringComparison.OrdinalIgnoreCase))) {
			remainingArgs.Add(FilterSwitch);
			remainingArgs.Add("*");
		}

		return new BenchmarkCommandLineOptions(runMode, targetName, jobKind, soakMinutes, remainingArgs.ToArray());
	}

	static BenchmarkJobKind ParseJobKind(string value) {
		return value.ToLowerInvariant() switch {
			"dry" => BenchmarkJobKind.Dry,
			"standard" or "default" => BenchmarkJobKind.Standard,
			"long" => BenchmarkJobKind.Long,
			_ => throw new ArgumentException($"Unrecognised job kind '{value}'. Expected one of: dry, standard, long.", nameof(value))
		};
	}
}
