// Created on 2024-02-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Egodystonic.TinyFFR;

public interface ITransformedShape : IShape;
public interface ITransformedShape<TSelf> : ITransformedShape, IShape<TSelf>, IPointTransformable<TSelf> where TSelf : ITransformedShape<TSelf>; 
public interface ITransformedShape<TSelf, TBase> : ITransformedShape<TSelf> where TSelf : ITransformedShape<TSelf, TBase> where TBase : IShape<TBase> { TBase BaseShape { get; init; } } 
public interface ITransformedConvexShape : ITransformedShape, IConvexShape; 
public interface ITransformedConvexShape<TSelf> : ITransformedShape<TSelf>, ITransformedConvexShape, IConvexShape<TSelf> where TSelf : ITransformedConvexShape<TSelf>;
public interface ITransformedConvexShape<TSelf, TBase> : ITransformedConvexShape<TSelf>, ITransformedShape<TSelf, TBase> where TSelf : ITransformedConvexShape<TSelf, TBase> where TBase : IConvexShape<TBase>;

public readonly struct TransformedShape<T> : ITransformedShape<TransformedShape<T>, T> where T : IShape<T> {
	const string StringComponentSeparator = " @ ";
	public T BaseShape { get; init; }
	public Transform Transform { get; init; }

	public bool IsPhysicallyValid {
		get {
			return BaseShape.IsPhysicallyValid
				&& Transform.IsPhysicallyValid
				&& Transform.Scaling.X != 0f
				&& Transform.Scaling.Y != 0f
				&& Transform.Scaling.Z != 0f;
		}
	}

	public TransformedShape(T baseShape, Transform transform) {
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

	internal Ray TransformToShapeSpace(Ray val) {
		var start = val.StartPoint.TransformedAroundOriginByInverseOf(Transform);
		var farPoint = (val.StartPoint + val.Direction * 1f).TransformedAroundOriginByInverseOf(Transform);
		return new Ray(start, Direction.FromVector3(farPoint.ToVector3() - start.ToVector3()));
	}
	internal Ray TransformToWorldSpace(Ray val) {
		var start = val.StartPoint.TransformedAroundOriginBy(Transform);
		var farPoint = (val.StartPoint + val.Direction * 1f).TransformedAroundOriginBy(Transform);
		return new Ray(start, Direction.FromVector3(farPoint.ToVector3() - start.ToVector3()));
	}
	internal Ray? TransformToWorldSpace(Ray? val) => val == null ? null : TransformToWorldSpace(val.Value);
	internal Line TransformToShapeSpace(Line val) {
		var point = val.PointOnLine.TransformedAroundOriginByInverseOf(Transform);
		var farPoint = (val.PointOnLine + val.Direction * 1f).TransformedAroundOriginByInverseOf(Transform);
		return new Line(point, Direction.FromVector3(farPoint.ToVector3() - point.ToVector3()));
	}
	internal Plane TransformToShapeSpace(Plane val) {
		var p0 = val.PointClosestToOrigin;
		var normalVec = val.Normal.ToVector3();
		var referenceVec = MathF.Abs(Vector3.Dot(normalVec, Vector3.UnitX)) < 0.9f ? Vector3.UnitX : Vector3.UnitY;
		var tangent1Vec = Vector3.Normalize(Vector3.Cross(normalVec, referenceVec));
		var tangent2Vec = Vector3.Cross(normalVec, tangent1Vec);
		var tp0 = p0.TransformedAroundOriginByInverseOf(Transform);
		var tp1 = Location.FromVector3(p0.ToVector3() + tangent1Vec).TransformedAroundOriginByInverseOf(Transform);
		var tp2 = Location.FromVector3(p0.ToVector3() + tangent2Vec).TransformedAroundOriginByInverseOf(Transform);
		return Plane.FromTriangleOnSurface(tp0, tp1, tp2) ?? val;
	}

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
	public static TransformedShape<T> Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
	public static bool TryParse(string? s, IFormatProvider? provider, out TransformedShape<T> result) => TryParse(s.AsSpan(), provider, out result);
	public static TransformedShape<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
		if (!TryParse(s, provider, out var result)) {
			throw new ArgumentException($"Given input string \"{s}\" does not represent a valid Transformed {typeof(T).Name}.", nameof(s));
		}
		return result;
	}
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TransformedShape<T> result) {
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
	public static void SerializeToBytes(Span<byte> dest, TransformedShape<T> src) {
		T.SerializeToBytes(dest, src.BaseShape);
		Transform.SerializeToBytes(dest[T.SerializationByteSpanLength..], src.Transform);
	}
	public static TransformedShape<T> DeserializeFromBytes(ReadOnlySpan<byte> src) {
		return new(
			T.DeserializeFromBytes(src),
			Transform.DeserializeFromBytes(src[T.SerializationByteSpanLength..])
		);
	}
	#endregion

	#region Move / Rotate / Scale
	public TransformedShape<T> MovedBy(Vect v) => new(BaseShape, Transform.WithAdditionalTranslation(v));
	public static TransformedShape<T> operator +(TransformedShape<T> left, Vect right) => new(left.BaseShape, left.Transform.WithAdditionalTranslation(right));
	public static TransformedShape<T> operator -(TransformedShape<T> left, Vect right) => new(left.BaseShape, left.Transform.WithAdditionalTranslation(-right));
	public static TransformedShape<T> operator +(Vect left, TransformedShape<T> right) => new(right.BaseShape, right.Transform.WithAdditionalTranslation(left));

	public static TransformedShape<T> operator *(TransformedShape<T> left, Rotation right) => new(left.BaseShape, left.Transform.WithAdditionalRotation(right));
	public static TransformedShape<T> operator *(Rotation left, TransformedShape<T> right) => new(right.BaseShape, right.Transform.WithAdditionalRotation(left));
	public TransformedShape<T> RotatedBy(Rotation rot) => new(BaseShape, Transform.WithAdditionalRotation(rot));
	public TransformedShape<T> RotatedAroundOriginBy(Rotation rot) {
		return new(BaseShape, new Transform(
			Transform.Translation.RotatedBy(rot),
			Transform.RotationQuaternion,
			Transform.Scaling
		).WithAdditionalRotation(rot));
	}
	public TransformedShape<T> RotatedBy(Rotation rot, Location pivot) {
		var offset = pivot.AsVect();
		return MovedBy(-offset).RotatedAroundOriginBy(rot).MovedBy(offset);
	}
	public static TransformedShape<T> operator *(TransformedShape<T> left, (Rotation Rotation, Location Pivot) right) => left.RotatedBy(right.Rotation, right.Pivot);
	public static TransformedShape<T> operator *((Rotation Rotation, Location Pivot) left, TransformedShape<T> right) => right.RotatedBy(left.Rotation, left.Pivot);
	public static TransformedShape<T> operator *(TransformedShape<T> left, (Location Pivot, Rotation Rotation) right) => left.RotatedBy(right.Rotation, right.Pivot);
	public static TransformedShape<T> operator *((Location Pivot, Rotation Rotation) left, TransformedShape<T> right) => right.RotatedBy(left.Rotation, left.Pivot);

	public static TransformedShape<T> operator *(TransformedShape<T> left, float right) => new(left.BaseShape * right, left.Transform);
	public static TransformedShape<T> operator /(TransformedShape<T> left, float right) => new(left.BaseShape / right, left.Transform);
	public static TransformedShape<T> operator *(float left, TransformedShape<T> right) => new(left * right.BaseShape, right.Transform);
	public TransformedShape<T> ScaledBy(float scalar) => new(BaseShape * scalar, Transform);
	public TransformedShape<T> ScaledBy(Vect vect) => new(BaseShape, Transform.WithScalingMultipliedBy(vect));
	public TransformedShape<T> ScaledFromOriginBy(Vect vect) {
		return new(BaseShape, new Transform(
			Transform.Translation * vect,
			Transform.RotationQuaternion,
			Transform.Scaling * vect
		));
	}
	public TransformedShape<T> ScaledBy(Vect vect, Location scalingOrigin) {
		var offset = scalingOrigin.AsVect();
		return MovedBy(-offset).ScaledFromOriginBy(vect).MovedBy(offset);
	}
	#endregion

	#region Transform
	public static TransformedShape<T> operator *(TransformedShape<T> left, Transform right) => left.TransformedBy(right);
	public static TransformedShape<T> operator *(Transform left, TransformedShape<T> right) => right.TransformedBy(left);
	public TransformedShape<T> TransformedBy(Transform transform) => ScaledBy(transform.Scaling).RotatedBy(transform.Rotation).MovedBy(transform.Translation);
	public TransformedShape<T> TransformedBy(Matrix4x4 transformMatrix) => TransformedAroundOriginBy(transformMatrix);
	public TransformedShape<T> TransformedByInverseOf(Transform transform) => MovedBy(-transform.Translation).RotatedBy(transform.Rotation.Reversed).ScaledBy(transform.Scaling.Reciprocal ?? Vect.Zero);
	public TransformedShape<T> TransformedByInverseOf(Matrix4x4 transformMatrix) => TransformedAroundOriginByInverseOf(transformMatrix);
	public TransformedShape<T> TransformedAroundOriginBy(Transform transform) => ScaledFromOriginBy(transform.Scaling).RotatedAroundOriginBy(transform.Rotation).MovedBy(transform.Translation);
	public TransformedShape<T> TransformedBy(Transform transform, Location origin) => ScaledBy(transform.Scaling, origin).RotatedBy(transform.Rotation, origin).MovedBy(transform.Translation);
	public TransformedShape<T> TransformedAroundOriginBy(Matrix4x4 transformMatrix) {
		var combined = Transform.ToMatrix() * transformMatrix;
		return new(BaseShape, Transform.FromBestGuessDecompositionOfMatrix(combined));
	}
	public TransformedShape<T> TransformedBy(Matrix4x4 transformMatrix, Location origin) {
		var offset = origin.AsVect();
		return MovedBy(-offset).TransformedAroundOriginBy(transformMatrix).MovedBy(offset);
	}
	public TransformedShape<T> TransformedAroundOriginByInverseOf(Transform transform) => MovedBy(-transform.Translation).RotatedAroundOriginBy(transform.Rotation.Reversed).ScaledFromOriginBy(transform.Scaling.Reciprocal ?? Vect.Zero);
	public TransformedShape<T> TransformedByInverseOf(Transform transform, Location origin) {
		var adjustedOrigin = origin - transform.Translation;
		return MovedBy(-transform.Translation).RotatedBy(transform.Rotation.Reversed, adjustedOrigin).ScaledBy(transform.Scaling.Reciprocal ?? Vect.Zero, adjustedOrigin);
	}
	public TransformedShape<T> TransformedAroundOriginByInverseOf(Matrix4x4 transformMatrix) {
		var combined = Transform.ToMatrix() * MathUtils.ForceInvertMatrix(transformMatrix);
		return new(BaseShape, Transform.FromBestGuessDecompositionOfMatrix(combined));
	}
	public TransformedShape<T> TransformedByInverseOf(Matrix4x4 transformMatrix, Location origin) {
		var offset = origin.AsVect();
		return MovedBy(-offset).TransformedAroundOriginByInverseOf(transformMatrix).MovedBy(offset);
	}
	#endregion

	#region Equality
	public override bool Equals(object? obj) => obj is TransformedShape<T> other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(BaseShape, Transform);
	public bool Equals(TransformedShape<T> other) => BaseShape.Equals(other.BaseShape) && Transform.Equals(other.Transform);
	public bool Equals(TransformedShape<T> other, float tolerance) => BaseShape.Equals(other.BaseShape, tolerance) && Transform.Equals(other.Transform, tolerance);
	public static bool operator ==(TransformedShape<T> left, TransformedShape<T> right) => left.Equals(right);
	public static bool operator !=(TransformedShape<T> left, TransformedShape<T> right) => !left.Equals(right);
	#endregion

	#region Random / Interp / Clamp
	public static TransformedShape<T> Random() => new(T.Random(), Transform.Random());
	public static TransformedShape<T> Random(TransformedShape<T> minInclusive, TransformedShape<T> maxExclusive) {
		return new(
			T.Random(minInclusive.BaseShape, maxExclusive.BaseShape),
			Transform.Random(minInclusive.Transform, maxExclusive.Transform)
		);
	}
	public static TransformedShape<T> Interpolate(TransformedShape<T> start, TransformedShape<T> end, float distance) {
		return new(
			T.Interpolate(start.BaseShape, end.BaseShape, distance),
			Transform.Interpolate(start.Transform, end.Transform, distance)
		);
	}
	public TransformedShape<T> Clamp(TransformedShape<T> min, TransformedShape<T> max) {
		return new(
			BaseShape.Clamp(min.BaseShape, max.BaseShape),
			Transform.Clamp(min.Transform, max.Transform)
		);
	}
	#endregion
}

