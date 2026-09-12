// Created on 2024-03-22 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

// This file hosts all the trait interfaces for geometric primitives

namespace Egodystonic.TinyFFR;

#region Scale/Rotate/Translate & Transform
/// <summary>
/// Trait interface used to mark a type as being able to be scaled uniformly by a single scalar factor.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IScalable<TSelf> :
	IMultiplicative<TSelf, float, TSelf>
	where TSelf : IScalable<TSelf> {
	TSelf IMultiplicative<TSelf, float, TSelf>.MultipliedBy(float scalar) => ScaledBy(scalar);
	TSelf IMultiplicative<TSelf, float, TSelf>.DividedBy(float scalar) => ScaledBy(1f / scalar);
	/// <summary>
	/// Returns this value scaled uniformly by <paramref name="scalar"/>.
	/// </summary>
	/// <param name="scalar">The scale factor. Can be negative or zero, depending on what is meaningful for the implementing type.</param>
	TSelf ScaledBy(float scalar);
}
/// <summary>
/// Extension of <see cref="IScalable{TSelf}"/> for types that can also be scaled around an arbitrary pivot point, rather than always around their own origin.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IPointScalable<TSelf> :
	IScalable<TSelf> where TSelf : IPointScalable<TSelf>, IScalable<TSelf> {
	/// <summary>
	/// Returns this value scaled uniformly by <paramref name="scalar"/>, around <paramref name="scalingOrigin"/>.
	/// </summary>
	/// <param name="scalar">The scale factor.</param>
	/// <param name="scalingOrigin">The point to scale around; unlike this value's own position (if any), it is unaffected by the scale.</param>
	TSelf ScaledBy(float scalar, Location scalingOrigin);
	/// <summary>
	/// Returns this value scaled uniformly by <paramref name="scalar"/>, around the world origin (<see cref="Location.Origin"/>).
	/// </summary>
	/// <param name="scalar">The scale factor.</param>
	TSelf ScaledFromOriginBy(float scalar);
}

/// <summary>
/// Extension of <see cref="IScalable{TSelf}"/> for types that can also be scaled independently on each axis, rather than only uniformly.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IIndependentAxisScalable<TSelf> :
	IScalable<TSelf>
	where TSelf : IIndependentAxisScalable<TSelf> {
	/// <summary>
	/// Returns this value scaled independently per axis by <paramref name="vect"/>'s components.
	/// </summary>
	/// <param name="vect">The per-axis scale factors to apply.</param>
	TSelf ScaledBy(Vect vect);
}
/// <summary>
/// Combines <see cref="IIndependentAxisScalable{TSelf}"/> and <see cref="IPointScalable{TSelf}"/>: allows scaling independently per axis around an arbitrary pivot point.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IPointIndependentAxisScalable<TSelf> :
	IIndependentAxisScalable<TSelf>
	where TSelf : IPointIndependentAxisScalable<TSelf>, IIndependentAxisScalable<TSelf> {
	/// <summary>
	/// Returns this value scaled independently per axis by <paramref name="vect"/>'s components, around <paramref name="scalingOrigin"/>.
	/// </summary>
	/// <param name="vect">The per-axis scale factors to apply.</param>
	/// <param name="scalingOrigin">The point to scale around; unlike this value's own position (if any), it is unaffected by the scale.</param>
	TSelf ScaledBy(Vect vect, Location scalingOrigin);
	/// <summary>
	/// Returns this value scaled independently per axis by <paramref name="vect"/>'s components, around the world origin (<see cref="Location.Origin"/>).
	/// </summary>
	/// <param name="vect">The per-axis scale factors to apply.</param>
	TSelf ScaledFromOriginBy(Vect vect);
}

/// <summary>
/// Trait interface used to mark a type as being able to be rotated by a <see cref="Rotation"/>.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IRotatable<TSelf> :
	IMultiplyOperators<TSelf, Rotation, TSelf>
	where TSelf : IRotatable<TSelf> {
	/// <summary>
	/// Returns <paramref name="right"/> after being turned by <paramref name="left"/>; equivalent to <c>right.RotatedBy(left)</c>.
	/// </summary>
	/// <param name="left">The rotation to apply.</param>
	/// <param name="right">The value to rotate.</param>
	static abstract TSelf operator *(Rotation left, TSelf right);
	/// <summary>
	/// Returns this value after being turned by <paramref name="rot"/>.
	/// </summary>
	/// <param name="rot">The rotation to apply.</param>
	TSelf RotatedBy(Rotation rot);
	/// <summary>
	/// Returns this value after being turned by <paramref name="rotQuat"/>.
	/// </summary>
	/// <param name="rotQuat">The rotation, as a raw <see cref="Quaternion"/>, to apply.</param>
	TSelf RotatedBy(Quaternion rotQuat);
}

/// <summary>
/// Extension of <see cref="IRotatable{TSelf}"/> for types that can also be rotated around an arbitrary pivot point, rather than always around their own origin.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IPointRotatable<TSelf> :
	IRotatable<TSelf>,
	IMultiplyOperators<TSelf, (Rotation Rotation, Location Pivot), TSelf>,
	IMultiplyOperators<TSelf, (Location Pivot, Rotation Rotation), TSelf>
	where TSelf : IPointRotatable<TSelf>, IRotatable<TSelf> {
	/// <summary>
	/// Returns <paramref name="right"/> after being turned by <paramref name="left"/>'s rotation, around <paramref name="left"/>'s pivot; equivalent to <c>right.RotatedBy(left.Rotation, left.Pivot)</c>.
	/// </summary>
	/// <param name="left">The rotation to apply, and the point to pivot around.</param>
	/// <param name="right">The value to rotate.</param>
	static abstract TSelf operator *((Rotation Rotation, Location Pivot) left, TSelf right);
	/// <summary>
	/// Returns <paramref name="right"/> after being turned by <paramref name="left"/>'s rotation, around <paramref name="left"/>'s pivot; equivalent to <c>right.RotatedBy(left.Rotation, left.Pivot)</c>.
	/// </summary>
	/// <param name="left">The point to pivot around, and the rotation to apply.</param>
	/// <param name="right">The value to rotate.</param>
	static abstract TSelf operator *((Location Pivot, Rotation Rotation) left, TSelf right);
	/// <summary>
	/// Returns this value after being turned by <paramref name="rot"/>, around <paramref name="pivot"/>.
	/// </summary>
	/// <param name="rot">The rotation to apply.</param>
	/// <param name="pivot">The point to rotate around.</param>
	TSelf RotatedBy(Rotation rot, Location pivot);
	/// <summary>
	/// Returns this value after being turned by <paramref name="rot"/>, around the world origin (<see cref="Location.Origin"/>).
	/// </summary>
	/// <param name="rot">The rotation to apply.</param>
	TSelf RotatedAroundOriginBy(Rotation rot);
	/// <summary>
	/// Returns this value after being turned by <paramref name="rotQuat"/>, around <paramref name="pivot"/>.
	/// </summary>
	/// <param name="rotQuat">The rotation, as a raw <see cref="Quaternion"/>, to apply.</param>
	/// <param name="pivot">The point to rotate around.</param>
	TSelf RotatedBy(Quaternion rotQuat, Location pivot);
	/// <summary>
	/// Returns this value after being turned by <paramref name="rotQuat"/>, around the world origin (<see cref="Location.Origin"/>).
	/// </summary>
	/// <param name="rotQuat">The rotation, as a raw <see cref="Quaternion"/>, to apply.</param>
	TSelf RotatedAroundOriginBy(Quaternion rotQuat);
}

