// Created on 2026-08-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Diagnostics;

namespace Egodystonic.TinyFFR.Rendering.Local;

[TestFixture]
class RendererFrameRateLimitTest {
	static long TicksPerSecond => Stopwatch.Frequency;

	static int CountDueFrames(ref RendererFrameRateLimit limit, long startTimestamp, long tickIncrement, int frameCount) {
		var result = 0;
		for (var i = 0; i < frameCount; ++i) {
			if (RendererFrameRateLimit.AdvanceAndDetermineIfDue(ref limit, startTimestamp + tickIncrement * i)) result++;
		}
		return result;
	}

	[Test]
	public void ShouldNotLimitWhenNoCapOrRatioIsSet() {
		var limit = RendererFrameRateLimit.None;

		Assert.That(limit.IsLimiting, Is.False);
		Assert.That(limit.FrameRateCapHz, Is.Null);
		Assert.That(limit.FrameRatio, Is.EqualTo(1));
		Assert.That(CountDueFrames(ref limit, TicksPerSecond, TicksPerSecond / 144L, 500), Is.EqualTo(500));
	}

	[Test]
	public void ShouldConsiderAnyCapOrRatioToBeLimiting() {
		Assert.That(RendererFrameRateLimit.None.WithFrameRatio(2).IsLimiting, Is.True);
		Assert.That(RendererFrameRateLimit.None.WithCapIntervalTicks(RendererFrameRateLimit.TicksPerFrameAtFrameRate(60)).IsLimiting, Is.True);
		Assert.That(RendererFrameRateLimit.None.WithFrameRatio(1).IsLimiting, Is.False);
		Assert.That(RendererFrameRateLimit.None.WithCapIntervalTicks(0L).IsLimiting, Is.False);
	}

	[Test]
	public void ShouldRoundTripFrameRateCapHz() {
		foreach (var hz in new[] { 1, 10, 24, 30, 60, 120, 144 }) {
			var limit = RendererFrameRateLimit.None.WithCapIntervalTicks(RendererFrameRateLimit.TicksPerFrameAtFrameRate(hz));
			Assert.That(limit.FrameRateCapHz, Is.EqualTo(hz), $"Round trip failed for {hz}Hz.");
		}
	}

	[Test]
	public void ShouldRenderExactlyEveryNthFrameWithRatioLimit() {
		for (var ratio = 1; ratio <= 6; ++ratio) {
			var limit = RendererFrameRateLimit.None.WithFrameRatio(ratio);
			var dueFrameIndices = new System.Collections.Generic.List<int>();
			for (var i = 0; i < 60; ++i) {
				if (RendererFrameRateLimit.AdvanceAndDetermineIfDue(ref limit, TicksPerSecond + i)) dueFrameIndices.Add(i);
			}

			Assert.That(dueFrameIndices.Count, Is.EqualTo(60 / ratio), $"Unexpected due count for ratio {ratio}.");
			for (var i = 0; i < dueFrameIndices.Count; ++i) {
				Assert.That(dueFrameIndices[i], Is.EqualTo(ratio - 1 + i * ratio), $"Unexpected cadence for ratio {ratio}.");
			}
		}
	}

	[Test]
	public void ShouldRenderEveryFrameWhenCapMatchesLoopRate() {
		var limit = RendererFrameRateLimit.None.WithCapIntervalTicks(RendererFrameRateLimit.TicksPerFrameAtFrameRate(60));
		Assert.That(CountDueFrames(ref limit, TicksPerSecond, TicksPerSecond / 60L, 600), Is.EqualTo(600));
	}

	[Test]
	public void ShouldRenderEveryFrameWhenLoopIsSlowerThanCap() {
		var limit = RendererFrameRateLimit.None.WithCapIntervalTicks(RendererFrameRateLimit.TicksPerFrameAtFrameRate(120));
		Assert.That(CountDueFrames(ref limit, TicksPerSecond, TicksPerSecond / 30L, 300), Is.EqualTo(300));
	}

	[Test]
	public void ShouldApproximateCapWhenLoopIsFasterThanCap() {
		const int LoopHz = 144;
		const int CapHz = 60;
		const int SimulatedSeconds = 10;

		var limit = RendererFrameRateLimit.None.WithCapIntervalTicks(RendererFrameRateLimit.TicksPerFrameAtFrameRate(CapHz));
		var dueCount = CountDueFrames(ref limit, TicksPerSecond, TicksPerSecond / LoopHz, LoopHz * SimulatedSeconds);
		var actualHz = dueCount / (double) SimulatedSeconds;

		Assert.That(actualHz, Is.EqualTo(CapHz).Within(CapHz * 0.03d), $"Expected ~{CapHz}Hz but measured {actualHz}Hz.");
	}

	[Test]
	public void ShouldNotDriftAcrossManyFramesWhenCapIsNotADivisorOfLoopRate() {
		const int LoopHz = 100;
		const int CapHz = 30;
		const int SimulatedSeconds = 60;

		var limit = RendererFrameRateLimit.None.WithCapIntervalTicks(RendererFrameRateLimit.TicksPerFrameAtFrameRate(CapHz));
		var dueCount = CountDueFrames(ref limit, TicksPerSecond, TicksPerSecond / LoopHz, LoopHz * SimulatedSeconds);
		var actualHz = dueCount / (double) SimulatedSeconds;

		Assert.That(actualHz, Is.EqualTo(CapHz).Within(CapHz * 0.03d), $"Expected ~{CapHz}Hz but measured {actualHz}Hz.");
	}

