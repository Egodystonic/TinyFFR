// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly struct BackdropTexture : IDisposableResource<BackdropTexture, IBackdropTextureImplProvider> {
	readonly ResourceHandle<BackdropTexture> _handle;
	readonly IBackdropTextureImplProvider _impl;

	internal ResourceHandle<BackdropTexture> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(BackdropTexture)) : _handle;
	internal IBackdropTextureImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<BackdropTexture>();

	IBackdropTextureImplProvider IResource<BackdropTexture, IBackdropTextureImplProvider>.Implementation => Implementation;
	ResourceHandle<BackdropTexture> IResource<BackdropTexture>.Handle => Handle;

	internal UIntPtr SkyboxTextureHandle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSkyboxTextureHandle(_handle);
	}
	internal UIntPtr IndirectLightingTextureHandle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetIndirectLightingTextureHandle(_handle);
	} 

	internal BackdropTexture(ResourceHandle<BackdropTexture> handle, IBackdropTextureImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static BackdropTexture IResource<BackdropTexture>.CreateFromHandleAndImpl(ResourceHandle<BackdropTexture> handle, IResourceImplProvider impl) {
		return new BackdropTexture(handle, impl as IBackdropTextureImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Environment Cubemap {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	public bool Equals(BackdropTexture other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is BackdropTexture other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(BackdropTexture left, BackdropTexture right) => left.Equals(right);
	public static bool operator !=(BackdropTexture left, BackdropTexture right) => !left.Equals(right);
	#endregion
}