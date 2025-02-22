// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

interface IResourceDependencyTracker {
	public readonly record struct EnumerationInput(IResourceDependencyTracker Tracker, ResourceIdent ArgumentIdent);
	void RegisterDependency<TDependent, TTarget>(TDependent dependent, TTarget targetNowInUse) where TDependent : IResource where TTarget : IResource;
	void DeregisterDependency<TDependent, TTarget>(TDependent dependent, TTarget targetNoLongerInUse) where TDependent : IResource where TTarget : IResource;
	void DeregisterAllDependencies<TDependent>(TDependent dependent) where TDependent : IResource;
	void ThrowForPrematureDisposalIfTargetHasDependents<TTarget>(TTarget targetPotentiallyInUse) where TTarget : IResource;
	TypedReferentIterator<EnumerationInput, ResourceStub> GetDependents<TTarget>(TTarget targetPotentiallyInUse) where TTarget : IResource;
	TypedReferentIterator<EnumerationInput, ResourceStub> GetTargets<TDependent>(TDependent dependent) where TDependent : IResource;
	TypedReferentIterator<EnumerationInput, TDependent> GetDependentsOfGivenType<TTarget, TDependent, TImpl>(TTarget targetPotentiallyInUse)
		where TTarget : IResource
		where TDependent : IResource<TDependent, TImpl>
		where TImpl : class, IResourceImplProvider;
	TypedReferentIterator<EnumerationInput, TTarget> GetTargetsOfGivenType<TDependent, TTarget, TImpl>(TDependent dependent)
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