// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Egodystonic.TinyFFR.Assets.Materials;

public static unsafe partial class TexturePattern {
	public const int CirclesDefaultInteriorRadius = 256;
	public const int CirclesDefaultBorderSize = 24;
	public static readonly XYPair<int> CirclesDefaultPaddingSize = new(96);
	public static readonly XYPair<int> CirclesDefaultRepetitions = new(3);

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