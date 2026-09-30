// Created on 2026-09-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshGroupAnimationTableImplProvider : IDisposableResourceImplProvider<MeshGroupAnimationTable> {
	IndirectEnumerable<MeshGroupAnimationTable, MeshAnimation> GetAnimations(ResourceHandle<MeshGroupAnimationTable> handle, MeshAnimationType? type);
	MeshAnimation? TryGetAnimationByName(ResourceHandle<MeshGroupAnimationTable> handle, ReadOnlySpan<char> name, MeshAnimationType? type);
	IndirectEnumerable<MeshGroupAnimationTable, MeshNode> GetNodes(ResourceHandle<MeshGroupAnimationTable> handle);
	MeshNode? TryGetNodeByName(ResourceHandle<MeshGroupAnimationTable> handle, ReadOnlySpan<char> name);
	void ApplyBindPose(ResourceHandle<MeshGroupAnimationTable> handle, SceneObject targetInstance);
	void GetBindPoseNodeTransforms(ResourceHandle<MeshGroupAnimationTable> handle, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms);
	void GetBindPoseNodeTransforms(ResourceHandle<MeshGroupAnimationTable> handle, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms);
}
