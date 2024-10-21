// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

interface IResourceDependencyTracker {
	public readonly record struct EnumerationInput(IResourceDependencyTracker Tracker, ResourceIdent TargetIdent);
	void DeregisterDependency<TDependent, TTarget>(TDependent dependent, TTarget target) where TDependent : IResource where TTarget : IResource;
	void RegisterDependency<TDependent, TTarget>(TDependent dependent, TTarget target) where TDependent : IResource where TTarget : IResource;
	void ThrowForPrematureDisposalIfTargetHasDependents<TTarget>(TTarget target) where TTarget : IResource;
	OneToManyEnumerator<EnumerationInput, TDependent> EnumerateDependentsOfGivenType<TTarget, TDependent, THandle, TImpl>(TTarget target)
		where TTarget : IResource
		where TDependent : IResource<TDependent, THandle, TImpl>
		where THandle : unmanaged, IResourceHandle<THandle>
		where TImpl : class, IResourceImplProvider;
}