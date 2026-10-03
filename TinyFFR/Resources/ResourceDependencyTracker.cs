// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using static Egodystonic.TinyFFR.Resources.IResourceDependencyTracker;
using StubMap = Egodystonic.TinyFFR.Resources.Memory.ArrayPoolBackedMap<Egodystonic.TinyFFR.Resources.ResourceIdent, Egodystonic.TinyFFR.Resources.Memory.ArrayPoolBackedSet<Egodystonic.TinyFFR.Resources.ResourceStub>>;

namespace Egodystonic.TinyFFR.Resources;

sealed unsafe class ResourceDependencyTracker : IResourceDependencyTracker, IDisposable {
	readonly SetPool<ResourceStub> _setPool = new(zeroMemoryOnReturn: false);
	readonly StubMap _targetsToDependentsMap = new();
	readonly StubMap _dependentsToTargetsMap = new();
	readonly ArrayPoolBackedMap<ResourceIdent, ResourceIdent> _ownedToOwnerMap = new();
	readonly ArrayPoolBackedVector<ResourceStub> _prematureDisposalClosure = new();
	readonly ArrayPoolBackedSet<ResourceIdent> _prematureDisposalClosureIdents = new();
	readonly ArrayPoolBackedMap<ResourceIdent, int> _orderingNodeMap = new();
	readonly ArrayPoolBackedVector<ResourceStub> _orderingClosure = new();
	readonly ArrayPoolBackedVector<int> _orderingClosureNodes = new();
	readonly ArrayPoolBackedVector<(int Before, int After)> _orderingEdges = new();
	bool _isDisposed = false;
	int _stateVersion = 0;

	public void RegisterDependency<TDependent, TTarget>(TDependent dependent, TTarget targetNowInUse) where TDependent : IResource where TTarget : IResource {
		ThrowIfDisposed();

		static void AddStubToMap(SetPool<ResourceStub> setPool, StubMap map, ResourceIdent key, ResourceStub value) {
			if (!map.TryGetValue(key, out var values)) {
				values = setPool.Rent();
				map.Add(key, values);
			}
			else if (values.Contains(value)) return;
			values.Add(value);
		}
		
		var dependentStub = new ResourceStub(dependent.Ident, dependent.Implementation);
		var targetStub = new ResourceStub(targetNowInUse.Ident, targetNowInUse.Implementation);
		AddStubToMap(_setPool, _targetsToDependentsMap, targetStub.Ident, dependentStub);
		AddStubToMap(_setPool, _dependentsToTargetsMap, dependentStub.Ident, targetStub);
		_stateVersion++;
	}

	public void DeregisterDependency<TDependent, TTarget>(TDependent dependent, TTarget targetNoLongerInUse) where TDependent : IResource where TTarget : IResource {
		ThrowIfDisposed();

		static void RemoveStubFromMap(SetPool<ResourceStub> setPool, StubMap map, ResourceIdent key, ResourceStub value) {
			if (!map.TryGetValue(key, out var values)) return;
			if (!values.Remove(value)) return;
			if (values.Count != 0) return;
			map.Remove(key);
			setPool.Return(values);
		}

		var dependentStub = new ResourceStub(dependent.Ident, dependent.Implementation);
		var targetStub = new ResourceStub(targetNoLongerInUse.Ident, targetNoLongerInUse.Implementation);
		RemoveStubFromMap(_setPool, _targetsToDependentsMap, targetStub.Ident, dependentStub);
		RemoveStubFromMap(_setPool, _dependentsToTargetsMap, dependentStub.Ident, targetStub);
		if (IsOwnedBy(dependentStub.Ident, targetStub.Ident)) _ownedToOwnerMap.Remove(dependentStub.Ident);
		_stateVersion++;
	}

	public void RegisterOwnership<TOwner, TOwned>(TOwner owner, TOwned owned) where TOwner : IResource where TOwned : IResource {
		RegisterDependency(owned, owner);
		_ownedToOwnerMap[owned.Ident] = owner.Ident;
	}

