// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// A mesh that is a single flat rectangle, presented as its own type so that it can be used without restating that it is flat. Created via the factory's <see cref="IMeshBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// Quads are what billboards, sprites, labels and canvas panels are drawn on. The underlying mesh is a one-by-one square centred
/// on its own origin, which is then scaled and rotated in to place, so a quad's size is expressed as its scaling rather than
/// baked in to its geometry.
/// </para>
/// <para>
/// This wraps an ordinary <see cref="Mesh"/> and can be used anywhere one is expected.
/// </para>
/// <para>
/// This is a resource in its own right, but it shares its identity with its <see cref="UnderlyingMesh"/>: it has the same name and the same
/// dependencies, and while it is held in a <see cref="ResourceGroup"/> its underlying <see cref="Mesh"/> can not be disposed. A group only lists
/// it under the type it was added as (i.e. <see cref="ResourceGroup.QuadMeshes"/> rather than <see cref="ResourceGroup.Meshes"/>).
/// </para>
/// </remarks>
public readonly struct QuadMesh : IDisposableResource<QuadMesh> {
	/// <summary>
	/// The general-purpose mesh this quad is a specialized view of.
	/// </summary>
	public Mesh UnderlyingMesh { get; } 

	internal QuadMesh(Mesh underlyingMesh) {
		UnderlyingMesh = underlyingMesh;
	}
	

	/// <summary>
	/// Wraps an existing mesh as a <see cref="QuadMesh"/>, without creating anything new.
	/// </summary>
	/// <remarks>
	/// Only use this for a mesh that really was built as a standard unit quad; nothing here verifies that it was.
	/// </remarks>
	/// <param name="underlyingMesh">The mesh to wrap.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static QuadMesh FromPreviouslyAllocatedUnderlyingMesh(Mesh underlyingMesh) => new(underlyingMesh);


	static QuadMesh WrapBase(Mesh b) => new(b);
	ResourceHandle<QuadMesh> IResource<QuadMesh>.Handle => UnderlyingMesh.Handle.AsInteger;
	ResourceHandle IResource.Handle => UnderlyingMesh.Handle;
	IResourceImplProvider IResource.Implementation => UnderlyingMesh.Implementation;
	ResourceIdent IResource.Ident => UnderlyingMesh.Handle.Ident;
	ResourceStub IResource.AsStub => new(UnderlyingMesh.Handle.Ident, UnderlyingMesh.Implementation);
	ResourceHandle<QuadMesh> IResource<QuadMesh>.GetHandleWithoutDisposeCheck() => UnderlyingMesh.GetHandleWithoutDisposeCheck().AsInteger;
	static QuadMesh IResource<QuadMesh>.CreateFromHandleAndImpl(ResourceHandle<QuadMesh> handle, IResourceImplProvider impl) => WrapBase(ResourceUtils.FastFromStub<Mesh>(new ResourceStub(new ResourceIdent(ResourceHandle<Mesh>.TypeHandle, handle.AsInteger), impl)));
	static QuadMesh IResource<QuadMesh>.CreateFromStub(ResourceStub stub) => WrapBase(ResourceUtils.FromStub<Mesh>(stub));
	static QuadMesh IResource<QuadMesh>.FastCreateFromStub(ResourceStub stub) => WrapBase(ResourceUtils.FastFromStub<Mesh>(stub));

	/// <summary>
	/// Calculates the transform that places, orients and sizes a standard quad mesh as described.
	/// </summary>
	/// <remarks>
	/// A standard quad mesh is a one-by-one square centred on its own origin, so it must be scaled and rotated in to place
	/// rather than being built at the size and angle you want. This does that arithmetic for you.
	/// </remarks>
	/// <param name="position">Where to put the quad.</param>
	/// <param name="size">How large the quad should be, in world units (metres).</param>
	/// <param name="facingDirection">Which way the quad's front face should point.</param>
	/// <param name="uprightDirection">Which way is "up" across the quad's face, or <see langword="null"/> to derive one from <paramref name="facingDirection"/>. Must not be parallel to <paramref name="facingDirection"/>.</param>
	/// <param name="positionAnchor">Which point of the quad is placed at <paramref name="position"/> (or the centre if <see cref="Orientation2D.None"/>).</param>
	public static Transform CalculateTransformForStandardQuadMesh(Location position, XYPair<float> size, Direction facingDirection, Direction? uprightDirection = null, Orientation2D positionAnchor = Orientation2D.None) {
		// Quad meshes are built as 1x1 squares centred on their origin on the XY plane facing backward with up being the upright direction by the IMeshBuilder default implementation
		var rotation = Rotation.FromStartAndEndOrientation(Direction.Backward, Direction.Up, facingDirection, uprightDirection ?? Direction.Up);
		return new Transform(
			translation: (CalculateAnchorOffsetForStandardQuadMesh(size, positionAnchor) * rotation) + position.AsVect(),
			rotation: rotation,
			scaling: new Vect(size.X, size.Y, 1f)
		);
	}

	/// <summary>
	/// Calculates how far a standard quad mesh must be shifted so that the given point of it, rather than its centre, sits at its position.
	/// </summary>
	/// <remarks>
	/// The offset is in the quad's own unrotated frame, so it must be rotated along with the quad before being applied.
	/// </remarks>
	/// <param name="size">How large the quad is, in world units (metres).</param>
	/// <param name="positionAnchor">Which point of the quad should end up at its position (or the centre if <see cref="Orientation2D.None"/>, in which case the offset is zero).</param>
	public static Vect CalculateAnchorOffsetForStandardQuadMesh(XYPair<float> size, Orientation2D positionAnchor) {
		var translatedAnchorPoint = (MathUtils.FindAnchorInNormalized2DCoordinateSystem(DiagonalOrientation2D.DownRight, positionAnchor) - new XYPair<float>(0.5f)) * -size;
		return new Vect(translatedAnchorPoint.X, translatedAnchorPoint.Y, 0f);
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingMesh.GetNameAsNewStringObject();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingMesh.GetNameLength();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingMesh.CopyName(destinationBuffer);

	/// <summary>
	/// Disposes the underlying mesh, releasing its GPU resources.
	/// </summary>
	/// <remarks>
	/// Nothing using this mesh may still be alive when it is disposed.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => UnderlyingMesh.Dispose();

	/// <inheritdoc />
	public override string ToString() => $"Quad {UnderlyingMesh}";

	/// <summary>
	/// Returns the general-purpose mesh this quad is a specialized view of.
	/// </summary>
	/// <param name="operand">The quad mesh to convert.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Mesh(QuadMesh operand) => operand.UnderlyingMesh;

	#region Equality
	/// <inheritdoc />
	public bool Equals(QuadMesh other) => UnderlyingMesh.Equals(other.UnderlyingMesh);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is QuadMesh other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => UnderlyingMesh.GetHashCode();
	/// <summary>
	/// Returns whether the two given quad meshes wrap the same underlying mesh.
	/// </summary>
	/// <param name="left">The first quad mesh to compare.</param>
	/// <param name="right">The second quad mesh to compare.</param>
	public static bool operator ==(QuadMesh left, QuadMesh right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given quad meshes wrap different underlying meshes.
	/// </summary>
	/// <param name="left">The first quad mesh to compare.</param>
	/// <param name="right">The second quad mesh to compare.</param>
	public static bool operator !=(QuadMesh left, QuadMesh right) => !left.Equals(right);
	#endregion
}

/// <summary>
/// Common ground between the two kinds of quad instance, so that either can be held and disposed without knowing which it is.
/// </summary>
public interface IQuadInstance : IDisposable, IStringSpanNameEnabled;

/// <summary>
/// An instance of a <see cref="QuadMesh"/>: One flat rectangle placed in a scene, free to be positioned and oriented like any other object. Created via the factory's <see cref="IObjectBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// Use this for a flat surface that genuinely belongs in the world and should be seen edge-on when viewed from the side,
/// such as a poster on a wall or a patch of ground. For a quad that should always face the camera, use
/// <see cref="CameraLockedQuadInstance"/> instead.
/// </para>
/// <para>
/// Use <see cref="SetTransform(Location, XYPair{float}, Direction, Direction?, Orientation2D)"/> for a convenient way to place this in-world. 
/// </para>
/// <para>
/// This is a resource in its own right, but it shares its identity with its <see cref="UnderlyingModelInstance"/>: it has the same name and the same
/// dependencies, and while it is held in a <see cref="ResourceGroup"/> its underlying <see cref="ModelInstance"/> can not be disposed. A group only lists
/// it under the type it was added as (i.e. <see cref="ResourceGroup.QuadInstances"/> rather than <see cref="ResourceGroup.ModelInstances"/>).
/// </para>
/// </remarks>
public readonly struct QuadInstance : IQuadInstance, ITransformedSceneObject, IMaterialUsingSceneObject, IDisposableResource<QuadInstance> {
	static SceneObjectType ISceneObject.SceneObjectType { get; } = SceneObjectType.QuadInstance;

	/// <summary>
	/// The general-purpose model instance this quad instance is a specialized view of.
	/// </summary>
	public ModelInstance UnderlyingModelInstance { get; }

	internal QuadInstance(ModelInstance underlyingModelInstance) {
		UnderlyingModelInstance = underlyingModelInstance;
	}
	
	
	/// <summary>
	/// Wraps an existing model instance as a <see cref="QuadInstance"/>, without creating anything new.
	/// </summary>
	/// <remarks>
	/// Only use this for an instance that really was created from a standard quad mesh; nothing here verifies that it was.
	/// The given anchor replaces any the instance already had, and is what <see cref="PositionAnchor"/> reports from then on; it does not move the quad.
	/// </remarks>
	/// <param name="underlyingModelInstance">The model instance to wrap.</param>
	/// <param name="positionAnchor">The value for <see cref="PositionAnchor"/>.</param>
	public static QuadInstance FromPreviouslyAllocatedUnderlyingModelInstance(ModelInstance underlyingModelInstance, Orientation2D positionAnchor) {
		underlyingModelInstance.Implementation.SetQuadInstancePositionAnchor(underlyingModelInstance.Handle, positionAnchor);
		return new(underlyingModelInstance);
	}

	/// <summary>
	/// Which point of the quad is placed at its <see cref="Position"/> (or the centre if <see cref="Orientation2D.None"/>).
	/// </summary>
	/// <remarks>
	/// Set when the quad is created or placed via <see cref="SetTransform(Location, XYPair{float}, Direction, Direction?, Orientation2D)"/>.
	/// Moving, turning and rescaling the quad keep this point where it is.
	/// </remarks>
	public Orientation2D PositionAnchor => UnderlyingModelInstance.Implementation.GetQuadInstancePositionAnchor(UnderlyingModelInstance.GetHandleWithoutDisposeCheck());


	static QuadInstance WrapBase(ModelInstance b) => new(b);
	ResourceHandle<QuadInstance> IResource<QuadInstance>.Handle => UnderlyingModelInstance.Handle.AsInteger;
	ResourceHandle IResource.Handle => UnderlyingModelInstance.Handle;
	IResourceImplProvider IResource.Implementation => UnderlyingModelInstance.Implementation;
	ResourceIdent IResource.Ident => UnderlyingModelInstance.Handle.Ident;
	ResourceStub IResource.AsStub => new(UnderlyingModelInstance.Handle.Ident, UnderlyingModelInstance.Implementation);
	ResourceHandle<QuadInstance> IResource<QuadInstance>.GetHandleWithoutDisposeCheck() => UnderlyingModelInstance.GetHandleWithoutDisposeCheck().AsInteger;
	static QuadInstance IResource<QuadInstance>.CreateFromHandleAndImpl(ResourceHandle<QuadInstance> handle, IResourceImplProvider impl) => WrapBase(ResourceUtils.FastFromStub<ModelInstance>(new ResourceStub(new ResourceIdent(ResourceHandle<ModelInstance>.TypeHandle, handle.AsInteger), impl)));
	static QuadInstance IResource<QuadInstance>.CreateFromStub(ResourceStub stub) => WrapBase(ResourceUtils.FromStub<ModelInstance>(stub));
	static QuadInstance IResource<QuadInstance>.FastCreateFromStub(ResourceStub stub) => WrapBase(ResourceUtils.FastFromStub<ModelInstance>(stub));
	
	/// <inheritdoc />
	/// <remarks>
	/// The transform's translation is the centre of the quad, which differs from <see cref="Position"/> when the quad has a non-<c>None</c> <see cref="PositionAnchor"/>.
	/// </remarks>
	public Transform Transform {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Transform;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetTransform(value);
	}
	/// <summary>
	/// Sets <see cref="Transform"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="transform">The new value for <see cref="Transform"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTransform(Transform transform) => Transform = transform;
	
	/// <summary>
	/// Where this quad's <see cref="PositionAnchor"/> point is in the world (its centre if the anchor is <see cref="Orientation2D.None"/>).
	/// </summary>
	/// <remarks>
	/// This is the same point that was given as the position when this quad was created or last placed. Turning or rescaling the quad keeps
	/// this point where it is.
	/// </remarks>
	public Location Position {
		get {
			var transform = UnderlyingModelInstance.Transform;
			return (transform.Translation - CalculateRotatedAnchorOffset(in transform)).AsLocation();
		}
		set {
			var transform = UnderlyingModelInstance.Transform;
			UnderlyingModelInstance.SetPosition(value + CalculateRotatedAnchorOffset(in transform));
		}
	}
	/// <summary>
	/// Sets <see cref="Position"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="position">The new value for <see cref="Position"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	/// <inheritdoc />
	/// <remarks>
	/// The quad turns around its <see cref="Position"/> (i.e. its anchor point).
	/// </remarks>
	public Rotation Rotation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Rotation;
		set {
			var transform = UnderlyingModelInstance.Transform;
			SetTransformPreservingAnchor(in transform, transform with { Rotation = value });
		}
	}
	/// <summary>
	/// Sets <see cref="Rotation"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="rotation">The new value for <see cref="Rotation"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotation(Rotation rotation) => Rotation = rotation;

	/// <inheritdoc />
	/// <remarks>
	/// The quad turns around its <see cref="Position"/> (i.e. its anchor point).
	/// </remarks>
	public Quaternion RotationQuaternion {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.RotationQuaternion;
		set {
			var transform = UnderlyingModelInstance.Transform;
			SetTransformPreservingAnchor(in transform, transform with { RotationQuaternion = value });
		}
	}
	/// <summary>
	/// Sets <see cref="RotationQuaternion"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="rotationQuaternion">The new value for <see cref="RotationQuaternion"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotationQuaternion(Quaternion rotationQuaternion) => RotationQuaternion = rotationQuaternion;

	Vect IScaledSceneObject.Scaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Scaling;
		set {
			var transform = UnderlyingModelInstance.Transform;
			SetTransformPreservingAnchor(in transform, transform with { Scaling = value });
		}
	}
	/// <summary>
	/// How large this quad is on each of its two axes, where <c>(1, 1)</c> is its unmodified size.
	/// </summary>
	/// <remarks>
	/// A quad is flat, so only two axes are meaningful; setting this leaves the third axis at <c>1</c>. Because a standard
	/// quad mesh is a one-by-one square, this doubles as the quad's size in world units (metres). Rescaling keeps the quad's
	/// <see cref="Position"/> (i.e. its anchor point) where it is.
	/// </remarks>
	public XYPair<float> Scaling {
		get {
			var scalingVect = UnderlyingModelInstance.Scaling;
			return (scalingVect.X, scalingVect.Y);
		}
		set {
			var transform = UnderlyingModelInstance.Transform;
			SetTransformPreservingAnchor(in transform, transform with { Scaling = new Vect(value.X, value.Y, 1f) });
		}
	}
	/// <summary>
	/// Sets <see cref="Scaling"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="scaling">The new value for <see cref="Scaling"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetScaling(XYPair<float> scaling) => Scaling = scaling;
	
	/// <inheritdoc />
	public Material Material {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Material;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetMaterial(value);
	}
	/// <summary>
	/// Sets <see cref="Material"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="material">The new value for <see cref="Material"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetMaterial(Material material) => Material = material;

	/// <inheritdoc />
	public MaterialEffectController? MaterialEffects {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.MaterialEffects;
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingModelInstance.GetNameAsNewStringObject();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingModelInstance.GetNameLength();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingModelInstance.CopyName(destinationBuffer);
	
	/// <summary>
	/// Places, orients and sizes this quad in one call.
	/// </summary>
	/// <remarks>
	/// This is the convenient way to position a quad, as it works in terms a flat object actually has (a size and a facing
	/// direction) rather than requiring a full transform to be assembled first.
	/// </remarks>
	/// <param name="position">Where to put the quad.</param>
	/// <param name="size">How large the quad should be, in world units (metres).</param>
	/// <param name="facingDirection">Which way the quad's front face should point.</param>
	/// <param name="uprightDirection">Which way is "up" across the quad's face, or <see langword="null"/> to derive one from <paramref name="facingDirection"/>. Must not be parallel to <paramref name="facingDirection"/>.</param>
	/// <param name="positionAnchor">Which point of the quad is placed at <paramref name="position"/> (or the centre if <see cref="Orientation2D.None"/>). This becomes the quad's new <see cref="PositionAnchor"/>.</param>
	public void SetTransform(Location position, XYPair<float> size, Direction facingDirection, Direction? uprightDirection = null, Orientation2D positionAnchor = Orientation2D.None) {
		UnderlyingModelInstance.Implementation.SetQuadInstancePositionAnchor(UnderlyingModelInstance.GetHandleWithoutDisposeCheck(), positionAnchor);
		UnderlyingModelInstance.SetTransform(QuadMesh.CalculateTransformForStandardQuadMesh(position, size, facingDirection, uprightDirection, positionAnchor));
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveBy(Vect translation) => UnderlyingModelInstance.MoveBy(translation);
	/// <inheritdoc />
	/// <remarks>
	/// The quad turns around its <see cref="Position"/> (i.e. its anchor point).
	/// </remarks>
	public void RotateBy(Rotation rotation) => UnderlyingModelInstance.RotateBy(rotation, Position);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Rotation rotation, Location pivotPoint) => UnderlyingModelInstance.RotateBy(rotation, pivotPoint);
	/// <inheritdoc />
	/// <remarks>
	/// The quad turns around its <see cref="Position"/> (i.e. its anchor point).
	/// </remarks>
	public void RotateBy(Quaternion rotationQuaternion) => UnderlyingModelInstance.RotateBy(rotationQuaternion, Position);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Quaternion rotationQuaternion, Location pivotPoint) => UnderlyingModelInstance.RotateBy(rotationQuaternion, pivotPoint);
	/// <inheritdoc />
	public void ScaleBy(float scalar) {
		var transform = UnderlyingModelInstance.Transform;
		SetTransformPreservingAnchor(in transform, transform.WithScalingMultipliedBy(scalar));
	}
	/// <inheritdoc />
	public void ScaleBy(Vect vect) {
		var transform = UnderlyingModelInstance.Transform;
		SetTransformPreservingAnchor(in transform, transform.WithScalingMultipliedBy(vect));
	}
	/// <inheritdoc />
	public void AdjustScaleBy(float scalar) {
		var transform = UnderlyingModelInstance.Transform;
		SetTransformPreservingAnchor(in transform, transform.WithScalingAdjustedBy(scalar));
	}
	/// <inheritdoc />
	public void AdjustScaleBy(Vect vect) {
		var transform = UnderlyingModelInstance.Transform;
		SetTransformPreservingAnchor(in transform, transform.WithScalingAdjustedBy(vect));
	}

	Vect CalculateRotatedAnchorOffset(in Transform transform) {
		var anchor = PositionAnchor;
		if (anchor == Orientation2D.None) return Vect.Zero;
		return QuadMesh.CalculateAnchorOffsetForStandardQuadMesh(new XYPair<float>(transform.Scaling.X, transform.Scaling.Y), anchor) * transform.Rotation;
	}

	void SetTransformPreservingAnchor(in Transform currentTransform, Transform newTransform) {
		var anchorPoint = currentTransform.Translation - CalculateRotatedAnchorOffset(in currentTransform);
		UnderlyingModelInstance.SetTransform(newTransform with { Translation = anchorPoint + CalculateRotatedAnchorOffset(in newTransform) });
	}
	
	/// <summary>
	/// Sets the base colour this quad is drawn in, for a quad using one of the built-in default materials.
	/// </summary>
	/// <remarks>
	/// This has no effect on a quad using a material of your own.
	/// </remarks>
	/// <param name="baseColor">The colour to draw the quad in.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetDefaultMaterialBaseColor(ColorVect baseColor) => UnderlyingModelInstance.SetDefaultMaterialBaseColor(baseColor);
	/// <summary>
	/// Sets how this quad reacts to light, for a quad using one of the built-in default materials.
	/// </summary>
	/// <remarks>
	/// This has no effect on a quad using a material of your own.
	/// </remarks>
	/// <param name="style">The shading style to use.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetDefaultMaterialShadingStyle(DefaultMaterialShadingStyle style) => UnderlyingModelInstance.SetDefaultMaterialShadingStyle(style);

	/// <summary>
	/// Disposes the underlying model instance, removing this quad from any scene it is in.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => UnderlyingModelInstance.Dispose();

	/// <inheritdoc />
	public override string ToString() => $"Quad {UnderlyingModelInstance}";

	#region Equality
	/// <inheritdoc />
	public bool Equals(QuadInstance other) => UnderlyingModelInstance.Equals(other.UnderlyingModelInstance);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is QuadInstance other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => UnderlyingModelInstance.GetHashCode();
	/// <summary>
	/// Returns whether the two given quad instances wrap the same underlying model instance.
	/// </summary>
	/// <param name="left">The first quad instance to compare.</param>
	/// <param name="right">The second quad instance to compare.</param>
	public static bool operator ==(QuadInstance left, QuadInstance right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given quad instances wrap different underlying model instances.
	/// </summary>
	/// <param name="left">The first quad instance to compare.</param>
	/// <param name="right">The second quad instance to compare.</param>
	public static bool operator !=(QuadInstance left, QuadInstance right) => !left.Equals(right);
	#endregion
}

/// <summary>
/// An instance of a <see cref="QuadMesh"/>: One flat rectangle placed in a scene that continually turns to face the camera, so that it never appears edge-on. Created via the factory's <see cref="IObjectBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is what makes a flat image read as an object in the world rather than as a flat image: a distant tree drawn on a single
/// quad looks like a tree from every angle, instead of vanishing when you walk around it. The same technique is used for
/// floating labels and diagnostic data.
/// </para>
/// <para>
/// This is a resource in its own right, but it shares its identity with the model instance underlying its <see cref="UnderlyingQuadInstance"/>: it has the same name and the same
/// dependencies, and while it is held in a <see cref="ResourceGroup"/> its underlying <see cref="ModelInstance"/> can not be disposed. A group only lists
/// it under the type it was added as (i.e. <see cref="ResourceGroup.CameraLockedQuadInstances"/> rather than <see cref="ResourceGroup.ModelInstances"/>).
/// </para>
/// </remarks>
public readonly struct CameraLockedQuadInstance : IQuadInstance, IScaledSceneObject, IPositionedSceneObject, IMaterialUsingSceneObject, IDisposableResource<CameraLockedQuadInstance> {
	static SceneObjectType ISceneObject.SceneObjectType { get; } = SceneObjectType.CameraLockedQuadInstance;

	/// <summary>
	/// The quad instance this camera-locked quad is a specialized view of.
	/// </summary>
	public QuadInstance UnderlyingQuadInstance { get; }
	/// <summary>
	/// Which way is "up" across this quad's face as it turns to follow the camera.
	/// </summary>
	/// <remarks>
	/// Without this the quad would be free to spin about its own facing direction; fixing it is what keeps the quad the
	/// right way up as the camera moves around.
	/// </remarks>
	public Direction LockedUprightDirection => GetLockConfig().LockedUprightDirection;
	/// <summary>
	/// Which point of the quad is placed at its <see cref="Position"/> (or the centre if <see cref="Orientation2D.None"/>).
	/// </summary>
	/// <remarks>
	/// The anchor holds in every <see cref="ScalingMode"/>, and stays put when the quad is rescaled.
	/// </remarks>
	public Orientation2D PositionAnchor => GetLockConfig().PositionAnchor;
	/// <summary>
	/// How this quad's size responds to its distance from the camera.
	/// </summary>
	public CameraLockedScalingMode ScalingMode => GetLockConfig().ScalingMode;
	/// <summary>
	/// Which axes this quad is free to turn about as it follows the camera.
	/// </summary>
	public CameraLockStyle LockStyle => GetLockConfig().LockStyle;

	internal CameraLockedQuadInstance(QuadInstance underlyingQuadInstance) {
		UnderlyingQuadInstance = underlyingQuadInstance;
	}

	CameraLockConfig GetLockConfig() {
		var instance = UnderlyingQuadInstance.UnderlyingModelInstance;
		return instance.Implementation.GetCameraLockConfig(instance.Handle) ?? CameraLockConfig.Default;
	}


	static CameraLockedQuadInstance WrapBase(ModelInstance b) => new(new QuadInstance(b));
	ResourceHandle<CameraLockedQuadInstance> IResource<CameraLockedQuadInstance>.Handle => UnderlyingQuadInstance.UnderlyingModelInstance.Handle.AsInteger;
	ResourceHandle IResource.Handle => UnderlyingQuadInstance.UnderlyingModelInstance.Handle;
	IResourceImplProvider IResource.Implementation => UnderlyingQuadInstance.UnderlyingModelInstance.Implementation;
	ResourceIdent IResource.Ident => UnderlyingQuadInstance.UnderlyingModelInstance.Handle.Ident;
	ResourceStub IResource.AsStub => new(UnderlyingQuadInstance.UnderlyingModelInstance.Handle.Ident, UnderlyingQuadInstance.UnderlyingModelInstance.Implementation);
	ResourceHandle<CameraLockedQuadInstance> IResource<CameraLockedQuadInstance>.GetHandleWithoutDisposeCheck() => UnderlyingQuadInstance.UnderlyingModelInstance.GetHandleWithoutDisposeCheck().AsInteger;
	static CameraLockedQuadInstance IResource<CameraLockedQuadInstance>.CreateFromHandleAndImpl(ResourceHandle<CameraLockedQuadInstance> handle, IResourceImplProvider impl) => WrapBase(ResourceUtils.FastFromStub<ModelInstance>(new ResourceStub(new ResourceIdent(ResourceHandle<ModelInstance>.TypeHandle, handle.AsInteger), impl)));
	static CameraLockedQuadInstance IResource<CameraLockedQuadInstance>.CreateFromStub(ResourceStub stub) => WrapBase(ResourceUtils.FromStub<ModelInstance>(stub));
	static CameraLockedQuadInstance IResource<CameraLockedQuadInstance>.FastCreateFromStub(ResourceStub stub) => WrapBase(ResourceUtils.FastFromStub<ModelInstance>(stub));
	
	
	/// <summary>
	/// Wraps an existing quad instance as a <see cref="CameraLockedQuadInstance"/>, recording the given lock settings against it.
	/// </summary>
	/// <remarks>
	/// Only use this for an instance that really was created as a camera-locked quad; nothing here verifies that it was.
	/// The given settings replace any the instance already had, and are what this object's properties report from then on. Scenes copy the
	/// settings when the object is added, so re-add it to any scene it is already in for a change to take effect there.
	/// </remarks>
	/// <param name="underlyingQuadInstance">The quad instance to wrap.</param>
	/// <param name="lockedUprightDirection">The value for <see cref="LockedUprightDirection"/>.</param>
	/// <param name="positionAnchor">The value for <see cref="PositionAnchor"/>.</param>
	/// <param name="scalingMode">The value for <see cref="ScalingMode"/>.</param>
	/// <param name="lockStyle">The value for <see cref="LockStyle"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CameraLockedQuadInstance FromPreviouslyAllocatedUnderlyingQuadInstance(QuadInstance underlyingQuadInstance, Direction lockedUprightDirection, Orientation2D positionAnchor, CameraLockedScalingMode scalingMode, CameraLockStyle lockStyle) {
		var instance = underlyingQuadInstance.UnderlyingModelInstance;
		instance.Implementation.SetCameraLockConfig(instance.Handle, new CameraLockConfig(lockedUprightDirection, positionAnchor, scalingMode, lockStyle));
		return new(underlyingQuadInstance);
	}
	
	/// <summary>
	/// Where this quad's <see cref="PositionAnchor"/> point is in the world (its centre if the anchor is <see cref="Orientation2D.None"/>).
	/// </summary>
	/// <remarks>
	/// This is the same point that was given as the position when this quad was created.
	/// </remarks>
	public Location Position {
		get {
			var transform = UnderlyingQuadInstance.Transform;
			return (transform.Translation - CalculateRotatedAnchorOffset(in transform)).AsLocation();
		}
		set {
			var transform = UnderlyingQuadInstance.Transform;
			UnderlyingQuadInstance.UnderlyingModelInstance.SetPosition(value + CalculateRotatedAnchorOffset(in transform));
		}
	}
	/// <summary>
	/// Sets <see cref="Position"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="position">The new value for <see cref="Position"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	Vect IScaledSceneObject.Scaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingQuadInstance.UnderlyingModelInstance.Scaling;
		set {
			var transform = UnderlyingQuadInstance.Transform;
			SetTransformPreservingAnchor(in transform, transform with { Scaling = value });
		}
	}
	/// <summary>
	/// How large this quad is on each of its two axes, where <c>(1, 1)</c> is its unmodified size.
	/// </summary>
	/// <remarks>
	/// How this translates in to the quad's size on screen depends on <see cref="ScalingMode"/>. Rescaling keeps the <see cref="PositionAnchor"/> point where it is.
	/// </remarks>
	public XYPair<float> Scaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingQuadInstance.Scaling;
		set {
			var transform = UnderlyingQuadInstance.Transform;
			SetTransformPreservingAnchor(in transform, transform with { Scaling = new Vect(value.X, value.Y, 1f) });
		}
	}
	/// <summary>
	/// Sets <see cref="Scaling"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="scaling">The new value for <see cref="Scaling"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetScaling(XYPair<float> scaling) => Scaling = scaling;
	
	/// <inheritdoc />
	public Material Material {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingQuadInstance.Material;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingQuadInstance.SetMaterial(value);
	}
	/// <summary>
	/// Sets <see cref="Material"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="material">The new value for <see cref="Material"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetMaterial(Material material) => Material = material;

	/// <inheritdoc />
	public MaterialEffectController? MaterialEffects {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingQuadInstance.MaterialEffects;
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingQuadInstance.GetNameAsNewStringObject();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingQuadInstance.GetNameLength();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingQuadInstance.CopyName(destinationBuffer);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveBy(Vect translation) => UnderlyingQuadInstance.MoveBy(translation);
	/// <inheritdoc />
	public void ScaleBy(float scalar) {
		var transform = UnderlyingQuadInstance.Transform;
		SetTransformPreservingAnchor(in transform, transform.WithScalingMultipliedBy(scalar));
	}
	/// <inheritdoc />
	public void ScaleBy(Vect vect) {
		var transform = UnderlyingQuadInstance.Transform;
		SetTransformPreservingAnchor(in transform, transform.WithScalingMultipliedBy(vect));
	}
	/// <inheritdoc />
	public void AdjustScaleBy(float scalar) {
		var transform = UnderlyingQuadInstance.Transform;
		SetTransformPreservingAnchor(in transform, transform.WithScalingAdjustedBy(scalar));
	}
	/// <inheritdoc />
	public void AdjustScaleBy(Vect vect) {
		var transform = UnderlyingQuadInstance.Transform;
		SetTransformPreservingAnchor(in transform, transform.WithScalingAdjustedBy(vect));
	}

	Vect CalculateRotatedAnchorOffset(in Transform transform) {
		var anchor = PositionAnchor;
		if (anchor == Orientation2D.None) return Vect.Zero;
		return QuadMesh.CalculateAnchorOffsetForStandardQuadMesh(new XYPair<float>(transform.Scaling.X, transform.Scaling.Y), anchor) * transform.Rotation;
	}

	void SetTransformPreservingAnchor(in Transform currentTransform, Transform newTransform) {
		var anchorPoint = currentTransform.Translation - CalculateRotatedAnchorOffset(in currentTransform);
		UnderlyingQuadInstance.UnderlyingModelInstance.SetTransform(newTransform with { Translation = anchorPoint + CalculateRotatedAnchorOffset(in newTransform) });
	}
	
	/// <summary>
	/// Sets the base colour this quad is drawn in, for a quad using one of the built-in default materials.
	/// </summary>
	/// <remarks>
	/// This has no effect on a quad using a material of your own.
	/// </remarks>
	/// <param name="baseColor">The colour to draw the quad in.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetDefaultMaterialBaseColor(ColorVect baseColor) => UnderlyingQuadInstance.SetDefaultMaterialBaseColor(baseColor);
	/// <summary>
	/// Sets how this quad reacts to light, for a quad using one of the built-in default materials.
	/// </summary>
	/// <remarks>
	/// This has no effect on a quad using a material of your own.
	/// </remarks>
	/// <param name="style">The shading style to use.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetDefaultMaterialShadingStyle(DefaultMaterialShadingStyle style) => UnderlyingQuadInstance.SetDefaultMaterialShadingStyle(style);

	/// <summary>
	/// Disposes the underlying quad instance, removing this quad from any scene it is in.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => UnderlyingQuadInstance.Dispose();

	/// <inheritdoc />
	public override string ToString() => UnderlyingQuadInstance.ToString();

	#region Equality
	/// <inheritdoc />
	public bool Equals(CameraLockedQuadInstance other) => UnderlyingQuadInstance.Equals(other.UnderlyingQuadInstance);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is CameraLockedQuadInstance other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => UnderlyingQuadInstance.GetHashCode();
	/// <summary>
	/// Returns whether the two given camera-locked quads wrap the same underlying quad instance.
	/// </summary>
	/// <param name="left">The first quad to compare.</param>
	/// <param name="right">The second quad to compare.</param>
	public static bool operator ==(CameraLockedQuadInstance left, CameraLockedQuadInstance right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given camera-locked quads wrap different underlying quad instances.
	/// </summary>
	/// <param name="left">The first quad to compare.</param>
	/// <param name="right">The second quad to compare.</param>
	public static bool operator !=(CameraLockedQuadInstance left, CameraLockedQuadInstance right) => !left.Equals(right);
	#endregion
}
