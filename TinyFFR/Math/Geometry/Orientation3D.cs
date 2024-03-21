// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using static Egodystonic.TinyFFR.DirectionalBits;

namespace Egodystonic.TinyFFR;

file static class DirectionalBits {
	public const int PositiveDirectionBitShift = 0;
	public const int NegativeDirectionBitShift = 1;
	public const int PositiveDirectionBitMask = 0b1 << PositiveDirectionBitShift;
	public const int NegativeDirectionBitMask = 0b1 << NegativeDirectionBitShift;

	// These should probably remain even values. See GetAxis() implementation below for more info.
	public const int XAxisShift = 2;
	public const int YAxisShift = 4;
	public const int ZAxisShift = 6;

	public const int LeftBit = PositiveDirectionBitMask << XAxisShift;
	public const int RightBit = NegativeDirectionBitMask << XAxisShift;
	public const int UpBit = PositiveDirectionBitMask << YAxisShift;
	public const int DownBit = NegativeDirectionBitMask << YAxisShift;
	public const int ForwardBit = PositiveDirectionBitMask << ZAxisShift;
	public const int BackwardBit = NegativeDirectionBitMask << ZAxisShift;

	public const int XAxisBitMask = (PositiveDirectionBitMask | NegativeDirectionBitMask) << XAxisShift;
	public const int YAxisBitMask = (PositiveDirectionBitMask | NegativeDirectionBitMask) << YAxisShift;
	public const int ZAxisBitMask = (PositiveDirectionBitMask | NegativeDirectionBitMask) << ZAxisShift;
}

#pragma warning disable CA1027 //"Mark flags enums with Flags attribute" ... This isn't a bitfield enum
public enum Axis {
	None = 0,
	X = XAxisShift,
	Y = YAxisShift,
	Z = ZAxisShift
}
#pragma warning restore CA1027

[Flags]
public enum XAxisOrientation3D { // TODO mention in XMLDoc that's always safe to cast this to Orientation3D but not the other way around
	None = 0,
	Left = LeftBit,
	Right = RightBit,
}
[Flags]
public enum YAxisOrientation3D { // TODO mention in XMLDoc that's always safe to cast this to Orientation3D but not the other way around
	None = 0,
	Up = UpBit,
	Down = DownBit,
}
[Flags]
public enum ZAxisOrientation3D { // TODO mention in XMLDoc that's always safe to cast this to Orientation3D but not the other way around
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

public enum IntercardinalOrientation3D { // TODO mention in XMLDoc that's always safe to cast this to Orientation3D but not the other way around
	None = 0,
	LeftUp = LeftBit | UpBit,
	RightUp = RightBit | UpBit,
	UpForward = UpBit | ForwardBit,
	UpBackward = UpBit | BackwardBit,
	LeftDown = LeftBit | DownBit,
	RightDown = RightBit | DownBit,
	DownForward = DownBit | ForwardBit,
	DownBackward = DownBit | BackwardBit,
	LeftForward = LeftBit | ForwardBit,
	LeftBackward = LeftBit | BackwardBit,
	RightForward = RightBit | ForwardBit,
	RightBackward = RightBit | BackwardBit,
}

public enum DiagonalOrientation3D { // TODO mention in XMLDoc that's always safe to cast this to Orientation3D but not the other way around
	None = 0,
	LeftUpForward = LeftBit | UpBit | ForwardBit,
	RightUpForward = RightBit | UpBit | ForwardBit,
	LeftUpBackward = LeftBit | UpBit | BackwardBit,
	RightUpBackward = RightBit | UpBit | BackwardBit,
	LeftDownForward = LeftBit | DownBit | ForwardBit,
	RightDownForward = RightBit | DownBit | ForwardBit,
	LeftDownBackward = LeftBit | DownBit | BackwardBit,
	RightDownBackward = RightBit | DownBit | BackwardBit,
}

[Flags]
public enum Orientation3D {
	None = 0,

	Left = LeftBit,
	Right = RightBit,
	Up = UpBit,
	Down = DownBit,
	Forward = ForwardBit,
	Backward = BackwardBit,

	LeftUp = LeftBit | UpBit,
	RightUp = RightBit | UpBit,
	UpForward = UpBit | ForwardBit,
	UpBackward = UpBit | BackwardBit,
	LeftDown = LeftBit | DownBit,
	RightDown = RightBit | DownBit,
	DownForward = DownBit | ForwardBit,
	DownBackward = DownBit | BackwardBit,
	LeftForward = LeftBit | ForwardBit,
	LeftBackward = LeftBit | BackwardBit,
	RightForward = RightBit | ForwardBit,
	RightBackward = RightBit | BackwardBit,

