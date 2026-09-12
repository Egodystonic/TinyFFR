// Created on 2024-10-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

#pragma warning disable CA1027 //"Mark flags enums with Flags attribute" ... This isn't a bitfield enum
/// <summary>
/// Enumeration of the colour channels generally found in texel data (R, G, B, A).
/// </summary>
public enum ColorChannel {
	/// <summary>
	/// Alpha channel
	/// </summary>
	A = 0,
	/// <summary>
	/// Red channel
	/// </summary>
	R = Axis.X,
	/// <summary>
	/// Green channel
	/// </summary>
	G = Axis.Y,
	/// <summary>
	/// Blue channel
	/// </summary>
	B = Axis.Z,
}
#pragma warning restore CA1027