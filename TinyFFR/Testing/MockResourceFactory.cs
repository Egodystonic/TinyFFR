// Created on 2026-03-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Testing;

public static class MockResourceFactory {
	public static TResource Create<TResource, TImpl>(ResourceHandle<TResource> resourceHandle, TImpl mockImplementation) where TResource : IResource<TResource, TImpl> where TImpl : class, IResourceImplProvider<TResource> {
		return TResource.CreateFromHandleAndImpl(resourceHandle, mockImplementation);
	}
}