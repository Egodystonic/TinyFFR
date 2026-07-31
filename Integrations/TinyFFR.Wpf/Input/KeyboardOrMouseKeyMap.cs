// Created on 2026-07-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Windows.Input;
using Egodystonic.TinyFFR.Environment.Input;

namespace Egodystonic.TinyFFR.Wpf.Input {
	static class KeyboardOrMouseKeyMap {
		public static KeyboardOrMouseKey Translate(Key key) {
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

		public static MouseKey Translate(MouseButton button) {
			return button switch {
				MouseButton.Left => MouseKey.MouseLeft,
				MouseButton.Middle => MouseKey.MouseMiddle,
				MouseButton.Right => MouseKey.MouseRight,
				MouseButton.XButton1 => MouseKey.Mouse4,
				MouseButton.XButton2 => MouseKey.Mouse5,
				_ => MouseKey.Unknown
			};
		}

		static KeyboardOrMouseKey CharKey(char c) => InputUtils.KeyFromCharacterValue(c) ?? KeyboardOrMouseKey.Unknown;
	}
}