	[Test]
	public void ShouldNotBurstCatchUpAfterALongStall() {
		var capIntervalTicks = RendererFrameRateLimit.TicksPerFrameAtFrameRate(60);
		var limit = RendererFrameRateLimit.None.WithCapIntervalTicks(capIntervalTicks);

		var timestamp = TicksPerSecond;
		Assert.That(RendererFrameRateLimit.AdvanceAndDetermineIfDue(ref limit, timestamp), Is.True);

		timestamp += TicksPerSecond * 5L;
		Assert.That(RendererFrameRateLimit.AdvanceAndDetermineIfDue(ref limit, timestamp), Is.True);

		var dueCountImmediatelyAfterStall = 0;
		var tinyIncrement = capIntervalTicks / 100L;
		for (var i = 0; i < 20; ++i) {
			timestamp += tinyIncrement;
			if (RendererFrameRateLimit.AdvanceAndDetermineIfDue(ref limit, timestamp)) dueCountImmediatelyAfterStall++;
		}

		Assert.That(dueCountImmediatelyAfterStall, Is.Zero, "A five-second stall was repaid as a burst of catch-up renders.");
	}

	[Test]
	public void ShouldBeDueOnFirstFrameAfterCapIsApplied() {
		var limit = RendererFrameRateLimit.None.WithCapIntervalTicks(RendererFrameRateLimit.TicksPerFrameAtFrameRate(10));
		Assert.That(RendererFrameRateLimit.AdvanceAndDetermineIfDue(ref limit, TicksPerSecond * 1234L), Is.True);
	}

	[Test]
	public void ShouldTakeTheLowerRateWhenCapAndRatioAreCombined() {
		const int LoopHz = 120;
		const int SimulatedSeconds = 10;
		var loopIncrement = TicksPerSecond / LoopHz;

		var ratioIsStricter = RendererFrameRateLimit.None
			.WithCapIntervalTicks(RendererFrameRateLimit.TicksPerFrameAtFrameRate(60))
			.WithFrameRatio(8);
		var ratioIsStricterCount = CountDueFrames(ref ratioIsStricter, TicksPerSecond, loopIncrement, LoopHz * SimulatedSeconds);
		Assert.That(ratioIsStricterCount / (double) SimulatedSeconds, Is.EqualTo(15d).Within(1d));

		var capIsStricter = RendererFrameRateLimit.None
			.WithCapIntervalTicks(RendererFrameRateLimit.TicksPerFrameAtFrameRate(10))
			.WithFrameRatio(2);
		var capIsStricterCount = CountDueFrames(ref capIsStricter, TicksPerSecond, loopIncrement, LoopHz * SimulatedSeconds);
		Assert.That(capIsStricterCount / (double) SimulatedSeconds, Is.EqualTo(10d).Within(1d));
	}

	[Test]
	public void ShouldNeverMeaningfullyExceedTheRequestedCap() {
		const int SimulatedSeconds = 20;

		foreach (var loopHz in new[] { 61, 75, 90, 144, 240, 1000 }) {
			foreach (var capHz in new[] { 10, 24, 30, 60 }) {
				var limit = RendererFrameRateLimit.None.WithCapIntervalTicks(RendererFrameRateLimit.TicksPerFrameAtFrameRate(capHz));
				var dueCount = CountDueFrames(ref limit, TicksPerSecond, TicksPerSecond / loopHz, loopHz * SimulatedSeconds);
				var actualHz = dueCount / (double) SimulatedSeconds;

				Assert.That(actualHz, Is.LessThanOrEqualTo(capHz * 1.02d), $"Cap of {capHz}Hz was exceeded at a loop rate of {loopHz}Hz (measured {actualHz}Hz).");
			}
		}
	}

	[Test]
	public void ShouldResetPacingStateWhenLimitsAreReconfigured() {
		var limit = RendererFrameRateLimit.None.WithCapIntervalTicks(RendererFrameRateLimit.TicksPerFrameAtFrameRate(60));
		_ = CountDueFrames(ref limit, TicksPerSecond, TicksPerSecond / 144L, 50);
		Assert.That(limit.PreviousTimestamp, Is.Not.Zero);

		var reconfigured = limit.WithCapIntervalTicks(RendererFrameRateLimit.TicksPerFrameAtFrameRate(30));
		Assert.That(reconfigured.PreviousTimestamp, Is.Zero);
		Assert.That(reconfigured.AccumulatorTicks, Is.Zero);

		var ratioLimit = RendererFrameRateLimit.None.WithFrameRatio(4);
		_ = CountDueFrames(ref ratioLimit, TicksPerSecond, 1L, 3);
		Assert.That(ratioLimit.FramesSincePreviousRender, Is.Not.Zero);
		Assert.That(ratioLimit.WithFrameRatio(3).FramesSincePreviousRender, Is.Zero);
	}
}
