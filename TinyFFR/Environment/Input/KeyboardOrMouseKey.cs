// Created on 2024-01-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using static Egodystonic.TinyFFR.Environment.Input.KeyboardOrMouseKeyExtensions;

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

	Return = '\r' | CharBasedValueBit,
	Escape = '\x1B' | CharBasedValueBit,
	Backspace = '\b' | CharBasedValueBit,
	Tab = '\t' | CharBasedValueBit,
	Space = ' ' | CharBasedValueBit,
	ExclamationMark = '!' | CharBasedValueBit,
	DoubleQuote = '"' | CharBasedValueBit,
	Hash = '#' | CharBasedValueBit,
	Percent = '%' | CharBasedValueBit,
	Dollar = '$' | CharBasedValueBit,
	Ampersand = '&' | CharBasedValueBit,
	SingleQuote = '\'' | CharBasedValueBit,
	OpeningParenthesis = '(' | CharBasedValueBit,
	ClosingParenthesis = ')' | CharBasedValueBit,
	Asterisk = '*' | CharBasedValueBit,
	Plus = '+' | CharBasedValueBit,
	Comma = ',' | CharBasedValueBit,
	Minus = '-' | CharBasedValueBit,
	Period = '.' | CharBasedValueBit,
	ForwardSlash = '/' | CharBasedValueBit,
	NumberRow0 = '0' | CharBasedValueBit,
	NumberRow1 = '1' | CharBasedValueBit,
	NumberRow2 = '2' | CharBasedValueBit,
	NumberRow3 = '3' | CharBasedValueBit,
	NumberRow4 = '4' | CharBasedValueBit,
	NumberRow5 = '5' | CharBasedValueBit,
	NumberRow6 = '6' | CharBasedValueBit,
	NumberRow7 = '7' | CharBasedValueBit,
	NumberRow8 = '8' | CharBasedValueBit,
	NumberRow9 = '9' | CharBasedValueBit,
	Colon = ':' | CharBasedValueBit,
	Semicolon = ';' | CharBasedValueBit,
	LessThan = '<' | CharBasedValueBit,
	Equals = '=' | CharBasedValueBit,
	GreaterThan = '>' | CharBasedValueBit,
	QuestionMark = '?' | CharBasedValueBit,
	AtSymbol = '@' | CharBasedValueBit,
	LeftSquareBracket = '[' | CharBasedValueBit,
	BackSlash = '\\' | CharBasedValueBit,
	RightSquareBracket = ']' | CharBasedValueBit,
	Caret = '^' | CharBasedValueBit,
	Underscore = '_' | CharBasedValueBit,
	Backtick = '`' | CharBasedValueBit,
	A = 'a' | CharBasedValueBit,
	B = 'b' | CharBasedValueBit,
	C = 'c' | CharBasedValueBit,
	D = 'd' | CharBasedValueBit,
	E = 'e' | CharBasedValueBit,
	F = 'f' | CharBasedValueBit,
	G = 'g' | CharBasedValueBit,
	H = 'h' | CharBasedValueBit,
	I = 'i' | CharBasedValueBit,
	J = 'j' | CharBasedValueBit,
	K = 'k' | CharBasedValueBit,
	L = 'l' | CharBasedValueBit,
	M = 'm' | CharBasedValueBit,
	N = 'n' | CharBasedValueBit,
	O = 'o' | CharBasedValueBit,
	P = 'p' | CharBasedValueBit,
	Q = 'q' | CharBasedValueBit,
	R = 'r' | CharBasedValueBit,
	S = 's' | CharBasedValueBit,
	T = 't' | CharBasedValueBit,
	U = 'u' | CharBasedValueBit,
	V = 'v' | CharBasedValueBit,
	W = 'w' | CharBasedValueBit,
	X = 'x' | CharBasedValueBit,
	Y = 'y' | CharBasedValueBit,
	Z = 'z' | CharBasedValueBit,

	CapsLock = 57 | SdlScancodeToKeycodeBit,

	F1 = 58 | SdlScancodeToKeycodeBit,
	F2 = 59 | SdlScancodeToKeycodeBit,
	F3 = 60 | SdlScancodeToKeycodeBit,
	F4 = 61 | SdlScancodeToKeycodeBit,
	F5 = 62 | SdlScancodeToKeycodeBit,
	F6 = 63 | SdlScancodeToKeycodeBit,
	F7 = 64 | SdlScancodeToKeycodeBit,
	F8 = 65 | SdlScancodeToKeycodeBit,
	F9 = 66 | SdlScancodeToKeycodeBit,
	F10 = 67 | SdlScancodeToKeycodeBit,
	F11 = 68 | SdlScancodeToKeycodeBit,
	F12 = 69 | SdlScancodeToKeycodeBit,

	PrintScreen = 70 | SdlScancodeToKeycodeBit,
	ScrollLock = 71 | SdlScancodeToKeycodeBit,
	Pause = 72 | SdlScancodeToKeycodeBit,
	Insert = 73 | SdlScancodeToKeycodeBit,
	Home = 74 | SdlScancodeToKeycodeBit,
	PageUp = 75 | SdlScancodeToKeycodeBit,
	Delete = '\x7F' | CharBasedValueBit,
	End = 77 | SdlScancodeToKeycodeBit,
	PageDown = 78 | SdlScancodeToKeycodeBit,
	ArrowRight = 79 | SdlScancodeToKeycodeBit,
	ArrowLeft = 80 | SdlScancodeToKeycodeBit,
	ArrowDown = 81 | SdlScancodeToKeycodeBit,
	ArrowUp = 82 | SdlScancodeToKeycodeBit,

	NumLock = 83 | SdlScancodeToKeycodeBit,
	
	NumpadDivide = 84 | SdlScancodeToKeycodeBit,
	NumpadMultiply = 85 | SdlScancodeToKeycodeBit,
	NumpadMinus = 86 | SdlScancodeToKeycodeBit,
	NumpadPlus = 87 | SdlScancodeToKeycodeBit,
	NumpadEnter = 88 | SdlScancodeToKeycodeBit,
	Numpad1 = 89 | SdlScancodeToKeycodeBit,
	Numpad2 = 90 | SdlScancodeToKeycodeBit,
	Numpad3 = 91 | SdlScancodeToKeycodeBit,
	Numpad4 = 92 | SdlScancodeToKeycodeBit,
	Numpad5 = 93 | SdlScancodeToKeycodeBit,
	Numpad6 = 94 | SdlScancodeToKeycodeBit,
	Numpad7 = 95 | SdlScancodeToKeycodeBit,
	Numpad8 = 96 | SdlScancodeToKeycodeBit,
	Numpad9 = 97 | SdlScancodeToKeycodeBit,
	Numpad0 = 98 | SdlScancodeToKeycodeBit,
	NumpadPeriod = 99 | SdlScancodeToKeycodeBit,

	WindowsContextMenu = 101 | SdlScancodeToKeycodeBit,
	
	// DedicatedPowerButton = 102 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	
	NumpadEquals = 103 | SdlScancodeToKeycodeBit,
	F13 = 104 | SdlScancodeToKeycodeBit,
	F14 = 105 | SdlScancodeToKeycodeBit,
	F15 = 106 | SdlScancodeToKeycodeBit,
	F16 = 107 | SdlScancodeToKeycodeBit,
	F17 = 108 | SdlScancodeToKeycodeBit,
	F18 = 109 | SdlScancodeToKeycodeBit,
	F19 = 110 | SdlScancodeToKeycodeBit,
	F20 = 111 | SdlScancodeToKeycodeBit,
	F21 = 112 | SdlScancodeToKeycodeBit,
	F22 = 113 | SdlScancodeToKeycodeBit,
	F23 = 114 | SdlScancodeToKeycodeBit,
	F24 = 115 | SdlScancodeToKeycodeBit,
	
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
	
	NumpadComma = 133 | SdlScancodeToKeycodeBit,
	
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

	NumpadDoubleZero = 176 | SdlScancodeToKeycodeBit,
	NumpadTripleZero = 177 | SdlScancodeToKeycodeBit,
	NumpadThousandsSeparator = 178 | SdlScancodeToKeycodeBit,
	NumpadDecimalsSeparator = 179 | SdlScancodeToKeycodeBit,
	NumpadCurrencyUnit = 180 | SdlScancodeToKeycodeBit,
	NumpadCurrencySubUnit = 181 | SdlScancodeToKeycodeBit,
	NumpadOpeningParenthesis = 182 | SdlScancodeToKeycodeBit,
	NumpadClosingParenthesis = 183 | SdlScancodeToKeycodeBit,
	NumpadOpeningBrace = 184 | SdlScancodeToKeycodeBit,
	NumpadClosingBrace = 185 | SdlScancodeToKeycodeBit,
	NumpadTab = 186 | SdlScancodeToKeycodeBit,
	NumpadBackspace = 187 | SdlScancodeToKeycodeBit,
	
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

	LeftControl = 224 | SdlScancodeToKeycodeBit,
	LeftShift = 225 | SdlScancodeToKeycodeBit,
	LeftAlt = 226 | SdlScancodeToKeycodeBit,
	LeftWinKey = 227 | SdlScancodeToKeycodeBit,
	RightControl = 228 | SdlScancodeToKeycodeBit,
	RightShift = 229 | SdlScancodeToKeycodeBit,
	RightAlt = 230 | SdlScancodeToKeycodeBit,
	RightWinKey = 231 | SdlScancodeToKeycodeBit,

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

	MouseLeft = NonSdlKeyStartValue,
	MouseMiddle = NonSdlKeyStartValue + 1,
	MouseRight = NonSdlKeyStartValue + 2,
	Mouse4 = NonSdlKeyStartValue + 3,
	Mouse5 = NonSdlKeyStartValue + 4,
	MouseWheelUp = NonSdlKeyStartValue + 5,
	MouseWheelDown = NonSdlKeyStartValue + 6,
}

