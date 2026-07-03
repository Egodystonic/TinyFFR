// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Text;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Text;

public readonly ref struct FontCreationConfig : IConfigStruct<FontCreationConfig> {
	static readonly Rune[] _defaultSupportedRunes = BuildDefaultRuneSet();
	public static ReadOnlySpan<Rune> DefaultSupportedRunes => _defaultSupportedRunes;
	public ReadOnlySpan<Rune> SupportedRunes { get; init; } = DefaultSupportedRunes;
	public ReadOnlySpan<char> Name { get; init; }
	
	public FontCreationConfig() { }

#pragma warning disable CA1822 // "Could be static" -- Placeholder method for future
	internal void ThrowIfInvalid() {
		/* no op */
	}
#pragma warning restore CA1822

	public static int GetHeapStorageFormattedLength(in FontCreationConfig src) {
		return SerializationSizeOfString(src.Name); // Name
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in FontCreationConfig src) {
		SerializationWriteString(ref dest, src.Name);
	}
	public static FontCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new FontCreationConfig {
			Name = SerializationReadString(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
	
	static Rune[] BuildDefaultRuneSet() {
		Rune[] Execute(params ReadOnlySpan<(int StartInclusive, int EndExclusive)> ranges) {
			var arrayLength = 0;
			foreach (var range in ranges) {
				arrayLength += range.EndExclusive - range.StartInclusive;
			}
			var result = new Rune[arrayLength];
			var index = 0;
			foreach (var range in ranges) {
				for (var i = range.StartInclusive; i < range.EndExclusive; ++i) {
					result[index++] = new Rune(i);
				}
			}
			
			return result;
		}
		
		// All codepage ranges below try to dodge non-graphics
		return Execute(
			(0x0020, 0x007F), // Language: Basic Latin
			(0x00A1, 0x00AD), // Language: Latin-1 (up to soft-hyphen)
			(0x00AE, 0x0100), // Language: Latin-1 (after soft-hyphen)
			(0x0100, 0x0180), // Language: Latin Extended-A
			(0x0391, 0x03AA), // Maths: Greek Upper Case
			(0x03B1, 0x03CA), // Maths: Greek Lower Case
			(0x2013, 0x2015), // Language: Dashes
			(0x2018, 0x201A), // Language: Curly Single Quotes
			(0x201C, 0x201E), // Language: Curly Double Quotes
			(0x2020, 0x2022), // Language: Daggers
			(0x2022, 0x2023), // Language: Bullet
			(0x2026, 0x2027), // Language: Ellipsis
			(0x2030, 0x2031), // Language: Permille
			(0x2039, 0x203A), // Language: Single French Quote Left (Doubles are in Latin-1)
			(0x203A, 0x203B), // Language: Single French Quote Right (Doubles are in Latin-1)
			(0x2070, 0x20A0), // Maths: Superscript/Subscript Digits
			(0x20AC, 0x20AD), // Language: Euro Symbol
			(0x2122, 0x2123), // Language: TM (Trademark)
			(0x2190, 0x2200), // UI: Arrows
			(0x2200, 0x2300), // Maths: Various Symbols
			(0x2500, 0x2600), // UI: Boxes, Gradients, Tree/Graph Symbols, Shapes, State Markers
			(0xFFFD, 0xFFFE)  // Unicode replacement char
		);
	}
}