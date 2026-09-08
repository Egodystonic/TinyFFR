// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;

namespace Egodystonic.TinyFFR.Benchmarks.Harness;

static class InspectRunner {
	static readonly TimeSpan SectionDisplayTime = TimeSpan.FromSeconds(2.5d);

	public static void Run(string? targetName) {
		var sections = BenchmarkDiscovery.FindSections(targetName);
		if (sections.Count == 0) {
			Console.WriteLine($"No benchmark sections found{(targetName == null ? "" : $" matching '{targetName}'")}.");
			return;
		}

		BenchmarkEnvironment.RetainAcrossBenchmarks = true;
		BenchmarkEnvironment.Initialize(BenchmarkRenderTargetKind.Window);

		try {
			Console.WriteLine("Inspection mode. Space skips the current section; Escape quits.");
			foreach (var section in sections) {
				Console.WriteLine(section.DisplayName);
				if (!DisplaySection(section)) return;
			}
		}
		finally {
			BenchmarkEnvironment.TearDown(force: true);
		}
	}

	static bool DisplaySection(DiscoveredSection section) {
		var loop = BenchmarkEnvironment.Loop;
		var window = BenchmarkEnvironment.RenderTarget.Window;

		window?.SetTitle(section.DisplayName);
		_ = loop.IterateOnce();

		try {
			section.Invoke();
		}
		catch (Exception e) {
			Console.WriteLine($"  {e.GetType().Name} in {section.SectionName}: {e.Message}");
		}

		var elapsed = TimeSpan.Zero;
		while (elapsed < SectionDisplayTime) {
			elapsed += loop.IterateOnce();
			if (loop.Input.UserQuitRequested) return false;
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Escape)) return false;
			if (loop.Input.KeyboardAndMouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.Space)) return true;
		}

		return true;
	}
}
