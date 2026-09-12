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

/// <summary>
/// Identifies one of the three spatial axes (X, Y, or Z), independent of sign/direction.
/// </summary>
#pragma warning disable CA1027 //"Mark flags enums with Flags attribute" ... This isn't a bitfield enum
public enum Axis {
	/// <summary>
	/// No axis.
	/// </summary>
	None = 0,
	/// <summary>
	/// The X axis (<see cref="Direction.Left"/>/<see cref="Direction.Right"/>).
	/// </summary>
	X = XAxisShift,
	/// <summary>
	/// The Y axis (<see cref="Direction.Up"/>/<see cref="Direction.Down"/>).
	/// </summary>
	Y = YAxisShift,
	/// <summary>
	/// The Z axis (<see cref="Direction.Forward"/>/<see cref="Direction.Backward"/>).
	/// </summary>
	Z = ZAxisShift
}
#pragma warning restore CA1027

/// <summary>
/// Identifies a direction along the X axis: <see cref="Left"/>, <see cref="Right"/>, or <see cref="None"/>.
/// </summary>
/// <remarks>
/// Any value of this enum can be safely cast to <see cref="Orientation"/>, but it is not safe to cast back to this type from <see cref="Orientation" />.
/// </remarks>
[Flags]
public enum XAxisOrientation {
	/// <summary>
	/// No orientation along the X axis.
	/// </summary>
	None = 0,
	/// <summary>
	/// Corresponds to <see cref="Direction.Left"/>.
	/// </summary>
	Left = LeftBit,
	/// <summary>
	/// Corresponds to <see cref="Direction.Right"/>.
	/// </summary>
	Right = RightBit,
}
/// <summary>
/// Identifies a direction along the Y axis: <see cref="Up"/>, <see cref="Down"/>, or <see cref="None"/>.
/// </summary>
/// <remarks>
/// Any value of this enum can be safely cast to <see cref="Orientation"/>, but it is not safe to cast back to this type from <see cref="Orientation" />.
/// </remarks>
[Flags]
public enum YAxisOrientation {
	/// <summary>
	/// No orientation along the Y axis.
	/// </summary>
	None = 0,
	/// <summary>
	/// Corresponds to <see cref="Direction.Up"/>.
	/// </summary>
	Up = UpBit,
	/// <summary>
	/// Corresponds to <see cref="Direction.Down"/>.
	/// </summary>
	Down = DownBit,
}
/// <summary>
/// Identifies a direction along the Z axis: <see cref="Forward"/>, <see cref="Backward"/>, or <see cref="None"/>.
/// </summary>
/// <remarks>
/// Any value of this enum can be safely cast to <see cref="Orientation"/>, but it is not safe to cast back to this type from <see cref="Orientation" />.
/// </remarks>
[Flags]
public enum ZAxisOrientation {
	/// <summary>
	/// No orientation along the Z axis.
	/// </summary>
	None = 0,
	/// <summary>
	/// Corresponds to <see cref="Direction.Forward"/>.
	/// </summary>
	Forward = ForwardBit,
	/// <summary>
	/// Corresponds to <see cref="Direction.Backward"/>.
	/// </summary>
	Backward = BackwardBit
}

/// <summary>
/// Identifies one of the six axis-aligned directions (see <see cref="Direction.AllCardinals"/>), or <see cref="None"/>.
/// </summary>
/// <remarks>
/// Any value of this enum can be safely cast to <see cref="Orientation"/>, but it is not safe to cast back to this type from <see cref="Orientation" />.
/// </remarks>
[Flags]
public enum CardinalOrientation {
	/// <summary>No orientation.</summary>
	None = 0,
	/// <summary>Corresponds to <see cref="Direction.Left"/>.</summary>
	Left = LeftBit,
	/// <summary>Corresponds to <see cref="Direction.Right"/>.</summary>
	Right = RightBit,
	/// <summary>Corresponds to <see cref="Direction.Up"/>.</summary>
	Up = UpBit,
	/// <summary>Corresponds to <see cref="Direction.Down"/>.</summary>
	Down = DownBit,
	/// <summary>Corresponds to <see cref="Direction.Forward"/>.</summary>
	Forward = ForwardBit,
	/// <summary>Corresponds to <see cref="Direction.Backward"/>.</summary>
	Backward = BackwardBit
}

