// Created on 2024-02-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024
using static Egodystonic.TinyFFR.Bits;

namespace Egodystonic.TinyFFR;

file static class Bits {
	public const int RightBit = 0b1;
	public const int UpBit = 0b10;
	public const int LeftBit = 0b100;
	public const int DownBit = 0b1000;

	public const int HorizontalBits = RightBit | LeftBit;
	public const int VerticalBits = UpBit | DownBit;
}

[Flags]
public enum Orientation2DHorizontal { // TODO mention in XMLDoc that's always safe to cast this to Orientation2D but not the other way around
	None = 0,
	Right = RightBit,
	Left = LeftBit,
}

[Flags]
public enum Orientation2DVertical { // TODO mention in XMLDoc that's always safe to cast this to Orientation2D but not the other way around
	None = 0,
	Up = UpBit,
	Down = DownBit,
}

[Flags]
public enum Orientation2D {
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

public static class Orientation2DExtensions {
	public static Orientation2D Plus(this Orientation2DHorizontal @this, Orientation2DVertical verticalComponent) => (Orientation2D) ((int) @this | (int) verticalComponent);

	public static Orientation2D Plus(this Orientation2DVertical @this, Orientation2DHorizontal horizontalComponent) => (Orientation2D) ((int) @this | (int) horizontalComponent);

	public static Orientation2DHorizontal GetHorizontalComponent(this Orientation2D @this) => (Orientation2DHorizontal) ((int) @this & HorizontalBits);
	public static Orientation2DVertical GetVerticalComponent(this Orientation2D @this) => (Orientation2DVertical) ((int) @this & VerticalBits);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Angle? ToAngle(this Orientation2D @this) => Angle.FromPolarAngleAround2DPlane(@this);
}