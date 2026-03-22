// Created on 2026-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Globalization;
using System.Security;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Meshes.Local;

[SuppressUnmanagedCodeSecurity]
sealed unsafe class LocalMeshAnimationTable : IMeshAnimationImplProvider, IDisposable {
	const string DefaultNodeNamePrefix = "unnamed_node_";
	readonly record struct StartingAnimationData(ResourceHandle<MeshAnimation> AnimHandle, float TimePointSeconds);
	readonly record struct EndingAnimationData(ResourceHandle<MeshAnimation> AnimHandle, float TimePointSeconds, float InterpolationDistance);
	readonly record struct AnimationData(
		PooledHeapMemory<SkeletalAnimationScalingKeyframe> ScalingKeyframes,
		PooledHeapMemory<SkeletalAnimationRotationKeyframe> RotationKeyframes,
		PooledHeapMemory<SkeletalAnimationTranslationKeyframe> TranslationKeyframes,
		PooledHeapMemory<SkeletalAnimationNodeMutationDescriptor> BoneMutationDescriptors,
		float DefaultCompletionTimeSeconds
	);
	readonly record struct SkeletonData(
		Mesh OwningMesh,
		int NodeCount,
		int BoneCount,
		int FirstParentedNodeIndex,
		PooledHeapMemory<Matrix4x4> DefaultLocalTransforms,
		PooledHeapMemory<Matrix4x4> BindPoseInversions,
		PooledHeapMemory<Matrix4x4> Workspace,
		PooledHeapMemory<int> ParentIndices,
		PooledHeapMemory<int> BoneToNodeMap,
		PooledHeapMemory<int> MutationTargetIndexMap,
		Matrix4x4 ModelImportTransformMatrix
	);
	static nuint _nextHandleId = 0U;
	readonly MeshNodeImplProvider _meshNodeImplProvider;
	readonly ArrayPoolBackedStringKeyMap<MeshAnimation> _animationNameMap = new();
	readonly ArrayPoolBackedMap<ResourceHandle<MeshAnimation>, AnimationData> _animationDataMap = new();
	readonly ArrayPoolBackedStringKeyMap<MeshNode> _nodeNameMap = new();
	readonly UnmanagedBuffer<Matrix4x4> _applyTransformsBuffer = new(IMeshBuilder.MaxSkeletalBoneCount, alignment: 64); // 64 byte alignment keeps each matrix on its own cache line
	readonly LocalFactoryGlobalObjectGroup _globals;
	SkeletonData? _currentSkeleton = null;
	bool _isDisposed = false;

	public LocalMeshAnimationTable(LocalFactoryGlobalObjectGroup globals) {
		_globals = globals;
		_meshNodeImplProvider = new(this);
	}

	sealed class MeshNodeImplProvider : IMeshNodeImplProvider {
		readonly LocalMeshAnimationTable _owner;
		public MeshNodeImplProvider(LocalMeshAnimationTable owner) => _owner = owner;
		
		public string GetNameAsNewStringObject(ResourceHandle<MeshNode> handle) => _owner.GetNameAsNewStringObject(handle);
		public int GetNameLength(ResourceHandle<MeshNode> handle) => _owner.GetNameLength(handle);
		public void CopyName(ResourceHandle<MeshNode> handle, Span<char> destinationBuffer) => _owner.CopyName(handle, destinationBuffer);
		public bool IsDisposed(ResourceHandle<MeshNode> handle) => _owner.IsDisposed(handle);
		public int GetIndex(ResourceHandle<MeshNode> handle) => _owner.GetIndex(handle);
	}

