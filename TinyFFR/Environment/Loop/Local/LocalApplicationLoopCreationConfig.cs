// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Environment.Local;

public readonly ref struct LocalApplicationLoopCreationConfig : IConfigStruct<LocalApplicationLoopCreationConfig> {
	internal readonly TimeSpan MaxCpuBusyWaitTime = TimeSpan.FromMilliseconds(1d);

	public ApplicationLoopCreationConfig BaseConfig { get; private init; } = new();
	public int? FrameRateCapHz {
		get => BaseConfig.FrameRateCapHz;
		init => BaseConfig = BaseConfig with { FrameRateCapHz = value };
	} 

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

	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	public bool IterationShouldRefreshGlobalInputStates { get; init; } = true;

	public LocalApplicationLoopCreationConfig() { }
	public LocalApplicationLoopCreationConfig(ApplicationLoopCreationConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
	}

	public static int GetHeapStorageFormattedLength(in LocalApplicationLoopCreationConfig src) {
		return	SerializationSizeOfSubConfig(src.BaseConfig) // BaseConfig
			+	SerializationSizeOfLong() // FrameTimingPrecisionBusyWaitTime
			+	SerializationSizeOfBool(); // IterationShouldRefreshGlobalInputStates
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in LocalApplicationLoopCreationConfig src) {
		SerializationWriteSubConfig(ref dest, src.BaseConfig);
		SerializationWriteLong(ref dest, src.FrameTimingPrecisionBusyWaitTime.Ticks);
		SerializationWriteBool(ref dest, src.IterationShouldRefreshGlobalInputStates);
	}
	public static LocalApplicationLoopCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new() {
			BaseConfig = SerializationReadSubConfig<ApplicationLoopCreationConfig>(ref src),
			FrameTimingPrecisionBusyWaitTime = TimeSpan.FromTicks(SerializationReadLong(ref src)),
			IterationShouldRefreshGlobalInputStates = SerializationReadBool(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}