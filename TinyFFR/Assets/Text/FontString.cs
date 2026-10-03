// Created on 2026-06-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

/// <summary>
/// A string of text that has been laid out with a particular font, ready to be rendered. Created via <see cref="Font.CreateString"/>.
/// </summary>
/// <remarks>
/// <para>
/// Preparing a string works out where each character sits and builds the geometry to draw it, which is why it is done once and
/// kept rather than repeated every frame. Text that changes constantly (a frame counter, say) is better served by a small set
/// of prepared strings than by preparing a new one each frame.
/// </para>
/// <para>
/// Dispose a prepared string when it is no longer needed; this does not dispose the font.
/// </para>
/// </remarks>
public readonly record struct FontString : IDisposableResource<FontString, IFontStringImplProvider> {
	readonly ResourceHandle<FontString> _handle;
	readonly IFontStringImplProvider _impl;

	internal ResourceHandle<FontString> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(FontString)) : _handle;
	internal IFontStringImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<FontString>();

	IFontStringImplProvider IResource<FontString, IFontStringImplProvider>.Implementation => Implementation;
	ResourceHandle<FontString> IResource<FontString>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	/// <summary>
	/// The font this string was prepared with.
	/// </summary>
	public Font Font => Implementation.GetFont(_handle);
	
	/// <summary>
	/// How large this string is when drawn at the font's natural size, in world units (metres).
	/// </summary>
	/// <remarks>
	/// This is what a text object's layout scales from, and is also useful for sizing anything that has to fit around the text.
	/// </remarks>
	public XYPair<float> Size => Implementation.GetSize(_handle);

	internal FontString(ResourceHandle<FontString> handle, IFontStringImplProvider impl) {
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

	static FontString IResource<FontString>.CreateFromHandleAndImpl(ResourceHandle<FontString> handle, IResourceImplProvider impl) {
		return new FontString(handle, impl as IFontStringImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<FontString> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<FontString> IResource<FontString>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	internal Mesh GetStringMesh() => Implementation.GetMesh(_handle);

	#region Disposal
	/// <summary>
	/// Disposes this prepared string, releasing the geometry built for it.
	/// </summary>
	/// <remarks>
	/// The font itself is not disposed, and its other strings are unaffected.
	/// </remarks>
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	/// <inheritdoc />
	public override string ToString() => $"Font String {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";
}