// Created on 2026-08-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Runtime.ExceptionServices;
using System.Threading;

namespace Egodystonic.TinyFFR.Threading;

sealed class TinyFfrSynchronizationContext : SynchronizationContext {
	readonly IPrimaryThreadDispatcher _dispatcher;

	public TinyFfrSynchronizationContext(IPrimaryThreadDispatcher dispatcher) {
		ArgumentNullException.ThrowIfNull(dispatcher);
		_dispatcher = dispatcher;
	}

	public override void Post(SendOrPostCallback d, object? state) {
		ArgumentNullException.ThrowIfNull(d);
		_ = _dispatcher.SchedulePrimaryThreadContinuation(() => d(state));
	}

	public override void Send(SendOrPostCallback d, object? state) {
		ArgumentNullException.ThrowIfNull(d);

		if (ThreadSafetyTracker.CurrentThreadIsPrimary()) {
			d(state);
			return;
		}

		using var completionIndicator = new ManualResetEventSlim(false);
		ExceptionDispatchInfo? capturedException = null;
		var wasScheduled = _dispatcher.SchedulePrimaryThreadContinuation(() => {
			try {
				d(state);
			}
#pragma warning disable CA1031 // "Don't catch/swallow Exception" -- We're rethrowing it on the calling thread
			catch (Exception e) {
#pragma warning restore CA1031
				capturedException = ExceptionDispatchInfo.Capture(e);
			}
			finally {
				// ReSharper disable once AccessToDisposedClosure The wait below guarantees we outlive this callback
				completionIndicator.Set();
			}
		});

		if (!wasScheduled) {
			throw new ObjectDisposedException(
				nameof(TinyFfrSynchronizationContext),
				$"Can not send a callback to the primary thread: TinyFFR has been torn down and the primary thread is no longer accepting work."
			);
		}

		completionIndicator.Wait();
		capturedException?.Throw();
	}

	public override SynchronizationContext CreateCopy() => this;
}