/// <summary>
/// Identifies one of the twelve directions exactly between two adjacent cardinal directions (see <see cref="Direction.AllIntercardinals"/>), or <see cref="None"/>.
/// </summary>
/// <remarks>
/// Any value of this enum can be safely cast to <see cref="Orientation"/>, but it is not safe to cast back to this type from <see cref="Orientation" />.
/// </remarks>
public enum IntercardinalOrientation {
	/// <summary>No orientation.</summary>
	None = 0,
	/// <summary>Combines <see cref="CardinalOrientation.Left"/> and <see cref="CardinalOrientation.Up"/>.</summary>
	LeftUp = LeftBit | UpBit,
	/// <summary>Combines <see cref="CardinalOrientation.Right"/> and <see cref="CardinalOrientation.Up"/>.</summary>
	RightUp = RightBit | UpBit,
	/// <summary>Combines <see cref="CardinalOrientation.Up"/> and <see cref="CardinalOrientation.Forward"/>.</summary>
	UpForward = UpBit | ForwardBit,
	/// <summary>Combines <see cref="CardinalOrientation.Up"/> and <see cref="CardinalOrientation.Backward"/>.</summary>
	UpBackward = UpBit | BackwardBit,
	/// <summary>Combines <see cref="CardinalOrientation.Left"/> and <see cref="CardinalOrientation.Down"/>.</summary>
	LeftDown = LeftBit | DownBit,
	/// <summary>Combines <see cref="CardinalOrientation.Right"/> and <see cref="CardinalOrientation.Down"/>.</summary>
	RightDown = RightBit | DownBit,
	/// <summary>Combines <see cref="CardinalOrientation.Down"/> and <see cref="CardinalOrientation.Forward"/>.</summary>
	DownForward = DownBit | ForwardBit,
	/// <summary>Combines <see cref="CardinalOrientation.Down"/> and <see cref="CardinalOrientation.Backward"/>.</summary>
	DownBackward = DownBit | BackwardBit,
	/// <summary>Combines <see cref="CardinalOrientation.Left"/> and <see cref="CardinalOrientation.Forward"/>.</summary>
	LeftForward = LeftBit | ForwardBit,
	/// <summary>Combines <see cref="CardinalOrientation.Left"/> and <see cref="CardinalOrientation.Backward"/>.</summary>
	LeftBackward = LeftBit | BackwardBit,
	/// <summary>Combines <see cref="CardinalOrientation.Right"/> and <see cref="CardinalOrientation.Forward"/>.</summary>
	RightForward = RightBit | ForwardBit,
	/// <summary>Combines <see cref="CardinalOrientation.Right"/> and <see cref="CardinalOrientation.Backward"/>.</summary>
	RightBackward = RightBit | BackwardBit,
}

