// Created on 2024-10-31 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Scene;

public interface ITransformedSceneObject {
	Transform Transform { get; set; }
	Location Position { get; set; }
	Rotation Rotation { get; set; }
	Vect Scaling { get; set; }

	void Scale(float scalar);
	void Scale(Vect vect);
	void Rotate(Rotation rotation);
	void Move(Vect translation);
}