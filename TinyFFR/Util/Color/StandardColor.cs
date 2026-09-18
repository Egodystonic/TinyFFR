// Created on 2024-10-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

/// <summary>
/// An enumeration of standard colours that can be implicitly converted to a <see cref="ColorVect"/>.
/// </summary>
public enum StandardColor : uint { // RGB 24-bit format
	// Dielectrics, Format RealWorld<Name>
	/// <summary>The approximate real-world surface colour of coal (<c>#323232</c>).</summary>
	RealWorldCoal = 0x323232,
	/// <summary>The approximate real-world surface colour of rubber (<c>#353535</c>).</summary>
	RealWorldRubber = 0x353535,
	/// <summary>The approximate real-world surface colour of mud (<c>#553D31</c>).</summary>
	RealWorldMud = 0x553D31,
	/// <summary>The approximate real-world surface colour of wood (<c>#875C3C</c>).</summary>
	RealWorldWood = 0x875C3C,
	/// <summary>The approximate real-world surface colour of vegetation/foliage (<c>#7B824E</c>).</summary>
	RealWorldVegetation = 0x7B824E,
	/// <summary>The approximate real-world surface colour of brick (<c>#947D75</c>).</summary>
	RealWorldBrick = 0x947D75,
	/// <summary>The approximate real-world surface colour of sand (<c>#B1A884</c>).</summary>
	RealWorldSand = 0xB1A884,
	/// <summary>The approximate real-world surface colour of concrete (<c>#C0BFBB</c>).</summary>
	RealWorldConcrete = 0xC0BFBB,

	// Conductors, Format RealWorldSpecular<Name>
	/// <summary>The approximate real-world specular (reflected highlight) colour of silver (<c>#F7F4E8</c>).</summary>
	RealWorldSpecularSilver = 0xF7F4E8,
	/// <summary>The approximate real-world specular (reflected highlight) colour of aluminum (<c>#E8EAEA</c>).</summary>
	RealWorldSpecularAluminum = 0xE8EAEA,
	/// <summary>The approximate real-world specular (reflected highlight) colour of titanium (<c>#C1BAAF</c>).</summary>
	RealWorldSpecularTitanium = 0xC1BAAF,
	/// <summary>The approximate real-world specular (reflected highlight) colour of iron (<c>#C4C6C6</c>).</summary>
	RealWorldSpecularIron = 0xC4C6C6,
	/// <summary>The approximate real-world specular (reflected highlight) colour of platinum (<c>#D3CEC6</c>).</summary>
	RealWorldSpecularPlatinum = 0xD3CEC6,
	/// <summary>The approximate real-world specular (reflected highlight) colour of gold (<c>#FFD891</c>).</summary>
	RealWorldSpecularGold = 0xFFD891,
	/// <summary>The approximate real-world specular (reflected highlight) colour of brass (<c>#F9E596</c>).</summary>
	RealWorldSpecularBrass = 0xF9E596,
	/// <summary>The approximate real-world specular (reflected highlight) colour of copper (<c>#F7BC9E</c>).</summary>
	RealWorldSpecularCopper = 0xF7BC9E,

	// Lights, Format Lighting<Name>
	/// <summary>The approximate colour cast by candlelight (<c>#FF8701</c>).</summary>
	LightingCandle = 0xFF8701,
	/// <summary>The approximate colour cast by a traditional incandescent light bulb (<c>#FFC180</c>).</summary>
	LightingIncandescentBulb = 0xFFC180,
	/// <summary>The approximate colour of sunlight at sunrise/sunset (<c>#FFA64C</c>).</summary>
	LightingSunRiseSet = 0xFFA64C,
	/// <summary>The approximate colour of direct sunlight at midday (<c>#FFE9D7</c>).</summary>
	LightingSunMidday = 0xFFE9D7,
	/// <summary>The approximate colour of ambient (indirect) daylight (<c>#FFF3F1</c>).</summary>
	LightingAmbientDaylight = 0xFFF3F1,
	/// <summary>The approximate colour of ambient light under an overcast sky (<c>#FAF6FF</c>).</summary>
	LightingAmbientOvercast = 0xFAF6FF,
	/// <summary>The approximate colour of ambient light in open shade (<c>#EBECFF</c>).</summary>
	LightingAmbientShaded = 0xEBECFF,

	// Html 4.01 Colours, Format <Name>
	/// <summary>The standard HTML/CSS colour keyword "white" (<c>#FFFFFF</c>).</summary>
	White = 0xFFFFFF,
	/// <summary>The standard HTML/CSS colour keyword "silver" (<c>#C0C0C0</c>).</summary>
	Silver = 0xC0C0C0,
	/// <summary>The standard HTML/CSS colour keyword "gray" (<c>#808080</c>).</summary>
	Gray = 0x808080,
	/// <summary>The standard HTML/CSS colour keyword "black" (<c>#000000</c>).</summary>
	Black = 0x000000,
	/// <summary>The standard HTML/CSS colour keyword "red" (<c>#FF0000</c>).</summary>
	Red = 0xFF0000,
	/// <summary>The standard HTML/CSS colour keyword "maroon" (<c>#800000</c>).</summary>
	Maroon = 0x800000,
	/// <summary>The standard HTML/CSS colour keyword "yellow" (<c>#FFFF00</c>).</summary>
	Yellow = 0xFFFF00,
	/// <summary>The standard HTML/CSS colour keyword "olive" (<c>#808000</c>).</summary>
	Olive = 0x808000,
	/// <summary>The standard HTML/CSS colour keyword "lime" (<c>#00FF00</c>).</summary>
	Lime = 0x00FF00,
	/// <summary>The standard HTML/CSS colour keyword "green" (<c>#008000</c>).</summary>
	Green = 0x008000,
	/// <summary>The standard HTML/CSS colour keyword "aqua" (<c>#00FFFF</c>).</summary>
	Aqua = 0x00FFFF,
	/// <summary>The standard HTML/CSS colour keyword "teal" (<c>#008080</c>).</summary>
	Teal = 0x008080,
	/// <summary>The standard HTML/CSS colour keyword "blue" (<c>#0000FF</c>).</summary>
	Blue = 0x0000FF,
	/// <summary>The standard HTML/CSS colour keyword "navy" (<c>#000080</c>).</summary>
	Navy = 0x000080,
	/// <summary>The standard HTML/CSS colour keyword "fuchsia" (<c>#FF00FF</c>).</summary>
	Fuchsia = 0xFF00FF,
	/// <summary>The standard HTML/CSS colour keyword "purple" (<c>#800080</c>).</summary>
	Purple = 0x800080
}

/// <summary>
/// A static class holding extension methods for <see cref="StandardColor"/>.
/// </summary>
public static class StandardColorExtensions {
	/// <summary>
	/// Converts this <see cref="StandardColor"/> to an equivalent <see cref="ColorVect"/>; equivalent to <see cref="ColorVect.FromStandardColor"/>.
	/// </summary>
	/// <param name="c">The colour to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ColorVect ToColorVect(this StandardColor c) => ColorVect.FromStandardColor(c);
}