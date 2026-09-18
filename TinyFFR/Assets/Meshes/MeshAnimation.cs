// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// One named animation belonging to a mesh, which can pose any object using that mesh at any moment in its timeline.
/// </summary>
/// <remarks>
/// <para>
/// An animation is not a thing that plays by itself; it is a function from a moment in time to a pose. Your own code decides
/// which moment to ask for each frame, which is what makes speed, looping and reversal your choice rather than the animation's.
/// </para>
/// <para>
/// <see cref="MeshAnimationPlayer"/> and <see cref="MeshBlendedAnimationPlayer"/> wrap this with the bookkeeping for speed
/// and wrapping, and are usually easier to use directly.
/// </para>
/// <para>
/// An animation belongs to its mesh and is disposed along with it.
/// </para>
/// </remarks>
public readonly struct MeshAnimation : IResource<MeshAnimation, IMeshAnimationImplProvider> {
	readonly ResourceHandle<MeshAnimation> _handle;
	readonly IMeshAnimationImplProvider _impl;

	internal ResourceHandle<MeshAnimation> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(MeshAnimation)) : _handle;
	internal IMeshAnimationImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<MeshAnimation>();

	IMeshAnimationImplProvider IResource<MeshAnimation, IMeshAnimationImplProvider>.Implementation => Implementation;
	ResourceHandle<MeshAnimation> IResource<MeshAnimation>.Handle => Handle;
	
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	/// <summary>
	/// Which of the two ways of deforming a mesh this animation uses.
	/// </summary>
	public MeshAnimationType Type {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetType(_handle);
	}
	
	/// <summary>
	/// How long this animation runs for at its authored speed, in seconds.
	/// </summary>
	/// <remarks>
	/// An animation player can be asked to run it faster or slower than this without altering the animation itself.
	/// </remarks>
	public float DefaultDurationSeconds {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetDefaultDurationSeconds(_handle);
	}

	internal MeshAnimation(ResourceHandle<MeshAnimation> handle, IMeshAnimationImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}
	
	/// <summary>
	/// Poses the given node as this animation has it at the given moment, without applying the animation to anything.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="targetTimePointSeconds">The moment in the animation to evaluate, in seconds.</param>
	/// <param name="node">The node whose resulting position is wanted.</param>
	/// <param name="modelSpaceTransform">Set to the node's resulting transform, relative to the model's own origin.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetNodeTransforms(float targetTimePointSeconds, MeshNode node, out Matrix4x4 modelSpaceTransform) {
		Unsafe.SkipInit(out modelSpaceTransform);
		GetNodeTransforms(targetTimePointSeconds, new ReadOnlySpan<MeshNode>(in node), new Span<Matrix4x4>(ref modelSpaceTransform));
	}
	
	/// <summary>
	/// Poses the given nodes as this animation has them at the given moment, without applying the animation to anything.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="targetTimePointSeconds">The moment in the animation to evaluate, in seconds.</param>
	/// <param name="nodes">The nodes whose resulting positions are wanted.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetNodeTransforms(float targetTimePointSeconds, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		Implementation.GetNodeTransforms(_handle, targetTimePointSeconds, nodes, modelSpaceTransforms);
	}
	
	/// <summary>
	/// Poses the nodes at the given indices as this animation has them at the given moment, without applying the animation to anything.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="targetTimePointSeconds">The moment in the animation to evaluate, in seconds.</param>
	/// <param name="nodeIndices">The indices of the nodes whose resulting positions are wanted. Indices can be put on the stack where the nodes themselves can not.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetNodeTransforms(float targetTimePointSeconds, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		Implementation.GetNodeTransforms(_handle, targetTimePointSeconds, nodeIndices, modelSpaceTransforms);
	}
	
	/// <summary>
	/// Poses the given object as this animation has it at the given moment.
	/// </summary>
	/// <remarks>
	/// The object's mesh must be the one this animation was attached to.
	/// </remarks>
	/// <param name="targetInstance">The object to pose.</param>
	/// <param name="targetTimePointSeconds">The moment in the animation to apply, in seconds.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Apply(ModelInstance targetInstance, float targetTimePointSeconds) {
		Implementation.Apply(targetInstance, _handle, targetTimePointSeconds);
	}
	
	/// <summary>
	/// Poses the given object as this animation has it at the given moment, and reports where the given node ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="targetInstance">The object to pose.</param>
	/// <param name="targetTimePointSeconds">The moment in the animation to apply, in seconds.</param>
	/// <param name="node">The node whose resulting position is wanted.</param>
	/// <param name="modelSpaceTransform">Set to the node's resulting transform, relative to the model's own origin.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyAndGetNodeTransforms(ModelInstance targetInstance, float targetTimePointSeconds, MeshNode node, out Matrix4x4 modelSpaceTransform) {
		Unsafe.SkipInit(out modelSpaceTransform);
		ApplyAndGetNodeTransforms(targetInstance, targetTimePointSeconds, new ReadOnlySpan<MeshNode>(in node), new Span<Matrix4x4>(ref modelSpaceTransform));
	}
	
	/// <summary>
	/// Poses the given object as this animation has it at the given moment, and reports where the given nodes ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="targetInstance">The object to pose.</param>
	/// <param name="targetTimePointSeconds">The moment in the animation to apply, in seconds.</param>
	/// <param name="nodes">The nodes whose resulting positions are wanted.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyAndGetNodeTransforms(ModelInstance targetInstance, float targetTimePointSeconds, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		Implementation.ApplyAndGetNodeTransforms(targetInstance, _handle, targetTimePointSeconds, nodes, modelSpaceTransforms);
	}
	
	/// <summary>
	/// Poses the given object as this animation has it at the given moment, and reports where the nodes at the given indices ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="targetInstance">The object to pose.</param>
	/// <param name="targetTimePointSeconds">The moment in the animation to apply, in seconds.</param>
	/// <param name="nodeIndices">The indices of the nodes whose resulting positions are wanted. Indices can be put on the stack where the nodes themselves can not.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyAndGetNodeTransforms(ModelInstance targetInstance, float targetTimePointSeconds, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		Implementation.ApplyAndGetNodeTransforms(targetInstance, _handle, targetTimePointSeconds, nodeIndices, modelSpaceTransforms);
	}
	
	/// <summary>
	/// Poses the given node as a blend of this animation and another, without applying anything.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="targetTimePointSeconds">The moment in this animation to evaluate, in seconds.</param>
	/// <param name="blendAnimation">The animation to blend towards.</param>
	/// <param name="blendAnimTargetTimePointSeconds">The moment in <paramref name="blendAnimation"/> to evaluate, in seconds.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely this animation and <c>1f</c> entirely <paramref name="blendAnimation"/>.</param>
	/// <param name="node">The node whose resulting position is wanted.</param>
	/// <param name="modelSpaceTransform">Set to the node's resulting transform, relative to the model's own origin.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetBlendedNodeTransforms(float targetTimePointSeconds, MeshAnimation blendAnimation, float blendAnimTargetTimePointSeconds, float interpolationDistance, MeshNode node, out Matrix4x4 modelSpaceTransform) {
		Unsafe.SkipInit(out modelSpaceTransform);
		GetBlendedNodeTransforms(targetTimePointSeconds, blendAnimation, blendAnimTargetTimePointSeconds, interpolationDistance, new ReadOnlySpan<MeshNode>(in node), new Span<Matrix4x4>(ref modelSpaceTransform));
	}
	
	/// <summary>
	/// Poses the given nodes as a blend of this animation and another, without applying anything.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="targetTimePointSeconds">The moment in this animation to evaluate, in seconds.</param>
	/// <param name="blendAnimation">The animation to blend towards.</param>
	/// <param name="blendAnimTargetTimePointSeconds">The moment in <paramref name="blendAnimation"/> to evaluate, in seconds.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely this animation and <c>1f</c> entirely <paramref name="blendAnimation"/>.</param>
	/// <param name="nodes">The nodes whose resulting positions are wanted.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetBlendedNodeTransforms(float targetTimePointSeconds, MeshAnimation blendAnimation, float blendAnimTargetTimePointSeconds, float interpolationDistance, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		Implementation.GetBlendedNodeTransforms(_handle, targetTimePointSeconds, blendAnimation.Handle, blendAnimTargetTimePointSeconds, interpolationDistance, nodes, modelSpaceTransforms);
	}
	
	/// <summary>
	/// Poses the nodes at the given indices as a blend of this animation and another, without applying anything.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="targetTimePointSeconds">The moment in this animation to evaluate, in seconds.</param>
	/// <param name="blendAnimation">The animation to blend towards.</param>
	/// <param name="blendAnimTargetTimePointSeconds">The moment in <paramref name="blendAnimation"/> to evaluate, in seconds.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely this animation and <c>1f</c> entirely <paramref name="blendAnimation"/>.</param>
	/// <param name="nodeIndices">The indices of the nodes whose resulting positions are wanted. Indices can be put on the stack where the nodes themselves can not.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetBlendedNodeTransforms(float targetTimePointSeconds, MeshAnimation blendAnimation, float blendAnimTargetTimePointSeconds, float interpolationDistance, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		Implementation.GetBlendedNodeTransforms(_handle, targetTimePointSeconds, blendAnimation.Handle, blendAnimTargetTimePointSeconds, interpolationDistance, nodeIndices, modelSpaceTransforms);
	}
	
	/// <summary>
	/// Poses the given object as a blend of this animation and another.
	/// </summary>
	/// <remarks>
	/// Blending is what makes one animation give way to another smoothly — a character easing from a walk in to a run —
	/// rather than snapping between poses.
	/// </remarks>
	/// <param name="targetInstance">The object to pose.</param>
	/// <param name="targetTimePointSeconds">The moment in this animation to evaluate, in seconds.</param>
	/// <param name="blendAnimation">The animation to blend towards.</param>
	/// <param name="blendAnimTargetTimePointSeconds">The moment in <paramref name="blendAnimation"/> to evaluate, in seconds.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely this animation and <c>1f</c> entirely <paramref name="blendAnimation"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyBlended(ModelInstance targetInstance, float targetTimePointSeconds, MeshAnimation blendAnimation, float blendAnimTargetTimePointSeconds, float interpolationDistance) {
		Implementation.ApplyBlended(targetInstance, _handle, targetTimePointSeconds, blendAnimation.Handle, blendAnimTargetTimePointSeconds, interpolationDistance);
	}
	
	/// <summary>
	/// Poses the given object as a blend of this animation and another, and reports where the given node ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="targetInstance">The object to pose.</param>
	/// <param name="targetTimePointSeconds">The moment in this animation to evaluate, in seconds.</param>
	/// <param name="blendAnimation">The animation to blend towards.</param>
	/// <param name="blendAnimTargetTimePointSeconds">The moment in <paramref name="blendAnimation"/> to evaluate, in seconds.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely this animation and <c>1f</c> entirely <paramref name="blendAnimation"/>.</param>
	/// <param name="node">The node whose resulting position is wanted.</param>
	/// <param name="modelSpaceTransform">Set to the node's resulting transform, relative to the model's own origin.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyBlendedAndGetNodeTransforms(ModelInstance targetInstance, float targetTimePointSeconds, MeshAnimation blendAnimation, float blendAnimTargetTimePointSeconds, float interpolationDistance, MeshNode node, out Matrix4x4 modelSpaceTransform) {
		Unsafe.SkipInit(out modelSpaceTransform);
		ApplyBlendedAndGetNodeTransforms(targetInstance, targetTimePointSeconds, blendAnimation, blendAnimTargetTimePointSeconds, interpolationDistance, new ReadOnlySpan<MeshNode>(in node), new Span<Matrix4x4>(ref modelSpaceTransform));
	}
	
	/// <summary>
	/// Poses the given object as a blend of this animation and another, and reports where the given nodes ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="targetInstance">The object to pose.</param>
	/// <param name="targetTimePointSeconds">The moment in this animation to evaluate, in seconds.</param>
	/// <param name="blendAnimation">The animation to blend towards.</param>
	/// <param name="blendAnimTargetTimePointSeconds">The moment in <paramref name="blendAnimation"/> to evaluate, in seconds.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely this animation and <c>1f</c> entirely <paramref name="blendAnimation"/>.</param>
	/// <param name="nodes">The nodes whose resulting positions are wanted.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyBlendedAndGetNodeTransforms(ModelInstance targetInstance, float targetTimePointSeconds, MeshAnimation blendAnimation, float blendAnimTargetTimePointSeconds, float interpolationDistance, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		Implementation.ApplyBlendedAndGetNodeTransforms(targetInstance, _handle, targetTimePointSeconds, blendAnimation.Handle, blendAnimTargetTimePointSeconds, interpolationDistance, nodes, modelSpaceTransforms);
	}
	
	/// <summary>
	/// Poses the given object as a blend of this animation and another, and reports where the nodes at the given indices ended up.
	/// </summary>
	/// <remarks>
	/// The transforms are relative to the model's own origin, so multiplying one by the object's own transform gives a
	/// world-space position — which is how a sword is made to follow a character's hand.
	/// </remarks>
	/// <param name="targetInstance">The object to pose.</param>
	/// <param name="targetTimePointSeconds">The moment in this animation to evaluate, in seconds.</param>
	/// <param name="blendAnimation">The animation to blend towards.</param>
	/// <param name="blendAnimTargetTimePointSeconds">The moment in <paramref name="blendAnimation"/> to evaluate, in seconds.</param>
	/// <param name="interpolationDistance">How far between the two animations to blend, where <c>0f</c> is entirely this animation and <c>1f</c> entirely <paramref name="blendAnimation"/>.</param>
	/// <param name="nodeIndices">The indices of the nodes whose resulting positions are wanted. Indices can be put on the stack where the nodes themselves can not.</param>
	/// <param name="modelSpaceTransforms">Receives each node's resulting transform, relative to the model's own origin, in the same order the nodes were given. Must be at least as long as the node span.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ApplyBlendedAndGetNodeTransforms(ModelInstance targetInstance, float targetTimePointSeconds, MeshAnimation blendAnimation, float blendAnimTargetTimePointSeconds, float interpolationDistance, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		Implementation.ApplyBlendedAndGetNodeTransforms(targetInstance, _handle, targetTimePointSeconds, blendAnimation.Handle, blendAnimTargetTimePointSeconds, interpolationDistance, nodeIndices, modelSpaceTransforms);
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static MeshAnimation IResource<MeshAnimation>.CreateFromHandleAndImpl(ResourceHandle<MeshAnimation> handle, IResourceImplProvider impl) {
		return new MeshAnimation(handle, impl as IMeshAnimationImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<MeshAnimation> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<MeshAnimation> IResource<MeshAnimation>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	#region Disposal
	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion
	
	/// <inheritdoc />
	public override string ToString() => $"Mesh Animation \"{GetNameAsNewStringObject()}\"";

	#region Equality
	/// <inheritdoc />
	public bool Equals(MeshAnimation other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is MeshAnimation other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	/// <summary>
	/// Returns whether the two given animations are the same animation.
	/// </summary>
	/// <param name="left">The first animation to compare.</param>
	/// <param name="right">The second animation to compare.</param>
	public static bool operator ==(MeshAnimation left, MeshAnimation right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given animations are different animations.
	/// </summary>
	/// <param name="left">The first animation to compare.</param>
	/// <param name="right">The second animation to compare.</param>
	public static bool operator !=(MeshAnimation left, MeshAnimation right) => !left.Equals(right);
	#endregion
}