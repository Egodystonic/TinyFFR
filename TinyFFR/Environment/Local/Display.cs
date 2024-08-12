// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Local;

public readonly struct Display : IEquatable<Display> {
	readonly DisplayHandle _handle;
	readonly IDisplayImplProvider _impl;

	IDisplayImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Display>();

	public bool IsPrimary => Implementation.GetIsPrimary(_handle);
	public bool IsRecommended => Implementation.GetIsRecommended(_handle);
	public string Name => Implementation.GetName(_handle);
	public ReadOnlySpan<DisplayMode> SupportedDisplayModes => Implementation.GetSupportedDisplayModes(_handle);
	public DisplayMode HighestSupportedResolutionMode => Implementation.GetHighestSupportedResolutionMode(_handle);
	public DisplayMode HighestSupportedRefreshRateMode => Implementation.GetHighestSupportedRefreshRateMode(_handle);
	public XYPair<int> CurrentResolution => Implementation.GetCurrentResolution(_handle);
	internal XYPair<int> GlobalPositionOffset => Implementation.GetGlobalPositionOffset(_handle);

	internal Display(DisplayHandle handle, IDisplayImplProvider impl) {
		ArgumentNullException.ThrowIfNull(impl);
		_handle = handle;
		_impl = impl;
	}

	public int GetNameUsingSpan(Span<char> dest) => Implementation.GetNameUsingSpan(_handle, dest);
	public int GetNameSpanMaxLength() => Implementation.GetNameSpanMaxLength(_handle);

	internal XYPair<int> TranslateDisplayLocalWindowPositionToGlobal(XYPair<int> displayLocalPosition) => displayLocalPosition + GlobalPositionOffset;
	internal XYPair<int> TranslateGlobalWindowPositionToDisplayLocal(XYPair<int> globalPosition) => globalPosition - GlobalPositionOffset;

	public override string ToString() => $"{nameof(Display)} \"{Name}\" ({CurrentResolution.X:#} x {CurrentResolution.Y:#}){(IsPrimary ? " (Primary)" : "")}{(IsRecommended ? " (Recommended)" : "")}";

	#region Equality
	public bool Equals(Display other) => _handle == other._handle && _impl == other._impl;
	public override bool Equals(object? obj) => obj is Display other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(Display left, Display right) => left.Equals(right);
	public static bool operator !=(Display left, Display right) => !left.Equals(right);
	#endregion
}