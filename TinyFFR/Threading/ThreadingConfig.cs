// Created on 2026-08-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Threading;

public sealed class ThreadingConfig {
	public static readonly TimeSpan DefaultMaxShutdownWaitTime = TimeSpan.FromSeconds(10d);
	public static readonly TimeSpan MaxMaxShutdownWaitTime = TimeSpan.FromMilliseconds(Int32.MaxValue); // From Thread.Join constraint
	public static readonly TimeSpan DefaultHostPumpTimeCap = TimeSpan.FromMilliseconds(2d);
	
	public int? WorkerThreadCount {
		get;
		init {
			if (value is < 0) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Worker thread count can not be negative.");
			}
			field = value;
		}
	} = null;
	
	public TimeSpan MaxShutdownWaitTime {
		get;
		init {
			if (value < TimeSpan.Zero || value > MaxMaxShutdownWaitTime) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max shutdown wait time can not be negative or greater than {nameof(MaxMaxShutdownWaitTime)} ({MaxMaxShutdownWaitTime}).");
			}
			field = value;
		}
	} = DefaultMaxShutdownWaitTime;

	public bool WakeHostMessageLoopForPrimaryThreadWork { get; init; } = true;

	public TimeSpan HostPumpTimeCap {
		get;
		init {
			if (value <= TimeSpan.Zero) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Host pump time cap must be greater than zero.");
			}
			field = value;
		}
	} = DefaultHostPumpTimeCap;
}