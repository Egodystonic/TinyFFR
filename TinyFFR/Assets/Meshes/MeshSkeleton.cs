// Created on 2026-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly record struct MeshSkeleton(Mesh Mesh) {
	public MeshNodeIndex Nodes {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(this);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyBindPose(ModelInstance targetInstance) => Mesh.ApplySkeletalBindPose(targetInstance);
	
	public void GetBindPoseNodeTransforms(MeshNode node, out Matrix4x4 modelSpaceTransform) {
		Unsafe.SkipInit(out modelSpaceTransform);
		GetBindPoseNodeTransforms(new ReadOnlySpan<MeshNode>(in node), new Span<Matrix4x4>(ref modelSpaceTransform));
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetBindPoseNodeTransforms(ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) => Mesh.GetSkeletalBindPoseNodeModelTransforms(nodes, modelSpaceTransforms);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetBindPoseNodeTransforms(ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) => Mesh.GetSkeletalBindPoseNodeModelTransforms(nodeIndices, modelSpaceTransforms);
}