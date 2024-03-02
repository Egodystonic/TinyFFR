// Created on 2024-03-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public readonly partial struct Line {
	public const float DefaultLineThickness = 0.01f;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray ToRayFromStart() => new(_startPoint, Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray ToRayFromEnd() => new(_startPoint + _vect, -Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(Line line, float scalar) => line.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(float scalar, Line line) => line.ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line ScaledBy(float scalar) => new(_startPoint, _vect.ScaledBy(scalar));


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(Line line, Rotation rot) => line.RotatedAroundMiddleBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator *(Rotation rot, Line line) => line.RotatedAroundMiddleBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line RotatedAroundStartBy(Rotation rotation) => new(_startPoint, _vect * rotation);
	public Line RotatedAroundEndBy(Rotation rotation) {
		var endPoint = EndPoint;
		return new(endPoint + _vect.Reversed * rotation, endPoint);
	}
	public Line RotatedAroundMiddleBy(Rotation rotation) {
		var newVect = _vect * rotation;
		var newStartPoint = _startPoint + ((_vect * 0.5f) - (newVect * 0.5f));
		return new(newStartPoint, newVect);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator +(Line line, Vect v) => line.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Line operator +(Vect v, Line line) => line.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line MovedBy(Vect v) => new(_startPoint + v, _vect);


	public static Line Interpolate(Line start, Line end, float distance) {
		return new(
			Location.Interpolate(start._startPoint, end._startPoint, distance),
			Vect.Interpolate(start._vect, end._vect, distance)
		);
	}
	public static Line CreateNewRandom() => new(Location.CreateNewRandom(), Location.CreateNewRandom());
	public static Line CreateNewRandom(Line minInclusive, Line maxExclusive) => new(Location.CreateNewRandom(minInclusive.StartPoint, maxExclusive.StartPoint), Vect.CreateNewRandom(minInclusive._vect, maxExclusive._vect));

	public Location ClosestPointTo(Location location) {
		var vectCoefficient = Vector3.Dot((location - _startPoint).ToVector3(), _vect.ToVector3()) / LengthSquared;
		return vectCoefficient switch {
			<= 0f => _startPoint,
			>= 1f => EndPoint,
			_ => _startPoint + _vect * vectCoefficient
		};
	}
	public Location UnboundedProjectionOf(Location location) {
		var vectCoefficient = Vector3.Dot((location - _startPoint).ToVector3(), _vect.ToVector3()) / LengthSquared;
		return _startPoint + _vect * vectCoefficient;
	}

	public float DistanceFrom(Location location) => location.DistanceFrom(ClosestPointTo(location));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location) => Contains(location, DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location, float lineThickness) => DistanceFrom(location) <= lineThickness;

	public Location ClosestPointTo(Ray ray) {
		return default;
	}
	public Location ClosestPointTo(Line line) { return default; }
}