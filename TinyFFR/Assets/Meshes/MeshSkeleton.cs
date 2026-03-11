// Created on 2026-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly record struct MeshSkeleton(Mesh Mesh) {
	public IndirectEnumerable<Mesh, MeshNode> Nodes {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Mesh.GetNodes();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshNode? TryGetNodeByName(ReadOnlySpan<char> name) => Mesh.TryGetNodeByName(name);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyBindPose(ModelInstance targetInstance) => Mesh.ApplySkeletalBindPose(targetInstance);
	
	public void GetBindPoseNodeTransform(MeshNode node, out Matrix4x4 modelSpaceTransform) {
		Unsafe.SkipInit(out modelSpaceTransform);
		GetBindPoseNodeTransforms(new ReadOnlySpan<MeshNode>(in node), new Span<Matrix4x4>(ref modelSpaceTransform));
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetBindPoseNodeTransforms(ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) => Mesh.GetSkeletalBindPoseNodeModelTransforms(nodes, modelSpaceTransforms);
}