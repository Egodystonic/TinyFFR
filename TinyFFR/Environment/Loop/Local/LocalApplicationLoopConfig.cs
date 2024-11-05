// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Local;

public readonly ref struct LocalApplicationLoopConfig {
	internal readonly TimeSpan MaxCpuBusyWaitTime = TimeSpan.FromMilliseconds(1d);

	public ApplicationLoopConfig BaseConfig { get; private init; }
	public int? FrameRateCapHz {
		get => BaseConfig.FrameRateCapHz;
		init => BaseConfig = BaseConfig with { FrameRateCapHz = value };
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

	public ReadOnlySpan<char> NameAsSpan {
		get => BaseConfig.NameAsSpan;
		init => BaseConfig = BaseConfig with { NameAsSpan = value };
	}
	public string Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	public LocalApplicationLoopConfig() { }
	public LocalApplicationLoopConfig(ApplicationLoopConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
	}
}