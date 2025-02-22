// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

public interface IResourceGroupImplProvider : IDisposableResourceImplProvider<ResourceGroup> {
#pragma warning disable CA1034 // "Nested types should not be visible" -- Similar to enumerators, this is meant to be "namespaced" to this interface and shouldn't really need to be used directly (at least when using implicit typing)
	public readonly record struct EnumerationInput(IResourceGroupImplProvider Impl, ResourceHandle<ResourceGroup> Handle, IntPtr ResourceTypeHandle);
#pragma warning restore CA1034

	internal ReadOnlySpan<ResourceStub> GetResources(ResourceHandle<ResourceGroup> handle);
	int GetResourceCount(ResourceHandle<ResourceGroup> handle);
	bool IsSealed(ResourceHandle<ResourceGroup> handle);
	void Seal(ResourceHandle<ResourceGroup> handle);
	void AddResource<TResource>(ResourceHandle<ResourceGroup> handle, TResource resource) where TResource : IResource;
	TypedReferentIterator<EnumerationInput, TResource> GetAllResourcesOfType<TResource>(ResourceHandle<ResourceGroup> handle) where TResource : IResource<TResource>;
	TResource GetNthResourceOfType<TResource>(ResourceHandle<ResourceGroup> handle, int index) where TResource : IResource<TResource>;

	void Dispose(ResourceHandle<ResourceGroup> handle, bool disposeContainedResources);
	bool GetDisposesContainedResourcesByDefaultWhenDisposed(ResourceHandle<ResourceGroup> handle);
}