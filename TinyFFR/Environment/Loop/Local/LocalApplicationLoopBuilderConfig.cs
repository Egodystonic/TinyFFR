// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Local;

public sealed record LocalApplicationLoopBuilderConfig {
	public const int DefaultFrameRateBufferSizeLog2 = 8;
	public const int MaxFrameRateBufferSizeLog2 = 16;

	public int FrameRateBufferSizeLog2 {
		get;
		init {
			if (value is <= 0 or > MaxFrameRateBufferSizeLog2) {
				throw new ArgumentOutOfRangeException(nameof(FrameRateBufferSizeLog2), value, $"Must be between 1 and {MaxFrameRateBufferSizeLog2}.");
			}
			field = value;
		}
	} = DefaultFrameRateBufferSizeLog2;

	//TODO xmldoc Bounds the time each application loop iteration may spend executing pending primary-thread async work. There are
	//TODO xmldoc three modes:
	//TODO xmldoc   null (the default): no limit. Every pending task is executed before the iteration proceeds.
	//TODO xmldoc   TimeSpan.Zero: use only the iteration's idle time, i.e. the portion of the frame that IterateOnce() would
	//TODO xmldoc     otherwise have spent asleep waiting for the next frame to start. When the loop is running behind schedule
	//TODO xmldoc     there is no idle time, in which case exactly one pending task is executed so that pending work still
	//TODO xmldoc     progresses rather than starving entirely.
	//TODO xmldoc   Any positive value: that value is used as the per-iteration budget directly.
	//TODO xmldoc In all bounded modes the budget is only checked between tasks, never during one, so a single long-running task
	//TODO xmldoc can still overrun the frame.
	//TODO xmldoc TryIterateOnce() executes pending work on every call, including calls that return false because the next frame is
	//TODO xmldoc not yet due; its idle time is the time remaining until that next frame. Pass
	//TODO xmldoc executePendingPrimaryThreadCooperativeTasks: false to either method to skip executing pending work entirely.
	public TimeSpan? TargetPerFrameAsyncCooperativeTaskTime {
		get;
		init {
			if (value is not null && value < TimeSpan.Zero) {
				throw new ArgumentOutOfRangeException(nameof(TargetPerFrameAsyncCooperativeTaskTime), value, $"Can not be a negative value (use null for no limit, or {nameof(TimeSpan)}.{nameof(TimeSpan.Zero)} to use only idle time).");
			}
			field = value;
		}
	} = null;
}