	#region Initialization + Setup
	public void SetSkeleton(Mesh owningMesh, int boneCount, ReadOnlySpan<SkeletalAnimationNode> skeletalNodes, Matrix4x4 modelImportTransformMatrix) {
		var nodeCount = skeletalNodes.Length;
		if (_currentSkeleton != null) {
			throw new InvalidOperationException("Skeleton already set for this animation table (this is a bug in TinyFFR).");
		}
		if (boneCount is < 1 or > IMeshBuilder.MaxSkeletalBoneCount) {
			throw new InvalidOperationException($"Bone count = {boneCount} (this is a bug in TinyFFR).");
		}
		
		// Maintainer's note: This first step calculates the optimal processing order of the nodes so we can do a single-pass
		// calculation of the animation transform matrices in Apply().
		// This algorithm works by determining the dependency depth of each bone index.
		// Bones with the same depth can't rely on each other, so a simple ordering of bones by dependency depth means
		// we'll always process them in the right order down below when applying an animation.
		// (The reordering itself is necessary as when processing the animation, some transformations rely on transforms
		// calculated on parent bones higher up the skeletal hierarchy first).
		Span<int> depths = stackalloc int[nodeCount];
		Span<int> processingOrder = stackalloc int[nodeCount];
		var inputToOutputIndexMapHeapMemory = _globals.HeapPool.Borrow<int>(nodeCount);
		var maxDepth = 0;
		
		for (var i = 0; i < nodeCount; ++i) {
			var parentNodeIndex = skeletalNodes[i].ParentNodeIndex;
			while (parentNodeIndex is { } pni) {
				if (pni < 0 || pni >= skeletalNodes.Length) {
					throw new ArgumentException(
						$"Given skeletal node at index {i} refers to parent node at index '{pni}'; " +
						$"but only {nodeCount} nodes were supplied.",
						nameof(skeletalNodes)
					);
				}
				maxDepth = Int32.Max(maxDepth, ++depths[i]);
				parentNodeIndex = skeletalNodes[pni].ParentNodeIndex;
			}
		}
		var processingOrderCursor = 0;
		for (var curDepthToSearchFor = 0; curDepthToSearchFor <= maxDepth; ++curDepthToSearchFor) {
			for (var i = 0; i < nodeCount; ++i) {
				if (depths[i] == curDepthToSearchFor) {
					inputToOutputIndexMapHeapMemory.Buffer[i] = processingOrderCursor;
					processingOrder[processingOrderCursor++] = i;
				}
			}
		}
		
		var defaultLocalTransformsHeapMemory = _globals.HeapPool.Borrow<Matrix4x4>(nodeCount);
		var workspaceHeapMemory = _globals.HeapPool.Borrow<Matrix4x4>(nodeCount);
		var parentIndicesHeapMemory = _globals.HeapPool.Borrow<int>(nodeCount);
		var boneToNodeMapHeapMemory = _globals.HeapPool.Borrow<int>(boneCount);
		var bindPoseInversionsHeapMemory = _globals.HeapPool.Borrow<Matrix4x4>(boneCount);
		
		var firstParentedNodeIndex = 0;
		for (var outputIndex = 0; outputIndex < processingOrder.Length; ++outputIndex) {
			var inputIndex = processingOrder[outputIndex];
			defaultLocalTransformsHeapMemory.Buffer[outputIndex] = skeletalNodes[inputIndex].DefaultLocalTransform;
			
			if (skeletalNodes[inputIndex].ParentNodeIndex is { } parentNodeIndex) {
				parentIndicesHeapMemory.Buffer[outputIndex] = inputToOutputIndexMapHeapMemory.Buffer[parentNodeIndex];
			}
			else firstParentedNodeIndex = outputIndex + 1;
			
			if (skeletalNodes[inputIndex].CorrespondingBoneIndex is { } boneIndex) {
				boneToNodeMapHeapMemory.Buffer[boneIndex] = outputIndex;
				bindPoseInversionsHeapMemory.Buffer[boneIndex] = skeletalNodes[inputIndex].BindPoseInversion;
			}
		}

		if (Matrix4x4.Invert(modelImportTransformMatrix, out var inverseModelImportTransformMatrix)) {
			for (var i = 0; i < boneCount; ++i) {
				bindPoseInversionsHeapMemory.Buffer[i] = inverseModelImportTransformMatrix * bindPoseInversionsHeapMemory.Buffer[i];
			}	
		}

		_currentSkeleton = new(
			owningMesh,
			nodeCount,
			boneCount,
			firstParentedNodeIndex,
			defaultLocalTransformsHeapMemory,
			bindPoseInversionsHeapMemory,
			workspaceHeapMemory,
			parentIndicesHeapMemory,
			boneToNodeMapHeapMemory,
			inputToOutputIndexMapHeapMemory,
			modelImportTransformMatrix
		);
	}
	SkeletonData GetSkeletonOrThrow() => _currentSkeleton ?? throw new InvalidOperationException("No skeleton set for this animation table (this is a bug in TinyFFR).");

	public MeshAnimation Add( 
		ReadOnlySpan<SkeletalAnimationScalingKeyframe> scalingKeyframes,
		ReadOnlySpan<SkeletalAnimationRotationKeyframe> rotationKeyframes, 
		ReadOnlySpan<SkeletalAnimationTranslationKeyframe> translationKeyframes, 
		ReadOnlySpan<SkeletalAnimationNodeMutationDescriptor> nodeMutations, 
		float defaultCompletionTimeSeconds, 
		ReadOnlySpan<char> name
	) {
		ThrowIfThisIsDisposed();
		
		var scalingHeapBuffer = _globals.HeapPool.BorrowAndCopy(scalingKeyframes);
		var rotationHeapBuffer = _globals.HeapPool.BorrowAndCopy(rotationKeyframes);
		var translationHeapBuffer = _globals.HeapPool.BorrowAndCopy(translationKeyframes);
		var mutationHeapBuffer = _globals.HeapPool.BorrowAndCopy(nodeMutations);
		
		return AddAndTransferBufferOwnership(scalingHeapBuffer, rotationHeapBuffer, translationHeapBuffer, mutationHeapBuffer, defaultCompletionTimeSeconds, name);
	}
	public MeshAnimation AddAndTransferBufferOwnership( 
		PooledHeapMemory<SkeletalAnimationScalingKeyframe> scalingKeyframes, 
		PooledHeapMemory<SkeletalAnimationRotationKeyframe> rotationKeyframes, 
		PooledHeapMemory<SkeletalAnimationTranslationKeyframe> translationKeyframes, 
		PooledHeapMemory<SkeletalAnimationNodeMutationDescriptor> nodeMutations, 
		float defaultCompletionTimeSeconds, 
		ReadOnlySpan<char> name
	) {
		ThrowIfThisIsDisposed();
		var skeleton = GetSkeletonOrThrow();
		static void EnsureKeyframesOrderedByTime<T, TValue>(ReadOnlySpan<T> keyframes) where T : IAnimationKeyframe<TValue> {
			for (var i = 1; i < keyframes.Length; ++i) {
				if (keyframes[i].TimeKeySeconds < keyframes[i - 1].TimeKeySeconds) {
					throw new ArgumentException($"{typeof(T).Name} at index {i} had time key [{keyframes[i].TimeKeySeconds:N3}secs], " +
						$"preceded by keyframe at index {i - 1} which had time key [{keyframes[i - 1].TimeKeySeconds:N3}secs].");
				}
			}
		}
		
		var handle = new ResourceHandle<MeshAnimation>(++_nextHandleId);
		for (var m = 0; m < nodeMutations.Buffer.Length; ++m) {
			try {
				EnsureKeyframesOrderedByTime<SkeletalAnimationScalingKeyframe, Vect>(scalingKeyframes.Buffer.Slice(nodeMutations.Buffer[m].ScalingKeyframeStartIndex, nodeMutations.Buffer[m].ScalingKeyframeCount));
				EnsureKeyframesOrderedByTime<SkeletalAnimationRotationKeyframe, Quaternion>(rotationKeyframes.Buffer.Slice(nodeMutations.Buffer[m].RotationKeyframeStartIndex, nodeMutations.Buffer[m].RotationKeyframeCount));
				EnsureKeyframesOrderedByTime<SkeletalAnimationTranslationKeyframe, Vect>(translationKeyframes.Buffer.Slice(nodeMutations.Buffer[m].TranslationKeyframeStartIndex, nodeMutations.Buffer[m].TranslationKeyframeCount));
			}
			catch (Exception e) {
				throw new ArgumentException(
					$"Mutation at index {m} ({nodeMutations.Buffer[m]}) requests keyframes that are not time-ordered.",
					nameof(nodeMutations),
					e
				);
			}
			nodeMutations.Buffer[m] = nodeMutations.Buffer[m] with { TargetNodeIndex = skeleton.MutationTargetIndexMap.Buffer[nodeMutations.Buffer[m].TargetNodeIndex] };
		}
		// We pre-sort the mutations by target node index for the following reasons:
		// 1) Get better cache locality later when writing the animation matrices to the workspace buffer
		// 2) When blending animations we actually rely on them being sorted
		nodeMutations.Buffer.Sort(static (a, b) => a.TargetNodeIndex.CompareTo(b.TargetNodeIndex));
		var data = new AnimationData(scalingKeyframes, rotationKeyframes, translationKeyframes, nodeMutations, defaultCompletionTimeSeconds);
		
		_animationDataMap.Add(handle, data);
		_globals.StoreMandatoryResourceName(handle.Ident, name);
		_animationNameMap.Add(name, HandleToInstance(handle));
		
		return HandleToInstance(handle);
	}
	