/// <summary>
/// Trait interface used to mark a type as being able to be moved by a <see cref="Vect"/>.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface ITranslatable<TSelf> :
	IAdditive<TSelf, Vect, TSelf>
	where TSelf : ITranslatable<TSelf> {
	TSelf IAdditive<TSelf, Vect, TSelf>.Plus(Vect v) => MovedBy(v);
	TSelf IAdditive<TSelf, Vect, TSelf>.Minus(Vect v) => MovedBy(-v);
	/// <summary>
	/// Returns this value moved by <paramref name="v"/>.
	/// </summary>
	/// <param name="v">The vector to move by.</param>
	TSelf MovedBy(Vect v);
}

/// <summary>
/// Combines <see cref="IIndependentAxisScalable{TSelf}"/>, <see cref="IRotatable{TSelf}"/>, and <see cref="ITranslatable{TSelf}"/>: allows applying a full <see cref="Transform"/> (scale, then rotate, then translate) in one operation.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface ITransformable<TSelf> :
	IIndependentAxisScalable<TSelf>,
	IRotatable<TSelf>,
	ITranslatable<TSelf>,
	IMultiplyOperators<TSelf, Transform, TSelf>
	where TSelf : ITransformable<TSelf>, IIndependentAxisScalable<TSelf>, IRotatable<TSelf>, ITranslatable<TSelf> {
	/// <summary>
	/// Returns this value after having <paramref name="transform"/> applied to it.
	/// </summary>
	/// <param name="transform">The transform to apply.</param>
	TSelf TransformedBy(Transform transform);
	/// <summary>
	/// Returns this value after having the inverse of <paramref name="transform"/> applied to it.
	/// </summary>
	/// <param name="transform">The transform whose inverse should be applied.</param>
	TSelf TransformedByInverseOf(Transform transform);
	/// <summary>
	/// Returns <paramref name="right"/> after having <paramref name="left"/> applied to it; equivalent to <c>right.TransformedBy(left)</c>.
	/// </summary>
	/// <param name="left">The transform to apply.</param>
	/// <param name="right">The value to transform.</param>
	static abstract TSelf operator *(Transform left, TSelf right);
}

/// <summary>
/// Extension of <see cref="ITransformable{TSelf}"/> for types that can also apply a <see cref="Transform"/> around an arbitrary origin point, rather than always around their own origin.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IPointTransformable<TSelf> :
	ITransformable<TSelf>, IPointIndependentAxisScalable<TSelf>, IPointRotatable<TSelf>
	where TSelf : IPointTransformable<TSelf>, ITransformable<TSelf>, IPointIndependentAxisScalable<TSelf>, IPointRotatable<TSelf> {
	/// <summary>
	/// Returns this value after having <paramref name="transform"/> applied to it, treating <paramref name="transformationOrigin"/> as the transform's origin.
	/// </summary>
	/// <param name="transform">The transform to apply.</param>
	/// <param name="transformationOrigin">The point to treat as the origin of <paramref name="transform"/>.</param>
	TSelf TransformedBy(Transform transform, Location transformationOrigin);
	/// <summary>
	/// Returns this value after having <paramref name="transform"/> applied to it, treating the world origin (<see cref="Location.Origin"/>) as the transform's origin.
	/// </summary>
	/// <param name="transform">The transform to apply.</param>
	TSelf TransformedAroundOriginBy(Transform transform);
	/// <summary>
	/// Returns this value after having the inverse of <paramref name="transform"/> applied to it, treating <paramref name="transformationOrigin"/> as the transform's origin.
	/// </summary>
	/// <param name="transform">The transform whose inverse should be applied.</param>
	/// <param name="transformationOrigin">The point to treat as the origin of <paramref name="transform"/>.</param>
	TSelf TransformedByInverseOf(Transform transform, Location transformationOrigin);
	/// <summary>
	/// Returns this value after having the inverse of <paramref name="transform"/> applied to it, treating the world origin (<see cref="Location.Origin"/>) as the transform's origin.
	/// </summary>
	/// <param name="transform">The transform whose inverse should be applied.</param>
	TSelf TransformedAroundOriginByInverseOf(Transform transform);
}
#endregion

#region 2D Scale/Rotate/Translate & Transform
/*
 * Maintainer's note:
 * Some interface inheritances are commented out below, mostly ones that import operator overloads.
 * The reason for this is the prevalence of CS0695 errors occurring when types try to import those operators
 * for generic type arguments that unify (e.g. XYPair<T> + XYPair<T> must be implemented as well as XYPair<T> + XYPair<float>).
 * The current C# compiler does not give us a way to resolve those overloads when e.g. T = float. If it does one day, we can
 * re-activate these interfaces.
 */


/// <summary>
/// 2D counterpart to <see cref="IPointScalable{TSelf}"/>: allows scaling uniformly around an arbitrary pivot point.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IPointScalable2D<TSelf> :
	IScalable<TSelf> where TSelf : IPointScalable2D<TSelf>, IScalable<TSelf> {
	/// <summary>
	/// Returns this value scaled uniformly by <paramref name="scalar"/>, around <paramref name="scalingOrigin"/>.
	/// </summary>
	/// <param name="scalar">The scale factor.</param>
	/// <param name="scalingOrigin">The point to scale around; unlike this value's own position (if any), it is unaffected by the scale.</param>
	TSelf ScaledBy(float scalar, XYPair<float> scalingOrigin);
	/// <summary>
	/// Returns this value scaled uniformly by <paramref name="scalar"/>, around the 2D origin (<see cref="XYPair{T}.Zero"/>).
	/// </summary>
	/// <param name="scalar">The scale factor.</param>
	TSelf ScaledFromOriginBy(float scalar);
}

/// <summary>
/// 2D counterpart to <see cref="IIndependentAxisScalable{TSelf}"/>: allows scaling independently on each axis.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IIndependentAxisScalable2D<TSelf> :
	IScalable<TSelf>
	where TSelf : IIndependentAxisScalable2D<TSelf> {
	/// <summary>
	/// Returns this value scaled independently per axis by <paramref name="vect"/>'s components.
	/// </summary>
	/// <param name="vect">The per-axis scale factors to apply.</param>
	TSelf ScaledBy(XYPair<float> vect);
}
/// <summary>
/// Combines <see cref="IIndependentAxisScalable2D{TSelf}"/> and <see cref="IPointScalable2D{TSelf}"/>: allows scaling independently per axis around an arbitrary pivot point.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IPointIndependentAxisScalable2D<TSelf> :
	IIndependentAxisScalable2D<TSelf>
	where TSelf : IPointIndependentAxisScalable2D<TSelf>, IIndependentAxisScalable2D<TSelf> {
	/// <summary>
	/// Returns this value scaled independently per axis by <paramref name="vect"/>'s components, around <paramref name="scalingOrigin"/>.
	/// </summary>
	/// <param name="vect">The per-axis scale factors to apply.</param>
	/// <param name="scalingOrigin">The point to scale around; unlike this value's own position (if any), it is unaffected by the scale.</param>
	TSelf ScaledBy(XYPair<float> vect, XYPair<float> scalingOrigin);
	/// <summary>
	/// Returns this value scaled independently per axis by <paramref name="vect"/>'s components, around the 2D origin (<see cref="XYPair{T}.Zero"/>).
	/// </summary>
	/// <param name="vect">The per-axis scale factors to apply.</param>
	TSelf ScaledFromOriginBy(XYPair<float> vect);
}

