// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Security;
using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Desktop;

[SuppressUnmanagedCodeSecurity]
sealed class NativeMonitorLoader : IMonitorLoader, IMonitorHandleImplProvider, IDisposable {
	readonly InteropStringBuffer _monitorNameBuffer = new(200);
	bool _isDisposed = false;

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_monitor_count")]
	static extern InteropResult GetMonitorCount(out int outResult);
	public unsafe IterableResourceCollection<Monitor> LoadAll() {
		ThrowIfThisIsDisposed();
		return new IterableResourceCollection<Monitor>(this, &GetResourceCollectionCount, &GetResourceCollectionItem);
	}
	static int GetResourceCollectionCount(object loader) {
		((NativeMonitorLoader) loader).ThrowIfThisIsDisposed();
		GetMonitorCount(out var result);
		return result;
	}
	static Monitor GetResourceCollectionItem(object loader, int index) => new(index, ((NativeMonitorLoader) loader));

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_monitor_resolution")]
	static extern InteropResult GetMonitorResolution(MonitorHandle handle, out int outWidth, out int outHeight);
	public XYPair GetResolution(MonitorHandle handle) {
		ThrowIfThisIsDisposed();
		GetMonitorResolution(
			handle,
			out var width,
			out var height
		).ThrowIfFailure();
		return (width, height);
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_monitor_positional_offset")]
	static extern InteropResult GetMonitorPositionalOffset(MonitorHandle handle, out int outXOffset, out int outYOffset);
	public XYPair GetPositionOffset(MonitorHandle handle) {
		ThrowIfThisIsDisposed();
		GetMonitorPositionalOffset(
			handle,
			out var x,
			out var y
		).ThrowIfFailure();
		return (x, y);
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_monitor_name")]
	static extern InteropResult GetMonitorName(MonitorHandle handle, ref byte utf8BufferPtr, int bufferLength);
	public int GetNameMaxLength() => _monitorNameBuffer.BufferLength;
	public int GetName(MonitorHandle handle, Span<char> dest) {
		ThrowIfThisIsDisposed();
		GetMonitorName(
			handle,
			ref _monitorNameBuffer.BufferRef,
			_monitorNameBuffer.BufferLength
		).ThrowIfFailure();
		return _monitorNameBuffer.ReadTo(dest);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			_monitorNameBuffer.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		if (_isDisposed) throw new InvalidOperationException("Loader has been disposed.");
	}
}