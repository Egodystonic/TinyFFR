// Created on 2026-06-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

public interface IFontImplProvider : IDisposableResourceImplProvider<Font> {
	FontPen CreatePen(ResourceHandle<Font> handle, ColorVect foregroundColor, ColorVect backgroundColor, ColorVect outlineColor, float outlineThicknessMultiplier, ColorVect glowColor, float glowSizeMultiplier);
	FontString CreateString(ResourceHandle<Font> handle, ReadOnlySpan<char> text);
	XYPair<float> MeasureString(ResourceHandle<Font> handle, ReadOnlySpan<char> text);
	Material GetPenMaterial(ResourceHandle<Font> handle, nuint penHandle);
	Mesh GetStringMesh(ResourceHandle<Font> handle, nuint stringHandle);
	void DisposePen(ResourceHandle<Font> handle, nuint penHandle);
	void DisposeString(ResourceHandle<Font> handle, nuint stringHandle);
}