/// <summary>
/// 2D counterpart to <see cref="IRotatable{TSelf}"/>: allows rotating by an <see cref="Angle"/>.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IRotatable2D<TSelf> /*:
	IMultiplyOperators<TSelf, Angle, TSelf>*/
	where TSelf : IRotatable2D<TSelf> {
	//static abstract TSelf operator *(Angle left, TSelf right);
	/// <summary>
	/// Returns this value after being turned by <paramref name="rot"/>.
	/// </summary>
	/// <remarks>
	/// A positive <paramref name="rot"/> turns anticlockwise (the same convention used by <see cref="Rotation"/> and <see cref="XYPair{T}.RotatedAroundOriginBy"/>).
	/// </remarks>
	/// <param name="rot">The angle to rotate by.</param>
	TSelf RotatedBy(Angle rot);
}

/// <summary>
/// Extension of <see cref="IRotatable2D{TSelf}"/> for types that can also be rotated around an arbitrary pivot point, rather than always around their own origin.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IPointRotatable2D<TSelf> /*:
	IMultiplyOperators<TSelf, (Angle Rotation, XYPair<float> Pivot), TSelf>,
	IMultiplyOperators<TSelf, (XYPair<float> Pivot, Angle Rotation), TSelf>*/
	where TSelf : IPointRotatable2D<TSelf> {
	//static abstract TSelf operator *((Angle Rotation, XYPair<float> Pivot) left, TSelf right);
	//static abstract TSelf operator *((XYPair<float> Pivot, Angle Rotation) left, TSelf right);
	/// <summary>
	/// Returns this value after being turned by <paramref name="rot"/>, around <paramref name="pivot"/>.
	/// </summary>
	/// <param name="rot">The angle to rotate by. A positive value turns anticlockwise (see <see cref="IRotatable2D{TSelf}.RotatedBy(Angle)"/>).</param>
	/// <param name="pivot">The point to rotate around.</param>
	TSelf RotatedBy(Angle rot, XYPair<float> pivot);
	/// <summary>
	/// Returns this value after being turned by <paramref name="rot"/>, around the 2D origin (<see cref="XYPair{T}.Zero"/>).
	/// </summary>
	/// <param name="rot">The angle to rotate by. A positive value turns anticlockwise (see <see cref="IRotatable2D{TSelf}.RotatedBy(Angle)"/>).</param>
	TSelf RotatedAroundOriginBy(Angle rot);
}

/// <summary>
/// 2D counterpart to <see cref="ITranslatable{TSelf}"/>: allows moving by an <see cref="XYPair{T}"/> offset.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface ITranslatable2D<TSelf> /*:
	IAdditive<TSelf, XYPair<float>, TSelf>*/
	where TSelf : ITranslatable2D<TSelf> {
	//TSelf IAdditive<TSelf, XYPair<float>, TSelf>.Plus(XYPair<float> v) => MovedBy(v);
	//TSelf IAdditive<TSelf, XYPair<float>, TSelf>.Minus(XYPair<float> v) => MovedBy(-v);
	/// <summary>
	/// Returns this value moved by <paramref name="v"/>.
	/// </summary>
	/// <param name="v">The offset to move by.</param>
	TSelf MovedBy(XYPair<float> v);
}

/// <summary>
/// 2D counterpart to <see cref="ITransformable{TSelf}"/>: allows applying a full <see cref="Transform2D"/> (scale, then rotate, then translate) in one operation.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface ITransformable2D<TSelf> :
	IIndependentAxisScalable2D<TSelf>,
	IRotatable2D<TSelf>,
	ITranslatable2D<TSelf>,
	IMultiplyOperators<TSelf, Transform2D, TSelf>
	where TSelf : ITransformable2D<TSelf>, IIndependentAxisScalable2D<TSelf>, IRotatable2D<TSelf>, ITranslatable2D<TSelf> {
	/// <summary>
	/// Returns this value after having <paramref name="transform"/> applied to it.
	/// </summary>
	/// <param name="transform">The transform to apply.</param>
	TSelf TransformedBy(Transform2D transform);
	/// <summary>
	/// Returns this value after having the inverse of <paramref name="transform"/> applied to it.
	/// </summary>
	/// <param name="transform">The transform whose inverse should be applied.</param>
	TSelf TransformedByInverseOf(Transform2D transform);
	/// <summary>
	/// Returns <paramref name="right"/> after having <paramref name="left"/> applied to it; equivalent to <c>right.TransformedBy(left)</c>.
	/// </summary>
	/// <param name="left">The transform to apply.</param>
	/// <param name="right">The value to transform.</param>
	static abstract TSelf operator *(Transform2D left, TSelf right);
}

/// <summary>
/// Extension of <see cref="ITransformable2D{TSelf}"/> for types that can also apply a <see cref="Transform2D"/> around an arbitrary origin point, rather than always around their own origin.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface IPointTransformable2D<TSelf> :
	ITransformable2D<TSelf>, IPointIndependentAxisScalable2D<TSelf>, IPointRotatable2D<TSelf>
	where TSelf : IPointTransformable2D<TSelf>, ITransformable2D<TSelf>, IPointIndependentAxisScalable2D<TSelf>, IPointRotatable2D<TSelf> {
	/// <summary>
	/// Returns this value after having <paramref name="transform"/> applied to it, treating <paramref name="transformationOrigin"/> as the transform's origin.
	/// </summary>
	/// <param name="transform">The transform to apply.</param>
	/// <param name="transformationOrigin">The point to treat as the origin of <paramref name="transform"/>.</param>
	TSelf TransformedBy(Transform2D transform, XYPair<float> transformationOrigin);
	/// <summary>
	/// Returns this value after having the inverse of <paramref name="transform"/> applied to it, treating <paramref name="transformationOrigin"/> as the transform's origin.
	/// </summary>
	/// <param name="transform">The transform whose inverse should be applied.</param>
	/// <param name="transformationOrigin">The point to treat as the origin of <paramref name="transform"/>.</param>
	TSelf TransformedByInverseOf(Transform2D transform, XYPair<float> transformationOrigin);
	/// <summary>
	/// Returns this value after having <paramref name="transform"/> applied to it, treating the 2D origin (<see cref="XYPair{T}.Zero"/>) as the transform's origin.
	/// </summary>
	/// <param name="transform">The transform to apply.</param>
	TSelf TransformedAroundOriginBy(Transform2D transform);
	/// <summary>
	/// Returns this value after having the inverse of <paramref name="transform"/> applied to it, treating the 2D origin (<see cref="XYPair{T}.Zero"/>) as the transform's origin.
	/// </summary>
	/// <param name="transform">The transform whose inverse should be applied.</param>
	TSelf TransformedAroundOriginByInverseOf(Transform2D transform);
}
#endregion

