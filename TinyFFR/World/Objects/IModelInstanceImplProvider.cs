// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public interface IModelInstanceImplProvider : IDisposableResourceImplProvider<ModelInstanceHandle> {
	Transform GetTransform(ModelInstanceHandle handle);
	void SetTransform(ModelInstanceHandle handle, Transform newTransform);
	
	Location GetPosition(ModelInstanceHandle handle);
	void SetPosition(ModelInstanceHandle handle, Location newPosition);

	Rotation GetRotation(ModelInstanceHandle handle);
	void SetRotation(ModelInstanceHandle handle, Rotation newRotation);

	Vect GetScaling(ModelInstanceHandle handle);
	void SetScaling(ModelInstanceHandle handle, Vect newScaling);

	Material GetMaterial(ModelInstanceHandle handle);
	void SetMaterial(ModelInstanceHandle handle, Material newMaterial);

	Mesh GetMesh(ModelInstanceHandle handle);
	void SetMesh(ModelInstanceHandle handle, Mesh newMesh);

	void TranslateBy(ModelInstanceHandle handle, Vect translation);
	void RotateBy(ModelInstanceHandle handle, Rotation rotation);
	void ScaleBy(ModelInstanceHandle handle, float scalar);
	void ScaleBy(ModelInstanceHandle handle, Vect vect);
	void AdjustScaleBy(ModelInstanceHandle handle, float scalar);
	void AdjustScaleBy(ModelInstanceHandle handle, Vect vect);
}