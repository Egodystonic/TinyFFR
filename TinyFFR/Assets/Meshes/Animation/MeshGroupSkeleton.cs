// Created on 2026-09-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly record struct MeshGroupSkeleton(MeshGroupAnimationTable Table) : IMeshSkeleton<MeshGroupNodeIndex> {
	public MeshGroupNodeIndex Nodes {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyBindPose(SceneObject targetInstance) => Table.ApplyBindPose(targetInstance);

	public void GetBindPoseNodeTransforms(MeshNode node, out Matrix4x4 modelSpaceTransform) {
		Unsafe.SkipInit(out modelSpaceTransform);
		GetBindPoseNodeTransforms(new ReadOnlySpan<MeshNode>(in node), new Span<Matrix4x4>(ref modelSpaceTransform));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetBindPoseNodeTransforms(ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) => Table.GetBindPoseNodeTransforms(nodes, modelSpaceTransforms);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetBindPoseNodeTransforms(ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) => Table.GetBindPoseNodeTransforms(nodeIndices, modelSpaceTransforms);
}
