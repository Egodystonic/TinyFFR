// Created on 2026-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// The tree of joints belonging to one mesh, which skeletal animations move and the mesh's vertices follow.
/// </summary>
/// <param name="Mesh">The mesh whose skeleton this is.</param>
public readonly record struct MeshSkeleton(Mesh Mesh) {
	/// <summary>
	/// The joints that make up this skeleton.
	/// </summary>
	public MeshNodeIndex Nodes {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(this);
	}
	
	/// <summary>
	/// Poses the given object in this skeleton's bind pose, i.e. the shape the mesh takes with no animation applied.
	/// </summary>
	/// <remarks>
	/// This is what to apply to return an object to rest after an animation has finished with it.
	/// </remarks>
	/// <param name="targetInstance">The object to pose. Its mesh must be the one this skeleton belongs to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyBindPose(ModelInstance targetInstance) => Mesh.ApplySkeletalBindPose(targetInstance);
	
	/// <summary>
	/// Reports where the given joint sits in the bind pose.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the mesh's own origin, so multiplying one by an object's own transform gives a world-space
	/// position.
	/// </remarks>
	/// <param name="node">The joint whose position is wanted.</param>
	/// <param name="modelSpaceTransform">Set to the joint's transform, relative to the mesh's own origin.</param>
	public void GetBindPoseNodeTransforms(MeshNode node, out Matrix4x4 modelSpaceTransform) {
		Unsafe.SkipInit(out modelSpaceTransform);
		GetBindPoseNodeTransforms(new ReadOnlySpan<MeshNode>(in node), new Span<Matrix4x4>(ref modelSpaceTransform));
	}
	
	/// <summary>
	/// Reports where the given joints sit in the bind pose.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the mesh's own origin, so multiplying one by an object's own transform gives a world-space
	/// position.
	/// </remarks>
	/// <param name="nodes">The joints whose positions are wanted.</param>
	/// <param name="modelSpaceTransforms">Receives each joint's transform, in the same order the joints were given. Must be at least as long as <paramref name="nodes"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetBindPoseNodeTransforms(ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) => Mesh.GetSkeletalBindPoseNodeModelTransforms(nodes, modelSpaceTransforms);
	
	/// <summary>
	/// Reports where the joints at the given indices sit in the bind pose.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the mesh's own origin, so multiplying one by an object's own transform gives a world-space
	/// position.
	/// </remarks>
	/// <param name="nodeIndices">The indices of the joints whose positions are wanted. Indices can be put on the stack where the joints themselves cannot.</param>
	/// <param name="modelSpaceTransforms">Receives each joint's transform, in the same order the indices were given. Must be at least as long as <paramref name="nodeIndices"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetBindPoseNodeTransforms(ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) => Mesh.GetSkeletalBindPoseNodeModelTransforms(nodeIndices, modelSpaceTransforms);
}