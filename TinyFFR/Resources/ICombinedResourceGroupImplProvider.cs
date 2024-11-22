// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

public interface ICombinedResourceGroupImplProvider : IDisposableResourceImplProvider<CombinedResourceGroupHandle> {
	public readonly record struct EnumerationArg(ICombinedResourceGroupImplProvider Impl, CombinedResourceGroupHandle Handle, IntPtr ResourceTypeHandle);

	internal ReadOnlySpan<ResourceStub> GetResources(CombinedResourceGroupHandle handle);
	int GetResourceCount(CombinedResourceGroupHandle handle);
	bool IsSealed(CombinedResourceGroupHandle handle);
	void Seal(CombinedResourceGroupHandle handle);
	void AddResource<TResource>(CombinedResourceGroupHandle handle, TResource resource) where TResource : IResource;
	OneToManyEnumerator<EnumerationArg, TResource> GetAllResourcesOfType<TResource>(CombinedResourceGroupHandle handle) where TResource : IResource<TResource>;
	TResource GetNthResourceOfType<TResource>(CombinedResourceGroupHandle handle, int index) where TResource : IResource<TResource>;

	void Dispose(CombinedResourceGroupHandle handle, bool disposeContainedResources);
	bool GetDisposesContainedResourcesByDefaultWhenDisposed(CombinedResourceGroupHandle handle);
}