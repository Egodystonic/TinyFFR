// Created on 2026-07-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Environment.Input;
using System.Runtime.InteropServices;

namespace Egodystonic.TinyFFR.WinForms {
	static partial class KeyboardOrMouseKeyMap {
		const int VkLeftShift = 0xA0;
		const int VkRightShift = 0xA1;
		const int VkLeftControl = 0xA2;
		const int VkRightControl = 0xA3;
		const int VkLeftAlt = 0xA4;
		const int VkRightAlt = 0xA5;
		const short KeyDownStateMask = unchecked((short) 0x8000);

		// Keys are translated to their unshifted value, matching the SDL keycode semantics of KeyboardOrMouseKey
		// (e.g. Shift+1 is reported as NumberRow1, not ExclamationMark). Punctuation is mapped according to a US layout.
		public static KeyboardOrMouseKey Translate(Keys key) {
			if (key is >= Keys.A and <= Keys.Z) return CharKey((char) ('a' + (key - Keys.A)));
			if (key is >= Keys.D0 and <= Keys.D9) return InputUtils.KeyFromNumericValue(key - Keys.D0) ?? KeyboardOrMouseKey.Unknown;
			if (key is >= Keys.NumPad0 and <= Keys.NumPad9) return InputUtils.KeyFromNumericValue(key - Keys.NumPad0, returnNumberRowValue: false) ?? KeyboardOrMouseKey.Unknown;
			if (key is >= Keys.F1 and <= Keys.F12) return KeyboardOrMouseKey.F1 + (key - Keys.F1);
			if (key is >= Keys.F13 and <= Keys.F24) return KeyboardOrMouseKey.F13 + (key - Keys.F13);

			return key switch {
				Keys.Escape => KeyboardOrMouseKey.Escape,
				Keys.Return => KeyboardOrMouseKey.Return,
				Keys.Space => KeyboardOrMouseKey.Space,
				Keys.Tab => KeyboardOrMouseKey.Tab,
				Keys.Back => KeyboardOrMouseKey.Backspace,
				Keys.Delete => KeyboardOrMouseKey.Delete,
				Keys.Insert => KeyboardOrMouseKey.Insert,
				Keys.Home => KeyboardOrMouseKey.Home,
				Keys.End => KeyboardOrMouseKey.End,
				Keys.PageUp => KeyboardOrMouseKey.PageUp,
				Keys.PageDown => KeyboardOrMouseKey.PageDown,
				Keys.Left => KeyboardOrMouseKey.ArrowLeft,
				Keys.Right => KeyboardOrMouseKey.ArrowRight,
				Keys.Up => KeyboardOrMouseKey.ArrowUp,
				Keys.Down => KeyboardOrMouseKey.ArrowDown,
				Keys.CapsLock => KeyboardOrMouseKey.CapsLock,
				Keys.NumLock => KeyboardOrMouseKey.NumLock,
				Keys.Scroll => KeyboardOrMouseKey.ScrollLock,
				Keys.PrintScreen => KeyboardOrMouseKey.PrintScreen,
				Keys.Pause => KeyboardOrMouseKey.Pause,
				Keys.Apps => KeyboardOrMouseKey.WindowsContextMenu,
				Keys.LWin => KeyboardOrMouseKey.LeftWinKey,
				Keys.RWin => KeyboardOrMouseKey.RightWinKey,
				Keys.Add => KeyboardOrMouseKey.NumpadPlus,
				Keys.Subtract => KeyboardOrMouseKey.NumpadMinus,
				Keys.Multiply => KeyboardOrMouseKey.NumpadMultiply,
				Keys.Divide => KeyboardOrMouseKey.NumpadDivide,
				Keys.Decimal => KeyboardOrMouseKey.NumpadPeriod,
				Keys.LShiftKey => KeyboardOrMouseKey.LeftShift,
				Keys.RShiftKey => KeyboardOrMouseKey.RightShift,
				Keys.LControlKey => KeyboardOrMouseKey.LeftControl,
				Keys.RControlKey => KeyboardOrMouseKey.RightControl,
				Keys.LMenu => KeyboardOrMouseKey.LeftAlt,
				Keys.RMenu => KeyboardOrMouseKey.RightAlt,
				// WinForms reports modifiers without indicating which side of the keyboard they came from, so we ask the OS
				Keys.ShiftKey => SidedModifier(VkRightShift, VkLeftShift, KeyboardOrMouseKey.RightShift, KeyboardOrMouseKey.LeftShift),
				Keys.ControlKey => SidedModifier(VkRightControl, VkLeftControl, KeyboardOrMouseKey.RightControl, KeyboardOrMouseKey.LeftControl),
				Keys.Menu => SidedModifier(VkRightAlt, VkLeftAlt, KeyboardOrMouseKey.RightAlt, KeyboardOrMouseKey.LeftAlt),
				Keys.Oemplus => CharKey('='),
				Keys.OemMinus => CharKey('-'),
				Keys.Oemcomma => CharKey(','),
				Keys.OemPeriod => CharKey('.'),
				Keys.OemQuestion => CharKey('/'),
				Keys.Oemtilde => CharKey('`'),
				Keys.OemOpenBrackets => CharKey('['),
				Keys.OemCloseBrackets => CharKey(']'),
				Keys.OemPipe or Keys.OemBackslash => CharKey('\\'),
				Keys.OemQuotes => CharKey('\''),
				Keys.OemSemicolon => CharKey(';'),
				_ => KeyboardOrMouseKey.Unknown
			};
		}

