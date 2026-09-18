// Created on 2026-07-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

/// <summary>
/// A string of text placed in a scene, free to be positioned and oriented in the 3D world like any other object.
/// </summary>
/// <remarks>
/// <para>
/// Use this for text that genuinely belongs in the world and should be seen edge-on when viewed from the side — a sign on a
/// wall, or writing on the ground. For text that should always face the camera, use <see cref="CameraLockedTextInstance"/>
/// instead.
/// </para>
/// <para>
/// Use <see cref="SetTransform(Location, Direction, Direction?, TextLayout)"/> to easily set the position + scale of this
/// text instance in-world.
/// </para>
/// </remarks>
public readonly struct TextInstance : ITextInstance, IResourceSpecialization<TextInstance, ModelInstance>, IEquatable<TextInstance>, ITransformedSceneObject {
	/// <summary>
	/// The general-purpose model instance this text object is a specialized view of.
	/// </summary>
	public ModelInstance UnderlyingModelInstance { get; }
	
	/// <summary>
	/// The font this object's text is drawn with.
	/// </summary>
	public Font Font => String.Font;

	/// <inheritdoc />
	public FontPen Pen {
		get => UnderlyingModelInstance.Implementation.GetTextInstancePen(UnderlyingModelInstance.GetHandleWithoutDisposeCheck());
		set {
			UnderlyingModelInstance.SetMaterial(value.GetPenMaterial());
			UnderlyingModelInstance.Implementation.UpdateTextInstancePen(UnderlyingModelInstance.GetHandleWithoutDisposeCheck(), value);
		}
	}
	/// <summary>
	/// Sets <see cref="Pen"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="pen">The new value for <see cref="Pen"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPen(FontPen pen) => Pen = pen;
	
	/// <inheritdoc />
	public FontString String {
		get => UnderlyingModelInstance.Implementation.GetTextInstanceString(UnderlyingModelInstance.GetHandleWithoutDisposeCheck());
		set {
			UnderlyingModelInstance.SetMesh(value.GetStringMesh());
			UnderlyingModelInstance.Implementation.UpdateTextInstanceString(UnderlyingModelInstance.GetHandleWithoutDisposeCheck(), value);
		}
	}
	/// <summary>
	/// Sets <see cref="String"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="string">The new value for <see cref="String"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetString(FontString @string) => String = @string;
	
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
	/// How large this text is on each of its two axes, where <c>(1, 1)</c> is the size its layout gives it.
	/// </summary>
	/// <remarks>
	/// Text is flat, so only two axes are meaningful; setting this leaves the third axis at <c>1</c>.
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

	internal TextInstance(ModelInstance underlyingModelInstance) {
		UnderlyingModelInstance = underlyingModelInstance;
	}
	
	internal TextInstance(ModelInstance underlyingModelInstance, FontPen pen, FontString @string, TextLayout layout) {
		UnderlyingModelInstance = underlyingModelInstance;
		UnderlyingModelInstance.Implementation.SetTextInstanceInitialPenAndString(UnderlyingModelInstance.GetHandleWithoutDisposeCheck(), pen, @string, layout);
	}
	
	#region Specialization
	static IntPtr IResourceSpecialization<TextInstance, ModelInstance>.SpecializationTypeIdentifier => typeof(TextInstance).TypeHandle.Value;
	int IResourceSpecialization<TextInstance, ModelInstance>.SpecializationDataLength => 0;
	static void IResourceSpecialization<TextInstance, ModelInstance>.Smuggle(TextInstance resource, Span<byte> specializationDataBuffer, out ModelInstance outBaseResource, out ResourceStub? additionalResourceRef) {
		additionalResourceRef = null;
		outBaseResource = resource.UnderlyingModelInstance;
	}
	static TextInstance IResourceSpecialization<TextInstance, ModelInstance>.DeSmuggle(ModelInstance baseResource, ReadOnlySpan<byte> specializationDataBuffer, ResourceStub? additionalResourceRef) {
		return new(baseResource);	
	}
	#endregion
	
	/// <summary>
	/// Wraps an existing model instance as a <see cref="TextInstance"/>, without creating anything new.
	/// </summary>
	/// <remarks>
	/// Only use this for an instance that really was created to display text; nothing here verifies that it was.
	/// </remarks>
	/// <param name="underlyingModelInstance">The model instance to wrap.</param>
	/// <param name="pen">The pen the text is drawn with.</param>
	/// <param name="string">The prepared string the text displays.</param>
	/// <param name="layout">How the text is sized and anchored.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TextInstance FromPreviouslyAllocatedUnderlyingModelInstance(ModelInstance underlyingModelInstance, FontPen pen, FontString @string, TextLayout layout) => new(underlyingModelInstance, pen, @string, layout);

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
	/// Places and orients this text in one call, applying the given layout.
	/// </summary>
	/// <remarks>
	/// This is the convenient way to position text, as it works in terms of a facing direction and a layout rather than
	/// requiring a full transform to be assembled first. The layout is applied to the object as well as being used here, so
	/// later changes to the text re-use it.
	/// </remarks>
	/// <param name="position">Where to put the text.</param>
	/// <param name="facingDirection">Which way the text should face.</param>
	/// <param name="uprightDirection">Which way is "up" across the text, or <see langword="null"/> to derive one from <paramref name="facingDirection"/>. Must not be parallel to <paramref name="facingDirection"/>.</param>
	/// <param name="layout">How the text should be sized and anchored.</param>
	public void SetTransform(Location position, Direction facingDirection, Direction? uprightDirection, TextLayout layout) {
		UnderlyingModelInstance.Implementation.SetTextInstanceLayout(UnderlyingModelInstance.GetHandleWithoutDisposeCheck(), layout);
		
		var @string = String;
		var transform = @string.Font.GetTextInstanceTransform(@string.Size, position, facingDirection, uprightDirection, layout);
		UnderlyingModelInstance.SetTransform(transform);
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
	/// Disposes the underlying model instance, removing this text from any scene it is in.
	/// </summary>
	/// <remarks>
	/// The pen and prepared string are not disposed; they belong to the font and may still be in use elsewhere.
	/// </remarks>
	public void Dispose() => UnderlyingModelInstance.Dispose();

	/// <inheritdoc />
	public override string ToString() => $"Text {UnderlyingModelInstance}";

	#region Equality
	/// <inheritdoc />
	public bool Equals(TextInstance other) => UnderlyingModelInstance.Equals(other.UnderlyingModelInstance);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is TextInstance other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => UnderlyingModelInstance.GetHashCode();
	/// <summary>
	/// Returns whether the two given text objects wrap the same underlying model instance.
	/// </summary>
	/// <param name="left">The first text object to compare.</param>
	/// <param name="right">The second text object to compare.</param>
	public static bool operator ==(TextInstance left, TextInstance right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given text objects wrap different underlying model instances.
	/// </summary>
	/// <param name="left">The first text object to compare.</param>
	/// <param name="right">The second text object to compare.</param>
	public static bool operator !=(TextInstance left, TextInstance right) => !left.Equals(right);
	#endregion
}