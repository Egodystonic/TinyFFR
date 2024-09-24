// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Materials;

public readonly struct Material : IEquatable<Material>, IDisposable {
	readonly MaterialHandle _handle;
	readonly IMaterialAssetImplProvider _impl;

	internal IMaterialAssetImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Material>();
	internal MaterialHandle Handle => _handle;

	public string Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(_handle);
	}

	internal Material(MaterialHandle handle, IMaterialAssetImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameUsingSpan(Span<char> dest) => Implementation.GetNameUsingSpan(_handle, dest);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameSpanMaxLength() => Implementation.GetNameSpanMaxLength(_handle);

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	public bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Material {(IsDisposed ? "(Disposed)" : $"\"{Name}\"")}";

	public bool Equals(Material other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is Material other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(Material left, Material right) => left.Equals(right);
	public static bool operator !=(Material left, Material right) => !left.Equals(right);
}