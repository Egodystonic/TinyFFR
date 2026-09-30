// Created on 2026-09-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Threading;

namespace Egodystonic.TinyFFR.Assets.Local;

unsafe partial class LocalAssetLoader {
	sealed class CombinedSkeletonAccumulator : IDisposable {
		readonly ArrayPoolBackedVector<NodeHandle> _uniqueNodes = new();
		readonly ArrayPoolBackedMap<UIntPtr, int> _uniqueNodeIndicesByPointer = new();
		readonly ArrayPoolBackedVector<int> _uniqueNodeParents = new();
		readonly ArrayPoolBackedVector<int> _boneSetBoneCounts = new();
		readonly ArrayPoolBackedVector<int> _boneToUniqueNodeIndices = new();
		readonly ArrayPoolBackedVector<Matrix4x4> _rawBindPoseInversions = new();
		readonly ArrayPoolBackedVector<Mesh> _boneSetMeshes = new();
		readonly ArrayPoolBackedVector<SkeletalAnimationNode> _combinedNodes = new();
		readonly ArrayPoolBackedVector<int> _boneToCombinedNodeIndices = new();
		Matrix4x4 _modelImportTransformMatrix = Matrix4x4.Identity;
		bool _isBuilt = false;

		public MeshSkeletalGatherBuffers GatheredData { get; } = new();
		public int BoneSetCount => _boneSetBoneCounts.Count;

		public void AddSkeletalSubMeshOnWorker(ReadOnlySpan<NodeHandle> rawNodeHandles, ReadOnlySpan<SkeletalAnimationNode> rawNodes, int boneCount, ThreadSafeHeapPoolWrapper heapPool) {
			if (_isBuilt) throw new InvalidOperationException("Combined skeleton has already been built (this is a bug in TinyFFR).");

			using var rawToUniqueIndicesHeapMemory = heapPool.Borrow<int>(rawNodes.Length);
			var rawToUniqueIndices = rawToUniqueIndicesHeapMemory.Span;
			for (var n = 0; n < rawNodes.Length; ++n) {
				var nodePointer = rawNodeHandles[n].NodePointer;
				if (!_uniqueNodeIndicesByPointer.TryGetValue(nodePointer, out var uniqueIndex)) {
					uniqueIndex = _uniqueNodes.Count;
					_uniqueNodes.Add(new NodeHandle(nodePointer));
					_uniqueNodeParents.Add(-1);
					_uniqueNodeIndicesByPointer.Add(nodePointer, uniqueIndex);
				}
				rawToUniqueIndices[n] = uniqueIndex;
			}

			var boneToUniqueStart = _boneToUniqueNodeIndices.Count;
			for (var b = 0; b < boneCount; ++b) {
				_boneToUniqueNodeIndices.Add(-1);
				_rawBindPoseInversions.Add(Matrix4x4.Identity);
			}

			for (var n = 0; n < rawNodes.Length; ++n) {
				var uniqueIndex = rawToUniqueIndices[n];
				if (rawNodes[n].ParentNodeIndex is { } parentRawIndex) _uniqueNodeParents[uniqueIndex] = rawToUniqueIndices[parentRawIndex];
				if (rawNodes[n].CorrespondingBoneIndex is { } boneIndex) {
					_boneToUniqueNodeIndices[boneToUniqueStart + boneIndex] = uniqueIndex;
					_rawBindPoseInversions[boneToUniqueStart + boneIndex] = rawNodes[n].BindPoseInversion;
				}
			}

			for (var b = 0; b < boneCount; ++b) {
				if (_boneToUniqueNodeIndices[boneToUniqueStart + b] < 0) {
					throw new InvalidOperationException($"Bone {b} of skeletal sub-mesh has no corresponding node (this is a bug in TinyFFR).");
				}
			}
			_boneSetBoneCounts.Add(boneCount);
		}

		public void AddBoneSetMeshOnPrimary(Mesh mesh) {
			ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
			_boneSetMeshes.Add(mesh);
		}

