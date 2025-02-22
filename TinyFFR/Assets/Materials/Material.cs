// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly struct Material : IDisposableResource<Material, IMaterialImplProvider> {
	readonly ResourceHandle<Material> _handle;
	readonly IMaterialImplProvider _impl;

	internal ResourceHandle<Material> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Material)) : _handle;
	internal IMaterialImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Material>();

	IMaterialImplProvider IResource<Material, IMaterialImplProvider>.Implementation => Implementation;
	ResourceHandle<Material> IResource<Material>.Handle => Handle;

	public ReadOnlySpan<char> Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(_handle);
	}

	internal Material(ResourceHandle<Material> handle, IMaterialImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static Material IResource<Material>.CreateFromHandleAndImpl(ResourceHandle<Material> handle, IResourceImplProvider impl) {
		return new Material(handle, impl as IMaterialImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Material {(IsDisposed ? "(Disposed)" : $"\"{Name}\"")}";

	#region Equality
	public bool Equals(Material other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is Material other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(Material left, Material right) => left.Equals(right);
	public static bool operator !=(Material left, Material right) => !left.Equals(right);
	#endregion
}