// Created on 2026-06-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

public interface IFontImplProvider : IDisposableResourceImplProvider<Font> {
	FontPen CreatePen(ResourceHandle<Font> handle, ColorVect foregroundColor, ColorVect backgroundColor, ColorVect outlineColor, float outlineThicknessMultiplier, ColorVect glowColor, float glowSizeMultiplier);
	XYPair<int> MeasureString(ResourceHandle<Font> handle, ReadOnlySpan<char> str);
	Material GetPenMaterial(ResourceHandle<Font> handle, nuint penHandle);
	TextInstance CreateTextInstance(ResourceHandle<Font> handle);
	void DisposeTextInstance(ResourceHandle<Font> handle, TextInstance instance);
	void Dispose(ResourceHandle<Font> handle, nuint penHandle);
}