		public void BuildOnWorker(UIntPtr assetHandle, Vect originTranslation, float linearRescalingFactor, float? animationTicksPerSecondOverride, InteropStringBuffer? nameBuffer, ThreadSafeHeapPoolWrapper heapPool) {
			if (_isBuilt) throw new InvalidOperationException("Combined skeleton has already been built (this is a bug in TinyFFR).");
			_isBuilt = true;
			if (BoneSetCount == 0) return;
			if (nameBuffer is not { } nameBufferValue) throw new InvalidOperationException("Name buffer was null despite skeletal data being gathered (this is a bug in TinyFFR).");

			_modelImportTransformMatrix = LocalMeshBuilder.CalculateModelImportTransformMatrix(originTranslation, linearRescalingFactor);

			var nodeCount = _uniqueNodes.Count;
			using var depths = heapPool.Borrow<int>(nodeCount);
			using var uniqueToCombinedIndices = heapPool.Borrow<int>(nodeCount);
			using var combinedNodeHandles = heapPool.Borrow<NodeHandle>(nodeCount);

			var maxDepth = 0;
			for (var n = 0; n < nodeCount; ++n) {
				var depth = 0;
				for (var parent = _uniqueNodeParents[n]; parent >= 0; parent = _uniqueNodeParents[parent]) {
					if (++depth > nodeCount) throw new InvalidOperationException("Cyclical skeletal node hierarchy detected (this is a bug in TinyFFR).");
				}
				depths.Span[n] = depth;
				maxDepth = Int32.Max(maxDepth, depth);
			}

			var cursor = 0;
			for (var depth = 0; depth <= maxDepth; ++depth) {
				for (var n = 0; n < nodeCount; ++n) {
					if (depths.Span[n] != depth) continue;
					uniqueToCombinedIndices.Span[n] = cursor;
					combinedNodeHandles.Span[cursor] = _uniqueNodes[n];
					++cursor;
				}
			}

			var maxNodeNameLength = 0;
			fixed (NodeHandle* combinedNodeHandlesPtr = combinedNodeHandles.Span) {
				for (var n = 0; n < nodeCount; ++n) {
					GetLoadedAssetMeshSkeletalNode(
						combinedNodeHandlesPtr,
						nodeCount,
						n,
						out _,
						out var defaultTransformMatrix,
						out var parentNodeIndex,
						out _,
						out var nameLength
					).ThrowIfFailure();

					_combinedNodes.Add(new SkeletalAnimationNode(
						defaultTransformMatrix,
						Matrix4x4.Identity,
						parentNodeIndex >= 0 ? parentNodeIndex : null,
						null
					));
					maxNodeNameLength = Int32.Max(nameLength, maxNodeNameLength);
				}

				GatherMeshAnimations(assetHandle, combinedNodeHandlesPtr, nodeCount, animationTicksPerSecondOverride, nameBufferValue, heapPool, GatheredData);
				GatherNodeNames(new ReadOnlySpan<NodeHandle>(combinedNodeHandlesPtr, nodeCount), maxNodeNameLength, nameBufferValue, heapPool, GatheredData);
			}

			for (var b = 0; b < _boneToUniqueNodeIndices.Count; ++b) {
				_boneToCombinedNodeIndices.Add(uniqueToCombinedIndices.Span[_boneToUniqueNodeIndices[b]]);
			}
		}

		public MeshGroupAnimationTable? CreateTableOnPrimary(LocalMeshGroupAnimationTableImplProvider provider, ReadOnlySpan<char> name) {
			ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
			if (!_isBuilt || BoneSetCount == 0) return null;
			if (_boneSetMeshes.Count != BoneSetCount) {
				throw new InvalidOperationException($"Combined skeleton has {BoneSetCount} bone sets but {_boneSetMeshes.Count} meshes (this is a bug in TinyFFR).");
			}

			var result = provider.Create(
				_combinedNodes.AsSpan,
				_modelImportTransformMatrix,
				_boneSetMeshes.AsSpan,
				_boneSetBoneCounts.AsSpan,
				_rawBindPoseInversions.AsSpan,
				_boneToCombinedNodeIndices.AsSpan,
				name
			);

			try {
				for (var i = 0; i < GatheredData.Animations.Count; ++i) {
					var animation = GatheredData.Animations[i];
					provider.AttachAnimationAndTransferBufferOwnership(
						result,
						animation.ScalingKeyframes,
						animation.RotationKeyframes,
						animation.TranslationKeyframes,
						animation.Mutations,
						animation.DurationSeconds,
						GatheredData.GetName(animation.Name)
					);
					GatheredData.NumAnimationsWithTransferredOwnership = i + 1;
				}
				for (var i = 0; i < GatheredData.NodeNameSlices.Count; ++i) {
					provider.SetNodeName(result, i, GatheredData.GetName(GatheredData.NodeNameSlices[i]));
				}
			}
			catch {
				result.Dispose();
				throw;
			}

			return result;
		}

		public void Reset() {
			_uniqueNodes.ClearWithoutZeroingMemory();
			_uniqueNodeIndicesByPointer.Clear();
			_uniqueNodeParents.ClearWithoutZeroingMemory();
			_boneSetBoneCounts.ClearWithoutZeroingMemory();
			_boneToUniqueNodeIndices.ClearWithoutZeroingMemory();
			_rawBindPoseInversions.ClearWithoutZeroingMemory();
			_boneSetMeshes.Clear();
			_combinedNodes.ClearWithoutZeroingMemory();
			_boneToCombinedNodeIndices.ClearWithoutZeroingMemory();
			_modelImportTransformMatrix = Matrix4x4.Identity;
			_isBuilt = false;
			GatheredData.DisposeUntransferredBuffersAndReset();
		}

		public void Dispose() {
			Reset();
			_uniqueNodes.Dispose();
			_uniqueNodeIndicesByPointer.Dispose();
			_uniqueNodeParents.Dispose();
			_boneSetBoneCounts.Dispose();
			_boneToUniqueNodeIndices.Dispose();
			_rawBindPoseInversions.Dispose();
			_boneSetMeshes.Dispose();
			_combinedNodes.Dispose();
			_boneToCombinedNodeIndices.Dispose();
			GatheredData.Dispose();
		}
	}
}
