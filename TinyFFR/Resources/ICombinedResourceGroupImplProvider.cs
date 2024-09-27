// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

public interface ICombinedResourceGroupImplProvider : IResourceImplProvider<CombinedResourceGroupHandle> {
	public readonly record struct EnumerationArg(ICombinedResourceGroupImplProvider Impl, CombinedResourceGroupHandle Handle, IntPtr ResourceTypeHandle);

	int GetResourceCount(CombinedResourceGroupHandle handle);
	int GetResourceCapacity(CombinedResourceGroupHandle handle);
	void AddResource<TResource>(CombinedResourceGroupHandle handle, TResource resource) where TResource : IHandleImplPairResource;
	OneToManyEnumerator<EnumerationArg, TResource> GetAllResourcesOfType<TResource>(CombinedResourceGroupHandle handle) where TResource : IHandleImplPairResource<TResource>;

	void Dispose(CombinedResourceGroupHandle handle, bool disposeContainedResources);
}