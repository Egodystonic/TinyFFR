// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using static Egodystonic.TinyFFR.DirectionalBits;

namespace Egodystonic.TinyFFR;

file static class DirectionalBits {
	// Important -- Less significant bit in each axis pair (e.g. Left in Left/Right) should correspond to positive values in world (e.g. positive X means left Direction)
	// This makes GetAxisSign() below work
	public const int LeftBit = 0b1;
	public const int RightBit = 0b10;
	public const int UpBit = 0b100;
	public const int DownBit = 0b1000;
	public const int ForwardBit = 0b1_0000;
	public const int BackwardBit = 0b10_0000;

	public const int XAxisBits = LeftBit | RightBit;
	public const int YAxisBits = UpBit | DownBit;
	public const int ZAxisBits = ForwardBit | BackwardBit;
}

public enum Axis {
	None = 0,
	X = XAxisBits,
	Y = YAxisBits,
	Z = ZAxisBits
}

[Flags]
public enum Orientation3DCardinal { // TODO mention in XMLDoc that's always safe to cast this to Orientation3D but not the other way around
	None = 0,
	Left = LeftBit,
	Right = RightBit,
	Up = UpBit,
	Down = DownBit,
	Forward = ForwardBit,
	Backward = BackwardBit
}

public enum Orientation3DDiagonal { // TODO mention in XMLDoc that's always safe to cast this to Orientation3D but not the other way around
	None = 0,
	UpLeftForward = UpBit | LeftBit | ForwardBit,
	UpRightForward = UpBit | RightBit | ForwardBit,
	UpLeftBackward = UpBit | LeftBit | BackwardBit,
	UpRightBackward = UpBit | RightBit | BackwardBit,
	DownLeftForward = DownBit | LeftBit | ForwardBit,
	DownRightForward = DownBit | RightBit | ForwardBit,
	DownLeftBackward = DownBit | LeftBit | BackwardBit,
	DownRightBackward = DownBit | RightBit | BackwardBit,
}

[Flags]
public enum Orientation3D {
	None = 0,

	Left = Orientation3DCardinal.Left,
	Right = Orientation3DCardinal.Right,
	Up = Orientation3DCardinal.Up,
	Down = Orientation3DCardinal.Down,
	Forward = Orientation3DCardinal.Forward,
	Backward = Orientation3DCardinal.Backward,

	UpLeftForward = Orientation3DDiagonal.UpLeftForward,
	UpRightForward = Orientation3DDiagonal.UpRightForward,
	UpLeftBackward = Orientation3DDiagonal.UpLeftBackward,
	UpRightBackward = Orientation3DDiagonal.UpRightBackward,
	DownLeftForward = Orientation3DDiagonal.DownLeftForward,
	DownRightForward = Orientation3DDiagonal.DownRightForward,
	DownLeftBackward = Orientation3DDiagonal.DownLeftBackward,
	DownRightBackward = Orientation3DDiagonal.DownRightBackward,
}

public static class Direction3dExtensions {
	public static bool IsCardinal(this Orientation3D @this) => Int32.PopCount((int) @this) == 1; // TODO in XMLDoc make it clear that "None" does not count for this
	public static bool IsDiagonal(this Orientation3D @this) => Int32.PopCount((int) @this) == 3; // TODO in XMLDoc make it clear that "None" does not count for this

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this Orientation3D @this) => Direction.FromOrientation(@this);

	public static Axis GetAxis(this Orientation3DCardinal @this) {
		return @this switch {
			Orientation3DCardinal.Left or Orientation3DCardinal.Right => Axis.X,
			Orientation3DCardinal.Up or Orientation3DCardinal.Down => Axis.Y,
			Orientation3DCardinal.Forward or Orientation3DCardinal.Backward => Axis.Z,
			_ => Axis.None
		};
	}

	internal static int GetAxisSign(this Orientation3D @this, Axis axis) {
		return 1 - ((Int32.TrailingZeroCount((int) @this & (int) axis) & 0b1) << 1);
	}
}