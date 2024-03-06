// Created on 2024-03-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public readonly partial struct BoundedLine : 
	IUnaryNegationOperators<BoundedLine, BoundedLine>,
	IMultiplyOperators<BoundedLine, float, BoundedLine>,
	IMultiplyOperators<BoundedLine, Rotation, BoundedLine>,
	IAdditionOperators<BoundedLine, Vect, BoundedLine> {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray ToRayFromStart() => new(_startPoint, Direction);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Ray ToRayFromEnd() => new(_startPoint + _vect, -Direction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Line ToUnboundedLine() => new(_startPoint, Direction);

	public BoundedLine Reversed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(_startPoint, -_vect);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedLine operator -(BoundedLine operand) => operand.Reversed;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedLine operator *(BoundedLine line, float scalar) => line.ScaledFromMiddleBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedLine operator *(float scalar, BoundedLine line) => line.ScaledFromMiddleBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedLine ScaledFromStartBy(float scalar) => new(_startPoint, _vect.ScaledBy(scalar));
	public BoundedLine ScaledFromMiddleBy(float scalar) {
		var scaledVect = _vect.ScaledBy(scalar);
		return new BoundedLine(_startPoint - scaledVect * 0.5f, scaledVect);
	}
	public BoundedLine ScaledFromEndBy(float scalar) {
		var scaledVect = _vect.ScaledBy(scalar);
		return new BoundedLine(_startPoint - scaledVect, scaledVect);
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedLine operator *(BoundedLine line, Rotation rot) => line.RotatedAroundMiddleBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedLine operator *(Rotation rot, BoundedLine line) => line.RotatedAroundMiddleBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedLine RotatedAroundStartBy(Rotation rotation) => new(_startPoint, _vect * rotation);
	public BoundedLine RotatedAroundEndBy(Rotation rotation) {
		var endPoint = EndPoint;
		return new(endPoint + _vect.Reversed * rotation, endPoint);
	}
	public BoundedLine RotatedAroundMiddleBy(Rotation rotation) {
		var newVect = _vect * rotation;
		var newStartPoint = _startPoint + ((_vect * 0.5f) - (newVect * 0.5f));
		return new(newStartPoint, newVect);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedLine operator +(BoundedLine line, Vect v) => line.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BoundedLine operator +(Vect v, BoundedLine line) => line.MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BoundedLine MovedBy(Vect v) => new(_startPoint + v, _vect);


	public static BoundedLine Interpolate(BoundedLine start, BoundedLine end, float distance) {
		return new(
			Location.Interpolate(start._startPoint, end._startPoint, distance),
			Vect.Interpolate(start._vect, end._vect, distance)
		);
	}
	public static BoundedLine CreateNewRandom() => new(Location.CreateNewRandom(), Location.CreateNewRandom());
	public static BoundedLine CreateNewRandom(BoundedLine minInclusive, BoundedLine maxExclusive) => new(Location.CreateNewRandom(minInclusive.StartPoint, maxExclusive.StartPoint), Vect.CreateNewRandom(minInclusive._vect, maxExclusive._vect));

	public Location ClosestPointTo(Location location) {
		var vectCoefficient = Vector3.Dot((location - _startPoint).ToVector3(), _vect.ToVector3()) / LengthSquared;
		return vectCoefficient switch {
			<= 0f => _startPoint,
			>= 1f => EndPoint,
			_ => _startPoint + _vect * vectCoefficient
		};
	}
	public Location ClosestPointToOrigin() {
		var vectCoefficient = -Vector3.Dot(_startPoint.ToVector3(), _vect.ToVector3()) / LengthSquared;
		return vectCoefficient switch {
			<= 0f => _startPoint,
			>= 1f => EndPoint,
			_ => _startPoint + _vect * vectCoefficient
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom(Location location) => location.DistanceFrom(ClosestPointTo(location));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location) => Contains(location, ILine.DefaultLineThickness);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(Location location, float lineThickness) => DistanceFrom(location) <= lineThickness;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location ClosestPointTo<TLine>(TLine line) where TLine : ILine => ILine.CalculateClosestPointToOtherLine(this, line);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DistanceFrom<TLine>(TLine line) where TLine : ILine => DistanceFrom(ClosestPointTo(line));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Location? IntersectionPointWith<TLine>(TLine line) where TLine : ILine => GetIntersectionPointOn(line, ILine.DefaultLineThickness);
	public Location? GetIntersectionPointOn<TLine>(TLine line, float lineThickness) where TLine : ILine {
		var closestPointOnLine = line.ClosestPointTo(this);
		return DistanceFrom(closestPointOnLine) <= lineThickness ? closestPointOnLine : null;
	}
}