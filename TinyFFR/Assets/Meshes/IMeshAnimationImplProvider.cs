// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// Provides the implementation behind <see cref="MeshAnimation"/>.
/// </summary>
public interface IMeshAnimationImplProvider : IResourceImplProvider<MeshAnimation> {
	/// <summary>
	/// Invoked via <see cref="MeshAnimation.DefaultDurationSeconds"/>.
	/// </summary>
	float GetDefaultDurationSeconds(ResourceHandle<MeshAnimation> handle);
	/// <summary>
	/// Invoked via <see cref="MeshAnimation.Type"/>.
	/// </summary>
	MeshAnimationType GetType(ResourceHandle<MeshAnimation> handle);
	
	/// <summary>
	/// Invoked via <see cref="MeshAnimation.Apply"/>.
	/// </summary>
	void Apply(ModelInstance targetInstance, ResourceHandle<MeshAnimation> handle, float targetTimePointSeconds);
	/// <summary>
	/// Invoked via <see cref="MeshAnimation.GetNodeTransforms(float, ReadOnlySpan{MeshNode}, Span{Matrix4x4})"/>.
	/// </summary>
	void GetNodeTransforms(ResourceHandle<MeshAnimation> handle, float targetTimePointSeconds, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms);
	/// <summary>
	/// Invoked via <see cref="MeshAnimation.GetNodeTransforms(float, ReadOnlySpan{int}, Span{Matrix4x4})"/>.
	/// </summary>
	void GetNodeTransforms(ResourceHandle<MeshAnimation> handle, float targetTimePointSeconds, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms);
	/// <summary>
	/// Invoked via <see cref="MeshAnimation.ApplyAndGetNodeTransforms(ModelInstance, float, ReadOnlySpan{MeshNode}, Span{Matrix4x4})"/>.
	/// </summary>
	void ApplyAndGetNodeTransforms(ModelInstance targetInstance, ResourceHandle<MeshAnimation> handle, float targetTimePointSeconds, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms);
	/// <summary>
	/// Invoked via <see cref="MeshAnimation.ApplyAndGetNodeTransforms(ModelInstance, float, ReadOnlySpan{int}, Span{Matrix4x4})"/>.
	/// </summary>
	void ApplyAndGetNodeTransforms(ModelInstance targetInstance, ResourceHandle<MeshAnimation> handle, float targetTimePointSeconds, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms);
	
	/// <summary>
	/// Invoked via <see cref="MeshAnimation.ApplyBlended"/>.
	/// </summary>
	void ApplyBlended(ModelInstance targetInstance, ResourceHandle<MeshAnimation> startAnimHandle, float startAnimTargetTimePointSeconds, ResourceHandle<MeshAnimation> endAnimHandle, float endAnimTargetTimePointSeconds, float interpolationDistance);
	/// <summary>
	/// Invoked via <see cref="MeshAnimation.ApplyBlendedAndGetNodeTransforms(ModelInstance, float, MeshAnimation, float, float, ReadOnlySpan{MeshNode}, Span{Matrix4x4})"/>.
	/// </summary>
	void ApplyBlendedAndGetNodeTransforms(ModelInstance targetInstance, ResourceHandle<MeshAnimation> startAnimHandle, float startAnimTargetTimePointSeconds, ResourceHandle<MeshAnimation> endAnimHandle, float endAnimTargetTimePointSeconds, float interpolationDistance, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms);
	/// <summary>
	/// Invoked via <see cref="MeshAnimation.ApplyBlendedAndGetNodeTransforms(ModelInstance, float, MeshAnimation, float, float, ReadOnlySpan{int}, Span{Matrix4x4})"/>.
	/// </summary>
	void ApplyBlendedAndGetNodeTransforms(ModelInstance targetInstance, ResourceHandle<MeshAnimation> startAnimHandle, float startAnimTargetTimePointSeconds, ResourceHandle<MeshAnimation> endAnimHandle, float endAnimTargetTimePointSeconds, float interpolationDistance, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms);
	/// <summary>
	/// Invoked via <see cref="MeshAnimation.GetBlendedNodeTransforms(float, MeshAnimation, float, float, ReadOnlySpan{MeshNode}, Span{Matrix4x4})"/>.
	/// </summary>
	void GetBlendedNodeTransforms(ResourceHandle<MeshAnimation> startAnimHandle, float startAnimTargetTimePointSeconds, ResourceHandle<MeshAnimation> endAnimHandle, float endAnimTargetTimePointSeconds, float interpolationDistance, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms);
	/// <summary>
	/// Invoked via <see cref="MeshAnimation.GetBlendedNodeTransforms(float, MeshAnimation, float, float, ReadOnlySpan{int}, Span{Matrix4x4})"/>.
	/// </summary>
	void GetBlendedNodeTransforms(ResourceHandle<MeshAnimation> startAnimHandle, float startAnimTargetTimePointSeconds, ResourceHandle<MeshAnimation> endAnimHandle, float endAnimTargetTimePointSeconds, float interpolationDistance, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms);
	
	/// <summary>
	/// Invoked internally to determine whether a mesh animation's owning mesh has been disposed.
	/// </summary>
	bool IsDisposed(ResourceHandle<MeshAnimation> handle);
}