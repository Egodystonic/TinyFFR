// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

public interface ITransformableShape : IShape;
public interface ITransformableShape<TSelf> : ITransformableShape, IShape<TSelf>, ITransformable<TSelf> where TSelf : ITransformableShape<TSelf>; 
public interface ITransformableShape<TSelf, TBase> : ITransformableShape<TSelf> where TSelf : ITransformableShape<TSelf, TBase> where TBase : IShape<TBase> { TBase BaseShape { get; init; } } 
public interface ITransformableConvexShape : ITransformableShape, IConvexShape; 
public interface ITransformableConvexShape<TSelf> : ITransformableShape<TSelf>, ITransformableConvexShape, IConvexShape<TSelf> where TSelf : ITransformableConvexShape<TSelf>;
public interface ITransformableConvexShape<TSelf, TBase> : ITransformableConvexShape<TSelf>, ITransformableShape<TSelf, TBase> where TSelf : ITransformableConvexShape<TSelf, TBase> where TBase : IConvexShape<TBase>;

public readonly struct TransformableShape<T> : ITransformableShape<TransformableShape<T>, T> where T : IShape<T> {
	const string StringComponentSeparator = " @ ";
	public T BaseShape { get; init; }
	public Transform Transform { get; init; }

	public bool IsPhysicallyValid => BaseShape.IsPhysicallyValid && Transform.IsPhysicallyValid;
	
	public TransformableShape(T baseShape, Transform transform) {
		BaseShape = baseShape;
		Transform = transform;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToShapeSpace<TVal>(TVal val) where TVal : ITransformable<TVal> => val.TransformedByInverseOf(Transform);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToWorldSpace<TVal>(TVal val) where TVal : ITransformable<TVal> => val.TransformedBy(Transform);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToShapeSpace<TVal>(TVal? val) where TVal : struct, ITransformable<TVal> => val?.TransformedByInverseOf(Transform);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToWorldSpace<TVal>(TVal? val) where TVal : struct, ITransformable<TVal> => val?.TransformedBy(Transform);

	#region ToString / Format / Parse
	public string ToString(string? format, IFormatProvider? formatProvider) {
		return BaseShape.ToString(format, formatProvider) + StringComponentSeparator + Transform.ToString(format, formatProvider);
	}
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		charsWritten = 0;
		
		if (!BaseShape.TryFormat(destination, out var c, format, provider)) return false;
		charsWritten += c;
		destination = destination[c..];
		
		if (!StringComponentSeparator.TryCopyTo(destination)) return false;
		charsWritten += StringComponentSeparator.Length;
		destination = destination[StringComponentSeparator.Length..];
		
		if (!Transform.TryFormat(destination, out c, format, provider)) return false;
		charsWritten += c;
		return true;
	}
	public static TransformableShape<T> Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out TransformableShape<T> result) => TryParse(s.AsSpan(), provider, out result);
	public static TransformableShape<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		if (!TryParse(s, provider, out var result)) {
			throw new ArgumentException($"Given input string \"{s}\" does not represent a valid transformable {typeof(T).Name}.", nameof(s));
		}
		return result;
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TransformableShape<T> result) {
		result = default;
		
		var splitIndex = s.IndexOf(StringComponentSeparator);
		if (splitIndex < 0) return false;
		
		if (!T.TryParse(s[..splitIndex], provider, out var baseShape)) return false;
		if (!Transform.TryParse(s[(splitIndex + StringComponentSeparator.Length)..], provider, out var transform)) return false;
		
		result = new(baseShape, transform);
		return true;
	}
	#endregion

	#region Byte Span Serialization / Deserialization
	public static int SerializationByteSpanLength => T.SerializationByteSpanLength + Transform.SerializationByteSpanLength;
	public static void SerializeToBytes(Span<byte> dest, TransformableShape<T> src) {
		T.SerializeToBytes(dest, src.BaseShape);
		Transform.SerializeToBytes(dest[T.SerializationByteSpanLength..], src.Transform);
	}
	public static TransformableShape<T> DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			T.DeserializeFromBytes(src),
			Transform.DeserializeFromBytes(src[T.SerializationByteSpanLength..])
		);
	}
	#endregion

	#region Move / Rotate / Scale
	public TransformableShape<T> MovedBy(Vect v) => new(BaseShape, Position.MovedBy(v));
	public static TransformableShape<T> operator +(TransformableShape<T> left, Vect right) => new(left.BaseShape, left.Position + right);
	public static TransformableShape<T> operator -(TransformableShape<T> left, Vect right) => new(left.BaseShape, left.Position - right);
	public static TransformableShape<T> operator +(Vect left, TransformableShape<T> right) => new(right.BaseShape, right.Position + left);

	public static TransformableShape<T> operator *(TransformableShape<T> left, float right) => new(left.BaseShape * right, left.Position);
	public static TransformableShape<T> operator /(TransformableShape<T> left, float right) => new(left.BaseShape / right, left.Position);
	public static TransformableShape<T> operator *(float left, TransformableShape<T> right) => new(left * right.BaseShape, right.Position);
	public TransformableShape<T> ScaledBy(float scalar) => new(BaseShape * scalar, Position);
	public TransformableShape<T> ScaledBy(Vect vect) => new(BaseShape, ;
	#endregion

	#region Equality
	public override bool Equals(object? obj) => obj is TransformableShape<T> other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(BaseShape, Transform);
	public bool Equals(TransformableShape<T> other) => BaseShape.Equals(other.BaseShape) && Transform.Equals(other.Transform);
	public bool Equals(TransformableShape<T> other, float tolerance) => BaseShape.Equals(other.BaseShape, tolerance) && Transform.Equals(other.Transform, tolerance);
	public static bool operator ==(TransformableShape<T> left, TransformableShape<T> right) => left.Equals(right);
	public static bool operator !=(TransformableShape<T> left, TransformableShape<T> right) => !left.Equals(right);
	#endregion

	#region Random / Interp / Clamp
	public static TransformableShape<T> Random() => new(T.Random(), Transform.Random());
	public static TransformableShape<T> Random(TransformableShape<T> minInclusive, TransformableShape<T> maxExclusive) {
		return new(
			T.Random(minInclusive.BaseShape, maxExclusive.BaseShape),
			Transform.Random(minInclusive.Transform, maxExclusive.Transform)
		);
	}
	public static TransformableShape<T> Interpolate(TransformableShape<T> start, TransformableShape<T> end, float distance) {
		return new(
			T.Interpolate(start.BaseShape, end.BaseShape, distance),
			Transform.Interpolate(start.Transform, end.Transform, distance)
		);
	}
	public TransformableShape<T> Clamp(TransformableShape<T> min, TransformableShape<T> max) {
		return new(
			BaseShape.Clamp(min.BaseShape, max.BaseShape),
			Transform.Clamp(min.Transform, max.Transform)
		);
	}
	#endregion
}

