// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshImplProvider : IDisposableResourceImplProvider<Mesh> {
	MeshBufferData GetBufferData(ResourceHandle<Mesh> handle);
	IndirectEnumerable<Mesh, MeshAnimation> GetAnimations(ResourceHandle<Mesh> handle, MeshAnimationType? type);
	MeshAnimation? TryGetAnimationByName(ResourceHandle<Mesh> handle, ReadOnlySpan<char> name, MeshAnimationType? type);
	void ApplySkeletalBindPose(ResourceHandle<Mesh> handle, ModelInstance targetInstance);
	bool GetHasAnyAnimations(ResourceHandle<Mesh> handle);
}