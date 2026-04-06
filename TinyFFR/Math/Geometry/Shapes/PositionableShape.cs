// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

public interface IPositionableShape : IShape;
public interface IPositionableShape<TSelf> : IPositionableShape, IShape<TSelf>, ITranslatable<TSelf> where TSelf : IPositionableShape<TSelf>; 
public interface IPositionableShape<TSelf, TBase> : IPositionableShape<TSelf> where TSelf : IPositionableShape<TSelf, TBase> where TBase : IShape<TBase> { TBase BaseShape { get; init; } } 
public interface IPositionableConvexShape : IPositionableShape, IConvexShape; 
public interface IPositionableConvexShape<TSelf> : IPositionableShape<TSelf>, IPositionableConvexShape, IConvexShape<TSelf> where TSelf : IPositionableConvexShape<TSelf>;
public interface IPositionableConvexShape<TSelf, TBase> : IPositionableConvexShape<TSelf>, IPositionableShape<TSelf, TBase> where TSelf : IPositionableConvexShape<TSelf, TBase> where TBase : IConvexShape<TBase>; 

public readonly struct PositionableShape<T> : IPositionableShape<PositionableShape<T>, T> where T : IShape<T> {
	const string StringComponentSeparator = " @ ";
	public T BaseShape { get; init; }
	public Location Position { get; init; }

	public bool IsPhysicallyValid => BaseShape.IsPhysicallyValid && Position.IsPhysicallyValid;
	
	public PositionableShape(T baseShape, Location position) {
		BaseShape = baseShape;
		Position = position;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TVal TranslateToShapeSpace<TVal>(TVal val) where TVal : ITranslatable<TVal> => val - Position.AsVect();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TVal TranslateToWorldSpace<TVal>(TVal val) where TVal : ITranslatable<TVal> => val + Position.AsVect();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TVal? TranslateToShapeSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal> => val == null ? null : val.Value - Position.AsVect();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TVal? TranslateToWorldSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal> => val == null ? null : val.Value + Position.AsVect();

	#region ToString / Format / Parse
	public string ToString(string? format, IFormatProvider? formatProvider) {
		return BaseShape.ToString(format, formatProvider) + StringComponentSeparator + Position.ToString(format, formatProvider);
	}
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		charsWritten = 0;
		
		if (!BaseShape.TryFormat(destination, out var c, format, provider)) return false;
		charsWritten += c;
		destination = destination[c..];
		
		if (!StringComponentSeparator.TryCopyTo(destination)) return false;
		charsWritten += StringComponentSeparator.Length;
		destination = destination[StringComponentSeparator.Length..];
		
		if (!Position.TryFormat(destination, out c, format, provider)) return false;
		charsWritten += c;
		return true;
	}
	public static PositionableShape<T> Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out PositionableShape<T> result) => TryParse(s.AsSpan(), provider, out result);
	public static PositionableShape<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		if (!TryParse(s, provider, out var result)) {
			throw new ArgumentException($"Given input string \"{s}\" does not represent a valid positionable {typeof(T).Name}.", nameof(s));
		}
		return result;
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out PositionableShape<T> result) {
		result = default;
		
		var splitIndex = s.IndexOf(StringComponentSeparator);
		if (splitIndex < 0) return false;
		
		if (!T.TryParse(s[..splitIndex], provider, out var baseShape)) return false;
		if (!Location.TryParse(s[(splitIndex + StringComponentSeparator.Length)..], provider, out var position)) return false;
		
		result = new(baseShape, position);
		return true;
	}
	#endregion

	#region Byte Span Serialization / Deserialization
	public static int SerializationByteSpanLength => T.SerializationByteSpanLength + Location.SerializationByteSpanLength;
	public static void SerializeToBytes(Span<byte> dest, PositionableShape<T> src) {
		T.SerializeToBytes(dest, src.BaseShape);
		Location.SerializeToBytes(dest[T.SerializationByteSpanLength..], src.Position);
	}
	public static PositionableShape<T> DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			T.DeserializeFromBytes(src),
			Location.DeserializeFromBytes(src[T.SerializationByteSpanLength..])
		);
	}
	#endregion

	#region Move / Scale
	public PositionableShape<T> MovedBy(Vect v) => new(BaseShape, Position.MovedBy(v));
	public static PositionableShape<T> operator +(PositionableShape<T> left, Vect right) => new(left.BaseShape, left.Position + right);
	public static PositionableShape<T> operator -(PositionableShape<T> left, Vect right) => new(left.BaseShape, left.Position - right);
	public static PositionableShape<T> operator +(Vect left, PositionableShape<T> right) => new(right.BaseShape, right.Position + left);

	public static PositionableShape<T> operator *(PositionableShape<T> left, float right) => new(left.BaseShape * right, left.Position);
	public static PositionableShape<T> operator /(PositionableShape<T> left, float right) => new(left.BaseShape / right, left.Position);
	public static PositionableShape<T> operator *(float left, PositionableShape<T> right) => new(left * right.BaseShape, right.Position);
	public PositionableShape<T> ScaledBy(float scalar) => new(BaseShape * scalar, Position);
	#endregion

	#region Equality
	public override bool Equals(object? obj) => obj is PositionableShape<T> other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(BaseShape, Position);
	public bool Equals(PositionableShape<T> other) => BaseShape.Equals(other.BaseShape) && Position.Equals(other.Position);
	public bool Equals(PositionableShape<T> other, float tolerance) => BaseShape.Equals(other.BaseShape, tolerance) && Position.Equals(other.Position, tolerance);
	public static bool operator ==(PositionableShape<T> left, PositionableShape<T> right) => left.Equals(right);
	public static bool operator !=(PositionableShape<T> left, PositionableShape<T> right) => !left.Equals(right);
	#endregion

	#region Random / Interp / Clamp
	public static PositionableShape<T> Random() => new(T.Random(), Location.Random());
	public static PositionableShape<T> Random(PositionableShape<T> minInclusive, PositionableShape<T> maxExclusive) {
		return new(
			T.Random(minInclusive.BaseShape, maxExclusive.BaseShape),
			Location.Random(minInclusive.Position, maxExclusive.Position)
		);
	}
	public static PositionableShape<T> Interpolate(PositionableShape<T> start, PositionableShape<T> end, float distance) {
		return new(
			T.Interpolate(start.BaseShape, end.BaseShape, distance),
			Location.Interpolate(start.Position, end.Position, distance)
		);
	}
	public PositionableShape<T> Clamp(PositionableShape<T> min, PositionableShape<T> max) {
		return new(
			BaseShape.Clamp(min.BaseShape, max.BaseShape),
			Position.Clamp(min.Position, max.Position)
		);
	}
	#endregion
}

