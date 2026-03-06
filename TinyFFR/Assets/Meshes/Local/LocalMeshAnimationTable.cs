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
	readonly ArrayPoolBackedStringKeyMap<MeshAnimation> _nameMap = new();
	readonly ArrayPoolBackedMap<ResourceHandle<MeshAnimation>, AnimationData> _dataMap = new();
	readonly FixedByteBufferPool _applyTransformsBuffer = new(IMeshBuilder.MaxSkeletalBoneCount * sizeof(Matrix4x4));
	readonly LocalFactoryGlobalObjectGroup _globals;
	SkeletonData? _currentSkeleton = null;
	bool _isDisposed = false;

	public LocalMeshAnimationTable(LocalFactoryGlobalObjectGroup globals) => _globals = globals;

	public int Count => _nameMap.Count;
	public MeshAnimation? FindByName(ReadOnlySpan<char> name) => _nameMap.TryGetValue(name, out var animData) ? animData : null;
	public ArrayPoolBackedMap<ManagedStringPool.RentedStringHandle, MeshAnimation>.KeyEnumerator Keys => _nameMap.Keys;
	public ArrayPoolBackedMap<ManagedStringPool.RentedStringHandle, MeshAnimation>.ValueEnumerator Values => _nameMap.Values;
	public ArrayPoolBackedMap<ManagedStringPool.RentedStringHandle, MeshAnimation>.Enumerator GetEnumerator() => _nameMap.GetEnumerator();

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
		
		_dataMap.Add(handle, data);
		_globals.StoreMandatoryResourceName(handle.Ident, name);
		_nameMap.Add(name, HandleToInstance(handle));
		
		return HandleToInstance(handle);
	}


	public float GetDefaultDurationSeconds(ResourceHandle<MeshAnimation> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _dataMap[handle].DefaultCompletionTimeSeconds;
	}
	
	public MeshAnimationType GetType(ResourceHandle<MeshAnimation> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return MeshAnimationType.Skeletal;
	}
	
	static string MatStr(Matrix4x4 m) {
		var result = "<";
		var isIdentity = true;
		var isOnlyTranslation = true;
		for (var r = 0; r < 4; ++r) {
			for (var c = 0; c < 4; ++c) {
				var val2dp = m[r, c].ToString("N2", CultureInfo.InvariantCulture);
				result += val2dp + (r == 3 && c == 3 ? ">" : " ");
				if (MathF.Abs(Matrix4x4.Identity[r, c] - m[r, c]) > 0.001f) {
					isIdentity = false;
					if (r != 3) isOnlyTranslation = false;
				}
			}
			if (r != 3) result += "| ";
		}
		
		//return result;
		if (isIdentity) return "Identity";
		if (isOnlyTranslation) return $"Translation[{m[3, 0]:N2}/{m[3,1]:N2}/{m[3,2]:N2}]";
		
		foreach (var c in OrientationUtils.AllCardinals) {
			var rotMat = new Transform(rotation: 90f % c.ToDirection()).ToMatrix();
			for (var x = 0; x < 3; ++x) {
				for (var y = 0; y < 3; ++y) {
					if (MathF.Abs(rotMat[x, y] - m[x, y]) >= 0.001f) {
						goto noMatch;
					}
				}
			}
			result = "Rotation[" + new Angle(90f).ToString("N0", CultureInfo.InvariantCulture) + " around " + c +"]";
			if (MathF.Abs(m[3, 0]) > 0.001f || MathF.Abs(m[3, 1]) > 0.001f || MathF.Abs(m[3, 2]) > 0.001f) {
				result += $"Translation[{m[3, 0]:N2}/{m[3,1]:N2}/{m[3,2]:N2}]";
			}  
			return result;
			noMatch: continue;
		}
		
		return result;
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
		
		var workspace = skeleton.Workspace.Buffer;
		skeleton.DefaultLocalTransforms.Buffer.CopyTo(workspace);
		var bindPoseInversions = skeleton.BindPoseInversions.Buffer;
		var parents = skeleton.ParentIndices.Buffer;
		var boneToNodeMap = skeleton.BoneToNodeMap.Buffer;
		var firstParentedNodeIndex = skeleton.FirstParentedNodeIndex;
		var nodeCount = skeleton.Workspace.Buffer.Length;
		var boneCount = skeleton.BoneCount;
		
		var resultBuffer = _applyTransformsBuffer.Rent<Matrix4x4>(skeleton.BoneCount);
		var results = resultBuffer.AsSpan<Matrix4x4>(skeleton.BoneCount);
		
		try {
			for (var i = firstParentedNodeIndex; i < nodeCount; ++i) {
				workspace[i] *= workspace[parents[i]];
			}
			
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
	
	public void Apply(ResourceHandle<MeshAnimation> handle, ModelInstance targetInstance, float targetTimePointSeconds) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (_currentSkeleton is not { } skeleton) return;
		if (targetInstance.Mesh != skeleton.OwningMesh) {
			throw new InvalidOperationException(
				$"Can not apply animation to {targetInstance} via {nameof(MeshAnimationIndex)} for a different mesh. " +
				$"Model instance is using {targetInstance.Mesh}, {nameof(MeshAnimationIndex)} is for {skeleton.OwningMesh}."
			);
		}

		var workspace = skeleton.Workspace.Buffer;
		skeleton.DefaultLocalTransforms.Buffer.CopyTo(workspace);
		var bindPoseInversions = skeleton.BindPoseInversions.Buffer;
		var parents = skeleton.ParentIndices.Buffer;
		var boneToNodeMap = skeleton.BoneToNodeMap.Buffer;
		var firstParentedNodeIndex = skeleton.FirstParentedNodeIndex;
		
		var animation = _dataMap[handle];
		var nodeCount = skeleton.NodeCount;
		var boneCount = skeleton.BoneCount;
		var mutations = animation.BoneMutationDescriptors.Buffer;
		var translationKeys = animation.TranslationKeyframes.Buffer;
		var rotationKeys = animation.RotationKeyframes.Buffer;
		var scalingKeys = animation.ScalingKeyframes.Buffer;

		var resultBuffer = _applyTransformsBuffer.Rent<Matrix4x4>(skeleton.BoneCount);
		var results = resultBuffer.AsSpan<Matrix4x4>(skeleton.BoneCount);
		
		try {
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
			
			for (var i = 0; i < mutations.Length; ++i) {
				var mutation = mutations[i];
				var transform = new Transform(
					scaling: InterpolateKeyframes<SkeletalAnimationScalingKeyframe, Vect>(scalingKeys.Slice(mutation.ScalingKeyframeStartIndex, mutation.ScalingKeyframeCount), targetTimePointSeconds),
					rotationQuaternion: InterpolateKeyframes<SkeletalAnimationRotationKeyframe, Quaternion>(rotationKeys.Slice(mutation.RotationKeyframeStartIndex, mutation.RotationKeyframeCount), targetTimePointSeconds),
					translation: InterpolateKeyframes<SkeletalAnimationTranslationKeyframe, Vect>(translationKeys.Slice(mutation.TranslationKeyframeStartIndex, mutation.TranslationKeyframeCount), targetTimePointSeconds)
				);

				transform.ToMatrix(ref workspace[mutation.TargetNodeIndex]);
			}

			for (var i = firstParentedNodeIndex; i < nodeCount; ++i) {
				workspace[i] *= workspace[parents[i]];
			}
			
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

	public MeshAnimation GetAnimationAtUnstableIndex(int index) => HandleToInstance(_dataMap.GetPairAtIndex(index).Key);

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
	public bool IsDisposed(ResourceHandle<MeshAnimation> handle) => _isDisposed || !_dataMap.ContainsKey(handle);
	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<MeshAnimation> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(MeshAnimation));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);

	public void Recycle() {
		foreach (var kvp in _dataMap) {
			var data = kvp.Value;
			data.BoneMutationDescriptors.Dispose();
			data.TranslationKeyframes.Dispose();
			data.RotationKeyframes.Dispose();
			data.ScalingKeyframes.Dispose();
		}
		_dataMap.Clear();
		_nameMap.Clear();
		
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
		_nameMap.Dispose();
		_dataMap.Dispose();
	}
	#endregion
}
