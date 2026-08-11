// Created on 2026-08-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Diagnostics;

namespace Egodystonic.TinyFFR.Rendering.Local;

readonly record struct CompositorSubRendererRateLimitHelper {
	public static readonly CompositorSubRendererRateLimitHelper Unlimited = new();

	public long RateCapPeriodTicks { get; init; } = 0L;
	public long RateCapAccumulatedTicks { get; init; } = 0L;
	public long PreviousCompositorFrameTimestamp { get; init; } = 0L;
	public int RateRatio { get; init; } = 1;
	public int ConsecutiveSkippedFramesCount { get; init; } = 0;
	public bool InitialPostConfigChangeFrameStillPending { get; init; } = false;

	public CompositorSubRendererRateLimitHelper() { }

	public bool CapEnabled => RateCapPeriodTicks > 0L;
	public bool RatioEnabled => RateRatio > 1;
	public bool CapOrRatioEnabled => CapEnabled || RatioEnabled;
	public int? RateCapHz => CapEnabled ? (int) (Stopwatch.Frequency / RateCapPeriodTicks) : null;

	public static long TicksPerFrameAtFrameRate(int framesPerSecond) => Stopwatch.Frequency / framesPerSecond;

	public CompositorSubRendererRateLimitHelper WithFrameCap(int? framesPerSecond) {
		return this with {
			RateCapPeriodTicks = framesPerSecond is { } fps ? TicksPerFrameAtFrameRate(fps) : 0L,
			RateCapAccumulatedTicks = 0L,
			PreviousCompositorFrameTimestamp = 0L,
			InitialPostConfigChangeFrameStillPending = true
		};
	}

	public CompositorSubRendererRateLimitHelper WithFrameRatio(int newFrameRatio) {
		return this with {
			RateRatio = newFrameRatio,
			ConsecutiveSkippedFramesCount = 0,
			InitialPostConfigChangeFrameStillPending = true
		};
	}

	public static bool ExecuteCompositorFrameAndDetermineIfShouldRender(ref CompositorSubRendererRateLimitHelper @this, long currentTimestamp) {
		const int CapToleranceShift = 6;

		if (@this.InitialPostConfigChangeFrameStillPending) {
			@this = @this with {
				InitialPostConfigChangeFrameStillPending = false,
				RateCapAccumulatedTicks = 0L,
				PreviousCompositorFrameTimestamp = currentTimestamp,
				ConsecutiveSkippedFramesCount = 0
			};
			return true;
		}

		var accumulatedTicks = @this.RateCapAccumulatedTicks;

		if (@this.CapEnabled) {
			if (@this.PreviousCompositorFrameTimestamp == 0L) {
				accumulatedTicks = @this.RateCapPeriodTicks;
			}
			else {
				var intervalToPreviousFrameTicks = currentTimestamp - @this.PreviousCompositorFrameTimestamp;
				if (intervalToPreviousFrameTicks < 0L) intervalToPreviousFrameTicks = 0L;
				else if (intervalToPreviousFrameTicks > @this.RateCapPeriodTicks) intervalToPreviousFrameTicks = @this.RateCapPeriodTicks;
				accumulatedTicks += intervalToPreviousFrameTicks;
			}
		}

		var framesSincePreviousRender = @this.ConsecutiveSkippedFramesCount + 1;
		var capIsDue = !@this.CapEnabled || accumulatedTicks + (@this.RateCapPeriodTicks >> CapToleranceShift) >= @this.RateCapPeriodTicks;
		var ratioIsDue = @this.RateRatio <= 1 || framesSincePreviousRender >= @this.RateRatio;

		if (capIsDue && ratioIsDue) {
			@this = @this with {
				RateCapAccumulatedTicks = @this.CapEnabled ? accumulatedTicks - @this.RateCapPeriodTicks : 0L,
				PreviousCompositorFrameTimestamp = currentTimestamp,
				ConsecutiveSkippedFramesCount = 0
			};
			return true;
		}

		@this = @this with {
			RateCapAccumulatedTicks = accumulatedTicks,
			PreviousCompositorFrameTimestamp = currentTimestamp,
			ConsecutiveSkippedFramesCount = framesSincePreviousRender
		};
		return false;
	}
}
