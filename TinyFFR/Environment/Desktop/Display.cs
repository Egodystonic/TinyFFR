// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;
using System.Reflection.Metadata;

namespace Egodystonic.TinyFFR.Environment.Desktop;

public readonly struct Display : IEquatable<Display> {
	readonly DisplayHandle _handle;
	readonly DisplayMode[] _displayModes;

	public bool IsPrimary { get; }
	public bool IsRecommended { get; }
	public string Name { get; }
	public ReadOnlySpan<DisplayMode> SupportedDisplayModes => _displayModes.AsSpan();
	public DisplayMode HighestSupportedResolution { get; }
	public DisplayMode HighestSupportedRefreshRate { get; }

	public XYPair<int> CurrentResolution {
		get {
			GetDisplayResolution(
				_handle,
				out var width,
				out var height
			).ThrowIfFailure();
			return (width, height);
		}
	}

	internal XYPair<int> GlobalPositionOffset {
		get {
			GetDisplayPositionalOffset(
				_handle,
				out var x,
				out var y
			).ThrowIfFailure();
			return (x, y);
		}
	}

	internal Display(DisplayHandle handle, DisplayMode[] displayModes, bool isPrimary, bool isRecommended, string name) {
		ArgumentNullException.ThrowIfNull(displayModes);
		ArgumentNullException.ThrowIfNull(name);
		if (displayModes.Length < 1) throw new ArgumentException("Expected at least one supported display mode.", nameof(displayModes));

		_handle = handle;
		_displayModes = displayModes;
		IsPrimary = isPrimary;
		IsRecommended = isRecommended;
		Name = name;

		HighestSupportedResolution = CalculateHighestResDisplayMode(displayModes);
		HighestSupportedRefreshRate = CalculateHighestRateDisplayMode(displayModes);
	}

	static DisplayMode CalculateHighestResDisplayMode(DisplayMode[] modes) {
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
	static DisplayMode CalculateHighestRateDisplayMode(DisplayMode[] modes) {
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

#pragma warning disable CA1024 // "Use properties" - These two methods are provided just for consistency's sake with other objects that actually provide a benefit to using them.
	public int GetNameUsingSpan(Span<char> dest) {
		Name.CopyTo(dest);
		return dest.Length;
	}
	public int GetNameSpanMaxLength() => Name.Length;
#pragma warning restore CA1024

	internal XYPair<int> TranslateDisplayLocalWindowPositionToGlobal(XYPair<int> displayLocalPosition) => displayLocalPosition + GlobalPositionOffset;
	internal XYPair<int> TranslateGlobalWindowPositionToDisplayLocal(XYPair<int> globalPosition) => globalPosition - GlobalPositionOffset;

	public override string ToString() => $"{nameof(Display)} \"{Name}\" ({CurrentResolution.X:#} x {CurrentResolution.Y:#}){(IsPrimary ? " (Primary)" : "")}{(IsRecommended ? " (Recommended)" : "")}";

	#region Native Methods
	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_display_resolution")]
	static extern InteropResult GetDisplayResolution(DisplayHandle handle, out int outWidth, out int outHeight);

	[DllImport(NativeUtils.NativeLibName, EntryPoint = "get_display_positional_offset")]
	static extern InteropResult GetDisplayPositionalOffset(DisplayHandle handle, out int outXOffset, out int outYOffset);
	#endregion

	#region Equality
	public bool Equals(Display other) => _handle == other._handle;
	public override bool Equals(object? obj) => obj is Display other && Equals(other);
	public override int GetHashCode() => _handle.GetHashCode();
	public static bool operator ==(Display left, Display right) => left.Equals(right);
	public static bool operator !=(Display left, Display right) => !left.Equals(right);
	#endregion
}