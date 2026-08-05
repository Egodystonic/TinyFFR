// Created on 2024-10-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.World;

public interface IMovableSceneObject {
	void MoveBy(Vect translation);
}
public interface IPositionedSceneObject : IMovableSceneObject {
	Location Position { get; set; }
}

public interface IReorientableSceneObject {
	void RotateBy(Rotation rotation);
	void RotateBy(Quaternion rotationQuaternion);
}
public interface IOrientedSceneObject : IReorientableSceneObject {
	Rotation Rotation { get; set; }
	Quaternion RotationQuaternion { get; set; }
}

public interface IRescalableSceneObject {
	void ScaleBy(float scalar);
	void ScaleBy(Vect vect);
	void AdjustScaleBy(float scalar);
	void AdjustScaleBy(Vect vect);
}
public interface IScaledSceneObject : IRescalableSceneObject {
	Vect Scaling { get; set; }
}

public interface ITransformedSceneObject : IPositionedSceneObject, IOrientedSceneObject, IScaledSceneObject {
	Transform Transform { get; set; }
	void RotateBy(Rotation rotation, Location pivotPoint);
	void RotateBy(Quaternion rotationQuaternion, Location pivotPoint);
}






public interface IMovable2DSceneObject {
	void MoveBy(XYPair<float> translation);
}
public interface IPositioned2DSceneObject : IMovable2DSceneObject {
	XYPair<float> Position { get; set; }
}

public interface IReorientable2DSceneObject {
	void RotateBy(Angle rotation);
}
public interface IOriented2DSceneObject : IReorientable2DSceneObject {
	Angle Rotation { get; set; }
}

public interface IRescalable2DSceneObject {
	void ScaleBy(float scalar);
	void ScaleBy(XYPair<float> vect);
	void AdjustScaleBy(float scalar);
	void AdjustScaleBy(XYPair<float> vect);
}
public interface IScaled2DSceneObject : IRescalable2DSceneObject {
	XYPair<float> Scaling { get; set; }
}

public interface ITransformed2DSceneObject : IPositioned2DSceneObject, IOriented2DSceneObject, IScaled2DSceneObject {
	Transform2D Transform { get; set; }
	void RotateBy(Angle rotation, XYPair<float> pivotPoint);
}