// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Text;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Text;

/// <summary>
/// Controls how a font is prepared when it is loaded (such as which characters it covers and how its lines are spaced).
/// </summary>
/// <remarks>
/// A font is prepared by drawing every character it supports in to a texture once, up front, so that text can then be assembled
/// from that texture cheaply. That is why the set of supported characters is fixed at load time rather than being discovered as
/// text is drawn.
/// </remarks>
public readonly ref struct FontCreationConfig : IConfigStruct<FontCreationConfig> {
	static readonly Rune[] _defaultSupportedRunes = BuildDefaultRuneSet();
	/// <summary>
	/// The characters a font covers when no other set is specified.
	/// </summary>
	/// <remarks>
	/// This spans basic and extended Latin, Greek letters, common punctuation and quotation marks, currency and mathematical
	/// symbols, arrows and box-drawing characters, which is enough for most Latin-derived languages and for user interface
	/// work.
	/// </remarks>
	public static ReadOnlySpan<Rune> DefaultSupportedRunes => _defaultSupportedRunes;
	/// <summary>
	/// Which characters the font should be able to draw. Defaults to <see cref="DefaultSupportedRunes"/>.
	/// </summary>
	/// <remarks>
	/// Every character listed here is rendered in advance in to the font's texture, so a larger set costs more video memory and
	/// more time to prepare. Narrow it where you know only a few characters are needed. Must not be empty.
	/// </remarks>
	public ReadOnlySpan<Rune> SupportedRunes { get; init; } = DefaultSupportedRunes;
	/// <summary>
	/// Which character starts a new line. Defaults to <c>'\n'</c>.
	/// </summary>
	/// <remarks>
	/// This character is treated as a line break rather than being drawn, and does not itself need to appear in
	/// <see cref="SupportedRunes"/>.
	/// </remarks>
	public Rune LineBreakRune { get; init; } = new Rune('\n');
	/// <summary>
	/// How far apart consecutive lines of text sit, where <c>1f</c> is the font's own spacing. Defaults to <c>1f</c>.
	/// </summary>
	/// <remarks>
	/// Must be finite and not negative. Values below <c>1f</c> pull lines closer together; values above push them apart.
	/// </remarks>
	public float LineSpacingMultiplier { get; init; } = 1f;
	/// <summary>
	/// The name to give the font. May be left empty.
	/// </summary>
	public ReadOnlySpan<char> Name { get; init; }

	/// <summary>
	/// Constructs a new <see cref="FontCreationConfig"/> with default values for every property.
	/// </summary>
	public FontCreationConfig() { }

	internal void ThrowIfInvalid() {
		if (SupportedRunes.Length == 0) {
			throw new ArgumentOutOfRangeException(nameof(SupportedRunes), SupportedRunes.Length, "Must supplt at least one supported rune.");
		}
		if (!Single.IsFinite(LineSpacingMultiplier) || LineSpacingMultiplier < 0f) {
			throw new ArgumentOutOfRangeException(nameof(LineSpacingMultiplier), LineSpacingMultiplier, "Line spacing multiplier must be a finite, non-negative value.");
		}
	}

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in FontCreationConfig src) {
		return SerializationSizeOfSpan(src.SupportedRunes) // SupportedRunes
			+ SerializationSizeOfInt() // LineBreakRune
			+ SerializationSizeOfFloat() // LineSpacingMultiplier
			+ SerializationSizeOfString(src.Name); // Name
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in FontCreationConfig src) {
		SerializationWriteSpan(ref dest, src.SupportedRunes);
		SerializationWriteInt(ref dest, src.LineBreakRune.Value);
		SerializationWriteFloat(ref dest, src.LineSpacingMultiplier);
		SerializationWriteString(ref dest, src.Name);
	}
	/// <inheritdoc />
	public static FontCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new FontCreationConfig {
			SupportedRunes = SerializationReadSpan<Rune>(ref src),
			LineBreakRune = new Rune(SerializationReadInt(ref src)),
			LineSpacingMultiplier = SerializationReadFloat(ref src),
			Name = SerializationReadString(ref src)
		};
	}
	/// <inheritdoc />
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