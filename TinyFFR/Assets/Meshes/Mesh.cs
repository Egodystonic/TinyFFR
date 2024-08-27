// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly struct Mesh : IEquatable<Mesh>, IDisposable {
	readonly MeshAssetHandle _handle;
	readonly IMeshAssetImplProvider _impl;

	IMeshAssetImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Mesh>();

	public string Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(_handle);
	}

	internal Mesh(MeshAssetHandle handle, IMeshAssetImplProvider impl) {
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

	public override string ToString() => $"Mesh {(IsDisposed ? "(Disposed)" : $"\"{Name}\"")}";

	public bool Equals(Mesh other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is Mesh other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(Mesh left, Mesh right) => left.Equals(right);
	public static bool operator !=(Mesh left, Mesh right) => !left.Equals(right);
}