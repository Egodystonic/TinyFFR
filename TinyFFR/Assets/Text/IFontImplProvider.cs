// Created on 2026-06-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

public interface IFontImplProvider : IDisposableResourceImplProvider<Font> {
	FontPen CreatePen(ResourceHandle<Font> handle, ColorVect foregroundColor, ColorVect backgroundColor, ColorVect outlineColor, float outlineThicknessMultiplier);
	FontString CreateString(ResourceHandle<Font> handle, ReadOnlySpan<char> text);
	XYPair<float> MeasureString(ResourceHandle<Font> handle, ReadOnlySpan<char> text);
	Material GetPenMaterial(ResourceHandle<Font> handle, nuint penHandle);
	Mesh GetStringMesh(ResourceHandle<Font> handle, nuint stringHandle);
	XYPair<float> GetStringSize(ResourceHandle<Font> handle, nuint stringHandle);
	void DisposePen(ResourceHandle<Font> handle, nuint penHandle);
	void DisposeString(ResourceHandle<Font> handle, nuint stringHandle);
	Transform GetStringTransformUsingFixedWidth(ResourceHandle<Font> handle, XYPair<float> stringSize, Location position, float width, Direction facingDirection, Direction uprightDirection, Orientation2D positionAnchor);
	Transform GetStringTransformUsingFixedHeight(ResourceHandle<Font> handle, XYPair<float> stringSize, Location position, float height, Direction facingDirection, Direction uprightDirection, Orientation2D positionAnchor);
	Transform GetStringTransformUsingFixedWidthAndHeight(ResourceHandle<Font> handle, XYPair<float> stringSize, Location position, XYPair<float> widthAndHeight, Direction facingDirection, Direction uprightDirection, Orientation2D positionAnchor);
	Transform GetStringTransformUsingFontSize(ResourceHandle<Font> handle, XYPair<float> stringSize, Location position, float fontSizeMultiplier, Direction facingDirection, Direction uprightDirection, Orientation2D positionAnchor);
}