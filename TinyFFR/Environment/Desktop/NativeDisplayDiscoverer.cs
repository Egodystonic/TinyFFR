// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Reflection.Metadata;
using System.Security;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Environment.Desktop;

[SuppressUnmanagedCodeSecurity]
sealed class NativeDisplayDiscoverer : IDisplayDiscoverer {
	const int MaxDisplayNameLength = 200; // Should be enough to be stackalloc'able (or rewrite ctor)
	const int MaxDisplayCount = 1_000_000;
	readonly Display[] _displays;

	public ReadOnlySpan<Display> All => _displays.AsSpan();
	public Display? Recommended { get; }
	public Display? Primary { get; }

	public NativeDisplayDiscoverer() {
		GetDisplayCount(out var numDisplays).ThrowIfFailure();
		if (numDisplays > MaxDisplayCount || numDisplays < 0) throw new InvalidOperationException($"Display discoverer found {numDisplays} displays (invalid number).");
		_displays = new Display[numDisplays];
		if (numDisplays == 0) {
			Recommended = null;
			Primary = null;
			return;
		}

		GetPrimaryDisplay(out var primaryHandle).ThrowIfFailure();
		GetRecommendedDisplay(out var recommendedHandle).ThrowIfFailure();

		using var nameBuffer = new InteropStringBuffer(MaxDisplayNameLength, true);
		Span<char> nameBufferUtf16 = stackalloc char[MaxDisplayNameLength];

		for (var handle = 0; handle < numDisplays; ++handle) {
			GetDisplayModeCount(handle, out var numDisplayModes).ThrowIfFailure();
			if (numDisplayModes < 1) continue;
			var modes = new DisplayMode[numDisplayModes];
			for (var i = 0; i < numDisplayModes; ++i) {
				GetDisplayMode(handle, i, out var modeWidth, out var modeHeight, out var modeRate).ThrowIfFailure();
				modes[i] = new DisplayMode((modeWidth, modeHeight), modeRate);
			}

			var isPrimary = handle == primaryHandle;
			var isRecommended = handle == recommendedHandle;

			GetDisplayName(
				handle,
				ref nameBuffer.BufferRef,
				nameBuffer.BufferLength
			).ThrowIfFailure();
			var nameLen = nameBuffer.ConvertToUtf16(nameBufferUtf16);

			var display = new Display(handle, modes, isPrimary, isRecommended, new(nameBufferUtf16[..nameLen]));
			if (isPrimary) Primary = display;
			if (isRecommended) Recommended = display;
			_displays[handle] = display;
		}
	}
	
	#region Native Methods
	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_recommended_display")]
	static extern InteropResult GetRecommendedDisplay(out DisplayHandle outResult);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_primary_display")]
	static extern InteropResult GetPrimaryDisplay(out DisplayHandle outResult);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_display_count")]
	static extern InteropResult GetDisplayCount(out int outResult);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_display_name")]
	static extern InteropResult GetDisplayName(DisplayHandle handle, ref byte utf8BufferPtr, int bufferLength);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_display_mode_count")]
	static extern InteropResult GetDisplayModeCount(DisplayHandle handle, out int outNumDisplayModes);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_display_mode")]
	static extern InteropResult GetDisplayMode(DisplayHandle handle, int displayModeIndex, out int outWidth, out int outHeight, out int outRefreshRateHz);
	#endregion
}