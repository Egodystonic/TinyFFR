using System;

namespace Egodystonic.TinyFFR.Resources;

public interface IResourceImplProvider {
	internal ReadOnlySpan<char> GetName(ResourceHandle handle);
}
public interface IResourceImplProvider<TResource> : IResourceImplProvider where TResource : IResource<TResource> {
	ReadOnlySpan<char> GetName(ResourceHandle<TResource> handle);

	ReadOnlySpan<char> IResourceImplProvider.GetName(ResourceHandle handle) => GetName((ResourceHandle<TResource>) handle);
}



public interface IDisposableResourceImplProvider : IResourceImplProvider {
	internal bool IsDisposed(ResourceHandle handle);
	internal void Dispose(ResourceHandle handle);
}
public interface IDisposableResourceImplProvider<TResource> : IDisposableResourceImplProvider, IResourceImplProvider<TResource> where TResource : IResource<TResource> {
	internal bool IsDisposed(ResourceHandle<TResource> handle);
	void Dispose(ResourceHandle<TResource> handle);

	bool IDisposableResourceImplProvider.IsDisposed(ResourceHandle handle) => IsDisposed((ResourceHandle<TResource>) handle);
	void IDisposableResourceImplProvider.Dispose(ResourceHandle handle) => Dispose((ResourceHandle<TResource>) handle);
}