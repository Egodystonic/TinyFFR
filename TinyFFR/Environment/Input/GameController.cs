// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Input;

public readonly struct GameController : IEquatable<GameController> {
	readonly IGameControllerHandleImplProvider _impl;
	internal GameControllerHandle Handle { get; }

	public string Name {
		get {
			var maxSpanLength = GetNameSpanMaxLength();
			var dest = maxSpanLength <= 1000 ? stackalloc char[maxSpanLength] : new char[maxSpanLength];

			var numCharsWritten = GetNameUsingSpan(dest);
			return new(dest[..numCharsWritten]);
		}
	}

	public GameControllerStickPosition LeftStickPosition {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetStickPosition(Handle, leftStick: true);
	}
	public GameControllerStickPosition RightStickPosition {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetStickPosition(Handle, leftStick: false);
	}
	public GameControllerTriggerPosition LeftTriggerPosition {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetTriggerPosition(Handle, leftTrigger: true);
	}
	public GameControllerTriggerPosition RightTriggerPosition {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetTriggerPosition(Handle, leftTrigger: false);
	}

	public ReadOnlySpan<GameControllerButtonEvent> NewButtonEvents {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetNewButtonEvents(Handle);
	}
	public ReadOnlySpan<GameControllerButton> NewButtonDownEvents {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetNewButtonDownEvents(Handle);
	}
	public ReadOnlySpan<GameControllerButton> NewButtonUpEvents {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetNewButtonUpEvents(Handle);
	}
	public ReadOnlySpan<GameControllerButton> CurrentlyPressedButtons {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetCurrentlyPressedButtons(Handle);
	}

	internal GameController(GameControllerHandle handle, IGameControllerHandleImplProvider impl) {
		Handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameUsingSpan(Span<char> dest) => _impl.GetName(Handle, dest);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameSpanMaxLength() => _impl.GetNameMaxLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool ButtonIsCurrentlyDown(GameControllerButton button) => _impl.IsButtonDown(Handle, button);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool ButtonWasPressedThisIteration(GameControllerButton button) => _impl.WasButtonPressed(Handle, button);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool ButtonWasReleasedThisIteration(GameControllerButton button) => _impl.WasButtonReleased(Handle, button);

	public bool Equals(GameController other) => Handle == other.Handle;
	public override bool Equals(object? obj) => obj is GameController other && Equals(other);
	public override int GetHashCode() => Handle.GetHashCode();
	public static bool operator ==(GameController left, GameController right) => left.Equals(right);
	public static bool operator !=(GameController left, GameController right) => !left.Equals(right);

	public override string ToString() => $"{nameof(GameController)} \"{Name}\"";
}