// Created on 2024-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Factory;
using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR;

[TestFixture, Explicit]
class LocalLoopTimingTest {
	const double MaxJitterFraction = 0.03f;

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

		var resultMeasured = stopwatch.Elapsed;
		var resultSumOfDt = TimeSpan.FromTicks(reportedTimesList.Sum(ts => ts.Ticks));
		var resultReported = loop.TotalIteratedTime;
		Console.WriteLine($"Total time: {resultMeasured.ToStringMs()} measured, {resultSumOfDt.ToStringMs()} reported sum-of-dt, {resultReported.ToStringMs()} reported");
		AssertWithinJitterTolerance(resultMeasured, resultSumOfDt);
		AssertWithinJitterTolerance(resultSumOfDt, resultReported);

		// Skip first 10% as JIT interferes with it
		resultMeasured = TimeSpan.FromTicks((long) measuredTimesList.Skip(measuredTimesList.Count / 10).Average(dt => dt.Ticks));
		resultReported = TimeSpan.FromTicks((long) reportedTimesList.Skip(reportedTimesList.Count / 10).Average(dt => dt.Ticks));
		Console.WriteLine($"Average time: {resultMeasured.ToStringMs()} measured, {resultReported.ToStringMs()} reported");
		AssertWithinJitterTolerance(resultMeasured, resultReported);

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

		resultMeasured = stopwatch.Elapsed;
		resultSumOfDt = TimeSpan.FromTicks(reportedTimesList.Sum(ts => ts.Ticks));
		resultReported = loop.TotalIteratedTime;
		Console.WriteLine($"Total time: {resultMeasured.ToStringMs()} measured, {resultSumOfDt.ToStringMs()} reported sum-of-dt, {resultReported.ToStringMs()} reported");
		AssertWithinJitterTolerance(resultMeasured, resultSumOfDt);
		AssertWithinJitterTolerance(resultSumOfDt, resultReported);

		// Skip first 10% as JIT interferes with it
		resultMeasured = TimeSpan.FromTicks((long) measuredTimesList.Skip(measuredTimesList.Count / 10).Average(dt => dt.Ticks));
		resultReported = TimeSpan.FromTicks((long) reportedTimesList.Skip(reportedTimesList.Count / 10).Average(dt => dt.Ticks));
		Console.WriteLine($"Average time: {resultMeasured.ToStringMs()} measured, {resultReported.ToStringMs()} reported");
		AssertWithinJitterTolerance(resultMeasured, resultReported);

		loop.Dispose();
	}

	void AssertWithinJitterTolerance(TimeSpan a, TimeSpan b, [CallerArgumentExpression(nameof(a))] string? aArgName = null, [CallerArgumentExpression(nameof(b))] string? bArgName = null) {
		var diff = Double.Abs(a.TotalMilliseconds - b.TotalMilliseconds);
		if (diff > Double.Abs(Double.MaxMagnitude(a.TotalMilliseconds, b.TotalMilliseconds)) * MaxJitterFraction) {
			Assert.Fail($"Discrepancy was high ({aArgName}:{a.ToStringMs()} vs {bArgName}:{b.ToStringMs()}). Check results, maybe retry test (could be jitter outlier).");
		}
	}
} 