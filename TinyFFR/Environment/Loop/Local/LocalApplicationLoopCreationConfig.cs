// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Environment.Local;

/// <summary>
/// Configuration for creating a new <see cref="ApplicationLoop"/> with a local factory.
/// </summary>
public readonly ref struct LocalApplicationLoopCreationConfig : IConfigStruct<LocalApplicationLoopCreationConfig> {
	internal readonly TimeSpan MaxCpuBusyWaitTime = TimeSpan.FromMilliseconds(1d);

	/// <summary>
	/// The configuration common to all application loops, regardless of factory type.
	/// </summary>
	public ApplicationLoopCreationConfig BaseConfig { get; private init; } = new();
	/// <inheritdoc cref="ApplicationLoopCreationConfig.FrameRateCapHz"/>
	public int? FrameRateCapHz {
		get => BaseConfig.FrameRateCapHz;
		init => BaseConfig = BaseConfig with { FrameRateCapHz = value };
	}

	/// <summary>
	/// How much of the wait before each iteration the loop may spend busy-waiting rather than sleeping, in order to start the iteration on time. Must not be negative. Defaults to one millisecond.
	/// </summary>
	/// <remarks>
	/// Sleeping a thread is cheap but imprecise: the operating system guarantees only that it will wake the thread no sooner than asked, and overshoots of a
	/// millisecond or more are normal. Busy-waiting (repeatedly checking the clock) is precise but occupies a CPU core for its whole duration. This loop therefore
	/// does both, sleeping for all but the last stretch of the wait and busy-waiting through that stretch; this value is how long that stretch may be. Raising it
	/// buys steadier frame pacing at the cost of CPU time (and, on battery-powered devices, power); setting it to <see cref="TimeSpan.Zero"/> disables busy-waiting
	/// altogether, leaving the loop entirely at the mercy of the operating system's sleep precision.
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when set to a negative value.</exception>
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

	/// <inheritdoc cref="ApplicationLoopCreationConfig.Name"/>
	public ReadOnlySpan<char> Name {
		get => BaseConfig.Name;
		init => BaseConfig = BaseConfig with { Name = value };
	}

	/// <summary>
	/// Whether each iteration of this loop should poll the operating system for new input, refreshing <see cref="ApplicationLoop.Input"/>. Defaults to <see langword="null"/>,
	/// meaning the loop polls input only if no other application loop is active at the moment it is created.
	/// </summary>
	/// <remarks>
	/// Input is polled once per iteration for the whole application rather than per-loop, so only one loop should normally have this enabled. Leaving this as
	/// <see langword="null"/> gives the first-created loop that role and disables it on any loop created while another is still active (such as a sub-tick loop),
	/// which suits the common case where the first loop is the primary one.
	/// <para>
	/// Set this explicitly to override that choice: <see langword="true"/> to always poll, or <see langword="false"/> where something else is responsible for
	/// supplying input, such as a host UI framework that delivers its own input events (as the WPF, Avalonia and WinForms integrations do). A loop that does not
	/// poll input still keeps time and still executes pending primary-thread work as normal.
	/// </para>
	/// </remarks>
	public bool? IterationShouldPumpSystemEventQueue { get; init; } = null;

	/// <summary>
	/// Constructs a new <see cref="LocalApplicationLoopCreationConfig"/> with default values for every setting.
	/// </summary>
	public LocalApplicationLoopCreationConfig() { }
	/// <summary>
	/// Constructs a new <see cref="LocalApplicationLoopCreationConfig"/> that takes its common settings from <paramref name="baseConfig"/> and uses default values for the local-only ones.
	/// </summary>
	/// <param name="baseConfig">The configuration to use as this config's <see cref="BaseConfig"/>.</param>
	public LocalApplicationLoopCreationConfig(ApplicationLoopCreationConfig baseConfig) => BaseConfig = baseConfig;

	internal void ThrowIfInvalid() {
		BaseConfig.ThrowIfInvalid();
	}

	/// <inheritdoc/>
	public static int GetHeapStorageFormattedLength(in LocalApplicationLoopCreationConfig src) {
		return	SerializationSizeOfSubConfig(src.BaseConfig) // BaseConfig
			+	SerializationSizeOfLong() // FrameTimingPrecisionBusyWaitTime
			+	SerializationSizeOfNullableBool(); // IterationShouldRefreshGlobalInputStates
	}
	/// <inheritdoc/>
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in LocalApplicationLoopCreationConfig src) {
		SerializationWriteSubConfig(ref dest, src.BaseConfig);
		SerializationWriteLong(ref dest, src.FrameTimingPrecisionBusyWaitTime.Ticks);
		SerializationWriteNullableBool(ref dest, src.IterationShouldPumpSystemEventQueue);
	}
	/// <inheritdoc/>
	public static LocalApplicationLoopCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new() {
			BaseConfig = SerializationReadSubConfig<ApplicationLoopCreationConfig>(ref src),
			FrameTimingPrecisionBusyWaitTime = TimeSpan.FromTicks(SerializationReadLong(ref src)),
			IterationShouldPumpSystemEventQueue = SerializationReadNullableBool(ref src)
		};
	}
	/// <inheritdoc/>
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}