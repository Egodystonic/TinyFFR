// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers;
using Egodystonic.TinyFFR.Resources.Memory;
using static Egodystonic.TinyFFR.Resources.IResource;

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

	public void Dispose() {
		foreach (var kvp in _dependencyMap) {
			kvp.Value.Clear();
			_dependentsVectorPool.Return(kvp.Value);
		}
		_dependencyMap.Clear();
	}

	static ArrayPoolBackedVector<DependentResourceData> CreateNewDependentsVector() => new();
}