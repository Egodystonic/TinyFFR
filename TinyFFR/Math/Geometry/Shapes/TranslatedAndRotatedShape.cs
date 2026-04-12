// Created on 2026-04-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

public interface ITranslatedAndRotatedShape : IShape;
public interface ITranslatedAndRotatedShape<TSelf> : ITranslatedAndRotatedShape, IShape<TSelf>, ITranslatable<TSelf>, IRotatable<TSelf> where TSelf : ITranslatedAndRotatedShape<TSelf>; 
public interface ITranslatedAndRotatedShape<TSelf, TBase> : ITranslatedAndRotatedShape<TSelf> where TSelf : ITranslatedAndRotatedShape<TSelf, TBase> where TBase : IShape<TBase> { TBase BaseShape { get; init; } } 
public interface ITranslatedAndRotatedConvexShape : ITranslatedAndRotatedShape, IConvexShape; 
public interface ITranslatedAndRotatedConvexShape<TSelf> : ITranslatedAndRotatedShape<TSelf>, ITranslatedAndRotatedConvexShape, IConvexShape<TSelf> where TSelf : ITranslatedAndRotatedConvexShape<TSelf>;
public interface ITranslatedAndRotatedConvexShape<TSelf, TBase> : ITranslatedAndRotatedConvexShape<TSelf>, ITranslatedAndRotatedShape<TSelf, TBase> where TSelf : ITranslatedAndRotatedConvexShape<TSelf, TBase> where TBase : IConvexShape<TBase>; 

