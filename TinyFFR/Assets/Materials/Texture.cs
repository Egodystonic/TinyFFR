// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly struct Texture : IDisposableResource<Texture, TextureHandle, ITextureImplProvider> {
	readonly TextureHandle _handle;
	readonly ITextureImplProvider _impl;

	internal TextureHandle Handle => _handle;
	internal ITextureImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Texture>();

	ITextureImplProvider IResource<TextureHandle, ITextureImplProvider>.Implementation => Implementation;
	TextureHandle IResource<TextureHandle, ITextureImplProvider>.Handle => Handle;

	public string Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(_handle);
	}

	internal Texture(TextureHandle handle, ITextureImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static Texture IResource<Texture>.RecreateFromRawHandleAndImpl(nuint rawHandle, IResourceImplProvider impl) {
		return new Texture(rawHandle, impl as ITextureImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameUsingSpan(Span<char> dest) => Implementation.GetNameUsingSpan(_handle, dest);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameSpanLength() => Implementation.GetNameSpanLength(_handle);

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	public bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Texture {(IsDisposed ? "(Disposed)" : $"\"{Name}\"")}";

	#region Equality
	public bool Equals(Texture other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is Texture other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(Texture left, Texture right) => left.Equals(right);
	public static bool operator !=(Texture left, Texture right) => !left.Equals(right);
	#endregion
}