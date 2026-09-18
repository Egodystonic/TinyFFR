// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Adjusts the per-instance material effects of a single <see cref="ModelInstance"/>.
/// </summary>
/// <remarks>
/// <para>
/// Per-instance effects let one object's appearance be altered without affecting anything else that shares its material.
/// Obtained from <see cref="ModelInstance.MaterialEffects"/>, which returns <see langword="null"/> when the object's material does
/// not support effects.
/// </para>
/// <para>
/// Only materials created with effects enabled expose a controller at all, and only the map types the material was actually
/// created with respond to being set; asking to blend a map the material does not have does nothing.
/// </para>
/// </remarks>
public readonly record struct MaterialEffectController {
	readonly ModelInstance _attachedModelInstance;

	/// <summary>
	/// Constructs a new <see cref="MaterialEffectController"/> for the given model instance.
	/// </summary>
	/// <remarks>
	/// Usually obtained from <see cref="ModelInstance.MaterialEffects"/> rather than constructed directly, because that checks first whether the instance's material
	/// actually supports effects.
	/// </remarks>
	/// <param name="attachedModelInstance">The model instance whose effects this controller adjusts.</param>
	public MaterialEffectController(ModelInstance attachedModelInstance) => _attachedModelInstance = attachedModelInstance;

	/// <summary>
	/// Sets how the effect's textures are positioned, rotated and scaled across the object's surface.
	/// </summary>
	/// <remarks>
	/// Animating this is how an effect is made to move over a surface, such as scrolling water or drifting cloud shadows.
	/// </remarks>
	/// <param name="newTransform">The transform to apply to the effect's textures.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTransform(Transform2D newTransform) {
		_attachedModelInstance.SetMaterialEffectTransform(newTransform);
	}

	/// <summary>
	/// Sets the texture blended over this object for one of the effect's map types.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Texture blending allows you to linearly blend between the material's default texture for the given <paramref name="mapType"/>
	/// and the given <paramref name="texture"/>.
	/// Adjust the distance between the start and end texture using <see cref="SetBlendDistance"/>.
	/// </para>
	/// <para>
	/// Only the map types the material was actually
	/// created with respond to being set; asking to blend a map the material does not have does nothing.
	/// </para>
	/// </remarks>
	/// <param name="mapType">Which aspect of the surface this texture affects.</param>
	/// <param name="texture">The texture to blend over the object.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBlendTexture(MaterialEffectMapType mapType, Texture texture) {
		_attachedModelInstance.SetEffectBlendTexture(mapType, texture);
	}

	/// <summary>
	/// Sets the linear interpolation distance between this material's default texture for the given <paramref name="mapType"/> and
	/// the destination texture previously set via <see cref="SetBlendTexture"/>.
	/// </summary>
	/// <remarks>
	/// Texture blending allows you to linearly blend between the material's default texture for the given <paramref name="mapType"/>
	/// and a texture previously set via <see cref="SetBlendTexture"/>.
	/// If no destination texture has already been set, this method has no effect.
	/// </remarks>
	/// <param name="mapType">Which aspect of the surface this applies to.</param>
	/// <param name="distance">How far to blend towards the effect texture, where <c>0f</c> leaves the object's own material untouched and <c>1f</c> replaces it entirely.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBlendDistance(MaterialEffectMapType mapType, float distance) {
		_attachedModelInstance.SetEffectBlendDistance(mapType, distance);
	}
}

