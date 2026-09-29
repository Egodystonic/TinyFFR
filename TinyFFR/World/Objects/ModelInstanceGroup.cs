// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Buffers;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

#pragma warning disable CA1710 // "Rename to ModelInstanceStack/Queue/List etc" -- Compiler is being overly aggressive because this implements IReadOnlyCollection; but in this case 'Group' is better aligned with other names in TinyFFR
/// <summary>
/// A set of <see cref="ModelInstance"/>s that can be moved, rotated and scaled together as though they were one object.
/// </summary>
/// <remarks>
/// Useful wherever several instances make up one conceptual thing, such as a vehicle assembled from separate parts. Transforming the group applies the same change
/// to every instance in it.
/// </remarks>
public readonly struct ModelInstanceGroup : ITransformedSceneObject, IMaterialReceivingSceneObject, IDisposable, IStringSpanNameEnabled, IReadOnlyCollection<ModelInstance>, IEquatable<ModelInstanceGroup> {
#pragma warning restore CA1710
	static SceneObjectType ISceneObject.SceneObjectType { get; } = SceneObjectType.ModelInstanceGroup;

	/// <summary>
	/// The <see cref="ResourceGroup"/> instance backing this instance group, which is what actually owns the contained instances.
	/// </summary>
	public ResourceGroup UnderlyingResourceGroup { get; }
	/// <summary>
	/// Every model instance in this group.
	/// </summary>
	public IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, ModelInstance> Instances { get; }
	/// <summary>
	/// How many model instances are in this group.
	/// </summary>
	public int Count { get; }
	readonly ModelBundleAnimationTable _animTable;
	
	ModelInstance? FirstInstance => Count > 0 ? Instances[0] : null;
	
	/// <summary>
	/// Returns the model instance at the given position in this group.
	/// </summary>
	/// <param name="index">The position of the instance to return. Must be in the range <c>0 &lt;= n &lt; </c><see cref="Count"/>.</param>
	/// <exception cref="IndexOutOfRangeException">Thrown if <paramref name="index"/> is greater than or equal to <see cref="Count"/>.</exception>
	public ModelInstance this[int index] => Instances[index];

	/// <summary>
	/// Constructs a new <see cref="ModelInstanceGroup"/> over an existing resource group.
	/// </summary>
	/// <param name="underlyingResourceGroup">The resource group whose model instances this group should transform together.</param>
	public ModelInstanceGroup(ResourceGroup underlyingResourceGroup) {
		if (!underlyingResourceGroup.IsSealed) throw new ArgumentException("Resource group must be sealed.", nameof(underlyingResourceGroup));
		UnderlyingResourceGroup = underlyingResourceGroup;
		Instances = UnderlyingResourceGroup.ModelInstances;
		Count = Instances.Count;
		_animTable = UnderlyingResourceGroup.AnimationTables.Count > 0 ? UnderlyingResourceGroup.AnimationTables[0] : ModelBundleAnimationTable.Empty;
	}
	
	public ModelBundleAnimationIndex Animations {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _animTable.Animations;
	}
	
	public ModelBundleSkeleton Skeleton {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _animTable.Skeleton;
	}

	/// <inheritdoc />
	public Transform Transform {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FirstInstance?.Transform ?? Transform.None;
		set {
			for (var i = 0; i < Count; ++i) Instances[i].SetTransform(value);
		}
	}
	/// <summary>
	/// Sets <see cref="Transform"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="transform">The new value for <see cref="Transform"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTransform(Transform transform) => Transform = transform;

	/// <inheritdoc />
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FirstInstance?.Position ?? Location.Origin;
		set {
			for (var i = 0; i < Count; ++i) Instances[i].SetPosition(value);
		}
	}
	/// <summary>
	/// Sets <see cref="Position"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="position">The new value for <see cref="Position"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	/// <inheritdoc />
	public Rotation Rotation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FirstInstance?.Rotation ?? Rotation.None;
		set {
			for (var i = 0; i < Count; ++i) Instances[i].SetRotation(value);
		}
	}
	/// <summary>
	/// Sets <see cref="Rotation"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="rotation">The new value for <see cref="Rotation"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotation(Rotation rotation) => Rotation = rotation;

	/// <inheritdoc />
	public Quaternion RotationQuaternion {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FirstInstance?.RotationQuaternion ?? Quaternion.Identity;
		set {
			for (var i = 0; i < Count; ++i) Instances[i].SetRotationQuaternion(value);
		}
	}
	/// <summary>
	/// Sets <see cref="RotationQuaternion"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="rotationQuaternion">The new value for <see cref="RotationQuaternion"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotationQuaternion(Quaternion rotationQuaternion) => RotationQuaternion = rotationQuaternion;

	/// <inheritdoc />
	public Vect Scaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => FirstInstance?.Scaling ?? Vect.One;
		set {
			for (var i = 0; i < Count; ++i) Instances[i].SetScaling(value);
		}
	}
	/// <summary>
	/// Sets <see cref="Scaling"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="scaling">The new value for <see cref="Scaling"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetScaling(Vect scaling) => Scaling = scaling;
	/// <summary>
	/// Sets <see cref="Scaling"/> to the same value on every axis.
	/// </summary>
	/// <param name="uniformScaling">The scaling to apply on all three axes, where <c>1f</c> is the group's unmodified size.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetScaling(float uniformScaling) => Scaling = new Vect(uniformScaling);

	/// <inheritdoc />
	public void MoveBy(Vect translation) {
		for (var i = 0; i < Count; ++i) Instances[i].MoveBy(translation);
	}
	/// <inheritdoc />
	public void RotateBy(Rotation rotation) {
		for (var i = 0; i < Count; ++i) Instances[i].RotateBy(rotation);
	}
	/// <inheritdoc />
	public void RotateBy(Rotation rotation, Location pivotPoint) {
		for (var i = 0; i < Count; ++i) Instances[i].RotateBy(rotation, pivotPoint);
	}
	/// <inheritdoc />
	public void RotateBy(Quaternion rotationQuaternion) {
		for (var i = 0; i < Count; ++i) Instances[i].RotateBy(rotationQuaternion);
	}
	/// <inheritdoc />
	public void RotateBy(Quaternion rotationQuaternion, Location pivotPoint) {
		for (var i = 0; i < Count; ++i) Instances[i].RotateBy(rotationQuaternion, pivotPoint);
	}
	/// <inheritdoc />
	public void ScaleBy(float scalar) {
		for (var i = 0; i < Count; ++i) Instances[i].ScaleBy(scalar);
	}
	/// <inheritdoc />
	public void ScaleBy(Vect vect) {
		for (var i = 0; i < Count; ++i) Instances[i].ScaleBy(vect);
	}
	/// <inheritdoc />
	public void AdjustScaleBy(float scalar) {
		for (var i = 0; i < Count; ++i) Instances[i].AdjustScaleBy(scalar);
	}
	/// <inheritdoc />
	public void AdjustScaleBy(Vect vect) {
		for (var i = 0; i < Count; ++i) Instances[i].AdjustScaleBy(vect);
	}

	/// <inheritdoc />
	public void SetMaterial(Material material) {
		for (var i = 0; i < Count; ++i) Instances[i].SetMaterial(material);
	}
	/// <inheritdoc />
	public void SetDefaultMaterialBaseColor(ColorVect baseColor) {
		for (var i = 0; i < Count; ++i) Instances[i].SetDefaultMaterialBaseColor(baseColor);
	}
	/// <inheritdoc />
	public void SetDefaultMaterialShadingStyle(DefaultMaterialShadingStyle style) {
		for (var i = 0; i < Count; ++i) Instances[i].SetDefaultMaterialShadingStyle(style);
	}
	
	/// <summary>
	/// Returns a player that runs the given animation on this instance at its natural speed.
	/// </summary>
	/// <remarks>
	/// It's okay to 'create' many <see cref="MeshAnimationPlayer"/>s per instance per frame (they are lightweight and do not generate GC pressure).
	/// </remarks>
	/// <param name="animation">The animation to play.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshAnimationPlayer GetAnimationPlayer(MeshAnimation animation) => new(this, animation);
	
	/// <summary>
	/// Returns a player that runs the given animation on this instance at a multiple of its natural speed.
	/// </summary>
	/// <remarks>
	/// It's okay to 'create' many <see cref="MeshAnimationPlayer"/>s per instance per frame (they are lightweight and do not generate GC pressure).
	/// </remarks>
	/// <param name="animation">The animation to play.</param>
	/// <param name="animationSpeedMultiplier">How much faster than natural to play, where <c>1f</c> is natural speed and <c>2f</c> is twice as fast.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshAnimationPlayer GetAnimationPlayerWithSpeedMultiplier(MeshAnimation animation, float animationSpeedMultiplier) => MeshAnimationPlayer.CreateWithSpeedMultiplier(this, animation, animationSpeedMultiplier);
	
	/// <summary>
	/// Returns a player that runs the given animation on this instance stretched or compressed to a chosen duration.
	/// </summary>
	/// <remarks>
	/// It's okay to 'create' many <see cref="MeshAnimationPlayer"/>s per instance per frame (they are lightweight and do not generate GC pressure).
	/// </remarks>
	/// <param name="animation">The animation to play.</param>
	/// <param name="animationDurationSeconds">How long one cycle of the animation should take, in seconds.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshAnimationPlayer GetAnimationPlayerWithTargetDuration(MeshAnimation animation, float animationDurationSeconds) => MeshAnimationPlayer.CreateWithTargetDuration(this, animation, animationDurationSeconds);
	
	/// <summary>
	/// Returns a player that blends between two animations on this instance, so that one can be cross-faded in to the other.
	/// </summary>
	/// <remarks>
	/// It's okay to 'create' many <see cref="MeshBlendedAnimationPlayer"/>s per instance per frame (they are lightweight and do not generate GC pressure).
	/// </remarks>
	/// <param name="startAnimation">The animation blended towards at one end of the range.</param>
	/// <param name="endAnimation">The animation blended towards at the other end of the range.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshBlendedAnimationPlayer GetAnimationPlayer(MeshAnimation startAnimation, MeshAnimation endAnimation) => new(this, startAnimation, endAnimation);
	
	/// <summary>
	/// Returns a player that blends between two animations on this instance, each running at a multiple of its natural speed.
	/// </summary>
	/// <remarks>
	/// It's okay to 'create' many <see cref="MeshBlendedAnimationPlayer"/>s per instance per frame (they are lightweight and do not generate GC pressure).
	/// </remarks>
	/// <param name="startAnimation">The animation blended towards at one end of the range.</param>
	/// <param name="startAnimationSpeedMultiplier">How much faster than natural to play <paramref name="startAnimation"/>.</param>
	/// <param name="endAnimation">The animation blended towards at the other end of the range.</param>
	/// <param name="endAnimationSpeedMultiplier">How much faster than natural to play <paramref name="endAnimation"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshBlendedAnimationPlayer GetAnimationPlayerWithSpeedMultiplier(MeshAnimation startAnimation, float startAnimationSpeedMultiplier, MeshAnimation endAnimation, float endAnimationSpeedMultiplier) => MeshBlendedAnimationPlayer.CreateWithSpeedMultiplier(this, startAnimation, endAnimation, startAnimationSpeedMultiplier, endAnimationSpeedMultiplier);
	
	/// <summary>
	/// Returns a player that blends between two animations on this instance, each stretched or compressed to a chosen duration.
	/// </summary>
	/// <remarks>
	/// It's okay to 'create' many <see cref="MeshBlendedAnimationPlayer"/>s per instance per frame (they are lightweight and do not generate GC pressure).
	/// </remarks>
	/// <param name="startAnimation">The animation blended towards at one end of the range.</param>
	/// <param name="startAnimationDurationSeconds">How long one cycle of <paramref name="startAnimation"/> should take, in seconds.</param>
	/// <param name="endAnimation">The animation blended towards at the other end of the range.</param>
	/// <param name="endAnimationDurationSeconds">How long one cycle of <paramref name="endAnimation"/> should take, in seconds.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MeshBlendedAnimationPlayer GetAnimationPlayerWithTargetDuration(MeshAnimation startAnimation, float startAnimationDurationSeconds, MeshAnimation endAnimation, float endAnimationDurationSeconds) => MeshBlendedAnimationPlayer.CreateWithTargetDuration(this, startAnimation, endAnimation, endAnimationDurationSeconds, endAnimationDurationSeconds);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingResourceGroup.GetNameAsNewStringObject();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingResourceGroup.GetNameLength();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingResourceGroup.CopyName(destinationBuffer);

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	IEnumerator<ModelInstance> IEnumerable<ModelInstance>.GetEnumerator() => GetEnumerator();
	/// <summary>
	/// Returns an enumerator over the model instances in this group.
	/// </summary>
	public IndirectEnumerable<IResourceGroupImplProvider.EnumerationInput, ModelInstance>.Enumerator GetEnumerator() => Instances.GetEnumerator();

	#region Disposal
	/// <inheritdoc />
	public void Dispose() => UnderlyingResourceGroup.Dispose();
	/// <summary>
	/// Disposes this group, optionally disposing the model instances in it as well.
	/// </summary>
	/// <param name="disposeContainedInstances">If <see langword="true"/>, every instance in this group is disposed too; if <see langword="false"/>, only the grouping itself is discarded and the instances remain usable.</param>
	public void Dispose(bool disposeContainedInstances) => UnderlyingResourceGroup.Dispose(disposeContainedInstances);
	#endregion

	/// <inheritdoc />
	public override string ToString() => $"Model Instance " + UnderlyingResourceGroup;

	#region Equality
	/// <inheritdoc />
	public bool Equals(ModelInstanceGroup other) => UnderlyingResourceGroup.Equals(other.UnderlyingResourceGroup);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is ModelInstanceGroup other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => UnderlyingResourceGroup.GetHashCode();
	/// <summary>
	/// <see cref="Equals(ModelInstanceGroup)"/>
	/// </summary>
	public static bool operator ==(ModelInstanceGroup left, ModelInstanceGroup right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(ModelInstanceGroup)"/>
	/// </summary>
	public static bool operator !=(ModelInstanceGroup left, ModelInstanceGroup right) => !left.Equals(right);
	#endregion
}