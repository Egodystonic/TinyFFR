// Created on 2026-06-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

public interface IFontImplProvider : IDisposableResourceImplProvider<Font> {
	FontPen CreatePen(ResourceHandle<Font> handle, ColorVect foregroundColor, ColorVect backgroundColor, ColorVect outlineColor, float outlineThicknessNormalized);
	FontString CreateString(ResourceHandle<Font> handle, ReadOnlySpan<char> text, TextJustification multiLineJustification);
	XYPair<float> MeasureString(ResourceHandle<Font> handle, ReadOnlySpan<char> text, TextJustification multiLineJustification);
	Material GetPenMaterial(ResourceHandle<Font> handle, nuint penHandle);
	Mesh GetStringMesh(ResourceHandle<Font> handle, nuint stringHandle);
	XYPair<float> GetStringSize(ResourceHandle<Font> handle, nuint stringHandle);
	void DisposePen(ResourceHandle<Font> handle, nuint penHandle);
	void DisposeString(ResourceHandle<Font> handle, nuint stringHandle);
	Transform GetTextInstanceTransform(ResourceHandle<Font> handle, float? textInstanceWidth, float? textInstanceHeight, XYPair<float> stringSize, Location position, Direction facingDirection, Direction uprightDirection, Orientation2D positionAnchor, bool rescaleHeightAccordingToLineCount);
	Vect GetTextInstanceAnchorOffset(ResourceHandle<Font> handle, XYPair<float> stringSize, XYPair<float> scaling, Orientation2D positionAnchor);
}