// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Input;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = sizeof(KeyboardOrMouseKey) + sizeof(bool))]
public readonly struct KeyboardOrMouseKeyEvent : IEquatable<KeyboardOrMouseKeyEvent> {
	readonly KeyboardOrMouseKey _key;
	[MarshalAs(UnmanagedType.U1)]
	readonly bool _keyDown;

	public KeyboardOrMouseKey Key => _key;
	public bool KeyDown => _keyDown;

	public KeyboardOrMouseKeyEvent(KeyboardOrMouseKey key, bool keyDown) {
		_key = key;
		_keyDown = keyDown;
	}

	public bool Equals(KeyboardOrMouseKeyEvent other) => _key == other._key && _keyDown == other._keyDown;
	public override bool Equals(object? obj) => obj is KeyboardOrMouseKeyEvent other && Equals(other);
	public override int GetHashCode() => HashCode.Combine((int) _key, _keyDown);
	public static bool operator ==(KeyboardOrMouseKeyEvent left, KeyboardOrMouseKeyEvent right) => left.Equals(right);
	public static bool operator !=(KeyboardOrMouseKeyEvent left, KeyboardOrMouseKeyEvent right) => !left.Equals(right);
}