// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Input;

/// <summary>
/// Represents a <see cref="KeyboardOrMouseKey"/> either being pressed down or released.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 8)]
public readonly struct KeyboardOrMouseKeyEvent : IEquatable<KeyboardOrMouseKeyEvent> {
	[FieldOffset(0)]
	readonly KeyboardOrMouseKey _key;
	[FieldOffset(4)]
	readonly InteropBool _keyDown;

	/// <summary>
	/// The key this event pertains to.
	/// </summary>
	public KeyboardOrMouseKey Key => _key;
	/// <summary>
	/// <see langword="true"/> if this event represents <see cref="Key"/> being pressed down; <see langword="false"/> if it represents it being released.
	/// </summary>
	public bool KeyDown => _keyDown;

	/// <summary>
	/// Constructs a new <see cref="KeyboardOrMouseKeyEvent"/>.
	/// </summary>
	/// <param name="key">The value for <see cref="Key"/>.</param>
	/// <param name="keyDown">The value for <see cref="KeyDown"/>.</param>
	public KeyboardOrMouseKeyEvent(KeyboardOrMouseKey key, bool keyDown) {
		_key = key;
		_keyDown = keyDown;
	}

	/// <inheritdoc/>
	public bool Equals(KeyboardOrMouseKeyEvent other) => _key == other._key && _keyDown == other._keyDown;
	/// <inheritdoc/>
	public override bool Equals(object? obj) => obj is KeyboardOrMouseKeyEvent other && Equals(other);
	/// <inheritdoc/>
	public override int GetHashCode() => HashCode.Combine((int) _key, _keyDown);
	/// <summary>
	/// <see cref="Equals(KeyboardOrMouseKeyEvent)"/>
	/// </summary>
	public static bool operator ==(KeyboardOrMouseKeyEvent left, KeyboardOrMouseKeyEvent right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(KeyboardOrMouseKeyEvent)"/>
	/// </summary>
	public static bool operator !=(KeyboardOrMouseKeyEvent left, KeyboardOrMouseKeyEvent right) => !left.Equals(right);
}