/// <summary>
/// Identifies one of the eight corner-diagonal directions (see <see cref="Direction.AllDiagonals"/>), or <see cref="None"/>.
/// </summary>
/// <remarks>
/// Any value of this enum can be safely cast to <see cref="Orientation"/>, but it is not safe to cast back to this type from <see cref="Orientation" />.
/// </remarks>
public enum DiagonalOrientation {
	/// <summary>No orientation.</summary>
	None = 0,
	/// <summary>Combines <see cref="CardinalOrientation.Left"/>, <see cref="CardinalOrientation.Up"/>, and <see cref="CardinalOrientation.Forward"/>.</summary>
	LeftUpForward = LeftBit | UpBit | ForwardBit,
	/// <summary>Combines <see cref="CardinalOrientation.Right"/>, <see cref="CardinalOrientation.Up"/>, and <see cref="CardinalOrientation.Forward"/>.</summary>
	RightUpForward = RightBit | UpBit | ForwardBit,
	/// <summary>Combines <see cref="CardinalOrientation.Left"/>, <see cref="CardinalOrientation.Up"/>, and <see cref="CardinalOrientation.Backward"/>.</summary>
	LeftUpBackward = LeftBit | UpBit | BackwardBit,
	/// <summary>Combines <see cref="CardinalOrientation.Right"/>, <see cref="CardinalOrientation.Up"/>, and <see cref="CardinalOrientation.Backward"/>.</summary>
	RightUpBackward = RightBit | UpBit | BackwardBit,
	/// <summary>Combines <see cref="CardinalOrientation.Left"/>, <see cref="CardinalOrientation.Down"/>, and <see cref="CardinalOrientation.Forward"/>.</summary>
	LeftDownForward = LeftBit | DownBit | ForwardBit,
	/// <summary>Combines <see cref="CardinalOrientation.Right"/>, <see cref="CardinalOrientation.Down"/>, and <see cref="CardinalOrientation.Forward"/>.</summary>
	RightDownForward = RightBit | DownBit | ForwardBit,
	/// <summary>Combines <see cref="CardinalOrientation.Left"/>, <see cref="CardinalOrientation.Down"/>, and <see cref="CardinalOrientation.Backward"/>.</summary>
	LeftDownBackward = LeftBit | DownBit | BackwardBit,
	/// <summary>Combines <see cref="CardinalOrientation.Right"/>, <see cref="CardinalOrientation.Down"/>, and <see cref="CardinalOrientation.Backward"/>.</summary>
	RightDownBackward = RightBit | DownBit | BackwardBit,
}

/// <summary>
/// Identifies any of the twenty-six cardinal, intercardinal, or diagonal directions (see <see cref="Direction.AllOrientations"/>), or <see cref="None"/>.
/// </summary>
[Flags]
public enum Orientation {
	/// <summary>No orientation.</summary>
	None = 0,

	/// <summary>Corresponds to <see cref="Direction.Left"/>.</summary>
	Left = LeftBit,
	/// <summary>Corresponds to <see cref="Direction.Right"/>.</summary>
	Right = RightBit,
	/// <summary>Corresponds to <see cref="Direction.Up"/>.</summary>
	Up = UpBit,
	/// <summary>Corresponds to <see cref="Direction.Down"/>.</summary>
	Down = DownBit,
	/// <summary>Corresponds to <see cref="Direction.Forward"/>.</summary>
	Forward = ForwardBit,
	/// <summary>Corresponds to <see cref="Direction.Backward"/>.</summary>
	Backward = BackwardBit,

	/// <summary>Combines <see cref="Left"/> and <see cref="Up"/>.</summary>
	LeftUp = LeftBit | UpBit,
	/// <summary>Combines <see cref="Right"/> and <see cref="Up"/>.</summary>
	RightUp = RightBit | UpBit,
	/// <summary>Combines <see cref="Up"/> and <see cref="Forward"/>.</summary>
	UpForward = UpBit | ForwardBit,
	/// <summary>Combines <see cref="Up"/> and <see cref="Backward"/>.</summary>
	UpBackward = UpBit | BackwardBit,
	/// <summary>Combines <see cref="Left"/> and <see cref="Down"/>.</summary>
	LeftDown = LeftBit | DownBit,
	/// <summary>Combines <see cref="Right"/> and <see cref="Down"/>.</summary>
	RightDown = RightBit | DownBit,
	/// <summary>Combines <see cref="Down"/> and <see cref="Forward"/>.</summary>
	DownForward = DownBit | ForwardBit,
	/// <summary>Combines <see cref="Down"/> and <see cref="Backward"/>.</summary>
	DownBackward = DownBit | BackwardBit,
	/// <summary>Combines <see cref="Left"/> and <see cref="Forward"/>.</summary>
	LeftForward = LeftBit | ForwardBit,
	/// <summary>Combines <see cref="Left"/> and <see cref="Backward"/>.</summary>
	LeftBackward = LeftBit | BackwardBit,
	/// <summary>Combines <see cref="Right"/> and <see cref="Forward"/>.</summary>
	RightForward = RightBit | ForwardBit,
	/// <summary>Combines <see cref="Right"/> and <see cref="Backward"/>.</summary>
	RightBackward = RightBit | BackwardBit,

