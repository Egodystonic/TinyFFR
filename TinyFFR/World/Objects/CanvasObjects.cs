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
	public XYPair<int> PositionPixels { get; set; }
	public XYPair<float> PositionFraction { get; set; }
	public XYPair<int> SizePixels { get; set; }
	public XYPair<float> SizeFraction { get; set; }
	public int Layer { get; set; }
}

public readonly record struct CanvasTexture : ICanvasObject {
	public CanvasScene Canvas { get; }
	public QuadInstance UnderlyingQuadInstance { get; }
	
	public Texture Texture {
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