		// By the time a key release is reported the OS no longer considers either side of the modifier to be down, so we
		// can't tell which one was released. Callers pass both candidates to the retriever instead, which discards the
		// one it isn't currently holding.
		public static bool TryGetSidedModifierPair(Keys key, out KeyboardOrMouseKey left, out KeyboardOrMouseKey right) {
			switch (key) {
				case Keys.ShiftKey:
					(left, right) = (KeyboardOrMouseKey.LeftShift, KeyboardOrMouseKey.RightShift);
					return true;
				case Keys.ControlKey:
					(left, right) = (KeyboardOrMouseKey.LeftControl, KeyboardOrMouseKey.RightControl);
					return true;
				case Keys.Menu:
					(left, right) = (KeyboardOrMouseKey.LeftAlt, KeyboardOrMouseKey.RightAlt);
					return true;
				default:
					(left, right) = (KeyboardOrMouseKey.Unknown, KeyboardOrMouseKey.Unknown);
					return false;
			}
		}

		public static MouseKey Translate(MouseButtons button) {
			return button switch {
				MouseButtons.Left => MouseKey.MouseLeft,
				MouseButtons.Middle => MouseKey.MouseMiddle,
				MouseButtons.Right => MouseKey.MouseRight,
				MouseButtons.XButton1 => MouseKey.Mouse4,
				MouseButtons.XButton2 => MouseKey.Mouse5,
				_ => MouseKey.Unknown
			};
		}

		static KeyboardOrMouseKey SidedModifier(int preferredVirtualKey, int fallbackVirtualKey, KeyboardOrMouseKey preferredResult, KeyboardOrMouseKey fallbackResult) {
			return IsVirtualKeyDown(preferredVirtualKey) && !IsVirtualKeyDown(fallbackVirtualKey) ? preferredResult : fallbackResult;
		}

		static bool IsVirtualKeyDown(int virtualKey) => (GetKeyState(virtualKey) & KeyDownStateMask) != 0;

		static KeyboardOrMouseKey CharKey(char c) => InputUtils.KeyFromCharacterValue(c) ?? KeyboardOrMouseKey.Unknown;

		[LibraryImport("user32.dll")]
		private static partial short GetKeyState(int virtualKey);
	}
}
