// Created on 2026-08-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Diagnostics;
using System.Threading;
using Egodystonic.TinyFFR.Factory;

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

	// Maintainer's note: This isn't meant to be used exhaustively everywhere throughout the library, just in choice places
	// that are off the hot path and would have the most destructive impact if not checked
	public static bool CurrentThreadIsPrimary() {
		lock (_staticMutationLock) {
			return _primaryThread == Thread.CurrentThread;
		}
	}

	public static void AssertCurrentThreadIsPrimary() {
		lock (_staticMutationLock) {
			if (_primaryThread != Thread.CurrentThread) {
				throw new InvalidOperationException(
					$"TinyFFR: The requested operation can not be executed from the current thread (\"{Thread.CurrentThread}\"), only the primary thread (\"{_primaryThread}\"). " +
					$"The primary thread is set as the thread used to create the active {nameof(ITinyFfrFactory)}."
				);
			}
		}
	}
}
