---
title: Loading Fonts
description: Information on how to load fonts in TinyFFR.
---

<div class="grid cards" markdown>

-   :chestnut:{ : style="margin-right:0.3em" } __In a nutshell...__

    * Fonts can be loaded from `.ttf` files, or you can use one of TinyFFR's built-in fonts. :material-arrow-right: [Loading Fonts](#loading-fonts)
    * Every character a font can draw is prepared when the font is loaded, so the set of supported characters is chosen up front. :material-arrow-right: [Customizing the Load Operation](#customizing-the-load-operation)
    * Once loaded, fonts are used to draw text in 2D or 3D scenes. :material-arrow-right: [FontString & FontPen](fontstring_and_fontpen.md)

</div>

## Loading Fonts

```csharp
using var defaultFont = factory.AssetLoader.LoadFont(); // (1)!
using var monoFont = factory.AssetLoader.LoadFont(BuiltInFont.Monospace); // (2)!
using var customFont = factory.AssetLoader.LoadFont(@"Assets/Fonts/Roboto-Regular.ttf"); // (3)!

using var pen = defaultFont.CreatePen(StandardColor.White); // (4)!
using var helloWorld = defaultFont.CreateString("Hello, world!");
```

1.	Loads TinyFFR's default built-in font.

2.	Loads TinyFFR's built-in monospace font (see [Built-in Fonts](#built-in-fonts) below).

3.	Loads a font from a `.ttf` file.

4.	Creates a pen and a string from the font, ready to be drawn. Pens and strings are explained on the next page: [FontString & FontPen](fontstring_and_fontpen.md).

A `Font` represents a typeface loaded and prepared for drawing text. Fonts can be loaded via `factory.AssetLoader.LoadFont()`, either from one of TinyFFR's [built-in fonts](#built-in-fonts) or from a font file. Font files must be in TrueType (`.ttf`) format.

A font on its own doesn't draw anything. To draw text, you create a `FontPen` (which determines the colours text is drawn in) and a `FontString` (the text itself) from the font; both are explained on the [next page](fontstring_and_fontpen.md).

??? abstract "Font System Under the Hood"
	When a font is loaded, TinyFFR draws every character the font should be able to display in to a single texture (known as the font's *atlas*), once, up front. Text is then assembled from that texture, which makes drawing text cheap; but it also means the set of characters a font can draw is fixed when it's loaded (see [Customizing the Load Operation](#customizing-the-load-operation) below).

	The characters are stored in the atlas as *signed distance fields* rather than as plain images, which keeps their edges crisp at almost any size and makes it possible to draw them with outlines.

### Built-in Fonts

TinyFFR comes with three built-in fonts, selected via the `BuiltInFont` enum, which can be used without supplying any font file of your own:

<span class="def-icon">:material-card-bulleted-outline:</span> `BuiltInFont.SansSerif`

:   A typeface whose letters have no decorative strokes on the ends of their stems.

<span class="def-icon">:material-card-bulleted-outline:</span> `BuiltInFont.Serif`

:   A typeface whose letters carry small decorative strokes ("serifs") on the ends of their stems.

<span class="def-icon">:material-card-bulleted-outline:</span> `BuiltInFont.Monospace`

:   A typeface in which every character occupies the same horizontal width, so that text lines up in columns. Useful for numbers that change frequently (such as scores or timers), tables, and code.

`BuiltInFont.Default` (which is what `LoadFont()` uses when no font is specified) is currently the same as `BuiltInFont.SansSerif`, but may change in future versions of TinyFFR.

## Customizing the Load Operation

```csharp
Span<Rune> scoreRunes = stackalloc Rune[12]; // (1)!
for (var i = 0; i < 10; ++i) scoreRunes[i] = new Rune('0' + i);
scoreRunes[10] = new Rune(':');
scoreRunes[11] = new Rune(0xFFFD);

using var scoreFont = factory.AssetLoader.LoadFont(BuiltInFont.Monospace, new FontCreationConfig {
	SupportedRunes = scoreRunes, // (2)!
	LineSpacingMultiplier = 1.2f, // (3)!
	Name = "Score Font"
});
```

1.	Builds a list of the only characters this font needs to draw: The digits 0 to 9, a colon, and the Unicode replacement character (see [Missing Characters](#missing-characters) below).

	A `Rune` (`System.Text.Rune`) represents a single Unicode character.

2.	Prepares only those twelve characters, which makes the font quicker to load and its atlas smaller.

3.	Spaces lines of text 20% further apart than the font's natural line spacing.

Every `LoadFont()` method has an overload that takes a `FontCreationConfig`, which has the following properties:

<span class="def-icon">:material-card-bulleted-outline:</span> `SupportedRunes`

:   The characters the font should be able to draw. Defaults to `FontCreationConfig.DefaultSupportedRunes` (see below). Must not be empty.

	Characters listed here that the font file doesn't actually contain are skipped.

<span class="def-icon">:material-card-bulleted-outline:</span> `LineBreakRune`

:   The character that starts a new line in a string. Defaults to `'\n'`. This character is never drawn, and doesn't need to be included in `SupportedRunes`.

<span class="def-icon">:material-card-bulleted-outline:</span> `LineSpacingMultiplier`

:   How far apart consecutive lines of text are, where `1f` is the font's own line spacing. Values below `1f` pull lines closer together, and values above push them further apart. Defaults to `1f`. Must be finite and not negative.

<span class="def-icon">:material-card-bulleted-outline:</span> `Name`

:   The name to give the font. May be left empty.

??? abstract "Default Supported Characters"
	`FontCreationConfig.DefaultSupportedRunes` covers the following, which is enough for most languages that use the Latin alphabet, as well as most user interface work:

	* Basic Latin (i.e. standard English letters, digits, and punctuation);
	* Latin-1 Supplement and Latin Extended-A (accented letters for most European languages);
	* Greek letters;
	* Dashes, curly quotation marks, guillemets, daggers, bullets, ellipses, and the per-mille sign;
	* The euro and trademark symbols;
	* Superscript and subscript digits;
	* Arrows, mathematical symbols, box-drawing characters, and geometric shapes;
	* The Unicode replacement character (`U+FFFD`, "�").

??? warning "Large Character Sets"
	Every supported character takes up space in the font's atlas, so larger character sets take longer to load and use more video memory. For very large character sets TinyFFR also has to draw each character at a lower resolution to fit them all in.

	If a character set is too large to fit in the largest atlas TinyFFR supports, `LoadFont()` throws an `InvalidOperationException`. This is most likely with languages that have very large numbers of characters (such as Chinese, Japanese, or Korean); in these cases you should restrict `SupportedRunes` to only the characters your application actually displays.
	
	Dynamic font atlas rendering for CJK text is not yet supported, but may be at a later date. 

### Missing Characters

If a string contains a character that the font wasn't loaded with (or that the font file doesn't contain), it is drawn as the Unicode replacement character (`U+FFFD`, "�") if that was loaded, or as a question mark (`?`) if not. If neither was loaded, the missing character simply isn't drawn.

The replacement character is included in the default character set, so you should usually include it in your own custom character sets too (as in the example above), so that missing characters are easy to spot.

## Font Lifetimes

Like every other resource in TinyFFR, a font should be disposed when you're done with it. Disposing a font also disposes every `FontPen` and `FontString` created from it (as well as its atlas).

However, anything still *using* those pens or strings (such as a text instance in a scene) must be disposed before the font; attempting to dispose a font whose pens or strings are still in use throws a `ResourceDependencyException`.
