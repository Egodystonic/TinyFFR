// Created on 2026-09-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// The tree of joints shared by a group of meshes, which skeletal animations move and every mesh's vertices follow.
/// </summary>
/// <remarks>
/// This is the group equivalent of <see cref="MeshSkeleton"/>. You'll normally reach it via <see cref="ModelBundle.Skeleton"/> or
/// <see cref="ModelInstanceGroup.Skeleton"/>, rather than constructing one yourself.
/// </remarks>
/// <param name="Table">The animation table whose skeleton this is.</param>
public readonly record struct MeshGroupSkeleton(MeshGroupAnimationTable Table) : IMeshSkeleton<MeshGroupNodeIndex> {
	/// <summary>
	/// The joints that make up this skeleton.
	/// </summary>
	public MeshGroupNodeIndex Nodes {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(this);
	}

	/// <summary>
	/// Poses the given object in this skeleton's bind pose, i.e. the shape the group's meshes take with no animation applied.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is what to apply to return an object to rest after an animation has finished with it.
	/// </para>
	/// <para>
	/// The target is usually a <see cref="ModelInstanceGroup"/>, in which case every instance in it is posed. Instances (or a lone
	/// <see cref="ModelInstance"/>) whose mesh is not part of this group are left unchanged, as is any target that is not a model instance or instance group.
	/// </para>
	/// </remarks>
	/// <param name="targetInstance">The object to pose.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyBindPose(SceneObject targetInstance) => Table.ApplyBindPose(targetInstance);

	/// <summary>
	/// Reports where the given joint sits in the bind pose.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the meshes' shared origin, so multiplying one by an instance's own transform gives a world-space
	/// position.
	/// </remarks>
	/// <param name="node">The joint whose position is wanted.</param>
	/// <param name="modelSpaceTransform">Set to the joint's transform, relative to the meshes' shared origin.</param>
	public void GetBindPoseNodeTransforms(MeshNode node, out Matrix4x4 modelSpaceTransform) {
		Unsafe.SkipInit(out modelSpaceTransform);
		GetBindPoseNodeTransforms(new ReadOnlySpan<MeshNode>(in node), new Span<Matrix4x4>(ref modelSpaceTransform));
	}
	/// <summary>
	/// Reports where the given joints sit in the bind pose.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the meshes' shared origin, so multiplying one by an instance's own transform gives a world-space
	/// position.
	/// </remarks>
	/// <param name="nodes">The joints whose positions are wanted.</param>
	/// <param name="modelSpaceTransforms">Receives each joint's transform, in the same order the joints were given. Must be at least as long as <paramref name="nodes"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetBindPoseNodeTransforms(ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) => Table.GetBindPoseNodeTransforms(nodes, modelSpaceTransforms);
	/// <summary>
	/// Reports where the joints at the given indices sit in the bind pose.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the meshes' shared origin, so multiplying one by an instance's own transform gives a world-space
	/// position.
	/// </remarks>
	/// <param name="nodeIndices">The indices of the joints whose positions are wanted. Indices can be put on the stack where the joints themselves cannot.</param>
	/// <param name="modelSpaceTransforms">Receives each joint's transform, in the same order the indices were given. Must be at least as long as <paramref name="nodeIndices"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetBindPoseNodeTransforms(ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) => Table.GetBindPoseNodeTransforms(nodeIndices, modelSpaceTransforms);
}
