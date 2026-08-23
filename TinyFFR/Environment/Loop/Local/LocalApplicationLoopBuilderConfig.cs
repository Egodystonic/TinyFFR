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
	//TODO xmldoc   null: no limit. Every pending task is executed before the iteration proceeds.
	//TODO xmldoc   0f: exactly one pending task is executed per iteration, so pending work still progresses rather than starving
	//TODO xmldoc     entirely, but never occupies more than a single task's worth of any given frame.
	//TODO xmldoc   Any value up to and including 1f (the default is 0.25f): that fraction of the measured iteration interval is used
	//TODO xmldoc     as the per-iteration budget. Because the budget scales with the observed frame time it adapts automatically to
	//TODO xmldoc     the display's refresh rate, including when frame pacing comes from vsync rather than from FrameRateCapHz.
	//TODO xmldoc The interval the fraction is applied to deliberately excludes the time previously spent executing cooperative tasks.
	//TODO xmldoc Measuring the raw iteration interval instead would let the budget feed back into itself (more task time makes frames
	//TODO xmldoc longer, which grants more task time), so the budget is keyed on the iteration interval as it would be without any
	//TODO xmldoc cooperative work in it.
	//TODO xmldoc In all bounded modes the budget is only checked between tasks, never during one, so a single long-running task
	//TODO xmldoc can still overrun the frame. The first iteration of a loop has no measured interval yet, and executes exactly one
	//TODO xmldoc pending task: granting an unbounded budget before any interval has been measured would let a startup backlog push
	//TODO xmldoc the very first frame over the display's refresh interval, which the measurement would then adopt as the baseline.
	//TODO xmldoc TryIterateOnce() executes pending work on every call, including calls that return false because the next frame is
	//TODO xmldoc not yet due. Pass executePendingPrimaryThreadCooperativeTasks: false to either method to skip executing pending work
	//TODO xmldoc entirely.
	public float? TargetPerFrameAsyncCooperativeTaskTimeFraction {
		get;
		init {
			if (value is { } v && !(v >= 0f && v <= 1f)) {
				throw new ArgumentOutOfRangeException(nameof(TargetPerFrameAsyncCooperativeTaskTimeFraction), value, $"Must be a value between 0f and 1f inclusive (use null for no limit).");
			}
			field = value;
		}
	} = DefaultTargetPerFrameAsyncCooperativeTaskTimeFraction;
}
