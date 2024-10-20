// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.Scene;

public interface IObjectBuilder {
	ModelInstance CreateModelInstance(Mesh mesh, Material material);
	ModelInstance CreateModelInstance(Mesh mesh, Material material, in ModelInstanceCreationConfig config);
}