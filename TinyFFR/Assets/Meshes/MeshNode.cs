// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

public readonly struct MeshNode : IResource<MeshNode, IMeshNodeImplProvider> {
	readonly ResourceHandle<MeshNode> _handle;
	readonly IMeshNodeImplProvider _impl;

	internal ResourceHandle<MeshNode> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(MeshNode)) : _handle;
	internal IMeshNodeImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<MeshNode>();

	IMeshNodeImplProvider IResource<MeshNode, IMeshNodeImplProvider>.Implementation => Implementation;
	ResourceHandle<MeshNode> IResource<MeshNode>.Handle => Handle;
	
	internal MeshNode(ResourceHandle<MeshNode> handle, IMeshNodeImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static MeshNode IResource<MeshNode>.CreateFromHandleAndImpl(ResourceHandle<MeshNode> handle, IResourceImplProvider impl) {
		return new MeshNode(handle, impl as IMeshNodeImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ResourceHandle<MeshNode> GetHandleWithoutDisposeCheck() => _handle;

	#region Disposal
	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion
	
	public override string ToString() => $"Mesh Node {_handle.AsInteger} \"{GetNameAsNewStringObject()}\"";

	#region Equality
	public bool Equals(MeshNode other) => _handle == other._handle && _impl.Equals(other._impl);
	public override bool Equals(object? obj) => obj is MeshNode other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	public static bool operator ==(MeshNode left, MeshNode right) => left.Equals(right);
	public static bool operator !=(MeshNode left, MeshNode right) => !left.Equals(right);
	#endregion
}