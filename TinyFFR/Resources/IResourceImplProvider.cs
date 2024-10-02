using System;

namespace Egodystonic.TinyFFR.Resources;

public interface IResourceImplProvider {
	internal string RawHandleGetName(nuint handle);
	internal int RawHandleGetNameUsingSpan(nuint handle, Span<char> dest);
	internal int RawHandleGetNameSpanLength(nuint handle);
}
public interface IResourceImplProvider<in THandle> : IResourceImplProvider where THandle : IResourceHandle<THandle> {
	string GetName(THandle handle);
	int GetNameUsingSpan(THandle handle, Span<char> dest);
	int GetNameSpanLength(THandle handle);

	string IResourceImplProvider.RawHandleGetName(nuint handle) => GetName(THandle.CreateFromInteger(handle));
	int IResourceImplProvider.RawHandleGetNameUsingSpan(nuint handle, Span<char> dest) => GetNameUsingSpan(THandle.CreateFromInteger(handle), dest);
	int IResourceImplProvider.RawHandleGetNameSpanLength(nuint handle) => GetNameSpanLength(THandle.CreateFromInteger(handle));
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