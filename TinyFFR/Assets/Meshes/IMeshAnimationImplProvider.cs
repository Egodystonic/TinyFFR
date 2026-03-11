// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshAnimationImplProvider : IResourceImplProvider<MeshAnimation> {
	float GetDefaultDurationSeconds(ResourceHandle<MeshAnimation> handle);
	MeshAnimationType GetType(ResourceHandle<MeshAnimation> handle);
	void Apply(ResourceHandle<MeshAnimation> handle, ModelInstance targetInstance, float targetTimePointSeconds);
	void GetNodeTransforms(ResourceHandle<MeshAnimation> handle, float targetTimePointSeconds, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms);
	void ApplyAndGetNodeTransforms(ResourceHandle<MeshAnimation> handle, ModelInstance targetInstance, float targetTimePointSeconds, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms);
	bool IsDisposed(ResourceHandle<MeshAnimation> handle);
}