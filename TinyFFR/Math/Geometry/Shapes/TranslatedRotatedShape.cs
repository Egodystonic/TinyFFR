// Created on 2026-04-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

public interface ITranslatedRotatedShape : ITranslatedShape {
	Rotation Rotation { get; init; }
}
public interface ITranslatedRotatedShape<TSelf> : ITranslatedRotatedShape, IShape<TSelf>, ITranslatable<TSelf>, IRotatable<TSelf> where TSelf : ITranslatedRotatedShape<TSelf>; 
public interface ITranslatedRotatedShape<TSelf, TBase> : ITranslatedRotatedShape<TSelf> where TSelf : ITranslatedRotatedShape<TSelf, TBase> where TBase : IShape<TBase> { TBase BaseShape { get; init; } } 
public interface ITranslatedRotatedConvexShape : ITranslatedRotatedShape, IConvexShape; 
public interface ITranslatedRotatedConvexShape<TSelf> : ITranslatedRotatedShape<TSelf>, ITranslatedRotatedConvexShape, IConvexShape<TSelf> where TSelf : ITranslatedRotatedConvexShape<TSelf>;
public interface ITranslatedRotatedConvexShape<TSelf, TBase> : ITranslatedRotatedConvexShape<TSelf>, ITranslatedRotatedShape<TSelf, TBase> where TSelf : ITranslatedRotatedConvexShape<TSelf, TBase> where TBase : IConvexShape<TBase>; 

