// Created on 2026-06-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Assets.Text;

/// <summary>
/// Identifies one of the typefaces shipped with TinyFFR, for use when you want text on screen without supplying a font file
/// of your own.
/// </summary>
public enum BuiltInFont {
	/// <summary>
	/// The font used when no other is specified. This is currently the same typeface as <see cref="SansSerif"/>, but
	/// may change in future.
	/// </summary>
	Default,
	/// <summary>
	/// A typeface whose letters have no decorative strokes on the ends of their stems.
	/// </summary>
	SansSerif = Default,
	/// <summary>
	/// A typeface whose letters carry small decorative strokes ("serifs") on the ends of their stems.
	/// </summary>
	Serif,
	/// <summary>
	/// A typeface in which every character occupies the same horizontal width, so that text lines up in columns.
	/// </summary>
	Monospace
}
