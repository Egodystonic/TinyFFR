// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

interface IResourceDependencyTracker {
	public readonly record struct EnumerationInput(IResourceDependencyTracker Tracker, ResourceIdent ArgumentIdent);
	public readonly record struct DisposalOrderFailure(ResourceStub BlockedResource, ResourceStub? BlockingDependent);
	void RegisterDependency<TDependent, TTarget>(TDependent dependent, TTarget targetNowInUse) where TDependent : IResource where TTarget : IResource;
	void RegisterOwnership<TOwner, TOwned>(TOwner owner, TOwned owned) where TOwner : IResource where TOwned : IResource;
	bool TryGetDisposalOrder(ReadOnlySpan<ResourceStub> resources, ResourceIdent ignoredDependent, Span<int> orderDest, out DisposalOrderFailure failure);
	void DeregisterDependency<TDependent, TTarget>(TDependent dependent, TTarget targetNoLongerInUse) where TDependent : IResource where TTarget : IResource;
	void DeregisterAllDependencies<TDependent>(TDependent dependent) where TDependent : IResource;
	void ThrowForPrematureDisposalIfTargetHasDependents<TTarget>(TTarget targetPotentiallyInUse) where TTarget : IResource;
	IndirectEnumerable<EnumerationInput, ResourceStub> GetDependents<TTarget>(TTarget targetPotentiallyInUse) where TTarget : IResource;
	IndirectEnumerable<EnumerationInput, ResourceStub> GetTargets<TDependent>(TDependent dependent) where TDependent : IResource;
	IndirectEnumerable<EnumerationInput, TDependent> GetDependentsOfGivenType<TTarget, TDependent, TImpl>(TTarget targetPotentiallyInUse)
		where TTarget : IResource
		where TDependent : IResource<TDependent, TImpl>
		where TImpl : class, IResourceImplProvider;
	IndirectEnumerable<EnumerationInput, TTarget> GetTargetsOfGivenType<TDependent, TTarget, TImpl>(TDependent dependent)
		where TDependent : IResource
		where TTarget : IResource<TTarget, TImpl>
		where TImpl : class, IResourceImplProvider;
	TDependent GetNthDependentOfGivenType<TTarget, TDependent, TImpl>(TTarget target, int index)
		where TTarget : IResource
		where TDependent : IResource<TDependent, TImpl>
		where TImpl : class, IResourceImplProvider;
	TTarget GetNthTargetOfGivenType<TDependent, TTarget, TImpl>(TDependent dependent, int index)
		where TDependent : IResource
		where TTarget : IResource<TTarget, TImpl>
		where TImpl : class, IResourceImplProvider;
}