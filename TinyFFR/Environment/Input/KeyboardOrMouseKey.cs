// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Input;

/* Made to map directly to SDL's SDL_KeyCode for the keyboard keys.
 *
 * For the sake of keeping things simpler for the user and easier to document I don't export every simple key (for example
 * the android buttons, hardware control such as brightness, some extended numpad keys that no modern kb uses to my knowledge, etc.).
 * However I've kept the copy/paste/rename work of those omitted keys in this file commented out in case we want to add them in at a later date
 * or in case someone else wants to build TinyFFR from source and include them.
 *
 * At the end of this enum is some mouse buttons defined.
 */
public enum KeyboardOrMouseKey : int {
	Unknown = 0,

	Return = '\r',
	Escape = '\x1B',
	Backspace = '\b',
	Tab = '\t',
	Space = ' ',
	ExclamationMark = '!',
	DoubleQuote = '"',
	Hash = '#',
	Percent = '%',
	Dollar = '$',
	Ampersand = '&',
	SingleQuote = '\'',
	OpeningParenthesis = '(',
	ClosingParenthesis = ')',
	Asterisk = '*',
	Plus = '+',
	Comma = ',',
	Minus = '-',
	Period = '.',
	ForwardSlash = '/',
	NumberRow0 = '0',
	NumberRow1 = '1',
	NumberRow2 = '2',
	NumberRow3 = '3',
	NumberRow4 = '4',
	NumberRow5 = '5',
	NumberRow6 = '6',
	NumberRow7 = '7',
	NumberRow8 = '8',
	NumberRow9 = '9',
	Colon = ':',
	Semicolon = ';',
	LessThan = '<',
	Equals = '=',
	GreaterThan = '>',
	QuestionMark = '?',
	AtSymbol = '@',
	LeftSquareBracket = '[',
	BackSlash = '\\',
	RightSquareBracket = ']',
	Caret = '^',
	Underscore = '_',
	Backtick = '`',
	A = 'a',
	B = 'b',
	C = 'c',
	D = 'd',
	E = 'e',
	F = 'f',
	G = 'g',
	H = 'h',
	I = 'i',
	J = 'j',
	K = 'k',
	L = 'l',
	M = 'm',
	N = 'n',
	O = 'o',
	P = 'p',
	Q = 'q',
	R = 'r',
	S = 's',
	T = 't',
	U = 'u',
	V = 'v',
	W = 'w',
	X = 'x',
	Y = 'y',
	Z = 'z',

	CapsLock = 57 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,

	F1 = 58 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F2 = 59 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F3 = 60 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F4 = 61 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F5 = 62 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F6 = 63 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F7 = 64 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F8 = 65 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F9 = 66 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F10 = 67 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F11 = 68 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F12 = 69 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,

	PrintScreen = 70 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	ScrollLock = 71 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	Pause = 72 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	Insert = 73 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	Home = 74 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	PageUp = 75 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	Delete = '\x7F',
	End = 77 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	PageDown = 78 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	ArrowRight = 79 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	ArrowLeft = 80 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	ArrowDown = 81 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	ArrowUp = 82 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,

	NumLock = 83 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	
	NumpadDivide = 84 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	NumpadMultiply = 85 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	NumpadMinus = 86 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	NumpadPlus = 87 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	NumpadEnter = 88 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	Numpad1 = 89 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	Numpad2 = 90 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	Numpad3 = 91 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	Numpad4 = 92 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	Numpad5 = 93 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	Numpad6 = 94 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	Numpad7 = 95 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	Numpad8 = 96 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	Numpad9 = 97 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	Numpad0 = 98 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	NumpadPeriod = 99 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,

	WindowsContextMenu = 101 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	
	// DedicatedPowerButton = 102 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	
	NumpadEquals = 103 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F13 = 104 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F14 = 105 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F15 = 106 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F16 = 107 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F17 = 108 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F18 = 109 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F19 = 110 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F20 = 111 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F21 = 112 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F22 = 113 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F23 = 114 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	F24 = 115 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	
	// DedicatedExecuteButton = 116 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedHelpButton = 117 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedMenuButton = 118 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedSelectButton = 119 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedStopButton = 120 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedAgainButton = 121 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedUndoButton = 122 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedCutButton = 123 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedCopyButton = 124 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedPasteButton = 125 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedFindButton = 126 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// Mute = 127 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// VolumeUp = 128 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// VolumeDown = 129 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	
	NumpadComma = 133 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	
	// LegacyEquals = 134 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,

