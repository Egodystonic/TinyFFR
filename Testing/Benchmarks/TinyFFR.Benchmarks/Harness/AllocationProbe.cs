// Created on 2026-09-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Benchmarks.Harness;

sealed record ProbeGroup(string Name, Action Run);

static class AllocationProbe {
	const int WarmupIterations = 3;
	const int MeasuredIterations = 5;

	public static void Run(string? targetName, IReadOnlyList<ProbeGroup> groups) {
		var matches = new List<ProbeGroup>();
		foreach (var group in groups) {
			if (targetName == null || group.Name.Contains(targetName, StringComparison.OrdinalIgnoreCase)) matches.Add(group);
		}

		if (matches.Count == 0) {
			Console.WriteLine($"No probe groups found{(targetName == null ? "" : $" matching '{targetName}'")}.");
			Console.WriteLine($"Available: {String.Join(", ", groups.Select(g => g.Name))}");
			return;
		}

		Console.WriteLine("Allocation probe: exact per-operation managed allocation.");
		Console.WriteLine($"Measured with GC.GetAllocatedBytesForCurrentThread (per-thread, exact), {WarmupIterations} warmup then min of {MeasuredIterations}.");
		Console.WriteLine("Unlike the BenchmarkDotNet 'Allocated' column, these figures carry no ArrayPool trim floor.");

		BenchmarkEnvironment.RetainAcrossBenchmarks = true;
		BenchmarkEnvironment.Initialize(BenchmarkRenderTargetKind.OutputBuffer);

		try {
			foreach (var group in matches) {
				Console.WriteLine();
				Console.WriteLine($"--- {group.Name} ---");
				try {
					group.Run();
				}
				catch (Exception e) {
					Console.WriteLine($"  {e.GetType().Name}: {e.Message}");
				}
				BenchmarkEnvironment.PumpGpuResourceReclamation();
			}
		}
		finally {
			BenchmarkEnvironment.TearDown(force: true);
		}
	}

	public static void Measure(string label, Action action) => Measure(label, action, 1);

	public static void Measure(string label, Action action, int operationCount) {
		for (var i = 0; i < WarmupIterations; ++i) action();

		var best = Int64.MaxValue;
		for (var i = 0; i < MeasuredIterations; ++i) {
			var before = GC.GetAllocatedBytesForCurrentThread();
			action();
			best = Int64.Min(best, GC.GetAllocatedBytesForCurrentThread() - before);
		}

		var perOp = operationCount > 1 ? $" ({best / (double) operationCount,9:N1} B/op)" : new string(' ', 17);
		Console.WriteLine($"  {best,12:N0} B{perOp}  {label}");
	}
}
