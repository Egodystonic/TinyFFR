// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

public interface IResourceGroupImplProvider : IDisposableResourceImplProvider<ResourceGroupHandle> {
#pragma warning disable CA1034 // "Nested types should not be visible" -- Similar to enumerators, this is meant to be "namespaced" to this interface and shouldn't really need to be used directly (at least when using implicit typing)
	public readonly record struct EnumerationInput(IResourceGroupImplProvider Impl, ResourceGroupHandle Handle, IntPtr ResourceTypeHandle);
#pragma warning restore CA1034

	internal ReadOnlySpan<ResourceStub> GetResources(ResourceGroupHandle handle);
	int GetResourceCount(ResourceGroupHandle handle);
	bool IsSealed(ResourceGroupHandle handle);
	void Seal(ResourceGroupHandle handle);
	void AddResource<TResource>(ResourceGroupHandle handle, TResource resource) where TResource : IResource;
	TypedReferentIterator<EnumerationInput, TResource> GetAllResourcesOfType<TResource>(ResourceGroupHandle handle) where TResource : IResource<TResource>;
	TResource GetNthResourceOfType<TResource>(ResourceGroupHandle handle, int index) where TResource : IResource<TResource>;

	void Dispose(ResourceGroupHandle handle, bool disposeContainedResources);
	bool GetDisposesContainedResourcesByDefaultWhenDisposed(ResourceGroupHandle handle);
}