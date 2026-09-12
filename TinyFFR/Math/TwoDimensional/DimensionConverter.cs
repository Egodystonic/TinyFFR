using System;

namespace Egodystonic.TinyFFR;

/// <summary>
/// A DimensionConverter helps you convert <see cref="Location"/>s, <see cref="Vect"/>s, and <see cref="Direction"/>s back and forward
/// between 3D space and 2D space, using a given plane to define the 2D co-ordinate system in the 3D world. 
/// </summary>
public readonly record struct DimensionConverter {
	/// <summary>
	/// The 3D direction that corresponds to the 2D X axis.
	/// </summary>
	public Direction XBasis { get; }
	/// <summary>
	/// The 3D direction that corresponds to the 2D Y axis.
	/// </summary>
	public Direction YBasis { get; }
	/// <summary>
	/// The 3D direction that corresponds to the (implicit) 2D Z axis, i.e. the direction pointing "out of the page" towards the viewer of the 2D coordinate system.
	/// </summary>
	public Direction ZBasis { get; }
	/// <summary>
	/// The 3D point that corresponds to the 2D origin (<c>(0f, 0f)</c>).
	/// </summary>
	public Location Origin { get; }

	/// <summary>
	/// Constructs a new <see cref="DimensionConverter"/> using <paramref name="zBasis"/> as the plane's normal, with the 3D origin as the 2D origin and an arbitrarily-chosen (but consistent) pair of axes for <see cref="XBasis"/>/<see cref="YBasis"/>.
	/// </summary>
	/// <param name="zBasis">The direction pointing "out of the page" towards the viewer.</param>
	public DimensionConverter(Direction zBasis) {
		XBasis = zBasis.AnyOrthogonal();
		YBasis = Direction.FromDualOrthogonalization(zBasis, XBasis);
		ZBasis = zBasis;
		Origin = Location.Origin;
	}

	/// <summary>
	/// Constructs a new <see cref="DimensionConverter"/> using <paramref name="zBasis"/> as the plane's normal and <paramref name="origin"/> as the 2D origin, with an arbitrarily-chosen (but consistent) pair of axes for <see cref="XBasis"/>/<see cref="YBasis"/>.
	/// </summary>
	/// <param name="zBasis">The direction pointing "out of the page" towards the viewer.</param>
	/// <param name="origin">The 3D point that should correspond to the 2D origin.</param>
	public DimensionConverter(Direction zBasis, Location origin) {
		XBasis = zBasis.AnyOrthogonal();
		YBasis = Direction.FromDualOrthogonalization(zBasis, XBasis);
		ZBasis = zBasis;
		Origin = origin;
	}

	/// <summary>
	/// Constructs a new <see cref="DimensionConverter"/> from explicit basis directions, with the 3D origin as the 2D origin.
	/// </summary>
	/// <remarks>
	/// The three bases are used exactly as given and are not checked for orthogonality; use <see cref="FromBasesWithOrthogonalization(Axis,Direction,Direction,Direction)"/> if you need them corrected to be mutually orthogonal first.
	/// </remarks>
	/// <param name="xBasis">The 3D direction that should correspond to the 2D X axis.</param>
	/// <param name="yBasis">The 3D direction that should correspond to the 2D Y axis.</param>
	/// <param name="zBasis">The direction pointing "out of the page" towards the viewer.</param>
	public DimensionConverter(Direction xBasis, Direction yBasis, Direction zBasis) : this(xBasis, yBasis, zBasis, Location.Origin) { }

	/// <summary>
	/// Constructs a new <see cref="DimensionConverter"/> from explicit basis directions and origin.
	/// </summary>
	/// <remarks>
	/// The three bases are used exactly as given and are not checked for orthogonality; use <see cref="FromBasesWithOrthogonalization(Axis,Direction,Direction,Direction,Location)"/> if you need them corrected to be mutually orthogonal first.
	/// </remarks>
	/// <param name="xBasis">The 3D direction that should correspond to the 2D X axis.</param>
	/// <param name="yBasis">The 3D direction that should correspond to the 2D Y axis.</param>
	/// <param name="zBasis">The direction pointing "out of the page" towards the viewer.</param>
	/// <param name="origin">The 3D point that should correspond to the 2D origin.</param>
	public DimensionConverter(Direction xBasis, Direction yBasis, Direction zBasis, Location origin) {
		XBasis = xBasis;
		YBasis = yBasis;
		ZBasis = zBasis;
		Origin = origin;
	}

	/// <summary>
	/// Constructs a new <see cref="DimensionConverter"/> from explicit basis directions, correcting them to be mutually orthogonal first, with the 3D origin as the 2D origin.
	/// </summary>
	/// <param name="orthogonalizationTargetAxis">Which of <paramref name="xBasis"/>, <paramref name="yBasis"/> or <paramref name="zBasis"/> should be kept fixed; the other two are adjusted (orthogonalized) around it.</param>
	/// <param name="xBasis">The desired 3D direction for the 2D X axis.</param>
	/// <param name="yBasis">The desired 3D direction for the 2D Y axis.</param>
	/// <param name="zBasis">The desired direction pointing "out of the page" towards the viewer.</param>
	public static DimensionConverter FromBasesWithOrthogonalization(Axis orthogonalizationTargetAxis, Direction xBasis, Direction yBasis, Direction zBasis) => FromBasesWithOrthogonalization(orthogonalizationTargetAxis, xBasis, yBasis, zBasis, Location.Origin);
	/// <summary>
	/// Constructs a new <see cref="DimensionConverter"/> from explicit basis directions and origin, correcting the bases to be mutually orthogonal first.
	/// </summary>
	/// <param name="orthogonalizationTargetAxis">Which of <paramref name="xBasis"/>, <paramref name="yBasis"/> or <paramref name="zBasis"/> should be kept fixed; the other two are adjusted (orthogonalized) around it.</param>
	/// <param name="xBasis">The desired 3D direction for the 2D X axis.</param>
	/// <param name="yBasis">The desired 3D direction for the 2D Y axis.</param>
	/// <param name="zBasis">The desired direction pointing "out of the page" towards the viewer.</param>
	/// <param name="origin3D">The 3D point that should correspond to the 2D origin.</param>
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

	/// <summary>
	/// Converts <paramref name="location"/> from 3D space to this converter's 2D coordinate system.
	/// </summary>
	/// <param name="location">The 3D location to convert.</param>
	public XYPair<float> ConvertLocation(Location location) {
		location -= (Vect) Origin;
		return new(XBasis.Dot((Vect) location), YBasis.Dot((Vect) location));
	}
	/// <summary>
	/// Converts <paramref name="location2D"/> from this converter's 2D coordinate system to 3D space.
	/// </summary>
	/// <param name="location2D">The 2D location to convert.</param>
	public Location ConvertLocation(XYPair<float> location2D) {
		return (XBasis * location2D.X + YBasis * location2D.Y) + Origin;
	}
	/// <summary>
	/// Converts <paramref name="location2D"/> from this converter's 2D coordinate system to 3D space, additionally offsetting the result by <paramref name="zAxisDimension"/> along <see cref="ZBasis"/> (i.e. out of the 2D plane).
	/// </summary>
	/// <param name="location2D">The 2D location to convert.</param>
	/// <param name="zAxisDimension">The distance to offset the result along <see cref="ZBasis"/>.</param>
	public Location ConvertLocation(XYPair<float> location2D, float zAxisDimension) {
		return ConvertLocation(location2D) + ZBasis * zAxisDimension;
	}

	/// <summary>
	/// Converts <paramref name="vect"/> from 3D space to this converter's 2D coordinate system.
	/// </summary>
	/// <param name="vect">The 3D vector to convert.</param>
	public XYPair<float> ConvertVect(Vect vect) {
		return new(XBasis.Dot(vect), YBasis.Dot(vect));
	}
	/// <summary>
	/// Converts <paramref name="vect2D"/> from this converter's 2D coordinate system to a 3D vector.
	/// </summary>
	/// <param name="vect2D">The 2D vector to convert.</param>
	public Vect ConvertVect(XYPair<float> vect2D) {
		return XBasis * vect2D.X + YBasis * vect2D.Y;
	}
	/// <summary>
	/// Converts <paramref name="vect2D"/> from this converter's 2D coordinate system to a 3D vector, additionally adding <paramref name="zAxisDimension"/> along <see cref="ZBasis"/>.
	/// </summary>
	/// <param name="vect2D">The 2D vector to convert.</param>
	/// <param name="zAxisDimension">The additional distance to add along <see cref="ZBasis"/>.</param>
	public Vect ConvertVect(XYPair<float> vect2D, float zAxisDimension) {
		return ConvertVect(vect2D) + ZBasis * zAxisDimension;
	}

	/// <summary>
	/// Converts <paramref name="dir"/> from a 3D direction to this converter's 2D coordinate system.
	/// </summary>
	/// <param name="dir">The 3D direction to convert.</param>
	/// <returns><see langword="null"/> if <paramref name="dir"/> is exactly parallel to <see cref="ZBasis"/> (and so has no meaningful representation within the 2D plane); the converted (unit-length) 2D direction otherwise.</returns>
	public XYPair<float>? ConvertDirection(Direction dir) {
		var result = ConvertVect(dir.AsVect()).WithLengthOne();
		return result.LengthSquared > 0f ? result : null;
	}
	/// <summary>
	/// Converts <paramref name="dir2D"/> from this converter's 2D coordinate system to a 3D direction.
	/// </summary>
	/// <param name="dir2D">The 2D direction to convert.</param>
	/// <returns><see langword="null"/> if <paramref name="dir2D"/> is <see cref="XYPair{T}.Zero"/> (and so has no meaningful direction); the converted 3D direction otherwise.</returns>
	public Direction? ConvertDirection(XYPair<float> dir2D) {
		var result = ConvertVect(dir2D).Direction;
		return result != Direction.None ? result : null;
	}
}