public readonly struct TranslatedAndRotatedShape<T> : ITranslatedAndRotatedShape<TranslatedAndRotatedShape<T>, T> where T : IShape<T> {
	const string StringShapeTransformSeparator = " rotated by ";
	const string StringPositionRotationSeparator = " @ ";
	public T BaseShape { get; init; }
	public Location Position { get; init; }
	public Rotation Rotation { get; init; }

	public bool IsPhysicallyValid => BaseShape.IsPhysicallyValid && Position.IsPhysicallyValid && Rotation.IsPhysicallyValid;
	
	public TranslatedAndRotatedShape(T baseShape, Location position, Rotation rotation) {
		BaseShape = baseShape;
		Position = position;
		Rotation = rotation;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToShapeSpace<TVal>(TVal val) where TVal : ITranslatable<TVal>, IPointRotatable<TVal> => val.Minus(Position.AsVect()).RotatedAroundOriginBy(Rotation.Reversed);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToWorldSpace<TVal>(TVal val) where TVal : ITranslatable<TVal>, IPointRotatable<TVal> => val.RotatedAroundOriginBy(Rotation).Plus(Position.AsVect());
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToShapeSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal>, IPointRotatable<TVal> => val?.Minus(Position.AsVect()).RotatedAroundOriginBy(Rotation.Reversed);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToWorldSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal>, IPointRotatable<TVal> => val?.RotatedAroundOriginBy(Rotation).Plus(Position.AsVect());

	#region ToString / Format / Parse
	public string ToString(string? format, IFormatProvider? formatProvider) {
		return BaseShape.ToString(format, formatProvider)
			+ StringShapeTransformSeparator + Rotation.ToString(format, formatProvider)
			+ StringPositionRotationSeparator + Position.ToString(format, formatProvider);
	}
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		charsWritten = 0;
		
		if (!BaseShape.TryFormat(destination, out var c, format, provider)) return false;
		charsWritten += c;
		destination = destination[c..];

		if (!StringShapeTransformSeparator.TryCopyTo(destination)) return false;
		charsWritten += StringShapeTransformSeparator.Length;
		destination = destination[StringShapeTransformSeparator.Length..];

		if (!Rotation.TryFormat(destination, out c, format, provider)) return false;
		charsWritten += c;
		destination = destination[c..];

		if (!StringPositionRotationSeparator.TryCopyTo(destination)) return false;
		charsWritten += StringPositionRotationSeparator.Length;
		destination = destination[StringPositionRotationSeparator.Length..];
		
		if (!Position.TryFormat(destination, out c, format, provider)) return false;
		charsWritten += c;
		return true;
	}
	public static TranslatedAndRotatedShape<T> Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out TranslatedAndRotatedShape<T> result) => TryParse(s.AsSpan(), provider, out result);
	public static TranslatedAndRotatedShape<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		if (!TryParse(s, provider, out var result)) {
			throw new ArgumentException($"Given input string \"{s}\" does not represent a valid translated-and-rotated {typeof(T).Name}.", nameof(s));
		}
		return result;
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TranslatedAndRotatedShape<T> result) {
		result = default;
		
		var shapeTransformSplitIndex = s.IndexOf(StringShapeTransformSeparator);
		if (shapeTransformSplitIndex < 0) return false;
		
		if (!T.TryParse(s[..shapeTransformSplitIndex], provider, out var baseShape)) return false;
		s = s[(shapeTransformSplitIndex + StringShapeTransformSeparator.Length)..];

		var positionRotationSplitIndex = s.IndexOf(StringPositionRotationSeparator);
		if (positionRotationSplitIndex < 0) return false;
		if (!Rotation.TryParse(s[..positionRotationSplitIndex], provider, out var rotation)) return false;
		if (!Location.TryParse(s[(positionRotationSplitIndex + StringPositionRotationSeparator.Length)..], provider, out var position)) return false;
		
		result = new(baseShape, position, rotation);
		return true;
	}
	#endregion

	#region Byte Span Serialization / Deserialization
	public static int SerializationByteSpanLength => T.SerializationByteSpanLength + Location.SerializationByteSpanLength + Rotation.SerializationByteSpanLength;
	public static void SerializeToBytes(Span<byte> dest, TranslatedAndRotatedShape<T> src) {
		T.SerializeToBytes(dest, src.BaseShape);
		Location.SerializeToBytes(dest[T.SerializationByteSpanLength..], src.Position);
		Rotation.SerializeToBytes(dest[(T.SerializationByteSpanLength + Location.SerializationByteSpanLength)..], src.Rotation);
	}
	public static TranslatedAndRotatedShape<T> DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			T.DeserializeFromBytes(src),
			Location.DeserializeFromBytes(src[T.SerializationByteSpanLength..]),
			Rotation.DeserializeFromBytes(src[(T.SerializationByteSpanLength + Location.SerializationByteSpanLength)..])
		);
	}
	#endregion

	#region Move / Scale / Rotate
	public TranslatedAndRotatedShape<T> MovedBy(Vect v) => new(BaseShape, Position.MovedBy(v), Rotation);
	public static TranslatedAndRotatedShape<T> operator +(TranslatedAndRotatedShape<T> left, Vect right) => new(left.BaseShape, left.Position + right, left.Rotation);
	public static TranslatedAndRotatedShape<T> operator -(TranslatedAndRotatedShape<T> left, Vect right) => new(left.BaseShape, left.Position - right, left.Rotation);
	public static TranslatedAndRotatedShape<T> operator +(Vect left, TranslatedAndRotatedShape<T> right) => new(right.BaseShape, right.Position + left, right.Rotation);

	public static TranslatedAndRotatedShape<T> operator *(TranslatedAndRotatedShape<T> left, float right) => new(left.BaseShape * right, left.Position, left.Rotation);
	public static TranslatedAndRotatedShape<T> operator /(TranslatedAndRotatedShape<T> left, float right) => new(left.BaseShape / right, left.Position, left.Rotation);
	public static TranslatedAndRotatedShape<T> operator *(float left, TranslatedAndRotatedShape<T> right) => new(left * right.BaseShape, right.Position, right.Rotation);
	public TranslatedAndRotatedShape<T> ScaledBy(float scalar) => new(BaseShape * scalar, Position, Rotation);

	public static TranslatedAndRotatedShape<T> operator *(TranslatedAndRotatedShape<T> left, Rotation right) => new(left.BaseShape, left.Position, left.Rotation + right);
	public static TranslatedAndRotatedShape<T> operator *(Rotation left, TranslatedAndRotatedShape<T> right) => new(right.BaseShape, right.Position, right.Rotation + left);
	public TranslatedAndRotatedShape<T> RotatedBy(Rotation rot) => new(BaseShape, Position, Rotation + rot);
	#endregion

	#region Equality
	public override bool Equals(object? obj) => obj is TranslatedAndRotatedShape<T> other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(BaseShape, Position, Rotation);
	public bool Equals(TranslatedAndRotatedShape<T> other) => BaseShape.Equals(other.BaseShape) && Position.Equals(other.Position) && Rotation.Equals(other.Rotation);
	public bool Equals(TranslatedAndRotatedShape<T> other, float tolerance) => BaseShape.Equals(other.BaseShape, tolerance) && Position.Equals(other.Position, tolerance) && Rotation.Equals(other.Rotation, tolerance);
	public static bool operator ==(TranslatedAndRotatedShape<T> left, TranslatedAndRotatedShape<T> right) => left.Equals(right);
	public static bool operator !=(TranslatedAndRotatedShape<T> left, TranslatedAndRotatedShape<T> right) => !left.Equals(right);
	#endregion

	#region Random / Interp / Clamp
	public static TranslatedAndRotatedShape<T> Random() => new(T.Random(), Location.Random(), Rotation.Random());
	public static TranslatedAndRotatedShape<T> Random(TranslatedAndRotatedShape<T> minInclusive, TranslatedAndRotatedShape<T> maxExclusive) {
		return new(
			T.Random(minInclusive.BaseShape, maxExclusive.BaseShape),
			Location.Random(minInclusive.Position, maxExclusive.Position),
			Rotation.Random(minInclusive.Rotation, maxExclusive.Rotation)
		);
	}
	public static TranslatedAndRotatedShape<T> Interpolate(TranslatedAndRotatedShape<T> start, TranslatedAndRotatedShape<T> end, float distance) {
		return new(
			T.Interpolate(start.BaseShape, end.BaseShape, distance),
			Location.Interpolate(start.Position, end.Position, distance),
			Rotation.Interpolate(start.Rotation, end.Rotation, distance)
		);
	}
	public TranslatedAndRotatedShape<T> Clamp(TranslatedAndRotatedShape<T> min, TranslatedAndRotatedShape<T> max) {
		return new(
			BaseShape.Clamp(min.BaseShape, max.BaseShape),
			Position.Clamp(min.Position, max.Position),
			Rotation.Clamp(min.Rotation, max.Rotation)
		);
	}
	#endregion
}

