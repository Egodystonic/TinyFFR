// Created on 2026-07-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.World;

public readonly record struct PrimitivePaintbrush(ColorVect PrimaryColor, ColorVect? SecondaryColor, ColorVect? TertiaryColor) {
	public PrimitivePaintbrush(ColorVect primaryColor) : this(primaryColor, null, null) { }
	public PrimitivePaintbrush(ColorVect primaryColor, ColorVect secondaryColor) : this(primaryColor, secondaryColor, null) { }
}