// Created on 2024-09-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

interface IResourceDependencyTracker {
	void DeregisterDependency<TDependent, TTarget>(TDependent dependent, TTarget target) where TDependent : IResource where TTarget : IResource;
	void RegisterDependency<TDependent, TTarget>(TDependent dependent, TTarget target) where TDependent : IResource where TTarget : IResource;
	void ThrowForPrematureDisposalIfTargetHasDependents<TTarget>(TTarget target) where TTarget : IResource;
}