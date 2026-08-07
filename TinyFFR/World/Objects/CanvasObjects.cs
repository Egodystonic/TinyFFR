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

	public XYPair<int> SizePixels { get; set; }
	public XYPair<float> SizeFraction { get; set; }
	XYPair<float> IScaled2DSceneObject.Scaling {
		get => SizeFraction;
		set => SizeFraction = value;
	}

	void MoveByPixels(XYPair<int> translation);
	void MoveByFraction(XYPair<float> translation);
	void IMovable2DSceneObject.MoveBy(XYPair<float> translation) => MoveByFraction(translation);

	Transform2D ITransformed2DSceneObject.Transform {
		get => new(PositionFraction, Rotation, SizeFraction);
		set {
			SizeFraction = value.Scaling;
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
	ModelInstance Instance {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingQuadInstance.UnderlyingModelInstance;
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
	public XYPair<int> SizePixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectSizePixels(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectSizePixels(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetSizePixels(XYPair<int> sizePixels) => SizePixels = sizePixels;
	public XYPair<float> SizeFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectSizeFraction(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectSizeFraction(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetSizeFraction(XYPair<float> sizeFraction) => SizeFraction = sizeFraction;

	internal CanvasTexture(CanvasScene canvas, QuadInstance underlyingQuadInstance) {
		Canvas = canvas;
		UnderlyingQuadInstance = underlyingQuadInstance;
	}
	
	public void SetPlacementPixels(Orientation2D canvasAnchor, Orientation2D? objectAnchor, XYPair<int> position, XYPair<int> size) {
		CanvasAnchor = canvasAnchor;
		ObjectAnchor = objectAnchor;
		PositionPixels = position;
		SizePixels = size;
	}
	public void SetPlacementFraction(Orientation2D canvasAnchor, Orientation2D? objectAnchor, XYPair<float> position, XYPair<float> size) {
		CanvasAnchor = canvasAnchor;
		ObjectAnchor = objectAnchor;
		PositionFraction = position;
		SizeFraction = size;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTexture(Texture t) => Implementation.SetCanvasObjectTexture(SceneHandle, UnderlyingQuadInstance, t);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTextureOffsetPixels(XYPair<int> offset) => Implementation.SetCanvasObjectTextureOffsetPixels(SceneHandle, UnderlyingQuadInstance, offset);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTextureOffsetFraction(XYPair<float> offset) => Implementation.SetCanvasObjectTextureOffset(SceneHandle, UnderlyingQuadInstance, offset);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTextureExtentPixels(XYPair<int> extent) => Implementation.SetCanvasObjectTextureExtentPixels(SceneHandle, UnderlyingQuadInstance, extent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTextureExtentFraction(XYPair<float> extent) => Implementation.SetCanvasObjectTextureExtent(SceneHandle, UnderlyingQuadInstance, extent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBlendTexture(Texture blendTex) => Implementation.SetCanvasBlendTexture(SceneHandle, UnderlyingQuadInstance, blendTex);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBlendTextureDistance(float distance) => Implementation.SetCanvasBlendTextureDistance(SceneHandle, UnderlyingQuadInstance, distance);

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
	ModelInstance Instance {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => UnderlyingTextInstance.UnderlyingModelInstance;
	}

	public FontString String {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasTextString(SceneHandle, UnderlyingTextInstance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasTextString(SceneHandle, UnderlyingTextInstance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetString(FontString @string) => String = @string;

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
	public XYPair<int> SizePixels {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectSizePixels(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectSizePixels(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetSizePixels(XYPair<int> sizePixels) => SizePixels = sizePixels;
	public XYPair<float> SizeFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetCanvasObjectSizeFraction(SceneHandle, Instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetCanvasObjectSizeFraction(SceneHandle, Instance, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetSizeFraction(XYPair<float> sizeFraction) => SizeFraction = sizeFraction;

	internal CanvasText(CanvasScene canvas, TextInstance underlyingTextInstance) {
		Canvas = canvas;
		UnderlyingTextInstance = underlyingTextInstance;
	}
	
	public void SetPlacementPixels(Orientation2D canvasAnchor, Orientation2D? objectAnchor, XYPair<int> position, XYPair<int> size) {
		CanvasAnchor = canvasAnchor;
		ObjectAnchor = objectAnchor;
		PositionPixels = position;
		SizePixels = size;
	}
	public void SetPlacementFraction(Orientation2D canvasAnchor, Orientation2D? objectAnchor, XYPair<float> position, XYPair<float> size) {
		CanvasAnchor = canvasAnchor;
		ObjectAnchor = objectAnchor;
		PositionFraction = position;
		SizeFraction = size;
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
