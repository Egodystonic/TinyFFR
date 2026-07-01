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
	
	public FontPen CreatePen(ColorVect foregroundColor, ColorVect backgroundColor) => CreatePen(foregroundColor, backgroundColor, ColorVect.BlackTransparent, 0f);
	public FontPen CreatePen(ColorVect foregroundColor, ColorVect backgroundColor, ColorVect outlineColor, float outlineThicknessMultiplier) => CreatePen(foregroundColor, backgroundColor, outlineColor, outlineThicknessMultiplier, ColorVect.BlackTransparent, 0f);
	public FontPen CreatePen(ColorVect foregroundColor, ColorVect backgroundColor, ColorVect outlineColor, float outlineThicknessMultiplier, ColorVect glowColor, float glowSizeMultiplier) {
		return Implementation.CreatePen(_handle, foregroundColor, backgroundColor, outlineColor, outlineThicknessMultiplier, glowColor, glowSizeMultiplier);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public XYPair<int> MeasureString(ReadOnlySpan<char> str) => Implementation.MeasureString(_handle, str);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Mesh CreateTextInstance() => Implementation.CreateTextInstance(_handle);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void DisposeTextInstance(TextInstance instance) => Implementation.DisposeTextInstance(_handle, instance);
	
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