	/// <summary>Combines <see cref="Left"/>, <see cref="Up"/>, and <see cref="Forward"/>.</summary>
	LeftUpForward = LeftBit | UpBit | ForwardBit,
	/// <summary>Combines <see cref="Right"/>, <see cref="Up"/>, and <see cref="Forward"/>.</summary>
	RightUpForward = RightBit | UpBit | ForwardBit,
	/// <summary>Combines <see cref="Left"/>, <see cref="Up"/>, and <see cref="Backward"/>.</summary>
	LeftUpBackward = LeftBit | UpBit | BackwardBit,
	/// <summary>Combines <see cref="Right"/>, <see cref="Up"/>, and <see cref="Backward"/>.</summary>
	RightUpBackward = RightBit | UpBit | BackwardBit,
	/// <summary>Combines <see cref="Left"/>, <see cref="Down"/>, and <see cref="Forward"/>.</summary>
	LeftDownForward = LeftBit | DownBit | ForwardBit,
	/// <summary>Combines <see cref="Right"/>, <see cref="Down"/>, and <see cref="Forward"/>.</summary>
	RightDownForward = RightBit | DownBit | ForwardBit,
	/// <summary>Combines <see cref="Left"/>, <see cref="Down"/>, and <see cref="Backward"/>.</summary>
	LeftDownBackward = LeftBit | DownBit | BackwardBit,
	/// <summary>Combines <see cref="Right"/>, <see cref="Down"/>, and <see cref="Backward"/>.</summary>
	RightDownBackward = RightBit | DownBit | BackwardBit,
}

/// <summary>
/// A static class housing extension methods for the 3D orientation enums (<see cref="Orientation"/>, <see cref="CardinalOrientation"/>, <see cref="IntercardinalOrientation"/>, <see cref="DiagonalOrientation"/>, <see cref="XAxisOrientation"/>, <see cref="YAxisOrientation"/>, <see cref="ZAxisOrientation"/>).
/// </summary>
public static class Orientation3DExtensions {
	/// <summary>
	/// Converts this orientation to the general-purpose <see cref="Orientation"/> type; equivalent to a direct cast.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation AsGeneralOrientation(this CardinalOrientation @this) => (Orientation) @this;

	/// <summary>
	/// Converts this orientation to the general-purpose <see cref="Orientation"/> type; equivalent to a direct cast.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation AsGeneralOrientation(this IntercardinalOrientation @this) => (Orientation) @this;

	/// <summary>
	/// Converts this orientation to the general-purpose <see cref="Orientation"/> type; equivalent to a direct cast.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation AsGeneralOrientation(this DiagonalOrientation @this) => (Orientation) @this;

	/// <summary>
	/// Converts this orientation to the equivalent <see cref="CardinalOrientation"/>; equivalent to a direct cast.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CardinalOrientation AsCardinalOrientation(this XAxisOrientation @this) => (CardinalOrientation) @this;

	/// <summary>
	/// Converts this orientation to the equivalent <see cref="CardinalOrientation"/>; equivalent to a direct cast.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CardinalOrientation AsCardinalOrientation(this YAxisOrientation @this) => (CardinalOrientation) @this;

	/// <summary>
	/// Converts this orientation to the equivalent <see cref="CardinalOrientation"/>; equivalent to a direct cast.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CardinalOrientation AsCardinalOrientation(this ZAxisOrientation @this) => (CardinalOrientation) @this;

	/// <summary>
	/// Converts this orientation to the general-purpose <see cref="Orientation"/> type; equivalent to a direct cast.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation AsGeneralOrientation(this XAxisOrientation @this) => (Orientation) @this;

	/// <summary>
	/// Converts this orientation to the general-purpose <see cref="Orientation"/> type; equivalent to a direct cast.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation AsGeneralOrientation(this YAxisOrientation @this) => (Orientation) @this;

