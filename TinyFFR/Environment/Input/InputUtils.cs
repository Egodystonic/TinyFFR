// Created on 2025-05-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Environment.Input;

public static class InputUtils {
	static readonly KeyboardOrMouseKey[] _allKeys = Enum.GetValues<KeyboardOrMouseKey>();
	static readonly KeyboardOrMouseKeyCategory[] _allCategories = Enum.GetValues<KeyboardOrMouseKeyCategory>();
	
	public static ReadOnlySpan<KeyboardOrMouseKey> AllKeys => _allKeys.AsSpan(1); // Ignore "Unknown"
	public static ReadOnlySpan<KeyboardOrMouseKeyCategory> AllCategories => _allCategories.AsSpan(1); // Ignore "Other"

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

	public static char? KeyToCharacterValue(KeyboardOrMouseKey key) {
		var keyInt = (int) key;
		var potentialResult = keyInt & ~KeyboardOrMouseKeyExtensions.CharBasedValueBit;
		return potentialResult != keyInt ? (char) potentialResult : null;
	}

	public static KeyboardOrMouseKey? KeyFromNumericValue(int valueZeroToNine, bool returnNumberRowValue = true) {
		return (valueZeroToNine, returnNumberRowValue) switch {
			( >= 0 and <= 9, true) => KeyboardOrMouseKey.NumberRow0 + valueZeroToNine,
			( >= 1 and <= 9, false) => KeyboardOrMouseKey.Numpad1 + (valueZeroToNine - 1),
			(0, false) => KeyboardOrMouseKey.Numpad0,
			_ => null,
		};
	}

	public static KeyboardOrMouseKey? KeyFromCharacterValue(char character) {
		var potentialResult = (KeyboardOrMouseKey) (character | KeyboardOrMouseKeyExtensions.CharBasedValueBit);
		return Enum.IsDefined(potentialResult) ? potentialResult : null;
	}
}