// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static Egodystonic.TinyFFR.Assets.Materials.TexturePatternDefaultValues;

namespace Egodystonic.TinyFFR.Assets.Materials;

public static unsafe partial class TexturePattern {
	/// <summary>
	/// Creates a pattern of circles, each with a border, laid out in a grid.
	/// </summary>
	/// <typeparam name="T">The type of value this pattern produces at each texel.</typeparam>
	/// <param name="interiorValue">The value inside each circle.</param>
	/// <param name="borderValue">The value of the ring around each circle.</param>
	/// <param name="paddingValue">The value of the space between circles.</param>
	/// <param name="interiorRadius">The radius of each circle's interior, in texels. Defaults to <c>256</c>.</param>
	/// <param name="borderSize">The thickness of the ring drawn around each circle, in texels. Defaults to <c>24</c>.</param>
	/// <param name="paddingSize">The space left around each circle, in texels, or <see langword="null"/> for <c>(96, 96)</c>.</param>
	/// <param name="repetitions">How many circles the pattern is across and down, or <see langword="null"/> for <c>(3, 3)</c>.</param>
	/// <param name="transform">How to scale, rotate and shift the pattern, or <see langword="null"/> for no change. Scaling below <c>1f</c> squashes the pattern so it repeats more often; rotation turns it anticlockwise; translation shifts it. They are applied in that order.</param>
	public static TexturePattern<T> Circles<T>(T interiorValue, T borderValue, T paddingValue, int interiorRadius = CirclesDefaultInteriorRadius, int borderSize = CirclesDefaultBorderSize, XYPair<int>? paddingSize = null, XYPair<int>? repetitions = null, Transform2D? transform = null) where T : unmanaged {
		static T GetTexel(ReadOnlySpan<byte> args, XYPair<int> dimensions, XYPair<int> xy) {
			args
				.ReadFirstArg(out int interiorRadius)
				.AndThen(out int borderSize)
				.AndThen(out XYPair<int> paddingSize)
				.AndThen(out T interiorValue)
				.AndThen(out T borderValue)
				.AndThen(out T paddingValue);

			var completeRectSize = (new XYPair<int>(interiorRadius) + new XYPair<int>(borderSize) + paddingSize) * 2;
			var rectCentre = completeRectSize / 2;
			var distFromCentre = rectCentre.DistanceFrom(new XYPair<int>(xy.X % completeRectSize.X, xy.Y % completeRectSize.Y));

			if (distFromCentre <= interiorRadius) return interiorValue;
			else if (distFromCentre > interiorRadius + borderSize) return paddingValue;
			else return borderValue;
		}

		paddingSize ??= CirclesDefaultPaddingSize;
		repetitions ??= CirclesDefaultRepetitions;

		var argData = new TexturePatternArgData();
		argData
			.WriteFirstArg(interiorRadius)
				.AndThen(borderSize)
				.AndThen(paddingSize.Value)
				.AndThen(interiorValue)
				.AndThen(borderValue)
				.AndThen(paddingValue);
		return new TexturePattern<T>((new XYPair<int>(interiorRadius) + new XYPair<int>(borderSize) + paddingSize.Value) * 2 * repetitions.Value, &GetTexel, argData, transform);
	}

	/// <summary>
	/// Creates a pattern of circles whose borders blend between four values around their circumference.
	/// </summary>
	/// <remarks>
	/// The four border values are placed at the right, top, left and bottom of each ring and blended between, which produces a
	/// ring whose value varies smoothly with direction, which is the form a normal map of a dome takes.
	/// </remarks>
	/// <typeparam name="T">The type of value this pattern produces at each texel. Must support interpolation, since this pattern blends between the values given.</typeparam>
	/// <param name="interiorValue">The value inside each circle.</param>
	/// <param name="borderValueRight">The border value at the right of each circle.</param>
	/// <param name="borderValueTop">The border value at the top of each circle.</param>
	/// <param name="borderValueLeft">The border value at the left of each circle.</param>
	/// <param name="borderValueBottom">The border value at the bottom of each circle.</param>
	/// <param name="paddingValue">The value of the space between circles.</param>
	/// <param name="interiorRadius">The radius of each circle's interior, in texels. Defaults to <c>256</c>.</param>
	/// <param name="borderSize">The thickness of the ring drawn around each circle, in texels. Defaults to <c>24</c>.</param>
	/// <param name="paddingSize">The space left around each circle, in texels, or <see langword="null"/> for <c>(96, 96)</c>.</param>
	/// <param name="repetitions">How many circles the pattern is across and down, or <see langword="null"/> for <c>(3, 3)</c>.</param>
	/// <param name="transform">How to scale, rotate and shift the pattern, or <see langword="null"/> for no change. Scaling below <c>1f</c> squashes the pattern so it repeats more often; rotation turns it anticlockwise; translation shifts it. They are applied in that order.</param>
	public static TexturePattern<T> Circles<T>(T interiorValue, T borderValueRight, T borderValueTop, T borderValueLeft, T borderValueBottom, T paddingValue, int interiorRadius = CirclesDefaultInteriorRadius, int borderSize = CirclesDefaultBorderSize, XYPair<int>? paddingSize = null, XYPair<int>? repetitions = null, Transform2D? transform = null) where T : unmanaged, IInterpolatable<T> {
		return Circles(
			interiorValue, 
			interiorValue, 
			interiorValue, 
			interiorValue, 
			borderValueRight, 
			borderValueTop, 
			borderValueLeft, 
			borderValueBottom, 
			paddingValue, 
			interiorRadius, 
			borderSize, 
			paddingSize, 
			repetitions,
			transform
		);
	}

