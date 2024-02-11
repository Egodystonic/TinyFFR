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

	bool IsConnected {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.IsConnected(Handle);
	}

	GameControllerStickPosition LeftStickPosition {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetStickPosition(Handle, leftStick: true);
	}
	GameControllerStickPosition RightStickPosition {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetStickPosition(Handle, leftStick: false);
	}
	GameControllerTriggerPosition LeftTriggerPosition {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetTriggerPosition(Handle, leftTrigger: true);
	}
	GameControllerTriggerPosition RightTriggerPosition {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetTriggerPosition(Handle, leftTrigger: false);
	}

	ReadOnlySpan<GameControllerButtonEvent> NewButtonEvents {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _impl.GetNewButtonEvents(Handle);
	}
	ReadOnlySpan<GameControllerButton> CurrentlyPressedButtons {
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
	bool IsButtonDown(GameControllerButton button) => _impl.IsButtonDown(Handle, button);

	public bool Equals(GameController other) => Handle == other.Handle;
	public override bool Equals(object? obj) => obj is GameController other && Equals(other);
	public override int GetHashCode() => Handle.GetHashCode();
	public static bool operator ==(GameController left, GameController right) => left.Equals(right);
	public static bool operator !=(GameController left, GameController right) => !left.Equals(right);

	public override string ToString() => $"{nameof(GameController)} \"{Name}\"";
}