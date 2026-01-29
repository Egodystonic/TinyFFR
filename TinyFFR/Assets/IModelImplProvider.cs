// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets;

public interface IModelImplProvider : IDisposableResourceImplProvider<Model> {
	Mesh GetMesh(ResourceHandle<Model> handle);
	Material GetMaterial(ResourceHandle<Model> handle);
}