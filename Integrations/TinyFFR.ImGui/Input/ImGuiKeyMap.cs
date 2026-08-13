// Created on 2026-08-11 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Hexa.NET.ImGui;

namespace Egodystonic.TinyFFR.DearImGui.Input;

static class ImGuiKeyMap {
	static readonly GameControllerButton[] _mappedGamepadButtons = {
		GameControllerButton.A,
		GameControllerButton.B,
		GameControllerButton.X,
		GameControllerButton.Y,
		GameControllerButton.SelectOrView,
		GameControllerButton.StartOrMenu,
		GameControllerButton.LeftBumper,
		GameControllerButton.RightBumper,
		GameControllerButton.LeftStick,
		GameControllerButton.RightStick,
		GameControllerButton.DirectionalPadUp,
		GameControllerButton.DirectionalPadDown,
		GameControllerButton.DirectionalPadLeft,
		GameControllerButton.DirectionalPadRight
	};

	public static ReadOnlySpan<GameControllerButton> MappedGamepadButtons => _mappedGamepadButtons;

	public static ImGuiKey TranslateGamepadButton(GameControllerButton button) => button switch {
		GameControllerButton.A => ImGuiKey.GamepadFaceDown,
		GameControllerButton.B => ImGuiKey.GamepadFaceRight,
		GameControllerButton.X => ImGuiKey.GamepadFaceLeft,
		GameControllerButton.Y => ImGuiKey.GamepadFaceUp,
		GameControllerButton.SelectOrView => ImGuiKey.GamepadBack,
		GameControllerButton.StartOrMenu => ImGuiKey.GamepadStart,
		GameControllerButton.LeftBumper => ImGuiKey.GamepadL1,
		GameControllerButton.RightBumper => ImGuiKey.GamepadR1,
		GameControllerButton.LeftStick => ImGuiKey.GamepadL3,
		GameControllerButton.RightStick => ImGuiKey.GamepadR3,
		GameControllerButton.DirectionalPadUp => ImGuiKey.GamepadDpadUp,
		GameControllerButton.DirectionalPadDown => ImGuiKey.GamepadDpadDown,
		GameControllerButton.DirectionalPadLeft => ImGuiKey.GamepadDpadLeft,
		GameControllerButton.DirectionalPadRight => ImGuiKey.GamepadDpadRight,
		_ => ImGuiKey.None
	};

	public static void DecomposeStick(GameControllerStickPosition stick, float deadzone, out float left, out float right, out float up, out float down) {
		var horizontal = stick.GetDisplacementHorizontalWithDeadzone(deadzone);
		var vertical = stick.GetDisplacementVerticalWithDeadzone(deadzone);
		left = horizontal < 0f ? -horizontal : 0f;
		right = horizontal > 0f ? horizontal : 0f;
		up = vertical > 0f ? vertical : 0f;
		down = vertical < 0f ? -vertical : 0f;
	}

	public static int TranslateMouseButton(KeyboardOrMouseKey key) => key switch {
		KeyboardOrMouseKey.MouseLeft => 0,
		KeyboardOrMouseKey.MouseRight => 1,
		KeyboardOrMouseKey.MouseMiddle => 2,
		KeyboardOrMouseKey.Mouse4 => 3,
		KeyboardOrMouseKey.Mouse5 => 4,
		_ => -1
	};

	public static MouseCursorStyle TranslateCursor(ImGuiMouseCursor cursor) => cursor switch {
		ImGuiMouseCursor.TextInput => MouseCursorStyle.TextInput,
		ImGuiMouseCursor.ResizeAll => MouseCursorStyle.ResizeAll,
		ImGuiMouseCursor.ResizeNs => MouseCursorStyle.ResizeVertical,
		ImGuiMouseCursor.ResizeEw => MouseCursorStyle.ResizeHorizontal,
		ImGuiMouseCursor.ResizeNesw => MouseCursorStyle.ResizeTopRightBottomLeft,
		ImGuiMouseCursor.ResizeNwse => MouseCursorStyle.ResizeTopLeftBottomRight,
		ImGuiMouseCursor.Hand => MouseCursorStyle.Hand,
		ImGuiMouseCursor.NotAllowed => MouseCursorStyle.NotAllowed,
		ImGuiMouseCursor.None => MouseCursorStyle.Invisible,
		_ => MouseCursorStyle.Arrow
	};

