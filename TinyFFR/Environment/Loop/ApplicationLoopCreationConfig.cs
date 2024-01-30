// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Loop;

public readonly record struct ApplicationLoopCreationConfig {
	internal readonly TimeSpan FrameInterval = TimeSpan.Zero;
	public int? FrameRateCapHz {
		get {
			return FrameInterval <= TimeSpan.Zero ? null : (int) Math.Round(TimeSpan.FromSeconds(1d) / FrameInterval, 0, MidpointRounding.AwayFromZero);
		}
		init {
			if (value <= 0) {
				throw new ArgumentOutOfRangeException(nameof(FrameRateCapHz), value, $"Frame rate cap must be a positive integer, or 'null' for no cap.");
			}
			FrameInterval = value != null ? (TimeSpan.FromSeconds(1d) / value.Value) : TimeSpan.Zero;
		}
	}
	public bool WaitForVSync { get; init; } = false;

	public ApplicationLoopCreationConfig() { }

	internal void ThrowIfInvalid() {
		
	}
}