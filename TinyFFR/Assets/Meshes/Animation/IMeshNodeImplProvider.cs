// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// Provides the implementation behind <see cref="MeshNode"/>.
/// </summary>
public interface IMeshNodeImplProvider : IResourceImplProvider<MeshNode> {
	/// <summary>
	/// Invoked via <see cref="MeshNode.Index"/>.
	/// </summary>
	int GetIndex(ResourceHandle<MeshNode> handle);
	/// <summary>
	/// Invoked internally to determine whether a mesh node's owning mesh has been disposed.
	/// </summary>
	bool IsDisposed(ResourceHandle<MeshNode> handle);
}