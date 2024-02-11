// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Factory;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class NativeLoopTimingTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	// No assertions around timing because it leads to flaky tests, but manual check of output in console is still useful
	public void Execute() {
		using var factory = new TffrFactory();

		var loopBuilder = factory.GetApplicationLoopBuilder(new() { InputTrackerConfig = new() { MaxControllerNameLength = 20 } });
		var loop = loopBuilder.BuildLoop(new() { FrameRateCapHz = 30 });

		var reportedTimesList = new List<DeltaTime>();
		var measuredTimesList = new List<DeltaTime>();
		var stopwatch = Stopwatch.StartNew();
		var prevMeasuredTime = stopwatch.Elapsed;
		while (stopwatch.Elapsed < TimeSpan.FromSeconds(3d)) {
			var dt = loop.IterateOnce();
			var measured = stopwatch.Elapsed - prevMeasuredTime;
			prevMeasuredTime = stopwatch.Elapsed;
			Console.WriteLine($"{new DeltaTime(measured)} measured vs {dt} reported");
			reportedTimesList.Add(dt);
			measuredTimesList.Add(measured);
		}

		Console.WriteLine($"Total time: {stopwatch.Elapsed.TotalMilliseconds:N2}ms measured, {reportedTimesList.Sum(dt => dt.ToTimeSpan().TotalMilliseconds):N2}ms reported sum-of-dt, {loop.TotalIteratedTime.TotalMilliseconds:N2}ms reported");
		// Skip first 10% as JIT interferes with it
		Console.WriteLine($"Average time: {measuredTimesList.Skip(measuredTimesList.Count / 10).Average(dt => dt.ToTimeSpan().TotalMilliseconds):N2}ms measured, {reportedTimesList.Skip(reportedTimesList.Count / 10).Average(dt => dt.ToTimeSpan().TotalMilliseconds):N2}ms reported");

		Console.WriteLine("===========================================================================================================================");

		loop.Dispose();
		loop = loopBuilder.BuildLoop(new() { FrameRateCapHz = 30 });
		reportedTimesList = new List<DeltaTime>();
		measuredTimesList = new List<DeltaTime>();
		stopwatch = Stopwatch.StartNew();
		prevMeasuredTime = stopwatch.Elapsed;
		while (stopwatch.Elapsed < TimeSpan.FromSeconds(3d)) {
			DeltaTime dt;
			while (!loop.TryIterateOnce(out dt)) {
				Thread.Sleep(10);
				Console.WriteLine($"Time remaining: {loop.TimeUntilNextIteration.TotalMilliseconds:N2}ms");
			}
			var measured = stopwatch.Elapsed - prevMeasuredTime;
			prevMeasuredTime = stopwatch.Elapsed;
			Console.WriteLine($"{new DeltaTime(measured)} measured vs {dt} reported");
			reportedTimesList.Add(dt);
			measuredTimesList.Add(measured);
		}

		Console.WriteLine($"Total time: {stopwatch.Elapsed.TotalMilliseconds:N2}ms measured, {reportedTimesList.Sum(dt => dt.ToTimeSpan().TotalMilliseconds):N2}ms reported sum-of-dt, {loop.TotalIteratedTime.TotalMilliseconds:N2}ms reported");
		// Skip first 10% as JIT interferes with it
		Console.WriteLine($"Average time: {measuredTimesList.Skip(measuredTimesList.Count / 10).Average(dt => dt.ToTimeSpan().TotalMilliseconds):N2}ms measured, {reportedTimesList.Skip(reportedTimesList.Count / 10).Average(dt => dt.ToTimeSpan().TotalMilliseconds):N2}ms reported");
		loop.Dispose();
	}
} 