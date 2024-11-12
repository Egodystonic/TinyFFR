using System;

namespace Egodystonic.TinyFFR.Resources;

public interface IResourceImplProvider {
	internal ReadOnlySpan<char> RawHandleGetName(nuint handle);
}
public interface IResourceImplProvider<in THandle> : IResourceImplProvider where THandle : IResourceHandle<THandle> {
	ReadOnlySpan<char> GetName(THandle handle);

	ReadOnlySpan<char> IResourceImplProvider.RawHandleGetName(nuint handle) => GetName(THandle.CreateFromInteger(handle));
}



public interface IDisposableResourceImplProvider : IResourceImplProvider {
	internal bool RawHandleIsDisposed(nuint handle);
	internal void RawHandleDispose(nuint handle);
}
public interface IDisposableResourceImplProvider<in THandle> : IDisposableResourceImplProvider, IResourceImplProvider<THandle> where THandle : IResourceHandle<THandle> {
	bool IsDisposed(THandle handle);
	void Dispose(THandle handle);

	bool IDisposableResourceImplProvider.RawHandleIsDisposed(nuint handle) => IsDisposed(THandle.CreateFromInteger(handle));
	void IDisposableResourceImplProvider.RawHandleDispose(nuint handle) => Dispose(THandle.CreateFromInteger(handle));
}