	/// <summary>
	/// Creates a pattern of circles whose interiors and borders both blend between four values around their circumference.
	/// </summary>
	/// <typeparam name="T">The type of value this pattern produces at each texel. Must support interpolation, since this pattern blends between the values given.</typeparam>
	/// <param name="interiorValueRight">The interior value at the right of each circle.</param>
	/// <param name="interiorValueTop">The interior value at the top of each circle.</param>
	/// <param name="interiorValueLeft">The interior value at the left of each circle.</param>
	/// <param name="interiorValueBottom">The interior value at the bottom of each circle.</param>
	/// <param name="borderValueRight">The border value at the right of each circle.</param>
	/// <param name="borderValueTop">The border value at the top of each circle.</param>
	/// <param name="borderValueLeft">The border value at the left of each circle.</param>
	/// <param name="borderValueBottom">The border value at the bottom of each circle.</param>
	/// <param name="paddingValue">The value of the space between circles.</param>
	/// <param name="interiorRadius">The radius of each circle's interior, in texels. Defaults to <c>256</c>.</param>
	/// <param name="borderSize">The thickness of the ring drawn around each circle, in texels. Defaults to <c>24</c>.</param>
	/// <param name="paddingSize">The space left around each circle, in texels, or <see langword="null"/> for <c>(96, 96)</c>.</param>
	/// <param name="repetitions">How many circles the pattern is across and down, or <see langword="null"/> for <c>(3, 3)</c>.</param>
	/// <param name="transform">How to scale, rotate and shift the pattern, or <see langword="null"/> for no change. Scaling below <c>1f</c> squashes the pattern so it repeats more often; rotation turns it anticlockwise; translation shifts it. They are applied in that order.</param>
	public static TexturePattern<T> Circles<T>(T interiorValueRight, T interiorValueTop, T interiorValueLeft, T interiorValueBottom, T borderValueRight, T borderValueTop, T borderValueLeft, T borderValueBottom, T paddingValue, int interiorRadius = CirclesDefaultInteriorRadius, int borderSize = CirclesDefaultBorderSize, XYPair<int>? paddingSize = null, XYPair<int>? repetitions = null, Transform2D? transform = null) where T : unmanaged, IInterpolatable<T> {
		static T GetTexel(ReadOnlySpan<byte> args, XYPair<int> dimensions, XYPair<int> xy) {
			args
				.ReadFirstArg(out int interiorRadius)
				.AndThen(out int borderSize)
				.AndThen(out XYPair<int> paddingSize)
				.AndThen(out T interiorValueRight)
				.AndThen(out T interiorValueTop)
				.AndThen(out T interiorValueLeft)
				.AndThen(out T interiorValueBottom)
				.AndThen(out T borderValueRight)
				.AndThen(out T borderValueTop)
				.AndThen(out T borderValueLeft)
				.AndThen(out T borderValueBottom)
				.AndThen(out T paddingValue);

			var completeRectSize = (new XYPair<int>(interiorRadius) + new XYPair<int>(borderSize) + paddingSize) * 2;
			var rectCentre = completeRectSize / 2;
			var xyInRect = new XYPair<int>(xy.X % completeRectSize.X, xy.Y % completeRectSize.Y);
			var distFromCentre = rectCentre.DistanceFrom(xyInRect);

			if (distFromCentre > interiorRadius + borderSize) return paddingValue;

			var polarAngle = ((xyInRect - rectCentre).PolarAngle ?? Angle.Zero).Degrees;
			(T Start, T End, float DistanceOffset) paramsTuple = (polarAngle, distFromCentre <= interiorRadius) switch {
				( >= 0f and < 90f, true) => (interiorValueRight, interiorValueTop, 0f),
				( >= 90f and < 180f, true) => (interiorValueTop, interiorValueLeft, 90f),
				( >= 180f and < 270f, true) => (interiorValueLeft, interiorValueBottom, 180f),
				(_, true) => (interiorValueBottom, interiorValueRight, 270f),

				( >= 0f and < 90f, false) => (borderValueRight, borderValueTop, 0f),
				( >= 90f and < 180f, false) => (borderValueTop, borderValueLeft, 90f),
				( >= 180f and < 270f, false) => (borderValueLeft, borderValueBottom, 180f),
				_ => (borderValueBottom, borderValueRight, 270f),
			};

			return T.Interpolate(paramsTuple.Start, paramsTuple.End, (polarAngle - paramsTuple.DistanceOffset) / 90f);
		}

		paddingSize ??= CirclesDefaultPaddingSize;
		repetitions ??= CirclesDefaultRepetitions;

		var argData = new TexturePatternArgData();
		argData
			.WriteFirstArg(interiorRadius)
				.AndThen(borderSize)
				.AndThen(paddingSize.Value)
				.AndThen(interiorValueRight)
				.AndThen(interiorValueTop)
				.AndThen(interiorValueLeft)
				.AndThen(interiorValueBottom)
				.AndThen(borderValueRight)
				.AndThen(borderValueTop)
				.AndThen(borderValueLeft)
				.AndThen(borderValueBottom)
				.AndThen(paddingValue);
		return new TexturePattern<T>((new XYPair<int>(interiorRadius) + new XYPair<int>(borderSize) + paddingSize.Value) * 2 * repetitions.Value, &GetTexel, argData, transform);
	}
}