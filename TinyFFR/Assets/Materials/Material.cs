// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly struct Material : IDisposableResource<Material, IMaterialImplProvider> {
	readonly ResourceHandle<Material> _handle;
	readonly IMaterialImplProvider _impl;

	internal ResourceHandle<Material> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Material)) : _handle;
	internal IMaterialImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Material>();

	IMaterialImplProvider IResource<Material, IMaterialImplProvider>.Implementation => Implementation;
	ResourceHandle<Material> IResource<Material>.Handle => Handle;

	public bool SupportsPerInstanceEffects {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSupportsPerInstanceEffects(_handle);
	}

	internal Material(ResourceHandle<Material> handle, IMaterialImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Material Duplicate() => Implementation.Duplicate(_handle);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetEffectTransform(Transform2D transform) => Implementation.SetEffectTransform(_handle, transform);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetEffectBlendTexture(MaterialEffectMapType mapType, Texture blendTex) => Implementation.SetEffectBlendTexture(_handle, mapType, blendTex);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void SetEffectBlendDistance(MaterialEffectMapType mapType, float distance) => Implementation.SetEffectBlendDistance(_handle, mapType, distance);

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

	public override string ToString() => $"Material {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	public bool Equals(Material other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is Material other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(Material left, Material right) => left.Equals(right);
	public static bool operator !=(Material left, Material right) => !left.Equals(right);
	#endregion
}