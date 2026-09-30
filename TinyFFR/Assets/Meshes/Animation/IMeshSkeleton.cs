// Created on 2026-09-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// A tree of joints which skeletal animations move and a mesh's vertices follow.
/// </summary>
/// <remarks>
/// <see cref="MeshSkeleton"/> is the implementation for a single <see cref="Mesh"/>; <see cref="MeshGroupSkeleton"/> is the implementation for a skeleton
/// shared by every mesh in a <see cref="MeshGroupAnimationTable"/>.
/// </remarks>
public interface IMeshSkeleton {
	/// <summary>
	/// Poses the given object in this skeleton's bind pose, i.e. the shape its mesh(es) take with no animation applied.
	/// </summary>
	/// <param name="targetInstance">The object to pose. Objects whose mesh(es) this skeleton does not drive are left unchanged.</param>
	void ApplyBindPose(SceneObject targetInstance);
	/// <summary>
	/// Reports where the given joint sits in the bind pose.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the mesh's own origin, so multiplying one by an object's own transform gives a world-space
	/// position.
	/// </remarks>
	/// <param name="node">The joint whose position is wanted.</param>
	/// <param name="modelSpaceTransform">Set to the joint's transform, relative to the mesh's own origin.</param>
	void GetBindPoseNodeTransforms(MeshNode node, out Matrix4x4 modelSpaceTransform);
	/// <summary>
	/// Reports where the given joints sit in the bind pose.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the mesh's own origin, so multiplying one by an object's own transform gives a world-space
	/// position.
	/// </remarks>
	/// <param name="nodes">The joints whose positions are wanted.</param>
	/// <param name="modelSpaceTransforms">Receives each joint's transform, in the same order the joints were given. Must be at least as long as <paramref name="nodes"/>.</param>
	void GetBindPoseNodeTransforms(ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms);
	/// <summary>
	/// Reports where the joints at the given indices sit in the bind pose.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the mesh's own origin, so multiplying one by an object's own transform gives a world-space
	/// position.
	/// </remarks>
	/// <param name="nodeIndices">The indices of the joints whose positions are wanted. Indices can be put on the stack where the joints themselves cannot.</param>
	/// <param name="modelSpaceTransforms">Receives each joint's transform, in the same order the indices were given. Must be at least as long as <paramref name="nodeIndices"/>.</param>
	void GetBindPoseNodeTransforms(ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms);
}

/// <summary>
/// An <see cref="IMeshSkeleton"/> that exposes its joints through a strongly-typed node index.
/// </summary>
/// <typeparam name="TNodeIndex">The type of index used to look up this skeleton's joints.</typeparam>
public interface IMeshSkeleton<out TNodeIndex> : IMeshSkeleton where TNodeIndex : IMeshNodeIndex {
	/// <summary>
	/// The joints that make up this skeleton.
	/// </summary>
	TNodeIndex Nodes {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get;
	}
}
