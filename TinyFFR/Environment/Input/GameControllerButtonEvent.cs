// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Input;

/// <summary>
/// Represents a <see cref="GameControllerButton"/> either being pressed or released.
/// </summary>
public readonly struct GameControllerButtonEvent : IEquatable<GameControllerButtonEvent> {
	readonly GameControllerButton _button;
	readonly bool _buttonDown;

	/// <summary>
	/// The button this event pertains to.
	/// </summary>
	public GameControllerButton Button => _button;
	/// <summary>
	/// <see langword="true"/> if this event represents <see cref="Button"/> being pressed down; <see langword="false"/> if it represents it being released.
	/// </summary>
	public bool ButtonDown => _buttonDown;

	/// <summary>
	/// Constructs a new <see cref="GameControllerButtonEvent"/>.
	/// </summary>
	/// <param name="button">The value for <see cref="Button"/>.</param>
	/// <param name="buttonDown">The value for <see cref="ButtonDown"/>.</param>
	public GameControllerButtonEvent(GameControllerButton button, bool buttonDown) {
		_button = button;
		_buttonDown = buttonDown;
	}

	/// <inheritdoc/>
	public bool Equals(GameControllerButtonEvent other) => _button == other._button && _buttonDown == other._buttonDown;
	/// <inheritdoc/>
	public override bool Equals(object? obj) => obj is GameControllerButtonEvent other && Equals(other);
	/// <inheritdoc/>
	public override int GetHashCode() => HashCode.Combine((int) _button, _buttonDown);
	/// <summary>
	/// <see cref="Equals(GameControllerButtonEvent)"/>
	/// </summary>
	public static bool operator ==(GameControllerButtonEvent left, GameControllerButtonEvent right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(GameControllerButtonEvent)"/>
	/// </summary>
	public static bool operator !=(GameControllerButtonEvent left, GameControllerButtonEvent right) => !left.Equals(right);
}