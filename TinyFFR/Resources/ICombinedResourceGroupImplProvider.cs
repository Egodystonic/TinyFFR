// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

public interface ICombinedResourceGroupImplProvider : IDisposableResourceImplProvider<CombinedResourceGroupHandle> {
#pragma warning disable CA1034 // "Nested types should not be visible" -- Similar to enumerators, this is meant to be "namespaced" to this interface and shouldn't really need to be used directly (at least when using implicit typing)
	public readonly record struct EnumerationArg(ICombinedResourceGroupImplProvider Impl, CombinedResourceGroupHandle Handle, IntPtr ResourceTypeHandle);
#pragma warning restore CA1034

	internal ReadOnlySpan<ResourceStub> GetResources(CombinedResourceGroupHandle handle);
	int GetResourceCount(CombinedResourceGroupHandle handle);
	bool IsSealed(CombinedResourceGroupHandle handle);
	void Seal(CombinedResourceGroupHandle handle);
	void AddResource<TResource>(CombinedResourceGroupHandle handle, TResource resource) where TResource : IResource;
	ReferentEnumerator<EnumerationArg, TResource> GetAllResourcesOfType<TResource>(CombinedResourceGroupHandle handle) where TResource : IResource<TResource>;
	TResource GetNthResourceOfType<TResource>(CombinedResourceGroupHandle handle, int index) where TResource : IResource<TResource>;

	void Dispose(CombinedResourceGroupHandle handle, bool disposeContainedResources);
	bool GetDisposesContainedResourcesByDefaultWhenDisposed(CombinedResourceGroupHandle handle);
}