	/// <summary>
	/// Converts this orientation to the general-purpose <see cref="Orientation"/> type; equivalent to a direct cast.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation AsGeneralOrientation(this ZAxisOrientation @this) => (Orientation) @this;

	/// <summary>
	/// Combines this orientation with <paramref name="other"/> in to a single <see cref="Orientation"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <param name="other">The orientation to combine with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this XAxisOrientation @this, YAxisOrientation other) => (Orientation) ((int) @this | (int) other);
	/// <summary>
	/// Combines this orientation with <paramref name="other"/> in to a single <see cref="Orientation"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <param name="other">The orientation to combine with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this XAxisOrientation @this, ZAxisOrientation other) => (Orientation) ((int) @this | (int) other);
	/// <summary>
	/// Combines this orientation with <paramref name="other"/> in to a single <see cref="Orientation"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <param name="other">The orientation to combine with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this YAxisOrientation @this, XAxisOrientation other) => (Orientation) ((int) @this | (int) other);
	/// <summary>
	/// Combines this orientation with <paramref name="other"/> in to a single <see cref="Orientation"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <param name="other">The orientation to combine with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this YAxisOrientation @this, ZAxisOrientation other) => (Orientation) ((int) @this | (int) other);
	/// <summary>
	/// Combines this orientation with <paramref name="other"/> in to a single <see cref="Orientation"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <param name="other">The orientation to combine with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this ZAxisOrientation @this, YAxisOrientation other) => (Orientation) ((int) @this | (int) other);
	/// <summary>
	/// Combines this orientation with <paramref name="other"/> in to a single <see cref="Orientation"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <param name="other">The orientation to combine with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this ZAxisOrientation @this, XAxisOrientation other) => (Orientation) ((int) @this | (int) other);

	/// <summary>
	/// Combines this orientation with <paramref name="y"/> and <paramref name="z"/> in to a single <see cref="Orientation"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <param name="y">The Y-axis orientation to combine with.</param>
	/// <param name="z">The Z-axis orientation to combine with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this XAxisOrientation @this, YAxisOrientation y, ZAxisOrientation z) => (Orientation) ((int) @this | (int) y | (int) z);
	/// <summary>
	/// Combines this orientation with <paramref name="z"/> and <paramref name="y"/> in to a single <see cref="Orientation"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <param name="z">The Z-axis orientation to combine with.</param>
	/// <param name="y">The Y-axis orientation to combine with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this XAxisOrientation @this, ZAxisOrientation z, YAxisOrientation y) => (Orientation) ((int) @this | (int) y | (int) z);
	/// <summary>
	/// Combines this orientation with <paramref name="x"/> and <paramref name="z"/> in to a single <see cref="Orientation"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <param name="x">The X-axis orientation to combine with.</param>
	/// <param name="z">The Z-axis orientation to combine with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this YAxisOrientation @this, XAxisOrientation x, ZAxisOrientation z) => (Orientation) ((int) @this | (int) x | (int) z);
	/// <summary>
	/// Combines this orientation with <paramref name="z"/> and <paramref name="x"/> in to a single <see cref="Orientation"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <param name="z">The Z-axis orientation to combine with.</param>
	/// <param name="x">The X-axis orientation to combine with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this YAxisOrientation @this, ZAxisOrientation z, XAxisOrientation x) => (Orientation) ((int) @this | (int) x | (int) z);
	/// <summary>
	/// Combines this orientation with <paramref name="x"/> and <paramref name="y"/> in to a single <see cref="Orientation"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <param name="x">The X-axis orientation to combine with.</param>
	/// <param name="y">The Y-axis orientation to combine with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this ZAxisOrientation @this, XAxisOrientation x, YAxisOrientation y) => (Orientation) ((int) @this | (int) x | (int) y);
	/// <summary>
	/// Combines this orientation with <paramref name="y"/> and <paramref name="x"/> in to a single <see cref="Orientation"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <param name="y">The Y-axis orientation to combine with.</param>
	/// <param name="x">The X-axis orientation to combine with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation Plus(this ZAxisOrientation @this, YAxisOrientation y, XAxisOrientation x) => (Orientation) ((int) @this | (int) x | (int) y);

