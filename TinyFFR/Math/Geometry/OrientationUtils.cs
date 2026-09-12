// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

/// <summary>
/// A static class providing enumerable collections of the orientation enum values, and factory methods for constructing an <see cref="Orientation"/> from its axis components.
/// </summary>
public static class OrientationUtils {
	static readonly Orientation[] _all3DOrientations = {
		Orientation.Left,
		Orientation.Right,
		Orientation.Up,
		Orientation.Down,
		Orientation.Forward,
		Orientation.Backward,

		Orientation.LeftUp,
		Orientation.RightUp,
		Orientation.UpBackward,
		Orientation.UpForward,
		Orientation.LeftDown,
		Orientation.RightDown,
		Orientation.DownBackward,
		Orientation.DownForward,
		Orientation.LeftForward,
		Orientation.LeftBackward,
		Orientation.RightForward,
		Orientation.RightBackward,

		Orientation.LeftUpForward,
		Orientation.RightUpForward,
		Orientation.LeftUpBackward,
		Orientation.RightUpBackward,
		Orientation.LeftDownForward,
		Orientation.RightDownForward,
		Orientation.LeftDownBackward,
		Orientation.RightDownBackward
	};
	static readonly Axis[] _allAxes = {
		Axis.X, 
		Axis.Y, 
		Axis.Z
	};
	static readonly CardinalOrientation[] _allCardinals = {
		CardinalOrientation.Left,
		CardinalOrientation.Right,
		CardinalOrientation.Up,
		CardinalOrientation.Down,
		CardinalOrientation.Forward,
		CardinalOrientation.Backward
	};
	static readonly IntercardinalOrientation[] _allIntercardinals = {
		IntercardinalOrientation.LeftUp,
		IntercardinalOrientation.RightUp,
		IntercardinalOrientation.UpForward,
		IntercardinalOrientation.UpBackward,
		IntercardinalOrientation.LeftDown,
		IntercardinalOrientation.RightDown,
		IntercardinalOrientation.DownForward,
		IntercardinalOrientation.DownBackward,
		IntercardinalOrientation.LeftForward,
		IntercardinalOrientation.LeftBackward,
		IntercardinalOrientation.RightForward,
		IntercardinalOrientation.RightBackward,
	};
	static readonly DiagonalOrientation[] _allDiagonals = {
		DiagonalOrientation.LeftUpForward,
		DiagonalOrientation.RightUpForward,
		DiagonalOrientation.LeftUpBackward,
		DiagonalOrientation.RightUpBackward,
		DiagonalOrientation.LeftDownForward,
		DiagonalOrientation.RightDownForward,
		DiagonalOrientation.LeftDownBackward,
		DiagonalOrientation.RightDownBackward
	};

	static readonly Orientation2D[] _all2DOrientations = {
		Orientation2D.Right,
		Orientation2D.UpRight,
		Orientation2D.Up,
		Orientation2D.UpLeft,
		Orientation2D.Left,
		Orientation2D.DownLeft,
		Orientation2D.Down,
		Orientation2D.DownRight
	};
	static readonly HorizontalOrientation2D[] _allHorizontals = {
		HorizontalOrientation2D.Right,
		HorizontalOrientation2D.Left,
	};
	static readonly VerticalOrientation2D[] _allVerticals = {
		VerticalOrientation2D.Up,
		VerticalOrientation2D.Down,
	};
	static readonly DiagonalOrientation2D[] _all2DDiagonals = {
		DiagonalOrientation2D.UpRight,
		DiagonalOrientation2D.UpLeft,
		DiagonalOrientation2D.DownLeft,
		DiagonalOrientation2D.DownRight,
	};

