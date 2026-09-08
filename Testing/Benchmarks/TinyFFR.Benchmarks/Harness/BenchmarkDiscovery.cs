// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace Egodystonic.TinyFFR.Benchmarks.Harness;

sealed record DiscoveredSection(string BenchmarkName, string SectionName, Action Invoke) {
	public string DisplayName => $"{BenchmarkName}.{SectionName}";
}

static class BenchmarkDiscovery {
	public static List<DiscoveredSection> FindSections(string? targetName) {
		var result = new List<DiscoveredSection>();

		var benchmarkTypes = typeof(BenchmarkDiscovery).Assembly
			.GetTypes()
			.Where(t => t is { IsAbstract: false, IsClass: true } && typeof(TinyFfrBenchmark).IsAssignableFrom(t))
			.OrderBy(t => t.Name);

		foreach (var benchmarkType in benchmarkTypes) {
			var benchmarkMatches = targetName == null || benchmarkType.Name.Contains(targetName, StringComparison.OrdinalIgnoreCase);
			var instance = Activator.CreateInstance(benchmarkType)!;

			foreach (var method in benchmarkType.GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
				if (method.GetCustomAttribute<BenchmarkAttribute>() == null || method.GetParameters().Length > 0) continue;
				if (!benchmarkMatches && !method.Name.Contains(targetName!, StringComparison.OrdinalIgnoreCase)) continue;

				result.Add(new DiscoveredSection(benchmarkType.Name, method.Name, method.CreateDelegate<Action>(instance)));
			}
		}

		return result;
	}
}