public readonly struct TransformableConvexShape<T> : ITransformableConvexShape<TransformableConvexShape<T>, T> where T : IConvexShape<T> {
	public T BaseShape { get; init; }
	public Transform Transform { get; init; }
	
	public TransformableConvexShape(T baseShape, Transform transform) {
		BaseShape = baseShape;
		Transform = transform;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TransformableShape<T>(TransformableConvexShape<T> operand) => new(operand.BaseShape, operand.Transform);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TransformableConvexShape<T>(TransformableShape<T> operand) => new(operand.BaseShape, operand.Transform);
	
	#region Deferred Members
	public bool IsPhysicallyValid {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ((TransformableShape<T>) this).IsPhysicallyValid;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToShapeSpace<TVal>(TVal val) where TVal : ITransformable<TVal> => ((TransformableShape<T>) this).TransformToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToWorldSpace<TVal>(TVal val) where TVal : ITransformable<TVal> => ((TransformableShape<T>) this).TransformToWorldSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToShapeSpace<TVal>(TVal? val) where TVal : struct, ITransformable<TVal> => ((TransformableShape<T>) this).TransformToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToWorldSpace<TVal>(TVal? val) where TVal : struct, ITransformable<TVal> => ((TransformableShape<T>) this).TransformToWorldSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ToString(string? format, IFormatProvider? formatProvider) => ((TransformableShape<T>) this).ToString(format, formatProvider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => ((TransformableShape<T>) this).TryFormat(destination, out charsWritten, format, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformableConvexShape<T> Parse(string s, IFormatProvider? provider) => TransformableShape<T>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(string? s, IFormatProvider? provider, out TransformableConvexShape<T> result) {
		var returnValue = TransformableShape<T>.TryParse(s, provider, out var r);
		result = r;
		return returnValue;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformableConvexShape<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => TransformableShape<T>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TransformableConvexShape<T> result) {
		var returnValue = TransformableShape<T>.TryParse(s, provider, out var r);
		result = r;
		return returnValue;
	}
	public static int SerializationByteSpanLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TransformableShape<T>.SerializationByteSpanLength;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SerializeToBytes(Span<byte> dest, TransformableConvexShape<T> src) => TransformableShape<T>.SerializeToBytes(dest, src); 
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformableConvexShape<T> DeserializeFromBytes(ReadOnlySpan<byte> src) => TransformableShape<T>.DeserializeFromBytes(src);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformableConvexShape<T> MovedBy(Vect v) => ((TransformableShape<T>) this).MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformableConvexShape<T> operator +(TransformableConvexShape<T> left, Vect right) => ((TransformableShape<T>) left) + right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformableConvexShape<T> operator -(TransformableConvexShape<T> left, Vect right) => ((TransformableShape<T>) left) - right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformableConvexShape<T> operator +(Vect left, TransformableConvexShape<T> right) => left + ((TransformableShape<T>) right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformableConvexShape<T> operator *(TransformableConvexShape<T> left, float right) => ((TransformableShape<T>) left) * right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformableConvexShape<T> operator /(TransformableConvexShape<T> left, float right) => ((TransformableShape<T>) left) / right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformableConvexShape<T> operator *(float left, TransformableConvexShape<T> right) => ((TransformableShape<T>) right) * left;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformableConvexShape<T> ScaledBy(float scalar) => ((TransformableShape<T>) this).ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformableConvexShape<T> Random() => TransformableShape<T>.Random();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformableConvexShape<T> Random(TransformableConvexShape<T> minInclusive, TransformableConvexShape<T> maxExclusive) => TransformableShape<T>.Random(minInclusive, maxExclusive);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformableConvexShape<T> Interpolate(TransformableConvexShape<T> start, TransformableConvexShape<T> end, float distance) => TransformableShape<T>.Interpolate(start, end, distance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformableConvexShape<T> Clamp(TransformableConvexShape<T> min, TransformableConvexShape<T> max) => ((TransformableShape<T>) this).Clamp(min, max);
	#endregion

	#region Equality
	public override bool Equals(object? obj) => obj is TransformableConvexShape<T> other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(BaseShape, Position);
	public bool Equals(TransformableConvexShape<T> other) => BaseShape.Equals(other.BaseShape) && Position.Equals(other.Position);
	public bool Equals(TransformableConvexShape<T> other, float tolerance) => BaseShape.Equals(other.BaseShape, tolerance) && Position.Equals(other.Position, tolerance);
	public static bool operator ==(TransformableConvexShape<T> left, TransformableConvexShape<T> right) => left.Equals(right);
	public static bool operator !=(TransformableConvexShape<T> left, TransformableConvexShape<T> right) => !left.Equals(right);
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