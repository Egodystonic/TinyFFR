// Created on 2024-10-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Represents a scene object that can be moved around the world.
/// </summary>
public interface IMovableSceneObject {
	/// <summary>
	/// Moves this object by <paramref name="translation"/>, relative to wherever it currently is.
	/// </summary>
	/// <param name="translation">The distance and direction to move this object by.</param>
	void MoveBy(Vect translation);
}
/// <summary>
/// Represents a scene object whose position in the world can be read and set directly.
/// </summary>
public interface IPositionedSceneObject : IMovableSceneObject {
	/// <summary>
	/// Where this object is in the world.
	/// </summary>
	Location Position { get; set; }
}

/// <summary>
/// Represents a scene object that can be rotated.
/// </summary>
public interface IReorientableSceneObject {
	/// <summary>
	/// Rotates this object by <paramref name="rotation"/>, relative to however it is currently oriented.
	/// </summary>
	/// <param name="rotation">The rotation to apply to this object.</param>
	void RotateBy(Rotation rotation);
	/// <summary>
	/// Rotates this object by <paramref name="rotationQuaternion"/>, relative to however it is currently oriented.
	/// </summary>
	/// <param name="rotationQuaternion">The rotation to apply to this object.</param>
	void RotateBy(Quaternion rotationQuaternion);
}
/// <summary>
/// Represents a scene object whose orientation in the world can be read and set directly.
/// </summary>
public interface IOrientedSceneObject : IReorientableSceneObject {
	/// <summary>
	/// How this object is oriented in the world.
	/// </summary>
	Rotation Rotation { get; set; }
	/// <summary>
	/// How this object is oriented in the world, expressed as a <see cref="Quaternion"/>.
	/// </summary>
	/// <remarks>
	/// This describes the same orientation as <see cref="Rotation"/>, offered in quaternion form for interoperating with other libraries.
	/// </remarks>
	Quaternion RotationQuaternion { get; set; }
}

/// <summary>
/// Represents a scene object that can be made larger or smaller.
/// </summary>
public interface IRescalableSceneObject {
	/// <summary>
	/// Multiplies this object's current scaling by <paramref name="scalar"/> on every axis.
	/// </summary>
	/// <param name="scalar">The factor to multiply this object's scaling by.</param>
	void ScaleBy(float scalar);
	/// <summary>
	/// Multiplies this object's current scaling by <paramref name="vect"/>, each axis independently.
	/// </summary>
	/// <param name="vect">The per-axis factors to multiply this object's scaling by.</param>
	void ScaleBy(Vect vect);
	/// <summary>
	/// Adds <paramref name="scalar"/> to this object's current scaling on every axis.
	/// </summary>
	/// <param name="scalar">The amount to add to this object's scaling. May be negative, to shrink the object.</param>
	void AdjustScaleBy(float scalar);
	/// <summary>
	/// Adds <paramref name="vect"/> to this object's current scaling, each axis independently.
	/// </summary>
	/// <param name="vect">The per-axis amounts to add to this object's scaling. Components may be negative, to shrink the object on that axis.</param>
	void AdjustScaleBy(Vect vect);
}
/// <summary>
/// Represents a scene object whose scaling can be read and set directly.
/// </summary>
public interface IScaledSceneObject : IRescalableSceneObject {
	/// <summary>
	/// How large this object is on each axis, where <c>(1, 1, 1)</c> is its unmodified size.
	/// </summary>
	Vect Scaling { get; set; }
}

/// <summary>
/// Represents a scene object whose position, orientation and scaling can all be read and set, both individually and together as a single <see cref="Egodystonic.TinyFFR.Transform"/>.
/// </summary>
public interface ITransformedSceneObject : IPositionedSceneObject, IOrientedSceneObject, IScaledSceneObject {
	/// <summary>
	/// This object's position, orientation and scaling, combined in to a single value.
	/// </summary>
	Transform Transform { get; set; }
	/// <summary>
	/// Rotates this object by <paramref name="rotation"/> around <paramref name="pivotPoint"/>, which moves the object as well as reorienting it.
	/// </summary>
	/// <remarks>
	/// Rotating around a pivot point sweeps this object along an arc centred on that point, in the way a planet orbits a star whilst also turning on its own axis.
	/// Supplying this object's own <see cref="IPositionedSceneObject.Position"/> as the pivot point is equivalent to <see cref="IReorientableSceneObject.RotateBy(Rotation)"/>.
	/// </remarks>
	/// <param name="rotation">The rotation to apply to this object.</param>
	/// <param name="pivotPoint">The point to rotate this object around.</param>
	void RotateBy(Rotation rotation, Location pivotPoint);
	/// <summary>
	/// Rotates this object by <paramref name="rotationQuaternion"/> around <paramref name="pivotPoint"/>, which moves the object as well as reorienting it.
	/// </summary>
	/// <remarks>
	/// Rotating around a pivot point sweeps this object along an arc centred on that point, in the way a planet orbits a star whilst also turning on its own axis.
	/// Supplying this object's own <see cref="IPositionedSceneObject.Position"/> as the pivot point is equivalent to <see cref="IReorientableSceneObject.RotateBy(Quaternion)"/>.
	/// </remarks>
	/// <param name="rotationQuaternion">The rotation to apply to this object.</param>
	/// <param name="pivotPoint">The point to rotate this object around.</param>
	void RotateBy(Quaternion rotationQuaternion, Location pivotPoint);
}




