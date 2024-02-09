// Created on 2024-02-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024
using static Egodystonic.TinyFFR.DirectionalBits;

namespace Egodystonic.TinyFFR;

file static class DirectionalBits {
	public const int RightBit = 0b1;
	public const int UpBit = 0b10;
	public const int LeftBit = 0b100;
	public const int DownBit = 0b1000;
}

[Flags]
public enum HorizontalDirection {
	None = 0,
	Right = RightBit,
	Left = LeftBit,
}

[Flags]
public enum VerticalDirection {
	None = 0,
	Up = UpBit,
	Down = DownBit,
}

[Flags]
public enum CardinalDirection {
	None = 0,
	Right = RightBit,
	UpRight = UpBit | RightBit,
	Up = UpBit,
	UpLeft = UpBit | LeftBit,
	Left = LeftBit,
	DownLeft = DownBit | LeftBit,
	Down = DownBit,
	DownRight = DownBit | RightBit
}