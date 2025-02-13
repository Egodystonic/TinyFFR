// Created on 2025-01-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using Point2D = Egodystonic.TinyFFR.XYPair<float>;

namespace Egodystonic.TinyFFR;

public static class XYPairExtensions {
	public static Point2D WithLengthOne(this Point2D @this) => @this.LengthSquared != 0f ? Point2D.FromVector2(Vector2.Normalize(@this.ToVector2())) : @this;

	// TODO handle parallel lines, add fast variants, check parameters
	public static Point2D ClosestPointOn2DLine(this Point2D @this, Point2D anyPointOn2DLine, Point2D unitLength2DLineDirection) {
		return (@this - anyPointOn2DLine).Dot(unitLength2DLineDirection) * unitLength2DLineDirection + anyPointOn2DLine;
	}

	// TODO handle parallel lines, add fast variants, check parameters
	public static Point2D ClosestPointOn2DBoundedRay(this Point2D @this, Point2D startPointOf2DBoundedRay, Point2D endPointOf2DBoundedRay) {
		var startToEnd = endPointOf2DBoundedRay - startPointOf2DBoundedRay;
		var maxDistance = startToEnd.Length;
		var direction = startToEnd.WithLengthOne();

		return MathF.Min((@this - startPointOf2DBoundedRay).Dot(direction), maxDistance) * direction + startPointOf2DBoundedRay;
	}

	#region Rounding
	public static XYPair<TNew> Round<T, TNew>(this XYPair<T> @this, int roundingDigits = 0, MidpointRounding midpointRounding = MidpointRounding.ToEven) where T : unmanaged, IFloatingPoint<T> where TNew : unmanaged, IBinaryInteger<TNew> {
		return new(TNew.CreateSaturating(T.Round(@this.X, roundingDigits, midpointRounding)), TNew.CreateSaturating(T.Round(@this.Y, roundingDigits, midpointRounding)));
	}

	public static XYPair<TNew> CastWithRoundingIfNecessary<T, TNew>(this XYPair<T> @this, int roundingDigits = 0, MidpointRounding midpointRounding = MidpointRounding.ToEven) where T : unmanaged, IFloatingPoint<T> where TNew : unmanaged, INumber<TNew> {
		return XYPair<TNew>.IsFloatingPoint 
			? @this.Cast<TNew>() 
			: new(TNew.CreateSaturating(T.Round(@this.X, roundingDigits, midpointRounding)), TNew.CreateSaturating(T.Round(@this.Y, roundingDigits, midpointRounding)));
	}
	#endregion
}