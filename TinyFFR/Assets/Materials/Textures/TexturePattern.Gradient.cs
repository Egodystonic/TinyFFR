// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static Egodystonic.TinyFFR.Assets.Materials.TexturePatternDefaultValues;

namespace Egodystonic.TinyFFR.Assets.Materials;

public static unsafe partial class TexturePattern {
	public static TexturePattern<T> GradientHorizontal<T>(T left, T right, T? centre = null, XYPair<int>? resolution = null, Transform2D? transform = null) where T : unmanaged, IInterpolatable<T> {
		centre ??= T.Interpolate(left, right, 0.5f);

		return Gradient(
			right: right,
			topRight: right,
			top: centre.Value,
			topLeft: left,
			left: left,
			bottomLeft: left,
			bottom: centre.Value,
			bottomRight: right,
			centre: centre.Value,
			resolution,
			transform
		);
	}

	public static TexturePattern<T> GradientVertical<T>(T top, T bottom, T? centre = null, XYPair<int>? resolution = null, Transform2D? transform = null) where T : unmanaged, IInterpolatable<T> {
		centre ??= T.Interpolate(top, bottom, 0.5f);

		return Gradient(
			right: centre.Value,
			topRight: top,
			top: top,
			topLeft: top,
			left: centre.Value,
			bottomLeft: bottom,
			bottom: bottom,
			bottomRight: bottom,
			centre: centre.Value,
			resolution,
			transform
		);
	}
	
	public static TexturePattern<T> Gradient<T>(T right, T topRight, T top, T topLeft, T left, T bottomLeft, T bottom, T bottomRight, T centre, XYPair<int>? resolution = null, Transform2D? transform = null) where T : unmanaged, IInterpolatable<T> {
		static T GetTexel(ReadOnlySpan<byte> args, XYPair<int> dimensions, XYPair<int> xy) {
			args
				.ReadFirstArg(out XYPair<int> halfResolution)
				.AndThen(out T right)
				.AndThen(out T topRight)
				.AndThen(out T top)
				.AndThen(out T topLeft)
				.AndThen(out T left)
				.AndThen(out T bottomLeft)
				.AndThen(out T bottom)
				.AndThen(out T bottomRight)
				.AndThen(out T centre);

			var blendDistance = new XYPair<int>(xy.X % halfResolution.X, xy.Y % halfResolution.Y).Cast<float>() / halfResolution.Cast<float>();
			T minMin, minMax, maxMin, maxMax;
			switch ((xy.X < halfResolution.X, xy.Y < halfResolution.Y)) {
				case (true, true):
					minMin = bottomLeft;
					minMax = left;
					maxMin = bottom;
					maxMax = centre;
					break;
				case (true, false):
					minMin = left;
					minMax = topLeft;
					maxMin = centre;
					maxMax = top;
					break;
				case (false, true):
					minMin = bottom;
					minMax = centre;
					maxMin = bottomRight;
					maxMax = right;
					break;
				default:
					minMin = centre;
					minMax = top;
					maxMin = right;
					maxMax = topRight;
					break;
			}

			return T.Interpolate(T.Interpolate(minMin, maxMin, blendDistance.X), T.Interpolate(minMax, maxMax, blendDistance.X), blendDistance.Y);
		}

		resolution ??= GradientDefaultResolution;
		var argData = new TexturePatternArgData();
		argData
			.WriteFirstArg(resolution.Value / 2)
			.AndThen(right)
			.AndThen(topRight)
			.AndThen(top)
			.AndThen(topLeft)
			.AndThen(left)
			.AndThen(bottomLeft)
			.AndThen(bottom)
			.AndThen(bottomRight)
			.AndThen(centre);
		return new TexturePattern<T>(resolution.Value, &GetTexel, argData, transform);
	}

	public static TexturePattern<T> GradientRadial<T>(T inner, T outer, bool fringeCorners = true, float innerOuterRatio = 3f, XYPair<int>? resolution = null, Transform2D? transform = null) where T : unmanaged, IInterpolatable<T> {
		static T GetTexel(ReadOnlySpan<byte> args, XYPair<int> dimensions, XYPair<int> xy) {
			args
				.ReadFirstArg(out XYPair<int> centrePoint)
				.AndThen(out T inner)
				.AndThen(out T outer)
				.AndThen(out bool fringeCorners)
				.AndThen(out float innerSizeCoefficient);

			var distance = xy.DistanceFrom(centrePoint) / Int32.Min(centrePoint.X, centrePoint.Y);
			if (!fringeCorners) distance = MathF.Min(distance, 1f);

			return T.Interpolate(inner, outer, MathF.Pow(distance, innerSizeCoefficient));
		}

		resolution ??= GradientDefaultResolution;
		var argData = new TexturePatternArgData();
		argData
			.WriteFirstArg(resolution.Value / 2)
			.AndThen(inner)
			.AndThen(outer)
			.AndThen(fringeCorners)
			.AndThen(innerOuterRatio);
		return new TexturePattern<T>(resolution.Value, &GetTexel, argData, transform);
	}
}