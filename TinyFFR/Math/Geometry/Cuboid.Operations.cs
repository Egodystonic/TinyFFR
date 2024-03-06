// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public readonly partial struct Cuboid
	: IMultiplyOperators<Cuboid, float, Cuboid> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Cuboid operator *(Cuboid cuboid, float scalar) => cuboid.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Cuboid operator *(float scalar, Cuboid cuboid) => cuboid.ScaledBy(scalar);

	public Cuboid ScaledBy(float scalar) => FromHalfDimensions(_halfWidth * scalar, _halfHeight * scalar, _halfDepth * scalar);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Cuboid WithWidth(float newWidth) => this with { Width = newWidth };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Cuboid WithHeight(float newHeight) => this with { Height = newHeight };
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Cuboid WithDepth(float newDepth) => this with { Depth = newDepth };

	public float DistanceFrom(Location location) {
		var xDist = MathF.Max(0f, MathF.Abs(location.X) - HalfWidth);
		var yDist = MathF.Max(0f, MathF.Abs(location.Y) - HalfHeight);
		var zDist = MathF.Max(0f, MathF.Abs(location.Z) - HalfDepth);

		return new Vector3(xDist, yDist, zDist).Length();
	}
	public float SurfaceDistanceFrom(Location location) {
		var xDist = MathF.Abs(MathF.Abs(location.X) - HalfWidth);
		var yDist = MathF.Abs(MathF.Abs(location.Y) - HalfHeight);
		var zDist = MathF.Abs(MathF.Abs(location.Z) - HalfDepth);

		return new Vector3(xDist, yDist, zDist).Length();
	}

	public bool Contains(Location location) => MathF.Abs(location.X) <= HalfWidth && MathF.Abs(location.Y) <= HalfHeight && MathF.Abs(location.Z) <= HalfDepth;

	public Location ClosestPointTo(Location location) {
		return new(
			Single.Clamp(location.X, -HalfWidth, HalfWidth),
			Single.Clamp(location.Y, -HalfHeight, HalfHeight),
			Single.Clamp(location.Z, -HalfDepth, HalfDepth)
		);
	}
	public Location ClosestSurfacePointTo(Location location) {
		static Axis GetMinAxis(float x, float y, float z) {
			if (MathF.Abs(x) < MathF.Abs(y)) return MathF.Abs(x) < MathF.Abs(z) ? Axis.X : Axis.Z;
			else return MathF.Abs(y) < MathF.Abs(z) ? Axis.Y : Axis.Z;
		}

		var xSign = location.X < 0f ? -1f : 1f;
		var ySign = location.Y < 0f ? -1f : 1f;
		var zSign = location.Z < 0f ? -1f : 1f;
		var xDelta = xSign * (HalfWidth - MathF.Abs(location.X));
		var yDelta = ySign * (HalfHeight - MathF.Abs(location.Y));
		var zDelta = zSign * (HalfDepth - MathF.Abs(location.Z));

		return GetMinAxis(xDelta, yDelta, zDelta) switch {
			Axis.X => location with { X = location.X + xDelta },
			Axis.Y => location with { Y = location.Y + yDelta },
			_ => location with { Z = location.Z + zDelta },
		};
	}

	
}