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
/// <summary>
/// Represents a single key on a keyboard, or a button/wheel movement on a mouse.
/// </summary>
/// <remarks>
/// <para>
/// Keyboard keys are identified by the character or function they produce on the user's <i>current keyboard layout</i>, not by their physical
/// position. For example, the key sitting where <see cref="Z"/> is on a QWERTY layout reports <see cref="Y"/> on a QWERTZ layout. If you want a
/// key's meaning to follow its physical position instead (e.g. WASD movement controls), bear in mind that users on other layouts may need to rebind.
/// </para>
/// <para>
/// Not every key that exists on every keyboard is represented here: keys that are rare, hardware-specific, or absent from most modern keyboards
/// (media controls, display brightness, dedicated application buttons, and so on) are deliberately omitted.
/// </para>
/// <para>
/// The final few values represent mouse buttons and wheel movements rather than keyboard keys. See also <see cref="MouseKey"/>, a mouse-only
/// enumeration whose values are directly interchangeable with the equivalent values here.
/// </para>
/// </remarks>
public enum KeyboardOrMouseKey : int {
	/// <summary>
	/// Unknown or unrecognised key.
	/// </summary>
	Unknown = 0,

	/// <summary>
	/// Return key (sometimes known as 'Enter'). 
	/// </summary>
	Return = '\r' | CharBasedValueBit,
	/// <summary>
	/// Escape key.
	/// </summary>
	Escape = '\x1B' | CharBasedValueBit,
	/// <summary>
	/// Backspace key.
	/// </summary>
	Backspace = '\b' | CharBasedValueBit,
	/// <summary>
	/// Tab key.
	/// </summary>
	Tab = '\t' | CharBasedValueBit,
	/// <summary>
	/// Space bar.
	/// </summary>
	Space = ' ' | CharBasedValueBit,
	/// <summary>
	/// <c>!</c> key.
	/// </summary>
	ExclamationMark = '!' | CharBasedValueBit,
	/// <summary>
	/// <c>"</c> key.
	/// </summary>
	DoubleQuote = '"' | CharBasedValueBit,
	/// <summary>
	/// <c>#</c> key.
	/// </summary>
	Hash = '#' | CharBasedValueBit,
	/// <summary>
	/// <c>%</c> key.
	/// </summary>
	Percent = '%' | CharBasedValueBit,
	/// <summary>
	/// <c>$</c> key.
	/// </summary>
	Dollar = '$' | CharBasedValueBit,
	/// <summary>
	/// <c>&amp;</c> key.
	/// </summary>
	Ampersand = '&' | CharBasedValueBit,
	/// <summary>
	/// <c>'</c> key.
	/// </summary>
	SingleQuote = '\'' | CharBasedValueBit,
	/// <summary>
	/// <c>(</c> key.
	/// </summary>
	OpeningParenthesis = '(' | CharBasedValueBit,
	/// <summary>
	/// <c>)</c> key.
	/// </summary>
	ClosingParenthesis = ')' | CharBasedValueBit,
	/// <summary>
	/// <c>*</c> key.
	/// </summary>
	Asterisk = '*' | CharBasedValueBit,
	/// <summary>
	/// <c>+</c> key.
	/// </summary>
	Plus = '+' | CharBasedValueBit,
	/// <summary>
	/// <c>,</c> key.
	/// </summary>
	Comma = ',' | CharBasedValueBit,
	/// <summary>
	/// <c>-</c> key.
	/// </summary>
	Minus = '-' | CharBasedValueBit,
	/// <summary>
	/// <c>.</c> key.
	/// </summary>
	Period = '.' | CharBasedValueBit,
	/// <summary>
	/// <c>/</c> key.
	/// </summary>
	ForwardSlash = '/' | CharBasedValueBit,
	/// <summary>
	/// <c>0</c> key on the number row above the letter keys (as opposed to <see cref="Numpad0"/>).
	/// </summary>
	NumberRow0 = '0' | CharBasedValueBit,
	/// <summary>
	/// <c>1</c> key on the number row above the letter keys (as opposed to <see cref="Numpad1"/>).
	/// </summary>
	NumberRow1 = '1' | CharBasedValueBit,
	/// <summary>
	/// <c>2</c> key on the number row above the letter keys (as opposed to <see cref="Numpad2"/>).
	/// </summary>
	NumberRow2 = '2' | CharBasedValueBit,
	/// <summary>
	/// <c>3</c> key on the number row above the letter keys (as opposed to <see cref="Numpad3"/>).
	/// </summary>
	NumberRow3 = '3' | CharBasedValueBit,
	/// <summary>
	/// <c>4</c> key on the number row above the letter keys (as opposed to <see cref="Numpad4"/>).
	/// </summary>
	NumberRow4 = '4' | CharBasedValueBit,
	/// <summary>
	/// <c>5</c> key on the number row above the letter keys (as opposed to <see cref="Numpad5"/>).
	/// </summary>
	NumberRow5 = '5' | CharBasedValueBit,
	/// <summary>
	/// <c>6</c> key on the number row above the letter keys (as opposed to <see cref="Numpad6"/>).
	/// </summary>
	NumberRow6 = '6' | CharBasedValueBit,
	/// <summary>
	/// <c>7</c> key on the number row above the letter keys (as opposed to <see cref="Numpad7"/>).
	/// </summary>
	NumberRow7 = '7' | CharBasedValueBit,
	/// <summary>
	/// <c>8</c> key on the number row above the letter keys (as opposed to <see cref="Numpad8"/>).
	/// </summary>
	NumberRow8 = '8' | CharBasedValueBit,
	/// <summary>
	/// <c>9</c> key on the number row above the letter keys (as opposed to <see cref="Numpad9"/>).
	/// </summary>
	NumberRow9 = '9' | CharBasedValueBit,
	/// <summary>
	/// <c>:</c> key.
	/// </summary>
	Colon = ':' | CharBasedValueBit,
	/// <summary>
	/// <c>;</c> key.
	/// </summary>
	Semicolon = ';' | CharBasedValueBit,
	/// <summary>
	/// <c>&lt;</c> key.
	/// </summary>
	LessThan = '<' | CharBasedValueBit,
	/// <summary>
	/// <c>=</c> key.
	/// </summary>
	Equals = '=' | CharBasedValueBit,
	/// <summary>
	/// <c>&gt;</c> key.
	/// </summary>
	GreaterThan = '>' | CharBasedValueBit,
	/// <summary>
	/// <c>?</c> key.
	/// </summary>
	QuestionMark = '?' | CharBasedValueBit,
	/// <summary>
	/// <c>@</c> key.
	/// </summary>
	AtSymbol = '@' | CharBasedValueBit,
	/// <summary>
	/// <c>[</c> key.
	/// </summary>
	LeftSquareBracket = '[' | CharBasedValueBit,
	/// <summary>
	/// <c>\</c> key.
	/// </summary>
	BackSlash = '\\' | CharBasedValueBit,
	/// <summary>
	/// <c>]</c> key.
	/// </summary>
	RightSquareBracket = ']' | CharBasedValueBit,
	/// <summary>
	/// <c>^</c> key.
	/// </summary>
	Caret = '^' | CharBasedValueBit,
	/// <summary>
	/// <c>_</c> key.
	/// </summary>
	Underscore = '_' | CharBasedValueBit,
	/// <summary>
	/// <c>`</c> key.
	/// </summary>
	Backtick = '`' | CharBasedValueBit,
	/// <summary>
	/// <c>A</c> key.
	/// </summary>
	A = 'a' | CharBasedValueBit,
	/// <summary>
	/// <c>B</c> key.
	/// </summary>
	B = 'b' | CharBasedValueBit,
	/// <summary>
	/// <c>C</c> key.
	/// </summary>
	C = 'c' | CharBasedValueBit,
	/// <summary>
	/// <c>D</c> key.
	/// </summary>
	D = 'd' | CharBasedValueBit,
	/// <summary>
	/// <c>E</c> key.
	/// </summary>
	E = 'e' | CharBasedValueBit,
	/// <summary>
	/// <c>F</c> key.
	/// </summary>
	F = 'f' | CharBasedValueBit,
	/// <summary>
	/// <c>G</c> key.
	/// </summary>
	G = 'g' | CharBasedValueBit,
	/// <summary>
	/// <c>H</c> key.
	/// </summary>
	H = 'h' | CharBasedValueBit,
	/// <summary>
	/// <c>I</c> key.
	/// </summary>
	I = 'i' | CharBasedValueBit,
	/// <summary>
	/// <c>J</c> key.
	/// </summary>
	J = 'j' | CharBasedValueBit,
	/// <summary>
	/// <c>K</c> key.
	/// </summary>
	K = 'k' | CharBasedValueBit,
	/// <summary>
	/// <c>L</c> key.
	/// </summary>
	L = 'l' | CharBasedValueBit,
	/// <summary>
	/// <c>M</c> key.
	/// </summary>
	M = 'm' | CharBasedValueBit,
	/// <summary>
	/// <c>N</c> key.
	/// </summary>
	N = 'n' | CharBasedValueBit,
	/// <summary>
	/// <c>O</c> key.
	/// </summary>
	O = 'o' | CharBasedValueBit,
	/// <summary>
	/// <c>P</c> key.
	/// </summary>
	P = 'p' | CharBasedValueBit,
	/// <summary>
	/// <c>Q</c> key.
	/// </summary>
	Q = 'q' | CharBasedValueBit,
	/// <summary>
	/// <c>R</c> key.
	/// </summary>
	R = 'r' | CharBasedValueBit,
	/// <summary>
	/// <c>S</c> key.
	/// </summary>
	S = 's' | CharBasedValueBit,
	/// <summary>
	/// <c>T</c> key.
	/// </summary>
	T = 't' | CharBasedValueBit,
	/// <summary>
	/// <c>U</c> key.
	/// </summary>
	U = 'u' | CharBasedValueBit,
	/// <summary>
	/// <c>V</c> key.
	/// </summary>
	V = 'v' | CharBasedValueBit,
	/// <summary>
	/// <c>W</c> key.
	/// </summary>
	W = 'w' | CharBasedValueBit,
	/// <summary>
	/// <c>X</c> key.
	/// </summary>
	X = 'x' | CharBasedValueBit,
	/// <summary>
	/// <c>Y</c> key.
	/// </summary>
	Y = 'y' | CharBasedValueBit,
	/// <summary>
	/// <c>Z</c> key.
	/// </summary>
	Z = 'z' | CharBasedValueBit,