	/// <summary>
	/// Converts this axis to a <see cref="CardinalOrientation"/>, using <paramref name="sign"/> to pick the positive or negative direction along the axis.
	/// </summary>
	/// <param name="this">The extended axis.</param>
	/// <param name="sign">The sign of the desired direction: positive values map to the axis's positive direction (e.g. <see cref="Direction.Up"/> for <see cref="Axis.Y"/>), negative values to its negative direction, and <c>0</c> to <see cref="CardinalOrientation.None"/>.</param>
	public static CardinalOrientation ToCardinal(this Axis @this, int sign) {
		var leadingZeroes = (uint) Int32.LeadingZeroCount(sign);
		return (CardinalOrientation) (((sign & 0x8000_0000) >> (31 - NegativeDirectionBitShift)) | ((~leadingZeroes & 0b10_0000) >> (6 - PositiveDirectionBitShift)));
	}

	/// <summary>
	/// Determines whether this orientation is one of the six cardinal directions (i.e. has exactly one axis component).
	/// </summary>
	/// <remarks>
	/// <see cref="Orientation.None"/> returns <see langword="false"/>.
	/// </remarks>
	/// <param name="this">The extended orientation.</param>
	public static bool IsCardinal(this Orientation @this) => Int32.PopCount((int) @this) == 1;
	/// <summary>
	/// Determines whether this orientation is one of the twelve intercardinal directions (i.e. has exactly two axis components).
	/// </summary>
	/// <remarks>
	/// <see cref="Orientation.None"/> returns <see langword="false"/>.
	/// </remarks>
	/// <param name="this">The extended orientation.</param>
	public static bool IsIntercardinal(this Orientation @this) => Int32.PopCount((int) @this) == 2;
	/// <summary>
	/// Determines whether this orientation is one of the eight diagonal directions (i.e. has exactly three axis components).
	/// </summary>
	/// <remarks>
	/// <see cref="Orientation.None"/> returns <see langword="false"/>.
	/// </remarks>
	/// <param name="this">The extended orientation.</param>
	public static bool IsDiagonal(this Orientation @this) => Int32.PopCount((int) @this) == 3;

	/// <summary>
	/// Converts this orientation to the equivalent <see cref="Direction"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this CardinalOrientation @this) => Direction.FromOrientation(@this.AsGeneralOrientation());

	/// <summary>
	/// Converts this orientation to the equivalent <see cref="Direction"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this IntercardinalOrientation @this) => Direction.FromOrientation(@this.AsGeneralOrientation());

	/// <summary>
	/// Converts this orientation to the equivalent <see cref="Direction"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this DiagonalOrientation @this) => Direction.FromOrientation(@this.AsGeneralOrientation());

	/// <summary>
	/// Converts this orientation to the equivalent <see cref="Direction"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this XAxisOrientation @this) => Direction.FromOrientation(@this.AsGeneralOrientation());

	/// <summary>
	/// Converts this orientation to the equivalent <see cref="Direction"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this YAxisOrientation @this) => Direction.FromOrientation(@this.AsGeneralOrientation());

	/// <summary>
	/// Converts this orientation to the equivalent <see cref="Direction"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this ZAxisOrientation @this) => Direction.FromOrientation(@this.AsGeneralOrientation());

	/// <summary>
	/// Converts this orientation to the equivalent <see cref="Direction"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Direction ToDirection(this Orientation @this) => Direction.FromOrientation(@this);

