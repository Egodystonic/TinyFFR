// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;
using System.Buffers.Binary;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// A mesh that is a single flat rectangle, presented as its own type so that it can be used without restating that it is flat.
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
/// </remarks>
public readonly struct QuadMesh : IResourceSpecialization<QuadMesh, Mesh>, IStringSpanNameEnabled, IEquatable<QuadMesh> {
	/// <summary>
	/// The general-purpose mesh this quad is a specialized view of.
	/// </summary>
	public Mesh UnderlyingMesh { get; } 

	internal QuadMesh(Mesh underlyingMesh) {
		UnderlyingMesh = underlyingMesh;
	}
	
	#region Specialization
	static IntPtr IResourceSpecialization<QuadMesh, Mesh>.SpecializationTypeIdentifier => typeof(QuadMesh).TypeHandle.Value;
	int IResourceSpecialization<QuadMesh, Mesh>.SpecializationDataLength => 0;
	static void IResourceSpecialization<QuadMesh, Mesh>.Smuggle(QuadMesh resource, Span<byte> specializationDataBuffer, out Mesh outBaseResource, out ResourceStub? additionalResourceRef) {
		additionalResourceRef = null;
		outBaseResource = resource.UnderlyingMesh;
	}
	static QuadMesh IResourceSpecialization<QuadMesh, Mesh>.DeSmuggle(Mesh baseResource, ReadOnlySpan<byte> specializationDataBuffer, ResourceStub? additionalResourceRef) {
		return new(baseResource);	
	}
	#endregion

	/// <summary>
	/// Wraps an existing mesh as a <see cref="QuadMesh"/>, without creating anything new.
	/// </summary>
	/// <remarks>
	/// Only use this for a mesh that really was built as a standard unit quad; nothing here verifies that it was.
	/// </remarks>
	/// <param name="underlyingMesh">The mesh to wrap.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static QuadMesh FromPreviouslyAllocatedUnderlyingMesh(Mesh underlyingMesh) => new(underlyingMesh);

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
/// An instance of a <see cref="QuadMesh"/>: One flat rectangle placed in a scene, free to be positioned and oriented like any other object.
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
/// </remarks>
public readonly struct QuadInstance : IQuadInstance, IResourceSpecialization<QuadInstance, ModelInstance>, IEquatable<QuadInstance>, ITransformedSceneObject, IMaterialUsingSceneObject {
	/// <summary>
	/// The general-purpose model instance this quad instance is a specialized view of.
	/// </summary>
	public ModelInstance UnderlyingModelInstance { get; }

	internal QuadInstance(ModelInstance underlyingModelInstance) {
		UnderlyingModelInstance = underlyingModelInstance;
	}
	
	#region Specialization
	static IntPtr IResourceSpecialization<QuadInstance, ModelInstance>.SpecializationTypeIdentifier => typeof(QuadInstance).TypeHandle.Value;
	int IResourceSpecialization<QuadInstance, ModelInstance>.SpecializationDataLength => 0;
	static void IResourceSpecialization<QuadInstance, ModelInstance>.Smuggle(QuadInstance resource, Span<byte> specializationDataBuffer, out ModelInstance outBaseResource, out ResourceStub? additionalResourceRef) {
		additionalResourceRef = null;
		outBaseResource = resource.UnderlyingModelInstance;
	}
	static QuadInstance IResourceSpecialization<QuadInstance, ModelInstance>.DeSmuggle(ModelInstance baseResource, ReadOnlySpan<byte> specializationDataBuffer, ResourceStub? additionalResourceRef) {
		return new(baseResource);	
	}
	#endregion
	
	/// <summary>
	/// Wraps an existing model instance as a <see cref="QuadInstance"/>, without creating anything new.
	/// </summary>
	/// <remarks>
	/// Only use this for an instance that really was created from a standard quad mesh; nothing here verifies that it was.
	/// </remarks>
	/// <param name="underlyingModelInstance">The model instance to wrap.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static QuadInstance FromPreviouslyAllocatedUnderlyingModelInstance(ModelInstance underlyingModelInstance) => new(underlyingModelInstance);
	
	/// <inheritdoc />
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
	
	/// <inheritdoc />
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance.Position;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetPosition(value);
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
		get => UnderlyingModelInstance.Rotation;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetRotation(value);
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
		get => UnderlyingModelInstance.RotationQuaternion;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetRotationQuaternion(value);
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingModelInstance.SetScaling(value);
	}
	/// <summary>
	/// How large this quad is on each of its two axes, where <c>(1, 1)</c> is its unmodified size.
	/// </summary>
	/// <remarks>
	/// A quad is flat, so only two axes are meaningful; setting this leaves the third axis at <c>1</c>. Because a standard
	/// quad mesh is a one-by-one square, this doubles as the quad's size in world units (metres).
	/// </remarks>
	public XYPair<float> Scaling {
		get {
			var scalingVect = UnderlyingModelInstance.Scaling;
			return (scalingVect.X, scalingVect.Y);
		}
		set {
			UnderlyingModelInstance.SetScaling(new Vect(value.X, value.Y, 1f));
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
	/// <param name="positionAnchor">Which point of the quad is placed at <paramref name="position"/> (or the centre if <see cref="Orientation2D.None"/>).</param>
	public void SetTransform(Location position, XYPair<float> size, Direction facingDirection, Direction? uprightDirection = null, Orientation2D positionAnchor = Orientation2D.None) {
		UnderlyingModelInstance.SetTransform(QuadMesh.CalculateTransformForStandardQuadMesh(position, size, facingDirection, uprightDirection, positionAnchor));
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveBy(Vect translation) => UnderlyingModelInstance.MoveBy(translation);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Rotation rotation) => UnderlyingModelInstance.RotateBy(rotation);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Rotation rotation, Location pivotPoint) => UnderlyingModelInstance.RotateBy(rotation, pivotPoint);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Quaternion rotationQuaternion) => UnderlyingModelInstance.RotateBy(rotationQuaternion);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Quaternion rotationQuaternion, Location pivotPoint) => UnderlyingModelInstance.RotateBy(rotationQuaternion, pivotPoint);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(float scalar) => UnderlyingModelInstance.ScaleBy(scalar);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(Vect vect) => UnderlyingModelInstance.ScaleBy(vect);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(float scalar) => UnderlyingModelInstance.AdjustScaleBy(scalar);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(Vect vect) => UnderlyingModelInstance.AdjustScaleBy(vect);
	
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
/// An instance of a <see cref="QuadMesh"/>: One flat rectangle placed in a scene that continually turns to face the camera, so that it never appears edge-on.
/// </summary>
/// <remarks>
/// <para>
/// This is what makes a flat image read as an object in the world rather than as a flat image: a distant tree drawn on a single
/// quad looks like a tree from every angle, instead of vanishing when you walk around it. The same technique is used for
/// floating labels and diagnostic data.
/// </para>
/// </remarks>
public readonly struct CameraLockedQuadInstance : IQuadInstance, IResourceSpecialization<CameraLockedQuadInstance, ModelInstance>, IEquatable<CameraLockedQuadInstance>, IScaledSceneObject, IPositionedSceneObject, IMaterialUsingSceneObject {
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
	public Direction LockedUprightDirection { get; }
	/// <summary>
	/// Which point of the quad is placed at its position (or the centre if <see cref="Orientation2D.None"/>).
	/// </summary>
	public Orientation2D PositionAnchor { get; }
	/// <summary>
	/// How this quad's size responds to its distance from the camera.
	/// </summary>
	public CameraLockedScalingMode ScalingMode { get; }
	/// <summary>
	/// Which axes this quad is free to turn about as it follows the camera.
	/// </summary>
	public CameraLockStyle LockStyle { get; }

	internal CameraLockedQuadInstance(QuadInstance underlyingQuadInstance, Direction lockedUprightDirection, Orientation2D positionAnchor, CameraLockedScalingMode scalingMode, CameraLockStyle lockStyle) {
		UnderlyingQuadInstance = underlyingQuadInstance;
		LockedUprightDirection = lockedUprightDirection;
		PositionAnchor = positionAnchor;
		ScalingMode = scalingMode;
		LockStyle = lockStyle;
	}
	
	#region Specialization
	static IntPtr IResourceSpecialization<CameraLockedQuadInstance, ModelInstance>.SpecializationTypeIdentifier => typeof(CameraLockedQuadInstance).TypeHandle.Value;
	int IResourceSpecialization<CameraLockedQuadInstance, ModelInstance>.SpecializationDataLength => Direction.SerializationByteSpanLength + sizeof(int) + sizeof(int) + sizeof(int);
	static void IResourceSpecialization<CameraLockedQuadInstance, ModelInstance>.Smuggle(CameraLockedQuadInstance resource, Span<byte> specializationDataBuffer, out ModelInstance outBaseResource, out ResourceStub? additionalResourceRef) {
		additionalResourceRef = null;
		Direction.SerializeToBytes(specializationDataBuffer, resource.LockedUprightDirection);
		BinaryPrimitives.WriteInt32LittleEndian(specializationDataBuffer[Direction.SerializationByteSpanLength..], (int) resource.PositionAnchor);
		BinaryPrimitives.WriteInt32LittleEndian(specializationDataBuffer[(Direction.SerializationByteSpanLength + sizeof(int) * 1)..], (int) resource.ScalingMode);
		BinaryPrimitives.WriteInt32LittleEndian(specializationDataBuffer[(Direction.SerializationByteSpanLength + sizeof(int) * 2)..], (int) resource.LockStyle);
		outBaseResource = resource.UnderlyingQuadInstance.UnderlyingModelInstance;
	}
	static CameraLockedQuadInstance IResourceSpecialization<CameraLockedQuadInstance, ModelInstance>.DeSmuggle(ModelInstance baseResource, ReadOnlySpan<byte> specializationDataBuffer, ResourceStub? additionalResourceRef) {
		return new(
			new QuadInstance(baseResource),
			Direction.DeserializeFromBytes(specializationDataBuffer),
			(Orientation2D) BinaryPrimitives.ReadInt32LittleEndian(specializationDataBuffer[Direction.SerializationByteSpanLength..]),
			(CameraLockedScalingMode) BinaryPrimitives.ReadInt32LittleEndian(specializationDataBuffer[(Direction.SerializationByteSpanLength + sizeof(int) * 1)..]),
			(CameraLockStyle) BinaryPrimitives.ReadInt32LittleEndian(specializationDataBuffer[(Direction.SerializationByteSpanLength + sizeof(int) * 2)..])
		);	
	}
	#endregion
	
	/// <summary>
	/// Wraps an existing quad instance as a <see cref="CameraLockedQuadInstance"/>, without creating anything new.
	/// </summary>
	/// <remarks>
	/// Only use this for an instance that really was created as a camera-locked quad; nothing here verifies that it was.
	/// </remarks>
	/// <param name="underlyingQuadInstance">The quad instance to wrap.</param>
	/// <param name="lockedUprightDirection">The value for <see cref="LockedUprightDirection"/>.</param>
	/// <param name="positionAnchor">The value for <see cref="PositionAnchor"/>.</param>
	/// <param name="scalingMode">The value for <see cref="ScalingMode"/>.</param>
	/// <param name="lockStyle">The value for <see cref="LockStyle"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CameraLockedQuadInstance FromPreviouslyAllocatedUnderlyingQuadInstance(QuadInstance underlyingQuadInstance, Direction lockedUprightDirection, Orientation2D positionAnchor, CameraLockedScalingMode scalingMode, CameraLockStyle lockStyle) {
		return new(underlyingQuadInstance, lockedUprightDirection, positionAnchor, scalingMode, lockStyle);
	}
	
	/// <inheritdoc />
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingQuadInstance.Position;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingQuadInstance.SetPosition(value);
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingQuadInstance.UnderlyingModelInstance.SetScaling(value);
	}
	/// <summary>
	/// How large this quad is on each of its two axes, where <c>(1, 1)</c> is its unmodified size.
	/// </summary>
	/// <remarks>
	/// How this translates in to the quad's size on screen depends on <see cref="ScalingMode"/>.
	/// </remarks>
	public XYPair<float> Scaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingQuadInstance.Scaling;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingQuadInstance.SetScaling(value);
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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(float scalar) => UnderlyingQuadInstance.ScaleBy(scalar);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(Vect vect) => UnderlyingQuadInstance.ScaleBy(vect);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(float scalar) => UnderlyingQuadInstance.AdjustScaleBy(scalar);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(Vect vect) => UnderlyingQuadInstance.AdjustScaleBy(vect);
	
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