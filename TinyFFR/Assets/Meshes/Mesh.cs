// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// The geometry of an object, i.e. its vertices and the triangles that join them. Created via the factory's <see cref="IMeshBuilder"/> or loaded via its <see cref="IAssetLoader"/>.
/// </summary>
/// <remarks>
/// <para>
/// A mesh describes shape only, never appearance; pairing it with a <see cref="Materials.Material"/> gives a
/// <see cref="Model"/>, and creating an instance of that puts it in a scene. One mesh can back any number of objects.
/// </para>
/// <para>
/// Meshes are created from shape descriptions or raw vertex data by the mesh builder, or loaded from a model file by the asset
/// loader. Dispose a mesh when nothing uses it any more.
/// </para>
/// </remarks>
public readonly struct Mesh : IDisposableResource<Mesh, IMeshImplProvider> {
	readonly ResourceHandle<Mesh> _handle;
	readonly IMeshImplProvider _impl;

	internal ResourceHandle<Mesh> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Mesh)) : _handle;
	internal IMeshImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Mesh>();

	IMeshImplProvider IResource<Mesh, IMeshImplProvider>.Implementation => Implementation;
	ResourceHandle<Mesh> IResource<Mesh>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	/// <summary>
	/// Identifies the GPU buffers this mesh is drawn from, and the region of the index buffer belonging to it.
	/// </summary>
	/// <remarks>
	/// This property is rarely useful in most user-code and is provided for advanced users.
	/// </remarks>
	public MeshBufferData BufferData {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetBufferData(_handle);
	}

	internal MeshBufferData? WireframeBufferData {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetWireframeBufferData(_handle);
	}
	
	/// <summary>
	/// Whether this mesh can also be drawn as a wireframe, showing its triangle edges rather than its surfaces.
	/// </summary>
	public bool SupportsWireframeRendering {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => WireframeBufferData != null;
	}

	/// <summary>
	/// The smallest box in the mesh's own space that contains all of its geometry.
	/// </summary>
	/// <remarks>
	/// This is the tight box, oriented with the mesh itself. It is the one to use when the mesh's actual extents matter, but it
	/// only stays valid while the mesh is unrotated.
	/// </remarks>
	public PositionedCuboid BoundingBox {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetBoundingBox(_handle);
	}
	
	/// <summary>
	/// A box in the mesh's own space that contains all of its geometry at any rotation.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Unlike <see cref="BoundingBox"/> this is deliberately enlarged so that it still contains the geometry however the mesh
	/// is turned, which is what makes it usable for the quick visibility and overlap checks a renderer performs every frame
	/// without recomputing anything as objects rotate.
	/// </para>
	/// <para>
	/// Because that enlargement is applied before any per-object scaling, this may only be scaled by the same factor on every
	/// axis. Scaling it per-axis produces a box too small to contain the geometry.
	/// </para>
	/// </remarks>
	public PositionedCuboid AxisAlignedBoundingBox {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetAxisAlignedBoundingBox(_handle);
	}
	
	/// <summary>
	/// A sphere in the mesh's own space that contains all of its geometry at any rotation.
	/// </summary>
	/// <remarks>
	/// Being a sphere, this is inherently unaffected by rotation. It describes the same volume as
	/// <see cref="AxisAlignedBoundingBox"/> and carries the same restriction: scale it by one factor on every axis, never
	/// per-axis.
	/// </remarks>
	public PositionedSphere BoundingSphere {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetBoundingSphere(_handle);
	}

	/// <summary>
	/// Whether objects using this mesh may alter their own copy of its vertices at runtime.
	/// </summary>
	/// <remarks>
	/// This is fixed when the mesh is created. Allowing it costs ordinary memory in addition to the usual video memory, since
	/// the vertices must then be kept on both sides.
	/// </remarks>
	public bool AllowsPerInstanceVertexMutation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetAllowsPerInstanceVertexMutation(_handle);
	}

	/// <summary>
	/// The animations attached to this mesh, addressable by name, by index or by kind.
	/// </summary>
	public MeshAnimationIndex Animations {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(this);
	}
	
	/// <summary>
	/// This mesh's skeleton: The tree of joints that a skeletal animation moves.
	/// </summary>
	public MeshSkeleton Skeleton {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(this);
	}

	internal Mesh(ResourceHandle<Mesh> handle, IMeshImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}
	
	/// <summary>
	/// Borrows this mesh's unaltered vertices for reading. <see cref="AllowsPerInstanceVertexMutation"/> must be <c>true</c> to use this method.
	/// </summary>
	/// <remarks>
	/// These are the vertices as the mesh was created, before any object's own alterations.
	/// </remarks>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="AllowsPerInstanceVertexMutation"/> is <c>false</c>.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ScopedReadOnlySpanLease<MeshVertex> BorrowDefaultVerticesSpan() => BorrowDefaultVerticesSpan(Range.All);
	/// <summary>
	/// Borrows part of this mesh's unaltered vertices for reading. <see cref="AllowsPerInstanceVertexMutation"/> must be <c>true</c> to use this method.
	/// </summary>
	/// <remarks>
	/// These are the vertices as the mesh was created, before any object's own alterations.
	/// </remarks>
	/// <param name="range">Which stretch of the vertex list to borrow.</param>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="AllowsPerInstanceVertexMutation"/> is <c>false</c>.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ScopedReadOnlySpanLease<MeshVertex> BorrowDefaultVerticesSpan(Range range) => Implementation.BorrowDefaultVerticesSpan(_handle, range);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal IndirectEnumerable<Mesh, MeshAnimation> GetAnimations(MeshAnimationType? type) => Implementation.GetAnimations(_handle, type);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal IndirectEnumerable<Mesh, MeshNode> GetNodes() => Implementation.GetNodes(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal MeshAnimation? TryGetAnimationByName(ReadOnlySpan<char> name, MeshAnimationType? type) => Implementation.TryGetAnimationByName(_handle, name, type);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal MeshNode? TryGetNodeByName(ReadOnlySpan<char> name) => Implementation.TryGetNodeByName(_handle, name);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void ApplySkeletalBindPose(ModelInstance targetInstance) => Implementation.ApplySkeletalBindPose(_handle, targetInstance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void GetSkeletalBindPoseNodeModelTransforms(ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) => Implementation.GetSkeletalBindPoseNodeModelTransforms(_handle, nodes, modelSpaceTransforms);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void GetSkeletalBindPoseNodeModelTransforms(ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) => Implementation.GetSkeletalBindPoseNodeModelTransforms(_handle, nodeIndices, modelSpaceTransforms);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static Mesh IResource<Mesh>.CreateFromHandleAndImpl(ResourceHandle<Mesh> handle, IResourceImplProvider impl) {
		return new Mesh(handle, impl as IMeshImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<Mesh> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<Mesh> IResource<Mesh>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	#region Disposal
	/// <summary>
	/// Disposes this mesh, releasing its GPU buffers.
	/// </summary>
	/// <remarks>
	/// Every object using this mesh must be disposed first.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	/// <inheritdoc />
	public override string ToString() => $"Mesh {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	/// <inheritdoc />
	public bool Equals(Mesh other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Mesh other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	/// <summary>
	/// Returns whether the two given meshs are the same mesh.
	/// </summary>
	/// <param name="left">The first mesh to compare.</param>
	/// <param name="right">The second mesh to compare.</param>
	public static bool operator ==(Mesh left, Mesh right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given meshs are different meshs.
	/// </summary>
	/// <param name="left">The first mesh to compare.</param>
	/// <param name="right">The second mesh to compare.</param>
	public static bool operator !=(Mesh left, Mesh right) => !left.Equals(right);
	#endregion
}