	LeftUpForward = LeftBit | UpBit | ForwardBit,
	RightUpForward = RightBit | UpBit | ForwardBit,
	LeftUpBackward = LeftBit | UpBit | BackwardBit,
	RightUpBackward = RightBit | UpBit | BackwardBit,
	LeftDownForward = LeftBit | DownBit | ForwardBit,
	RightDownForward = RightBit | DownBit | ForwardBit,
	LeftDownBackward = LeftBit | DownBit | BackwardBit,
	RightDownBackward = RightBit | DownBit | BackwardBit,
}

public static class Orientation3DExtensions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D AsGeneralOrientation(this CardinalOrientation3D @this) => (Orientation3D) @this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D AsGeneralOrientation(this IntercardinalOrientation3D @this) => (Orientation3D) @this;

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
	public static Orientation3D Plus(this XAxisOrientation3D @this, YAxisOrientation3D other) => (Orientation3D) ((int) @this | (int) other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D Plus(this XAxisOrientation3D @this, ZAxisOrientation3D other) => (Orientation3D) ((int) @this | (int) other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D Plus(this YAxisOrientation3D @this, XAxisOrientation3D other) => (Orientation3D) ((int) @this | (int) other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D Plus(this YAxisOrientation3D @this, ZAxisOrientation3D other) => (Orientation3D) ((int) @this | (int) other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D Plus(this ZAxisOrientation3D @this, YAxisOrientation3D other) => (Orientation3D) ((int) @this | (int) other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D Plus(this ZAxisOrientation3D @this, XAxisOrientation3D other) => (Orientation3D) ((int) @this | (int) other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D Plus(this XAxisOrientation3D @this, YAxisOrientation3D y, ZAxisOrientation3D z) => (Orientation3D) ((int) @this | (int) y | (int) z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D Plus(this XAxisOrientation3D @this, ZAxisOrientation3D z, YAxisOrientation3D y) => (Orientation3D) ((int) @this | (int) y | (int) z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D Plus(this YAxisOrientation3D @this, XAxisOrientation3D x, ZAxisOrientation3D z) => (Orientation3D) ((int) @this | (int) x | (int) z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D Plus(this YAxisOrientation3D @this, ZAxisOrientation3D z, XAxisOrientation3D x) => (Orientation3D) ((int) @this | (int) x | (int) z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D Plus(this ZAxisOrientation3D @this, XAxisOrientation3D x, YAxisOrientation3D y) => (Orientation3D) ((int) @this | (int) x | (int) y);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D Plus(this ZAxisOrientation3D @this, YAxisOrientation3D y, XAxisOrientation3D x) => (Orientation3D) ((int) @this | (int) x | (int) y);

	public static CardinalOrientation3D ToCardinal(this Axis @this, int sign) {
		var leadingZeroes = (uint) Int32.LeadingZeroCount(sign);
		return (CardinalOrientation3D) (((sign & 0x8000_0000) >> (31 - NegativeDirectionBitShift)) | ((~leadingZeroes & 0b10_0000) >> (6 - PositiveDirectionBitShift)));
	}

	public static bool IsCardinal(this Orientation3D @this) => Int32.PopCount((int) @this) == 1; // TODO in XMLDoc make it clear that "None" does not count for this
	public static bool IsIntercardinal(this Orientation3D @this) => Int32.PopCount((int) @this) == 2; // TODO in XMLDoc make it clear that "None" does not count for this
	public static bool IsDiagonal(this Orientation3D @this) => Int32.PopCount((int) @this) == 3; // TODO in XMLDoc make it clear that "None" does not count for this

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this CardinalOrientation3D @this) => Direction.FromOrientation(@this.AsGeneralOrientation());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this IntercardinalOrientation3D @this) => Direction.FromOrientation(@this.AsGeneralOrientation());

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
		// This mask comes from saying we want the TZCNT of @this, but only care about the bottom five bits (e.g. 0b1_1111) as TZCNT should be < 32 for valid values or 32 (e.g. 0b10_0000) if @this is None.
		// We then knock off the lsb to "join" related pairs of bits (e.g. at time of writing 2 or 3 become 2 (Axis.X), 4 or 5 become 4 (Axis.Y), 6 or 7 become 6 (Axis.Z));
		// and the value will simply remain at 0 if TZCNT was 32 (indicating @this is None), meaning we'll return Axis.None.
		// This relies on the shift amount for each axis being a positive value. If in future we need to make them odd, it might be as simple as instead OR'ing the result with 0b1 instead, but not sure.
		const int TrailingZeroCountMask = 0b1_1110;
		return (Axis) (Int32.TrailingZeroCount((int) @this) & TrailingZeroCountMask);
	}

	public static Axis GetUnspecifiedAxis(this IntercardinalOrientation3D @this) { // TODO document that this returns None if @this is None
		return GetAxis((CardinalOrientation3D) ~(int) @this);
	}

	public static XAxisOrientation3D GetXAxis(this Orientation3D @this) => (XAxisOrientation3D) (((int) @this) & XAxisBitMask);
	public static YAxisOrientation3D GetYAxis(this Orientation3D @this) => (YAxisOrientation3D) (((int) @this) & YAxisBitMask);
	public static ZAxisOrientation3D GetZAxis(this Orientation3D @this) => (ZAxisOrientation3D) (((int) @this) & ZAxisBitMask);
	public static XAxisOrientation3D GetXAxis(this IntercardinalOrientation3D @this) => @this.AsGeneralOrientation().GetXAxis();
	public static YAxisOrientation3D GetYAxis(this IntercardinalOrientation3D @this) => @this.AsGeneralOrientation().GetYAxis();
	public static ZAxisOrientation3D GetZAxis(this IntercardinalOrientation3D @this) => @this.AsGeneralOrientation().GetZAxis();
	public static XAxisOrientation3D GetXAxis(this DiagonalOrientation3D @this) => @this.AsGeneralOrientation().GetXAxis();
	public static YAxisOrientation3D GetYAxis(this DiagonalOrientation3D @this) => @this.AsGeneralOrientation().GetYAxis();
	public static ZAxisOrientation3D GetZAxis(this DiagonalOrientation3D @this) => @this.AsGeneralOrientation().GetZAxis();

	public static int GetAxisSign(this Orientation3D @this, Axis axis) {
		var directionalBits = ((int) @this) >> ((int) axis);
		return ((directionalBits & PositiveDirectionBitMask) >> PositiveDirectionBitShift) - ((directionalBits & NegativeDirectionBitMask) >> NegativeDirectionBitShift);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetAxisSign(this IntercardinalOrientation3D @this, Axis axis) => GetAxisSign(@this.AsGeneralOrientation(), axis);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetAxisSign(this DiagonalOrientation3D @this, Axis axis) => GetAxisSign(@this.AsGeneralOrientation(), axis);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetAxisSign(this CardinalOrientation3D @this, Axis axis) => GetAxisSign(@this.AsGeneralOrientation(), axis);

	public static int GetAxisSign(this XAxisOrientation3D @this) {
		var intThis = ((int) @this) >> XAxisShift;
		return ((intThis & PositiveDirectionBitMask) >> PositiveDirectionBitShift) - ((intThis & NegativeDirectionBitMask) >> NegativeDirectionBitShift);
	}
	public static int GetAxisSign(this YAxisOrientation3D @this) {
		var intThis = ((int) @this) >> YAxisShift;
		return ((intThis & PositiveDirectionBitMask) >> PositiveDirectionBitShift) - ((intThis & NegativeDirectionBitMask) >> NegativeDirectionBitShift);
	}
	public static int GetAxisSign(this ZAxisOrientation3D @this) {
		var intThis = ((int) @this) >> ZAxisShift;
		return ((intThis & PositiveDirectionBitMask) >> PositiveDirectionBitShift) - ((intThis & NegativeDirectionBitMask) >> NegativeDirectionBitShift);
	}

	public static Orientation3D WithAxisSign(this Orientation3D @this, Axis axis, int sign) {
		var intThis = (int) @this;
		var intAxis = (int) axis;
		var secondBit = sign & 0b10;
		var signBits = secondBit | ((sign & 0b1) ^ (secondBit >> 1));
		// This line makes sure if axis is None we do nothing
		signBits &= (intAxis >> ZAxisShift) | (intAxis >> YAxisShift) | (intAxis >> XAxisShift);
		return (Orientation3D) ((intThis & ~(0b11 << intAxis)) | (signBits << intAxis));
	}

	internal static XAxisOrientation3D CreateXAxisOrientationFromValueSign<T>(T v) where T : INumber<T> {
		var sign = T.Sign(v);
		var bits = ((sign & 0x8000_0000) >> (31 - NegativeDirectionBitShift));
		bits |= (sign & 0b1 & ~(bits >> NegativeDirectionBitShift));

		return (XAxisOrientation3D) (bits << XAxisShift);
	}

	internal static YAxisOrientation3D CreateYAxisOrientationFromValueSign<T>(T v) where T : INumber<T> {
		var sign = T.Sign(v);
		var bits = ((sign & 0x8000_0000) >> (31 - NegativeDirectionBitShift));
		bits |= (sign & 0b1 & ~(bits >> NegativeDirectionBitShift));

		return (YAxisOrientation3D) (bits << YAxisShift);
	}

	internal static ZAxisOrientation3D CreateZAxisOrientationFromValueSign<T>(T v) where T : INumber<T> {
		var sign = T.Sign(v);
		var bits = ((sign & 0x8000_0000) >> (31 - NegativeDirectionBitShift));
		bits |= (sign & 0b1 & ~(bits >> NegativeDirectionBitShift));

		return (ZAxisOrientation3D) (bits << ZAxisShift);
	}
}