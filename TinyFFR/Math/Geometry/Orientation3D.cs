// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using static Egodystonic.TinyFFR.DirectionalBits;

namespace Egodystonic.TinyFFR;

file static class DirectionalBits {
	// Important -- Less significant bit in each axis pair (e.g. Left in Left/Right) should correspond to positive values in world (e.g. positive X means left Direction)
	// This makes GetAxisSign() below work, and some stuff in OrientationUtils
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
public enum XAxisOrientation3D {
	None = 0,
	Left = LeftBit,
	Right = RightBit,
}
[Flags]
public enum YAxisOrientation3D {
	None = 0,
	Up = UpBit,
	Down = DownBit,
}
[Flags]
public enum ZAxisOrientation3D {
	None = 0,
	Forward = ForwardBit,
	Backward = BackwardBit
}

[Flags]
public enum CardinalOrientation3D { // TODO mention in XMLDoc that's always safe to cast this to Orientation3D but not the other way around
	None = 0,
	Left = LeftBit,
	Right = RightBit,
	Up = UpBit,
	Down = DownBit,
	Forward = ForwardBit,
	Backward = BackwardBit
}

public enum DiagonalOrientation3D { // TODO mention in XMLDoc that's always safe to cast this to Orientation3D but not the other way around
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

	Left = CardinalOrientation3D.Left,
	Right = CardinalOrientation3D.Right,
	Up = CardinalOrientation3D.Up,
	Down = CardinalOrientation3D.Down,
	Forward = CardinalOrientation3D.Forward,
	Backward = CardinalOrientation3D.Backward,

	UpLeft = CardinalOrientation3D.Up | CardinalOrientation3D.Left,
	UpRight = CardinalOrientation3D.Up | CardinalOrientation3D.Right,
	UpForward = CardinalOrientation3D.Up | CardinalOrientation3D.Forward,
	UpBackward = CardinalOrientation3D.Up | CardinalOrientation3D.Backward,
	DownLeft = CardinalOrientation3D.Down | CardinalOrientation3D.Left,
	DownRight = CardinalOrientation3D.Down | CardinalOrientation3D.Right,
	DownForward = CardinalOrientation3D.Down | CardinalOrientation3D.Forward,
	DownBackward = CardinalOrientation3D.Down | CardinalOrientation3D.Backward,
	LeftForward = CardinalOrientation3D.Left | CardinalOrientation3D.Forward,
	LeftBackward = CardinalOrientation3D.Left | CardinalOrientation3D.Backward,
	RightForward = CardinalOrientation3D.Right | CardinalOrientation3D.Forward,
	RightBackward = CardinalOrientation3D.Right | CardinalOrientation3D.Backward,

	UpLeftForward = DiagonalOrientation3D.UpLeftForward,
	UpRightForward = DiagonalOrientation3D.UpRightForward,
	UpLeftBackward = DiagonalOrientation3D.UpLeftBackward,
	UpRightBackward = DiagonalOrientation3D.UpRightBackward,
	DownLeftForward = DiagonalOrientation3D.DownLeftForward,
	DownRightForward = DiagonalOrientation3D.DownRightForward,
	DownLeftBackward = DiagonalOrientation3D.DownLeftBackward,
	DownRightBackward = DiagonalOrientation3D.DownRightBackward, // Also known as the Conservative Party orientation amirite
}

public static class Orientation3DExtensions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D AsGeneralOrientation(this CardinalOrientation3D @this) => (Orientation3D) @this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D AsGeneralOrientation(this DiagonalOrientation3D @this) => (Orientation3D) @this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CardinalOrientation3D AsCardinalOrientation(this XAxisOrientation3D @this) => (CardinalOrientation3D) @this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CardinalOrientation3D AsCardinalOrientation(this YAxisOrientation3D @this) => (CardinalOrientation3D) @this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CardinalOrientation3D AsCardinalOrientation(this ZAxisOrientation3D @this) => (CardinalOrientation3D) @this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D AsGeneralOrientation(this XAxisOrientation3D @this) => (Orientation3D) @this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D AsGeneralOrientation(this YAxisOrientation3D @this) => (Orientation3D) @this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D AsGeneralOrientation(this ZAxisOrientation3D @this) => (Orientation3D) @this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D Plus(this XAxisOrientation3D @this, YAxisOrientation3D y, ZAxisOrientation3D z) => OrientationUtils.CreateOrientation(@this, y, z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D Plus(this YAxisOrientation3D @this, XAxisOrientation3D x, ZAxisOrientation3D z) => OrientationUtils.CreateOrientation(x, @this, z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D Plus(this ZAxisOrientation3D @this, XAxisOrientation3D x, YAxisOrientation3D y) => OrientationUtils.CreateOrientation(x, y, @this);

	public static CardinalOrientation3D ToCardinal(this Axis @this, int sign) => @this switch {
		Axis.X => OrientationUtils.CreateXAxisOrientationFromSign(sign).AsCardinalOrientation(),
		Axis.Y => OrientationUtils.CreateYAxisOrientationFromSign(sign).AsCardinalOrientation(),
		Axis.Z => OrientationUtils.CreateZAxisOrientationFromSign(sign).AsCardinalOrientation(),
		_ => CardinalOrientation3D.None
	};

	public static bool IsCardinal(this Orientation3D @this) => Int32.PopCount((int) @this) == 1; // TODO in XMLDoc make it clear that "None" does not count for this
	public static bool IsDiagonal(this Orientation3D @this) => Int32.PopCount((int) @this) == 3; // TODO in XMLDoc make it clear that "None" does not count for this

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this CardinalOrientation3D @this) => Direction.FromOrientation(@this.AsGeneralOrientation());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this DiagonalOrientation3D @this) => Direction.FromOrientation(@this.AsGeneralOrientation());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this XAxisOrientation3D @this) => Direction.FromOrientation(@this.AsGeneralOrientation());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this YAxisOrientation3D @this) => Direction.FromOrientation(@this.AsGeneralOrientation());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this ZAxisOrientation3D @this) => Direction.FromOrientation(@this.AsGeneralOrientation());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this Orientation3D @this) => Direction.FromOrientation(@this);

	public static Axis GetAxis(this CardinalOrientation3D @this) {
		return @this switch {
			CardinalOrientation3D.Left or CardinalOrientation3D.Right => Axis.X,
			CardinalOrientation3D.Up or CardinalOrientation3D.Down => Axis.Y,
			CardinalOrientation3D.Forward or CardinalOrientation3D.Backward => Axis.Z,
			_ => Axis.None
		};
	}

	public static XAxisOrientation3D GetXAxis(this Orientation3D @this) => (XAxisOrientation3D) (((int) @this) & XAxisBits);
	public static YAxisOrientation3D GetYAxis(this Orientation3D @this) => (YAxisOrientation3D) (((int) @this) & YAxisBits);
	public static ZAxisOrientation3D GetZAxis(this Orientation3D @this) => (ZAxisOrientation3D) (((int) @this) & ZAxisBits);
	public static XAxisOrientation3D GetXAxis(this DiagonalOrientation3D @this) => @this.AsGeneralOrientation().GetXAxis();
	public static YAxisOrientation3D GetYAxis(this DiagonalOrientation3D @this) => @this.AsGeneralOrientation().GetYAxis();
	public static ZAxisOrientation3D GetZAxis(this DiagonalOrientation3D @this) => @this.AsGeneralOrientation().GetZAxis();

	public static int GetAxisSign(this Orientation3D @this, Axis axis) => axis switch {
		Axis.X => @this.GetXAxis().GetAxisSign(),
		Axis.Y => @this.GetYAxis().GetAxisSign(),
		Axis.Z => @this.GetZAxis().GetAxisSign(),
		_ => 0
	};
	public static int GetAxisSign(this XAxisOrientation3D @this) {
		var intThis = ((int) @this) >> 0;
		return (intThis & 0b1) - ((intThis & 0b10) >> 1);
	}
	public static int GetAxisSign(this YAxisOrientation3D @this) {
		var intThis = ((int) @this) >> 2;
		return (intThis & 0b1) - ((intThis & 0b10) >> 1);
	}
	public static int GetAxisSign(this ZAxisOrientation3D @this) {
		var intThis = ((int) @this) >> 4;
		return (intThis & 0b1) - ((intThis & 0b10) >> 1);
	}
}