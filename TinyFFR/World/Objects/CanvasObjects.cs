// Created on 2026-07-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

public interface ICanvasObject : IDisposable, IStringSpanNameEnabled, ITransformed2DSceneObject {
	CanvasScene Canvas { get; }

	ModelInstance UnderlyingModelInstance { get; }

	public XYPair<int> ActualSizePixels { get; }
	public XYPair<float> ActualSizeFraction { get; }

	void SetDockParent<TCanvasObject>(TCanvasObject? parent) where TCanvasObject : struct, ICanvasObject;

	bool Contains(XYPair<int> canvasLocalPixelCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft);
	bool Contains(XYPair<float> canvasLocalFractionCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft);

	public Orientation2D CanvasAnchor { get; set; }
	public Orientation2D? ObjectAnchor { get; set; }

	public int Layer { get; set; }
	public bool IsVisible { get; set; }

	public XYPair<int> PositionPixels { get; set; }
	public XYPair<float> PositionFraction { get; set; }
	XYPair<float> IPositioned2DSceneObject.Position {
		get => PositionFraction;
		set => PositionFraction = value;
	}

	public int? WidthPixels { get; set; }
	public int? HeightPixels { get; set; }
	public float? WidthFraction { get; set; }
	public float? HeightFraction { get; set; }
	XYPair<float> IScaled2DSceneObject.Scaling {
		get => new(WidthFraction ?? 0f, HeightFraction ?? 0f);
		set {
			WidthFraction = value.X;
			HeightFraction = value.Y;
		}
	}

	void MoveByPixels(XYPair<int> translation);
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

	void AdjustScaleByPixels(int scalar);
	void AdjustScaleByPixels(XYPair<int> vect);
	void AdjustScaleByFraction(float scalar);
	void AdjustScaleByFraction(XYPair<float> vect);
	void IRescalable2DSceneObject.AdjustScaleBy(float scalar) => AdjustScaleByFraction(scalar);
	void IRescalable2DSceneObject.AdjustScaleBy(XYPair<float> vect) => AdjustScaleByFraction(vect);

	void RotateBy(Angle rotation, XYPair<int> pivotPointPixels);
}

