// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Input;

public readonly struct GameControllerButtonEvent : IEquatable<GameControllerButtonEvent> {
	readonly GameControllerButton _button;
	readonly bool _buttonDown;

	public GameControllerButton Button => _button;
	public bool ButtonDown => _buttonDown;

	public GameControllerButtonEvent(GameControllerButton button, bool buttonDown) {
		_button = button;
		_buttonDown = buttonDown;
	}

	public bool Equals(GameControllerButtonEvent other) => _button == other._button && _buttonDown == other._buttonDown;
	public override bool Equals(object? obj) => obj is GameControllerButtonEvent other && Equals(other);
	public override int GetHashCode() => HashCode.Combine((int) _button, _buttonDown);
	public static bool operator ==(GameControllerButtonEvent left, GameControllerButtonEvent right) => left.Equals(right);
	public static bool operator !=(GameControllerButtonEvent left, GameControllerButtonEvent right) => !left.Equals(right);
}