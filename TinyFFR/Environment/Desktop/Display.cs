// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Desktop;

public readonly struct Display : IEquatable<Display> {
	readonly IDisplayHandleImplProvider _impl;
	internal DisplayHandle Handle { get; }

	public bool IsPrimary {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetIsPrimary(Handle);
	}

	public bool IsRecommended {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetIsRecommended(Handle);
	}

	public string Name {
		get {
			var maxSpanLength = GetNameSpanMaxLength();
			var dest = maxSpanLength <= 1000 ? stackalloc char[maxSpanLength] : new char[maxSpanLength];

			var numCharsWritten = GetNameUsingSpan(dest);
			return new(dest[..numCharsWritten]);
		}
	}

	public XYPair CurrentResolution {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetResolution(Handle);
	}

	public ReadOnlySpan<DisplayMode> SupportedDisplayModes {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetSupportedDisplayModes(Handle);
	}

	public DisplayMode HighestSupportedResolution {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetHighestSupportedResolution(Handle);
	}

	public DisplayMode HighestSupportedRefreshRate {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetHighestSupportedRefreshRate(Handle);
	}

	internal Display(DisplayHandle handle, IDisplayHandleImplProvider impl) {
		Handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameUsingSpan(Span<char> dest) => _impl.GetName(Handle, dest);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameSpanMaxLength() => _impl.GetNameMaxLength();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal XYPair TranslateDisplayLocalWindowPositionToGlobal(XYPair displayLocalPosition) => displayLocalPosition + _impl.GetPositionOffset(Handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal XYPair TranslateGlobalWindowPositionToDisplayLocal(XYPair globalPosition) => globalPosition - _impl.GetPositionOffset(Handle);

	public bool Equals(Display other) => Handle == other.Handle;
	public override bool Equals(object? obj) => obj is Display other && Equals(other);
	public override int GetHashCode() => Handle.GetHashCode();
	public static bool operator ==(Display left, Display right) => left.Equals(right);
	public static bool operator !=(Display left, Display right) => !left.Equals(right);

	public override string ToString() => $"{nameof(Display)} \"{Name}\" ({CurrentResolution.X:#} x {CurrentResolution.Y:#}){(IsPrimary ? " (Primary)" : "")}{(IsRecommended ? " (Recommended)" : "")}";
}