	/// <summary>
	/// Caps Lock key.
	/// </summary>
	CapsLock = 57 | SdlScancodeToKeycodeBit,

	/// <summary>
	/// <c>F1</c> key.
	/// </summary>
	F1 = 58 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F2</c> key.
	/// </summary>
	F2 = 59 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F3</c> key.
	/// </summary>
	F3 = 60 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F4</c> key.
	/// </summary>
	F4 = 61 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F5</c> key.
	/// </summary>
	F5 = 62 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F6</c> key.
	/// </summary>
	F6 = 63 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F7</c> key.
	/// </summary>
	F7 = 64 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F8</c> key.
	/// </summary>
	F8 = 65 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F9</c> key.
	/// </summary>
	F9 = 66 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F10</c> key.
	/// </summary>
	F10 = 67 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F11</c> key.
	/// </summary>
	F11 = 68 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F12</c> key.
	/// </summary>
	F12 = 69 | SdlScancodeToKeycodeBit,

	/// <summary>
	/// Print Screen key.
	/// </summary>
	PrintScreen = 70 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Scroll Lock key.
	/// </summary>
	ScrollLock = 71 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Pause/Break key.
	/// </summary>
	Pause = 72 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Insert key.
	/// </summary>
	Insert = 73 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Home key.
	/// </summary>
	Home = 74 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Page Up key.
	/// </summary>
	PageUp = 75 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Delete key.
	/// </summary>
	Delete = '\x7F' | CharBasedValueBit,
	/// <summary>
	/// End key.
	/// </summary>
	End = 77 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Page Down key.
	/// </summary>
	PageDown = 78 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Right arrow key.
	/// </summary>
	ArrowRight = 79 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Left arrow key.
	/// </summary>
	ArrowLeft = 80 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Down arrow key.
	/// </summary>
	ArrowDown = 81 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Up arrow key.
	/// </summary>
	ArrowUp = 82 | SdlScancodeToKeycodeBit,

