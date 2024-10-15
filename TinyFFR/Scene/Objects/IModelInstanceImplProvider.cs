// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.Scene;

public interface IModelInstanceImplProvider : IDisposableResourceImplProvider<ModelInstanceHandle> {
	Material GetMaterial(ModelInstanceHandle handle);
	void SetMaterial(ModelInstanceHandle handle, Material newMaterial);

	Mesh GetMesh(ModelInstanceHandle handle);
	void SetMesh(ModelInstanceHandle handle, Mesh newMesh);
}