public static class KeyboardOrMouseKeyExtensions {
	internal const int SdlScancodeToKeycodeBit = 1 << 30;
	internal const int CharBasedValueBitDistanceToScancodeBit = 21;
	internal const int CharBasedValueBit = SdlScancodeToKeycodeBit >> CharBasedValueBitDistanceToScancodeBit;
	internal const int NonSdlKeyStartValue = 380; // Note: This is linked with a constant in native_impl_loop::iterate_events
	internal const int RecommendedEnumValueArraySize = CharBasedValueBit * 2;
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
				>= KeyboardOrMouseKey.Space and <= KeyboardOrMouseKey.Backtick => KeyboardOrMouseKeyCategory.PunctuationAndSymbols,
				>= KeyboardOrMouseKey.LeftControl and <= KeyboardOrMouseKey.RightWinKey => KeyboardOrMouseKeyCategory.Modifier,
				>= KeyboardOrMouseKey.ArrowRight and <= KeyboardOrMouseKey.ArrowUp => KeyboardOrMouseKeyCategory.Arrow,
				(>= KeyboardOrMouseKey.Insert and <= KeyboardOrMouseKey.PageDown) or KeyboardOrMouseKey.Delete => KeyboardOrMouseKeyCategory.EditingAndNavigation,
				(>= KeyboardOrMouseKey.F1 and <= KeyboardOrMouseKey.F12) or (>= KeyboardOrMouseKey.F13 and <= KeyboardOrMouseKey.F24) => KeyboardOrMouseKeyCategory.Function,
				(>= KeyboardOrMouseKey.NumpadDivide and <= KeyboardOrMouseKey.NumpadPeriod) or KeyboardOrMouseKey.NumpadEquals or KeyboardOrMouseKey.NumpadComma or (>= KeyboardOrMouseKey.NumpadDoubleZero and <= KeyboardOrMouseKey.NumpadBackspace) => KeyboardOrMouseKeyCategory.Numpad,
				KeyboardOrMouseKey.Unknown => KeyboardOrMouseKeyCategory.Other,
				_ => KeyboardOrMouseKeyCategory.Control
			};
		}
	}

	public static KeyboardOrMouseKeyCategory GetCategory(this KeyboardOrMouseKey @this) {
		try {
			return _precomputedCategoryArray[((int) @this) & ~SdlScancodeToKeycodeBit];
		}
		catch (IndexOutOfRangeException e) {
			throw new ArgumentOutOfRangeException($"Given {nameof(KeyboardOrMouseKey)} value '{nameof(@this)}' ({@this}) is likely not defined, " +
												  $"resulting in an {nameof(IndexOutOfRangeException)} when looking up its precomputed {nameof(KeyboardOrMouseKeyCategory)}.", e);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int? GetNumericValue(this KeyboardOrMouseKey @this) => InputUtils.KeyToNumericValue(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static char? GetCharacterValue(this KeyboardOrMouseKey @this) => InputUtils.KeyToCharacterValue(@this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static KeyboardOrMouseKey ToKeyboardOrMouseKey(this MouseKey @this) => (KeyboardOrMouseKey) @this;
}