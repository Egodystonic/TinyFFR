// Created on 2026-08-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Threading;

/// <summary>
/// Object used to configure a <see cref="Egodystonic.TinyFFR.Factory.Local.LocalTinyFfrFactory"/>'s threading.
/// </summary>
public sealed class ThreadingConfig {
	/// <summary>
	/// The default value of <see cref="MaxShutdownWaitTime"/> if not explicitly set.
	/// </summary>
	public static readonly TimeSpan DefaultMaxShutdownWaitTime = TimeSpan.FromSeconds(300d);
	/// <summary>
	/// The maximum permitted value of <see cref="MaxShutdownWaitTime"/>.
	/// </summary>
	public static readonly TimeSpan MaxMaxShutdownWaitTime = TimeSpan.FromMilliseconds(Int32.MaxValue); // From Thread.Join constraint
	/// <summary>
	/// The default value of <see cref="HostPumpTimeCap"/> if not explicitly set.
	/// </summary>
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
	
	/// <summary>
	/// The maximum amount of time the factory should wait for worker threads to complete their <see cref="TinyFfrAsyncOperation"/>s when shutting down (being disposed).
	/// Defaults to 5 minutes.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Invoking <c>Dispose()</c> on the factory can block the calling thread for up to this long in an attempt to gracefully wait for any ongoing async operations; after
	/// which any ongoing operations will be forcibly cancelled.
	/// </para>
	/// <para>
	/// Forcibly cancelling operations is generally undesirable as it creates a race condition where worker threads may attempt to access disposed resources or freed memory.
	/// Therefore avoiding this by waiting for outstanding async operations before disposing the factory is advisable. 
	/// </para>
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if attempting to set a negative value or a value greater than <see cref="MaxMaxShutdownWaitTime"/>.</exception>
	public TimeSpan MaxShutdownWaitTime {
		get;
		init {
			if (value < TimeSpan.Zero || value > MaxMaxShutdownWaitTime) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max shutdown wait time can not be negative or greater than {nameof(MaxMaxShutdownWaitTime)} ({MaxMaxShutdownWaitTime}).");
			}
			field = value;
		}
	} = DefaultMaxShutdownWaitTime;

	/// <summary>
	/// Whether the factory should actively wake up a host application's own message loop (e.g. a WPF, WinForms, or other UI framework's loop)
	/// as soon as primary-thread factory work becomes available, rather than waiting for that host to next poll for it itself. Defaults to <c>true</c>.
	/// </summary>
	/// <remarks>
	/// This only has an effect if a <see cref="System.Threading.SynchronizationContext"/> belonging to such a host is current at the point the factory is constructed.
	/// When it does apply, <see cref="HostPumpTimeCap"/> limits how long each such wake-up is permitted to spend executing pending work before returning control to the host's message loop.
	/// </remarks>
	public bool WakeHostMessageLoopForPrimaryThreadWork { get; init; } = true;

	/// <summary>
	/// The maximum amount of time a single host-triggered wake-up (see <see cref="WakeHostMessageLoopForPrimaryThreadWork"/>) is permitted to spend executing pending
	/// primary-thread factory work before returning control to the host's message loop.
	/// Defaults to <see cref="DefaultHostPumpTimeCap"/>.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if attempting to set a value less than or equal to <see cref="TimeSpan.Zero"/>.</exception>
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