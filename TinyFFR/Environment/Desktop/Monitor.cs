// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Desktop;

public readonly struct Monitor : IEquatable<Monitor> {
	readonly IMonitorHandleImplProvider _impl;
	internal MonitorHandle Handle { get; }

	public string Name {
		get {
			var maxSpanLength = GetNameSpanMaxLength();
			var dest = maxSpanLength <= 1000 ? stackalloc char[maxSpanLength] : new char[maxSpanLength];

			var numCharsWritten = GetNameUsingSpan(dest);
			return new(dest[..numCharsWritten]);
		}
	}

	public XYPair Resolution {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetResolution(Handle);
	}
	
	internal Monitor(MonitorHandle handle, IMonitorHandleImplProvider impl) {
		Handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameUsingSpan(Span<char> dest) => _impl.GetName(Handle, dest);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameSpanMaxLength() => _impl.GetNameMaxLength();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal XYPair TranslateMonitorLocalWindowPositionToGlobal(XYPair monitorLocalPosition) => monitorLocalPosition + _impl.GetPositionOffset(Handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal XYPair TranslateGlobalWindowPositionToMonitorLocal(XYPair globalPosition) => globalPosition - _impl.GetPositionOffset(Handle);

	internal void ThrowIfInvalid() {
		if (_impl == null) throw InvalidObjectException.InvalidDefault<Monitor>();
	}

	public bool Equals(Monitor other) => Handle == other.Handle;
	public override bool Equals(object? obj) => obj is Monitor other && Equals(other);
	public override int GetHashCode() => Handle.GetHashCode();
	public static bool operator ==(Monitor left, Monitor right) => left.Equals(right);
	public static bool operator !=(Monitor left, Monitor right) => !left.Equals(right);
}