	// SysAltErase = 153 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// SysSysReq = 154 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// SysCancel = 155 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// SysClear = 156 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// SysPrior = 157 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// SysReturn2 = 158 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// SysSeparator = 159 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// SysOut = 160 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// SysOper = 161 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// SysClearAgain = 162 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// SysCrSel = 163 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// SysExSel = 164 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,

	NumpadDoubleZero = 176 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	NumpadTripleZero = 177 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	NumpadThousandsSeparator = 178 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	NumpadDecimalsSeparator = 179 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	NumpadCurrencyUnit = 180 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	NumpadCurrencySubUnit = 181 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	NumpadOpeningParenthesis = 182 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	NumpadClosingParenthesis = 183 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	NumpadOpeningBrace = 184 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	NumpadClosingBrace = 185 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	NumpadTab = 186 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	NumpadBackspace = 187 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	
	// ProgrammerHexA = 188 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerHexB = 189 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerHexC = 190 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerHexD = 191 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerHexE = 192 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerHexF = 193 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerXor = 194 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerPower = 195 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerPercent = 196 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerLessThan = 197 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerGreaterThan = 198 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerAmpersand = 199 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerDoubleAmpersand = 200 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerVerticalBar = 201 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerDoubleVerticalBar = 202 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerColon = 203 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerHash = 204 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerSpace = 205 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerAt = 206 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerExclaim = 207 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerMemStore = 208 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerMemRecall = 209 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerMemClear = 210 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerMemAdd = 211 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerMemSubtract = 212 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerMemMultiply = 213 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerMemDivide = 214 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerPlusMinus = 215 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerClear = 216 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerClearEntry = 217 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerBinary = 218 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerOctal = 219 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerDecimal = 220 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// ProgrammerHexadecimal = 221 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,

	LeftControl = 224 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	LeftShift = 225 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	LeftAlt = 226 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	LeftWinKey = 227 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	RightControl = 228 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	RightShift = 229 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	RightAlt = 230 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	RightWinKey = 231 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,

	// Mode = 257 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,

	// MediaNext = 258 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// MediaPrevious = 259 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// MediaStop = 260 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// MediaPlay = 261 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// MediaMute = 262 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// MediaSelect = 263 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedWebButton = 264 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedMailButton = 265 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedCalculatorButton = 266 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedMyComputerButton = 267 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedSearchButton = 268 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedHomeButton = 269 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedBackButton = 270 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedForwardButton = 271 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedStopLoadingButton = 272 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedRefreshButton = 273 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedBookmarksButton = 274 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,

	// DisplayBrightnessDown = 275 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DisplayBrightnessUp = 276 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DisplaySwitch = 277 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// KeyboardBacklightToggle = 278 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// KeyboardBacklightDimmer = 279 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// KeyboardBacklightBrighter = 280 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedEjectButton = 281 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedSleepButton = 282 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedApp1Button = 283 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// DedicatedApp2Button = 284 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,

	// MediaRewind = 285 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// MediaFastForward = 286 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,

	// AndroidLeft = 287 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// AndroidRight = 288 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// AndroidStartCall = 289 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	// AndroidEndCall = 290 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,

	// ========= This is the end of SDL's SDL_Keycode; everything below this line is just TinyFFR =========

	MouseLeft = KeyboardOrMouseKeyExtensions.NonSdlKeyStartValue,
	MouseMiddle = KeyboardOrMouseKeyExtensions.NonSdlKeyStartValue + 1,
	MouseRight = KeyboardOrMouseKeyExtensions.NonSdlKeyStartValue + 2,
	Mouse4 = KeyboardOrMouseKeyExtensions.NonSdlKeyStartValue + 3,
	Mouse5 = KeyboardOrMouseKeyExtensions.NonSdlKeyStartValue + 4,
	MouseWheelUp = KeyboardOrMouseKeyExtensions.NonSdlKeyStartValue + 5,
	MouseWheelDown = KeyboardOrMouseKeyExtensions.NonSdlKeyStartValue + 6,
}

