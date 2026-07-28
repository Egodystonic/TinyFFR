// Created on 2026-07-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources.Memory;
using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

public static unsafe partial class TexturePattern {
	internal static TexturePattern<T> Grid<T>(T centreValue, T majorValue, T minorValue, T backgroundValue, int resolution, float majorFraction, float minorFraction, int centreLineThickness, int majorLineThickness, int minorLineThickness) where T : unmanaged {
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

		var majorPeriodTexels = Single.IsFinite(majorFraction) && majorFraction > 0f ? majorFraction * resolution : 0f;
		var minorPeriodTexels = Single.IsFinite(minorFraction) && minorFraction > 0f ? minorFraction * resolution : 0f;

		var argData = new TexturePatternArgData();
		argData
			.WriteFirstArg(resolution)
			.AndThen(majorPeriodTexels)
			.AndThen(minorPeriodTexels)
			.AndThen(centreLineThickness)
			.AndThen(majorLineThickness)
			.AndThen(minorLineThickness)
			.AndThen(centreValue)
			.AndThen(majorValue)
			.AndThen(minorValue)
			.AndThen(backgroundValue);
		return new TexturePattern<T>((resolution, resolution), &GetTexel, argData, null);
	}
}
