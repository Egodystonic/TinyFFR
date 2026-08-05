// Created on 2026-07-24 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

public interface ICanvasObject : IDisposable, IStringSpanNameEnabled, ITransformed2DSceneObject {
	CanvasScene Canvas { get; }
	
	public Orientation2D CanvasAnchor { get; set; }
	public Orientation2D? ObjectAnchor { get; set; }
	
	public int Layer { get; set; }
	
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
	
	public Texture Texture {
		get =>
		set => 
	}

	public Orientation2D CanvasAnchor {
		get => 
		set =>
	}
	public Orientation2D? ObjectAnchor {
		get =>  
		set => 
	}
	public Angle Rotation {
		get => 
		set =>
	}
	public int Layer {
		get => 
		set =>
	}
	public XYPair<int> PositionPixels {
		get => 
		set =>
	}
	public XYPair<float> PositionFraction {
		get => 
		set =>
	}
	public XYPair<int> SizePixels {
		get => 
		set =>
	}
	public XYPair<float> SizeFraction {
		get => 
		set =>
	}

	internal CanvasTexture(CanvasScene canvas, QuadInstance underlyingQuadInstance) {
		Canvas = canvas;
		UnderlyingQuadInstance = underlyingQuadInstance;
	}
	
	public void SetTextureOffsetPixels(XYPair<int> offset) { }
	public void SetTextureOffsetFraction(XYPair<float> offset) { }
	public void SetTextureExtentPixels(XYPair<int> extent) { }
	public void SetTextureExtentFraction(XYPair<float> extent) { }

	public void ScaleBy(float scalar) { }
	public void ScaleBy(XYPair<float> vect) { }
	public void AdjustScaleByPixels(int scalar) { }
	public void AdjustScaleByPixels(XYPair<int> vect) { }
	public void AdjustScaleByFraction(float scalar) { }
	public void AdjustScaleByFraction(XYPair<float> vect) { }
	public void RotateBy(Angle rotation) { }
	public void RotateBy(Angle rotation, XYPair<float> pivotPoint) { }
	public void RotateBy(Angle rotation, XYPair<int> pivotPointPixels) { }
	public void MoveByPixels(XYPair<int> translation) { }
	public void MoveByFraction(XYPair<float> translation) { }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingQuadInstance.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingQuadInstance.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingQuadInstance.CopyName(destinationBuffer);
	
	public override string ToString() => $"Canvas Texture {UnderlyingQuadInstance.UnderlyingModelInstance}";
}

public readonly record struct CanvasText : ICanvasObject {
	public CanvasScene Canvas { get; }
	public TextInstance UnderlyingTextInstance { get; }
	
	public FontString String {
		get =>
		set => 
	}
	
	public TextLayout Layout {
		get =>
		set => 
	}

	internal CanvasText(CanvasScene canvas, TextInstance underlyingTextInstance) {
		Canvas = canvas;
		UnderlyingTextInstance = underlyingTextInstance;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => UnderlyingTextInstance.GetNameAsNewStringObject();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => UnderlyingTextInstance.GetNameLength();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => UnderlyingTextInstance.CopyName(destinationBuffer);
	
	public override string ToString() => $"Canvas {UnderlyingTextInstance}";
}