public readonly record struct CanvasTexture : ICanvasObject {
	public CanvasScene Canvas { get; }
	public QuadInstance UnderlyingQuadInstance { get; }

	ISceneImplProvider Implementation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Canvas.UnderlyingScene.Implementation;
	}
	ResourceHandle<Scene> SceneHandle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Canvas.UnderlyingScene.GetHandleWithoutDisposeCheck();
	}
	public ModelInstance UnderlyingModelInstance {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingQuadInstance.UnderlyingModelInstance;
	}
	ModelInstance Instance {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance;
	}

	public XYPair<int> ActualSizePixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectActualSizePixels(SceneHandle, Instance);
	}
	public XYPair<float> ActualSizeFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectActualSizeFraction(SceneHandle, Instance);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetDockParent<TCanvasObject>(TCanvasObject? parent) where TCanvasObject : struct, ICanvasObject {
		Implementation.SetCanvasObjectDockParent(SceneHandle, Instance, parent?.UnderlyingModelInstance);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(XYPair<int> canvasLocalPixelCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft) {
		return Implementation.CanvasObjectContainsPixelCoord(SceneHandle, Instance, canvasLocalPixelCoord, coordOrigin);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(XYPair<float> canvasLocalFractionCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft) {
		return Contains(Implementation.ConvertCanvasFractionToPixels(SceneHandle, canvasLocalFractionCoord), coordOrigin);
	}

	public Orientation2D CanvasAnchor {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectCanvasAnchor(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectCanvasAnchor(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetCanvasAnchor(Orientation2D canvasAnchor) => CanvasAnchor = canvasAnchor;
	
	public Orientation2D? ObjectAnchor {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectAnchor(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectAnchor(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetObjectAnchor(Orientation2D? objectAnchor) => ObjectAnchor = objectAnchor;
	public Angle Rotation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectRotation(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectRotation(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotation(Angle rotation) => Rotation = rotation;
	public int Layer {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectLayer(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectLayer(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetLayer(int layer) => Layer = layer;
	public bool IsVisible {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectVisibility(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectVisibility(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetIsVisible(bool isVisible) => IsVisible = isVisible;
	public XYPair<int> PositionPixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectPositionPixels(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectPositionPixels(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPositionPixels(XYPair<int> positionPixels) => PositionPixels = positionPixels;
	public XYPair<float> PositionFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectPositionFraction(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectPositionFraction(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPositionFraction(XYPair<float> positionFraction) => PositionFraction = positionFraction;
	public int? WidthPixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectWidthPixels(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectWidthPixels(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetWidthPixels(int? widthPixels) => WidthPixels = widthPixels;
	public int? HeightPixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectHeightPixels(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectHeightPixels(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetHeightPixels(int? heightPixels) => HeightPixels = heightPixels;
	public float? WidthFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectWidthFraction(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectWidthFraction(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetWidthFraction(float? widthFraction) => WidthFraction = widthFraction;
	public float? HeightFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectHeightFraction(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectHeightFraction(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetHeightFraction(float? heightFraction) => HeightFraction = heightFraction;

	internal CanvasTexture(CanvasScene canvas, QuadInstance underlyingQuadInstance) {
		Canvas = canvas;
		UnderlyingQuadInstance = underlyingQuadInstance;
	}
	
	public void SetPlacementPixels(Orientation2D canvasAnchor, XYPair<int> position, XYPair<int> size, Orientation2D? objectAnchor = null) {
		Implementation.SetCanvasObjectPlacement(SceneHandle, Instance, canvasAnchor, objectAnchor, position, XYPair<float>.Zero, size.X, size.Y, null, null);
	}
	public void SetPlacementFraction(Orientation2D canvasAnchor, XYPair<float> position, XYPair<float> size, Orientation2D? objectAnchor = null) {
		Implementation.SetCanvasObjectPlacement(SceneHandle, Instance, canvasAnchor, objectAnchor, XYPair<int>.Zero, position, null, null, size.X, size.Y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTexture(Texture t) => Implementation.SetCanvasObjectTexture(SceneHandle, UnderlyingQuadInstance, t);
	public XYPair<float> FillFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectFillFraction(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectFillFraction(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetFillFraction(XYPair<float> fillFraction) => FillFraction = fillFraction;
	public XYPair<int> TextureDimensions {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectTextureDimensions(SceneHandle, UnderlyingQuadInstance);
	}
	public XYPair<int> TextureOffsetPixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TextureDimensions.ScaledByReal(TextureOffsetFraction);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectTextureOffsetPixels(SceneHandle, UnderlyingQuadInstance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTextureOffsetPixels(XYPair<int> offset) => TextureOffsetPixels = offset;
	public XYPair<float> TextureOffsetFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectTextureOffset(SceneHandle, UnderlyingQuadInstance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectTextureOffset(SceneHandle, UnderlyingQuadInstance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTextureOffsetFraction(XYPair<float> offset) => TextureOffsetFraction = offset;
	public XYPair<int> TextureExtentPixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => TextureDimensions.ScaledByReal(TextureExtentFraction);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectTextureExtentPixels(SceneHandle, UnderlyingQuadInstance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTextureExtentPixels(XYPair<int> extent) => TextureExtentPixels = extent;
	public XYPair<float> TextureExtentFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectTextureExtent(SceneHandle, UnderlyingQuadInstance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectTextureExtent(SceneHandle, UnderlyingQuadInstance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTextureExtentFraction(XYPair<float> extent) => TextureExtentFraction = extent;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBlendTexture(Texture blendTex) => Implementation.SetCanvasBlendTexture(SceneHandle, UnderlyingQuadInstance, blendTex);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBlendTextureDistance(float distance) => Implementation.SetCanvasBlendTextureDistance(SceneHandle, UnderlyingQuadInstance, distance);
	public float Opacity {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectOpacity(SceneHandle, UnderlyingQuadInstance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectOpacity(SceneHandle, UnderlyingQuadInstance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetOpacity(float opacity) => Opacity = opacity;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(float scalar) => Implementation.ScaleCanvasObjectBy(SceneHandle, Instance, scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(XYPair<float> vect) => Implementation.ScaleCanvasObjectBy(SceneHandle, Instance, vect);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleByPixels(int scalar) => Implementation.AdjustCanvasObjectScaleByPixels(SceneHandle, Instance, new XYPair<int>(scalar));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleByPixels(XYPair<int> vect) => Implementation.AdjustCanvasObjectScaleByPixels(SceneHandle, Instance, vect);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleByFraction(float scalar) => Implementation.AdjustCanvasObjectScaleByFraction(SceneHandle, Instance, new XYPair<float>(scalar));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleByFraction(XYPair<float> vect) => Implementation.AdjustCanvasObjectScaleByFraction(SceneHandle, Instance, vect);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Angle rotation) => Implementation.RotateCanvasObjectBy(SceneHandle, Instance, rotation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Angle rotation, XYPair<float> pivotPoint) => Implementation.RotateCanvasObjectBy(SceneHandle, Instance, rotation, pivotPoint);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Angle rotation, XYPair<int> pivotPointPixels) => Implementation.RotateCanvasObjectBy(SceneHandle, Instance, rotation, pivotPointPixels);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveByPixels(XYPair<int> translation) => Implementation.MoveCanvasObjectByPixels(SceneHandle, Instance, translation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveByFraction(XYPair<float> translation) => Implementation.MoveCanvasObjectByFraction(SceneHandle, Instance, translation);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingQuadInstance.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingQuadInstance.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingQuadInstance.CopyName(destinationBuffer);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.DisposeCanvasObject(SceneHandle, Instance);

	public override string ToString() => $"Canvas Texture {UnderlyingQuadInstance.UnderlyingModelInstance}";
}

public readonly record struct CanvasText : ICanvasObject {
	public CanvasScene Canvas { get; }
	public TextInstance UnderlyingTextInstance { get; }

	ISceneImplProvider Implementation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Canvas.UnderlyingScene.Implementation;
	}
	ResourceHandle<Scene> SceneHandle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Canvas.UnderlyingScene.GetHandleWithoutDisposeCheck();
	}
	public ModelInstance UnderlyingModelInstance {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingTextInstance.UnderlyingModelInstance;
	}
	ModelInstance Instance {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingModelInstance;
	}

	public XYPair<int> ActualSizePixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectActualSizePixels(SceneHandle, Instance);
	}
	public XYPair<float> ActualSizeFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectActualSizeFraction(SceneHandle, Instance);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetDockParent<TCanvasObject>(TCanvasObject? parent) where TCanvasObject : struct, ICanvasObject {
		Implementation.SetCanvasObjectDockParent(SceneHandle, Instance, parent?.UnderlyingModelInstance);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(XYPair<int> canvasLocalPixelCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft) {
		return Implementation.CanvasObjectContainsPixelCoord(SceneHandle, Instance, canvasLocalPixelCoord, coordOrigin);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(XYPair<float> canvasLocalFractionCoord, DiagonalOrientation2D coordOrigin = DiagonalOrientation2D.UpLeft) {
		return Contains(Implementation.ConvertCanvasFractionToPixels(SceneHandle, canvasLocalFractionCoord), coordOrigin);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetText(FontString @string) => Implementation.SetCanvasTextString(SceneHandle, UnderlyingTextInstance, @string);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetText(ReadOnlySpan<char> str, TextJustification multiLineJustification = TextJustification.Center) => Implementation.SetCanvasTextString(SceneHandle, UnderlyingTextInstance, str, multiLineJustification);

	public FontPen Pen {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingTextInstance.Pen;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => UnderlyingTextInstance.SetPen(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPen(FontPen pen) => Pen = pen;
	
	public bool DisableAutomaticLineCountBasedHeightScaling {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasTextAutomaticLineCountScalingDisabled(SceneHandle, UnderlyingTextInstance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasTextAutomaticLineCountScalingDisabled(SceneHandle, UnderlyingTextInstance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetDisableAutomaticLineCountBasedHeightScaling(bool disableAutomaticLineCountBasedHeightScaling) => DisableAutomaticLineCountBasedHeightScaling = disableAutomaticLineCountBasedHeightScaling;

	public TextLayout Layout {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasTextLayout(SceneHandle, UnderlyingTextInstance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasTextLayout(SceneHandle, UnderlyingTextInstance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetLayout(TextLayout layout) => Layout = layout;

	public Orientation2D CanvasAnchor {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectCanvasAnchor(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectCanvasAnchor(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetCanvasAnchor(Orientation2D canvasAnchor) => CanvasAnchor = canvasAnchor;

	public Orientation2D? ObjectAnchor {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectAnchor(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectAnchor(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetObjectAnchor(Orientation2D? objectAnchor) => ObjectAnchor = objectAnchor;
	public Angle Rotation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectRotation(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectRotation(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetRotation(Angle rotation) => Rotation = rotation;
	public int Layer {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectLayer(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectLayer(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetLayer(int layer) => Layer = layer;
	public bool IsVisible {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectVisibility(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectVisibility(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetIsVisible(bool isVisible) => IsVisible = isVisible;
	public XYPair<int> PositionPixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectPositionPixels(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectPositionPixels(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPositionPixels(XYPair<int> positionPixels) => PositionPixels = positionPixels;
	public XYPair<float> PositionFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectPositionFraction(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectPositionFraction(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPositionFraction(XYPair<float> positionFraction) => PositionFraction = positionFraction;
	public int? WidthPixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectWidthPixels(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectWidthPixels(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetWidthPixels(int? widthPixels) => WidthPixels = widthPixels;
	public int? HeightPixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectHeightPixels(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectHeightPixels(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetHeightPixels(int? heightPixels) => HeightPixels = heightPixels;
	public float? WidthFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectWidthFraction(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectWidthFraction(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetWidthFraction(float? widthFraction) => WidthFraction = widthFraction;
	public float? HeightFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectHeightFraction(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectHeightFraction(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetHeightFraction(float? heightFraction) => HeightFraction = heightFraction;

	internal CanvasText(CanvasScene canvas, TextInstance underlyingTextInstance) {
		Canvas = canvas;
		UnderlyingTextInstance = underlyingTextInstance;
	}
	
	public void SetPlacementPixels(Orientation2D canvasAnchor, XYPair<int> position, int fontHeight, Orientation2D? objectAnchor = null) {
		Implementation.SetCanvasObjectPlacement(SceneHandle, Instance, canvasAnchor, objectAnchor, position, XYPair<float>.Zero, null, fontHeight, null, null);
	}
	public void SetPlacementFraction(Orientation2D canvasAnchor, XYPair<float> position, float fontHeight, Orientation2D? objectAnchor = null) {
		Implementation.SetCanvasObjectPlacement(SceneHandle, Instance, canvasAnchor, objectAnchor, XYPair<int>.Zero, position, null, null, null, fontHeight);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(float scalar) => Implementation.ScaleCanvasObjectBy(SceneHandle, Instance, scalar);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ScaleBy(XYPair<float> vect) => Implementation.ScaleCanvasObjectBy(SceneHandle, Instance, vect);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleByPixels(int scalar) => Implementation.AdjustCanvasObjectScaleByPixels(SceneHandle, Instance, new XYPair<int>(scalar));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleByPixels(XYPair<int> vect) => Implementation.AdjustCanvasObjectScaleByPixels(SceneHandle, Instance, vect);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleByFraction(float scalar) => Implementation.AdjustCanvasObjectScaleByFraction(SceneHandle, Instance, new XYPair<float>(scalar));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AdjustScaleByFraction(XYPair<float> vect) => Implementation.AdjustCanvasObjectScaleByFraction(SceneHandle, Instance, vect);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Angle rotation) => Implementation.RotateCanvasObjectBy(SceneHandle, Instance, rotation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Angle rotation, XYPair<float> pivotPoint) => Implementation.RotateCanvasObjectBy(SceneHandle, Instance, rotation, pivotPoint);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RotateBy(Angle rotation, XYPair<int> pivotPointPixels) => Implementation.RotateCanvasObjectBy(SceneHandle, Instance, rotation, pivotPointPixels);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveByPixels(XYPair<int> translation) => Implementation.MoveCanvasObjectByPixels(SceneHandle, Instance, translation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveByFraction(XYPair<float> translation) => Implementation.MoveCanvasObjectByFraction(SceneHandle, Instance, translation);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingTextInstance.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingTextInstance.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingTextInstance.CopyName(destinationBuffer);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.DisposeCanvasObject(SceneHandle, Instance);

	public override string ToString() => $"Canvas {UnderlyingTextInstance}";
}
