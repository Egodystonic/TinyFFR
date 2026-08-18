// Created on 2026-08-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Threading;

namespace Egodystonic.TinyFFR.Threading;

static class ThreadSafetyTracker {
	static readonly Lock _staticMutationLock = new();
	static Thread? _primaryThread;

	public static void SetPrimaryThread(Thread primaryThread) {
		ArgumentNullException.ThrowIfNull(primaryThread);
		lock (_staticMutationLock) {
			_primaryThread = primaryThread;
		}
	}

	public static void ClearPrimaryThread() {
		lock (_staticMutationLock) {
			_primaryThread = null;
		}
	}

	public static void AssertCurrentThreadIsPrimary() {
		lock (_staticMutationLock) {
			if (_primaryThread != Thread.CurrentThread) {
				throw new InvalidOperationException($"Current thread is {Thread.CurrentThread}, primary was previously marked as {_primaryThread}.");
			}
		}
	}
}
