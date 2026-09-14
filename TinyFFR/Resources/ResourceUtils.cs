// Created on 2025-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Resources;

/// <summary>
/// A static class that assists in creating custom resources, and accessing their handles and implementations.
/// </summary>
public static class ResourceUtils {
	/// <summary>
	/// Creates a <typeparamref name="TResource"/> using the given <paramref name="impl"/> and <paramref name="handle"/>.
	/// </summary>
	/// <param name="handle">The handle for this resource. Can be anything of your choosing, it will be passed to every method that requires it
	/// on your <paramref name="impl"/>.</param>
	/// <param name="impl">Your custom implementation that will be invoked when any member of the returned resource is invoked. Must
	/// not be null.</param>
	/// <typeparam name="TResource">The type of resource that will be returned.</typeparam>
	/// <typeparam name="TImpl">The type of your implementation. It should implement <see cref="IResourceImplProvider{TResource}"/>.</typeparam>
	public static TResource CreateCustom<TResource, TImpl>(ResourceHandle<TResource> handle, TImpl impl) where TResource : IResource<TResource, TImpl> where TImpl : class, IResourceImplProvider<TResource> {
		return TResource.CreateFromHandleAndImpl(handle, impl);
	}

	/// <summary>
	/// Extracts the <see cref="ResourceHandle{T}"/> from the given <paramref name="resource"/>.
	/// </summary>
	/// <remarks>
	/// Note that function is supported for <i>all</i> resources (including those created via <see cref="CreateCustom"/> and 'typical' resources created via the factory and its builders).
	/// Be cautioned that the returned handle object may represent a pointer/offset in to managed or unmanaged resources and may outlive the lifetime of that data.
	/// </remarks>
	/// <param name="resource">The resource to extract the handle from.</param>
	/// <typeparam name="TResource">The type of resource to extract a handle from.</typeparam>
	/// <exception cref="ObjectDisposedException">Thrown if <paramref name="resource"/> has been disposed.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ResourceHandle<TResource> ExtractHandle<TResource>(TResource resource) where TResource : IResource<TResource> => resource.Handle;
	
	/// <summary>
	/// Extracts the <see cref="IResourceImplProvider{TResource}"/> for the given <paramref name="resource"/>.
	/// </summary>
	/// <param name="resource">The resource to extract the implementation for.</param>
	/// <typeparam name="TResource">The type of resource to extract an implementation from.</typeparam>
	/// <typeparam name="TImpl">The implementation type used by <typeparamref name="TResource"/>.</typeparam>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TImpl ExtractImplementation<TResource, TImpl>(TResource resource) where TResource : IResource<TResource, TImpl> where TImpl : class, IResourceImplProvider<TResource> {
		return resource.Implementation;
	}
}