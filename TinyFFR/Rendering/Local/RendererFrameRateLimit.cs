// Created on 2026-08-10 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Diagnostics;

namespace Egodystonic.TinyFFR.Rendering.Local;

readonly record struct RendererFrameRateLimit {
	const int CapToleranceShift = 6;

	public static readonly RendererFrameRateLimit None = new();

	public long CapIntervalTicks { get; init; } = 0L;
	public int FrameRatio { get; init; } = 1;
	public long AccumulatorTicks { get; init; } = 0L;
	public long PreviousTimestamp { get; init; } = 0L;
	public int FramesSincePreviousRender { get; init; } = 0;

	public RendererFrameRateLimit() { }

	public bool IsLimiting => CapIntervalTicks > 0L || FrameRatio > 1;

	public int? FrameRateCapHz => CapIntervalTicks > 0L ? (int) (Stopwatch.Frequency / CapIntervalTicks) : null;

	public static long TicksPerFrameAtFrameRate(int framesPerSecond) => Stopwatch.Frequency / framesPerSecond;

	public RendererFrameRateLimit WithCapIntervalTicks(long newCapIntervalTicks) {
		return this with {
			CapIntervalTicks = newCapIntervalTicks,
			AccumulatorTicks = 0L,
			PreviousTimestamp = 0L
		};
	}

	public RendererFrameRateLimit WithFrameRatio(int newFrameRatio) {
		return this with {
			FrameRatio = newFrameRatio,
			FramesSincePreviousRender = 0
		};
	}

	public static bool AdvanceAndDetermineIfDue(ref RendererFrameRateLimit limit, long currentTimestamp) {
		var capIntervalTicks = limit.CapIntervalTicks;
		var capIsActive = capIntervalTicks > 0L;
		var accumulatorTicks = limit.AccumulatorTicks;

		if (capIsActive) {
			if (limit.PreviousTimestamp == 0L) {
				accumulatorTicks = capIntervalTicks;
			}
			else {
				var elapsedTicks = currentTimestamp - limit.PreviousTimestamp;
				if (elapsedTicks < 0L) elapsedTicks = 0L;
				else if (elapsedTicks > capIntervalTicks) elapsedTicks = capIntervalTicks;
				accumulatorTicks += elapsedTicks;
			}
		}

		var framesSincePreviousRender = limit.FramesSincePreviousRender + 1;
		var capIsDue = !capIsActive || accumulatorTicks + (capIntervalTicks >> CapToleranceShift) >= capIntervalTicks;
		var ratioIsDue = limit.FrameRatio <= 1 || framesSincePreviousRender >= limit.FrameRatio;

		if (capIsDue && ratioIsDue) {
			limit = limit with {
				AccumulatorTicks = capIsActive ? accumulatorTicks - capIntervalTicks : 0L,
				PreviousTimestamp = currentTimestamp,
				FramesSincePreviousRender = 0
			};
			return true;
		}

		limit = limit with {
			AccumulatorTicks = accumulatorTicks,
			PreviousTimestamp = currentTimestamp,
			FramesSincePreviousRender = framesSincePreviousRender
		};
		return false;
	}
}
