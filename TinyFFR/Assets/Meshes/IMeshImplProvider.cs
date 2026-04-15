// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshImplProvider : IDisposableResourceImplProvider<Mesh> {
	MeshBufferData GetBufferData(ResourceHandle<Mesh> handle);
	PositionedCuboid GetBoundingBox(ResourceHandle<Mesh> handle);
	IndirectEnumerable<Mesh, MeshAnimation> GetAnimations(ResourceHandle<Mesh> handle, MeshAnimationType? type);
	IndirectEnumerable<Mesh, MeshNode> GetNodes(ResourceHandle<Mesh> handle);
	MeshAnimation? TryGetAnimationByName(ResourceHandle<Mesh> handle, ReadOnlySpan<char> name, MeshAnimationType? type);
	MeshNode? TryGetNodeByName(ResourceHandle<Mesh> handle, ReadOnlySpan<char> name);
	void ApplySkeletalBindPose(ResourceHandle<Mesh> handle, ModelInstance targetInstance);
	void GetSkeletalBindPoseNodeModelTransforms(ResourceHandle<Mesh> handle, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms);
	void GetSkeletalBindPoseNodeModelTransforms(ResourceHandle<Mesh> handle, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms);
}