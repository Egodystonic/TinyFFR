// Created on 2026-04-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

public interface ITranslatedShape : IShape {
	Vect Translation { get; init; }
}
public interface ITranslatedShape<TSelf> : ITranslatedShape, IShape<TSelf>, ITranslatable<TSelf> where TSelf : ITranslatedShape<TSelf>; 
public interface ITranslatedShape<TSelf, TBase> : ITranslatedShape<TSelf> where TSelf : ITranslatedShape<TSelf, TBase> where TBase : IShape<TBase> { TBase BaseShape { get; init; } } 
public interface ITranslatedConvexShape : ITranslatedShape, IConvexShape; 
public interface ITranslatedConvexShape<TSelf> : ITranslatedShape<TSelf>, ITranslatedConvexShape, IConvexShape<TSelf> where TSelf : ITranslatedConvexShape<TSelf>;
public interface ITranslatedConvexShape<TSelf, TBase> : ITranslatedConvexShape<TSelf>, ITranslatedShape<TSelf, TBase> where TSelf : ITranslatedConvexShape<TSelf, TBase> where TBase : IConvexShape<TBase>; 

public readonly struct TranslatedShape<T> : ITranslatedShape<TranslatedShape<T>, T> where T : IShape<T> {
	const string StringComponentSeparator = " @ ";
	public T BaseShape { get; init; }
	public Vect Translation { get; init; }

	public bool IsPhysicallyValid => BaseShape.IsPhysicallyValid && Translation.IsPhysicallyValid;
	
	public TranslatedShape(T baseShape, Vect translation) {
		BaseShape = baseShape;
		Translation = translation;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToShapeSpace<TVal>(TVal val) where TVal : ITranslatable<TVal> => val.Minus(Translation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToWorldSpace<TVal>(TVal val) where TVal : ITranslatable<TVal> => val.Plus(Translation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToShapeSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal> => val?.Minus(Translation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToWorldSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal> => val?.Plus(Translation);

	#region ToString / Format / Parse
	public string ToString(string? format, IFormatProvider? formatProvider) {
		return BaseShape.ToString(format, formatProvider) + StringComponentSeparator + Translation.ToString(format, formatProvider);
	}
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
		charsWritten = 0;
		
		if (!BaseShape.TryFormat(destination, out var c, format, provider)) return false;
		charsWritten += c;
		destination = destination[c..];
		
		if (!StringComponentSeparator.TryCopyTo(destination)) return false;
		charsWritten += StringComponentSeparator.Length;
		destination = destination[StringComponentSeparator.Length..];
		
		if (!Translation.TryFormat(destination, out c, format, provider)) return false;
		charsWritten += c;
		return true;
	}
	public static TranslatedShape<T> Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out TranslatedShape<T> result) => TryParse(s.AsSpan(), provider, out result);
	public static TranslatedShape<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		if (!TryParse(s, provider, out var result)) {
			throw new ArgumentException($"Given input string \"{s}\" does not represent a valid translated {typeof(T).Name}.", nameof(s));
		}
		return result;
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TranslatedShape<T> result) {
		result = default;
		
		var splitIndex = s.IndexOf(StringComponentSeparator);
		if (splitIndex < 0) return false;
		
		if (!T.TryParse(s[..splitIndex], provider, out var baseShape)) return false;
		if (!Vect.TryParse(s[(splitIndex + StringComponentSeparator.Length)..], provider, out var translation)) return false;
		
		result = new(baseShape, translation);
		return true;
	}
	#endregion

	#region Byte Span Serialization / Deserialization
	public static int SerializationByteSpanLength => T.SerializationByteSpanLength + Location.SerializationByteSpanLength;
	public static void SerializeToBytes(Span<byte> dest, TranslatedShape<T> src) {
		T.SerializeToBytes(dest, src.BaseShape);
		Vect.SerializeToBytes(dest[T.SerializationByteSpanLength..], src.Translation);
	}
	public static TranslatedShape<T> DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			T.DeserializeFromBytes(src),
			Vect.DeserializeFromBytes(src[T.SerializationByteSpanLength..])
		);
	}
	#endregion

	#region Move / Scale
	public TranslatedShape<T> MovedBy(Vect v) => new(BaseShape, Translation.Plus(v));
	public static TranslatedShape<T> operator +(TranslatedShape<T> left, Vect right) => new(left.BaseShape, left.Translation + right);
	public static TranslatedShape<T> operator -(TranslatedShape<T> left, Vect right) => new(left.BaseShape, left.Translation - right);
	public static TranslatedShape<T> operator +(Vect left, TranslatedShape<T> right) => new(right.BaseShape, right.Translation + left);

	public static TranslatedShape<T> operator *(TranslatedShape<T> left, float right) => new(left.BaseShape * right, left.Translation);
	public static TranslatedShape<T> operator /(TranslatedShape<T> left, float right) => new(left.BaseShape / right, left.Translation);
	public static TranslatedShape<T> operator *(float left, TranslatedShape<T> right) => new(left * right.BaseShape, right.Translation);
	public TranslatedShape<T> ScaledBy(float scalar) => new(BaseShape * scalar, Translation);
	#endregion

	#region Equality
	public override bool Equals(object? obj) => obj is TranslatedShape<T> other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(BaseShape, Translation);
	public bool Equals(TranslatedShape<T> other) => BaseShape.Equals(other.BaseShape) && Translation.Equals(other.Translation);
	public bool Equals(TranslatedShape<T> other, float tolerance) => BaseShape.Equals(other.BaseShape, tolerance) && Translation.Equals(other.Translation, tolerance);
	public static bool operator ==(TranslatedShape<T> left, TranslatedShape<T> right) => left.Equals(right);
	public static bool operator !=(TranslatedShape<T> left, TranslatedShape<T> right) => !left.Equals(right);
	#endregion

	#region Random / Interp / Clamp
	public static TranslatedShape<T> Random() => new(T.Random(), Vect.Random());
	public static TranslatedShape<T> Random(TranslatedShape<T> minInclusive, TranslatedShape<T> maxExclusive) {
		return new(
			T.Random(minInclusive.BaseShape, maxExclusive.BaseShape),
			Vect.Random(minInclusive.Translation, maxExclusive.Translation)
		);
	}
	public static TranslatedShape<T> Interpolate(TranslatedShape<T> start, TranslatedShape<T> end, float distance) {
		return new(
			T.Interpolate(start.BaseShape, end.BaseShape, distance),
			Vect.Interpolate(start.Translation, end.Translation, distance)
		);
	}
	public TranslatedShape<T> Clamp(TranslatedShape<T> min, TranslatedShape<T> max) {
		return new(
			BaseShape.Clamp(min.BaseShape, max.BaseShape),
			Translation.Clamp(min.Translation, max.Translation)
		);
	}
	#endregion
}

