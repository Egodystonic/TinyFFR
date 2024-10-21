// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers;
using Egodystonic.TinyFFR.Resources.Memory;
using static Egodystonic.TinyFFR.Resources.IResource;
using static Egodystonic.TinyFFR.Resources.IResourceDependencyTracker;

namespace Egodystonic.TinyFFR.Resources;

sealed unsafe class ResourceDependencyTracker : IResourceDependencyTracker {
	readonly struct DependentResourceData : IEquatable<DependentResourceData> {
		public readonly ResourceIdent Ident;
		public readonly IResourceImplProvider ImplProvider;

		public DependentResourceData(ResourceIdent ident, IResourceImplProvider implProvider) {
			Ident = ident;
			ImplProvider = implProvider;
		}

		public bool Equals(DependentResourceData other) => Ident.Equals(other.Ident);
		public override bool Equals(object? obj) => obj is DependentResourceData other && Equals(other);
		public override int GetHashCode() => Ident.GetHashCode();
		public static bool operator ==(DependentResourceData left, DependentResourceData right) => left.Equals(right);
		public static bool operator !=(DependentResourceData left, DependentResourceData right) => !left.Equals(right);
	}

	const int InitialDependentsArrayLength = 4;
	readonly ObjectPool<ArrayPoolBackedVector<DependentResourceData>> _dependentsVectorPool = new(&CreateNewDependentsVector);
	readonly ArrayPoolBackedMap<ResourceIdent, ArrayPoolBackedVector<DependentResourceData>> _dependencyMap = new();

	public void RegisterDependency<TDependent, TTarget>(TDependent dependent, TTarget target) where TDependent : IResource where TTarget : IResource {
		var dependentData = new DependentResourceData(dependent.Ident, dependent.Implementation);
		var targetIdent = target.Ident;
		if (!_dependencyMap.TryGetValue(targetIdent, out var dependents)) {
			dependents = _dependentsVectorPool.Rent();
			_dependencyMap.Add(targetIdent, dependents);
		}
		if (dependents.Contains(dependentData)) return;
		dependents.Add(dependentData);
	}

	public void DeregisterDependency<TDependent, TTarget>(TDependent dependent, TTarget target) where TDependent : IResource where TTarget : IResource {
		var dependentData = new DependentResourceData(dependent.Ident, dependent.Implementation);
		var targetIdent = target.Ident;
		if (!_dependencyMap.TryGetValue(targetIdent, out var dependents)) return;
		dependents.Remove(dependentData);
		if (dependents.Count == 0) {
			_dependencyMap.Remove(targetIdent);
			_dependentsVectorPool.Return(dependents);
		}
	}

	public void ThrowForPrematureDisposalIfTargetHasDependents<TTarget>(TTarget target) where TTarget : IResource {
		if (!_dependencyMap.TryGetValue(target.Ident, out var dependents)) return;
		throw ResourceDependencyException.CreateForPrematureDisposal(
			target.GetType().Name,
			target.Name,
			dependents.Select(drd => drd.ImplProvider.RawHandleGetName(drd.Ident.RawResourceHandle)).ToArray()
		);
	}

	public OneToManyEnumerator<EnumerationInput, TDependent> EnumerateDependentsOfGivenType<TTarget, TDependent, THandle, TImpl>(TTarget target) 
		where TTarget : IResource
		where TDependent : IResource<TDependent, THandle, TImpl>
		where THandle : unmanaged, IResourceHandle<THandle>
		where TImpl : class, IResourceImplProvider {
		return new OneToManyEnumerator<EnumerationInput, TDependent>(
			new(this, target.Ident),
			&GetEnumerationCount<TDependent, THandle, TImpl>,
			&GetEnumerationItem<TDependent, THandle, TImpl>
		);
	}
	static int GetEnumerationCount<TDependent, THandle, TImpl>(EnumerationInput input) 
		where TDependent : IResource<TDependent, THandle, TImpl> 
		where THandle : unmanaged, IResourceHandle<THandle> 
		where TImpl : class, IResourceImplProvider {
		var @this = input.Tracker as ResourceDependencyTracker;
		if (@this == null) return 0;
		if (!@this._dependencyMap.TryGetValue(input.TargetIdent, out var dependents)) return 0;
		var result = 0;
		foreach (var d in dependents) {
			if (d.Ident.TypeHandle == THandle.TypeHandle) result++;
		}
		return result;
	}
	static TDependent GetEnumerationItem<TDependent, THandle, TImpl>(EnumerationInput input, int index)
		where TDependent : IResource<TDependent, THandle, TImpl>
		where THandle : unmanaged, IResourceHandle<THandle>
		where TImpl : class, IResourceImplProvider {
		const string InvalidEnumerationErrorMsg = "Invalid enumeration state. Tracked resource was probably modified.";

		var @this = (input.Tracker as ResourceDependencyTracker) ?? throw new InvalidOperationException(InvalidEnumerationErrorMsg);
		if (!@this._dependencyMap.TryGetValue(input.TargetIdent, out var dependents)) {
			throw new InvalidOperationException(InvalidEnumerationErrorMsg);
		}
		var count = 0;
		foreach (var d in dependents) {
			if (d.Ident.TypeHandle != THandle.TypeHandle) continue;
			if (count == index) return TDependent.RecreateFromRawHandleAndImpl(d.Ident.RawResourceHandle, d.ImplProvider);
			else count++;
		}
		throw new InvalidOperationException(InvalidEnumerationErrorMsg);
	}

	public void Dispose() {
		foreach (var kvp in _dependencyMap) {
			kvp.Value.Clear();
			_dependentsVectorPool.Return(kvp.Value);
		}
		_dependencyMap.Clear();
	}

	static ArrayPoolBackedVector<DependentResourceData> CreateNewDependentsVector() => new();
}