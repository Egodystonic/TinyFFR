// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

interface IResourceDependencyTracker {
	public readonly record struct EnumerationInput(IResourceDependencyTracker Tracker, ResourceIdent ArgumentIdent);
	void RegisterDependency<TDependent, TTarget>(TDependent dependent, TTarget targetNowInUse) where TDependent : IResource where TTarget : IResource;
	void DeregisterDependency<TDependent, TTarget>(TDependent dependent, TTarget targetNoLongerInUse) where TDependent : IResource where TTarget : IResource;
	void DeregisterAllDependencies<TDependent>(TDependent dependent) where TDependent : IResource;
	void ThrowForPrematureDisposalIfTargetHasDependents<TTarget>(TTarget targetPotentiallyInUse) where TTarget : IResource;
	TypedReferentIterator<EnumerationInput, ResourceStub> EnumerateDependents<TTarget>(TTarget targetPotentiallyInUse) where TTarget : IResource;
	TypedReferentIterator<EnumerationInput, ResourceStub> EnumerateTargets<TDependent>(TDependent dependent) where TDependent : IResource;
	TypedReferentIterator<EnumerationInput, TDependent> EnumerateDependentsOfGivenType<TTarget, TDependent, THandle, TImpl>(TTarget targetPotentiallyInUse)
		where TTarget : IResource
		where TDependent : IResource<TDependent, THandle, TImpl>
		where THandle : unmanaged, IResourceHandle<THandle>
		where TImpl : class, IResourceImplProvider;
	TypedReferentIterator<EnumerationInput, TTarget> EnumerateTargetsOfGivenType<TDependent, TTarget, THandle, TImpl>(TDependent dependent)
		where TDependent : IResource
		where TTarget : IResource<TTarget, THandle, TImpl>
		where THandle : unmanaged, IResourceHandle<THandle>
		where TImpl : class, IResourceImplProvider;
	TDependent GetNthDependentOfGivenType<TTarget, TDependent, THandle, TImpl>(TTarget targetPotentiallyInUse, int index)
		where TTarget : IResource
		where TDependent : IResource<TDependent, THandle, TImpl>
		where THandle : unmanaged, IResourceHandle<THandle>
		where TImpl : class, IResourceImplProvider;
	TTarget GetNthTargetOfGivenType<TDependent, TTarget, THandle, TImpl>(TDependent dependent, int index)
		where TDependent : IResource
		where TTarget : IResource<TTarget, THandle, TImpl>
		where THandle : unmanaged, IResourceHandle<THandle>
		where TImpl : class, IResourceImplProvider;
}