// Created on 2026-06-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR.Resources.Memory;

interface IResourceHandleBasedSpanLeaseTracker<T> : IDisposable {
	ScopedSpanLease<T> CreateScopedLeaseOrThrow(ResourceHandle handle, Span<T> span);
	ScopedReadOnlySpanLease<T> CreateScopedLeaseOrThrow(ResourceHandle handle, ReadOnlySpan<T> span);
	int GetActiveRentalsCount(ResourceHandle handle);
	void ThrowIfAnyActiveRentals(ResourceHandle handle, ReadOnlySpan<char> targetResourceTypeName, ReadOnlySpan<char> targetResourceName);
}

sealed unsafe class ResourceHandleBasedSpanLeaseTracker<T> : IResourceHandleBasedSpanLeaseTracker<T> {
	readonly int? _maxRentalsPerHandle;
	readonly bool _throwOnDisposeIfAnyActiveRentals;
	readonly bool _failFastInsteadOfThrowingOnDispose;
	readonly delegate* managed<object?, ResourceHandle, nuint, int, void> _disposalCallback;
	readonly object? _disposalCallbackArg;
	readonly ArrayPoolBackedMap<ResourceHandle, int> _activeRentalCounts = new();
	readonly ArrayPoolBackedMap<nuint, ResourceHandle> _activeRentalIds = new();
	nuint _prevRentalId = 0U;

	public ResourceHandleBasedSpanLeaseTracker(int? maxRentalsPerHandle, bool throwOnDisposeIfAnyActiveRentals, bool failFastInsteadOfThrowingOnDispose) : this(maxRentalsPerHandle, throwOnDisposeIfAnyActiveRentals, failFastInsteadOfThrowingOnDispose, null, null) { }
	public ResourceHandleBasedSpanLeaseTracker(int? maxRentalsPerHandle, bool throwOnDisposeIfAnyActiveRentals, bool failFastInsteadOfThrowingOnDispose, delegate* managed<object?, ResourceHandle, nuint, int, void> disposalCallback, object? disposalCallbackArg) {
		if (maxRentalsPerHandle <= 0) throw new ArgumentOutOfRangeException(nameof(maxRentalsPerHandle), maxRentalsPerHandle, $"Max rentals per handle should be positive or null.");
		_maxRentalsPerHandle = maxRentalsPerHandle;
		_throwOnDisposeIfAnyActiveRentals = throwOnDisposeIfAnyActiveRentals;
		_failFastInsteadOfThrowingOnDispose = failFastInsteadOfThrowingOnDispose;
		_disposalCallback = disposalCallback;
		_disposalCallbackArg = disposalCallbackArg;
	}
	
	public ScopedSpanLease<T> CreateScopedLeaseOrThrow(ResourceHandle handle, Span<T> span) {
		return new(&HandleDisposal, this, RecordRentalIfPermittedOrThrow(handle), span);
	}
	
	public ScopedReadOnlySpanLease<T> CreateScopedLeaseOrThrow(ResourceHandle handle, ReadOnlySpan<T> span) {
		return new(&HandleDisposal, this, RecordRentalIfPermittedOrThrow(handle), span);
	}
	
	public int GetActiveRentalsCount(ResourceHandle handle) => _activeRentalCounts.TryGetValue(handle, out var result) ? result : 0;

	public void ThrowIfAnyActiveRentals(ResourceHandle handle, ReadOnlySpan<char> targetResourceTypeName, ReadOnlySpan<char> targetResourceName) {
		if (GetActiveRentalsCount(handle) == 0) return;
		
		var rentalStrings = _activeRentalIds
			.Where(kvp => kvp.Value == handle)
			.Select(kvp => $"Leased {typeof(T)} span #{kvp.Key}")
			.ToArray();
		
		throw ResourceDependencyException.CreateForPrematureDisposalOrMutation(
			targetResourceTypeName.ToString(),
			targetResourceName.ToString(),
			rentalStrings
		);
	}

