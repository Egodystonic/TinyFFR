// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Interop;

namespace Egodystonic.TinyFFR.Environment.Input;

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 8)]
public readonly struct KeyboardOrMouseKeyEvent : IEquatable<KeyboardOrMouseKeyEvent> {
	[FieldOffset(0)]
	readonly KeyboardOrMouseKey _key;
	[FieldOffset(4)]
	readonly InteropBool _keyDown;

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