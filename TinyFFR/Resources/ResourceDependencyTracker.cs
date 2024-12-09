// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using static Egodystonic.TinyFFR.Resources.IResourceDependencyTracker;
using StubMap = Egodystonic.TinyFFR.Resources.Memory.ArrayPoolBackedMap<Egodystonic.TinyFFR.Resources.ResourceIdent, Egodystonic.TinyFFR.Resources.Memory.ArrayPoolBackedVector<Egodystonic.TinyFFR.Resources.ResourceStub>>;
using VectPool = Egodystonic.TinyFFR.Resources.Memory.ObjectPool<Egodystonic.TinyFFR.Resources.Memory.ArrayPoolBackedVector<Egodystonic.TinyFFR.Resources.ResourceStub>>;

namespace Egodystonic.TinyFFR.Resources;

sealed unsafe class ResourceDependencyTracker : IResourceDependencyTracker, IDisposable {
	const int InitialDependentsArrayLength = 4;
	readonly VectPool _vectorPool = new(&CreateNewVector);
	readonly StubMap _targetsToDependentsMap = new();
	readonly StubMap _dependentsToTargetsMap = new();
	bool _isDisposed = false;
	int _stateVersion = 0;

	public void RegisterDependency<TDependent, TTarget>(TDependent dependent, TTarget targetNowInUse) where TDependent : IResource where TTarget : IResource {
		ThrowIfDisposed();

		static void AddStubToMap(VectPool vectorPool, StubMap map, ResourceIdent key, ResourceStub value) {
			if (!map.TryGetValue(key, out var values)) {
				values = vectorPool.Rent();
				map.Add(key, values);
			}
			else if (values.Contains(value)) return;
			values.Add(value);
		}
		
		var dependentStub = new ResourceStub(dependent.Ident, dependent.Implementation);
		var targetStub = new ResourceStub(targetNowInUse.Ident, targetNowInUse.Implementation);
		AddStubToMap(_vectorPool, _targetsToDependentsMap, targetStub.Ident, dependentStub);
		AddStubToMap(_vectorPool, _dependentsToTargetsMap, dependentStub.Ident, targetStub);
		_stateVersion++;
	}

	public void DeregisterDependency<TDependent, TTarget>(TDependent dependent, TTarget targetNoLongerInUse) where TDependent : IResource where TTarget : IResource {
		ThrowIfDisposed();

		static void RemoveStubFromMap(VectPool vectorPool, StubMap map, ResourceIdent key, ResourceStub value) {
			if (!map.TryGetValue(key, out var values)) return;
			if (!values.Remove(value)) return;
			if (values.Count != 0) return;
			map.Remove(key);
			vectorPool.Return(values);
		}

		var dependentStub = new ResourceStub(dependent.Ident, dependent.Implementation);
		var targetStub = new ResourceStub(targetNoLongerInUse.Ident, targetNoLongerInUse.Implementation);
		RemoveStubFromMap(_vectorPool, _targetsToDependentsMap, targetStub.Ident, dependentStub);
		RemoveStubFromMap(_vectorPool, _dependentsToTargetsMap, dependentStub.Ident, targetStub);
		_stateVersion++;
	}

	public void DeregisterAllDependencies<TDependent>(TDependent dependent) where TDependent : IResource {
		ThrowIfDisposed();

		if (!_dependentsToTargetsMap.TryGetValue(dependent.Ident, out var targets)) return;
		var dependentStub = new ResourceStub(dependent.Ident, dependent.Implementation);
		
		foreach (var target in targets) {
			if (!_targetsToDependentsMap.TryGetValue(target.Ident, out var dependents)) continue;
			if (!dependents.Remove(dependentStub)) continue;
			if (dependents.Count != 0) continue;
			_targetsToDependentsMap.Remove(target.Ident);
			_vectorPool.Return(dependents);
		}

		_dependentsToTargetsMap.Remove(dependentStub.Ident);
		targets.Clear();
		_vectorPool.Return(targets);
		_stateVersion++;
	}

