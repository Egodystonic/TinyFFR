// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

public interface IPositionedShape : IShape;
public interface IPositionedShape<TSelf> : IPositionedShape, IShape<TSelf>, ITranslatable<TSelf> where TSelf : IPositionedShape<TSelf>; 
public interface IPositionedShape<TSelf, TBase> : IPositionedShape<TSelf> where TSelf : IPositionedShape<TSelf, TBase> where TBase : IShape<TBase> { TBase BaseShape { get; init; } } 
public interface IPositionedConvexShape : IPositionedShape, IConvexShape; 
public interface IPositionedConvexShape<TSelf> : IPositionedShape<TSelf>, IPositionedConvexShape, IConvexShape<TSelf> where TSelf : IPositionedConvexShape<TSelf>;
public interface IPositionedConvexShape<TSelf, TBase> : IPositionedConvexShape<TSelf>, IPositionedShape<TSelf, TBase> where TSelf : IPositionedConvexShape<TSelf, TBase> where TBase : IConvexShape<TBase>; 

public readonly struct PositionedShape<T> : IPositionedShape<PositionedShape<T>, T> where T : IShape<T> {
	const string StringComponentSeparator = " @ ";
	public T BaseShape { get; init; }
	public Location Position { get; init; }

	public bool IsPhysicallyValid => BaseShape.IsPhysicallyValid && Position.IsPhysicallyValid;
	
	public PositionedShape(T baseShape, Location position) {
		BaseShape = baseShape;
		Position = position;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TranslateToShapeSpace<TVal>(TVal val) where TVal : ITranslatable<TVal> => val - Position.AsVect();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TranslateToWorldSpace<TVal>(TVal val) where TVal : ITranslatable<TVal> => val + Position.AsVect();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TranslateToShapeSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal> => val?.Minus(Position.AsVect());
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TranslateToWorldSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal> => val?.Plus(Position.AsVect());

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
	public static PositionedShape<T> Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out PositionedShape<T> result) => TryParse(s.AsSpan(), provider, out result);
	public static PositionedShape<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		if (!TryParse(s, provider, out var result)) {
			throw new ArgumentException($"Given input string \"{s}\" does not represent a valid Positioned {typeof(T).Name}.", nameof(s));
		}
		return result;
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out PositionedShape<T> result) {
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
	public static void SerializeToBytes(Span<byte> dest, PositionedShape<T> src) {
		T.SerializeToBytes(dest, src.BaseShape);
		Location.SerializeToBytes(dest[T.SerializationByteSpanLength..], src.Position);
	}
	public static PositionedShape<T> DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			T.DeserializeFromBytes(src),
			Location.DeserializeFromBytes(src[T.SerializationByteSpanLength..])
		);
	}
	#endregion

	#region Move / Scale
	public PositionedShape<T> MovedBy(Vect v) => new(BaseShape, Position.MovedBy(v));
	public static PositionedShape<T> operator +(PositionedShape<T> left, Vect right) => new(left.BaseShape, left.Position + right);
	public static PositionedShape<T> operator -(PositionedShape<T> left, Vect right) => new(left.BaseShape, left.Position - right);
	public static PositionedShape<T> operator +(Vect left, PositionedShape<T> right) => new(right.BaseShape, right.Position + left);

	public static PositionedShape<T> operator *(PositionedShape<T> left, float right) => new(left.BaseShape * right, left.Position);
	public static PositionedShape<T> operator /(PositionedShape<T> left, float right) => new(left.BaseShape / right, left.Position);
	public static PositionedShape<T> operator *(float left, PositionedShape<T> right) => new(left * right.BaseShape, right.Position);
	public PositionedShape<T> ScaledBy(float scalar) => new(BaseShape * scalar, Position);
	#endregion

	#region Equality
	public override bool Equals(object? obj) => obj is PositionedShape<T> other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(BaseShape, Position);
	public bool Equals(PositionedShape<T> other) => BaseShape.Equals(other.BaseShape) && Position.Equals(other.Position);
	public bool Equals(PositionedShape<T> other, float tolerance) => BaseShape.Equals(other.BaseShape, tolerance) && Position.Equals(other.Position, tolerance);
	public static bool operator ==(PositionedShape<T> left, PositionedShape<T> right) => left.Equals(right);
	public static bool operator !=(PositionedShape<T> left, PositionedShape<T> right) => !left.Equals(right);
	#endregion

	#region Random / Interp / Clamp
	public static PositionedShape<T> Random() => new(T.Random(), Location.Random());
	public static PositionedShape<T> Random(PositionedShape<T> minInclusive, PositionedShape<T> maxExclusive) {
		return new(
			T.Random(minInclusive.BaseShape, maxExclusive.BaseShape),
			Location.Random(minInclusive.Position, maxExclusive.Position)
		);
	}
	public static PositionedShape<T> Interpolate(PositionedShape<T> start, PositionedShape<T> end, float distance) {
		return new(
			T.Interpolate(start.BaseShape, end.BaseShape, distance),
			Location.Interpolate(start.Position, end.Position, distance)
		);
	}
	public PositionedShape<T> Clamp(PositionedShape<T> min, PositionedShape<T> max) {
		return new(
			BaseShape.Clamp(min.BaseShape, max.BaseShape),
			Position.Clamp(min.Position, max.Position)
		);
	}
	#endregion
}

