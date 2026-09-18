// Created on 2026-07-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// The set of colours a debug primitive is drawn with.
/// </summary>
/// <remarks>
/// How many of the colours are used depends on the primitive: a point uses the primary for its body and the secondary for its outline, whilst a grid uses all three
/// for its axes, major lines and minor lines respectively.
/// </remarks>
/// <param name="PrimaryColor">The main colour of the primitive.</param>
/// <param name="SecondaryColor">The secondary colour, where the primitive uses one, or <see langword="null"/> to leave it at the default.</param>
/// <param name="TertiaryColor">The tertiary colour, where the primitive uses one, or <see langword="null"/> to leave it at the default.</param>
public readonly record struct PrimitivePaintbrush(ColorVect PrimaryColor, ColorVect? SecondaryColor, ColorVect? TertiaryColor) {
	/// <summary>
	/// Constructs a new <see cref="PrimitivePaintbrush"/> using a single colour.
	/// </summary>
	/// <param name="primaryColor">The main colour of the primitive.</param>
	public PrimitivePaintbrush(ColorVect primaryColor) : this(primaryColor, null, null) { }
	/// <summary>
	/// Constructs a new <see cref="PrimitivePaintbrush"/> using a primary and a secondary colour.
	/// </summary>
	/// <param name="primaryColor">The main colour of the primitive.</param>
	/// <param name="secondaryColor">The secondary colour, where the primitive uses one.</param>
	public PrimitivePaintbrush(ColorVect primaryColor, ColorVect secondaryColor) : this(primaryColor, secondaryColor, null) { }
}