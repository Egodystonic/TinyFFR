// Created on 2026-03-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// One joint in the tree of joints that makes up a skeletal mesh's skeleton.
/// </summary>
/// <remarks>
/// <para>
/// A skeletal mesh is animated by moving a small number of joints and letting each vertex follow the joints it is attached to.
/// The joints form a tree — moving an elbow carries the forearm and hand with it — and each node here is one entry in that tree,
/// identifying its parent by index.
/// </para>
/// <para>
/// Every node is a joint, but not every node drives vertices directly: Only those with a <paramref name="CorrespondingBoneIndex"/>
/// are "bones" that vertices can be weighted against. The others exist purely to position their descendants.
/// </para>
/// </remarks>
/// <param name="DefaultLocalTransform">The transform relative to this node's parent that places it correctly for the mesh's
/// bind pose, i.e. where it sits when no animation is playing.</param>
/// <param name="BindPoseInversion">The transform that takes vertices from the mesh's own space in to this node's local space.</param>
/// <param name="ParentNodeIndex">The index of this node's parent in the skeleton's node list, or <see langword="null"/> if this
/// is the root node.</param>
/// <param name="CorrespondingBoneIndex">The index of the bone this node drives, or <see langword="null"/> if this node is not a
/// bone and therefore has no vertices weighted against it.</param>
public readonly record struct SkeletalAnimationNode(Matrix4x4 DefaultLocalTransform, Matrix4x4 BindPoseInversion, int? ParentNodeIndex, int? CorrespondingBoneIndex);
