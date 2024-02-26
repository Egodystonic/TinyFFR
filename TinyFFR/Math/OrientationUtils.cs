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

		Orientation3D.UpLeft,
		Orientation3D.UpRight,
		Orientation3D.UpBackward,
		Orientation3D.UpForward,
		Orientation3D.DownLeft,
		Orientation3D.DownRight,
		Orientation3D.DownBackward,
		Orientation3D.DownForward,
		Orientation3D.LeftForward,
		Orientation3D.LeftBackward,
		Orientation3D.RightForward,
		Orientation3D.RightBackward,

		Orientation3D.UpLeftForward,
		Orientation3D.UpRightForward,
		Orientation3D.UpLeftBackward,
		Orientation3D.UpRightBackward,
		Orientation3D.DownLeftForward,
		Orientation3D.DownRightForward,
		Orientation3D.DownLeftBackward,
		Orientation3D.DownRightBackward
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
		DiagonalOrientation3D.UpLeftForward,
		DiagonalOrientation3D.UpRightForward,
		DiagonalOrientation3D.UpLeftBackward,
		DiagonalOrientation3D.UpRightBackward,
		DiagonalOrientation3D.DownLeftForward,
		DiagonalOrientation3D.DownRightForward,
		DiagonalOrientation3D.DownLeftBackward,
		DiagonalOrientation3D.DownRightBackward
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

	public static Orientation3D CreateOrientation(XAxisOrientation3D xValue, YAxisOrientation3D yValue, ZAxisOrientation3D zValue) {
		return (Orientation3D) ((int) xValue | (int) yValue | (int) zValue);
	}

	public static XAxisOrientation3D CreateXAxisOrientationFromSign<T>(T v) where T : INumber<T> => (XAxisOrientation3D) (T.Sign(v) switch {
		1 => (0b1 << 0),
		-1 => (0b1 << 1),
		_ => 0
	});
	public static YAxisOrientation3D CreateYAxisOrientationFromSign<T>(T v) where T : INumber<T> => (YAxisOrientation3D) (T.Sign(v) switch {
		1 => (0b1 << 2),
		-1 => (0b1 << 3),
		_ => 0
	});
	public static ZAxisOrientation3D CreateZAxisOrientationFromSign<T>(T v) where T : INumber<T> => (ZAxisOrientation3D) (T.Sign(v) switch {
		1 => (0b1 << 4),
		-1 => (0b1 << 5),
		_ => 0
	});
}