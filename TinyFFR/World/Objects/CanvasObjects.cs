// Created on 2026-07-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// Something drawn flat on a <see cref="CanvasScene"/>, such as an image or a piece of text.
/// </summary>
/// <remarks>
/// <para>
/// Every canvas object is placed by an <i>anchor pair</i>: <see cref="CanvasAnchor"/> says which part of the canvas the position is measured from, and
/// <see cref="ObjectAnchor"/> says which part of the object is put there. Anchoring a button to the canvas's bottom-right corner by its own bottom-right corner, for
/// example, keeps it pinned to that corner however the window is resized.
/// </para>
/// <para>
/// Sizes and positions can be given either in pixels or as fractions of the canvas. Fractions generally produce layouts that survive a resize; pixels are better
/// where an element must stay a fixed size regardless of the window.
/// </para>
/// </remarks>
public interface ICanvasObject : IDisposable, IStringSpanNameEnabled, ITransformed2DSceneObject {
	/// <summary>
	/// The canvas this object is drawn on.
	/// </summary>
	CanvasScene Canvas { get; }

	/// <summary>
	/// The model instance that actually draws this object.
	/// </summary>
	/// <remarks>
	/// Exposed for the occasional case where an instance-level facility is needed that the canvas API does not surface. Note that this instance's transform is
	/// derived from the canvas layout, so setting it directly will be overwritten.
	/// </remarks>
	ModelInstance UnderlyingModelInstance { get; }

	/// <summary>
	/// How large this object actually ends up on the canvas, in pixels.
	/// </summary>
	/// <remarks>
	/// Distinct from the width and height properties, which may be left <see langword="null"/> to mean "size to fit the content"; this reports the size arrived at.
	/// </remarks>
	public XYPair<int> ActualSizePixels { get; }
	/// <summary>
	/// How large this object actually ends up on the canvas, as a fraction of the canvas size.
	/// </summary>
	public XYPair<float> ActualSizeFraction { get; }

	/// <summary>
	/// Docks this object inside another, so that it is positioned and sized relative to that object rather than to the canvas as a whole.
	/// </summary>
	/// <typeparam name="TCanvasObject">The kind of canvas object being docked in to.</typeparam>
	/// <param name="parent">The object to dock inside, or <see langword="null"/> to undock.</param>
	void SetDockParent<TCanvasObject>(TCanvasObject? parent) where TCanvasObject : struct, ICanvasObject;

	/// <summary>
	/// Returns whether the given canvas coordinate, in pixels, falls inside this object.
	/// </summary>
	/// <remarks>
	/// The purpose-built way to hit-test a canvas element. Pair it with <see cref="CanvasScene.ConvertRenderTargetCoordToLocal"/> to find out whether the user
	/// clicked on this object.
	/// </remarks>
	/// <param name="canvasLocalPixelCoord">The coordinate to test, in canvas pixels.</param>
	/// <param name="coordOrigin">Which corner of the canvas the coordinate is measured from (or the centre if <see cref="DiagonalOrientation2D.None"/>). Defaults to <see cref="DiagonalOrientation2D.UpLeft"/>.</param>
	bool Contains(XYPair<int> canvasLocalPixelCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft);
	/// <summary>
	/// Returns whether the given canvas coordinate, expressed as a fraction of the canvas size, falls inside this object.
	/// </summary>
	/// <param name="canvasLocalFractionCoord">The coordinate to test, where <c>1f</c> on an axis is the full extent of the canvas along that axis.</param>
	/// <param name="coordOrigin">Which corner of the canvas the coordinate is measured from (or the centre if <see cref="DiagonalOrientation2D.None"/>). Defaults to <see cref="DiagonalOrientation2D.UpLeft"/>.</param>
	bool Contains(XYPair<float> canvasLocalFractionCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft);

	/// <summary>
	/// Which corner or edge of the canvas (or its dock parent if set with <see cref="SetDockParent"/>) this object is positioned relative to (or the centre if <see cref="Orientation2D.None"/>).
	/// </summary>
	public Orientation2D CanvasAnchor { get; set; }
	/// <summary>
	/// Which corner or edge of this object is placed at its position (or the centre if <see cref="Orientation2D.None"/>), or <see langword="null"/> to mirror <see cref="CanvasAnchor"/>.
	/// </summary>
	public Orientation2D? ObjectAnchor { get; set; }

