// Created on 2026-08-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Egodystonic.TinyFFR.Rendering.Local;

namespace Egodystonic.TinyFFR.Rendering;

[TestFixture]
class CompositorSubRendererRateLimitHelperTest {
	static long TicksPerSecond => Stopwatch.Frequency;

	static int CountRenderedFrames(ref CompositorSubRendererRateLimitHelper limitHelper, long startTimestamp, long tickIncrement, int frameCount) {
		var result = 0;
		for (var i = 0; i < frameCount; ++i) {
			if (CompositorSubRendererRateLimitHelper.ExecuteCompositorFrameAndDetermineIfShouldRender(ref limitHelper, startTimestamp + tickIncrement * i)) result++;
		}
		return result;
	}

	static List<int> GetRenderedFrameIndices(ref CompositorSubRendererRateLimitHelper limitHelper, long startTimestamp, long tickIncrement, int frameCount) {
		var result = new List<int>();
		for (var i = 0; i < frameCount; ++i) {
			if (CompositorSubRendererRateLimitHelper.ExecuteCompositorFrameAndDetermineIfShouldRender(ref limitHelper, startTimestamp + tickIncrement * i)) result.Add(i);
		}
		return result;
	}

	[Test]
	public void ShouldNotLimitWhenNoCapOrRatioIsSet() {
		var limitHelper = CompositorSubRendererRateLimitHelper.Unlimited;

		Assert.That(limitHelper.CapEnabled, Is.False);
		Assert.That(limitHelper.RatioEnabled, Is.False);
		Assert.That(limitHelper.CapOrRatioEnabled, Is.False);
		Assert.That(limitHelper.RateCapHz, Is.Null);
		Assert.That(limitHelper.RateRatio, Is.EqualTo(1));
		Assert.That(limitHelper.InitialPostConfigChangeFrameStillPending, Is.False);
		Assert.That(CountRenderedFrames(ref limitHelper, TicksPerSecond, TicksPerSecond / 144L, 500), Is.EqualTo(500));
	}

	[Test]
	public void ShouldConsiderAnyCapOrRatioToBeLimiting() {
		Assert.That(CompositorSubRendererRateLimitHelper.Unlimited.WithFrameRatio(2).CapOrRatioEnabled, Is.True);
		Assert.That(CompositorSubRendererRateLimitHelper.Unlimited.WithFrameCap(60).CapOrRatioEnabled, Is.True);
		Assert.That(CompositorSubRendererRateLimitHelper.Unlimited.WithFrameRatio(1).CapOrRatioEnabled, Is.False);
		Assert.That(CompositorSubRendererRateLimitHelper.Unlimited.WithFrameCap(null).CapOrRatioEnabled, Is.False);

		Assert.That(CompositorSubRendererRateLimitHelper.Unlimited.WithFrameRatio(2).RatioEnabled, Is.True);
		Assert.That(CompositorSubRendererRateLimitHelper.Unlimited.WithFrameRatio(2).CapEnabled, Is.False);
		Assert.That(CompositorSubRendererRateLimitHelper.Unlimited.WithFrameCap(60).CapEnabled, Is.True);
		Assert.That(CompositorSubRendererRateLimitHelper.Unlimited.WithFrameCap(60).RatioEnabled, Is.False);
	}

	[Test]
	public void ShouldRoundTripFrameRateCapHz() {
		foreach (var hz in new[] { 1, 10, 24, 30, 60, 120, 144 }) {
			var limitHelper = CompositorSubRendererRateLimitHelper.Unlimited.WithFrameCap(hz);
			Assert.That(limitHelper.RateCapHz, Is.EqualTo(hz), $"Round trip failed for {hz}Hz.");
		}

		Assert.That(CompositorSubRendererRateLimitHelper.Unlimited.WithFrameCap(60).WithFrameCap(null).RateCapHz, Is.Null);
	}

	[Test]
	public void ShouldRenderExactlyEveryNthFrameWithRatioLimit() {
		for (var ratio = 1; ratio <= 6; ++ratio) {
			var limitHelper = CompositorSubRendererRateLimitHelper.Unlimited.WithFrameRatio(ratio);
			var renderedFrameIndices = GetRenderedFrameIndices(ref limitHelper, TicksPerSecond, 1L, 60);

			for (var i = 0; i < renderedFrameIndices.Count; ++i) {
				Assert.That(renderedFrameIndices[i], Is.EqualTo(i * ratio), $"Unexpected cadence for ratio {ratio}.");
			}
			Assert.That(renderedFrameIndices.Count, Is.EqualTo((60 + ratio - 1) / ratio), $"Unexpected rendered frame count for ratio {ratio}.");
		}
	}

	[Test]
	public void ShouldRenderEveryFrameWhenCapMatchesLoopRate() {
		var limitHelper = CompositorSubRendererRateLimitHelper.Unlimited.WithFrameCap(60);
		Assert.That(CountRenderedFrames(ref limitHelper, TicksPerSecond, TicksPerSecond / 60L, 600), Is.EqualTo(600));
	}

