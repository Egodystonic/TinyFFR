// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets;

/// <summary>
/// Provides the implementation behind <see cref="Model"/>.
/// </summary>
public interface IModelImplProvider : IDisposableResourceImplProvider<Model> {
	/// <summary>
	/// Invoked via <see cref="Model.Mesh"/>.
	/// </summary>
	Mesh GetMesh(ResourceHandle<Model> handle);
	/// <summary>
	/// Invoked via <see cref="Model.Material"/>.
	/// </summary>
	Material GetMaterial(ResourceHandle<Model> handle);
}