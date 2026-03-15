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
		PooledHeapMemory<int> MutationTargetIndexMap
	);
	static nuint _nextHandleId = 0U;
	readonly MeshNodeImplProvider _meshNodeImplProvider;
	readonly ArrayPoolBackedStringKeyMap<MeshAnimation> _animationNameMap = new();
	readonly ArrayPoolBackedMap<ResourceHandle<MeshAnimation>, AnimationData> _animationDataMap = new();
	readonly ArrayPoolBackedStringKeyMap<MeshNode> _nodeNameMap = new();
	readonly FixedByteBufferPool _applyTransformsBuffer = new(IMeshBuilder.MaxSkeletalBoneCount * sizeof(Matrix4x4));
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
	}

	public int Count => _animationNameMap.Count;
	public MeshAnimation? FindByName(ReadOnlySpan<char> name) => _animationNameMap.TryGetValue(name, out var animData) ? animData : null;
	public ArrayPoolBackedMap<ManagedStringPool.RentedStringHandle, MeshAnimation>.KeyEnumerator Keys => _animationNameMap.Keys;
	public ArrayPoolBackedMap<ManagedStringPool.RentedStringHandle, MeshAnimation>.ValueEnumerator Values => _animationNameMap.Values;
	public ArrayPoolBackedMap<ManagedStringPool.RentedStringHandle, MeshAnimation>.Enumerator GetEnumerator() => _animationNameMap.GetEnumerator();

	public void SetSkeleton(Mesh owningMesh, int boneCount, ReadOnlySpan<SkeletalAnimationNode> skeletalNodes) {
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
			inputToOutputIndexMapHeapMemory
		);
	}

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
		if (_currentSkeleton is not { } skeleton) {
			throw new InvalidOperationException("Skeleton not set for this animation table (this is a bug in TinyFFR).");
		}
		
		var handle = new ResourceHandle<MeshAnimation>(++_nextHandleId);
		for (var m = 0; m < nodeMutations.Buffer.Length; ++m) {
			nodeMutations.Buffer[m] = nodeMutations.Buffer[m] with { TargetNodeIndex = skeleton.MutationTargetIndexMap.Buffer[nodeMutations.Buffer[m].TargetNodeIndex] };
		}
		// We pre-sort the mutations by target node index to get better cache locality later when writing the animation matrices to the workspace buffer
		nodeMutations.Buffer.Sort(static (a, b) => a.TargetNodeIndex.CompareTo(b.TargetNodeIndex));
		var data = new AnimationData(scalingKeyframes, rotationKeyframes, translationKeyframes, nodeMutations, defaultCompletionTimeSeconds);
		
		_animationDataMap.Add(handle, data);
		_globals.StoreMandatoryResourceName(handle.Ident, name);
		_animationNameMap.Add(name, HandleToInstance(handle));
		
		return HandleToInstance(handle);
	}
	
	public void SetNodeName(int nodeIndex, ReadOnlySpan<char> name) {
		ThrowIfThisIsDisposed();
		if (_currentSkeleton is not { } skeleton) {
			throw new InvalidOperationException("Skeleton not set for this animation table (this is a bug in TinyFFR).");
		}
		if (nodeIndex < 0 || nodeIndex >= skeleton.NodeCount) throw new ArgumentOutOfRangeException(nameof(nodeIndex), nodeIndex, $"Node index must be non-negative and less than node count ({skeleton.NodeCount}).");
		
		nodeIndex = skeleton.MutationTargetIndexMap.Buffer[nodeIndex];
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
			newNameBuffer.Buffer[name.Length] = '_';
			var repeatCharsStartIndex = name.Length + 1;
			var repeatCount = 2;
			do {
				if (!repeatCount.TryFormat(newNameBuffer.Buffer[repeatCharsStartIndex..], out var repeatCharsCount, provider: CultureInfo.InvariantCulture)) {
					throw new InvalidOperationException($"Can not handle repeated node name '{name}'.");
				}
				name = newNameBuffer.Buffer[..(repeatCharsStartIndex + repeatCharsCount)];
			} while (_nodeNameMap.ContainsKey(name));
		}
		_nodeNameMap[name] = meshNode;
	}
	public int GetNodeCount() => _currentSkeleton?.NodeCount ?? 0;
	public MeshNode GetNode(int index) => index < _currentSkeleton?.NodeCount ? new(new ResourceHandle<MeshNode>((nuint) index), _meshNodeImplProvider) : throw new ArgumentOutOfRangeException(nameof(index));
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
	
	void WriteBindPoseNodeTransformsToWorkspace() {
		ThrowIfThisIsDisposed();
		if (_currentSkeleton is not { } skeleton) return;
		
		var workspace = skeleton.Workspace.Buffer;
		var parents = skeleton.ParentIndices.Buffer;
		var firstParentedNodeIndex = skeleton.FirstParentedNodeIndex;
		var nodeCount = skeleton.Workspace.Buffer.Length;
		
		skeleton.DefaultLocalTransforms.Buffer.CopyTo(workspace);
		
		for (var i = firstParentedNodeIndex; i < nodeCount; ++i) {
			workspace[i] *= workspace[parents[i]];
		}
	}
	
	public void ApplyBindPose(ModelInstance targetInstance) {
		ThrowIfThisIsDisposed();
		if (_currentSkeleton is not { } skeleton) return;
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
		
		var resultBuffer = _applyTransformsBuffer.Rent<Matrix4x4>(skeleton.BoneCount);
		var results = resultBuffer.AsSpan<Matrix4x4>(skeleton.BoneCount);
		
		try {
			for (var i = 0; i < boneCount; ++i) {
				results[i] = bindPoseInversions[i] * workspace[boneToNodeMap[i]];
			}
			
			SetModelInstanceBoneTransforms(
				targetInstance.Handle, 
				(Matrix4x4*) resultBuffer.StartPtr, 
				results.Length
			).ThrowIfFailure();
		}
		finally {
			_applyTransformsBuffer.Return(resultBuffer);
		}
	}
	public void GetBindPoseNodeTransforms(ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		WriteBindPoseNodeTransformsToWorkspace();
		CopyRequestedNodeTransformsFromWorkspace(nodes, modelSpaceTransforms);
	}
	
	void WriteAnimationNodeTransformsToWorkspace(ResourceHandle<MeshAnimation> handle, float targetTimePointSeconds) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (_currentSkeleton is not { } skeleton) return;

		var workspace = skeleton.Workspace.Buffer;
		var parents = skeleton.ParentIndices.Buffer;
		var firstParentedNodeIndex = skeleton.FirstParentedNodeIndex;
		
		var animation = _animationDataMap[handle];
		var nodeCount = skeleton.NodeCount;
		var mutations = animation.BoneMutationDescriptors.Buffer;
		var translationKeys = animation.TranslationKeyframes.Buffer;
		var rotationKeys = animation.RotationKeyframes.Buffer;
		var scalingKeys = animation.ScalingKeyframes.Buffer;
		
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
		
		skeleton.DefaultLocalTransforms.Buffer.CopyTo(workspace);
		
		for (var i = 0; i < mutations.Length; ++i) {
			var mutation = mutations[i];
			var transform = new Transform(
				scaling: InterpolateKeyframes<SkeletalAnimationScalingKeyframe, Vect>(scalingKeys.Slice(mutation.ScalingKeyframeStartIndex, mutation.ScalingKeyframeCount), targetTimePointSeconds),
				rotationQuaternion: InterpolateKeyframes<SkeletalAnimationRotationKeyframe, Quaternion>(rotationKeys.Slice(mutation.RotationKeyframeStartIndex, mutation.RotationKeyframeCount), targetTimePointSeconds),
				translation: InterpolateKeyframes<SkeletalAnimationTranslationKeyframe, Vect>(translationKeys.Slice(mutation.TranslationKeyframeStartIndex, mutation.TranslationKeyframeCount), targetTimePointSeconds)
			);

			transform.ToMatrix(out workspace[mutation.TargetNodeIndex]);
		}

		for (var i = firstParentedNodeIndex; i < nodeCount; ++i) {
			workspace[i] *= workspace[parents[i]];
		}
	}
	
	void CopyRequestedNodeTransformsFromWorkspace(ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		if (nodes.Length > modelSpaceTransforms.Length) {
			throw new ArgumentException($"Requested {nodes.Length} {nameof(nodes)}, but {nameof(modelSpaceTransforms)} destination span is too small (length {modelSpaceTransforms.Length}).", nameof(modelSpaceTransforms));
		}
		
		static void ThrowIfNodeInvalid(MeshNode node, IMeshNodeImplProvider implProvider, int nodeCount) {
			var nodeIndex = (int) node.GetHandleWithoutDisposeCheck().AsInteger;
			if (!ReferenceEquals(node.Implementation, implProvider) || nodeIndex < 0 || nodeIndex >= nodeCount) {
				throw new ArgumentException($"Given node {node} is not valid for this mesh.", nameof(nodes));
			}
		}
		
		if (_currentSkeleton is { } skeleton) {
			for (var i = 0; i < nodes.Length; ++i) {
				var node = nodes[i];
				ThrowIfNodeInvalid(node, _meshNodeImplProvider, skeleton.NodeCount);
				var nodeIndex = (int) node.GetHandleWithoutDisposeCheck().AsInteger;
				modelSpaceTransforms[i] = skeleton.Workspace.Buffer[nodeIndex];
			}
		}
		else {
			for (var i = 0; i < nodes.Length; ++i) {
				var node = nodes[i];
				ThrowIfNodeInvalid(node, _meshNodeImplProvider, Int32.MaxValue);
				modelSpaceTransforms[i] = Matrix4x4.Identity;
			}	
		}
	}
	
	public void Apply(ResourceHandle<MeshAnimation> handle, ModelInstance targetInstance, float targetTimePointSeconds) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (_currentSkeleton is not { } skeleton) return;
		if (targetInstance.Mesh != skeleton.OwningMesh) {
			throw new InvalidOperationException(
				$"Can not apply animation to {targetInstance} via {nameof(MeshAnimationIndex)} for a different mesh. " +
				$"Model instance is using {targetInstance.Mesh}, {nameof(MeshAnimationIndex)} is for {skeleton.OwningMesh}."
			);
		}
		WriteAnimationNodeTransformsToWorkspace(handle, targetTimePointSeconds);

		var workspace = skeleton.Workspace.Buffer;
		var bindPoseInversions = skeleton.BindPoseInversions.Buffer;
		var boneToNodeMap = skeleton.BoneToNodeMap.Buffer;
		var boneCount = skeleton.BoneCount;

		var resultBuffer = _applyTransformsBuffer.Rent<Matrix4x4>(skeleton.BoneCount);
		var results = resultBuffer.AsSpan<Matrix4x4>(skeleton.BoneCount);
		
		try {
			for (var i = 0; i < boneCount; ++i) {
				results[i] = bindPoseInversions[i] * workspace[boneToNodeMap[i]];
			}
			
			SetModelInstanceBoneTransforms(
				targetInstance.Handle, 
				(Matrix4x4*) resultBuffer.StartPtr, 
				results.Length
			).ThrowIfFailure();
		}
		finally {
			_applyTransformsBuffer.Return(resultBuffer);
		}
	}

	public void ApplyAndGetNodeTransforms(ResourceHandle<MeshAnimation> handle, ModelInstance targetInstance, float targetTimePointSeconds, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		Apply(handle, targetInstance, targetTimePointSeconds);
		CopyRequestedNodeTransformsFromWorkspace(nodes, modelSpaceTransforms);
	}

	public void GetNodeTransforms(ResourceHandle<MeshAnimation> handle, float targetTimePointSeconds, ReadOnlySpan<MeshNode> nodes, Span<Matrix4x4> modelSpaceTransforms) {
		WriteAnimationNodeTransformsToWorkspace(handle, targetTimePointSeconds);
		CopyRequestedNodeTransformsFromWorkspace(nodes, modelSpaceTransforms);
	}

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

	public MeshAnimation GetAnimationAtUnstableIndex(int index) => HandleToInstance(_animationDataMap.GetPairAtIndex(index).Key);

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

	#region Disposal
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
