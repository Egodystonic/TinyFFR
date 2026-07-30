// Created on 2026-07-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Avalonia.Input;
using Egodystonic.TinyFFR.Environment.Input;

namespace Egodystonic.TinyFFR.Avalonia;

static class KeyboardOrMouseKeyMap {
	// Keys are translated to their unshifted value, matching the SDL keycode semantics of KeyboardOrMouseKey
	// (e.g. Shift+1 is reported as NumberRow1, not ExclamationMark). Punctuation is mapped according to a US
	// layout; 'keySymbol' (where the host framework supplies one) is used as a fallback for other layouts.
	public static KeyboardOrMouseKey Translate(Key key, string? keySymbol = null) {
		var result = TranslateKey(key);
		if (result != KeyboardOrMouseKey.Unknown) return result;
		if (keySymbol is { Length: 1 }) return CharKey(Char.ToLowerInvariant(keySymbol[0]));
		return KeyboardOrMouseKey.Unknown;
	}

	static KeyboardOrMouseKey CharKey(char c) => InputUtils.KeyFromCharacterValue(c) ?? KeyboardOrMouseKey.Unknown;

	static KeyboardOrMouseKey TranslateKey(Key key) {
		if (key is >= Key.A and <= Key.Z) return CharKey((char) ('a' + (key - Key.A)));
		if (key is >= Key.D0 and <= Key.D9) return InputUtils.KeyFromNumericValue(key - Key.D0) ?? KeyboardOrMouseKey.Unknown;
		if (key is >= Key.NumPad0 and <= Key.NumPad9) return InputUtils.KeyFromNumericValue(key - Key.NumPad0, returnNumberRowValue: false) ?? KeyboardOrMouseKey.Unknown;
		if (key is >= Key.F1 and <= Key.F12) return KeyboardOrMouseKey.F1 + (key - Key.F1);
		if (key is >= Key.F13 and <= Key.F24) return KeyboardOrMouseKey.F13 + (key - Key.F13);

		return key switch {
			Key.Escape => KeyboardOrMouseKey.Escape,
			Key.Return => KeyboardOrMouseKey.Return,
			Key.Space => KeyboardOrMouseKey.Space,
			Key.Tab => KeyboardOrMouseKey.Tab,
			Key.Back => KeyboardOrMouseKey.Backspace,
			Key.Delete => KeyboardOrMouseKey.Delete,
			Key.Insert => KeyboardOrMouseKey.Insert,
			Key.Home => KeyboardOrMouseKey.Home,
			Key.End => KeyboardOrMouseKey.End,
			Key.PageUp => KeyboardOrMouseKey.PageUp,
			Key.PageDown => KeyboardOrMouseKey.PageDown,
			Key.Left => KeyboardOrMouseKey.ArrowLeft,
			Key.Right => KeyboardOrMouseKey.ArrowRight,
			Key.Up => KeyboardOrMouseKey.ArrowUp,
			Key.Down => KeyboardOrMouseKey.ArrowDown,
			Key.CapsLock => KeyboardOrMouseKey.CapsLock,
			Key.NumLock => KeyboardOrMouseKey.NumLock,
			Key.Scroll => KeyboardOrMouseKey.ScrollLock,
			Key.PrintScreen => KeyboardOrMouseKey.PrintScreen,
			Key.Pause => KeyboardOrMouseKey.Pause,
			Key.Apps => KeyboardOrMouseKey.WindowsContextMenu,
			Key.LeftCtrl => KeyboardOrMouseKey.LeftControl,
			Key.RightCtrl => KeyboardOrMouseKey.RightControl,
			Key.LeftShift => KeyboardOrMouseKey.LeftShift,
			Key.RightShift => KeyboardOrMouseKey.RightShift,
			Key.LeftAlt => KeyboardOrMouseKey.LeftAlt,
			Key.RightAlt => KeyboardOrMouseKey.RightAlt,
			Key.LWin => KeyboardOrMouseKey.LeftWinKey,
			Key.RWin => KeyboardOrMouseKey.RightWinKey,
			Key.Add => KeyboardOrMouseKey.NumpadPlus,
			Key.Subtract => KeyboardOrMouseKey.NumpadMinus,
			Key.Multiply => KeyboardOrMouseKey.NumpadMultiply,
			Key.Divide => KeyboardOrMouseKey.NumpadDivide,
			Key.Decimal => KeyboardOrMouseKey.NumpadPeriod,
			Key.OemPlus => CharKey('='),
			Key.OemMinus => CharKey('-'),
			Key.OemComma => CharKey(','),
			Key.OemPeriod => CharKey('.'),
			Key.OemQuestion => CharKey('/'),
			Key.OemTilde => CharKey('`'),
			Key.OemOpenBrackets => CharKey('['),
			Key.OemCloseBrackets => CharKey(']'),
			Key.OemPipe or Key.OemBackslash => CharKey('\\'),
			Key.OemQuotes => CharKey('\''),
			Key.OemSemicolon => CharKey(';'),
			_ => KeyboardOrMouseKey.Unknown
		};
	}

	public static MouseKey Translate(PointerUpdateKind kind) {
		return kind switch {
			PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.LeftButtonReleased => MouseKey.MouseLeft,
			PointerUpdateKind.MiddleButtonPressed or PointerUpdateKind.MiddleButtonReleased => MouseKey.MouseMiddle,
			PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased => MouseKey.MouseRight,
			PointerUpdateKind.XButton1Pressed or PointerUpdateKind.XButton1Released => MouseKey.Mouse4,
			PointerUpdateKind.XButton2Pressed or PointerUpdateKind.XButton2Released => MouseKey.Mouse5,
			_ => MouseKey.Unknown
		};
	}
}
