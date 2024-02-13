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
	readonly ArrayPoolBackedMap<DisplayHandle, ArrayPoolBackedVector<DisplayMode>> _displayModes = new();
	bool _isDisposed = false;

	public NativeDisplayDiscoverer() {
		GetDisplayCount(out var numDisplays).ThrowIfFailure();
		for (var i = 0; i < numDisplays; ++i) {
			_displays.Add(new Display(i, this));
			_displayModes.Add(i, new ArrayPoolBackedVector<DisplayMode>());
			GetDisplayModeCount(i, out var numDisplayModes).ThrowIfFailure();
			for (var j = 0; j < numDisplayModes; ++j) {
				GetDisplayMode(i, j, out var modeWidth, out var modeHeight, out var modeRate).ThrowIfFailure();
				_displayModes[i].Add(new DisplayMode((modeWidth, modeHeight), modeRate));
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
	public XYPair<int> GetResolution(DisplayHandle handle) {
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
	public XYPair<int> GetPositionOffset(DisplayHandle handle) {
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
	public ReadOnlySpan<DisplayMode> GetSupportedDisplayModes(DisplayHandle handle) {
		ThrowIfThisIsDisposed();
		if (!_displayModes.TryGetValue(handle, out var vector)) throw new InvalidOperationException($"Invalid {nameof(Display)}.");
		return vector.AsSpan;
	}
	public DisplayMode GetHighestSupportedResolution(DisplayHandle handle) {
		var supportedModes = GetSupportedDisplayModes(handle);
		if (supportedModes.Length == 0) throw new InvalidOperationException($"Can not get highest resolution mode for {nameof(Display)}; no supported modes found.");
		
		var result = supportedModes[0];
		foreach (var displayMode in supportedModes[1..]) {
			if (displayMode.Resolution.ToVector2().LengthSquared() > result.Resolution.ToVector2().LengthSquared()) {
				result = displayMode;
			}
			else if (displayMode.Resolution == result.Resolution && displayMode.RefreshRateHz > result.RefreshRateHz) {
				result = displayMode;
			}
		}
		return result;
	}
	public DisplayMode GetHighestSupportedRefreshRate(DisplayHandle handle) {
		var supportedModes = GetSupportedDisplayModes(handle);
		if (supportedModes.Length == 0) throw new InvalidOperationException($"Can not get highest refresh-rate mode for {nameof(Display)}; no supported modes found.");

		var result = supportedModes[0];
		foreach (var displayMode in supportedModes[1..]) {
			if (displayMode.RefreshRateHz > result.RefreshRateHz) {
				result = displayMode;
			}
			else if (displayMode.RefreshRateHz == result.RefreshRateHz && displayMode.Resolution.ToVector2().LengthSquared() > result.Resolution.ToVector2().LengthSquared()) {
				result = displayMode;
			}
		}
		return result;
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			_displayNameBuffer.Dispose();
			_displays.Dispose();
			foreach (var kvp in _displayModes) kvp.Value.Dispose();
			_displayModes.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, this);
	}
}