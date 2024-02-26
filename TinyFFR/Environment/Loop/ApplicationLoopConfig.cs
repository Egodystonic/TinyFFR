// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment;

public readonly record struct ApplicationLoopConfig {
	internal readonly TimeSpan FrameInterval = TimeSpan.Zero;
	internal readonly TimeSpan MaxCpuBusyWaitTime = TimeSpan.FromMilliseconds(1d);

	public int? FrameRateCapHz {
		get {
			return FrameInterval <= TimeSpan.Zero ? null : (int) Math.Round(TimeSpan.FromSeconds(1d) / FrameInterval, 0, MidpointRounding.AwayFromZero);
		}
		init {
			if (value <= 0) {
				throw new ArgumentOutOfRangeException(nameof(FrameRateCapHz), value, $"Frame rate cap must be a positive value, or 'null' for no cap.");
			}
			FrameInterval = value != null ? (TimeSpan.FromSeconds(1d) / value.Value) : TimeSpan.Zero;
		}
	}

	public bool WaitForVSync { get; init; } = false;

	public TimeSpan FrameTimingPrecisionBusyWaitTime {
		get {
			return MaxCpuBusyWaitTime;
		}
		init {
			if (value < TimeSpan.Zero) {
				throw new ArgumentOutOfRangeException(nameof(FrameTimingPrecisionBusyWaitTime), value, "Value must be positive or zero.");
			}
			MaxCpuBusyWaitTime = value;
		}
	}

	public ApplicationLoopConfig() { }

#pragma warning disable CA1822 // "Could be static" - Yes, for now. Keeping this as a placeholder for future.
	internal void ThrowIfInvalid() {
		/* no op */
	}
#pragma warning restore CA1822
}