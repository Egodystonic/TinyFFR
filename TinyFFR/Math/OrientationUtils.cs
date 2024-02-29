// Created on 2024-02-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

// TODO document that all the collections do not include the None value for their respective enum
public static class OrientationUtils {
	static readonly Orientation3D[] _all3DOrientations = {
		Orientation3D.Left,
		Orientation3D.Right,
		Orientation3D.Up,
		Orientation3D.Down,
		Orientation3D.Forward,
		Orientation3D.Backward,

		Orientation3D.LeftUp,
		Orientation3D.RightUp,
		Orientation3D.UpBackward,
		Orientation3D.UpForward,
		Orientation3D.LeftDown,
		Orientation3D.RightDown,
		Orientation3D.DownBackward,
		Orientation3D.DownForward,
		Orientation3D.LeftForward,
		Orientation3D.LeftBackward,
		Orientation3D.RightForward,
		Orientation3D.RightBackward,

		Orientation3D.LeftUpForward,
		Orientation3D.RightUpForward,
		Orientation3D.LeftUpBackward,
		Orientation3D.RightUpBackward,
		Orientation3D.LeftDownForward,
		Orientation3D.RightDownForward,
		Orientation3D.LeftDownBackward,
		Orientation3D.RightDownBackward
	};
	static readonly Axis[] _allAxes = { Axis.X, Axis.Y, Axis.Z };
	static readonly CardinalOrientation3D[] _allCardinals = {
		CardinalOrientation3D.Left,
		CardinalOrientation3D.Right,
		CardinalOrientation3D.Up,
		CardinalOrientation3D.Down,
		CardinalOrientation3D.Forward,
		CardinalOrientation3D.Backward
	};
	static readonly DiagonalOrientation3D[] _allDiagonals = {
		DiagonalOrientation3D.LeftUpForward,
		DiagonalOrientation3D.RightUpForward,
		DiagonalOrientation3D.LeftUpBackward,
		DiagonalOrientation3D.RightUpBackward,
		DiagonalOrientation3D.LeftDownForward,
		DiagonalOrientation3D.RightDownForward,
		DiagonalOrientation3D.LeftDownBackward,
		DiagonalOrientation3D.RightDownBackward
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

	public static ReadOnlySpan<Orientation3D> All3DOrientations => _all3DOrientations;
	public static ReadOnlySpan<Axis> AllAxes => _allAxes;
	public static ReadOnlySpan<CardinalOrientation3D> AllCardinals => _allCardinals;
	public static ReadOnlySpan<DiagonalOrientation3D> AllDiagonals => _allDiagonals;
	public static ReadOnlySpan<Orientation2D> All2DOrientations => _all2DOrientations;
	public static ReadOnlySpan<HorizontalOrientation2D> AllHorizontals => _allHorizontals;
	public static ReadOnlySpan<VerticalOrientation2D> AllVerticals => _allVerticals;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Orientation3D CreateOrientation(XAxisOrientation3D xValue, YAxisOrientation3D yValue, ZAxisOrientation3D zValue) => xValue.Plus(yValue, zValue);
	
	public static Orientation3D CreateOrientationFromValueSigns<T>(T x, T y, T z) where T : INumber<T> => CreateOrientation(
		CreateXAxisOrientationFromValueSign(x),
		CreateYAxisOrientationFromValueSign(y),
		CreateZAxisOrientationFromValueSign(z)
	);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static XAxisOrientation3D CreateXAxisOrientationFromValueSign<T>(T v) where T : INumber<T> => Orientation3DExtensions.CreateXAxisOrientationFromValueSign(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static YAxisOrientation3D CreateYAxisOrientationFromValueSign<T>(T v) where T : INumber<T> => Orientation3DExtensions.CreateYAxisOrientationFromValueSign(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ZAxisOrientation3D CreateZAxisOrientationFromValueSign<T>(T v) where T : INumber<T> => Orientation3DExtensions.CreateZAxisOrientationFromValueSign(v);
}