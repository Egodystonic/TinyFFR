// Created on 2026-07-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

/// <summary>
/// A string of text placed in a scene that continually turns to face the camera, so that it is always readable. Created via the factory's <see cref="IObjectBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is what floating labels, nameplates and damage numbers are made of: text that marks a point in the world but must stay
/// legible from wherever the camera happens to be.
/// </para>
/// <para>
/// This is a resource in its own right, but it shares its identity with the model instance underlying its <see cref="UnderlyingTextInstance"/>: it has the same name and the same
/// dependencies, and while it is held in a <see cref="ResourceGroup"/> its underlying <see cref="ModelInstance"/> can not be disposed. A group only lists
/// it under the type it was added as (i.e. <see cref="ResourceGroup.CameraLockedTextInstances"/> rather than <see cref="ResourceGroup.ModelInstances"/>).
/// </para>
/// </remarks>
public readonly struct CameraLockedTextInstance : ITextInstance, IEquatable<CameraLockedTextInstance>, IScaledSceneObject, IPositionedSceneObject, IDisposableResource<CameraLockedTextInstance> {
	static SceneObjectType ISceneObject.SceneObjectType { get; } = SceneObjectType.CameraLockedTextInstance;

	/// <summary>
	/// The text object this camera-locked text is a specialized view of.
	/// </summary>
	public TextInstance UnderlyingTextInstance { get; }
	/// <summary>
	/// Which way is "up" across this text as it turns to follow the camera (or unconstrained if <see cref="Direction.None"/>).
	/// </summary>
	/// <remarks>
	/// Without this the text would be free to spin about its own facing direction; fixing it is what keeps the text the right
	/// way up as the camera moves around.
	/// </remarks>
	public Direction LockedUprightDirection => GetLockConfig().LockedUprightDirection;
	/// <summary>
	/// Which point of the text is placed at its position (or the centre if <see cref="Orientation2D.None"/>).
	/// </summary>
	public Orientation2D PositionAnchor => GetLockConfig().PositionAnchor;
	/// <summary>
	/// How this text's size responds to its distance from the camera.
	/// </summary>
	public CameraLockedScalingMode ScalingMode => GetLockConfig().ScalingMode;
	/// <summary>
	/// Which axes this text is free to turn about as it follows the camera.
	/// </summary>
	public CameraLockStyle LockStyle => GetLockConfig().LockStyle;

	internal CameraLockedTextInstance(TextInstance underlyingTextInstance) {
		UnderlyingTextInstance = underlyingTextInstance;
	}

	CameraLockConfig GetLockConfig() {
		var instance = UnderlyingTextInstance.UnderlyingModelInstance;
		return instance.Implementation.GetCameraLockConfig(instance.Handle) ?? CameraLockConfig.Default;
	}


	static CameraLockedTextInstance WrapBase(ModelInstance b) => new(new TextInstance(b));
	ResourceHandle<CameraLockedTextInstance> IResource<CameraLockedTextInstance>.Handle => UnderlyingTextInstance.UnderlyingModelInstance.Handle.AsInteger;
	ResourceHandle IResource.Handle => UnderlyingTextInstance.UnderlyingModelInstance.Handle;
	IResourceImplProvider IResource.Implementation => UnderlyingTextInstance.UnderlyingModelInstance.Implementation;
	ResourceIdent IResource.Ident => UnderlyingTextInstance.UnderlyingModelInstance.Handle.Ident;
	ResourceStub IResource.AsStub => new(UnderlyingTextInstance.UnderlyingModelInstance.Handle.Ident, UnderlyingTextInstance.UnderlyingModelInstance.Implementation);
	ResourceHandle<CameraLockedTextInstance> IResource<CameraLockedTextInstance>.GetHandleWithoutDisposeCheck() => UnderlyingTextInstance.UnderlyingModelInstance.GetHandleWithoutDisposeCheck().AsInteger;
	static CameraLockedTextInstance IResource<CameraLockedTextInstance>.CreateFromHandleAndImpl(ResourceHandle<CameraLockedTextInstance> handle, IResourceImplProvider impl) => WrapBase(ResourceUtils.FastFromStub<ModelInstance>(new ResourceStub(new ResourceIdent(ResourceHandle<ModelInstance>.TypeHandle, handle.AsInteger), impl)));
	static CameraLockedTextInstance IResource<CameraLockedTextInstance>.CreateFromStub(ResourceStub stub) => WrapBase(ResourceUtils.FromStub<ModelInstance>(stub));
	static CameraLockedTextInstance IResource<CameraLockedTextInstance>.FastCreateFromStub(ResourceStub stub) => WrapBase(ResourceUtils.FastFromStub<ModelInstance>(stub));
	
	
	/// <summary>
	/// Wraps an existing text object as a <see cref="CameraLockedTextInstance"/>, recording the given lock settings against it.
	/// </summary>
	/// <remarks>
	/// Only use this for an instance that really was created as camera-locked text; nothing here verifies that it was.
	/// The given settings replace any the instance already had, and are what this object's properties report from then on. Scenes copy the
	/// settings when the object is added, so re-add it to any scene it is already in for a change to take effect there.
	/// </remarks>
	/// <param name="underlyingTextInstance">The text object to wrap.</param>
	/// <param name="lockedUprightDirection">The value for <see cref="LockedUprightDirection"/>.</param>
	/// <param name="positionAnchor">The value for <see cref="PositionAnchor"/>.</param>
	/// <param name="scalingMode">The value for <see cref="ScalingMode"/>.</param>
	/// <param name="lockStyle">The value for <see cref="LockStyle"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CameraLockedTextInstance FromPreviouslyAllocatedUnderlyingTextInstance(TextInstance underlyingTextInstance, Direction lockedUprightDirection, Orientation2D positionAnchor, CameraLockedScalingMode scalingMode, CameraLockStyle lockStyle) {
		var instance = underlyingTextInstance.UnderlyingModelInstance;
		instance.Implementation.SetCameraLockConfig(instance.Handle, new CameraLockConfig(lockedUprightDirection, positionAnchor, scalingMode, lockStyle));
		return new(underlyingTextInstance);
	}

	/// <summary>
	/// The font this object's text is drawn with.
	/// </summary>
	/// <remarks>
	/// This follows from <see cref="String"/>, since a prepared string belongs to the font it was prepared with.
	/// </remarks>
	public Font Font => UnderlyingTextInstance.Font;

	/// <inheritdoc />
	public FontPen Pen {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingTextInstance.Pen;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingTextInstance.SetPen(value);
	}
	/// <summary>
	/// Sets <see cref="Pen"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="pen">The new value for <see cref="Pen"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPen(FontPen pen) => Pen = pen;

	/// <inheritdoc />
	public FontString String {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingTextInstance.String;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingTextInstance.SetString(value);
	}
	/// <summary>
	/// Sets <see cref="String"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="string">The new value for <see cref="String"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetString(FontString @string) => String = @string;

	/// <inheritdoc />
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingTextInstance.Position;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingTextInstance.SetPosition(value);
	}
	/// <summary>
	/// Sets <see cref="Position"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="position">The new value for <see cref="Position"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	Vect IScaledSceneObject.Scaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingTextInstance.UnderlyingModelInstance.Scaling;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingTextInstance.UnderlyingModelInstance.SetScaling(value);
	}
	/// <summary>
	/// How large this text is on each of its two axes, where <c>(1, 1)</c> is the size its layout gives it.
	/// </summary>
	/// <remarks>
	/// How this translates in to the text's size on screen depends on <see cref="ScalingMode"/>.
	/// </remarks>
	public XYPair<float> Scaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingTextInstance.Scaling;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingTextInstance.SetScaling(value);
	}
	/// <summary>
	/// Sets <see cref="Scaling"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="scaling">The new value for <see cref="Scaling"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetScaling(XYPair<float> scaling) => Scaling = scaling;

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingTextInstance.GetNameAsNewStringObject();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingTextInstance.GetNameLength();
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingTextInstance.CopyName(destinationBuffer);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveBy(Vect translation) => UnderlyingTextInstance.MoveBy(translation);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(float scalar) => UnderlyingTextInstance.ScaleBy(scalar);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(Vect vect) => UnderlyingTextInstance.ScaleBy(vect);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(float scalar) => UnderlyingTextInstance.AdjustScaleBy(scalar);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleBy(Vect vect) => UnderlyingTextInstance.AdjustScaleBy(vect);

	/// <summary>
	/// Disposes the underlying text object, removing this text from any scene it is in.
	/// </summary>
	/// <remarks>
	/// The pen and prepared string are not disposed; they belong to the font and may still be in use elsewhere.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => UnderlyingTextInstance.Dispose();

	/// <inheritdoc />
	public override string ToString() => UnderlyingTextInstance.ToString();

	#region Equality
	/// <inheritdoc />
	public bool Equals(CameraLockedTextInstance other) => UnderlyingTextInstance.Equals(other.UnderlyingTextInstance);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is CameraLockedTextInstance other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => UnderlyingTextInstance.GetHashCode();
	/// <summary>
	/// Returns whether the two given camera-locked text objects wrap the same underlying text object.
	/// </summary>
	/// <param name="left">The first text object to compare.</param>
	/// <param name="right">The second text object to compare.</param>
	public static bool operator ==(CameraLockedTextInstance left, CameraLockedTextInstance right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given camera-locked text objects wrap different underlying text objects.
	/// </summary>
	/// <param name="left">The first text object to compare.</param>
	/// <param name="right">The second text object to compare.</param>
	public static bool operator !=(CameraLockedTextInstance left, CameraLockedTextInstance right) => !left.Equals(right);
	#endregion
}