	bool IsOwnedBy(ResourceIdent candidate, ResourceIdent owner) => _ownedToOwnerMap.TryGetValue(candidate, out var actualOwner) && actualOwner == owner;

	bool HasOwnedDependents(ResourceIdent owner, ArrayPoolBackedSet<ResourceStub> dependents) {
		if (_ownedToOwnerMap.Count == 0) return false;
		foreach (var dependent in dependents) {
			if (IsOwnedBy(dependent.Ident, owner)) return true;
		}
		return false;
	}

	void AppendOwnershipClosure(ResourceStub root, ArrayPoolBackedVector<ResourceStub> dest, ReadOnlySpan<ResourceStub> excludedSubtreeRoots = default) {
		var index = dest.Count;
		dest.Add(root);
		if (_ownedToOwnerMap.Count == 0) return;
		for (; index < dest.Count; ++index) {
			var owner = dest[index];
			if (!_targetsToDependentsMap.TryGetValue(owner.Ident, out var dependents)) continue;
			foreach (var dependent in dependents) {
				if (!IsOwnedBy(dependent.Ident, owner.Ident)) continue;
				if (!excludedSubtreeRoots.IsEmpty && IsExcludedSubtreeRoot(dependent.Ident, excludedSubtreeRoots)) continue;
				dest.Add(dependent);
			}
		}
	}

	bool IsExcludedSubtreeRoot(ResourceIdent ident, ReadOnlySpan<ResourceStub> excludedSubtreeRoots) {
		return _orderingNodeMap.TryGetValue(ident, out var node) && node < excludedSubtreeRoots.Length && excludedSubtreeRoots[node].Ident == ident;
	}

	public void DeregisterAllDependencies<TDependent>(TDependent dependent) where TDependent : IResource {
		ThrowIfDisposed();

		_ownedToOwnerMap.Remove(dependent.Ident);
		if (!_dependentsToTargetsMap.TryGetValue(dependent.Ident, out var targets)) return;
		var dependentStub = new ResourceStub(dependent.Ident, dependent.Implementation);
		
		foreach (var target in targets) {
			if (!_targetsToDependentsMap.TryGetValue(target.Ident, out var dependents)) continue;
			if (!dependents.Remove(dependentStub)) continue;
			if (dependents.Count != 0) continue;
			_targetsToDependentsMap.Remove(target.Ident);
			_setPool.Return(dependents);
		}

		_dependentsToTargetsMap.Remove(dependentStub.Ident);
		_setPool.Return(targets);
		_stateVersion++;
	}

	public void ThrowForPrematureDisposalIfTargetHasDependents<TTarget>(TTarget targetPotentiallyInUse) where TTarget : IResource {
		ThrowIfDisposed();

		if (!_targetsToDependentsMap.TryGetValue(targetPotentiallyInUse.Ident, out var dependents)) return;
		if (!HasOwnedDependents(targetPotentiallyInUse.Ident, dependents)) {
			throw ResourceDependencyException.CreateForPrematureDisposalOrMutation(
				targetPotentiallyInUse.GetType().Name,
				targetPotentiallyInUse.GetNameAsNewStringObject(),
				dependents.Select(sr => sr.Implementation.GetNameAsNewStringObject(sr.Ident.RawResourceHandle).ToString()).ToArray()
			);
		}

		List<string>? externalDependentNames = null;
		try {
			AppendOwnershipClosure(new ResourceStub(targetPotentiallyInUse.Ident, targetPotentiallyInUse.Implementation), _prematureDisposalClosure);
			for (var i = 0; i < _prematureDisposalClosure.Count; ++i) _prematureDisposalClosureIdents.Add(_prematureDisposalClosure[i].Ident);

			for (var i = 0; i < _prematureDisposalClosure.Count; ++i) {
				if (!_targetsToDependentsMap.TryGetValue(_prematureDisposalClosure[i].Ident, out var closureMemberDependents)) continue;
				foreach (var dependent in closureMemberDependents) {
					if (_prematureDisposalClosureIdents.Contains(dependent.Ident)) continue;
					externalDependentNames ??= new();
					var name = dependent.Implementation.GetNameAsNewStringObject(dependent.Ident.RawResourceHandle);
					if (!externalDependentNames.Contains(name)) externalDependentNames.Add(name);
				}
			}
		}
		finally {
			for (var i = 0; i < _prematureDisposalClosure.Count; ++i) _prematureDisposalClosureIdents.Remove(_prematureDisposalClosure[i].Ident);
			ResetScratchVector(_prematureDisposalClosure);
		}

		if (externalDependentNames == null) return;
		throw ResourceDependencyException.CreateForPrematureDisposalOrMutation(
			targetPotentiallyInUse.GetType().Name,
			targetPotentiallyInUse.GetNameAsNewStringObject(),
			externalDependentNames
		);
	}

