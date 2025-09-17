using System;

namespace Egodystonic.TinyFFR.Resources;

public interface IResourceImplProvider {
	string GetNameAsNewStringObject(ResourceHandle handle);
	int GetNameLength(ResourceHandle handle);
	void CopyName(ResourceHandle handle, Span<char> destinationBuffer);
}
public interface IResourceImplProvider<TResource> : IResourceImplProvider where TResource : IResource<TResource> {
	string GetNameAsNewStringObject(ResourceHandle<TResource> handle);
	int GetNameLength(ResourceHandle<TResource> handle);
	void CopyName(ResourceHandle<TResource> handle, Span<char> destinationBuffer);

	string IResourceImplProvider.GetNameAsNewStringObject(ResourceHandle handle) => GetNameAsNewStringObject((ResourceHandle<TResource>) handle);
	int IResourceImplProvider.GetNameLength(ResourceHandle handle) => GetNameLength((ResourceHandle<TResource>) handle);
	void IResourceImplProvider.CopyName(ResourceHandle handle, Span<char> destinationBuffer) => CopyName((ResourceHandle<TResource>) handle, destinationBuffer);
}



public interface IDisposableResourceImplProvider : IResourceImplProvider {
	bool IsDisposed(ResourceHandle handle);
	void Dispose(ResourceHandle handle);
}
public interface IDisposableResourceImplProvider<TResource> : IDisposableResourceImplProvider, IResourceImplProvider<TResource> where TResource : IResource<TResource> {
	bool IsDisposed(ResourceHandle<TResource> handle);
	void Dispose(ResourceHandle<TResource> handle);

	bool IDisposableResourceImplProvider.IsDisposed(ResourceHandle handle) => IsDisposed((ResourceHandle<TResource>) handle);
	void IDisposableResourceImplProvider.Dispose(ResourceHandle handle) => Dispose((ResourceHandle<TResource>) handle);
}