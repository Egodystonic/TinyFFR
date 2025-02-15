using System;

namespace Egodystonic.TinyFFR;

public readonly record struct DimensionConverter {
	public Direction XBasis { get; }
	public Direction YBasis { get; }
	public Direction ZBasis { get; }
	public Location Origin { get; }

	public DimensionConverter(Direction zBasis) {
		XBasis = zBasis.AnyOrthogonal();
		YBasis = Direction.FromDualOrthogonalization(zBasis, XBasis);
		ZBasis = zBasis;
		Origin = Location.Origin;
	}

	public DimensionConverter(Direction zBasis, Location origin) {
		XBasis = zBasis.AnyOrthogonal();
		YBasis = Direction.FromDualOrthogonalization(zBasis, XBasis);
		ZBasis = zBasis;
		Origin = origin;
	}

	public DimensionConverter(Direction xBasis, Direction yBasis, Direction zBasis) : this(xBasis, yBasis, zBasis, Location.Origin) { }

	public DimensionConverter(Direction xBasis, Direction yBasis, Direction zBasis, Location origin) {
		XBasis = xBasis;
		YBasis = yBasis;
		ZBasis = zBasis;
		Origin = origin;
	}

	public static DimensionConverter FromBasesWithOrthogonalization(Axis orthogonalizationTargetAxis, Direction xBasis, Direction yBasis, Direction zBasis) => FromBasesWithOrthogonalization(orthogonalizationTargetAxis, xBasis, yBasis, zBasis, Location.Origin);
	public static DimensionConverter FromBasesWithOrthogonalization(Axis orthogonalizationTargetAxis, Direction xBasis, Direction yBasis, Direction zBasis, Location origin3D) {
		static void OrthogonalizeAxes(Direction target, ref Direction secondary, ref Direction tertiary) {
			secondary = secondary.OrthogonalizedAgainst(target) ?? target.AnyOrthogonal();
			if (tertiary.IsOrthogonalTo(target) && tertiary.IsOrthogonalTo(secondary)) return;

			var newTertiary = tertiary.OrthogonalizedAgainst(target);
			if (newTertiary == null || !newTertiary.Value.IsOrthogonalTo(secondary)) newTertiary = Direction.FromDualOrthogonalization(target, secondary);
			tertiary = newTertiary.Value;
		}

		switch (orthogonalizationTargetAxis) {
			case Axis.X:
				OrthogonalizeAxes(xBasis, ref yBasis, ref zBasis);
				break;
			case Axis.Y:
				OrthogonalizeAxes(yBasis, ref xBasis, ref zBasis);
				break;
			case Axis.Z:
				OrthogonalizeAxes(zBasis, ref xBasis, ref yBasis);
				break;
		}

		return new(xBasis, yBasis, zBasis, origin3D);
	}

	public XYPair<float> ConvertLocation(Location location) {
		location -= (Vect) Origin;
		return new(XBasis.Dot((Vect) location), YBasis.Dot((Vect) location));
	}
	public Location ConvertLocation(XYPair<float> location2D) {
		return (XBasis * location2D.X + YBasis * location2D.Y) + Origin;
	}
	public Location ConvertLocation(XYPair<float> location2D, float zAxisDimension) {
		return ConvertLocation(location2D) + ZBasis * zAxisDimension;
	}

	public XYPair<float> ConvertVect(Vect vect) {
		return new(XBasis.Dot(vect), YBasis.Dot(vect));
	}
	public Vect ConvertVect(XYPair<float> vect2D) {
		return XBasis * vect2D.X + YBasis * vect2D.Y;
	}
	public Vect ConvertVect(XYPair<float> vect2D, float zAxisDimension) {
		return ConvertVect(vect2D) + ZBasis * zAxisDimension;
	}

	public XYPair<float>? ConvertDirection(Direction dir) {
		var result = ConvertVect(dir.AsVect()).WithLengthOne();
		return result.LengthSquared > 0f ? result : null;
	}
	public Direction? ConvertDirection(XYPair<float> dir2D) {
		var result = ConvertVect(dir2D).Direction;
		return result != Direction.None ? result : null;
	}
}