	/// <summary>
	/// Num Lock key.
	/// </summary>
	NumLock = 83 | SdlScancodeToKeycodeBit,
	
	/// <summary>
	/// <c>/</c> key on the numpad.
	/// </summary>
	NumpadDivide = 84 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>*</c> key on the numpad.
	/// </summary>
	NumpadMultiply = 85 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>-</c> key on the numpad.
	/// </summary>
	NumpadMinus = 86 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>+</c> key on the numpad.
	/// </summary>
	NumpadPlus = 87 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Enter key on the numpad.
	/// </summary>
	NumpadEnter = 88 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>1</c> key on the numpad (as opposed to <see cref="NumberRow1"/>).
	/// </summary>
	Numpad1 = 89 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>2</c> key on the numpad (as opposed to <see cref="NumberRow2"/>).
	/// </summary>
	Numpad2 = 90 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>3</c> key on the numpad (as opposed to <see cref="NumberRow3"/>).
	/// </summary>
	Numpad3 = 91 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>4</c> key on the numpad (as opposed to <see cref="NumberRow4"/>).
	/// </summary>
	Numpad4 = 92 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>5</c> key on the numpad (as opposed to <see cref="NumberRow5"/>).
	/// </summary>
	Numpad5 = 93 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>6</c> key on the numpad (as opposed to <see cref="NumberRow6"/>).
	/// </summary>
	Numpad6 = 94 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>7</c> key on the numpad (as opposed to <see cref="NumberRow7"/>).
	/// </summary>
	Numpad7 = 95 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>8</c> key on the numpad (as opposed to <see cref="NumberRow8"/>).
	/// </summary>
	Numpad8 = 96 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>9</c> key on the numpad (as opposed to <see cref="NumberRow9"/>).
	/// </summary>
	Numpad9 = 97 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>0</c> key on the numpad (as opposed to <see cref="NumberRow0"/>).
	/// </summary>
	Numpad0 = 98 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>.</c> key on the numpad.
	/// </summary>
	NumpadPeriod = 99 | SdlScancodeToKeycodeBit,

