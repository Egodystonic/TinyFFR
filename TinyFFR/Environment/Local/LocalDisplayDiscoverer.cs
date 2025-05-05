// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Security;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using DisplayModeArray = Egodystonic.TinyFFR.Environment.Local.DisplayMode[];

namespace Egodystonic.TinyFFR.Environment.Local;

[SuppressUnmanagedCodeSecurity]
sealed class LocalDisplayDiscoverer : IDisplayDiscoverer, IDisplayImplProvider, IDisposable {
	const int MaxDisplayNameLength = 200; // Should be low enough to be stackalloc'able (or rewrite ctor)
	const int MaxDisplayCount = 1_000_000;
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly DisplayModeArray[] _displayModes;
	readonly string[] _displayNames;
	readonly Display[] _displays;
	readonly ResourceHandle<Display>? _recommendedHandle;
	readonly ResourceHandle<Display>? _primaryHandle;
	bool _isDisposed = false;

	public ReadOnlySpan<Display> All => _isDisposed ? throw new ObjectDisposedException(nameof(IDisplayDiscoverer)) : _displays.AsSpan();
	public Display? Recommended => _isDisposed ? throw new ObjectDisposedException(nameof(IDisplayDiscoverer)) : (_recommendedHandle != null ? new Display(_recommendedHandle.Value, this) : null);
	public Display? Primary => _isDisposed ? throw new ObjectDisposedException(nameof(IDisplayDiscoverer)) : (_primaryHandle != null ? new Display(_primaryHandle.Value, this) : null);

	public LocalDisplayDiscoverer(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		
		_globals = globals;

		GetDisplayCount(out var numDisplays).ThrowIfFailure();
		if (numDisplays is > MaxDisplayCount or < 0) throw new InvalidOperationException($"Display discoverer found {numDisplays} displays (too low/too high).");
		_displays = new Display[numDisplays];
		_displayModes = new DisplayModeArray[numDisplays];
		_displayNames = new string[numDisplays];
		if (numDisplays == 0) {
			_recommendedHandle = null;
			_primaryHandle = null;
			return;
		}

		GetPrimaryDisplay(out var primaryHandle).ThrowIfFailure();
		GetRecommendedDisplay(out var recommendedHandle).ThrowIfFailure();

		using var nameBuffer = new InteropStringBuffer(MaxDisplayNameLength, true);
		Span<char> nameBufferUtf16 = stackalloc char[MaxDisplayNameLength];

		for (var handle = (nuint) 0; handle < (nuint) numDisplays; ++handle) {
			GetDisplayModeCount(handle, out var numDisplayModes).ThrowIfFailure();
			if (numDisplayModes < 1) continue;
			var modes = new DisplayMode[numDisplayModes];
			for (var i = 0; i < numDisplayModes; ++i) {
				GetDisplayMode(handle, i, out var modeWidth, out var modeHeight, out var modeRate).ThrowIfFailure();
				modes[i] = new DisplayMode((modeWidth, modeHeight), modeRate);
			}

			if (handle == primaryHandle) _primaryHandle = handle;
			if (handle == recommendedHandle) _recommendedHandle = handle;

			GetDisplayName(
				handle,
				ref nameBuffer.BufferRef,
				nameBuffer.BufferLength
			).ThrowIfFailure();
			var nameLen = nameBuffer.ConvertToUtf16(nameBufferUtf16);
			var name = new String(nameBufferUtf16[..nameLen]);
			
			var display = new Display(handle, this);
			_displays[handle] = display;
			_displayModes[handle] = modes;
			_displayNames[handle] = name;
		}
	}

