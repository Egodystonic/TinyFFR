// Created on 2026-09-03 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Meshes.Local;

static class SkeletalMeshUtils {
	public static int ProcessRawNodeData(
		ReadOnlySpan<SkeletalAnimationNode> skeletalNodes,
		int boneCount,
		Matrix4x4 modelImportTransformMatrix,
		Span<int> depthsScratch,
		Span<int> processingOrderScratch,
		Span<Matrix4x4> defaultLocalTransforms,
		Span<Matrix4x4> bindPoseInversions,
		Span<int> parentIndices,
		Span<int> boneToNodeMap,
		Span<int> inputToOutputIndexMap
	) {
		// Maintainer's note: This first step calculates the optimal processing order of the nodes so we can do a single-pass
		// calculation of the animation transform matrices in Apply().
		// This algorithm works by determining the dependency depth of each bone index.
		// Bones with the same depth can't rely on each other, so a simple ordering of bones by dependency depth means
		// we'll always process them in the right order down below when applying an animation.
		// (The reordering itself is necessary as when processing the animation, some transformations rely on transforms
		// calculated on parent bones higher up the skeletal hierarchy first).
		
		var nodeCount = skeletalNodes.Length;
		var maxDepth = 0;

		depthsScratch.Clear();
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
				maxDepth = Int32.Max(maxDepth, ++depthsScratch[i]);
				parentNodeIndex = skeletalNodes[pni].ParentNodeIndex;
			}
		}

		var processingOrderCursor = 0;
		for (var curDepthToSearchFor = 0; curDepthToSearchFor <= maxDepth; ++curDepthToSearchFor) {
			for (var i = 0; i < nodeCount; ++i) {
				if (depthsScratch[i] == curDepthToSearchFor) {
					inputToOutputIndexMap[i] = processingOrderCursor;
					processingOrderScratch[processingOrderCursor++] = i;
				}
			}
		}

		var firstParentedNodeIndex = 0;
		for (var outputIndex = 0; outputIndex < nodeCount; ++outputIndex) {
			var inputIndex = processingOrderScratch[outputIndex];
			defaultLocalTransforms[outputIndex] = skeletalNodes[inputIndex].DefaultLocalTransform;

			if (skeletalNodes[inputIndex].ParentNodeIndex is { } parentNodeIndex) {
				parentIndices[outputIndex] = inputToOutputIndexMap[parentNodeIndex];
			}
			else firstParentedNodeIndex = outputIndex + 1;

			if (skeletalNodes[inputIndex].CorrespondingBoneIndex is { } boneIndex) {
				boneToNodeMap[boneIndex] = outputIndex;
				bindPoseInversions[boneIndex] = skeletalNodes[inputIndex].BindPoseInversion;
			}
		}

		if (Matrix4x4.Invert(modelImportTransformMatrix, out var inverseModelImportTransformMatrix)) {
			for (var i = 0; i < boneCount; ++i) {
				bindPoseInversions[i] = inverseModelImportTransformMatrix * bindPoseInversions[i];
			}
		}

		return firstParentedNodeIndex;
	}

	public static void ApplySkeletalTransformHierarchy(ReadOnlySpan<int> parentIndices, int firstParentedNodeIndex, Matrix4x4 modelImportTransformMatrix, Span<Matrix4x4> workspace) {
		for (var i = 0; i < firstParentedNodeIndex; ++i) {
			workspace[i] *= modelImportTransformMatrix;
		}

		for (var i = firstParentedNodeIndex; i < workspace.Length; ++i) {
			workspace[i] *= workspace[parentIndices[i]];
		}
	}

	public static void ApplyBindPoseInversions(ReadOnlySpan<Matrix4x4> bindPoseInversions, ReadOnlySpan<int> boneToNodeMap, ReadOnlySpan<Matrix4x4> workspace, Span<Matrix4x4> destination) {
		for (var i = 0; i < bindPoseInversions.Length; ++i) {
			destination[i] = bindPoseInversions[i] * workspace[boneToNodeMap[i]];
		}
	}

	public static TValue InterpolateKeyframes<T, TValue>(ReadOnlySpan<T> keys, float targetTimeSecs) where T : IAnimationKeyframe<TValue> {
		if (keys.Length == 0) return T.FallbackValue;
		if (keys.Length == 1 || targetTimeSecs <= keys[0].TimeKeySeconds) return keys[0].Value;
		if (targetTimeSecs >= keys[^1].TimeKeySeconds) return keys[^1].Value;

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

	public static Transform GetInterpolatedMutationTransform(
		in SkeletalAnimationNodeMutationDescriptor mutation,
		ReadOnlySpan<SkeletalAnimationScalingKeyframe> scalingKeys,
		ReadOnlySpan<SkeletalAnimationRotationKeyframe> rotationKeys,
		ReadOnlySpan<SkeletalAnimationTranslationKeyframe> translationKeys,
		float targetTimePointSeconds
	) {
		return new Transform(
			scaling: InterpolateKeyframes<SkeletalAnimationScalingKeyframe, Vect>(scalingKeys.Slice(mutation.ScalingKeyframeStartIndex, mutation.ScalingKeyframeCount), targetTimePointSeconds),
			rotationQuaternion: InterpolateKeyframes<SkeletalAnimationRotationKeyframe, Quaternion>(rotationKeys.Slice(mutation.RotationKeyframeStartIndex, mutation.RotationKeyframeCount), targetTimePointSeconds),
			translation: InterpolateKeyframes<SkeletalAnimationTranslationKeyframe, Vect>(translationKeys.Slice(mutation.TranslationKeyframeStartIndex, mutation.TranslationKeyframeCount), targetTimePointSeconds)
		);
	}

	public static void InterpolateKeyframesInToWorkspace(
		ReadOnlySpan<SkeletalAnimationNodeMutationDescriptor> mutations,
		ReadOnlySpan<SkeletalAnimationScalingKeyframe> scalingKeys,
		ReadOnlySpan<SkeletalAnimationRotationKeyframe> rotationKeys,
		ReadOnlySpan<SkeletalAnimationTranslationKeyframe> translationKeys,
		float targetTimePointSeconds,
		ReadOnlySpan<int> targetNodeIndexRemap,
		Span<Matrix4x4> workspace
	) {
		for (var i = 0; i < mutations.Length; ++i) {
			var mutation = mutations[i];
			var targetIndex = targetNodeIndexRemap.IsEmpty ? mutation.TargetNodeIndex : targetNodeIndexRemap[mutation.TargetNodeIndex];
			GetInterpolatedMutationTransform(in mutation, scalingKeys, rotationKeys, translationKeys, targetTimePointSeconds)
				.ToMatrix(out workspace[targetIndex]);
		}
	}

	public static readonly Location EmptyBoundsMinimum = new(Single.PositiveInfinity, Single.PositiveInfinity, Single.PositiveInfinity);
	public static readonly Location EmptyBoundsMaximum = new(Single.NegativeInfinity, Single.NegativeInfinity, Single.NegativeInfinity);

	static Location Min(Location a, Location b) => Location.FromVector3(Vector3.Min(a.ToVector3(), b.ToVector3()));
	static Location Max(Location a, Location b) => Location.FromVector3(Vector3.Max(a.ToVector3(), b.ToVector3()));

	public static void CalculateBoneLocalBounds(
		ReadOnlySpan<MeshVertexSkeletal> vertices,
		ReadOnlySpan<Matrix4x4> bindPoseInversions,
		Span<Location> boneMinimums,
		Span<Location> boneMaximums,
		Span<bool> boneInfluenceMap
	) {
		boneInfluenceMap.Clear();
		for (var b = 0; b < bindPoseInversions.Length; ++b) {
			boneMinimums[b] = EmptyBoundsMinimum;
			boneMaximums[b] = EmptyBoundsMaximum;
		}

		for (var v = 0; v < vertices.Length; ++v) {
			var vertex = vertices[v];
			var position = vertex.Location;
			var indices = vertex.BoneIndices;
			var weights = vertex.BoneWeights;

			for (var i = 0; i < MeshVertexSkeletal.MaxBonesPerVertex; ++i) {
				if (weights[i] <= 0f) continue;
				var boneIndex = indices[i];
				if (boneIndex >= bindPoseInversions.Length) continue;

				var boneLocalPosition = position * new Transform(bindPoseInversions[boneIndex]);
				boneMinimums[boneIndex] = Min(boneMinimums[boneIndex], boneLocalPosition);
				boneMaximums[boneIndex] = Max(boneMaximums[boneIndex], boneLocalPosition);
				boneInfluenceMap[boneIndex] = true;
			}
		}
	}

	public static void CalculateBindPoseSkinnedBounds(
		ReadOnlySpan<MeshVertexSkeletal> vertices,
		ReadOnlySpan<Matrix4x4> boneMatrices,
		ref Location minimum,
		ref Location maximum
	) {
		for (var v = 0; v < vertices.Length; ++v) {
			var vertex = vertices[v];
			var position = vertex.Location;
			var indices = vertex.BoneIndices;
			var weights = vertex.BoneWeights;

			var accumulator = Vect.Zero;
			var totalWeight = 0f;
			for (var i = 0; i < MeshVertexSkeletal.MaxBonesPerVertex; ++i) {
				var weight = weights[i];
				if (weight <= 0f) continue;
				var boneIndex = indices[i];
				if (boneIndex >= boneMatrices.Length) continue;

				accumulator += (position * new Transform(boneMatrices[boneIndex])).AsVect() * weight;
				totalWeight += weight;
			}

			if (totalWeight <= 0f) {
				minimum = Min(minimum, Min(position, Location.Origin));
				maximum = Max(maximum, Max(position, Location.Origin));
			}
			else {
				minimum = Min(minimum, accumulator.AsLocation());
				maximum = Max(maximum, accumulator.AsLocation());
			}
		}
	}

	public static void ExpandBoundsByPose(
		ReadOnlySpan<Location> boneMinimums,
		ReadOnlySpan<Location> boneMaximums,
		ReadOnlySpan<bool> boneInfluenceMap,
		ReadOnlySpan<int> boneToNodeMap,
		ReadOnlySpan<Matrix4x4> workspace,
		ref Location minimum,
		ref Location maximum
	) {
		for (var b = 0; b < boneInfluenceMap.Length; ++b) {
			if (!boneInfluenceMap[b]) continue;

			var boneMinimum = boneMinimums[b];
			var boneMaximum = boneMaximums[b];
			var nodeTransform = new Transform(workspace[boneToNodeMap[b]]);

			for (var cornerIndex = 0; cornerIndex < 8; ++cornerIndex) {
				var corner = new Location(
					(cornerIndex & 1) == 0 ? boneMinimum.X : boneMaximum.X,
					(cornerIndex & 2) == 0 ? boneMinimum.Y : boneMaximum.Y,
					(cornerIndex & 4) == 0 ? boneMinimum.Z : boneMaximum.Z
				);
				var transformedCorner = corner * nodeTransform;
				minimum = Min(minimum, transformedCorner);
				maximum = Max(maximum, transformedCorner);
			}
		}
	}

	public static bool AllInfluencingBoneMatricesAreMirrored(ReadOnlySpan<Matrix4x4> boneMatrices, ReadOnlySpan<bool> boneInfluenceMap) {
		var foundAny = false;
		for (var b = 0; b < boneInfluenceMap.Length; ++b) {
			if (!boneInfluenceMap[b]) continue;
			if (boneMatrices[b].GetDeterminant() >= 0f) return false;
			foundAny = true;
		}
		return foundAny;
	}

	public static PositionedCuboid BoundsToCuboid(Location minimum, Location maximum) {
		if (minimum.X > maximum.X || minimum.Y > maximum.Y || minimum.Z > maximum.Z) return PositionedCuboid.UnitCubeAtOrigin;

		return new PositionedCuboid(
			maximum.X - minimum.X,
			maximum.Y - minimum.Y,
			maximum.Z - minimum.Z,
			((minimum.AsVect() + maximum.AsVect()) * 0.5f).AsLocation()
		);
	}
}

