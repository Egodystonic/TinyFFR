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
}