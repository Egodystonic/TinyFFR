// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Environment.Local;

namespace Egodystonic.TinyFFR.Environment.Input;

/// <summary>
/// Represents a single click of a mouse button, including where it happened and whether it formed part of a double-click (or longer run).
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 16)]
public readonly struct MouseClickEvent : IEquatable<MouseClickEvent> {
	[FieldOffset(0)]
	readonly XYPair<int> _location;
	[FieldOffset(8)]
	readonly MouseKey _key;
	[FieldOffset(12)]
	readonly int _consecutiveClickCount;

	/// <summary>
	/// The location of this mouse click relative to the target <see cref="Window"/>'s top-left corner.
	/// </summary>
	public XYPair<int> Location => _location;
	/// <summary>
	/// The mouse button that was clicked.
	/// </summary>
	public MouseKey Key => _key;
	/// <summary>
	/// Where this click falls in a run of rapid repeated clicks: <c>1</c> for a single click, <c>2</c> for the second click of a double-click, <c>3</c> for the third, and so on.
	/// </summary>
	/// <remarks>
	/// The maximum delay permitted between two clicks for them to count as consecutive is determined by the host operating system's own double-click setting,
	/// meaning this respects whatever preference the user has configured. Note that a double-click produces two separate events (one with a count of <c>1</c>,
	/// then one with a count of <c>2</c>), rather than a single event with a count of <c>2</c>.
	/// </remarks>
	public int ConsecutiveClickCount => _consecutiveClickCount;

	/// <summary>
	/// Constructs a new <see cref="MouseClickEvent"/>.
	/// </summary>
	/// <param name="location">The value for <see cref="Location"/>.</param>
	/// <param name="key">The value for <see cref="Key"/>.</param>
	/// <param name="consecutiveClickCount">The value for <see cref="ConsecutiveClickCount"/>.</param>
	public MouseClickEvent(XYPair<int> location, MouseKey key, int consecutiveClickCount) {
		_location = location;
		_key = key;
		_consecutiveClickCount = consecutiveClickCount;
	}

	/// <inheritdoc/>
	public override string ToString() {
		return $"{Key} click #{ConsecutiveClickCount:N0} at {Location}";
	}

	/// <inheritdoc/>
	public bool Equals(MouseClickEvent other) {
		return _location.Equals(other._location) && _key == other._key && _consecutiveClickCount == other._consecutiveClickCount;
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj) {
		return obj is MouseClickEvent other && Equals(other);
	}

	/// <inheritdoc/>
	public override int GetHashCode() {
		return HashCode.Combine(_location, (int) _key, _consecutiveClickCount);
	}

	/// <summary>
	/// <see cref="Equals(MouseClickEvent)"/>
	/// </summary>
	public static bool operator ==(MouseClickEvent left, MouseClickEvent right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(MouseClickEvent)"/>
	/// </summary>
	public static bool operator !=(MouseClickEvent left, MouseClickEvent right) => !left.Equals(right);
}