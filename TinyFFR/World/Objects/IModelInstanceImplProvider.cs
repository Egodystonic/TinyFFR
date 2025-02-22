// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public interface IModelInstanceImplProvider : IDisposableResourceImplProvider<ModelInstance> {
	Transform GetTransform(ResourceHandle<ModelInstance> handle);
	void SetTransform(ResourceHandle<ModelInstance> handle, Transform newTransform);
	
	Location GetPosition(ResourceHandle<ModelInstance> handle);
	void SetPosition(ResourceHandle<ModelInstance> handle, Location newPosition);

	Rotation GetRotation(ResourceHandle<ModelInstance> handle);
	void SetRotation(ResourceHandle<ModelInstance> handle, Rotation newRotation);

	Vect GetScaling(ResourceHandle<ModelInstance> handle);
	void SetScaling(ResourceHandle<ModelInstance> handle, Vect newScaling);

	Material GetMaterial(ResourceHandle<ModelInstance> handle);
	void SetMaterial(ResourceHandle<ModelInstance> handle, Material newMaterial);

	Mesh GetMesh(ResourceHandle<ModelInstance> handle);
	void SetMesh(ResourceHandle<ModelInstance> handle, Mesh newMesh);

	void TranslateBy(ResourceHandle<ModelInstance> handle, Vect translation);
	void RotateBy(ResourceHandle<ModelInstance> handle, Rotation rotation);
	void ScaleBy(ResourceHandle<ModelInstance> handle, float scalar);
	void ScaleBy(ResourceHandle<ModelInstance> handle, Vect vect);
	void AdjustScaleBy(ResourceHandle<ModelInstance> handle, float scalar);
	void AdjustScaleBy(ResourceHandle<ModelInstance> handle, Vect vect);
}