#region Angle/reflection/projection/parallelization/orthogonalization
/// <summary>
/// Trait interface used to mark a type as being able to calculate the angle between itself and an element of type <typeparamref name="TOther"/>.
/// </summary>
/// <typeparam name="TOther">The other element type capable of being measured against.</typeparam>
public interface IAngleMeasurable<in TOther> where TOther : allows ref struct {
	/// <summary>
	/// Calculates the (unsigned) angle between this value and <paramref name="other"/>.
	/// </summary>
	/// <param name="other">The other element to measure the angle to.</param>
	Angle AngleTo(TOther other);
}
/// <summary>
/// Aggregate of <see cref="IAngleMeasurable{TOther}"/> for all three line-like types (<see cref="Line"/>, <see cref="Ray"/>, <see cref="BoundedRay"/>).
/// </summary>
public interface ILineAngleMeasurable : IAngleMeasurable<Line>, IAngleMeasurable<Ray>, IAngleMeasurable<BoundedRay>;
/// <summary>
/// Extension of <see cref="IAngleMeasurable{TOther}"/> that additionally exposes the <c>^</c> operator as a shorthand for <see cref="IAngleMeasurable{TOther}.AngleTo"/>.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
/// <typeparam name="TOther">The other element type capable of being measured against.</typeparam>
public interface IAngleMeasurable<in TSelf, in TOther> : IAngleMeasurable<TOther> where TSelf : IAngleMeasurable<TSelf, TOther>, allows ref struct where TOther : IAngleMeasurable<TSelf>, allows ref struct {
	/// <summary>
	/// Calculates the (unsigned) angle between <paramref name="self"/> and <paramref name="other"/>; equivalent to <c>self.AngleTo(other)</c>.
	/// </summary>
	/// <param name="self">The first element.</param>
	/// <param name="other">The second element.</param>
	static abstract Angle operator ^(TSelf self, TOther other);
	/// <summary>
	/// Calculates the (unsigned) angle between <paramref name="other"/> and <paramref name="self"/>; equivalent to <c>self.AngleTo(other)</c>.
	/// </summary>
	/// <param name="other">The second element.</param>
	/// <param name="self">The first element.</param>
	static abstract Angle operator ^(TOther other, TSelf self);
}


/// <summary>
/// Trait interface used to mark a type as being able to reflect an element of type <typeparamref name="TOther"/> off of it, as if it were a mirror.
/// </summary>
/// <typeparam name="TOther">The type of element that can be reflected.</typeparam>
/// <typeparam name="TReflection">The type of the reflected result.</typeparam>
public interface IReflectable<in TOther, TReflection> where TReflection : struct where TOther : allows ref struct {
	/// <summary>
	/// Reflects <paramref name="element"/> off of this value.
	/// </summary>
	/// <param name="element">The element to reflect.</param>
	/// <returns><see langword="null"/> if the reflection is not well-defined for the given input (the exact condition depends on the implementing type); the reflected result otherwise.</returns>
	TReflection? ReflectedBy(TOther element);
	/// <summary>
	/// Executes the same function as <see cref="ReflectedBy"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="element">The element to reflect.</param>
	TReflection FastReflectedBy(TOther element);
	/// <summary>
	/// Calculates the incident angle between <paramref name="element"/> and this value's surface.
	/// </summary>
	/// <param name="element">The element to measure the incident angle of.</param>
	/// <returns><see langword="null"/> if the incident angle is not well-defined for the given input (the exact condition depends on the implementing type); the incident angle otherwise.</returns>
	Angle? IncidentAngleWith(TOther element);
	/// <summary>
	/// Executes the same function as <see cref="IncidentAngleWith"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="element">The element to measure the incident angle of.</param>
	Angle FastIncidentAngleWith(TOther element);
}
/// <summary>
/// Trait interface used to mark a type as being able to act as a mirror, reflecting an element of type <typeparamref name="TOther"/> off of itself.
/// </summary>
/// <remarks>
/// This is the mirror image of <see cref="IReflectable{TOther,TReflection}"/>: implement this on the "mirror" type, and <see cref="IReflectable{TOther,TReflection}"/> on the type being reflected.
/// </remarks>
/// <typeparam name="TOther">The type of element that can be reflected off of this value.</typeparam>
/// <typeparam name="TReflection">The type of the reflected result.</typeparam>
public interface IReflectionTarget<in TOther, TReflection> where TReflection : struct where TOther : allows ref struct {
	/// <summary>
	/// Reflects <paramref name="element"/> off of this value; equivalent to <c>element.ReflectedBy(this)</c>.
	/// </summary>
	/// <param name="element">The element to reflect.</param>
	/// <returns><see langword="null"/> if the reflection is not well-defined for the given input (the exact condition depends on the implementing type); the reflected result otherwise.</returns>
	TReflection? ReflectionOf(TOther element);
	/// <summary>
	/// Executes the same function as <see cref="ReflectionOf"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="element">The element to reflect.</param>
	TReflection FastReflectionOf(TOther element);
	/// <summary>
	/// Calculates the incident angle between <paramref name="element"/> and this value.
	/// </summary>
	/// <param name="element">The element to measure the incident angle of.</param>
	/// <returns><see langword="null"/> if the incident angle is not well-defined for the given input (the exact condition depends on the implementing type); the incident angle otherwise.</returns>
	Angle? IncidentAngleWith(TOther element);
	/// <summary>
	/// Executes the same function as <see cref="IncidentAngleWith"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="element">The element to measure the incident angle of.</param>
	Angle FastIncidentAngleWith(TOther element);
}
/// <summary>
/// Aggregate of <see cref="IReflectionTarget{TOther,TReflection}"/> for all three line-like types (<see cref="Line"/>, <see cref="Ray"/>, <see cref="BoundedRay"/>).
/// </summary>
/// <typeparam name="TReflection">The type of the reflected result.</typeparam>
public interface ILineReflectionTarget<TReflection> : IReflectionTarget<Line, TReflection>, IReflectionTarget<Ray, TReflection>, IReflectionTarget<BoundedRay, TReflection> where TReflection : struct;
/// <summary>
/// Aggregate of <see cref="IReflectionTarget{TOther,TReflection}"/> for all three line-like types (<see cref="Line"/>, <see cref="Ray"/>, <see cref="BoundedRay"/>), where each reflects as its own type.
/// </summary>
public interface ILineReflectionTarget : IReflectionTarget<Line, Line>, IReflectionTarget<Ray, Ray>, IReflectionTarget<BoundedRay, BoundedRay>;
interface IReflectionTarget<in TSelf, in TOther, TReflection> : IReflectionTarget<TOther, TReflection> where TOther : IReflectable<TSelf, TReflection>, allows ref struct where TSelf : IReflectionTarget<TOther, TReflection>, allows ref struct where TReflection : struct;
/// <summary>
/// Trait interface used to mark a type as being able to reflect off of any <see cref="IConvexShape{TSelf}"/>.
/// </summary>
/// <typeparam name="TReflection">The type of the reflected result.</typeparam>
public interface IConvexShapeReflectable<TReflection> where TReflection : struct {
	/// <summary>
	/// Reflects this value off of <paramref name="shape"/>.
	/// </summary>
	/// <typeparam name="TShape">The type of the convex shape to reflect off of.</typeparam>
	/// <param name="shape">The shape to reflect off of.</param>
	/// <returns><see langword="null"/> if the reflection is not well-defined for the given input; the reflected result otherwise.</returns>
	TReflection? ReflectedBy<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	/// <summary>
	/// Executes the same function as <see cref="ReflectedBy{TShape}"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <typeparam name="TShape">The type of the convex shape to reflect off of.</typeparam>
	/// <param name="shape">The shape to reflect off of.</param>
	TReflection FastReflectedBy<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	/// <summary>
	/// Calculates the incident angle between this value and <paramref name="shape"/>.
	/// </summary>
	/// <typeparam name="TShape">The type of the convex shape to measure against.</typeparam>
	/// <param name="shape">The shape to measure the incident angle with.</param>
	/// <returns><see langword="null"/> if the incident angle is not well-defined for the given input; the incident angle otherwise.</returns>
	Angle? IncidentAngleWith<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	/// <summary>
	/// Executes the same function as <see cref="IncidentAngleWith{TShape}"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <typeparam name="TShape">The type of the convex shape to measure against.</typeparam>
	/// <param name="shape">The shape to measure the incident angle with.</param>
	Angle FastIncidentAngleWith<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
}