public static class KeyboardOrMouseKeyExtensions {
	internal const int NonSdlKeyStartValue = 380; // Note: This is linked with a constant in native_impl_loop::iterate_events
	internal const int SdlScancodeToKeycodeBit = 1 << 30;
	internal const int RecommendedEnumValueArraySize = 400;
	static readonly KeyboardOrMouseKeyCategory[] _precomputedCategoryArray = new KeyboardOrMouseKeyCategory[RecommendedEnumValueArraySize];

	static KeyboardOrMouseKeyExtensions() {
		foreach (var key in Enum.GetValues<KeyboardOrMouseKey>()) {
			var maskedVal = ((int) key) & ~SdlScancodeToKeycodeBit;
#pragma warning disable CA1065 //Don't raise exceptions in static constructors -> Usually a good rule, but I'm using this exception as essentially a static assert
			if (maskedVal < 0 || maskedVal >= _precomputedCategoryArray.Length) throw new InvalidOperationException("Precomputed key category array needs to be larger (or negative value found).");
#pragma warning restore CA1065

			_precomputedCategoryArray[maskedVal] = key switch {
				>= KeyboardOrMouseKey.MouseLeft and <= KeyboardOrMouseKey.MouseWheelDown => KeyboardOrMouseKeyCategory.Mouse,
				>= KeyboardOrMouseKey.A and <= KeyboardOrMouseKey.Z => KeyboardOrMouseKeyCategory.Alphabetic,
				>= KeyboardOrMouseKey.NumberRow0 and <= KeyboardOrMouseKey.NumberRow9 => KeyboardOrMouseKeyCategory.NumberRow,
				>= KeyboardOrMouseKey.Space and <= KeyboardOrMouseKey.Backtick => KeyboardOrMouseKeyCategory.PunctuationAndSymbology,
				>= KeyboardOrMouseKey.LeftControl and <= KeyboardOrMouseKey.RightWinKey => KeyboardOrMouseKeyCategory.Modifier,
				>= KeyboardOrMouseKey.ArrowRight and <= KeyboardOrMouseKey.ArrowUp => KeyboardOrMouseKeyCategory.Arrow,
				(>= KeyboardOrMouseKey.Insert and <= KeyboardOrMouseKey.PageDown) or KeyboardOrMouseKey.Delete => KeyboardOrMouseKeyCategory.EditingAndNavigation,
				(>= KeyboardOrMouseKey.F1 and <= KeyboardOrMouseKey.F12) or (>= KeyboardOrMouseKey.F13 and <= KeyboardOrMouseKey.F24) => KeyboardOrMouseKeyCategory.Function,
				(>= KeyboardOrMouseKey.NumpadDivide and <= KeyboardOrMouseKey.NumpadPeriod) or KeyboardOrMouseKey.NumpadEquals or KeyboardOrMouseKey.NumpadComma or (>= KeyboardOrMouseKey.NumpadDoubleZero and <= KeyboardOrMouseKey.NumpadBackspace) => KeyboardOrMouseKeyCategory.Numpad,
				_ => KeyboardOrMouseKeyCategory.Control
			};
		}
	}

	public static KeyboardOrMouseKeyCategory GetCategory(this KeyboardOrMouseKey @this) {
		try {
			return _precomputedCategoryArray[((int) @this) & ~SdlScancodeToKeycodeBit];
		}
		catch (IndexOutOfRangeException e) {
			throw new ArgumentOutOfRangeException($"Given {nameof(KeyboardOrMouseKey)} value ({@this}) is likely not defined, " +
												  $"resulting in an {nameof(IndexOutOfRangeException)} when looking up its precomputed {nameof(KeyboardOrMouseKeyCategory)}.", e);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static KeyboardOrMouseKey ToKeyboardOrMouseKey(this MouseKey @this) => (KeyboardOrMouseKey) @this;
}