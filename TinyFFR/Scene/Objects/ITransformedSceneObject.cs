// Created on 2024-10-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Scene;

public interface ITransformedSceneObject {
	Transform Transform { get; set; }
	Location Position { get; set; }
	Rotation Rotation { get; set; }
	Vect Scaling { get; set; }

	void ScaleBy(float scalar);
	void ScaleBy(Vect vect);
	void RotateBy(Rotation rotation);
	void MoveBy(Vect translation);
}