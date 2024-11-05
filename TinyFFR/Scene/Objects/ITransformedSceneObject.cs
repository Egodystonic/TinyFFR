// Created on 2024-10-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Scene;

public interface IPositionedSceneObject {
	Location Position { get; set; }
	void Move(Vect translation);
}
public interface IOrientedSceneObject {
	Rotation Rotation { get; set; }
	void Rotate(Rotation rotation);
}
public interface IScaledSceneObject {
	Vect Scaling { get; set; }
	void Scale(float scalar);
	void Scale(Vect vect);
}
public interface ITransformedSceneObject : IPositionedSceneObject, IOrientedSceneObject, IScaledSceneObject {
	Transform Transform { get; set; }
}