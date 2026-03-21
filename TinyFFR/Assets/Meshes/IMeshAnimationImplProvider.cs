// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public interface IMeshAnimationImplProvider : IResourceImplProvider<MeshAnimation> {
	float GetDefaultDurationSeconds(ResourceHandle<MeshAnimation> handle);
	MeshAnimationType GetType(ResourceHandle<MeshAnimation> handle);
	
	void Apply(ModelInstance targetInstance, ResourceHandle<MeshAnimation> handle, float targetTimePointSeconds);
	void GetNodeTransforms(ResourceHandle<MeshAnimation> handle, float targetTimePointSeconds, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms);
	void GetNodeTransforms(ResourceHandle<MeshAnimation> handle, float targetTimePointSeconds, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms);
	void ApplyAndGetNodeTransforms(ModelInstance targetInstance, ResourceHandle<MeshAnimation> handle, float targetTimePointSeconds, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms);
	void ApplyAndGetNodeTransforms(ModelInstance targetInstance, ResourceHandle<MeshAnimation> handle, float targetTimePointSeconds, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms);
	
	void ApplyBlended(ModelInstance targetInstance, ResourceHandle<MeshAnimation> startAnimHandle, float startAnimTargetTimePointSeconds, ResourceHandle<MeshAnimation> endAnimHandle, float endAnimTargetTimePointSeconds, float interpolationDistance);
	void ApplyBlendedAndGetNodeTransforms(ModelInstance targetInstance, ResourceHandle<MeshAnimation> startAnimHandle, float startAnimTargetTimePointSeconds, ResourceHandle<MeshAnimation> endAnimHandle, float endAnimTargetTimePointSeconds, float interpolationDistance, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms);
	void ApplyBlendedAndGetNodeTransforms(ModelInstance targetInstance, ResourceHandle<MeshAnimation> startAnimHandle, float startAnimTargetTimePointSeconds, ResourceHandle<MeshAnimation> endAnimHandle, float endAnimTargetTimePointSeconds, float interpolationDistance, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms);
	void GetBlendedNodeTransforms(ResourceHandle<MeshAnimation> startAnimHandle, float startAnimTargetTimePointSeconds, ResourceHandle<MeshAnimation> endAnimHandle, float endAnimTargetTimePointSeconds, float interpolationDistance, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms);
	void GetBlendedNodeTransforms(ResourceHandle<MeshAnimation> startAnimHandle, float startAnimTargetTimePointSeconds, ResourceHandle<MeshAnimation> endAnimHandle, float endAnimTargetTimePointSeconds, float interpolationDistance, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms);
	
	bool IsDisposed(ResourceHandle<MeshAnimation> handle);
}