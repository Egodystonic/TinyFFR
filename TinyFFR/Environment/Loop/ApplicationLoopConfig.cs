// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment;

public readonly ref struct ApplicationLoopConfig {
	internal readonly TimeSpan FrameInterval = TimeSpan.Zero;

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

	public ReadOnlySpan<char> NameAsSpan { get; init; }
	public string Name {
		get => new(NameAsSpan);
		init => NameAsSpan = value.AsSpan();
	}

	public ApplicationLoopConfig() { }

#pragma warning disable CA1822 // "Could be static" - Yes, for now. Keeping this as a placeholder for future.
	internal void ThrowIfInvalid() {
		/* no op */
	}
#pragma warning restore CA1822
}