	public void SetNodeName(int nodeIndex, ReadOnlySpan<char> name) {
		ThrowIfThisIsDisposed();
		var skeleton = GetSkeletonOrThrow();
		if (nodeIndex < 0 || nodeIndex >= skeleton.NodeCount) throw new ArgumentOutOfRangeException(nameof(nodeIndex), nodeIndex, $"Node index must be non-negative and less than node count ({skeleton.NodeCount}).");
		
		var meshNode = new MeshNode((nuint) nodeIndex, _meshNodeImplProvider);
		
		ReadOnlySpan<char> keyToRemove = default; 
		foreach (var kvp in _nodeNameMap) {
			if (kvp.Value == meshNode) {
				keyToRemove = kvp.Key.AsSpan;
				break;
			}
		}
		if (!keyToRemove.IsEmpty) _nodeNameMap.Remove(keyToRemove);
		
		if (_nodeNameMap.ContainsKey(name)) {
			using var newNameBuffer = _globals.HeapPool.Borrow<char>(name.Length + 20);
			name.CopyTo(newNameBuffer.Buffer);
			var repeatCharsStartIndex = name.Length;
			var repeatCount = 2;
			do {
				if (!repeatCount.TryFormat(newNameBuffer.Buffer[repeatCharsStartIndex..], out var repeatCharsCount, provider: CultureInfo.InvariantCulture)) {
					throw new InvalidOperationException($"Can not handle repeated node name '{name}'.");
				}
				name = newNameBuffer.Buffer[..(repeatCharsStartIndex + repeatCharsCount)];
				repeatCount++;
			} while (_nodeNameMap.ContainsKey(name));
		}
		_nodeNameMap[name] = meshNode;
	}
	#endregion
	
	#region Properties & Node/Anim Lookup
	public int Count => _animationNameMap.Count;
	public MeshAnimation? FindByName(ReadOnlySpan<char> name) => _animationNameMap.TryGetValue(name, out var animData) ? animData : null;
	public ArrayPoolBackedMap<ManagedStringPool.RentedStringHandle, MeshAnimation>.KeyEnumerator Keys => _animationNameMap.Keys;
	public ArrayPoolBackedMap<ManagedStringPool.RentedStringHandle, MeshAnimation>.ValueEnumerator Values => _animationNameMap.Values;
	public ArrayPoolBackedMap<ManagedStringPool.RentedStringHandle, MeshAnimation>.Enumerator GetEnumerator() => _animationNameMap.GetEnumerator();
	
	
	public int GetNodeCount() => GetSkeletonOrThrow().NodeCount;
	public MeshNode GetNode(int index) => index < GetNodeCount() ? new(new ResourceHandle<MeshNode>((nuint) index), _meshNodeImplProvider) : throw new ArgumentOutOfRangeException(nameof(index));
	public MeshNode? TryGetNode(ReadOnlySpan<char> name) {
		if (_nodeNameMap.TryGetValue(name, out var result)) return result;
		if (!name.StartsWith(DefaultNodeNamePrefix, StringComparison.Ordinal)) return null;
		if (!Int32.TryParse(name[DefaultNodeNamePrefix.Length..], CultureInfo.InvariantCulture, out var index)) return null;
		return new((nuint) index, _meshNodeImplProvider);
	}