	public void Dispose() {
		if (_throwOnDisposeIfAnyActiveRentals && _activeRentalCounts.Count > 0) {
			var message = 
				$"Disposal operation can not be executed safely as there is still at " +
				$"least one active scoped span lease of type {typeof(T).Name} not yet disposed. " +
				$"Note that this exception should not be ignored/swallowed/hidden as it represents " +
				$"a non-recoverable failed attempt to dispose internal buffers that could not be completed " +
				$"due to those buffers being actively held by the library user. " +
				$"Attempting to use TinyFFR from this point may result in undefined behaviour " +
				$"(including potential security risks). Note that if the factory was created with " +
				$"the given {nameof(LocalTinyFfrFactoryConfig)}'s {nameof(LocalTinyFfrFactoryConfig.EnhanceSecurity)} " +
				$"property set to true, this condition will kill the running application immediately instead of " +
				$"throwing an exception.";
			
			if (_failFastInsteadOfThrowingOnDispose) {
				System.Environment.FailFast(message);
			}
			else {
				throw new InvalidOperationException(message);	
			}
		} 
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
	
	static void HandleDisposal(object? tracker, nuint id) {
		var @this = (ResourceHandleBasedSpanLeaseTracker<T>) tracker!;
		if (!@this._activeRentalIds.Remove(id, out var handle)) return;
		var newRentalCount = @this._activeRentalCounts[handle] - 1;
		if (newRentalCount == 0) @this._activeRentalCounts.Remove(handle);
		else @this._activeRentalCounts[handle] = newRentalCount;
		
		if (@this._disposalCallback != null) {
			@this._disposalCallback(@this._disposalCallbackArg, handle, id, newRentalCount);
		}
	}
}

sealed unsafe class ResourceHandleBasedSpanLeaseTracker<T, TArg> : IResourceHandleBasedSpanLeaseTracker<T> {
	readonly ResourceHandleBasedSpanLeaseTracker<T> _unaryTracker;
	readonly delegate* managed<object?, ResourceHandle, TArg, int, void> _disposalCallback;
	readonly object? _disposalCallbackArg;
	readonly ArrayPoolBackedMap<nuint, TArg> _trackedRentals = new();
	
	public ResourceHandleBasedSpanLeaseTracker(int? maxRentalsPerHandle, bool throwOnDisposeIfAnyActiveRentals, bool failFastInsteadOfThrowingOnDispose, delegate* managed<object?, ResourceHandle, TArg, int, void> disposalCallback, object? disposalCallbackArg) {
		_unaryTracker = new(maxRentalsPerHandle, throwOnDisposeIfAnyActiveRentals, failFastInsteadOfThrowingOnDispose, &HandleDisposalCallback, this);
		_disposalCallback = disposalCallback;
		_disposalCallbackArg = disposalCallbackArg;
	}
	
	public ScopedSpanLease<T> CreateScopedLeaseOrThrow(ResourceHandle handle, Span<T> span) => _unaryTracker.CreateScopedLeaseOrThrow(handle, span);
	public ScopedReadOnlySpanLease<T> CreateScopedLeaseOrThrow(ResourceHandle handle, ReadOnlySpan<T> span) => _unaryTracker.CreateScopedLeaseOrThrow(handle, span);
	public int GetActiveRentalsCount(ResourceHandle handle) => _unaryTracker.GetActiveRentalsCount(handle);
	public void ThrowIfAnyActiveRentals(ResourceHandle handle, ReadOnlySpan<char> targetResourceTypeName, ReadOnlySpan<char> targetResourceName) => _unaryTracker.ThrowIfAnyActiveRentals(handle, targetResourceTypeName, targetResourceName);
	
	public ScopedSpanLease<T> CreateScopedLeaseOrThrow(ResourceHandle handle, Span<T> span, TArg arg) {
		var result = CreateScopedLeaseOrThrow(handle, span);
		_trackedRentals[result.LeaseId] = arg;
		return result;
	}
	public ScopedReadOnlySpanLease<T> CreateScopedLeaseOrThrow(ResourceHandle handle, ReadOnlySpan<T> span, TArg arg) {
		var result = CreateScopedLeaseOrThrow(handle, span);
		_trackedRentals[result.LeaseId] = arg;
		return result;
	}
	
	static void HandleDisposalCallback(object? binaryTrackerRef, ResourceHandle handle, nuint leaseId, int numRentalsRemaining) {
		var @this = (ResourceHandleBasedSpanLeaseTracker<T, TArg>) binaryTrackerRef!;
		if (!@this._trackedRentals.Remove(leaseId, out var arg)) return;
		@this._disposalCallback(@this._disposalCallbackArg, handle, arg, numRentalsRemaining);
	}
	
	public void Dispose() {
		_trackedRentals.Dispose();
		_unaryTracker.Dispose();
	}
}