	public bool TryGetDisposalOrder(ReadOnlySpan<ResourceStub> resources, ResourceIdent ignoredDependent, Span<int> orderDest, out DisposalOrderFailure failure) {
		ThrowIfDisposed();
		if (orderDest.Length < resources.Length) throw new ArgumentException("Destination span is too small.", nameof(orderDest));

		failure = default;
		try {
			for (var i = 0; i < resources.Length; ++i) _orderingNodeMap.TryAdd(resources[i].Ident, i);
			for (var i = 0; i < resources.Length; ++i) {
				var closureStart = _orderingClosure.Count;
				AppendOwnershipClosure(resources[i], _orderingClosure, resources);
				for (var c = closureStart; c < _orderingClosure.Count; ++c) {
					_orderingClosureNodes.Add(i);
					if (c > closureStart) _orderingNodeMap.TryAdd(_orderingClosure[c].Ident, i);
				}
			}

			for (var c = 0; c < _orderingClosure.Count; ++c) {
				var member = _orderingClosure[c];
				var node = _orderingClosureNodes[c];
				if (!_targetsToDependentsMap.TryGetValue(member.Ident, out var dependents)) continue;
				foreach (var dependent in dependents) {
					if (dependent.Ident == ignoredDependent) continue;
					if (!_orderingNodeMap.TryGetValue(dependent.Ident, out var dependentNode)) {
						failure = new(member, dependent);
						return false;
					}
					if (dependentNode != node) _orderingEdges.Add((dependentNode, node));
				}
			}

			if (_orderingEdges.Count == 0) {
				for (var i = 0; i < resources.Length; ++i) orderDest[i] = resources.Length - 1 - i;
				return true;
			}

			return TryTopologicallySort(resources, orderDest, out failure);
		}
		finally {
			for (var i = 0; i < resources.Length; ++i) _orderingNodeMap.Remove(resources[i].Ident);
			for (var c = 0; c < _orderingClosure.Count; ++c) _orderingNodeMap.Remove(_orderingClosure[c].Ident);
			ResetScratchVector(_orderingClosure);
			_orderingClosureNodes.ClearWithoutZeroingMemory();
			_orderingEdges.ClearWithoutZeroingMemory();
		}
	}

	static void ResetScratchVector<T>(ArrayPoolBackedVector<T> vector) {
		vector.AsSpan[..vector.Count].Clear();
		vector.ClearWithoutZeroingMemory();
	}

