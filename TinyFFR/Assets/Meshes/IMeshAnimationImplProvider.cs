// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshAnimationImplProvider : IResourceImplProvider<MeshAnimation> {
	float GetDefaultCompletionTimeSeconds(ResourceHandle<MeshAnimation> handle);
	MeshAnimationType GetType(ResourceHandle<MeshAnimation> handle);
	void Apply(ResourceHandle<MeshAnimation> handle, ModelInstance targetInstance, float targetTimePointSeconds);
	bool IsDisposed(ResourceHandle<MeshAnimation> handle);
}