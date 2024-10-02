// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly struct Material : IDisposableResource<Material, MaterialHandle, IMaterialImplProvider> {
	readonly MaterialHandle _handle;
	readonly IMaterialImplProvider _impl;

	internal MaterialHandle Handle => _handle;
	internal IMaterialImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Material>();

	IMaterialImplProvider IResource<MaterialHandle, IMaterialImplProvider>.Implementation => Implementation;
	MaterialHandle IResource<MaterialHandle, IMaterialImplProvider>.Handle => Handle;

	public string Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(_handle);
	}

	internal Material(MaterialHandle handle, IMaterialImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static Material IResource<Material>.RecreateFromRawHandleAndImpl(nuint rawHandle, IResourceImplProvider impl) {
		return new Material(rawHandle, impl as IMaterialImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
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

	public override string ToString() => $"Material {(IsDisposed ? "(Disposed)" : $"\"{Name}\"")}";

	#region Equality
	public bool Equals(Material other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is Material other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(Material left, Material right) => left.Equals(right);
	public static bool operator !=(Material left, Material right) => !left.Equals(right);
	#endregion
}