	public DisplayMode GetHighestSupportedResolutionMode(ResourceHandle<Display> handle) {
		ThrowIfDisposedOrUnrecognizedDisplay(handle);
		var modes = _displayModes[handle];
		var result = modes[0];
		foreach (var displayMode in modes[1..]) {
			if (displayMode.Resolution.ToVector2().LengthSquared() > result.Resolution.ToVector2().LengthSquared()) {
				result = displayMode;
			}
			else if (displayMode.Resolution == result.Resolution && displayMode.RefreshRateHz > result.RefreshRateHz) {
				result = displayMode;
			}
		}
		return result;
	}
	public DisplayMode GetHighestSupportedRefreshRateMode(ResourceHandle<Display> handle) {
		ThrowIfDisposedOrUnrecognizedDisplay(handle);
		var modes = _displayModes[handle];
		var result = modes[0];
		foreach (var displayMode in modes[1..]) {
			if (displayMode.RefreshRateHz > result.RefreshRateHz) {
				result = displayMode;
			}
			else if (displayMode.RefreshRateHz == result.RefreshRateHz && displayMode.Resolution.ToVector2().LengthSquared() > result.Resolution.ToVector2().LengthSquared()) {
				result = displayMode;
			}
		}
		return result;
	}

	public bool GetIsPrimary(ResourceHandle<Display> handle) {
		ThrowIfDisposedOrUnrecognizedDisplay(handle);
		return handle == _primaryHandle;
	}
	public bool GetIsRecommended(ResourceHandle<Display> handle) {
		ThrowIfDisposedOrUnrecognizedDisplay(handle);
		return handle == _recommendedHandle;
	}

	public string GetNameAsNewStringObject(ResourceHandle<Display> handle) {
		ThrowIfDisposedOrUnrecognizedDisplay(handle);
		return _displayNames[handle];
	}
	public int GetNameLength(ResourceHandle<Display> handle) {
		ThrowIfDisposedOrUnrecognizedDisplay(handle);
		return _displayNames[handle].Length;
	}
	public void CopyName(ResourceHandle<Display> handle, Span<char> destinationBuffer) {
		ThrowIfDisposedOrUnrecognizedDisplay(handle);
		_displayNames[handle].CopyTo(destinationBuffer);
	}

	public ReadOnlySpan<DisplayMode> GetSupportedDisplayModes(ResourceHandle<Display> handle) {
		ThrowIfDisposedOrUnrecognizedDisplay(handle);
		return _displayModes[handle];
	}
	public XYPair<int> GetCurrentResolution(ResourceHandle<Display> handle) {
		ThrowIfDisposedOrUnrecognizedDisplay(handle);
		GetDisplayResolution(
			handle,
			out var outWidth,
			out var outHeight
		).ThrowIfFailure();
		return (outWidth, outHeight);
	}
	public XYPair<int> GetGlobalPositionOffset(ResourceHandle<Display> handle) {
		ThrowIfDisposedOrUnrecognizedDisplay(handle);
		GetDisplayPositionalOffset(
			handle,
			out var outXOffset,
			out var outYOffset
		).ThrowIfFailure();
		return (outXOffset, outYOffset);
	}

	public override string ToString() => _isDisposed ? "TinyFFR Display Discoverer [Disposed]" : "TinyFFR Display Discoverer";

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_recommended_display")]
	static extern InteropResult GetRecommendedDisplay(out nuint outResult);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_primary_display")]
	static extern InteropResult GetPrimaryDisplay(out nuint outResult);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_display_count")]
	static extern InteropResult GetDisplayCount(out int outResult);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_display_name")]
	static extern InteropResult GetDisplayName(nuint handle, ref byte utf8BufferPtr, int bufferLength);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_display_mode_count")]
	static extern InteropResult GetDisplayModeCount(nuint handle, out int outNumDisplayModes);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_display_mode")]
	static extern InteropResult GetDisplayMode(nuint handle, int displayModeIndex, out int outWidth, out int outHeight, out int outRefreshRateHz);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_display_resolution")]
	static extern InteropResult GetDisplayResolution(nuint handle, out int outWidth, out int outHeight);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_display_positional_offset")]
	static extern InteropResult GetDisplayPositionalOffset(nuint handle, out int outXOffset, out int outYOffset);
	#endregion

	#region Disposal
	void ThrowIfDisposedOrUnrecognizedDisplay(ResourceHandle<Display> handle) {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(Display));
		if (handle >= (nuint) _displays.Length) throw new InvalidOperationException("Given display was not created by this display discoverer.");
	}

	public void Dispose() {
		_isDisposed = true;
	}

	public bool IsValid(ResourceHandle<Display> handle) => !_isDisposed && handle < (nuint) _displays.Length;
	#endregion
}