	/// <summary>
	/// Context-menu key (usually found between the right Alt and right Control keys; opens the same menu as a right-click).
	/// </summary>
	WindowsContextMenu = 101 | SdlScancodeToKeycodeBit,
	
	// DedicatedPowerButton = 102 | KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit,
	
	/// <summary>
	/// <c>=</c> key on the numpad.
	/// </summary>
	NumpadEquals = 103 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F13</c> key.
	/// </summary>
	F13 = 104 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F14</c> key.
	/// </summary>
	F14 = 105 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F15</c> key.
	/// </summary>
	F15 = 106 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F16</c> key.
	/// </summary>
	F16 = 107 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F17</c> key.
	/// </summary>
	F17 = 108 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F18</c> key.
	/// </summary>
	F18 = 109 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F19</c> key.
	/// </summary>
	F19 = 110 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F20</c> key.
	/// </summary>
	F20 = 111 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F21</c> key.
	/// </summary>
	F21 = 112 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F22</c> key.
	/// </summary>
	F22 = 113 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F23</c> key.
	/// </summary>
	F23 = 114 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>F24</c> key.
	/// </summary>
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
	
	/// <summary>
	/// <c>,</c> key on the numpad.
	/// </summary>
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

	/// <summary>
	/// <c>00</c> key on the numpad.
	/// </summary>
	NumpadDoubleZero = 176 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>000</c> key on the numpad.
	/// </summary>
	NumpadTripleZero = 177 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Thousands-separator key on the numpad (only present on some keyboard layouts).
	/// </summary>
	NumpadThousandsSeparator = 178 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Decimal-separator key on the numpad (only present on some keyboard layouts).
	/// </summary>
	NumpadDecimalsSeparator = 179 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Currency-unit key on the numpad (only present on some keyboard layouts).
	/// </summary>
	NumpadCurrencyUnit = 180 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Currency-subunit key on the numpad (only present on some keyboard layouts).
	/// </summary>
	NumpadCurrencySubUnit = 181 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>(</c> key on the numpad.
	/// </summary>
	NumpadOpeningParenthesis = 182 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>)</c> key on the numpad.
	/// </summary>
	NumpadClosingParenthesis = 183 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>{</c> key on the numpad.
	/// </summary>
	NumpadOpeningBrace = 184 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// <c>}</c> key on the numpad.
	/// </summary>
	NumpadClosingBrace = 185 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Tab key on the numpad (only present on some keyboard layouts).
	/// </summary>
	NumpadTab = 186 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Backspace key on the numpad (only present on some keyboard layouts).
	/// </summary>
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

	/// <summary>
	/// Left Control key.
	/// </summary>
	LeftControl = 224 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Left Shift key.
	/// </summary>
	LeftShift = 225 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Left Alt key.
	/// </summary>
	LeftAlt = 226 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Left Windows key (also known as the Command key on macOS, or the Super key on Linux).
	/// </summary>
	LeftWinKey = 227 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Right Control key.
	/// </summary>
	RightControl = 228 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Right Shift key.
	/// </summary>
	RightShift = 229 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Right Alt key (also known as AltGr on layouts that have one).
	/// </summary>
	RightAlt = 230 | SdlScancodeToKeycodeBit,
	/// <summary>
	/// Right Windows key (also known as the Command key on macOS, or the Super key on Linux).
	/// </summary>
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

	/// <summary>
	/// Left mouse button.
	/// </summary>
	MouseLeft = NonSdlKeyStartValue,
	/// <summary>
	/// Middle mouse button (usually pressing down on the scroll wheel).
	/// </summary>
	MouseMiddle = NonSdlKeyStartValue + 1,
	/// <summary>
	/// Right mouse button.
	/// </summary>
	MouseRight = NonSdlKeyStartValue + 2,
	/// <summary>
	/// Fourth mouse button (usually the rearmost thumb button, conventionally 'back').
	/// </summary>
	Mouse4 = NonSdlKeyStartValue + 3,
	/// <summary>
	/// Fifth mouse button (usually the frontmost thumb button, conventionally 'forward').
	/// </summary>
	Mouse5 = NonSdlKeyStartValue + 4,
	/// <summary>
	/// A single notch of upward mouse wheel scrolling, reported as a momentary key press.
	/// </summary>
	/// <remarks>
	/// Use <see cref="ILatestKeyboardAndMouseInputRetriever.MouseScrollWheelDelta"/> instead if you want an aggregated measure of how far the wheel was scrolled in an iteration, rather than discrete notches.
	/// </remarks>
	MouseWheelUp = NonSdlKeyStartValue + 5,
	/// <summary>
	/// A single notch of downward mouse wheel scrolling, reported as a momentary key press.
	/// </summary>
	/// <remarks>
	/// Use <see cref="ILatestKeyboardAndMouseInputRetriever.MouseScrollWheelDelta"/> instead if you want an aggregated measure of how far the wheel was scrolled in an iteration, rather than discrete notches.
	/// </remarks>
	MouseWheelDown = NonSdlKeyStartValue + 6,
}

/// <summary>
/// A static class holding extension methods for <see cref="KeyboardOrMouseKey"/> and <see cref="MouseKey"/>.
/// </summary>
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

