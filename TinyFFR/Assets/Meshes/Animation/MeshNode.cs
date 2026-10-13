// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// One joint in a mesh's skeleton, which animations move and vertices follow. Obtained via a mesh's <see cref="Mesh.Skeleton"/>.
/// </summary>
/// <remarks>
/// <para>
/// Nodes are named, so a particular joint (a hand, say) can be found by name and its position read back after an animation has
/// been applied, which is how an object is made to follow a character's grip.
/// </para>
/// <para>
/// A node belongs to its mesh and is disposed along with it.
/// </para>
/// </remarks>
public readonly struct MeshNode : IResource<MeshNode, IMeshNodeImplProvider> {
	readonly ResourceHandle<MeshNode> _handle;
	readonly IMeshNodeImplProvider _impl;

	internal ResourceHandle<MeshNode> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(MeshNode)) : _handle;
	internal IMeshNodeImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<MeshNode>();

	IMeshNodeImplProvider IResource<MeshNode, IMeshNodeImplProvider>.Implementation => Implementation;
	ResourceHandle<MeshNode> IResource<MeshNode>.Handle => Handle;
	
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	/// <summary>
	/// This node's position in its skeleton's node list.
	/// </summary>
	/// <remarks>
	/// Indices are what the animation methods accept when several nodes are queried at once, since they can be put on the stack
	/// where the nodes themselves cannot.
	/// </remarks>
	public int Index {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetIndex(_handle);
	}
	
	internal MeshNode(ResourceHandle<MeshNode> handle, IMeshNodeImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static MeshNode IResource<MeshNode>.CreateFromHandleAndImpl(ResourceHandle<MeshNode> handle, IResourceImplProvider impl) {
		return new MeshNode(handle, impl as IMeshNodeImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<MeshNode> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<MeshNode> IResource<MeshNode>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	#region Disposal
	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion
	
	/// <inheritdoc />
	public override string ToString() => $"Mesh Node {_handle.AsInteger} \"{GetNameAsNewStringObject()}\"";

	#region Equality
	/// <inheritdoc />
	public bool Equals(MeshNode other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is MeshNode other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	/// <summary>
	/// Returns whether the two given nodes are the same node.
	/// </summary>
	/// <param name="left">The first node to compare.</param>
	/// <param name="right">The second node to compare.</param>
	public static bool operator ==(MeshNode left, MeshNode right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given nodes are different nodes.
	/// </summary>
	/// <param name="left">The first node to compare.</param>
	/// <param name="right">The second node to compare.</param>
	public static bool operator !=(MeshNode left, MeshNode right) => !left.Equals(right);
	#endregion
}