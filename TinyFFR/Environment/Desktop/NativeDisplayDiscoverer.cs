// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Security;
using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Desktop;

[SuppressUnmanagedCodeSecurity]
sealed class NativeDisplayDiscoverer : IDisplayDiscoverer, IDisplayHandleImplProvider, IDisposable {
	readonly InteropStringBuffer _displayNameBuffer = new(200);
	bool _isDisposed = false;

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_display_count")]
	static extern InteropResult GetDisplayCount(out int outResult);
	public unsafe NativeResourceCollection<Display> GetAll() {
		ThrowIfThisIsDisposed();
		return new NativeResourceCollection<Display>(this, &GetResourceCollectionCount, &GetResourceCollectionItem);
	}
	static int GetResourceCollectionCount(object loader) {
		((NativeDisplayDiscoverer) loader).ThrowIfThisIsDisposed();
		GetDisplayCount(out var result).ThrowIfFailure();
		return result;
	}
	static Display GetResourceCollectionItem(object loader, int index) => new(index, ((NativeDisplayDiscoverer) loader));

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_recommended_display")]
	static extern InteropResult GetRecommendedDisplay(out DisplayHandle outResult);
	public Display GetRecommended() {
		ThrowIfThisIsDisposed();
		GetRecommendedDisplay(
			out var result
		).ThrowIfFailure();
		return new(result, this);
	}
	public bool GetIsRecommended(DisplayHandle handle) {
		ThrowIfThisIsDisposed();
		GetRecommendedDisplay(
			out var result
		).ThrowIfFailure();
		return handle == result;
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_primary_display")]
	static extern InteropResult GetPrimaryDisplay(out DisplayHandle outResult);
	public Display GetPrimary() {
		ThrowIfThisIsDisposed();
		GetPrimaryDisplay(
			out var result
		).ThrowIfFailure();
		return new(result, this);
	}
	public bool GetIsPrimary(DisplayHandle handle) {
		ThrowIfThisIsDisposed();
		GetPrimaryDisplay(
			out var result
		).ThrowIfFailure();
		return handle == result;
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_display_resolution")]
	static extern InteropResult GetDisplayResolution(DisplayHandle handle, out int outWidth, out int outHeight);
	public XYPair GetResolution(DisplayHandle handle) {
		ThrowIfThisIsDisposed();
		GetDisplayResolution(
			handle,
			out var width,
			out var height
		).ThrowIfFailure();
		return (width, height);
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_display_positional_offset")]
	static extern InteropResult GetDisplayPositionalOffset(DisplayHandle handle, out int outXOffset, out int outYOffset);
	public XYPair GetPositionOffset(DisplayHandle handle) {
		ThrowIfThisIsDisposed();
		GetDisplayPositionalOffset(
			handle,
			out var x,
			out var y
		).ThrowIfFailure();
		return (x, y);
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_display_name")]
	static extern InteropResult GetDisplayName(DisplayHandle handle, ref byte utf8BufferPtr, int bufferLength);
	public int GetNameMaxLength() => _displayNameBuffer.BufferLength;
	public int GetName(DisplayHandle handle, Span<char> dest) {
		ThrowIfThisIsDisposed();
		GetDisplayName(
			handle,
			ref _displayNameBuffer.BufferRef,
			_displayNameBuffer.BufferLength
		).ThrowIfFailure();
		return _displayNameBuffer.ReadTo(dest);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			_displayNameBuffer.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		if (_isDisposed) throw new InvalidOperationException("Discoverer has been disposed.");
	}
}