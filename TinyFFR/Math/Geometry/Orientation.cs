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
public enum XAxisOrientation { // TODO mention in XMLDoc that's always safe to cast this to Orientation but not the other way around
	None = 0,
	Left = LeftBit,
	Right = RightBit,
}
[Flags]
public enum YAxisOrientation { // TODO mention in XMLDoc that's always safe to cast this to Orientation but not the other way around
	None = 0,
	Up = UpBit,
	Down = DownBit,
}
[Flags]
public enum ZAxisOrientation { // TODO mention in XMLDoc that's always safe to cast this to Orientation but not the other way around
	None = 0,
	Forward = ForwardBit,
	Backward = BackwardBit
}

[Flags]
public enum CardinalOrientation { // TODO mention in XMLDoc that's always safe to cast this to Orientation but not the other way around
	None = 0,
	Left = LeftBit,
	Right = RightBit,
	Up = UpBit,
	Down = DownBit,
	Forward = ForwardBit,
	Backward = BackwardBit
}

public enum IntercardinalOrientation { // TODO mention in XMLDoc that's always safe to cast this to Orientation but not the other way around
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

public enum DiagonalOrientation { // TODO mention in XMLDoc that's always safe to cast this to Orientation but not the other way around
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
public enum Orientation {
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
	public static Orientation AsGeneralOrientation(this CardinalOrientation @this) => (Orientation) @this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation AsGeneralOrientation(this IntercardinalOrientation @this) => (Orientation) @this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation AsGeneralOrientation(this DiagonalOrientation @this) => (Orientation) @this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CardinalOrientation AsCardinalOrientation(this XAxisOrientation @this) => (CardinalOrientation) @this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CardinalOrientation AsCardinalOrientation(this YAxisOrientation @this) => (CardinalOrientation) @this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CardinalOrientation AsCardinalOrientation(this ZAxisOrientation @this) => (CardinalOrientation) @this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation AsGeneralOrientation(this XAxisOrientation @this) => (Orientation) @this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation AsGeneralOrientation(this YAxisOrientation @this) => (Orientation) @this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation AsGeneralOrientation(this ZAxisOrientation @this) => (Orientation) @this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this XAxisOrientation @this, YAxisOrientation other) => (Orientation) ((int) @this | (int) other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this XAxisOrientation @this, ZAxisOrientation other) => (Orientation) ((int) @this | (int) other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this YAxisOrientation @this, XAxisOrientation other) => (Orientation) ((int) @this | (int) other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this YAxisOrientation @this, ZAxisOrientation other) => (Orientation) ((int) @this | (int) other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this ZAxisOrientation @this, YAxisOrientation other) => (Orientation) ((int) @this | (int) other);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this ZAxisOrientation @this, XAxisOrientation other) => (Orientation) ((int) @this | (int) other);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this XAxisOrientation @this, YAxisOrientation y, ZAxisOrientation z) => (Orientation) ((int) @this | (int) y | (int) z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this XAxisOrientation @this, ZAxisOrientation z, YAxisOrientation y) => (Orientation) ((int) @this | (int) y | (int) z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this YAxisOrientation @this, XAxisOrientation x, ZAxisOrientation z) => (Orientation) ((int) @this | (int) x | (int) z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this YAxisOrientation @this, ZAxisOrientation z, XAxisOrientation x) => (Orientation) ((int) @this | (int) x | (int) z);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this ZAxisOrientation @this, XAxisOrientation x, YAxisOrientation y) => (Orientation) ((int) @this | (int) x | (int) y);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this ZAxisOrientation @this, YAxisOrientation y, XAxisOrientation x) => (Orientation) ((int) @this | (int) x | (int) y);

	public static CardinalOrientation ToCardinal(this Axis @this, int sign) {
		var leadingZeroes = (uint) Int32.LeadingZeroCount(sign);
		return (CardinalOrientation) (((sign & 0x8000_0000) >> (31 - NegativeDirectionBitShift)) | ((~leadingZeroes & 0b10_0000) >> (6 - PositiveDirectionBitShift)));
	}

	public static bool IsCardinal(this Orientation @this) => Int32.PopCount((int) @this) == 1; // TODO in XMLDoc make it clear that "None" does not count for this
	public static bool IsIntercardinal(this Orientation @this) => Int32.PopCount((int) @this) == 2; // TODO in XMLDoc make it clear that "None" does not count for this
	public static bool IsDiagonal(this Orientation @this) => Int32.PopCount((int) @this) == 3; // TODO in XMLDoc make it clear that "None" does not count for this

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this CardinalOrientation @this) => Direction.FromOrientation(@this.AsGeneralOrientation());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this IntercardinalOrientation @this) => Direction.FromOrientation(@this.AsGeneralOrientation());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this DiagonalOrientation @this) => Direction.FromOrientation(@this.AsGeneralOrientation());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this XAxisOrientation @this) => Direction.FromOrientation(@this.AsGeneralOrientation());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this YAxisOrientation @this) => Direction.FromOrientation(@this.AsGeneralOrientation());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this ZAxisOrientation @this) => Direction.FromOrientation(@this.AsGeneralOrientation());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this Orientation @this) => Direction.FromOrientation(@this);

	public static Axis GetAxis(this CardinalOrientation @this) {
		// This mask comes from saying we want the TZCNT of @this, but only care about the bottom five bits (e.g. 0b1_1111) as TZCNT should be < 32 for valid values or 32 (e.g. 0b10_0000) if @this is None.
		// We then knock off the lsb to "join" related pairs of bits (e.g. at time of writing 2 or 3 become 2 (Axis.X), 4 or 5 become 4 (Axis.Y), 6 or 7 become 6 (Axis.Z));
		// and the value will simply remain at 0 if TZCNT was 32 (indicating @this is None), meaning we'll return Axis.None.
		// This relies on the shift amount for each axis being a positive value. If in future we need to make them odd, it might be as simple as instead OR'ing the result with 0b1 instead, but not sure.
		const int TrailingZeroCountMask = 0b1_1110;
		return (Axis) (Int32.TrailingZeroCount((int) @this) & TrailingZeroCountMask);
	}

	public static Axis GetUnspecifiedAxis(this IntercardinalOrientation @this) { // TODO document that this returns None if @this is None
		var xBit = @this.GetAxisSign(Axis.X) & 0b1;
		var yBit = @this.GetAxisSign(Axis.Y) & 0b1;
		var zBit = @this.GetAxisSign(Axis.Z) & 0b1;
		var result = XAxisShift * (1 - xBit) + YAxisShift * (1 - yBit) + ZAxisShift * (1 - zBit);
		var noneBit = result & 0b1000;
		result &= ~(noneBit | noneBit >> 1 | noneBit >> 2 | noneBit >> 3);
		return (Axis) result;
	}

	public static XAxisOrientation GetXAxis(this Orientation @this) => (XAxisOrientation) (((int) @this) & XAxisBitMask);
	public static YAxisOrientation GetYAxis(this Orientation @this) => (YAxisOrientation) (((int) @this) & YAxisBitMask);
	public static ZAxisOrientation GetZAxis(this Orientation @this) => (ZAxisOrientation) (((int) @this) & ZAxisBitMask);
	public static XAxisOrientation GetXAxis(this IntercardinalOrientation @this) => @this.AsGeneralOrientation().GetXAxis();
	public static YAxisOrientation GetYAxis(this IntercardinalOrientation @this) => @this.AsGeneralOrientation().GetYAxis();
	public static ZAxisOrientation GetZAxis(this IntercardinalOrientation @this) => @this.AsGeneralOrientation().GetZAxis();
	public static XAxisOrientation GetXAxis(this DiagonalOrientation @this) => @this.AsGeneralOrientation().GetXAxis();
	public static YAxisOrientation GetYAxis(this DiagonalOrientation @this) => @this.AsGeneralOrientation().GetYAxis();
	public static ZAxisOrientation GetZAxis(this DiagonalOrientation @this) => @this.AsGeneralOrientation().GetZAxis();

	public static int GetAxisSign(this Orientation @this, Axis axis) {
		var directionalBits = ((int) @this) >> ((int) axis);
		return ((directionalBits & PositiveDirectionBitMask) >> PositiveDirectionBitShift) - ((directionalBits & NegativeDirectionBitMask) >> NegativeDirectionBitShift);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetAxisSign(this IntercardinalOrientation @this, Axis axis) => GetAxisSign(@this.AsGeneralOrientation(), axis);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetAxisSign(this DiagonalOrientation @this, Axis axis) => GetAxisSign(@this.AsGeneralOrientation(), axis);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetAxisSign(this CardinalOrientation @this, Axis axis) => GetAxisSign(@this.AsGeneralOrientation(), axis);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetAxisSign(this CardinalOrientation @this) => GetAxisSign(@this.AsGeneralOrientation(), @this.GetAxis());

	public static int GetAxisSign(this XAxisOrientation @this) {
		var intThis = ((int) @this) >> XAxisShift;
		return ((intThis & PositiveDirectionBitMask) >> PositiveDirectionBitShift) - ((intThis & NegativeDirectionBitMask) >> NegativeDirectionBitShift);
	}
	public static int GetAxisSign(this YAxisOrientation @this) {
		var intThis = ((int) @this) >> YAxisShift;
		return ((intThis & PositiveDirectionBitMask) >> PositiveDirectionBitShift) - ((intThis & NegativeDirectionBitMask) >> NegativeDirectionBitShift);
	}
	public static int GetAxisSign(this ZAxisOrientation @this) {
		var intThis = ((int) @this) >> ZAxisShift;
		return ((intThis & PositiveDirectionBitMask) >> PositiveDirectionBitShift) - ((intThis & NegativeDirectionBitMask) >> NegativeDirectionBitShift);
	}

	public static Orientation WithAxisSign(this Orientation @this, Axis axis, int sign) {
		var intThis = (int) @this;
		var intAxis = (int) axis;
		var secondBit = sign & 0b10;
		var signBits = secondBit | ((sign & 0b1) ^ (secondBit >> 1));
		return (Orientation) ((intThis & ~(0b11 << intAxis)) | ((signBits << intAxis) & ~0b11)); // Second mask against ~0b11 makes this have no effect when axis is None
	}

	internal static XAxisOrientation CreateXAxisOrientationFromValueSign<T>(T v) where T : INumber<T> {
		var sign = T.Sign(v);
		var bits = ((sign & 0x8000_0000) >> (31 - NegativeDirectionBitShift));
		bits |= (sign & 0b1 & ~(bits >> NegativeDirectionBitShift));

		return (XAxisOrientation) (bits << XAxisShift);
	}

	internal static YAxisOrientation CreateYAxisOrientationFromValueSign<T>(T v) where T : INumber<T> {
		var sign = T.Sign(v);
		var bits = ((sign & 0x8000_0000) >> (31 - NegativeDirectionBitShift));
		bits |= (sign & 0b1 & ~(bits >> NegativeDirectionBitShift));

		return (YAxisOrientation) (bits << YAxisShift);
	}

	internal static ZAxisOrientation CreateZAxisOrientationFromValueSign<T>(T v) where T : INumber<T> {
		var sign = T.Sign(v);
		var bits = ((sign & 0x8000_0000) >> (31 - NegativeDirectionBitShift));
		bits |= (sign & 0b1 & ~(bits >> NegativeDirectionBitShift));

		return (ZAxisOrientation) (bits << ZAxisShift);
	}
}