	/// <summary>
	/// Which layer this object is drawn on; objects on higher layers are drawn in front of those on lower ones.
	/// </summary>
	/// <remarks>
	/// Clamped to between <see cref="CanvasScene.LayerMin"/> and <see cref="CanvasScene.LayerMax"/>. Objects on the same layer are drawn in an unspecified order, so
	/// give elements that must overlap predictably distinct layers.
	/// </remarks>
	public int Layer { get; set; }
	/// <summary>
	/// Whether this object is drawn at all.
	/// </summary>
	/// <remarks>
	/// Hiding an object leaves it on the canvas with its layout intact, which is cheaper and simpler than removing and re-adding it.
	/// </remarks>
	public bool IsVisible { get; set; }

	/// <summary>
	/// Where this object sits, in pixels measured from <see cref="CanvasAnchor"/>.
	/// </summary>
	public XYPair<int> PositionPixels { get; set; }
	/// <summary>
	/// Where this object sits, as a fraction of the canvas size measured from <see cref="CanvasAnchor"/>.
	/// </summary>
	public XYPair<float> PositionFraction { get; set; }
	XYPair<float> IPositioned2DSceneObject.Position {
		get => PositionFraction;
		set => PositionFraction = value;
	}

	/// <summary>
	/// How wide this object is, in pixels, or <see langword="null"/> to size it to fit its content.
	/// </summary>
	public int? WidthPixels { get; set; }
	/// <summary>
	/// How tall this object is, in pixels, or <see langword="null"/> to size it to fit its content.
	/// </summary>
	public int? HeightPixels { get; set; }
	/// <summary>
	/// How wide this object is, in a fraction of the canvas width, or <see langword="null"/> to size it to fit its content.
	/// </summary>
	public float? WidthFraction { get; set; }
	/// <summary>
	/// How tall this object is, in a fraction of the canvas height, or <see langword="null"/> to size it to fit its content.
	/// </summary>
	public float? HeightFraction { get; set; }
	XYPair<float> IScaled2DSceneObject.Scaling {
		get => new(WidthFraction ?? 0f, HeightFraction ?? 0f);
		set {
			WidthFraction = value.X;
			HeightFraction = value.Y;
		}
	}

	/// <summary>
	/// Moves this object by the given number of pixels, relative to where it currently is.
	/// </summary>
	/// <param name="translation">How far to move this object, in pixels.</param>
	void MoveByPixels(XYPair<int> translation);
	/// <summary>
	/// Moves this object by the given fraction of the canvas size, relative to where it currently is.
	/// </summary>
	/// <param name="translation">How far to move this object, as a fraction of the canvas size.</param>
	void MoveByFraction(XYPair<float> translation);
	void IMovable2DSceneObject.MoveBy(XYPair<float> translation) => MoveByFraction(translation);

	Transform2D ITransformed2DSceneObject.Transform {
		get => new(PositionFraction, Rotation, new XYPair<float>(WidthFraction ?? 0f, HeightFraction ?? 0f));
		set {
			WidthFraction = value.Scaling.X;
			HeightFraction = value.Scaling.Y;
			Rotation = value.Rotation;
			PositionFraction = value.Translation;
		}
	}

	/// <summary>
	/// Grows this object by the given number of pixels on both axes.
	/// </summary>
	/// <param name="scalar">How many pixels to add to this object's size on each axis. May be negative, to shrink it.</param>
	void AdjustScaleByPixels(int scalar);
	/// <summary>
	/// Grows this object by the given number of pixels, each axis independently.
	/// </summary>
	/// <param name="vect">How many pixels to add to this object's size on each axis. Components may be negative, to shrink it.</param>
	void AdjustScaleByPixels(XYPair<int> vect);
	/// <summary>
	/// Grows this object by the given fraction of the canvas size on both axes.
	/// </summary>
	/// <param name="scalar">How much to add to this object's size on each axis, as a fraction of the canvas size. May be negative, to shrink it.</param>
	void AdjustScaleByFraction(float scalar);
	/// <summary>
	/// Grows this object by the given fraction of the canvas size, each axis independently.
	/// </summary>
	/// <param name="vect">How much to add to this object's size on each axis, as a fraction of the canvas size. Components may be negative, to shrink it.</param>
	void AdjustScaleByFraction(XYPair<float> vect);
	void IRescalable2DSceneObject.AdjustScaleBy(float scalar) => AdjustScaleByFraction(scalar);
	void IRescalable2DSceneObject.AdjustScaleBy(XYPair<float> vect) => AdjustScaleByFraction(vect);

	/// <summary>
	/// Rotates this object around a pivot point given in canvas pixels, which moves it as well as turning it.
	/// </summary>
	/// <param name="rotation">How far to rotate this object. Positive angles rotate it anticlockwise.</param>
	/// <param name="pivotPointPixels">The point to rotate around, in canvas pixels.</param>
	void RotateBy(Angle rotation, XYPair<int> pivotPointPixels);
}

