using System;

namespace Egodystonic.TinyFFR;

public readonly struct DimensionConverter : IEquatable<DimensionConverter> {
	public Direction XBasis { get; }
	public Direction YBasis { get; }
	public Direction ZBasis { get; }
	public Location Origin3D { get; }

	public DimensionConverter(Direction zBasis) {
		XBasis = zBasis.AnyOrthogonal();
		YBasis = Direction.FromDualOrthogonalization(zBasis, XBasis);
		ZBasis = zBasis;
		Origin3D = Location.Origin;
	}

	public DimensionConverter(Direction zBasis, Location origin3D) {
		XBasis = zBasis.AnyOrthogonal();
		YBasis = Direction.FromDualOrthogonalization(zBasis, XBasis);
		ZBasis = zBasis;
		Origin3D = origin3D;
	}

	public DimensionConverter(Direction xBasis, Direction yBasis, Direction zBasis, Location origin3D) {
		XBasis = xBasis;
		YBasis = yBasis;
		ZBasis = zBasis;
		Origin3D = origin3D;
	}

	public static DimensionConverter FromOrthogonalizedAxes(Axis orthogonalizationTargetAxis, Direction xBasis, Direction yBasis, Direction zBasis) => FromOrthogonalizedAxes(orthogonalizationTargetAxis, xBasis, yBasis, zBasis, Location.Origin);
	public static DimensionConverter FromOrthogonalizedAxes(Axis orthogonalizationTargetAxis, Direction xBasis, Direction yBasis, Direction zBasis, Location origin3D) {
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

	public XYPair<float> ConvertLocation(Location location3D) {
		location3D -= (Vect) Origin3D;
		return new(XBasis.Dot((Vect) location3D), YBasis.Dot((Vect) location3D));
	}
	public Location ConvertLocation(XYPair<float> location2D) {
		return (XBasis * location2D.X + YBasis * location2D.Y) + Origin3D;
	}
	public Location ConvertLocation(XYPair<float> location2D, float zAxisDimension) {
		return ConvertLocation(location2D) + ZBasis * zAxisDimension;
	}

	public XYPair<float> ConvertVect(Vect v) {
		return new(XBasis.Dot(v), YBasis.Dot(v));
	}
	public Vect ConvertVect(XYPair<float> vect2D) {
		return XBasis * vect2D.X + YBasis * vect2D.Y;
	}
	public Vect ConvertVect(XYPair<float> vect2D, float zAxisDimension) {
		return ConvertVect(vect2D) + ZBasis * zAxisDimension;
	}

	public XYPair<float>? ConvertDirection(Direction d) {
		var result = ConvertVect(d.AsVect()).WithLengthOne();
		return result.LengthSquared > 0f ? result : null;
	}
	public Direction? ConvertDirection(XYPair<float> direction2D) {
		var result = ConvertVect(direction2D).Direction;
		return result != Direction.None ? result : null;
	}

	public bool Equals(DimensionConverter other) {
		return XBasis.Equals(other.XBasis) 
			   && YBasis.Equals(other.YBasis)
			   && ZBasis.Equals(other.ZBasis)
			   && Origin3D.Equals(other.Origin3D);
	}
	public override bool Equals(object? obj) => obj is DimensionConverter other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(XBasis, YBasis, ZBasis, Origin3D);

	public static bool operator ==(DimensionConverter left, DimensionConverter right) => left.Equals(right);
	public static bool operator !=(DimensionConverter left, DimensionConverter right) => !left.Equals(right);
}