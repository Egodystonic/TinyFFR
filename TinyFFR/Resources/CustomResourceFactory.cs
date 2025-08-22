// Created on 2025-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Resources;

public static class CustomResourceFactory {
	public static TResource Create<TResource, TImpl>(ResourceHandle<TResource> handle, TImpl impl) where TResource : IResource<TResource, TImpl> where TImpl : class, IResourceImplProvider<TResource> {
		return TResource.CreateFromHandleAndImpl(handle, impl);
	}

	public static TResource Create<TResource>(ResourceHandle handle, IResourceImplProvider impl) where TResource : IResource<TResource> {
		return TResource.CreateFromStub(new ResourceStub(new ResourceIdent(ResourceHandle<TResource>.TypeHandle, handle.AsInteger), impl));
	}
}