	[Test]
	public void ShouldRenderEveryFrameWhenLoopIsSlowerThanCap() {
		var limitHelper = CompositorSubRendererRateLimitHelper.Unlimited.WithFrameCap(120);
		Assert.That(CountRenderedFrames(ref limitHelper, TicksPerSecond, TicksPerSecond / 30L, 300), Is.EqualTo(300));
	}

	[Test]
	public void ShouldApproximateCapWhenLoopIsFasterThanCap() {
		const int LoopHz = 144;
		const int CapHz = 60;
		const int SimulatedSeconds = 10;

		var limitHelper = CompositorSubRendererRateLimitHelper.Unlimited.WithFrameCap(CapHz);
		var renderedCount = CountRenderedFrames(ref limitHelper, TicksPerSecond, TicksPerSecond / LoopHz, LoopHz * SimulatedSeconds);
		var actualHz = renderedCount / (double) SimulatedSeconds;

		Assert.That(actualHz, Is.EqualTo(CapHz).Within(CapHz * 0.03d), $"Expected ~{CapHz}Hz but measured {actualHz}Hz.");
	}

	[Test]
	public void ShouldNotDriftAcrossManyFramesWhenCapIsNotADivisorOfLoopRate() {
		const int LoopHz = 100;
		const int CapHz = 30;
		const int SimulatedSeconds = 60;

		var limitHelper = CompositorSubRendererRateLimitHelper.Unlimited.WithFrameCap(CapHz);
		var renderedCount = CountRenderedFrames(ref limitHelper, TicksPerSecond, TicksPerSecond / LoopHz, LoopHz * SimulatedSeconds);
		var actualHz = renderedCount / (double) SimulatedSeconds;

		Assert.That(actualHz, Is.EqualTo(CapHz).Within(CapHz * 0.03d), $"Expected ~{CapHz}Hz but measured {actualHz}Hz.");
	}

	[Test]
	public void ShouldNotBurstCatchUpAfterALongStall() {
		var capPeriodTicks = CompositorSubRendererRateLimitHelper.TicksPerFrameAtFrameRate(60);
		var limitHelper = CompositorSubRendererRateLimitHelper.Unlimited.WithFrameCap(60);

		var timestamp = TicksPerSecond;
		Assert.That(CompositorSubRendererRateLimitHelper.ExecuteCompositorFrameAndDetermineIfShouldRender(ref limitHelper, timestamp), Is.True);

		timestamp += TicksPerSecond * 5L;
		Assert.That(CompositorSubRendererRateLimitHelper.ExecuteCompositorFrameAndDetermineIfShouldRender(ref limitHelper, timestamp), Is.True);

		var renderedCountImmediatelyAfterStall = 0;
		var tinyIncrement = capPeriodTicks / 100L;
		for (var i = 0; i < 20; ++i) {
			timestamp += tinyIncrement;
			if (CompositorSubRendererRateLimitHelper.ExecuteCompositorFrameAndDetermineIfShouldRender(ref limitHelper, timestamp)) renderedCountImmediatelyAfterStall++;
		}

		Assert.That(renderedCountImmediatelyAfterStall, Is.Zero, "A five-second stall was repaid as a burst of catch-up renders.");
	}

	[Test]
	public void ShouldTakeTheLowerRateWhenCapAndRatioAreCombined() {
		const int LoopHz = 120;
		const int SimulatedSeconds = 10;
		var loopIncrement = TicksPerSecond / LoopHz;

		var ratioIsStricter = CompositorSubRendererRateLimitHelper.Unlimited
			.WithFrameCap(60)
			.WithFrameRatio(8);
		var ratioIsStricterCount = CountRenderedFrames(ref ratioIsStricter, TicksPerSecond, loopIncrement, LoopHz * SimulatedSeconds);
		Assert.That(ratioIsStricterCount / (double) SimulatedSeconds, Is.EqualTo(15d).Within(1d));

		var capIsStricter = CompositorSubRendererRateLimitHelper.Unlimited
			.WithFrameCap(10)
			.WithFrameRatio(2);
		var capIsStricterCount = CountRenderedFrames(ref capIsStricter, TicksPerSecond, loopIncrement, LoopHz * SimulatedSeconds);
		Assert.That(capIsStricterCount / (double) SimulatedSeconds, Is.EqualTo(10d).Within(1d));
	}

	[Test]
	public void ShouldNeverMeaningfullyExceedTheRequestedCap() {
		const int SimulatedSeconds = 20;

		foreach (var loopHz in new[] { 61, 75, 90, 144, 240, 1000 }) {
			foreach (var capHz in new[] { 10, 24, 30, 60 }) {
				var limitHelper = CompositorSubRendererRateLimitHelper.Unlimited.WithFrameCap(capHz);
				var renderedCount = CountRenderedFrames(ref limitHelper, TicksPerSecond, TicksPerSecond / loopHz, loopHz * SimulatedSeconds);
				var actualHz = renderedCount / (double) SimulatedSeconds;

				Assert.That(actualHz, Is.LessThanOrEqualTo(capHz * 1.02d), $"Cap of {capHz}Hz was exceeded at a loop rate of {loopHz}Hz (measured {actualHz}Hz).");
			}
		}
	}