	public static ImGuiKey Translate(KeyboardOrMouseKey key) {
		var numericValue = key.GetNumericValue();
		if (numericValue is >= 0 and <= 9 && key.GetCategory() == KeyboardOrMouseKeyCategory.NumberRow) {
			return ImGuiKey.Key0 + numericValue.Value;
		}

		var charValue = key.GetCharacterValue();
		if (charValue is >= 'a' and <= 'z') return ImGuiKey.A + (charValue.Value - 'a');

		return key switch {
			KeyboardOrMouseKey.Tab => ImGuiKey.Tab,
			KeyboardOrMouseKey.ArrowLeft => ImGuiKey.LeftArrow,
			KeyboardOrMouseKey.ArrowRight => ImGuiKey.RightArrow,
			KeyboardOrMouseKey.ArrowUp => ImGuiKey.UpArrow,
			KeyboardOrMouseKey.ArrowDown => ImGuiKey.DownArrow,
			KeyboardOrMouseKey.PageUp => ImGuiKey.PageUp,
			KeyboardOrMouseKey.PageDown => ImGuiKey.PageDown,
			KeyboardOrMouseKey.Home => ImGuiKey.Home,
			KeyboardOrMouseKey.End => ImGuiKey.End,
			KeyboardOrMouseKey.Insert => ImGuiKey.Insert,
			KeyboardOrMouseKey.Delete => ImGuiKey.Delete,
			KeyboardOrMouseKey.Backspace => ImGuiKey.Backspace,
			KeyboardOrMouseKey.Space => ImGuiKey.Space,
			KeyboardOrMouseKey.Return => ImGuiKey.Enter,
			KeyboardOrMouseKey.Escape => ImGuiKey.Escape,
			KeyboardOrMouseKey.LeftControl => ImGuiKey.LeftCtrl,
			KeyboardOrMouseKey.LeftShift => ImGuiKey.LeftShift,
			KeyboardOrMouseKey.LeftAlt => ImGuiKey.LeftAlt,
			KeyboardOrMouseKey.LeftWinKey => ImGuiKey.LeftSuper,
			KeyboardOrMouseKey.RightControl => ImGuiKey.RightCtrl,
			KeyboardOrMouseKey.RightShift => ImGuiKey.RightShift,
			KeyboardOrMouseKey.RightAlt => ImGuiKey.RightAlt,
			KeyboardOrMouseKey.RightWinKey => ImGuiKey.RightSuper,
			KeyboardOrMouseKey.CapsLock => ImGuiKey.CapsLock,
			KeyboardOrMouseKey.ScrollLock => ImGuiKey.ScrollLock,
			KeyboardOrMouseKey.NumLock => ImGuiKey.NumLock,
			KeyboardOrMouseKey.PrintScreen => ImGuiKey.PrintScreen,
			KeyboardOrMouseKey.Pause => ImGuiKey.Pause,
			KeyboardOrMouseKey.Comma => ImGuiKey.Comma,
			KeyboardOrMouseKey.Period => ImGuiKey.Period,
			KeyboardOrMouseKey.ForwardSlash => ImGuiKey.Slash,
			KeyboardOrMouseKey.BackSlash => ImGuiKey.Backslash,
			KeyboardOrMouseKey.Semicolon => ImGuiKey.Semicolon,
			KeyboardOrMouseKey.SingleQuote => ImGuiKey.Apostrophe,
			KeyboardOrMouseKey.LeftSquareBracket => ImGuiKey.LeftBracket,
			KeyboardOrMouseKey.RightSquareBracket => ImGuiKey.RightBracket,
			KeyboardOrMouseKey.Minus => ImGuiKey.Minus,
			KeyboardOrMouseKey.Equals => ImGuiKey.Equal,
			KeyboardOrMouseKey.Backtick => ImGuiKey.GraveAccent,
			KeyboardOrMouseKey.F1 => ImGuiKey.F1,
			KeyboardOrMouseKey.F2 => ImGuiKey.F2,
			KeyboardOrMouseKey.F3 => ImGuiKey.F3,
			KeyboardOrMouseKey.F4 => ImGuiKey.F4,
			KeyboardOrMouseKey.F5 => ImGuiKey.F5,
			KeyboardOrMouseKey.F6 => ImGuiKey.F6,
			KeyboardOrMouseKey.F7 => ImGuiKey.F7,
			KeyboardOrMouseKey.F8 => ImGuiKey.F8,
			KeyboardOrMouseKey.F9 => ImGuiKey.F9,
			KeyboardOrMouseKey.F10 => ImGuiKey.F10,
			KeyboardOrMouseKey.F11 => ImGuiKey.F11,
			KeyboardOrMouseKey.F12 => ImGuiKey.F12,
			_ => ImGuiKey.None
		};
	}
}
