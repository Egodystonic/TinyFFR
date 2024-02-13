// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Input;

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 16)]
public readonly struct MouseClickEvent : IEquatable<MouseClickEvent> {
	[FieldOffset(0)]
	readonly XYPair<int> _location;
	[FieldOffset(8)]
	readonly MouseKey _key;
	[FieldOffset(12)]
	readonly int _consecutiveClickCount;

	public XYPair<int> Location => _location;
	public MouseKey Key => _key;
	public int ConsecutiveClickCount => _consecutiveClickCount;

	public MouseClickEvent(XYPair<int> location, MouseKey key, int consecutiveClickCount) {
		_location = location;
		_key = key;
		_consecutiveClickCount = consecutiveClickCount;
	}

	public override string ToString() {
		return $"{Key} click #{ConsecutiveClickCount:N0} at {Location}";
	}

	public bool Equals(MouseClickEvent other) {
		return _location.Equals(other._location) && _key == other._key && _consecutiveClickCount == other._consecutiveClickCount;
	}

	public override bool Equals(object? obj) {
		return obj is MouseClickEvent other && Equals(other);
	}

	public override int GetHashCode() {
		return HashCode.Combine(_location, (int) _key, _consecutiveClickCount);
	}

	public static bool operator ==(MouseClickEvent left, MouseClickEvent right) => left.Equals(right);
	public static bool operator !=(MouseClickEvent left, MouseClickEvent right) => !left.Equals(right);
}