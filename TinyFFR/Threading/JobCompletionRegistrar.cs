// Created on 2026-08-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Diagnostics;
using System.Threading;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Threading;

sealed class JobCompletionRegistrar : IDisposable {
	readonly record struct StripedRegistrationBucket(object SyncObject, ArrayPoolBackedSet<ulong> RegisteredIds);
	const int NumBuckets = 64; // Must be power of two
	const int BucketHash = NumBuckets - 1;

	readonly StripedRegistrationBucket[] _buckets = new StripedRegistrationBucket[NumBuckets];
	volatile bool _isDisposed;

	public JobCompletionRegistrar() {
		for (var i = 0; i < NumBuckets; ++i) {
#pragma warning disable CA2000 // "Dispose object before losing scope" -- we're not losing scope, this is a Roslyn bug
			_buckets[i] = new StripedRegistrationBucket(new object(), new ArrayPoolBackedSet<ulong>());
#pragma warning restore CA2000
		}
	}

	public void RegisterInterest(ulong jobId) {
		var bucket = _buckets[jobId & BucketHash];
		lock (bucket.SyncObject) {
			ObjectDisposedException.ThrowIf(_isDisposed, this);
			bucket.RegisteredIds.Add(jobId);
		}
	}

	public bool WaitForCompletion(ulong jobId, TimeSpan timeout, CancellationToken cancellationToken) {
		var bucket = _buckets[jobId & BucketHash];
		var startTimestamp = Stopwatch.GetTimestamp();
		var hasTimeout = timeout >= TimeSpan.Zero;

		var cancellationRegistration = cancellationToken.CanBeCanceled
			? cancellationToken.UnsafeRegister(static state => { lock (state!) { Monitor.PulseAll(state); } }, bucket.SyncObject)
			: default;

		try {
			lock (bucket.SyncObject) {
				while (!_isDisposed && bucket.RegisteredIds.Contains(jobId)) {
					cancellationToken.ThrowIfCancellationRequested();

					var remainingTime = Timeout.InfiniteTimeSpan;
					if (hasTimeout) {
						remainingTime = timeout - Stopwatch.GetElapsedTime(startTimestamp);
						if (remainingTime <= TimeSpan.Zero) return false;
					}

					Monitor.Wait(bucket.SyncObject, remainingTime);
				}

				return !_isDisposed;
			}
		}
		finally {
			cancellationRegistration.Dispose();
		}
	}

	public void NotifyCompletion(ulong jobId) {
		var bucket = _buckets[jobId & BucketHash];
		lock (bucket.SyncObject) {
			if (_isDisposed) return;
			if (bucket.RegisteredIds.Remove(jobId)) Monitor.PulseAll(bucket.SyncObject);
		}
	}

	public void Dispose() {
		if (_isDisposed) return;
		_isDisposed = true;

		for (var i = 0; i < NumBuckets; ++i) {
			lock (_buckets[i].SyncObject) {
				_buckets[i].RegisteredIds.Clear();
				_buckets[i].RegisteredIds.Dispose();
				Monitor.PulseAll(_buckets[i].SyncObject);
			}
		}
	}
}
