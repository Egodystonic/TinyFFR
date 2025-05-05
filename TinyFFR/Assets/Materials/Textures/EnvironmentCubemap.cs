// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly struct EnvironmentCubemap : IDisposableResource<EnvironmentCubemap, IEnvironmentCubemapImplProvider> {
	readonly ResourceHandle<EnvironmentCubemap> _handle;
	readonly IEnvironmentCubemapImplProvider _impl;

	internal ResourceHandle<EnvironmentCubemap> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(EnvironmentCubemap)) : _handle;
	internal IEnvironmentCubemapImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<EnvironmentCubemap>();

	IEnvironmentCubemapImplProvider IResource<EnvironmentCubemap, IEnvironmentCubemapImplProvider>.Implementation => Implementation;
	ResourceHandle<EnvironmentCubemap> IResource<EnvironmentCubemap>.Handle => Handle;

	internal UIntPtr SkyboxTextureHandle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSkyboxTextureHandle(_handle);
	}
	internal UIntPtr IndirectLightingTextureHandle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetIndirectLightingTextureHandle(_handle);
	} 

	internal EnvironmentCubemap(ResourceHandle<EnvironmentCubemap> handle, IEnvironmentCubemapImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static EnvironmentCubemap IResource<EnvironmentCubemap>.CreateFromHandleAndImpl(ResourceHandle<EnvironmentCubemap> handle, IResourceImplProvider impl) {
		return new EnvironmentCubemap(handle, impl as IEnvironmentCubemapImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
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
	public bool Equals(EnvironmentCubemap other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is EnvironmentCubemap other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(EnvironmentCubemap left, EnvironmentCubemap right) => left.Equals(right);
	public static bool operator !=(EnvironmentCubemap left, EnvironmentCubemap right) => !left.Equals(right);
	#endregion
}