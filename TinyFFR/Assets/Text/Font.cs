// Created on 2026-06-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

public readonly struct Font : IDisposableResource<Font, IFontImplProvider> {
	readonly ResourceHandle<Font> _handle;
	readonly IFontImplProvider _impl;

	internal ResourceHandle<Font> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Font)) : _handle;
	internal IFontImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Font>();

	IFontImplProvider IResource<Font, IFontImplProvider>.Implementation => Implementation;
	ResourceHandle<Font> IResource<Font>.Handle => Handle;

	internal Font(ResourceHandle<Font> handle, IFontImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static Font IResource<Font>.CreateFromHandleAndImpl(ResourceHandle<Font> handle, IResourceImplProvider impl) {
		return new Font(handle, impl as IFontImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ResourceHandle<Font> GetHandleWithoutDisposeCheck() => _handle;
	
	public FontPen CreatePen(ColorVect foregroundColor) => CreatePen(foregroundColor, ColorVect.BlackTransparent, 0f);
	public FontPen CreatePen(ColorVect foregroundColor, ColorVect outlineColor, float outlineThicknessNormalized) {
		return CreatePen(foregroundColor, outlineColor, outlineThicknessNormalized, ColorVect.BlackTransparent);
	}
	public FontPen CreatePen(ColorVect foregroundColor, ColorVect backgroundColor) {
		return CreatePen(foregroundColor, ColorVect.BlackTransparent, 0f, backgroundColor);
	}
	public FontPen CreatePen(ColorVect foregroundColor, ColorVect outlineColor, float outlineThicknessNormalized, ColorVect backgroundColor) {
		return Implementation.CreatePen(_handle, foregroundColor, backgroundColor, outlineColor, outlineThicknessNormalized);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<float> MeasureString(ReadOnlySpan<char> str, TextJustification multiLineJustification = TextJustification.Center) => Implementation.MeasureString(_handle, str, multiLineJustification);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public FontString CreateString(ReadOnlySpan<char> str, TextJustification multiLineJustification = TextJustification.Center) => Implementation.CreateString(_handle, str, multiLineJustification);
	
	public Transform GetTextInstanceTransform(XYPair<float> stringSize, Location position, Direction facingDirection, float? textInstanceHeight, float? textInstanceWidth = null, Direction? uprightDirection = null, Orientation2D positionAnchor = Orientation2D.None, bool rescaleHeightAccordingToLineCount = true) {
		return Implementation.GetTextInstanceTransform(_handle, textInstanceWidth, textInstanceHeight, stringSize, position, facingDirection, uprightDirection ?? Direction.Up.OrthogonalizedAgainst(facingDirection) ?? facingDirection.AnyOrthogonal(), positionAnchor, rescaleHeightAccordingToLineCount);
	}

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Font {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	public bool Equals(Font other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	public override bool Equals(object? obj) => obj is Font other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(Font left, Font right) => left.Equals(right);
	public static bool operator !=(Font left, Font right) => !left.Equals(right);
	#endregion
}