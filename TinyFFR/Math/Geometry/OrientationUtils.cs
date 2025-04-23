// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

// TODO document that all the collections do not include the None value for their respective enum
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
	static readonly Axis[] _allAxes = { Axis.X, Axis.Y, Axis.Z };
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

	public static ReadOnlySpan<Orientation> All3DOrientations => _all3DOrientations;
	public static ReadOnlySpan<Axis> AllAxes => _allAxes;
	public static ReadOnlySpan<CardinalOrientation> AllCardinals => _allCardinals;
	public static ReadOnlySpan<IntercardinalOrientation> AllIntercardinals => _allIntercardinals;
	public static ReadOnlySpan<DiagonalOrientation> AllDiagonals => _allDiagonals;
	public static ReadOnlySpan<Orientation2D> All2DOrientations => _all2DOrientations;
	public static ReadOnlySpan<HorizontalOrientation2D> AllHorizontals => _allHorizontals;
	public static ReadOnlySpan<VerticalOrientation2D> AllVerticals => _allVerticals;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation CreateOrientation(XAxisOrientation xValue, YAxisOrientation yValue, ZAxisOrientation zValue) => xValue.Plus(yValue, zValue);
	
	public static Orientation CreateOrientationFromValueSigns<T>(T x, T y, T z) where T : INumber<T> => CreateOrientation(
		CreateXAxisOrientationFromValueSign(x),
		CreateYAxisOrientationFromValueSign(y),
		CreateZAxisOrientationFromValueSign(z)
	);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XAxisOrientation CreateXAxisOrientationFromValueSign<T>(T v) where T : INumber<T> => Orientation3DExtensions.CreateXAxisOrientationFromValueSign(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static YAxisOrientation CreateYAxisOrientationFromValueSign<T>(T v) where T : INumber<T> => Orientation3DExtensions.CreateYAxisOrientationFromValueSign(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ZAxisOrientation CreateZAxisOrientationFromValueSign<T>(T v) where T : INumber<T> => Orientation3DExtensions.CreateZAxisOrientationFromValueSign(v);
}