// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Local;

public sealed record LocalApplicationLoopBuilderConfig {
	public const int DefaultFrameRateBufferSizeLog2 = 8;
	public const int MaxFrameRateBufferSizeLog2 = 16;
	public const float DefaultTargetPerFrameAsyncCooperativeTaskTimeFraction = 0.25f;

	public int FrameRateBufferSizeLog2 {
		get;
		init {
			if (value is <= 0 or > MaxFrameRateBufferSizeLog2) {
				throw new ArgumentOutOfRangeException(nameof(FrameRateBufferSizeLog2), value, $"Must be between 1 and {MaxFrameRateBufferSizeLog2}.");
			}
			field = value;
		}
	} = DefaultFrameRateBufferSizeLog2;

	//TODO xmldoc Bounds the time each application loop iteration may spend executing pending primary-thread async work, expressed
	//TODO xmldoc as a fraction of the measured iteration interval. There are three modes:
	//TODO xmldoc   null: no limit. Every pending task is executed before the iteration proceeds. Note that a large backlog can
	//TODO xmldoc     therefore consume an entire frame (or more), meaning nothing is rendered until the backlog drains, 
	//TODO xmldoc     giving the impression that the application has crashed/stalled for an unbounded amount of time (but
	//TODO xmldoc     actually completes everything in the fastest total time).
	//TODO xmldoc   0f: exactly one pending task is executed per iteration, so pending work still progresses rather than starving
	//TODO xmldoc     entirely, but never occupies more than a single task's worth of any given frame.
	//TODO xmldoc   Any positive value (the default): that fraction of the measured iteration interval is used as the
	//TODO xmldoc     per-iteration budget. Because the budget scales with the observed frame time it adapts automatically to the
	//TODO xmldoc     display's refresh rate, including when frame pacing comes from vsync rather than from FrameRateCapHz.
	//TODO xmldoc The most useful way to think about a positive value is its effect on frame rate: while there is pending work, the
	//TODO xmldoc resulting frame rate settles at approximately (nominal frame rate / (1 + fraction)), and the share of each frame
	//TODO xmldoc given over to pending work settles at approximately (fraction / (1 + fraction)). So 0.25f costs about a fifth of
	//TODO xmldoc each frame, 1f halves the frame rate, and 3f quarters it.
	//TODO xmldoc Values greater than 1f are permitted. They are intended for loading screens, where devoting most of each frame to
	//TODO xmldoc pending work is desirable: unlike null, a large fraction still guarantees a predictable frame cadence rather than
	//TODO xmldoc letting a backlog consume the frame entirely. Returns diminish above roughly 8f, which already yields ~89% of each
	//TODO xmldoc frame. The relationships above are asymptotic and remain stable at every fraction; no value causes runaway feedback.
	//TODO xmldoc The interval the fraction is applied to deliberately excludes the time previously spent executing cooperative tasks.
	//TODO xmldoc Measuring the raw iteration interval instead would let the budget feed back into itself (more task time makes frames
	//TODO xmldoc longer, which grants more task time), so the budget is keyed on the iteration interval as it would be without any
	//TODO xmldoc cooperative work in it.
	//TODO xmldoc In all modes at least one pending task is executed per iteration when any are pending, because the budget is only
	//TODO xmldoc checked between tasks and never during one. That also means a single long-running task can still overrun the frame.
	//TODO xmldoc The first iteration of a loop has no measured interval yet, and executes exactly one pending task: granting an
	//TODO xmldoc unbounded budget before any interval has been measured would let a startup backlog push the very first frame over
	//TODO xmldoc the display's refresh interval, which the measurement would then adopt as the baseline.
	//TODO xmldoc TryIterateOnce() executes pending work on every call, including calls that return false because the next frame is
	//TODO xmldoc not yet due. Pass executePendingPrimaryThreadCooperativeTasks: false to either method to skip executing pending work
	//TODO xmldoc entirely.
	public float? TargetPerFrameAsyncCooperativeTaskTimeFraction {
		get;
		init {
			if (value is { } v && !v.IsNonNegativeAndFinite()) {
				throw new ArgumentOutOfRangeException(nameof(TargetPerFrameAsyncCooperativeTaskTimeFraction), value, $"Must be a finite, non-negative value (use null for no limit).");
			}
			field = value;
		}
	} = DefaultTargetPerFrameAsyncCooperativeTaskTimeFraction;
}