/// <summary>
/// Trait interface used to mark a type as being able to project itself on to an element of type <typeparamref name="TOther"/>.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
/// <typeparam name="TOther">The type of element that can be projected on to.</typeparam>
public interface IProjectable<TSelf, in TOther> where TSelf : struct where TOther : allows ref struct {
	/// <summary>
	/// Returns this value's projection on to <paramref name="element"/>.
	/// </summary>
	/// <param name="element">The element to project on to.</param>
	/// <returns><see langword="null"/> if the projection is not well-defined for the given input (the exact condition depends on the implementing type); the projected result otherwise.</returns>
	TSelf? ProjectedOnTo(TOther element);
	/// <summary>
	/// Executes the same function as <see cref="ProjectedOnTo"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="element">The element to project on to.</param>
	TSelf FastProjectedOnTo(TOther element);
}
/// <summary>
/// Trait interface used to mark a type as being able to have another element of type <typeparamref name="TOther"/> projected on to it.
/// </summary>
/// <remarks>
/// This is the mirror image of <see cref="IProjectable{TSelf,TOther}"/>: implement this on the type being projected on to, and <see cref="IProjectable{TSelf,TOther}"/> on the type being projected.
/// </remarks>
/// <typeparam name="TOther">The type of element that can be projected on to this value.</typeparam>
public interface IProjectionTarget<TOther> where TOther : struct {
	/// <summary>
	/// Returns <paramref name="element"/>'s projection on to this value; equivalent to <c>element.ProjectedOnTo(this)</c>.
	/// </summary>
	/// <param name="element">The element to project.</param>
	/// <returns><see langword="null"/> if the projection is not well-defined for the given input; the projected result otherwise.</returns>
	TOther? ProjectionOf(TOther element);
	/// <summary>
	/// Executes the same function as <see cref="ProjectionOf"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="element">The element to project.</param>
	TOther FastProjectionOf(TOther element);
}
/// <summary>
/// Aggregate of <see cref="IProjectionTarget{TOther}"/> for all three line-like types (<see cref="Line"/>, <see cref="Ray"/>, <see cref="BoundedRay"/>).
/// </summary>
public interface ILineProjectionTarget : IProjectionTarget<Line>, IProjectionTarget<Ray>, IProjectionTarget<BoundedRay>;
interface IProjectionTarget<in TSelf, TOther> : IProjectionTarget<TOther> where TOther : struct, IProjectable<TOther, TSelf> where TSelf : IProjectionTarget<TOther>, allows ref struct;

/// <summary>
/// Trait interface used to mark a type as being able to determine whether it is parallel to an element of type <typeparamref name="TOther"/>.
/// </summary>
/// <typeparam name="TOther">The other element type capable of being tested against.</typeparam>
public interface IParallelDiscernible<in TOther> where TOther : allows ref struct {
	/// <summary>
	/// Determines whether this value is exactly parallel (or exactly opposite) to <paramref name="element"/>.
	/// </summary>
	/// <param name="element">The other element to compare to.</param>
	bool IsParallelTo(TOther element);
	/// <summary>
	/// Determines whether this value is parallel (or opposite) to <paramref name="element"/>, within a small default tolerance.
	/// </summary>
	/// <param name="element">The other element to compare to.</param>
	bool IsApproximatelyParallelTo(TOther element);
	/// <summary>
	/// Determines whether this value is parallel (or opposite) to <paramref name="element"/>, within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="element">The other element to compare to.</param>
	/// <param name="tolerance">How far away from exactly 0° or exactly 180° the angle between the two elements is allowed to be.</param>
	bool IsApproximatelyParallelTo(TOther element, Angle tolerance);
}
/// <summary>
/// Extension of <see cref="IParallelDiscernible{TOther}"/> for types that can also adjust themselves to become parallel to an element of type <typeparamref name="TOther"/>.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
/// <typeparam name="TOther">The other element type capable of being parallelized against.</typeparam>
public interface IParallelizable<TSelf, in TOther> : IParallelDiscernible<TOther> where TSelf : struct where TOther : allows ref struct {
	/// <summary>
	/// Attempts to parallelize this value with <paramref name="element"/>.
	/// </summary>
	/// <param name="element">The target element.</param>
	/// <returns><see langword="null"/> if there is no single answer (i.e. this value is already exactly orthogonal to <paramref name="element"/>); the parallelized result otherwise.</returns>
	TSelf? ParallelizedWith(TOther element);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizedWith"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="element">The target element.</param>
	TSelf FastParallelizedWith(TOther element);
}
/// <summary>
/// Trait interface used to mark a type as being able to have another element of type <typeparamref name="TOther"/> parallelized against it.
/// </summary>
/// <remarks>
/// This is the mirror image of <see cref="IParallelizable{TSelf,TOther}"/>: implement this on the "target" type, and <see cref="IParallelizable{TSelf,TOther}"/> on the type being parallelized.
/// </remarks>
/// <typeparam name="TOther">The type of element that can be parallelized against this value.</typeparam>
public interface IParallelizationTarget<TOther> : IParallelDiscernible<TOther> where TOther : struct {
	/// <summary>
	/// Attempts to parallelize <paramref name="element"/> with this value; equivalent to <c>element.ParallelizedWith(this)</c>.
	/// </summary>
	/// <param name="element">The element to parallelize.</param>
	/// <returns><see langword="null"/> if there is no single answer (i.e. <paramref name="element"/> is already exactly orthogonal to this value); the parallelized result otherwise.</returns>
	TOther? ParallelizationOf(TOther element);
	/// <summary>
	/// Executes the same function as <see cref="ParallelizationOf"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="element">The element to parallelize.</param>
	TOther FastParallelizationOf(TOther element);
}
/// <summary>
/// Aggregate of <see cref="IParallelizationTarget{TOther}"/> for all three line-like types (<see cref="Line"/>, <see cref="Ray"/>, <see cref="BoundedRay"/>).
/// </summary>
public interface ILineParallelizationTarget : IParallelizationTarget<Line>, IParallelizationTarget<Ray>, IParallelizationTarget<BoundedRay>;
interface IParallelizationTarget<in TSelf, TOther> : IParallelizationTarget<TOther> where TOther : struct, IParallelizable<TOther, TSelf> where TSelf : IParallelizationTarget<TOther>, allows ref struct;

