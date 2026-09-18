// Created on 2026-07-13 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Assets.Text;

/// <summary>
/// Describes how the lines of a block of text are aligned against each other horizontally.
/// </summary>
/// <remarks>
/// This only has a visible effect on text that occupies more than one line, as it controls where each line sits relative to
/// the others rather than where the block as a whole is placed.
/// </remarks>
public enum TextJustification {
	/// <summary>
	/// Every line is centred, so the block's ragged edges fall on both the left and the right.
	/// </summary>
	Center = 0,
	/// <summary>
	/// Every line starts at the same left-hand edge, so the block's ragged edge falls on the right.
	/// </summary>
	Left,
	/// <summary>
	/// Every line ends at the same right-hand edge, so the block's ragged edge falls on the left.
	/// </summary>
	Right
}