public readonly struct PositionedConvexShape<T> : IPositionedConvexShape<PositionedConvexShape<T>, T> where T : IConvexShape<T> {
	public T BaseShape { get; init; }
	public Location Position { get; init; }
	
	public PositionedConvexShape(T baseShape, Location position) {
		BaseShape = baseShape;
		Position = position;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PositionedShape<T>(PositionedConvexShape<T> operand) => new(operand.BaseShape, operand.Position);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PositionedConvexShape<T>(PositionedShape<T> operand) => new(operand.BaseShape, operand.Position);
	
	#region Deferred Members
	public bool IsPhysicallyValid {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ((PositionedShape<T>) this).IsPhysicallyValid;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TranslateToShapeSpace<TVal>(TVal val) where TVal : ITranslatable<TVal> => ((PositionedShape<T>) this).TranslateToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TranslateToWorldSpace<TVal>(TVal val) where TVal : ITranslatable<TVal> => ((PositionedShape<T>) this).TranslateToWorldSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TranslateToShapeSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal> => ((PositionedShape<T>) this).TranslateToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TranslateToWorldSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal> => ((PositionedShape<T>) this).TranslateToWorldSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ToString(string? format, IFormatProvider? formatProvider) => ((PositionedShape<T>) this).ToString(format, formatProvider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => ((PositionedShape<T>) this).TryFormat(destination, out charsWritten, format, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionedConvexShape<T> Parse(string s, IFormatProvider? provider) => PositionedShape<T>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(string? s, IFormatProvider? provider, out PositionedConvexShape<T> result) {
		var returnValue = PositionedShape<T>.TryParse(s, provider, out var r);
		result = r;
		return returnValue;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionedConvexShape<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => PositionedShape<T>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out PositionedConvexShape<T> result) {
		var returnValue = PositionedShape<T>.TryParse(s, provider, out var r);
		result = r;
		return returnValue;
	}
	public static int SerializationByteSpanLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => PositionedShape<T>.SerializationByteSpanLength;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SerializeToBytes(Span<byte> dest, PositionedConvexShape<T> src) => PositionedShape<T>.SerializeToBytes(dest, src); 
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionedConvexShape<T> DeserializeFromBytes(ReadOnlySpan<byte> src) => PositionedShape<T>.DeserializeFromBytes(src);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PositionedConvexShape<T> MovedBy(Vect v) => ((PositionedShape<T>) this).MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionedConvexShape<T> operator +(PositionedConvexShape<T> left, Vect right) => ((PositionedShape<T>) left) + right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionedConvexShape<T> operator -(PositionedConvexShape<T> left, Vect right) => ((PositionedShape<T>) left) - right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionedConvexShape<T> operator +(Vect left, PositionedConvexShape<T> right) => left + ((PositionedShape<T>) right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionedConvexShape<T> operator *(PositionedConvexShape<T> left, float right) => ((PositionedShape<T>) left) * right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionedConvexShape<T> operator /(PositionedConvexShape<T> left, float right) => ((PositionedShape<T>) left) / right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionedConvexShape<T> operator *(float left, PositionedConvexShape<T> right) => ((PositionedShape<T>) right) * left;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PositionedConvexShape<T> ScaledBy(float scalar) => ((PositionedShape<T>) this).ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionedConvexShape<T> Random() => PositionedShape<T>.Random();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionedConvexShape<T> Random(PositionedConvexShape<T> minInclusive, PositionedConvexShape<T> maxExclusive) => PositionedShape<T>.Random(minInclusive, maxExclusive);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PositionedConvexShape<T> Interpolate(PositionedConvexShape<T> start, PositionedConvexShape<T> end, float distance) => PositionedShape<T>.Interpolate(start, end, distance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PositionedConvexShape<T> Clamp(PositionedConvexShape<T> min, PositionedConvexShape<T> max) => ((PositionedShape<T>) this).Clamp(min, max);
	#endregion

	#region Equality
	public override bool Equals(object? obj) => obj is PositionedConvexShape<T> other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(BaseShape, Position);
	public bool Equals(PositionedConvexShape<T> other) => BaseShape.Equals(other.BaseShape) && Position.Equals(other.Position);
	public bool Equals(PositionedConvexShape<T> other, float tolerance) => BaseShape.Equals(other.BaseShape, tolerance) && Position.Equals(other.Position, tolerance);
	public static bool operator ==(PositionedConvexShape<T> left, PositionedConvexShape<T> right) => left.Equals(right);
	public static bool operator !=(PositionedConvexShape<T> left, PositionedConvexShape<T> right) => !left.Equals(right);
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