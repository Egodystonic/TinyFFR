// Created on 2025-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR;

public static class XYPairExtensions {
	public static XYPair<float> WithLengthOne(this XYPair<float> @this) => @this.LengthSquared != 0f ? XYPair<float>.FromVector2(Vector2.Normalize(@this.ToVector2())) : @this;

	// TODO handle parallel lines, add fast variants, check parameters
	public static XYPair<float> ClosestPointOn2DLine(this XYPair<float> @this, XYPair<float> anyPointOn2DLine, XYPair<float> unitLength2DLineDirection) {
		return (@this - anyPointOn2DLine).Dot(unitLength2DLineDirection) * unitLength2DLineDirection + anyPointOn2DLine;
	}

	// TODO handle parallel lines, add fast variants, check parameters
	public static XYPair<float> ClosestPointOn2DBoundedRay(this XYPair<float> @this, XYPair<float> startPointOf2DBoundedRay, XYPair<float> endPointOf2DBoundedRay) {
		var startToEnd = endPointOf2DBoundedRay - startPointOf2DBoundedRay;
		var maxDistance = startToEnd.Length;
		var direction = startToEnd.WithLengthOne();

		return MathF.Min((@this - startPointOf2DBoundedRay).Dot(direction), maxDistance) * direction + startPointOf2DBoundedRay;
	}

	#region Rounding
	public static XYPair<TNew> Round<T, TNew>(this XYPair<T> @this, int roundingDigits = 0, MidpointRounding midpointRounding = MidpointRounding.ToEven) where T : unmanaged, IFloatingPoint<T> where TNew : unmanaged, IBinaryInteger<TNew> {
		return new(TNew.CreateSaturating(T.Round(@this.X, roundingDigits, midpointRounding)), TNew.CreateSaturating(T.Round(@this.Y, roundingDigits, midpointRounding)));
	}

	public static XYPair<TNew> CastWithRoundingIfNecessary<T, TNew>(this XYPair<T> @this, MidpointRounding midpointRounding = MidpointRounding.ToEven) where T : unmanaged, IFloatingPoint<T> where TNew : unmanaged, INumber<TNew> {
		return XYPair<TNew>.IsFloatingPoint 
			? @this.Cast<TNew>() 
			: new(TNew.CreateSaturating(T.Round(@this.X, midpointRounding)), TNew.CreateSaturating(T.Round(@this.Y, midpointRounding)));
	}
	#endregion
}