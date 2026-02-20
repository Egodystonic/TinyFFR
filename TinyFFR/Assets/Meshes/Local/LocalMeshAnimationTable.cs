// Created on 2026-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
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
		float DefaultCompletionTimeSeconds,
		PooledHeapMemory<AnimationChannelHeader> ChannelHeaders,
		PooledHeapMemory<AnimationTranslationKeyframe> TranslationKeys,
		PooledHeapMemory<AnimationRotationKeyframe> RotationKeys,
		PooledHeapMemory<AnimationScalingKeyframe> ScalingKeys
	);
	readonly record struct SkeletonData(
		int BoneCount, 
		PooledHeapMemory<int> OriginalIndices, 
		PooledHeapMemory<int> ParentIndices, 
		PooledHeapMemory<Matrix4x4> BindPoseInversionMatrices, 
		PooledHeapMemory<Matrix4x4> DefaultLocalTransforms
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

	public void SetSkeleton(int boneCount, ReadOnlySpan<int> parentIndices, ReadOnlySpan<Matrix4x4> bindPoseInversionMatrices, ReadOnlySpan<Matrix4x4> defaultLocalTransforms) {
		if (_currentSkeleton != null) {
			throw new InvalidOperationException("Skeleton already set for this animation table (this is a bug in TinyFFR).");
		}
		if (boneCount is < 1 or > IMeshBuilder.MaxSkeletalBoneCount) {
			throw new InvalidOperationException($"Bone count = {boneCount} (this is a bug in TinyFFR).");
		}
		if (boneCount != parentIndices.Length || boneCount != bindPoseInversionMatrices.Length || boneCount != defaultLocalTransforms.Length) {
			throw new InvalidOperationException("Given bone count did not match length of at least one input buffer (this is a bug in TinyFFR).");
		}

		static void ComputeProcessingOrder(ReadOnlySpan<int> parentIndices, Span<int> indicesInProcessingOrder, Span<int> originalToProcessingOrderLookup) {
			// Maintainer's note: This algorithm works by determining the dependency depth of each bone index.
			// Bones with the same depth can't rely on each other, so a simple ordering of bones by dependency depth means
			// we'll always process them in the right order down below when applying an animation.
			// (The reordering itself is necessary as when processing the animation, some transformations rely on transforms
			// calculated on parent bones higher up the skeletal heirarchy first).
			var numBones = parentIndices.Length;
			Span<int> depths = stackalloc int[numBones];
			
			var maxDepth = 0;
			for (var i = 0; i < numBones; ++i) {
				var curBoneIndex = i;
				while (parentIndices[curBoneIndex] > -1) {
					maxDepth = Int32.Max(maxDepth, ++depths[i]);
					curBoneIndex = parentIndices[curBoneIndex];
				}
			}
			
			var o = 0;
			for (var curDepthToSearchFor = 0; curDepthToSearchFor <= maxDepth; ++curDepthToSearchFor) {
				for (var i = 0; i < numBones; ++i) {
					if (depths[i] == curDepthToSearchFor) {
						indicesInProcessingOrder[o] = i;
						originalToProcessingOrderLookup[i] = o;
						++o;
					}
				}
			}
		}
		
		Span<int> processingOrder = stackalloc int[boneCount];
		Span<int> originalToProcessingOrderLookup = stackalloc int[boneCount];
		ComputeProcessingOrder(parentIndices, processingOrder, originalToProcessingOrderLookup);
		
		var originalIndicesHeapMemory = _globals.HeapPool.Borrow<int>(boneCount);
		var parentIndicesHeapMemory = _globals.HeapPool.Borrow<int>(boneCount);
		var bindPoseInversionMatricesHeapMemory = _globals.HeapPool.Borrow<Matrix4x4>(boneCount);
		var defaultLocalTransformsHeapMemory = _globals.HeapPool.Borrow<Matrix4x4>(boneCount);
		
		for (var i = 0; i < processingOrder.Length; ++i) {
			var originalIndex = processingOrder[i];
			originalIndicesHeapMemory.Buffer[i] = originalIndex;
			var originalParentIndex = parentIndices[originalIndex];
			parentIndicesHeapMemory.Buffer[i] = originalParentIndex >= 0 ? originalToProcessingOrderLookup[originalParentIndex] : -1;
			bindPoseInversionMatricesHeapMemory.Buffer[i] = bindPoseInversionMatrices[originalIndex];
			defaultLocalTransformsHeapMemory.Buffer[i] = defaultLocalTransforms[originalIndex];
		}
		
		_currentSkeleton = new(
			boneCount,
			originalIndicesHeapMemory,
			parentIndicesHeapMemory,
			bindPoseInversionMatricesHeapMemory,
			defaultLocalTransformsHeapMemory
		);
	}

	public MeshAnimation Add(ReadOnlySpan<char> name, float defaultCompletionTimeSeconds,
		ReadOnlySpan<AnimationChannelHeader> channelHeaders,
		ReadOnlySpan<AnimationVectorKeyframe> translationKeys,
		ReadOnlySpan<AnimationQuaternionKeyframe> rotationKeys,
		ReadOnlySpan<AnimationVectorKeyframe> scalingKeys) {
		ThrowIfThisIsDisposed();

		var chBorrow = _globals.HeapPool.Borrow<AnimationChannelHeader>(channelHeaders.Length);
		channelHeaders.CopyTo(chBorrow.Buffer);
		var pkBorrow = _globals.HeapPool.Borrow<AnimationVectorKeyframe>(translationKeys.Length);
		translationKeys.CopyTo(pkBorrow.Buffer);
		var rkBorrow = _globals.HeapPool.Borrow<AnimationQuaternionKeyframe>(rotationKeys.Length);
		rotationKeys.CopyTo(rkBorrow.Buffer);
		var skBorrow = _globals.HeapPool.Borrow<AnimationVectorKeyframe>(scalingKeys.Length);
		scalingKeys.CopyTo(skBorrow.Buffer);

		var data = new AnimationData(defaultCompletionTimeSeconds, chBorrow, pkBorrow, rkBorrow, skBorrow);
		++_nextHandleId;
		var newHandle = new ResourceHandle<MeshAnimation>(_nextHandleId);
		_dataMap.Add(newHandle, data);
		_globals.StoreMandatoryResourceName(newHandle.Ident, name);
		_nameMap.Add(name, HandleToInstance(newHandle));
		return HandleToInstance(newHandle);
	}

	public float GetDefaultCompletionTimeSeconds(ResourceHandle<MeshAnimation> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _dataMap[handle].DefaultCompletionTimeSeconds;
	}
	
	public MeshAnimationType GetType(ResourceHandle<MeshAnimation> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return MeshAnimationType.Skeletal;
	}
	
	public void Apply(ResourceHandle<MeshAnimation> handle, ModelInstance targetInstance, float targetTimePointSeconds) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (_currentSkeleton is not { } skeleton) return;
		var animation = _dataMap[handle];
		var boneCount = skeleton.BoneCount;

		var channelHeaders = animation.ChannelHeaders.Buffer;
		var translationKeys = animation.TranslationKeys.Buffer;
		var rotationKeys = animation.RotationKeys.Buffer;
		var scalingKeys = animation.ScalingKeys.Buffer;

		var globalTransformsBuffer = _applyTransformsBuffer.Rent<Matrix4x4>(boneCount);
		var outputTransformsBuffer = _applyTransformsBuffer.Rent<Matrix4x4>(boneCount);
		
		try {
			var globalTransforms = globalTransformsBuffer.AsSpan<Matrix4x4>(boneCount);
			var outputTransforms = outputTransformsBuffer.AsSpan<Matrix4x4>(boneCount);

			var defaultLocalTransforms = skeleton.DefaultLocalTransforms.Buffer;
			var bindPoseInversionMatrices = skeleton.BindPoseInversionMatrices.Buffer;
			var parentIndices = skeleton.ParentIndices.Buffer;
			var originalIndices = skeleton.OriginalIndices.Buffer;

			// Step 1: Use outputTransforms to calcualte local transforms (for now)
			for (var i = 0; i < boneCount; ++i) {
				outputTransforms[originalIndices[i]] = defaultLocalTransforms[i];
			}
			
			// Step 2: Overwrite local transforms according to channels
			static TValue InterpolateKeyframes<T, TValue>(ReadOnlySpan<T> keys, float targetTimeSecs) where T : IAnimationKeyframe<TValue> where TValue : IInterpolatable<TValue> {
				if (keys.Length == 0) return T.FallbackValue;
				if (keys.Length == 1 || targetTimeSecs <= keys[0].TimeKeySeconds) return keys[0].Value;
				if (targetTimeSecs >= keys[^1].TimeKeySeconds) return keys[^1].Value;
				
				// Binary search
				var lowIndex = 0;
				var highIndex = keys.Length - 1;
				while (lowIndex < highIndex - 1) {
					var curMidpointIndex = (lowIndex + highIndex) >> 1;
					if (keys[curMidpointIndex].TimeKeySeconds <= targetTimeSecs) lowIndex = curMidpointIndex;
					else highIndex = curMidpointIndex;
				}
				
				var low = keys[lowIndex];
				var high = keys[highIndex];
				return TValue.Interpolate(
					low.Value, 
					high.Value, 
					Real.GetInterpolationDistance(low.TimeKeySeconds, high.TimeKeySeconds, targetTimeSecs)
				);
			}
			
			for (var c = 0; c < channelHeaders.Length; ++c) {
				var channel = channelHeaders[c];
				var transform = new Transform(
					scaling: InterpolateKeyframes<AnimationScalingKeyframe, Vect>(scalingKeys.Slice(channel.ScalingKeyStart, channel.ScalingKeyCount), targetTimePointSeconds),
					rotation: InterpolateKeyframes<AnimationRotationKeyframe, Rotation>(rotationKeys.Slice(channel.RotationKeyStart, channel.RotationKeyCount), targetTimePointSeconds),
					translation: InterpolateKeyframes<AnimationTranslationKeyframe, Vect>(translationKeys.Slice(channel.TranslationKeyStart, channel.TranslationKeyCount), targetTimePointSeconds)
				);

				transform.ToMatrix(ref outputTransforms[channel.BoneIndex]);
			}

			// Step 3: Accumulate global transforms and compute final output
			for (var i = 0; i < boneCount; ++i) {
				var originalBoneIndex = originalIndices[i];
				var parentBoneIndex = parentIndices[i];
				globalTransforms[i] = parentBoneIndex < 0 ? outputTransforms[originalBoneIndex] : outputTransforms[originalBoneIndex] * globalTransforms[parentBoneIndex];
				outputTransforms[originalBoneIndex] = bindPoseInversionMatrices[i] * globalTransforms[i];
			}

			// Step 4: Send it
			SetModelInstanceBoneTransforms(
				targetInstance.Handle, 
				(Matrix4x4*) outputTransformsBuffer.StartPtr, 
				boneCount
			).ThrowIfFailure();
		}
		finally {
			_applyTransformsBuffer.Return(globalTransformsBuffer);
			_applyTransformsBuffer.Return(outputTransformsBuffer);
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
			data.ChannelHeaders.Dispose();
			data.TranslationKeys.Dispose();
			data.RotationKeys.Dispose();
			data.ScalingKeys.Dispose();
		}
		_dataMap.Clear();
		_nameMap.Clear();
		
		_currentSkeleton?.BindPoseInversionMatrices.Dispose();
		_currentSkeleton?.DefaultLocalTransforms.Dispose();
		_currentSkeleton?.ParentIndices.Dispose();
		_currentSkeleton?.OriginalIndices.Dispose();
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
