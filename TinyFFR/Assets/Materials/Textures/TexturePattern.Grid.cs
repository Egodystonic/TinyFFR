// Created on 2026-07-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using static Egodystonic.TinyFFR.Assets.Materials.TexturePatternDefaultValues;

namespace Egodystonic.TinyFFR.Assets.Materials;

public static unsafe partial class TexturePattern {
	/// <summary>
	/// Creates a square pattern of evenly-spaced horizontal and vertical lines over a background, like a sheet of graph paper.
	/// </summary>
	/// <remarks>
	/// The lines are laid out outwards from the centre of the pattern, so one horizontal and one vertical line always pass
	/// through its exact centre.
	/// </remarks>
	/// <typeparam name="T">The type of value this pattern produces at each texel.</typeparam>
	/// <param name="lineValue">The value of every line.</param>
	/// <param name="backgroundValue">The value of the space between lines.</param>
	/// <param name="lineSpacing">The distance between neighbouring lines, as a fraction of the pattern's width (e.g. <c>0.25f</c> for lines a quarter of the pattern apart). Defaults to <c>0.25f</c>. A value of <c>0f</c> or less draws only the two centre lines.</param>
	/// <param name="lineThickness">The thickness of every line, in texels. Defaults to <c>4</c>.</param>
	/// <param name="resolution">The width and height of the pattern, in texels. Defaults to <c>1024</c>.</param>
	/// <param name="transform">How to scale, rotate and shift the pattern, or <see langword="null"/> for no change. Scaling below <c>1f</c> squashes the pattern so it repeats more often; rotation turns it anticlockwise; translation shifts it. They are applied in that order.</param>
	public static TexturePattern<T> Grid<T>(T lineValue, T backgroundValue, float lineSpacing = GridDefaultMajorLineSpacing, int lineThickness = GridDefaultMajorLineThickness, int resolution = GridDefaultResolution, Transform2D? transform = null) where T : unmanaged {
		return Grid(
			centreLineValue: lineValue,
			majorLineValue: lineValue,
			minorLineValue: lineValue,
			backgroundValue: backgroundValue,
			majorLineSpacing: lineSpacing,
			minorLineSpacing: 0f,
			centreLineThickness: lineThickness,
			majorLineThickness: lineThickness,
			minorLineThickness: 0,
			resolution: resolution,
			transform: transform
		);
	}

	/// <summary>
	/// Creates a square pattern of evenly-spaced horizontal and vertical lines over a background, with separate values for the
	/// two centre lines, the widely-spaced major lines, and the closely-spaced minor lines between them.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The lines are laid out outwards from the centre of the pattern, so the two centre lines always pass through its exact centre.
	/// Where lines of different kinds overlap, centre lines are drawn over major lines, which are drawn over minor lines.
	/// </para>
	/// <para>
	/// Either set of lines can be omitted by setting its spacing to <c>0f</c> (or less).
	/// </para>
	/// </remarks>
	/// <typeparam name="T">The type of value this pattern produces at each texel.</typeparam>
	/// <param name="centreLineValue">The value of the horizontal and vertical lines through the centre of the pattern.</param>
	/// <param name="majorLineValue">The value of the major lines.</param>
	/// <param name="minorLineValue">The value of the minor lines.</param>
	/// <param name="backgroundValue">The value of the space between lines.</param>
	/// <param name="majorLineSpacing">The distance between neighbouring major lines, as a fraction of the pattern's width. Defaults to <c>0.25f</c>. A value of <c>0f</c> or less draws no major lines.</param>
	/// <param name="minorLineSpacing">The distance between neighbouring minor lines, as a fraction of the pattern's width. Defaults to <c>0.0625f</c>. A value of <c>0f</c> or less draws no minor lines.</param>
	/// <param name="centreLineThickness">The thickness of the two centre lines, in texels. Defaults to <c>6</c>.</param>
	/// <param name="majorLineThickness">The thickness of each major line, in texels. Defaults to <c>4</c>.</param>
	/// <param name="minorLineThickness">The thickness of each minor line, in texels. Defaults to <c>1</c>.</param>
	/// <param name="resolution">The width and height of the pattern, in texels. Defaults to <c>1024</c>.</param>
	/// <param name="transform">How to scale, rotate and shift the pattern, or <see langword="null"/> for no change. Scaling below <c>1f</c> squashes the pattern so it repeats more often; rotation turns it anticlockwise; translation shifts it. They are applied in that order.</param>
	public static TexturePattern<T> Grid<T>(T centreLineValue, T majorLineValue, T minorLineValue, T backgroundValue, float majorLineSpacing = GridDefaultMajorLineSpacing, float minorLineSpacing = GridDefaultMinorLineSpacing, int centreLineThickness = GridDefaultCentreLineThickness, int majorLineThickness = GridDefaultMajorLineThickness, int minorLineThickness = GridDefaultMinorLineThickness, int resolution = GridDefaultResolution, Transform2D? transform = null) where T : unmanaged {
		static float DistanceToNearestGridLine(float signedDistanceFromCentre, float periodInTexels) {
			var offsetFromNearestLine = signedDistanceFromCentre - MathF.Round(signedDistanceFromCentre / periodInTexels) * periodInTexels;
			return MathF.Abs(offsetFromNearestLine);
		}
		
		static T GetTexel(ReadOnlySpan<byte> args, XYPair<int> dimensions, XYPair<int> xy) {
			args
				.ReadFirstArg(out int resolution)
				.AndThen(out float majorPeriod)
				.AndThen(out float minorPeriod)
				.AndThen(out int centreLineThickness)
				.AndThen(out int majorLineThickness)
				.AndThen(out int minorLineThickness)
				.AndThen(out T centreValue)
				.AndThen(out T majorValue)
				.AndThen(out T minorValue)
				.AndThen(out T backgroundValue);

			var centreCoord = resolution * 0.5f;
			var dx = (xy.X + 0.5f) - centreCoord;
			var dy = (xy.Y + 0.5f) - centreCoord;

			if (MathF.Abs(dx) <= centreLineThickness * 0.5f || MathF.Abs(dy) <= centreLineThickness * 0.5f) return centreValue;

			if (majorPeriod >= 1f && (DistanceToNearestGridLine(dx, majorPeriod) <= majorLineThickness * 0.5f || DistanceToNearestGridLine(dy, majorPeriod) <= majorLineThickness * 0.5f)) return majorValue;

			if (minorPeriod >= 1f && (DistanceToNearestGridLine(dx, minorPeriod) <= minorLineThickness * 0.5f || DistanceToNearestGridLine(dy, minorPeriod) <= minorLineThickness * 0.5f)) return minorValue;

			return backgroundValue;
		}

		var majorPeriodTexels = Single.IsFinite(majorLineSpacing) && majorLineSpacing > 0f ? majorLineSpacing * resolution : 0f;
		var minorPeriodTexels = Single.IsFinite(minorLineSpacing) && minorLineSpacing > 0f ? minorLineSpacing * resolution : 0f;

		var argData = new TexturePatternArgData();
		argData
			.WriteFirstArg(resolution)
			.AndThen(majorPeriodTexels)
			.AndThen(minorPeriodTexels)
			.AndThen(centreLineThickness)
			.AndThen(majorLineThickness)
			.AndThen(minorLineThickness)
			.AndThen(centreLineValue)
			.AndThen(majorLineValue)
			.AndThen(minorLineValue)
			.AndThen(backgroundValue);
		return new TexturePattern<T>((resolution, resolution), &GetTexel, argData, transform);
	}
}
