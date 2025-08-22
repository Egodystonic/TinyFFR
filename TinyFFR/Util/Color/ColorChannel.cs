// Created on 2024-10-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

#pragma warning disable CA1027 //"Mark flags enums with Flags attribute" ... This isn't a bitfield enum
public enum ColorChannel {
	A = 0,
	R = Axis.X,
	G = Axis.Y,
	B = Axis.Z,
}
#pragma warning restore CA1027