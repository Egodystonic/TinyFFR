// Created on 2026-03-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Testing;

/// <summary>
/// A static class that assists in creating mock resources, useful for unit testing.
/// </summary>
public static class MockResourceFactory {
	/// <summary>
	/// Create a mock <typeparamref name="TResource"/> using the given <paramref name="mockImplementation"/> and <paramref name="resourceHandle"/>.
	/// </summary>
	/// <param name="resourceHandle">The handle for this resource. Can be anything of your choosing, it will be passed to every method that requires it
	/// on your <paramref name="mockImplementation"/>.</param>
	/// <param name="mockImplementation">Your mock implementation that will be invoked when any member of the returned resource is invoked. Must
	/// not be null.</param>
	/// <typeparam name="TResource">The type of resource that will be returned.</typeparam>
	/// <typeparam name="TImpl">The type of your mock implementation. It should implement <see cref="IResourceImplProvider{TResource}"/>.</typeparam>
	public static TResource Create<TResource, TImpl>(ResourceHandle<TResource> resourceHandle, TImpl mockImplementation) where TResource : IResource<TResource, TImpl> where TImpl : class, IResourceImplProvider<TResource> {
		return TResource.CreateFromHandleAndImpl(resourceHandle, mockImplementation);
	}
}