public readonly struct TranslatedRotatedShape<T> : ITranslatedRotatedShape<TranslatedRotatedShape<T>, T> where T : IShape<T> {
	const string StringShapeTransformSeparator = " rotated by ";
	const string StringPositionRotationSeparator = " @ ";
	public T BaseShape { get; init; }
	public Vect Translation { get; init; }
	public Rotation Rotation { get; init; }

	public bool IsPhysicallyValid => BaseShape.IsPhysicallyValid && Translation.IsPhysicallyValid && Rotation.IsPhysicallyValid;
	
	public TranslatedRotatedShape(T baseShape, Vect translation, Rotation rotation) {
		BaseShape = baseShape;
		Translation = translation;
		Rotation = rotation;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToShapeSpace<TVal>(TVal val) where TVal : ITranslatable<TVal>, IPointRotatable<TVal> => val.Minus(Translation).RotatedAroundOriginBy(Rotation.Reversed);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToWorldSpace<TVal>(TVal val) where TVal : ITranslatable<TVal>, IPointRotatable<TVal> => val.RotatedAroundOriginBy(Rotation).Plus(Translation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToShapeSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal>, IPointRotatable<TVal> => val?.Minus(Translation).RotatedAroundOriginBy(Rotation.Reversed);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToWorldSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal>, IPointRotatable<TVal> => val?.RotatedAroundOriginBy(Rotation).Plus(Translation);

	#region ToString / Format / Parse
	public string ToString(string? format, IFormatProvider? formatProvider) {
		return BaseShape.ToString(format, formatProvider)
			+ StringShapeTransformSeparator + Rotation.ToString(format, formatProvider)
			+ StringPositionRotationSeparator + Translation.ToString(format, formatProvider);
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
		
		if (!Translation.TryFormat(destination, out c, format, provider)) return false;
		charsWritten += c;
		return true;
	}
	public static TranslatedRotatedShape<T> Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out TranslatedRotatedShape<T> result) => TryParse(s.AsSpan(), provider, out result);
	public static TranslatedRotatedShape<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		if (!TryParse(s, provider, out var result)) {
			throw new ArgumentException($"Given input string \"{s}\" does not represent a valid translated-and-rotated {typeof(T).Name}.", nameof(s));
		}
		return result;
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TranslatedRotatedShape<T> result) {
		result = default;
		
		var shapeTransformSplitIndex = s.IndexOf(StringShapeTransformSeparator);
		if (shapeTransformSplitIndex < 0) return false;
		
		if (!T.TryParse(s[..shapeTransformSplitIndex], provider, out var baseShape)) return false;
		s = s[(shapeTransformSplitIndex + StringShapeTransformSeparator.Length)..];

		var positionRotationSplitIndex = s.IndexOf(StringPositionRotationSeparator);
		if (positionRotationSplitIndex < 0) return false;
		if (!Rotation.TryParse(s[..positionRotationSplitIndex], provider, out var rotation)) return false;
		if (!Vect.TryParse(s[(positionRotationSplitIndex + StringPositionRotationSeparator.Length)..], provider, out var position)) return false;
		
		result = new(baseShape, position, rotation);
		return true;
	}
	#endregion

	#region Byte Span Serialization / Deserialization
	public static int SerializationByteSpanLength => T.SerializationByteSpanLength + Location.SerializationByteSpanLength + Rotation.SerializationByteSpanLength;
	public static void SerializeToBytes(Span<byte> dest, TranslatedRotatedShape<T> src) {
		T.SerializeToBytes(dest, src.BaseShape);
		Vect.SerializeToBytes(dest[T.SerializationByteSpanLength..], src.Translation);
		Rotation.SerializeToBytes(dest[(T.SerializationByteSpanLength + Location.SerializationByteSpanLength)..], src.Rotation);
	}
	public static TranslatedRotatedShape<T> DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			T.DeserializeFromBytes(src),
			Vect.DeserializeFromBytes(src[T.SerializationByteSpanLength..]),
			Rotation.DeserializeFromBytes(src[(T.SerializationByteSpanLength + Location.SerializationByteSpanLength)..])
		);
	}
	#endregion

	#region Move / Scale / Rotate
	public TranslatedRotatedShape<T> MovedBy(Vect v) => new(BaseShape, Translation.Plus(v), Rotation);
	public static TranslatedRotatedShape<T> operator +(TranslatedRotatedShape<T> left, Vect right) => new(left.BaseShape, left.Translation + right, left.Rotation);
	public static TranslatedRotatedShape<T> operator -(TranslatedRotatedShape<T> left, Vect right) => new(left.BaseShape, left.Translation - right, left.Rotation);
	public static TranslatedRotatedShape<T> operator +(Vect left, TranslatedRotatedShape<T> right) => new(right.BaseShape, right.Translation + left, right.Rotation);

	public static TranslatedRotatedShape<T> operator *(TranslatedRotatedShape<T> left, float right) => new(left.BaseShape * right, left.Translation, left.Rotation);
	public static TranslatedRotatedShape<T> operator /(TranslatedRotatedShape<T> left, float right) => new(left.BaseShape / right, left.Translation, left.Rotation);
	public static TranslatedRotatedShape<T> operator *(float left, TranslatedRotatedShape<T> right) => new(left * right.BaseShape, right.Translation, right.Rotation);
	public TranslatedRotatedShape<T> ScaledBy(float scalar) => new(BaseShape * scalar, Translation, Rotation);

	public static TranslatedRotatedShape<T> operator *(TranslatedRotatedShape<T> left, Rotation right) => new(left.BaseShape, left.Translation, left.Rotation + right);
	public static TranslatedRotatedShape<T> operator *(Rotation left, TranslatedRotatedShape<T> right) => new(right.BaseShape, right.Translation, right.Rotation + left);
	public TranslatedRotatedShape<T> RotatedBy(Rotation rot) => new(BaseShape, Translation, Rotation + rot);
	#endregion

	#region Equality
	public override bool Equals(object? obj) => obj is TranslatedRotatedShape<T> other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(BaseShape, Translation, Rotation);
	public bool Equals(TranslatedRotatedShape<T> other) => BaseShape.Equals(other.BaseShape) && Translation.Equals(other.Translation) && Rotation.Equals(other.Rotation);
	public bool Equals(TranslatedRotatedShape<T> other, float tolerance) => BaseShape.Equals(other.BaseShape, tolerance) && Translation.Equals(other.Translation, tolerance) && Rotation.Equals(other.Rotation, tolerance);
	public static bool operator ==(TranslatedRotatedShape<T> left, TranslatedRotatedShape<T> right) => left.Equals(right);
	public static bool operator !=(TranslatedRotatedShape<T> left, TranslatedRotatedShape<T> right) => !left.Equals(right);
	#endregion

	#region Random / Interp / Clamp
	public static TranslatedRotatedShape<T> Random() => new(T.Random(), Vect.Random(), Rotation.Random());
	public static TranslatedRotatedShape<T> Random(TranslatedRotatedShape<T> minInclusive, TranslatedRotatedShape<T> maxExclusive) {
		return new(
			T.Random(minInclusive.BaseShape, maxExclusive.BaseShape),
			Vect.Random(minInclusive.Translation, maxExclusive.Translation),
			Rotation.Random(minInclusive.Rotation, maxExclusive.Rotation)
		);
	}
	public static TranslatedRotatedShape<T> Interpolate(TranslatedRotatedShape<T> start, TranslatedRotatedShape<T> end, float distance) {
		return new(
			T.Interpolate(start.BaseShape, end.BaseShape, distance),
			Vect.Interpolate(start.Translation, end.Translation, distance),
			Rotation.Interpolate(start.Rotation, end.Rotation, distance)
		);
	}
	public TranslatedRotatedShape<T> Clamp(TranslatedRotatedShape<T> min, TranslatedRotatedShape<T> max) {
		return new(
			BaseShape.Clamp(min.BaseShape, max.BaseShape),
			Translation.Clamp(min.Translation, max.Translation),
			Rotation.Clamp(min.Rotation, max.Rotation)
		);
	}
	#endregion
}