	bool TryTopologicallySort(ReadOnlySpan<ResourceStub> resources, Span<int> orderDest, out DisposalOrderFailure failure) {
		var nodeCount = resources.Length;
		var edgeCount = _orderingEdges.Count;
		var intPool = TinyFfrArrayPool<int>.Shared;
		var inDegrees = intPool.Rent(nodeCount);
		var adjacencyStarts = intPool.Rent(nodeCount + 1);
		var adjacencyFill = intPool.Rent(nodeCount);
		var adjacency = intPool.Rent(edgeCount);
		var readyHeap = intPool.Rent(nodeCount);

		try {
			Array.Clear(inDegrees, 0, nodeCount);
			Array.Clear(adjacencyStarts, 0, nodeCount + 1);
			for (var e = 0; e < edgeCount; ++e) {
				var (before, after) = _orderingEdges[e];
				++adjacencyStarts[before + 1];
				++inDegrees[after];
			}
			for (var n = 0; n < nodeCount; ++n) {
				adjacencyStarts[n + 1] += adjacencyStarts[n];
				adjacencyFill[n] = adjacencyStarts[n];
			}
			for (var e = 0; e < edgeCount; ++e) {
				var (before, after) = _orderingEdges[e];
				adjacency[adjacencyFill[before]++] = after;
			}

			var heapCount = 0;
			for (var n = 0; n < nodeCount; ++n) {
				if (inDegrees[n] == 0) PushReadyNode(readyHeap, ref heapCount, n);
			}

			var orderCount = 0;
			while (heapCount > 0) {
				var node = PopReadyNode(readyHeap, ref heapCount);
				orderDest[orderCount++] = node;
				for (var a = adjacencyStarts[node]; a < adjacencyStarts[node + 1]; ++a) {
					var after = adjacency[a];
					if (--inDegrees[after] == 0) PushReadyNode(readyHeap, ref heapCount, after);
				}
			}

			if (orderCount == nodeCount) {
				failure = default;
				return true;
			}

			var blockedNode = 0;
			while (inDegrees[blockedNode] == 0) ++blockedNode;
			failure = new(resources[blockedNode], null);
			return false;
		}
		finally {
			intPool.Return(inDegrees);
			intPool.Return(adjacencyStarts);
			intPool.Return(adjacencyFill);
			intPool.Return(adjacency);
			intPool.Return(readyHeap);
		}
	}

	static void PushReadyNode(int[] heap, ref int count, int node) {
		var index = count++;
		heap[index] = node;
		while (index > 0) {
			var parent = (index - 1) >> 1;
			if (heap[parent] >= heap[index]) break;
			(heap[parent], heap[index]) = (heap[index], heap[parent]);
			index = parent;
		}
	}

	static int PopReadyNode(int[] heap, ref int count) {
		var result = heap[0];
		heap[0] = heap[--count];
		var index = 0;
		while (true) {
			var left = index * 2 + 1;
			if (left >= count) break;
			var largest = left + 1 < count && heap[left + 1] > heap[left] ? left + 1 : left;
			if (heap[index] >= heap[largest]) break;
			(heap[index], heap[largest]) = (heap[largest], heap[index]);
			index = largest;
		}
		return result;
	}

	public IndirectEnumerable<EnumerationInput, ResourceStub> GetDependents<TTarget>(TTarget targetPotentiallyInUse) where TTarget : IResource {
		ThrowIfDisposed();

		return new IndirectEnumerable<EnumerationInput, ResourceStub>(
			new(this, targetPotentiallyInUse.Ident),
			_stateVersion,
			&GetDependentsEnumerationCount,
			&GetStateVersion,
			&GetDependentsEnumerationItem
		);
	}
	static int GetDependentsEnumerationCount(EnumerationInput input) {
		var @this = (input.Tracker as ResourceDependencyTracker)!;
		@this.ThrowIfDisposed();
		return GetMapEnumerationCount(@this._targetsToDependentsMap, input.ArgumentIdent);
	}
	static ResourceStub GetDependentsEnumerationItem(EnumerationInput input, int index) {
		var @this = (input.Tracker as ResourceDependencyTracker)!;
		@this.ThrowIfDisposed();
		return GetMapEnumerationItem(@this._targetsToDependentsMap, input.ArgumentIdent, index);
	}
	public IndirectEnumerable<EnumerationInput, ResourceStub> GetTargets<TDependent>(TDependent dependent) where TDependent : IResource {
		ThrowIfDisposed();

		return new IndirectEnumerable<EnumerationInput, ResourceStub>(
			new(this, dependent.Ident),
			_stateVersion,
			&GetTargetsEnumerationCount,
			&GetStateVersion,
			&GetTargetsEnumerationItem
		);
	}
	static int GetTargetsEnumerationCount(EnumerationInput input) {
		var @this = (input.Tracker as ResourceDependencyTracker)!;
		@this.ThrowIfDisposed();
		return GetMapEnumerationCount(@this._dependentsToTargetsMap, input.ArgumentIdent);
	}
	static ResourceStub GetTargetsEnumerationItem(EnumerationInput input, int index) {
		var @this = (input.Tracker as ResourceDependencyTracker)!;
		@this.ThrowIfDisposed();
		return GetMapEnumerationItem(@this._dependentsToTargetsMap, input.ArgumentIdent, index);
	}

