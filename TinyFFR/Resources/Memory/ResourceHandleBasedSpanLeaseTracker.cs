// Created on 2026-06-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Resources.Memory;

sealed unsafe class ResourceHandleBasedSpanLeaseTracker<T> : IDisposable {
	readonly int _maxRentalsPerHandle;
	readonly ArrayPoolBackedMap<ResourceHandle, int> _activeRentalCounts = new();
	readonly ArrayPoolBackedMap<nuint, ResourceHandle> _activeRentalIds = new();
	nuint _prevRentalId = 0U;

	public ResourceHandleBasedSpanLeaseTracker(int maxRentalsPerHandle) {
		if (maxRentalsPerHandle <= 0) throw new ArgumentOutOfRangeException(nameof(maxRentalsPerHandle), maxRentalsPerHandle, $"Max rentals per handle should be positive.");
		_maxRentalsPerHandle = maxRentalsPerHandle;
	}
	
	public ScopedSpanLease<T> CreateScopedLeaseOrThrow(ResourceHandle handle, Span<T> span) {
		return new(
			this,
			RecordRentalIfPermittedOrThrow(handle),
			&HandleDisposal,
			span
		);
	}
	
	public ScopedReadOnlySpanLease<T> CreateScopedLeaseOrThrow(ResourceHandle handle, ReadOnlySpan<T> span) {
		return new(
			this,
			RecordRentalIfPermittedOrThrow(handle),
			&HandleDisposal,
			span
		);
	}
	
	public int GetActiveRentalsCount(ResourceHandle handle) => _activeRentalCounts.TryGetValue(handle, out var result) ? result : 0;

	public void Dispose() {
		_activeRentalIds.Dispose();
		_activeRentalCounts.Dispose();
	}

	nuint RecordRentalIfPermittedOrThrow(ResourceHandle handle) {
		if (_activeRentalCounts.TryGetValue(handle, out var curRentalCount)) {
			if (curRentalCount == _maxRentalsPerHandle) {
				throw new InvalidOperationException($"Can not create scoped {typeof(T).Name} span lease as there are already {curRentalCount} active lease(s).");
			}
		}
		else curRentalCount = 0;
		
		_activeRentalCounts[handle] = curRentalCount + 1;
		_activeRentalIds[++_prevRentalId] = handle;
		return _prevRentalId;
	}
	
	static void HandleDisposal(object tracker, nuint id) {
		var t = (ResourceHandleBasedSpanLeaseTracker<T>) tracker;
		if (!t._activeRentalIds.Remove(id, out var handle)) return;
		var newRentalCount = t._activeRentalCounts[handle] - 1;
		if (newRentalCount == 0) t._activeRentalCounts.Remove(handle);
		else t._activeRentalCounts[handle] = newRentalCount;
	}
}