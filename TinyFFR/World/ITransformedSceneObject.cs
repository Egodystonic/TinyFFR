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
}
public interface IOrientedSceneObject : IReorientableSceneObject {
	Rotation Rotation { get; set; }
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
}