/// <summary>
/// Trait interface used to mark a type as being able to determine whether it is orthogonal (perpendicular) to an element of type <typeparamref name="TOther"/>.
/// </summary>
/// <typeparam name="TOther">The other element type capable of being tested against.</typeparam>
public interface IOrthogonalDiscernible<in TOther> where TOther : allows ref struct {
	/// <summary>
	/// Determines whether this value is exactly orthogonal (perpendicular) to <paramref name="element"/>.
	/// </summary>
	/// <param name="element">The other element to compare to.</param>
	bool IsOrthogonalTo(TOther element);
	/// <summary>
	/// Determines whether this value is orthogonal (perpendicular) to <paramref name="element"/>, within a small default tolerance.
	/// </summary>
	/// <param name="element">The other element to compare to.</param>
	bool IsApproximatelyOrthogonalTo(TOther element);
	/// <summary>
	/// Determines whether this value is orthogonal (perpendicular) to <paramref name="element"/>, within a given <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="element">The other element to compare to.</param>
	/// <param name="tolerance">How far away from exactly 90° the angle between the two elements is allowed to be.</param>
	bool IsApproximatelyOrthogonalTo(TOther element, Angle tolerance);
}
/// <summary>
/// Extension of <see cref="IOrthogonalDiscernible{TOther}"/> for types that can also adjust themselves to become orthogonal (perpendicular) to an element of type <typeparamref name="TOther"/>.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
/// <typeparam name="TOther">The other element type capable of being orthogonalized against.</typeparam>
public interface IOrthogonalizable<TSelf, in TOther> : IOrthogonalDiscernible<TOther> where TSelf : struct where TOther : allows ref struct {
	/// <summary>
	/// Attempts to orthogonalize this value against <paramref name="element"/>.
	/// </summary>
	/// <param name="element">The target element.</param>
	/// <returns><see langword="null"/> if there is no single answer (i.e. this value is already exactly parallel or exactly opposite to <paramref name="element"/>); the orthogonalized result otherwise.</returns>
	TSelf? OrthogonalizedAgainst(TOther element);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizedAgainst"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="element">The target element.</param>
	TSelf FastOrthogonalizedAgainst(TOther element);
}
/// <summary>
/// Trait interface used to mark a type as being able to have another element of type <typeparamref name="TOther"/> orthogonalized against it.
/// </summary>
/// <remarks>
/// This is the mirror image of <see cref="IOrthogonalizable{TSelf,TOther}"/>: implement this on the "target" type, and <see cref="IOrthogonalizable{TSelf,TOther}"/> on the type being orthogonalized.
/// </remarks>
/// <typeparam name="TOther">The type of element that can be orthogonalized against this value.</typeparam>
public interface IOrthogonalizationTarget<TOther> : IOrthogonalDiscernible<TOther> where TOther : struct {
	/// <summary>
	/// Attempts to orthogonalize <paramref name="element"/> against this value; equivalent to <c>element.OrthogonalizedAgainst(this)</c>.
	/// </summary>
	/// <param name="element">The element to orthogonalize.</param>
	/// <returns><see langword="null"/> if there is no single answer (i.e. <paramref name="element"/> is already exactly parallel or exactly opposite to this value); the orthogonalized result otherwise.</returns>
	TOther? OrthogonalizationOf(TOther element);
	/// <summary>
	/// Executes the same function as <see cref="OrthogonalizationOf"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <param name="element">The element to orthogonalize.</param>
	TOther FastOrthogonalizationOf(TOther element);
}
/// <summary>
/// Aggregate of <see cref="IOrthogonalizationTarget{TOther}"/> for all three line-like types (<see cref="Line"/>, <see cref="Ray"/>, <see cref="BoundedRay"/>).
/// </summary>
public interface ILineOrthogonalizationTarget : IOrthogonalizationTarget<Line>, IOrthogonalizationTarget<Ray>, IOrthogonalizationTarget<BoundedRay>;
interface IOrthogonalizationTarget<in TSelf, TOther> : IOrthogonalizationTarget<TOther> where TOther : struct, IOrthogonalizable<TOther, TSelf> where TSelf : IOrthogonalizationTarget<TOther>, allows ref struct;
#endregion

#region Distance measurable
/// <summary>
/// Trait interface used to mark a type as being able to calculate its distance from an element of type <typeparamref name="TOther"/>.
/// </summary>
/// <typeparam name="TOther">The other element type capable of being measured against.</typeparam>
public interface IDistanceMeasurable<in TOther> where TOther : allows ref struct {
	/// <summary>
	/// Calculates the distance between this value and <paramref name="element"/>.
	/// </summary>
	/// <param name="element">The element to measure the distance to.</param>
	float DistanceFrom(TOther element);
	/// <summary>
	/// Calculates the square of the distance between this value and <paramref name="element"/>.
	/// </summary>
	/// <remarks>
	/// This is faster than <see cref="DistanceFrom"/> as it avoids a square root, and is sufficient when you only need to compare distances rather than know the exact value.
	/// </remarks>
	/// <param name="element">The element to measure the distance to.</param>
	float DistanceSquaredFrom(TOther element);
}
/// <summary>
/// Aggregate of <see cref="IDistanceMeasurable{TOther}"/> for all three line-like types (<see cref="Line"/>, <see cref="Ray"/>, <see cref="BoundedRay"/>).
/// </summary>
public interface ILineDistanceMeasurable : IDistanceMeasurable<Line>, IDistanceMeasurable<Ray>, IDistanceMeasurable<BoundedRay>;
interface IDistanceMeasurable<in TSelf, in TOther> : IDistanceMeasurable<TOther> where TOther : IDistanceMeasurable<TSelf>, allows ref struct where TSelf : allows ref struct;

/// <summary>
/// Extension of <see cref="IDistanceMeasurable{TOther}"/> for types that have a meaningful notion of "side", allowing the distance to an element of type <typeparamref name="TOther"/> to be signed rather than always non-negative.
/// </summary>
/// <typeparam name="TOther">The other element type capable of being measured against.</typeparam>
public interface ISignedDistanceMeasurable<in TOther> : IDistanceMeasurable<TOther> where TOther : allows ref struct {
	/// <summary>
	/// Calculates the signed distance between this value and <paramref name="element"/>.
	/// </summary>
	/// <remarks>
	/// The sign indicates which side of this value <paramref name="element"/> is on; the exact meaning of "positive" is defined by the implementing type.
	/// </remarks>
	/// <param name="element">The element to measure the distance to.</param>
	float SignedDistanceFrom(TOther element);
}
/// <summary>
/// Aggregate of <see cref="ISignedDistanceMeasurable{TOther}"/> for all three line-like types (<see cref="Line"/>, <see cref="Ray"/>, <see cref="BoundedRay"/>).
/// </summary>
public interface ILineSignedDistanceMeasurable : ILineDistanceMeasurable, ISignedDistanceMeasurable<Line>, ISignedDistanceMeasurable<Ray>, ISignedDistanceMeasurable<BoundedRay>;
interface ISignedDistanceMeasurable<in TSelf, in TOther> : ISignedDistanceMeasurable<TOther> where TOther : ISignedDistanceMeasurable<TSelf>, allows ref struct where TSelf : allows ref struct;

/// <summary>
/// Trait interface used to mark a type as being able to calculate its distance from any <see cref="IConvexShape{TSelf}"/>, both to the shape's interior and to its surface.
/// </summary>
public interface IConvexShapeDistanceMeasurable {
	/// <summary>
	/// Calculates the distance between this value and the closest point inside (or on the surface of) <paramref name="shape"/>.
	/// </summary>
	/// <remarks>
	/// This is <c>0f</c> if this value is inside <paramref name="shape"/>. For the distance to the surface even when inside the shape, see <see cref="DistanceFromSurfaceOf{TShape}"/>.
	/// </remarks>
	/// <typeparam name="TShape">The type of the convex shape to measure against.</typeparam>
	/// <param name="shape">The shape to measure the distance to.</param>
	float DistanceFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	/// <summary>
	/// Calculates the distance between this value and the closest point on the surface of <paramref name="shape"/>.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="DistanceFrom{TShape}"/>, this is not clamped to <c>0f</c> when this value is inside <paramref name="shape"/>.
	/// </remarks>
	/// <typeparam name="TShape">The type of the convex shape to measure against.</typeparam>
	/// <param name="shape">The shape to measure the distance to.</param>
	float DistanceFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	/// <summary>
	/// Calculates the square of the distance between this value and the closest point inside (or on the surface of) <paramref name="shape"/>.
	/// </summary>
	/// <remarks>
	/// This is faster than <see cref="DistanceFrom{TShape}"/> as it avoids a square root, and is sufficient when you only need to compare distances rather than know the exact value.
	/// </remarks>
	/// <typeparam name="TShape">The type of the convex shape to measure against.</typeparam>
	/// <param name="shape">The shape to measure the distance to.</param>
	float DistanceSquaredFrom<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	/// <summary>
	/// Calculates the square of the distance between this value and the closest point on the surface of <paramref name="shape"/>.
	/// </summary>
	/// <remarks>
	/// This is faster than <see cref="DistanceFromSurfaceOf{TShape}"/> as it avoids a square root, and is sufficient when you only need to compare distances rather than know the exact value.
	/// </remarks>
	/// <typeparam name="TShape">The type of the convex shape to measure against.</typeparam>
	/// <param name="shape">The shape to measure the distance to.</param>
	float DistanceSquaredFromSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
}
#endregion

