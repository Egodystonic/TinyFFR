// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Security;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using DisplayModeArray = Egodystonic.TinyFFR.Environment.Local.DisplayMode[];

namespace Egodystonic.TinyFFR.Environment.Local;

[SuppressUnmanagedCodeSecurity]
sealed class LocalDisplayDiscoverer : IDisplayDiscoverer, IDisplayImplProvider {
	const int MaxDisplayNameLength = 200; // Should be low enough to be stackalloc'able (or rewrite ctor)
	const int MaxDisplayCount = 1_000_000;
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly DisplayModeArray[] _displayModes;
	readonly string[] _displayNames;
	readonly Display[] _displays;
	readonly DisplayHandle? _recommendedHandle;
	readonly DisplayHandle? _primaryHandle;

	public ReadOnlySpan<Display> All => _displays.AsSpan();
	public Display? Recommended => _recommendedHandle != null ? new Display(_recommendedHandle.Value, this) : null;
	public Display? Primary => _primaryHandle != null ? new Display(_primaryHandle.Value, this) : null;

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

	public DisplayMode GetHighestSupportedResolutionMode(DisplayHandle handle) {
		ThrowIfUnrecognizedDisplay(handle);
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
	public DisplayMode GetHighestSupportedRefreshRateMode(DisplayHandle handle) {
		ThrowIfUnrecognizedDisplay(handle);
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

	public bool GetIsPrimary(DisplayHandle handle) {
		ThrowIfUnrecognizedDisplay(handle);
		return handle == _primaryHandle;
	}
	public bool GetIsRecommended(DisplayHandle handle) {
		ThrowIfUnrecognizedDisplay(handle);
		return handle == _recommendedHandle;
	}

	public string GetName(DisplayHandle handle) {
		ThrowIfUnrecognizedDisplay(handle);
		return _displayNames[handle];
	}
	public int GetNameUsingSpan(DisplayHandle handle, Span<char> dest) {
		ThrowIfUnrecognizedDisplay(handle);
		_displayNames[handle].CopyTo(dest);
		return _displayNames[handle].Length;
	}
	public int GetNameSpanLength(DisplayHandle handle) {
		ThrowIfUnrecognizedDisplay(handle);
		return _displayNames[handle].Length;
	}
	public ReadOnlySpan<DisplayMode> GetSupportedDisplayModes(DisplayHandle handle) {
		ThrowIfUnrecognizedDisplay(handle);
		return _displayModes[handle];
	}
	public XYPair<int> GetCurrentResolution(DisplayHandle handle) {
		ThrowIfUnrecognizedDisplay(handle);
		GetDisplayResolution(
			handle,
			out var outWidth,
			out var outHeight
		).ThrowIfFailure();
		return (outWidth, outHeight);
	}
	public XYPair<int> GetGlobalPositionOffset(DisplayHandle handle) {
		ThrowIfUnrecognizedDisplay(handle);
		GetDisplayPositionalOffset(
			handle,
			out var outXOffset,
			out var outYOffset
		).ThrowIfFailure();
		return (outXOffset, outYOffset);
	}

	void ThrowIfUnrecognizedDisplay(DisplayHandle handle) {
		if (handle >= (nuint) _displays.Length) throw new InvalidOperationException("Given display was not created by this display discoverer.");
	}

	public override string ToString() => "TinyFFR Display Discoverer";

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
}