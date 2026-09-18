// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// Provides the implementation behind <see cref="Mesh"/>.
/// </summary>
public interface IMeshImplProvider : IDisposableResourceImplProvider<Mesh> {
	/// <summary>
	/// Invoked via <see cref="Mesh.BufferData"/>.
	/// </summary>
	MeshBufferData GetBufferData(ResourceHandle<Mesh> handle);
	/// <summary>
	/// Invoked via <see cref="Mesh.SupportsWireframeRendering"/>.
	/// </summary>
	MeshBufferData? GetWireframeBufferData(ResourceHandle<Mesh> handle);
	/// <summary>
	/// Invoked via <see cref="Mesh.BoundingBox"/>.
	/// </summary>
	PositionedCuboid GetBoundingBox(ResourceHandle<Mesh> handle);
	/// <summary>
	/// Invoked via <see cref="Mesh.AxisAlignedBoundingBox"/>.
	/// </summary>
	PositionedCuboid GetAxisAlignedBoundingBox(ResourceHandle<Mesh> handle);
	/// <summary>
	/// Invoked via <see cref="Mesh.BoundingSphere"/>.
	/// </summary>
	PositionedSphere GetBoundingSphere(ResourceHandle<Mesh> handle);
	/// <summary>
	/// Invoked via <see cref="Mesh.AllowsPerInstanceVertexMutation"/>.
	/// </summary>
	bool GetAllowsPerInstanceVertexMutation(ResourceHandle<Mesh> handle);
	/// <summary>
	/// Invoked via <see cref="Mesh.BorrowDefaultVerticesSpan()"/>, <see cref="Mesh.BorrowDefaultVerticesSpan(Range)"/>.
	/// </summary>
	ScopedReadOnlySpanLease<MeshVertex> BorrowDefaultVerticesSpan(ResourceHandle<Mesh> handle, Range range);
	/// <summary>
	/// Invoked via <see cref="MeshAnimationIndex.All"/>, <see cref="MeshAnimationIndex.Skeletal"/>, <see cref="MeshAnimationIndex.Morphing"/>.
	/// </summary>
	IndirectEnumerable<Mesh, MeshAnimation> GetAnimations(ResourceHandle<Mesh> handle, MeshAnimationType? type);
	/// <summary>
	/// Invoked via <see cref="MeshNodeIndex.All"/>.
	/// </summary>
	IndirectEnumerable<Mesh, MeshNode> GetNodes(ResourceHandle<Mesh> handle);
	/// <summary>
	/// Invoked via <see cref="MeshAnimationIndex.TryGetAnimationByName(ReadOnlySpan{char})"/>, <see cref="MeshAnimationIndex.TryGetAnimationByName(ReadOnlySpan{char}, MeshAnimationType)"/>.
	/// </summary>
	MeshAnimation? TryGetAnimationByName(ResourceHandle<Mesh> handle, ReadOnlySpan<char> name, MeshAnimationType? type);
	/// <summary>
	/// Invoked via <see cref="MeshNodeIndex.TryGetNodeByName"/>.
	/// </summary>
	MeshNode? TryGetNodeByName(ResourceHandle<Mesh> handle, ReadOnlySpan<char> name);
	/// <summary>
	/// Invoked via <see cref="MeshSkeleton.ApplyBindPose"/>.
	/// </summary>
	void ApplySkeletalBindPose(ResourceHandle<Mesh> handle, ModelInstance targetInstance);
	/// <summary>
	/// Invoked via <see cref="MeshSkeleton.GetBindPoseNodeTransforms(ReadOnlySpan{MeshNode}, Span{Matrix4x4})"/>.
	/// </summary>
	void GetSkeletalBindPoseNodeModelTransforms(ResourceHandle<Mesh> handle, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms);
	/// <summary>
	/// Invoked via <see cref="MeshSkeleton.GetBindPoseNodeTransforms(ReadOnlySpan{int}, Span{Matrix4x4})"/>.
	/// </summary>
	void GetSkeletalBindPoseNodeModelTransforms(ResourceHandle<Mesh> handle, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms);
}