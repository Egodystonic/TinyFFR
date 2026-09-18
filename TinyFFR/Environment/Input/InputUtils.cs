// Created on 2025-05-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Environment.Input;

/// <summary>
/// A static class holding helper methods for working with <see cref="KeyboardOrMouseKey"/> values.
/// </summary>
public static class InputUtils {
	static readonly KeyboardOrMouseKey[] _allKeys = Enum.GetValues<KeyboardOrMouseKey>();
	static readonly KeyboardOrMouseKeyCategory[] _allCategories = Enum.GetValues<KeyboardOrMouseKeyCategory>();

	/// <summary>
	/// Every <see cref="KeyboardOrMouseKey"/> this library can report, excluding <see cref="KeyboardOrMouseKey.Unknown"/>.
	/// </summary>
	public static ReadOnlySpan<KeyboardOrMouseKey> AllKeys => _allKeys.AsSpan(1); // Ignore "Unknown"
	/// <summary>
	/// Every <see cref="KeyboardOrMouseKeyCategory"/>, excluding <see cref="KeyboardOrMouseKeyCategory.Other"/>.
	/// </summary>
	public static ReadOnlySpan<KeyboardOrMouseKeyCategory> AllCategories => _allCategories.AsSpan(1); // Ignore "Other"

	/// <summary>
	/// Returns the digit <c>0</c> to <c>9</c> that <paramref name="key"/> represents, if it is a number-row or numpad digit key.
	/// </summary>
	/// <param name="key">The key to get the numeric value of.</param>
	/// <returns>The digit this key represents, or <see langword="null"/> if it is not a digit key.</returns>
	public static int? KeyToNumericValue(KeyboardOrMouseKey key) {
		static int? GetNumpadValue(KeyboardOrMouseKey key) {
			var keyInt = ((int) key) & ~KeyboardOrMouseKeyExtensions.SdlScancodeToKeycodeBit;

			return keyInt switch {
				98 => 0,
				>= 89 and <= 97 => keyInt - 88,
				_ => null
			};
		}

		return key.GetCategory() switch {
			KeyboardOrMouseKeyCategory.NumberRow => (((int) key) & ~KeyboardOrMouseKeyExtensions.CharBasedValueBit) - '0',
			KeyboardOrMouseKeyCategory.Numpad => GetNumpadValue(key),
			_ => null
		};
	}

	/// <summary>
	/// Returns the character that <paramref name="key"/> represents, if it is a character-producing key.
	/// </summary>
	/// <remarks>
	/// Letter keys return their lowercase form (e.g. <see cref="KeyboardOrMouseKey.A"/> returns <c>'a'</c>), as this reflects the key itself rather than any modifier applied at the time it was pressed.
	/// Keys with no character representation (function keys, modifiers, mouse buttons, etc.) return <see langword="null"/>.
	/// </remarks>
	/// <param name="key">The key to get the character value of.</param>
	public static char? KeyToCharacterValue(KeyboardOrMouseKey key) {
		var keyInt = (int) key;
		var potentialResult = keyInt & ~KeyboardOrMouseKeyExtensions.CharBasedValueBit;
		return potentialResult != keyInt ? (char) potentialResult : null;
	}

	/// <summary>
	/// Returns the key that represents the digit <paramref name="valueZeroToNine"/>; the inverse of <see cref="KeyToNumericValue"/>.
	/// </summary>
	/// <param name="valueZeroToNine">The digit to find the key for. Must be in the range <c>0 &lt;= n &lt;= 9</c>.</param>
	/// <param name="returnNumberRowValue">If <see langword="true"/> (the default), returns the number-row key for this digit; if <see langword="false"/>, returns the numpad key instead.</param>
	/// <returns>The key representing the given digit, or <see langword="null"/> if <paramref name="valueZeroToNine"/> is outside the range <c>0 &lt;= n &lt;= 9</c>.</returns>
	public static KeyboardOrMouseKey? KeyFromNumericValue(int valueZeroToNine, bool returnNumberRowValue = true) {
		return (valueZeroToNine, returnNumberRowValue) switch {
			( >= 0 and <= 9, true) => KeyboardOrMouseKey.NumberRow0 + valueZeroToNine,
			( >= 1 and <= 9, false) => KeyboardOrMouseKey.Numpad1 + (valueZeroToNine - 1),
			(0, false) => KeyboardOrMouseKey.Numpad0,
			_ => null,
		};
	}

	/// <summary>
	/// Returns the key that produces <paramref name="character"/>; the inverse of <see cref="KeyToCharacterValue"/>.
	/// </summary>
	/// <remarks>
	/// Letter characters must be given in lowercase to match (e.g. <c>'a'</c> returns <see cref="KeyboardOrMouseKey.A"/>, but <c>'A'</c> returns <see langword="null"/>).
	/// </remarks>
	/// <param name="character">The character to find the key for.</param>
	/// <returns>The key producing the given character, or <see langword="null"/> if no key represents it.</returns>
	public static KeyboardOrMouseKey? KeyFromCharacterValue(char character) {
		var potentialResult = (KeyboardOrMouseKey) (character | KeyboardOrMouseKeyExtensions.CharBasedValueBit);
		return Enum.IsDefined(potentialResult) ? potentialResult : null;
	}
}