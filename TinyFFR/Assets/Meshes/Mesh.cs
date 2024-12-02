// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly struct Mesh : IDisposableResource<Mesh, MeshHandle, IMeshImplProvider> {
	readonly MeshHandle _handle;
	readonly IMeshImplProvider _impl;

	internal MeshHandle Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Mesh)) : _handle;
	internal IMeshImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Mesh>();

	IMeshImplProvider IResource<MeshHandle, IMeshImplProvider>.Implementation => Implementation;
	MeshHandle IResource<MeshHandle, IMeshImplProvider>.Handle => Handle;

	public ReadOnlySpan<char> Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(_handle);
	}

	public MeshBufferData BufferData {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetBufferData(_handle);
	}

	internal Mesh(MeshHandle handle, IMeshImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static Mesh IResource<Mesh>.RecreateFromRawHandleAndImpl(nuint rawHandle, IResourceImplProvider impl) {
		return new Mesh(rawHandle, impl as IMeshImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Mesh {(IsDisposed ? "(Disposed)" : $"\"{Name}\"")}";

	#region Equality
	public bool Equals(Mesh other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is Mesh other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(Mesh left, Mesh right) => left.Equals(right);
	public static bool operator !=(Mesh left, Mesh right) => !left.Equals(right);
	#endregion
}