public readonly struct PositionableConvexShape<T> : IPositionableConvexShape<PositionableConvexShape<T>, T> where T : IConvexShape<T> {
	public T BaseShape { get; init; }
	public Location Position { get; init; }
	
	public PositionableConvexShape(T baseShape, Location position) {
		BaseShape = baseShape;
		Position = position;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PositionableShape<T>(PositionableConvexShape<T> operand) => new(operand.BaseShape, operand.Position);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PositionableConvexShape<T>(PositionableShape<T> operand) => new(operand.BaseShape, operand.Position);
	
	#region Deferred Members
	public bool IsPhysicallyValid {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ((PositionableShape<T>) this).IsPhysicallyValid;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TVal TranslateToShapeSpace<TVal>(TVal val) where TVal : ITranslatable<TVal> => ((PositionableShape<T>) this).TranslateToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TVal TranslateToWorldSpace<TVal>(TVal val) where TVal : ITranslatable<TVal> => ((PositionableShape<T>) this).TranslateToWorldSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TVal? TranslateToShapeSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal> => ((PositionableShape<T>) this).TranslateToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TVal? TranslateToWorldSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal> => ((PositionableShape<T>) this).TranslateToWorldSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ToString(string? format, IFormatProvider? formatProvider) => ((PositionableShape<T>) this).ToString(format, formatProvider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => ((PositionableShape<T>) this).TryFormat(destination, out charsWritten, format, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionableConvexShape<T> Parse(string s, IFormatProvider? provider) => PositionableShape<T>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(string? s, IFormatProvider? provider, out PositionableConvexShape<T> result) {
		var returnValue = PositionableShape<T>.TryParse(s, provider, out var r);
		result = r;
		return returnValue;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionableConvexShape<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => PositionableShape<T>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out PositionableConvexShape<T> result) {
		var returnValue = PositionableShape<T>.TryParse(s, provider, out var r);
		result = r;
		return returnValue;
	}
	public static int SerializationByteSpanLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => PositionableShape<T>.SerializationByteSpanLength;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SerializeToBytes(Span<byte> dest, PositionableConvexShape<T> src) => PositionableShape<T>.SerializeToBytes(dest, src); 
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionableConvexShape<T> DeserializeFromBytes(ReadOnlySpan<byte> src) => PositionableShape<T>.DeserializeFromBytes(src);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PositionableConvexShape<T> MovedBy(Vect v) => ((PositionableShape<T>) this).MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionableConvexShape<T> operator +(PositionableConvexShape<T> left, Vect right) => ((PositionableShape<T>) left) + right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionableConvexShape<T> operator -(PositionableConvexShape<T> left, Vect right) => ((PositionableShape<T>) left) - right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionableConvexShape<T> operator +(Vect left, PositionableConvexShape<T> right) => left + ((PositionableShape<T>) right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionableConvexShape<T> operator *(PositionableConvexShape<T> left, float right) => ((PositionableShape<T>) left) * right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionableConvexShape<T> operator /(PositionableConvexShape<T> left, float right) => ((PositionableShape<T>) left) / right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionableConvexShape<T> operator *(float left, PositionableConvexShape<T> right) => ((PositionableShape<T>) right) * left;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PositionableConvexShape<T> ScaledBy(float scalar) => ((PositionableShape<T>) this).ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionableConvexShape<T> Random() => PositionableShape<T>.Random();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionableConvexShape<T> Random(PositionableConvexShape<T> minInclusive, PositionableConvexShape<T> maxExclusive) => PositionableShape<T>.Random(minInclusive, maxExclusive);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionableConvexShape<T> Interpolate(PositionableConvexShape<T> start, PositionableConvexShape<T> end, float distance) => PositionableShape<T>.Interpolate(start, end, distance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PositionableConvexShape<T> Clamp(PositionableConvexShape<T> min, PositionableConvexShape<T> max) => ((PositionableShape<T>) this).Clamp(min, max);
	#endregion

	#region Equality
	public override bool Equals(object? obj) => obj is PositionableConvexShape<T> other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(BaseShape, Position);
	public bool Equals(PositionableConvexShape<T> other) => BaseShape.Equals(other.BaseShape) && Position.Equals(other.Position);
	public bool Equals(PositionableConvexShape<T> other, float tolerance) => BaseShape.Equals(other.BaseShape, tolerance) && Position.Equals(other.Position, tolerance);
	public static bool operator ==(PositionableConvexShape<T> left, PositionableConvexShape<T> right) => left.Equals(right);
	public static bool operator !=(PositionableConvexShape<T> left, PositionableConvexShape<T> right) => !left.Equals(right);
	#endregion

	public Location PointClosestTo(Location location) => TranslateToWorldSpace(BaseShape.PointClosestTo(TranslateToShapeSpace(location)));
	public float DistanceFrom(Location location) => BaseShape.DistanceFrom(TranslateToShapeSpace(location));
	public float DistanceSquaredFrom(Location location) => BaseShape.DistanceSquaredFrom(TranslateToShapeSpace(location));
	public bool Contains(Location location) => BaseShape.Contains(TranslateToShapeSpace(location));

	public Ray? ReflectionOf(Ray ray) => TranslateToWorldSpace(BaseShape.ReflectionOf(TranslateToShapeSpace(ray)));
	public Ray FastReflectionOf(Ray ray) => TranslateToWorldSpace(BaseShape.FastReflectionOf(TranslateToShapeSpace(ray)));
	public Angle? IncidentAngleWith(Ray ray) => BaseShape.IncidentAngleWith(TranslateToShapeSpace(ray));
	public Angle FastIncidentAngleWith(Ray ray) => BaseShape.FastIncidentAngleWith(TranslateToShapeSpace(ray));
	public BoundedRay? ReflectionOf(BoundedRay ray) => TranslateToWorldSpace(BaseShape.ReflectionOf(TranslateToShapeSpace(ray)));
	public BoundedRay FastReflectionOf(BoundedRay ray) => TranslateToWorldSpace(BaseShape.FastReflectionOf(TranslateToShapeSpace(ray)));
	public Angle? IncidentAngleWith(BoundedRay ray) => BaseShape.IncidentAngleWith(TranslateToShapeSpace(ray));
	public Angle FastIncidentAngleWith(BoundedRay ray) => BaseShape.FastIncidentAngleWith(TranslateToShapeSpace(ray));

	public Location ClosestPointOn(Line line) => TranslateToWorldSpace(BaseShape.ClosestPointOn(TranslateToShapeSpace(line)));
	public Location ClosestPointOn(Ray ray) => TranslateToWorldSpace(BaseShape.ClosestPointOn(TranslateToShapeSpace(ray)));
	public Location ClosestPointOn(BoundedRay ray) => TranslateToWorldSpace(BaseShape.ClosestPointOn(TranslateToShapeSpace(ray)));
	public Location PointClosestTo(Line line) => TranslateToWorldSpace(BaseShape.PointClosestTo(TranslateToShapeSpace(line)));
	public Location PointClosestTo(Ray ray) => TranslateToWorldSpace(BaseShape.PointClosestTo(TranslateToShapeSpace(ray)));
	public Location PointClosestTo(BoundedRay ray) => TranslateToWorldSpace(BaseShape.PointClosestTo(TranslateToShapeSpace(ray)));
	public float DistanceFrom(Line line) => BaseShape.DistanceFrom(TranslateToShapeSpace(line));
	public float DistanceSquaredFrom(Line line) => BaseShape.DistanceSquaredFrom(TranslateToShapeSpace(line));
	public float DistanceFrom(Ray ray) => BaseShape.DistanceFrom(TranslateToShapeSpace(ray));
	public float DistanceSquaredFrom(Ray ray) => BaseShape.DistanceSquaredFrom(TranslateToShapeSpace(ray));
	public float DistanceFrom(BoundedRay ray) => BaseShape.DistanceFrom(TranslateToShapeSpace(ray));
	public float DistanceSquaredFrom(BoundedRay ray) => BaseShape.DistanceSquaredFrom(TranslateToShapeSpace(ray));
	public bool Contains(BoundedRay ray) => BaseShape.Contains(TranslateToShapeSpace(ray));

	public bool IsIntersectedBy(Line line) => BaseShape.IsIntersectedBy(TranslateToShapeSpace(line));
	public bool IsIntersectedBy(Ray ray) => BaseShape.IsIntersectedBy(TranslateToShapeSpace(ray));
	public bool IsIntersectedBy(BoundedRay ray) => BaseShape.IsIntersectedBy(TranslateToShapeSpace(ray));
	public ConvexShapeLineIntersection? IntersectionWith(Line line) {
		var shapeSpaceResult = BaseShape.IntersectionWith(TranslateToShapeSpace(line));
		return shapeSpaceResult == null
			? null
			: new(TranslateToWorldSpace(shapeSpaceResult.Value.First), TranslateToWorldSpace(shapeSpaceResult.Value.Second));
	}
	public ConvexShapeLineIntersection FastIntersectionWith(Line line) {
		var shapeSpaceResult = BaseShape.FastIntersectionWith(TranslateToShapeSpace(line));
		return new(TranslateToWorldSpace(shapeSpaceResult.First), TranslateToWorldSpace(shapeSpaceResult.Second));
	}
	public ConvexShapeLineIntersection? IntersectionWith(Ray ray) {
		var shapeSpaceResult = BaseShape.IntersectionWith(TranslateToShapeSpace(ray));
		return shapeSpaceResult == null
			? null
			: new(TranslateToWorldSpace(shapeSpaceResult.Value.First), TranslateToWorldSpace(shapeSpaceResult.Value.Second));
	}
	public ConvexShapeLineIntersection FastIntersectionWith(Ray ray) {
		var shapeSpaceResult = BaseShape.FastIntersectionWith(TranslateToShapeSpace(ray));
		return new(TranslateToWorldSpace(shapeSpaceResult.First), TranslateToWorldSpace(shapeSpaceResult.Second));
	}
	public ConvexShapeLineIntersection? IntersectionWith(BoundedRay ray) {
		var shapeSpaceResult = BaseShape.IntersectionWith(TranslateToShapeSpace(ray));
		return shapeSpaceResult == null
			? null
			: new(TranslateToWorldSpace(shapeSpaceResult.Value.First), TranslateToWorldSpace(shapeSpaceResult.Value.Second));
	}
	public ConvexShapeLineIntersection FastIntersectionWith(BoundedRay ray) {
		var shapeSpaceResult = BaseShape.FastIntersectionWith(TranslateToShapeSpace(ray));
		return new(TranslateToWorldSpace(shapeSpaceResult.First), TranslateToWorldSpace(shapeSpaceResult.Second));
	}

	public float DistanceFrom(Plane plane) => BaseShape.DistanceFrom(TranslateToShapeSpace(plane));
	public float DistanceSquaredFrom(Plane plane) => BaseShape.DistanceSquaredFrom(TranslateToShapeSpace(plane));
	public float SignedDistanceFrom(Plane plane) => BaseShape.SignedDistanceFrom(TranslateToShapeSpace(plane));
	public Location PointClosestTo(Plane plane) => BaseShape.PointClosestTo(TranslateToShapeSpace(plane));
	public Location ClosestPointOn(Plane plane) => BaseShape.ClosestPointOn(TranslateToShapeSpace(plane));
	public PlaneObjectRelationship RelationshipTo(Plane plane) => BaseShape.RelationshipTo(TranslateToShapeSpace(plane));

	public Location SurfacePointClosestTo(Location point) => TranslateToWorldSpace(BaseShape.SurfacePointClosestTo(TranslateToShapeSpace(point)));
	public float SurfaceDistanceFrom(Location point) => BaseShape.SurfaceDistanceFrom(TranslateToShapeSpace(point));
	public float SurfaceDistanceSquaredFrom(Location point) => BaseShape.SurfaceDistanceSquaredFrom(TranslateToShapeSpace(point));
	public Location SurfacePointClosestTo(Line line) => TranslateToWorldSpace(BaseShape.SurfacePointClosestTo(TranslateToShapeSpace(line)));
	public Location ClosestPointToSurfaceOn(Line line) => TranslateToWorldSpace(BaseShape.ClosestPointToSurfaceOn(TranslateToShapeSpace(line)));
	public float SurfaceDistanceFrom(Line line) => BaseShape.SurfaceDistanceFrom(TranslateToShapeSpace(line));
	public float SurfaceDistanceSquaredFrom(Line line) => BaseShape.SurfaceDistanceSquaredFrom(TranslateToShapeSpace(line));
	public Location SurfacePointClosestTo(Ray ray) => TranslateToWorldSpace(BaseShape.SurfacePointClosestTo(TranslateToShapeSpace(ray)));
	public Location ClosestPointToSurfaceOn(Ray ray) => TranslateToWorldSpace(BaseShape.ClosestPointToSurfaceOn(TranslateToShapeSpace(ray)));
	public float SurfaceDistanceFrom(Ray ray) => BaseShape.SurfaceDistanceFrom(TranslateToShapeSpace(ray));
	public float SurfaceDistanceSquaredFrom(Ray ray) => BaseShape.SurfaceDistanceSquaredFrom(TranslateToShapeSpace(ray));
	public Location SurfacePointClosestTo(BoundedRay ray) => TranslateToWorldSpace(BaseShape.SurfacePointClosestTo(TranslateToShapeSpace(ray)));
	public Location ClosestPointToSurfaceOn(BoundedRay ray) => TranslateToWorldSpace(BaseShape.ClosestPointToSurfaceOn(TranslateToShapeSpace(ray)));
	public float SurfaceDistanceFrom(BoundedRay ray) => BaseShape.SurfaceDistanceFrom(TranslateToShapeSpace(ray));
	public float SurfaceDistanceSquaredFrom(BoundedRay ray) => BaseShape.SurfaceDistanceSquaredFrom(TranslateToShapeSpace(ray));
	public Location SurfacePointClosestTo(Plane plane) => TranslateToWorldSpace(BaseShape.SurfacePointClosestTo(TranslateToShapeSpace(plane)));
	public Location ClosestPointToSurfaceOn(Plane plane) => TranslateToWorldSpace(BaseShape.ClosestPointToSurfaceOn(TranslateToShapeSpace(plane)));
	public float SurfaceDistanceFrom(Plane plane) => BaseShape.SurfaceDistanceFrom(TranslateToShapeSpace(plane));
	public float SurfaceDistanceSquaredFrom(Plane plane) => BaseShape.SurfaceDistanceSquaredFrom(TranslateToShapeSpace(plane));
}