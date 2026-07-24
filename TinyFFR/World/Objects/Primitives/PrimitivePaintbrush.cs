// Created on 2026-07-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.World;

public readonly record struct PrimitivePaintbrush(
	ColorVect PrimaryColor,
	ColorVect? SecondaryColor,
	float Size
) {
	public PrimitivePaintbrush(ColorVect primaryColor, ColorVect? secondaryColor, Magnitude size) : this(primaryColor, secondaryColor, ConvertMagnitudeToSize(size)) { }
	
	public PrimitivePaintbrush(ColorVect primaryColor) : this(primaryColor, null, Magnitude.Default) { }
	public PrimitivePaintbrush(ColorVect primaryColor, ColorVect? secondaryColor) : this(primaryColor, secondaryColor, Magnitude.Default) { }
	
	public PrimitivePaintbrush(float size) : this(ColorVect.WhiteOpaque, null, size) { }
	public PrimitivePaintbrush(Magnitude size) : this(ColorVect.WhiteOpaque, null, size) { }
	
	public PrimitivePaintbrush(ColorVect primaryColor, float size) : this(primaryColor, null, size) { }
	public PrimitivePaintbrush(ColorVect primaryColor, Magnitude size) : this(primaryColor, null, size) { }
	
	public static float ConvertMagnitudeToSize(Magnitude m) {
		return m switch {
			Magnitude.VerySmall => 0.0005f,
			Magnitude.Small => 0.0125f,
			Magnitude.Large => 0.0275f,
			Magnitude.VeryLarge => 0.035f,
			_ => 0.02f
		};
	}
}