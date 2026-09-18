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
	/// Creates a pattern of rectangles laid out in a grid, with space between them.
	/// </summary>
	/// <typeparam name="T">The type of value this pattern produces at each texel.</typeparam>
	/// <param name="interiorValue">The value inside each rectangle.</param>
	/// <param name="paddingValue">The value of the space between rectangles.</param>
	/// <param name="interiorSize">The width and height of each rectangle's interior, in texels, or <see langword="null"/> for <c>(512, 256)</c>.</param>
	/// <param name="paddingSize">The space left around each rectangle, in texels, or <see langword="null"/> for <c>(128, 64)</c>.</param>
	/// <param name="repetitions">How many rectangles the pattern is across and down, or <see langword="null"/> for <c>(4, 8)</c>.</param>
	/// <param name="transform">How to scale, rotate and shift the pattern, or <see langword="null"/> for no change. Scaling below <c>1f</c> squashes the pattern so it repeats more often; rotation turns it anticlockwise; translation shifts it. They are applied in that order.</param>
	public static TexturePattern<T> Rectangles<T>(T interiorValue, T paddingValue, XYPair<int>? interiorSize = null, XYPair<int>? paddingSize = null, XYPair<int>? repetitions = null, Transform2D? transform = null) where T : unmanaged {
		return Rectangles(interiorValue, paddingValue, default, interiorSize, paddingSize, (0, 0), repetitions, transform);
	}

	/// <summary>
	/// Creates a pattern of bordered rectangles laid out in a grid, with space between them.
	/// </summary>
	/// <typeparam name="T">The type of value this pattern produces at each texel.</typeparam>
	/// <param name="interiorValue">The value inside each rectangle.</param>
	/// <param name="paddingValue">The value of the space between rectangles.</param>
	/// <param name="borderValue">The value of the border around each rectangle.</param>
	/// <param name="interiorSize">The width and height of each rectangle's interior, in texels, or <see langword="null"/> for <c>(512, 256)</c>.</param>
	/// <param name="paddingSize">The space left around each rectangle, in texels, or <see langword="null"/> for <c>(128, 64)</c>.</param>
	/// <param name="borderSize">The thickness of the border drawn around each rectangle, in texels, or <see langword="null"/> for <c>(64, 32)</c>.</param>
	/// <param name="repetitions">How many rectangles the pattern is across and down, or <see langword="null"/> for <c>(4, 8)</c>.</param>
	/// <param name="transform">How to scale, rotate and shift the pattern, or <see langword="null"/> for no change. Scaling below <c>1f</c> squashes the pattern so it repeats more often; rotation turns it anticlockwise; translation shifts it. They are applied in that order.</param>
	public static TexturePattern<T> Rectangles<T>(T interiorValue, T paddingValue, T borderValue, XYPair<int>? interiorSize = null, XYPair<int>? paddingSize = null, XYPair<int>? borderSize = null, XYPair<int>? repetitions = null, Transform2D? transform = null) where T : unmanaged {
		return Rectangles(
			interiorSize ?? RectanglesDefaultInteriorSize,
			borderSize ?? RectanglesDefaultBorderSize,
			paddingSize ?? RectanglesDefaultPaddingSize,
			interiorValue,
			borderValue,
			borderValue,
			borderValue,
			borderValue,
			paddingValue,
			repetitions ?? RectanglesDefaultRepetitions,
			transform
		);
	}

	/// <summary>
	/// Creates a pattern of rectangles whose four border edges each take their own value.
	/// </summary>
	/// <remarks>
	/// Giving each edge its own value is what produces the bevelled look of a raised or recessed panel.
	/// </remarks>
	/// <typeparam name="T">The type of value this pattern produces at each texel.</typeparam>
	/// <param name="interiorSize">The width and height of each rectangle's interior, in texels.</param>
	/// <param name="borderSize">The thickness of the border around each rectangle, in texels.</param>
	/// <param name="paddingSize">The space left around each rectangle, in texels.</param>
	/// <param name="interiorValue">The value inside each rectangle.</param>
	/// <param name="borderRightValue">The value of each rectangle's right border edge.</param>
	/// <param name="borderTopValue">The value of each rectangle's top border edge.</param>
	/// <param name="borderLeftValue">The value of each rectangle's left border edge.</param>
	/// <param name="borderBottomValue">The value of each rectangle's bottom border edge.</param>
	/// <param name="paddingValue">The value of the space between rectangles.</param>
	/// <param name="repetitions">How many rectangles the pattern is across and down.</param>
	/// <param name="transform">How to scale, rotate and shift the pattern, or <see langword="null"/> for no change. Scaling below <c>1f</c> squashes the pattern so it repeats more often; rotation turns it anticlockwise; translation shifts it. They are applied in that order.</param>
	public static TexturePattern<T> Rectangles<T>(XYPair<int> interiorSize, XYPair<int> borderSize, XYPair<int> paddingSize, T interiorValue, T borderRightValue, T borderTopValue, T borderLeftValue, T borderBottomValue, T paddingValue, XYPair<int> repetitions, Transform2D? transform = null) where T : unmanaged {
		static T GetTexel(ReadOnlySpan<byte> args, XYPair<int> dimensions, XYPair<int> xy) {
			args
				.ReadFirstArg(out XYPair<int> rectSize)
				.AndThen(out XYPair<int> borderSize)
				.AndThen(out XYPair<int> paddingSize)
				.AndThen(out T rectValue)
				.AndThen(out T borderRightValue)
				.AndThen(out T borderTopValue)
				.AndThen(out T borderLeftValue)
				.AndThen(out T borderBottomValue)
				.AndThen(out T paddingValue);

			var completeRectSize = rectSize + borderSize + paddingSize;
			var rectCentre = completeRectSize / 2;
			var rectInteriorHalfSize = rectSize / 2;
			var rectAndBorderHalfSize = rectInteriorHalfSize + borderSize;
			
			var rectRelativeXy = new XYPair<int>(xy.X % completeRectSize.X, xy.Y % completeRectSize.Y);
			var centreOffset = rectRelativeXy - rectCentre;
			var centreOffsetAbs = centreOffset.Absolute;

			if (centreOffsetAbs.X <= rectInteriorHalfSize.X && centreOffsetAbs.Y <= rectInteriorHalfSize.Y) return rectValue;
			else if (centreOffsetAbs.X > rectAndBorderHalfSize.X || centreOffsetAbs.Y > rectAndBorderHalfSize.Y) return paddingValue;

			var xBorderDepth = centreOffsetAbs.X - rectInteriorHalfSize.X;
			var yBorderDepth = centreOffsetAbs.Y - rectInteriorHalfSize.Y;
			var isXBorder = (float) xBorderDepth / borderSize.X >= (float) yBorderDepth / borderSize.Y;

			return isXBorder
				? (centreOffset.X > 0 ? borderRightValue : borderLeftValue)
				: (centreOffset.Y > 0 ? borderTopValue : borderBottomValue);
		}

		paddingSize *= 2;
		var argData = new TexturePatternArgData();
		argData
			.WriteFirstArg(interiorSize)
				.AndThen(borderSize)
				.AndThen(paddingSize)
				.AndThen(interiorValue)
				.AndThen(borderRightValue)
				.AndThen(borderTopValue)
				.AndThen(borderLeftValue)
				.AndThen(borderBottomValue)
				.AndThen(paddingValue);
		return new TexturePattern<T>((interiorSize + borderSize + paddingSize) * repetitions, &GetTexel, argData, transform);
	}
}