// Created on 2026-07-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Assets.Text;

/// <summary>
/// Describes how a block of text should be sized and placed when it is drawn.
/// </summary>
/// <remarks>
/// <para>
/// The text itself is measured from the font and the string, and this then scales that measurement to fit whatever
/// <see cref="Width"/> and <see cref="Height"/> are given. Leaving either <see langword="null"/> lets that axis size itself
/// from the text, so supplying only a height is the usual way to say "this tall, as wide as it needs to be".
/// </para>
/// <para>
/// Sizes are in world units (metres) for text placed in a scene.
/// </para>
/// </remarks>
/// <param name="Height">How tall the text block should be, or <see langword="null"/> to let the height follow from the text and
/// the width.</param>
/// <param name="PositionAnchor">Which point of the text block is placed at the position it is given (or the centre if
/// <see cref="Orientation2D.None"/>). For example <see cref="Orientation2D.UpLeft"/> puts the block's top-left corner at that
/// position, so the text extends right and downward from it.</param>
/// <param name="Width">How wide the text block should be, or <see langword="null"/> to let the width follow from the text and
/// the height.</param>
/// <param name="DisableAutomaticLineCountBasedHeightScaling">Whether to stop the block growing taller as its text wraps on to
/// more lines. Leave this <see langword="false"/> for a label that should always show all its text; set it <see langword="true"/>
/// where the block must keep a fixed height, accepting that multi-line text may be squashed.</param>
public readonly record struct TextLayout(float? Height, Orientation2D PositionAnchor, float? Width, bool DisableAutomaticLineCountBasedHeightScaling) {
	/// <summary>
	/// Constructs a new <see cref="TextLayout"/> of the given height, centred on its position, with its width following from
	/// the text and automatic line-count height scaling left enabled.
	/// </summary>
	/// <param name="height">How tall the text block should be.</param>
	public TextLayout(float height) : this(height, Orientation2D.None, null, false) { }
	/// <summary>
	/// Constructs a new <see cref="TextLayout"/> of the given height and anchor, with its width following from the text and
	/// automatic line-count height scaling left enabled.
	/// </summary>
	/// <param name="height">How tall the text block should be.</param>
	/// <param name="positionAnchor">Which point of the text block is placed at the position it is given (or the centre if
	/// <see cref="Orientation2D.None"/>).</param>
	public TextLayout(float height, Orientation2D positionAnchor) : this(height, positionAnchor, null, false) { }
}
