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
		PooledHeapMemory<SkeletalAnimationBoneMutationDescriptor> BoneMutationDescriptors,
		float DefaultCompletionTimeSeconds
	);
	readonly record struct SkeletonData(
		int BoneCount, 
		PooledHeapMemory<Matrix4x4> DefaultLocalTransforms,
		PooledHeapMemory<Matrix4x4> BindPoseInversions, 
		PooledHeapMemory<int> ParentIndices,
		PooledHeapMemory<int> OutputIndices
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

	public void SetSkeleton(int boneCount, ReadOnlySpan<SkeletalAnimationNode> skeletalNodes) {
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
		Span<int> inputToOutputIndexMap = stackalloc int[nodeCount];
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
					inputToOutputIndexMap[i] = processingOrderCursor;
					processingOrder[processingOrderCursor++] = i;
				}
			}
		}
		
		var bindPoseInversionsHeapMemory = _globals.HeapPool.Borrow<Matrix4x4>(nodeCount);
		var defaultLocalTransformsHeapMemory = _globals.HeapPool.Borrow<Matrix4x4>(nodeCount);
		var parentIndicesHeapMemory = _globals.HeapPool.Borrow<int>(nodeCount);
		var outputIndicesHeapMemory = _globals.HeapPool.Borrow<int>(nodeCount);
		
		for (var outputIndex = 0; outputIndex < processingOrder.Length; ++outputIndex) {
			var inputIndex = processingOrder[outputIndex];
			bindPoseInversionsHeapMemory.Buffer[outputIndex] = skeletalNodes[inputIndex].BindPoseInversion;
			defaultLocalTransformsHeapMemory.Buffer[outputIndex] = skeletalNodes[inputIndex].DefaultLocalTransform;
			parentIndicesHeapMemory.Buffer[outputIndex] = skeletalNodes[inputIndex].ParentNodeIndex.HasValue ? inputToOutputIndexMap[skeletalNodes[inputIndex].ParentNodeIndex!.Value] : -1;
			outputIndicesHeapMemory.Buffer[outputIndex] = skeletalNodes[inputIndex].CorrespondingBoneIndex ?? -1;
		}

		_currentSkeleton = new(
			boneCount,
			defaultLocalTransformsHeapMemory,
			bindPoseInversionsHeapMemory,
			parentIndicesHeapMemory,
			outputIndicesHeapMemory
		);
		
		Console.WriteLine("SKELETON:");
		Console.WriteLine("\tBoneCount: " + _currentSkeleton.Value.BoneCount);
		for (var i = 0; i < nodeCount; ++i) {
			Console.WriteLine("\tNode " + i + " Parent(" + parentIndicesHeapMemory.Buffer[i] + ") Bone(" + outputIndicesHeapMemory.Buffer[i] + "):");
			Console.WriteLine("\t\tDLT: " + MatStr(defaultLocalTransformsHeapMemory.Buffer[i]));
			Console.WriteLine("\t\tBPI: " + MatStr(bindPoseInversionsHeapMemory.Buffer[i]));
		}
	}

	public MeshAnimation Add( 
		ReadOnlySpan<SkeletalAnimationScalingKeyframe> scalingKeyframes,
		ReadOnlySpan<SkeletalAnimationRotationKeyframe> rotationKeyframes, 
		ReadOnlySpan<SkeletalAnimationTranslationKeyframe> translationKeyframes, 
		ReadOnlySpan<SkeletalAnimationBoneMutationDescriptor> boneMutations, 
		float defaultCompletionTimeSeconds, 
		ReadOnlySpan<char> name
	) {
		ThrowIfThisIsDisposed();
		
		var scalingHeapBuffer = _globals.HeapPool.BorrowAndCopy(scalingKeyframes);
		var rotationHeapBuffer = _globals.HeapPool.BorrowAndCopy(rotationKeyframes);
		var translationHeapBuffer = _globals.HeapPool.BorrowAndCopy(translationKeyframes);
		var mutationHeapBuffer = _globals.HeapPool.BorrowAndCopy(boneMutations);
		
		return AddAndTransferBufferOwnership(scalingHeapBuffer, rotationHeapBuffer, translationHeapBuffer, mutationHeapBuffer, defaultCompletionTimeSeconds, name);
	}
	public MeshAnimation AddAndTransferBufferOwnership( 
		PooledHeapMemory<SkeletalAnimationScalingKeyframe> scalingKeyframes, 
		PooledHeapMemory<SkeletalAnimationRotationKeyframe> rotationKeyframes, 
		PooledHeapMemory<SkeletalAnimationTranslationKeyframe> translationKeyframes, 
		PooledHeapMemory<SkeletalAnimationBoneMutationDescriptor> boneMutations, 
		float defaultCompletionTimeSeconds, 
		ReadOnlySpan<char> name
	) {
		ThrowIfThisIsDisposed();
		
		var handle = new ResourceHandle<MeshAnimation>(++_nextHandleId);
		var data = new AnimationData(scalingKeyframes, rotationKeyframes, translationKeyframes, boneMutations, defaultCompletionTimeSeconds);
		
		_dataMap.Add(handle, data);
		_globals.StoreMandatoryResourceName(handle.Ident, name);
		_nameMap.Add(name, HandleToInstance(handle));
		
		Console.WriteLine($"ANIM '{name}':");
		Console.WriteLine("\tScaling Keyframes: " + String.Join(", ", data.ScalingKeyframes.Buffer.ToArray()));
		Console.WriteLine("\tRotation Keyframes: " + String.Join(", ", data.RotationKeyframes.Buffer.ToArray()));
		Console.WriteLine("\tTranslation Keyframes: " + String.Join(", ", data.TranslationKeyframes.Buffer.ToArray()));
		Console.WriteLine("\tMutations: " + String.Join(", ", data.BoneMutationDescriptors.Buffer.ToArray()));
		
		return HandleToInstance(handle);
	}


	public float GetDefaultCompletionTimeSeconds(ResourceHandle<MeshAnimation> handle) {
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
		return isIdentity ? "Identity" : (isOnlyTranslation ? $"Translation[{m[3, 0]:N2}/{m[3,1]:N2}/{m[3,2]:N2}]" : result);
	}
	
	public void Apply(ResourceHandle<MeshAnimation> handle, ModelInstance targetInstance, float targetTimePointSeconds) {
		ThrowIfThisOrHandleIsDisposed(handle);
		if (_currentSkeleton is not { } skeleton) return;
		var animation = _dataMap[handle];
		var boneCount = skeleton.BoneCount;

		var mutations = animation.BoneMutationDescriptors.Buffer;
		var translationKeys = animation.TranslationKeyframes.Buffer;
		var rotationKeys = animation.RotationKeyframes.Buffer;
		var scalingKeys = animation.ScalingKeyframes.Buffer;

		var globalTransformsBuffer = _applyTransformsBuffer.Rent<Matrix4x4>(boneCount);
		var outputTransformsBuffer = _applyTransformsBuffer.Rent<Matrix4x4>(boneCount);
		
		try {
			var globalTransforms = globalTransformsBuffer.AsSpan<Matrix4x4>(boneCount);
			var outputTransforms = outputTransformsBuffer.AsSpan<Matrix4x4>(boneCount);

			var defaultLocalTransforms = skeleton.DefaultLocalTransforms.Buffer;
			var bindPoseInversionMatrices = skeleton.BindPoseInversions.Buffer;
			var parentIndices = skeleton.ParentIndices.Buffer;

			// Step 1: Use outputTransforms to calcualte local transforms (for now)
			for (var i = 0; i < boneCount; ++i) {
				outputTransforms[i] = defaultLocalTransforms[i];
				Console.WriteLine($"\tSTEP 1 | Index #{i} | outputTransforms[{i}] = {MatStr(outputTransforms[i])}");
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
					var midpointIndex = (lowIndex + highIndex) >> 1;
					if (keys[midpointIndex].TimeKeySeconds <= targetTimeSecs) lowIndex = midpointIndex;
					else highIndex = midpointIndex;
				}
				
				var low = keys[lowIndex];
				var high = keys[highIndex];
				Console.WriteLine($"Interp({typeof(TValue).Name}): {lowIndex}->{highIndex}@{Real.GetInterpolationDistance(low.TimeKeySeconds, high.TimeKeySeconds, targetTimeSecs)}");
				return TValue.Interpolate(
					low.Value,
					high.Value, 
					Real.GetInterpolationDistance(low.TimeKeySeconds, high.TimeKeySeconds, targetTimeSecs)
				);
			}
			
			for (var m = 0; m < mutations.Length; ++m) {
				var mutation = mutations[m];
				var transform = new Transform(
					scaling: InterpolateKeyframes<SkeletalAnimationScalingKeyframe, Vect>(scalingKeys.Slice(mutation.ScalingKeyframeStartIndex, mutation.ScalingKeyframeCount), targetTimePointSeconds),
					rotation: InterpolateKeyframes<SkeletalAnimationRotationKeyframe, Rotation>(rotationKeys.Slice(mutation.RotationKeyframeStartIndex, mutation.RotationKeyframeCount), targetTimePointSeconds),
					translation: InterpolateKeyframes<SkeletalAnimationTranslationKeyframe, Vect>(translationKeys.Slice(mutation.TranslationKeyframeStartIndex, mutation.TranslationKeyframeCount), targetTimePointSeconds)
				);

				transform.ToMatrix(ref outputTransforms[mutation.TargetNodeIndex]);
				Console.WriteLine($"\tSTEP 2 | Mutation #{m} | mutation = {mutation} | transform = {transform} | outputTransforms[{mutation.TargetNodeIndex}] = {MatStr(outputTransforms[mutation.TargetNodeIndex])}");
			}

			// Step 3: Accumulate global transforms and compute final output
			for (var i = 0; i < boneCount; ++i) {
				var originalBoneIndex = skeleton.OriginalIndices.Buffer[i];
				var parentBoneIndex = parentIndices[i];
				Console.Write($"\tSTEP 3 | Index #{i} | originalBoneIndex = {originalBoneIndex} | parentBoneIndex = {parentBoneIndex} | outputTransforms[{i}] = {MatStr(outputTransforms[i])}");
				outputTransforms[i] = parentBoneIndex < 0 ? outputTransforms[i] : outputTransforms[parentBoneIndex] * outputTransforms[i];
				Console.WriteLine($" | outputTransformsAFTER_PARENT[{i}] = {MatStr(outputTransforms[i])}");
			}
			
			for (var i = 0; i < boneCount; ++i) {
				outputTransforms[i] *= bindPoseInversionMatrices[i];
				Console.WriteLine($"\tSTEP 4 | outputTransforms[{i}] = {MatStr(outputTransforms[i])}");
			}

			Console.Write("\tFINAL OUTPUT: ");
			foreach (var m in outputTransformsBuffer.AsReadOnlySpan<Matrix4x4>(boneCount)) {
				Console.Write(MatStr(m) + "   ");
			}
			Console.WriteLine();
			
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
			data.BoneMutationDescriptors.Dispose();
			data.TranslationKeyframes.Dispose();
			data.RotationKeyframes.Dispose();
			data.ScalingKeyframes.Dispose();
		}
		_dataMap.Clear();
		_nameMap.Clear();
		
		_currentSkeleton?.BindPoseInversions.Dispose();
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