	[Test]
	public void ShouldResetPacingStateWhenLimitsAreReconfigured() {
		var limitHelper = CompositorSubRendererRateLimitHelper.Unlimited.WithFrameCap(60);
		_ = CountRenderedFrames(ref limitHelper, TicksPerSecond, TicksPerSecond / 144L, 50);
		Assert.That(limitHelper.PreviousCompositorFrameTimestamp, Is.Not.Zero);

		var reconfigured = limitHelper.WithFrameCap(30);
		Assert.That(reconfigured.PreviousCompositorFrameTimestamp, Is.Zero);
		Assert.That(reconfigured.RateCapAccumulatedTicks, Is.Zero);

		var ratioLimitHelper = CompositorSubRendererRateLimitHelper.Unlimited.WithFrameRatio(4);
		_ = CountRenderedFrames(ref ratioLimitHelper, TicksPerSecond, 1L, 3);
		Assert.That(ratioLimitHelper.ConsecutiveSkippedFramesCount, Is.Not.Zero);
		Assert.That(ratioLimitHelper.WithFrameRatio(3).ConsecutiveSkippedFramesCount, Is.Zero);
	}

	[Test]
	public void ShouldForceRenderOnFirstFrameAfterCapOrRatioIsConfigured() {
		Assert.That(CompositorSubRendererRateLimitHelper.Unlimited.WithFrameCap(10).InitialPostConfigChangeFrameStillPending, Is.True);
		Assert.That(CompositorSubRendererRateLimitHelper.Unlimited.WithFrameRatio(10).InitialPostConfigChangeFrameStillPending, Is.True);

		var capOnly = CompositorSubRendererRateLimitHelper.Unlimited.WithFrameCap(10);
		Assert.That(CompositorSubRendererRateLimitHelper.ExecuteCompositorFrameAndDetermineIfShouldRender(ref capOnly, TicksPerSecond), Is.True);
		Assert.That(capOnly.InitialPostConfigChangeFrameStillPending, Is.False);

		var ratioOnly = CompositorSubRendererRateLimitHelper.Unlimited.WithFrameRatio(10);
		Assert.That(CompositorSubRendererRateLimitHelper.ExecuteCompositorFrameAndDetermineIfShouldRender(ref ratioOnly, TicksPerSecond), Is.True);
		Assert.That(ratioOnly.InitialPostConfigChangeFrameStillPending, Is.False);

		var capAndRatio = CompositorSubRendererRateLimitHelper.Unlimited.WithFrameCap(10).WithFrameRatio(10);
		Assert.That(CompositorSubRendererRateLimitHelper.ExecuteCompositorFrameAndDetermineIfShouldRender(ref capAndRatio, TicksPerSecond), Is.True);
		Assert.That(capAndRatio.InitialPostConfigChangeFrameStillPending, Is.False);
	}

	[Test]
	public void ShouldForceRenderOnFirstFrameEvenWhenReconfiguredMidCycle() {
		const int LoopHz = 120;
		var loopIncrement = TicksPerSecond / LoopHz;
		var timestamp = TicksPerSecond;

		var limitHelper = CompositorSubRendererRateLimitHelper.Unlimited.WithFrameCap(10);
		for (var i = 0; i < 5; ++i) {
			_ = CompositorSubRendererRateLimitHelper.ExecuteCompositorFrameAndDetermineIfShouldRender(ref limitHelper, timestamp);
			timestamp += loopIncrement;
		}
		Assert.That(CompositorSubRendererRateLimitHelper.ExecuteCompositorFrameAndDetermineIfShouldRender(ref limitHelper, timestamp), Is.False, "Test premise broken: helper should be mid-cycle and not due here.");

		limitHelper = limitHelper.WithFrameRatio(6);
		timestamp += loopIncrement;
		Assert.That(CompositorSubRendererRateLimitHelper.ExecuteCompositorFrameAndDetermineIfShouldRender(ref limitHelper, timestamp), Is.True, "Adding a ratio mid-cycle did not force an immediate render.");
	}

	[Test]
	public void ShouldResumeNormalPacingAfterAForcedRender() {
		const int LoopHz = 120;
		const int CapHz = 10;
		const int SimulatedSeconds = 10;

		var limitHelper = CompositorSubRendererRateLimitHelper.Unlimited.WithFrameCap(CapHz);
		var renderedFrameIndices = GetRenderedFrameIndices(ref limitHelper, TicksPerSecond, TicksPerSecond / LoopHz, LoopHz * SimulatedSeconds);

		Assert.That(renderedFrameIndices[0], Is.Zero, "The forced render did not occur on the very first frame.");
		Assert.That(renderedFrameIndices.Count / (double) SimulatedSeconds, Is.EqualTo(CapHz).Within(CapHz * 0.03d));

		var framesBetweenFirstTwoRenders = renderedFrameIndices[1] - renderedFrameIndices[0];
		Assert.That(framesBetweenFirstTwoRenders, Is.EqualTo(LoopHz / CapHz).Within(1), "Pacing did not resume from the forced render's timestamp.");
	}
}