	public float GetDefaultDurationSeconds(ResourceHandle<MeshAnimation> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _animationDataMap[handle].DefaultCompletionTimeSeconds;
	}
	
	public MeshAnimationType GetType(ResourceHandle<MeshAnimation> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return MeshAnimationType.Skeletal;
	}
	
	int GetIndex(ResourceHandle<MeshNode> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return (int) handle.AsInteger;
	}
	
	public MeshAnimation GetAnimationAtUnstableIndex(int index) => HandleToInstance(_animationDataMap.GetPairAtIndex(index).Key);

	public string GetNameAsNewStringObject(ResourceHandle<MeshAnimation> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetMandatoryResourceName(handle.Ident));
	}
	public int GetNameLength(ResourceHandle<MeshAnimation> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetMandatoryResourceName(handle.Ident).Length;
	}
	public void CopyName(ResourceHandle<MeshAnimation> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyMandatoryResourceName(handle.Ident, destinationBuffer);
	}
	
	public string GetNameAsNewStringObject(ResourceHandle<MeshNode> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		foreach (var kvp in _nodeNameMap) {
			if (kvp.Value.Handle == handle) return kvp.Key.AsNewStringObject;
		}
		return DefaultNodeNamePrefix + handle.AsInteger.ToString(CultureInfo.InvariantCulture);
	}
	public int GetNameLength(ResourceHandle<MeshNode> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		foreach (var kvp in _nodeNameMap) {
			if (kvp.Value.Handle == handle) return kvp.Key.Length;
		}
		
		Span<char> scrubSpace = stackalloc char[100];
		if (!handle.AsInteger.TryFormat(scrubSpace, out var charsWritten, provider: CultureInfo.InvariantCulture)) {
			return GetNameAsNewStringObject(handle).Length;
		}
		
		return DefaultNodeNamePrefix.Length + charsWritten; 
	}
	public void CopyName(ResourceHandle<MeshNode> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		foreach (var kvp in _nodeNameMap) {
			if (kvp.Value.Handle == handle) kvp.Key.AsSpan.CopyTo(destinationBuffer);
		}
		
		Span<char> scrubSpace = stackalloc char[100];
		if (!handle.AsInteger.TryFormat(scrubSpace, out var charsWritten, provider: CultureInfo.InvariantCulture)) {
			GetNameAsNewStringObject(handle).CopyTo(destinationBuffer);
			return;
		}
		
		DefaultNodeNamePrefix.CopyTo(destinationBuffer);
		scrubSpace[..charsWritten].CopyTo(destinationBuffer[charsWritten..]);
	}
	#endregion
	
	#region Keyframe Interpolation Math
	static TValue InterpolateKeyframes<T, TValue>(ReadOnlySpan<T> keys, float targetTimeSecs) where T : IAnimationKeyframe<TValue> {
		if (keys.Length == 0) return T.FallbackValue;
		if (keys.Length == 1 || targetTimeSecs <= keys[0].TimeKeySeconds) return keys[0].Value;
		if (targetTimeSecs >= keys[^1].TimeKeySeconds) return keys[^1].Value;
		
		// Binary search
		var lowIndex = 0;
		var highIndex = keys.Length - 1;
		while (lowIndex < highIndex - 1) {
			var midpointIndex = (lowIndex + highIndex) >> 1;
			if (keys[midpointIndex].TimeKeySeconds <= targetTimeSecs) lowIndex = midpointIndex;
			else highIndex = midpointIndex;
		}
		
		var low = keys[lowIndex];
		var high = keys[highIndex];
		return T.InterpolateValues(
			low.Value,
			high.Value, 
			Real.GetInterpolationDistance(low.TimeKeySeconds, high.TimeKeySeconds, targetTimeSecs)
		);
	}
	
	void InterpolateKeyframesInToWorkspace(AnimationData anim, float targetTimePointSeconds, Span<Matrix4x4> workspace) {
		var mutations = anim.BoneMutationDescriptors.Buffer;
		var translationKeys = anim.TranslationKeyframes.Buffer;
		var rotationKeys = anim.RotationKeyframes.Buffer;
		var scalingKeys = anim.ScalingKeyframes.Buffer;
		
		for (var i = 0; i < mutations.Length; ++i) {
			var mutation = mutations[i];
			var transform = new Transform(
				scaling: InterpolateKeyframes<SkeletalAnimationScalingKeyframe, Vect>(scalingKeys.Slice(mutation.ScalingKeyframeStartIndex, mutation.ScalingKeyframeCount), targetTimePointSeconds),
				rotationQuaternion: InterpolateKeyframes<SkeletalAnimationRotationKeyframe, Quaternion>(rotationKeys.Slice(mutation.RotationKeyframeStartIndex, mutation.RotationKeyframeCount), targetTimePointSeconds),
				translation: InterpolateKeyframes<SkeletalAnimationTranslationKeyframe, Vect>(translationKeys.Slice(mutation.TranslationKeyframeStartIndex, mutation.TranslationKeyframeCount), targetTimePointSeconds)
			);

			transform.ToMatrix(out workspace[mutation.TargetNodeIndex]);
		}
	}
	
	void InterpolateKeyframesInToWorkspace(AnimationData startAnim, float startTargetTimePointSeconds, AnimationData endAnim, float endTargetTimePointSeconds, float interpolationDistance, Span<Matrix4x4> workspace) {
		var startMutations = startAnim.BoneMutationDescriptors.Buffer;
		var startTranslationKeys = startAnim.TranslationKeyframes.Buffer;
		var startRotationKeys = startAnim.RotationKeyframes.Buffer;
		var startScalingKeys = startAnim.ScalingKeyframes.Buffer;
		
		var endMutations = endAnim.BoneMutationDescriptors.Buffer;
		var endTranslationKeys = endAnim.TranslationKeyframes.Buffer;
		var endRotationKeys = endAnim.RotationKeyframes.Buffer;
		var endScalingKeys = endAnim.ScalingKeyframes.Buffer;
		
		var startMutationsCursor = 0;
		var endMutationsCursor = 0;
		
		for (var i = 0; i < workspace.Length; ++i) {
			switch ((startMutationsCursor < startMutations.Length && startMutations[startMutationsCursor].TargetNodeIndex == i, endMutationsCursor < endMutations.Length && endMutations[endMutationsCursor].TargetNodeIndex == i)) {
				case (true, true): {
					var startMutation = startMutations[startMutationsCursor];
					var startTransform = new Transform(
						scaling: InterpolateKeyframes<SkeletalAnimationScalingKeyframe, Vect>(startScalingKeys.Slice(startMutation.ScalingKeyframeStartIndex, startMutation.ScalingKeyframeCount), startTargetTimePointSeconds),
						rotationQuaternion: InterpolateKeyframes<SkeletalAnimationRotationKeyframe, Quaternion>(startRotationKeys.Slice(startMutation.RotationKeyframeStartIndex, startMutation.RotationKeyframeCount), startTargetTimePointSeconds),
						translation: InterpolateKeyframes<SkeletalAnimationTranslationKeyframe, Vect>(startTranslationKeys.Slice(startMutation.TranslationKeyframeStartIndex, startMutation.TranslationKeyframeCount), startTargetTimePointSeconds)
					);
					var endMutation = endMutations[endMutationsCursor];
					var endTransform = new Transform(
						scaling: InterpolateKeyframes<SkeletalAnimationScalingKeyframe, Vect>(endScalingKeys.Slice(endMutation.ScalingKeyframeStartIndex, endMutation.ScalingKeyframeCount), endTargetTimePointSeconds),
						rotationQuaternion: InterpolateKeyframes<SkeletalAnimationRotationKeyframe, Quaternion>(endRotationKeys.Slice(endMutation.RotationKeyframeStartIndex, endMutation.RotationKeyframeCount), endTargetTimePointSeconds),
						translation: InterpolateKeyframes<SkeletalAnimationTranslationKeyframe, Vect>(endTranslationKeys.Slice(endMutation.TranslationKeyframeStartIndex, endMutation.TranslationKeyframeCount), endTargetTimePointSeconds)
					);
					
					Transform.Interpolate(startTransform, endTransform, interpolationDistance).ToMatrix(out workspace[i]);
					
					while (startMutationsCursor < startMutations.Length && startMutations[startMutationsCursor].TargetNodeIndex <= i) ++startMutationsCursor;
					while (endMutationsCursor < endMutations.Length && endMutations[endMutationsCursor].TargetNodeIndex <= i) ++endMutationsCursor;
					break;
				}
				case (true, false): {
					var mutation = startMutations[startMutationsCursor];
					var transform = new Transform(
						scaling: InterpolateKeyframes<SkeletalAnimationScalingKeyframe, Vect>(startScalingKeys.Slice(mutation.ScalingKeyframeStartIndex, mutation.ScalingKeyframeCount), startTargetTimePointSeconds),
						rotationQuaternion: InterpolateKeyframes<SkeletalAnimationRotationKeyframe, Quaternion>(startRotationKeys.Slice(mutation.RotationKeyframeStartIndex, mutation.RotationKeyframeCount), startTargetTimePointSeconds),
						translation: InterpolateKeyframes<SkeletalAnimationTranslationKeyframe, Vect>(startTranslationKeys.Slice(mutation.TranslationKeyframeStartIndex, mutation.TranslationKeyframeCount), startTargetTimePointSeconds)
					);

					var defaultTransform = MathUtils.GetBestGuessTransformFromMatrix(workspace[i]);
					Transform.Interpolate(transform, defaultTransform, interpolationDistance).ToMatrix(out workspace[i]);

					while (startMutationsCursor < startMutations.Length && startMutations[startMutationsCursor].TargetNodeIndex <= i) ++startMutationsCursor;
					break;
				}
				case (false, true): {
					var mutation = endMutations[endMutationsCursor];
					var transform = new Transform(
						scaling: InterpolateKeyframes<SkeletalAnimationScalingKeyframe, Vect>(endScalingKeys.Slice(mutation.ScalingKeyframeStartIndex, mutation.ScalingKeyframeCount), endTargetTimePointSeconds),
						rotationQuaternion: InterpolateKeyframes<SkeletalAnimationRotationKeyframe, Quaternion>(endRotationKeys.Slice(mutation.RotationKeyframeStartIndex, mutation.RotationKeyframeCount), endTargetTimePointSeconds),
						translation: InterpolateKeyframes<SkeletalAnimationTranslationKeyframe, Vect>(endTranslationKeys.Slice(mutation.TranslationKeyframeStartIndex, mutation.TranslationKeyframeCount), endTargetTimePointSeconds)
					);

					var defaultTransform = MathUtils.GetBestGuessTransformFromMatrix(workspace[i]);
					Transform.Interpolate(defaultTransform, transform, interpolationDistance).ToMatrix(out workspace[i]);

					while (endMutationsCursor < endMutations.Length && endMutations[endMutationsCursor].TargetNodeIndex <= i) ++endMutationsCursor;
					break;
				}
			}
		}
	}
	#endregion
	
	#region Node Transform Math & Bone Writing 
	void WriteBindPoseNodeTransformsToWorkspace() {
		ThrowIfThisIsDisposed();
		var skeleton = GetSkeletonOrThrow();

		var workspace = skeleton.Workspace.Buffer;
		var parents = skeleton.ParentIndices.Buffer;
		var firstParentedNodeIndex = skeleton.FirstParentedNodeIndex;
		var nodeCount = skeleton.Workspace.Buffer.Length;

		skeleton.DefaultLocalTransforms.Buffer.CopyTo(workspace);

		for (var i = 0; i < firstParentedNodeIndex; ++i) {
			workspace[i] *= skeleton.ModelImportTransformMatrix;
		}

		for (var i = firstParentedNodeIndex; i < nodeCount; ++i) {
			workspace[i] *= workspace[parents[i]];
		}
	}
	
	void WriteAnimationNodeTransformsToWorkspace(StartingAnimationData startAnimData, EndingAnimationData? endAnimData) {
		ThrowIfThisOrHandleIsDisposed(startAnimData.AnimHandle);
		if (endAnimData.HasValue) ObjectDisposedException.ThrowIf(IsDisposed(endAnimData.Value.AnimHandle), typeof(MeshAnimation));
		var skeleton = GetSkeletonOrThrow();

		var workspace = skeleton.Workspace.Buffer;
		var parents = skeleton.ParentIndices.Buffer;
		var firstParentedNodeIndex = skeleton.FirstParentedNodeIndex;
		var nodeCount = skeleton.NodeCount;
		
		skeleton.DefaultLocalTransforms.Buffer.CopyTo(workspace);

		if (endAnimData is { } endAnimDataValue) {
			InterpolateKeyframesInToWorkspace(
				_animationDataMap[startAnimData.AnimHandle], 
				startAnimData.TimePointSeconds, 
				_animationDataMap[endAnimDataValue.AnimHandle], 
				endAnimDataValue.TimePointSeconds, 
				endAnimDataValue.InterpolationDistance, 
				workspace
			);
		}
		else {
			InterpolateKeyframesInToWorkspace(
				_animationDataMap[startAnimData.AnimHandle], 
				startAnimData.TimePointSeconds, 
				workspace
			);
		}

		for (var i = 0; i < firstParentedNodeIndex; ++i) {
			workspace[i] *= skeleton.ModelImportTransformMatrix;
		}

		for (var i = firstParentedNodeIndex; i < nodeCount; ++i) {
			workspace[i] *= workspace[parents[i]];
		}
	}
	
	void WriteAnimationNodeTransformsToWorkspaceAndSetBoneTransforms(ModelInstance targetInstance, StartingAnimationData startAnimData, EndingAnimationData? endAnimData) {
		ThrowIfThisIsDisposed();
		var skeleton = GetSkeletonOrThrow();
		if (targetInstance.Mesh != skeleton.OwningMesh) {
			throw new InvalidOperationException(
				$"Can not apply animation to {targetInstance} via {nameof(MeshAnimationIndex)} for a different mesh. " +
				$"Model instance is using {targetInstance.Mesh}, {nameof(MeshAnimationIndex)} is for {skeleton.OwningMesh}."
			);
		}
		
		WriteAnimationNodeTransformsToWorkspace(startAnimData, endAnimData);

		var workspace = skeleton.Workspace.Buffer;
		var bindPoseInversions = skeleton.BindPoseInversions.Buffer;
		var boneToNodeMap = skeleton.BoneToNodeMap.Buffer;
		var boneCount = skeleton.BoneCount;

		var results = _applyTransformsBuffer.AsSpan;
		
		for (var i = 0; i < boneCount; ++i) {
			results[i] = bindPoseInversions[i] * workspace[boneToNodeMap[i]];
		}
		
		SetModelInstanceBoneTransforms(
			targetInstance.Handle, 
			_applyTransformsBuffer.BufferPointer, 
			skeleton.BoneCount
		).ThrowIfFailure();
	}
	#endregion
	
	#region Animation Control Helpers
	void CopyRequestedNodeTransformsFromWorkspace(ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		var skeleton = GetSkeletonOrThrow();
		if (nodes.Length > modelSpaceTransforms.Length) {
			throw new ArgumentException($"Requested {nodes.Length} {nameof(nodes)}, but {nameof(modelSpaceTransforms)} destination span is too small (length {modelSpaceTransforms.Length}).", nameof(modelSpaceTransforms));
		}
		
		static void ThrowIfNodeInvalid(MeshNode node, IMeshNodeImplProvider implProvider, int nodeCount) {
			var nodeIndex = (int) node.GetHandleWithoutDisposeCheck().AsInteger;
			if (!ReferenceEquals(node.Implementation, implProvider) || nodeIndex < 0 || nodeIndex >= nodeCount) {
				throw new ArgumentException($"Given node {node} is not valid for this mesh.", nameof(nodes));
			}
		}
		
		for (var i = 0; i < nodes.Length; ++i) {
			var node = nodes[i];
			ThrowIfNodeInvalid(node, _meshNodeImplProvider, skeleton.NodeCount);
			var nodeIndex = skeleton.MutationTargetIndexMap.Buffer[(int) node.GetHandleWithoutDisposeCheck().AsInteger];
			modelSpaceTransforms[i] = skeleton.Workspace.Buffer[nodeIndex];
		}
	}
	void CopyRequestedNodeTransformsFromWorkspace(ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		var skeleton = GetSkeletonOrThrow();
		if (nodeIndices.Length > modelSpaceTransforms.Length) {
			throw new ArgumentException($"Requested {nodeIndices.Length} {nameof(nodeIndices)}, but {nameof(modelSpaceTransforms)} destination span is too small (length {modelSpaceTransforms.Length}).", nameof(modelSpaceTransforms));
		}
		
		static void ThrowIfNodeInvalid(int nodeIndex, int nodeCount) {
			if (nodeIndex < 0 || nodeIndex >= nodeCount) {
				throw new ArgumentException($"Given node index {nodeIndex} is not valid for this mesh.", nameof(nodeIndices));
			}
		}
		
		for (var i = 0; i < nodeIndices.Length; ++i) {
			var nodeIndex = nodeIndices[i];
			ThrowIfNodeInvalid(nodeIndex, skeleton.NodeCount);
			modelSpaceTransforms[i] = skeleton.Workspace.Buffer[skeleton.MutationTargetIndexMap.Buffer[nodeIndex]];
		}
	}
	
	void ApplyAndGetNodeTransforms(ModelInstance targetInstance, StartingAnimationData startAnimData, EndingAnimationData? endAnimData, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		WriteAnimationNodeTransformsToWorkspaceAndSetBoneTransforms(targetInstance, startAnimData, endAnimData);
		CopyRequestedNodeTransformsFromWorkspace(nodes, modelSpaceTransforms);
	}
	
	void ApplyAndGetNodeTransforms(ModelInstance targetInstance, StartingAnimationData startAnimData, EndingAnimationData? endAnimData, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		WriteAnimationNodeTransformsToWorkspaceAndSetBoneTransforms(targetInstance, startAnimData, endAnimData);
		CopyRequestedNodeTransformsFromWorkspace(nodeIndices, modelSpaceTransforms);
	}

	void GetNodeTransforms(StartingAnimationData startAnimData, EndingAnimationData? endAnimData, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		WriteAnimationNodeTransformsToWorkspace(startAnimData, endAnimData);
		CopyRequestedNodeTransformsFromWorkspace(nodes, modelSpaceTransforms);
	}
	
	void GetNodeTransforms(StartingAnimationData startAnimData, EndingAnimationData? endAnimData, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		WriteAnimationNodeTransformsToWorkspace(startAnimData, endAnimData);
		CopyRequestedNodeTransformsFromWorkspace(nodeIndices, modelSpaceTransforms);
	}
	#endregion
	
	#region Animation Controls
	public void ApplyBindPose(ModelInstance targetInstance) {
		ThrowIfThisIsDisposed();
		var skeleton = GetSkeletonOrThrow();
		if (targetInstance.Mesh != skeleton.OwningMesh) {
			throw new InvalidOperationException(
				$"Can not apply bind pose to {targetInstance} via {nameof(MeshAnimationIndex)} for a different mesh. " +
				$"Model instance is using {targetInstance.Mesh}, {nameof(MeshAnimationIndex)} is for {skeleton.OwningMesh}."
			);
		}
		WriteBindPoseNodeTransformsToWorkspace();
		
		var workspace = skeleton.Workspace.Buffer;
		var bindPoseInversions = skeleton.BindPoseInversions.Buffer;
		var boneToNodeMap = skeleton.BoneToNodeMap.Buffer;
		var boneCount = skeleton.BoneCount;
		
		var results = _applyTransformsBuffer.AsSpan;
		
		for (var i = 0; i < boneCount; ++i) {
			results[i] = bindPoseInversions[i] * workspace[boneToNodeMap[i]];
		}
		
		SetModelInstanceBoneTransforms(
			targetInstance.Handle, 
			_applyTransformsBuffer.BufferPointer, 
			skeleton.BoneCount
		).ThrowIfFailure();
	}
	
	public void GetBindPoseNodeTransforms(ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		WriteBindPoseNodeTransformsToWorkspace();
		CopyRequestedNodeTransformsFromWorkspace(nodes, modelSpaceTransforms);
	}
	public void GetBindPoseNodeTransforms(ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		WriteBindPoseNodeTransformsToWorkspace();
		CopyRequestedNodeTransformsFromWorkspace(nodeIndices, modelSpaceTransforms);
	}
	
	public void Apply(ModelInstance targetInstance, ResourceHandle<MeshAnimation> handle, float targetTimePointSeconds) {
		WriteAnimationNodeTransformsToWorkspaceAndSetBoneTransforms(targetInstance, new(handle, targetTimePointSeconds), null);
	}
	public void GetNodeTransforms(ResourceHandle<MeshAnimation> handle, float targetTimePointSeconds, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		GetNodeTransforms(new(handle, targetTimePointSeconds), null, nodes, modelSpaceTransforms);
	}
	public void GetNodeTransforms(ResourceHandle<MeshAnimation> handle, float targetTimePointSeconds, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		GetNodeTransforms(new(handle, targetTimePointSeconds), null, nodeIndices, modelSpaceTransforms);
	}
	public void ApplyAndGetNodeTransforms(ModelInstance targetInstance, ResourceHandle<MeshAnimation> handle, float targetTimePointSeconds, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		ApplyAndGetNodeTransforms(targetInstance, new(handle, targetTimePointSeconds), null, nodes, modelSpaceTransforms);
	}
	public void ApplyAndGetNodeTransforms(ModelInstance targetInstance, ResourceHandle<MeshAnimation> handle, float targetTimePointSeconds, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		ApplyAndGetNodeTransforms(targetInstance, new(handle, targetTimePointSeconds), null, nodeIndices, modelSpaceTransforms);
	}
	public void ApplyBlended(ModelInstance targetInstance, ResourceHandle<MeshAnimation> startAnimHandle, float startAnimTargetTimePointSeconds, ResourceHandle<MeshAnimation> endAnimHandle, float endAnimTargetTimePointSeconds, float interpolationDistance) {
		WriteAnimationNodeTransformsToWorkspaceAndSetBoneTransforms(targetInstance, new(startAnimHandle, startAnimTargetTimePointSeconds), new EndingAnimationData(endAnimHandle, endAnimTargetTimePointSeconds, interpolationDistance));
	}
	public void GetBlendedNodeTransforms(ResourceHandle<MeshAnimation> startAnimHandle, float startAnimTargetTimePointSeconds, ResourceHandle<MeshAnimation> endAnimHandle, float endAnimTargetTimePointSeconds, float interpolationDistance, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		GetNodeTransforms(new(startAnimHandle, startAnimTargetTimePointSeconds), new EndingAnimationData(endAnimHandle, endAnimTargetTimePointSeconds, interpolationDistance), nodes, modelSpaceTransforms);
	}
	public void GetBlendedNodeTransforms(ResourceHandle<MeshAnimation> startAnimHandle, float startAnimTargetTimePointSeconds, ResourceHandle<MeshAnimation> endAnimHandle, float endAnimTargetTimePointSeconds, float interpolationDistance, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		GetNodeTransforms(new(startAnimHandle, startAnimTargetTimePointSeconds), new EndingAnimationData(endAnimHandle, endAnimTargetTimePointSeconds, interpolationDistance), nodeIndices, modelSpaceTransforms);
	}
	public void ApplyBlendedAndGetNodeTransforms(ModelInstance targetInstance, ResourceHandle<MeshAnimation> startAnimHandle, float startAnimTargetTimePointSeconds, ResourceHandle<MeshAnimation> endAnimHandle, float endAnimTargetTimePointSeconds, float interpolationDistance, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		ApplyAndGetNodeTransforms(targetInstance, new(startAnimHandle, startAnimTargetTimePointSeconds), new EndingAnimationData(endAnimHandle, endAnimTargetTimePointSeconds, interpolationDistance), nodes, modelSpaceTransforms);
	}
	public void ApplyBlendedAndGetNodeTransforms(ModelInstance targetInstance, ResourceHandle<MeshAnimation> startAnimHandle, float startAnimTargetTimePointSeconds, ResourceHandle<MeshAnimation> endAnimHandle, float endAnimTargetTimePointSeconds, float interpolationDistance, ReadOnlySpan<int> nodeIndices, Span<Matrix4x4> modelSpaceTransforms) {
		ApplyAndGetNodeTransforms(targetInstance, new(startAnimHandle, startAnimTargetTimePointSeconds), new EndingAnimationData(endAnimHandle, endAnimTargetTimePointSeconds, interpolationDistance), nodeIndices, modelSpaceTransforms);
	}
	#endregion

	public override string ToString() => _isDisposed ? "TinyFFR Local Mesh Animation Table [Disposed]" : "TinyFFR Local Mesh Animation Table";

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	MeshAnimation HandleToInstance(ResourceHandle<MeshAnimation> h) => new(h, this);

	#region Native Methods
	[SuppressGCTransition]
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "set_model_instance_bone_transforms")]
	static extern InteropResult SetModelInstanceBoneTransforms(
		UIntPtr modelInstanceHandle,
		Matrix4x4* transformsPtr,
		int boneCount
	);
	#endregion

	#region Disposal & Recycle
	public bool IsDisposed(ResourceHandle<MeshAnimation> handle) => _isDisposed || !_animationDataMap.ContainsKey(handle);
	public bool IsDisposed(ResourceHandle<MeshNode> handle) => _isDisposed || _currentSkeleton is not { } skeleton || handle.AsInteger >= (nuint) skeleton.NodeCount;
	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<MeshAnimation> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(MeshAnimation));
	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<MeshNode> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(MeshAnimation));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);

	public void Recycle() {
		foreach (var kvp in _animationDataMap) {
			var data = kvp.Value;
			data.BoneMutationDescriptors.Dispose();
			data.TranslationKeyframes.Dispose();
			data.RotationKeyframes.Dispose();
			data.ScalingKeyframes.Dispose();
		}
		_animationDataMap.Clear();
		_animationNameMap.Clear();
		_nodeNameMap.Clear();
		
		_currentSkeleton?.BindPoseInversions.Dispose();
		_currentSkeleton?.DefaultLocalTransforms.Dispose();
		_currentSkeleton?.Workspace.Dispose();
		_currentSkeleton?.ParentIndices.Dispose();
		_currentSkeleton?.BoneToNodeMap.Dispose();
		_currentSkeleton?.MutationTargetIndexMap.Dispose();
		_currentSkeleton = null;
	}

	public void Dispose() {
		_isDisposed = true;
		Recycle();
		_applyTransformsBuffer.Dispose();
		_animationNameMap.Dispose();
		_animationDataMap.Dispose();
		_nodeNameMap.Dispose();
	}
	#endregion
}