/// <summary>
/// An <see cref="ICanvasObject"/> that knows both its own concrete type and the resource type underlying it.
/// </summary>
/// <typeparam name="TSelf">The implementing type itself.</typeparam>
/// <typeparam name="TBase">The resource type this canvas object is a specialization of.</typeparam>
public interface ICanvasObject<TSelf, TBase> : ICanvasObject, IResourceSpecialization<TSelf, TBase> where TSelf : struct, ICanvasObject<TSelf, TBase> where TBase : IResource<TBase>;

/// <summary>
/// An image drawn flat on a <see cref="CanvasScene"/>. Created by adding a texture or material to a <see cref="CanvasScene"/>.
/// </summary>
/// <remarks>
/// As well as the placement members every canvas object has, this offers control over which part of its source image is shown (the texture offset and extent
/// properties) and how much of its own area is filled (<see cref="FillFraction"/>). Between them these are enough to build icons from a packed sheet, progress
/// bars and similar interface elements without separate images for each.
/// </remarks>
public readonly record struct CanvasTexture : ICanvasObject<CanvasTexture, ModelInstance> {
	/// <inheritdoc />
	public CanvasScene Canvas { get; }
	/// <summary>
	/// The quad instance that actually draws this object.
	/// </summary>
	/// <remarks>
	/// You shouldn't need to use this property for most operations.
	/// </remarks>
	public QuadInstance UnderlyingQuadInstance { get; }

	ISceneImplProvider Implementation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Canvas.UnderlyingScene.Implementation;
	}
	ResourceHandle<Scene> SceneHandle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Canvas.UnderlyingScene.GetHandleWithoutDisposeCheck();
	}
	/// <inheritdoc />
	public ModelInstance UnderlyingModelInstance {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingQuadInstance.UnderlyingModelInstance;
	}
	ModelInstance Instance {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance;
	}

	/// <inheritdoc />
	public XYPair<int> ActualSizePixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectActualSizePixels(SceneHandle, Instance);
	}
	/// <inheritdoc />
	public XYPair<float> ActualSizeFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectActualSizeFraction(SceneHandle, Instance);
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetDockParent<TCanvasObject>(TCanvasObject? parent) where TCanvasObject : struct, ICanvasObject {
		Implementation.SetCanvasObjectDockParent(SceneHandle, Instance, parent?.UnderlyingModelInstance);
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(XYPair<int> canvasLocalPixelCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft) {
		return Implementation.CanvasObjectContainsPixelCoord(SceneHandle, Instance, canvasLocalPixelCoord, coordOrigin);
	}
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(XYPair<float> canvasLocalFractionCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft) {
		return Contains(Implementation.ConvertCanvasFractionToPixels(SceneHandle, canvasLocalFractionCoord), coordOrigin);
	}

	/// <inheritdoc />
	public Orientation2D CanvasAnchor {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectCanvasAnchor(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectCanvasAnchor(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="CanvasAnchor"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="canvasAnchor">The new value for <see cref="CanvasAnchor"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetCanvasAnchor(Orientation2D canvasAnchor) => CanvasAnchor = canvasAnchor;
	
	/// <inheritdoc />
	public Orientation2D? ObjectAnchor {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectAnchor(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectAnchor(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="ObjectAnchor"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="objectAnchor">The new value for <see cref="ObjectAnchor"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetObjectAnchor(Orientation2D? objectAnchor) => ObjectAnchor = objectAnchor;
	/// <inheritdoc />
	public Angle Rotation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectRotation(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectRotation(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="Rotation"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="rotation">The new value for <see cref="Rotation"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotation(Angle rotation) => Rotation = rotation;
	/// <inheritdoc />
	public int Layer {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectLayer(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectLayer(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="Layer"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="layer">The new value for <see cref="Layer"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetLayer(int layer) => Layer = layer;
	/// <inheritdoc />
	public bool IsVisible {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectVisibility(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectVisibility(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="IsVisible"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="isVisible">The new value for <see cref="IsVisible"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetIsVisible(bool isVisible) => IsVisible = isVisible;
	/// <inheritdoc />
	public XYPair<int> PositionPixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectPositionPixels(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectPositionPixels(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="PositionPixels"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="positionPixels">The new value for <see cref="PositionPixels"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPositionPixels(XYPair<int> positionPixels) => PositionPixels = positionPixels;
	/// <inheritdoc />
	public XYPair<float> PositionFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectPositionFraction(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectPositionFraction(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="PositionFraction"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="positionFraction">The new value for <see cref="PositionFraction"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPositionFraction(XYPair<float> positionFraction) => PositionFraction = positionFraction;
	/// <inheritdoc />
	public int? WidthPixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectWidthPixels(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectWidthPixels(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="WidthPixels"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="widthPixels">The new value for <see cref="WidthPixels"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetWidthPixels(int? widthPixels) => WidthPixels = widthPixels;
	/// <inheritdoc />
	public int? HeightPixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectHeightPixels(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectHeightPixels(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="HeightPixels"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="heightPixels">The new value for <see cref="HeightPixels"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetHeightPixels(int? heightPixels) => HeightPixels = heightPixels;
	/// <inheritdoc />
	public float? WidthFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectWidthFraction(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectWidthFraction(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="WidthFraction"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="widthFraction">The new value for <see cref="WidthFraction"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetWidthFraction(float? widthFraction) => WidthFraction = widthFraction;
	/// <inheritdoc />
	public float? HeightFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectHeightFraction(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectHeightFraction(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="HeightFraction"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="heightFraction">The new value for <see cref="HeightFraction"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetHeightFraction(float? heightFraction) => HeightFraction = heightFraction;

	internal CanvasTexture(CanvasScene canvas, QuadInstance underlyingQuadInstance) {
		Canvas = canvas;
		UnderlyingQuadInstance = underlyingQuadInstance;
	}
	
	#region Specialization
	static IntPtr IResourceSpecialization<CanvasTexture, ModelInstance>.SpecializationTypeIdentifier => typeof(CanvasTexture).TypeHandle.Value;
	int IResourceSpecialization<CanvasTexture, ModelInstance>.SpecializationDataLength => 0;
	static void IResourceSpecialization<CanvasTexture, ModelInstance>.Smuggle(CanvasTexture resource, Span<byte> specializationDataBuffer, out ModelInstance outBaseResource, out ResourceStub? additionalResourceRef) {
		additionalResourceRef = ((IResource<Scene>) resource.Canvas.UnderlyingScene).AsStub;
		outBaseResource = resource.UnderlyingQuadInstance.UnderlyingModelInstance;
	}
	static CanvasTexture IResourceSpecialization<CanvasTexture, ModelInstance>.DeSmuggle(ModelInstance baseResource, ReadOnlySpan<byte> specializationDataBuffer, ResourceStub? additionalResourceRef) {
		return new(new CanvasScene(TypeUtils.StubToResource<Scene>(additionalResourceRef!.Value)), new QuadInstance(baseResource));	
	}
	#endregion
	
	/// <summary>
	/// Sets this object’s anchor, position and size together, in pixels.
	/// </summary>
	/// <param name="canvasAnchor">Which corner or edge of the canvas the position is measured from (or the centre if <see cref="Orientation2D.None"/>).</param>
	/// <param name="position">Where to place this object, in pixels measured from <paramref name="canvasAnchor"/>.</param>
	/// <param name="size">How large this object should be, in pixels.</param>
	/// <param name="objectAnchor">Which part of this object is placed at <paramref name="position"/> (or the centre if <see cref="Orientation2D.None"/>). If <see langword="null"/> (the default), mirrors <paramref name="canvasAnchor"/>.</param>
	public void SetPlacementPixels(Orientation2D canvasAnchor, XYPair<int> position, XYPair<int> size, Orientation2D? objectAnchor = null) {
		Implementation.SetCanvasObjectPlacement(SceneHandle, Instance, canvasAnchor, objectAnchor, position, XYPair<float>.Zero, size.X, size.Y, null, null);
	}
	/// <summary>
	/// Sets this object’s anchor, position and size together, as fractions of the canvas size.
	/// </summary>
	/// <param name="canvasAnchor">Which corner or edge of the canvas the position is measured from (or the centre if <see cref="Orientation2D.None"/>).</param>
	/// <param name="position">Where to place this object, as a fraction of the canvas size measured from <paramref name="canvasAnchor"/>.</param>
	/// <param name="size">How large this object should be, as a fraction of the canvas size.</param>
	/// <param name="objectAnchor">Which part of this object is placed at <paramref name="position"/> (or the centre if <see cref="Orientation2D.None"/>). If <see langword="null"/> (the default), mirrors <paramref name="canvasAnchor"/>.</param>
	public void SetPlacementFraction(Orientation2D canvasAnchor, XYPair<float> position, XYPair<float> size, Orientation2D? objectAnchor = null) {
		Implementation.SetCanvasObjectPlacement(SceneHandle, Instance, canvasAnchor, objectAnchor, XYPair<int>.Zero, position, null, null, size.X, size.Y);
	}

	/// <summary>
	/// Replaces the image this object draws.
	/// </summary>
	/// <param name="t">The texture to draw.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTexture(Texture t) => Implementation.SetCanvasObjectTexture(SceneHandle, UnderlyingQuadInstance, t);
	/// <summary>
	/// How much of this object’s area is actually filled with its image, from <c>0f</c> to <c>1f</c> on each axis.
	/// </summary>
	/// <remarks>
	/// Reducing this reveals only part of the object, anchored at its edge. This is the usual way to build a progress bar without resizing the element itself.
	/// </remarks>
	public XYPair<float> FillFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectFillFraction(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectFillFraction(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="FillFraction"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="fillFraction">The new value for <see cref="FillFraction"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetFillFraction(XYPair<float> fillFraction) => FillFraction = fillFraction;
	/// <summary>
	/// The size of this object’s source image, in pixels.
	/// </summary>
	public XYPair<int> TextureDimensions {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectTextureDimensions(SceneHandle, UnderlyingQuadInstance);
	}
	/// <summary>
	/// Which point in the source image is drawn at this object’s origin, in pixels.
	/// </summary>
	/// <remarks>
	/// Together with the texture extent this selects a sub-rectangle of the source image, which is how several icons packed in to one image are drawn separately.
	/// </remarks>
	public XYPair<int> TextureOffsetPixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TextureDimensions.ScaledByReal(TextureOffsetFraction);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectTextureOffsetPixels(SceneHandle, UnderlyingQuadInstance, value);
	}
	/// <summary>
	/// Sets <see cref="TextureOffsetPixels"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="offset">The new value for <see cref="TextureOffsetPixels"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTextureOffsetPixels(XYPair<int> offset) => TextureOffsetPixels = offset;
	/// <summary>
	/// Which point in the source image is drawn at this object’s origin, as a fraction of the image size.
	/// </summary>
	public XYPair<float> TextureOffsetFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectTextureOffset(SceneHandle, UnderlyingQuadInstance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectTextureOffset(SceneHandle, UnderlyingQuadInstance, value);
	}
	/// <summary>
	/// Sets <see cref="TextureOffsetFraction"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="offset">The new value for <see cref="TextureOffsetFraction"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTextureOffsetFraction(XYPair<float> offset) => TextureOffsetFraction = offset;
	/// <summary>
	/// How much of the source image is drawn, in pixels, starting from the texture offset.
	/// </summary>
	public XYPair<int> TextureExtentPixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TextureDimensions.ScaledByReal(TextureExtentFraction);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectTextureExtentPixels(SceneHandle, UnderlyingQuadInstance, value);
	}
	/// <summary>
	/// Sets <see cref="TextureExtentPixels"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="extent">The new value for <see cref="TextureExtentPixels"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTextureExtentPixels(XYPair<int> extent) => TextureExtentPixels = extent;
	/// <summary>
	/// How much of the source image is drawn, as a fraction of the image size, starting from the texture offset.
	/// </summary>
	public XYPair<float> TextureExtentFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectTextureExtent(SceneHandle, UnderlyingQuadInstance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectTextureExtent(SceneHandle, UnderlyingQuadInstance, value);
	}
	/// <summary>
	/// Sets <see cref="TextureExtentFraction"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="extent">The new value for <see cref="TextureExtentFraction"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTextureExtentFraction(XYPair<float> extent) => TextureExtentFraction = extent;
	/// <summary>
	/// Sets a second texture blended over this object’s own.
	/// </summary>
	/// <param name="blendTex">The texture to blend over this object.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBlendTexture(Texture blendTex) => Implementation.SetCanvasBlendTexture(SceneHandle, UnderlyingQuadInstance, blendTex);
	/// <summary>
	/// Sets how strongly the blend texture is mixed over this object’s own texture.
	/// </summary>
	/// <param name="distance">How far to blend towards the blend texture, where <c>0f</c> shows only the original and <c>1f</c> shows only the blend texture.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBlendTextureDistance(float distance) => Implementation.SetCanvasBlendTextureDistance(SceneHandle, UnderlyingQuadInstance, distance);
	/// <summary>
	/// How opaque this object is, from <c>0f</c> (fully transparent) to <c>1f</c> (fully opaque).
	/// </summary>
	public float Opacity {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectOpacity(SceneHandle, UnderlyingQuadInstance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectOpacity(SceneHandle, UnderlyingQuadInstance, value);
	}
	/// <summary>
	/// Sets <see cref="Opacity"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="opacity">The new value for <see cref="Opacity"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetOpacity(float opacity) => Opacity = opacity;

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(float scalar) => Implementation.ScaleCanvasObjectBy(SceneHandle, Instance, scalar);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(XYPair<float> vect) => Implementation.ScaleCanvasObjectBy(SceneHandle, Instance, vect);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleByPixels(int scalar) => Implementation.AdjustCanvasObjectScaleByPixels(SceneHandle, Instance, new XYPair<int>(scalar));
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleByPixels(XYPair<int> vect) => Implementation.AdjustCanvasObjectScaleByPixels(SceneHandle, Instance, vect);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleByFraction(float scalar) => Implementation.AdjustCanvasObjectScaleByFraction(SceneHandle, Instance, new XYPair<float>(scalar));
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleByFraction(XYPair<float> vect) => Implementation.AdjustCanvasObjectScaleByFraction(SceneHandle, Instance, vect);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Angle rotation) => Implementation.RotateCanvasObjectBy(SceneHandle, Instance, rotation);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Angle rotation, XYPair<float> pivotPoint) => Implementation.RotateCanvasObjectBy(SceneHandle, Instance, rotation, pivotPoint);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Angle rotation, XYPair<int> pivotPointPixels) => Implementation.RotateCanvasObjectBy(SceneHandle, Instance, rotation, pivotPointPixels);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveByPixels(XYPair<int> translation) => Implementation.MoveCanvasObjectByPixels(SceneHandle, Instance, translation);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveByFraction(XYPair<float> translation) => Implementation.MoveCanvasObjectByFraction(SceneHandle, Instance, translation);

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
	public void Dispose() => Implementation.DisposeCanvasObject(SceneHandle, Instance);

	/// <inheritdoc />
	public override string ToString() => $"Canvas Texture {UnderlyingQuadInstance.UnderlyingModelInstance}";
}

/// <summary>
/// A piece of text drawn flat on a <see cref="CanvasScene"/>. Created by adding text to a <see cref="CanvasScene"/>.
/// </summary>
/// <remarks>
/// By default the height of the element grows with the number of lines the text occupies; set
/// <see cref="DisableAutomaticLineCountBasedHeightScaling"/> to keep the height fixed instead.
/// </remarks>
public readonly record struct CanvasText : ICanvasObject<CanvasText, ModelInstance> {
	/// <inheritdoc />
	public CanvasScene Canvas { get; }
	/// <summary>
	/// The text instance that actually draws this object.
	/// </summary>
	/// <remarks>
	/// You shouldn't need to use this property for most operations.
	/// </remarks>
	public TextInstance UnderlyingTextInstance { get; }

	ISceneImplProvider Implementation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Canvas.UnderlyingScene.Implementation;
	}
	ResourceHandle<Scene> SceneHandle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Canvas.UnderlyingScene.GetHandleWithoutDisposeCheck();
	}
	/// <inheritdoc />
	public ModelInstance UnderlyingModelInstance {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingTextInstance.UnderlyingModelInstance;
	}
	ModelInstance Instance {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance;
	}

	/// <inheritdoc />
	public XYPair<int> ActualSizePixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectActualSizePixels(SceneHandle, Instance);
	}
	/// <inheritdoc />
	public XYPair<float> ActualSizeFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectActualSizeFraction(SceneHandle, Instance);
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetDockParent<TCanvasObject>(TCanvasObject? parent) where TCanvasObject : struct, ICanvasObject {
		Implementation.SetCanvasObjectDockParent(SceneHandle, Instance, parent?.UnderlyingModelInstance);
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(XYPair<int> canvasLocalPixelCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft) {
		return Implementation.CanvasObjectContainsPixelCoord(SceneHandle, Instance, canvasLocalPixelCoord, coordOrigin);
	}
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(XYPair<float> canvasLocalFractionCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft) {
		return Contains(Implementation.ConvertCanvasFractionToPixels(SceneHandle, canvasLocalFractionCoord), coordOrigin);
	}

	/// <summary>
	/// Replaces the text this object draws with a pre-built string.
	/// </summary>
	/// <param name="string">The text to draw.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetText(FontString @string) => Implementation.SetCanvasTextString(SceneHandle, UnderlyingTextInstance, @string);
	/// <summary>
	/// Replaces the text this object draws.
	/// </summary>
	/// <param name="str">The text to draw.</param>
	/// <param name="multiLineJustification">How lines are aligned with one another when the text spans more than one line. Defaults to <see cref="TextJustification.Center"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetText(ReadOnlySpan<char> str, TextJustification multiLineJustification = TextJustification.Center) => Implementation.SetCanvasTextString(SceneHandle, UnderlyingTextInstance, str, multiLineJustification);

	/// <summary>
	/// The pen used to render this text object.
	/// </summary>
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
	
	/// <summary>
	/// Whether to stop this object's height growing automatically with the number of lines its text occupies.
	/// </summary>
	/// <remarks>
	/// By default a text element gets taller as its text wraps on to more lines, which is what you want for a label that should always show all its text. Set this to
	/// <see langword="true"/> where the element must keep a fixed height instead (for a button, say), accepting that multi-line text may be squashed.
	/// </remarks>
	public bool DisableAutomaticLineCountBasedHeightScaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasTextAutomaticLineCountScalingDisabled(SceneHandle, UnderlyingTextInstance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasTextAutomaticLineCountScalingDisabled(SceneHandle, UnderlyingTextInstance, value);
	}
	/// <summary>
	/// Sets <see cref="DisableAutomaticLineCountBasedHeightScaling"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="disableAutomaticLineCountBasedHeightScaling">The new value for <see cref="DisableAutomaticLineCountBasedHeightScaling"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetDisableAutomaticLineCountBasedHeightScaling(bool disableAutomaticLineCountBasedHeightScaling) => DisableAutomaticLineCountBasedHeightScaling = disableAutomaticLineCountBasedHeightScaling;

	/// <summary>
	/// Describes how this object's text is actually laid out, such as where its lines fall.
	/// </summary>
	public TextLayout Layout {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasTextLayout(SceneHandle, UnderlyingTextInstance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasTextLayout(SceneHandle, UnderlyingTextInstance, value);
	}
	/// <summary>
	/// Sets <see cref="Layout"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="layout">The new value for <see cref="Layout"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetLayout(TextLayout layout) => Layout = layout;

	/// <inheritdoc />
	public Orientation2D CanvasAnchor {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectCanvasAnchor(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectCanvasAnchor(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="CanvasAnchor"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="canvasAnchor">The new value for <see cref="CanvasAnchor"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetCanvasAnchor(Orientation2D canvasAnchor) => CanvasAnchor = canvasAnchor;

	/// <inheritdoc />
	public Orientation2D? ObjectAnchor {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectAnchor(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectAnchor(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="ObjectAnchor"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="objectAnchor">The new value for <see cref="ObjectAnchor"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetObjectAnchor(Orientation2D? objectAnchor) => ObjectAnchor = objectAnchor;
	/// <inheritdoc />
	public Angle Rotation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectRotation(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectRotation(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="Rotation"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="rotation">The new value for <see cref="Rotation"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotation(Angle rotation) => Rotation = rotation;
	/// <inheritdoc />
	public int Layer {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectLayer(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectLayer(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="Layer"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="layer">The new value for <see cref="Layer"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetLayer(int layer) => Layer = layer;
	/// <inheritdoc />
	public bool IsVisible {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectVisibility(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectVisibility(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="IsVisible"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="isVisible">The new value for <see cref="IsVisible"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetIsVisible(bool isVisible) => IsVisible = isVisible;
	/// <inheritdoc />
	public XYPair<int> PositionPixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectPositionPixels(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectPositionPixels(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="PositionPixels"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="positionPixels">The new value for <see cref="PositionPixels"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPositionPixels(XYPair<int> positionPixels) => PositionPixels = positionPixels;
	/// <inheritdoc />
	public XYPair<float> PositionFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectPositionFraction(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectPositionFraction(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="PositionFraction"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="positionFraction">The new value for <see cref="PositionFraction"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPositionFraction(XYPair<float> positionFraction) => PositionFraction = positionFraction;
	/// <inheritdoc />
	public int? WidthPixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectWidthPixels(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectWidthPixels(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="WidthPixels"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="widthPixels">The new value for <see cref="WidthPixels"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetWidthPixels(int? widthPixels) => WidthPixels = widthPixels;
	/// <inheritdoc />
	public int? HeightPixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectHeightPixels(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectHeightPixels(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="HeightPixels"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="heightPixels">The new value for <see cref="HeightPixels"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetHeightPixels(int? heightPixels) => HeightPixels = heightPixels;
	/// <inheritdoc />
	public float? WidthFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectWidthFraction(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectWidthFraction(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="WidthFraction"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="widthFraction">The new value for <see cref="WidthFraction"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetWidthFraction(float? widthFraction) => WidthFraction = widthFraction;
	/// <inheritdoc />
	public float? HeightFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectHeightFraction(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectHeightFraction(SceneHandle, Instance, value);
	}
	/// <summary>
	/// Sets <see cref="HeightFraction"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="heightFraction">The new value for <see cref="HeightFraction"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetHeightFraction(float? heightFraction) => HeightFraction = heightFraction;

	internal CanvasText(CanvasScene canvas, TextInstance underlyingTextInstance) {
		Canvas = canvas;
		UnderlyingTextInstance = underlyingTextInstance;
	}
	
	#region Specialization
	static IntPtr IResourceSpecialization<CanvasText, ModelInstance>.SpecializationTypeIdentifier => typeof(CanvasText).TypeHandle.Value;
	int IResourceSpecialization<CanvasText, ModelInstance>.SpecializationDataLength => 0;
	static void IResourceSpecialization<CanvasText, ModelInstance>.Smuggle(CanvasText resource, Span<byte> specializationDataBuffer, out ModelInstance outBaseResource, out ResourceStub? additionalResourceRef) {
		additionalResourceRef = ((IResource<Scene>) resource.Canvas.UnderlyingScene).AsStub;
		outBaseResource = resource.UnderlyingTextInstance.UnderlyingModelInstance;
	}
	static CanvasText IResourceSpecialization<CanvasText, ModelInstance>.DeSmuggle(ModelInstance baseResource, ReadOnlySpan<byte> specializationDataBuffer, ResourceStub? additionalResourceRef) {
		return new(new CanvasScene(TypeUtils.StubToResource<Scene>(additionalResourceRef!.Value)), new TextInstance(baseResource));	
	}
	#endregion
	
	/// <summary>
	/// Sets this object’s anchor, position and size together, in pixels.
	/// </summary>
	/// <param name="canvasAnchor">Which corner or edge of the canvas the position is measured from (or the centre if <see cref="Orientation2D.None"/>).</param>
	/// <param name="position">Where to place this object, in pixels measured from <paramref name="canvasAnchor"/>.</param>
	/// <param name="fontHeight">How tall the text should be drawn, in pixels.</param>
	/// <param name="objectAnchor">Which part of this object is placed at <paramref name="position"/> (or the centre if <see cref="Orientation2D.None"/>). If <see langword="null"/> (the default), mirrors <paramref name="canvasAnchor"/>.</param>
	public void SetPlacementPixels(Orientation2D canvasAnchor, XYPair<int> position, int fontHeight, Orientation2D? objectAnchor = null) {
		Implementation.SetCanvasObjectPlacement(SceneHandle, Instance, canvasAnchor, objectAnchor, position, XYPair<float>.Zero, null, fontHeight, null, null);
	}
	/// <summary>
	/// Sets this object’s anchor, position and size together, as fractions of the canvas size.
	/// </summary>
	/// <param name="canvasAnchor">Which corner or edge of the canvas the position is measured from (or the centre if <see cref="Orientation2D.None"/>).</param>
	/// <param name="position">Where to place this object, as a fraction of the canvas size measured from <paramref name="canvasAnchor"/>.</param>
	/// <param name="fontHeight">How tall the text should be drawn, as a fraction of the canvas height.</param>
	/// <param name="objectAnchor">Which part of this object is placed at <paramref name="position"/> (or the centre if <see cref="Orientation2D.None"/>). If <see langword="null"/> (the default), mirrors <paramref name="canvasAnchor"/>.</param>
	public void SetPlacementFraction(Orientation2D canvasAnchor, XYPair<float> position, float fontHeight, Orientation2D? objectAnchor = null) {
		Implementation.SetCanvasObjectPlacement(SceneHandle, Instance, canvasAnchor, objectAnchor, XYPair<int>.Zero, position, null, null, null, fontHeight);
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(float scalar) => Implementation.ScaleCanvasObjectBy(SceneHandle, Instance, scalar);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(XYPair<float> vect) => Implementation.ScaleCanvasObjectBy(SceneHandle, Instance, vect);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleByPixels(int scalar) => Implementation.AdjustCanvasObjectScaleByPixels(SceneHandle, Instance, new XYPair<int>(scalar));
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleByPixels(XYPair<int> vect) => Implementation.AdjustCanvasObjectScaleByPixels(SceneHandle, Instance, vect);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleByFraction(float scalar) => Implementation.AdjustCanvasObjectScaleByFraction(SceneHandle, Instance, new XYPair<float>(scalar));
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleByFraction(XYPair<float> vect) => Implementation.AdjustCanvasObjectScaleByFraction(SceneHandle, Instance, vect);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Angle rotation) => Implementation.RotateCanvasObjectBy(SceneHandle, Instance, rotation);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Angle rotation, XYPair<float> pivotPoint) => Implementation.RotateCanvasObjectBy(SceneHandle, Instance, rotation, pivotPoint);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Angle rotation, XYPair<int> pivotPointPixels) => Implementation.RotateCanvasObjectBy(SceneHandle, Instance, rotation, pivotPointPixels);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveByPixels(XYPair<int> translation) => Implementation.MoveCanvasObjectByPixels(SceneHandle, Instance, translation);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveByFraction(XYPair<float> translation) => Implementation.MoveCanvasObjectByFraction(SceneHandle, Instance, translation);

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
	public void Dispose() => Implementation.DisposeCanvasObject(SceneHandle, Instance);

	/// <inheritdoc />
	public override string ToString() => $"Canvas {UnderlyingTextInstance}";
}