/// <summary>
/// Represents a two-dimensional scene object that can be moved around its canvas.
/// </summary>
public interface IMovable2DSceneObject {
	/// <summary>
	/// Moves this object by <paramref name="translation"/>, relative to wherever it currently is.
	/// </summary>
	/// <param name="translation">The distance and direction to move this object by.</param>
	void MoveBy(XYPair<float> translation);
}
/// <summary>
/// Represents a two-dimensional scene object whose position on its canvas can be read and set directly.
/// </summary>
public interface IPositioned2DSceneObject : IMovable2DSceneObject {
	/// <summary>
	/// Where this object is on its canvas.
	/// </summary>
	XYPair<float> Position { get; set; }
}

/// <summary>
/// Represents a two-dimensional scene object that can be rotated.
/// </summary>
public interface IReorientable2DSceneObject {
	/// <summary>
	/// Rotates this object by <paramref name="rotation"/>, relative to however it is currently oriented.
	/// </summary>
	/// <param name="rotation">The rotation to apply to this object. Positive angles rotate the object anticlockwise.</param>
	void RotateBy(Angle rotation);
}
/// <summary>
/// Represents a two-dimensional scene object whose orientation can be read and set directly.
/// </summary>
public interface IOriented2DSceneObject : IReorientable2DSceneObject {
	/// <summary>
	/// How far this object is rotated from its unrotated orientation, measured anticlockwise.
	/// </summary>
	Angle Rotation { get; set; }
}

/// <summary>
/// Represents a two-dimensional scene object that can be made larger or smaller.
/// </summary>
public interface IRescalable2DSceneObject {
	/// <summary>
	/// Multiplies this object's current scaling by <paramref name="scalar"/> on both axes.
	/// </summary>
	/// <param name="scalar">The factor to multiply this object's scaling by.</param>
	void ScaleBy(float scalar);
	/// <summary>
	/// Multiplies this object's current scaling by <paramref name="vect"/>, each axis independently.
	/// </summary>
	/// <param name="vect">The per-axis factors to multiply this object's scaling by.</param>
	void ScaleBy(XYPair<float> vect);
	/// <summary>
	/// Adds <paramref name="scalar"/> to this object's current scaling on both axes.
	/// </summary>
	/// <param name="scalar">The amount to add to this object's scaling. May be negative, to shrink the object.</param>
	void AdjustScaleBy(float scalar);
	/// <summary>
	/// Adds <paramref name="vect"/> to this object's current scaling, each axis independently.
	/// </summary>
	/// <param name="vect">The per-axis amounts to add to this object's scaling. Components may be negative, to shrink the object on that axis.</param>
	void AdjustScaleBy(XYPair<float> vect);
}
/// <summary>
/// Represents a two-dimensional scene object whose scaling can be read and set directly.
/// </summary>
public interface IScaled2DSceneObject : IRescalable2DSceneObject {
	/// <summary>
	/// How large this object is on each axis, where <c>(1, 1)</c> is its unmodified size.
	/// </summary>
	XYPair<float> Scaling { get; set; }
}

/// <summary>
/// Represents a two-dimensional scene object whose position, orientation and scaling can all be read and set, both individually and together as a single <see cref="Transform2D"/>.
/// </summary>
public interface ITransformed2DSceneObject : IPositioned2DSceneObject, IOriented2DSceneObject, IScaled2DSceneObject {
	/// <summary>
	/// This object's position, orientation and scaling, combined in to a single value.
	/// </summary>
	Transform2D Transform { get; set; }
	/// <summary>
	/// Rotates this object by <paramref name="rotation"/> around <paramref name="pivotPoint"/>, which moves the object as well as reorienting it.
	/// </summary>
	/// <remarks>
	/// Rotating around a pivot point sweeps this object along an arc centred on that point. Supplying this object's own
	/// <see cref="IPositioned2DSceneObject.Position"/> as the pivot point is equivalent to <see cref="IReorientable2DSceneObject.RotateBy(Angle)"/>.
	/// </remarks>
	/// <param name="rotation">The rotation to apply to this object. Positive angles rotate the object anticlockwise.</param>
	/// <param name="pivotPoint">The point to rotate this object around.</param>
	void RotateBy(Angle rotation, XYPair<float> pivotPoint);
}
