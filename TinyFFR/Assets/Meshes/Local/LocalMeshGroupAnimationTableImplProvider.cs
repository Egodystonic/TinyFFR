// Created on 2026-09-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Baking;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Threading;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes.Local;

sealed unsafe class LocalMeshGroupAnimationTableImplProvider : IMeshGroupAnimationTableImplProvider, IResourceDirectory<MeshGroupAnimationTable>, IDisposable, ILocalResourceImplProvider {
	const string DefaultTableName = "Model Bundle Animation Table";
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly LocalMeshAnimationTableProvider _tableProvider;
	readonly ArrayPoolBackedMap<ResourceHandle<MeshGroupAnimationTable>, LocalMeshAnimationTable> _activeTables = new();
	nuint _prevHandleId = 0U;
	bool _isDisposed = false;

	public LocalMeshGroupAnimationTableImplProvider(LocalFactoryGlobalObjectGroup globals, LocalMeshAnimationTableProvider tableProvider) {
		ArgumentNullException.ThrowIfNull(globals);
		ArgumentNullException.ThrowIfNull(tableProvider);
		_globals = globals;
		_tableProvider = tableProvider;
	}

	#region Creation
	public MeshGroupAnimationTable Create(
		ReadOnlySpan<SkeletalAnimationNode> boneFreeSkeletalNodes,
		Matrix4x4 modelImportTransformMatrix,
		ReadOnlySpan<Mesh> boneSetMeshes,
		ReadOnlySpan<int> boneSetBoneCounts,
		ReadOnlySpan<Matrix4x4> concatenatedRawBindPoseInversions,
		ReadOnlySpan<int> concatenatedBoneToRawNodeIndices,
		ReadOnlySpan<char> name
	) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();

