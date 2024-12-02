// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly struct VertexBuffer : IDisposableResource<VertexBuffer, VertexBufferHandle, IVertexBufferImplProvider> {
	readonly VertexBufferHandle _handle;
	readonly IVertexBufferImplProvider _impl;

	internal VertexBufferHandle Handle => IsDisposed ? throw new ObjectDisposedException(nameof(VertexBuffer)) : _handle;
	internal IVertexBufferImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<VertexBuffer>();

	IVertexBufferImplProvider IResource<VertexBufferHandle, IVertexBufferImplProvider>.Implementation => Implementation;
	VertexBufferHandle IResource<VertexBufferHandle, IVertexBufferImplProvider>.Handle => Handle;

	public ReadOnlySpan<char> Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetName(_handle);
	}

	internal VertexBuffer(VertexBufferHandle handle, IVertexBufferImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static VertexBuffer IResource<VertexBuffer>.RecreateFromRawHandleAndImpl(nuint rawHandle, IResourceImplProvider impl) {
		return new VertexBuffer(rawHandle, impl as IVertexBufferImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"Vertex Buffer {(IsDisposed ? "(Disposed)" : $"\"{Name}\"")}";

	#region Equality
	public bool Equals(VertexBuffer other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is VertexBuffer other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(VertexBuffer left, VertexBuffer right) => left.Equals(right);
	public static bool operator !=(VertexBuffer left, VertexBuffer right) => !left.Equals(right);
	#endregion
}