	/// <summary>
	/// Returns the axis this orientation lies along.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	public static Axis GetAxis(this CardinalOrientation @this) {
		// This mask comes from saying we want the TZCNT of @this, but only care about the bottom five bits (e.g. 0b1_1111) as TZCNT should be < 32 for valid values or 32 (e.g. 0b10_0000) if @this is None.
		// We then knock off the lsb to "join" related pairs of bits (e.g. at time of writing 2 or 3 become 2 (Axis.X), 4 or 5 become 4 (Axis.Y), 6 or 7 become 6 (Axis.Z));
		// and the value will simply remain at 0 if TZCNT was 32 (indicating @this is None), meaning we'll return Axis.None.
		// This relies on the shift amount for each axis being a positive value. If in future we need to make them odd, it might be as simple as instead OR'ing the result with 0b1 instead, but not sure.
		const int TrailingZeroCountMask = 0b1_1110;
		return (Axis) (Int32.TrailingZeroCount((int) @this) & TrailingZeroCountMask);
	}

	/// <summary>
	/// Returns the one axis this (intercardinal) orientation does not have a component along.
	/// </summary>
	/// <remarks>
	/// Returns <see cref="Axis.None"/> if this orientation is <see cref="IntercardinalOrientation.None"/>.
	/// </remarks>
	/// <param name="this">The extended orientation.</param>
	public static Axis GetUnspecifiedAxis(this IntercardinalOrientation @this) {
		var xBit = @this.GetAxisSign(Axis.X) & 0b1;
		var yBit = @this.GetAxisSign(Axis.Y) & 0b1;
		var zBit = @this.GetAxisSign(Axis.Z) & 0b1;
		var result = XAxisShift * (1 - xBit) + YAxisShift * (1 - yBit) + ZAxisShift * (1 - zBit);
		var noneBit = result & 0b1000;
		result &= ~(noneBit | noneBit >> 1 | noneBit >> 2 | noneBit >> 3);
		return (Axis) result;
	}

	/// <summary>
	/// Returns this orientation's component along the X axis.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	public static XAxisOrientation GetXAxis(this Orientation @this) => (XAxisOrientation) (((int) @this) & XAxisBitMask);
	/// <summary>
	/// Returns this orientation's component along the Y axis.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	public static YAxisOrientation GetYAxis(this Orientation @this) => (YAxisOrientation) (((int) @this) & YAxisBitMask);
	/// <summary>
	/// Returns this orientation's component along the Z axis.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	public static ZAxisOrientation GetZAxis(this Orientation @this) => (ZAxisOrientation) (((int) @this) & ZAxisBitMask);
	/// <summary>
	/// Returns this orientation's component along the X axis.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	public static XAxisOrientation GetXAxis(this IntercardinalOrientation @this) => @this.AsGeneralOrientation().GetXAxis();
	/// <summary>
	/// Returns this orientation's component along the Y axis.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	public static YAxisOrientation GetYAxis(this IntercardinalOrientation @this) => @this.AsGeneralOrientation().GetYAxis();
	/// <summary>
	/// Returns this orientation's component along the Z axis.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	public static ZAxisOrientation GetZAxis(this IntercardinalOrientation @this) => @this.AsGeneralOrientation().GetZAxis();
	/// <summary>
	/// Returns this orientation's component along the X axis.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	public static XAxisOrientation GetXAxis(this DiagonalOrientation @this) => @this.AsGeneralOrientation().GetXAxis();
	/// <summary>
	/// Returns this orientation's component along the Y axis.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	public static YAxisOrientation GetYAxis(this DiagonalOrientation @this) => @this.AsGeneralOrientation().GetYAxis();
	/// <summary>
	/// Returns this orientation's component along the Z axis.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	public static ZAxisOrientation GetZAxis(this DiagonalOrientation @this) => @this.AsGeneralOrientation().GetZAxis();

	/// <summary>
	/// Returns the sign of this orientation's component along <paramref name="axis"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <param name="axis">The axis to inspect.</param>
	/// <returns><c>1</c> if this orientation's component along <paramref name="axis"/> is positive, <c>-1</c> if negative, or <c>0</c> if neither (i.e. this orientation has no component along <paramref name="axis"/>, or <paramref name="axis"/> is <see cref="Axis.None"/>).</returns>
	public static int GetAxisSign(this Orientation @this, Axis axis) {
		var directionalBits = ((int) @this) >> ((int) axis);
		return ((directionalBits & PositiveDirectionBitMask) >> PositiveDirectionBitShift) - ((directionalBits & NegativeDirectionBitMask) >> NegativeDirectionBitShift);
	}

	/// <summary>
	/// Returns the sign of this orientation's component along <paramref name="axis"/>; equivalent to calling this on the equivalent general-purpose <see cref="Orientation"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <param name="axis">The axis to inspect.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetAxisSign(this IntercardinalOrientation @this, Axis axis) => GetAxisSign(@this.AsGeneralOrientation(), axis);

	/// <summary>
	/// Returns the sign of this orientation's component along <paramref name="axis"/>; equivalent to calling this on the equivalent general-purpose <see cref="Orientation"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <param name="axis">The axis to inspect.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetAxisSign(this DiagonalOrientation @this, Axis axis) => GetAxisSign(@this.AsGeneralOrientation(), axis);

	/// <summary>
	/// Returns the sign of this orientation's component along <paramref name="axis"/>; equivalent to calling this on the equivalent general-purpose <see cref="Orientation"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <param name="axis">The axis to inspect.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetAxisSign(this CardinalOrientation @this, Axis axis) => GetAxisSign(@this.AsGeneralOrientation(), axis);

	/// <summary>
	/// Returns the sign of this orientation along its own axis (see <see cref="GetAxis"/>).
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <returns><c>1</c> if this orientation is the positive direction along its axis, <c>-1</c> if negative, or <c>0</c> if this orientation is <see cref="CardinalOrientation.None"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetAxisSign(this CardinalOrientation @this) => GetAxisSign(@this.AsGeneralOrientation(), @this.GetAxis());

	/// <summary>
	/// Returns the sign of this orientation.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <returns><c>1</c> if this orientation is <see cref="XAxisOrientation.Left"/>, <c>-1</c> if <see cref="XAxisOrientation.Right"/>, or <c>0</c> if <see cref="XAxisOrientation.None"/>.</returns>
	public static int GetAxisSign(this XAxisOrientation @this) {
		var intThis = ((int) @this) >> XAxisShift;
		return ((intThis & PositiveDirectionBitMask) >> PositiveDirectionBitShift) - ((intThis & NegativeDirectionBitMask) >> NegativeDirectionBitShift);
	}
	/// <summary>
	/// Returns the sign of this orientation.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <returns><c>1</c> if this orientation is <see cref="YAxisOrientation.Up"/>, <c>-1</c> if <see cref="YAxisOrientation.Down"/>, or <c>0</c> if <see cref="YAxisOrientation.None"/>.</returns>
	public static int GetAxisSign(this YAxisOrientation @this) {
		var intThis = ((int) @this) >> YAxisShift;
		return ((intThis & PositiveDirectionBitMask) >> PositiveDirectionBitShift) - ((intThis & NegativeDirectionBitMask) >> NegativeDirectionBitShift);
	}
	/// <summary>
	/// Returns the sign of this orientation.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <returns><c>1</c> if this orientation is <see cref="ZAxisOrientation.Forward"/>, <c>-1</c> if <see cref="ZAxisOrientation.Backward"/>, or <c>0</c> if <see cref="ZAxisOrientation.None"/>.</returns>
	public static int GetAxisSign(this ZAxisOrientation @this) {
		var intThis = ((int) @this) >> ZAxisShift;
		return ((intThis & PositiveDirectionBitMask) >> PositiveDirectionBitShift) - ((intThis & NegativeDirectionBitMask) >> NegativeDirectionBitShift);
	}

	/// <summary>
	/// Returns this orientation with its component along <paramref name="axis"/> replaced according to <paramref name="sign"/>.
	/// </summary>
	/// <param name="this">The extended orientation.</param>
	/// <param name="axis">The axis to replace the component of. If this is <see cref="Axis.None"/>, this orientation is returned unchanged.</param>
	/// <param name="sign">The new sign for the component along <paramref name="axis"/>: positive for the positive direction, negative for the negative direction, or <c>0</c> to clear that axis's component entirely.</param>
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