	/// <summary>
	/// All twenty-six cardinal, intercardinal, and diagonal 3D orientations, excluding <see cref="Orientation.None"/>.
	/// </summary>
	public static ReadOnlySpan<Orientation> All3DOrientations => _all3DOrientations;
	/// <summary>
	/// All three spatial axes, excluding <see cref="Axis.None"/>.
	/// </summary>
	public static ReadOnlySpan<Axis> AllAxes => _allAxes;
	/// <summary>
	/// All six cardinal orientations, excluding <see cref="CardinalOrientation.None"/>.
	/// </summary>
	public static ReadOnlySpan<CardinalOrientation> AllCardinals => _allCardinals;
	/// <summary>
	/// All twelve intercardinal orientations, excluding <see cref="IntercardinalOrientation.None"/>.
	/// </summary>
	public static ReadOnlySpan<IntercardinalOrientation> AllIntercardinals => _allIntercardinals;
	/// <summary>
	/// All eight diagonal orientations, excluding <see cref="DiagonalOrientation.None"/>.
	/// </summary>
	public static ReadOnlySpan<DiagonalOrientation> AllDiagonals => _allDiagonals;
	/// <summary>
	/// All eight 2D orientations, excluding <see cref="Orientation2D.None"/>.
	/// </summary>
	public static ReadOnlySpan<Orientation2D> All2DOrientations => _all2DOrientations;
	/// <summary>
	/// Both horizontal 2D orientations, excluding <see cref="HorizontalOrientation2D.None"/>.
	/// </summary>
	public static ReadOnlySpan<HorizontalOrientation2D> AllHorizontals => _allHorizontals;
	/// <summary>
	/// Both vertical 2D orientations, excluding <see cref="VerticalOrientation2D.None"/>.
	/// </summary>
	public static ReadOnlySpan<VerticalOrientation2D> AllVerticals => _allVerticals;
	/// <summary>
	/// All four 2D diagonal orientations, excluding <see cref="DiagonalOrientation2D.None"/>.
	/// </summary>
	public static ReadOnlySpan<DiagonalOrientation2D> All2DDiagonals => _all2DDiagonals;

	/// <summary>
	/// Combines the given per-axis orientations in to a single <see cref="Orientation"/>; equivalent to <c>xValue.Plus(yValue, zValue)</c>.
	/// </summary>
	/// <param name="xValue">The X-axis component.</param>
	/// <param name="yValue">The Y-axis component.</param>
	/// <param name="zValue">The Z-axis component.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation CreateOrientation(XAxisOrientation xValue, YAxisOrientation yValue, ZAxisOrientation zValue) => xValue.Plus(yValue, zValue);

	/// <summary>
	/// Constructs an <see cref="Orientation"/> from the signs of <paramref name="x"/>, <paramref name="y"/>, and <paramref name="z"/>.
	/// </summary>
	/// <param name="x">A value whose sign determines the X-axis component (positive, negative, or zero).</param>
	/// <param name="y">A value whose sign determines the Y-axis component (positive, negative, or zero).</param>
	/// <param name="z">A value whose sign determines the Z-axis component (positive, negative, or zero).</param>
	public static Orientation CreateOrientationFromValueSigns<T>(T x, T y, T z) where T : INumber<T> => CreateOrientation(
		CreateXAxisOrientationFromValueSign(x),
		CreateYAxisOrientationFromValueSign(y),
		CreateZAxisOrientationFromValueSign(z)
	);

	/// <summary>
	/// Constructs an <see cref="XAxisOrientation"/> from the sign of <paramref name="v"/>.
	/// </summary>
	/// <param name="v">A value whose sign determines the resultant orientation (positive maps to <see cref="XAxisOrientation.Left"/>, negative to <see cref="XAxisOrientation.Right"/>, and zero to <see cref="XAxisOrientation.None"/>).</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XAxisOrientation CreateXAxisOrientationFromValueSign<T>(T v) where T : INumber<T> => Orientation3DExtensions.CreateXAxisOrientationFromValueSign(v);
	/// <summary>
	/// Constructs a <see cref="YAxisOrientation"/> from the sign of <paramref name="v"/>.
	/// </summary>
	/// <param name="v">A value whose sign determines the resultant orientation (positive maps to <see cref="YAxisOrientation.Up"/>, negative to <see cref="YAxisOrientation.Down"/>, and zero to <see cref="YAxisOrientation.None"/>).</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static YAxisOrientation CreateYAxisOrientationFromValueSign<T>(T v) where T : INumber<T> => Orientation3DExtensions.CreateYAxisOrientationFromValueSign(v);
	/// <summary>
	/// Constructs a <see cref="ZAxisOrientation"/> from the sign of <paramref name="v"/>.
	/// </summary>
	/// <param name="v">A value whose sign determines the resultant orientation (positive maps to <see cref="ZAxisOrientation.Forward"/>, negative to <see cref="ZAxisOrientation.Backward"/>, and zero to <see cref="ZAxisOrientation.None"/>).</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ZAxisOrientation CreateZAxisOrientationFromValueSign<T>(T v) where T : INumber<T> => Orientation3DExtensions.CreateZAxisOrientationFromValueSign(v);
}