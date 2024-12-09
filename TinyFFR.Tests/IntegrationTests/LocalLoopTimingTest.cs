// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalLoopTimingTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	// No assertions around timing because it leads to flaky tests, but manual check of output in console is still useful
	public void Execute() {
		using var factory = new LocalTinyFfrFactory();

		var loopBuilder = factory.ApplicationLoopBuilder;
		var loop = loopBuilder.CreateLoop(new() { FrameRateCapHz = 30 });

		var reportedTimesList = new List<TimeSpan>();
		var measuredTimesList = new List<TimeSpan>();
		var stopwatch = Stopwatch.StartNew();
		var prevMeasuredTime = stopwatch.Elapsed;
		while (stopwatch.Elapsed < TimeSpan.FromSeconds(3d)) {
			var dt = loop.IterateOnce();
			var measured = stopwatch.Elapsed - prevMeasuredTime;
			prevMeasuredTime = stopwatch.Elapsed;
			Console.WriteLine($"{measured.ToStringMs()} measured vs {dt.ToStringMs()} reported");
			reportedTimesList.Add(dt);
			measuredTimesList.Add(measured);
		}

		Console.WriteLine($"Total time: {stopwatch.Elapsed.ToStringMs()} measured, {TimeSpan.FromTicks(reportedTimesList.Sum(ts => ts.Ticks)).ToStringMs()} reported sum-of-dt, {loop.TotalIteratedTime.ToStringMs()} reported");
		// Skip first 10% as JIT interferes with it
		Console.WriteLine($"Average time: {TimeSpan.FromTicks((long) measuredTimesList.Skip(measuredTimesList.Count / 10).Average(dt => dt.Ticks)).ToStringMs()} measured, {TimeSpan.FromTicks((long) reportedTimesList.Skip(reportedTimesList.Count / 10).Average(dt => dt.Ticks)).ToStringMs()} reported");

		Console.WriteLine("===========================================================================================================================");

		loop.Dispose();
		loop = loopBuilder.CreateLoop(new() { FrameRateCapHz = 30 });
		reportedTimesList = new List<TimeSpan>();
		measuredTimesList = new List<TimeSpan>();
		stopwatch = Stopwatch.StartNew();
		prevMeasuredTime = stopwatch.Elapsed;
		while (stopwatch.Elapsed < TimeSpan.FromSeconds(3d)) {
			TimeSpan dt;
			while (!loop.TryIterateOnce(out dt)) {
				Thread.Sleep(10);
				Console.WriteLine($"Time remaining: {loop.TimeUntilNextIteration.ToStringMs()}");
			}
			var measured = stopwatch.Elapsed - prevMeasuredTime;
			prevMeasuredTime = stopwatch.Elapsed;
			Console.WriteLine($"{measured.ToStringMs()} measured vs {dt.ToStringMs()} reported");
			reportedTimesList.Add(dt);
			measuredTimesList.Add(measured);
		}

		Console.WriteLine($"Total time: {stopwatch.Elapsed.ToStringMs()} measured, {TimeSpan.FromTicks(reportedTimesList.Sum(ts => ts.Ticks)).ToStringMs()} reported sum-of-dt, {loop.TotalIteratedTime.ToStringMs()} reported");
		// Skip first 10% as JIT interferes with it
		Console.WriteLine($"Average time: {TimeSpan.FromTicks((long) measuredTimesList.Skip(measuredTimesList.Count / 10).Average(dt => dt.Ticks)).ToStringMs()} measured, {TimeSpan.FromTicks((long) reportedTimesList.Skip(reportedTimesList.Count / 10).Average(dt => dt.Ticks)).ToStringMs()} reported");
		loop.Dispose();
	}
} 