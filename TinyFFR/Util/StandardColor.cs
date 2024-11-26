// Created on 2024-10-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public enum StandardColor : uint { // RGB 24-bit format
	// Dielectrics, Format RealWorld<Name>
	RealWorldCoal = 0x323232,
	RealWorldRubber = 0x353535,
	RealWorldMud = 0x553D31,
	RealWorldWood = 0x875C3C,
	RealWorldVegetation = 0x7B824E,
	RealWorldBrick = 0x947D75,
	RealWorldSand = 0xB1A884,
	RealWorldConcrete = 0xC0BFBB,

	// Conductors, Format RealWorldSpecular<Name>
	RealWorldSpecularSilver = 0xF7F4E8,
	RealWorldSpecularAluminum = 0xE8EAEA,
	RealWorldSpecularTitanium = 0xC1BAAF,
	RealWorldSpecularIron = 0xC4C6C6,
	RealWorldSpecularPlatinum = 0xD3CEC6,
	RealWorldSpecularGold = 0xFFD891,
	RealWorldSpecularBrass = 0xF9E596,
	RealWorldSpecularCopper = 0xF7BC9E,

	// Html 4.01 Colours, Format <Name>
	White = 0xFFFFFF,
	Silver = 0xC0C0C0,
	Gray = 0x808080,
	Black = 0x000000,
	Red = 0xFF0000,
	Maroon = 0x800000,
	Yellow = 0xFFFF00,
	Olive = 0x808000,
	Lime = 0x00FF00,
	Green = 0x008000,
	Aqua = 0x00FFFF,
	Teal = 0x008080,
	Blue = 0x0000FF,
	Navy = 0x000080,
	Fuchsia = 0xFF00FF,
	Purple = 0x800080
}

public static class StandardColorExtensions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect ToColorVect(this StandardColor c) => ColorVect.FromStandardColor(c);
}