readonly struct SkeletalBoundsWorkspace : IDisposable {
	readonly PooledHeapMemory<Matrix4x4> _defaultLocalTransforms;
	readonly PooledHeapMemory<Matrix4x4> _pose;
	readonly PooledHeapMemory<Matrix4x4> _bindPoseInversions;
	readonly PooledHeapMemory<Matrix4x4> _boneMatrices;
	readonly PooledHeapMemory<Location> _boneMinimums;
	readonly PooledHeapMemory<Location> _boneMaximums;
	readonly PooledHeapMemory<bool> _boneInfluenceMap;
	readonly PooledHeapMemory<int> _parentIndices;
	readonly PooledHeapMemory<int> _inputToOutputIndexMap;
	readonly PooledHeapMemory<int> _boneToNodeMap;
	readonly Matrix4x4 _modelImportTransformMatrix;
	readonly int _nodeCount;
	readonly int _boneCount;
	readonly int _firstParentedNodeIndex;

	ReadOnlySpan<Matrix4x4> DefaultLocalTransforms => _defaultLocalTransforms.Span[.._nodeCount];
	Span<Matrix4x4> Pose => _pose.Span[.._nodeCount];
	ReadOnlySpan<Matrix4x4> BindPoseInversions => _bindPoseInversions.Span[.._boneCount];
	Span<Matrix4x4> BoneMatrices => _boneMatrices.Span[.._boneCount];
	Span<Location> BoneMinimums => _boneMinimums.Span[.._boneCount];
	Span<Location> BoneMaximums => _boneMaximums.Span[.._boneCount];
	Span<bool> BoneInfluenceMap => _boneInfluenceMap.Span[.._boneCount];
	ReadOnlySpan<int> ParentIndices => _parentIndices.Span[.._nodeCount];
	ReadOnlySpan<int> InputToOutputIndexMap => _inputToOutputIndexMap.Span[.._nodeCount];
	ReadOnlySpan<int> BoneToNodeMap => _boneToNodeMap.Span[.._boneCount];

	public SkeletalBoundsWorkspace(IHeapPool pool, ReadOnlySpan<SkeletalAnimationNode> skeletalNodes, int boneCount, Matrix4x4 modelImportTransformMatrix) {
		_nodeCount = skeletalNodes.Length;
		_boneCount = boneCount;
		_modelImportTransformMatrix = modelImportTransformMatrix;

		_defaultLocalTransforms = pool.Borrow<Matrix4x4>(_nodeCount);
		_pose = pool.Borrow<Matrix4x4>(_nodeCount);
		_parentIndices = pool.Borrow<int>(_nodeCount);
		_inputToOutputIndexMap = pool.Borrow<int>(_nodeCount);
		_bindPoseInversions = pool.Borrow<Matrix4x4>(boneCount);
		_boneToNodeMap = pool.Borrow<int>(boneCount);
		_boneMatrices = pool.Borrow<Matrix4x4>(boneCount);
		_boneMinimums = pool.Borrow<Location>(boneCount);
		_boneMaximums = pool.Borrow<Location>(boneCount);
		_boneInfluenceMap = pool.Borrow<bool>(boneCount);

		using var depths = pool.Borrow<int>(_nodeCount);
		using var processingOrder = pool.Borrow<int>(_nodeCount);

		_firstParentedNodeIndex = SkeletalMeshUtils.ProcessRawNodeData(
			skeletalNodes,
			boneCount,
			modelImportTransformMatrix,
			depths.Span[.._nodeCount],
			processingOrder.Span[.._nodeCount],
			_defaultLocalTransforms.Span[.._nodeCount],
			_bindPoseInversions.Span[..boneCount],
			_parentIndices.Span[.._nodeCount],
			_boneToNodeMap.Span[..boneCount],
			_inputToOutputIndexMap.Span[.._nodeCount]
		);

		BoneInfluenceMap.Clear();

		DefaultLocalTransforms.CopyTo(Pose);
		ResolvePoseFromLocalTransforms();
		SkeletalMeshUtils.ApplyBindPoseInversions(BindPoseInversions, BoneToNodeMap, Pose, BoneMatrices);
	}

	void ResolvePoseFromLocalTransforms() {
		SkeletalMeshUtils.ApplySkeletalTransformHierarchy(ParentIndices, _firstParentedNodeIndex, _modelImportTransformMatrix, Pose);
	}

	public void AddBindPoseBounds(ReadOnlySpan<MeshVertexSkeletal> vertices, ref Location minimum, ref Location maximum) {
		SkeletalMeshUtils.CalculateBindPoseSkinnedBounds(vertices, BoneMatrices, ref minimum, ref maximum);
	}

	public void PrepareBoneLocalBounds(ReadOnlySpan<MeshVertexSkeletal> vertices) {
		SkeletalMeshUtils.CalculateBoneLocalBounds(vertices, BindPoseInversions, BoneMinimums, BoneMaximums, BoneInfluenceMap);
	}

	public void ApplyAnimationPose(
		ReadOnlySpan<SkeletalAnimationNodeMutationDescriptor> mutations,
		ReadOnlySpan<SkeletalAnimationScalingKeyframe> scalingKeys,
		ReadOnlySpan<SkeletalAnimationRotationKeyframe> rotationKeys,
		ReadOnlySpan<SkeletalAnimationTranslationKeyframe> translationKeys,
		float timePointSeconds
	) {
		DefaultLocalTransforms.CopyTo(Pose);
		SkeletalMeshUtils.InterpolateKeyframesInToWorkspace(mutations, scalingKeys, rotationKeys, translationKeys, timePointSeconds, InputToOutputIndexMap, Pose);
		ResolvePoseFromLocalTransforms();
	}

	public void ExpandBoundsByCurrentPose(ref Location minimum, ref Location maximum) {
		SkeletalMeshUtils.ExpandBoundsByPose(BoneMinimums, BoneMaximums, BoneInfluenceMap, BoneToNodeMap, Pose, ref minimum, ref maximum);
	}

	public bool AllInfluencingBonesAreMirrored => SkeletalMeshUtils.AllInfluencingBoneMatricesAreMirrored(BoneMatrices, BoneInfluenceMap);

	public void Dispose() {
		_boneInfluenceMap.Dispose();
		_boneMaximums.Dispose();
		_boneMinimums.Dispose();
		_boneMatrices.Dispose();
		_boneToNodeMap.Dispose();
		_bindPoseInversions.Dispose();
		_inputToOutputIndexMap.Dispose();
		_parentIndices.Dispose();
		_pose.Dispose();
		_defaultLocalTransforms.Dispose();
	}
}