public readonly struct TransformedConvexShape<T> : ITransformedConvexShape<TransformedConvexShape<T>, T> where T : IConvexShape<T> {
	public T BaseShape { get; init; }
	public Transform Transform { get; init; }
	
	public TransformedConvexShape(T baseShape, Transform transform) {
		BaseShape = baseShape;
		Transform = transform;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TransformedShape<T>(TransformedConvexShape<T> operand) => new(operand.BaseShape, operand.Transform);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator TransformedConvexShape<T>(TransformedShape<T> operand) => new(operand.BaseShape, operand.Transform);
	
	#region Deferred Members
	public bool IsPhysicallyValid {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ((TransformedShape<T>) this).IsPhysicallyValid;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToShapeSpace<TVal>(TVal val) where TVal : ITransformable<TVal> => ((TransformedShape<T>) this).TransformToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal TransformToWorldSpace<TVal>(TVal val) where TVal : ITransformable<TVal> => ((TransformedShape<T>) this).TransformToWorldSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToShapeSpace<TVal>(TVal? val) where TVal : struct, ITransformable<TVal> => ((TransformedShape<T>) this).TransformToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal TVal? TransformToWorldSpace<TVal>(TVal? val) where TVal : struct, ITransformable<TVal> => ((TransformedShape<T>) this).TransformToWorldSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Ray TransformToShapeSpace(Ray val) => ((TransformedShape<T>) this).TransformToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Ray TransformToWorldSpace(Ray val) => ((TransformedShape<T>) this).TransformToWorldSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Ray? TransformToWorldSpace(Ray? val) => ((TransformedShape<T>) this).TransformToWorldSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Line TransformToShapeSpace(Line val) => ((TransformedShape<T>) this).TransformToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Plane TransformToShapeSpace(Plane val) => ((TransformedShape<T>) this).TransformToShapeSpace(val);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ToString(string? format, IFormatProvider? formatProvider) => ((TransformedShape<T>) this).ToString(format, formatProvider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => ((TransformedShape<T>) this).TryFormat(destination, out charsWritten, format, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> Parse(string s, IFormatProvider? provider) => TransformedShape<T>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(string? s, IFormatProvider? provider, out TransformedConvexShape<T> result) {
		var returnValue = TransformedShape<T>.TryParse(s, provider, out var r);
		result = r;
		return returnValue;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => TransformedShape<T>.Parse(s, provider);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TransformedConvexShape<T> result) {
		var returnValue = TransformedShape<T>.TryParse(s, provider, out var r);
		result = r;
		return returnValue;
	}
	public static int SerializationByteSpanLength {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TransformedShape<T>.SerializationByteSpanLength;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SerializeToBytes(Span<byte> dest, TransformedConvexShape<T> src) => TransformedShape<T>.SerializeToBytes(dest, src); 
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> DeserializeFromBytes(ReadOnlySpan<byte> src) => TransformedShape<T>.DeserializeFromBytes(src);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> MovedBy(Vect v) => ((TransformedShape<T>) this).MovedBy(v);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> operator +(TransformedConvexShape<T> left, Vect right) => ((TransformedShape<T>) left) + right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> operator -(TransformedConvexShape<T> left, Vect right) => ((TransformedShape<T>) left) - right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> operator +(Vect left, TransformedConvexShape<T> right) => left + ((TransformedShape<T>) right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> operator *(TransformedConvexShape<T> left, float right) => ((TransformedShape<T>) left) * right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> operator /(TransformedConvexShape<T> left, float right) => ((TransformedShape<T>) left) / right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> operator *(float left, TransformedConvexShape<T> right) => ((TransformedShape<T>) right) * left;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> ScaledBy(float scalar) => ((TransformedShape<T>) this).ScaledBy(scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> ScaledBy(Vect vect) => ((TransformedShape<T>) this).ScaledBy(vect);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> ScaledFromOriginBy(Vect vect) => ((TransformedShape<T>) this).ScaledFromOriginBy(vect);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> ScaledBy(Vect vect, Location scalingOrigin) => ((TransformedShape<T>) this).ScaledBy(vect, scalingOrigin);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> operator *(TransformedConvexShape<T> left, Rotation right) => ((TransformedShape<T>) left) * right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> operator *(Rotation left, TransformedConvexShape<T> right) => left * ((TransformedShape<T>) right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> RotatedBy(Rotation rot) => ((TransformedShape<T>) this).RotatedBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> RotatedAroundOriginBy(Rotation rot) => ((TransformedShape<T>) this).RotatedAroundOriginBy(rot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> RotatedBy(Rotation rot, Location pivot) => ((TransformedShape<T>) this).RotatedBy(rot, pivot);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> operator *(TransformedConvexShape<T> left, (Rotation Rotation, Location Pivot) right) => ((TransformedShape<T>) left) * right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> operator *((Rotation Rotation, Location Pivot) left, TransformedConvexShape<T> right) => left * ((TransformedShape<T>) right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> operator *(TransformedConvexShape<T> left, (Location Pivot, Rotation Rotation) right) => ((TransformedShape<T>) left) * right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> operator *((Location Pivot, Rotation Rotation) left, TransformedConvexShape<T> right) => left * ((TransformedShape<T>) right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> operator *(TransformedConvexShape<T> left, Transform right) => ((TransformedShape<T>) left) * right;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> operator *(Transform left, TransformedConvexShape<T> right) => left * ((TransformedShape<T>) right);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> TransformedBy(Transform transform) => ((TransformedShape<T>) this).TransformedBy(transform);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> TransformedBy(Matrix4x4 transformMatrix) => ((TransformedShape<T>) this).TransformedBy(transformMatrix);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> TransformedByInverseOf(Transform transform) => ((TransformedShape<T>) this).TransformedByInverseOf(transform);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> TransformedByInverseOf(Matrix4x4 transformMatrix) => ((TransformedShape<T>) this).TransformedByInverseOf(transformMatrix);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> TransformedAroundOriginBy(Transform transform) => ((TransformedShape<T>) this).TransformedAroundOriginBy(transform);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> TransformedBy(Transform transform, Location origin) => ((TransformedShape<T>) this).TransformedBy(transform, origin);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> TransformedAroundOriginBy(Matrix4x4 transformMatrix) => ((TransformedShape<T>) this).TransformedAroundOriginBy(transformMatrix);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> TransformedBy(Matrix4x4 transformMatrix, Location origin) => ((TransformedShape<T>) this).TransformedBy(transformMatrix, origin);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> TransformedAroundOriginByInverseOf(Transform transform) => ((TransformedShape<T>) this).TransformedAroundOriginByInverseOf(transform);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> TransformedByInverseOf(Transform transform, Location origin) => ((TransformedShape<T>) this).TransformedByInverseOf(transform, origin);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> TransformedAroundOriginByInverseOf(Matrix4x4 transformMatrix) => ((TransformedShape<T>) this).TransformedAroundOriginByInverseOf(transformMatrix);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> TransformedByInverseOf(Matrix4x4 transformMatrix, Location origin) => ((TransformedShape<T>) this).TransformedByInverseOf(transformMatrix, origin);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> Random() => TransformedShape<T>.Random();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> Random(TransformedConvexShape<T> minInclusive, TransformedConvexShape<T> maxExclusive) => TransformedShape<T>.Random(minInclusive, maxExclusive);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TransformedConvexShape<T> Interpolate(TransformedConvexShape<T> start, TransformedConvexShape<T> end, float distance) => TransformedShape<T>.Interpolate(start, end, distance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TransformedConvexShape<T> Clamp(TransformedConvexShape<T> min, TransformedConvexShape<T> max) => ((TransformedShape<T>) this).Clamp(min, max);
	#endregion

	#region Equality
	public override bool Equals(object? obj) => obj is TransformedConvexShape<T> other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(BaseShape, Transform);
	public bool Equals(TransformedConvexShape<T> other) => BaseShape.Equals(other.BaseShape) && Transform.Equals(other.Transform);
	public bool Equals(TransformedConvexShape<T> other, float tolerance) => BaseShape.Equals(other.BaseShape, tolerance) && Transform.Equals(other.Transform, tolerance);
	public static bool operator ==(TransformedConvexShape<T> left, TransformedConvexShape<T> right) => left.Equals(right);
	public static bool operator !=(TransformedConvexShape<T> left, TransformedConvexShape<T> right) => !left.Equals(right);
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
	public Location PointClosestTo(Plane plane) => BaseShape.PointClosestTo(TransformToShapeSpace(plane));
	public Location ClosestPointOn(Plane plane) => BaseShape.ClosestPointOn(TransformToShapeSpace(plane));
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