public readonly struct TranslatedConvexShape<T> : ITranslatedConvexShape<TranslatedConvexShape<T>, T> where T : IConvexShape<T> {
	public T BaseShape { get; init; }
	public Vect Translation { get; init; }
	
	public TranslatedConvexShape(T baseShape, Vect translation) {
		BaseShape = baseShape;
		Translation = translation;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedShape<T>(TranslatedConvexShape<T> operand) => new(operand.BaseShape, operand.Translation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedConvexShape<T>(TranslatedShape<T> operand) => new(operand.BaseShape, operand.Translation);
	
	#region Deferred Members
	public bool IsPhysicallyValid {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ((TranslatedShape<T>) this).IsPhysicallyValid;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToShapeSpace<TVal>(TVal val) where TVal : ITranslatable<TVal> => ((TranslatedShape<T>) this).TransformToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToWorldSpace<TVal>(TVal val) where TVal : ITranslatable<TVal> => ((TranslatedShape<T>) this).TransformToWorldSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToShapeSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal> => ((TranslatedShape<T>) this).TransformToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToWorldSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal> => ((TranslatedShape<T>) this).TransformToWorldSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ToString(string? format, IFormatProvider? formatProvider) => ((TranslatedShape<T>) this).ToString(format, formatProvider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => ((TranslatedShape<T>) this).TryFormat(destination, out charsWritten, format, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> Parse(string s, IFormatProvider? provider) => TranslatedShape<T>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(string? s, IFormatProvider? provider, out TranslatedConvexShape<T> result) {
		var returnValue = TranslatedShape<T>.TryParse(s, provider, out var r);
		result = r;
		return returnValue;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => TranslatedShape<T>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TranslatedConvexShape<T> result) {
		var returnValue = TranslatedShape<T>.TryParse(s, provider, out var r);
		result = r;
		return returnValue;
	}
	public static int SerializationByteSpanLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TranslatedShape<T>.SerializationByteSpanLength;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SerializeToBytes(Span<byte> dest, TranslatedConvexShape<T> src) => TranslatedShape<T>.SerializeToBytes(dest, src); 
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> DeserializeFromBytes(ReadOnlySpan<byte> src) => TranslatedShape<T>.DeserializeFromBytes(src);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TranslatedConvexShape<T> MovedBy(Vect v) => ((TranslatedShape<T>) this).MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> operator +(TranslatedConvexShape<T> left, Vect right) => ((TranslatedShape<T>) left) + right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> operator -(TranslatedConvexShape<T> left, Vect right) => ((TranslatedShape<T>) left) - right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> operator +(Vect left, TranslatedConvexShape<T> right) => left + ((TranslatedShape<T>) right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> operator *(TranslatedConvexShape<T> left, float right) => ((TranslatedShape<T>) left) * right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> operator /(TranslatedConvexShape<T> left, float right) => ((TranslatedShape<T>) left) / right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> operator *(float left, TranslatedConvexShape<T> right) => ((TranslatedShape<T>) right) * left;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TranslatedConvexShape<T> ScaledBy(float scalar) => ((TranslatedShape<T>) this).ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> Random() => TranslatedShape<T>.Random();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> Random(TranslatedConvexShape<T> minInclusive, TranslatedConvexShape<T> maxExclusive) => TranslatedShape<T>.Random(minInclusive, maxExclusive);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedConvexShape<T> Interpolate(TranslatedConvexShape<T> start, TranslatedConvexShape<T> end, float distance) => TranslatedShape<T>.Interpolate(start, end, distance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TranslatedConvexShape<T> Clamp(TranslatedConvexShape<T> min, TranslatedConvexShape<T> max) => ((TranslatedShape<T>) this).Clamp(min, max);
	#endregion

	#region Equality
	public override bool Equals(object? obj) => obj is TranslatedConvexShape<T> other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(BaseShape, Translation);
	public bool Equals(TranslatedConvexShape<T> other) => BaseShape.Equals(other.BaseShape) && Translation.Equals(other.Translation);
	public bool Equals(TranslatedConvexShape<T> other, float tolerance) => BaseShape.Equals(other.BaseShape, tolerance) && Translation.Equals(other.Translation, tolerance);
	public static bool operator ==(TranslatedConvexShape<T> left, TranslatedConvexShape<T> right) => left.Equals(right);
	public static bool operator !=(TranslatedConvexShape<T> left, TranslatedConvexShape<T> right) => !left.Equals(right);
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
}