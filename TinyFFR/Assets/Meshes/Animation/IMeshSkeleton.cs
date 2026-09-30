// Created on 2026-09-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshSkeleton {
	void ApplyBindPose(SceneObject targetInstance);
	void GetBindPoseNodeTransforms(MeshNode node, out Matrix4x4 modelSpaceTransform);
	void GetBindPoseNodeTransforms(ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms);
	void GetBindPoseNodeTransforms(ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms);
}

public interface IMeshSkeleton<out TNodeIndex> : IMeshSkeleton where TNodeIndex : IMeshNodeIndex {
	TNodeIndex Nodes {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get;
	}
}