	public IndirectEnumerable<EnumerationInput, TDependent> GetDependentsOfGivenType<TTarget, TDependent, TImpl>(TTarget targetPotentiallyInUse) 
		where TTarget : IResource
		where TDependent : IResource<TDependent, TImpl>
		where TImpl : class, IResourceImplProvider {
		ThrowIfDisposed();

		return new IndirectEnumerable<EnumerationInput, TDependent>(
			new(this, targetPotentiallyInUse.Ident),
			_stateVersion,
			&GetDependentsEnumerationCount<TDependent, TImpl>,
			&GetStateVersion,
			&GetDependentsEnumerationItem<TDependent, TImpl>
		);
	}
	public TDependent GetNthDependentOfGivenType<TTarget, TDependent, TImpl>(TTarget target, int index) 
		where TTarget : IResource 
		where TDependent : IResource<TDependent, TImpl> 
		where TImpl : class, IResourceImplProvider {
		return GetDependentsEnumerationItem<TDependent, TImpl>(new(this, target.Ident), index);
	}
	static int GetDependentsEnumerationCount<TDependent, TImpl>(EnumerationInput input)
		where TDependent : IResource<TDependent, TImpl>
		where TImpl : class, IResourceImplProvider {
		var @this = (input.Tracker as ResourceDependencyTracker)!;
		@this.ThrowIfDisposed();
		return GetMapEnumerationCount<TDependent>(@this._targetsToDependentsMap, input.ArgumentIdent);
	}
	static TDependent GetDependentsEnumerationItem<TDependent, TImpl>(EnumerationInput input, int index)
		where TDependent : IResource<TDependent, TImpl>
		where TImpl : class, IResourceImplProvider {
		var @this = (input.Tracker as ResourceDependencyTracker)!;
		@this.ThrowIfDisposed();
		return IResource<TDependent, TImpl>.RecreateFromResourceStub(GetMapEnumerationItem<TDependent>(@this._targetsToDependentsMap, input.ArgumentIdent, index));
	}
	public IndirectEnumerable<EnumerationInput, TTarget> GetTargetsOfGivenType<TDependent, TTarget, TImpl>(TDependent dependent)
		where TDependent : IResource
		where TTarget : IResource<TTarget, TImpl>
		where TImpl : class, IResourceImplProvider {
		ThrowIfDisposed();

		return new IndirectEnumerable<EnumerationInput, TTarget>(
			new(this, dependent.Ident),
			_stateVersion,
			&GetTargetsEnumerationCount<TTarget, TImpl>,
			&GetStateVersion,
			&GetTargetsEnumerationItem<TTarget, TImpl>
		);
	}
	public TTarget GetNthTargetOfGivenType<TDependent, TTarget, TImpl>(TDependent dependent, int index)
		where TDependent : IResource
		where TTarget : IResource<TTarget, TImpl>
		where TImpl : class, IResourceImplProvider {
		return GetTargetsEnumerationItem<TTarget, TImpl>(new(this, dependent.Ident), index);
	}
	static int GetTargetsEnumerationCount<TTarget, TImpl>(EnumerationInput input)
		where TTarget : IResource<TTarget, TImpl>
		where TImpl : class, IResourceImplProvider {
		var @this = (input.Tracker as ResourceDependencyTracker)!;
		@this.ThrowIfDisposed();
		return GetMapEnumerationCount<TTarget>(@this._dependentsToTargetsMap, input.ArgumentIdent);
	}
	static TTarget GetTargetsEnumerationItem<TTarget, TImpl>(EnumerationInput input, int index)
		where TTarget : IResource<TTarget, TImpl>
		where TImpl : class, IResourceImplProvider {
		var @this = (input.Tracker as ResourceDependencyTracker)!;
		@this.ThrowIfDisposed();
		return IResource<TTarget, TImpl>.RecreateFromResourceStub(GetMapEnumerationItem<TTarget>(@this._dependentsToTargetsMap, input.ArgumentIdent, index));
	}

