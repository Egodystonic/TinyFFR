// Created on 2026-09-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Globalization;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Egodystonic.TinyFFR.Benchmarks.Harness;

sealed class AllocationRateColumn : IColumn {
	public static readonly IColumn Default = new AllocationRateColumn();

	public string Id => nameof(AllocationRateColumn);
	public string ColumnName => "Alloc Rate";
	public string Legend => "Managed bytes allocated per second of wall-clock time (Allocated / Mean). Unlike the Gen columns this never quantises to zero, so it ranks GC pressure even when no collection happens inside a single operation.";
	public UnitType UnitType => UnitType.Dimensionless;
	public ColumnCategory Category => ColumnCategory.Custom;
	public int PriorityInCategory => 0;
	public bool AlwaysShow => true;
	public bool IsNumeric => true;

	public bool IsAvailable(Summary summary) => true;
	public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

	public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, SummaryStyle.Default);

	public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) {
		if (summary[benchmarkCase] is not { } report) return "?";
		if (report.ResultStatistics?.Mean is not { } meanNanoseconds || meanNanoseconds <= 0d) return "?";
		if (report.GcStats.GetBytesAllocatedPerOperation(benchmarkCase) is not { } allocatedBytes) return "?";

		var megabytesPerSecond = allocatedBytes / (meanNanoseconds / 1_000_000_000d) / (1024d * 1024d);
		var format = megabytesPerSecond switch {
			>= 10d => "N1",
			>= 0.01d => "N2",
			_ => "N4"
		};
		return megabytesPerSecond.ToString(format, CultureInfo.InvariantCulture) + " MB/s";
	}

	public override string ToString() => ColumnName;
}