		var table = _tableProvider.Rent();
		table.SetCombinedSkeleton(boneFreeSkeletalNodes, modelImportTransformMatrix, boneSetMeshes, boneSetBoneCounts, concatenatedRawBindPoseInversions, concatenatedBoneToRawNodeIndices);
		return Register(table, name);
	}

	MeshGroupAnimationTable Register(LocalMeshAnimationTable table, ReadOnlySpan<char> name) {
		var handle = new ResourceHandle<MeshGroupAnimationTable>(++_prevHandleId);
		_activeTables.Add(handle, table);
		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, name, DefaultTableName);

		var result = HandleToInstance(handle);
		for (var i = 0; i < table.BoneSetCount; ++i) {
			_globals.DependencyTracker.RegisterDependency(result, table.GetBoneSetMesh(i));
		}
		if (_globals.Bakery.Enabled) RegisterInBakery(result, table);
		return result;
	}

	void RegisterInBakery(MeshGroupAnimationTable resource, LocalMeshAnimationTable table) {
		var bakery = _globals.Bakery;
		bakery.StartResourceBake(resource);
		bakery.AddResourceBakeValue(resource, LocalAssetBakery.ResourceNameSectionName, _globals.GetResourceName(resource.GetHandleWithoutDisposeCheck().Ident, DefaultTableName));
		for (var i = 0; i < table.BoneSetCount; ++i) {
			bakery.AddResourceBakeReference(resource, BakedResourceSchemata.BakedReferenceSlot.AnimationTableMesh, table.GetBoneSetMesh(i));
		}
		bakery.CompleteResourceBakePendingFinalization(resource, this, &FinalizeBake);
	}

	static void FinalizeBake(object invoker, LocalAssetBakery bakery, MeshGroupAnimationTable resource) {
		var self = (LocalMeshGroupAnimationTableImplProvider) invoker;
		if (!self._activeTables.TryGetValue(resource.GetHandleWithoutDisposeCheck(), out var table)) return;
		table.WriteCombinedBakeData(bakery, resource);
	}

	internal MeshGroupAnimationTable CreateFromBakedData(
		ReadOnlySpan<char> name,
		int nodeCount,
		int firstParentedNodeIndex,
		Matrix4x4 modelImportTransformMatrix,
		ReadOnlySpan<Matrix4x4> defaultLocalTransforms,
		ReadOnlySpan<int> parentIndices,
		ReadOnlySpan<int> mutationTargetIndexMap,
		ReadOnlySpan<Mesh> boneSetMeshes,
		ReadOnlySpan<int> boneSetBoneCounts,
		ReadOnlySpan<Matrix4x4> concatenatedBindPoseInversions,
		ReadOnlySpan<int> concatenatedBoneToNodeMaps
	) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposed();

		var table = _tableProvider.Rent();
		table.SetCombinedSkeletonFromBakedData(nodeCount, firstParentedNodeIndex, modelImportTransformMatrix, defaultLocalTransforms, parentIndices, mutationTargetIndexMap, boneSetMeshes, boneSetBoneCounts, concatenatedBindPoseInversions, concatenatedBoneToNodeMaps);
		return Register(table, name);
	}

	internal void RestoreBakedAnimation(
		MeshGroupAnimationTable table,
		ReadOnlySpan<SkeletalAnimationScalingKeyframe> scalingKeyframes,
		ReadOnlySpan<SkeletalAnimationRotationKeyframe> rotationKeyframes,
		ReadOnlySpan<SkeletalAnimationTranslationKeyframe> translationKeyframes,
		ReadOnlySpan<SkeletalAnimationNodeMutationDescriptor> nodeMutations,
		float defaultCompletionTimeSeconds,
		ReadOnlySpan<char> name
	) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		GetTableOrThrow(table.Handle).AddPreProcessedAnimation(scalingKeyframes, rotationKeyframes, translationKeyframes, nodeMutations, defaultCompletionTimeSeconds, name);
	}

	internal void RestoreBakedNodeName(MeshGroupAnimationTable table, int nodeIndex, ReadOnlySpan<char> name) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		GetTableOrThrow(table.Handle).SetNodeNameFromBakedData(nodeIndex, name);
	}

	public MeshAnimation AttachAnimationAndTransferBufferOwnership(
		MeshGroupAnimationTable table,
		PooledHeapMemory<SkeletalAnimationScalingKeyframe> scalingKeyframes,
		PooledHeapMemory<SkeletalAnimationRotationKeyframe> rotationKeyframes,
		PooledHeapMemory<SkeletalAnimationTranslationKeyframe> translationKeyframes,
		PooledHeapMemory<SkeletalAnimationNodeMutationDescriptor> boneMutations,
		float defaultCompletionTimeSeconds,
		ReadOnlySpan<char> name
	) {
		var animTable = GetTableOrThrow(table.Handle);
		return animTable.AddAndTransferBufferOwnership(scalingKeyframes, rotationKeyframes, translationKeyframes, boneMutations, defaultCompletionTimeSeconds, name);
	}

	public void SetNodeName(MeshGroupAnimationTable table, int nodeIndex, ReadOnlySpan<char> name) {
		GetTableOrThrow(table.Handle).SetNodeName(nodeIndex, name);
	}

	internal LocalMeshAnimationTable GetUnderlyingTable(MeshGroupAnimationTable table) => GetTableOrThrow(table.Handle);
	#endregion

	#region Animations & Nodes
	LocalMeshAnimationTable GetTableOrThrow(ResourceHandle<MeshGroupAnimationTable> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _activeTables[handle];
	}

	static LocalMeshAnimationTable? GetTableForIterator(MeshGroupAnimationTable t) {
		if (t.Implementation is not LocalMeshGroupAnimationTableImplProvider provider) return null;
		return provider._activeTables.TryGetValue(t.GetHandleWithoutDisposeCheck(), out var result) ? result : null;
	}

	public IndirectEnumerable<MeshGroupAnimationTable, MeshAnimation> GetAnimations(ResourceHandle<MeshGroupAnimationTable> handle, MeshAnimationType? type) {
		var animTable = GetTableOrThrow(handle);
		if (type == MeshAnimationType.Morphing) return IndirectEnumerable<MeshGroupAnimationTable, MeshAnimation>.Empty;

		static int GetCount(MeshGroupAnimationTable t) => GetTableForIterator(t)?.Count ?? -1;
		static MeshAnimation GetItem(MeshGroupAnimationTable t, int index) => GetTableForIterator(t)?.GetAnimationAtUnstableIndex(index) ?? throw new InvalidOperationException("Somehow attempted to access null animation table.");

		return new IndirectEnumerable<MeshGroupAnimationTable, MeshAnimation>(
			HandleToInstance(handle),
			animTable.Count,
			&GetCount,
			&GetCount,
			&GetItem
		);
	}

	public MeshAnimation? TryGetAnimationByName(ResourceHandle<MeshGroupAnimationTable> handle, ReadOnlySpan<char> name, MeshAnimationType? type) {
		var match = GetTableOrThrow(handle).FindByName(name);
		if (match == null || (type != null && type != match.Value.Type)) return null;
		return match;
	}

	public IndirectEnumerable<MeshGroupAnimationTable, MeshNode> GetNodes(ResourceHandle<MeshGroupAnimationTable> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);

		static int GetCount(MeshGroupAnimationTable t) => GetTableForIterator(t)?.GetNodeCount() ?? -1;
		static int GetVersion(MeshGroupAnimationTable _) => 0;
		static MeshNode GetItem(MeshGroupAnimationTable t, int index) => GetTableForIterator(t)?.GetNode(index) ?? throw new InvalidOperationException("Somehow attempted to access null animation table.");

		return new IndirectEnumerable<MeshGroupAnimationTable, MeshNode>(
			HandleToInstance(handle),
			0,
			&GetCount,
			&GetVersion,
			&GetItem
		);
	}

	public MeshNode? TryGetNodeByName(ResourceHandle<MeshGroupAnimationTable> handle, ReadOnlySpan<char> name) => GetTableOrThrow(handle).TryGetNode(name);
	public void ApplyBindPose(ResourceHandle<MeshGroupAnimationTable> handle, SceneObject targetInstance) => GetTableOrThrow(handle).ApplyBindPose(targetInstance);
	public void GetBindPoseNodeTransforms(ResourceHandle<MeshGroupAnimationTable> handle, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) => GetTableOrThrow(handle).GetBindPoseNodeTransforms(nodes, modelSpaceTransforms);
	public void GetBindPoseNodeTransforms(ResourceHandle<MeshGroupAnimationTable> handle, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) => GetTableOrThrow(handle).GetBindPoseNodeTransforms(nodeIndices, modelSpaceTransforms);
	#endregion

	#region Names
	public string GetNameAsNewStringObject(ResourceHandle<MeshGroupAnimationTable> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultTableName));
	}
	public int GetNameLength(ResourceHandle<MeshGroupAnimationTable> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultTableName).Length;
	}
	public void CopyName(ResourceHandle<MeshGroupAnimationTable> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultTableName, destinationBuffer);
	}
	#endregion

	#region Resource Directory
	public IndirectEnumerable<object, MeshGroupAnimationTable> AllActiveInstances {
		get {
			static LocalMeshGroupAnimationTableImplProvider CastSelf(object self) => self as LocalMeshGroupAnimationTableImplProvider ?? throw new InvalidOperationException($"Enumeration invoked on {self?.GetType().Name}.");
			static int GetCount(object self) => CastSelf(self)._activeTables.Count;
			static int GetVersion(object self) => CastSelf(self)._activeTables.Version;
			static MeshGroupAnimationTable GetItem(object self, int index) => CastSelf(self).HandleToInstance(CastSelf(self)._activeTables.GetPairAtIndex(index).Key);

			ThrowIfThisIsDisposed();
			return new(
				this,
				GetVersion(this),
				&GetCount,
				&GetVersion,
				&GetItem
			);
		}
	}

	public bool ResourceNameMatchIsMatching(MeshGroupAnimationTable resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType) {
		var handle = resource.GetHandleWithoutDisposeCheck();
		ThrowIfThisOrHandleIsDisposed(handle);
		return allowPartialMatch
			? _globals.GetResourceName(handle.Ident, DefaultTableName).Contains(name, comparisonType)
			: _globals.GetResourceName(handle.Ident, DefaultTableName).Equals(name, comparisonType);
	}
	#endregion

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	MeshGroupAnimationTable HandleToInstance(ResourceHandle<MeshGroupAnimationTable> h) => new(h, this);

	public override string ToString() => _isDisposed ? "TinyFFR Local Model Bundle Animation Table Impl Provider [Disposed]" : "TinyFFR Local Model Bundle Animation Table Impl Provider";

	#region Disposal
	public bool IsDisposed(ResourceHandle<MeshGroupAnimationTable> handle) => _isDisposed || !_activeTables.ContainsKey(handle);

	public void Dispose(ResourceHandle<MeshGroupAnimationTable> handle) => Dispose(handle, removeFromMap: true);
	void Dispose(ResourceHandle<MeshGroupAnimationTable> handle, bool removeFromMap) {
		if (IsDisposed(handle)) return;
		var instance = HandleToInstance(handle);
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(instance);
		_globals.Bakery.DiscardBakeryDataIfPresent(instance);

		var table = _activeTables[handle];
		for (var i = 0; i < table.BoneSetCount; ++i) {
			_globals.DependencyTracker.DeregisterDependency(instance, table.GetBoneSetMesh(i));
		}
		_tableProvider.Return(table);
		_globals.DisposeResourceNameIfExists(handle.Ident);

		if (removeFromMap) _activeTables.Remove(handle);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var kvp in _activeTables) Dispose(kvp.Key, removeFromMap: false);
			_activeTables.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<MeshGroupAnimationTable> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(MeshGroupAnimationTable));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}