	public void ThrowForPrematureDisposalIfTargetHasDependents<TTarget>(TTarget targetPotentiallyInUse) where TTarget : IResource {
		ThrowIfDisposed();

		if (!_targetsToDependentsMap.TryGetValue(targetPotentiallyInUse.Ident, out var dependents)) return;
		throw ResourceDependencyException.CreateForPrematureDisposal(
			targetPotentiallyInUse.GetType().Name,
			targetPotentiallyInUse.Name.ToString(),
			dependents.Select(sr => sr.Implementation.RawHandleGetName(sr.Ident.RawResourceHandle).ToString()).ToArray()
		);
	}

	public TypedReferentIterator<EnumerationInput, ResourceStub> GetDependents<TTarget>(TTarget targetPotentiallyInUse) where TTarget : IResource {
		ThrowIfDisposed();

		return new TypedReferentIterator<EnumerationInput, ResourceStub>(
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
	public TypedReferentIterator<EnumerationInput, ResourceStub> GetTargets<TDependent>(TDependent dependent) where TDependent : IResource {
		ThrowIfDisposed();

		return new TypedReferentIterator<EnumerationInput, ResourceStub>(
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

	public TypedReferentIterator<EnumerationInput, TDependent> GetDependentsOfGivenType<TTarget, TDependent, THandle, TImpl>(TTarget targetPotentiallyInUse) 
		where TTarget : IResource
		where TDependent : IResource<TDependent, THandle, TImpl>
		where THandle : unmanaged, IResourceHandle<THandle>
		where TImpl : class, IResourceImplProvider {
		ThrowIfDisposed();

		return new TypedReferentIterator<EnumerationInput, TDependent>(
			new(this, targetPotentiallyInUse.Ident),
			_stateVersion,
			&GetDependentsEnumerationCount<TDependent, THandle, TImpl>,
			&GetStateVersion,
			&GetDependentsEnumerationItem<TDependent, THandle, TImpl>
		);
	}
	public TDependent GetNthDependentOfGivenType<TTarget, TDependent, THandle, TImpl>(TTarget targetPotentiallyInUse, int index) 
		where TTarget : IResource 
		where TDependent : IResource<TDependent, THandle, TImpl> 
		where THandle : unmanaged, IResourceHandle<THandle> where TImpl : class, IResourceImplProvider {
		return GetDependentsEnumerationItem<TDependent, THandle, TImpl>(new(this, targetPotentiallyInUse.Ident), index);
	}
	static int GetDependentsEnumerationCount<TDependent, THandle, TImpl>(EnumerationInput input)
		where TDependent : IResource<TDependent, THandle, TImpl>
		where THandle : unmanaged, IResourceHandle<THandle>
		where TImpl : class, IResourceImplProvider {
		var @this = (input.Tracker as ResourceDependencyTracker)!;
		@this.ThrowIfDisposed();
		return GetMapEnumerationCount<THandle>(@this._targetsToDependentsMap, input.ArgumentIdent);
	}
	static TDependent GetDependentsEnumerationItem<TDependent, THandle, TImpl>(EnumerationInput input, int index)
		where TDependent : IResource<TDependent, THandle, TImpl>
		where THandle : unmanaged, IResourceHandle<THandle>
		where TImpl : class, IResourceImplProvider {
		var @this = (input.Tracker as ResourceDependencyTracker)!;
		@this.ThrowIfDisposed();
		return IResource<TDependent, THandle, TImpl>.RecreateFromResourceStub(GetMapEnumerationItem<THandle>(@this._targetsToDependentsMap, input.ArgumentIdent, index));
	}
	public TypedReferentIterator<EnumerationInput, TTarget> GetTargetsOfGivenType<TDependent, TTarget, THandle, TImpl>(TDependent dependent)
		where TDependent : IResource
		where TTarget : IResource<TTarget, THandle, TImpl>
		where THandle : unmanaged, IResourceHandle<THandle>
		where TImpl : class, IResourceImplProvider {
		ThrowIfDisposed();

		return new TypedReferentIterator<EnumerationInput, TTarget>(
			new(this, dependent.Ident),
			_stateVersion,
			&GetTargetsEnumerationCount<TTarget, THandle, TImpl>,
			&GetStateVersion,
			&GetTargetsEnumerationItem<TTarget, THandle, TImpl>
		);
	}
	public TTarget GetNthTargetOfGivenType<TDependent, TTarget, THandle, TImpl>(TDependent dependent, int index)
		where TDependent : IResource
		where TTarget : IResource<TTarget, THandle, TImpl>
		where THandle : unmanaged, IResourceHandle<THandle> where TImpl : class, IResourceImplProvider {
		return GetTargetsEnumerationItem<TTarget, THandle, TImpl>(new(this, dependent.Ident), index);
	}
	static int GetTargetsEnumerationCount<TTarget, THandle, TImpl>(EnumerationInput input)
		where TTarget : IResource<TTarget, THandle, TImpl>
		where THandle : unmanaged, IResourceHandle<THandle>
		where TImpl : class, IResourceImplProvider {
		var @this = (input.Tracker as ResourceDependencyTracker)!;
		@this.ThrowIfDisposed();
		return GetMapEnumerationCount<THandle>(@this._dependentsToTargetsMap, input.ArgumentIdent);
	}
	static TTarget GetTargetsEnumerationItem<TTarget, THandle, TImpl>(EnumerationInput input, int index)
		where TTarget : IResource<TTarget, THandle, TImpl>
		where THandle : unmanaged, IResourceHandle<THandle>
		where TImpl : class, IResourceImplProvider {
		var @this = (input.Tracker as ResourceDependencyTracker)!;
		@this.ThrowIfDisposed();
		return IResource<TTarget, THandle, TImpl>.RecreateFromResourceStub(GetMapEnumerationItem<THandle>(@this._dependentsToTargetsMap, input.ArgumentIdent, index));
	}

	static int GetMapEnumerationCount(StubMap map, ResourceIdent key) => map.TryGetValue(key, out var values) ? values.Count : 0;
	static int GetMapEnumerationCount<THandle>(StubMap map, ResourceIdent key) where THandle : IResourceHandle<THandle> {
		if (!map.TryGetValue(key, out var values)) return 0;
		var result = 0;
		for (var i = 0; i < values.Count; ++i) {
			if (values[i].TypeHandle == THandle.TypeHandle) ++result;
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
		return values[index];
	}
	static ResourceStub GetMapEnumerationItem<THandle>(StubMap map, ResourceIdent key, int index) where THandle : IResourceHandle<THandle> {
		InvalidOperationException CreateException() {
			return new InvalidOperationException(
				"Invalid enumeration state. Tracked resource was probably modified while enumeration was ongoing. " +
				"If you see this error it may indicate a concurrency issue or a bug in TinyFFR. Debug information: " +
				$"Key = {key}; Index = {index}; map.ContainsKey(key) = {map.ContainsKey(key)}; THandle = {typeof(THandle).Name}" +
				$"{(map.TryGetValue(key, out var v) ? $"; map[key].Count = {v.Count}; map[key].Count(r => r.TypeHandle == THandle.TypeHandle) = {v.Count(r => r.TypeHandle == THandle.TypeHandle)}" : "")}."
			);
		}

		if (!map.TryGetValue(key, out var values)) throw CreateException();
		var curIndex = 0;
		foreach (var value in values) {
			if (value.TypeHandle != THandle.TypeHandle) continue;
			if (curIndex == index) return value;
			++curIndex;
		}
		throw CreateException();
	}

	public void EraseAllDependencies() {
		foreach (var kvp in _targetsToDependentsMap) {
			kvp.Value.Clear();
			_vectorPool.Return(kvp.Value);
		}
		_targetsToDependentsMap.Clear();

		foreach (var kvp in _dependentsToTargetsMap) {
			kvp.Value.Clear();
			_vectorPool.Return(kvp.Value);
		}
		_dependentsToTargetsMap.Clear();
	}

	public void Dispose() {
		if (_isDisposed) return;

		EraseAllDependencies();
		_targetsToDependentsMap.Dispose();
		_dependentsToTargetsMap.Dispose();
		_vectorPool.Dispose();
		_isDisposed = true;
	}

	void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, typeof(IResourceDependencyTracker));

	static int GetStateVersion(EnumerationInput input) => (input.Tracker as ResourceDependencyTracker)!._stateVersion;

	static ArrayPoolBackedVector<ResourceStub> CreateNewVector() => new(InitialDependentsArrayLength);
}