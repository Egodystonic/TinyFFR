// Created on 2024-10-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.World;

public interface IPositionedSceneObject {
	Location Position { get; set; }
	void MoveBy(Vect translation);
}
public interface IOrientedSceneObject {
	Rotation Rotation { get; set; }
	void RotateBy(Rotation rotation);
}
public interface IScaledSceneObject {
	Vect Scaling { get; set; }
	void ScaleBy(float scalar);
	void ScaleBy(Vect vect);
	void AdjustScaleBy(float scalar);
	void AdjustScaleBy(Vect vect);
}
public interface ITransformedSceneObject : IPositionedSceneObject, IOrientedSceneObject, IScaledSceneObject {
	Transform Transform { get; set; }
}