#region Containment testable
/// <summary>
/// Trait interface used to mark a type as being able to determine whether it contains an element of type <typeparamref name="TOther"/>.
/// </summary>
/// <typeparam name="TOther">The other element type capable of being tested.</typeparam>
public interface IContainer<in TOther> where TOther : allows ref struct {
	/// <summary>
	/// Determines whether this value contains <paramref name="element"/>.
	/// </summary>
	/// <param name="element">The element to test.</param>
	bool Contains(TOther element);
}
/// <summary>
/// Aggregate of <see cref="IContainer{TOther}"/> for all three line-like types (<see cref="Line"/>, <see cref="Ray"/>, <see cref="BoundedRay"/>).
/// </summary>
public interface ILineContainer : IContainer<Line>, IContainer<Ray>, IContainer<BoundedRay>;
interface IContainer<in TSelf, in TOther> : IContainer<TOther> where TOther : IContainable<TSelf>, allows ref struct where TSelf : allows ref struct;
/// <summary>
/// Trait interface used to mark a type as being able to determine whether it is contained within an element of type <typeparamref name="TOther"/>.
/// </summary>
/// <remarks>
/// This is the mirror image of <see cref="IContainer{TOther}"/>: implement this on the type being tested, and <see cref="IContainer{TOther}"/> on the "containing" type.
/// </remarks>
/// <typeparam name="TOther">The other element type capable of containing this value.</typeparam>
public interface IContainable<in TOther> where TOther : allows ref struct {
	/// <summary>
	/// Determines whether this value is contained within <paramref name="element"/>; equivalent to <c>element.Contains(this)</c>.
	/// </summary>
	/// <param name="element">The element to test against.</param>
	bool IsContainedWithin(TOther element);
}
/// <summary>
/// Aggregate of <see cref="IContainable{TOther}"/> for all three line-like types (<see cref="Line"/>, <see cref="Ray"/>, <see cref="BoundedRay"/>).
/// </summary>
public interface ILineContainable : IContainable<Line>, IContainable<Ray>, IContainable<BoundedRay>;
interface IContainable<in TSelf, in TOther> : IContainable<TOther> where TOther : IContainer<TSelf>, allows ref struct where TSelf : allows ref struct;
#endregion

#region Closest point discoverable
/// <summary>
/// Trait interface used to mark a mathematical or geometric primitive as capable of calculating
/// which point within it is closest to another element of type <typeparamref name="TOther"/>.
/// </summary>
/// <typeparam name="TOther">The other element type capable of being tested against.</typeparam>
public interface IClosestEndogenousPointDiscoverable<in TOther> where TOther : allows ref struct {
	/// <summary>
	/// Returns the point within this value that is closest to <paramref name="element"/>.
	/// </summary>
	/// <param name="element">The element to find the closest point to.</param>
	Location PointClosestTo(TOther element);
}
/// <summary>
/// Aggregate of <see cref="IClosestEndogenousPointDiscoverable{TOther}"/> for all three line-like types (<see cref="Line"/>, <see cref="Ray"/>, <see cref="BoundedRay"/>).
/// </summary>
public interface ILineClosestEndogenousPointDiscoverable : IClosestEndogenousPointDiscoverable<Line>, IClosestEndogenousPointDiscoverable<Ray>, IClosestEndogenousPointDiscoverable<BoundedRay>;
interface IClosestEndogenousPointDiscoverable<in TSelf, in TOther> : IClosestEndogenousPointDiscoverable<TOther> where TOther : IClosestExogenousPointDiscoverable<TSelf>, allows ref struct where TSelf : allows ref struct;
/// <summary>
/// Trait interface used to mark a mathematical or geometric primitive as capable of calculating
/// which point on another element of type <typeparamref name="TOther"/> is closest to it.
/// </summary>
/// <typeparam name="TOther">The other element type capable of being tested against.</typeparam>
public interface IClosestExogenousPointDiscoverable<in TOther> where TOther : allows ref struct {
	/// <summary>
	/// Returns the point on <paramref name="element"/> that is closest to this value; equivalent to <c>element.PointClosestTo(this)</c>.
	/// </summary>
	/// <param name="element">The element to find the closest point on.</param>
	Location ClosestPointOn(TOther element);
}
/// <summary>
/// Aggregate of <see cref="IClosestExogenousPointDiscoverable{TOther}"/> for all three line-like types (<see cref="Line"/>, <see cref="Ray"/>, <see cref="BoundedRay"/>).
/// </summary>
public interface ILineClosestExogenousPointDiscoverable : IClosestExogenousPointDiscoverable<Line>, IClosestExogenousPointDiscoverable<Ray>, IClosestExogenousPointDiscoverable<BoundedRay>;
interface IClosestExogenousPointDiscoverable<in TSelf, in TOther> : IClosestExogenousPointDiscoverable<TOther> where TOther : IClosestEndogenousPointDiscoverable<TSelf>, allows ref struct where TSelf : allows ref struct;

/// <summary>
/// Trait interface used to mark a type as being able to calculate the closest points between itself and any <see cref="IConvexShape{TSelf}"/>, both to the shape's interior and to its surface.
/// </summary>
public interface IClosestConvexShapePointsDiscoverable {
	/// <summary>
	/// Returns the point inside (or on the surface of) <paramref name="shape"/> that is closest to this value.
	/// </summary>
	/// <remarks>
	/// This is the same as this value's own position if this value already lies inside <paramref name="shape"/>. For the closest point on the surface even when inside the shape, see <see cref="ClosestPointOnSurfaceOf{TShape}"/>.
	/// </remarks>
	/// <typeparam name="TShape">The type of the convex shape to measure against.</typeparam>
	/// <param name="shape">The shape to find the closest point within.</param>
	Location ClosestPointInsideOf<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	/// <summary>
	/// Returns the point on the surface of <paramref name="shape"/> that is closest to this value.
	/// </summary>
	/// <typeparam name="TShape">The type of the convex shape to measure against.</typeparam>
	/// <param name="shape">The shape to find the closest surface point on.</param>
	Location ClosestPointOnSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	/// <summary>
	/// Returns the point on this value that is closest to <paramref name="shape"/>.
	/// </summary>
	/// <typeparam name="TShape">The type of the convex shape to measure against.</typeparam>
	/// <param name="shape">The shape to measure against.</param>
	Location PointClosestTo<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	/// <summary>
	/// Returns the point on this value that is closest to the surface of <paramref name="shape"/>.
	/// </summary>
	/// <typeparam name="TShape">The type of the convex shape to measure against.</typeparam>
	/// <param name="shape">The shape to measure against.</param>
	Location PointClosestToSurfaceOf<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
}
#endregion