	static int GetMapEnumerationCount(StubMap map, ResourceIdent key) => map.TryGetValue(key, out var values) ? values.Count : 0;
	static int GetMapEnumerationCount<TResource>(StubMap map, ResourceIdent key) where TResource : IResource<TResource> {
		if (!map.TryGetValue(key, out var values)) return 0;
		var result = 0;
		for (var i = 0; i < values.Count; ++i) {
			if (values.GetItemAtIndex(i).TypeHandle == ResourceHandle<TResource>.TypeHandle) ++result;
		}
		return result;
	}
	static ResourceStub GetMapEnumerationItem(StubMap map, ResourceIdent key, int index) {
		InvalidOperationException CreateException() {
			return new InvalidOperationException(
				"Invalid enumeration state. Tracked resource was probably modified while enumeration was ongoing. " +
				"If you see this error it may indicate a concurrency issue or a bug in TinyFFR. Debug information: " +
				$"Key = {key}; Index = {index}; map.ContainsKey(key) = {map.ContainsKey(key)}" +
				$"{(map.TryGetValue(key, out var v) ? $"; map[key].Count = {v.Count}" : "")}."
			);
		}

		if (!map.TryGetValue(key, out var values) || values.Count <= index) throw CreateException();
		return values.GetItemAtIndex(index);
	}
	static ResourceStub GetMapEnumerationItem<TResource>(StubMap map, ResourceIdent key, int index) where TResource : IResource<TResource> {
		InvalidOperationException CreateException() {
			return new InvalidOperationException(
				"Invalid enumeration state. Tracked resource was probably modified while enumeration was ongoing. " +
				"If you see this error it may indicate a concurrency issue or a bug in TinyFFR. Debug information: " +
				$"Key = {key}; Index = {index}; map.ContainsKey(key) = {map.ContainsKey(key)}; TResource = {typeof(TResource).Name}" +
				$"{(map.TryGetValue(key, out var v) ? $"; map[key].Count = {v.Count}; map[key].Count(r => r.TypeHandle == THandle.TypeHandle) = {v.Count(r => r.TypeHandle == ResourceHandle<TResource>.TypeHandle)}" : "")}."
			);
		}

		if (!map.TryGetValue(key, out var values)) throw CreateException();
		var curIndex = 0;
		foreach (var value in values) {
			if (value.TypeHandle != ResourceHandle<TResource>.TypeHandle) continue;
			if (curIndex == index) return value;
			++curIndex;
		}
		throw CreateException();
	}

	public void EraseAllDependencies() {
		foreach (var kvp in _targetsToDependentsMap) {
			_setPool.Return(kvp.Value);
		}
		_targetsToDependentsMap.Clear();

		foreach (var kvp in _dependentsToTargetsMap) {
			_setPool.Return(kvp.Value);
		}
		_dependentsToTargetsMap.Clear();
		_ownedToOwnerMap.Clear();
	}

	public void Dispose() {
		if (_isDisposed) return;

		EraseAllDependencies();
		_targetsToDependentsMap.Dispose();
		_dependentsToTargetsMap.Dispose();
		_ownedToOwnerMap.Dispose();
		_prematureDisposalClosure.Dispose();
		_prematureDisposalClosureIdents.Dispose();
		_orderingNodeMap.Dispose();
		_orderingClosure.Dispose();
		_orderingClosureNodes.Dispose();
		_orderingEdges.Dispose();
		_setPool.Dispose();
		_isDisposed = true;
	}

	void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, typeof(IResourceDependencyTracker));

	static int GetStateVersion(EnumerationInput input) => (input.Tracker as ResourceDependencyTracker)!._stateVersion;
}