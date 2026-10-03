// Created on 2026-06-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

/// <summary>
/// A particular combination of colours for drawing text with: Fill, outline, background. Created via <see cref="Font.CreatePen(ColorVect)"/>.
/// </summary>
/// <remarks>
/// <para>
/// A pen is created from a font and holds the material that text drawn with it uses, so one font can serve any number of
/// differently-coloured pens without being loaded again.
/// </para>
/// <para>
/// Dispose a pen when it is no longer needed; this does not dispose the font.
/// </para>
/// </remarks>
public readonly record struct FontPen : IDisposableResource<FontPen, IFontPenImplProvider> {
	readonly ResourceHandle<FontPen> _handle;
	readonly IFontPenImplProvider _impl;

	internal ResourceHandle<FontPen> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(FontPen)) : _handle;
	internal IFontPenImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<FontPen>();

	IFontPenImplProvider IResource<FontPen, IFontPenImplProvider>.Implementation => Implementation;
	ResourceHandle<FontPen> IResource<FontPen>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	/// <summary>
	/// The font this pen draws with.
	/// </summary>
	public Font Font => Implementation.GetFont(_handle);

	internal FontPen(ResourceHandle<FontPen> handle, IFontPenImplProvider impl) {
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

	static FontPen IResource<FontPen>.CreateFromHandleAndImpl(ResourceHandle<FontPen> handle, IResourceImplProvider impl) {
		return new FontPen(handle, impl as IFontPenImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<FontPen> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<FontPen> IResource<FontPen>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	internal Material GetPenMaterial() => Implementation.GetMaterial(_handle);

	#region Disposal
	/// <summary>
	/// Disposes this pen, releasing the material it draws with.
	/// </summary>
	/// <remarks>
	/// The font itself is not disposed, and its other pens are unaffected.
	/// </remarks>
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	/// <inheritdoc />
	public override string ToString() => $"Font Pen {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";
}