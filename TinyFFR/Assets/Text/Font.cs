// Created on 2026-06-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

/// <summary>
/// A ready-made combination of text, outline and background colours for use when creating a <see cref="FontPen"/>.
/// </summary>
/// <remarks>
/// Each of these is shorthand for a particular call to <see cref="Font.CreatePen(ColorVect, ColorVect, float, ColorVect)"/>;
/// create a pen from one of these where you simply need legible text, and specify the colours yourself where you do not.
/// </remarks>
public enum BuiltInFontPenStyle {
	/// <summary>
	/// The default pen style. Currently the same as <see cref="WhiteWithOutline"/>. 
	/// </summary>
	Default,
	/// <summary>
	/// White text with a thick black outline around it.
	/// </summary>
	WhiteWithOutline,
	/// <summary>
	/// Black text with a thinner white outline around it.
	/// </summary>
	BlackWithOutline,
	/// <summary>
	/// White text drawn on a solid black background rather than directly over the scene.
	/// </summary>
	WhiteWithBackground,
	/// <summary>
	/// Black text drawn on a solid white background rather than directly over the scene.
	/// </summary>
	BlackWithBackground
}

/// <summary>
/// A typeface loaded and prepared for drawing text.
/// </summary>
/// <remarks>
/// <para>
/// Loading a font draws every character it supports in to a single texture once, up front; text is then assembled from that
/// texture, which is why drawing text is cheap but the set of supported characters is fixed at load time.
/// </para>
/// <para>
/// A font on its own draws nothing. Create a <see cref="FontPen"/> from it for the colours to draw in, and a
/// <see cref="FontString"/> for the text itself; both must be disposed before the font is.
/// </para>
/// <para>
/// Preparing a string is not free, so where text changes every frame it
/// is usually better to keep a small set of prepared strings than to build a new one each time.
/// </para>
/// </remarks>
public readonly struct Font : IDisposableResource<Font, IFontImplProvider> {
	readonly ResourceHandle<Font> _handle;
	readonly IFontImplProvider _impl;

	internal ResourceHandle<Font> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Font)) : _handle;
	internal IFontImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Font>();

	IFontImplProvider IResource<Font, IFontImplProvider>.Implementation => Implementation;
	ResourceHandle<Font> IResource<Font>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	internal Font(ResourceHandle<Font> handle, IFontImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static Font IResource<Font>.CreateFromHandleAndImpl(ResourceHandle<Font> handle, IResourceImplProvider impl) {
		return new Font(handle, impl as IFontImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<Font> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<Font> IResource<Font>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();
	
	/// <summary>
	/// Creates a pen that draws text in the given colour, with no outline and no background.
	/// </summary>
	/// <param name="foregroundColor">The colour of the text itself.</param>
	public FontPen CreatePen(ColorVect foregroundColor) => CreatePen(foregroundColor, ColorVect.BlackTransparent, 0f);
	/// <summary>
	/// Creates a pen that draws outlined text with no background.
	/// </summary>
	/// <param name="foregroundColor">The colour of the text itself.</param>
	/// <param name="outlineColor">The colour of the outline drawn around the text.</param>
	/// <param name="outlineThicknessNormalized">How thick the outline is, in the range <c>0f &lt;= n &lt;= 1f</c>, where <c>0f</c> is no outline at all.</param>
	public FontPen CreatePen(ColorVect foregroundColor, ColorVect outlineColor, float outlineThicknessNormalized) {
		return CreatePen(foregroundColor, outlineColor, outlineThicknessNormalized, ColorVect.BlackTransparent);
	}
	/// <summary>
	/// Creates a pen that draws text on a solid background, with no outline.
	/// </summary>
	/// <param name="foregroundColor">The colour of the text itself.</param>
	/// <param name="backgroundColor">The colour drawn behind the text. Use a fully transparent colour for no background at all.</param>
	public FontPen CreatePen(ColorVect foregroundColor, ColorVect backgroundColor) {
		return CreatePen(foregroundColor, ColorVect.BlackTransparent, 0f, backgroundColor);
	}
	/// <summary>
	/// Creates a pen that draws outlined text on a background.
	/// </summary>
	/// <param name="foregroundColor">The colour of the text itself.</param>
	/// <param name="outlineColor">The colour of the outline drawn around the text.</param>
	/// <param name="outlineThicknessNormalized">How thick the outline is, in the range <c>0f &lt;= n &lt;= 1f</c>, where <c>0f</c> is no outline at all.</param>
	/// <param name="backgroundColor">The colour drawn behind the text. Use a fully transparent colour for no background at all.</param>
	public FontPen CreatePen(ColorVect foregroundColor, ColorVect outlineColor, float outlineThicknessNormalized, ColorVect backgroundColor) {
		return Implementation.CreatePen(_handle, foregroundColor, backgroundColor, outlineColor, outlineThicknessNormalized);
	}
	/// <summary>
	/// Creates a pen from one of the ready-made colour combinations.
	/// </summary>
	/// <remarks>
	/// This is the quickest way to get legible text on screen without deciding on colours.
	/// </remarks>
	/// <param name="builtInPenStyle">Which ready-made combination to use.</param>
	public FontPen CreatePen(BuiltInFontPenStyle builtInPenStyle) {
		return builtInPenStyle switch {
			BuiltInFontPenStyle.BlackWithBackground => CreatePen(ColorVect.BlackOpaque, ColorVect.WhiteOpaque),
			BuiltInFontPenStyle.WhiteWithBackground => CreatePen(ColorVect.WhiteOpaque, ColorVect.BlackOpaque),
			BuiltInFontPenStyle.BlackWithOutline => CreatePen(ColorVect.BlackOpaque, ColorVect.WhiteOpaque, 0.2f),
			_ => CreatePen(ColorVect.WhiteOpaque, ColorVect.BlackOpaque, 0.5f)
		};
	}

	/// <summary>
	/// Measures how large the given text would be when drawn with this font, without preparing it.
	/// </summary>
	/// <remarks>
	/// Use this to size or place something around text before committing to building it. The result is in world units (metres)
	/// at the font's natural size.
	/// </remarks>
	/// <param name="str">The text to measure. May contain line breaks.</param>
	/// <param name="multiLineJustification">How the lines are aligned against each other, for text that occupies more than one line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<float> MeasureString(ReadOnlySpan<char> str, TextJustification multiLineJustification = TextJustification.Center) => Implementation.MeasureString(_handle, str, multiLineJustification);

	/// <summary>
	/// Prepares the given text for drawing, working out where each character sits and building the geometry for it.
	/// </summary>
	/// <remarks>
	/// The result must be disposed when no longer needed. Preparing a string is not free, so where text changes every frame it
	/// is usually better to keep a small set of prepared strings than to build a new one each time.
	/// </remarks>
	/// <param name="str">The text to prepare. May contain line breaks, and should only contain characters the font was loaded with.</param>
	/// <param name="multiLineJustification">How the lines are aligned against each other, for text that occupies more than one line.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public FontString CreateString(ReadOnlySpan<char> str, TextJustification multiLineJustification = TextJustification.Center) => Implementation.CreateString(_handle, str, multiLineJustification);
	
	/// <summary>
	/// Calculates the transform that places, orients and sizes a prepared string as the given layout describes.
	/// </summary>
	/// <param name="stringSize">The size of the prepared string, from <see cref="FontString.Size"/>.</param>
	/// <param name="position">Where to put the text.</param>
	/// <param name="facingDirection">Which way the text should face.</param>
	/// <param name="uprightDirection">Which way is "up" across the text, or <see langword="null"/> to derive one from <paramref name="facingDirection"/>.</param>
	/// <param name="layout">How the text should be sized and anchored.</param>
	public Transform GetTextInstanceTransform(XYPair<float> stringSize, Location position, Direction facingDirection, Direction? uprightDirection, TextLayout layout) {
		return Implementation.GetTextInstanceTransform(_handle, layout.Width, layout.Height, stringSize, position, facingDirection, uprightDirection ?? Direction.Up.OrthogonalizedAgainst(facingDirection) ?? facingDirection.AnyOrthogonal(), layout.PositionAnchor, layout.DisableAutomaticLineCountBasedHeightScaling);
	}

	/// <summary>
	/// Calculates how much a prepared string must be scaled to meet the given layout's width and height.
	/// </summary>
	/// <param name="stringSize">The size of the prepared string, from <see cref="FontString.Size"/>.</param>
	/// <param name="layout">How the text should be sized.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<float> GetTextInstanceScaling(XYPair<float> stringSize, TextLayout layout) {
		return Implementation.GetTextInstanceScaling(_handle, stringSize, layout.Width, layout.Height, layout.DisableAutomaticLineCountBasedHeightScaling);
	}

	/// <summary>
	/// Calculates how far a prepared string must be shifted so that the given point of it, rather than its centre, sits at its position.
	/// </summary>
	/// <param name="stringSize">The size of the prepared string, from <see cref="FontString.Size"/>.</param>
	/// <param name="scaling">The scaling being applied to the string, from <see cref="GetTextInstanceScaling"/>.</param>
	/// <param name="positionAnchor">Which point of the text should end up at its position (or the centre if <see cref="Orientation2D.None"/>, in which case the offset is zero).</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vect GetTextInstanceAnchorOffset(XYPair<float> stringSize, XYPair<float> scaling, Orientation2D positionAnchor) {
		return Implementation.GetTextInstanceAnchorOffset(_handle, stringSize, scaling, positionAnchor);
	}

	#region Disposal
	/// <summary>
	/// Disposes this font, releasing the texture its characters were drawn in to.
	/// </summary>
	/// <remarks>
	/// Every pen and prepared string made from this font must be disposed first.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	/// <inheritdoc />
	public override string ToString() => $"Font {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	/// <inheritdoc />
	public bool Equals(Font other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Font other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	/// <summary>
	/// Returns whether the two given fonts are the same font.
	/// </summary>
	/// <param name="left">The first font to compare.</param>
	/// <param name="right">The second font to compare.</param>
	public static bool operator ==(Font left, Font right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given fonts are different fonts.
	/// </summary>
	/// <param name="left">The first font to compare.</param>
	/// <param name="right">The second font to compare.</param>
	public static bool operator !=(Font left, Font right) => !left.Equals(right);
	#endregion
}