public readonly struct TranslatedRotatedConvexShape<T> : ITranslatedRotatedConvexShape<TranslatedRotatedConvexShape<T>, T> where T : IConvexShape<T> {
	public T BaseShape { get; init; }
	public Vect Translation { get; init; }
	public Rotation Rotation { get; init; }
	
	public TranslatedRotatedConvexShape(T baseShape, Vect translation, Rotation rotation) {
		BaseShape = baseShape;
		Translation = translation;
		Rotation = rotation;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedRotatedShape<T>(TranslatedRotatedConvexShape<T> operand) => new(operand.BaseShape, operand.Translation, operand.Rotation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TranslatedRotatedConvexShape<T>(TranslatedRotatedShape<T> operand) => new(operand.BaseShape, operand.Translation, operand.Rotation);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator TranslatedShape<T>(TranslatedRotatedConvexShape<T> operand) => new(operand.BaseShape, operand.Translation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator TranslatedConvexShape<T>(TranslatedRotatedConvexShape<T> operand) => new(operand.BaseShape, operand.Translation);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator TranslatedRotatedConvexShape<T>(TranslatedShape<T> operand) => new(operand.BaseShape, operand.Translation, Rotation.None);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator TranslatedRotatedConvexShape<T>(TranslatedConvexShape<T> operand) => new(operand.BaseShape, operand.Translation, Rotation.None);
	
	#region Deferred Members
	public bool IsPhysicallyValid {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ((TranslatedRotatedShape<T>) this).IsPhysicallyValid;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToShapeSpace<TVal>(TVal val) where TVal : ITranslatable<TVal>, IPointRotatable<TVal> => ((TranslatedRotatedShape<T>) this).TransformToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToWorldSpace<TVal>(TVal val) where TVal : ITranslatable<TVal>, IPointRotatable<TVal> => ((TranslatedRotatedShape<T>) this).TransformToWorldSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToShapeSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal>, IPointRotatable<TVal> => ((TranslatedRotatedShape<T>) this).TransformToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToWorldSpace<TVal>(TVal? val) where TVal : struct, ITranslatable<TVal>, IPointRotatable<TVal> => ((TranslatedRotatedShape<T>) this).TransformToWorldSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ToString(string? format, IFormatProvider? formatProvider) => ((TranslatedRotatedShape<T>) this).ToString(format, formatProvider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => ((TranslatedRotatedShape<T>) this).TryFormat(destination, out charsWritten, format, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> Parse(string s, IFormatProvider? provider) => TranslatedRotatedShape<T>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(string? s, IFormatProvider? provider, out TranslatedRotatedConvexShape<T> result) {
		var returnValue = TranslatedRotatedShape<T>.TryParse(s, provider, out var r);
		result = r;
		return returnValue;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => TranslatedRotatedShape<T>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TranslatedRotatedConvexShape<T> result) {
		var returnValue = TranslatedRotatedShape<T>.TryParse(s, provider, out var r);
		result = r;
		return returnValue;
	}
	public static int SerializationByteSpanLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TranslatedRotatedShape<T>.SerializationByteSpanLength;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SerializeToBytes(Span<byte> dest, TranslatedRotatedConvexShape<T> src) => TranslatedRotatedShape<T>.SerializeToBytes(dest, src); 
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> DeserializeFromBytes(ReadOnlySpan<byte> src) => TranslatedRotatedShape<T>.DeserializeFromBytes(src);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TranslatedRotatedConvexShape<T> MovedBy(Vect v) => ((TranslatedRotatedShape<T>) this).MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> operator +(TranslatedRotatedConvexShape<T> left, Vect right) => ((TranslatedRotatedShape<T>) left) + right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> operator -(TranslatedRotatedConvexShape<T> left, Vect right) => ((TranslatedRotatedShape<T>) left) - right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> operator +(Vect left, TranslatedRotatedConvexShape<T> right) => left + ((TranslatedRotatedShape<T>) right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> operator *(TranslatedRotatedConvexShape<T> left, float right) => ((TranslatedRotatedShape<T>) left) * right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> operator /(TranslatedRotatedConvexShape<T> left, float right) => ((TranslatedRotatedShape<T>) left) / right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> operator *(float left, TranslatedRotatedConvexShape<T> right) => ((TranslatedRotatedShape<T>) right) * left;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TranslatedRotatedConvexShape<T> ScaledBy(float scalar) => ((TranslatedRotatedShape<T>) this).ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> Random() => TranslatedRotatedShape<T>.Random();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> Random(TranslatedRotatedConvexShape<T> minInclusive, TranslatedRotatedConvexShape<T> maxExclusive) => TranslatedRotatedShape<T>.Random(minInclusive, maxExclusive);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> Interpolate(TranslatedRotatedConvexShape<T> start, TranslatedRotatedConvexShape<T> end, float distance) => TranslatedRotatedShape<T>.Interpolate(start, end, distance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TranslatedRotatedConvexShape<T> Clamp(TranslatedRotatedConvexShape<T> min, TranslatedRotatedConvexShape<T> max) => ((TranslatedRotatedShape<T>) this).Clamp(min, max);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> operator *(TranslatedRotatedConvexShape<T> left, Rotation right) => ((TranslatedRotatedShape<T>) left) * right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TranslatedRotatedConvexShape<T> operator *(Rotation left, TranslatedRotatedConvexShape<T> right) => left * ((TranslatedRotatedShape<T>) right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TranslatedRotatedConvexShape<T> RotatedBy(Rotation rot) => ((TranslatedRotatedShape<T>) this).RotatedBy(rot);
	#endregion

	#region Equality
	public override bool Equals(object? obj) => obj is TranslatedRotatedConvexShape<T> other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(BaseShape, Translation, Rotation);
	public bool Equals(TranslatedRotatedConvexShape<T> other) => BaseShape.Equals(other.BaseShape) && Translation.Equals(other.Translation) && Rotation.Equals(other.Rotation);
	public bool Equals(TranslatedRotatedConvexShape<T> other, float tolerance) => BaseShape.Equals(other.BaseShape, tolerance) && Translation.Equals(other.Translation, tolerance) && Rotation.Equals(other.Rotation, tolerance);
	public static bool operator ==(TranslatedRotatedConvexShape<T> left, TranslatedRotatedConvexShape<T> right) => left.Equals(right);
	public static bool operator !=(TranslatedRotatedConvexShape<T> left, TranslatedRotatedConvexShape<T> right) => !left.Equals(right);
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
	Location IConvexShape.GetRandomInternalLocation() => TransformToWorldSpace(BaseShape.GetRandomInternalLocation());
}