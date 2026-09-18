// Created on 2026-06-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

/// <summary>
/// Provides the implementation behind <see cref="Font"/> and its pens and strings.
/// </summary>
public interface IFontImplProvider : IDisposableResourceImplProvider<Font> {
	/// <summary>
	/// Invoked via <see cref="Font.CreatePen(ColorVect, ColorVect, float, ColorVect)"/>.
	/// </summary>
	FontPen CreatePen(ResourceHandle<Font> handle, ColorVect foregroundColor, ColorVect backgroundColor, ColorVect outlineColor, float outlineThicknessNormalized);
	/// <summary>
	/// Invoked via <see cref="Font.CreateString"/>.
	/// </summary>
	FontString CreateString(ResourceHandle<Font> handle, ReadOnlySpan<char> text, TextJustification multiLineJustification);
	/// <summary>
	/// Invoked via <see cref="Font.MeasureString"/>.
	/// </summary>
	XYPair<float> MeasureString(ResourceHandle<Font> handle, ReadOnlySpan<char> text, TextJustification multiLineJustification);
	/// <summary>
	/// Invoked internally to obtain the material a <see cref="FontPen"/> draws with.
	/// </summary>
	Material GetPenMaterial(ResourceHandle<Font> handle, nuint penHandle);
	/// <summary>
	/// Invoked internally to obtain the geometry a <see cref="FontString"/> is drawn from.
	/// </summary>
	Mesh GetStringMesh(ResourceHandle<Font> handle, nuint stringHandle);
	/// <summary>
	/// Invoked via <see cref="FontString.Size"/>.
	/// </summary>
	XYPair<float> GetStringSize(ResourceHandle<Font> handle, nuint stringHandle);
	/// <summary>
	/// Invoked via <see cref="FontPen.Dispose"/>.
	/// </summary>
	void DisposePen(ResourceHandle<Font> handle, nuint penHandle);
	/// <summary>
	/// Invoked via <see cref="FontString.Dispose"/>.
	/// </summary>
	void DisposeString(ResourceHandle<Font> handle, nuint stringHandle);
	/// <summary>
	/// Invoked via <see cref="Font.GetTextInstanceScaling"/>.
	/// </summary>
	XYPair<float> GetTextInstanceScaling(ResourceHandle<Font> handle, XYPair<float> stringSize, float? textInstanceWidth, float? textInstanceHeight, bool disableAutomaticLineCountBasedHeightScaling);
	/// <summary>
	/// Invoked via <see cref="Font.GetTextInstanceTransform"/>.
	/// </summary>
	Transform GetTextInstanceTransform(ResourceHandle<Font> handle, float? textInstanceWidth, float? textInstanceHeight, XYPair<float> stringSize, Location position, Direction facingDirection, Direction uprightDirection, Orientation2D positionAnchor, bool disableAutomaticLineCountBasedHeightScaling);
	/// <summary>
	/// Invoked via <see cref="Font.GetTextInstanceAnchorOffset"/>.
	/// </summary>
	Vect GetTextInstanceAnchorOffset(ResourceHandle<Font> handle, XYPair<float> stringSize, XYPair<float> scaling, Orientation2D positionAnchor);
}