/// <summary>
/// How an object painted with the built-in <see cref="IMaterialBuilder.DefaultMaterial"/> is drawn.
/// </summary>
/// <remarks>
/// This applies only to objects using the built-in default material. Objects opt in to using the default material either by supplying
/// <c>null</c> as their material parameter in the <see cref="IObjectBuilder"/>, or by invoking <see cref="IMaterialUsingSceneObject.SetDefaultMaterialShadingStyle">SetDefaultMaterialShadingStyle()</see>
/// of <see cref="IMaterialUsingSceneObject.SetDefaultMaterialBaseColor">SetDefaultMaterialBaseColor()</see>.
/// </remarks>
#pragma warning disable CA1027 // This isn't a bitfield enum
public enum DefaultMaterialShadingStyle {
#pragma warning restore CA1027
	/// <summary>
	/// Creates a 3D effect by making all surfaces of the object appear to be lit from a pseudo-light emitted from the camera;
	/// causing surfaces viewed at oblique angles to appear slightly darker than ones those viewed head-on.
	/// </summary>
	Plain3D = LocalShaderPackageConstants.DefaultMaterialShaderConstants.ShadingModeVariant.Plain3DOpaque,
	/// <summary>
	/// The object is drawn in a flat, uniform colour that ignores lighting or viewing angle entirely.
	/// </summary>
	/// <remarks>
	/// Because the shape is not picked out by even pseudo-lighting, this tends to read as a silhouette; it is most useful for markers, highlights and other elements that
	/// should stay legible regardless of how the scene is lit.
	/// </remarks>
	Plain = LocalShaderPackageConstants.DefaultMaterialShaderConstants.ShadingModeVariant.PlainOpaque,
	/// <summary>
	/// Only the edges of the object's triangles are drawn, leaving its faces empty.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Chiefly a diagnostic view: it shows how an object is actually constructed, which makes it useful for checking geometry that looks wrong when shaded normally.
	/// </para>
	/// <para>
	/// Note that the target <see cref="Mesh"/> must have been created with <see cref="Mesh.SupportsWireframeRendering"/> being <c>true</c>.
	/// </para>
	/// </remarks>
	Wireframe = LocalShaderPackageConstants.DefaultMaterialShaderConstants.ShadingModeVariant.Wireframe
}

