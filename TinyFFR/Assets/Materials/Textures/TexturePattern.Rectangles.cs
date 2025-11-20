// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static Egodystonic.TinyFFR.Assets.Materials.TexturePatternDefaultValues;

namespace Egodystonic.TinyFFR.Assets.Materials;

public static unsafe partial class TexturePattern {
	public static TexturePattern<T> Rectangles<T>(T interiorValue, T paddingValue, XYPair<int>? interiorSize = null, XYPair<int>? paddingSize = null, XYPair<int>? repetitions = null, Transform2D? transform = null) where T : unmanaged {
		return Rectangles(interiorValue, paddingValue, default, interiorSize, paddingSize, (0, 0), repetitions, transform);
	}

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