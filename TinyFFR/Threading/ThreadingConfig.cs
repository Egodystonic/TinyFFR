// Created on 2026-08-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Threading;

/// <summary>
/// Object used to configure a <see cref="Egodystonic.TinyFFR.Factory.Local.LocalTinyFfrFactory"/>'s threading.
/// </summary>
public sealed class ThreadingConfig {
	public static readonly TimeSpan DefaultMaxShutdownWaitTime = TimeSpan.FromSeconds(10d);
	public static readonly TimeSpan MaxMaxShutdownWaitTime = TimeSpan.FromMilliseconds(Int32.MaxValue); // From Thread.Join constraint
	public static readonly TimeSpan DefaultHostPumpTimeCap = TimeSpan.FromMilliseconds(2d);
	
	/// <summary>
	/// Sets the max number of worker threads the factory is permitted to create, used to execute <see cref="TinyFfrAsyncOperation"/>s.
	/// </summary>
	/// <remarks>
	/// <ul>
	/// <li>A value of <c>null</c> (the default) lets the factory decide the best thread count for the host machine.</li>
	/// <li>A value of <c>0</c> disables asynchrony entirely: Invocations that create <see cref="TinyFfrAsyncOperation"/>s will be
	/// completed in their entirety at the point of invocation, blocking the caller for the entire duration, and only returning once
	/// the target asset is fully loaded.</li>
	/// </ul>
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if attempting to set a negative value.</exception>
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