/// <summary>
/// A single occurrence of a model in a scene: a <see cref="Assets.Meshes.Mesh"/> giving its shape, a <see cref="Assets.Materials.Material"/> giving its surface,
/// and a transform (<see cref="Position"/>/<see cref="Scaling"/>/<see cref="Rotation"/> and/or <see cref="Transform"/>) placing it in the world.
/// </summary>
/// <remarks>
/// The mesh and material are shared resources, whilst the instance is cheap; this is what lets a scene contain a thousand copies of the same tree without holding a
/// thousand copies of its geometry. An instance must be added to a <see cref="Scene"/> before it is rendered.
/// </remarks>
public readonly struct ModelInstance : IDisposableResource<ModelInstance, IModelInstanceImplProvider>, ITransformedSceneObject, IMaterialUsingSceneObject {
	readonly ResourceHandle<ModelInstance> _handle;
	readonly IModelInstanceImplProvider _impl;

	internal IModelInstanceImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<ModelInstance>();
	internal ResourceHandle<ModelInstance> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(ModelInstance)) : _handle;

	IModelInstanceImplProvider IResource<ModelInstance, IModelInstanceImplProvider>.Implementation => Implementation;
	ResourceHandle<ModelInstance> IResource<ModelInstance>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	/// <inheritdoc />
	public Transform Transform {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetTransform(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetTransform(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="Transform"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="transform">The new value for <see cref="Transform"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTransform(Transform transform) => Implementation.SetTransform(_handle, transform);

	/// <summary>
	/// Records a new <see cref="Transform"/> for this instance without moving what is actually drawn.
	/// Under most circumstances you should not use this API.
	/// </summary>
	/// <remarks>
	/// Paired with <see cref="SetWorldMatrixWithoutUpdatingTransform"/> to let the reported transform and the rendered placement deliberately diverge. This is a
	/// specialist tool (it is what camera-locked objects use to report a sensible size whilst being drawn at a screen-derived one) and is easy to misuse.
	/// </remarks>
	/// <param name="newTransform">The transform to record.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTransformWithoutUpdatingWorldMatrix(in Transform newTransform) => Implementation.SetTransformWithoutUpdatingWorldMatrix(_handle, newTransform);
	/// <summary>
	/// Sets where this instance is actually drawn without changing the <see cref="Transform"/> it reports.
	/// Under most circumstances you should not use this API.
	/// </summary>
	/// <remarks>
	/// Paired with <see cref="SetTransformWithoutUpdatingWorldMatrix"/> to let the reported transform and the rendered placement deliberately diverge. This is a
	/// specialist tool (it is what camera-locked objects use to report a sensible size whilst being drawn at a screen-derived one) and is easy to misuse.
	/// </remarks>
	/// <param name="worldMatrix">The matrix to draw this instance with.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetWorldMatrixWithoutUpdatingTransform(in Matrix4x4 worldMatrix) => Implementation.SetWorldMatrixWithoutUpdatingTransform(_handle, worldMatrix);

	/// <inheritdoc />
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetPosition(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetPosition(_handle, value);
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
		get => Implementation.GetRotation(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetRotation(_handle, value);
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
		get => Implementation.GetRotationQuaternion(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetRotationQuaternion(_handle, value);
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
		get => Implementation.GetScaling(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetScaling(_handle, value);
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
	/// <param name="uniformScaling">The scaling to apply on all three axes, where <c>1f</c> is the object's unmodified size.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetScaling(float uniformScaling) => Scaling = new Vect(uniformScaling);

	/// <inheritdoc />
	public Material Material {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetMaterial(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetMaterial(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="Material"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="material">The new value for <see cref="Material"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetMaterial(Material material) => Material = material;

	/// <inheritdoc />
	public MaterialEffectController? MaterialEffects {
		get {
			if (!Material.SupportsPerInstanceEffects) return null;
			return new MaterialEffectController(this);
		}
	}
	
	/// <summary>
	/// Returns the index (library) of animations available on this instance's <see cref="Mesh"/>.
	/// </summary>
	public MeshAnimationIndex Animations {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Mesh.Animations;
	}
	
	/// <summary>
	/// The skeleton of this instance's <see cref="Mesh"/>; i.e. the hierarchy of bones its animations move.
	/// </summary>
	public MeshSkeleton Skeleton {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Mesh.Skeleton;
	}

	/// <summary>
	/// The mesh giving this instance its shape.
	/// </summary>
	/// <remarks>
	/// Several instances commonly share one mesh; changing this swaps which shape this instance draws without affecting any other instance.
	/// </remarks>
	public Mesh Mesh {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetMesh(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetMesh(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="Mesh"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="mesh">The new value for <see cref="Mesh"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetMesh(Mesh mesh) => Mesh = mesh;

	/// <summary>
	/// Whether this instance's vertices may be altered individually, without affecting other instances sharing the same mesh.
	/// <see cref="BorrowVerticesSpan(bool)"/> can not be used unless this returns <c>true</c>.
	/// </summary>
	/// <remarks>
	/// This is a property of the underlying mesh, decided when it was created..
	/// </remarks>
	public bool AllowsVertexMutation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Mesh.AllowsPerInstanceVertexMutation;
	}

	internal ModelInstance(ResourceHandle<ModelInstance> handle, IModelInstanceImplProvider impl) {
		_handle = handle;
		_impl = impl;
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
	
	/// <summary>
	/// Borrows this instance's vertices for direct modification, so that its shape can be altered without affecting other instances sharing the same mesh.
	/// </summary>
	/// <remarks>
	/// Only usable when <see cref="AllowsVertexMutation"/> is <see langword="true"/>. Dispose the returned lease as soon as you are done with it; the vertices are
	/// not uploaded for rendering until you do. Moving vertices changes the shape but not the cached bounds, which is why the bounds should generally be
	/// recalculated afterwards. A stale bounding box makes an object vanish when it should be on screen, and makes scene queries miss it.
	/// </remarks>
	/// <param name="recalculateBoundingBoxOnLeaseDispose">Whether to recalculate this instance's bounding box when the lease is disposed.
	/// Pass <see langword="false"/> only if you intend to set the bounds yourself (with <see cref="TriggerManualBoundingBoxRecalculation"/> or <see cref="SetModelSpaceBoundingBox"/>)
	/// or pre-calculated a bounding box at mesh-authoring time that you know encompasses all possible permutations of the mutated vertices.</param>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="AllowsVertexMutation"/> is <c>false</c>.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ScopedSpanLease<MeshVertex> BorrowVerticesSpan(bool recalculateBoundingBoxOnLeaseDispose) => BorrowVerticesSpan(recalculateBoundingBoxOnLeaseDispose, Range.All);
	
	/// <summary>
	/// Borrows part of this instance's vertices for direct modification; as <see cref="BorrowVerticesSpan(bool)"/>, but restricted to a range of the vertex list,
	/// which avoids uploading the whole mesh again when only a few vertices change.
	/// </summary>
	/// <remarks>
	/// Only usable when <see cref="AllowsVertexMutation"/> is <see langword="true"/>. Dispose the returned lease as soon as you are done with it; the vertices are
	/// not uploaded for rendering until you do. Moving vertices changes the shape but not the cached bounds, which is why the bounds should generally be
	/// recalculated afterwards. A stale bounding box makes an object vanish when it should be on screen, and makes scene queries miss it.
	/// </remarks>
	/// <param name="recalculateBoundingBoxOnLeaseDispose">Whether to recalculate this instance's bounding box when the lease is disposed.
	/// Pass <see langword="false"/> only if you intend to set the bounds yourself (with <see cref="TriggerManualBoundingBoxRecalculation"/> or <see cref="SetModelSpaceBoundingBox"/>)
	/// or pre-calculated a bounding box at mesh-authoring time that you know encompasses all possible permutations of the mutated vertices.</param>
	/// <param name="range">Which vertices to borrow.</param>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="AllowsVertexMutation"/> is <c>false</c>.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ScopedSpanLease<MeshVertex> BorrowVerticesSpan(bool recalculateBoundingBoxOnLeaseDispose, Range range) => Implementation.BorrowVerticesSpan(_handle, range, recalculateBoundingBoxOnLeaseDispose);
	
	/// <summary>
	/// Borrows this instance's vertices for inspection without modifying them.
	/// </summary>
	/// <remarks>
	/// Only usable when <see cref="AllowsVertexMutation"/> is <see langword="true"/>. Dispose the returned lease as soon as you are done with it.
	/// </remarks>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="AllowsVertexMutation"/> is <c>false</c>.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ScopedReadOnlySpanLease<MeshVertex> BorrowVerticesSpanReadOnly() => Implementation.BorrowVerticesSpanReadOnly(_handle);

	/// <summary>
	/// Recalculates this instance's bounding box from its current vertices.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is a reasonably expensive operation (depending on how many vertices the <see cref="Mesh"/> has).
	/// </para>
	/// <para>
	/// Generally only needed after mutating vertices (i.e. with <see cref="BorrowVerticesSpan(bool)"/> or <see cref="BorrowVerticesSpan(bool, Range)"/>) with
	/// <c>recalculateBoundingBoxOnLeaseDispose</c> set to <see langword="false"/>.
	/// </para>
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void TriggerManualBoundingBoxRecalculation() => Implementation.TriggerManualBoundingBoxRecalculation(_handle);

	/// <summary>
	/// Overrides the <see cref="Mesh"/> bounding box for this model instance only.
	/// </summary>
	/// <param name="newBoundingBox">The bounding box to use, in the mesh's own coordinate space.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetModelSpaceBoundingBox(PositionedCuboid newBoundingBox) => Implementation.SetModelSpaceBoundingBox(_handle, newBoundingBox);

	/// <summary>
	/// Returns this instance's bounding box in the typical space they are usually authored.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PositionedCuboid GetModelSpaceBoundingBox() => Implementation.GetModelSpaceBoundingBox(_handle);
	
	/// <summary>
	/// Returns this instance's bounding box in world space, including its rotation. This tells you exactly the bounding box
	/// of this model instance as it is actually tested against in the world according to the current <see cref="Transform"/>.
	/// </summary>
	/// <remarks>
	/// This is the tightest of the world-space bounds, because it turns with the object rather than having to enclose every orientation it might take.
	/// </remarks>
	public PositionedRotatedCuboid GetWorldSpaceBoundingBox() {
		var nonTransformed = GetModelSpaceBoundingBox();
		var transform = Transform;
		var scaledNonTransformedTranslation = nonTransformed.Position.AsVect() * transform.Scaling;
		return new PositionedRotatedCuboid(
			nonTransformed.ToStandardCuboid().ScaledBy(transform.Scaling),
			transform.Translation.AsLocation() + scaledNonTransformedTranslation * transform.Rotation,
			transform.Rotation
		);
	}
	
	/// <summary>
	/// Returns a sphere in world space that encloses this instance.
	/// </summary>
	/// <remarks>
	/// The cheapest of the bounds to test against, which is why it is used as the first, coarse pass of a scene query.
	/// </remarks>
	public PositionedSphere GetWorldSpaceBoundingSphere() {
		var transform = Transform;
		var modelSpaceSphere = Mesh.BoundingSphere;
		return new PositionedSphere(
			modelSpaceSphere.Radius * transform.Scaling.MaxComponentMagnitude,
			transform.Translation.AsLocation() + modelSpaceSphere.Position.AsVect() * transform.Scaling * transform.Rotation
		);
	}
	
	/// <summary>
	/// Returns an axis-aligned box in world space that encloses this instance.
	/// </summary>
	/// <remarks>
	/// Because this box does not turn with the object, it must be large enough to contain it at its current orientation, and is therefore looser than
	/// <see cref="GetWorldSpaceBoundingBox()"/>.
	/// </remarks>
	public PositionedCuboid GetWorldSpaceAxisAlignedBoundingBox() {
		var transform = Transform;
		var modelSpaceBox = Mesh.AxisAlignedBoundingBox;
		return new PositionedCuboid(
			modelSpaceBox.ToStandardCuboid().ScaledBy(transform.Scaling.MaxComponentMagnitude),
			transform.Translation.AsLocation() + modelSpaceBox.Position.AsVect() * transform.Scaling * transform.Rotation
		);
	}

	/// <summary>
	/// Returns a sphere in world space that encloses this instance, optionally computing the tightest possible fit.
	/// </summary>
	/// <param name="calculateSmallestFitFromLiveBoundingBox">If <see langword="true"/>, derives the sphere from the instance's current bounds rather than a cached approximation,
	/// which gives a more accurate result and is required if your instance's bounding box has diverged from that of its underlying <see cref="Mesh"/>.</param>
	public PositionedSphere GetWorldSpaceBoundingSphere(bool calculateSmallestFitFromLiveBoundingBox) {
		return calculateSmallestFitFromLiveBoundingBox ? GetWorldSpaceBoundingBox().SmallestEnclosingSphere : GetWorldSpaceBoundingSphere();
	}

	/// <summary>
	/// Returns an axis-aligned box in world space that encloses this instance, optionally computing the tightest possible fit.
	/// </summary>
	/// <param name="calculateSmallestFitFromLiveBoundingBox">If <see langword="true"/>, derives the box from the instance's current bounds rather than a cached approximation,
	/// which gives a more accurate result and is required if your instance's bounding box has diverged from that of its underlying <see cref="Mesh"/>.</param>
	public PositionedCuboid GetWorldSpaceAxisAlignedBoundingBox(bool calculateSmallestFitFromLiveBoundingBox) {
		return calculateSmallestFitFromLiveBoundingBox ? GetWorldSpaceBoundingBox().SmallestEnclosingNonRotatedCuboid : GetWorldSpaceAxisAlignedBoundingBox();
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

	static ModelInstance IResource<ModelInstance>.CreateFromHandleAndImpl(ResourceHandle<ModelInstance> handle, IResourceImplProvider impl) {
		return new ModelInstance(handle, impl as IModelInstanceImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<ModelInstance> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<ModelInstance> IResource<ModelInstance>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	/// <inheritdoc />
	public void MoveBy(Vect translation) => Implementation.TranslateBy(_handle, translation);
	/// <inheritdoc />
	public void RotateBy(Rotation rotation) => Implementation.RotateBy(_handle, rotation);
	/// <inheritdoc />
	public void RotateBy(Rotation rotation, Location pivotPoint) => Implementation.RotateBy(_handle, rotation, pivotPoint);
	/// <inheritdoc />
	public void RotateBy(Quaternion rotationQuaternion) => Implementation.RotateBy(_handle, rotationQuaternion);
	/// <inheritdoc />
	public void RotateBy(Quaternion rotationQuaternion, Location pivotPoint) => Implementation.RotateBy(_handle, rotationQuaternion, pivotPoint);
	/// <inheritdoc />
	public void ScaleBy(float scalar) => Implementation.ScaleBy(_handle, scalar);
	/// <inheritdoc />
	public void ScaleBy(Vect vect) => Implementation.ScaleBy(_handle, vect);
	/// <inheritdoc />
	public void AdjustScaleBy(float scalar) => Implementation.AdjustScaleBy(_handle, scalar);
	/// <inheritdoc />
	public void AdjustScaleBy(Vect vect) => Implementation.AdjustScaleBy(_handle, vect);
	
	/// <inheritdoc />
	public void SetDefaultMaterialBaseColor(ColorVect baseColor) => Implementation.SetDefaultMaterialBaseColor(_handle, baseColor);
	/// <inheritdoc />
	public void SetDefaultMaterialShadingStyle(DefaultMaterialShadingStyle style) => Implementation.SetDefaultMaterialShadingStyle(_handle, style);
	/// <summary>
	/// Sets one of the colours used when this object is painted with a colour-keyed material.
	/// </summary>
	/// <remarks>
	/// A colour-keyed material uses the red, green and blue channels of its texture as masks rather than as colours, letting one texture be recoloured per
	/// instance. This is the usual way to give a team or faction its own livery without authoring a texture for each.
	/// </remarks>
	/// <param name="key">Which of the texture's channels to recolour.</param>
	/// <param name="color">The colour to use for that channel.</param>
	public void SetKeyedMaterialColor(ColorChannel key, ColorVect color) => Implementation.SetKeyedMaterialColor(_handle, key, color);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetMaterialEffectTransform(Transform2D newTransform) => Implementation.SetMaterialEffectTransform(_handle, newTransform);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetEffectBlendTexture(MaterialEffectMapType mapType, Texture texture) => Implementation.SetMaterialEffectBlendTexture(_handle, mapType, texture);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetEffectBlendDistance(MaterialEffectMapType mapType, float distance) => Implementation.SetMaterialEffectBlendDistance(_handle, mapType, distance);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetEffectOpacity(float opacity) => Implementation.SetMaterialEffectOpacity(_handle, opacity);

	/// <summary>
	/// The largest permitted <see cref="DrawOrderDeferralAmount"/>: <c>32,767</c>.
	/// </summary>
	public const int MaxDrawOrderDeferralAmount = 32767;

	/// <summary>
	/// How far this instance is pushed back in the drawing order, or <see langword="null"/> to leave it in its natural place.
	/// Must be between <c>0</c> and <see cref="MaxDrawOrderDeferralAmount"/> inclusive.
	/// </summary>
	/// <remarks>
	/// Objects are normally drawn in an order the renderer chooses. Deferring an instance forces it to be drawn after others, which is how you resolve cases where
	/// two surfaces occupy the same space and the wrong one wins, or where a transparent object needs to be composited over something specific.
	/// </remarks>
	public int? DrawOrderDeferralAmount {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetDrawOrderDeferralAmount(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetDrawOrderDeferralAmount(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="DrawOrderDeferralAmount"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="newValue">The new value for <see cref="DrawOrderDeferralAmount"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetDrawOrderDeferralAmount(int? newValue) => DrawOrderDeferralAmount = newValue;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetScissorRect(XYPair<int> viewportRelativeBottomLeftOffset, XYPair<int> dimensions) => Implementation.SetScissorRect(_handle, viewportRelativeBottomLeftOffset, dimensions);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void ClearScissorRect() => Implementation.ClearScissorRect(_handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Material GetOrCreatePrivateMaterial() => Implementation.GetOrCreatePrivateMaterial(_handle);

	#region Disposal
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	/// <inheritdoc />
	public override string ToString() => $"Model Instance {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	/// <inheritdoc />
	public bool Equals(ModelInstance other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is ModelInstance other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	/// <summary>
	/// <see cref="Equals(ModelInstance)"/>
	/// </summary>
	public static bool operator ==(ModelInstance left, ModelInstance right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(ModelInstance)"/>
	/// </summary>
	public static bool operator !=(ModelInstance left, ModelInstance right) => !left.Equals(right);
	#endregion
}