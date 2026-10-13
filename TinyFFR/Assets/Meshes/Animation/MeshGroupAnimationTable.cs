// Created on 2026-09-28 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// A skeleton and set of animations shared by a group of meshes, such as every skinned part of a character loaded from one file.
/// </summary>
/// <remarks>
/// <para>
/// Tables are created when loading a composite file that contains skeletal animation data, via
/// <see cref="IAssetLoader.LoadBundledAsset(ReadOnlySpan{char}, ReadOnlySpan{char})"/> or <see cref="IAssetLoader.LoadMeshGroup(ReadOnlySpan{char}, ReadOnlySpan{char})"/>.
/// The table is placed in the returned group alongside the meshes it animates (see <see cref="ResourceGroup.AnimationTables"/>), and disposing that group disposes the
/// table.
/// </para>
/// <para>
/// Applying one of the table's animations to a <see cref="ModelInstanceGroup"/> evaluates the skeleton's pose once and then poses every instance in the group from
/// it, which is considerably cheaper than animating each instance's mesh separately. Instances whose mesh is not part of this table are left unchanged.
/// </para>
/// <para>
/// Each mesh in the group also keeps its own per-mesh animations (see <see cref="Mesh.Animations"/>), so individual parts can still be animated separately if desired.
/// </para>
/// </remarks>
public readonly struct MeshGroupAnimationTable : IDisposableResource<MeshGroupAnimationTable, IMeshGroupAnimationTableImplProvider> {
	sealed class EmptyMeshGroupAnimationTableImplProvider : IMeshGroupAnimationTableImplProvider {
		const string EmptyTableName = "Empty Mesh Group Animation Table";
		public static readonly EmptyMeshGroupAnimationTableImplProvider Instance = new();

		EmptyMeshGroupAnimationTableImplProvider() { }

		public IndirectEnumerable<MeshGroupAnimationTable, MeshAnimation> GetAnimations(ResourceHandle<MeshGroupAnimationTable> handle, MeshAnimationType? type) => IndirectEnumerable<MeshGroupAnimationTable, MeshAnimation>.Empty;
		public MeshAnimation? TryGetAnimationByName(ResourceHandle<MeshGroupAnimationTable> handle, ReadOnlySpan<char> name, MeshAnimationType? type) => null;
		public IndirectEnumerable<MeshGroupAnimationTable, MeshNode> GetNodes(ResourceHandle<MeshGroupAnimationTable> handle) => IndirectEnumerable<MeshGroupAnimationTable, MeshNode>.Empty;
		public MeshNode? TryGetNodeByName(ResourceHandle<MeshGroupAnimationTable> handle, ReadOnlySpan<char> name) => null;
		public void ApplyBindPose(ResourceHandle<MeshGroupAnimationTable> handle, SceneObject targetInstance) { }
		public void GetBindPoseNodeTransforms(ResourceHandle<MeshGroupAnimationTable> handle, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) => ThrowForNonEmptyNodeRequest(nodes.Length);
		public void GetBindPoseNodeTransforms(ResourceHandle<MeshGroupAnimationTable> handle, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) => ThrowForNonEmptyNodeRequest(nodeIndices.Length);
		public string GetNameAsNewStringObject(ResourceHandle<MeshGroupAnimationTable> handle) => EmptyTableName;
		public int GetNameLength(ResourceHandle<MeshGroupAnimationTable> handle) => EmptyTableName.Length;
		public void CopyName(ResourceHandle<MeshGroupAnimationTable> handle, Span<char> destinationBuffer) => EmptyTableName.CopyTo(destinationBuffer);
		public bool IsDisposed(ResourceHandle<MeshGroupAnimationTable> handle) => false;
		public void Dispose(ResourceHandle<MeshGroupAnimationTable> handle) { }

		static void ThrowForNonEmptyNodeRequest(int requestedNodeCount) {
			if (requestedNodeCount > 0) throw new ArgumentException($"This mesh group skeleton has no nodes.");
		}
	}
	/// <summary>
	/// A table with no skeleton and no animations.
	/// </summary>
	/// <remarks>
	/// This is what <see cref="ModelBundle"/> and <see cref="ModelInstanceGroup"/> use when there is no real table to expose: applying its bind pose does nothing,
	/// its indices are empty, and disposing it does nothing.
	/// </remarks>
	public static readonly MeshGroupAnimationTable Empty = new(new ResourceHandle<MeshGroupAnimationTable>(0U), EmptyMeshGroupAnimationTableImplProvider.Instance);

	readonly ResourceHandle<MeshGroupAnimationTable> _handle;
	readonly IMeshGroupAnimationTableImplProvider _impl;

	internal ResourceHandle<MeshGroupAnimationTable> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(MeshGroupAnimationTable)) : _handle;
	internal IMeshGroupAnimationTableImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<MeshGroupAnimationTable>();

	IMeshGroupAnimationTableImplProvider IResource<MeshGroupAnimationTable, IMeshGroupAnimationTableImplProvider>.Implementation => Implementation;
	ResourceHandle<MeshGroupAnimationTable> IResource<MeshGroupAnimationTable>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	/// <summary>
	/// The animations in this table, addressable by name, by position or by kind.
	/// </summary>
	public MeshGroupAnimationIndex Animations {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(this);
	}

	/// <summary>
	/// The skeleton shared by every mesh in this table.
	/// </summary>
	public MeshGroupSkeleton Skeleton {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => new(this);
	}

	internal MeshGroupAnimationTable(ResourceHandle<MeshGroupAnimationTable> handle, IMeshGroupAnimationTableImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal IndirectEnumerable<MeshGroupAnimationTable, MeshAnimation> GetAnimations(MeshAnimationType? type) => Implementation.GetAnimations(_handle, type);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal MeshAnimation? TryGetAnimationByName(ReadOnlySpan<char> name, MeshAnimationType? type) => Implementation.TryGetAnimationByName(_handle, name, type);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal IndirectEnumerable<MeshGroupAnimationTable, MeshNode> GetNodes() => Implementation.GetNodes(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal MeshNode? TryGetNodeByName(ReadOnlySpan<char> name) => Implementation.TryGetNodeByName(_handle, name);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void ApplyBindPose(SceneObject targetInstance) => Implementation.ApplyBindPose(_handle, targetInstance);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void GetBindPoseNodeTransforms(ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) => Implementation.GetBindPoseNodeTransforms(_handle, nodes, modelSpaceTransforms);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void GetBindPoseNodeTransforms(ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) => Implementation.GetBindPoseNodeTransforms(_handle, nodeIndices, modelSpaceTransforms);

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static MeshGroupAnimationTable IResource<MeshGroupAnimationTable>.CreateFromHandleAndImpl(ResourceHandle<MeshGroupAnimationTable> handle, IResourceImplProvider impl) {
		return new MeshGroupAnimationTable(handle, impl as IMeshGroupAnimationTableImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<MeshGroupAnimationTable> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<MeshGroupAnimationTable> IResource<MeshGroupAnimationTable>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	#region Disposal
	/// <summary>
	/// Disposes this table.
	/// </summary>
	/// <remarks>
	/// Tables are normally owned by the resource group they were loaded in to, and are disposed along with it; you only need to dispose a table directly if you have
	/// removed it from that ownership.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	/// <inheritdoc />
	public override string ToString() => $"Model Bundle Animation Table {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	/// <inheritdoc />
	public bool Equals(MeshGroupAnimationTable other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is MeshGroupAnimationTable other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	/// <summary>
	/// <see cref="Equals(MeshGroupAnimationTable)"/>
	/// </summary>
	public static bool operator ==(MeshGroupAnimationTable left, MeshGroupAnimationTable right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(MeshGroupAnimationTable)"/>
	/// </summary>
	public static bool operator !=(MeshGroupAnimationTable left, MeshGroupAnimationTable right) => !left.Equals(right);
	#endregion
}
