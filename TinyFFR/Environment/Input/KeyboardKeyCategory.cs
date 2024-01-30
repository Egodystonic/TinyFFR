// Created on 2024-01-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Input;

public enum KeyboardKeyCategory : int {
	/// <summary>
	/// Represents keys that are not contained in any other category.
	/// </summary>
	Other,
	/// <summary>
	/// Represents keys A through Z.
	/// </summary>
	Alphabetic,
	/// <summary>
	/// Represents keys on the top number row (1, 2, 3, 4, 5, 6, 7, 8, 9, 0).
	/// </summary>
	NumberRow,
	/// <summary>
	/// Represents all keys on the number pad (also known as the keypad), usually to the right of the main keyboard layout.
	/// </summary>
	Numpad,
	/// <summary>
	/// Represents all keys that are symbols or punctuation (including space).
	/// </summary>
	PunctuationAndSymbology,
	/// <summary>
	/// Represents control, alt, and shift (left and right).
	/// </summary>
	Modifier,
	/// <summary>
	/// Represents F1 through F24 (the keys usually found isolated above the number row).
	/// </summary>
	Function,
	/// <summary>
	/// Represents the four arrow keys (left, right, up, down).
	/// </summary>
	Arrow,
	/// <summary>
	/// Represents the six text/page navigation/editing keys (insert, delete, home, end, page up, page down), usually found above the arrow keys.
	/// </summary>
	EditingAndNavigation,
	/// <summary>
	/// Represents the common system/application control keys (such as escape, return, caps lock, tab, backspace, etc).
	/// </summary>
	Control
}