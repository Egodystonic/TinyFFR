// Created on 2026-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Globalization;
using System.Security;
using Egodystonic.TinyFFR.Assets.Baking;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;
using static Egodystonic.TinyFFR.Assets.Baking.BakedResourceSchemata;

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
	static nuint _prevHandleId = 0U;
	readonly LocalMeshNodeImplProvider _meshNodeImplProvider;
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

	#region Initialization + Setup
	public void SetSkeleton(Mesh owningMesh, int boneCount, ReadOnlySpan<SkeletalAnimationNode> skeletalNodes, Matrix4x4 modelImportTransformMatrix) {
		var nodeCount = skeletalNodes.Length;
		if (_currentSkeleton != null) {
			throw new InvalidOperationException("Skeleton already set for this animation table (this is a bug in TinyFFR).");
		}
		if (boneCount is < 1 or > IMeshBuilder.MaxSkeletalBoneCount) {
			throw new InvalidOperationException($"Bone count = {boneCount} (this is a bug in TinyFFR).");
		}

		Span<int> depths = stackalloc int[nodeCount];
		Span<int> processingOrder = stackalloc int[nodeCount];

		var inputToOutputIndexMapHeapMemory = _globals.HeapPool.Borrow<int>(nodeCount);
		var defaultLocalTransformsHeapMemory = _globals.HeapPool.Borrow<Matrix4x4>(nodeCount);
		var workspaceHeapMemory = _globals.HeapPool.Borrow<Matrix4x4>(nodeCount);
		var parentIndicesHeapMemory = _globals.HeapPool.Borrow<int>(nodeCount);
		var boneToNodeMapHeapMemory = _globals.HeapPool.Borrow<int>(boneCount);
		var bindPoseInversionsHeapMemory = _globals.HeapPool.Borrow<Matrix4x4>(boneCount);

		var firstParentedNodeIndex = SkeletalMeshUtils.ProcessRawNodeData(
			skeletalNodes,
			boneCount,
			modelImportTransformMatrix,
			depths,
			processingOrder,
			defaultLocalTransformsHeapMemory.Span,
			bindPoseInversionsHeapMemory.Span,
			parentIndicesHeapMemory.Span,
			boneToNodeMapHeapMemory.Span,
			inputToOutputIndexMapHeapMemory.Span
		);

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
		
		var handle = new ResourceHandle<MeshAnimation>(++_prevHandleId);
		for (var m = 0; m < nodeMutations.Span.Length; ++m) {
			try {
				EnsureKeyframesOrderedByTime<SkeletalAnimationScalingKeyframe, Vect>(scalingKeyframes.Span.Slice(nodeMutations.Span[m].ScalingKeyframeStartIndex, nodeMutations.Span[m].ScalingKeyframeCount));
				EnsureKeyframesOrderedByTime<SkeletalAnimationRotationKeyframe, Quaternion>(rotationKeyframes.Span.Slice(nodeMutations.Span[m].RotationKeyframeStartIndex, nodeMutations.Span[m].RotationKeyframeCount));
				EnsureKeyframesOrderedByTime<SkeletalAnimationTranslationKeyframe, Vect>(translationKeyframes.Span.Slice(nodeMutations.Span[m].TranslationKeyframeStartIndex, nodeMutations.Span[m].TranslationKeyframeCount));
			}
			catch (Exception e) {
				throw new ArgumentException(
					$"Mutation at index {m} ({nodeMutations.Span[m]}) requests keyframes that are not time-ordered.",
					nameof(nodeMutations),
					e
				);
			}
			nodeMutations.Span[m] = nodeMutations.Span[m] with { TargetNodeIndex = skeleton.MutationTargetIndexMap.Span[nodeMutations.Span[m].TargetNodeIndex] };
		}
		// We pre-sort the mutations by target node index for the following reasons:
		// 1) Get better cache locality later when writing the animation matrices to the workspace buffer
		// 2) When blending animations we actually rely on them being sorted
		nodeMutations.Span.Sort(static (a, b) => a.TargetNodeIndex.CompareTo(b.TargetNodeIndex));
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
			name.CopyTo(newNameBuffer.Span);
			var repeatCharsStartIndex = name.Length;
			var repeatCount = 2;
			do {
				if (!repeatCount.TryFormat(newNameBuffer.Span[repeatCharsStartIndex..], out var repeatCharsCount, provider: CultureInfo.InvariantCulture)) {
					throw new InvalidOperationException($"Can not handle repeated node name '{name}'.");
				}
				name = newNameBuffer.Span[..(repeatCharsStartIndex + repeatCharsCount)];
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
	
	internal int GetIndex(ResourceHandle<MeshNode> handle) {
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
			if (kvp.Value.Handle == handle) {
				kvp.Key.AsSpan.CopyTo(destinationBuffer);
				return;
			}
		}

		Span<char> scrubSpace = stackalloc char[100];
		if (!handle.AsInteger.TryFormat(scrubSpace, out var charsWritten, provider: CultureInfo.InvariantCulture)) {
			GetNameAsNewStringObject(handle).CopyTo(destinationBuffer);
			return;
		}

		DefaultNodeNamePrefix.CopyTo(destinationBuffer);
		scrubSpace[..charsWritten].CopyTo(destinationBuffer[DefaultNodeNamePrefix.Length..]);
	}
	#endregion
	
	#region Keyframe Interpolation Math	
	static void InterpolateKeyframesInToWorkspace(AnimationData anim, float targetTimePointSeconds, Span<Matrix4x4> workspace) {
		SkeletalMeshUtils.InterpolateKeyframesInToWorkspace(
			anim.BoneMutationDescriptors.Span,
			anim.ScalingKeyframes.Span,
			anim.RotationKeyframes.Span,
			anim.TranslationKeyframes.Span,
			targetTimePointSeconds,
			default,
			workspace
		);
	}
	
	void InterpolateKeyframesInToWorkspace(AnimationData startAnim, float startTargetTimePointSeconds, AnimationData endAnim, float endTargetTimePointSeconds, float interpolationDistance, Span<Matrix4x4> workspace) {
		var startMutations = startAnim.BoneMutationDescriptors.Span;
		var startTranslationKeys = startAnim.TranslationKeyframes.Span;
		var startRotationKeys = startAnim.RotationKeyframes.Span;
		var startScalingKeys = startAnim.ScalingKeyframes.Span;
		
		var endMutations = endAnim.BoneMutationDescriptors.Span;
		var endTranslationKeys = endAnim.TranslationKeyframes.Span;
		var endRotationKeys = endAnim.RotationKeyframes.Span;
		var endScalingKeys = endAnim.ScalingKeyframes.Span;
		
		var startMutationsCursor = 0;
		var endMutationsCursor = 0;
		
		for (var i = 0; i < workspace.Length; ++i) {
			switch ((startMutationsCursor < startMutations.Length && startMutations[startMutationsCursor].TargetNodeIndex == i, endMutationsCursor < endMutations.Length && endMutations[endMutationsCursor].TargetNodeIndex == i)) {
				case (true, true): {
					var startMutation = startMutations[startMutationsCursor];
					var startTransform = new Transform(
						scaling: SkeletalMeshUtils.InterpolateKeyframes<SkeletalAnimationScalingKeyframe, Vect>(startScalingKeys.Slice(startMutation.ScalingKeyframeStartIndex, startMutation.ScalingKeyframeCount), startTargetTimePointSeconds),
						rotationQuaternion: SkeletalMeshUtils.InterpolateKeyframes<SkeletalAnimationRotationKeyframe, Quaternion>(startRotationKeys.Slice(startMutation.RotationKeyframeStartIndex, startMutation.RotationKeyframeCount), startTargetTimePointSeconds),
						translation: SkeletalMeshUtils.InterpolateKeyframes<SkeletalAnimationTranslationKeyframe, Vect>(startTranslationKeys.Slice(startMutation.TranslationKeyframeStartIndex, startMutation.TranslationKeyframeCount), startTargetTimePointSeconds)
					);
					var endMutation = endMutations[endMutationsCursor];
					var endTransform = new Transform(
						scaling: SkeletalMeshUtils.InterpolateKeyframes<SkeletalAnimationScalingKeyframe, Vect>(endScalingKeys.Slice(endMutation.ScalingKeyframeStartIndex, endMutation.ScalingKeyframeCount), endTargetTimePointSeconds),
						rotationQuaternion: SkeletalMeshUtils.InterpolateKeyframes<SkeletalAnimationRotationKeyframe, Quaternion>(endRotationKeys.Slice(endMutation.RotationKeyframeStartIndex, endMutation.RotationKeyframeCount), endTargetTimePointSeconds),
						translation: SkeletalMeshUtils.InterpolateKeyframes<SkeletalAnimationTranslationKeyframe, Vect>(endTranslationKeys.Slice(endMutation.TranslationKeyframeStartIndex, endMutation.TranslationKeyframeCount), endTargetTimePointSeconds)
					);
					
					Transform.Interpolate(startTransform, endTransform, interpolationDistance).ToMatrix(out workspace[i]);
					
					while (startMutationsCursor < startMutations.Length && startMutations[startMutationsCursor].TargetNodeIndex <= i) ++startMutationsCursor;
					while (endMutationsCursor < endMutations.Length && endMutations[endMutationsCursor].TargetNodeIndex <= i) ++endMutationsCursor;
					break;
				}
				case (true, false): {
					var mutation = startMutations[startMutationsCursor];
					var transform = new Transform(
						scaling: SkeletalMeshUtils.InterpolateKeyframes<SkeletalAnimationScalingKeyframe, Vect>(startScalingKeys.Slice(mutation.ScalingKeyframeStartIndex, mutation.ScalingKeyframeCount), startTargetTimePointSeconds),
						rotationQuaternion: SkeletalMeshUtils.InterpolateKeyframes<SkeletalAnimationRotationKeyframe, Quaternion>(startRotationKeys.Slice(mutation.RotationKeyframeStartIndex, mutation.RotationKeyframeCount), startTargetTimePointSeconds),
						translation: SkeletalMeshUtils.InterpolateKeyframes<SkeletalAnimationTranslationKeyframe, Vect>(startTranslationKeys.Slice(mutation.TranslationKeyframeStartIndex, mutation.TranslationKeyframeCount), startTargetTimePointSeconds)
					);

					var defaultTransform = MathUtils.GetBestGuessTransformFromMatrix(workspace[i]);
					Transform.Interpolate(transform, defaultTransform, interpolationDistance).ToMatrix(out workspace[i]);

					while (startMutationsCursor < startMutations.Length && startMutations[startMutationsCursor].TargetNodeIndex <= i) ++startMutationsCursor;
					break;
				}
				case (false, true): {
					var mutation = endMutations[endMutationsCursor];
					var transform = new Transform(
						scaling: SkeletalMeshUtils.InterpolateKeyframes<SkeletalAnimationScalingKeyframe, Vect>(endScalingKeys.Slice(mutation.ScalingKeyframeStartIndex, mutation.ScalingKeyframeCount), endTargetTimePointSeconds),
						rotationQuaternion: SkeletalMeshUtils.InterpolateKeyframes<SkeletalAnimationRotationKeyframe, Quaternion>(endRotationKeys.Slice(mutation.RotationKeyframeStartIndex, mutation.RotationKeyframeCount), endTargetTimePointSeconds),
						translation: SkeletalMeshUtils.InterpolateKeyframes<SkeletalAnimationTranslationKeyframe, Vect>(endTranslationKeys.Slice(mutation.TranslationKeyframeStartIndex, mutation.TranslationKeyframeCount), endTargetTimePointSeconds)
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

		var workspace = skeleton.Workspace.Span[..skeleton.NodeCount];
		skeleton.DefaultLocalTransforms.Span[..skeleton.NodeCount].CopyTo(workspace);

		SkeletalMeshUtils.ApplySkeletalTransformHierarchy(
			skeleton.ParentIndices.Span,
			skeleton.FirstParentedNodeIndex,
			skeleton.ModelImportTransformMatrix,
			workspace
		);
	}
	
	void WriteAnimationNodeTransformsToWorkspace(StartingAnimationData startAnimData, EndingAnimationData? endAnimData) {
		ThrowIfThisOrHandleIsDisposed(startAnimData.AnimHandle);
		if (endAnimData.HasValue) ObjectDisposedException.ThrowIf(IsDisposed(endAnimData.Value.AnimHandle), typeof(MeshAnimation));
		var skeleton = GetSkeletonOrThrow();

		var workspace = skeleton.Workspace.Span[..skeleton.NodeCount];

		skeleton.DefaultLocalTransforms.Span[..skeleton.NodeCount].CopyTo(workspace);

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

		SkeletalMeshUtils.ApplySkeletalTransformHierarchy(
			skeleton.ParentIndices.Span,
			skeleton.FirstParentedNodeIndex,
			skeleton.ModelImportTransformMatrix,
			workspace
		);
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

		SkeletalMeshUtils.ApplyBindPoseInversions(
			skeleton.BindPoseInversions.Span[..skeleton.BoneCount],
			skeleton.BoneToNodeMap.Span,
			skeleton.Workspace.Span[..skeleton.NodeCount],
			_applyTransformsBuffer.AsSpan
		);
		
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
			var nodeIndex = skeleton.MutationTargetIndexMap.Span[(int) node.GetHandleWithoutDisposeCheck().AsInteger];
			modelSpaceTransforms[i] = skeleton.Workspace.Span[nodeIndex];
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
			modelSpaceTransforms[i] = skeleton.Workspace.Span[skeleton.MutationTargetIndexMap.Span[nodeIndex]];
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
		
		SkeletalMeshUtils.ApplyBindPoseInversions(
			skeleton.BindPoseInversions.Span[..skeleton.BoneCount],
			skeleton.BoneToNodeMap.Span,
			skeleton.Workspace.Span[..skeleton.NodeCount],
			_applyTransformsBuffer.AsSpan
		);
		
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

	#region Baking
	public bool SkeletonIsSet => _currentSkeleton != null;

	public void WriteBakeData(LocalAssetBakery bakery, Mesh mesh) {
		var skeleton = GetSkeletonOrThrow();

		bakery.AddResourceBakeValue(mesh, MeshBakingSchema.SkeletonNodeCount, skeleton.NodeCount);
		bakery.AddResourceBakeValue(mesh, MeshBakingSchema.SkeletonFirstParentedNodeIndex, skeleton.FirstParentedNodeIndex);
		bakery.AddResourceBakeValue(mesh, MeshBakingSchema.SkeletonModelImportTransform, skeleton.ModelImportTransformMatrix);
		bakery.AddResourceBakeValue(mesh, MeshBakingSchema.SkeletonDefaultLocalTransforms, MemoryMarshal.AsBytes(skeleton.DefaultLocalTransforms.Span[..skeleton.NodeCount]));
		bakery.AddResourceBakeValue(mesh, MeshBakingSchema.SkeletonBindPoseInversions, MemoryMarshal.AsBytes(skeleton.BindPoseInversions.Span[..skeleton.BoneCount]));
		bakery.AddResourceBakeValue(mesh, MeshBakingSchema.SkeletonParentIndices, MemoryMarshal.AsBytes(skeleton.ParentIndices.Span[..skeleton.NodeCount]));
		bakery.AddResourceBakeValue(mesh, MeshBakingSchema.SkeletonBoneToNodeMap, MemoryMarshal.AsBytes(skeleton.BoneToNodeMap.Span[..skeleton.BoneCount]));
		bakery.AddResourceBakeValue(mesh, MeshBakingSchema.SkeletonMutationTargetIndexMap, MemoryMarshal.AsBytes(skeleton.MutationTargetIndexMap.Span[..skeleton.NodeCount]));

		WriteAnimationBakeData(bakery, mesh);
		WriteNodeNameBakeData(bakery, mesh);
	}

	void WriteAnimationBakeData(LocalAssetBakery bakery, Mesh mesh) {
		var animationCount = _animationDataMap.Count;
		var totalScaling = 0;
		var totalRotation = 0;
		var totalTranslation = 0;
		var totalMutations = 0;
		var totalNameChars = 0;

		for (var i = 0; i < animationCount; ++i) {
			var pair = _animationDataMap.GetPairAtIndex(i);
			totalScaling += pair.Value.ScalingKeyframes.Span.Length;
			totalRotation += pair.Value.RotationKeyframes.Span.Length;
			totalTranslation += pair.Value.TranslationKeyframes.Span.Length;
			totalMutations += pair.Value.BoneMutationDescriptors.Span.Length;
			totalNameChars += _globals.GetResourceName(pair.Key.Ident, default).Length;
		}

		using var entries = _globals.HeapPool.Borrow<MeshBakingSchema.BakedAnimationEntry>(animationCount);
		using var scaling = _globals.HeapPool.Borrow<SkeletalAnimationScalingKeyframe>(totalScaling);
		using var rotation = _globals.HeapPool.Borrow<SkeletalAnimationRotationKeyframe>(totalRotation);
		using var translation = _globals.HeapPool.Borrow<SkeletalAnimationTranslationKeyframe>(totalTranslation);
		using var mutations = _globals.HeapPool.Borrow<SkeletalAnimationNodeMutationDescriptor>(totalMutations);
		using var nameChars = _globals.HeapPool.Borrow<char>(totalNameChars);

		var entryIndex = 0;
		var scalingCursor = 0;
		var rotationCursor = 0;
		var translationCursor = 0;
		var mutationCursor = 0;
		var nameCursor = 0;

		for (var i = 0; i < animationCount; ++i) {
			var pair = _animationDataMap.GetPairAtIndex(i);
			var data = pair.Value;
			var name = _globals.GetResourceName(pair.Key.Ident, default);

			data.ScalingKeyframes.Span.CopyTo(scaling.Span[scalingCursor..]);
			data.RotationKeyframes.Span.CopyTo(rotation.Span[rotationCursor..]);
			data.TranslationKeyframes.Span.CopyTo(translation.Span[translationCursor..]);
			data.BoneMutationDescriptors.Span.CopyTo(mutations.Span[mutationCursor..]);
			name.CopyTo(nameChars.Span[nameCursor..]);

			entries.Span[entryIndex++] = new MeshBakingSchema.BakedAnimationEntry(
				scalingCursor, data.ScalingKeyframes.Span.Length,
				rotationCursor, data.RotationKeyframes.Span.Length,
				translationCursor, data.TranslationKeyframes.Span.Length,
				mutationCursor, data.BoneMutationDescriptors.Span.Length,
				nameCursor, name.Length,
				data.DefaultCompletionTimeSeconds
			);

			scalingCursor += data.ScalingKeyframes.Span.Length;
			rotationCursor += data.RotationKeyframes.Span.Length;
			translationCursor += data.TranslationKeyframes.Span.Length;
			mutationCursor += data.BoneMutationDescriptors.Span.Length;
			nameCursor += name.Length;
		}

		bakery.AddResourceBakeValue(mesh, MeshBakingSchema.AnimationTable, MemoryMarshal.AsBytes(entries.Span[..entryIndex]));
		bakery.AddResourceBakeValue(mesh, MeshBakingSchema.AnimationScalingKeyframes, MemoryMarshal.AsBytes(scaling.Span[..scalingCursor]));
		bakery.AddResourceBakeValue(mesh, MeshBakingSchema.AnimationRotationKeyframes, MemoryMarshal.AsBytes(rotation.Span[..rotationCursor]));
		bakery.AddResourceBakeValue(mesh, MeshBakingSchema.AnimationTranslationKeyframes, MemoryMarshal.AsBytes(translation.Span[..translationCursor]));
		bakery.AddResourceBakeValue(mesh, MeshBakingSchema.AnimationMutationDescriptors, MemoryMarshal.AsBytes(mutations.Span[..mutationCursor]));
		bakery.AddResourceBakeValue(mesh, MeshBakingSchema.AnimationNameChars, nameChars.Span[..nameCursor]);
	}

	void WriteNodeNameBakeData(LocalAssetBakery bakery, Mesh mesh) {
		var nodeNameCount = _nodeNameMap.Count;
		var totalNameChars = 0;
		foreach (var kvp in _nodeNameMap) totalNameChars += kvp.Key.AsSpan.Length;

		using var entries = _globals.HeapPool.Borrow<MeshBakingSchema.BakedNodeNameEntry>(nodeNameCount);
		using var nameChars = _globals.HeapPool.Borrow<char>(totalNameChars);

		var entryIndex = 0;
		var nameCursor = 0;
		foreach (var kvp in _nodeNameMap) {
			var name = kvp.Key.AsSpan;
			name.CopyTo(nameChars.Span[nameCursor..]);
			entries.Span[entryIndex++] = new MeshBakingSchema.BakedNodeNameEntry(
				(int) kvp.Value.GetHandleWithoutDisposeCheck().AsInteger,
				nameCursor,
				name.Length
			);
			nameCursor += name.Length;
		}

		bakery.AddResourceBakeValue(mesh, MeshBakingSchema.NodeNameTable, MemoryMarshal.AsBytes(entries.Span[..entryIndex]));
		bakery.AddResourceBakeValue(mesh, MeshBakingSchema.NodeNameChars, nameChars.Span[..nameCursor]);
	}

	public void SetSkeletonFromBakedData(
		Mesh owningMesh,
		int nodeCount,
		int boneCount,
		int firstParentedNodeIndex,
		Matrix4x4 modelImportTransformMatrix,
		ReadOnlySpan<Matrix4x4> defaultLocalTransforms,
		ReadOnlySpan<Matrix4x4> bindPoseInversions,
		ReadOnlySpan<int> parentIndices,
		ReadOnlySpan<int> boneToNodeMap,
		ReadOnlySpan<int> mutationTargetIndexMap
	) {
		if (_currentSkeleton != null) {
			throw new InvalidOperationException("Skeleton already set for this animation table (this is a bug in TinyFFR).");
		}

		_currentSkeleton = new(
			owningMesh,
			nodeCount,
			boneCount,
			firstParentedNodeIndex,
			_globals.HeapPool.BorrowAndCopy(defaultLocalTransforms),
			_globals.HeapPool.BorrowAndCopy(bindPoseInversions),
			_globals.HeapPool.Borrow<Matrix4x4>(nodeCount),
			_globals.HeapPool.BorrowAndCopy(parentIndices),
			_globals.HeapPool.BorrowAndCopy(boneToNodeMap),
			_globals.HeapPool.BorrowAndCopy(mutationTargetIndexMap),
			modelImportTransformMatrix
		);
	}

	public MeshAnimation AddPreProcessedAnimation(
		ReadOnlySpan<SkeletalAnimationScalingKeyframe> scalingKeyframes,
		ReadOnlySpan<SkeletalAnimationRotationKeyframe> rotationKeyframes,
		ReadOnlySpan<SkeletalAnimationTranslationKeyframe> translationKeyframes,
		ReadOnlySpan<SkeletalAnimationNodeMutationDescriptor> nodeMutations,
		float defaultCompletionTimeSeconds,
		ReadOnlySpan<char> name
	) {
		ThrowIfThisIsDisposed();
		_ = GetSkeletonOrThrow();

		var handle = new ResourceHandle<MeshAnimation>(++_prevHandleId);
		var data = new AnimationData(
			_globals.HeapPool.BorrowAndCopy(scalingKeyframes),
			_globals.HeapPool.BorrowAndCopy(rotationKeyframes),
			_globals.HeapPool.BorrowAndCopy(translationKeyframes),
			_globals.HeapPool.BorrowAndCopy(nodeMutations),
			defaultCompletionTimeSeconds
		);

		_animationDataMap.Add(handle, data);
		_globals.StoreMandatoryResourceName(handle.Ident, name);
		_animationNameMap.Add(name, HandleToInstance(handle));

		return HandleToInstance(handle);
	}

	public void SetNodeNameFromBakedData(int nodeIndex, ReadOnlySpan<char> name) {
		_nodeNameMap[name] = new MeshNode((nuint) nodeIndex, _meshNodeImplProvider);
	}
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
