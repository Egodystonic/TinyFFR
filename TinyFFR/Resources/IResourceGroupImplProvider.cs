// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

/// <summary>
/// An <see cref="IResourceImplProvider{TResource}"/> for <see cref="ResourceGroup"/> resources.
/// </summary>
public interface IResourceGroupImplProvider : IDisposableResourceImplProvider<ResourceGroup> {
#pragma warning disable CA1034 // "Nested types should not be visible" -- Similar to enumerators, this is meant to be "namespaced" to this interface and shouldn't really need to be used directly (at least when using implicit typing)
	/// <summary>
	/// The value used as <see cref="IndirectEnumerable{TIn,TOut}"/>'s <c>TIn</c> for the various resource-enumeration members below (e.g. <see cref="GetAllResourcesOfType{TResource}"/>).
	/// </summary>
	/// <remarks>
	/// You should not typically need to reference this type directly; it exists purely to carry the state needed to enumerate a <see cref="ResourceGroup"/>'s contents without allocating.
	/// </remarks>
	public readonly record struct EnumerationInput(IResourceGroupImplProvider Impl, ResourceHandle<ResourceGroup> Handle, IntPtr ResourceTypeHandle);
#pragma warning restore CA1034

	/// <summary>
	/// Invoked via <see cref="ResourceGroup.ResourceCount"/>.
	/// </summary>
	int GetResourceCount(ResourceHandle<ResourceGroup> handle);
	/// <summary>
	/// Invoked via <see cref="ResourceGroup.IsSealed"/>.
	/// </summary>
	bool IsSealed(ResourceHandle<ResourceGroup> handle);
	/// <summary>
	/// Invoked via <see cref="ResourceGroup.Seal"/>.
	/// </summary>
	void Seal(ResourceHandle<ResourceGroup> handle);
	/// <summary>
	/// Invoked via <see cref="ResourceGroup.Add{TResource}"/>.
	/// </summary>
	void AddResource<TResource>(ResourceHandle<ResourceGroup> handle, TResource resource) where TResource : IResource;
	/// <summary>
	/// Invoked via <see cref="ResourceGroup.Add{TResource,TBase}"/>.
	/// </summary>
	void AddResource<TResource, TBase>(ResourceHandle<ResourceGroup> handle, TResource resource) where TResource : struct, IResourceSpecialization<TResource, TBase> where TBase : IResource<TBase>;
	/// <summary>
	/// Invoked via <see cref="ResourceGroup.GetAllResourcesOfType{TResource}"/>.
	/// </summary>
	IndirectEnumerable<EnumerationInput, TResource> GetAllResourcesOfType<TResource>(ResourceHandle<ResourceGroup> handle) where TResource : IResource<TResource>;
	/// <summary>
	/// Invoked via <see cref="ResourceGroup.GetAllResourcesOfType{TResource,TBase}"/>.
	/// </summary>
	IndirectEnumerable<EnumerationInput, TResource> GetAllResourcesOfType<TResource, TBase>(ResourceHandle<ResourceGroup> handle) where TResource : struct, IResourceSpecialization<TResource, TBase> where TBase : IResource<TBase>;
	/// <summary>
	/// Invoked via <see cref="ResourceGroup.GetNthResourceOfType{TResource}"/>.
	/// </summary>
	TResource GetNthResourceOfType<TResource>(ResourceHandle<ResourceGroup> handle, int index) where TResource : IResource<TResource>;
	/// <summary>
	/// Invoked via <see cref="ResourceGroup.GetNthResourceOfType{TResource,TBase}"/>.
	/// </summary>
	TResource GetNthResourceOfType<TResource, TBase>(ResourceHandle<ResourceGroup> handle, int index) where TResource : struct, IResourceSpecialization<TResource, TBase> where TBase : IResource<TBase>;
	/// <summary>
	/// Invoked via <see cref="ResourceGroup.GetAllResourcesBoxed"/>.
	/// </summary>
	internal IReadOnlyCollection<object> GetAllResourcesBoxed(ResourceHandle<ResourceGroup> handle);

	/// <summary>
	/// Invoked via <see cref="ResourceGroup.Dispose(bool)"/>.
	/// </summary>
	void Dispose(ResourceHandle<ResourceGroup> handle, bool disposeContainedResources);
	/// <summary>
	/// Invoked via <see cref="ResourceGroup.DisposesContainedResourcesByDefaultWhenDisposed"/>.
	/// </summary>
	bool GetDisposesContainedResourcesByDefaultWhenDisposed(ResourceHandle<ResourceGroup> handle);
}