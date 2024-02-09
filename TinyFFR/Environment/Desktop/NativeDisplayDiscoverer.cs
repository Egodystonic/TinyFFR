// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Security;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Environment.Desktop;

[SuppressUnmanagedCodeSecurity]
sealed class NativeDisplayDiscoverer : IDisplayDiscoverer, IDisplayHandleImplProvider, IDisposable {
	readonly InteropStringBuffer _displayNameBuffer = new(200);
	readonly ArrayPoolBackedVector<Display> _displays = new();
	readonly ArrayPoolBackedMap<DisplayHandle, ArrayPoolBackedVector<RefreshRate>> _refreshRates = new();
	bool _isDisposed = false;

	public NativeDisplayDiscoverer() {
		GetDisplayCount(out var numDisplays).ThrowIfFailure();
		for (var i = 0; i < numDisplays; ++i) {
			_displays.Add(new Display(i, this));
			_refreshRates.Add(i, new ArrayPoolBackedVector<RefreshRate>());
			GetDisplayModeCount(i, out var numDisplayModes).ThrowIfFailure();
			for (var j = 0; j < numDisplayModes; ++j) {
				GetDisplayMode(i, j, out var modeWidth, out var modeHeight, out var modeRate).ThrowIfFailure();
				_refreshRates[i].Add(new RefreshRate((modeWidth, modeHeight), modeRate));
			}
		}
	}

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_display_count")]
	static extern InteropResult GetDisplayCount(out int outResult);
	public ReadOnlySpan<Display> GetAll() {
		ThrowIfThisIsDisposed();
		return _displays.AsSpan;
	}

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

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_display_mode_count")]
	static extern InteropResult GetDisplayModeCount(DisplayHandle handle, out int outNumDisplayModes);
	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_display_mode")]
	static extern InteropResult GetDisplayMode(DisplayHandle handle, int displayModeIndex, out int outWidth, out int outHeight, out int outRefreshRateHz);
	public ReadOnlySpan<RefreshRate> GetSupportedRefreshRates(DisplayHandle handle) {
		ThrowIfThisIsDisposed();
		if (!_refreshRates.TryGetValue(handle, out var vector)) throw new InvalidOperationException($"Invalid {nameof(Display)}.");
		return vector.AsSpan;
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			_displayNameBuffer.Dispose();
			_displays.Dispose();
			foreach (var kvp in _refreshRates) kvp.Value.Dispose();
			_refreshRates.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		if (_isDisposed) throw new InvalidOperationException("Discoverer has been disposed.");
	}
}