public readonly struct TranslatedAndRotatedConvexShape<T> : ITranslatedAndRotatedConvexShape<TranslatedAndRotatedConvexShape<T>, T> where T : IConvexShape<T> {
	public T BaseShape { get; init; }
	public Location Position { get; init; }
	public Rotation Rotation { get; init; }
	
	public TranslatedAndRotatedConvexShape(T baseShape, Location position, Rotation rotation) {
		BaseShape = baseShape;
		Position = position;
		Rotation = rotation;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedAndRotatedShape<T>(TranslatedAndRotatedConvexShape<T> operand) => new(operand.BaseShape, operand.Position, operand.Rotation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedAndRotatedConvexShape<T>(TranslatedAndRotatedShape<T> operand) => new(operand.BaseShape, operand.Position, operand.Rotation);
	
	#region Deferred Members
	public bool IsPhysicallyValid {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ((TranslatedAndRotatedShape<T>) this).IsPhysicallyValid;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToShapeSpace<TVal>(TVal val) where TVal : ITranslatable<TVal>, IPointRotatable<TVal> => ((TranslatedAndRotatedShape<T>) this).TransformToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToWorldSpace<TVal>(TVal val) where TVal : ITranslatable<TVal>, IPointRotatable<TVal> => ((TranslatedAndRotatedShape<T>) this).TransformToWorldSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToShapeSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal>, IPointRotatable<TVal> => ((TranslatedAndRotatedShape<T>) this).TransformToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToWorldSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal>, IPointRotatable<TVal> => ((TranslatedAndRotatedShape<T>) this).TransformToWorldSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ToString(string? format, IFormatProvider? formatProvider) => ((TranslatedAndRotatedShape<T>) this).ToString(format, formatProvider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => ((TranslatedAndRotatedShape<T>) this).TryFormat(destination, out charsWritten, format, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedAndRotatedConvexShape<T> Parse(string s, IFormatProvider? provider) => TranslatedAndRotatedShape<T>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(string? s, IFormatProvider? provider, out TranslatedAndRotatedConvexShape<T> result) {
		var returnValue = TranslatedAndRotatedShape<T>.TryParse(s, provider, out var r);
		result = r;
		return returnValue;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedAndRotatedConvexShape<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => TranslatedAndRotatedShape<T>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TranslatedAndRotatedConvexShape<T> result) {
		var returnValue = TranslatedAndRotatedShape<T>.TryParse(s, provider, out var r);
		result = r;
		return returnValue;
	}
	public static int SerializationByteSpanLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TranslatedAndRotatedShape<T>.SerializationByteSpanLength;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SerializeToBytes(Span<byte> dest, TranslatedAndRotatedConvexShape<T> src) => TranslatedAndRotatedShape<T>.SerializeToBytes(dest, src); 
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedAndRotatedConvexShape<T> DeserializeFromBytes(ReadOnlySpan<byte> src) => TranslatedAndRotatedShape<T>.DeserializeFromBytes(src);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TranslatedAndRotatedConvexShape<T> MovedBy(Vect v) => ((TranslatedAndRotatedShape<T>) this).MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedAndRotatedConvexShape<T> operator +(TranslatedAndRotatedConvexShape<T> left, Vect right) => ((TranslatedAndRotatedShape<T>) left) + right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedAndRotatedConvexShape<T> operator -(TranslatedAndRotatedConvexShape<T> left, Vect right) => ((TranslatedAndRotatedShape<T>) left) - right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedAndRotatedConvexShape<T> operator +(Vect left, TranslatedAndRotatedConvexShape<T> right) => left + ((TranslatedAndRotatedShape<T>) right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedAndRotatedConvexShape<T> operator *(TranslatedAndRotatedConvexShape<T> left, float right) => ((TranslatedAndRotatedShape<T>) left) * right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedAndRotatedConvexShape<T> operator /(TranslatedAndRotatedConvexShape<T> left, float right) => ((TranslatedAndRotatedShape<T>) left) / right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedAndRotatedConvexShape<T> operator *(float left, TranslatedAndRotatedConvexShape<T> right) => ((TranslatedAndRotatedShape<T>) right) * left;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TranslatedAndRotatedConvexShape<T> ScaledBy(float scalar) => ((TranslatedAndRotatedShape<T>) this).ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedAndRotatedConvexShape<T> Random() => TranslatedAndRotatedShape<T>.Random();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedAndRotatedConvexShape<T> Random(TranslatedAndRotatedConvexShape<T> minInclusive, TranslatedAndRotatedConvexShape<T> maxExclusive) => TranslatedAndRotatedShape<T>.Random(minInclusive, maxExclusive);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedAndRotatedConvexShape<T> Interpolate(TranslatedAndRotatedConvexShape<T> start, TranslatedAndRotatedConvexShape<T> end, float distance) => TranslatedAndRotatedShape<T>.Interpolate(start, end, distance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TranslatedAndRotatedConvexShape<T> Clamp(TranslatedAndRotatedConvexShape<T> min, TranslatedAndRotatedConvexShape<T> max) => ((TranslatedAndRotatedShape<T>) this).Clamp(min, max);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedAndRotatedConvexShape<T> operator *(TranslatedAndRotatedConvexShape<T> left, Rotation right) => ((TranslatedAndRotatedShape<T>) left) * right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedAndRotatedConvexShape<T> operator *(Rotation left, TranslatedAndRotatedConvexShape<T> right) => left * ((TranslatedAndRotatedShape<T>) right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TranslatedAndRotatedConvexShape<T> RotatedBy(Rotation rot) => ((TranslatedAndRotatedShape<T>) this).RotatedBy(rot);
	#endregion

	#region Equality
	public override bool Equals(object? obj) => obj is TranslatedAndRotatedConvexShape<T> other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(BaseShape, Position, Rotation);
	public bool Equals(TranslatedAndRotatedConvexShape<T> other) => BaseShape.Equals(other.BaseShape) && Position.Equals(other.Position) && Rotation.Equals(other.Rotation);
	public bool Equals(TranslatedAndRotatedConvexShape<T> other, float tolerance) => BaseShape.Equals(other.BaseShape, tolerance) && Position.Equals(other.Position, tolerance) && Rotation.Equals(other.Rotation, tolerance);
	public static bool operator ==(TranslatedAndRotatedConvexShape<T> left, TranslatedAndRotatedConvexShape<T> right) => left.Equals(right);
	public static bool operator !=(TranslatedAndRotatedConvexShape<T> left, TranslatedAndRotatedConvexShape<T> right) => !left.Equals(right);
	#endregion

	public Location PointClosestTo(Location location) => TransformToWorldSpace(BaseShape.PointClosestTo(TransformToShapeSpace(location)));
	public float DistanceFrom(Location location) => BaseShape.DistanceFrom(TransformToShapeSpace(location));
	public float DistanceSquaredFrom(Location location) => BaseShape.DistanceSquaredFrom(TransformToShapeSpace(location));
	public bool Contains(Location location) => BaseShape.Contains(TransformToShapeSpace(location));

	public Ray? ReflectionOf(Ray ray) => TransformToWorldSpace(BaseShape.ReflectionOf(TransformToShapeSpace(ray)));
	public Ray FastReflectionOf(Ray ray) => TransformToWorldSpace(BaseShape.FastReflectionOf(TransformToShapeSpace(ray)));
	public Angle? IncidentAngleWith(Ray ray) => BaseShape.IncidentAngleWith(TransformToShapeSpace(ray));
	public Angle FastIncidentAngleWith(Ray ray) => BaseShape.FastIncidentAngleWith(TransformToShapeSpace(ray));
	public BoundedRay? ReflectionOf(BoundedRay ray) => TransformToWorldSpace(BaseShape.ReflectionOf(TransformToShapeSpace(ray)));
	public BoundedRay FastReflectionOf(BoundedRay ray) => TransformToWorldSpace(BaseShape.FastReflectionOf(TransformToShapeSpace(ray)));
	public Angle? IncidentAngleWith(BoundedRay ray) => BaseShape.IncidentAngleWith(TransformToShapeSpace(ray));
	public Angle FastIncidentAngleWith(BoundedRay ray) => BaseShape.FastIncidentAngleWith(TransformToShapeSpace(ray));

	public Location ClosestPointOn(Line line) => TransformToWorldSpace(BaseShape.ClosestPointOn(TransformToShapeSpace(line)));
	public Location ClosestPointOn(Ray ray) => TransformToWorldSpace(BaseShape.ClosestPointOn(TransformToShapeSpace(ray)));
	public Location ClosestPointOn(BoundedRay ray) => TransformToWorldSpace(BaseShape.ClosestPointOn(TransformToShapeSpace(ray)));
	public Location PointClosestTo(Line line) => TransformToWorldSpace(BaseShape.PointClosestTo(TransformToShapeSpace(line)));
	public Location PointClosestTo(Ray ray) => TransformToWorldSpace(BaseShape.PointClosestTo(TransformToShapeSpace(ray)));
	public Location PointClosestTo(BoundedRay ray) => TransformToWorldSpace(BaseShape.PointClosestTo(TransformToShapeSpace(ray)));
	public float DistanceFrom(Line line) => BaseShape.DistanceFrom(TransformToShapeSpace(line));
	public float DistanceSquaredFrom(Line line) => BaseShape.DistanceSquaredFrom(TransformToShapeSpace(line));
	public float DistanceFrom(Ray ray) => BaseShape.DistanceFrom(TransformToShapeSpace(ray));
	public float DistanceSquaredFrom(Ray ray) => BaseShape.DistanceSquaredFrom(TransformToShapeSpace(ray));
	public float DistanceFrom(BoundedRay ray) => BaseShape.DistanceFrom(TransformToShapeSpace(ray));
	public float DistanceSquaredFrom(BoundedRay ray) => BaseShape.DistanceSquaredFrom(TransformToShapeSpace(ray));
	public bool Contains(BoundedRay ray) => BaseShape.Contains(TransformToShapeSpace(ray));

	public bool IsIntersectedBy(Line line) => BaseShape.IsIntersectedBy(TransformToShapeSpace(line));
	public bool IsIntersectedBy(Ray ray) => BaseShape.IsIntersectedBy(TransformToShapeSpace(ray));
	public bool IsIntersectedBy(BoundedRay ray) => BaseShape.IsIntersectedBy(TransformToShapeSpace(ray));
	public ConvexShapeLineIntersection? IntersectionWith(Line line) {
		var shapeSpaceResult = BaseShape.IntersectionWith(TransformToShapeSpace(line));
		return shapeSpaceResult == null
			? null
			: new(TransformToWorldSpace(shapeSpaceResult.Value.First), TransformToWorldSpace(shapeSpaceResult.Value.Second));
	}
	public ConvexShapeLineIntersection FastIntersectionWith(Line line) {
		var shapeSpaceResult = BaseShape.FastIntersectionWith(TransformToShapeSpace(line));
		return new(TransformToWorldSpace(shapeSpaceResult.First), TransformToWorldSpace(shapeSpaceResult.Second));
	}
	public ConvexShapeLineIntersection? IntersectionWith(Ray ray) {
		var shapeSpaceResult = BaseShape.IntersectionWith(TransformToShapeSpace(ray));
		return shapeSpaceResult == null
			? null
			: new(TransformToWorldSpace(shapeSpaceResult.Value.First), TransformToWorldSpace(shapeSpaceResult.Value.Second));
	}
	public ConvexShapeLineIntersection FastIntersectionWith(Ray ray) {
		var shapeSpaceResult = BaseShape.FastIntersectionWith(TransformToShapeSpace(ray));
		return new(TransformToWorldSpace(shapeSpaceResult.First), TransformToWorldSpace(shapeSpaceResult.Second));
	}
	public ConvexShapeLineIntersection? IntersectionWith(BoundedRay ray) {
		var shapeSpaceResult = BaseShape.IntersectionWith(TransformToShapeSpace(ray));
		return shapeSpaceResult == null
			? null
			: new(TransformToWorldSpace(shapeSpaceResult.Value.First), TransformToWorldSpace(shapeSpaceResult.Value.Second));
	}
	public ConvexShapeLineIntersection FastIntersectionWith(BoundedRay ray) {
		var shapeSpaceResult = BaseShape.FastIntersectionWith(TransformToShapeSpace(ray));
		return new(TransformToWorldSpace(shapeSpaceResult.First), TransformToWorldSpace(shapeSpaceResult.Second));
	}

	public float DistanceFrom(Plane plane) => BaseShape.DistanceFrom(TransformToShapeSpace(plane));
	public float DistanceSquaredFrom(Plane plane) => BaseShape.DistanceSquaredFrom(TransformToShapeSpace(plane));
	public float SignedDistanceFrom(Plane plane) => BaseShape.SignedDistanceFrom(TransformToShapeSpace(plane));
	public Location PointClosestTo(Plane plane) => TransformToWorldSpace(BaseShape.PointClosestTo(TransformToShapeSpace(plane)));
	public Location ClosestPointOn(Plane plane) => TransformToWorldSpace(BaseShape.ClosestPointOn(TransformToShapeSpace(plane)));
	public PlaneObjectRelationship RelationshipTo(Plane plane) => BaseShape.RelationshipTo(TransformToShapeSpace(plane));

	public Location SurfacePointClosestTo(Location point) => TransformToWorldSpace(BaseShape.SurfacePointClosestTo(TransformToShapeSpace(point)));
	public float SurfaceDistanceFrom(Location point) => BaseShape.SurfaceDistanceFrom(TransformToShapeSpace(point));
	public float SurfaceDistanceSquaredFrom(Location point) => BaseShape.SurfaceDistanceSquaredFrom(TransformToShapeSpace(point));
	public Location SurfacePointClosestTo(Line line) => TransformToWorldSpace(BaseShape.SurfacePointClosestTo(TransformToShapeSpace(line)));
	public Location ClosestPointToSurfaceOn(Line line) => TransformToWorldSpace(BaseShape.ClosestPointToSurfaceOn(TransformToShapeSpace(line)));
	public float SurfaceDistanceFrom(Line line) => BaseShape.SurfaceDistanceFrom(TransformToShapeSpace(line));
	public float SurfaceDistanceSquaredFrom(Line line) => BaseShape.SurfaceDistanceSquaredFrom(TransformToShapeSpace(line));
	public Location SurfacePointClosestTo(Ray ray) => TransformToWorldSpace(BaseShape.SurfacePointClosestTo(TransformToShapeSpace(ray)));
	public Location ClosestPointToSurfaceOn(Ray ray) => TransformToWorldSpace(BaseShape.ClosestPointToSurfaceOn(TransformToShapeSpace(ray)));
	public float SurfaceDistanceFrom(Ray ray) => BaseShape.SurfaceDistanceFrom(TransformToShapeSpace(ray));
	public float SurfaceDistanceSquaredFrom(Ray ray) => BaseShape.SurfaceDistanceSquaredFrom(TransformToShapeSpace(ray));
	public Location SurfacePointClosestTo(BoundedRay ray) => TransformToWorldSpace(BaseShape.SurfacePointClosestTo(TransformToShapeSpace(ray)));
	public Location ClosestPointToSurfaceOn(BoundedRay ray) => TransformToWorldSpace(BaseShape.ClosestPointToSurfaceOn(TransformToShapeSpace(ray)));
	public float SurfaceDistanceFrom(BoundedRay ray) => BaseShape.SurfaceDistanceFrom(TransformToShapeSpace(ray));
	public float SurfaceDistanceSquaredFrom(BoundedRay ray) => BaseShape.SurfaceDistanceSquaredFrom(TransformToShapeSpace(ray));
	public Location SurfacePointClosestTo(Plane plane) => TransformToWorldSpace(BaseShape.SurfacePointClosestTo(TransformToShapeSpace(plane)));
	public Location ClosestPointToSurfaceOn(Plane plane) => TransformToWorldSpace(BaseShape.ClosestPointToSurfaceOn(TransformToShapeSpace(plane)));
	public float SurfaceDistanceFrom(Plane plane) => BaseShape.SurfaceDistanceFrom(TransformToShapeSpace(plane));
	public float SurfaceDistanceSquaredFrom(Plane plane) => BaseShape.SurfaceDistanceSquaredFrom(TransformToShapeSpace(plane));
}