#region Intersectable / Relatable
/// <summary>
/// Trait interface used to mark a type as being able to determine whether it is intersected by an element of type <typeparamref name="TOther"/>.
/// </summary>
/// <typeparam name="TOther">The other element type capable of being tested against.</typeparam>
public interface IIntersectable<in TOther> where TOther : allows ref struct {
	/// <summary>
	/// Determines whether this value is intersected by <paramref name="element"/>.
	/// </summary>
	/// <param name="element">The element to test against.</param>
	bool IsIntersectedBy(TOther element);
}
/// <summary>
/// Aggregate of <see cref="IIntersectable{TOther}"/> for all three line-like types (<see cref="Line"/>, <see cref="Ray"/>, <see cref="BoundedRay"/>).
/// </summary>
public interface ILineIntersectable : IIntersectable<Line>, IIntersectable<Ray>, IIntersectable<BoundedRay>;
interface IIntersectable<in TSelf, in TOther> : IIntersectable<TOther> where TOther : IIntersectable<TSelf>, allows ref struct where TSelf : allows ref struct;
/// <summary>
/// Extension of <see cref="IIntersectable{TOther}"/> for types that can also calculate the actual point(s) of intersection with an element of type <typeparamref name="TOther"/>.
/// </summary>
/// <typeparam name="TOther">The other element type capable of being tested against.</typeparam>
/// <typeparam name="TIntersection">The type describing the intersection result.</typeparam>
public interface IIntersectionDeterminable<in TOther, TIntersection> : IIntersectable<TOther> where TIntersection : struct where TOther : allows ref struct {
	/// <summary>
	/// Calculates the intersection between this value and <paramref name="element"/>, if any.
	/// </summary>
	/// <param name="element">The element to test against.</param>
	/// <returns><see langword="null"/> if <paramref name="element"/> does not intersect this value; the intersection result otherwise.</returns>
	TIntersection? IntersectionWith(TOther element);
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="element"/> does intersect this value. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <param name="element">The element to test against.</param>
	TIntersection FastIntersectionWith(TOther element);
}
/// <summary>
/// Aggregate of <see cref="IIntersectionDeterminable{TOther,TIntersection}"/> for all three line-like types (<see cref="Line"/>, <see cref="Ray"/>, <see cref="BoundedRay"/>).
/// </summary>
/// <typeparam name="TIntersection">The type describing the intersection result.</typeparam>
public interface ILineIntersectionDeterminable<TIntersection> : ILineIntersectable, IIntersectionDeterminable<Line, TIntersection>, IIntersectionDeterminable<Ray, TIntersection>, IIntersectionDeterminable<BoundedRay, TIntersection> where TIntersection : struct;
interface IIntersectionDeterminable<in TSelf, in TOther, TIntersection> : IIntersectionDeterminable<TOther, TIntersection> where TOther : IIntersectionDeterminable<TSelf, TIntersection>, allows ref struct where TSelf : allows ref struct where TIntersection : struct;
/// <summary>
/// Trait interface used to mark a type as being able to categorize its relationship to an element of type <typeparamref name="TOther"/>.
/// </summary>
/// <typeparam name="TOther">The other element type capable of being tested against.</typeparam>
/// <typeparam name="TRelationship">The type describing the possible relationships between this value and <typeparamref name="TOther"/>.</typeparam>
public interface IRelatable<in TOther, out TRelationship> where TOther : allows ref struct {
	/// <summary>
	/// Determines this value's relationship to <paramref name="element"/>.
	/// </summary>
	/// <param name="element">The element to test against.</param>
	TRelationship RelationshipTo(TOther element);
}
/// <summary>
/// Aggregate of <see cref="IRelatable{TOther,TRelationship}"/> for all three line-like types (<see cref="Line"/>, <see cref="Ray"/>, <see cref="BoundedRay"/>).
/// </summary>
/// <typeparam name="TRelationship">The type describing the possible relationships.</typeparam>
public interface ILineRelatable<out TRelationship> : IRelatable<Line, TRelationship>, IRelatable<Ray, TRelationship>, IRelatable<BoundedRay, TRelationship>;
interface IRelatable<in TSelf, in TOther, out TRelationship> : IRelatable<TOther, TRelationship> where TOther : IRelatable<TSelf, TRelationship>, allows ref struct where TSelf : allows ref struct;

/// <summary>
/// Trait interface used to mark a type as being able to calculate its intersection with any <see cref="IConvexShape{TSelf}"/>.
/// </summary>
/// <typeparam name="TIntersection">The type describing the intersection result.</typeparam>
public interface IConvexShapeIntersectable<TIntersection> where TIntersection : struct {
	/// <summary>
	/// Calculates the intersection between this value and <paramref name="shape"/>, if any.
	/// </summary>
	/// <typeparam name="TShape">The type of the convex shape to test against.</typeparam>
	/// <param name="shape">The shape to test against.</param>
	/// <returns><see langword="null"/> if <paramref name="shape"/> does not intersect this value; the intersection result otherwise.</returns>
	TIntersection? IntersectionWith<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
	/// <summary>
	/// Executes the same function as <see cref="IntersectionWith{TShape}"/> but skips some correctness checks, trading safety for speed.
	/// </summary>
	/// <remarks>
	/// This function assumes <paramref name="shape"/> does intersect this value. The returned value of this function is undefined when that condition is broken.
	/// </remarks>
	/// <typeparam name="TShape">The type of the convex shape to test against.</typeparam>
	/// <param name="shape">The shape to test against.</param>
	TIntersection FastIntersectionWith<TShape>(TShape shape) where TShape : IConvexShape<TShape>;
}
#endregion

#region Dimensionality / Extents
/// <summary>
/// Trait interface used to mark a type as having an adjustable length.
/// </summary>
/// <typeparam name="TSelf">The type that is actually implementing this interface.</typeparam>
public interface ILengthAdjustable<out TSelf> where TSelf : ILengthAdjustable<TSelf> {
	/// <summary>
	/// Returns this value with its length set to <paramref name="newLength"/>.
	/// </summary>
	/// <param name="newLength">The desired new length.</param>
	TSelf WithLength(float newLength);
	/// <summary>
	/// Returns this value with its length reduced by <paramref name="lengthDecrease"/>.
	/// </summary>
	/// <param name="lengthDecrease">The amount to reduce the length by. Can be negative, which increases the length instead.</param>
	TSelf WithLengthDecreasedBy(float lengthDecrease);
	/// <summary>
	/// Returns this value with its length increased by <paramref name="lengthIncrease"/>.
	/// </summary>
	/// <param name="lengthIncrease">The amount to increase the length by. Can be negative, which decreases the length instead.</param>
	TSelf WithLengthIncreasedBy(float lengthIncrease);
	/// <summary>
	/// Returns this value, shortened if necessary so its length does not exceed <paramref name="maxLength"/>.
	/// </summary>
	/// <param name="maxLength">The maximum permitted length of the resultant value. Must be non-negative.</param>
	TSelf WithMaxLength(float maxLength);
	/// <summary>
	/// Returns this value, lengthened if necessary so its length is not less than <paramref name="minLength"/>.
	/// </summary>
	/// <param name="minLength">The minimum permitted length of the resultant value. Must be non-negative.</param>
	TSelf WithMinLength(float minLength);
}

/// <summary>
/// Trait interface used to mark a mathematical or geometric primitive as capable of having
/// its physical validity tested (e.g. "does this shape have negative surface area?" etc). 
/// </summary>
public interface IPhysicalValidityDeterminable {
	/// <summary>
	/// True if this object is physically valid/possible in the real world, false if not.
	/// </summary>
	bool IsPhysicallyValid { get; }
}
#endregion