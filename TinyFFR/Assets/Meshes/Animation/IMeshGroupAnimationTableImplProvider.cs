// Created on 2026-09-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// Provides the implementation behind <see cref="MeshGroupAnimationTable"/>.
/// </summary>
public interface IMeshGroupAnimationTableImplProvider : IDisposableResourceImplProvider<MeshGroupAnimationTable> {
	/// <summary>
	/// Invoked via <see cref="MeshGroupAnimationIndex.All"/>, <see cref="MeshGroupAnimationIndex.Skeletal"/> and <see cref="MeshGroupAnimationIndex.Morphing"/>.
	/// </summary>
	IndirectEnumerable<MeshGroupAnimationTable, MeshAnimation> GetAnimations(ResourceHandle<MeshGroupAnimationTable> handle, MeshAnimationType? type);
	/// <summary>
	/// Invoked via <see cref="MeshGroupAnimationIndex.TryGetAnimationByName(ReadOnlySpan{char})"/> and <see cref="MeshGroupAnimationIndex.TryGetAnimationByName(ReadOnlySpan{char}, MeshAnimationType)"/>.
	/// </summary>
	MeshAnimation? TryGetAnimationByName(ResourceHandle<MeshGroupAnimationTable> handle, ReadOnlySpan<char> name, MeshAnimationType? type);
	/// <summary>
	/// Invoked via <see cref="MeshGroupNodeIndex.All"/>.
	/// </summary>
	IndirectEnumerable<MeshGroupAnimationTable, MeshNode> GetNodes(ResourceHandle<MeshGroupAnimationTable> handle);
	/// <summary>
	/// Invoked via <see cref="MeshGroupNodeIndex.TryGetNodeByName"/>.
	/// </summary>
	MeshNode? TryGetNodeByName(ResourceHandle<MeshGroupAnimationTable> handle, ReadOnlySpan<char> name);
	/// <summary>
	/// Invoked via <see cref="MeshGroupSkeleton.ApplyBindPose"/>.
	/// </summary>
	void ApplyBindPose(ResourceHandle<MeshGroupAnimationTable> handle, SceneObject targetInstance);
	/// <summary>
	/// Invoked via <see cref="MeshGroupSkeleton.GetBindPoseNodeTransforms(ReadOnlySpan{MeshNode}, Span{Matrix4x4})"/>.
	/// </summary>
	void GetBindPoseNodeTransforms(ResourceHandle<MeshGroupAnimationTable> handle, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms);
	/// <summary>
	/// Invoked via <see cref="MeshGroupSkeleton.GetBindPoseNodeTransforms(ReadOnlySpan{int}, Span{Matrix4x4})"/>.
	/// </summary>
	void GetBindPoseNodeTransforms(ResourceHandle<MeshGroupAnimationTable> handle, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms);
}