	/// <summary>
	/// Returns the <see cref="KeyboardOrMouseKeyCategory"/> that <paramref name="this"/> belongs to (e.g. alphabetic, numpad, modifier).
	/// </summary>
	/// <param name="this">The key to categorise. Must be a defined <see cref="KeyboardOrMouseKey"/> value.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="this"/> is not a defined <see cref="KeyboardOrMouseKey"/> value.</exception>
	public static KeyboardOrMouseKeyCategory GetCategory(this KeyboardOrMouseKey @this) {
		try {
			return _precomputedCategoryArray[((int) @this) & ~SdlScancodeToKeycodeBit];
		}
		catch (IndexOutOfRangeException e) {
			throw new ArgumentOutOfRangeException($"Given {nameof(KeyboardOrMouseKey)} value '{nameof(@this)}' ({@this}) is likely not defined, " +
												  $"resulting in an {nameof(IndexOutOfRangeException)} when looking up its precomputed {nameof(KeyboardOrMouseKeyCategory)}.", e);
		}
	}

	/// <summary>
	/// Returns the digit <c>0</c> to <c>9</c> that <paramref name="this"/> represents, if it is a number-row or numpad digit key.
	/// </summary>
	/// <param name="this">The key to get the numeric value of.</param>
	/// <returns>The digit this key represents, or <see langword="null"/> if it is not a digit key.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int? GetNumericValue(this KeyboardOrMouseKey @this) => InputUtils.KeyToNumericValue(@this);

	/// <summary>
	/// Returns the character that <paramref name="this"/> represents, if it is a character-producing key.
	/// </summary>
	/// <remarks>
	/// Letter keys return their lowercase form (e.g. <see cref="KeyboardOrMouseKey.A"/> returns <c>'a'</c>), as this reflects the key itself rather than any modifier applied at the time it was pressed.
	/// Keys with no character representation (function keys, modifiers, mouse buttons, etc.) return <see langword="null"/>.
	/// </remarks>
	/// <param name="this">The key to get the character value of.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static char? GetCharacterValue(this KeyboardOrMouseKey @this) => InputUtils.KeyToCharacterValue(@this);

	/// <summary>
	/// Converts <paramref name="this"/> to its equivalent <see cref="KeyboardOrMouseKey"/> value.
	/// </summary>
	/// <param name="this">The mouse key to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static KeyboardOrMouseKey ToKeyboardOrMouseKey(this MouseKey @this) => (KeyboardOrMouseKey) @this;
}